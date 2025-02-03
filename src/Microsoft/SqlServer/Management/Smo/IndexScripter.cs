// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;
using System.Linq;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class Index
    {
        private abstract class IndexScripter
        {
            protected ScriptSchemaObjectBase parent;
            protected Index index;
            protected ScriptingPreferences preferences;
            protected ColumnCollection columns;
            public bool TableCreate { get; set; }

            private const string NODE_ID = "$node_id";
            private const string EDGE_ID = "$edge_id";
            private const string TO_ID = "$to_id";
            private const string FROM_ID = "$from_id";

            public IndexScripter(Index index, ScriptingPreferences sp)
            {
                this.index = index;
                this.parent = (ScriptSchemaObjectBase)index.ParentColl.ParentInstance;
                Diagnostics.TraceHelper.Assert(null != this.parent, "parent == null");

                this.preferences = sp;

                // By default, the 'IndexScripter' is not for scripting a CREATE TABLE statement.
                //
                this.TableCreate = false;

                if (this.parent is TableViewTableTypeBase)
                {
                    this.columns = ((TableViewTableTypeBase)this.parent).Columns;
                }
                else if (this.parent is UserDefinedFunction)
                {
                    this.columns = ((UserDefinedFunction)this.parent).Columns;
                }
                else
                {
                    Diagnostics.TraceHelper.Assert(false, "Invalid parent");
                }
            }

            public bool? IsClustered
            {
                get
                {
                    //For backward compatiblity we need to following order
                    // Dirty IndexType over dirty IsClustered followed by non dirty IndexType then non dirty IsClustered
                    if (index.GetPropValueOptional<IndexType>("IndexType").HasValue
                        && (!index.GetPropertyOptional("IsClustered").Dirty
                            || index.GetPropertyOptional("IndexType").Dirty))
                    {
                        return (index.GetPropValueOptional<IndexType>("IndexType") == IndexType.ClusteredIndex);
                    }
                    else
                    {
                        return index.GetPropValueOptional<bool>("IsClustered");
                    }
                }
            }

            protected virtual bool IsIncludedColumnSupported
            {
                get { return false; }
            }

            #region Validation
            protected abstract void Validate();

            protected void CheckConflictingProperties()
            {
                //For backward compatiblity we need to check index type and clustered are conflicting if both are dirty
                //because previously existing code can just set a clustered index to nonclustered and recreate or vice-versa
                if (index.GetPropValueOptional<IndexType>("IndexType").HasValue
                    && index.GetPropertyOptional("IndexType").Dirty
                    && index.GetPropValueOptional<bool>("IsClustered").HasValue
                    && index.GetPropertyOptional("IsClustered").Dirty)
                {
                    if ((index.GetPropValueOptional<IndexType>("IndexType").Value == IndexType.ClusteredIndex) != index.GetPropValueOptional<bool>("IsClustered"))
                    {
                        throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingIndexProperties, "IsClustered", index.GetPropValueOptional<bool>("IsClustered").ToString(), "IndexType", index.GetPropValueOptional<IndexType>("IndexType").ToString()));
                    }
                }
            }

            protected void CheckProperty<T>(string propertyName, T defaultValue, Exception exception)
            {
                if (index.IsSupportedProperty(propertyName))
                {
                    Property prop = index.Properties.Get(propertyName);
                    if (!prop.IsNull && !EqualityComparer<T>.Default.Equals((T)prop.Value, defaultValue))
                    {
                        throw exception;
                    }
                }
            }

            protected void CheckProperty<T>(T propertyValue, T defaultValue, Exception exception)
            {
                if (propertyValue != null && !EqualityComparer<T>.Default.Equals(propertyValue, defaultValue))
                {
                    throw exception;
                }
            }

            /// <summary>
            /// Check spatial properties are not set for non spatial index
            /// </summary>
            protected void CheckSpatialProperties()
            {
                if (index.IsSupportedProperty("IsSpatialIndex"))
                {
                    Exception exception = new SmoException(ExceptionTemplates.UnsupportedNonSpatialParameters);

                    this.CheckProperty<SpatialIndexType>("SpatialIndexType", SpatialIndexType.None, exception);
                    this.CheckProperty<double>("BoundingBoxXMin", m_boundingBoxDef, exception);
                    this.CheckProperty<double>("BoundingBoxYMin", m_boundingBoxDef, exception);
                    this.CheckProperty<double>("BoundingBoxXMax", m_boundingBoxDef, exception);
                    this.CheckProperty<double>("BoundingBoxYMax", m_boundingBoxDef, exception);
                    this.CheckProperty<int>("CellsPerObject", m_cellsPerObjectDef, exception);
                    this.CheckProperty<SpatialGeoLevelSize>("Level1Grid", SpatialGeoLevelSize.None, exception);
                    this.CheckProperty<SpatialGeoLevelSize>("Level2Grid", SpatialGeoLevelSize.None, exception);
                    this.CheckProperty<SpatialGeoLevelSize>("Level3Grid", SpatialGeoLevelSize.None, exception);
                    this.CheckProperty<SpatialGeoLevelSize>("Level4Grid", SpatialGeoLevelSize.None, exception);
                }
            }

            /// <summary>
            /// Check xml properties are not set for non xml index
            /// </summary>
            protected void CheckXmlProperties()
            {
                if (index.IsSupportedProperty("IsXmlIndex"))
                {
                    Exception exception = new SmoException(ExceptionTemplates.UnsupportedNonXmlParameters);

                    this.CheckProperty<string>("ParentXmlIndex", string.Empty, exception);
                    this.CheckProperty<SecondaryXmlIndexType>("SecondaryXmlIndexType", SecondaryXmlIndexType.None, exception);
                }
            }

            /// <summary>
            /// Check non clustered properties are not set for non applicable index
            /// </summary>
            protected void CheckNonClusteredProperties()
            {
                if (index.IsSupportedProperty("FilterDefinition"))
                {
                    Exception exception = new SmoException(ExceptionTemplates.NotNonClusteredIndex);

                    this.CheckProperty<string>("FilterDefinition", string.Empty, exception);
                }
            }

            /// <summary>
            /// Check clustered properties are not set for non applicable index
            /// </summary>
            protected void CheckClusteredProperties()
            {
                if (index.IsSupportedProperty("FileStreamFileGroup"))
                {
                    Exception exception = new SmoException(ExceptionTemplates.NotClusteredIndex);

                    this.CheckProperty<string>("FileStreamFileGroup", string.Empty, exception);
                    this.CheckProperty<string>("FileStreamPartitionScheme", string.Empty, exception);
                }
            }

            /// <summary>
            /// Check row index properties are not set for non applicable index
            /// </summary>
            protected virtual void CheckRegularIndexProperties()
            {
                this.CheckProperty<bool>("IsClustered", false, new SmoException(ExceptionTemplates.NoIndexClustered));
                this.CheckProperty<bool>("IsUnique", false, new SmoException(ExceptionTemplates.NoIndexUnique));
                this.CheckProperty<bool>("IgnoreDuplicateKeys", false, new SmoException(ExceptionTemplates.NoIndexIgnoreDupKey));
                this.CheckProperty<bool>("IsOptimizedForSequentialKey", false,
                    new SmoException(ExceptionTemplates.NoIndexOptimizeForSequentialKey));
            }

            /// <summary>
            /// Check constraint properties are not set for non constraint index
            /// </summary>
            protected void CheckConstraintProperties()
            {
                this.CheckProperty<IndexKeyType>("IndexKeyType", IndexKeyType.None, new WrongPropertyValueException(index.Properties.Get("IndexKeyType")));

                if (!(parent is TableViewBase) && !(parent is UserDefinedTableType))
                {
                    throw new SmoException(ExceptionTemplates.IndexOnTableView);
                }

                //We have given exemption for Name field during SetParent operation (which calls
                //UpdateObjectState) in case of PrimaryKey and Unique constraint. However,
                //we need to validate this for other kind of indexes.
                if (index.IsDesignMode && index.GetIsSystemNamed())
                {
                    throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.PropertyNotSet("Name", typeof(Index).Name));
                }
            }
            #endregion

            #region Create Script
            /// <summary>
            /// Generates the index script.
            /// </summary>
            /// <returns>The generated index script.</returns>
            public virtual string GetCreateScript()
            {
                this.Validate();

                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                this.ScriptIndexHeader(sb);
                this.ScriptColumns(sb);
                this.ScriptIndexDetails(sb);

                StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                this.ScriptIndexOptions(withClause);
                this.ScriptDistribution(withClause);
                if (withClause.Length > 0)
                {
                    withClause.Length = withClause.Length - Globals.commaspace.Length;

                    sb.Append("WITH ");
                    if (preferences.TargetServerVersion != SqlServerVersion.Version80)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", withClause.ToString());
                    }
                    else
                    {
                        sb.Append(withClause.ToString());
                    }
                }

                this.ScriptIndexStorage(sb);
                return sb.ToString();
            }

            protected virtual void ScriptIndexHeader(StringBuilder sb)
            {
                if (!(this.index.IsSqlDwIndex && this.TableCreate))
                {
                    index.ScriptIncludeHeaders(sb, preferences, Index.UrnSuffix);
                    this.ScriptExistenceCheck(sb, true);
                }

                this.ScriptCreateHeaderDdl(sb);
            }

            protected virtual void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                throw new NotImplementedException();
            }

            protected virtual void ScriptIndexStorage(StringBuilder sb)
            {
                index.GenerateDataSpaceScript(sb, preferences);
            }

            protected void ScriptFileStream(StringBuilder sb)
            {
                if (index.IsSupportedProperty("FileStreamFileGroup", preferences))
                {
                    index.GenerateDataSpaceFileStreamScript(sb, preferences, false);
                }
            }

            protected virtual void ScriptIndexDetails(StringBuilder sb)
            {
                return;
            }

            protected virtual void ScriptIndexOptions(StringBuilder sb)
            {
                ScriptIndexOptions(sb, false, -1);
            }

            private void ScriptIndexOptions(StringBuilder sb, bool forRebuild, int rebuildPartitionNumber)
            {
                // ======================================================================================================================================
                // Here is the option table for constraint/index creation as applied to the next code section
                // ======================================================================================================================================
                //                                              ALTER TABLE...  CREATE TABLE
                // Index/constraint option	CREATE INDEX    	ADD CONSTRAINT  + CONSTRAINT
                //PAD_INDEX	                8/9	                9               9
                //FILLFACTOR	            8/9	                8/9             8/9
                //IGNORE_DUP_KEY	        8/9                 9               9
                //STATISTICS_NORECOMPUTE	8/9                 9               9
                //ALLOW_ROW_LOCKS	        9                   9               9
                //ALLOW_PAGE_LOCKS	        9                   9               9
                //SORT_IN_TEMPDB	        8/9	                9               -
                //ONLINE	                9	                9               -
                //MAXDOP	                9	                9               -
                //DROP_EXISTING	            8/9	                -               -
                //MAXDURATION               14                  14              14
                //RESUMABLE                 15                  16               -
                //OPTIMIZE_FOR_SEQUENTIAL_KEY 15                15              15
                // ======================================================================================================================================

                // options not valid for a stretch db target or sql dw db target in azure
                if (!preferences.TargetEngineIsAzureStretchDb() && !preferences.TargetEngineIsAzureSqlDw())
                {
                    // These options are not valid for 8.0 PK/UK constraints.

                    if (!forRebuild || rebuildPartitionNumber == -1)
                    {
                        if (preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                        {
                            this.ScriptIndexOption(sb, "PAD_INDEX", GetOnOffValue(index.GetPropValueOptional<bool>("PadIndex")));
                        }
                        this.ScriptIndexOption(sb, "STATISTICS_NORECOMPUTE", GetOnOffValue(index.GetPropValueOptional<bool>("NoAutomaticRecomputation")));
                    }

                    if (preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                    {
                        this.ScriptIndexOption(sb, "SORT_IN_TEMPDB", GetOnOffValue(index.sortInTempdb));
                    }

                    if ((!forRebuild || (rebuildPartitionNumber == -1))
                        && index.GetPropValueOptional<bool>("IsUnique", false)
                        && (index.GetPropValueOptional<IndexKeyType>("IndexKeyType", IndexKeyType.None) == IndexKeyType.None))
                    {
                        this.ScriptIndexOption(sb, "IGNORE_DUP_KEY", GetOnOffValue(index.GetPropValueOptional<bool>("IgnoreDuplicateKeys")));
                    }
                }

                // index options are not supported for the inlined index on SQL DW tables
                if (!forRebuild && !(this.index.IsSqlDwIndex && this.TableCreate))
                {
                    this.ScriptIndexOption(sb, "DROP_EXISTING", GetOnOffValue(index.dropExistingIndex));
                }

                // options not valid for a stretch db target or sql dw db target in azure
                if (!preferences.TargetEngineIsAzureStretchDb() && !preferences.TargetEngineIsAzureSqlDw())
                {
                    // The resumable option is valid for rebuild as of SQL version 140, and available for Create Index as of SQL version 150.
                    // Even if the script targets a higher version of SQL, do not script the option unless we are operating against at least
                    // server version 140 or Azure Sql DB, as otherwise the property will throw an error.
                    // Can be omitted if unset.
                    //
                    if (((forRebuild && preferences.TargetServerVersion >= SqlServerVersion.Version140) ||
                        (preferences.TargetServerVersion >= SqlServerVersion.Version150 && !this.TableCreate && !this.preferences.ScriptForAlter) ||
                        (index.DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase && !this.TableCreate && !this.preferences.ScriptForAlter)) &&
                        (VersionUtils.IsSql14OrLater(index.ServerVersion) || index.DatabaseEngineType ==  Cmn.DatabaseEngineType.SqlAzureDatabase) &&
                        index.ResumableIndexOperation)
                    {
                        this.ScriptIndexOption(sb, "RESUMABLE", GetOnOffValue(index.ResumableIndexOperation));
                        if (index.ResumableMaxDuration != 0) // MAX_DURATION can be omitted.
                        {
                            this.ScriptIndexOption(sb, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");
                        }
                    }

                    if (preferences.TargetServerVersion >= SqlServerVersion.Version90)
                    {
                        bool forCreateIndex = !forRebuild && !this.TableCreate && !this.preferences.ScriptForAlter;
                        if (!forRebuild || rebuildPartitionNumber == -1)
                        {
                            this.ScriptIndexOptionOnline(sb, forRebuild, forCreateIndex);

                            if (preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                            {
                                this.ScriptIndexOption(sb, "ALLOW_ROW_LOCKS", GetOnOffValue(RevertMeaning(index.GetPropValueOptional<bool>("DisallowRowLocks"))));
                                this.ScriptIndexOption(sb, "ALLOW_PAGE_LOCKS", GetOnOffValue(RevertMeaning(index.GetPropValueOptional<bool>("DisallowPageLocks"))));
                            }
                        }
                        else if (VersionUtils.IsSql12OrLater(index.ServerVersion))
                        {
                            // Single Partition Online Index Rebuild is supported for version
                            // 12 and later.
                            this.ScriptIndexOptionOnline(sb, forRebuild, forCreateIndex);
                        }

                        if (index.ServerVersion.Major >= 9 && index.MaximumDegreeOfParallelism > 0)
                        {
                            this.ScriptIndexOption(sb, "MAXDOP", index.MaximumDegreeOfParallelism);
                        }
                    }
                    if (!forRebuild || rebuildPartitionNumber == -1)
                    {
                        this.ScriptFillFactor(sb);
                    }

                    // OPTIMIZE_FOR_SEQUENTIAL_KEY is only supported for regular B-Tree indexes.
                    //
                    if (!forRebuild && (this is RegularIndexScripter))
                    {
                        this.ScriptOptimizeForSequentialKey(sb);
                    }
                }
            }

            protected void ScriptIndexOptionOnline(StringBuilder sb)
            {
                ScriptIndexOptionOnline(sb, forRebuild:false, forCreateIndex:false);
            }

            /// <summary>
            /// Scripts the ONLINE index option.
            /// </summary>
            /// <param name="sb">The StringBuilder to append the options</param>
            /// <param name="forRebuild">Specifies if this is a rebuild operation</param>
            /// <param name="forCreateIndex"></param>
            protected void ScriptIndexOptionOnline(StringBuilder sb, bool forRebuild, bool forCreateIndex)
            {
                if (!preferences.TargetEngineIsAzureStretchDb())
                {
                    StringBuilder onlinePropertiesSb = new StringBuilder(GetOnOffValue(index.onlineIndexOperation));

                    // Add the WAIT_AT_LOW_PRIORITY options.
                    ScriptWaitAtLowPriorityIndexOption(onlinePropertiesSb, forRebuild, forCreateIndex);

                    this.ScriptIndexOption(sb, "ONLINE", onlinePropertiesSb.ToString());
                }
            }

            /// <summary>
            /// Scripts the WAIT_AT_LOW_PRIORITY option for the Online Index Rebuild.
            /// </summary>
            /// <param name="sb">The StringBuilder to append the options</param>
            /// <param name="forRebuild">Specifies if this is a rebuild operation</param>
            /// <param name="forCreateIndex">Specifies if this is a create operation</param>
            protected void ScriptWaitAtLowPriorityIndexOption(StringBuilder sb, bool forRebuild, bool forCreateIndex)
            {
                // WAIT_AT_LOW_PRIORITY is only supported for versions 12 or later and only for
                // online rebuild.
                // For Azure DB (and later version 160), WAIT_AT_LOW_PRIORITY is also supported for online create index
                if (((VersionUtils.IsSql12OrLater(index.ServerVersion) && forRebuild) ||
                        (index.DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase && forCreateIndex)) &&
                        index.onlineIndexOperation)
                {
                    sb.AppendFormat(
                        SmoApplication.DefaultCulture,
                        " (WAIT_AT_LOW_PRIORITY (MAX_DURATION = {0} MINUTES, ABORT_AFTER_WAIT = {1}))",
                        index.lowPriorityMaxDuration,
                        index.LowPriorityAbortAfterWait.ToString().ToUpper());
                }
            }

            /// <summary>
            /// Scripts the WAIT_AT_LOW_PRIORITY option for the index Drop and Resume
            /// </summary>
            /// <param name="sb">The StringBuilder to append the options</param>
            protected void ScriptWaitAtLowPriorityIndexOptionForDropAndResume(StringBuilder sb)
            {
                // WAIT_AT_LOW_PRIORITY is only supported on server version 14 or later
                if (!VersionUtils.IsSql14OrLater(index.ServerVersion))
                {
                    return;
                }

                // Omit the options when the maxdop is equal to 0.
                if (index.LowPriorityMaxDuration == 0)
                {
                    return;
                }

                // For the Drop operation, WAIT_AT_LOW_PRIORITY is not supported for the clustered and offline index.
                // This restriction wouldn't impact the Resume operation, because it only supports the resumable index that should be online.
                if (index.IsClustered && !index.OnlineIndexOperation)
                {
                    throw new SmoException(ExceptionTemplates.LowPriorityCannotBeSetForDrop);
                }

                var converter = new AbortAfterWaitConverter();
                sb.AppendFormat(
                        SmoApplication.DefaultCulture,
                        "WAIT_AT_LOW_PRIORITY (MAX_DURATION = {0} MINUTES, ABORT_AFTER_WAIT = {1})",
                        index.LowPriorityMaxDuration,
                        converter.ConvertToInvariantString(index.LowPriorityAbortAfterWait));
                sb.Append(Globals.commaspace);
            }

            protected void ScriptFillFactor(StringBuilder sb)
            {
                if (index.IsSupportedProperty(nameof(index.FillFactor), preferences))
                {
                    Property prop = index.Properties.Get(nameof(index.FillFactor));
                    if (null != prop.Value && !(parent is UserDefinedTableType))    //No fill factor for UDTT
                    {
                        if ((byte)prop.Value != 0) // this check is needed because Fillfactor 0 is not a valid percentage; fillfactor must be between 1 and 100.
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "FILLFACTOR = {0}", prop.Value);
                            sb.Append(Globals.commaspace);
                        }
                    }
                }
            }

            /// <summary>
            /// Scripts the OPTIMIZE_FOR_SEQUENTIAL_KEY option if the corresponding property is supported
            /// on the source and target server (Version150 or later) and the property is set.
            /// </summary>
            /// <param name="sb">The StringBuilder to append the options</param>
            /// <param name="checkDirty">If true and the property is actually Dirty, script the option</param>
            protected void ScriptOptimizeForSequentialKey(StringBuilder sb, bool checkDirty = false)
            {
                const string optionName = "OPTIMIZE_FOR_SEQUENTIAL_KEY";
                const string propertyName = "IsOptimizedForSequentialKey";

                if (index.IsSupportedProperty(propertyName, preferences))
                {
                    if (checkDirty)
                    {
                        this.ScriptIndexOption(sb, optionName, GetOnOffValue(GetDirtyPropValueOptional<bool>(propertyName)));
                    }
                    else
                    {
                        this.ScriptIndexOption(sb, optionName, GetOnOffValue(index.GetPropValueOptional<bool>(propertyName)));
                    }
                }
            }

            protected bool? RevertMeaning(bool? propvalue)
            {
                if (propvalue.HasValue)
                {
                    return !propvalue.Value;
                }
                else
                {
                    return null;
                }
            }

            protected string GetOnOffValue(bool? propValue)
            {
                if (propValue.HasValue)
                {
                    return propValue.Value ? "ON" : (preferences.TargetServerVersion == SqlServerVersion.Version80) ? null : "OFF";
                }
                else
                {
                    return null;
                }
            }

            protected void ScriptIndexOption(StringBuilder sb, string optname, object propValue)
            {
                bool withEqual = preferences.TargetServerVersion != SqlServerVersion.Version80;

                if (propValue != null)
                {
                    if (withEqual)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0} = {1}", optname, propValue);
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", optname);
                    }
                    sb.Append(Globals.commaspace);
                }
            }

            /// <summary>
            /// Scripts the index columns.
            /// </summary>
            /// <param name="sb">The script to append the scripted index columns.</param>
            protected virtual void ScriptColumns(StringBuilder sb)
            {
                ISet<string> includedColumnSet = new HashSet<String>();

                sb.Append(preferences.NewLine);

                // add a tab for SQL DW inline index formatting
                if (this.index.IsSqlDwIndex && this.TableCreate)
                {
                    sb.Append(Globals.tab);
                }

                sb.Append(Globals.LParen);
                sb.Append(preferences.NewLine);

                StringBuilder columnsString = new StringBuilder();
                int columnsToSkip = 0;

                foreach (IndexedColumn col in index.IndexedColumns)
                {
                    Column colBase = columns[col.Name];

                    if (columnsToSkip > 0)
                    {
                        // The column that will be skipped should exist and either be the
                        // to_id, or the from_id column.
                        //
                        if (colBase == null)
                        {
                            // This should not happen. This is a product bug where
                            // the graph columns are not properly created. The node
                            // and edge ids should be of the form ('id, 'computed')
                            // and the edge from and to ids should be of the form ('obj_id', 'id', 'computed').
                            //
                            throw new InvalidSmoOperationException(ExceptionTemplates.ExpectedGraphColumnNotFound);
                        }

                        // In order to skip columns the "GraphType" property must be supported, as we only increment
                        // the 'columnToSkip' counter if "GraphType" is supported. This makes using the 'GetPropValueOptional' method
                        // ok to use in this context.
                        //
                        if (colBase.GetPropValueOptional("GraphType", GraphType.None) != GraphType.GraphToId &&
                            colBase.GraphType != GraphType.GraphFromId)
                        {
                            // This should not happen! This is a product bug where
                            // the graph columns are not properly created. The node
                            // and edge ids should be of the form ('id, 'computed')
                            // and the edge from and to ids should be of the form ('obj_id', 'id', 'computed').
                            //
                            throw new InvalidSmoOperationException(ExceptionTemplates.ExpectedGraphColumnNotFound);
                        }

                        columnsToSkip--;

                        continue;
                    }

                    bool isIncluded = col.GetPropValueOptional<bool>("IsIncluded", false);

                    // Included columns are scripted separately
                    //
                    if (isIncluded)
                    {
                        Table parentTable = colBase.Parent as Table;

                        GraphType columnGraphType = colBase.GetPropValueIfSupportedWithThrowOnTarget("GraphType", GraphType.None, preferences);

                        switch (columnGraphType)
                        {
                            case GraphType.GraphId:
                                includedColumnSet.Add(parentTable.IsNode ? NODE_ID : EDGE_ID);
                                break;
                            case GraphType.GraphToObjId:
                                includedColumnSet.Add(TO_ID);
                                break;
                            case GraphType.GraphFromObjId:
                                includedColumnSet.Add(FROM_ID);
                                break;
                            case GraphType.GraphToId:
                                includedColumnSet.Add(TO_ID);
                                break;
                            case GraphType.GraphFromId:
                                includedColumnSet.Add(FROM_ID);
                                break;
                            default:
                                includedColumnSet.Add(col.Name);
                                break;
                        }
                        continue;
                    }

                    // The behavior we want here for graph columns is to notice if the indexed
                    // column is a real column. If it is this index could have been refreshed
                    // with the actual underlying internal columns for the indexed columns.
                    // The internal column names are not allowed in any CREATE INDEX statement,
                    // so they must be replaced with the appropriate pseudo column and the appropriate
                    // number columns must be skipped. The columns that will be skipped depend on the
                    // type of column. If the column is a graph id type, the next column will be the
                    // computed column that needs to be skipped. If the column is a to object id column
                    // the following column will be a to id column to skip. If the column is a from object
                    // id column then the following column will be a from id column that must be skipped.
                    //
                    if (colBase != null)
                    {
                        GraphType columnGraphType = colBase.GetPropValueIfSupportedWithThrowOnTarget("GraphType", GraphType.None, preferences);

                        if (columnGraphType == GraphType.GraphId)
                        {
                            // Here we need to script the $node_id or $edge_id pseudo column.
                            //
                            Table parentTable = colBase.Parent as Table;

                            if (parentTable != null)
                            {
                                columnsString.Append(Globals.tab);
                                columnsString.Append(parentTable.IsNode ? NODE_ID : EDGE_ID);
                                columnsString.Append(Globals.comma);
                                columnsString.Append(preferences.NewLine);
                                continue;
                            }
                        }

                        if (columnGraphType == GraphType.GraphToObjId)
                        {
                            // This case is an index on the $to_id pseudo column. If the graph to object id
                            // is present the following column will be the graph id which needs to be
                            // skipped for scripting. '$to_id' logically maps to both of these columns.
                            //
                            columnsString.Append(Globals.tab);
                            columnsString.Append(TO_ID);
                            columnsString.Append(Globals.comma);
                            columnsString.Append(preferences.NewLine);
                            columnsToSkip = 1;
                            continue;
                        }

                        if (columnGraphType == GraphType.GraphFromObjId)
                        {
                            // This case is an index on the $from_id pseudo column. If the graph from obj id
                            // is present the following column will be the graph id which needs to be
                            // skipped for scripting. '$from_id' logically maps to both of these columns.
                            //
                            columnsString.Append(Globals.tab);
                            columnsString.Append(FROM_ID);
                            columnsString.Append(Globals.comma);
                            columnsString.Append(preferences.NewLine);
                            columnsToSkip = 1;
                            continue;
                        }
                    }

                    if (this.ScriptColumn(col, columnsString))
                    {
                        columnsString.Append(Globals.comma);
                        columnsString.Append(preferences.NewLine);
                    }
                }

                if (columnsString.Length < 1 && includedColumnSet.Count > 0)
                {
                    // We enter this block in a rare but possible edge case
                    // wherein an index was created with all included columns and none
                    // regular user defined columns (perhaps programmatically via SMO).
                    // To script such an index we treat the included columns
                    // as regular user defined columns.
                    //
                    string formattedColNames = string.Join(Globals.comma + preferences.NewLine, from string columnName in includedColumnSet
                                               select string.Format(SmoApplication.DefaultCulture, "{0}",
                                               Globals.tab + ((columnName.Equals(NODE_ID)
                                               || columnName.Equals(EDGE_ID)
                                               || columnName.Equals(TO_ID)
                                               || columnName.Equals(FROM_ID)) ? columnName : MakeSqlBraket(columnName))));

                    columnsString.Append(formattedColNames);
                    columnsString.Append(Globals.comma);
                    columnsString.Append(preferences.NewLine);
                    includedColumnSet.Clear();
                }

                if (columnsString.Length > 0)
                {
                    columnsString.Length = columnsString.Length - (preferences.NewLine.Length + Globals.comma.Length);
                }
                else
                {
                    throw new SmoException(ExceptionTemplates.NoObjectWithoutColumns("Index"));
                }

                sb.Append(columnsString.ToString());
                sb.Append(preferences.NewLine);

                // add a tab for SQL DW inline index formatting
                if (this.index.IsSqlDwIndex && this.TableCreate)
                {
                    sb.Append(Globals.tab);
                }
                sb.Append(Globals.RParen);

                ScriptPseudoColumn(includedColumnSet, sb);
            }

            /// <summary>
            /// Pseudo columns of graph tables are included as a part of includes clause of index DDLs,
            /// this method handles the script generation of these pseudo columns.
            /// </summary>
            /// <param name="includedColumnSet">Reference of a set containing names of included columns</param>
            /// <param name="sb">Reference of scripting StringBuilder on which the included columns script has to be included</param>
            private void ScriptPseudoColumn(ISet<string> includedColumnSet, StringBuilder sb)
            {
                if (includedColumnSet == null || includedColumnSet.Count < 1)
                {
                    return;
                }

                sb.Append(Globals.newline);
                sb.Append("INCLUDE");

                sb.Append(Globals.LParen);
                sb.Append(string.Join(",", from string columnName in includedColumnSet select (string.Format(SmoApplication.DefaultCulture, "{0}", columnName.Equals(NODE_ID) || columnName.Equals(EDGE_ID) || columnName.Equals(TO_ID) || columnName.Equals(FROM_ID) ? columnName : MakeSqlBraket(columnName)))));
                sb.Append(Globals.RParen + Globals.space);
            }

            /// <summary>
            /// Scripts out the indexed column.
            /// </summary>
            /// <param name="col">The indexed column.</param>
            /// <param name="sb">The script builder to append the columns to.</param>
            /// <returns></returns>
            protected virtual bool ScriptColumn(IndexedColumn col, StringBuilder sb)
            {
                //IsIncludedColumnSupported
                if (index.ServerVersion.Major > 9 && col.GetPropValueOptional<bool>("IsIncluded", false) && !this.IsIncludedColumnSupported)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.IncludedColumnNotSupported);
                }

                if (col.IsSupportedProperty("IsComputed"))
                {
                    index.m_bIsOnComputed = (!index.m_bIsOnComputed) ?
                    col.GetPropValueOptional<bool>("IsComputed", false) : index.m_bIsOnComputed;
                }

                // we check to see if the specified column exists,
                Column colBase = columns[col.Name];

                sb.Append(Globals.tab);

                // add a tab for SQL DW inline index formatting
                if (this.index.IsSqlDwIndex && this.TableCreate)
                {
                    sb.Append(Globals.tab);
                }

                // use proper name for scripting
                if (null != colBase)
                {
                    // In order to create the index with the graph computed column it must not
                    // be in brackets so the algebrizer can recognize it correctly.
                    //
                    if (colBase.IsGraphComputedColumn() || IsGraphPseudoColumn(col.Name))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", colBase.GetName(preferences));
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", (SqlBraket(colBase.GetName(preferences))));
                    }

                    this.index.isOnColumnWithAnsiPadding |= colBase.GetPropValueOptional<bool>("AnsiPaddingStatus", false);
                }
                else
                {
                    // In order to create the index with the graph computed column it must not
                    // be in brackets so the algebrizer can recognize it correctly.
                    //
                    if (IsGraphPseudoColumn(col.Name))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", col.Name);
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", (SqlBraket(col.Name)));
                    }
                }

                return true;
            }

            protected void ScriptColumnOrder(IndexedColumn col, StringBuilder sb)
            {
                bool? desc = col.GetPropValueOptional<bool>("Descending");
                if (desc.HasValue)
                {
                    sb.Append(desc.Value ? " DESC" : " ASC");
                }
            }

            protected void ScriptCompression(StringBuilder sb)
            {
                //code for data compression
                //SQL Datawarehouse and SQL StretchDatabase are special cases here - all indexes created are compressed but
                // they don't actually support the DATA_COMPRESSION option so we need to specifically leave
                // it out
                bool fDataCompression = this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse
                   && this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlStretchDatabase
                   && this.index.IsSupportedProperty("HasCompressedPartitions", this.preferences)
                   && this.preferences.Storage.DataCompression
                   && this.index.IsCompressionCodeRequired(false);
                bool fXmlCompression = this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse
                    && this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlStretchDatabase
                    && this.index.IsSupportedProperty(nameof(HasXmlCompressedPartitions), this.preferences)
                    && this.preferences.Storage.XmlCompression
                    && this.index.IsXmlCompressionCodeRequired(false);

                if (fDataCompression && fXmlCompression)
                {
                    // As suggested by DP team (sumesh). Reference VSTS# 930159
                    // update the default Init Fields for a PhysicalPartition object just before
                    // we  fetch PhysicalPartitions during Table scripting
                    Smo.Server srv = this.index.GetServerObject();
                    string[] fields = { "PartitionNumber", "DataCompression", "XmlCompression" };
                    srv.SetDefaultInitFields(typeof(PhysicalPartition), this.index.DatabaseEngineEdition, fields);

                    string dataCompressionClause = this.index.PhysicalPartitions.GetCompressionCode(false, false,
                        this.preferences);
                    string xmlCompressionClause = this.index.PhysicalPartitions.GetXmlCompressionCode(false, false,
                        this.preferences);

                    if (string.IsNullOrEmpty(dataCompressionClause) == false)
                    {
                        sb.Append(this.index.PhysicalPartitions.GetCompressionCode(false, false, this.preferences));
                        sb.Append(Globals.commaspace);
                    }
                    if (!string.IsNullOrEmpty(xmlCompressionClause))
                    {
                        sb.Append(this.index.PhysicalPartitions.GetXmlCompressionCode(false, false, this.preferences));
                        sb.Append(Globals.commaspace);
                    }
                }
                else if (fDataCompression)
                {
                    // As suggested by DP team (sumesh). Reference VSTS# 930159
                    // update the default Init Fields for a PhysicalPartition object just before
                    // we  fetch PhysicalPartitions during Table scripting
                    Smo.Server srv = this.index.GetServerObject();
                    string[] fields = { "PartitionNumber", "DataCompression" };
                    srv.SetDefaultInitFields(typeof(PhysicalPartition), this.index.DatabaseEngineEdition, fields);

                    string compressionClause = this.index.PhysicalPartitions.GetCompressionCode(false, false,
                        this.preferences);
                    if (string.IsNullOrEmpty(compressionClause) == false)
                    {
                        sb.Append(this.index.PhysicalPartitions.GetCompressionCode(false, false, this.preferences));
                        sb.Append(Globals.commaspace);
                    }
                }
                else if (fXmlCompression)
                {
                    // As suggested by DP team (sumesh). Reference VSTS# 930159
                    // update the default Init Fields for a PhysicalPartition object just before
                    // we  fetch PhysicalPartitions during Table scripting
                    Smo.Server srv = this.index.GetServerObject();
                    string[] fields = { "PartitionNumber", "XmlCompression" };
                    srv.SetDefaultInitFields(typeof(PhysicalPartition), this.index.DatabaseEngineEdition, fields);

                    if (fXmlCompression)
                    {
                        string xmlCompressionClause = this.index.PhysicalPartitions.GetXmlCompressionCode(false, false,
                            this.preferences);
                        if (string.IsNullOrEmpty(xmlCompressionClause) == false)
                        {
                            sb.Append(this.index.PhysicalPartitions.GetXmlCompressionCode(false, false, this.preferences));
                            sb.Append(Globals.commaspace);
                        }
                    }
                }
            }

            protected void ScriptDistribution(StringBuilder sb)
            {
            }

            /// <summary>
            /// Script the filter predicate
            /// </summary>
            /// <param name="sb">The string builder to hold the script</param>
            protected void ScriptFilter(StringBuilder sb)
            {
                //Where clause for FILTER. The filter clause is supported only on a nonclustered index.
                if (index.IsSupportedProperty("FilterDefinition", preferences))
                {
                    string FilterDefinition = index.Properties.Get("FilterDefinition").Value as string;
                    if (!string.IsNullOrEmpty(FilterDefinition) && !string.IsNullOrEmpty(FilterDefinition.Trim()))
                    {
                        sb.Append(preferences.NewLine);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "WHERE {0}", FilterDefinition.Trim());
                        sb.Append(preferences.NewLine);
                    }
                }
            }


            #endregion

            #region Drop Script

            public string GetDropScript()
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                index.ScriptIncludeHeaders(sb, preferences, Index.UrnSuffix);

                if (this.preferences.TargetServerVersion < SqlServerVersion.Version130 ||
                    this.index.IsMemoryOptimizedIndex)
                {
                    this.ScriptExistenceCheck(sb, false);
                }
                else if (this is ConstraintScripter)
                {
                    if (this.preferences.IncludeScripts.ExistenceCheck)
                    {
                        // Existence of parent table is not checked when ALTER TABLE DROP CONSTRAINT IF EXISTS syntax is used.
                        // Check is added here to keep behavior same as in previous versions.
                        //
                        sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "",
                            SqlString(this.parent.FormatFullNameForScripting(preferences)));
                        sb.Append(this.preferences.NewLine);
                    }
                }

                this.ScriptDropHeaderDdl(sb);

                if (this.preferences.TargetServerVersion > SqlServerVersion.Version80)
                {
                    this.ScriptDropOptions(sb);
                }

                return sb.ToString();
            }

            protected virtual void ScriptDropOptions(StringBuilder sb)
            {
                return;
            }

            protected virtual void ScriptDropHeaderDdl(StringBuilder sb)
            {
                if (this.preferences.TargetServerVersion <= SqlServerVersion.Version80)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "DROP INDEX {0}.{1}",
                                        this.parent.FormatFullNameForScripting(this.preferences),
                                        index.FormatFullNameForScripting(this.preferences));
                }
                else if (this.index.IsMemoryOptimizedIndex)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP INDEX {1}",
                                        this.parent.FormatFullNameForScripting(this.preferences),
                                        index.FormatFullNameForScripting(this.preferences));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "DROP INDEX {0}{1} ON {2}",
                                        (this.preferences.IncludeScripts.ExistenceCheck &&
                                         this.preferences.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                                        index.FormatFullNameForScripting(this.preferences),
                                        this.parent.FormatFullNameForScripting(this.preferences));
                }
            }

            #endregion

            #region Rebuild Script

            /// <summary>
            /// Get the tsql script of the index rebuild operation.
            /// </summary>
            public string GetRebuildScript(bool allIndexes, int rebuildPartitionNumber)
            {
                //this.Validate();

                if (index.ServerVersion.Major < 9 || (parent is View && ((View)parent).Parent.CompatibilityLevel < CompatibilityLevel.Version90))
                {
                    return Rebuild80(allIndexes);
                }
                else
                {
                    StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                    if (allIndexes)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX ALL ON {0} REBUILD",
                                             parent.FullQualifiedName);
                    }
                    else
                    {
                        StringBuilder partitionOption = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                        if (rebuildPartitionNumber != -1)
                        {
                            partitionOption.AppendFormat(SmoApplication.DefaultCulture, "PARTITION = {0}", rebuildPartitionNumber);
                        }
                        else
                        {
                            if ((this.preferences.TargetServerVersion >= SqlServerVersion.Version100))
                            {
                                partitionOption.AppendFormat(SmoApplication.DefaultCulture, "PARTITION = ALL");
                            }
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX [{0}] ON {1} REBUILD {2}",
                                        SqlBraket(index.Name), parent.FullQualifiedName, partitionOption.ToString());
                    }

                    StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    this.ScriptIndexRebuildOptions(withClause, rebuildPartitionNumber);

                    if (!allIndexes && rebuildPartitionNumber == -1)
                    {
                        this.ScriptDistribution(withClause);
                    }

                    if (withClause.Length > 0)
                    {
                        withClause.Length = withClause.Length - Globals.commaspace.Length;

                        sb.Append(" WITH ");
                        if (preferences.TargetServerVersion != SqlServerVersion.Version80)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", withClause.ToString());
                        }
                        else
                        {
                            sb.Append(withClause.ToString());
                        }
                    }
                    return sb.ToString();
                }
            }

            protected virtual void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                this.ScriptIndexOptions(withClause, true, rebuildPartitionNumber);
            }

            private string Rebuild80(bool allIndexes)
            {
                // pre 9.0 we were using DBCC
                if (allIndexes)
                {
                    return string.Format(SmoApplication.DefaultCulture, "DBCC DBREINDEX(N'{0}')",
                                SqlString(parent.FullQualifiedName));
                }
                else
                {
                    return string.Format(SmoApplication.DefaultCulture, "DBCC DBREINDEX(N'{0}', N'{1}', {2})",
                                    SqlString(parent.FullQualifiedName),
                                    SqlString(index.Name),
                                    (byte)index.Properties["FillFactor"].Value);
                }
            }

            #endregion

            #region Resume Script

            /// <summary>
            /// Get the tsql script of the index resume operation.
            /// </summary>
            public string GetResumeScript()
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX [{0}] ON {1} RESUME",
                                      SqlBraket(index.Name), parent.FullQualifiedName);

                StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                this.ScriptIndexResumeOptions(withClause);

                if (withClause.Length > 0)
                {
                    withClause.Length = withClause.Length - Globals.commaspace.Length;
                    sb.Append(" WITH ");
                    sb.AppendFormat(SmoApplication.DefaultCulture, "({0})", withClause.ToString());
                }

                return sb.ToString();
            }

            /// <summary>
            /// Get the options for the resume operation. </summary>
            /// <param name="sb">String builder for collecting the options</param>
            private void ScriptIndexResumeOptions(StringBuilder sb)
            {
                // =====================================================================
                // Here is the option table for resume index
                // =====================================================================
                // Index option             Minimum version
                // MAXDOP                   14
                // MAXDURATION              14
                // WAIT_AT_LOW_PRIORITY     14
                // =====================================================================
                if (index.MaximumDegreeOfParallelism > 0)
                {
                    this.ScriptIndexOption(sb, "MAXDOP", index.MaximumDegreeOfParallelism);
                }

                if (index.ResumableMaxDuration != 0) // MAX_DURATION can be omitted.
                {
                    this.ScriptIndexOption(sb, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");
                }

                // WAIT_AT_LOW_PRIORITY for Resume
                this.ScriptWaitAtLowPriorityIndexOptionForDropAndResume(sb);
            }

            #endregion

            #region AbortOrPause Script

            /// <summary>
            /// Get the tsql script of the index abort/pause operation.
            /// </summary>
            public string GetAbortOrPauseScript(bool isAbort)
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX [{0}] ON {1} {2}",
                    SqlBraket(index.Name), parent.FullQualifiedName, isAbort? "ABORT": "PAUSE");
                return sb.ToString();
            }

            #endregion

            #region Alter Script
            public string GetAlterScript90(bool allIndexes)
            {
                StringBuilder setClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                this.ScriptSetIndexOptions(setClause);

                if (setClause.Length > 0)
                {
                    setClause.Length = setClause.Length - Globals.commaspace.Length;

                    return string.Format(SmoApplication.DefaultCulture, "ALTER INDEX {0} ON {1} SET ( {2} )",
                        allIndexes ? "ALL" : index.FullQualifiedName, parent.FullQualifiedName, setClause.ToString());
                }

                StringBuilder scriptScriptAlterDetails = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                this.ScriptAlterDetails(scriptScriptAlterDetails);

                if (this is HashIndexScripter)
                {
                    return string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} ALTER INDEX {1} {2}",
                        this.parent.FormatFullNameForScripting(this.preferences),
                        index.FormatFullNameForScripting(this.preferences),
                        scriptScriptAlterDetails.ToString());
                }

                if (scriptScriptAlterDetails.Length > 0)
                {
                    return string.Format(SmoApplication.DefaultCulture, "ALTER INDEX {0} ON {1} {2}",
                        allIndexes ? "ALL" : index.FullQualifiedName, parent.FullQualifiedName, scriptScriptAlterDetails.ToString());
                }

                return string.Empty;

            }

            protected virtual void ScriptAlterDetails(StringBuilder sb)
            {
                return;
            }

            protected virtual void ScriptSetIndexOptions(StringBuilder setClause)
            {
                if (!preferences.TargetEngineIsAzureStretchDb())
                {
                    this.ScriptIndexOption(setClause, "ALLOW_ROW_LOCKS", GetOnOffValue(RevertMeaning(GetDirtyPropValueOptional<bool>("DisallowRowLocks"))));
                    this.ScriptIndexOption(setClause, "ALLOW_PAGE_LOCKS", GetOnOffValue(RevertMeaning(GetDirtyPropValueOptional<bool>("DisallowPageLocks"))));
                    this.ScriptIndexOption(setClause, "STATISTICS_NORECOMPUTE", GetOnOffValue(GetDirtyPropValueOptional<bool>("NoAutomaticRecomputation")));
                    this.ScriptIndexOption(setClause, "IGNORE_DUP_KEY", GetOnOffValue(GetDirtyPropValueOptional<bool>("IgnoreDuplicateKeys")));
                }

                // OPTIMIZE_FOR_SEQUENTIAL_KEY is only supported for regular B-Tree indexes.
                //
                if (this is RegularIndexScripter)
                {
                    this.ScriptOptimizeForSequentialKey(setClause, checkDirty: true);
                }
            }

            protected Nullable<T> GetDirtyPropValueOptional<T>(string propName) where T : struct
            {
                if (index.Properties.Get(propName).Dirty)
                {
                    return index.GetPropValueOptional<T>(propName);
                }
                else
                {
                    return null;
                }
            }
            #endregion

            protected void ScriptExistenceCheck(StringBuilder sb ,bool not)
            {
                if (this.preferences.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                        (this.preferences.TargetServerVersion >= SqlServerVersion.Version90 ?
                        Scripts.INCLUDE_EXISTS_INDEX90 : Scripts.INCLUDE_EXISTS_INDEX80),
                        not ? "NOT" : string.Empty,
                                SqlString(this.parent.FormatFullNameForScripting(preferences)),
                                this.index.FormatFullNameForScripting(this.preferences, false));

                    sb.Append(this.preferences.NewLine);
                }
            }

            public static IndexScripter GetIndexScripterForCreate(Index index, ScriptingPreferences sp)
            {
                index.isOnColumnWithAnsiPadding = false;

                if (index.IsMemoryOptimizedIndex)
                {
                    return IndexScripter.GetMemoryOptimizedIndexScripter(index, sp);
                }
                if (index.IsSqlDwIndex)
                {
                    return IndexScripter.GetSqlDwIndexScripter(index, sp);
                }

                //In case of creating and design mode if IndexType is missing and cannot be retreived so we infer it
                //Otherwise if it is present or existing we can retreive it we will use it
                //But if IsClustered is dirty and IndexType is untouched we have to infer IndexType and not use property bag one.
                else if (
                        (!index.GetPropValueOptional<IndexType>("IndexType").HasValue && (index.State == SqlSmoState.Creating || index.IsDesignMode))
                    ||  (
                            index.GetPropValueOptional<IndexType>("IndexType").Value.Equals(IndexType.NonClusteredColumnStoreIndex)
                        &&  index.GetPropertyOptional("IsClustered").Dirty && !index.GetPropertyOptional("IndexType").Dirty
                        )
                    )
                {
                    return IndexScripter.GetCreatingIndexScripterForCreate(index, sp);
                }
                else
                {
                    return IndexScripter.GetExistingIndexScripterForCreate(index, sp);
                }
            }

            private static IndexScripter GetCreatingIndexScripterForCreate(Index index, ScriptingPreferences sp)
            {
                ColumnCollection columns = index.Parent.Urn.Type.Equals(UserDefinedFunction.UrnSuffix) ?
                    ((UserDefinedFunction)index.Parent).Columns : ((TableViewTableTypeBase)index.Parent).Columns;

                //loop through the indexed columns and find if there are any xml or spatial columns
                foreach (IndexedColumn col in index.IndexedColumns)
                {
                    // we check to see if the specified column exists,
                    Column colBase = columns[col.Name];
                    if (null == colBase)
                    {
                        // for views we only enforce checking if the view is not creating as it may not have the columns populated
                        if (!(index.Parent.State == SqlSmoState.Creating && index.Parent is View))
                        {
                            // the column does not exist, so we need to abort this scripting
                            ScriptSchemaObjectBase parentobj = index.Parent as TableViewTableTypeBase;
                            throw new SmoException(ExceptionTemplates.ObjectRefsNonexCol(UrnSuffix, index.Name, parentobj.FullQualifiedName + ".[" + SqlStringBraket(col.Name) + "]"));
                        }
                    }
                    else
                    {
                        if (colBase.IgnoreForScripting)
                        {
                            // flag this object to be ignored for scripting and return from the function
                            index.IgnoreForScripting = true;
                            return null;
                        }
                        else if (col.IsSupportedProperty("IsIncluded") && !col.GetPropValueOptional<bool>("IsIncluded", false) && colBase.DataType.SqlDataType == SqlDataType.Xml)
                        {
                            if (!string.IsNullOrEmpty(index.GetPropValueOptional("ParentXmlIndex", string.Empty).ToString()))
                            {
                                return new SecondaryXmlIndexScripter(index, sp);
                            }
                            else
                            {
                                return new PrimaryXmlIndexScripter(index, sp);
                            }
                        }
                        else if (col.IsSupportedProperty("IsIncluded") && !col.GetPropValueOptional<bool>("IsIncluded", false) && index.IsSpatialColumn(colBase))
                        {
                            return new SpatialIndexScripter(index, sp);
                        }
                    }
                }

                if ((GetSupportedPropertyValue<IndexKeyType>(index, "IndexKeyType", IndexKeyType.None) != IndexKeyType.None)
                    && !index.dropExistingIndex && !index.IsMemoryOptimizedIndex)
                {
                    return new ConstraintScripter(index, sp);
                }
                else if (GetSupportedPropertyValue<bool>(index, "IsClustered", false))
                {
                    return new ClusteredRegularIndexScripter(index, sp);
                }
                else
                {
                    return new NonClusteredRegularIndexScripter(index, sp);
                }
            }

            private static IndexScripter GetExistingIndexScripterForCreate(Index index, ScriptingPreferences sp)
            {
                if ((GetSupportedPropertyValue<IndexKeyType>(index, "IndexKeyType", IndexKeyType.None) != IndexKeyType.None)
                    && !index.dropExistingIndex && !index.IsMemoryOptimizedIndex)
                {
                    return new ConstraintScripter(index, sp);
                }
                else
                {
                    return GetIndexScripter(index, sp);
                }
            }

            public static IndexScripter GetIndexScripterForDrop(Index index, ScriptingPreferences sp)
            {
                Property typeProp = index.Properties.Get("IndexKeyType");
                IndexKeyType kt = (IndexKeyType)index.GetPropValueOptional("IndexKeyType", IndexKeyType.None);

                // if someone changed the key type, we need to get the right one, so that we can generate
                // the proper drop script
                if (typeProp.Dirty && index.State != SqlSmoState.Creating)
                {
                    kt = (IndexKeyType)index.GetRealValue(typeProp, index.oldIndexKeyTypeValue);
                }

                if (kt != IndexKeyType.None)
                {
                    return new ConstraintScripter(index, sp);
                }
                else if (GetSupportedPropertyValue<IndexType>(index, "IndexType", IndexType.NonClusteredIndex) == IndexType.ClusteredColumnStoreIndex)
                {
                    return new ClusteredColumnstoreIndexScripter(index, sp);
                }
                else if ((GetSupportedPropertyValue<IndexType>(index, "IndexType", IndexType.NonClusteredIndex) == IndexType.ClusteredIndex)
                    || GetSupportedPropertyValue<bool>(index, "IsClustered", false))
                {
                    return new ClusteredRegularIndexScripter(index, sp);
                }
                else
                {
                    return new NonClusteredRegularIndexScripter(index, sp);
                }
            }

            /// <summary>
            /// Gets the IndexScripter for a Memory Optimized index based on the Index Type
            /// </summary>
            /// <param name="index"></param>
            /// <param name="sp"></param>
            /// <returns></returns>
            public static IndexScripter GetMemoryOptimizedIndexScripter(Index index, ScriptingPreferences sp)
            {
                switch (index.InferredIndexType)
                {
                    case IndexType.NonClusteredIndex:
                        return new RangeIndexScripter(index, sp);
                    case IndexType.NonClusteredHashIndex:
                        return new HashIndexScripter(index, sp);
                    case IndexType.ClusteredColumnStoreIndex:
                        return new ClusteredColumnstoreIndexScripter(index, sp);
                    default:
                        throw new InvalidSmoOperationException(ExceptionTemplates.TableMemoryOptimizedIndexDependency);
                }
            }

            /// <summary>
            /// Gets the IndexScripter for a SQL DW index based on the Index Type.
            /// </summary>
            /// <param name="index">The SQL DW index.</param>
            /// <param name="sp">The scripting preferences.</param>
            /// <returns>The IndexScripter for the SQL DW index type.</returns>
            public static IndexScripter GetSqlDwIndexScripter(Index index, ScriptingPreferences sp)
            {
                // Get scripters for primary and unique keys
                switch (index.IndexKeyType)
                {
                    case IndexKeyType.DriPrimaryKey:
                    case IndexKeyType.DriUniqueKey:
                        return new ConstraintScripter(index, sp);
                    case IndexKeyType.None:
                        break;
                }

                switch (index.InferredIndexType)
                {
                    case IndexType.ClusteredColumnStoreIndex:
                        return new ClusteredColumnstoreIndexScripter(index, sp);
                    case IndexType.ClusteredIndex:
                        return new ClusteredRegularIndexScripter(index, sp);
                    case IndexType.NonClusteredIndex:
                        return new NonClusteredRegularIndexScripter(index, sp);
                    default:
                        throw new InvalidSmoOperationException(ExceptionTemplates.TableSqlDwIndexTypeRestrictions(index.InferredIndexType.ToString()));
                }
            }

            public static IndexScripter GetIndexScripter(Index index, ScriptingPreferences sp)
            {
                if (index.IsMemoryOptimizedIndex)
                {
                    return IndexScripter.GetMemoryOptimizedIndexScripter(index, sp);
                }

                if (index.IsSqlDwIndex)
                {
                    return IndexScripter.GetSqlDwIndexScripter(index, sp);
                }

                switch (index.InferredIndexType)
                {
                    case IndexType.ClusteredIndex:
                        if (index.Parent is UserDefinedTableType)
                        {
                            // Support inline index on disk based user defined table type.
                            return new UserDefinedTableTypeIndexScripter(index, sp);
                        }
                        else
                        {
                            return new ClusteredRegularIndexScripter(index, sp);
                        }
                    case IndexType.NonClusteredIndex:
                        if (index.Parent is UserDefinedTableType)
                        {
                            // Support inline index on disk based user defined table type.
                            return new UserDefinedTableTypeIndexScripter(index, sp);
                        }
                        else
                        {
                            return new NonClusteredRegularIndexScripter(index, sp);
                        }
                    case IndexType.PrimaryXmlIndex:
                        return new PrimaryXmlIndexScripter(index, sp);
                    case IndexType.SecondaryXmlIndex:
                        return new SecondaryXmlIndexScripter(index, sp);
                    case IndexType.SpatialIndex:
                        return new SpatialIndexScripter(index, sp);
                    case IndexType.NonClusteredColumnStoreIndex:
                        return new NonClusteredColumnStoreIndexScripter(index, sp);
                    case IndexType.NonClusteredHashIndex:
                        //Only hekaton tables can have Hash Indexes
                        throw new InvalidSmoOperationException(ExceptionTemplates.HashIndexTableDependency);
                    case IndexType.SelectiveXmlIndex:
                        return new SelectiveXMLIndexScripter(index,sp);
                    case IndexType.SecondarySelectiveXmlIndex:
                        return new SecondarySelectiveXMLIndexScripter(index, sp);
                    case IndexType.ClusteredColumnStoreIndex:
                        return new ClusteredColumnstoreIndexScripter(index, sp);
                    default:
                        throw new WrongPropertyValueException(index.Properties["IndexType"]);
                }
            }

            public static IndexScripter GetIndexScripterForAlter(Index index, ScriptingPreferences sp)
            {

                if (GetSupportedPropertyValue<IndexType>(index, "IndexType", IndexType.NonClusteredIndex) == IndexType.NonClusteredColumnStoreIndex)
                {
                    return new NonClusteredColumnStoreIndexScripter(index, sp);
                }
                else if (GetSupportedPropertyValue<IndexType>(index, "IndexType", IndexType.ClusteredIndex) == IndexType.ClusteredColumnStoreIndex)
                {
                    return new ClusteredColumnstoreIndexScripter(index, sp);
                }
                else if (GetSupportedPropertyValue<IndexType>(index, "IndexType", IndexType.NonClusteredIndex) == IndexType.SelectiveXmlIndex)
                {
                    return new SelectiveXMLIndexScripter(index, sp);
                }
                else if (index.IsMemoryOptimizedIndex)
                {
                    return GetIndexScripter(index, sp);
                }
                else
                {
                    return new NonClusteredRegularIndexScripter(index, sp);
                }
            }

            private static T GetSupportedPropertyValue<T>(Index index, string propertyName, T defaultValue)
            {
                if (index.IsSupportedProperty(propertyName))
                {
                    return index.GetPropValueOptional<T>(propertyName, defaultValue);
                }
                else
                {
                    return defaultValue;
                }
            }
        }

        private class ConstraintScripter : IndexScripter
        {
            public ConstraintScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override void Validate()
            {
                if (!this.IsClustered.GetValueOrDefault(IndexKeyType.DriPrimaryKey == index.GetIndexKeyType() ? true : false))
                {
                    this.CheckClusteredProperties();
                }

                this.CheckConflictingProperties();
                this.CheckNonClusteredProperties();
                this.CheckXmlProperties();
                this.CheckSpatialProperties();
            }

            protected override void ScriptIndexHeader(StringBuilder sb)
            {
                if (!this.TableCreate)
                {
                    index.ScriptIncludeHeaders(sb, preferences, Index.UrnSuffix);
                    this.ScriptExistenceCheck(sb, true);
                    //check for creating case and not generate the header part
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ADD ",
                                                parent.FormatFullNameForScripting(preferences));
                }

                if (parent is Table)
                {
                    index.AddConstraintName(sb, preferences);
                }
                sb.Append(IndexKeyType.DriPrimaryKey == index.GetIndexKeyType() ? "PRIMARY KEY " : "UNIQUE ");

                if (this.IsClustered.HasValue)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, this.IsClustered.Value ? "CLUSTERED " : "NONCLUSTERED ");
                }
            }

            protected override bool ScriptColumn(IndexedColumn col, StringBuilder sb)
            {
                base.ScriptColumn(col, sb);
                this.ScriptColumnOrder(col, sb);
                return true;
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                // index options are not supported for primary and unique keys on SQL DW tables
                if (this.index.IsSqlDwIndex || preferences.TargetEngineIsAzureSqlDw())
                {
                    return;
                }

                if ((preferences.TargetServerVersion >= SqlServerVersion.Version90)
                    && !preferences.TargetEngineIsAzureStretchDb())
                 {
                    if (this.parent is UserDefinedTableType)
                    {
                        this.ScriptIndexOption(sb, "IGNORE_DUP_KEY", GetOnOffValue(index.GetPropValueOptional<bool>("IgnoreDuplicateKeys")));
                        return;
                    }

                    bool alterTableScript = (this.parent is Table && !this.TableCreate);

                    if (preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                    {
                        this.ScriptIndexOption(sb, "PAD_INDEX", GetOnOffValue(index.GetPropValueOptional<bool>("PadIndex")));
                    }
                    this.ScriptIndexOption(sb, "STATISTICS_NORECOMPUTE", GetOnOffValue(index.GetPropValueOptional<bool>("NoAutomaticRecomputation")));

                    if (alterTableScript && (preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase))
                    {
                        this.ScriptIndexOption(sb, "SORT_IN_TEMPDB", GetOnOffValue(index.sortInTempdb));
                    }

                    this.ScriptIndexOption(sb, "IGNORE_DUP_KEY", GetOnOffValue(index.GetPropValueOptional<bool>("IgnoreDuplicateKeys")));

                    if (alterTableScript)
                    {
                        this.ScriptIndexOptionOnline(sb);
                    }

                    // options not valid for a stretch db target or sql dw db target in azure
                    if (!preferences.TargetEngineIsAzureSqlDw())
                    {
                        // The resumable option is valid for rebuild for Azure db (and later for version 160)
                        //
                        if (alterTableScript && index.DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase && index.ResumableIndexOperation)
                        {
                            this.ScriptIndexOption(sb, "RESUMABLE", GetOnOffValue(index.ResumableIndexOperation));
                            if (index.ResumableMaxDuration != 0) // MAX_DURATION can be omitted.
                            {
                                this.ScriptIndexOption(sb, "MAX_DURATION", index.ResumableMaxDuration.ToString() + " MINUTES");
                            }
                        }
                    }

                    if (preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                    {
                        this.ScriptIndexOption(sb, "ALLOW_ROW_LOCKS", GetOnOffValue(RevertMeaning(index.GetPropValueOptional<bool>("DisallowRowLocks"))));
                        this.ScriptIndexOption(sb, "ALLOW_PAGE_LOCKS", GetOnOffValue(RevertMeaning(index.GetPropValueOptional<bool>("DisallowPageLocks"))));
                    }

                    if (alterTableScript && index.ServerVersion.Major >= 9 && index.MaximumDegreeOfParallelism > 0)
                    {
                        this.ScriptIndexOption(sb, "MAXDOP", index.MaximumDegreeOfParallelism);
                    }

                    this.ScriptFillFactor(sb);

                    this.ScriptOptimizeForSequentialKey(sb); // OPTIMIZE_FOR_SEQUENTIAL_KEY
                }
                else
                {
                    this.ScriptFillFactor(sb);
                }

                this.ScriptCompression(sb);
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                base.ScriptIndexStorage(sb);

                if (this.IsClustered.GetValueOrDefault((index.GetIndexKeyType() == IndexKeyType.DriUniqueKey ? false : true)))
                {
                    this.ScriptFileStream(sb);
                }
            }

            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                if (preferences.TargetEngineIsAzureSqlDw())
                {
                    // Script enforcement if the target server is SQL DW. Always script NOT ENFORCED because for now, SQL DW only supports "NOT ENFORCED"
                    sb.Append(" NOT ENFORCED ");
                }
            }

            protected override void ScriptDropHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP CONSTRAINT {1}{2}",
                                    this.parent.FormatFullNameForScripting(this.preferences),
                                    (this.preferences.IncludeScripts.ExistenceCheck &&
                                    this.preferences.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                                    this.index.FormatFullNameForScripting(this.preferences));
            }

            protected override void ScriptDropOptions(StringBuilder sb)
            {
                // For the Standalone and Azure v12 (Sterling) Engines.
                if (this.preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase ||
                    (index.ServerVersion.Major >= 12 && this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse))
                {
                    StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                    //CLUSTERED options
                    if (this.IsClustered == true)
                    {
                        //ONLINE = { ON | OFF }
                        this.ScriptIndexOptionOnline(withClause);

                        //MAXDOP = max_degree_of_parallelism
                        if (index.MaximumDegreeOfParallelism > 0)
                        {
                            this.ScriptIndexOption(withClause, "MAXDOP", index.MaximumDegreeOfParallelism);
                        }
                    }

                    // WAIT_AT_LOW_PRIORITY
                    this.ScriptWaitAtLowPriorityIndexOptionForDropAndResume(withClause);

                    if (withClause.Length > 0)
                    {
                        withClause.Length = withClause.Length - Globals.commaspace.Length;
                        sb.AppendFormat(SmoApplication.DefaultCulture, " WITH ( {0} )", withClause.ToString());
                    }
                }
            }
        }

        private class RegularIndexScripter : IndexScripter
        {
            public RegularIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            public bool IsUnique
            {
                get { return index.GetPropValueOptional<bool>("IsUnique", false); }
            }

            protected override void Validate()
            {
                if (!index.dropExistingIndex)
                {
                    this.CheckConstraintProperties();
                }

                this.CheckConflictingProperties();
                this.CheckXmlProperties();
                this.CheckSpatialProperties();
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                // the SQL DW tables can include inlined clustered index as part of the create table DDL
                // this clustered index does not take a name and only includes the indexed columns
                if (this.index.IsSqlDwIndex && this.TableCreate)
                {
                    if (!this.IsClustered.GetValueOrDefault(false))
                    {
                        throw new InvalidSmoOperationException(ExceptionTemplates.UnexpectedIndexTypeDetected(index.InferredIndexType.ToString()));
                    }

                    sb.Append(Globals.tab);
                    TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(IndexType));
                    sb.AppendFormat(SmoApplication.DefaultCulture, typeConverter.ConvertToInvariantString(IndexType.ClusteredIndex));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE {0}{1} INDEX {2} ON {3}",
                    (IsUnique ? "UNIQUE " : string.Empty),
                    (IsClustered.GetValueOrDefault(false) ? "CLUSTERED" : "NONCLUSTERED"),
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
                }
            }

            protected override bool ScriptColumn(IndexedColumn col, StringBuilder sb)
            {
                base.ScriptColumn(col, sb);
                this.ScriptColumnOrder(col, sb);
                return true;
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                base.ScriptIndexOptions(sb);
                this.ScriptCompression(sb);
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                base.ScriptIndexRebuildOptions(withClause, rebuildPartitionNumber);

                if (rebuildPartitionNumber != -1)
                {
                    bool fDataCompressionStateDirty = index.PhysicalPartitions.IsDataCompressionStateDirty(rebuildPartitionNumber);
                    bool fXmlCompressionStateDirty = index.PhysicalPartitions.IsXmlCompressionStateDirty(rebuildPartitionNumber);


                    if (fDataCompressionStateDirty)
                    {
                        withClause.Append(index.PhysicalPartitions.GetCompressionCode(rebuildPartitionNumber));
                        withClause.Append(Globals.commaspace);
                    }

                    if (fXmlCompressionStateDirty)
                    {
                        withClause.Append(index.PhysicalPartitions.GetXmlCompressionCode(rebuildPartitionNumber));
                        withClause.Append(Globals.commaspace);
                    }

                }
                else
                {
                    this.ScriptCompression(withClause);
                }
            }
        }

        private class NonClusteredRegularIndexScripter : RegularIndexScripter
        {
            List<IndexedColumn> includedColumns;

            public NonClusteredRegularIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
                includedColumns = new List<IndexedColumn>();
            }

            protected override bool IsIncludedColumnSupported
            {
                get { return true; }
            }

            protected override void Validate()
            {
                base.Validate();
                this.CheckClusteredProperties();
            }

            protected override bool ScriptColumn(IndexedColumn col, StringBuilder sb)
            {
                if (index.ServerVersion.Major < 9 || !col.GetPropValueOptional<bool>("IsIncluded", false))
                {
                    return base.ScriptColumn(col, sb);
                }
                else
                {
                    this.includedColumns.Add(col);
                    return false;
                }
            }

            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                this.ScriptIncludedColumns(sb, this.includedColumns);
                this.ScriptFilter(sb);
            }

            private void ScriptIncludedColumns(StringBuilder sb, List<IndexedColumn> includedColumns)
            {
                if (0 < includedColumns.Count &&
                    preferences.TargetServerVersion >= SqlServerVersion.Version90)
                {
                    sb.Append(preferences.NewLine);
                    sb.Append("INCLUDE ( ");

                    foreach (IndexedColumn col in includedColumns)
                    {
                        index.m_bIsOnComputed = (!index.m_bIsOnComputed) ?
                            col.GetPropValueOptional<bool>("IsComputed", false) : index.m_bIsOnComputed;

                        // Check to see if the specified column exists.
                        Column colBase = (Column)columns.NoFaultLookup(new SimpleObjectKey(col.Name));

                        sb.Append(Globals.tab);

                        // use proper name for scripting
                        if (null != colBase)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", (SqlBraket(colBase.GetName(preferences))));
                            this.index.isOnColumnWithAnsiPadding |= colBase.GetPropValueOptional<bool>("AnsiPaddingStatus", false);
                        }
                        else
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", (SqlBraket(col.Name)));
                        }

                        sb.Append(Globals.comma);
                        sb.Append(preferences.NewLine);
                    }

                    sb.Length = sb.Length - (preferences.NewLine.Length + Globals.comma.Length);
                    sb.Append(") ");
                }
            }

            protected override void ScriptDropOptions(StringBuilder sb)
            {
                // For the Standalone and Azure v12 (Sterling) Engines.
                if (this.preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase ||
                    (index.ServerVersion.Major >= 12 && this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse))
                {
                    StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                    // WAIT_AT_LOW_PRIORITY
                    this.ScriptWaitAtLowPriorityIndexOptionForDropAndResume(withClause);

                    if (withClause.Length > 0)
                    {
                        withClause.Length = withClause.Length - Globals.commaspace.Length;
                        sb.AppendFormat(SmoApplication.DefaultCulture, " WITH ( {0} )", withClause.ToString());
                    }
                }
            }
        }

        private class ClusteredRegularIndexScripter : RegularIndexScripter
        {
            private string dataSpaceName;
            public string DataSpaceName
            {
                get
                {
                    return dataSpaceName;
                }
                set
                {
                    dataSpaceName = value;
                }
            }

            private StringCollection partitionSchemeParameters;
            public StringCollection PartitionSchemeParameters
            {
                get
                {
                    return partitionSchemeParameters;
                }
                set
                {
                    partitionSchemeParameters = value;
                }
            }

            public ClusteredRegularIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override void Validate()
            {
                base.Validate();
                this.CheckNonClusteredProperties();
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                base.ScriptIndexStorage(sb);
                this.ScriptFileStream(sb);
            }

            protected override void ScriptDropOptions(StringBuilder sb)
            {
                // For the Standalone and Azure v12 (Sterling) Engines.
                if (this.preferences.TargetDatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase ||
                    (index.ServerVersion.Major >= 12 && this.preferences.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse))
                {
                    StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                    //ONLINE
                    this.ScriptIndexOptionOnline(withClause);

                    //MAXDOP
                    if (index.MaximumDegreeOfParallelism > 0)
                    {
                        this.ScriptIndexOption(withClause, "MAXDOP", index.MaximumDegreeOfParallelism);
                    }

                    //MOVE TO
                    if (this.DataSpaceName != null)
                    {
                        withClause.AppendFormat(SmoApplication.DefaultCulture, "MOVE TO [{0}]", SqlBraket(this.DataSpaceName));

                        if (null != this.PartitionSchemeParameters)
                        {
                            withClause.Append("(");
                            int colCount = 0;
                            foreach (string colName in this.PartitionSchemeParameters)
                            {
                                if (0 < colCount++)
                                {
                                    withClause.Append(Globals.commaspace);
                                }

                                withClause.AppendFormat(SmoApplication.DefaultCulture, "[{0}]", SqlBraket(colName));
                            }

                            withClause.Append(")");
                        }

                        withClause.Append(Globals.commaspace);
                    }

                    // WAIT_AT_LOW_PRIORITY
                    this.ScriptWaitAtLowPriorityIndexOptionForDropAndResume(withClause);

                    if (withClause.Length > 0)
                    {
                        withClause.Length = withClause.Length - Globals.commaspace.Length;
                        sb.AppendFormat(SmoApplication.DefaultCulture, " WITH ( {0} )", withClause.ToString());
                    }
                }
            }
        }

        private class XmlIndexScripter : IndexScripter
        {
            public XmlIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
                index.xmlOrSpatialIndex = true;
            }

            protected override void Validate()
            {
                ThrowIfBelowVersion90(preferences.TargetServerVersion);
                index.ThrowIfCompatibilityLevelBelow90();
                //Azure v12 (Sterling) and above support XML indices
                if (preferences.TargetDatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
                {
                    ThrowIfBelowVersion120(preferences.TargetServerVersion);
                }

                if (parent is View)
                {
                    throw new SmoException(ExceptionTemplates.NotXmlIndexOnView);
                }

                if (index.IndexedColumns.Count != 1)
                {
                    throw new SmoException(ExceptionTemplates.OneColumnInXmlIndex);
                }

                this.CheckConstraintProperties();
                this.CheckRegularIndexProperties();
                this.CheckClusteredProperties();
                this.CheckNonClusteredProperties();
                this.CheckSpatialProperties();
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                return;
            }
        }

        private class PrimaryXmlIndexScripter : XmlIndexScripter
        {
            public PrimaryXmlIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override void Validate()
            {
                base.Validate();
                if (!string.IsNullOrEmpty(index.GetPropValueOptional("ParentXmlIndex", string.Empty)))
                {
                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingIndexProperties, "ParentXmlIndex", index.GetPropValueOptional("ParentXmlIndex").ToString(), "IndexType", index.GetPropValueOptional<IndexType>("IndexType").ToString()));
                }
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE PRIMARY XML INDEX {0} ON {1}",
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
            }
        }

        private class SecondaryXmlIndexScripter : XmlIndexScripter
        {
            public SecondaryXmlIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            public string ParentXmlIndex
            {
                get { return (string)index.Properties["ParentXmlIndex"].Value; }
            }

            protected override void Validate()
            {
                base.Validate();
                if (string.IsNullOrEmpty(index.GetPropValueOptional("ParentXmlIndex", string.Empty)))
                {
                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingIndexProperties, "ParentXmlIndex", string.Empty, "IndexType", index.GetPropValueOptional<IndexType>("IndexType").ToString()));
                }
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE XML INDEX {0} ON {1}",
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
            }

            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                sb.Append(preferences.NewLine);
                sb.AppendFormat(SmoApplication.DefaultCulture,
                    "USING XML INDEX {0} ", MakeSqlBraket(this.ParentXmlIndex));

                SecondaryXmlIndexType xmlIdxType = (SecondaryXmlIndexType)index.GetPropValue("SecondaryXmlIndexType");
                switch (xmlIdxType)
                {
                    case SecondaryXmlIndexType.Path:
                        sb.Append("FOR PATH ");
                        break;
                    case SecondaryXmlIndexType.Value:
                        sb.Append("FOR VALUE ");
                        break;
                    case SecondaryXmlIndexType.Property:
                        sb.Append("FOR PROPERTY ");
                        break;
                    default:
                        throw new WrongPropertyValueException(index.Properties.Get("SecondaryXmlIndexType"));

                }
            }
        }

        private class SpatialIndexScripter : IndexScripter
        {
            Property spatialIndexType;
            Property xMin;
            Property yMin;
            Property xMax;
            Property yMax;
            Property level1;
            Property level2;
            Property level3;
            Property level4;
            Property cellsPerObject;

            public SpatialIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
                index.xmlOrSpatialIndex = true;
            }

            protected override void Validate()
            {
                ThrowIfBelowVersion100(preferences.TargetServerVersion);
                index.ThrowIfCompatibilityLevelBelow100();

                if (this.parent is View)
                {
                    throw new SmoException(ExceptionTemplates.NotSpatialIndexOnView);
                }

                if (index.IndexedColumns.Count != 1)
                {
                    throw new SmoException(ExceptionTemplates.OneColumnInSpatialIndex);
                }

                this.CheckConstraintProperties();
                this.CheckRegularIndexProperties();
                this.CheckClusteredProperties();
                this.CheckNonClusteredProperties();
                this.CheckXmlProperties();
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE SPATIAL INDEX {0} ON {1}",
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
            }

            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                spatialIndexType = index.Properties.Get("SpatialIndexType");

                xMin = index.Properties.Get("BoundingBoxXMin");
                yMin = index.Properties.Get("BoundingBoxYMin");
                xMax = index.Properties.Get("BoundingBoxXMax");
                yMax = index.Properties.Get("BoundingBoxYMax");

                level1 = index.Properties.Get("Level1Grid");
                level2 = index.Properties.Get("Level2Grid");
                level3 = index.Properties.Get("Level3Grid");
                level4 = index.Properties.Get("Level4Grid");

                cellsPerObject = index.Properties.Get("CellsPerObject");


                // For Spatial indexes, the tessellation scheme is appended as "USING <Scheme> " T-SQL
                if (spatialIndexType.Value != null && (spatialIndexType.Dirty || !preferences.ScriptForAlter))
                {
                    if ((SpatialIndexType)spatialIndexType.Value != SpatialIndexType.None)
                    {
                        string spatialGridType = string.Empty;

                        SpatialIndexType sIndexType = (SpatialIndexType)spatialIndexType.Value;
                        switch (sIndexType)
                        {
                            case SpatialIndexType.GeometryGrid:
                                spatialGridType = " GEOMETRY_GRID ";
                                break;
                            case SpatialIndexType.GeographyGrid:
                                spatialGridType = " GEOGRAPHY_GRID ";
                                break;
                            case SpatialIndexType.GeometryAutoGrid:
                                ThrowIfBelowVersion110(this.preferences.TargetServerVersion,
                                    ExceptionTemplates.SpatialAutoGridDownlevel(this.index.FormatFullNameForScripting(this.preferences, true), GetSqlServerName(this.preferences)));
                                spatialGridType = " GEOMETRY_AUTO_GRID ";
                                break;
                            case SpatialIndexType.GeographyAutoGrid:
                                ThrowIfBelowVersion110(this.preferences.TargetServerVersion,
                                    ExceptionTemplates.SpatialAutoGridDownlevel(this.index.FormatFullNameForScripting(this.preferences, true), GetSqlServerName(this.preferences)));
                                spatialGridType = " GEOGRAPHY_AUTO_GRID ";
                                break;
                        }
                        sb.AppendFormat(SmoApplication.DefaultCulture, "USING {0}", spatialGridType);
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.newline);
                    }
                }
            }

            private void ScriptSpatialIndexOptions(StringBuilder sb)
            {
                SpatialIndexType spatialType = SpatialIndexType.GeometryGrid;

                if (spatialIndexType.Value != null)
                {
                    spatialType = (SpatialIndexType)spatialIndexType.Value;
                }

                if (spatialType == SpatialIndexType.GeometryGrid || spatialType == SpatialIndexType.GeometryAutoGrid)
                {
                    if (xMin.IsNull || yMin.IsNull || xMax.IsNull || yMax.IsNull)
                    {
                        // Throws an error when these properties are set for non geometry spatial types
                        throw new SmoException(ExceptionTemplates.MissingBoundingParameters);
                    }

                    // T- SQL: "BOUNDING_BOX = ({xmin, ymin, xmax, ymax}),"

                    sb.AppendFormat(SmoApplication.DefaultCulture, "BOUNDING_BOX =");
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);

                    sb.AppendFormat(SmoApplication.DefaultCulture, ((double)xMin.Value).ToString(SmoApplication.DefaultCulture));
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);

                    sb.AppendFormat(SmoApplication.DefaultCulture, ((double)yMin.Value).ToString(SmoApplication.DefaultCulture));
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);

                    sb.AppendFormat(SmoApplication.DefaultCulture, ((double)xMax.Value).ToString(SmoApplication.DefaultCulture));
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);

                    sb.AppendFormat(SmoApplication.DefaultCulture, ((double)yMax.Value).ToString(SmoApplication.DefaultCulture));
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                else if (!(xMin.IsNull || yMin.IsNull || xMax.IsNull || yMax.IsNull)
                    && !((double)xMin.Value == m_boundingBoxDef || (double)xMax.Value == m_boundingBoxDef
                       || (double)yMin.Value == m_boundingBoxDef || (double)yMax.Value == m_boundingBoxDef))
                {
                    // Throws an exception when these parameters are set and it is not Geometry type.
                    throw new SmoException(ExceptionTemplates.InvalidNonGeometryParameters);
                }

                if (spatialType != SpatialIndexType.GeometryAutoGrid && spatialType != SpatialIndexType.GeographyAutoGrid)
                {
                    // gridClause is used to generate script  for all levels.
                    // The user can set these levels optionally separated by commas
                    // T-SQL: GRIDS = (LEVEL_1 = LOW, LEVEL_4 = MEDIUM)
                    StringBuilder gridsClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);

                    ScriptGridClause(gridsClause);

                    if (gridsClause.Length > 0)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "GRIDS =");
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                        // appending the grid clause to with clause
                        sb.AppendFormat(SmoApplication.DefaultCulture, gridsClause.ToString());
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }
                }
                else
                {
                    //since GRID parameter is not available for GEOMETRY_AUTO_GRID && GEOGRAPHY_AUTO_GRID
                    if (!level1.IsNull && (SpatialGeoLevelSize)level1.Value != SpatialGeoLevelSize.None
                   || !level2.IsNull && (SpatialGeoLevelSize)level2.Value != SpatialGeoLevelSize.None
                   || !level3.IsNull && (SpatialGeoLevelSize)level3.Value != SpatialGeoLevelSize.None
                   || !level4.IsNull && (SpatialGeoLevelSize)level4.Value != SpatialGeoLevelSize.None)
                    {
                        throw new SmoException(ExceptionTemplates.NoAutoGridWithGrids(this.index.FormatFullNameForScripting(this.preferences, true)));
                    }
                }

                if (cellsPerObject.Value != null && (cellsPerObject.Dirty || !preferences.ScriptForAlter))
                {
                    if (preferences.ForDirectExecution || (int)cellsPerObject.Value != 0)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.newline);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "CELLS_PER_OBJECT = {0}", cellsPerObject.Value.ToString());
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }
                }
            }

            private void ScriptGridClause(StringBuilder gridsClause)
            {
                this.ScriptLevel(gridsClause, level1, "LEVEL_1");
                this.ScriptLevel(gridsClause, level2, "LEVEL_2");
                this.ScriptLevel(gridsClause, level3, "LEVEL_3");
                this.ScriptLevel(gridsClause, level4, "LEVEL_4");

                if (gridsClause.ToString().EndsWith(Globals.comma, StringComparison.Ordinal))
                {
                    gridsClause.Remove(gridsClause.Length - 1, 1);
                }
            }

            private void ScriptLevel(StringBuilder gridsClause, Property level, string levelString)
            {
                if (level.Value != null && (level.Dirty || !preferences.ScriptForAlter))
                {
                    if ((SpatialGeoLevelSize)level.Value != SpatialGeoLevelSize.None)
                    {
                        gridsClause.AppendFormat(SmoApplication.DefaultCulture, "{0} = {1}", levelString, level.Value.ToString().ToUpperInvariant());
                        gridsClause.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
                    }
                }
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                this.ScriptSpatialIndexOptions(sb);
                base.ScriptIndexOptions(sb);
                this.ScriptCompression(sb);
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                base.ScriptIndexRebuildOptions(withClause, rebuildPartitionNumber);

                bool fDataCompressionStateDirty = index.PhysicalPartitions.IsDataCompressionStateDirty(rebuildPartitionNumber);
                bool fXmlCompressionStateDirty = (this.index.IsSupportedProperty(nameof(HasXmlCompressedPartitions))) ? index.PhysicalPartitions.IsXmlCompressionStateDirty(rebuildPartitionNumber) : false ; //

                if ((rebuildPartitionNumber != -1) && fDataCompressionStateDirty)
                {
                    if (fDataCompressionStateDirty)
                    {
                        withClause.Append(index.PhysicalPartitions.GetCompressionCode(rebuildPartitionNumber));
                        withClause.Append(Globals.commaspace);
                    }

                    			if(fXmlCompressionStateDirty)
                    			{
                    				withClause.Append(index.PhysicalPartitions.GetXmlCompressionCode(rebuildPartitionNumber));
                    				withClause.Append(Globals.commaspace);
                    			}
                }
                else
                {
                    this.ScriptCompression(withClause);
                }
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                if (this.index.IsSupportedProperty("FileGroup", preferences) && !string.IsNullOrEmpty(this.index.GetPropValueOptional<string>("FileGroup", string.Empty)))
                {
                    base.ScriptIndexStorage(sb);
                }
            }
        }

        private abstract class ColumnstoreIndexScripter : IndexScripter
        {
            public ColumnstoreIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected virtual void ScriptIndexOptions(StringBuilder sb, bool forRebuild, int rebuildPartitionNumber)
            {
                // If the script is for rebuilding an index (as opposed to creating), and if the user has specified to drop existing index
                // then we must generate a DROP_EXISTING clause
                if (!forRebuild)
                {
                    this.ScriptIndexOption(sb, "DROP_EXISTING", GetOnOffValue(index.dropExistingIndex));
                    this.ScriptCompressionDelay(sb);
                }

                // generate MAXDOP clause if specified
                if (index.MaximumDegreeOfParallelism > 0)
                {
                    this.ScriptIndexOption(sb, "MAXDOP", index.MaximumDegreeOfParallelism);
                }

                // If rebuilding a particular partition and if a compression setting has been specified in the index object, then add a simple
                // DATA_COMPRESSION or XML_COMPRESSION clause accordingly

                if (forRebuild && (rebuildPartitionNumber != -1))
                {
                    if (index.PhysicalPartitions.IsDataCompressionStateDirty(rebuildPartitionNumber))
                    {
                        sb.Append(index.PhysicalPartitions.GetCompressionCode(rebuildPartitionNumber));
                        sb.Append(Globals.commaspace);
                    }
                    if (index.PhysicalPartitions.IsXmlCompressionStateDirty(rebuildPartitionNumber))
                    {
                        sb.Append(index.PhysicalPartitions.GetXmlCompressionCode(rebuildPartitionNumber));
                        sb.Append(Globals.commaspace);
                    }
                }
                // This handles more complex DATA_COMPRESSION clauses, such as per partition settings.
                else
                {
                    this.ScriptCompression(sb);
                }
            }

            protected override void ScriptSetIndexOptions(StringBuilder setClause)
            {
                if(!preferences.TargetEngineIsAzureStretchDb())
                {
                    this.ScriptCompressionDelay(setClause);
                }
            }

            protected void ScriptCompressionDelay(StringBuilder sb)
            {
                if (index.IsSupportedProperty("CompressionDelay", this.preferences))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "COMPRESSION_DELAY = {0}", index.CompressionDelay);
                    sb.Append(Globals.commaspace);
                }
            }

            // The create and alter syntax for columnstore indexes only supports limited options in the WITH clause.
            // DATA_COMPRESSION, MAX_DOP
            protected void CheckInvalidOptions()
            {
                // Memory optimized tables that contain column store indexes do not script index options.
                // The query that populates these from the server finds the default values which need
                // to be ignored during this validation check.
                if (index.IsMemoryOptimizedIndex)
                {
                    return;
                }
                Exception exception = new SmoException(ExceptionTemplates.InvaildColumnStoreIndexOption);
                this.CheckProperty<bool>("PadIndex", false, exception);
                this.CheckProperty<bool>("NoAutomaticRecomputation", false, exception);
                this.CheckProperty<bool>(index.sortInTempdb, false, exception);
                this.CheckProperty<bool>(index.onlineIndexOperation, false, exception);
                this.CheckProperty<bool>("DisallowRowLocks", true, exception);
                this.CheckProperty<bool>("DisallowPageLocks", true, exception);
                this.CheckProperty<byte>("FillFactor", fillFactorDef, exception);
                this.CheckProperty<bool>("IsOptimizedForSequentialKey", false,
                    new SmoException(ExceptionTemplates.NoIndexOptimizeForSequentialKey));
            }
        }

        private class NonClusteredColumnStoreIndexScripter : ColumnstoreIndexScripter
        {
            public NonClusteredColumnStoreIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override bool IsIncludedColumnSupported
            {
                get { return true; }
            }

            protected override void Validate()
            {
                ThrowIfBelowVersion110(preferences.TargetServerVersion);

                this.CheckConstraintProperties();
                this.CheckRegularIndexProperties();
                this.CheckClusteredProperties();
                this.CheckXmlProperties();
                this.CheckSpatialProperties();
                this.CheckInvalidOptions();
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE NONCLUSTERED COLUMNSTORE INDEX {0} ON {1}",
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                this.ScriptIndexOptions(sb, false, -1);
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                this.ScriptIndexOptions(withClause, true, rebuildPartitionNumber);
            }

            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                this.ScriptFilter(sb);
            }
        }

        /// <summary>
        /// Class is specialized for scripting hash indexes and keys
        /// </summary>
        private class HashIndexScripter : IndexScripter
        {
            public HashIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override bool IsIncludedColumnSupported
            {
                get { return false; }
            }

            protected override void CheckRegularIndexProperties()
            {
                this.CheckProperty<bool>("IsClustered", false, new SmoException(ExceptionTemplates.NoIndexClustered));
                this.CheckProperty<bool>("IgnoreDuplicateKeys", false, new SmoException(ExceptionTemplates.NoIndexIgnoreDupKey));
            }

            protected override void Validate()
            {
                ThrowIfBelowVersion120(preferences.TargetServerVersion);

                this.CheckRequiredProperties();
                this.CheckConflictingProperties();

                if (index.GetIndexKeyType() == Smo.IndexKeyType.None)
                {
                    this.CheckRegularIndexProperties();
                    this.CheckNonClusteredProperties();
                    this.CheckXmlProperties();
                    this.CheckSpatialProperties();
                }
            }

            private void CheckRequiredProperties()
            {
                if (!index.IsSupportedProperty("BucketCount")
                    || !index.GetPropValueOptional<int>("BucketCount").HasValue)
                {
                    throw new SmoException(ExceptionTemplates.BucketCountForHashIndex);
                }
            }

            protected override void ScriptAlterDetails(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "REBUILD WITH (BUCKET_COUNT = {0})",
                        index.GetPropValueOptional<int>("BucketCount").Value.ToString());
            }

            protected override void ScriptIndexHeader(StringBuilder sb)
            {
                this.ScriptCreateHeaderDdl(sb);
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                if (!this.TableCreate)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ADD ",
                        this.parent.FormatFullNameForScripting(this.preferences));
                }

                IndexKeyType indexKeyType = index.GetIndexKeyType();
                if (IndexKeyType.DriPrimaryKey == indexKeyType ||
                    IndexKeyType.DriUniqueKey == indexKeyType)
                {
                    // Script as a constraint if the key has a user provided name.
                    // Naming constraint is not supported for user defined table type.
                    if (parent is Table)
                    {
                        index.AddConstraintName(sb, preferences);
                    }
                    sb.Append(Globals.space);
                    sb.Append(IndexKeyType.DriPrimaryKey == indexKeyType ?
                        Scripts.PRIMARY_KEY : Scripts.UNIQUE);
                    sb.Append(Globals.space);
                }
                else
                {
                    sb.AppendFormat(Scripts.INDEX_NAME, SqlBraket(index.Name));
                    sb.Append(Globals.space);

                    if (true == index.GetPropValueOptional<bool>("IsUnique", false))
                    {
                        sb.Append(Scripts.UNIQUE);
                        sb.Append(Globals.space);
                    }
                }

                sb.Append(Scripts.HASH);
                sb.Append(Globals.space);
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                sb.Append(Globals.space);
                sb.AppendFormat(Scripts.WITH_BUCKET_COUNT, index.BucketCount);
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                // Rebuild is not supported for Hash Indexes
            }

            protected override void ScriptSetIndexOptions(StringBuilder setClause)
            {
                // Other index options are not supported for Hash Indexes
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                // This is not supported for Hash Indexes
            }
        }

        /// <summary>
        /// Class is specialized for scripting range indexes and keys
        /// </summary>
        private class RangeIndexScripter : IndexScripter
        {
            public RangeIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override bool IsIncludedColumnSupported
            {
                get { return false; }
            }

            protected override void CheckRegularIndexProperties()
            {
                this.CheckProperty<bool>("IsClustered", false, new SmoException(ExceptionTemplates.NoIndexClustered));
                this.CheckProperty<bool>("IgnoreDuplicateKeys", false, new SmoException(ExceptionTemplates.NoIndexIgnoreDupKey));
            }

            protected override void Validate()
            {
                ThrowIfBelowVersion120(preferences.TargetServerVersion);

                this.CheckConflictingProperties();

                if (index.GetIndexKeyType() == Smo.IndexKeyType.None)
                {
                    this.CheckRegularIndexProperties();
                    this.CheckNonClusteredProperties();
                    this.CheckXmlProperties();
                    this.CheckSpatialProperties();
                }
            }

            protected override void ScriptIndexHeader(StringBuilder sb)
            {
                this.ScriptCreateHeaderDdl(sb);
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                if (!this.TableCreate)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ADD ",
                        this.parent.FormatFullNameForScripting(this.preferences));
                }

                IndexKeyType indexKeyType = index.GetIndexKeyType();
                if (IndexKeyType.DriPrimaryKey == indexKeyType ||
                    IndexKeyType.DriUniqueKey == indexKeyType)
                {
                    // Script as a constraint if the key has a user provided name.
                    // Naming constraint is not supported for user defined table type.
                    if (parent is Table)
                    {
                        index.AddConstraintName(sb, preferences);
                    }
                    sb.Append(Globals.space);
                    sb.Append(IndexKeyType.DriPrimaryKey == indexKeyType ?
                        Scripts.PRIMARY_KEY : Scripts.UNIQUE);
                    sb.Append(Globals.space);
                }
                else
                {
                    sb.AppendFormat(Scripts.INDEX_NAME, SqlBraket(index.Name));
                    sb.Append(Globals.space);

                    if (true == index.GetPropValueOptional<bool>("IsUnique", false))
                    {
                        sb.Append(Scripts.UNIQUE);
                        sb.Append(Globals.space);
                    }
                }

                sb.Append(Scripts.NONCLUSTERED);
                sb.Append(Globals.space);
            }

            protected override bool ScriptColumn(IndexedColumn col, StringBuilder sb)
            {
                base.ScriptColumn(col, sb);
                this.ScriptColumnOrder(col, sb);
                return true;
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                // Other index options are not supported for Range Indexes
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                // Rebuild is not supported for Range Indexes
            }

            protected override void ScriptSetIndexOptions(StringBuilder setClause)
            {
                // Other index options are not supported for Range Indexes
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                // This is not supported for Range Indexes
            }
        }

        /// <summary>
        /// Class is specialized for scripting indexes on user defined table type
        /// </summary>
        private class UserDefinedTableTypeIndexScripter : IndexScripter
        {
            public UserDefinedTableTypeIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            protected override void Validate()
            {
                if (preferences.TargetDatabaseEngineType == Cmn.DatabaseEngineType.Standalone)
                {
                    ThrowIfBelowVersion130(preferences.TargetServerVersion);
                }
                CheckConflictingProperties();
            }

            protected override void ScriptIndexHeader(StringBuilder sb)
            {
                this.ScriptCreateHeaderDdl(sb);
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(Scripts.INDEX_NAME, SqlBraket(index.Name));
                sb.Append(Globals.space);
                if (index.IsUnique)
                {
                    sb.Append(Scripts.UNIQUE);
                    sb.Append(Globals.space);
                }
                sb.Append(index.IsClustered ? Scripts.CLUSTERED : Scripts.NONCLUSTERED);
                sb.Append(Globals.space);
            }

            protected override bool ScriptColumn(IndexedColumn col, StringBuilder sb)
            {
                base.ScriptColumn(col, sb);
                this.ScriptColumnOrder(col, sb);
                return true;
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                var ignoreDuplicateKeys = index.GetPropValueOptional<bool>(nameof(Index.IgnoreDuplicateKeys));
                if (ignoreDuplicateKeys == true)
                {
                    ScriptIndexOption(sb, "IGNORE_DUP_KEY", GetOnOffValue(ignoreDuplicateKeys));
                }
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                // Rebuild is not supported for user defined table type Indexes
            }

            protected override void ScriptSetIndexOptions(StringBuilder setClause)
            {
                // Other index options are not supported for user defined table type Indexes
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                // This is not supported for user defined table type Indexes
            }
        }

        /// <summary>
        /// Index scripter for Selective Xml Index (SXI)
        /// </summary>
        ///
        private class SelectiveXMLIndexScripter : IndexScripter
        {
            public SelectiveXMLIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
                index.xmlOrSpatialIndex = true;
            }

            protected override void Validate()
            {
                // SXI is supported from version 110 (Denali)
                ThrowIfBelowVersion110(preferences.TargetServerVersion);

                // ParentXmlIndex is not supported for SXI
                if (!string.IsNullOrEmpty(index.GetPropValueOptional("ParentXmlIndex", string.Empty)))
                {
                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingIndexProperties, "ParentXmlIndex", string.Empty, "IndexType", index.GetPropValueOptional<IndexType>("IndexType").ToString()));
                }

                this.CheckConstraintProperties();
                this.CheckRegularIndexProperties();
                this.CheckClusteredProperties();
                this.CheckNonClusteredProperties();
                this.CheckSpatialProperties();
                this.CheckInvalidOptions();

                //Check for more then one default namespace
                bool defaultNameSpaceFound = false;
                for (int i = 0; i < this.index.IndexedXmlPathNamespaces.Count; i++)
                {
                   if(this.index.IndexedXmlPathNamespaces[i].GetPropValueOptional<bool>("IsDefaultUri",false))
                   {
                       if(defaultNameSpaceFound)
                       {
                           throw new SmoException(string.Format(SmoApplication.DefaultCulture,ExceptionTemplates.MoreThenOneXmlDefaultNamespace,this.index.Name));
                       }
                       defaultNameSpaceFound = true;
                   }
                }
            }
            /// <summary>
            /// Invalid options should go here. Also update ExceptionTemplates.strings
            /// </summary>
            private void CheckInvalidOptions()
            {
                Exception exception = new SmoException(ExceptionTemplates.InvaildSXIOption);
                this.CheckProperty<bool>(index.OnlineIndexOperation, false, exception);
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE SELECTIVE XML INDEX {0} ON {1}",
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
            }

            protected override void ScriptAlterDetails(StringBuilder sb)
            {
                // Checking if there are any added XmlNamespaces.
                //
                if (this.index.IndexedXmlPathNamespaces.Count >0)
                {
                    StringBuilder sbNamespaces = new StringBuilder();
                    bool newNamespaces = false;
                    sbNamespaces.Append(preferences.NewLine);
                    sbNamespaces.AppendFormat(SmoApplication.DefaultCulture, "WITH XMLNAMESPACES");
                    sbNamespaces.Append(preferences.NewLine);
                    sbNamespaces.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                    sbNamespaces.Append(preferences.NewLine);


                    for (int i = 0; i < this.index.IndexedXmlPathNamespaces.Count; i++)
                    {
                        IndexedXmlPathNamespace ns = this.index.IndexedXmlPathNamespaces[i];

                        if (ns.State == SqlSmoState.Creating)
                        {
                            if(newNamespaces)
                            {
                                // this is not first namespace that we find
                                 sbNamespaces.AppendFormat(SmoApplication.DefaultCulture,Globals.comma);
                                 sbNamespaces.Append(preferences.NewLine);
                            }
                            newNamespaces = true; // There is at least one added namespace

                            sbNamespaces.AppendFormat(SmoApplication.DefaultCulture, "'{0}' as {1}",
                                ns.Uri, ns.Name);
                        }
                    }
                    if (newNamespaces)
                    {
                        sbNamespaces.Append(preferences.NewLine);
                        sbNamespaces.AppendFormat(SmoApplication.DefaultCulture,Globals.RParen);
                        sbNamespaces.Append(preferences.NewLine);

                        sb.AppendFormat(SmoApplication.DefaultCulture, sbNamespaces.ToString());
                    }
                }

                // Check if there are some path changes
                //
                if (this.index.IndexedXmlPaths.Count > 0)
                {
                    bool newPathChange = false;
                    sb.AppendFormat(SmoApplication.DefaultCulture,
                      "FOR (");
                    sb.Append(preferences.NewLine);

                    for (int i = 0; i < this.index.IndexedXmlPaths.Count; i++)
                    {
                        IndexedXmlPath path = this.index.IndexedXmlPaths[i];

                        if (path.State == SqlSmoState.Creating || path.State == SqlSmoState.ToBeDropped)
                        {
                            if (newPathChange)
                            {
                                // this is not first changed path that we find
                                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
                                sb.Append(preferences.NewLine);
                            }
                            newPathChange = true;

                            if (path.State == SqlSmoState.Creating)
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "ADD ");
                                // ID is not set, so this is newly added
                                ScriptSelectiveIndexPath(path, sb);
                            }
                            else
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, "REMOVE {0}", MakeSqlBraket(path.Name));
                            }
                        }
                    }
                    sb.AppendFormat(SmoApplication.DefaultCulture, ")");
                }
            }

            /*
             *   Create Index Syntax
             *
             *   CREATE SELECTIVE XML INDEX index_name
             *   ON <table_object> (xml_column_name)
             *   [WITH XMLNAMESPACES (<xmlnamespace_list>)]
             *   [FOR (<promoted_node_path_list>)]
             *   [WITH (<index_options>)]
             */
            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                // It was decided that XmlNamespaces will be put into ExtendedProperties
                // Syntax  [WITH XMLNAMESPACES (<xmlnamespace_list>)]
                //
                int xmlNamespacesCount = this.index.IndexedXmlPathNamespaces.Count;
                if (xmlNamespacesCount > 0)
                {
                    sb.Append(preferences.NewLine);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "WITH XMLNAMESPACES");
                    sb.Append(preferences.NewLine);
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                    sb.Append(preferences.NewLine);

                    for (int i = 0; i < xmlNamespacesCount; i++)
                    {
                        IndexedXmlPathNamespace xmlNamespace = this.index.IndexedXmlPathNamespaces[i];
                        if(xmlNamespace.GetPropValueOptional<bool>("IsDefaultUri",false))
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "DEFAULT '{0}'",
                                xmlNamespace.Uri);
                        }
                        else
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, "'{0}' as {1}",
                                xmlNamespace.Uri, xmlNamespace.Name);
                        }

                        if (i != xmlNamespacesCount - 1)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, Globals.comma);
                        }
                        sb.Append(preferences.NewLine);
                    }
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
                    sb.Append(preferences.NewLine);
                }

                // Scripting Paths
                //
                sb.Append(preferences.NewLine);
                sb.AppendFormat(SmoApplication.DefaultCulture, "FOR");
                sb.Append(preferences.NewLine);
                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                sb.Append(preferences.NewLine);

                IndexedXmlPath path;
                for (int i = 0; i < this.index.IndexedXmlPaths.Count; i++)
                {
                    path = this.index.IndexedXmlPaths[i];
                    ScriptSelectiveIndexPath(path, sb );
                    if (i != this.index.IndexedXmlPaths.Count - 1)
                    {
                        // this is not last path.
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }
                    sb.Append(preferences.NewLine);
                }

                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
                sb.Append(preferences.NewLine);
            }


            /// <summary>
            /// Scripting XmlPath
            /// </summary>
            /// <param name="path">Path added to index</param>
            /// <param name="sb">String builder</param>
            protected void ScriptSelectiveIndexPath(IndexedXmlPath path, StringBuilder sb)
            {
                /*
                 * [FOR (<promoted_node_path_list>)]
                 *
                 * <promoted_node_path_list> ::= <named_promoted_node_path_item> [, <promoted_node_path_list> ]
                 * <named_promoted_node_path_item> ::= <path_name> = <promoted_node_path_item>
                 * <promoted_node_path_item>::=<xquery_node_path_item> | <sql_values_node_path_item>
                 * <xquery_node_path_item> ::= <node_path> [as XQUERY <xsd_type_or_node_hint>] [SINGLETON]
                 * <xsd_type_or_node_hint> ::= [<xsd_type>] [MAXLENGTH(x)]1 | 'node()'
                 * <sql_values_node_path_item> ::= <node_path> as SQL <sql_type> [SINGLETON]
                 * <node_path> ::= <character_string_literal>
                 * <xsd_type> ::= <character_string_literal>
                 * <sql_type> ::= <identifier>
                 * <path_name> ::= <identifier>
                 *
                 */
                // <path_name>
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0} = '{1}'", MakeSqlBraket(path.Name), path.Path);

                // XQUERY VS SQL
                if (!path.GetPropValueOptional<IndexedXmlPathType>("PathType").HasValue)
                {
                    // Maybe we should throw excpetion when PathType is missing.
                    path.PathType = IndexedXmlPathType.XQuery;
                }
                if (path.PathType == IndexedXmlPathType.XQuery)
                {
                    // XQUERY node
                    // [as XQUERY <xsd_type_or_node_hint

                    if (path.GetPropValueOptional<bool>("IsNode").HasValue && path.IsNode)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " as XQUERY 'node()'");
                    }
                    else if (!string.IsNullOrEmpty(path.GetPropValueOptional<string>("XQueryTypeDescription", string.Empty)))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " as XQUERY '{0}'", path.XQueryTypeDescription);

                        // [MAXLENGTH(x)]
                        if (path.GetPropValueOptional<int>("XQueryMaxLength").HasValue)
                        {
                           if(path.XQueryMaxLength > 0)
                           {
                               sb.AppendFormat(SmoApplication.DefaultCulture, " MAXLENGTH ({0})", path.XQueryMaxLength);
                           }
                        }
                    }

                    if (path.GetPropValueOptional<bool>("IsSingleton").HasValue && path.IsSingleton)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " SINGLETON");
                    }
                }
                else
                {
                    // PathType == IndexedXmlPathType.Sql
                    // Sql values node path
                    //
                    sb.AppendFormat(SmoApplication.DefaultCulture, " as SQL ");
                    UserDefinedDataType.AppendScriptTypeDefinition(sb, this.preferences, path, path.DataType.SqlDataType);

                    if (path.GetPropValueOptional<bool>("IsSingleton").HasValue && path.IsSingleton)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " SINGLETON ");
                    }
                }
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                return;
            }
        }

        /// <summary>
        /// Scripter for secondary Selective Xml Index
        /// </summary>
        ///
        private class SecondarySelectiveXMLIndexScripter : IndexScripter
        {
            public SecondarySelectiveXMLIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
                index.xmlOrSpatialIndex = true;
            }

            public string ParentXmlIndex
            {
                get { return (string)index.Properties["ParentXmlIndex"].Value; }
            }

            protected override void Validate()
            {
                ThrowIfBelowVersion110(preferences.TargetServerVersion);

                // ParentXmlIndex is mandatory
                if (string.IsNullOrEmpty(index.GetPropValueOptional("ParentXmlIndex", string.Empty)))
                {
                    throw new SmoException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.ConflictingIndexProperties, "ParentXmlIndex", string.Empty, "IndexType", index.GetPropValueOptional<IndexType>("IndexType").ToString()));
                }

                this.CheckConstraintProperties();
                this.CheckRegularIndexProperties();
                this.CheckClusteredProperties();
                this.CheckNonClusteredProperties();
                this.CheckSpatialProperties();
                this.CheckInvalidOptions();
            }

            /// <summary>
            /// Invalid options should go here. Also update ExceptionTemplates.strings
            /// </summary>
            private void CheckInvalidOptions()
            {
                Exception exception = new SmoException(ExceptionTemplates.InvaildSXIOption);
                this.CheckProperty<bool>(index.OnlineIndexOperation, false, exception);
            }

            protected override void ScriptIndexDetails(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "USING XML INDEX {0} ", MakeSqlBraket(this.ParentXmlIndex));
                sb.AppendFormat(SmoApplication.DefaultCulture, "FOR (");
                sb.Append(preferences.NewLine);
                sb.AppendFormat(SmoApplication.DefaultCulture,
                      MakeSqlBraket(this.index.IndexedXmlPathName));
                sb.Append(preferences.NewLine);
                sb.AppendFormat(SmoApplication.DefaultCulture, ") ");
                sb.Append(preferences.NewLine);
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE XML INDEX {0} ON {1}",
                    index.FormatFullNameForScripting(preferences),
                    parent.FormatFullNameForScripting(preferences));
            }

            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                return;
            }

        }

        /// <summary>
        /// Class is specialized for scripting clustered columnstore indexes
        /// </summary>
        private class ClusteredColumnstoreIndexScripter : ColumnstoreIndexScripter
        {
            public ClusteredColumnstoreIndexScripter(Index index, ScriptingPreferences sp)
                : base(index, sp)
            {
            }

            // Clustered Columnstores implicitly include all columns
            protected override bool IsIncludedColumnSupported
            {
                get { return true; }
            }

            // Clustered Columnstore were first introduced in SQL 14
            protected override void Validate()
            {
                ThrowIfBelowVersion120(preferences.TargetServerVersion);

                this.CheckConstraintProperties();
                this.CheckClusteredProperties();
                this.CheckNonClusteredProperties();
                this.CheckXmlProperties();
                this.CheckSpatialProperties();
                this.CheckInvalidOptions();
            }

            protected override void ScriptCreateHeaderDdl(StringBuilder sb)
            {
                if (index.IsMemoryOptimizedIndex)
                {
                    // We include inline index in creating table without additional DDL for ALTER TABLE ADD INDEX DDL.
                    //
                    if (this.TableCreate)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "INDEX {0} CLUSTERED COLUMNSTORE ",
                            index.FormatFullNameForScripting(preferences));
                    }
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE CLUSTERED COLUMNSTORE INDEX {0} ON {1} ",
                        index.FormatFullNameForScripting(preferences),
                        parent.FormatFullNameForScripting(preferences));

                    List<IndexedColumn> indexedColumns =
                                (from IndexedColumn col in index.IndexedColumns
                                 where col.IsSupportedProperty("ColumnStoreOrderOrdinal") && col.GetPropValueOptional("ColumnStoreOrderOrdinal", 0) > 0
                                 select col).ToList();

                    if (indexedColumns.Count > 0)
                    {
                        List<IndexedColumn> orderedColumns = indexedColumns.OrderBy(x => x.GetPropValueOptional("ColumnStoreOrderOrdinal", 0)).ToList();
                        string listOfCols = string.Join(",", orderedColumns.Select(x => MakeSqlBraket(x.Name)).ToArray());
                        sb.AppendFormat(SmoApplication.DefaultCulture, "ORDER ({0})", listOfCols);
                    }
                }
            }

            protected override void ScriptIndexOptions(StringBuilder sb)
            {
                this.ScriptIndexOptions(sb, false, -1);
            }

            protected override void ScriptIndexRebuildOptions(StringBuilder withClause, int rebuildPartitionNumber)
            {
                this.ScriptIndexOptions(withClause, true, rebuildPartitionNumber);
            }

            protected override void ScriptIndexOptions(StringBuilder sb, bool forRebuild, int rebuildPartitionNumber)
            {
                if (index.IsMemoryOptimizedIndex)
                {
                    if (this.TableCreate)
                    {
                        this.ScriptCompressionDelay(sb);
                    }
                }
                else
                {
                    base.ScriptIndexOptions(sb, forRebuild, rebuildPartitionNumber);
                }
            }

            // Do nothing, columns are not explicitly indicated in clustered columnstore index
            protected override void ScriptColumns(StringBuilder sb)
            {
                return;
            }

            /// <summary>
            /// Scripts the index storage options.
            /// </summary>
            /// <param name="sb">The string builder.</param>
            protected override void ScriptIndexStorage(StringBuilder sb)
            {
                // Memory optimized tables with clustered column store indexes do not allow
                // storage options.
                //
                if(index.IsMemoryOptimizedIndex)
                {
                    return;
                }

                base.ScriptIndexStorage(sb);
            }
        }
    }
}
