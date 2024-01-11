// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// The enumeration specifies the tsql equivalent of DurabilityType of hekaton tables for scripting
    /// </summary>
    enum DurabilityTypeMap
    {
        /// <summary>
        /// DurabilityType.SchemaOnly
        /// </summary>
        SCHEMA_ONLY = 0,

        /// <summary>
        /// DurabilityType.SchemaAndData
        /// </summary>
        SCHEMA_AND_DATA = 1
    }

    /// <summary>
    /// This enumeration provides scripting options
    /// which are consumed when deciding which sql script to
    /// generate. Currently used for generating edge-constraint scripts
    /// </summary>
    enum ScriptFlag
    {
        CREATE,
        DROP
    }

    // Purpose: Base definition of the SMO Table class
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Table : TableViewBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable,
        Cmn.IDropIfExists, Cmn.IRenamable, ITableOptions
    {
        internal Table(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            Init();
        }

        public void ChangeSchema(string newSchema)
        {
            CheckObjectState();
            ChangeSchema(newSchema, true);
        }

        private void Init()
        {
            m_Checks = null;
            m_ForeignKeys = null;
            m_PartitionSchemeParameters = null;
            m_PhysicalPartitions = null;
            m_EdgeConstraints = null;
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "Table";
            }
        }

        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Index), SfcObjectFlags.Design | SfcObjectFlags.Deploy)]
        public override IndexCollection Indexes
        {
            get { return base.Indexes; }
        }

        private CheckCollection m_Checks;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Check), SfcObjectFlags.Design)]
        public CheckCollection Checks
        {
            get
            {
                CheckObjectState();
                if (null == m_Checks)
                {
                    m_Checks = new CheckCollection(this);
                }
                return m_Checks;
            }
        }

        /// <summary>
        /// Include Edge constraints as a child collection inside Table
        /// </summary>
        private EdgeConstraintCollection m_EdgeConstraints;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(EdgeConstraint), SfcObjectFlags.Design)]
        public EdgeConstraintCollection EdgeConstraints
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(EdgeConstraint));
                if (null == m_EdgeConstraints)
                {
                    m_EdgeConstraints = new EdgeConstraintCollection(this);
                }
                return m_EdgeConstraints;
            }
        }

        private ResumableIndexCollection m_ResumableIndexes;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ResumableIndex), SfcObjectFlags.Design)]
        public ResumableIndexCollection ResumableIndexes
        {
            get
            {
                CheckObjectState();
                if (null == m_ResumableIndexes)
                {
                    m_ResumableIndexes = new ResumableIndexCollection(this);
                }
                return m_ResumableIndexes;
            }
        }

        private Boolean m_OnlineHeapOperation = false;

        /// <summary>
        /// Online property for rebuild heap
        /// </summary>
        public Boolean OnlineHeapOperation
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion100();
                return m_OnlineHeapOperation;
            }
            set
            {
                CheckObjectState();
                ThrowIfBelowVersion100();
                m_OnlineHeapOperation = value;
            }
        }

        /// <summary>
        /// Specifies the MAX_DURATION for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation.
        /// </summary>
        private int m_lowPriorityMaxDuration = 0;

        /// <summary>
        /// Gets or sets the MAX_DURATION in minutes for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation. If needed, set this to a non-zero value before calling SwitchPartition or Rebuild.
        /// </summary>
        public int LowPriorityMaxDuration
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion120();
                return m_lowPriorityMaxDuration;
            }

            set
            {
                CheckObjectState();
                ThrowIfBelowVersion120();
                m_lowPriorityMaxDuration = value;
            }
        }

        private bool m_DataConsistencyCheckForSystemVersionedTable = true;

        /// <summary>
        /// Property for setting/getting if DATA_CONSISTENCY_CHECK option is used
        /// when creating system-versioned temporal table
        /// Sample syntax: CREATE TABLE t (....) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.History, DATA_CONSISTENCY_CHECK = ON))
        /// </summary>
        public bool DataConsistencyCheck
        {
            get
            {
                CheckObjectState();

                if (DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
                {
                    ThrowIfCloudAndVersionBelow12("DataConsistencyCheck");
                }
                else
                {
                    ThrowIfBelowVersion130();
                }

                return m_DataConsistencyCheckForSystemVersionedTable;
            }
            set
            {
                CheckObjectState();

                if (DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
                {
                    ThrowIfCloudAndVersionBelow12("DataConsistencyCheck");
                }
                else
                {
                    ThrowIfBelowVersion130();
                }

                m_DataConsistencyCheckForSystemVersionedTable = value;
            }
        }

        private struct SystemTimePeriodInfo
        {
            public string m_StartColumnName;
            public string m_EndColumnName;
            public bool m_MarkedForCreate;
            public bool m_MarkedForDrop;

            public void MarkForCreate(string start, string end)
            {
                this.m_StartColumnName = start;
                this.m_EndColumnName = end;
                this.m_MarkedForCreate = true;
            }

            public void Reset()
            {
                this.m_StartColumnName = String.Empty;
                this.m_EndColumnName = String.Empty;
                this.m_MarkedForCreate = false;
            }

            public void MarkForDrop(bool drop)
            {
                this.m_MarkedForDrop = drop;
            }
        }

        /// <summary>
        /// Stores various properties of a PERIOD that is about to be created/altered/dropped
        /// </summary>
        private SystemTimePeriodInfo m_systemTimePeriodInfo = new SystemTimePeriodInfo();

        /// <summary>
        /// Specifies the ABORT_AFTER_WAIT action for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation.
        /// </summary>
        private AbortAfterWait m_lowPriorityAbortAfterWait = AbortAfterWait.None;

        /// <summary>
        /// Gets or sets the ABORT_AFTER_WAIT action for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation. Set this before calling Rebuild or SwitchPartition.
        /// The default value is None.
        /// </summary>
        public AbortAfterWait LowPriorityAbortAfterWait
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion120();
                return m_lowPriorityAbortAfterWait;
            }

            set
            {
                CheckObjectState();
                ThrowIfBelowVersion120();
                m_lowPriorityAbortAfterWait = value;
            }
        }

        private Int32 m_MaximumDegreeOfParallelism = -1;

        /// <summary>
        /// Property for setting maximum number of processors that can be used when running
        /// rebuild heap
        /// </summary>
        public Int32 MaximumDegreeOfParallelism
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion100();
                return m_MaximumDegreeOfParallelism;
            }
            set
            {
                CheckObjectState();
                ThrowIfBelowVersion100();
                m_MaximumDegreeOfParallelism = value;
            }
        }

        /// <summary>
        /// Whether this table is a node table.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public Boolean IsNode
        {
            get
            {
                return (Boolean)this.Properties.GetValueWithNullReplacement("IsNode");
            }
            set
            {
                ThrowIfBelowVersion140Prop("IsNode");
                this.Properties.SetValueWithConsistencyCheck("IsNode", value);
            }
        }

        /// <summary>
        /// Whether this Table is an edge table.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public Boolean IsEdge
        {
            get
            {
                return (Boolean)this.Properties.GetValueWithNullReplacement("IsEdge");
            }
            set
            {
                ThrowIfBelowVersion140Prop("IsNode");
                this.Properties.SetValueWithConsistencyCheck("IsEdge", value);
            }
        }

        private ForeignKeyCollection m_ForeignKeys;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ForeignKey), SfcObjectFlags.Design)]
        public ForeignKeyCollection ForeignKeys
        {
            get
            {
                CheckObjectState();
                if (null == m_ForeignKeys)
                {
                    m_ForeignKeys = new ForeignKeyCollection(this);
                }
                return m_ForeignKeys;
            }
        }

        private PhysicalPartitionCollection m_PhysicalPartitions;

        /// <summary>
        /// Collection class instance for the PhysicalPartitions of the table
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(PhysicalPartition))]
        public PhysicalPartitionCollection PhysicalPartitions
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PhysicalPartition));
                if (null == m_PhysicalPartitions)
                {
                    m_PhysicalPartitions = new PhysicalPartitionCollection(this);
                }

                return m_PhysicalPartitions;
            }
        }
        private PartitionSchemeParameterCollection m_PartitionSchemeParameters;

        /// <summary>
        /// Specifies the columns that define the input parameters for the Partition Scheme.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(PartitionSchemeParameter))]
        public PartitionSchemeParameterCollection PartitionSchemeParameters
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PartitionSchemeParameter));
                if (null == m_PartitionSchemeParameters)
                {
                    m_PartitionSchemeParameters = new PartitionSchemeParameterCollection(this);

                    // if the table exists don't allow changes to its partition scheme parameters
                    if (State == SqlSmoState.Existing)
                    {
                        m_PartitionSchemeParameters.LockCollection(ExceptionTemplates.ReasonObjectAlreadyCreated(UrnSuffix));
                    }
                }
                return m_PartitionSchemeParameters;
            }
        }

        /// <summary>
        /// Gets the file table namespace path.
        /// </summary>
        /// <returns>UNC Path to Shared FileTable folder </returns>
        public string GetFileTableNamespacePath()
        {
            if (!this.IsFileTable)
            {
                throw new InvalidOperationException(ExceptionTemplates.TableNotFileTable(this.Name));
            }
            if (!this.FileTableNamespaceEnabled)
            {
                throw new InvalidOperationException(ExceptionTemplates.NamespaceNotEnabled(this.Name));
            }
            string machineName = this.GetServerObject().NetName;
            string sqlServerFileStreamShare = this.GetServerObject().FilestreamShareName;
            string dbFileStreamShare = this.Parent.FilestreamDirectoryName;
            string fileTableShare = this.FileTableDirectoryName;
            string path = string.Format(System.Globalization.CultureInfo.InvariantCulture
                            , @"\\{0}\{1}\{2}\{3}", machineName, sqlServerFileStreamShare, dbFileStreamShare, fileTableShare);
            return Path.GetFullPath(path);
        }

        internal bool IsDirty(string property)
        {
            return this.Properties.IsDirty(this.Properties.LookupID(property, PropertyAccessPurpose.Read));
        }

        public void Create()
        {
            base.CreateImpl();
            SetSchemaOwned();
        }

        internal override void ValidateName(string name)
        {
            base.ValidateName(name);
            CheckTableName(name);
        }

        /// <summary>
        /// Indexes scripted with Table
        /// </summary>
        private List<Index> indexPropagationList;

        /// <summary>
        /// Adds an index to the propagation list, initializing it if needed.
        /// </summary>
        /// <param name="i"></param>
        internal void AddToIndexPropagationList(Index i)
        {
            if (null == indexPropagationList)
            {
                indexPropagationList = new List<Index>();
            }

            indexPropagationList.Add(i);
        }

        /// <summary>
        /// Constraints scripted with Table
        /// </summary>
        private List<SqlSmoObject> embeddedForeignKeyChecksList;

        /// <summary>
        /// Adds a constraint to the propagation list, initializing it if needed.
        /// </summary>
        /// <param name="fkck"></param>
        internal void AddToEmbeddedForeignKeyChecksList(SqlSmoObject fkck)
        {
            if (null == embeddedForeignKeyChecksList)
            {
                embeddedForeignKeyChecksList = new List<SqlSmoObject>();
            }

            embeddedForeignKeyChecksList.Add(fkck);
        }

        /// <summary>
        /// This returns a collection of T-SQL INSERT statements for the data
        /// in this table.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal IEnumerable<string> ScriptDataInternal(ScriptingPreferences sp)
        {
            return new DataScriptCollection(this, sp);
        }

        /// <summary>
        /// Returns a T-SQL string used to be able to drop the data from the table.
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal StringCollection ScriptDropData(ScriptingPreferences sp)
        {
            string dropSql = string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "DELETE FROM {0}",
                                FormatFullNameForScripting(sp));

            StringCollection sc = new StringCollection();
            sc.Add(dropSql);

            return sc;
        }

        /// <summary>
        /// Returns an IEnumerable<script> object with the script for the table.
        /// </summary>
        /// <returns>an IEnumerable<script> object with the script for the table</returns>
        public IEnumerable<string> EnumScript()
        {
            return EnumScriptImpl(new ScriptingPreferences(this));
        }

        /// <summary>
        /// Returns an IEnumerable<script> object with the script for the table. Uses the passed
        /// value of scriptingOptions to generate the script
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns>an IEnumerable<script> object with the script for the table</returns>
        public IEnumerable<string> EnumScript(ScriptingOptions scriptingOptions)
        {
            if (null == scriptingOptions)
            {
                throw new ArgumentNullException("scriptingOptions");
            }

            Scripter tmpScripter = new Scripter(this.GetServerObject());

            // if the user has not set the target server version, then we'll
            // set it to match the version of this object
            if (!scriptingOptions.GetScriptingPreferences().TargetVersionAndDatabaseEngineTypeDirty)
            {
                scriptingOptions.SetTargetServerInfo(this, false);
            }
            tmpScripter.Options = scriptingOptions;
            return tmpScripter.EnumScript(this);
        }

        /// <summary>
        /// Initializes the PhysicalPartitions collection
        /// </summary>
        public void InitPhysicalPartitions()
        {
            InitChildLevel("PhysicalPartition", new ScriptingPreferences(), false);
        }

        /// <summary>
        /// Initializes the Index collection
        /// </summary>
        public void InitIndexes()
        {
            InitChildLevel("Index", new ScriptingPreferences(), false);
        }

        /// <summary>
        /// Initializes the Column collection
        /// </summary>
        public void InitColumns()
        {
            InitChildLevel("Column", new ScriptingPreferences(), false);
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            // validate sp
            if (sp == null)
            {
                throw new ArgumentNullException("Scripting preferences cannot be null.");
            }
            this.ThrowIfNotSupported(this.GetType(), sp);

            // if external table, validates it does not contain any unsupported collections
            this.ValidateExternalTable();

            // validate indexes on this table
            this.ValidateIndexes();

            // validate temporal
            bool hasPeriod = false;

            bool isEdgeTable = GetPropValueIfSupportedWithThrowOnTarget("IsEdge", false, sp);
            bool isNodeTable = GetPropValueIfSupportedWithThrowOnTarget("IsNode", false, sp);

            // If this is a graph table and the current SqlServer version does not support
            // scripting of graph tables then bubble up an UnsupportedOperation exception.
            // TODO: VSTS 12878128 - Short circuit setter(s) for all graph properties post instantiation
            // associated objects.
            //
            if (isEdgeTable || isNodeTable)
            {
                if (sp.TargetDatabaseEngineType == DatabaseEngineType.Standalone && ((int)sp.TargetServerVersionInternal < (int)SqlServerVersionInternal.Version140))
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersionException);
                }
            }

            if (this.IsSupportedProperty("HasSystemTimePeriod"))
            {
                this.ValidateSystemTimeTemporal();

                if (State == SqlSmoState.Existing)
                {
                    hasPeriod = this.HasSystemTimePeriod;
                }
                else
                {
                    hasPeriod = this.m_systemTimePeriodInfo.m_MarkedForCreate;
                }
            }

            // do not script a dropped ledger table
            if (GetPropValueOptional<bool>(nameof(IsDroppedLedgerTable), false))
            {
                return;
            }

            // do not script create history table for a temporal table in a ledger database
            //
            if (IsSupportedProperty(nameof(LedgerType)) && GetPropValueOptional(nameof(LedgerType), LedgerTableType.None) == LedgerTableType.HistoryTable)
            {
                return;
            }

            //create intermediate string collection
            StringCollection scqueries = new StringCollection();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full table name for scripting
            string sFullTableName = FormatFullNameForScripting(sp);

            if (sp.OldOptions.PrimaryObject)
            {
                if (sp.IncludeScripts.Header) // need to generate commentary headers
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, sFullTableName, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                bool fAnsiNullsExists = false;
                bool ansiPaddingStatus = false;

                if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    // save server settings first
                    Server svr = (Server)GetServerObject();
                    ansiPaddingStatus = svr.UserOptions.AnsiPadding;
                }

                // If parent database exists then take the setting from it.
                // AnsiPadding is supported in 7.0, but the DatabaseOption for it is not supported
                // in 7.0. It is because DATABASEPROPERTYEX cannot be applied to AnsiPaddingEnabled in 7.0.
                if (Parent.State == SqlSmoState.Existing && ServerVersion.Major > 7)
                {
                    ansiPaddingStatus = this.Parent.DatabaseOptions.AnsiPaddingEnabled;
                }

                bool bConsiderAnsiQI = (((int)SqlServerVersionInternal.Version80 <= (int)sp.TargetServerVersionInternal) &&
                    (ServerVersion.Major > 7));

                if (bConsiderAnsiQI)
                {
                    fAnsiNullsExists = (null != Properties.Get("AnsiNullsStatus").Value);

                    // set ANSI_NULLS and QUOTED_IDENTIFIER flags before CREATE TABLE ...
                    if (fAnsiNullsExists)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_ANSI_NULLS,
                                (bool)Properties["AnsiNullsStatus"].Value ? Globals.On : Globals.Off);
                        scqueries.Add(sb.ToString());
                        sb.Length = 0;
                    }

                    // QUOTED_IDENTIFIER in Tables metadata is always ON
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.SET_QUOTED_IDENTIFIER, Globals.On);
                    scqueries.Add(sb.ToString());
                    sb.Length = 0;
                }

                // set the ANSI_PADDING only if the table creation script
                // might benefit in any way from this
                Nullable<bool> tablePaddingSetting = GetTableAnsiPadded();

                bool scriptIndexOptionsForComputedColumns = false;

                if (this.indexPropagationList != null)
                {
                    foreach (Index idx in this.indexPropagationList)
                    {

                        // skip if computed columns are not supported
                        // Skip script index options for computed columns for Azure SQL DW (currently not supported in Azure SQL DW)
                        if (!IsSupportedProperty("IsComputed"))
                        {
                            continue;
                        }

                        foreach (IndexedColumn column in idx.IndexedColumns)
                        {
                            if (column.State != SqlSmoState.Creating &&
                                column.IsComputed)
                            {
                                scriptIndexOptionsForComputedColumns = true;
                                break;
                            }
                        }
                        if (scriptIndexOptionsForComputedColumns)
                        {
                            break;
                        }
                    }
                }

                if (scriptIndexOptionsForComputedColumns)
                {
                    tablePaddingSetting = true;

                    scqueries.Add(string.Format(SmoApplication.DefaultCulture,
                        "SET ARITHABORT ON"));
                }

                if (sp.IncludeScripts.AnsiPadding && tablePaddingSetting.HasValue)
                {
                    if (tablePaddingSetting == true)
                    {
                        scqueries.Add(string.Format(SmoApplication.DefaultCulture,
                            "SET ANSI_PADDING ON"));
                        //Setting Ansi padding for Clustered index as per table settings.
                        foreach (var idx in from Index idx in Indexes
                                            where idx.IsClustered
                                            select idx)
                        {
                            idx.IsParentBeingScriptedWithANSIPaddingON = true;
                        }
                    }
                    else
                    {
                        scqueries.Add(string.Format(SmoApplication.DefaultCulture,
                            "SET ANSI_PADDING OFF"));
                    }
                }


                if (sp.IncludeScripts.ExistenceCheck)
                {
                    // perform check for existing object
                    if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "NOT", SqlString(sFullTableName));
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE80, "NOT", SqlString(sFullTableName));
                    }
                    sb.Append(sp.NewLine);
                    sb.Append(Scripts.BEGIN);
                    sb.Append(sp.NewLine);
                }

                // check if the table is an external table
                bool isExternal = this.CheckIsExternalTable();

                if (!isExternal)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE TABLE {0}", sFullTableName);
                }
                else // create external table script
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE EXTERNAL TABLE {0}", sFullTableName);
                }

                // check if table is memory optimized
                var isMemoryOptimized = CheckIsMemoryOptimizedTable();

                var isSysVersioned = false;
                if (IsSupportedProperty(nameof(IsSystemVersioned), sp))
                {
                    isSysVersioned = GetPropValueOptional(nameof(IsSystemVersioned), false);
                }

                var histTableName = string.Empty;
                if (IsSupportedProperty(nameof(HistoryTableName), sp))
                {
                    histTableName = GetPropValueOptional(nameof(HistoryTableName), string.Empty);
                }

                var histTableSchema = string.Empty;
                if (IsSupportedProperty(nameof(HistoryTableSchema), sp))
                {
                    histTableSchema = GetPropValueOptional(nameof(HistoryTableSchema), string.Empty);
                }

                // history table can't be provided if system versioning is off
                if (!isSysVersioned && !IsSupportedProperty(nameof(LedgerType)))
                {
                    if (!string.IsNullOrEmpty(histTableName) && !string.IsNullOrEmpty(histTableName.Trim()))
                    {
                        throw new SmoException(ExceptionTemplates.HistoryTableWithoutSystemVersioning);
                    }

                    if (!string.IsNullOrEmpty(histTableSchema) && !string.IsNullOrEmpty(histTableSchema.Trim()))
                    {
                        throw new SmoException(ExceptionTemplates.HistoryTableWithoutSystemVersioning);
                    }
                }

                // check if the table is a SQL DW table
                var isSqlDw = CheckIsSqlDwTable();

                // for [Memory-Optimized|External|SQL DW] tables skip the file table checks
                Boolean isFileTable = false;
                if (!isMemoryOptimized && !isExternal && !isSqlDw)
                {
                    if (IsSupportedProperty("IsFileTable"))
                    {
                        Boolean isFileTableProp = this.GetPropValueOptional("IsFileTable", false);
                        if (isFileTableProp)
                        {
                            // IsFileTable is supported as a hard coded 0 on Azure; we can't script file table to that target
                            if (!IsSupportedProperty("IsFileTable", sp) || sp.TargetDatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
                            {
                                throw new UnsupportedVersionException(ExceptionTemplates.FileTableNotSupportedOnTargetEngine(GetSqlServerName(sp)));
                            }
                            if (Columns.Count > 0 && this.State == SqlSmoState.Creating)
                            {
                                throw new SmoException(ExceptionTemplates.FileTableCannotHaveUserColumns);
                            }

                            // temporal table can't be a file table
                            if (isSysVersioned || hasPeriod)
                            {
                                throw new SmoException(ExceptionTemplates.NoTemporalFileTables);
                            }

                            isFileTable = true;
                            sb.AppendFormat(SmoApplication.DefaultCulture, " AS FILETABLE");
                            // script the table with CREATE TABLE AS FILETABLE statement
                            GetFileTableCreationScript(sp, sb);
                        }
                    }
                }

                // for memory optimized, external, SQL DW and regular tables, if at least one column exists, script the table
                if (!isFileTable)
                {
                    // All tables must have at least one user defined column except edge tables. Edge tables
                    // do not require any user defined columns.
                    if (Columns.Count < 1 && !isEdgeTable)
                    {
                        throw new SmoException(ExceptionTemplates.ObjectWithNoChildren("Table", "Column"));
                    }

                    // if we have requested the script to contain the ANSI_PADDING
                    // settings and the table contains multiple settings for ANSI_PADDING
                    // then we need to script the table creation differently
                    if (sp.IncludeScripts.AnsiPadding && HasMultiplePaddings())
                    {
                        // at this point the table-level ansi_padding should have a valid value
                        Diagnostics.TraceHelper.Assert(tablePaddingSetting.HasValue);
                        GetTableCreationScriptWithAnsiPadding(sp, sb, tablePaddingSetting.Value);
                    }
                    else
                    {
                        if (isExternal)
                        {
                            // construct the create external table script
                            this.GetExternalTableCreationScript(sp, sb);
                        }
                        else if (isMemoryOptimized)
                        {
                            // If table is memory optimized get the script for Hekaton tables and add memory optimized properties
                            this.GetMemoryOptimizedTableCreationScript(sp, sb);
                        }
                        else if (isSqlDw)
                        {
                            // construct the create SQL DW table script
                            this.GetSqlDwTableCreationScript(sp, sb);
                        }
                        else
                        {
                            // script the table with one "regular" CREATE TABLE statement
                            this.GetTableCreationScript(sp, sb);
                        }
                    }
                }

                sb.Append(sp.NewLine);

                if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
                {
                    sb.Append(Scripts.END);
                }

                scqueries.Add(sb.ToString());
                sb.Length = 0;

                // if user wants to set vardecimal on, do it
                bool forCreateScript = true;
                ScriptVardecimalCompression(scqueries, sp, forCreateScript);

                // Script for Change Tracking options on table
                if (IsSupportedProperty("ChangeTrackingEnabled", sp) && sp.Data.ChangeTracking)
                {
                    // if yes, throw an exception as change tracking is no supported on external tables
                    if (this.GetPropValueOptional("ChangeTrackingEnabled", false) && this.CheckIsExternalTable())
                    {
                        throw new SmoException(ExceptionTemplates.ChangeTrackingNotSupportedOnExternalTables);
                    }
                    ScriptChangeTracking(scqueries, sp);
                }

                // restore the server-wide ANSI_PADDING settings
                if (sp.IncludeScripts.AnsiPadding && tablePaddingSetting.HasValue)
                {
                    scqueries.Add(string.Format(SmoApplication.DefaultCulture,
                        "SET ANSI_PADDING {0}", ansiPaddingStatus ? "ON" : "OFF"));
                }

                // if the table does not have a clustered index we want to update the
                // page and row count on the heap index that represents the storage
                // for that table.
                if (sp.Data.OptimizerData &&
                    this.ServerVersion.Major >= 9 &&
                    sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90 &&
                    !isMemoryOptimized &&
                    !isSqlDw &&
                    this.DatabaseEngineEdition != DatabaseEngineEdition.SqlOnDemand)
                {
                    ScriptUpdateStatistics(sp, scqueries, sb);
                }

                if (!sp.IncludeScripts.Data)
                {
                    ScriptBindings(scqueries, sp);
                }

                if (IsSupportedProperty("LockEscalation", sp))
                {
                    ScriptLockEscalationSettings(scqueries, sp);
                }

                ScriptAlterFileTableProp(scqueries, sp);

                // In Hekaton M5, ALTER AUTHORIZATION is not supported for a memory optimized table.
                if (!isMemoryOptimized && sp.IncludeScripts.Owner)
                {
                    ScriptOwner(scqueries, sp);
                }

                if (IsSupportedProperty("RemoteDataArchiveEnabled", sp))
                {
                    ScriptRemoteDataArchive(scqueries, sp);
                }

                // Add scripts for classified columns
                foreach (Column column in Columns)
                {
                    column.ScriptDataClassification(scqueries, sp, forCreateScript);
                }
            }

            this.indexPropagationList = null;
            this.embeddedForeignKeyChecksList = null;
            //copy intermediate string collection to result
            foreach (string s in scqueries)
            {
                queries.Add(s);
            }
        }

        /// <summary>
        /// scripts a memory optimized table
        /// </summary>
        /// <param name="sp"> Scripting preferences </param>
        /// <param name="sb"> The string builder to hold scripts </param>
        private void GetMemoryOptimizedTableCreationScript(ScriptingPreferences sp, StringBuilder sb)
        {
            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            // first make sure both current object and target scripting environment
            // support memory optimized table
            this.ThrowIfPropertyNotSupported("IsMemoryOptimized", sp);

            // script columns
            sb.Append(sp.NewLine);
            ScriptColumns(sp, sb, this.Columns);

            // script temporal PERIOD if table has one
            //
            if (IsSupportedProperty("HasSystemTimePeriod", sp))
            {
                // System-time temporal support only valid in SQL 2016+ (v13+)
                //
                ScriptPeriodForSystemTime(sb);
            }

            // add comma at the end of scripted columns. A memory optimized table has at least one index or a primary key.
            sb.Append(Globals.comma);
            sb.Append(sp.NewLine);

            // script all indexes and keys at the end
            GenerateMemoryOptimizedIndexes(sb, sp, this.Indexes);

            sb.Append(sp.NewLine);
            sb.Append(Globals.RParen);

            // Check if the table is system-versioned temporal table
            // If so, we need to append appropriate SYSTEM_VERSIONING clause besides
            // usual one for Hekaton tables
            //
            string systemVersioningWithClauseContent = GenerateSystemVersioningWithClauseContent(sp);

            if (!String.IsNullOrEmpty(systemVersioningWithClauseContent))
            {
                sb.AppendFormat(
                    Scripts.WITH_MEMORY_OPTIMIZED_AND_DURABILITY_AND_TEMPORAL_SYSTEM_VERSIONING,
                    (DurabilityTypeMap)this.Durability,
                    systemVersioningWithClauseContent);
            }
            else
            {
                // Mapping from DurabilityType enum to DurabilityTypeMap to get the correct TSQL syntax
                sb.AppendFormat(Scripts.WITH_MEMORY_OPTIMIZED_AND_DURABILITY, (DurabilityTypeMap)this.Durability);
            }
        }

        /// <summary>
        /// Scripts an external table.
        /// </summary>
        /// <param name="sp">The string builder to hold scripts.</param>
        /// <param name="sb">Scripting preferences.</param>
        private void GetExternalTableCreationScript(ScriptingPreferences sp, StringBuilder sb)
        {
            /* CREATE EXTERNAL TABLE [ database_name . [ dbo ] . | dbo. ] table_name
             * ( { <column_definition> } [ ,...n ] )
             * { WITH ( <external_table_options> [,...n] ) }
             *
             * <external_table_options> ::=
             * LOCATION = '<File Path or Folder in HDFS>',
             * DATA_SOURCE = <External_Data_Source>,
             * FILE_FORMAT = <File_Format>,
             * [REJECT_TYPE = Value],
             * [REJECT_VALUE = Num_Value],
             * [REJECT_SAMPLE_VALUE = Num_Value],
             * [REJECTED_ROW_LOCATION = '<File Path or Folder in HDFS>'])
             */

            const string DataSourceNamePropertyName = "DataSourceName";

            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            // first make sure both current object and target scripting environment
            // support external table
            this.ThrowIfPropertyNotSupported("IsExternal", sp);

            // Check that the required external data source name property is supported and set, if not, throw an exception.
            this.ValidateExternalTableRequiredStringProperty(DataSourceNamePropertyName, sp);
            string externalDataSourceName = this.GetPropertyOptional(DataSourceNamePropertyName).Value.ToString();

            // script columns
            sb.Append(sp.NewLine);
            ScriptColumns(sp, sb, this.Columns);

            sb.Append(sp.NewLine);
            sb.Append(Globals.RParen);
            sb.Append(sp.NewLine);

            // create a list of external table options
            StringBuilder externalTableOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            externalTableOptions.AppendFormat(SmoApplication.DefaultCulture, Scripts.EXTERNAL_DATASOURCE_NAME, MakeSqlBraket(externalDataSourceName));

            // Validate all optional properties; make sure there are no conflicts and the property
            // combination is correct.
            this.ValidateExternalTableOptionalProperties(sp);

            // add any optional properties if they are set
            this.ProcessExternalTableOptionalProperties(externalTableOptions, sp);

            // of there are external table options specified,
            // add them to the WITH clause
            if (externalTableOptions.Length > 0)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "WITH ({0})", externalTableOptions.ToString());
            }
        }

        /// <summary>
        /// Scripts a SQL DW table.
        /// </summary>
        /// <param name="sp">The string builder to hold scripts.</param>
        /// <param name="sb">Scripting preferences.</param>
        private void GetSqlDwTableCreationScript(ScriptingPreferences sp, StringBuilder sb)
        {
            /* CREATE TABLE [ database_name . [ dbo ] . | dbo. ] table_name
             * ( { <column_definition> } [ ,...n ] )
             * { WITH ( <table_options> [,...n] ) }
             *
             * table_options> ::=
             * CLUSTERED COLUMNSTORE INDEX | HEAP | CLUSTERED INDEX ({ index_column_name [ ASC | DESC ] } [ ,...n ] ),
             * DISTRIBUTION = {ROUND_ROBIN | HASH ([distribution_column_name]) | REPLICATE},
             * PARTITION ( partition_column_name RANGE [ LEFT | RIGHT ]
             *   FOR VALUES ( [ boundary_value [,...n] ] )
             */

            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            // first make sure both current object and target scripting environment
            // support SQL DW table
            this.ThrowIfPropertyNotSupported("DwTableDistribution", sp);

            // script columns
            sb.Append(sp.NewLine);
            ScriptColumns(sp, sb, this.Columns);

            // add primary and unique keys
            GeneratePkUkInCreateTable(sb, sp, sp.ForDirectExecution ? Indexes : (ICollection)indexPropagationList, true);

            sb.Append(sp.NewLine);
            sb.Append(Globals.RParen);
            sb.Append(sp.NewLine);

            // create a list of SQL DW table options
            StringBuilder sqlDwTableOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // add SQL DW properties
            this.ProcessSqlDwTableProperties(sqlDwTableOptions, sp);

            // add the table options to the WITH clause
            if (sqlDwTableOptions.Length > 0)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "WITH");
                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.newline);
                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);
                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.newline);
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", sqlDwTableOptions.ToString());
                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.newline);
                sb.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
            }
        }

        private void ScriptUpdateStatistics(ScriptingPreferences sp, StringCollection scqueries, StringBuilder sb)
        {
            bool hasClusteredIndex = false;

            foreach (Index idx in Indexes)
            {
                if (idx.GetPropValueOptional("IsClustered", false))
                {
                    hasClusteredIndex = true;
                    break;
                }
            }

            if (!hasClusteredIndex)
            {
                string streamQuery = string.Format(SmoApplication.DefaultCulture,
                    "DBCC SHOW_STATISTICS(N'{0}.{1}') WITH STATS_STREAM",
                    SqlString(Parent.FullQualifiedName),
                    SqlString(FullQualifiedName));

                DataSet ds = null;
                Cmn.SqlExecutionModes em = this.ExecutionManager.ConnectionContext.SqlExecutionModes;
                this.ExecutionManager.ConnectionContext.SqlExecutionModes = Cmn.SqlExecutionModes.ExecuteSql;
                try
                {
                    ds = this.ExecutionManager.ExecuteWithResults(streamQuery);
                }
                finally
                {
                    this.ExecutionManager.ConnectionContext.SqlExecutionModes = em;
                }

                if (ds.Tables.Count > 0)
                {
                    DataTable streamTbl = ds.Tables[0];
                    if (streamTbl.Rows.Count > 0)
                    {
                        object stRows = streamTbl.Rows[0]["Rows"];
                        object stPages = streamTbl.Rows[0]["Data Pages"];
                        if (null != stRows && !(stRows is DBNull) &&
                            null != stPages && !(stPages is DBNull))
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture,
                                "UPDATE STATISTICS {0} WITH ROWCOUNT = {1}, PAGECOUNT = {2}",
                                FormatFullNameForScripting(sp),
                                Convert.ToInt64(stRows, SmoApplication.DefaultCulture).ToString(SmoApplication.DefaultCulture),
                                Convert.ToInt64(stPages, SmoApplication.DefaultCulture).ToString(SmoApplication.DefaultCulture));
                            scqueries.Add(sb.ToString());
                            sb.Length = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Script bindings
        /// </summary>
        /// <param name="scqueries"></param>
        /// <param name="so"></param>
        internal void ScriptBindings(StringCollection scqueries, ScriptingPreferences sp)
        {
            // script all bindings
            if (sp.OldOptions.Bindings)
            {
                foreach (Column col in Columns)
                {
                    col.ScriptDefaultAndRuleBinding(scqueries, sp); //This method doesn't script anything for Cloud Scenarios.
                }
            }

        }

        /// <summary>
        /// This method just check whether compression related code is required or not.
        /// </summary>
        /// <param name="bAlter"></param>
        /// <returns></returns>
        private bool IsCompressionCodeRequired(bool bAlter)
        {
            Diagnostics.TraceHelper.Assert(this.ServerVersion.Major >= 10);
            Diagnostics.TraceHelper.Assert((this.State == SqlSmoState.Existing) || (this.State == SqlSmoState.Creating));
            if (this.State == SqlSmoState.Creating)
            {
                if (m_PhysicalPartitions != null)
                {
                    return PhysicalPartitions.IsCompressionCodeRequired(bAlter);
                }
                return false;
            }
            if (this.HasCompressedPartitions)
            {
                return true;
            }
            if ((null != m_PhysicalPartitions) &&
                                (PhysicalPartitions.IsCollectionDirty()))
            {
                return PhysicalPartitions.IsCompressionCodeRequired(bAlter);
            }
            return false;
        }

        /// <summary>
        /// This method just checks whether xml compression related code is required or not.
        /// </summary>
        /// <param name="bAlter"></param>
        /// <returns></returns>
        private bool IsXmlCompressionCodeRequired(bool bAlter)
        {
            if (!this.IsSupportedProperty(nameof(HasXmlCompressedPartitions)))
            {
                return false;
            }

            Diagnostics.TraceHelper.Assert(this.IsSupportedProperty(nameof(HasXmlCompressedPartitions)));
            Diagnostics.TraceHelper.Assert((this.State == SqlSmoState.Existing) || (this.State == SqlSmoState.Creating));
            if (this.State == SqlSmoState.Creating)
            {
                if (m_PhysicalPartitions != null)
                {
                    return PhysicalPartitions.IsXmlCompressionCodeRequired(bAlter);
                }
                return false;
            }

            if (this.HasXmlCompressedPartitions)
            {
                return true;
            }

            if ((null != m_PhysicalPartitions) &&
                (PhysicalPartitions.IsXmlCollectionDirty()))
            {
                return PhysicalPartitions.IsXmlCompressionCodeRequired(bAlter);
            }

            return false;
        }

        /// <summary>
        /// Generates the table creation script.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="sb"></param>
        private void GetTableCreationScript(ScriptingPreferences sp, StringBuilder sb)
        {
            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            bool isEdgeTable = GetPropValueIfSupportedWithThrowOnTarget("IsEdge", false, sp);

            ScriptTableInternal(sp, sb, Columns, sp.ForDirectExecution ? Indexes : (ICollection)indexPropagationList, isEdgeTable: isEdgeTable);

            if (IsSupportedProperty("HasSystemTimePeriod", sp))
            {
                // System-time temporal support only valid in SQL 2015+ (v13+)
                ScriptPeriodForSystemTime(sb);
            }

            ScriptChecksAndForeignKeys(sp, sb);

            // Only append the closing right parenthesis if the table is not an edge table
            // or if it is an edge table that has some columns. Edge tables are allowed to
            // not have any columns.
            //
            if (ShouldEmitColumnListParenthesis(isEdgeTable, Columns))
            {
                sb.Append(sp.NewLine);
                sb.Append(Globals.RParen);
            }

            // At this point if the table is a node or edge the node/edge syntax needs to be added.
            //
            GenerateGraphScript(sb, sp);

            if (sp.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse)
            {
                // Specify data space option
                //
                GenerateDataSpaceScript(sb, sp);

                // Specify the TEXTIMAGE_ON
                //
                GenerateTextFileGroupScript(sb, sp);
            }

            if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                // Specify the filestream filegroup or partition
                //
                GenerateDataSpaceFileStreamScript(sb, sp, false);
            }

            if (sp.TargetEngineIsAzureStretchDb())
            {
                // Stretch dbs need a HEAP clause if no clustered index
                // in any case it is done at this point
                GenerateStretchHeapWithClause(sb, sp);
            }
            else
            {
                // With Datacompression, Filetable, System-Versioning options and Data Retention
                //
                GenerateWithOptionScript(sb, sp);
            }
        }

        /// <summary>
        /// Generates the PERIOD script
        /// </summary>
        /// <param name="sb"></param>
        private void ScriptPeriodForSystemTime(StringBuilder sb)
        {
            Diagnostics.TraceHelper.Assert(IsSupportedProperty("HasSystemTimePeriod"));

            string startCol = String.Empty;
            string endCol = String.Empty;
            bool hasPeriod = false;

            hasPeriod = GetPropValueOptional<bool>("HasSystemTimePeriod", false);

            if (this.m_systemTimePeriodInfo.m_MarkedForCreate)
            {
                startCol = this.m_systemTimePeriodInfo.m_StartColumnName;
                endCol = this.m_systemTimePeriodInfo.m_EndColumnName;
            }
            else if (hasPeriod)
            {
                startCol = this.SystemTimePeriodStartColumn;
                endCol = this.SystemTimePeriodEndColumn;
            }
            else
            {
                return;
            }

            startCol = Util.EscapeString(startCol, ']');
            endCol = Util.EscapeString(endCol, ']');

            sb.Append(Globals.comma);
            sb.AppendLine();
            sb.Append(Globals.tab);
            sb.AppendFormat(SmoApplication.DefaultCulture,
                "PERIOD FOR SYSTEM_TIME ([{0}], [{1}])",
                startCol,
                endCol);
        }

        /// <summary>
        /// Generates the table creation script for a FileTable.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="sb"></param>
        private void GetFileTableCreationScript(ScriptingPreferences sp, StringBuilder sb)
        {
            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            //Code that follows from here is not supported on Cloud.
            GenerateDataSpaceScript(sb, sp);

            //specify the TEXTIAGE_ON
            GenerateTextFileGroupScript(sb, sp);

            //specify the filestream filegroup or partition
            GenerateDataSpaceFileStreamScript(sb, sp, false);

            //With Datacompression and Filetable options.
            GenerateWithOptionScript(sb, sp);
        }

        private void ScriptChecksAndForeignKeys(ScriptingPreferences sp, StringBuilder sb)
        {
            if (this.embeddedForeignKeyChecksList == null)
            {
                return;
            }

            foreach (SqlSmoObject fkck in this.embeddedForeignKeyChecksList)
            {
                sb.Append(Globals.comma);
                sb.Append(sp.NewLine);
                sb.Append((fkck.Urn.Type.Equals(ForeignKey.UrnSuffix)) ? ((ForeignKey)fkck).ScriptDdlBody(sp) : ((Check)fkck).ScriptDdlBody(sp));
            }
        }

        /// <summary>
        /// This method helps in avoiding the duplicate script generation for data compression
        /// in case if table has clustered primary key. Script generation for a existing table
        /// take care to include the primary key constraint inside the table create script itself.
        /// So, if table is compressed then primary key constraint also include the data compression
        /// property too. In such scenario we want to avoid the data compression code generation
        /// for table. At present in fact engine throws exception for such duplicate unlike
        /// filegroup parameter where it simply ignore the parameter value set for table if primary
        /// key has already.
        /// Avoid duplicating data compression for unique clustered index too.
        /// </summary>
        /// <returns></returns>
        private bool HasClusteredPrimaryOrUniqueKey(ScriptingPreferences sp)
        {
            if (this.State != SqlSmoState.Existing)
            {
                return false;
            }

            if (!this.HasClusteredIndex)
            {
                return false;
            }

            if ((this.indexPropagationList == null) || (this.indexPropagationList.Count == 0))
            {
                return false;
            }
            else
            {
                Index clusteredIndex = null;
                foreach (Index idx in this.indexPropagationList)
                {
                    if (idx.IsClustered)
                    {
                        clusteredIndex = idx;
                        break;
                    }
                }

                if (clusteredIndex == null)
                {
                    return false;
                }
                if (!sp.ScriptForCreateDrop && clusteredIndex.IgnoreForScripting)
                {
                    return false;
                }
                return (IndexKeyType.DriPrimaryKey == clusteredIndex.IndexKeyType ||
                    IndexKeyType.DriUniqueKey == clusteredIndex.IndexKeyType);
            }
        }

        /// <summary>
        /// Generates the creation script for tables with multiple different
        /// ansi_padding columns
        /// </summary>
        /// <param name="sp">scripting preference</param>
        /// <param name="sb">string builder</param>
        /// <param name="initialPadding">use initial padding</param>
        private void GetTableCreationScriptWithAnsiPadding(ScriptingPreferences sp, StringBuilder sb, bool initialPadding)
        {
            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            StringCollection col_strings = new StringCollection();
            bool fFirstColumn = true;
            IEnumerator enumColls = Columns.GetEnumerator();
            bool paddingHasFlipped = false;

            // script all columns with the same ansi_padding setting
            //
            bool isEdgeTable = GetPropValueIfSupportedWithThrowOnTarget("IsEdge", false, sp);

            // Only append the opening left parenthesis if the table is not an edge table
            // or if it is an edge table that has some columns. Edge tables are allowed to
            // not have any columns.
            //
            if (ShouldEmitColumnListParenthesis(isEdgeTable, Columns))
            {
                sb.Append(Globals.LParen);
                sb.Append(sp.NewLine);
            }

            while (enumColls.MoveNext())
            {
                Column column = enumColls.Current as Column;
                Diagnostics.TraceHelper.Assert(null != column);

                // prepare to script column only if in direct execution mode or not explicitly directed to ignore it
                if (sp.ScriptForCreateDrop || !column.IgnoreForScripting)
                {
                    Nullable<bool> columnPadding = GetColumnPadding(column);

                    // stop the loop if a column with a different padding is encountered
                    if (columnPadding.HasValue && (columnPadding != initialPadding))
                    {
                        paddingHasFlipped = true;
                        break;
                    }

                    // generate creation script for that column
                    column.ScriptDdlInternal(col_strings, sp);

                    if (fFirstColumn)
                    {
                        fFirstColumn = false;
                    }
                    else
                    {
                        sb.Append(Globals.comma);
                        sb.Append(sp.NewLine);
                    }
                    sb.Append(Globals.tab);
                    // there needs to be just one string in this collection
                    Diagnostics.TraceHelper.Assert(col_strings.Count == 1);
                    sb.Append(col_strings[0]);

                    col_strings.Clear();
                }
            }

            // Only append the closing right parenthesis if the table is not an edge table
            // or if it is an edge table that has some columns. Edge tables are allowed to
            // not have any columns.
            //
            if (ShouldEmitColumnListParenthesis(isEdgeTable, Columns))
            {
                // end the CREATE TABLE statement here
                sb.Append(sp.NewLine);
                sb.Append(Globals.RParen);
            }

            // Graph node and edge tables require the AS NODE/AS EDGE syntax in this location.
            //
            GenerateGraphScript(sb, sp);

            // we should exit the loop before we finish the collection
            Diagnostics.TraceHelper.Assert(paddingHasFlipped);

            // generate script for the location of the table
            GenerateDataSpaceScript(sb, sp);

            // we are going to create the textimage destination
            // even though the textimage columns might not be in the first list
            // because there is no way to create such a table
            if (HasTextimageColumn(sp) &&
                sp.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse &&
                sp.Storage.FileGroup &&
                null != Properties.Get("TextFileGroup").Value)
            {
                string textFileGroup = (string)Properties["TextFileGroup"].Value;
                if (0 < textFileGroup.Length)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " TEXTIMAGE_ON [{0}]", SqlBraket(textFileGroup));
                }
            }

            //specify the filestream filegroup or partition
            if (IsSupportedProperty("FileStreamPartitionScheme", sp))
            {
                GenerateDataSpaceFileStreamScript(sb, sp, false);
            }

            //With Datacompression and Filetable options.
            GenerateWithOptionScript(sb, sp);

            // add the rest of the columns through ALTER TABLE ... ADD ... statements
            // this list will contain columns that we could not script with
            // CREATE TABLE ...
            bool currentPadding = initialPadding;
            do
            {
                Column col = enumColls.Current as Column;
                Diagnostics.TraceHelper.Assert(null != col);

                Nullable<bool> thisPadding = GetColumnPadding(col);
                // check if we need to change the ANSI_PADDING
                if (thisPadding.HasValue && thisPadding != currentPadding)
                {
                    currentPadding = thisPadding.Value;
                    sb.Append(Globals.newline);
                    sb.Append("SET ANSI_PADDING ");
                    sb.Append(currentPadding ? "ON" : "OFF");
                }
                col_strings.Clear();


                // Yukon - just add the column. Since the table is empty
                // we can add non-nullable columns to it directly
                col.ScriptCreateInternal(col_strings, sp, true);


                foreach (string s in col_strings)
                {
                    sb.Append(Globals.newline);
                    sb.Append(s);
                }
                col_strings.Clear();
            }
            while (enumColls.MoveNext());
            // add primary and unique keys
            GeneratePkUkInCreateTable(sb, sp, sp.ForDirectExecution ? Indexes : (ICollection)indexPropagationList, false);
        }

        /// <summary>
        /// Append the CREATE TABLE WITH Option script
        /// </summary>
        private void GenerateWithOptionScript(StringBuilder sb, ScriptingPreferences sp)
        {
            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);

            StringBuilder withOptions = new StringBuilder();

            // Data compression options
            // SQL Datawarehouse is a special case here - all tables created are compressed but it
            // doesn't actually support the DATA_COMPRESSION option so we need to specifically leave
            // it out
            //
            bool fDataCompression = ((sp.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse)
                && (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100)
                && (this.ServerVersion.Major >= 10)
                && !sp.TargetEngineIsAzureStretchDb()
                && sp.Storage.DataCompression
                && (!this.HasClusteredPrimaryOrUniqueKey(sp))
                && (IsCompressionCodeRequired(false)));

            // Xml compression options
            //
            bool fXmlCompression = (sp.TargetDatabaseEngineEdition != Cmn.DatabaseEngineEdition.SqlDataWarehouse)
                && (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version160)
                && (this.ServerVersion.Major >= 16 )
                && !sp.TargetEngineIsAzureStretchDb()
                && sp.Storage.XmlCompression
                && (!this.HasClusteredPrimaryOrUniqueKey(sp))
                && (IsXmlCompressionCodeRequired(false));

            if (fDataCompression)
            {
                withOptions.Append(PhysicalPartitions.GetCompressionCode(false, true, sp));
            }
            if (fXmlCompression)
            {
                if (fDataCompression)
                {
                    withOptions.Append(Globals.commaspace);
                }
                withOptions.Append(PhysicalPartitions.GetXmlCompressionCode(false, true, sp));
            }

            // FileTable options
            if (IsSupportedProperty("IsFileTable"))
            {
                bool isFileTable = this.GetPropValueOptional("IsFileTable", false);
                string fileTableDirectoryName = this.GetPropValueOptional("FileTableDirectoryName", string.Empty);
                if (!string.IsNullOrEmpty(fileTableDirectoryName))
                {
                    if (!isFileTable)
                    {
                        throw new SmoException(ExceptionTemplates.PropertyOnlySupportedForFileTable("FileTableDirectoryName"));
                    }
                    if (!string.IsNullOrEmpty(withOptions.ToString()))
                    {
                        withOptions.Append(Globals.commaspace);
                    }
                    withOptions.AppendFormat(SmoApplication.DefaultCulture, "FILETABLE_DIRECTORY = {0}", SqlSmoObject.MakeSqlString(fileTableDirectoryName));
                }

                string fileTableCollation = this.GetPropValueOptional("FileTableNameColumnCollation", string.Empty);
                if (!string.IsNullOrEmpty(fileTableCollation))
                {
                    if (!isFileTable)
                    {
                        throw new SmoException(ExceptionTemplates.PropertyOnlySupportedForFileTable("FileTableNameColumnCollation"));
                    }
                    if (!string.IsNullOrEmpty(withOptions.ToString()))
                    {
                        withOptions.Append(Globals.commaspace);
                    }
                    CheckCollation(fileTableCollation, sp);
                    withOptions.AppendFormat(SmoApplication.DefaultCulture, "FILETABLE_COLLATE_FILENAME = {0}", fileTableCollation);
                }

                string pkIndex, stream_idIndex, fullpathIndex;
                if (isFileTable && FetchFileTableIndexNames(out pkIndex, out stream_idIndex, out fullpathIndex, sp.Table.SystemNamesForConstraints))
                {
                    if (!string.IsNullOrEmpty(pkIndex))
                    {
                        if (withOptions.Length != 0)
                        {
                            withOptions.Append(Globals.commaspace);
                            withOptions.Append(Globals.newline);
                        }
                        withOptions.AppendFormat(SmoApplication.DefaultCulture, "FILETABLE_PRIMARY_KEY_CONSTRAINT_NAME=[{0}]", pkIndex);
                    }

                    if (!string.IsNullOrEmpty(stream_idIndex))
                    {
                        if (withOptions.Length != 0)
                        {
                            withOptions.Append(Globals.commaspace);
                            withOptions.Append(Globals.newline);
                        }
                        withOptions.AppendFormat(SmoApplication.DefaultCulture, "FILETABLE_STREAMID_UNIQUE_CONSTRAINT_NAME=[{0}]", stream_idIndex);
                    }

                    if (!string.IsNullOrEmpty(fullpathIndex))
                    {
                        if (withOptions.Length != 0)
                        {
                            withOptions.Append(Globals.commaspace);
                            withOptions.Append(Globals.newline);
                        }
                        withOptions.AppendFormat(SmoApplication.DefaultCulture, "FILETABLE_FULLPATH_UNIQUE_CONSTRAINT_NAME=[{0}]", fullpathIndex);
                    }
                }
            }

            // Append temporal system-versioning WITH clause:
            // Sample script:
            // SYSTEM_VERSIONING = ON (HISTORY_TABLE = [SCHEMA].[TABLE], DATA_CONSISTENCY_CHECK = ON))
            var sysVersioningClause = GenerateSystemVersioningWithClauseContent(sp);

            if (!string.IsNullOrEmpty(sysVersioningClause))
            {
                if (withOptions.Length != 0)
                {
                    withOptions.Append(Globals.commaspace);
                    withOptions.Append(Globals.newline);
                }
                withOptions.Append(sysVersioningClause);
            }

            if (IsSupportedProperty(nameof(LedgerType), sp))
            {
                var ledgerOptionClause = GenerateLedgerOptionsWithClauseContent(sp);
                if (!string.IsNullOrEmpty(ledgerOptionClause))
                {
                    if (withOptions.Length != 0)
                    {
                        withOptions.Append(Globals.commaspace);
                        withOptions.Append(Globals.newline);
                    }
                    withOptions.Append(ledgerOptionClause);
                }
            }

            // Append Data Retention WITH clause
            // DATA_DELETION = ON ( FILTER_COLUMN = COL_NAME, RETENTION_PERIOD = INFINITE )
            //
            string dataRetentionClause = ScriptDataRetention(sp).ToString();
            if (!string.IsNullOrEmpty(dataRetentionClause))
            {
                if (withOptions.Length != 0)
                {
                    withOptions.Append(Globals.commaspace);
                    withOptions.Append(Globals.newline);
                }
                withOptions.Append(dataRetentionClause);
            }

            if (!string.IsNullOrEmpty(withOptions.ToString()))
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}WITH{0}({0}{1}{0})", sp.NewLine, withOptions.ToString());
            }
        }

        /// <summary>
        // Script system-versioning clause content for temporal tables (used within WITH statement)
        // Looks like this: SYSTEM_VERSIONING = ON (HISTORY_TABLE = [SCHEMA].[TABLE], DATA_CONSISTENCY_CHECK = ON, HISTORY_RETENTION_PERIOD = 4 WEEK)
        /// </summary>
        private string GenerateSystemVersioningWithClauseContent(ScriptingPreferences sp)
        {
            // If system versioning isn't supported or isn't set to true on the table, return empty string
            //
            if (!IsSupportedProperty(nameof(IsSystemVersioned), sp) || !GetPropValueOptional(nameof(IsSystemVersioned), false))
            {
                return string.Empty;
            }

            var systemVersioningOptionBuilder = new ScriptStringBuilder("SYSTEM_VERSIONING = ON");

            var historyTableName = GetPropValueOptional(nameof(HistoryTableName), string.Empty);
            var historyTableSchema = GetPropValueOptional(nameof(HistoryTableSchema), string.Empty);

            if (string.IsNullOrEmpty(historyTableName) != string.IsNullOrEmpty(historyTableSchema))
            {
                // Both schema and table name must be provided OR none
                //
                throw new SmoException(ExceptionTemplates.BothHistoryTableNameAndSchemaMustBeProvided);
            }

            // Set history table name option, if provided
            //
            if (!string.IsNullOrEmpty(historyTableName))
            {
                systemVersioningOptionBuilder.SetParameter(
                    "HISTORY_TABLE",
                    string.Format("{0}.{1}", MakeSqlBraket(historyTableSchema), MakeSqlBraket(historyTableName)),
                    ParameterValueFormat.NotString);
            }

            // Append data consistency check
            //
            if (!m_DataConsistencyCheckForSystemVersionedTable)
            {
                systemVersioningOptionBuilder.SetParameter("DATA_CONSISTENCY_CHECK", "OFF", ParameterValueFormat.NotString);
            }

            // Append history retention option
            //
            if (IsSupportedProperty(nameof(HistoryRetentionPeriod)))
            {
                var historyRetentionPeriod = GetPropValueOptional(nameof(HistoryRetentionPeriod), -1);
                var historyRetentionPeriodUnit = GetPropValueOptional(nameof(HistoryRetentionPeriodUnit), TemporalHistoryRetentionPeriodUnit.Undefined);

                if (historyRetentionPeriod > 0 &&
                    historyRetentionPeriodUnit != TemporalHistoryRetentionPeriodUnit.Undefined &&
                    historyRetentionPeriodUnit != TemporalHistoryRetentionPeriodUnit.Infinite)
                {
                    var converter = new TemporalHistoryRetentionPeriodUnitTypeConverter();
                    var unit = converter.ConvertToInvariantString(historyRetentionPeriodUnit);

                    systemVersioningOptionBuilder.SetParameter(
                        "HISTORY_RETENTION_PERIOD",
                        string.Format("{0} {1}", historyRetentionPeriod, unit),
                        ParameterValueFormat.NotString);
                }
            }
            return new StringBuilder().Append(systemVersioningOptionBuilder.ToString(scriptSemiColon: false)).ToString();
        }

        /// <summary>
        /// Script Ledger option clause content for ledger tables (used within WITH statement)
        /// Here's the syntax that gets scripted:
        ///
        /// <table_option>::=
        ///     [ LEDGER = ON [ ( <ledger_option> [,...n ] ) ]
        ///     | OFF 
        ///     ]
        ///
        /// <ledger_option>::= 
        /// {
        ///     [ LEDGER_VIEW = schema_name.ledger_view_name  [ ( <ledger_view_option> [,...n ] ) ]
        ///     [ APPEND_ONLY = ON | OFF ]
        /// }
        /// 
        /// <ledger_view_option>::= 
        /// {
        ///     [ TRANSACTION_ID_COLUMN_NAME = transaction_id_column_name ]
        ///     [ SEQUENCE_NUMBER_COLUMN_NAME = sequence_number_column_name ]
        ///     [ OPERATION_TYPE_COLUMN_NAME = operation_type_id column_name ]
        ///     [ OPERATION_TYPE_DESC_COLUMN_NAME = operation_type_desc_column_name ]
        /// }
        /// 
        /// Examples:
        /// LEDGER = ON
        /// 
        /// LEDGER = ON (APPEND_ONLY = ON)
        /// 
        /// LEDGER = ON (LEDGER_VIEW = [SCHEMA].[VIEW])
        ///
        /// LEDGER = ON (LEDGER_VIEW = [SCHEMA].[VIEW] (TRANSACTION_ID_COLUMN_NAME = [user_defined_name]))
        ///
        /// LEDGER = ON (
        ///     APPEND_ONLY = ON,
        ///     LEDGER_VIEW = [SCHEMA].[VIEW] (
        ///         TRANSACTION_ID_COLUMN_NAME = [user_defined_name],
        ///         SEQUENCE_NUMBER_COLUMN_NAME = [user_defined_name],
        ///         OPERATION_TYPE_COLUMN_NAME = [user_defined_name],
        ///         OPERATION_TYPE_DESC_COLUMN_NAME = [user_defined_name]
        ///     )
        /// )
        /// </summary>
        /// <param name="sp">Scripting Preferences</param>
        private string GenerateLedgerOptionsWithClauseContent(ScriptingPreferences sp)
        {
            // if the table isn't ledger, skip
            if (!GetPropValueOptional(nameof(IsLedger), false))
            {
                return string.Empty;
            }

            var ledgerOptionBuilder = new ScriptStringBuilder("LEDGER = ON");

            // check ledger type
            var ledgerType = GetPropValueOptional(nameof(LedgerType), LedgerTableType.None);
            switch(ledgerType)
            {
                case LedgerTableType.None:
                    throw new SmoException(ExceptionTemplates.LedgerTypeMustBeProvided);
                case LedgerTableType.AppendOnlyLedgerTable:
                    ledgerOptionBuilder.SetParameter("APPEND_ONLY", "ON", ParameterValueFormat.NotString);
                    break;
            }

            var ledgerViewSchema = GetPropValueOptional(nameof(LedgerViewSchema), string.Empty);
            var ledgerViewName = GetPropValueOptional(nameof(LedgerViewName), string.Empty);

            if (string.IsNullOrEmpty(ledgerViewSchema) != string.IsNullOrEmpty(ledgerViewName))
            {
                // Both schema and view name must be provided OR neither
                //
                throw new SmoException(ExceptionTemplates.BothLedgerViewNameAndSchemaMustBeProvided);
            }

            //Get ledger view column name options
            //
            var ledgerViewTransactionIdColumnName = GetPropValueOptional(nameof(LedgerViewTransactionIdColumnName), string.Empty);
            var ledgerViewSequenceNumberColumnName = GetPropValueOptional(nameof(LedgerViewSequenceNumberColumnName), string.Empty);
            var ledgerViewOperationTypeColumnName = GetPropValueOptional(nameof(LedgerViewOperationTypeColumnName), string.Empty);
            var ledgerViewOperationTypeDescColumnName = GetPropValueOptional(nameof(LedgerViewOperationTypeDescColumnName), string.Empty);

            // script ledger view properties, if they are provided
            //
            if (!string.IsNullOrEmpty(ledgerViewName))
            {
                var ledgerViewParameterList = new List<IScriptStringBuilderParameter>();

                if (!string.IsNullOrEmpty(ledgerViewTransactionIdColumnName))
                {
                    ledgerViewParameterList.Add(new ScriptStringBuilderParameter("TRANSACTION_ID_COLUMN_NAME", MakeSqlBraket(ledgerViewTransactionIdColumnName), ParameterValueFormat.NotString));
                }
                if (!string.IsNullOrEmpty(ledgerViewSequenceNumberColumnName))
                {
                    ledgerViewParameterList.Add(new ScriptStringBuilderParameter("SEQUENCE_NUMBER_COLUMN_NAME", MakeSqlBraket(ledgerViewSequenceNumberColumnName), ParameterValueFormat.NotString));
                }
                if (!string.IsNullOrEmpty(ledgerViewOperationTypeColumnName))
                {
                    ledgerViewParameterList.Add(new ScriptStringBuilderParameter("OPERATION_TYPE_COLUMN_NAME", MakeSqlBraket(ledgerViewOperationTypeColumnName), ParameterValueFormat.NotString));
                }
                if (!string.IsNullOrEmpty(ledgerViewOperationTypeDescColumnName))
                {
                    ledgerViewParameterList.Add(new ScriptStringBuilderParameter("OPERATION_TYPE_DESC_COLUMN_NAME", MakeSqlBraket(ledgerViewOperationTypeDescColumnName), ParameterValueFormat.NotString));
                }

                if (ledgerViewParameterList.Any())
                {
                    var ledgerViewClause = $"LEDGER_VIEW = {MakeSqlBraket(ledgerViewSchema)}.{MakeSqlBraket(ledgerViewName)}";
                    ledgerOptionBuilder.SetParameter(ledgerViewClause, ledgerViewParameterList);
                }
                else
                {
                    ledgerOptionBuilder.SetParameter(
                        "LEDGER_VIEW",
                        string.Format("{0}.{1}", MakeSqlBraket(ledgerViewSchema), MakeSqlBraket(ledgerViewName)),
                        ParameterValueFormat.NotString);
                }
            }
            else if (!string.IsNullOrEmpty(ledgerViewTransactionIdColumnName) ||
                     !string.IsNullOrEmpty(ledgerViewSequenceNumberColumnName) ||
                     !string.IsNullOrEmpty(ledgerViewOperationTypeColumnName) ||
                     !string.IsNullOrEmpty(ledgerViewOperationTypeDescColumnName))
            {
                // Ledger view name and view schema haven't been provided, check if ledger view column names are given and
                // throw an error if so
                throw new SmoException(ExceptionTemplates.CannotProvideLedgerViewColumnNamesWithoutLedgerViewNameAndSchema);
            }

            return new StringBuilder().Append(ledgerOptionBuilder.ToString(scriptSemiColon: false)).ToString();
        }

        /// <summary>
        /// Generates WITH clause related to Heap on Stretch dbs option
        /// Something like this: WITH HEAP
        /// </summary>
        /// <param name="sb">StringBuilder to append result to</param>
        /// <param name="sp">scripting preferences</param>
        /// <returns></returns>
        private void GenerateStretchHeapWithClause(StringBuilder sb, ScriptingPreferences sp)
        {
            if (sp.TargetEngineIsAzureStretchDb())
            {
                // Stretch db backends need to specify HEAP to avoid default clustered column store which
                // does not support NC indexes
                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}WITH (HEAP)", sp.NewLine);
            }
        }

        private bool FetchFileTableIndexNames(out string pkIndex, out string stream_idIndex, out string fullpathIndex, bool systemNameAllowed)
        {
            pkIndex = stream_idIndex = fullpathIndex = string.Empty;

            foreach (Index idx in this.Indexes)
            {
                // Clustered columnstore indexes do not have indexed columns
                if (VerifyIndexType(idx, IndexType.ClusteredColumnStoreIndex))
                {
                    continue;
                }

                string firstColumnName = idx.IndexedColumns[0].Name;
                if (string.Compare(firstColumnName, "path_locator", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (systemNameAllowed || (!idx.IsSystemNamed))
                    {
                        pkIndex = idx.Name;
                    }
                }
                else if (string.Compare(firstColumnName, "stream_id", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (systemNameAllowed || (!idx.IsSystemNamed))
                    {
                        stream_idIndex = idx.Name;
                    }
                }
                else if (string.Compare(firstColumnName, "parent_path_locator", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (systemNameAllowed || (!idx.IsSystemNamed))
                    {
                        fullpathIndex = idx.Name;
                    }
                }
            }

            //We may have situation where only one index name need to be scripted. e.g Two are systemnamed while third is userNamed
            if (string.IsNullOrEmpty(pkIndex) && string.IsNullOrEmpty(stream_idIndex) && string.IsNullOrEmpty(fullpathIndex))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// Append the CREATE TABLE TEXTIMAGE_ON script
        /// </summary>
        private void GenerateTextFileGroupScript(StringBuilder sb, ScriptingPreferences sp)
        {
            bool isPartitioned = false;
            if (IsSupportedProperty("IsPartitioned", sp))
            {
                isPartitioned = GetPropValueOptional("IsPartitioned", false);
            }

            if (HasTextimageColumn(sp) &&
                !isPartitioned &&
                sp.Storage.FileGroup &&
                null != Properties.Get("TextFileGroup").Value)
            {
                string textFileGroup = (string)Properties["TextFileGroup"].Value;
                if (0 < textFileGroup.Length)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " TEXTIMAGE_ON [{0}]", SqlBraket(textFileGroup));
                }
            }
        }

        /// <summary>
        /// This method creates the AS NODE/AS EDGE syntax if the table is a node or an edge.
        /// </summary>
        /// <param name="sb">String builder that is building the script.</param>
        /// <param name="sp">Preferences for scripting.</param>
        private void GenerateGraphScript(StringBuilder sb, ScriptingPreferences sp)
        {
            if (sp.TargetEngineIsAzureSqlDw() || (sp.TargetServerVersionInternal < SqlServerVersionInternal.Version140 && sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.Standalone))
            {
                return;
            }
            bool isNodeTable = this.GetPropValueOptional("IsNode", false);
            bool isEdgeTable = this.GetPropValueOptional("IsEdge", false);

            if (isNodeTable)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.AS_NODE);
            }

            if (isEdgeTable)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.AS_EDGE);
            }
        }

        /// <summary>
        /// performs actions needed after the object is created
        /// </summary>
        protected override void PostCreate()
        {
            // lock down the PartitionSchemeParameters collection
            if (null != m_PartitionSchemeParameters)
            {
                m_PartitionSchemeParameters.LockCollection(ExceptionTemplates.ReasonObjectAlreadyCreated(UrnSuffix));
            }

            // if there was a system-time temporal period created as well, reset the structure (do it anyways in fact)
            m_systemTimePeriodInfo.Reset();
            m_systemTimePeriodInfo.MarkForDrop(false);

            // Graph tables create columns that are not part of the column collection
            // so this column collection needs to be refreshed after the table is created.
            //
            bool isNodeTable = GetPropValueIfSupported("IsNode", false);
            bool isEdgeTable = GetPropValueIfSupported("IsEdge", false);

            if (isNodeTable || isEdgeTable)
            {
                Columns.Clear();
                Columns.Refresh();
            }
        }

        /// <summary>
        /// Processes optional properties for the external table
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="script">The external table T-SQL script.</param>
        /// <param name="sp">Scripting preferences.</param>
        private void ProcessExternalTableOptionalProperties(StringBuilder script, ScriptingPreferences sp)
        {
            // Polybase properties.
            string LocationPropertyName =  nameof(Location);
            string FileFormatNamePropertyName = nameof(FileFormatName);
            if (DatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand && sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlOnDemand)
            {
                LocationPropertyName = "LocationOd";
                FileFormatNamePropertyName = "FileFormatNameOd";
            }
            const string RejectTypePropertyName = nameof(RejectType);
            const string RejectValuePropertyName = nameof(RejectValue);
            const string RejectSampleValuePropertyName = nameof(RejectSampleValue);
            const string RejectedRowLocationPropertyName = nameof(RejectedRowLocation);

            // GQ Properties.
            const string DistributionPropertyName = nameof(ExternalTableDistribution);
            const string ShardingColumnPropertyName = nameof(ShardingColumnName);
            const string RemoteSchemaPropertyName = nameof(RemoteSchemaName);
            const string RemoteObjectPropertyName = nameof(RemoteObjectName);

            // Validate and process the location property.
            this.ValidateLocationProperties(LocationPropertyName, "LOCATION = {0}", sp, script);

            // Validate and process the file format property.
            this.ValidateOptionalProperty<string>(FileFormatNamePropertyName, "FILE_FORMAT = {0}", new List<string>(), script, sp, true);
            if (IsSupportedProperty(RejectTypePropertyName))
            {
                Property rejectTypeProp = this.GetPropertyOptional(RejectTypePropertyName);
                ExternalTableRejectType rejectType = rejectTypeProp.IsNull ? ExternalTableRejectType.None : (ExternalTableRejectType)rejectTypeProp.Value;

                if (rejectType != ExternalTableRejectType.None)
                {
                    // Check if the reject type property is supported and has been set
                    // if it has been, add it to the T-SQL script.
                    this.ValidateOptionalProperty<ExternalTableRejectType>(RejectTypePropertyName, "REJECT_TYPE = {0}", new List<ExternalTableRejectType>(), script, sp, typeConverter: SmoManagementUtil.GetTypeConverter(typeof(ExternalTableRejectType)));

                    // Validate and process the reject value property.
                    this.ValidateOptionalProperty<double>(RejectValuePropertyName, "REJECT_VALUE = {0}", new List<double>(), script, sp);

                    // Validate and process the reject sample value property.
                    this.ValidateOptionalProperty<double>(RejectSampleValuePropertyName, "REJECT_SAMPLE_VALUE = {0}", new List<double> { -1 }, script, sp);

                    // Validate and process the Rejected Row Location property.
                    this.ValidateLocationProperties(RejectedRowLocationPropertyName, "REJECTED_ROW_LOCATION = {0}", sp, script);
                }
            }
            // Validate and process the distribution property.
            if (this.IsSupportedProperty(DistributionPropertyName))
            {
                Property distributionProperty = this.GetPropertyOptional(DistributionPropertyName);
                TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(ExternalTableDistributionType));

                if (!distributionProperty.IsNull)
                {
                    ExternalTableDistributionType distribution = (ExternalTableDistributionType)distributionProperty.Value;

                    switch (distribution)
                    {
                        case ExternalTableDistributionType.Sharded:
                            Property shardingColNameProperty = this.GetPropertyOptional(ShardingColumnPropertyName);
                            Diagnostics.TraceHelper.Assert(!shardingColNameProperty.IsNull);
                            string distributionWithShardingColName = string.Format("{0}({1})",
                                typeConverter.ConvertToInvariantString(distribution),
                                MakeSqlBraket(Convert.ToString(shardingColNameProperty.Value, SmoApplication.DefaultCulture)));
                            this.AddPropertyToScript(distributionWithShardingColName, "DISTRIBUTION = {0}", script);
                            break;
                        case ExternalTableDistributionType.Replicated:
                        case ExternalTableDistributionType.RoundRobin:
                            this.AddPropertyToScript(typeConverter.ConvertToInvariantString(distribution), "DISTRIBUTION = {0}", script);
                            break;
                        default:
                            Diagnostics.TraceHelper.Assert(distribution == ExternalTableDistributionType.None);
                            break;
                    }
                }
            }

            // Validate and process the remote schema name property.
            if (this.IsSupportedProperty(RemoteSchemaPropertyName, sp))
            {
                Property remoteSchemaProperty = this.GetPropertyOptional(RemoteSchemaPropertyName);
                if (!remoteSchemaProperty.IsNull && !string.IsNullOrEmpty(remoteSchemaProperty.Value.ToString()))
                {
                    this.AddPropertyToScript(Util.MakeSqlString(remoteSchemaProperty.Value.ToString()), "SCHEMA_NAME = {0}", script);
                }
            }

            // Validate and process the remote object name property.
            if (this.IsSupportedProperty(RemoteObjectPropertyName, sp))
            {
                Property remoteObjectProperty = this.GetPropertyOptional(RemoteObjectPropertyName);
                if (!remoteObjectProperty.IsNull && !string.IsNullOrEmpty(remoteObjectProperty.Value.ToString()))
                {
                    this.AddPropertyToScript(Util.MakeSqlString(remoteObjectProperty.Value.ToString()), "OBJECT_NAME = {0}", script);
                }
            }
        }

        /// <summary>
        /// Processes location properties for the external table
        /// Validates that the location properties are not null or empty
        /// and adds them to the T-SQL script.
        /// These are string properties and Util.MakeSqlString should be used before adding them to the script.
        /// </summary>
        /// <param name="locationPropertyName">The property name.</param>
        /// <param name="sqlString">The T-SQL script to add.</param>
        /// <param name="sp">Scripting preferences.</param>
        /// <param name="script">The external table T-SQL script.</param>
        private void ValidateLocationProperties(string locationPropertyName, string sqlString, ScriptingPreferences sp, StringBuilder script)
        {
            // Validate and process the location properties.
            if (this.IsSupportedProperty(locationPropertyName, sp))
            {
                Property locationProperty = this.GetPropertyOptional(locationPropertyName);
                if (!locationProperty.IsNull && !string.IsNullOrEmpty(locationProperty.Value.ToString()))
                {
                    this.AddPropertyToScript(Util.MakeSqlString(locationProperty.Value.ToString()), sqlString, script);
                }
            }
        }

        /// <summary>
        /// Processes SQL DW table properties
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="script">The SQL DW table T-SQL script.</param>
        /// <param name="sp">Scripting preferences.</param>
        private void ProcessSqlDwTableProperties(StringBuilder script, ScriptingPreferences sp)
        {
            // SQL DW table properties
            const string DwTableDistributionPropertyName = "DwTableDistribution";
            const string DistributionColumnNamePropertyName = "DistributionColumnName";
            const string IsDistributedColumnPropertyName = "IsDistributedColumn";
            const string HasClusteredColumnStoreIndex = "HasClusteredColumnStoreIndex";
            const string HasClusteredIndex = "HasClusteredIndex";
            const string HasHeapIndex = "HasHeapIndex";
            const string IsPartitioned = "IsPartitioned";
            const string PartitionScheme = "PartitionScheme";
            const string ColumnStoreOrderOrdinal = "ColumnStoreOrderOrdinal";
            const string RightBoundaryValue = "RightBoundaryValue";

            // process the distribution property
            if (this.IsSupportedProperty(DwTableDistributionPropertyName))
            {
                Property distributionProperty = this.GetPropertyOptional(DwTableDistributionPropertyName);
                TypeConverter typeConverter = SmoManagementUtil.GetTypeConverter(typeof(DwTableDistributionType));

                // script the specified distribution property value
                if (!distributionProperty.IsNull)
                {
                    DwTableDistributionType distribution = (DwTableDistributionType)distributionProperty.Value;

                    switch (distribution)
                    {
                        case DwTableDistributionType.Hash:

                            // get the distribution column names
                            var distributionColumnNames = string.Join(",", Columns.Cast<Column>()
                                .Where(col => col.GetPropValueOptional(IsDistributedColumnPropertyName, false))
                                .Select(col => MakeSqlBraket(col.GetPropValueOptional(DistributionColumnNamePropertyName, string.Empty))));

                            var distributionWithDistributionColName = $"{typeConverter.ConvertToInvariantString(distribution)} ( {distributionColumnNames} )";

                            this.AddPropertyToScript(distributionWithDistributionColName, "DISTRIBUTION = {0}", script);
                            break;
                        case DwTableDistributionType.Replicate:
                        case DwTableDistributionType.RoundRobin:
                            this.AddPropertyToScript(typeConverter.ConvertToInvariantString(distribution), "DISTRIBUTION = {0}", script);
                            break;
                        default:
                            Diagnostics.TraceHelper.Assert(distribution == DwTableDistributionType.None);
                            break;
                    }
                }
                else if (distributionProperty.IsNull && this.State == SqlSmoState.Creating) //if the distribution property is not specified, script the default distribution policy of round_robin
                {
                    this.AddPropertyToScript(typeConverter.ConvertToInvariantString(DwTableDistributionType.RoundRobin), "DISTRIBUTION = {0}", script);
                }

                // add a tab to format the string as an inline statement
                script.Insert(0, Globals.tab);
            }

            // process index properties
            // check for supported index properties for SQL DW tables
            if (this.IsSupportedProperty(HasClusteredColumnStoreIndex) &&
                this.IsSupportedProperty(HasClusteredIndex) &&
                this.IsSupportedProperty(HasHeapIndex))
            {
                Property hasClusteredColumnStoreIndex = this.GetPropertyOptional(HasClusteredColumnStoreIndex);
                Property hasClusteredIndex = this.GetPropertyOptional(HasClusteredIndex);
                Property hasHeapIndex = this.GetPropertyOptional(HasHeapIndex);

                TypeConverter indexTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(IndexType));

                // if the table object has clustered column store index, clustered index or heap index, script it out
                // if a table object does not have any indexes (new table object), the default index type is the clustered column store
                if (!hasClusteredColumnStoreIndex.IsNull ||
                    !hasClusteredIndex.IsNull ||
                    !hasHeapIndex.IsNull)
                {

                    if (!hasClusteredIndex.IsNull && (bool)hasClusteredIndex.Value)
                    {
                        // for clustered columnstore indexes, there is no index name to be scripted
                        // for clustered indexes, script out the index name and the list of indexed columns
                        if (!hasClusteredColumnStoreIndex.IsNull && (bool)hasClusteredColumnStoreIndex.Value)
                        {
                            // Collecting only Clustered Columnstore Indexes to be inlined as a part of CREATE TABLE statement
                            var clusteredColumnstoreIndexes =
                               (from Index idx in this.Indexes
                                where VerifyIndexType(idx, IndexType.ClusteredColumnStoreIndex)
                                select idx).ToList();

                            List<IndexedColumn> orderedCols =
                                (from IndexedColumn col in clusteredColumnstoreIndexes[0].IndexedColumns
                                 where col.IsSupportedProperty(ColumnStoreOrderOrdinal) && col.GetPropValueOptional(ColumnStoreOrderOrdinal, 0) > 0
                                 select col).ToList();

                            if (orderedCols.Count == 0)
                            {
                                this.AddPropertyToScript(indexTypeConverter.ConvertToInvariantString(IndexType.ClusteredColumnStoreIndex), "{0}", script, true);
                            }
                            else
                            {
                                ScriptSqlDwOrderedClusteredColumnstoreIndexes(indexTypeConverter.ConvertToInvariantString(IndexType.ClusteredColumnStoreIndex), script, orderedCols);
                            }
                        }
                        else
                        {
                            if (script.Length > 0)
                            {
                                script.Append(Globals.comma);
                            }

                            // Collecting only Clustered Indexes to be inlined as a part of CREATE TABLE statement
                            var clusteredIndexes =
                               (from Index idx in this.Indexes
                                where VerifyIndexType(idx, IndexType.ClusteredIndex)
                                select idx).ToList();
                            ScriptSqlDwClusteredIndexes(script, sp, clusteredIndexes);
                        }
                    }
                    else if (!hasHeapIndex.IsNull && (bool)hasHeapIndex.Value)
                    {
                        this.AddPropertyToScript(indexTypeConverter.ConvertToInvariantString(IndexType.HeapIndex), "{0}", script, true);
                    }
                }
                else if (hasClusteredColumnStoreIndex.IsNull && hasClusteredIndex.IsNull && hasHeapIndex.IsNull && this.State == SqlSmoState.Creating)
                {
                    this.AddPropertyToScript(indexTypeConverter.ConvertToInvariantString(IndexType.ClusteredColumnStoreIndex), "{0}", script, true);
                }
            }

            // process partition properties
            // check for supported partition properties
            bool isPartitioned = false;
            if (IsSupportedProperty(IsPartitioned, sp))
            {
                isPartitioned = GetPropValueOptional(IsPartitioned, false);

                if (isPartitioned && this.PartitionSchemeParameters != null)
                {
                    // add a tab to format the string as an inline statement
                    script.Append(Globals.comma);
                    script.Append(Globals.newline);
                    script.Append(Globals.tab);

                    script.AppendFormat(SmoApplication.DefaultCulture, "PARTITION");
                    script.AppendFormat(Globals.newline);
                    script.AppendFormat(Globals.tab);
                    script.AppendFormat(Globals.LParen);

                    int columnCount = 0;
                    foreach (PartitionSchemeParameter partitionSchemeParameter in this.PartitionSchemeParameters)
                    {
                        // we check to see if the specified column exists
                        string partitionSchemeParameterName = partitionSchemeParameter.Name;
                        Column column = this.Columns[partitionSchemeParameterName];
                        if (null == column)
                        {
                            // if we are in creating state, the columns may not have been populated, so skip this check
                            if (this.ParentColl.ParentInstance.State != SqlSmoState.Creating)
                            {
                                string partitionSchemeName = GetPropValueOptional(PartitionScheme, string.Empty);

                                // the column does not exist, so we need to abort this scripting
                                throw new SmoException(ExceptionTemplates.ObjectRefsNonexCol(Table.UrnSuffix, partitionSchemeName, this.ToString() + "." + MakeSqlBraket(partitionSchemeParameterName)));
                            }
                        }

                        if (columnCount > 0)
                        {
                            script.Append(Globals.comma);
                        }

                        script.Append(Globals.newline);
                        script.Append(Globals.tab);
                        script.Append(Globals.tab);
                        script.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(column.Name));

                        columnCount++;
                    }

                    script.Append(Globals.space);
                    script.AppendFormat(SmoApplication.DefaultCulture, "{0}", "RANGE");
                    script.Append(Globals.space);

                    // add partition range type
                    if (this.PhysicalPartitions != null && this.PhysicalPartitions.Count > 0)
                    {
                        int partitionCount = 0;

                        // only set the range type once, as all partitions will have the same range type value
                        // it is valid to create a table with no boundary values specified
                        // such table will have a single partition (essentially a non-partitioned table)
                        RangeType rangeType = (RangeType)this.PhysicalPartitions[partitionCount].RangeType;

                        if (rangeType != RangeType.None)
                        {
                            TypeConverter rangeTypeConverter = SmoManagementUtil.GetTypeConverter(typeof(RangeType));
                            script.AppendFormat(SmoApplication.DefaultCulture, "{0} ", rangeType == RangeType.Left ? rangeTypeConverter.ConvertToInvariantString(RangeType.Left) : rangeTypeConverter.ConvertToInvariantString(RangeType.Right));
                        }

                        script.AppendFormat(SmoApplication.DefaultCulture, "{0} ", "FOR VALUES");
                        script.Append(Globals.LParen);

                        foreach (PhysicalPartition physicalPartition in this.PhysicalPartitions)
                        {
                            // get and script the boundary value for each partition
                            // the last partition will have the RightBoundaryValue of null
                            object rightBoundaryValue = physicalPartition.GetPropValueOptionalAllowNull(RightBoundaryValue);
                            if (rightBoundaryValue != null)
                            {
                                if (partitionCount > 0)
                                {
                                    script.Append(Globals.commaspace);
                                }

                                script.AppendFormat(SmoApplication.DefaultCulture, FormatSqlVariant(rightBoundaryValue));
                            }

                            partitionCount++;
                        }

                        script.Append(Globals.RParen);
                        script.Append(Globals.newline);
                        script.Append(Globals.tab);
                        script.Append(Globals.RParen);
                    }
                }
            }
        }

        /// <summary>
        /// Validates the specified property that it is not the default value and adds it to the T-SQL script.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="sqlString">The T-SQL script to add.</param>
        /// <param name="defaultValues">The default property values.</param>
        /// <param name="fileFormatOptions">The T-SQL script with the already added file format options.</param>
        /// <param name="sp">Scripting preferences.</param>
        /// <param name="bracketize">A flag to add a bracket around the property name.</param>
        /// <param name="typeConverter">The type converter object.</param>
        private void ValidateOptionalProperty<T>(string propertyName, string sqlString, List<T> defaultValues, StringBuilder fileFormatOptions, ScriptingPreferences sp, bool bracketize = false, TypeConverter typeConverter = null, bool isNullable = false)
        {
            // check if the property has been modified
            // if it has been and if the value is not the default, add it to the T-SQL script
            if (IsSupportedProperty(propertyName, sp))
            {
                Property prop = this.GetPropertyOptional(propertyName);

                if (!prop.IsNull && (!this.IsPropertyDefaultValue(prop, (T)prop.Value, defaultValues)))
                {
                    string propValue = string.Empty;

                    // for enumeration values, convert to an invariant string
                    if (typeConverter == null)
                    {
                        propValue = Convert.ToString(prop.Value, SmoApplication.DefaultCulture);
                    }
                    else
                    {
                        propValue = typeConverter.ConvertToInvariantString(prop.Value);
                    }


                    if(string.Empty == propValue)
                    {
                        return;
                    }

                    // add a bracket for identifiers
                    if (bracketize)
                    {
                        propValue = MakeSqlBraket(propValue);
                    }

                    this.AddPropertyToScript(propValue, sqlString, fileFormatOptions);
                }
            }
        }

        /// <summary>
        /// Adds a property to the specified T-SQL script.
        /// </summary>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="sqlString">The formated T-SQL string to insert the property value.</param>
        /// <param name="script">The T-SQL script to add the formated property value.</param>
        /// <param name="formatted">Adds newline and tab to the script.</param>
        private void AddPropertyToScript(string propertyValue, string sqlString, StringBuilder script, bool formatted = false)
        {
            // if this is the first property value being added, the string builder length will be 0, so don't prepend a comma
            // for all consecutive properties, we need to prepend a comma
            if (script.Length > 0)
            {
                script.Append(Globals.comma);

                if (formatted)
                {
                    script.Append(Globals.newline);
                    script.Append(Globals.tab);
                }
            }

            script.AppendFormat(SmoApplication.DefaultCulture, sqlString, propertyValue);
        }

        /// <summary>
        /// Check the specified property if it has the default value.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="prop">The property to check.</param>
        /// <param name="value">The property value to check.</param>
        /// <param name="defaultValues">The default property values.</param>
        /// <returns>True, if the property value has the default value.  False otherwise.</returns>
        private bool IsPropertyDefaultValue<T>(Property prop, T value, List<T> defaultValues)
        {
            // if the value is the default, return true
            // otherwise, return false
            if (!prop.IsNull)
            {
                foreach (T defaultValue in defaultValues)
                {
                    if (EqualityComparer<T>.Default.Equals((T)prop.Value, defaultValue))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the ANSI_PADDING with which we should create the table
        /// </summary>
        /// <returns></returns>
        private Nullable<bool> GetTableAnsiPadded()
        {
            // ANSI_PADDING must be on when creating memory optimized tables
            if (this.CheckIsMemoryOptimizedTable())
            {
                return true;
            }

            foreach (Column c in this.Columns)
            {
                Nullable<bool> colSetting = GetColumnPadding(c);
                if (colSetting.HasValue)
                {
                    return colSetting;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the ansi_padding setting for this column, or null in case
        /// the column does not have such a thing
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private Nullable<bool> GetColumnPadding(Column c)
        {
            if (IsPaddingType(c))
            {
                // if the type can have this setting try to get it from the property bag
                return c.GetPropValueOptional<bool>("AnsiPaddingStatus");
            }
            else if (c.GetPropValueOptional("Computed", false) &&
                            this.ServerVersion.Major >= 9 &&
                            c.GetPropValueOptional("IsPersisted", false))
            {
                // persisted columns always need ansi_padding set to true
                return true;
            }

            return null;
        }

        /// <summary>
        /// Returns true if the table has columns that need the TEXTIMAGE clause
        /// which means it has data type text, ntext, or image
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        internal bool HasTextimageColumn(ScriptingPreferences sp)
        {
            foreach (Column column in Columns)
            {
                // if the column is ignored while scripting then don't check its type
                if (sp.ScriptForCreateDrop || !column.IgnoreForScripting)
                {
                    // check if the column data type is one of text, ntext, varchar max, varbinary max, nvarchar max, xml,geometry,geography and image
                    // TEXT Filegroup is supported on large CLR type columns as well.
                    //If the user creates a UDT with int as internal then it will result in an engine error on creating a TEXT Filegroup
                    // The same holds true for UDDT
                    if (column.DataType.SqlDataType == SqlDataType.NText ||
                        column.DataType.SqlDataType == SqlDataType.Text ||
                        column.DataType.SqlDataType == SqlDataType.Image ||
                        column.DataType.SqlDataType == SqlDataType.VarCharMax ||
                        column.DataType.SqlDataType == SqlDataType.NVarCharMax ||
                        column.DataType.SqlDataType == SqlDataType.VarBinaryMax ||
                        column.DataType.SqlDataType == SqlDataType.Xml ||
                        column.DataType.SqlDataType == SqlDataType.Geometry ||
                        column.DataType.SqlDataType == SqlDataType.Geography ||
                        column.DataType.SqlDataType == SqlDataType.UserDefinedType ||
                        column.DataType.SqlDataType == SqlDataType.UserDefinedDataType)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// returns true if the columns in the table have multiple ansi_padding settings
        /// </summary>
        /// <returns></returns>
        internal bool HasMultiplePaddings()
        {
            Nullable<bool> currentPadding = null;
            foreach (Column column in Columns)
            {
                if (IsPaddingType(column))
                {
                    if (!currentPadding.HasValue)
                    {
                        // if padding has not been set then initialize it
                        // if the property does not have any value default to the original padding
                        currentPadding = column.GetPropValueOptional<bool>("AnsiPaddingStatus");
                    }
                    else
                    {
                        // if the current padding setting is different then we return true
                        // If the property is not set GetPropValueOptional will return the existing
                        // setting as a default
                        Nullable<bool> colPadding = column.GetPropValueOptional<bool>("AnsiPaddingStatus");
                        if (colPadding.HasValue && (currentPadding != colPadding))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        /// <summary>
        /// Returns true is the type of this column can have ansi_padding setting
        /// </summary>
        /// <param name="sqlDataType"></param>
        /// <returns></returns>
        internal bool IsPaddingType(Column col)
        {
            SqlDataType sqlDataType = col.DataType.SqlDataType;
            switch (sqlDataType)
            {
                case SqlDataType.Char:
                case SqlDataType.VarChar:
                case SqlDataType.VarCharMax:
                case SqlDataType.Binary:
                case SqlDataType.VarBinary:
                case SqlDataType.VarBinaryMax:

                    return true;

                case SqlDataType.UserDefinedDataType:
                    // for UDDT we need to look at the base type
                    switch (col.GetPropValueOptional("SystemType", string.Empty))
                    {
                        case "char":
                        case "varchar":
                        case "binary":
                        case "varbinary":
                            return true;
                    }
                    break;


            }

            return false;
        }

        internal static void ScriptTableInternal(ScriptingPreferences sp, StringBuilder sb, ColumnCollection columns, ICollection indexes, bool isEdgeTable = false)
        {
            Diagnostics.TraceHelper.Assert(null != sp);
            Diagnostics.TraceHelper.Assert(null != sb);
            Diagnostics.TraceHelper.Assert(null != columns);

            ScriptColumns(sp, sb, columns, isEdgeTable: isEdgeTable);

            GeneratePkUkInCreateTable(sb, sp, indexes, true);
        }

        private static void ScriptColumns(ScriptingPreferences sp, StringBuilder sb, ColumnCollection columns, bool isEdgeTable = false)
        {
            StringCollection col_strings = new StringCollection();

            // Omit an opening parenthesis if this table is not an edge
            // table, or the table has user defined columns.
            //
            if (ShouldEmitColumnListParenthesis(isEdgeTable, columns))
            {
                sb.Append(Globals.LParen);
                sb.Append(sp.NewLine);
            }

            // script all table columns
            bool fFirstColumn = true;
            foreach (Column column in columns)
            {
                // Internal graph columns do not need to be scripted, they will be
                // automatically created on behalf of the user.
                //
                if (column.IsGraphInternalColumn() || column.IsGraphComputedColumn())
                {
                    continue;
                }

                // Do not script dropped ledger columns.
                // These columns are hidden and "dropped", and should not be displayed.
                if (column.DroppedLedgerColumn())
                {
                    continue;
                }

                // prepare to script column only if in direct execution mode or not explicitly directed to ignore it
                if (sp.ScriptForCreateDrop || !column.IgnoreForScripting)
                {
                    column.ScriptDdlInternal(col_strings, sp);

                    if (fFirstColumn)
                    {
                        fFirstColumn = false;
                    }
                    else
                    {
                        sb.Append(Globals.comma);
                        sb.Append(sp.NewLine);
                    }
                    sb.Append(Globals.tab);
                    sb.Append(col_strings[0]);

                    col_strings.Clear();
                }
            }
        }

        /// <summary>
        /// This method determines if a parenthesis should be emitted when working with an edge table.
        /// The parenthesis should be emitted if the edge contains any non-graph type columns.
        /// </summary>
        /// <param name="isEdgeTable">True to indicate the table is an edge table, False otherwise.</param>
        /// <param name="columns">The list of columns.</param>
        /// <returns>True if a parenthesis should be emitted.</returns>
        private static bool ShouldEmitColumnListParenthesis(bool isEdgeTable, ColumnCollection columns)
        {
            if (!isEdgeTable)
            {
                return true;
            }

            // Only emit a parenthesis if there are user defined columns in the column list.
            //
            foreach (Column col in columns)
            {
                if (col.GetPropValueOptional("GraphType", GraphType.None) == GraphType.None)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// scripts indexes of a memory optimized table
        /// </summary>
        /// <param name="sb"> the string builder to hold scripts </param>
        /// <param name="sp"> scripting preferences </param>
        /// <param name="indexes"> indexes to script </param>
        private static void GenerateMemoryOptimizedIndexes(StringBuilder sb, ScriptingPreferences sp, ICollection indexes)
        {
            if (indexes == null)
            {
                return;
            }

            // imtermediate buffer to collect scripts
            StringCollection col_strings = new StringCollection();

            bool firstIndex = true;

            // script all non-primary indexes
            foreach (Index idx in indexes)
            {
                Diagnostics.TraceHelper.Assert(idx.IsMemoryOptimizedIndex);

                if (firstIndex)
                {
                    firstIndex = false;
                }
                else
                {
                    sb.Append(Globals.comma);
                }

                // script unique key only if in direct execution mode or not explicitly directed to ignore it
                if (sp.ScriptForCreateDrop || !idx.IgnoreForScripting)
                {
                    idx.ScriptDdl(col_strings, sp, true, true);
                    Diagnostics.TraceHelper.Assert(col_strings.Count != 0);

                    sb.Append(sp.NewLine);
                    sb.Append(col_strings[0]);
                    col_strings.Clear();
                }
            }
        }

        /// <summary>
        /// Scripts  clustered indexes for a SQL DW table that are inlined in the CREATE TABLE DDL.
        /// </summary>
        /// <param name="sb">The string builder to hold scripts.</param>
        /// <param name="sp">The scripting preferences.</param>
        /// <param name="indexes">The indexes to script.</param>
        private static void ScriptSqlDwClusteredIndexes(StringBuilder sb, ScriptingPreferences sp, ICollection indexes)
        {
            if (indexes == null)
            {
                return;
            }

            // imtermediate buffer to collect index scripts
            StringCollection scripts = new StringCollection();

            bool firstIndex = true;

            // script all clustered indexes
            foreach (Index idx in indexes)
            {
                Diagnostics.TraceHelper.Assert(idx.IndexType == IndexType.ClusteredIndex);

                if (firstIndex)
                {
                    firstIndex = false;
                }
                else
                {
                    sb.Append(Globals.comma);
                }

                // script unique key only if in direct execution mode or not explicitly directed to ignore it
                if (sp.ScriptForCreateDrop || !idx.IgnoreForScripting)
                {
                    idx.ScriptDdl(scripts, sp, true, true);
                    Diagnostics.TraceHelper.Assert(scripts.Count == 1);
                    sb.Append(sp.NewLine);
                    sb.Append(scripts[0]);
                    scripts.Clear();
                }
            }
        }

        /// <summary>
        /// Scripts ordered clustered indexes for a SQL DW table that are inlined in the CREATE TABLE DDL.
        /// </summary>
        /// <param name="indexTypeName"> TndexType. It's Columnstored Clustered Index in this function call.</param>
        /// <param name="sb">The string builder to hold scripts.</param>
        /// <param name="indexes">The indexed columns to script.</param>
        private static void ScriptSqlDwOrderedClusteredColumnstoreIndexes(string indexTypeName, StringBuilder script, List<IndexedColumn> indexedColumns)
        {
            if (script.Length > 0)
            {
                script.Append(Globals.comma);
                script.Append(Globals.newline);
                script.Append(Globals.tab);
            }

            List<IndexedColumn> orderedColumns = indexedColumns.OrderBy(x => x.GetPropValueOptional("ColumnStoreOrderOrdinal", 0)).ToList();
            string listOfCols = string.Join(",", orderedColumns.Select(x => MakeSqlBraket(x.Name)).ToArray());

            script.AppendFormat(SmoApplication.DefaultCulture,
                "{0} ORDER ({1})",
                indexTypeName,
                listOfCols);
        }

        /// <summary>
        /// Generates scripts for the primary key and unique keys present on the table
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="so"></param>
        /// <param name="indexes"></param>
        /// <param name="embedded">indicates if we will script separate statements
        /// to create those objects</param>
        private static void GeneratePkUkInCreateTable(StringBuilder sb, ScriptingPreferences sp, ICollection indexes, bool embedded)
        {
            if (indexes == null)
            {
                return;
            }

            // imtermediate buffer to collect scripts
            StringCollection col_strings = new StringCollection();

            // script a primary key
            foreach (Index idx in indexes)
            {
                // Columnstore Indexes do not have an IndexKeyType
                if (IsColumnstoreIndex(idx))
                {
                    continue;
                }

                if (IndexKeyType.DriPrimaryKey == (IndexKeyType)idx.GetPropValue("IndexKeyType")) // primary key
                {
                    // script primary key only if in direct execution mode or not explicitly directed to ignore it
                    if (sp.ScriptForCreateDrop || !idx.IgnoreForScripting)
                    {
                        // The last argument was added to tell we're in a create table statement
                        // as the second parameter has been abused to generate partial DDL
                        // to inject into the CREATE TABLE
                        idx.ScriptDdl(col_strings, sp, !embedded, true);
                        Diagnostics.TraceHelper.Assert(col_strings.Count != 0);

                        if (embedded)
                        {
                            sb.Append(Globals.comma);
                        }
                        sb.Append(sp.NewLine);
                        sb.Append(col_strings[0]);
                        col_strings.Clear();
                    }

                    break; // there can be only one primary key
                }
            }


            // script all unique keys
            foreach (Index idx in indexes)
            {
                // Columnstore Indexes do not have an IndexKeyType
                if (IsColumnstoreIndex(idx))
                {
                    continue;
                }

                if (IndexKeyType.DriUniqueKey == idx.IndexKeyType) // unique key
                {
                    // if our target version is 2000, and this unique key has Ignore Duplicate Keys, we need to script it out
                    // later as an index instead
                    if (sp.TargetServerVersion == SqlServerVersion.Version80 && idx.IgnoreDuplicateKeys)
                    {
                        continue;
                    }


                    // script unique key only if in direct execution mode or not explicitly directed to ignore it
                    if (sp.ScriptForCreateDrop || !idx.IgnoreForScripting)
                    {
                        // The last argument was added to tell we're in a create table statement
                        // as the second parameter has been abused to generate partial DDL
                        // to inject into the CREATE TABLE
                        idx.ScriptDdl(col_strings, sp, !embedded, true);
                        Diagnostics.TraceHelper.Assert(col_strings.Count != 0);

                        if (embedded)
                        {
                            sb.Append(Globals.comma);
                        }
                        sb.Append(sp.NewLine);
                        sb.Append(col_strings[0]);
                        col_strings.Clear();
                    }

                    // keep going there may be more unique indexes on the table
                }
            }
        }

        private static bool IsColumnstoreIndex(Index index)
        {
            return (VerifyIndexType(index, IndexType.ClusteredColumnStoreIndex) || VerifyIndexType(index, IndexType.NonClusteredColumnStoreIndex));
        }

        private static bool VerifyIndexType(Index index, IndexType expectedIndexType)
        {
            return (index.GetPropValueOptional<IndexType>("IndexType").HasValue
                   && expectedIndexType.Equals(index.GetPropValueOptional<IndexType>("IndexType").Value));
        }

        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);
            CheckObjectState();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // Dropped ledger tables and ledger history tables can't be dropped
            if (IsSupportedProperty(nameof(LedgerType)) && (LedgerType == LedgerTableType.HistoryTable || IsDroppedLedgerTable))
            {
                return;
            }
            // Script DROP SYSTEM VERSIONING prior to dropping a temporal table
            // otherwise, DROP statement will fail
            //
            if (IsSupportedProperty(nameof(IsSystemVersioned), sp))
            {
                // In Ledger you can't turn off SystemVersioning
                //
                bool ledgerTable = false;
                if (IsSupportedProperty(nameof(LedgerType), sp) && this.IsLedger)
                {
                    ledgerTable = true;
                }
                if ( !ledgerTable && this.IsSystemVersioned)
                {
                    this.IsSystemVersioned = false;
                    ScriptSystemVersioning(queries, sp);
                    this.IsSystemVersioned = true;
                }
            }

            // format full table name for scripting
            string sFullTableName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, sFullTableName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            var inlineIfExists = sp.IncludeScripts.ExistenceCheck && (sp.TargetDatabaseEngineEdition == DatabaseEngineEdition.SqlDatabase || sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version130);
            if (!inlineIfExists)
            {
                if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "", SqlString(sFullTableName));
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE80, "", SqlString(sFullTableName));
                }
                sb.Append(sp.NewLine);
            }

            // check if the table is an external table, and is also supported on target server for scripting
            bool isExternalAndCanScript = this.GetPropValueIfSupportedWithThrowOnTarget("IsExternal", false, sp);

            /*
            * This also removes all indexes, triggers, constraints,
            * and permission specifications for the table
            */
            if (!isExternalAndCanScript)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP TABLE {0}{1}",
                    (inlineIfExists) ? "IF EXISTS " : string.Empty,
                    sFullTableName);
            }
            else
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "DROP EXTERNAL TABLE {0}", sFullTableName);
            }

            queries.Add(sb.ToString());
        }

        public void Alter()
        {
            base.AlterImpl();
            SetSchemaOwned();
        }

        /// <summary>
        /// Rebuild particular partition
        /// </summary>
        /// <param name="rebuildPartitionNumber">The index of the partition to rebuild. -1 means all partitions</param>
        public void Rebuild(int rebuildPartitionNumber)
        {
            try
            {
                StringCollection queries = new StringCollection();
                var emptyQueryCount = InsertUseDbIfNeeded(queries).Count;
                var sp = new ScriptingPreferences
                {
                    ScriptForCreateDrop = true
                };
                sp.SetTargetServerInfo(this);
                if (-1 != rebuildPartitionNumber)
                {
                    StringBuilder onlineOption = new StringBuilder();
                    bool fOnlineOption = false;

                    // Single Partition Online Index Rebuild is supported for version
                    // 12 or later.
                    if (VersionUtils.IsSql12OrLater(this.ServerVersion) && OnlineHeapOperation)
                    {
                        onlineOption.AppendFormat(SmoApplication.DefaultCulture, "ONLINE = ON");
                        ScriptWaitAtLowPriorityIndexOption(onlineOption);
                        fOnlineOption = true;
                    }

                    bool fDataCompressionStateDirty = PhysicalPartitions.IsDataCompressionStateDirty(rebuildPartitionNumber);
                    bool fXmlCompressionCompatibleVersion = new PhysicalPartition(this, 0).IsSupportedProperty(nameof(PhysicalPartition.XmlCompression));
                    bool fXmlCompressionStateDirty = fXmlCompressionCompatibleVersion && PhysicalPartitions.IsXmlCompressionStateDirty(rebuildPartitionNumber);

                    if (fDataCompressionStateDirty || fXmlCompressionStateDirty)
                    {
                        if (fDataCompressionStateDirty && fXmlCompressionStateDirty)
                        {
                            string xmlCompressionScript = PhysicalPartitions.GetXmlCompressionCode(rebuildPartitionNumber);
                            bool fXmlCompressionScriptEmpty = string.IsNullOrEmpty(xmlCompressionScript);

                            queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} REBUILD PARTITION = {1} WITH({2}{3}{4}{5})",
                                FormatFullNameForScripting(sp), rebuildPartitionNumber, PhysicalPartitions.GetCompressionCode(rebuildPartitionNumber),
                                (fXmlCompressionScriptEmpty == false) ? "," + xmlCompressionScript : string.Empty,
                                (fOnlineOption) ? ", " + onlineOption.ToString() : string.Empty,
                                (MaximumDegreeOfParallelism > 0) ? ", MAXDOP = " + MaximumDegreeOfParallelism.ToString() : string.Empty));
                        }
                        else
                        {
                            var compressionScript = fDataCompressionStateDirty ?
                                PhysicalPartitions.GetCompressionCode(rebuildPartitionNumber) :
                                PhysicalPartitions.GetXmlCompressionCode(rebuildPartitionNumber);
                            bool isScriptEmpty = string.IsNullOrEmpty(compressionScript);
                            var onlineScript = string.Empty;

                            if (fOnlineOption)
                            {
                                if (!isScriptEmpty)
                                {
                                    onlineScript = "," + onlineOption.ToString();
                                }
                                else
                                {
                                    onlineScript = onlineOption.ToString();
                                }
                            }
                            var maxDopScript = MaximumDegreeOfParallelism > 0 ? ", MAXDOP = " + MaximumDegreeOfParallelism.ToString() : string.Empty;
                            queries.Add(FormattableString.Invariant($"ALTER TABLE {FormatFullNameForScripting(sp)} REBUILD PARTITION = {rebuildPartitionNumber} WITH({compressionScript}{onlineScript}{maxDopScript})"));
                        }
                    }
                    else
                    {
                        if (!fOnlineOption)
                        {
                            queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} REBUILD PARTITION = {1} {2}",
                                FormatFullNameForScripting(sp), rebuildPartitionNumber,
                                (MaximumDegreeOfParallelism > 0) ? "WITH (MAXDOP = " + MaximumDegreeOfParallelism.ToString() + ")" : string.Empty));
                        }
                        else
                        {
                            queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} REBUILD PARTITION = {1} WITH ({2}{3})",
                                FormatFullNameForScripting(sp), rebuildPartitionNumber, onlineOption.ToString(),
                                (MaximumDegreeOfParallelism > 0) ? ", MAXDOP = " + MaximumDegreeOfParallelism.ToString() : string.Empty));
                        }
                    }
                }
                else
                {
                    GenerateDataAndXmlCompressionAlterScript(queries, sp);
                }
                if (queries.Count == emptyQueryCount) //if only usedb script is there
                {
                    StringBuilder rebuildOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    ScriptRebuildOptions(rebuildOptions, sp);
                    if (rebuildOptions.Length > 0)
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} REBUILD WITH ({1})",
                      FormatFullNameForScripting(sp), rebuildOptions.ToString()));

                    }
                    else
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} REBUILD",
                              FormatFullNameForScripting(sp)));
                    }

                }
                this.ExecutionManager.ExecuteNonQuery(queries);

            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.RebuildHeapError(e.Message), this, e);
            }

            if (!this.ExecutionManager.Recording)
            {
                if (null != PhysicalPartitions)
                {
                    if (rebuildPartitionNumber == -1)
                    {
                        PhysicalPartitions.Reset();
                    }
                    else
                    {
                        PhysicalPartitions.Reset(rebuildPartitionNumber);
                    }
                }
            }
        }

        private StringCollection InsertUseDbIfNeeded(StringCollection queries)
        {
            if (DatabaseEngineType == Cmn.DatabaseEngineType.Standalone)
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            }
            return queries;
        }

        /// <summary>
        /// Rebuild table
        /// </summary>
        public void Rebuild()
        {
            Rebuild(-1);

        }

        /// <summary>
        /// This method script the rebuild options (except data compression) corresponding to rebuild command.
        /// At this moment it covers two options MAXDOP (MaximumDegreeOfParallelism) and ONLINE.
        ///
        /// This method will get modified whenever options like SORT_IN_TEMPDB, PAD_INDEX, IGNORE_DUP_KEY etc
        /// are going to be effective, which looks like under future plan.
        /// </summary>
        /// <param name="rebuildOptions"></param>
        private void ScriptRebuildOptions(StringBuilder rebuildOptions, ScriptingPreferences sp)
        {
            bool firstFlag = false;

            //By default OnLine option is off in engine. So, generation of script like
            //ONLINE = OFF is redundent.
            if (OnlineHeapOperation)
            {
                rebuildOptions.AppendFormat(SmoApplication.DefaultCulture, "ONLINE = ON");
                ScriptWaitAtLowPriorityIndexOption(rebuildOptions);
                firstFlag = true;
            }

            if (!IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType) &&
                MaximumDegreeOfParallelism > 0)
            {
                if (firstFlag)
                {
                    rebuildOptions.Append(Globals.commaspace);
                }

                rebuildOptions.AppendFormat(SmoApplication.DefaultCulture, "MAXDOP = {0}", MaximumDegreeOfParallelism);
                firstFlag = true;
            }

        }

        /// <summary>
        /// This method scripts the WAIT_AT_LOW_PRIORITY options for Online Table Rebuild and
        /// Switch Partition.
        /// </summary>
        /// <param name="options">The StringBuilder to append the options</param>
        private void ScriptWaitAtLowPriorityIndexOption(StringBuilder options)
        {
            if (VersionUtils.IsSql12OrLater(this.ServerVersion))
            {
                options.AppendFormat(
                    SmoApplication.DefaultCulture,
                    " (WAIT_AT_LOW_PRIORITY (MAX_DURATION = {0} MINUTES, ABORT_AFTER_WAIT = {1}))",
                    LowPriorityMaxDuration,
                    LowPriorityAbortAfterWait.ToString().ToUpper());
            }
        }

        private void GenerateDataAndXmlCompressionAlterScript(StringCollection alterQuery, ScriptingPreferences sp)
        {
            bool fDataCompressionPartitionDirty = PhysicalPartitions.IsCollectionDirty();
            bool fXmlCompressionCompatibleVersion = (this.ServerVersion.Major >= 16);
            bool fXmlCompressionPartitionDirty = fXmlCompressionCompatibleVersion ? PhysicalPartitions.IsXmlCollectionDirty() : false;
            var fullName = FormatFullNameForScripting(sp);

            if (fDataCompressionPartitionDirty && fXmlCompressionPartitionDirty)
            {
                StringBuilder rebuildOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                string dataCompressionScript = PhysicalPartitions.GetCompressionCode(true, true, sp);
                string xmlCompressionScript = PhysicalPartitions.GetXmlCompressionCode(true, true, sp);
                ScriptRebuildOptions(rebuildOptions, sp);
                var xmlScript = string.IsNullOrEmpty(xmlCompressionScript) ? string.Empty : "," + xmlCompressionScript;
                if (rebuildOptions.Length > 0)
                {
                    alterQuery.Add(FormattableString.Invariant($"ALTER TABLE {fullName} REBUILD PARTITION=ALL{sp.NewLine}WITH{sp.NewLine}({rebuildOptions},{sp.NewLine}{dataCompressionScript}{sp.NewLine}{xmlScript})"));
                }
                else
                {
                    alterQuery.Add(FormattableString.Invariant($"ALTER TABLE {fullName} REBUILD PARTITION=ALL{sp.NewLine}WITH{sp.NewLine}({dataCompressionScript}{sp.NewLine}{xmlScript})"));
                }
            }
            else if (fDataCompressionPartitionDirty || fXmlCompressionPartitionDirty)
            {
                StringBuilder rebuildOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                string compressionScript = fDataCompressionPartitionDirty ? PhysicalPartitions.GetCompressionCode(true, true, sp) : PhysicalPartitions.GetXmlCompressionCode(true, true, sp);
                ScriptRebuildOptions(rebuildOptions, sp);
                if (rebuildOptions.Length > 0)
                {
                    alterQuery.Add(FormattableString.Invariant($"ALTER TABLE {fullName} REBUILD PARTITION = ALL{sp.NewLine}WITH{sp.NewLine}({rebuildOptions}, {compressionScript}{sp.NewLine})"));
                }
                else
                {
                    alterQuery.Add(FormattableString.Invariant($"ALTER TABLE {fullName} REBUILD PARTITION = ALL{sp.NewLine}WITH{sp.NewLine}({compressionScript}{sp.NewLine})"));
                }
            }
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            // validate sp
            if (sp == null)
            {
                throw new ArgumentNullException("Scripting preferences cannot be null.");
            }
            this.ThrowIfNotSupported(this.GetType(), sp);

            // validate indexes on this table
            this.ValidateIndexes();

            // for state changes, we will suppose that all operations succeed and
            // we anticipate the state change. If operation fails, it is the client's
            // responsability to refresh the object's properties and the contained
            // collections

            bool forCreateScript = false;

            ScriptVardecimalCompression(alterQuery, sp, forCreateScript);

            // Script for Change Tracking Options on table
            if (IsSupportedProperty("ChangeTrackingEnabled", sp) && sp.Data.ChangeTracking)
            {
                ScriptChangeTracking(alterQuery, sp);
            }

            // Script for WITH SYSTEM VERSIONING ON/OFF
            if (IsSupportedProperty(nameof(IsSystemVersioned), sp))
            {
                ScriptSystemVersioning(alterQuery, sp);
            }

            // Check if system-time PERIOD needs to be altered
            if (IsSupportedProperty(nameof(HasSystemTimePeriod), sp))
            {
                ScriptSystemTimePeriodForAlter(alterQuery, sp);
            }

            //specify the filestream filegroup or partition and lock escalation specific setting
            if (IsSupportedProperty("FileStreamFileGroup", sp))
            {
                Property pFileStreamFileGroup = Properties.Get("FileStreamFileGroup");
                Property pFileStreamPartitionScheme = Properties.Get("FileStreamPartitionScheme");
                if (pFileStreamFileGroup.Dirty || pFileStreamPartitionScheme.Dirty)
                {
                    if (pFileStreamFileGroup.Dirty && pFileStreamPartitionScheme.Dirty)
                    {
                        throw new WrongPropertyValueException(
                               ExceptionTemplates.MutuallyExclusiveProperties("FileStreamPartitionScheme",
                               "FileStreamFileGroup"));
                    }
                    string sFullTableName = FormatFullNameForScripting(sp);
                    StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ", sFullTableName);
                    GenerateDataSpaceFileStreamScript(sb, sp, true);
                    alterQuery.Add(sb.ToString());
                }
            }

            if (IsSupportedProperty("LockEscalation", sp))
            {
                ScriptLockEscalationSettings(alterQuery, sp);
            }

            //script the filetable properties
            ScriptAlterFileTableProp(alterQuery, sp);

            if (sp.IncludeScripts.Owner)
            {
                //script change owner if dirty
                ScriptOwner(alterQuery, sp);
            }

            if (IsSupportedProperty("RemoteDataArchiveEnabled", sp))
            {
                ScriptRemoteDataArchive(alterQuery, sp);
            }

            alterQuery.Add(ScriptDataRetention(sp).ToString());
        }

        protected override void PostAlter()
        {
            base.PostAlter();

            this.m_systemTimePeriodInfo.Reset();
            this.m_systemTimePeriodInfo.MarkForDrop(false);
        }

        public void AlterWithNoCheck()
        {
            StringCollection alterQuery;
            ScriptingPreferences sp;
            try
            {
                AlterImplInit(out alterQuery, out sp);
                // Set DriWithNoCheck scripting flag - this makes all the dirrefence
                sp.Table.ConstraintsWithNoCheck = true;
                ScriptAlterInternal(alterQuery, sp);
                bool forCreateScript = false;
                ScriptVardecimalCompression(alterQuery, sp, forCreateScript);
                AlterImplFinish(alterQuery, sp);
                SetSchemaOwned();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Alter, this, e);
            }
        }

        public void Rename(string newname)
        {
            Table.CheckTableName(newname);
            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            this.ThrowIfNotSupported(this.GetType(), sp);

            AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_rename @objname = N'{0}', @newname = N'{1}', @objtype = N'OBJECT'",
                SqlString(this.FullQualifiedName),
                SqlString(newName)));
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_Checks)
            {
                m_Checks.MarkAllDropped();
            }

            if (null != m_EdgeConstraints)
            {
                m_EdgeConstraints.MarkAllDropped();
            }

            if (null != m_ForeignKeys)
            {
                m_ForeignKeys.MarkAllDropped();
            }

            if (null != m_PartitionSchemeParameters)
            {
                m_PartitionSchemeParameters.MarkAllDropped();
            }

            if (null != m_PhysicalPartitions)
            {
                m_PhysicalPartitions.MarkAllDropped();
            }
        }

        public StringCollection CheckIdentityValue()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                InsertUseDbIfNeeded(queries);


                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                                  "DBCC CHECKIDENT(N'{0}')", SqlString(this.FullQualifiedName)));


                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckIdentityValues, this, e);
            }
        }

        /// <summary>
        /// Tests the integrity of database pages implementing storage for the
        /// referenced table and indexes defined on it.
        /// </summary>
        /// <returns></returns>
        public StringCollection CheckTable()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                InsertUseDbIfNeeded(queries);

                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKTABLE (N'{0}') WITH NO_INFOMSGS", SqlString(this.FullQualifiedName)));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckTable, this, e);
            }
        }

        /// <summary>
        /// Tests the integrity of database pages implementing storage for the referenced table
        /// </summary>
        /// <returns></returns>
        public StringCollection CheckTableDataOnly()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                InsertUseDbIfNeeded(queries);

                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKTABLE (N'{0}', NOINDEX)", SqlString(this.FullQualifiedName)));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckTable, this, e);
            }
        }


        public DataTable EnumLastStatisticsUpdates()
        {
            try
            {
                CheckObjectState();
                return EnumLastStatisticsUpdates(null);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumLastStatisticsUpdates, this, e);
            }
        }

        public DataTable EnumLastStatisticsUpdates(string statname)
        {
            try
            {
                CheckObjectState();
                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/Statistic"));

                if (null != statname)
                {
                    req.Urn.Value += string.Format(SmoApplication.DefaultCulture, "[@Name='{0}']", Urn.EscapeString(statname));
                }

                req.Fields = new String[] { "Name", "LastUpdated" };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumLastStatisticsUpdates, this, e);
            }
        }

        /// <summary>
        /// this function enumerates all the ForeignKeys that reference the primary key
        /// of this table
        /// </summary>
        /// <returns></returns>
        public DataTable EnumForeignKeys()
        {
            try
            {
                Request req = new Request(this.ParentColl.ParentInstance.Urn + string.Format(SmoApplication.DefaultCulture, "/Table/ForeignKey[@ReferencedTable='{0}']",
                    Urn.EscapeString(this.Name)),
                    new string[] { "Name" });
                req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };

                req.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[] { "Schema", "Name" };
                parentProps.OrderByList = new OrderBy[] {   new OrderBy("Schema", OrderBy.Direction.Asc),
                                                            new OrderBy("Name", OrderBy.Direction.Asc) };
                req.ParentPropertiesRequests[0] = parentProps;

                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumForeignKeys, this, e);
            }
        }

        public void RebuildIndexes(int fillFactor)
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                InsertUseDbIfNeeded(queries);
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC DBREINDEX(N'{0}', N'', {1})", SqlString(FullQualifiedName), fillFactor));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RebuildIndexes, this, e);
            }
        }

        public void RecalculateSpaceUsage()
        {
            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                InsertUseDbIfNeeded(queries);
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC UPDATEUSAGE(0, N'{0}') WITH NO_INFOMSGS", SqlString(this.FullQualifiedName)));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RecalculateSpaceUsage, this, e);
            }
        }

        /// <summary>
        /// Truncate a table
        /// </summary>
        public void TruncateData()
        {
            // check if trying to truncate an external table
            // if yes, throw an exception as truncate operation is no supported on external tables
            if (this.CheckIsExternalTable())
            {
                throw new SmoException(ExceptionTemplates.TruncateOperationNotSupportedOnExternalTables);
            }

            try
            {
                CheckObjectState();
                StringCollection queries = new StringCollection();
                InsertUseDbIfNeeded(queries);
                queries.Add(string.Format(SmoApplication.DefaultCulture, "TRUNCATE TABLE [{0}].[{1}]", SqlBraket((string)this.Schema),
                    SqlBraket(Name)));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.TruncateData, this, e);
            }
        }

        /// <summary>
        /// Truncate a partition of partitioned table with the partition number
        /// </summary>
        /// <param name="partitionNumber">partiton to truncate</param>
        public void TruncateData(int partitionNumber)
        {
            if (VersionUtils.IsSql12OrLater(this.ServerVersion))
            {
                // check if trying to truncate an external table
                // if yes, throw an exception as truncate operation is no supported on external tables
                if (this.CheckIsExternalTable())
                {
                    throw new SmoException(ExceptionTemplates.TruncateOperationNotSupportedOnExternalTables);
                }

                try
                {
                    CheckObjectState();
                    StringCollection queries = new StringCollection();
                    queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "TRUNCATE TABLE [{0}].[{1}] WITH (PARTITIONS ({2}))", SqlBraket((string)this.Schema),
                        SqlBraket(Name), partitionNumber));

                    this.ExecutionManager.ExecuteNonQuery(queries);
                }
                catch (Exception e)
                {
                    SqlSmoObject.FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.TruncateData, this, e);
                }
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.TruncatePartitionsNotSupported);
            }
        }

        /// <summary>
        /// Disables all indexes.
        /// </summary>
        public void DisableAllIndexes()
        {
            try
            {
                CheckObjectState(true);
                ThrowIfBelowVersion90();

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.InternalName)));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER INDEX ALL ON {0} DISABLE", FullQualifiedName));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DisableAllIndexes, this, e);
            }
        }

        /// <summary>
        /// Enables all indexes. The action argument specifies how enable the index. It is
        /// possible to call Create() or Rebuild() on the Index
        /// </summary>
        /// <param name="action"></param>
        public void EnableAllIndexes(IndexEnableAction action)
        {
            try
            {
                CheckObjectState(true);
                ThrowIfBelowVersion90();

                var queries = new StringCollection();
                InsertUseDbIfNeeded(queries);

                if (action == IndexEnableAction.Rebuild)
                {
                    StringBuilder rebuildOptions = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    if (this.ServerVersion.Major >= 10)
                    {
                        ScriptRebuildOptions(rebuildOptions, new ScriptingPreferences(this));
                    }
                    if (rebuildOptions.Length > 0)
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER INDEX ALL ON {0} REBUILD WITH ({1})",
                            this.FullQualifiedName, rebuildOptions.ToString()));
                    }
                    else
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER INDEX ALL ON {0} REBUILD", this.FullQualifiedName));
                    }
                }
                else
                {
                    string dbName = GetDBName();

                    var sp = new ScriptingPreferences
                    {
                        ScriptForCreateDrop = false
                    };

                    // pass server version
                    sp.SetTargetServerInfo(this);

                    foreach (Index idx in this.Indexes)
                    {
                        bool oldValue = ((Index)idx).dropExistingIndex;
                        idx.dropExistingIndex = true;
                        try
                        {
                            idx.ScriptCreateInternal(queries, sp);
                        }
                        finally
                        {
                            ((Index)idx).dropExistingIndex = oldValue;
                        }
                    }
                }

                // execute the query
                this.ExecutionManager.ExecuteNonQuery(queries);

                // refresh the state of the indexes
                foreach (Index idx in this.Indexes)
                {
                    idx.Refresh();
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnableAllIndexes, this, e);
            }
        }

        /// <summary>
        /// Adds period information for the system-versioned temporal table.
        /// </summary>
        /// <param name="action"></param>
        public void AddPeriodForSystemTime(string periodStartColumn, string periodEndColumn, bool addPeriod)
        {
            if (!IsSupportedProperty("HasSystemTimePeriod"))
            {
                throw new SmoException(ExceptionTemplates.ReasonPropertyIsNotSupportedOnCurrentServerVersion);
            }

            if (this.State == SqlSmoState.Dropped || this.State == SqlSmoState.ToBeDropped)
            {
                throw new SmoException(ExceptionTemplates.NoAddingPeriodOnDroppedTable);
            }

            if (this.State == SqlSmoState.Existing && this.HasSystemTimePeriod)
            {
                throw new SmoException(ExceptionTemplates.CannotHaveMultiplePeriods);
            }

            if (addPeriod)
            {
                if (String.IsNullOrEmpty(periodStartColumn) || String.IsNullOrEmpty(periodEndColumn))
                {
                    throw new SmoException(ExceptionTemplates.InvalidPeriodColumnName);
                }

                this.m_systemTimePeriodInfo.MarkForCreate(periodStartColumn, periodEndColumn);
            }
            else
            {
                this.m_systemTimePeriodInfo.Reset();
            }
        }

        /// <summary>
        /// Drops sytem period from the temporal table
        /// </summary>
        /// <param name="action"></param>
        public void DropPeriodForSystemTime()
        {
            if (!IsSupportedProperty("HasSystemTimePeriod"))
            {
                throw new SmoException(ExceptionTemplates.ReasonPropertyIsNotSupportedOnCurrentServerVersion);
            }

            if (this.State == SqlSmoState.Dropped || this.State == SqlSmoState.ToBeDropped)
            {
                throw new SmoException(ExceptionTemplates.NoDroppingPeriodOnDroppedTable);
            }

            if (this.State == SqlSmoState.Creating)
            {
                throw new SmoException(ExceptionTemplates.NoDroppingPeriodOnNotYetCreatedTable);
            }

            if (this.State == SqlSmoState.Existing && !this.HasSystemTimePeriod)
            {
                throw new SmoException(ExceptionTemplates.CannotDropNonExistingPeriod);
            }

            this.m_systemTimePeriodInfo.MarkForDrop(true);
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return GetPropagateInfoImpl(action, false);
        }

        private PropagateInfo[] GetPropagateInfoImpl(PropagateAction action, bool forDiscovery)
        {
            bool bWithScript = action != PropagateAction.Create;
            ArrayList propInfo = new ArrayList();

            propInfo.Add(new PropagateInfo(ServerVersion.Major < 10 ? null : m_PhysicalPartitions, false, false));
            propInfo.Add(new PropagateInfo(Columns, bWithScript, true));
            propInfo.Add(new PropagateInfo(Statistics, true, Statistic.UrnSuffix));

            // Include EdgeConstraints for discovery and scripting if this object encapsulates any.
            //
            if (this.IsSupportedObject<EdgeConstraint>() &&
                (GetPropValueOptional("IsEdge", false) || GetPropValueOptional("IsNode", false)))
            {
                propInfo.Add(new PropagateInfo(EdgeConstraints, true, EdgeConstraint.UrnSuffix));
            }

            if (forDiscovery)
            {
                indexPropagationList = null;
                embeddedForeignKeyChecksList = null;
                // During discovery the Indexes collection is expanded so let's optimize it
                if (State == SqlSmoState.Existing)
                {
                    InitChildLevel(nameof(Index), new ScriptingPreferences(this), true);
                }
                (new IndexPropagateInfo(Indexes)).PropagateInfo(propInfo);
            }
            else
            {
                propInfo.Add(new PropagateInfo(Indexes, true, Index.UrnSuffix));
            }

            // Extended properties and Full Text index are not supported on SQL DW.
            // Triggers, foreign keys and check contraints are not supported for SQL DW.
            if (Cmn.DatabaseEngineEdition.SqlDataWarehouse != Parent.DatabaseEngineEdition)
            {
                // Extended properties and Full Text are only available on Azure for v12+
                if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType || ServerVersion.Major >= 12)
                {
                    propInfo.Add(new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix));

                    if (Parent.IsSupportedProperty("IsFullTextEnabled")
                        && null != FullTextIndex)
                    {
                        propInfo.Add(new PropagateInfo(FullTextIndex, true, FullTextIndex.UrnSuffix));
                    }
                }

                propInfo.Add(new PropagateInfo(Triggers, true, Trigger.UrnSuffix));

                if (!(IsSupportedProperty("IsFileTable") && GetPropValueOptional("IsFileTable", false)))
                {
                    propInfo.Add(new PropagateInfo(ForeignKeys, true, ForeignKey.UrnSuffix));
                    propInfo.Add(new PropagateInfo(Checks, true, Check.UrnSuffix));
                }
                else // For FileTable only add userdefined ForeignKeys and Checks
                {
                    List<ForeignKey> userDefinedForeignKeys = new List<ForeignKey>();
                    foreach (ForeignKey fk in ForeignKeys)
                    {
                        if (!fk.IsFileTableDefined)
                        {
                            userDefinedForeignKeys.Add(fk);
                        }
                    }
                    propInfo.Add(new PropagateInfo(userDefinedForeignKeys, true, ForeignKey.UrnSuffix));

                    List<Check> userDefinedChecks = new List<Check>();
                    foreach (Check check in Checks)
                    {
                        if (!check.IsFileTableDefined)
                        {
                            userDefinedChecks.Add(check);
                        }
                    }
                    propInfo.Add(new PropagateInfo(userDefinedChecks, true, Check.UrnSuffix));
                }
            }

            PropagateInfo[] retArr = new PropagateInfo[propInfo.Count];
            propInfo.CopyTo(retArr, 0);

            return retArr;
        }

        internal override PropagateInfo[] GetPropagateInfoForDiscovery(PropagateAction action)
        {
            return GetPropagateInfoImpl(action, true);
        }


        [SfcProperty(SfcPropertyFlags.Standalone)]
        public double RowCountAsDouble
        {
            get
            {
                return Convert.ToDouble(Properties["RowCount"].Value, SmoApplication.DefaultCulture);
            }
        }

        /// <summary>
        /// Switches the partition with the partition number sourcePartitionNumber of the
        /// current table to the partition with the partition number targetPartitionNumber
        /// of the table specified with targetTable.
        /// </summary>
        /// <param name="sourcePartitionNumber"></param>
        /// <param name="targetTable"></param>
        /// <param name="targetPartitionNumber"></param>
        public void SwitchPartition(int sourcePartitionNumber, Table targetTable, int targetPartitionNumber)
        {
            SwitchPartitionImpl(sourcePartitionNumber, targetTable, targetPartitionNumber);
        }

        /// <summary>
        /// Switches the partition with the partition number sourcePartitionNumber of the
        /// current table to the table specified with targetTable. This assumes that the
        /// target table is not partitioned.
        /// </summary>
        /// <param name="sourcePartitionNumber"></param>
        /// <param name="targetTable"></param>
        public void SwitchPartition(int sourcePartitionNumber, Table targetTable)
        {
            SwitchPartitionImpl(sourcePartitionNumber, targetTable, -1);
        }

        /// <summary>
        /// Switches the current table to the partition with the partition number
        /// targetPartitionNumber of the table specified with targetTable.
        /// </summary>
        /// <param name="targetTable"></param>
        /// <param name="targetPartitionNumber"></param>
        public void SwitchPartition(Table targetTable, int targetPartitionNumber)
        {
            SwitchPartitionImpl(-1, targetTable, targetPartitionNumber);
        }

        /// <summary>
        /// Switches the current table to the table specified with targetTable.
        /// </summary>
        /// <param name="targetTable"></param>
        public void SwitchPartition(Table targetTable)
        {
            SwitchPartitionImpl(-1, targetTable, -1);
        }

        /// <summary>
        /// Get remote table migration statistics. Null if Remote Data Archive is not enabled for table or the remote table provisioning is not complete
        /// </summary>
        /// <returns>Table Migration statistics if Remote Data Archive is enabled and the remote table is provisioned, else null</returns>
        public RemoteTableMigrationStatistics GetRemoteTableMigrationStatistics()
        {
            try
            {
                CheckObjectState();
                ThrowIfPropertyNotSupported("RemoteDataArchiveEnabled");

                if (this.RemoteDataArchiveEnabled && this.RemoteTableProvisioned)
                {

                    StringCollection queries = new StringCollection();

                    if (this.DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                    {
                        queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(this.Parent.Name)));
                    }
                    queries.Add(string.Format(CultureInfo.InvariantCulture, @"exec sp_spaceused @objname = N'[{0}].[{1}]', @mode = 'REMOTE_ONLY', @oneresultset = 1", Urn.EscapeString(this.Schema), Urn.EscapeString(this.Name)));

                    DataSet ds = this.ExecutionManager.ExecuteWithResults(queries);
                    double remoteTableSize = 0;
                    long rowCount = 0;
                    if (ds != null && ds.Tables != null && ds.Tables.Count > 0 && ds.Tables[0].Rows != null && ds.Tables[0].Rows.Count > 0)
                    {
                        string rowCountString = ds.Tables[0].Rows[0]["rows"].ToString();
                        rowCount = Int64.Parse(rowCountString.Trim());

                        string table_size = ds.Tables[0].Rows[0]["data"].ToString();
                        if (table_size.ToUpperInvariant().IndexOf("KB") > -1)
                        {
                            string sizeInKB = table_size.Substring(0, table_size.ToUpperInvariant().IndexOf("KB"));
                            remoteTableSize = Double.Parse(sizeInKB.Trim());
                        }
                    }
                    return new RemoteTableMigrationStatistics(remoteTableSize, rowCount);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetRemoteTableMigrationStatistics, this, e);
            }
        }

        /// <summary>
        /// Generate Data Retention clause.
        /// </summary>
        private StringBuilder ScriptDataRetention(ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            StringBuilder tempSb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (IsSupportedProperty(nameof(Table.DataRetentionEnabled), sp))
            {
                Property dataRetention = this.Properties.Get(nameof(Table.DataRetentionEnabled));

                bool isdataRetentionEnabled = dataRetention.IsNull ? false : (bool)dataRetention.Value;
                if (isdataRetentionEnabled)
                {
                    string colName = GetPropValueOptional(nameof(Table.DataRetentionFilterColumnName),"");

                    // Ensures at least one column was set as a filter column
                    //
                    if ( string.IsNullOrEmpty(colName) )
                    {
                        throw new InvalidSmoOperationException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.MissingDataRetentionFilterColumn));
                    }

                    colName = MakeSqlBraket(colName);
                    DataRetentionPeriodUnitTypeConverter converter = new DataRetentionPeriodUnitTypeConverter();
                    string unit = converter.ConvertToInvariantString(DataRetentionPeriodUnit);
                    string retentionPeriod = DataRetentionPeriodUnit != DataRetentionPeriodUnit.Infinite ? DataRetentionPeriod.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    tempSb.Append($"DATA_DELETION = ON ( FILTER_COLUMN = {colName}, RETENTION_PERIOD = {retentionPeriod} {unit} )");
                }
                else if (sp.ScriptForAlter && dataRetention.Dirty)
                {
                    tempSb.Append("DATA_DELETION = OFF");
                }

                if (sp.ScriptForAlter && dataRetention.Dirty)
                {
                    string sFullTableName = FormatFullNameForScripting(sp);
                    string dataDeletionBody = tempSb.ToString();
                    sb.Append($"ALTER TABLE {sFullTableName} SET ( {dataDeletionBody} )");
                }
                else
                {
                    sb = tempSb;
                }
            }

            return sb;
        }

        private void SwitchPartitionImpl(int sourcePartitionNumber, Table targetTable, int targetPartitionNumber)
        {
            if (null == targetTable)
            {
                throw new ArgumentNullException(nameof(targetTable));
            }

            try
            {

                CheckObjectState(true);
                if (this.Parent.Parent.ConnectionContext.SqlExecutionModes != Microsoft.SqlServer.Management.Common.SqlExecutionModes.CaptureSql)
                {
                    targetTable.CheckObjectState(true);
                }

                StringCollection queries = new StringCollection();

                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE [{0}].{1} SWITCH ",
                    SqlBraket(this.ParentColl.ParentInstance.InternalName), this.FullQualifiedName);
                if (0 <= sourcePartitionNumber)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, "PARTITION {0}", sourcePartitionNumber);
                }

                statement.AppendFormat(SmoApplication.DefaultCulture, " TO [{0}].{1}", SqlBraket(targetTable.Parent.Name),
                    targetTable.FullQualifiedName);

                if (0 <= targetPartitionNumber)
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, "PARTITION {0}", targetPartitionNumber);
                }

                if (VersionUtils.IsSql12OrLater(this.ServerVersion))
                {
                    statement.AppendFormat(SmoApplication.DefaultCulture, " WITH");

                    ScriptWaitAtLowPriorityIndexOption(statement);
                }

                queries.Add(statement.ToString());

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SwitchPartition, this, e);
            }
        }

        /// <summary>
        /// Checks for the following.
        ///		1. Any conflicting properties specified for the external table. If a conflicting
        ///		configuration is detected, throws an exception.
        ///		2. If Polybase properties are mixed with GQ Properties.
        ///		3. Depending on Polybase or GQ, if the right property combination is used for the
        ///		specific scenario.
        /// </summary>
        /// <param name="sp">Scripting preferences.</param>
        private void ValidateExternalTableOptionalProperties(ScriptingPreferences sp)
        {
            bool isPolybaseExtTable = true;

            // Polybase properties.
            const string RejectTypePropertyName = nameof(this.RejectType);
            const string RejectSampleValuePropertyName = nameof(this.RejectSampleValue);

            // GQ Properties.
            const string DistributionPropertyName = nameof(this.ExternalTableDistribution);
            const string ShardingColumnPropertyName = nameof(this.ShardingColumnName);
            const string RemoteSchemaPropertyName = nameof(this.RemoteSchemaName);
            const string RemoteObjectPropertyName = nameof(this.RemoteObjectName);

            // Assume GQ if the targetting the cloud; Polybase otherwise.
            if (sp.TargetDatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase)
            {
                isPolybaseExtTable = false;
            }

            // For polybase external tables confirm that if the reject type is 'value' (which is the default), then the reject sample value
            // is not specifed, as the reject sample value is only supported with the reject type being percentage.
            if (isPolybaseExtTable)
            {

                if (this.IsSupportedProperty(RejectTypePropertyName, sp))
                {
                    Property rejectTypeProp = this.GetPropertyOptional(RejectTypePropertyName);
                    ExternalTableRejectType rejectType = rejectTypeProp.IsNull ? ExternalTableRejectType.None : (ExternalTableRejectType)rejectTypeProp.Value;

                    // if the reject type is value (which is the default), the reject sample value property is not supported
                    // the reject sample value is only supported when the reject type is percentage
                    switch (rejectType)
                    {
                        case ExternalTableRejectType.Value:
                            // check the reject sample value property value
                            // if it is specified and not a default of NULL, throw an exception
                            if (IsSupportedProperty(RejectSampleValuePropertyName, sp))
                            {
                                Property prop = this.GetPropertyOptional(RejectSampleValuePropertyName);

                                // if the property value is not NULL and not a default of -1, throw an exception
                                if (!prop.IsNull && (double)prop.Value != -1)
                                {
                                    throw new SmoException(ExceptionTemplates.ConflictingExternalTableProperties(prop.Name, prop.Value.ToString(), rejectTypeProp.Name, rejectTypeProp.Value.ToString()));
                                }
                            }
                            break;
                        case ExternalTableRejectType.Percentage:

                            // check the reject sample value property value
                            // it must be set when the reject type is percentage
                            // if it is either NULL or the default -1, throw an exception
                            if (IsSupportedProperty(RejectSampleValuePropertyName, sp))
                            {
                                Property prop = this.GetPropertyOptional(RejectSampleValuePropertyName);

                                // if the property value is NULL or the default -1, throw an exception
                                if (prop.IsNull || (double)prop.Value == -1)
                                {
                                    throw new PropertyNotSetException(RejectSampleValuePropertyName);
                                }
                            }
                            break;
                        case ExternalTableRejectType.None:
                            // At the moment external generics tables have no reject type.
                            break;
                        default:
                            throw new WrongPropertyValueException(rejectTypeProp);
                    }
                }
            }
            else // GQ external table
            {
                if (this.IsSupportedProperty(DistributionPropertyName))
                {
                    Diagnostics.TraceHelper.Assert(this.IsSupportedProperty(ShardingColumnPropertyName));
                    Property distributionProperty = this.GetPropertyOptional(DistributionPropertyName);
                    Property shardingColProperty = this.GetPropertyOptional(ShardingColumnPropertyName);

                    if (!distributionProperty.IsNull)
                    {
                        switch ((ExternalTableDistributionType)distributionProperty.Value)
                        {
                            case ExternalTableDistributionType.Sharded:
                                // A sharding must be supplied when using shared distribution.
                                if (shardingColProperty.IsNull || string.IsNullOrEmpty(shardingColProperty.Value.ToString()))
                                {
                                    throw new SmoException(ExceptionTemplates.ShardingColumnNotSpecifiedForShardedDistribution(shardingColProperty.Name));
                                }
                                string shardingColName = shardingColProperty.Value.ToString();
                                if (!Columns.Contains(shardingColName))
                                {
                                    throw new SmoException(ExceptionTemplates.ShardingColumnNotAddedToTable(shardingColName));
                                }
                                break;
                            case ExternalTableDistributionType.Replicated:
                            case ExternalTableDistributionType.RoundRobin:
                            case ExternalTableDistributionType.None:
                                // A sharding column cannot be used when using a non-shared/no distribution.
                                if (!shardingColProperty.IsNull && !string.IsNullOrEmpty(shardingColProperty.Value.ToString()))
                                {
                                    throw new SmoException(ExceptionTemplates.ShardingColumnNotSupportedWithNonShardedDistribution(shardingColProperty.Name, distributionProperty.Value.ToString()));
                                }
                                break;
                            default:
                                throw new WrongPropertyValueException(distributionProperty);
                        }
                    }
                    else
                    {
                        // A sharding column is only valid with sharded distribution.
                        if (!shardingColProperty.IsNull && !string.IsNullOrEmpty(shardingColProperty.Value.ToString()))
                        {
                            throw new SmoException(ExceptionTemplates.ConflictingExternalTableProperties(shardingColProperty.Name, shardingColProperty.Value.ToString(), distributionProperty.Name, distributionProperty.Value.ToString()));
                        }
                    }

                    // Either both remote schema and remote object proerties be used, or none.
                    if (this.IsSupportedProperty(RemoteSchemaPropertyName))
                    {
                        Diagnostics.TraceHelper.Assert(this.IsSupportedProperty(RemoteObjectPropertyName));
                        Property remoteSchemaProperty = this.GetPropertyOptional(RemoteSchemaPropertyName);
                        Property remoteObjectProperty = this.GetPropertyOptional(RemoteObjectPropertyName);

                        bool remoteSchemaHasValue = !remoteSchemaProperty.IsNull && !string.IsNullOrEmpty(remoteSchemaProperty.Value.ToString());
                        bool remoteObjectHasValue = !remoteObjectProperty.IsNull && !string.IsNullOrEmpty(remoteObjectProperty.Value.ToString());

                        if ((remoteSchemaHasValue && !remoteObjectHasValue) || (!remoteSchemaHasValue && remoteObjectHasValue))
                        {
                            throw new SmoException(ExceptionTemplates.DependentPropertyMissing(remoteSchemaProperty.Name, remoteObjectProperty.Name));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks if current table object is an external table.
        /// </summary>
        /// <returns>True if table is an external table, false otherwise.</returns>
        private bool CheckIsExternalTable()
        {
            return this.GetPropValueIfSupported("IsExternal", false);
        }

        /// <summary>
        /// Checks if current table object is a SQL DW table.
        /// </summary>
        /// <returns>True if table is a SQL DW table, false otherwise.</returns>
        private bool CheckIsSqlDwTable()
        {
            bool isSqlDwTable = false;
            if (this.IsSupportedProperty("DwTableDistribution"))
            {
                isSqlDwTable = this.GetPropValueOptional<DwTableDistributionType>("DwTableDistribution").HasValue;
            }
            return isSqlDwTable;
        }

        /// <summary>
        /// Checks if current table object is a memory optimized table.
        /// </summary>
        /// <returns>True if table is memory optimized, false otherwise.</returns>
        private bool CheckIsMemoryOptimizedTable()
        {
            return this.GetPropValueIfSupported("IsMemoryOptimized", false);
        }

        static internal void CheckTableName(string tableName)
        {
            if (tableName.StartsWith("#", StringComparison.Ordinal))
            {
                // we don't support temp tables
                throw new WrongPropertyValueException(ExceptionTemplates.TempTablesNotSupported(tableName));
            }
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
            Cmn.ServerVersion version,
            Cmn.DatabaseEngineType databaseEngineType,
            Cmn.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {

            // Change Tracking and its property are added as scriptable fields
            string[] fields =
            {
                "AnsiNullsStatus",
                "ChangeTrackingEnabled",
                nameof(Table.DataRetentionEnabled),
                nameof(Table.DataRetentionFilterColumnName),
                nameof(Table.DataRetentionPeriod),
                nameof(Table.DataRetentionPeriodUnit),
                "DataSourceName",
                "DwTableDistribution",
                "Durability",
                "ExternalTableDistribution",
                "FileFormatName",
                "FileFormatNameOd",
                "FileGroup",
                "FileStreamFileGroup",
                "FileStreamPartitionScheme",
                "FileTableDirectoryName",
                "FileTableNameColumnCollation",
                "FileTableNamespaceEnabled",
                "HasClusteredIndex",
                "HasClusteredColumnStoreIndex",
                "HasHeapIndex",
                "HasSystemTimePeriod",
                nameof(Table.HistoryRetentionPeriod),
                nameof(Table.HistoryRetentionPeriodUnit),
                nameof(Table.HistoryTableID),
                nameof(Table.HistoryTableName),
                nameof(Table.HistoryTableSchema),
                "ID",
                nameof(Table.IsDroppedLedgerTable),
                "IsEdge",
                "IsExternal",
                "IsFileTable",
                nameof(Table.IsLedger),
                "IsMemoryOptimized",
                "IsNode",
                "IsSchemaOwned",
                "IsSystemObject",
                nameof(Table.IsSystemVersioned),
                "IsPartitioned",
                "IsVarDecimalStorageFormatEnabled",
                nameof(Table.LedgerType),
                nameof(Table.LedgerViewName),
                nameof(Table.LedgerViewSchema),
                nameof(Table.LedgerViewTransactionIdColumnName),
                nameof(Table.LedgerViewSequenceNumberColumnName),
                nameof(Table.LedgerViewOperationTypeColumnName),
                nameof(Table.LedgerViewOperationTypeDescColumnName),
                "Location",
                "LocationOd",
                "LockEscalation",
                "Owner",
                "PartitionScheme",
                "RejectedRowLocation",
                "RejectSampleValue",
                "RejectType",
                "RejectValue",
                "RemoteObjectName",
                "RemoteSchemaName",
                "ShardingColumnName",
                "SystemTimePeriodEndColumn",
                "SystemTimePeriodStartColumn",
                "TemporalType",
                "TextFileGroup",
                "TrackColumnsUpdatedEnabled"
            };
            List<string> list = GetSupportedScriptFields(typeof(Table.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }


        /// <summary>
        /// Returns the additional expensive fields that will be needed to script this object based on preference
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server.
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <param name="sp">The scripting preference</param>
        /// <returns></returns>
        internal static string[] GetScriptFields2(Type parentType, Cmn.ServerVersion version, Cmn.DatabaseEngineType databaseEngineType, Cmn.DatabaseEngineEdition databaseEngineEdition, bool defaultTextMode, ScriptingPreferences sp)
        {
            if ((version.Major > 9)
                && (sp.TargetServerVersionInternal > SqlServerVersionInternal.Version90)
                && (sp.Storage.DataCompression))
            {
                return new string[] { "HasCompressedPartitions" };
            }
            else if((version.Major >= 16)
                && (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version160)
                && (sp.Storage.XmlCompression))
            {
                return new string[] { "HasCompressedPartitions", "HasXmlCompressedPartitions"};
            }
            else
            {
                return new string[] { };
            }
        }

        /// <summary>
        /// Whether decimal data is stored in variable-length fields in the table
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Expensive | SfcPropertyFlags.Deploy | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.Boolean IsVarDecimalStorageFormatEnabled
        {
            get
            {
                return (System.Boolean)this.Properties.GetValueWithNullReplacement("IsVarDecimalStorageFormatEnabled");
            }

            set
            {
                if (!this.Parent.IsVarDecimalStorageFormatSupported)
                {
                    throw new Microsoft.SqlServer.Management.Common.PropertyNotAvailableException(
                        ExceptionTemplates.ReasonPropertyIsNotSupportedOnCurrentServerVersion);
                }
                this.Properties.SetValueWithConsistencyCheck("IsVarDecimalStorageFormatEnabled", value);
            }
        }

        /// <summary>
        /// Emit script to set vardecimal storage format for the table
        /// </summary>
        /// <param name="query"></param>
        /// <param name="so"></param>
        private void ScriptVardecimalCompression(StringCollection query, ScriptingPreferences sp, bool forCreate)
        {
            if (IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType))
            {
                return;
            }

            if ((sp.ForDirectExecution || !sp.OldOptions.NoVardecimal) &&
                this.Parent.IsVarDecimalStorageFormatSupported)
            {
                Property enableVarDecimal = Properties.Get("IsVarDecimalStorageFormatEnabled");

                // script this when
                //   1) the value has changed, or
                //   2) we're generating a create script for an existing table and vardecimal is enabled
                if (enableVarDecimal.Dirty ||
                    (forCreate && (this.State == SqlSmoState.Existing) && this.IsVarDecimalStorageFormatEnabled))
                {
                    query.Add(
                        string.Format(
                            SmoApplication.DefaultCulture,
                            "EXEC sys.sp_tableoption N'{0}', N'vardecimal storage format', N'{1}'",
                            FormatFullNameForScripting(sp),
                            (this.IsVarDecimalStorageFormatEnabled ? "ON" : "OFF")));
                }
            }
        }

        // Script Alter FileTable properties : FileTableNamespaceEnabled, FileTableDirectoryName
        private void ScriptAlterFileTableProp(StringCollection query, ScriptingPreferences sp)
        {
            if (!IsSupportedProperty("IsFileTable"))
            {
                return;
            }

            Boolean isFileTable = this.GetPropValueOptional("IsFileTable", false);
            Property fileTableNamespaceEnabled = this.GetPropertyOptional("FileTableNamespaceEnabled");
            Property fileTableDirectoryName = this.GetPropertyOptional("FileTableDirectoryName");


            if (fileTableDirectoryName.Dirty || fileTableNamespaceEnabled.Dirty)
            {
                if (!IsSupportedProperty("IsFileTable", sp))
                {
                    throw new SmoException(ExceptionTemplates.FileTableNotSupportedOnTargetEngine(GetSqlServerName(sp)));
                }
                if (!isFileTable)
                {
                    throw new SmoException(ExceptionTemplates.PropertyOnlySupportedForFileTable(fileTableDirectoryName.Dirty ?
                        "FileTableDirectoryName" : "FileTableNamespaceEnabled"));
                }
            }

            if (!string.IsNullOrEmpty(fileTableDirectoryName.Value as string) && sp.ScriptForAlter)
            {
                if (fileTableDirectoryName.Dirty || !sp.ForDirectExecution)
                {
                    string script = string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0}{1}SET( FILETABLE_DIRECTORY = {2})",
                        FormatFullNameForScripting(sp), Globals.newline, SqlSmoObject.MakeSqlString((string)fileTableDirectoryName.Value));
                    query.Add(script);
                }
            }
            if (isFileTable && fileTableNamespaceEnabled.Value != null &&
                (fileTableNamespaceEnabled.Dirty || (!sp.ForDirectExecution && (bool)fileTableNamespaceEnabled.Value == false)))
            {
                string script = string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0}{1}{2} FILETABLE_NAMESPACE",
                    FormatFullNameForScripting(sp), Globals.newline, (bool)fileTableNamespaceEnabled.Value ? Scripts.ENABLE : Scripts.DISABLE);
                query.Add(script);
            }
        }


        // Change Tracking can be enabled or disabled on the table
        private void ScriptChangeTracking(StringCollection query, ScriptingPreferences sp)
        {
            //Change tracking isn't enabled for Azure before v12
            if (IsCloudAtSrcOrDest(this.DatabaseEngineType, sp.TargetDatabaseEngineType) &&
                !VersionUtils.IsSql12OrLater(sp.TargetServerVersionInternal, this.ServerVersion))
            {
                return;
            }

            Property tableChangeTracking = this.Properties.Get("ChangeTrackingEnabled");
            bool changeTracking = false;

            StringBuilder sbStatement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (!tableChangeTracking.IsNull)
            {
                changeTracking = (bool)tableChangeTracking.Value;
                //While creating or scripting, script change tracking only when it is enabled. For details see VSTS bug 282883.
                if ((tableChangeTracking.Dirty && sp.ScriptForAlter) || (changeTracking && !sp.ScriptForAlter))
                {
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} {1} CHANGE_TRACKING ", this.FormatFullNameForScripting(sp), (bool)tableChangeTracking.Value ? "ENABLE" : "DISABLE");
                }
            }

            Property isTrackColumnsUpdated = this.Properties.Get("TrackColumnsUpdatedEnabled");
            if (!isTrackColumnsUpdated.IsNull && (isTrackColumnsUpdated.Dirty || !sp.ScriptForAlter))
            {
                if (!changeTracking)
                {
                    if ((bool)(isTrackColumnsUpdated.Value))
                    {
                        //Throw an error when the change tracking property on table is disabled and track columns property is set
                        throw new WrongPropertyValueException(ExceptionTemplates.TrackColumnsException);
                    }
                }
                else
                {
                    if (sbStatement.Length == 0)
                    {
                        sbStatement.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ENABLE CHANGE_TRACKING ", this.FormatFullNameForScripting(sp));
                    }
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "WITH(TRACK_COLUMNS_UPDATED = {0})", (bool)(isTrackColumnsUpdated.Value) ? "ON" : "OFF");
                }
            }

            if (sbStatement.Length > 0)
            {
                query.Add(sbStatement.ToString());
            }
        }

        // Scripts temporal system versioning option.
        // Here's the syntax that gets scripted:
        //
        // SYSTEM_VERSIONING = OFF
        // or
        // SYSTEM_VERSIONING = ON [(<options>)]
        //
        // <options> = [HISTORY_TABLE = [schema].[table]] | ,[DATA_CONSISTENCY_CHECK = ON|OFF] | [,HISTORY_RETENTION_PERIOD = n DAY|WEEK|MONTH|YEAR]
        //
        // An example:
        // SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.history, DATA_CONSISTENCY_CHECK = OFF, HISTORY_RETENTION_PERIOD = 14 DAY)
        //
        private void ScriptSystemVersioning(StringCollection query, ScriptingPreferences sp)
        {
            var systemVersioning = Properties.Get(nameof(IsSystemVersioned));
            var historyTableNameProperty = Properties.Get(nameof(HistoryTableName));
            var historyTableSchemaProperty = Properties.Get(nameof(HistoryTableSchema));

            // These are only available on Sterling
            //
            Property retentionPeriodProperty = null;
            Property retentionUnitProperty = null;
            bool retentionPeriodPropertyDirty = false;
            bool retentionUnitPropertyDirty = false;

            if (IsSupportedProperty(nameof(HistoryRetentionPeriod)))
            {
                retentionPeriodProperty = Properties.Get(nameof(HistoryRetentionPeriod));
                retentionUnitProperty = Properties.Get(nameof(HistoryRetentionPeriodUnit));

                retentionPeriodPropertyDirty = retentionPeriodProperty.Dirty;
                retentionUnitPropertyDirty = retentionUnitProperty.Dirty;
            }

            bool isSystemVersioned = false;
            string histTableName = String.Empty;
            string histTableSchema = String.Empty;

            if (!systemVersioning.IsNull)
            {
                isSystemVersioned = (bool)systemVersioning.Value;

                if (systemVersioning.Dirty || retentionPeriodPropertyDirty || retentionUnitPropertyDirty)
                {
                    StringBuilder sbSystemVersioning = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                    string sFullTableName = FormatFullNameForScripting(sp);

                    if (sp.IncludeScripts.Header) // need to generate commentary headers
                    {
                        sbSystemVersioning.Append(ExceptionTemplates.IncludeHeader(
                                UrnSuffix, sFullTableName, DateTime.Now.ToString(GetDbCulture())));

                        sbSystemVersioning.Append(sp.NewLine);
                    }

                    if (sp.IncludeScripts.ExistenceCheck)
                    {
                        if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version90)
                        {
                            sbSystemVersioning.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE90, "", SqlString(sFullTableName));
                        }
                        else
                        {
                            sbSystemVersioning.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_TABLE80, "", SqlString(sFullTableName));
                        }
                        sbSystemVersioning.Append(sp.NewLine);
                    }

                    ScriptStringBuilder systemVersioningClauseBuilder = new ScriptStringBuilder(
                        String.Format(
                            "SYSTEM_VERSIONING = {0}",
                            isSystemVersioned ? "ON" : "OFF"));

                    if (isSystemVersioned)
                    {
                        if (!historyTableNameProperty.IsNull)
                        {
                            histTableName = historyTableNameProperty.Value.ToString();
                        }

                        if (!historyTableSchemaProperty.IsNull)
                        {
                            histTableSchema = historyTableSchemaProperty.Value.ToString();
                        }

                        // Both history table name and history table schema must be provided
                        //
                        if (historyTableNameProperty.IsNull != historyTableSchemaProperty.IsNull)
                        {
                            throw new SmoException(ExceptionTemplates.BothHistoryTableNameAndSchemaMustBeProvided);
                        }

                        if (!String.IsNullOrEmpty(histTableName))
                        {
                            string schemaPart = MakeSqlBraket(histTableSchema);
                            string tablePart = MakeSqlBraket(histTableName);

                            systemVersioningClauseBuilder.SetParameter("HISTORY_TABLE", String.Format("{0}.{1}", schemaPart, tablePart), ParameterValueFormat.NotString);
                        }

                        // Script only if set to default, otherwise it defaults to TRUE if not specified
                        //
                        if (m_DataConsistencyCheckForSystemVersionedTable == false)
                        {
                            systemVersioningClauseBuilder.SetParameter("DATA_CONSISTENCY_CHECK", "OFF", ParameterValueFormat.NotString);
                        }

                        bool isRetentionPeriodNull = retentionPeriodProperty == null ? true : retentionPeriodProperty.IsNull;
                        bool isRetentionUnitNull = retentionUnitProperty == null ? true : retentionUnitProperty.IsNull;
                        int historyRetentionPeriod = isRetentionPeriodNull ? 0 : (int)retentionPeriodProperty.Value;
                        TemporalHistoryRetentionPeriodUnit historyRetentionPeriodUnit = isRetentionUnitNull ? TemporalHistoryRetentionPeriodUnit.Undefined : (TemporalHistoryRetentionPeriodUnit)retentionUnitProperty.Value;

                        if ((isRetentionPeriodNull && isRetentionUnitNull) || (historyRetentionPeriod == 0 && historyRetentionPeriodUnit == TemporalHistoryRetentionPeriodUnit.Undefined))
                        {
                            // Don't script anything, we default to INFINITE retention
                            //
                        }
                        else
                        {
                            if (historyRetentionPeriodUnit == TemporalHistoryRetentionPeriodUnit.Undefined)
                            {
                                throw new SmoException(ExceptionTemplates.InvalidHistoryRetentionPeriodUnitSpecification);
                            }
                            else if (historyRetentionPeriodUnit == TemporalHistoryRetentionPeriodUnit.Infinite)
                            {
                                // Don't script anything, we default to INFINITE retention
                                // and the retention period value is not scripted at all.
                                //
                                // Still, validate that retention period is >= -1, as it's the valid range that
                                // this field can have
                                // -2 denotes non-temporal table
                                // -1 denotes INFINITE retention
                                // 1+ denotes finite retention value
                                if (historyRetentionPeriod != -1)
                                {
                                    throw new SmoException(ExceptionTemplates.InvalidHistoryRetentionPeriodSpecification);
                                }
                            }
                            else if (historyRetentionPeriod < 1)
                            {
                                // If the retention is not infinite, we expect positive integer value here
                                //
                                throw new SmoException(ExceptionTemplates.InvalidHistoryRetentionPeriodSpecification);
                            }
                            else
                            {
                                TemporalHistoryRetentionPeriodUnitTypeConverter converter = new TemporalHistoryRetentionPeriodUnitTypeConverter();
                                string unit = converter.ConvertToInvariantString(historyRetentionPeriodUnit);

                                systemVersioningClauseBuilder.SetParameter(
                                    "HISTORY_RETENTION_PERIOD",
                                    String.Format(SmoApplication.DefaultCulture, "{0} {1}", historyRetentionPeriod.ToString(), unit),
                                    ParameterValueFormat.NotString);
                            }
                        }
                    }

                    sbSystemVersioning.Append(String.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} SET ( {1} )",
                            this.FormatFullNameForScripting(sp),
                            systemVersioningClauseBuilder.ToString(scriptSemiColon: false)));

                    query.Add(sbSystemVersioning.ToString());
                }
            }
        }

        // Validates if PERIOD has to be created/dropped during ALTER
        private void ScriptSystemTimePeriodForAlter(StringCollection query, ScriptingPreferences sp)
        {
            this.ValidateSystemTimeTemporal();

            if (this.HasSystemTimePeriod)
            {
                if (this.m_systemTimePeriodInfo.m_MarkedForCreate)
                {
                    return;
                }
                else if (this.m_systemTimePeriodInfo.m_MarkedForDrop)
                {
                    // dropping PERIOD
                    query.Add(String.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP PERIOD FOR SYSTEM_TIME", this.FormatFullNameForScripting(sp)));
                }
            }
            else
            {
                if (this.m_systemTimePeriodInfo.m_MarkedForCreate)
                {
                    // adding a PERIOD
                    query.Add(String.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} ADD PERIOD FOR SYSTEM_TIME ( [{1}], [{2}] )",
                        this.FormatFullNameForScripting(sp),
                        Util.EscapeString(this.m_systemTimePeriodInfo.m_StartColumnName, ']'),
                        Util.EscapeString(this.m_systemTimePeriodInfo.m_EndColumnName, ']')));
                }
            }
        }

        /// <summary>
        /// Adds the appropriate ALTER TABLE ... statements to the StringCollection based on
        /// the current state of Remote Data Archive Migration on the table.
        /// </summary>
        /// <param name="queries">The collection of statements to add to</param>
        /// <param name="sp">The settings for generating the scripts</param>
        private void ScriptRemoteDataArchive(StringCollection queries, ScriptingPreferences sp)
        {
            // Caller already checked RemoteDataArchiveEnabled is valid for sp

            Property propRemoteDataArchiveEnabled = this.Properties.Get("RemoteDataArchiveEnabled");

            bool remoteDataArchiveEnabled = false;
            StringBuilder sbStatement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (!propRemoteDataArchiveEnabled.IsNull)
            {
                remoteDataArchiveEnabled = (bool)propRemoteDataArchiveEnabled.Value;

                Property propRemoteDataArchiveMigrationState = this.Properties.Get("RemoteDataArchiveDataMigrationState");
                Property propRemoteDataArchiveFilterPredicate = this.Properties.Get("RemoteDataArchiveFilterPredicate");

                //While creating or scripting, script remote data archive only when it is enabled. (default is disabled so no reason to script it in that case)
                if (((propRemoteDataArchiveEnabled.Dirty || propRemoteDataArchiveMigrationState.Dirty || propRemoteDataArchiveFilterPredicate.Dirty) && sp.ScriptForAlter) ||
                    (remoteDataArchiveEnabled && !sp.ScriptForAlter))
                {
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.LParen);

                    RemoteDataArchiveMigrationState currentMigrationState = (RemoteDataArchiveMigrationState)propRemoteDataArchiveMigrationState.Value;
                    string migrationState;
                    switch (currentMigrationState)
                    {
                        case RemoteDataArchiveMigrationState.PausedInbound:
                        case RemoteDataArchiveMigrationState.PausedOutbound:
                            migrationState = "PAUSED";
                            break;
                        case RemoteDataArchiveMigrationState.Outbound:
                            migrationState = "OUTBOUND";
                            break;
                        case RemoteDataArchiveMigrationState.Inbound:
                            migrationState = "INBOUND";
                            break;
                        default:
                            migrationState = "PAUSED";
                            break;
                    }
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, "MIGRATION_STATE = {0}", migrationState);
                    if (!propRemoteDataArchiveFilterPredicate.IsNull)
                    {
                        if (currentMigrationState == RemoteDataArchiveMigrationState.Outbound || currentMigrationState == RemoteDataArchiveMigrationState.PausedOutbound)
                        {
                            string filterPredicate = (string)propRemoteDataArchiveFilterPredicate.Value;
                            if (!string.IsNullOrEmpty(filterPredicate))
                            {
                                sbStatement.AppendFormat(SmoApplication.DefaultCulture, ", FILTER_PREDICATE = {0}", filterPredicate);
                            }
                        }
                    }
                    sbStatement.AppendFormat(SmoApplication.DefaultCulture, Globals.RParen);
                }
            }

            if (sbStatement.Length > 0)
            {
                // ALTER TABLE statement is added only if any of the properties is changed
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                    "ALTER TABLE {0} {1}(REMOTE_DATA_ARCHIVE = {2} {3})", this.FormatFullNameForScripting(sp),
                    Scripts.SET, remoteDataArchiveEnabled ? Globals.On : Scripts.OFF_WITHOUT_DATA_RECOVERY, sbStatement.ToString()));
            }
        }

        /// <summary>
        /// It is a helper function to script lock escalation settings
        /// </summary>
        /// <param name="scqueries"></param>
        /// <param name="lockStatus"></param>
        /// <param name="so"></param>
        /// <param name="scriptTableGranularity"></param>
        internal void ScriptLockGranularity(StringCollection scqueries, LockEscalationType lockStatus, ScriptingPreferences sp, bool scriptTableGranularity)
        {
            if (Cmn.DatabaseEngineType.SqlAzureDatabase == sp.TargetDatabaseEngineType)
            {
                return;
            }

            StringBuilder sbStatement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sbStatement.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} SET ", this.FormatFullNameForScripting(sp));
            sbStatement.Append(Globals.LParen);

            if (Enum.IsDefined(typeof(LockEscalationType), lockStatus))
            {
                switch (lockStatus)
                {
                    case LockEscalationType.Auto:
                        sbStatement.AppendFormat(SmoApplication.DefaultCulture, "LOCK_ESCALATION = {0}", "AUTO");
                        break;
                    case LockEscalationType.Disable:
                        sbStatement.AppendFormat(SmoApplication.DefaultCulture, "LOCK_ESCALATION = {0}", "DISABLE");
                        break;
                    case LockEscalationType.Table:
                        if (scriptTableGranularity)
                        {
                            sbStatement.AppendFormat(SmoApplication.DefaultCulture, "LOCK_ESCALATION = {0}", "TABLE");
                        }
                        else
                        {
                            sbStatement.Length = 0;
                        }
                        break;
                }
            }
            else
            {
                throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("LockEscalationType"));
            }

            if (sbStatement.Length > 0)
            {
                sbStatement.Append(Globals.RParen);
                scqueries.Add(string.Format(SmoApplication.DefaultCulture, sbStatement.ToString()));
            }
        }


        /// <summary>
        /// This function scripts Lock Escalation settings on a Table
        /// </summary>
        /// <param name="scqueries"></param>
        /// <param name="so"></param>
        internal void ScriptLockEscalationSettings(StringCollection scqueries, ScriptingPreferences sp)
        {
            Property lockEscalation = this.properties.Get("LockEscalation");
            LockEscalationType lockStatus = LockEscalationType.Table;

            if (!lockEscalation.IsNull)
            {
                lockStatus = (LockEscalationType)lockEscalation.Value;
                if (lockEscalation.Dirty)
                {
                    ScriptLockGranularity(scqueries, lockStatus, sp, true);
                }
                else
                {
                    // just script whatever the proeprty had except LockEscalation.Table as it is default
                    if (!sp.ScriptForAlter)
                    {
                        ScriptLockGranularity(scqueries, lockStatus, sp, false);
                    }
                }
            }
        }



        internal override void ScriptCreateInternal(StringCollection query, ScriptingPreferences sp)
        {
            this.indexPropagationList = null;
            this.embeddedForeignKeyChecksList = null;
            ScriptMaker scriptMaker = new ScriptMaker(this.GetServerObject());
            scriptMaker.Preferences = sp;
            StringCollection createQuery = scriptMaker.Script(new SqlSmoObject[] { this });

            foreach (var item in createQuery)
            {
                query.Add(item);
            }
        }

        /// <summary>
        /// Validates the external table objects does not contain any unsupported collections.
        /// </summary>
        private void ValidateExternalTable()
        {
            // External table cannot contain:
            //   check constraints
            //   foreign key constraints
            //   partition scheme parameters
            //   triggers
            //   indexes
            //   physical partitions
            // External table can only have columns, statistics and extended properties collections.

            // Check if the table is an external table, then ensure it does not contain any unsupported collections.
            // If an unsupported collection contains any elements, throw an exception.
            bool isExternal = this.CheckIsExternalTable();

            // ensure that the object is being created
            if (isExternal && this.State == SqlSmoState.Creating)
            {
                // validate that the external table does not contain any checks
                if (this.Checks.Count > 0)
                {
                    throw new SmoException(ExceptionTemplates.ExternalTableCannotContainChecks);
                }

                // validate that the external table does not contain any foreign keys
                if (this.ForeignKeys.Count > 0)
                {
                    throw new SmoException(ExceptionTemplates.ExternalTableCannotContainForeignKeys);
                }

                // validate that the external table does not contain any partition scheme parameters
                if (this.PartitionSchemeParameters.Count > 0)
                {
                    throw new SmoException(ExceptionTemplates.ExternalTableCannotContainPartitionSchemeParameters);
                }

                // // validate that the external table does not contain any triggers
                if (this.Triggers.Count > 0)
                {
                    throw new SmoException(ExceptionTemplates.ExternalTableCannotContainTriggers);
                }

                // validate that the external table does not contain any indexes
                if (this.Indexes.Count > 0)
                {
                    throw new SmoException(ExceptionTemplates.ExternalTableCannotContainIndexes);
                }

                // validate that the external table does not contain any physical partitions
                if (this.PhysicalPartitions.Count > 0)
                {
                    throw new SmoException(ExceptionTemplates.ExternalTableCannotContainPhysicalPartitions);
                }
            }
        }

        /// <summary>
        /// Validate Indexes defined on this table before table creation or scripting
        /// </summary>
        private void ValidateIndexes()
        {
            // Rule:
            // #1 - Hash indexes can be created on memory optimized tables only and memory optimized tables can only have hash or range indexes
            // #2 - SQL DW distributed tables can have clustered column store, heap or clustered/non-clustered index

            // check if this table is memory optimized
            bool isMemoryOptimized = this.CheckIsMemoryOptimizedTable();

            // check if this table is a SQL DW table
            bool isSqlDwTable = this.CheckIsSqlDwTable();

            foreach (Index idx in this.Indexes)
            {
                // If IndexType is not defined, infer it
                IndexType indexType;
                try
                {
                    indexType = idx.IndexType;
                }
                catch
                {
                    indexType = idx.InferredIndexType;
                }

                // Throw with first instance of a hash index on a regular sql table
                if (indexType == IndexType.NonClusteredHashIndex && !isMemoryOptimized)
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.HashIndexTableDependency);
                }

                // Throw with first instance of a non-hash or non-range index on a memory optimized table
                if (isMemoryOptimized &&
                    (indexType != IndexType.NonClusteredHashIndex &&
                     indexType != IndexType.NonClusteredIndex &&
                     indexType != IndexType.ClusteredColumnStoreIndex))
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.TableMemoryOptimizedIndexDependency);
                }

                if (isSqlDwTable &&
                    indexType != IndexType.HeapIndex &&
                    indexType != IndexType.ClusteredColumnStoreIndex &&
                    indexType != IndexType.ClusteredIndex &&
                    indexType != IndexType.NonClusteredIndex)
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.TableSqlDwIndexRestrictions);
                }
            }
        }

        /// <summary>
        /// Validates external table required property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="sp">Scripting preferences.</param>
        private void ValidateExternalTableRequiredStringProperty(string propertyName, ScriptingPreferences sp)
        {
            if (IsSupportedProperty(propertyName, sp))
            {
                Property prop = this.GetPropertyOptional(propertyName);

                if (prop.IsNull)
                {
                    throw new ArgumentNullException(propertyName);
                }
                else if (string.IsNullOrEmpty(prop.Value.ToString()))
                {
                    throw new PropertyNotSetException(propertyName);
                }
                else
                {
                    // if the property is set but the table is not an external table, throw an exception
                    if (!CheckIsExternalTable())
                    {
                        throw new SmoException(ExceptionTemplates.PropertyOnlySupportedForExternalTable(propertyName));
                    }
                }
            }
        }

        /// <summary>
        /// Validates if temporal system time PERIOD is set-up properly
        /// </summary>
        private void ValidateSystemTimeTemporal()
        {
            // go through the columns and check two columns that are marked as
            // start and end columns for PERIOD.
            Column startCol;
            Column endCol;

            if (!m_systemTimePeriodInfo.m_MarkedForCreate)
            {
                return;
            }

            if ((startCol = this.Columns[m_systemTimePeriodInfo.m_StartColumnName]) == null)
            {
                throw new SmoException(ExceptionTemplates.MustProvideExistingColumn);
            }

            if ((endCol = this.Columns[m_systemTimePeriodInfo.m_EndColumnName]) == null)
            {
                throw new SmoException(ExceptionTemplates.MustProvideExistingColumn);
            }

            if (startCol == endCol)
            {
                throw new SmoException(ExceptionTemplates.PeriodMustHaveDifferentColumns);
            }

            if (startCol.DataType.SqlDataType != SqlDataType.DateTime2)
            {
                throw new SmoException(ExceptionTemplates.PeriodInvalidDataType);
            }

            if (endCol.DataType.SqlDataType != SqlDataType.DateTime2)
            {
                throw new SmoException(ExceptionTemplates.PeriodInvalidDataType);
            }

            if (this.State == SqlSmoState.Creating)
            {
                if (startCol.GetPropValueOptional<GeneratedAlwaysType>("GeneratedAlwaysType", GeneratedAlwaysType.None) != GeneratedAlwaysType.AsRowStart)
                {
                    throw new SmoException(ExceptionTemplates.PeriodStartColumnMustBeGeneratedAlways);
                }

                if (endCol.GetPropValueOptional<GeneratedAlwaysType>("GeneratedAlwaysType", GeneratedAlwaysType.None) != GeneratedAlwaysType.AsRowEnd)
                {
                    throw new SmoException(ExceptionTemplates.PeriodEndColumnMustBeGeneratedAlways);
                }
            }
        }
    }
}
