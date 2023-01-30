// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

// History
//
// Fixed bug 410365: not scripted PAD_INDEX and FIILFACTOR.

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Index : ScriptNameObjectBase,
            Cmn::ICreatable, Cmn::IDroppable, Cmn::IDropIfExists, Cmn::IMarkForDrop, Cmn::IAlterable, Cmn::IRenamable,
            IExtendedProperties, IScriptable
    {
        bool m_bIsOnComputed;
        bool xmlOrSpatialIndex;
        bool isOnColumnWithAnsiPadding = false;

        const double m_boundingBoxDef = 0.0;
        const int m_cellsPerObjectDef = 0;
        const byte fillFactorDef = 0;
        const int m_minCompressionDelay = 0;
        const int m_maxCompressionDelay = 10080;
        const int m_hkMinCompressionDelay = 60;

        internal Index(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_IndexedColumns = null;
            m_IndexedXmlPaths = null;
            m_IndexedXmlPathNamespaces = null;
            m_ExtendedProperties = null;
            m_PartitionSchemeParameters = null;
        }

        /// <summary>
        /// Returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "Index";
            }
        }
        // To Add Ansi Padding in clustered index based on Table Ansi Padding.
        internal bool? IsParentBeingScriptedWithANSIPaddingON;

        internal override object GetPropertyDefaultValue(string propname)
        {
            if (propname == "CompressionDelay")
            {
                return 0;
            }

            return base.GetPropertyDefaultValue(propname);
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Deploy | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                if (IsDesignMode && GetIsSystemNamed() && this.State == SqlSmoState.Creating)
                {
                    return null;
                }
                return base.Name;
            }
            set
            {
                base.Name = value;
                if (ParentColl != null)
                {
                    SetIsSystemNamed(false);
                }
            }
        }

        [SfcProperty(SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public System.Boolean IsSystemNamed
        {
            get
            {
                if (ParentColl != null && IsDesignMode && this.State != SqlSmoState.Existing)
                {
                    throw new PropertyNotSetException("IsSystemNamed");
                }
                return (System.Boolean)this.Properties.GetValueWithNullReplacement("IsSystemNamed");
            }
        }
        private IndexedColumnCollection m_IndexedColumns = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(IndexedColumn), SfcObjectFlags.Design | SfcObjectFlags.Deploy | SfcObjectFlags.NaturalOrder)]
        public IndexedColumnCollection IndexedColumns
        {
            get
            {
                CheckObjectState();
                if (null == m_IndexedColumns)
                {
                    m_IndexedColumns = new IndexedColumnCollection(this);

                    // if the index exists don't allow changes to its columns
                    if (State == SqlSmoState.Existing)
                    {
                        m_IndexedColumns.LockCollection(ExceptionTemplates.ReasonObjectAlreadyCreated(UrnSuffix));
                    }
                }
                return m_IndexedColumns;
            }
        }

        /// <summary>
        /// Collection of IndexedXMLPath objects that represent all the paths
        /// that are indexed by the Selective XML Index.
        /// </summary>
        private IndexedXmlPathCollection m_IndexedXmlPaths = null;

        /// <summary>
        /// Collection of IndexedXMLPath objects that represent all the paths
        /// that are indexed by the Selective XML Index.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(IndexedXmlPath), SfcObjectFlags.Design | SfcObjectFlags.Deploy | SfcObjectFlags.NaturalOrder)]
        public IndexedXmlPathCollection IndexedXmlPaths
        {
            get
            {

                // SXI is not supported on SQL Server Denali RTM, only after SP1
                //
                Version minSxiVersion = new Version(11, 0, 2813);
                Version currentVersion = new Version(this.ServerVersion.Major, this.ServerVersion.Minor, this.ServerVersion.BuildNumber);
                if (currentVersion < minSxiVersion)
                    {
                       throw new UnknownPropertyException(ExceptionTemplates.PropertySupportedOnlyOn110SP1("IndexedXmlPaths"));
                    }

                CheckObjectState();
                if (null == m_IndexedXmlPaths)
                {
                    m_IndexedXmlPaths = new IndexedXmlPathCollection(this);
                }
                return m_IndexedXmlPaths;
            }
        }

        /// <summary>
        /// Collection of IndexedXMLPath objects that represent all the paths
        /// that are indexed by the Selective XML Index.
        /// </summary>
        private IndexedXmlPathNamespaceCollection m_IndexedXmlPathNamespaces = null;

        /// <summary>
        /// Collection of IndexedXMLPath objects that represent all the paths
        /// that are indexed by the Selective XML Index.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(IndexedXmlPathNamespace), SfcObjectFlags.Design | SfcObjectFlags.Deploy | SfcObjectFlags.NaturalOrder)]
        public IndexedXmlPathNamespaceCollection IndexedXmlPathNamespaces
        {
            get
            {
                // SXI is not supported on SQL Server Denali RTM, only after SP1
                //
                Version minSxiVersion = new Version(11, 0, 2813);
                Version currentVersion = new Version(this.ServerVersion.Major, this.ServerVersion.Minor, this.ServerVersion.BuildNumber);
                if (currentVersion < minSxiVersion)
                    {
                       throw new UnknownPropertyException(ExceptionTemplates.PropertySupportedOnlyOn110SP1("IndexedXmlPathNamespaces"));
                    }

                CheckObjectState();
                if (null == m_IndexedXmlPathNamespaces)
                {
                    m_IndexedXmlPathNamespaces = new IndexedXmlPathNamespaceCollection(this);
                }
                return m_IndexedXmlPathNamespaces;
            }
        }


        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ExtendedProperty))]
        public ExtendedPropertyCollection ExtendedProperties
        {
            get
            {
                ThrowIfBelowVersion80();
                CheckObjectState();
                if (null == m_ExtendedProperties)
                {
                    m_ExtendedProperties = new ExtendedPropertyCollection(this);
                }
                return m_ExtendedProperties;
            }
        }

        private PartitionSchemeParameterCollection m_PartitionSchemeParameters;
        /// <summary>
        /// Specifies the columns that define the input parameters for the Partition Scheme.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(PartitionSchemeParameter))]
        [SfcInvalidForTypeAttribute(typeof(UserDefinedFunction))]
        public PartitionSchemeParameterCollection PartitionSchemeParameters
        {
            get
            {
                if (ParentColl.ParentInstance is UserDefinedTableType)
                {
                    return null;
                }

                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PartitionSchemeParameter));
                if (null == m_PartitionSchemeParameters)
                {
                    m_PartitionSchemeParameters = new PartitionSchemeParameterCollection(this);
                }
                return m_PartitionSchemeParameters;
            }
        }

        /// <summary>
        /// Collection class instance for the PhysicalPartitions of the table
        /// </summary>
        private PhysicalPartitionCollection m_PhysicalPartitions;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.OneToAny, typeof(PhysicalPartition))]
        public PhysicalPartitionCollection PhysicalPartitions
        {
            get
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(PhysicalPartition));
                if (this.Parent is UserDefinedTableType)
                {
                    return null;
                }

                if (null == m_PhysicalPartitions)
                {
                    m_PhysicalPartitions = new PhysicalPartitionCollection(this);
                }
                return m_PhysicalPartitions;
            }
        }

        /// <summary>
        /// Compression Delay valid only for columnstore indexes.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Deploy | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone)]
        public int CompressionDelay
        {
            get
            {
                CheckObjectState();
                this.ThrowIfPropertyNotSupported("CompressionDelay");
                if (this.InferredIndexType != Smo.IndexType.ClusteredColumnStoreIndex &&
                    this.InferredIndexType != Smo.IndexType.NonClusteredColumnStoreIndex)
                {
                    throw new InvalidSmoOperationException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.PropertyValidOnlyForColumnStoreIndexes, "CompressionDelay"));
                }
                return (int)this.Properties.GetValueWithNullReplacement("CompressionDelay", false, true);
            }

            set
            {
                CheckObjectState();
                this.ThrowIfPropertyNotSupported("CompressionDelay");
                if (this.InferredIndexType != Smo.IndexType.ClusteredColumnStoreIndex &&
                    this.InferredIndexType != Smo.IndexType.NonClusteredColumnStoreIndex)
                {
                    throw new InvalidSmoOperationException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.PropertyValidOnlyForColumnStoreIndexes, "CompressionDelay"));
                }

                if (value >= m_minCompressionDelay &&
                    value <= m_maxCompressionDelay &&
                    (!IsMemoryOptimizedIndex || (value >= m_hkMinCompressionDelay || value == 0)))
                {
                    this.Properties.SetValueWithConsistencyCheck("CompressionDelay", value);
                }
                else
                {
                    throw new InvalidSmoOperationException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.InvalidCompressionDelayValue,
                        value, m_minCompressionDelay, m_maxCompressionDelay, m_hkMinCompressionDelay, 0 /*Special value allowed in HKCS*/));
                }
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped
            base.MarkDropped();

            if (null != m_IndexedColumns)
            {
                m_IndexedColumns.MarkAllDropped();
            }

            if (null != m_IndexedXmlPaths)
            {
                m_IndexedXmlPaths.MarkAllDropped();
            }

            if (null != m_IndexedXmlPathNamespaces)
            {
                m_IndexedXmlPathNamespaces.MarkAllDropped();
            }

            if (null != m_ExtendedProperties)
            {
                m_ExtendedProperties.MarkAllDropped();
            }

            if (null != m_PartitionSchemeParameters)
            {
                m_PartitionSchemeParameters.MarkAllDropped();
            }
        }

        internal bool IsDirty(string property)
        {
            return this.Properties.IsDirty(this.Properties.LookupID(property, PropertyAccessPurpose.Read));
        }

        //In case of PrimaryKey or Unique constraint we really don't need
        //Name as compulsory property. We will check this Name constraint
        //on scripting time if Index category is not either PrimaryKey
        //or Unique constraint.
        internal override void UpdateObjectState()
        {
            if (this.State == SqlSmoState.Pending && null != this.ParentColl && (!key.IsNull || IsDesignMode))
            {
                SetState(SqlSmoState.Creating);
                if (key.IsNull)
                {
                    AutoGenerateName();
                }
                else
                {
                    SetIsSystemNamed(false);
                }
            }
        }

        public void Create()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            base.CreateImpl();

            //a new statistic will be automaticaly created
            ((TableViewBase)ParentColl.ParentInstance).Statistics.MarkOutOfSync();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }

            ScriptDdl(queries, sp, true);
        }

        protected override void PostCreate()
        {
            Table parentTable = Parent as Table;

            if (parentTable != null)
            {
                bool isNode = parentTable.GetPropValueIfSupported("IsNode", false);
                bool isEdge = parentTable.GetPropValueIfSupported("IsEdge", false);

                if (isNode || isEdge)
                {
                    // Get the latest columns, these will be different from what the user specified
                    // to the object before it was created. Node and edge tables will have auto created
                    // columns that are not part of the index definition when the index is created.
                    //
                    m_IndexedColumns.Refresh();

                    // If this index was created on a pseudo column, the indexed columns collection no longer
                    // can reflect this. The pseudo columns are not actual columns and should not show up on
                    // objects that are created against the server.
                    //
                    List<IndexedColumn> columnsToRemove = new List<IndexedColumn>();

                    foreach (IndexedColumn col in m_IndexedColumns)
                    {
                        if (IsGraphPseudoColumn(col.Name))
                        {
                            columnsToRemove.Add(col);
                        }
                    }

                    // The columns that will be removed here are the pseudo columns '$node_id', '$edge_id',
                    // '$from_id', and '$to_id' if they were specified by the user to create this index. This is
                    // safe to do because this happens before the collection is locked below. These pseudo columns
                    // don't actually exist, they are just names similar to '$identity' and should not participate
                    // in the columns displayed by the index object. If we did expose these back to the user after
                    // the index was created the user could try to get metadata about these columns and an error
                    // would be raised by SMO indicating that the column does not exist.
                    foreach (IndexedColumn col in columnsToRemove)
                    {
                        m_IndexedColumns.Remove(col);
                    }
                }
            }

            m_IndexedColumns.LockCollection(ExceptionTemplates.ReasonObjectAlreadyCreated(UrnSuffix));

            if (!this.ExecutionManager.Recording)
            {
                if (IsDesignMode && key.IsNull && GetIsSystemNamed())
                {
                    AutoGenerateName();
                }
            }
        }

        internal override void ScriptDdl(StringCollection queries, ScriptingPreferences sp)
        {
            if (sp.IncludeScripts.DatabaseContext)
            {
                AddDatabaseContext(queries, sp);
            }

            ScriptDdl(queries, sp, false);
        }

        internal void ScriptDdl(StringCollection queries, ScriptingPreferences sp, bool notEmbedded)
        {
            ScriptDdl(queries, sp, notEmbedded, false);
        }

        internal void ScriptDdl(StringCollection queries, ScriptingPreferences sp, bool notEmbedded, bool createStatement)
        {
            CheckObjectState();
            InitializeKeepDirtyValues();

            StringBuilder sb = new StringBuilder();
            StringCollection idx_strings = new StringCollection();

            string sDDL = GetDDL(sp, notEmbedded, createStatement);
            if (sDDL.Length > 0)
            {

                ScriptSchemaObjectBase parent = (ScriptSchemaObjectBase)ParentColl.ParentInstance;
                //if the index is on a view or on a computed column or on an xml column
                //the following set options must have the right value
                //but only if it is not part of CREATE TABLE or CREATE TYPE
                if (!createStatement)
                {
                    AddSetOptionsForIndex(idx_strings);

                    //StringCollectiong to single string
                    if (idx_strings.Count > 0)
                    {
                        foreach (string s in idx_strings)
                        {
                            sb.Append(s);
                            sb.Append(sp.NewLine);
                        }
                        idx_strings.Clear();

                        queries.Add(sb.ToString());
                    }
                }

                queries.Add(sDDL);

                // will script calls to sp_indexoption only if we are executing directly
                if (sp.ScriptForCreateDrop && (sp.TargetServerVersionInternal == SqlServerVersionInternal.Version70 ||
                    sp.TargetServerVersionInternal == SqlServerVersionInternal.Version80))
                {
                    ScriptSpIndexoptions(queries, sp);
                }
            }

            // Index can not be created in a disabled state.Disabling of index is valid for SQL Server 2005 onwards.
            if (ScriptConstraintWithName(sp) && this.ServerVersion.Major > 8 && (sp.TargetServerVersionInternal > SqlServerVersionInternal.Version80) && !dropExistingIndex)
            {
                bool bCheck = (this.properties.Get("IsDisabled").Value != null) ? (bool)this.properties.Get("IsDisabled").Value : false;
                if (bCheck)
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER INDEX [{0}] ON {1} DISABLE",
                            SqlBraket(this.Name), this.Parent.FullQualifiedName));
                }
            }
        }

        IndexKeyType GetIndexKeyType()
        {
            bool fKeyExists = (null != Properties.Get("IndexKeyType").Value);
            IndexKeyType iKey = IndexKeyType.None;
            if (fKeyExists)
            {
                iKey = (IndexKeyType)Properties["IndexKeyType"].Value;
            }
            return iKey;
        }


        private string GetDDL(ScriptingPreferences sp, bool creating)
        {
            return GetDDL(sp, creating, false);
        }

        /// <summary>
        /// This method is called for Table, and Index creation.
        /// </summary>
        /// <param name="so">Scriptingoptions</param>
        /// <param name="creating">This flag is set to true when an index is created,
        /// or a table constraint is added. For table creation it is false</param>
        /// <param name="tableCreate">Set to true for table creation with inline constraints\Indexes.</param>
        /// <returns>A script representing the requested object (Index, table, or constraint portion.</returns>
        private string GetDDL(ScriptingPreferences sp, bool creating, bool tableCreate)
        {
            IndexScripter scripter = IndexScripter.GetIndexScripterForCreate(this, sp);
            if (scripter != null)
            {
                if (scripter is ConstraintScripter ||
                    scripter is RangeIndexScripter ||
                    scripter is HashIndexScripter ||
                    scripter is ClusteredColumnstoreIndexScripter ||
                    scripter is ClusteredRegularIndexScripter ||
                    scripter is UserDefinedTableTypeIndexScripter)
                {
                    scripter.TableCreate = tableCreate;
                }

                return scripter.GetCreateScript();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Finds and gets IndexType
        /// </summary>
        /// <returns></returns>
        internal IndexType InferredIndexType
        {
            get
            {
                IndexType returnType;
                if (!this.GetPropValueOptional<IndexType>("IndexType").HasValue && (this.State == SqlSmoState.Creating || this.IsDesignMode))
                {
                    //IndexType not specified we need to infer it

                    IndexKeyType indexKeyType = this.GetPropValueOptional<IndexKeyType>("IndexKeyType", IndexKeyType.None);
                    bool? indexIsClustered = this.GetPropValueOptional<bool>("IsClustered");

                    //Primary key without any specification for clusetering is clustered
                    if (!indexIsClustered.HasValue && indexKeyType == IndexKeyType.DriPrimaryKey)
                    {
                        indexIsClustered = true;
                    }

                    if (indexIsClustered.HasValue && indexIsClustered.Value == true)
                    {
                        returnType = IndexType.ClusteredIndex;
                    }
                    else if (this.HasXmlColumn(false))
                    {
                        if (!string.IsNullOrEmpty(this.GetPropValueOptional("ParentXmlIndex", string.Empty).ToString()))
                        {
                            returnType = IndexType.SecondaryXmlIndex;
                        }
                        else
                        {
                            returnType = IndexType.PrimaryXmlIndex;
                        }
                    }
                    else if (this.HasSpatialColumn(false))
                    {
                        returnType = IndexType.SpatialIndex;
                    }
                    else
                    {
                        returnType = IndexType.NonClusteredIndex;
                    }
                }
                else
                {
                    returnType = (IndexType)this.Properties["IndexType"].Value;
                }

                return returnType;
            }
        }

        private bool IsSpatialColumn(Column colBase)
        {
            return (colBase.DataType.SqlDataType == SqlDataType.Geometry
                || colBase.DataType.SqlDataType == SqlDataType.Geography);
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
        /// This method just check whether xml compression related code is required or not.
        /// </summary>
        /// <param name="isAlter"></param>
        /// <returns></returns>
        private bool IsXmlCompressionCodeRequired(bool isAlter)
        {
            if (!this.IsSupportedProperty(nameof(HasXmlCompressedPartitions)))
            {
                return false;
            }

            Diagnostics.TraceHelper.Assert((this.State == SqlSmoState.Existing) || (this.State == SqlSmoState.Creating));

            if (this.State == SqlSmoState.Creating)
            {
                return false;
            }

            if (this.HasXmlCompressedPartitions)
            {
                return true;
            }

            if ((null != m_PhysicalPartitions) &&
                (PhysicalPartitions.IsXmlCollectionDirty()))
            {
                return PhysicalPartitions.IsXmlCompressionCodeRequired(isAlter);
            }

            return false;
        }

        internal bool HasXmlColumn(bool throwIfNotSet)
        {
            // XML indexes supported only on version 9.0
            if (this.ServerVersion.Major < 9 || this.DatabaseEngineType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.Standalone)
            {
                return false;
            }

            // if the object already has a property set, we can just use its value
            Nullable<IndexType> o = this.GetPropValueOptional<IndexType>("IndexType");
            if (o.HasValue)
            {
                return (o.Value == IndexType.PrimaryXmlIndex || o.Value == IndexType.SecondaryXmlIndex);
            }

            return CheckColumnsDataType(throwIfNotSet, new CheckColumnDataType(IsColumnXmlDataType));
        }

        internal bool HasSpatialColumn(bool throwIfNotSet)
        {
            // Spatial indexes supported only on version 10.0
            if (this.ServerVersion.Major < 10 || this.DatabaseEngineType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.Standalone)
            {
                return false;
            }

            // if the object already has a property set, we can just use its value
            Nullable<IndexType> o = this.GetPropValueOptional<IndexType>("IndexType");
            if (o.HasValue)
            {
                return (o.Value == IndexType.SpatialIndex);
            }

            return CheckColumnsDataType(throwIfNotSet, new CheckColumnDataType(IsColumnSpatialDataType));
        }

        private bool CheckColumnsDataType(bool throwIfNotSet, CheckColumnDataType checkDataType)
        {
            ColumnCollection columns = null;
            TableViewTableTypeBase parenttv = ParentColl.ParentInstance as TableViewTableTypeBase;
            ScriptSchemaObjectBase parentobj = null;
            if (null != parenttv)
            {
                parentobj = parenttv;
                columns = parenttv.Columns;

                // for a view that's not yet created, we have no way of figuring out
                // the data type of its columns, so we'll just assume that it does not
                // have an Spatial column, and let it fail at creation if so
                if (parentobj is View && parentobj.State == SqlSmoState.Creating)
                {
                    return false;
                }
            }

            UserDefinedFunction parentf = ParentColl.ParentInstance as UserDefinedFunction;
            if (null != parentf)
            {
                parentobj = parentf;
                columns = parentf.Columns;
            }

            foreach (IndexedColumn idxcol in this.IndexedColumns)
            {
                object isIncluded = idxcol.GetPropValueOptional("IsIncluded");

                if (isIncluded != null && ((bool)isIncluded))
                {
                    // Skip the included columns
                    continue;
                }

                // Graph index pseudo columns should be excluded in these checks.
                // Graph pseudo columns are not real columns and have no data types.
                //
                if (IsGraphPseudoColumn(idxcol.Name))
                {
                    continue;
                }

                Column col = columns[idxcol.Name];

                if (null == col)
                {
                    throw new SmoException(ExceptionTemplates.ObjectRefsNonexCol(UrnSuffix, Name, parentobj.FullQualifiedName + ".[" + SqlStringBraket(idxcol.Name) + "]"));
                }

                string dataType;
                if (throwIfNotSet)
                {
                    dataType = col.GetPropValue("DataType") as string;
                }
                else
                {
                    dataType = col.GetPropValueOptional("DataType") as string;
                }

                if (checkDataType(dataType))
                {
                    return true;
                }
            }

            return false;
        }

        private delegate bool CheckColumnDataType(string datatype);

        private static bool IsColumnXmlDataType(string dataType)
        {
            return null != dataType && dataType.ToLower(SmoApplication.DefaultCulture) == "xml";
        }

        private static bool IsColumnSpatialDataType(string dataType)
        {
            return null != dataType && (dataType.ToLower(SmoApplication.DefaultCulture) == "geometry" || dataType.ToLower(SmoApplication.DefaultCulture) == "geography");
        }

        /// <summary>
        /// This method determines if the column name is a graph pseudo column name.
        /// A pseudo column is an alias for a column that a user cannot specify as a normal
        /// name in a create index statement because they start with the symbol '$'.
        /// </summary>
        /// <param name="name">The column name.</param>
        /// <returns>TRUE if the column name is a pseudo column name.</returns>
        private static bool IsGraphPseudoColumn(string name)
        {
            return name == "$node_id" ||
                name == "$edge_id" ||
                name == "$from_id" ||
                name == "$to_id";
        }

        public void Drop()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            base.DropImpl();

            //the corresponding statistic will be automaticaly droped, so we have to reflect that
            ((TableViewBase)ParentColl.ParentInstance).Statistics.RemoveObject(new SimpleObjectKey(this.Name));
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
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }


            CheckObjectState();
            IndexScripter scripter = IndexScripter.GetIndexScripterForDrop(this, sp);
            queries.Add(scripter.GetDropScript());
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            base.MarkForDropImpl(dropOnAlter);
        }

        /// <summary>
        /// Raises FailedOperationException if index is SXI or SSXI.
        /// </summary>
        /// <param name="checkPrimarySXI">Should exception be raised if index is SXI</param>
        /// <param name="checkSecondarySXI">Should exception be raised if index is SSXI</param>
        /// <param name="operation">Operation which raised exception</param>
        /// <param name="reason">Reason for exception</param>
        private void CheckUnsupportedSXI(bool checkPrimarySXI, bool checkSecondarySXI, string operation, string reason)
        {
            Nullable<IndexType> o = this.GetPropValueOptional<IndexType>("IndexType");
            if (o.HasValue)
            {
                if ((checkPrimarySXI && o.Value == IndexType.SelectiveXmlIndex) ||
                    (checkSecondarySXI && o.Value == IndexType.SecondarySelectiveXmlIndex))
                {
                    throw new FailedOperationException(operation, this, null, reason);
                }
            }
        }

        /// <summary>
        /// Changes the index based on supplied index properties
        /// </summary>
        public void Alter()
        {

            CheckUnsupportedSXI(false, true, ExceptionTemplates.Alter, ExceptionTemplates.SecondarySelectiveXmlIndexModify);

            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            base.AlterImpl();
        }

        /// <summary>
        /// Performs an index operation using the current property settings of the object.
        /// </summary>
        /// <param name="operation"></param>
        public void Alter(IndexOperation operation)
        {
            CheckUnsupportedSXI(false, true, ExceptionTemplates.Alter, ExceptionTemplates.SecondarySelectiveXmlIndexModify);

            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);

            switch (operation)
            {
                case IndexOperation.Rebuild:
                    Rebuild();
                    break;
                case IndexOperation.Reorganize:
                    ThrowIfBelowVersion80();
                    Reorganize();
                    break;
                case IndexOperation.Disable:
                    ThrowIfBelowVersion90();
                    Disable();
                    break;
            }
        }

        /// <summary>
        /// Changes all indexes based on supplied index properties.
        /// </summary>
        public void AlterAllIndexes()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }


            ThrowIfBelowVersion90();
            try
            {
                affectAllIndexes = true;
                Alter();
            }
            finally
            {
                affectAllIndexes = false;
            }

        }

        // tells the scripter to alter all the indexes like this one
        bool affectAllIndexes = false;

        private void ScriptSpIndexoptions(StringCollection queries, ScriptingPreferences sp)
        {
            ScriptSchemaObjectBase parent = (ScriptSchemaObjectBase)ParentColl.ParentInstance;

            Property prop = Properties.Get("DisallowRowLocks");
            if (null != prop.Value && (prop.Dirty || sp.ScriptForCreateDrop))
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                                    "EXEC dbo.sp_indexoption @IndexNamePattern = N'{0}.[{1}]', @OptionName = 'disallowrowlocks', @OptionValue = '{2}'",
                                    SqlString(parent.FullQualifiedName),
                                    SqlStringBraket(this.Name),
                                    (bool)prop.Value ? "on" : "off"));
            }

            prop = Properties.Get("DisallowPageLocks");
            if (null != prop.Value && (prop.Dirty || sp.ScriptForCreateDrop))
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_indexoption @IndexNamePattern = N'{0}.[{1}]', @OptionName = 'disallowpagelocks', @OptionValue = '{2}'",
                                    SqlString(parent.FullQualifiedName),
                                    SqlStringBraket(this.Name),
                                    (bool)prop.Value ? "on" : "off"));
            }
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }


            // Alter is only supported with the HK hash index.
            //
            if (this.IsMemoryOptimizedIndex)
            {
                if (!IsObjectDirty())
                {
                    return;
                }

                if (this.GetPropValueOptional<IndexType>("IndexType").Value != IndexType.NonClusteredHashIndex)
                {
                    throw new InvalidSmoOperationException(ExceptionTemplates.OnlyHashIndexIsSupportedInAlter);
                }
            }

            if (sp.TargetServerVersionInternal <= SqlServerVersionInternal.Version80)
            {
                ScriptSpIndexoptions(alterQuery, sp);
            }
            else
            {
                IndexScripter scripter = IndexScripter.GetIndexScripterForAlter(this, sp);
                string setIndexOptionsScript = scripter.GetAlterScript90(affectAllIndexes);
                if (!string.IsNullOrEmpty(setIndexOptionsScript))
                {
                    alterQuery.Add(setIndexOptionsScript);
                }
            }
        }

        /// <summary>
        /// Hint parameter for Rebuild() or Reorganize() function. If this value is -1 then it means
        /// rebuild or reorganize all the partition
        /// </summary>
        private int optimizePartitionNumber = -1;

        /// <summary>
        /// Rebuild particular partition
        /// </summary>
        /// <param name="partitionNumber"></param>
        public void Rebuild(int partitionNumber)
        {
            optimizePartitionNumber = partitionNumber;
            try
            {
                if (ParentColl.ParentInstance is UserDefinedTableType)
                {
                    throw new FailedOperationException(ExceptionTemplates.RebuildIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
                }

                CheckObjectState(true);
                if (partitionNumber != -1)
                {
                    this.ThrowIfNotSupported(typeof(PhysicalPartition));
                }
                RebuildImpl(false);
            }
            finally
            {
                optimizePartitionNumber = -1;
            }
        }

        /// <summary>
        /// Re-creates the index based on the current property settings.
        /// </summary>
        public void Rebuild()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.RebuildIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            RebuildImpl(false);
        }

        /// <summary>
        /// Resume the resumable index that is in the PAUSED status.
        /// </summary>
        public void Resume()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.ResumeIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            ResumeImpl();
        }

        private void ResumeImpl()
        {
            ThrowIfPropertyNotSupported("ResumableOperationState");

            IndexScripter scripter;
            try
            {
                StringCollection queries = new StringCollection();

                ScriptingPreferences sp = new ScriptingPreferences(this);
                sp.ScriptForCreateDrop = true;

                this.GetContextDB().AddUseDb(queries, sp);

                scripter = IndexScripter.GetIndexScripter(this, sp);
                queries.Add(scripter.GetResumeScript());

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ResumeIndexes, this, e);
            }
        }

        /// <summary>
        /// Abort the resumable index that is in the PAUSED status.
        /// </summary>
        public void Abort()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.AbortIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            AbortImpl();
        }

        private void AbortImpl()
        {
            ThrowIfPropertyNotSupported("ResumableOperationState");

            IndexScripter scripter;
            try
            {
                StringCollection queries = new StringCollection();

                ScriptingPreferences sp = new ScriptingPreferences(this);
                sp.ScriptForCreateDrop = true;

                this.GetContextDB().AddUseDb(queries, sp);

                scripter = IndexScripter.GetIndexScripter(this, sp);
                queries.Add(scripter.GetAbortOrPauseScript(isAbort: true));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AbortIndexes, this, e);
            }
        }

        /// <summary>
        /// Pause the resumable index that is running.
        /// </summary>
        public void Pause()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.PauseIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            PauseImpl();
        }

        private void PauseImpl()
        {
            ThrowIfPropertyNotSupported("ResumableOperationState");

            IndexScripter scripter;
            try
            {
                StringCollection queries = new StringCollection();

                ScriptingPreferences sp = new ScriptingPreferences(this);
                sp.ScriptForCreateDrop = true;

                this.GetContextDB().AddUseDb(queries, sp);

                scripter = IndexScripter.GetIndexScripter(this, sp);
                queries.Add(scripter.GetAbortOrPauseScript(isAbort: false));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.PauseIndexes, this, e);
            }
        }

        /// <summary>
        /// Re-creates all indexes based on the current property settings.
        /// </summary>
        public void RebuildAllIndexes()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.RebuildIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            RebuildImpl(true);
        }

        /// <summary>
        /// Upgrades a Clustered Rowstore index to Clustered Columnstore Index
        /// </summary>
        public void UpgradeToClusteredColumnStoreIndex()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.RebuildIndexes, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }
            if (this.InferredIndexType != IndexType.ClusteredIndex)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, null, ExceptionTemplates.InvalidUpgradeToCCIIndexType);
            }

            try
            {
                CheckObjectState(true);

                ThrowIfBelowVersion120();
                UpgradeToCCI120Impl();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RebuildIndexes, this, e);
            }

        }

        internal bool AddSetOptionsForIndex(StringCollection queries)
        {
            //if the index is on a view or on a computed column or on an xml column
            //the following set options must have the right value
            if (((m_bIsOnComputed || xmlOrSpatialIndex) && IndexKeyType.None == GetIndexKeyType()) || this.Parent is View)
            {
                // we need to set those options in order to create view indexes
                queries.Add("SET ARITHABORT ON");
                queries.Add("SET CONCAT_NULL_YIELDS_NULL ON");
                queries.Add("SET QUOTED_IDENTIFIER ON");
                queries.Add("SET ANSI_NULLS ON");
                queries.Add("SET ANSI_PADDING ON");
                queries.Add("SET ANSI_WARNINGS ON");
                queries.Add("SET NUMERIC_ROUNDABORT OFF");
                return true;
            }
            else if ((m_bIsOnComputed && IndexKeyType.None != GetIndexKeyType()) || this.isOnColumnWithAnsiPadding
                || IsParentBeingScriptedWithANSIPaddingON == true)
            {
                //if primary key or unique is based on computed column
                queries.Add("SET ANSI_PADDING ON");
                // indexAnsiPaddingSetting is set in TableBase.cs based on Table's ANSI padding
                // Unsetting the value here as the state is persistent with the Index
                this.IsParentBeingScriptedWithANSIPaddingON = null;
            }
            return false;
        }

        private void RebuildImpl(bool allIndexes)
        {
            IndexScripter scripter;
            try
            {
                StringCollection queries = new StringCollection();
                ScriptSchemaObjectBase parent = (ScriptSchemaObjectBase)ParentColl.ParentInstance;
                ScriptingPreferences sp = new ScriptingPreferences(this);
                sp.ScriptForCreateDrop = true;

                this.GetContextDB().AddUseDb(queries, sp);

                if (ServerVersion.Major < 9 ||
                    (this.Parent is View && ((View)this.Parent).Parent.CompatibilityLevel < CompatibilityLevel.Version90))
                {
                    TableViewTableTypeBase tvp = this.Parent as TableViewTableTypeBase;
                    if (null != tvp)
                    {
                        foreach (Index idx in tvp.Indexes)
                        {
                            if (idx.AddSetOptionsForIndex(queries))
                            {
                                break;
                            }
                        }
                    }
                }

                scripter = IndexScripter.GetIndexScripter(this, sp);
                queries.Add(scripter.GetRebuildScript(allIndexes, optimizePartitionNumber));

                this.ExecutionManager.ExecuteNonQuery(queries);

                // Allow listeners (e.g. Object Explorer menu items and node hierarchy) to be notified that
                // the rebuild happened. This can be used to enable the "Disable" menu item on the index,
                // for example.
                this.GenerateAlterEvent();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Rebuild, this, e);
            }

            if ((this.ServerVersion.Major >= 9)
                && (null != m_PhysicalPartitions)
                && (scripter is RegularIndexScripter
                || (scripter is SpatialIndexScripter && this.ServerVersion.Major >= 11)))
            {
                if (!this.ExecutionManager.Recording)
                {
                    if (optimizePartitionNumber == -1)
                    {
                        PhysicalPartitions.Reset();
                    }
                    else
                    {
                        PhysicalPartitions.Reset(optimizePartitionNumber);
                    }
                }
            }
        }

        /// <summary>
        /// Rename the index
        /// </summary>
        /// <param name="newname"></param>
        public void Rename(string newname)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Rename, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            Table.CheckTableName(newname);
            base.RenameImpl(newname);
        }

        /// <summary>
        /// Generates script for the Rename operation.
        /// </summary>
        /// <param name="renameQuery"></param>
        /// <param name="so"></param>
        /// <param name="newName"></param>
        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            // the user is responsible to put the database in single user mode on 7.0 server
            AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC sp_rename N'{0}.{1}', N'{2}', N'INDEX'",
                SqlString(this.Parent.FullQualifiedName),
                SqlString(this.FullQualifiedName),
                SqlString(newName)));
        }

        /// <summary>
        /// Reorganizes the index based on the current property settings.
        /// </summary>
        public void Reorganize(int partitionNumber = -1)
        {
            try
            {
                if (partitionNumber != -1)
                {
                    this.ThrowIfNotSupported(typeof(PhysicalPartition));
                    optimizePartitionNumber = partitionNumber;
                }
                ReorganizeGeneralImpl(false);
            }
            finally
            {
                optimizePartitionNumber = -1;
            }
        }

        /// <summary>
        /// Reorganizes all indexes based on the current property settings.
        /// </summary>
        public void ReorganizeAllIndexes()
        {
            ReorganizeGeneralImpl(true);
        }

        private void ReorganizeGeneralImpl(bool allIndexes)
        {
            CheckUnsupportedSXI(true, true, ExceptionTemplates.Reorganize, ExceptionTemplates.SelectiveXmlIndexDoesNotSupportReorganize);

            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Reorganize, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            try
            {
                CheckObjectState(true);
                ThrowIfBelowVersion80();
                ScriptingPreferences sp = new ScriptingPreferences();
                sp.SetTargetServerInfo(this);

                if (this.ServerVersion.Major == 8 ||
                    sp.TargetServerVersionInternal == SqlServerVersionInternal.Version80 ||
                    sp.TargetServerVersionInternal == SqlServerVersionInternal.Version70)
                {
                    Reorganize80Impl(allIndexes);
                }
                else
                {
                    AlterIndexReorganizeImpl(allIndexes);
            }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Reorganize, this, e);
            }
        }

        private void Reorganize80Impl(bool allIndexes)
        {
            StringCollection queries = new StringCollection();
            queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            TableViewTableTypeBase parent = (TableViewTableTypeBase)ParentColl.ParentInstance;

            if (allIndexes)
            {
                foreach (Index idx in ((TableViewTableTypeBase)ParentColl.ParentInstance).Indexes)
                {
                    queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC INDEXDEFRAG( N'{0}', N'{1}', N'{2}' )",
                                                SqlString(GetDBName()),
                                                SqlString(parent.FullQualifiedName),
                                                SqlString(idx.Name)));
                }
            }
            else
            {
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC INDEXDEFRAG( N'{0}', N'{1}', N'{2}' )",
                                                SqlString(GetDBName()),
                                                SqlString(parent.FullQualifiedName),
                                                SqlString(this.Name)));
            }

            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        private void AlterIndexReorganizeImpl(bool allIndexes)
        {
            // Sawa V1 does not support ALTER INDEX REORGANIZE statements. It is supported only from Sterling V12
            //
            if (this.DatabaseEngineType == Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase &&
                this.ServerVersion.Major < 12)
            {
                throw new SmoException(ExceptionTemplates.PropertyNotSupportedForCloudVersion("REORGANIZE", this.ServerVersion.Major.ToString()));
            }

            StringCollection queries = new StringCollection();
            queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            ScriptSchemaObjectBase parent = (ScriptSchemaObjectBase)ParentColl.ParentInstance;

            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (allIndexes)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX ALL ON {0} REORGANIZE",
                                    parent.FullQualifiedName);
            }
            else
            {
				StringBuilder partitionOption = new StringBuilder(Globals.INIT_BUFFER_SIZE);
				if (optimizePartitionNumber != -1)
				{
					partitionOption.AppendFormat(SmoApplication.DefaultCulture, "PARTITION = {0}", optimizePartitionNumber);
				}
                statement.AppendFormat(SmoApplication.DefaultCulture, "ALTER INDEX [{0}] ON {1} REORGANIZE {2}",
                                    SqlBraket(this.Name), parent.FullQualifiedName, partitionOption.ToString());
            }

            StringBuilder withClause = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            int withCount = 0;

            ScriptAlterPropNonBag(CompactLargeObjects, "LOB_COMPACTION", this.ServerVersion, withClause, ref withCount);

            if (this.ServerVersion.Major > 12 &&
                (this.InferredIndexType == IndexType.ClusteredColumnStoreIndex ||
                this.InferredIndexType == IndexType.NonClusteredColumnStoreIndex))
            {
                ScriptAlterPropNonBag(CompressAllRowGroups, "COMPRESS_ALL_ROW_GROUPS", this.ServerVersion, withClause, ref withCount);
            }

            if (0 < withClause.Length)
            {
                statement.AppendFormat(SmoApplication.DefaultCulture, " WITH ( {0} )", withClause.ToString());
            }

            queries.Add(statement.ToString());

            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        private void UpgradeToCCI120Impl()
        {
            ScriptSchemaObjectBase parent = (ScriptSchemaObjectBase)ParentColl.ParentInstance;
            Index clusteredColumnstoreIndex = new Index(parent, this.Name);
            clusteredColumnstoreIndex.IndexType = IndexType.ClusteredColumnStoreIndex;
            clusteredColumnstoreIndex.dropExistingIndex = true;
            clusteredColumnstoreIndex.Create();
            ((TableViewBase)ParentColl.ParentInstance).Refresh();
        }

        private void ScriptAlterPropNonBag(bool propValue, string optname, ServerVersion serverVersion,
                                        StringBuilder statement, ref int optCount)
        {
            // Check if it is SqlVersion70 or SqlVersion80
            //
            if (serverVersion.Major == 7 || serverVersion.Major == 8)
            {
                if (propValue)
                {
                    if (0 < optCount++)
                    {
                        statement.Append(Globals.commaspace);
                    }

                    statement.Append(optname);
                }
            }
            else
            {
                if (0 < optCount++)
                {
                    statement.Append(Globals.commaspace);
                }

                statement.AppendFormat(SmoApplication.DefaultCulture, "{0} = {1}", optname, propValue ? "ON" : "OFF");
            }
        }


        /// <summary>
        /// Disables the index.
        /// </summary>
        public void Disable()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Disable, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            try
            {
                CheckObjectState(true);
                ThrowIfBelowVersion90();

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
                ScriptSchemaObjectBase parent = (ScriptSchemaObjectBase)ParentColl.ParentInstance;

                queries.Add(string.Format(SmoApplication.DefaultCulture, "ALTER INDEX [{0}] ON {1} DISABLE",
                                    SqlBraket(this.Name), parent.FullQualifiedName));

                this.ExecutionManager.ExecuteNonQuery(queries);

                //Disabled nonclustered or columnstore index do not have any physical partitions
                //So,refreshing the physical partitions collection
                if (this.ServerVersion.Major > 9 && this.PhysicalPartitions != null && !this.ExecutionManager.Recording)
                {
                    var indexType = this.InferredIndexType;

                    if (indexType == IndexType.NonClusteredIndex || indexType == IndexType.NonClusteredColumnStoreIndex)
                    {
                        this.PhysicalPartitions.Refresh();
                    }
                }

                // Allow listeners (e.g. Object Explorer menu items and node hierarchy) to be notified that
                // the disabling happened. This can be used to disable the "Disable" menu item on the index,
                // for example.
                this.GenerateAlterEvent();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Disable, this, e);
            }
        }

        /// <summary>
        /// Enables the index. The action argument specifies how enable the index.
        /// It is possible to call Create() or Rebuild() instead, to enable the index.
        /// </summary>
        /// <param name="action"></param>
        public void Enable(IndexEnableAction action)
        {

            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Enable, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            try
            {
                CheckObjectState(true);
                ThrowIfBelowVersion90();

                switch (action)
                {
                    case IndexEnableAction.Rebuild:
                        Rebuild();
                        break;
                    case IndexEnableAction.Recreate:
                        Recreate();
                        break;
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Enable, this, e);
            }
        }

        public void Recreate()
        {
            
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Recreate, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            bool dropExOld = dropExistingIndex;
            try
            {
                dropExistingIndex = true;
                string dbName = GetDBName();
                StringCollection createQuery = new StringCollection();
                createQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(dbName)));

                ScriptingPreferences sp = new ScriptingPreferences();
                sp.ScriptForCreateDrop = false;

                // pass target version
                sp.SetTargetServerInfo(this);

                // get the script we have to execute
                ScriptCreate(createQuery, sp);

                // execute generated script
                if (createQuery.Count > 1)
                {
                    this.ExecutionManager.ExecuteNonQuery(createQuery);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Recreate, this, e);
            }
            finally
            {
                dropExistingIndex = dropExOld;
            }
        }

        /// <summary>
        /// Tests the integrity of database pages implementing storage for the referenced index.
        /// </summary>
        /// <returns></returns>
        public StringCollection CheckIndex()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.CheckIndex, this, null, ExceptionTemplates.NotCheckIndexOnUDTT);
            }

            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName)));
                ScriptingPreferences sp = new ScriptingPreferences();
                sp.ScriptForCreateDrop = true;
                string fullname = ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FormatFullNameForScripting(sp);

                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKTABLE (N'{0}', {1}) WITH NO_INFOMSGS",
                                        SqlString(fullname), (int)Properties["ID"].Value));

                return this.ExecutionManager.ExecuteNonQueryWithMessage(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckIndex, this, e);
            }
        }

        /// <summary>
        /// Tests the integrity of database pages that store data for the referenced index.
        /// </summary>
        /// <returns></returns>
        public DataTable CheckIndexWithResult()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.CheckIndex, this, null, ExceptionTemplates.NotCheckIndexOnUDTT);
            }

            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName)));
                ScriptingPreferences sp = new ScriptingPreferences();
                sp.ScriptForCreateDrop = true;
                string fullname = ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FormatFullNameForScripting(sp);

                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC CHECKTABLE (N'{0}', {1}) WITH TABLERESULTS, NO_INFOMSGS",
                                        SqlString(fullname), (int)Properties["ID"].Value));
                DataSet ds = this.ExecutionManager.ExecuteWithResults(queries);
                if (ds.Tables.Count > 0)
                {
                    return ds.Tables[0];
                }
                else
                {
                    DataTable dt = new DataTable();
                    dt.Locale = CultureInfo.InvariantCulture;
                    return dt;
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CheckIndex, this, e);
            }
        }

        /// <summary>
        /// The EnumStatistics method returns a DataSet object that enumerates index statistics used to support Microsoft® SQL Server™ 2000 query optimization
        /// </summary>
        /// <returns></returns>
        public DataSet EnumStatistics()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.EnumStatistics, this, null, ExceptionTemplates.NotStatisticsOnUDTT);
            }

            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(ParentColl.ParentInstance.ParentColl.ParentInstance.InternalName)));
                ScriptingPreferences sp = new ScriptingPreferences();
                sp.ScriptForCreateDrop = true;

                string fullTableName = ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FormatFullNameForScripting(sp);
                Database db = (Database)ParentColl.ParentInstance.ParentColl.ParentInstance;

                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC SHOW_STATISTICS ({0}, {1})",
                                                MakeSqlString(fullTableName), MakeSqlString(this.Name)));

                return this.ExecutionManager.ExecuteWithResults(queries);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumStatistics, this, e);
            }
        }

        /// <summary>
        /// Forces the update of data reporting the disk resource usage of the referenced index.
        /// </summary>
        public void RecalculateSpaceUsage()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.RecalculateSpaceUsage, this, null, ExceptionTemplates.NotFragInfoOnUDTT);
            }

            try
            {
                CheckObjectState(true);
                StringCollection queries = new StringCollection();
                SqlSmoObject parent = ParentColl.ParentInstance;
                queries.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(parent.ParentColl.ParentInstance.InternalName)));
                queries.Add(string.Format(SmoApplication.DefaultCulture, "DBCC UPDATEUSAGE(0, N'[{0}].[{1}]', {2}) WITH NO_INFOMSGS",
                                SqlString((string)((ScriptSchemaObjectBase)parent).Schema),
                                SqlString(parent.InternalName), (Int32)Properties["ID"].Value));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RecalculateSpaceUsage, this, e);
            }
        }


        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            // SXI is supported only after Denali SP1. RTM was released with version 11.0.2100

            Version currentVersion = new Version(ServerVersion.Major, ServerVersion.Minor, ServerVersion.BuildNumber);

            Version minSXIVersion = new Version(11, 0, 2813);
            bool SXIsupported = currentVersion >= minSXIVersion;

            if (this.DatabaseEngineType != Microsoft.SqlServer.Management.Common.DatabaseEngineType.SqlAzureDatabase)
            {
                return new PropagateInfo[]
            {
                new PropagateInfo(ServerVersion.Major < 10 ? null : m_PhysicalPartitions, false, false),
                new PropagateInfo(IndexedColumns, false,false),
                new PropagateInfo(SXIsupported ? IndexedXmlPaths : null, false,false),
                new PropagateInfo(SXIsupported ? IndexedXmlPathNamespaces : null, false,false),
                new PropagateInfo((ServerVersion.Major < 8 || this.Parent is UserDefinedTableType) ? null : ExtendedProperties, true, ExtendedProperty.UrnSuffix ),
            };
            }
            else
            {
                return new PropagateInfo[]
            {
                new PropagateInfo(IndexedColumns, false)
            };
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // properties that are not in the property bag - they are not part of the metadata
        ////////////////////////////////////////////////////////////////////////////////

        internal bool dropExistingIndex;

        /// <summary>
        /// Whether to drop and rebuild the existing clustered or nonclustered index with modified
        /// column specifications and keep the same name for the index. The default is false.
        /// </summary>
        public bool DropExistingIndex
        {
            get { return dropExistingIndex; }
            set { dropExistingIndex = value; }
        }

        private bool sortInTempdb = false;
        /// <summary>
        /// Specifies that the intermediate sort results used to build the index will be
        /// stored in the tempdb database.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool SortInTempdb
        {
            get
            {
                CheckObjectState();
                return sortInTempdb;
            }
            set
            {
                CheckObjectState();
                sortInTempdb = value;
            }
        }

        private bool onlineIndexOperation = false;
        /// <summary>
        /// Specifies that the index that is created or modified will be kept online
        /// during the operation, allowing access to the underlying data structures.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public bool OnlineIndexOperation
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion90();

                return onlineIndexOperation;
            }
            set
            {
                CheckObjectState();
                ThrowIfBelowVersion90();

                onlineIndexOperation = value;
            }
        }

        private bool resumableIndexOperation = false;
        /// <summary>
        /// Sepcifies whether the index is resumable.
        /// The resumable index can be resume/abort if it is paused during the rebuild operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool ResumableIndexOperation
        {
            get
            {
                CheckObjectState();

                if (DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    ThrowIfBelowVersion140Prop("ResumableIndexOperation");
                }

                return resumableIndexOperation;
            }

            set
            {
                CheckObjectState();
                if (DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    ThrowIfBelowVersion140Prop("ResumableIndexOperation");
                }

                resumableIndexOperation = value;
            }
        }

        /// <summary>
        /// Specifies the MAX_DURATION for the resumable operation option of the
        /// DDL operation.
        /// </summary>
        private int resumableMaxDuration = 0;

        /// <summary>
        /// Gets or sets the MAX_DURATION for the resumable operation option of the
        /// DDL operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public int ResumableMaxDuration
        {
            get
            {
                CheckObjectState();
                if (DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    ThrowIfBelowVersion140Prop("ResumableMaxDuration");
                }

                return resumableMaxDuration;
            }

            set
            {
                CheckObjectState();
                if (DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                {
                    ThrowIfBelowVersion140Prop("ResumableMaxDuration");
                }

                resumableMaxDuration = value;
            }
        }

        /// <summary>
        /// Specifies the MAX_DURATION for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation.
        /// </summary>
        private int lowPriorityMaxDuration = 0;

        /// <summary>
        /// Gets or sets the MAX_DURATION for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public int LowPriorityMaxDuration
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion120();

                return lowPriorityMaxDuration;
            }

            set
            {
                CheckObjectState();
                ThrowIfBelowVersion120();

                lowPriorityMaxDuration = value;
            }
        }

        /// <summary>
        /// Specifies the ABORT_AFTER_WAIT action for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation.
        /// </summary>
        private AbortAfterWait lowPriorityAbortAfterWait = AbortAfterWait.None;

        /// <summary>
        /// Gets or sets the ABORT_AFTER_WAIT action for the WAIT_AT_LOW_PRIORITY option of the
        /// DDL operation.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public AbortAfterWait LowPriorityAbortAfterWait
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion120();

                return lowPriorityAbortAfterWait;
            }

            set
            {
                CheckObjectState();
                ThrowIfBelowVersion120();

                lowPriorityAbortAfterWait = value;
            }
        }

        private int maximumDegreeOfParallelism = -1;
        /// <summary>
        /// Set the maximum number of processors involved in the index operation.
        /// The system default is used if not set.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public int MaximumDegreeOfParallelism
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion90();
                return maximumDegreeOfParallelism;
            }
            set
            {
                CheckObjectState();
                ThrowIfBelowVersion90();
                maximumDegreeOfParallelism = value;
            }
        }

        /// <summary>
        /// This property is not persisted in the database. It is an
        /// option passed in for the ALTER INDEX REORGANIZE command.
        /// </summary>
        private bool compactLargeObjects = true;
        /// <summary>
        /// Compact LOB data, such as text, image, varchar(MAX), nvarchar(MAX) and varbinary.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool CompactLargeObjects
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion90();
                return compactLargeObjects;
            }
            set
            {
                CheckObjectState();
                ThrowIfBelowVersion90();
                compactLargeObjects = value;
            }
        }

        /// <summary>
        /// This property is not persisted in the database. It is an
        /// option passed in for the ALTER INDEX REORGANIZE command.
        /// </summary>
        private bool compressAllRowGroups = false;
        /// <summary>
        /// Compress open row groups (delta stores) as well.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public bool CompressAllRowGroups
        {
            get
            {
                CheckObjectState();
                ThrowIfBelowVersion130();
                return compressAllRowGroups;
            }
            set
            {
                CheckObjectState();
                ThrowIfBelowVersion130();

                // You can only set this property on columnstore indexes
                //
                if (this.IndexType != Smo.IndexType.ClusteredColumnStoreIndex &&
                    this.IndexType != Smo.IndexType.NonClusteredColumnStoreIndex)
                {
                    throw new InvalidSmoOperationException(string.Format(SmoApplication.DefaultCulture, ExceptionTemplates.PropertyValidOnlyForColumnStoreIndexes, "CompressAllRowGroups"));
                }
                compressAllRowGroups = value;
            }
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index
        /// </summary>
        public void UpdateStatistics()
        {
            UpdateStatistics(StatisticsScanType.Default, 0, true);
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index
        /// </summary>
        public void UpdateStatistics(StatisticsScanType scanType)
        {
            UpdateStatistics(scanType, 0, true);
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index
        /// </summary>
        public void UpdateStatistics(StatisticsScanType scanType, int sampleValue)
        {
            UpdateStatistics(scanType, sampleValue, true);
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index
        /// </summary>
        public void UpdateStatistics(StatisticsScanType scanType,
                            int sampleValue, bool recompute)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.UpdateStatistics, this, null, ExceptionTemplates.NotStatisticsOnUDTT);
            }

            try
            {
                CheckObjectState(true);
                string tablename = ((ScriptSchemaObjectBase)ParentColl.ParentInstance).FullQualifiedName;
                this.ExecutionManager.ExecuteNonQuery(Statistic.UpdateStatistics(
                                    MakeSqlBraket(GetDBName()), tablename,
                                    MakeSqlBraket(this.Name), scanType, StatisticsTarget.Index, !recompute, sampleValue));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.UpdateStatistics, this, e);
            }
        }


        // Index fragmentation support
        public DataTable EnumFragmentation()
        {
            return EnumFragmentation(FragmentationOption.Fast);
        }

        // Index fragmentation support
        public DataTable EnumFragmentation(FragmentationOption fragmentationOption)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.EnumFragmentation,
                    this,
                    null,
                    ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            try
            {
                CheckObjectState();

                string urn = string.Format(SmoApplication.DefaultCulture, "{0}/{1}", this.Urn.Value, GetFragOptionString(fragmentationOption));
                Request req = new Request(urn);

                req.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[] { "Name", "ID" };
                parentProps.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                req.ParentPropertiesRequests[0] = parentProps;

                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumFragmentation, this, e);
            }
        }

        // Index fragmentation support
        public DataTable EnumFragmentation(FragmentationOption fragmentationOption, int partitionNumber)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.EnumFragmentation, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            try
            {
                CheckObjectState();

                //Yukon only
                if (ServerVersion.Major < 9)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.InvalidParamForVersion("EnumFragmentation", "partitionNumber", GetSqlServerVersionName())).SetHelpContext("InvalidParamForVersion");
                }

                string urn = string.Format(SmoApplication.DefaultCulture,
                                          "{0}/{1}[@PartitionNumber={2}]", this.Urn.Value, GetFragOptionString(fragmentationOption), partitionNumber);
                Request req = new Request(urn);

                req.ParentPropertiesRequests = new PropertiesRequest[1];
                PropertiesRequest parentProps = new PropertiesRequest();
                parentProps.Fields = new String[] { "Name", "ID" };
                parentProps.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                req.ParentPropertiesRequests[0] = parentProps;

                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumFragmentation, this, e);
            }
        }


        /// <summary>
        /// Drops a clustered index and moves the underlying table data to the partition
        /// scheme with the parameters specified with partitionSchemeParameters.
        /// </summary>
        /// <param name="partitionScheme"></param>
        /// <param name="partitionSchemeParameters"></param>
        public void DropAndMove(System.String partitionScheme, StringCollection partitionSchemeParameters)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            ThrowIfBelowVersion90();

            if (null == partitionScheme)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this,
                                                    new ArgumentNullException("partitionScheme"));
            }

            if (partitionScheme.Length == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this, null,
                            ExceptionTemplates.EmptyInputParam("partitionScheme", "string"));
            }

            if (null == partitionSchemeParameters)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this,
                                                    new ArgumentNullException("partitionSchemeParameters"));
            }

            if (partitionSchemeParameters.Count == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this, null,
                            ExceptionTemplates.EmptyInputParam("partitionSchemeParameters", "Collection"));
            }

            DropAndMoveImpl(partitionScheme, partitionSchemeParameters);
        }

        /// <summary>
        /// Drops a clustered index and moves the underlying table data
        /// to the specified File Group.
        /// </summary>
        /// <param name="fileGroup"></param>
        public void DropAndMove(System.String fileGroup)
        {

            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this, null, ExceptionTemplates.UDTTIndexCannotBeModified);
            }

            CheckObjectState(true);
            ThrowIfBelowVersion90();

            if (null == fileGroup)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this,
                                        new ArgumentNullException("fileGroup"));
            }

            if (fileGroup.Length == 0)
            {
                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this, null,
                                    ExceptionTemplates.EmptyInputParam("fileGroup", "string"));
            }

            DropAndMoveImpl(fileGroup, null);
        }

        private void DropAndMoveImpl(string dataSpaceName, StringCollection partitionSchemeParameters)
        {
            try
            {
                DropAndMoveImplWorker(dataSpaceName, partitionSchemeParameters);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropAndMove, this, e);
            }
        }

        private void DropAndMoveImplWorker(string dataSpaceName, StringCollection partitionSchemeParameters)
        {
            if (!(bool)Properties["IsClustered"].Value)
            {
                throw new SmoException(ExceptionTemplates.IndexMustBeClustered(((ScriptSchemaObjectBase)ParentColl.ParentInstance).FullQualifiedName, this.FullQualifiedName));
            }

            ScriptingPreferences sp = new ScriptingPreferences(this);
            sp.ScriptForCreateDrop = true;
            sp.IncludeScripts.ExistenceCheck = false;
            sp.IncludeScripts.Header = false;

            ClusteredRegularIndexScripter scripter = new ClusteredRegularIndexScripter(this, sp);
            scripter.DataSpaceName = dataSpaceName;
            scripter.PartitionSchemeParameters = partitionSchemeParameters;

            StringCollection moveQuery = new StringCollection();
            moveQuery.Add(string.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            moveQuery.Add(scripter.GetDropScript());
            this.ExecutionManager.ExecuteNonQuery(moveQuery);

            // remove the object from the parent collection

            // update object state to only if we are in execution mode
            if (!this.ExecutionManager.Recording)
            {
                if (null != ParentColl)
                {
                    ParentColl.RemoveObject(new SimpleObjectKey(this.Name));
                }
                // mark the object as being dropped
                this.MarkDropped();
            }
        }


        public StringCollection Script()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Script, this, null, ExceptionTemplates.OperationNotSupportedWhenPartOfUDTT);
            }

            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// The IndexOnTable property specifies whether an index is defined for a table or a view.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public bool IsIndexOnTable
        {
            get { CheckObjectState(); return ParentColl.ParentInstance is Table; }
        }

        /// <summary>
        /// The IsOnComputed property indicates whether any column in an index is a computed column.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public bool IsIndexOnComputed
        {
            get
            {
                CheckObjectState();
                Table t = ParentColl.ParentInstance as Table;
                if (null == t)
                {
                    return false;
                }
                foreach (IndexedColumn ic in this.IndexedColumns)
                {
                    Column c = t.Columns[ic.Name];
                    if (null != c && c.Computed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        // old IndexKeyType value
        internal object oldIndexKeyTypeValue = null;

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "IndexKeyType" && !prop.Dirty)
            {
                oldIndexKeyTypeValue = prop.Value;
            }
        }

        /// <summary>
        /// Refresh the object.
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            oldIndexKeyTypeValue = null;
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
            string[] fields =
            {
                "BoundingBoxXMax",
                "BoundingBoxXMin",
                "BoundingBoxYMax",
                "BoundingBoxYMin",
                "BucketCount",
                "CellsPerObject",
                "CompressionDelay",
                "DisallowPageLocks",
                "DisallowRowLocks",
                "FileGroup",
                "FileStreamFileGroup",
                "FileStreamPartitionScheme",
                "FillFactor",
                "FilterDefinition",
                "IgnoreDuplicateKeys",
                "IndexedXmlPathName",
                "IndexKeyType",
                "IndexType",
                "IsClustered",
                "IsDisabled",
                "IsFileTableDefined",
                "IsSystemNamed",
                "IsSystemObject",
                "IsUnique",
                "Level1Grid",
                "Level2Grid",
                "Level3Grid",
                "Level4Grid",
                "NoAutomaticRecomputation",
                "PadIndex",
                "ParentXmlIndex",
                "PartitionScheme",
                "SecondaryXmlIndexType",
                "SpatialIndexType",
                "IsOptimizedForSequentialKey",
				nameof(HasXmlCompressedPartitions),
            };

            List<string> list = GetSupportedScriptFields(typeof(Index.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
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
            if (((parentType.Name == "Table") || (parentType.Name == "View"))
                && (version.Major > 9)
                && (sp.TargetServerVersionInternal > SqlServerVersionInternal.Version90)
                && (sp.Storage.DataCompression))
            {
                return new string[] { "HasCompressedPartitions" };
            }
            else
            {
                return new string[] { };
            }
        }

        //NOTE : This method is meant to be temporary to fix the issue described in TFS#7705404. Callers of this method
        //should use the GetRebuildFields with the DatabaseEngineEdition specified otherwise incorrect results may
        //be returned (unsupported fields)
        internal static string[] GetRebuildFields(Cmn.ServerVersion version, Cmn.DatabaseEngineType databaseEngineType)
        {
            return GetRebuildFields(version, databaseEngineType, Cmn.DatabaseEngineEdition.Unknown);
        }

        internal static string[] GetRebuildFields(Cmn.ServerVersion version, Cmn.DatabaseEngineType databaseEngineType, Cmn.DatabaseEngineEdition databaseEngineEdition)
        {
            string[] fields = {
                                        "IsSystemNamed",
                                        "IndexKeyType",
                                        "IndexType",
                                        "IsUnique",
                                        "IsClustered",
                                        "IsDisabled",
                                        "IgnoreDuplicateKeys",
                                        "FillFactor",
                                        "PadIndex",
                                        "DisallowRowLocks",
                                        "DisallowPageLocks",
                                        "NoAutomaticRecomputation",
                                        "HasCompressedPartitions",
                                        "HasXmlCompressedPartitions",
                                        "IsSystemObject"};
            List<string> list = GetSupportedScriptFields(typeof(Index.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }

        /// <summary>
        /// Returns a value indicating whether the index supports online rebuild
        /// </summary>
        public bool IsOnlineRebuildSupported
        {
            get
            {
                if (this.ServerVersion.Major < 9
                    || this.GetServerObject().Information.EngineEdition != Edition.EnterpriseOrDeveloper
                    || !(this.Parent is TableViewBase))
                {
                    return false;
                }

                IndexType indexType = this.InferredIndexType;
                //The option ONLINE is not valid when you rebuild an XML,spatial or columnstore index
                TableViewBase parent = (TableViewBase)this.Parent;

                switch (indexType)
                {
                    case IndexType.ClusteredIndex:
                        //Clustered indexes if the underlying structure contains LOB data types
                        foreach (Column col in parent.Columns)
                        {
                            if ((col.DataType.SqlDataType == SqlDataType.UserDefinedDataType
                                    && (IsLargeObject(DataType.SqlToEnum(col.GetPropValueOptional("SystemType").ToString()))))
                                || (IsLargeObject(col.DataType.SqlDataType)))
                            {
                                return false;
                            }
                        }
                        break;
                    case IndexType.NonClusteredIndex:
                        //For a non-clustered index, the LOB data type column could be an included column of the index.
                        if (this.IndexKeyType == IndexKeyType.None)
                        {
                            foreach (IndexedColumn col in this.IndexedColumns)
                            {
                                if (col.IsIncluded)
                                {
                                    Column column = parent.Columns[col.Name];
                                    if ((column.DataType.SqlDataType == SqlDataType.UserDefinedDataType
                                           && (IsLargeObject(DataType.SqlToEnum(column.GetPropValueOptional("SystemType").ToString()))))
                                        || IsLargeObject(column.DataType.SqlDataType))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                        break;
                    case IndexType.PrimaryXmlIndex:
                    case IndexType.SecondaryXmlIndex:
                    case IndexType.SpatialIndex:
                    case IndexType.NonClusteredColumnStoreIndex:
                    case IndexType.ClusteredColumnStoreIndex:
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Verifies whether the SqlDatatype is LOB or Large UDT
        /// </summary>
        /// <param name="dataType"></param>
        /// <returns></returns>
        internal static bool IsLargeObject(SqlDataType dataType)
        {
            return ((dataType == SqlDataType.Image) ||
                (dataType == SqlDataType.Text) ||
                (dataType == SqlDataType.NText) ||
                (dataType == SqlDataType.VarCharMax) ||
                (dataType == SqlDataType.NVarCharMax) ||
                (dataType == SqlDataType.VarBinaryMax) ||
                (dataType == SqlDataType.Xml) ||
                (dataType == SqlDataType.Geometry) ||
                (dataType == SqlDataType.Geography)
               );
        }

        /// <summary>
        /// Indicates if the index belongs to a memory optimized table.
        /// We need to rely on this getter instead of its SMO property(IsMemoryOptimized) while the index is being created.
        /// </summary>
        /// <returns></returns>
        internal bool IsMemoryOptimizedIndex
        {
            get
            {
                bool isMemoryOptimizedIndex = false;
                if (this.Parent.IsSupportedProperty("IsMemoryOptimized") && (this.Parent.GetPropValueOptional("IsMemoryOptimized", false)))
                {
                    if (this.InferredIndexType == IndexType.NonClusteredHashIndex ||
                        this.InferredIndexType == IndexType.NonClusteredIndex ||
                        this.InferredIndexType == IndexType.ClusteredColumnStoreIndex)
                    {
                        isMemoryOptimizedIndex = true;
                    }
                }
                return isMemoryOptimizedIndex;
            }
        }

        /// <summary>
        /// Indicates if the index belongs to a SQL DW table.
        /// We need to rely on this getter instead of the SMO property(DwTableDistribution) while the index is being created.
        /// </summary>
        /// <returns>True, if the index is a SQL DW table index; false otherwise.</returns>
        internal bool IsSqlDwIndex
        {
            get
            {
                bool isSqlDwIndex = false;
                if (this.Parent.IsSupportedProperty("DwTableDistribution"))
                {

                    if (this.Parent.GetPropValueOptional<DwTableDistributionType>("DwTableDistribution").HasValue)
                    {
                        if (this.Parent.GetPropValueOptional<DwTableDistributionType>("DwTableDistribution") != DwTableDistributionType.Undefined &&
                            this.Parent.GetPropValueOptional<DwTableDistributionType>("DwTableDistribution") != DwTableDistributionType.None)
                        {
                            isSqlDwIndex = true;
                        }
                    }
                }
                return isSqlDwIndex;
            }
        }
    }
}