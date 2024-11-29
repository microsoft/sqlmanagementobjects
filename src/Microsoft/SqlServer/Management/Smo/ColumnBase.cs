// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class Column : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists,
                                Cmn.IMarkForDrop, IExtendedProperties, Cmn.IRenamable
    {
        internal Column(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
            m_bDefaultInitialized = false;
            defaultConstraint = null;
        }

        public Column(SqlSmoObject parent, System.String name, DataType dataType)
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = parent;
            this.DataType = dataType;
        }

        //new constructor for making isfilestream property of column available for varbinary(max) datatype
        public Column(SqlSmoObject parent, System.String name, DataType dataType, bool isFileStream)
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = parent;

            if (IsSupportedProperty("IsFileStream"))
            {
                this.DataType = dataType;
                this.IsFileStream = isFileStream;

                //isfilestream property of column is available only for varbinary(max) datatype
                if (isFileStream && dataType.SqlDataType != SqlDataType.VarBinaryMax)
                {
                    throw new SmoException(ExceptionTemplates.ColumnNotVarbinaryMax);
                }
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.SupportedOnlyOn100).SetHelpContext("SupportedOnlyOn100");
            }
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return nameof(Column);
            }
        }

        /// <summary>
        /// Clears classification fields
        /// </summary>
        /// <returns></returns>
        public void RemoveClassification()
        {
            if (IsSupportedProperty("IsClassified"))
            {
                // Do not clear empty/default values to leave 'dirty' flag unchanged
                if (!string.IsNullOrEmpty(SensitivityLabelName))
                {
                    SensitivityLabelName = string.Empty;
                }

                if (!string.IsNullOrEmpty(SensitivityLabelId))
                {
                    SensitivityLabelId = string.Empty;
                }

                if (!string.IsNullOrEmpty(SensitivityInformationTypeName))
                {
                    SensitivityInformationTypeName = string.Empty;
                }

                if (!string.IsNullOrEmpty(SensitivityInformationTypeId))
                {
                    SensitivityInformationTypeId = string.Empty;
                }

                if (IsSupportedProperty("SensitivityRank"))
                {
                    if (SensitivityRank != SensitivityRank.Undefined)
                    {
                        SensitivityRank = SensitivityRank.Undefined;
                    }
                }
            }
        }

        /// <summary>
        /// Appends corresponding data classification script to the main query
        /// </summary>
        public void ScriptDataClassification(StringCollection queries, ScriptingPreferences sp, bool forCreateScript = false)
        {
            // We shouldn't script column's sensitivity classification in the following cases:
            // 1. The column doesn't support sensitivity classification at all
            // 2. The column neither in Creating nor Existing state
            // 3. The scripting preferences 'ExtendedProperty' option is true and both source and target server versions are lower than 15 or not Azure
            if (!IsSupportedProperty("IsClassified", sp) ||
                (State != SqlSmoState.Creating && State != SqlSmoState.Existing) ||
                (sp != null && sp.IncludeScripts.ExtendedProperties && !VersionUtils.IsSql15Azure12OrLater(this.DatabaseEngineType, this.ServerVersion) && !VersionUtils.IsTargetVersionSql15Azure12OrLater(sp.TargetDatabaseEngineType, sp.TargetServerVersion)))
            {
                return;
            }

            Property sensitivityLabelId = Properties.Get("SensitivityLabelId");
            Property sensitivityLabelName = Properties.Get("SensitivityLabelName");
            Property sensitivityInformationTypeId = Properties.Get("SensitivityInformationTypeId");
            Property sensitivityInformationTypeName = Properties.Get("SensitivityInformationTypeName");
            Property sensitivityRank = IsSupportedProperty("SensitivityRank") ? Properties.Get("SensitivityRank") : null;
            bool classified = !string.IsNullOrEmpty((string)sensitivityLabelId.Value) ||
                              !string.IsNullOrEmpty((string)sensitivityLabelName.Value) ||
                              !string.IsNullOrEmpty((string)sensitivityInformationTypeId.Value) ||
                              !string.IsNullOrEmpty((string)sensitivityInformationTypeName.Value) ||
                              (sensitivityRank != null && sensitivityRank.Value != null && (SensitivityRank)sensitivityRank.Value != SensitivityRank.Undefined);
            bool dirty = State == SqlSmoState.Existing && (sensitivityLabelId.Dirty ||
                                                           sensitivityLabelName.Dirty ||
                                                           sensitivityInformationTypeId.Dirty ||
                                                           sensitivityInformationTypeName.Dirty ||
                                                           (sensitivityRank != null && sensitivityRank.Dirty));

            // Validation
            if (classified || dirty)
            {
                // Classified column must belong to table
                if (!(this.Parent is Table))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoDataClassificationOnNonTables);
                }

                // Sparse, ColumnSet, Temporal, Filestream and Encrypted columns are supported by data classification,
                // while Computed don't
                if (GetPropValueOptional<bool>("Computed", false))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoDataClassificationOnComputedColumns);
                }
            }

            DataClassificationScriptGenerator generator = DataClassificationScriptGenerator.Create(this, sp);

            // Append corresponding script depends on object's state
            if ((State == SqlSmoState.Creating || forCreateScript) && classified)
            {
                queries.Add(generator.Add());
            }
            else
            {
                if (State == SqlSmoState.Existing && dirty)
                {
                    queries.Add(classified ? generator.Update() : generator.Drop());
                }
            }
        }

        /// <summary>
        /// This object supports permissions.
        /// </summary>
        internal override UserPermissionCollection Permissions
        {
            get
            {
                // call the base class 
                return GetUserPermissions();
            }
        }

        /// <summary>
        /// Returns the target object on which we will apply permisisons.
        /// </summary>
        /// <returns></returns>
        internal override SqlSmoObject GetPermTargetObject()
        {
            //  target object is the parent - Table, View or UserDefinedFunction
            return this.Parent;
        }

        /// <summary>
        /// Returns type of column, and underlying type if UDDT
        /// </summary>
        /// <returns></returns>
        internal SqlDataType UnderlyingSqlDataType
        {
            get
            {
                SqlDataType sqlDataType = this.DataType.SqlDataType;

                // if it's not a UDDT, just return it.
                if (sqlDataType != SqlDataType.UserDefinedDataType)
                {
                    return this.DataType.SqlDataType;
                }

                // otherwise, let's discover it.
                Server server = this.GetServerObject();
                Database database = server.Databases[this.GetDBName()];
                UserDefinedDataType uddt = database.UserDefinedDataTypes[this.DataType.Name, this.DataType.Schema];

                sqlDataType = DataType.UserDefinedDataTypeToEnum(uddt);

                return sqlDataType;
            }
        }

        /// <summary>
        /// Return true if default constraints should be embedded. 
        /// </summary>
        /// <returns></returns>
        private bool EmbedDefaultConstraints(ScriptingPreferences sp = null)
        {
            //UDTT always need to be embedded as they don't support ALTER
            // If it's a memory optimized table, default constraints should be embedded since alter is not supported before Sql130.
            // When adding a new column to existing object, embed the default in  case the existing object has rows
            if (this.Parent is UserDefinedTableType ||
                (this.Parent.State == SqlSmoState.Existing && this.State == SqlSmoState.Creating) ||
                (!VersionUtils.IsSql13Azure12OrLater(this.DatabaseEngineType, ServerVersion, sp) &&
                this.Parent.IsSupportedProperty("IsMemoryOptimized") && this.Parent.GetPropValueOptional("IsMemoryOptimized", false)))
            {
                return true;
            }
            return false;
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

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        private DataType dataType = null;
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Design)]

        // See the TODO near SfcReferenceAttribute in SfcAttributes.cs (code marker !SR!)
        [CLSCompliant(false)]
        [SfcReference(typeof(UserDefinedType), typeof(UserDefinedTypeResolver), "Resolve")]
        [SfcReference(typeof(UserDefinedDataType), typeof(UserDefinedDataTypeResolver), "Resolve")]
        public DataType DataType
        {
            get
            {
                return GetDataType(ref dataType);
            }
            set
            {
                if (value != null && value.SqlDataType == SqlDataType.UserDefinedTableType)
                {
                    throw new FailedOperationException(ExceptionTemplates.SetDataType, this, null);
                }

                SetDataType(ref dataType, value);
            }
        }

        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public bool IsEncrypted
        {
            get
            {
                return this.IsSupportedProperty("ColumnEncryptionKeyID") && null != this.GetPropValueOptionalAllowNull("ColumnEncryptionKeyID");
            }
        }

        public void Create()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Create, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            TableViewBase parent = (TableViewBase)ParentColl.ParentInstance;
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ADD ",
                parent.FormatFullNameForScripting(sp));

            ScriptDdlCreateImpl(sb, sp);
            queries.Add(sb.ToString());

            ScriptDefaultAndRuleBinding(queries, sp);
            ScriptDataClassification(queries, sp);
        }

        internal override void ScriptDdl(StringCollection queries, ScriptingPreferences sp)
        {
            CheckObjectState();
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            ScriptDdlCreateImpl(sb, sp);

            if (sb.Length > 0)
            {
                queries.Add(sb.ToString());
            }
        }

        private void ScriptDdlCreateImpl(StringBuilder sb, ScriptingPreferences sp)
        {
            VersionValidate(sp);

            bool isGraphInternalColumn = IsGraphInternalColumn();
            bool isGraphComputedColumn = IsGraphComputedColumn();

            string colScriptName = FormatFullNameForScripting(sp);

            // Do not script graph internal columns or computed columns.
            // These columns are created internally when the table is marked
            // with 'AS NODE' or 'AS EDGE'.
            //
            if (isGraphInternalColumn || isGraphComputedColumn)
            {
                return;
            }

            // Do not script dropped ledger columns.
            // These columns are hidden and "dropped", and should not be displayed.
            if (DroppedLedgerColumn())
            {
                return;
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} ", colScriptName);

            bool isSparse = false;
            bool isColumnSet = false;

            // Indicate if column belongs to a Hekaton table
            bool isMemoryOptimizedTable = false;
            if (this.Parent.IsSupportedProperty("IsMemoryOptimized"))
            {
                if (this.Parent.GetPropValueOptional("IsMemoryOptimized", false))
                {
                    isMemoryOptimizedTable = true;
                }
            }

            // Indicate if the column belongs to an external table
            bool isExternalTable = this.CheckIsExternalTableColumn(sp);

            bool isParentTable = this.Parent is Table;
            bool isGeneratedAlwaysColumn = false;
            bool isHiddenColumn = false;
            bool isNullableledgerColumn = false;

            GeneratedAlwaysType generatedAlwaysType = GeneratedAlwaysType.None;

            if (IsSupportedProperty(nameof(GeneratedAlwaysType), sp))
            {
                object value = this.GetPropValueOptional(nameof(GeneratedAlwaysType));
                if (value != null)
                {
                    generatedAlwaysType = (GeneratedAlwaysType)value;
                }

                isGeneratedAlwaysColumn = generatedAlwaysType != Smo.GeneratedAlwaysType.None;

                if (isGeneratedAlwaysColumn && !isParentTable)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoGeneratedAlwaysColumnsOnNonTables);
                }
                isNullableledgerColumn = generatedAlwaysType != Smo.GeneratedAlwaysType.AsTransactionIdEnd || generatedAlwaysType != Smo.GeneratedAlwaysType.AsSequenceNumberEnd;
            }

            if (IsSupportedProperty(nameof(IsHidden), sp))
            {
                if (null != Properties.Get(nameof(IsHidden)).Value)
                {
                    isHiddenColumn = (bool)Properties.Get(nameof(IsHidden)).Value;
                }

                if (isHiddenColumn && !isGeneratedAlwaysColumn)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoHiddenColumnsOnNonGeneratedAlwaysColumns);
                }
            }

            bool isMaskedColumn = false;
            string maskingFunction = null;
            if (IsSupportedProperty("IsMasked", sp))
            {
                if (null != Properties.Get("IsMasked").Value)
                {
                    isMaskedColumn = (bool)Properties.Get("IsMasked").Value;

                    if (isMaskedColumn)
                    {
                        maskingFunction = GetAndValidateMaskingFunction(isMaskedColumn);

                        if (!Util.IsNullOrWhiteSpace(maskingFunction))
                        {
                            if (!isParentTable)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnNonTables);
                            }
                        }
                    }

                }
            }

            if (IsSupportedProperty("IsColumnSet", sp))
            {
                if (null != Properties.Get("IsSparse").Value)
                {
                    isSparse = (bool)Properties["IsSparse"].Value;
                }

                if (null != Properties.Get("IsColumnSet").Value)
                {
                    isColumnSet = (bool)Properties["IsColumnSet"].Value;
                }

                if (isSparse && isColumnSet)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.NoSparseOnColumnSet);
                }
            }

            if ((isSparse || isColumnSet) && isGeneratedAlwaysColumn)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.NoSparseOrColumnSetOnTemporalColumns);
            }
            if (isColumnSet && isMaskedColumn)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnColumnSet);
            }
            if (isGeneratedAlwaysColumn && isMaskedColumn)
            {
                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnTemporalColumns);
            }

            if (null != Properties.Get("Computed").Value && (bool)Properties["Computed"].Value)
            {
                string sComputedText = (string)this.GetPropValue("ComputedText");
                if (null != sComputedText && sComputedText.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " AS {0}", sComputedText);
                    if (isSparse)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.NoSparseOnComputed);
                    }
                    if (isColumnSet)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.NoColumnSetOnComputed);
                    }
                    if (isGeneratedAlwaysColumn)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.ComputedTemporalColumns);
                    }
                    if (isMaskedColumn)
                    {
                        throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnComputedColumns);
                    }
                    if (sp.TargetServerVersion >= SqlServerVersion.Version90 &&
                        this.ServerVersion.Major >= 9)
                    {
                        if (GetPropValueOptional("IsPersisted", false))
                        {
                            sb.Append(" PERSISTED");
                            //you can set a computed column to be not null if it is persisted
                            if (!GetPropValueOptional("Nullable", false))
                            {
                                sb.Append(" NOT NULL");
                            }
                        }

                        if (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType)
                        {
                            // we need to force the collation of the computed columns
                            // when we script optimizer data because it is dependent on the 
                            // collation of the database and it might not match the stats blob
                            if (sp.Data.OptimizerData && RequiresCollate(sp))
                            {
                                string sCollation = Properties.Get("Collation").Value as string;
                                if (null != sCollation && 0 < sCollation.Length)
                                {
                                    CheckCollation(sCollation, sp);
                                    sb.AppendFormat(SmoApplication.DefaultCulture, " COLLATE {0}", sCollation);
                                }
                            }
                        }
                    }
                    return;
                }
            }

            UserDefinedDataType.AppendScriptTypeDefinition(sb, sp, this, DataType.SqlDataType);

            // Skip checking Filestream which is unsupported for hekaton and external tables
            if (!isMemoryOptimizedTable && !isExternalTable)
            {
                if (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType)
                {
                    if (IsSupportedProperty("IsFileStream", sp))
                    {
                        Property pFileStream = Properties.Get("IsFileStream");
                        if (pFileStream.Value != null && sp.Storage.FileStreamColumn)
                        {
                            if ((bool)pFileStream.Value)
                            {
                                if (isMaskedColumn)
                                {
                                    throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnFileStreamColumns);
                                }

                                // isfilestream property of column is available only for varbinary(max) datatype
                                if (this.dataType.SqlDataType != SqlDataType.VarBinaryMax)
                                {
                                    throw new SmoException(ExceptionTemplates.ColumnNotVarbinaryMax);
                                }
                                else
                                {
                                    sb.Append(" FILESTREAM ");
                                }
                            }
                        }
                    }
                }
            }

            if (RequiresCollate(sp))
            {
                string sCollation = Properties.Get("Collation").Value as string;
                if (null != sCollation && 0 < sCollation.Length)
                {
                    CheckCollation(sCollation, sp);
                    sb.AppendFormat(SmoApplication.DefaultCulture, " COLLATE {0}", sCollation);
                }
            }

            // Skip checking Sparse for hekaton and external tables since it is not supported
            if (!isMemoryOptimizedTable && !isExternalTable)
            {
                if (isSparse)
                {
                    sb.Append(" SPARSE ");
                }
                else if (isColumnSet)
                {
                    sb.Append(" COLUMN_SET FOR ALL_SPARSE_COLUMNS ");
                }
            }

            if (isMaskedColumn)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, " MASKED WITH (FUNCTION = '{0}')", maskingFunction);
            }

            if (sp.Table.Identities && null != Properties.Get("Identity").Value && (bool)Properties["Identity"].Value)
            {
                // Identity columns are not supported for external tables
                if (isExternalTable)
                {
                    throw new SmoException(ExceptionTemplates.IdentityColumnForExternalTable);
                }

                if (isGeneratedAlwaysColumn)
                {
                    throw new SmoException(ExceptionTemplates.IdentityTemporalColumns);
                }

                sb.Append(" IDENTITY");

                Property propSeed = Properties.Get("IdentitySeedAsDecimal");
                if (propSeed.Value == null)
                {
                    propSeed = Properties.Get("IdentitySeed");
                }

                Property propIncrement = Properties.Get("IdentityIncrementAsDecimal");
                if (propIncrement.Value == null)
                {
                    propIncrement = Properties.Get("IdentityIncrement");
                }

                if (null != propSeed.Value && null != propIncrement.Value)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "({0},{1})", propSeed.Value.ToString(), propIncrement.Value.ToString());
                }

                if (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType && IsSupportedProperty("NotForReplication", sp))
                {
                    Property nfr = Properties.Get("NotForReplication");
                    if (null != nfr.Value && (bool)nfr.Value)
                    {
                        sb.Append(" NOT FOR REPLICATION");
                    }
                }
            }

            switch (generatedAlwaysType)
            {
                case GeneratedAlwaysType.AsRowStart:
                    sb.Append(" GENERATED ALWAYS AS ROW START");
                    if (isHiddenColumn)
                    {
                        sb.Append(" HIDDEN");
                    }
                    break;
                case GeneratedAlwaysType.AsRowEnd:
                    sb.Append(" GENERATED ALWAYS AS ROW END");
                    if (isHiddenColumn)
                    {
                        sb.Append(" HIDDEN");
                    }
                    break;
                case GeneratedAlwaysType.AsTransactionIdStart:
                    sb.Append(" GENERATED ALWAYS AS transaction_id START");
                    if (isHiddenColumn)
                    {
                        sb.Append(" HIDDEN");
                    }
                    break;
                case GeneratedAlwaysType.AsTransactionIdEnd:
                    sb.Append(" GENERATED ALWAYS AS transaction_id END");
                    if (isHiddenColumn)
                    {
                        sb.Append(" HIDDEN");
                    }
                    break;
                case GeneratedAlwaysType.AsSequenceNumberStart:
                    sb.Append(" GENERATED ALWAYS AS sequence_number START");
                    if (isHiddenColumn)
                    {
                        sb.Append(" HIDDEN");
                    }
                    break;
                case GeneratedAlwaysType.AsSequenceNumberEnd:
                    sb.Append(" GENERATED ALWAYS AS sequence_number END");
                    if (isHiddenColumn)
                    {
                        sb.Append(" HIDDEN");
                    }
                    break;
                case GeneratedAlwaysType.None:
                    break;
                default:
                    Diagnostics.TraceHelper.Assert(false, "Unknown 'GeneratedAlwaysType' property value encountered.");
                    break;
            }

            // Skip checking RowGuidCol for hekaton and external tables since it is not supported
            if (!isMemoryOptimizedTable && !isExternalTable)
            {
                if (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType)
                {
                    if (null != Properties.Get("RowGuidCol").Value && (bool)Properties["RowGuidCol"].Value)
                    {
                        sb.Append(" ROWGUIDCOL ");
                    }
                }
            }

            // Add Always Encrypted expression
            if (IsSupportedProperty("ColumnEncryptionKeyName") && IsSupportedProperty("EncryptionAlgorithm") &&
                IsSupportedProperty("EncryptionType"))
            {
                object encryptionAlgorithm = Properties.Get("EncryptionAlgorithm").Value;
                object encryptionType = Properties.Get("EncryptionType").Value;
                if ((null != encryptionAlgorithm) || (null != encryptionType))
                {
                    //This value was never preloaded (it's expensive so we only get it if this is an encrypted
                    //column) so call GetPropValueOptional so that we fetch it from the DB if we don't have it yet
                    object columnEncryptionKeyName = this.GetPropValueOptional("ColumnEncryptionKeyName");
                    if (columnEncryptionKeyName != null)
                    {
                        //This column is encrypted, check that the target server type supports Always Encrypted and throw if not
                        //(we short and assume if one Always Encrypted property is not supported they all aren't)
                        this.ThrowIfPropertyNotSupported("ColumnEncryptionKeyID", sp);

                        // Validate that all Always Encrypted parameters are supplied
                        if ((null == columnEncryptionKeyName) || (null == encryptionAlgorithm) || (null == encryptionType))
                        {
                            throw new WrongPropertyValueException(ExceptionTemplates.InvalidAlwaysEncryptedPropertyValues);
                        }

                        if (isMaskedColumn)
                        {
                            throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnEncryptedColumns);
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            " ENCRYPTED WITH (COLUMN_ENCRYPTION_KEY = {0}, ENCRYPTION_TYPE = {1}, ALGORITHM = '{2}')",
                            MakeSqlBraket(this.ColumnEncryptionKeyName),
                            this.EncryptionType.ToString(),
                            this.EncryptionAlgorithm.Replace("'", "''"));
                    }
                }
            }
            
            if (null != Properties.Get("Nullable").Value && (this.CheckIsExternalTableColumn(sp) == false || sp.TargetDatabaseEngineEdition != Common.DatabaseEngineEdition.SqlOnDemand))
            {
                if (false == (bool)Properties["Nullable"].Value)
                {
                    // CLR TVF do not allow a NOT NULL constraint
                    if (!(this.Parent is UserDefinedFunction))
                    {
                        sb.Append(" NOT NULL");
                    }
                }
                else
                {
                    // only NOT NULL columns are supported
                    if (isGeneratedAlwaysColumn && !isNullableledgerColumn)
                    {
                        throw new SmoException(ExceptionTemplates.NullableTemporalColumns);
                    }

                    sb.Append(" NULL");
                }
            }
            else
            {
                // only NOT NULL columns are supported
                if (isGeneratedAlwaysColumn && !isNullableledgerColumn)
                {
                    throw new SmoException(ExceptionTemplates.NullableTemporalColumns);
                }
            }

            ScriptDefaultConstraint(sb, sp);
        }

        private void ScriptDefaultConstraint(StringBuilder sb, ScriptingPreferences sp)
        {
            // Default constraints are scripted with the query if
            // - The default constraint exists 
            // - The constraint isn't ignored for scripting or it's for direct execution
            // - We should be embedding default constraints or default constraints are forced to be embedded
            // We may get called here before the DefaultConstraint has been populated, so make sure it gets fully 
            // built for scripting
            InitDefaultConstraint(forScripting:true);
            if (null != this.DefaultConstraint &&
                (!this.DefaultConstraint.IgnoreForScripting || sp.ForDirectExecution) &&
                (this.EmbedDefaultConstraints(sp) || this.DefaultConstraint.forceEmbedDefaultConstraint) &&
                sb.Length > 0)
            {
                this.DefaultConstraint.forceEmbedDefaultConstraint = false;
                sb.Append(this.DefaultConstraint.ScriptDdl(sp));
            }
        }

        /// <summary>
        /// Returns permission script corresponding to the permission info passed as parameter.
        /// </summary>
        /// <param name="pi"></param>
        /// <param name="sp"></param>
        /// <returns></returns>
        internal override string ScriptPermissionInfo(PermissionInfo pi, ScriptingPreferences sp)
        {
            TableViewBase tableOrView = this.Parent as TableViewBase;
            if (tableOrView != null //Columns Permissions are only applicable to a Table or a View.
                && (pi.PermissionState == PermissionState.Grant || pi.PermissionState == PermissionState.Revoke))
            {
                List<string> withGrantPermissionKeys = tableOrView.GetKeysForPermissionWithGrantOptionFromCache();

                if (pi.PermissionTypeInternal.GetPermissionCount() == 1) //Enumeration of permissions from engine happens one permission at a time.
                {
                    string currentPermissionKey = TableViewBase.GetKeyToMatchColumnPermissions(
                                                        pi.ObjectClass.ToString(),
                                                        pi.Grantee,
                                                        pi.GranteeType.ToString(),
                                                        pi.Grantor,
                                                        pi.GrantorType.ToString(),
                                                        pi.PermissionTypeInternal.ToString());

                    if (withGrantPermissionKeys.Contains(currentPermissionKey))
                    {
                        if (pi.PermissionState == PermissionState.Grant)
                        {
                            //We need to generate REVOKE script with GRANT OPTION FOR
                            pi.SetPermissionState(PermissionState.Revoke); //PermissionState = Revoke
                            return (PermissionWorker.ScriptPermissionInfo(GetPermTargetObject(), pi, sp, true, true)); //grantGrant = true & cascade = true
                        }
                        else if (pi.PermissionState == PermissionState.Revoke)
                        {
                            return (PermissionWorker.ScriptPermissionInfo(GetPermTargetObject(), pi, sp, false, true)); //grantGrant = false & cascade = true
                        }
                    }
                }
            }

            return base.ScriptPermissionInfo(pi, sp);
        }

        /// <summary>
        /// Checks to see if this column is fit to be scripted to the target version
        /// </summary>
        /// <param name="so"></param>
        internal void VersionValidate(ScriptingPreferences sp)
        {
            // before we script it out, we need to verify that the target version will
            // support the data type of the column
            CheckSupportedType(sp);

            // Make sure we have compatible collations if our target is 2000
            if (sp.TargetServerVersion == SqlServerVersion.Version80)
            {

                // if it's a computed column and a foreign key, we need to bail
                if (null != Properties.Get("Computed").Value
                    && (bool)Properties["Computed"].Value
                    && this.IsForeignKey)
                {
                    throw new SmoException(ExceptionTemplates.ComputedColumnDownlevelContraint(FormatFullNameForScripting(sp, true), GetSqlServerName(sp)));
                }
            }
        }

        /// <summary>
        /// Returns the system SqlDataType for the passed column. If the DataType is UDDT then
        /// it returns the underlying SqlDataType for the UDDT
        /// </summary>
        /// <param name="column">
        /// The column for which to return the data type
        /// </param>
        /// <returns>
        /// Returns the system SqlDataType for the passed column. If the DataType is UDDT then
        /// it returns the underlying SqlDataType for the UDDT
        /// </returns>
        /// 
        private SqlDataType GetNativeDataType()
        {
            SqlDataType sqlDataType = this.DataType.SqlDataType;

            // If the DataType represents a UDDT then get the underlying
            // system type

            if (sqlDataType == SqlDataType.UserDefinedDataType)
            {
                Database database = (Database)this.Parent.ParentColl.ParentInstance; ;

                UserDefinedDataType uddt = database.UserDefinedDataTypes[this.DataType.Name, this.DataType.Schema];

                sqlDataType = DataType.UserDefinedDataTypeToEnum(uddt);

            }

            return sqlDataType;
        }


        /// <summary>
        /// Determines whether a database type is supported or not based on the passed
        /// options
        /// </summary>
        /// <param name="column">Column for which test the DataType</param>
        /// <param name="options">ScriptingPreferences to use</param>
        /// 
        private void CheckSupportedType(ScriptingPreferences options)
        {
            Diagnostics.TraceHelper.Assert(options != null);

            // Get the SqlDataType for the column, in the event
            // that it's a UDDT and we need to infer it's underlying type
            //
            SqlDataType sqlDataType = GetNativeDataType();

            // check for supportability
            //
            DataType.CheckColumnTypeSupportability(((NamedSmoObject)this.Parent).Name, this.Name, sqlDataType, options);

            if (Cmn.DatabaseEngineType.SqlAzureDatabase == options.TargetDatabaseEngineType)
            {
                // check for cloud
                bool isSupported = DataType.IsDataTypeSupportedOnCloud(sqlDataType);

                if (!isSupported)
                {
                    // parent can be table, view, udf
                    throw new SmoException(ExceptionTemplates.UnsupportedColumnTypeOnEngineType(((NamedSmoObject)this.Parent).Name, ((NamedSmoObject)this).Name, sqlDataType.ToString(), GetDatabaseEngineName(options)));
                }
            }
        }


        protected override void PostCreate()
        {
            // update object state to only if we are in execution mode
            if (!this.ExecutionManager.Recording)
            {
                if (null != this.DefaultConstraint)
                {
                    this.DefaultConstraint.SetState(SqlSmoState.Existing);

                    // reset all properties 			
                    this.DefaultConstraint.Properties.SetAllDirty(false);
                }
            }
        }

        public void Drop()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }

            CheckObjectState();
            TableViewBase table = (TableViewBase)base.ParentColl.ParentInstance;
            string sTableName = table.FormatFullNameForScripting(sp);
            string sColumnName = FormatFullNameForScripting(sp);

            Property pDefault = Properties.Get("Default");

            if (null != this.DefaultConstraint && this.DefaultConstraint.Name.Length > 0)
            {
                dropQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP CONSTRAINT [{1}]", sTableName, SqlBraket((string)this.DefaultConstraint.Name)));
            }
            else if (Cmn.DatabaseEngineType.SqlAzureDatabase != sp.TargetDatabaseEngineType &&
                null != pDefault.Value && ((string)pDefault.Value).Length > 0)
            {
                dropQuery.Add(string.Format(SmoApplication.DefaultCulture, "EXEC dbo.sp_unbindefault N'{0}.{1}'",
                                SqlString(sTableName), SqlString(sColumnName)));
            }

            dropQuery.Add(string.Format(SmoApplication.DefaultCulture, "ALTER TABLE {0} DROP COLUMN {1}{2}",
                sTableName,
                (sp.IncludeScripts.ExistenceCheck && sp.TargetServerVersion >= SqlServerVersion.Version130) ? "IF EXISTS " : string.Empty,
                sColumnName));
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        protected override bool IsObjectDirty()
        {
            return base.IsObjectDirty() || AddedDefCnstr();
        }

        private bool AddedDefCnstr()
        {
            return (DefaultConstraint != null && this.DefaultConstraint.State == SqlSmoState.Creating);
        }

        /// <summary>
        /// Returns true if the parent object is memory optimized
        /// </summary>        
        /// <returns></returns>        
        private bool IsParentMemoryOptimized()
        {
            bool isMemoryOptimized = false;
            if (((this.Parent is Table) || (this.Parent is UserDefinedTableType)) && this.Parent.IsSupportedProperty("IsMemoryOptimized"))
            {
                object value = this.Parent.GetPropValueOptional("IsMemoryOptimized");
                if (value != null)
                {
                    isMemoryOptimized = Convert.ToBoolean(value);
                }
            }
            return isMemoryOptimized;
        }

        /// <summary>
        /// Returns true if the current column should have collation
        /// </summary>
        /// <param name="so"></param>
        /// <returns></returns>
        private bool RequiresCollate(ScriptingPreferences sp)
        {
            // In Hekaton, indexes can be created on string columns only if they use BIN2 collation.
            // Similarly, Always Encrypted requires string columns (nvarchar, etc.) to use a BIN2 collations 
            // In SSMS, scripting collation option is set to false as default and so scripting through UI would give result without collation.
            // For better user experience, we script collation for memory optimized table or user-defined table types at all times.            
            bool scriptCollation = (sp.IncludeScripts.Collation || this.IsParentMemoryOptimized() || this.IsEncrypted);

            if (ServerVersion.Major > 7
                    && scriptCollation
                    && CompatibilityLevel.Version70 < GetCompatibilityLevel()
                    && UserDefinedDataType.IsSystemType(this, sp) //check is system because TypeAllowsCollation checks only the name , not the schema
                    && UserDefinedDataType.TypeAllowsCollation((string)GetPropValue("DataType"), this.StringComparer))
            {
                return true;
            }

            return false;
        }


        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (ParentColl.ParentInstance is UserDefinedFunction)
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.OperationNotSupportedWhenPartOfAUDF);
            }

            if (!IsObjectDirty())
            {
                return;
            }

            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }
            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Alter, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            bool mainAlterQuery = false;

            // alter the column type 
            // SQL 2000 and DW do not support "XmlSchemaNamespace" property, so extra care is taken to skip it
            if (Properties.Get("Collation").Dirty ||
                Properties.Get("Nullable").Dirty ||
                Properties.Get("DataType").Dirty ||
                Properties.Get("DataTypeSchema").Dirty ||
                Properties.Get("Length").Dirty ||
                Properties.Get("NumericPrecision").Dirty ||
                Properties.Get("NumericScale").Dirty ||
                (IsSupportedProperty("XmlSchemaNamespace") && Properties.Get("XmlSchemaNamespace").Dirty)
                )
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ALTER COLUMN {1} ",
                    ParentColl.ParentInstance.FullQualifiedName, FullQualifiedName);
                UserDefinedDataType.AppendScriptTypeDefinition(sb, sp, this, DataType.SqlDataType);

                if (RequiresCollate(sp))
                {
                    Property propCollation = Properties.Get("Collation");
                    string sCollation = propCollation.Value as string;
                    if (propCollation.Dirty && null != sCollation && 0 < sCollation.Length)
                    {
                        CheckCollation(sCollation, sp);
                        sb.AppendFormat(SmoApplication.DefaultCulture, " COLLATE {0}", sCollation);
                    }
                }
                
                if(this.CheckIsExternalTableColumn(sp) == false || sp.TargetDatabaseEngineEdition != Common.DatabaseEngineEdition.SqlOnDemand)
                {
                    if ((bool)Properties["Nullable"].Value)
                    {
                        sb.Append(" NULL");
                    }
                    else
                    {
                        sb.Append(" NOT NULL");
                    }
                }

                alterQuery.Add(sb.ToString());
                mainAlterQuery = true;
                sb.Length = 0;
            }

            // if column has rowguid col then add it here
            Property propRowGuidCol = Properties.Get("RowGuidCol");
            if (propRowGuidCol.Dirty &&
                0 == string.Compare((string)Properties["DataType"].Value, "uniqueidentifier", StringComparison.OrdinalIgnoreCase))
            {
                if ((bool)propRowGuidCol.Value ^ (bool)GetRealValue(propRowGuidCol, oldRowGuidColValue))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER TABLE {0} ALTER COLUMN {1}",
                        ParentColl.ParentInstance.FullQualifiedName, FullQualifiedName);
                    if ((bool)propRowGuidCol.Value)
                    {
                        sb.Append(" ADD");
                    }
                    else
                    {
                        sb.Append(" DROP");
                    }
                    sb.Append(" ROWGUIDCOL ");
                    alterQuery.Add(sb.ToString());
                    sb.Length = 0;
                }
            }

            // mark the column as persisted if needed
            if (sp.TargetServerVersion >= SqlServerVersion.Version90)
            {
                Property propPersisted = Properties.Get("IsPersisted");
                if (propPersisted.Dirty)
                {
                    alterQuery.Add(string.Format(SmoApplication.DefaultCulture,
                        "ALTER TABLE {0} ALTER COLUMN {1} {2} PERSISTED",
                        ParentColl.ParentInstance.FullQualifiedName, FullQualifiedName,
                        (bool)propPersisted.Value ? "ADD" : "DROP"));
                }
            }

            bool isColumnSet = false;
            bool isGeneratedAlwaysColumn = false;
            bool isComputedColumn = false;
            bool isFileStreamColumn = false;
            bool isEncryptedColumn = false;

            if (IsSupportedProperty("IsColumnSet", sp))
            {
                if (null != Properties.Get("IsColumnSet").Value)
                {
                    isColumnSet = (bool)Properties.Get("IsColumnSet").Value;
                }

                Property propSparse = Properties.Get("IsSparse");
                if (propSparse.Dirty)
                {
                    alterQuery.Add(string.Format(SmoApplication.DefaultCulture,
                        "ALTER TABLE {0} ALTER COLUMN {1} {2} SPARSE", ParentColl.ParentInstance.FullQualifiedName,
                        FullQualifiedName, (bool)propSparse.Value ? "ADD" : "DROP"));
                }
            }

            if (IsSupportedProperty("GeneratedAlwaysType", sp))
            {
                object value = this.GetPropValueOptional("GeneratedAlwaysType");
                if (value != null)
                {
                    isGeneratedAlwaysColumn = (GeneratedAlwaysType)value != Smo.GeneratedAlwaysType.None;
                }
            }

            if (null != Properties.Get("Computed").Value)
            {
                isComputedColumn = (bool)Properties.Get("Computed").Value;
            }

            if (IsSupportedProperty("IsFileStream", sp))
            {
                if (null != Properties.Get("IsFileStream").Value)
                {
                    isFileStreamColumn = (bool)Properties.Get("IsFileStream").Value;
                }
            }

            if (IsSupportedProperty("ColumnEncryptionKeyName", sp))
            {
                object columnEncryptionKeyName = Properties.Get("ColumnEncryptionKeyName").Value;
                if (null != Properties.Get("ColumnEncryptionKeyName").Value)
                {
                    isEncryptedColumn = true;
                }
            }

            if (IsSupportedProperty("IsMasked", sp))
            {
                bool isParentTable = this.Parent is Table;
                bool isMaskedColumn = false;
                bool isOldMaskedColumn = false;
                string maskingFunction = null;

                if (null != Properties.Get("IsMasked").Value)
                {
                    Property isMaskedProperty = Properties.Get("IsMasked");
                    isMaskedColumn = (bool)isMaskedProperty.Value;
                    isOldMaskedColumn = isMaskedProperty.Dirty ? !isMaskedColumn : isMaskedColumn;

                    if (isMaskedColumn)
                    {
                        maskingFunction = GetAndValidateMaskingFunction(isMaskedColumn);

                        if (!Util.IsNullOrWhiteSpace(maskingFunction))
                        {
                            if (!isParentTable)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnNonTables);
                            }
                            if (isColumnSet)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnColumnSet);
                            }
                            if (isGeneratedAlwaysColumn)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnTemporalColumns);
                            }
                            if (isComputedColumn)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnComputedColumns);
                            }
                            if (isFileStreamColumn)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnFileStreamColumns);
                            }
                            if (isEncryptedColumn)
                            {
                                throw new WrongPropertyValueException(ExceptionTemplates.NoDataMaskingOnEncryptedColumns);
                            }
                        }
                    }
                }

                // We will never supply the data masking options in the mainAlterQuery

                // This leaves us with two cases:
                // 1. mainAlterQuery is 'true':
                //    This causes the masking function to be dropped (if exists) since we didn't supply it in mainAlterQuery
                //    We will ADD the masking function in a separate ALTER statement if we still have masking
                // 2. mainAlterQuery is 'false' (if the column's type, collation, nullness has not changed)
                //    We will first DROP the masking function from the column (if previouly have one before the change), 
                //    and then ADD the function (if exists) after the change in separate ALTER statements
                if (!mainAlterQuery && isOldMaskedColumn)
                {
                    // No mainAlterQuery to drop the masking function for us
                    // We will create a separate ALTER statement to do that
                    alterQuery.Add(String.Format(SmoApplication.DefaultCulture,
                        "ALTER TABLE {0} ALTER COLUMN {1} DROP MASKED",
                        ParentColl.ParentInstance.FullQualifiedName,
                        FullQualifiedName));
                }

                // No we can assume that there is no masking function in the column
                // We only need to add the new masking function (if there is one)
                if (isMaskedColumn)
                {
                    alterQuery.Add(String.Format(SmoApplication.DefaultCulture,
                        "ALTER TABLE {0} ALTER COLUMN {1} ADD MASKED WITH (FUNCTION = '{2}')",
                        ParentColl.ParentInstance.FullQualifiedName,
                        FullQualifiedName, maskingFunction));
                }
            }

            ScriptDefaultAndRuleBinding(alterQuery, sp);
            ScriptDataClassification(alterQuery, sp);
        }

        /// <summary>
        /// Validates and returns the masking function stored in the MaskingFunction column property.
        /// If the IsMasked column property is 'false', returns 'null' instead.
        /// </summary>
        /// <param name="isMaskedColumn">The value of the IsMasked property set on the column.</param>
        /// <returns>A string representing the masking function applied to the column on the MaskingFunction property.</returns>
        private string GetAndValidateMaskingFunction(bool isMaskedColumn)
        {
            string maskingFunction = null;

            var maskingFunctionObj = GetPropValueOptional(nameof(MaskingFunction));
            if (isMaskedColumn && (null != maskingFunctionObj))
            {
                maskingFunction = (string)maskingFunctionObj;
                // We no longer validate masking functions on the client. 
                // When SQL Server adds new ones we don't want to have to update SMO to support them.
            }
            return maskingFunction;
        }

        public void Rename(string newname)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Rename, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Rename, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            base.RenameImpl(newname);
        }

        internal override void ScriptRename(StringCollection renameQuery, ScriptingPreferences sp, string newName)
        {
            // the user is responsible to put the database in single user mode on 7.0 server
            AddDatabaseContext(renameQuery, sp);
            renameQuery.Add(string.Format(SmoApplication.DefaultCulture,
                                "EXEC dbo.sp_rename @objname=N'{0}.{1}', @newname=N'{2}', @objtype=N'COLUMN'",
                                SqlString(ParentColl.ParentInstance.FullQualifiedName),
                                SqlString(this.FullQualifiedName),
                                SqlString(newName)));
        }

        // generates script if the default is bound to this column
        internal void ScriptDefaultAndRuleBinding(StringCollection queries, ScriptingPreferences sp)
        {
            if (sp.TargetEngineIsAzureSqlDw())
            {
                return;
            }
            // we script bindings if we are in direct execution mode (ie called from Create())
            // or if we are scripting and we are not directed to ignore this column or the bindings
            if ((sp.OldOptions.Bindings && !this.IgnoreForScripting) || sp.ScriptForCreateDrop)
            {
                TableViewBase parent = (TableViewBase)ParentColl.ParentInstance;

                // the user defined default takes precedence
                if (!UserDefinedDefault)
                {
                    object oDefault = Properties.Get("Default").Value;

                    if (null != oDefault && String.Empty != (String)oDefault)
                    {
                        object oDefaultSchema = Properties.Get("DefaultSchema").Value;
                        if (sp.IncludeScripts.SchemaQualify)
                        {
                            queries.Add(GetBindDefaultScript(sp, (string)oDefaultSchema, (string)oDefault, true));
                        }
                        else
                        {
                            queries.Add(GetBindDefaultScript(sp, null, (string)oDefault, true));
                        }
                    }
                }

                object oRule = Properties.Get("Rule").Value;
                if (null != oRule && String.Empty != (String)oRule)
                {
                    object oRuleSchema = Properties.Get("RuleSchema").Value;
                    if (sp.IncludeScripts.SchemaQualify)
                    {
                        queries.Add(GetBindRuleScript(sp, (string)oRuleSchema, (string)oRule, true));
                    }
                    else
                    {
                        queries.Add(GetBindRuleScript(sp, null, (string)oRule, true));
                    }
                }

            }
        }

        /// <summary>
        /// Invokes sp_bindrule to bind the column to the given rule
        /// </summary>
        /// <param name="ruleSchema"></param>
        /// <param name="ruleName"></param>
        /// <exception cref="FailedOperationException">When the parent of the column is a UserDefinedTableType or View</exception>
        public void BindRule(string ruleSchema, string ruleName)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Bind, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Bind, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            BindRuleImpl(ruleSchema, ruleName, false);
        }

        /// <summary>
        /// Invokes sp_unbindrule to unbind the column from its current rule binding
        /// </summary>
        /// <exception cref="FailedOperationException">When the parent of the column is a UserDefinedTableType or View</exception>
        public void UnbindRule()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Unbind, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Unbind, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            UnbindRuleImpl(false);
        }

        /// <summary>
        /// Invokes sp_bindefault to bind the column to the given named default
        /// </summary>
        /// <param name="defaultSchema"></param>
        /// <param name="defaultName"></param>
        /// <exception cref="FailedOperationException">When the parent of the column is a View</exception>
        public void BindDefault(string defaultSchema, string defaultName)
        {
            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Bind, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            BindDefaultImpl(defaultSchema, defaultName, false);
        }

        /// <summary>
        /// Invokes sp_unbindefault to unbind the column from its current named default
        /// </summary>
        /// <exception cref="FailedOperationException">When the parent of the column is a View</exception>
        public void UnbindDefault()
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Unbind, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.Unbind, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            UnbindDefaultImpl(false);
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.Drop, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            base.MarkForDropImpl(dropOnAlter);
        }

        internal bool UserDefinedDefault
        {
            get
            {
                CheckObjectState();
                return null != this.DefaultConstraint;
            }
        }

        private void InitDefaultConstraint(bool forScripting = false)
        {
            CheckObjectState();
            // try to initialize the constraint if it has not been initialized
            // before and if the object has already been created
            if (!m_bDefaultInitialized && this.State != SqlSmoState.Creating && !this.IsDesignMode)
            {
                if (!string.IsNullOrEmpty(DefaultConstraintName))
                {
                    InitChildLevel(DefaultConstraint.UrnSuffix, new ScriptingPreferences(), forScripting);
                }
                m_bDefaultInitialized = true;
            }
        }

        // default constraint for this column
        DefaultConstraint defaultConstraint;
        // true if the default constraint object for this column is
        // created and name set, it does not imply that the proeprties 
        // have been retrieved
        internal bool m_bDefaultInitialized;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.ZeroToOne, SfcObjectFlags.Design)]
        public DefaultConstraint DefaultConstraint
        {
            get
            {
                InitDefaultConstraint();
                return defaultConstraint;
            }
            internal set
            {
                defaultConstraint = value;
                DefaultConstraintName = defaultConstraint == null ? String.Empty : defaultConstraint.Name;
            }
        }

        internal DefaultConstraint GetDefaultConstraintBaseByName(string name)
        {
            DefaultConstraint dcb = this.DefaultConstraint;
            if (null == dcb)
            {
                AddDefaultConstraint(name);
                defaultConstraint.SetState(SqlSmoState.Creating);
                return defaultConstraint;
            }
            if (null != name && name.Length != 0 && dcb.Name != name)
            {
                throw new SmoException(ExceptionTemplates.InnerException, new ArgumentException(ExceptionTemplates.ColumnHasNoDefault(this.Name, name)));
            }
            return dcb;
        }
        
        /// <summary>
        /// initializes data for this default, this is only used from prefetch
        /// </summary>
        /// <param name="constraintRow">DataRow containing constraint data</param>
        /// <param name="colIdx">index for the column containing the name of the constraint</param>
        /// <param name="forScripting">true is the initialization is for scripting
        /// that is we have to init all properties</param>
        internal void InitializeDefault(System.Data.IDataReader reader, int colIdx, bool forScripting)
        {
            Diagnostics.TraceHelper.Assert(null != reader, "reader == null");
            Diagnostics.TraceHelper.Assert(colIdx < reader.FieldCount, "colIdx >= reader.FieldCount");

            //
            // initalize the default
            //
            DefaultConstraintName = reader.GetString(colIdx);
            defaultConstraint = new DefaultConstraint(this, new SimpleObjectKey(DefaultConstraintName),
                SqlSmoState.Existing);
            // at this point the default is initialized, that is the object is created and the Name is set
            m_bDefaultInitialized = true;

            Property propText = defaultConstraint.Properties.Get("Text");
            Property propSysName = defaultConstraint.Properties.Get("IsSystemNamed");
            Property propIsFileTableDefined = defaultConstraint.Properties.Get("IsFileTableDefined");
            //
            // set the text value, might be DBNull if there are no rights to see it
            //
            if (forScripting || GetServerObject().IsInitField(typeof (DefaultConstraint), "Text"))
            {
                int textColumnIdx = -1;
                try
                {
                    textColumnIdx = reader.GetOrdinal("Text");
                }
                catch (IndexOutOfRangeException)
                {
                    Diagnostics.TraceHelper.Assert(false,
                        "Text column should be present when initializing for scripting" +
                        " or if it is an init field");
                }

                Object oText = reader.GetValue(textColumnIdx);

                Diagnostics.TraceHelper.Assert(propText.Type.Equals(typeof (string)),
                    "text for the default should be of type string");
                Diagnostics.TraceHelper.Assert(null != oText, "enumerator is expected to return DBNull instead of null");

                if (DBNull.Value.Equals(oText))
                {
                    // DBNull for strings is replaced with string empty
                    propText.SetValue(string.Empty);
                }
                else
                {
                    // there is a valid value, set it
                    propText.SetValue(oText);
                }

                propText.SetRetrieved(true);
            }

            //
            // set the IsSystemNamed value, might be DBNull if there are no rights to see it
            //
            if (forScripting || GetServerObject().IsInitField(typeof (DefaultConstraint), "IsSystemNamed"))
            {
                int isSystemNamedColumnIdx = -1;
                try
                {
                    isSystemNamedColumnIdx = reader.GetOrdinal("IsSystemNamed");
                }
                catch (IndexOutOfRangeException)
                {
                    Diagnostics.TraceHelper.Assert(false,
                        "IsSystemNamed column should be present when initializing for scripting" +
                        " or if it is an init field");
                }

                Object oSysName = reader.GetValue(isSystemNamedColumnIdx);

                Diagnostics.TraceHelper.Assert(propSysName.Type.Equals(typeof (bool)),
                    "IsSystemNamed should be of type bool");
                Diagnostics.TraceHelper.Assert(null != oSysName,
                    "enumerator is expected to return DBNull instead of null");

                if (DBNull.Value.Equals(oSysName))
                {
                    // DBNull for bools is replaced with null
                    propSysName.SetValue(null);
                }
                else
                {
                    // there is a valid value, set it
                    propSysName.SetValue(oSysName);
                }
                propSysName.SetRetrieved(true);
            }

            if (forScripting || GetServerObject().IsInitField(typeof(DefaultConstraint), "IsFileTableDefined"))
            {
                int isFileTableDefinedIdx = -1;
                try
                {
                    isFileTableDefinedIdx = reader.GetOrdinal("IsFileTableDefined");
                }
                catch (IndexOutOfRangeException)
                {
                    Diagnostics.TraceHelper.Assert(false,
                        "IsFileTableDefined column should be present when initializing for scripting" +
                        " or if it is an init field");
                }

                Object oSysName = reader.GetValue(isFileTableDefinedIdx);

                Diagnostics.TraceHelper.Assert(propIsFileTableDefined.Type.Equals(typeof(bool)),
                    "IsFileTableDefined should be of type bool");
                Diagnostics.TraceHelper.Assert(null != oSysName,
                    "enumerator is expected to return DBNull instead of null");

                if (DBNull.Value.Equals(oSysName))
                {
                    // DBNull for bools is replaced with null
                    propIsFileTableDefined.SetValue(null);
                }
                else
                {
                    // there is a valid value, set it
                    propIsFileTableDefined.SetValue(oSysName);
                }
                propIsFileTableDefined.SetRetrieved(true);
            }

            // mark the object as initialized for scripting if the Text and IsSystemNamed are available
            defaultConstraint.InitializedForScripting = propText.Retrieved && propSysName.Retrieved;
        }

        /// <summary>
        /// This method gets called from Drop method of DefaultConstraint. As DefaultConstraint
        /// doesn't belong to ParentCollection, so generic code unable to remove this reference.
        /// Calling this method doesn't ensure to remove the actual DefaultConstraint on server
        /// in connected mode
        /// </summary>
        internal void RemoveDefaultConstraint()
        {
            defaultConstraint = null;
        }


        public DefaultConstraint AddDefaultConstraint()
        {
            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.AddDefaultConstraint, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            CheckObjectState();
            return AddDefaultConstraint(null);
        }

        public DefaultConstraint AddDefaultConstraint(String name)
        {
            if (ParentColl.ParentInstance is View)
            {
                throw new FailedOperationException(ExceptionTemplates.AddDefaultConstraint, this, null, ExceptionTemplates.ViewColumnsCannotBeModified);
            }

            try
            {
                CheckObjectState();
                if (null != this.DefaultConstraint)
                {
                    throw new SmoException(ExceptionTemplates.InnerException, new ArgumentException(ExceptionTemplates.ColumnAlreadyHasDefault(this.Name)));
                }

                if (null == name)
                {
                    name = string.Format(SmoApplication.DefaultCulture, "DF_{0}_{1}", this.ParentColl.ParentInstance.InternalName, this.InternalName);
                }
                DefaultConstraintName = name;
                defaultConstraint = new DefaultConstraint(this, new SimpleObjectKey(name), SqlSmoState.Creating);
                m_bDefaultInitialized = true;
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.AddDefaultConstraint, this, e);
            }
            return defaultConstraint;

        }        

        public DataTable EnumUserPermissions(string username)
        {
            try
            {
                CheckObjectState();
                if (null == username)
                {
                    username = "";
                }

                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/Permission[@Grantee='{0}']", Urn.EscapeString(username)));
                req.Fields = new String[] { 
                    "Grantee", 
                    "Grantor", 
                    "PermissionState", 
                    "Code", 
                    "ObjectClass", 
                    "GranteeType", 
                    "GrantorType", 
                    "ColumnName" };

                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPermissions, this, e);
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            ArrayList propInfo = new ArrayList();
            if (this.IsSupportedObject<ExtendedProperty>())
            {
                propInfo.Add(new PropagateInfo(ServerVersion.Major < 8 ? null : ExtendedProperties, true,
                    ExtendedProperty.UrnSuffix));
            }
            if (this.Parent is Table)
            {
                bool isFileTableDefinedDefaultConstraint = false;
                InitDefaultConstraint(forScripting:true);
                if (DefaultConstraint != null && DefaultConstraint.IsSupportedProperty("IsFileTableDefined"))
                {
                    isFileTableDefinedDefaultConstraint = DefaultConstraint.GetPropValueOptional<bool>("IsFileTableDefined", false);
                }

                //FileTables define a set of system-defined default constraints which we don't want to script
                //out since they will be generated automatically by the server when the FileTable is created.
                //So skip any default constraints that are created by the server (IsFileTableDefined == true)
                if (!isFileTableDefinedDefaultConstraint)
                {
                    propInfo.Add(new PropagateInfo(DefaultConstraint, !this.EmbedDefaultConstraints(), "DefaultColumn"));
                }
            }

            PropagateInfo[] retArr = new PropagateInfo[propInfo.Count];
            propInfo.CopyTo(retArr, 0);

            return retArr;
        }

        // this function enumerates all the foreign keys for which this column is 
        // a referenced column. This is needed to support column deletion scenarios
        // where the user needs to drop all the ForeignKeys that are dependent 
        // on that column
        public DataTable EnumForeignKeys()
        {
            try
            {
                if (ParentColl.ParentInstance is UserDefinedTableType || ParentColl.ParentInstance is View)
                {
                    DataTable dt = new DataTable();
                    dt.Locale = CultureInfo.InvariantCulture;
                    return dt;
                }

                Urn dbUrn = ParentColl.ParentInstance.ParentColl.ParentInstance.Urn;

                // first get the list of foreign keys
                Request req = new Request(string.Format(SmoApplication.DefaultCulture, "{0}/Table/ForeignKey[@ReferencedTable='{1}']/Column[@ReferencedColumn='{2}']",
                                            dbUrn, Urn.EscapeString(this.ParentColl.ParentInstance.InternalName),
                                            Urn.EscapeString(this.Name)),
                                                                new string[] { "Urn" });
                DataTable dtFKs = this.ExecutionManager.GetEnumeratorData(req);

                // from FK list, build a query to get parent information
                StringBuilder fkFilter = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                fkFilter.Append(dbUrn);
                fkFilter.Append("/Table/ForeignKey[");

                bool hasFKs = false;
                foreach (DataRow dr in dtFKs.Rows)
                {
                    fkFilter.Append(hasFKs ? " or " : "");
                    fkFilter.AppendFormat(SmoApplication.DefaultCulture, "@Name='{0}'", Urn.EscapeString(((Urn)((string)dr[0])).GetNameForType("ForeignKey")));
                    hasFKs = true;
                }
                fkFilter.Append("]");

                if (hasFKs)
                {
                    req = new Request(fkFilter.ToString(),
                        new string[] { "Name" },
                        new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) });

                    // add table and schema for the parent table
                    req.ParentPropertiesRequests = new PropertiesRequest[1];
                    PropertiesRequest parentProps = new PropertiesRequest();
                    parentProps.Fields = new String[] { "Schema", "Name" };
                    parentProps.OrderByList = new OrderBy[] { 	new OrderBy("Schema", OrderBy.Direction.Asc), 
                                                                new OrderBy("Name", OrderBy.Direction.Asc) };
                    req.ParentPropertiesRequests[0] = parentProps;

                    return this.ExecutionManager.GetEnumeratorData(req);
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

                throw new FailedOperationException(ExceptionTemplates.EnumForeignKeys, this, e);
            }
        }

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.Data.DataColumn.set_Caption(System.String)")]
        public DataTable EnumIndexes()
        {
            try
            {
                Request req = new Request(this.Urn.Parent + string.Format(SmoApplication.DefaultCulture, "/Index/IndexedColumn[@Name='{0}']", this.Name));
                req.Fields = new String[] { "Urn" };
                req.OrderByList = new OrderBy[] { new OrderBy("Urn", OrderBy.Direction.Asc) };

                DataTable dt = this.ExecutionManager.GetEnumeratorData(req);
                dt.Columns[0].Caption = "Name";
                foreach (DataRow dr in dt.Rows)
                {
                    dr[0] = new Urn((string)dr[0]).GetAttribute("Name", "Index");
                }
                return dt;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumIndexes, this, e);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            this.dataType = null;
            m_bDefaultInitialized = false;
            oldRowGuidColValue = null;
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public void UpdateStatistics()
        {
            UpdateStatistics(StatisticsScanType.Default, 0, true);
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public void UpdateStatistics(StatisticsScanType scanType)
        {
            UpdateStatistics(scanType, 0, true);
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public void UpdateStatistics(StatisticsScanType scanType, int sampleValue)
        {
            UpdateStatistics(scanType, 0, true);
        }

        /// <summary>
        /// forces data distribution statistics update for a referenced index or all 
        /// indexes defined on a SQL Server table.
        /// </summary>
        public void UpdateStatistics(StatisticsScanType scanType, int sampleValue, bool recompute)
        {
            if (ParentColl.ParentInstance is UserDefinedTableType)
            {
                throw new FailedOperationException(ExceptionTemplates.UpdateStatistics, this, null, ExceptionTemplates.UDTTColumnsCannotBeModified);
            }

            CheckObjectState(true);
            TableViewBase twb = this.ParentColl.ParentInstance as TableViewBase;
            if (null == twb)
            {
                throw new FailedOperationException(ExceptionTemplates.UpdateStatistics, this, null, ExceptionTemplates.TableOrViewParentForUpdateStatistics);
            }
            twb.UpdateStatistics(StatisticsTarget.All, scanType, sampleValue, recompute);
        }

        #region TextModeImpl

        /// <summary>
        /// validates that a property can change if the column is part of a text object
        ///	A column is locked for updates when its collection is locked.
        /// This happens when the collection is part of a text object with TextMode true.
        /// </summary>
        private void ValidatePropertyChangeForText(Property prop, object value)
        {
            SmoCollectionBase scb = this.ParentColl as SmoCollectionBase;
            if (null != scb && scb.IsCollectionLocked)
            {
                Validate_set_ChildTextObjectDDLProperty(prop, value);
            }
        }

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            switch (this.ServerVersion.Major)
            {
                case 7:
                    switch (prop.Name)
                    {
                        case "Computed":
                            ValidatePropertyChangeForText(prop, value);
                            break;
                        case "ComputedText": goto case "Computed";
                        case "Default": goto case "Computed";
                        case "DefaultSchema": goto case "Computed";
                        case "Identity": goto case "Computed";
                        case "IdentityIncrement": goto case "Computed";
                        case "IdentityIncrementAsDecimal": goto case "Computed";
                        case "IdentitySeed": goto case "Computed";
                        case "IdentitySeedAsDecimal": goto case "Computed";
                        case "NotForReplication": goto case "Computed";
                        case "Nullable": goto case "Computed";
                        case "RowGuidCol":
                            if (!prop.Dirty)
                            {
                                oldRowGuidColValue = prop.Value;
                            }
                            goto case "Computed";
                        case "Rule": goto case "Computed";
                        case "RuleSchema": goto case "Computed";

                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                case 8:
                    switch (prop.Name)
                    {
                        case "Collation":
                            ValidatePropertyChangeForText(prop, value);
                            break;
                        case "Computed": goto case "Collation";
                        case "ComputedText": goto case "Collation";
                        case "Default": goto case "Collation";
                        case "DefaultSchema": goto case "Collation";
                        case "Identity": goto case "Collation";
                        case "IdentityIncrement": goto case "Collation";
                        case "IdentitySeed": goto case "Collation";
                        case "IdentityIncrementAsDecimal": goto case "Collation";
                        case "IdentitySeedAsDecimal": goto case "Collation";
                        case "NotForReplication": goto case "Collation";
                        case "Nullable": goto case "Collation";
                        case "RowGuidCol":
                            if (!prop.Dirty)
                            {
                                oldRowGuidColValue = prop.Value;
                            }
                            goto case "Collation";
                        case "Rule": goto case "Collation";
                        case "RuleSchema": goto case "Collation";
                        default:
                            // other properties are not validated
                            break;
                    }
                    break;
                case 9: goto case 8;
                default: goto case 9;
            }
        }

        internal override string ScriptName
        {
            get { return base.ScriptName; }
            set { ((ScriptSchemaObjectBase)ParentColl.ParentInstance).CheckTextModeAccess("ScriptName"); base.ScriptName = value; }
        }
        #endregion

        // old RowGuidCol value
        internal object oldRowGuidColValue = null;
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
            // Column should always have a parent,.
            Diagnostics.TraceHelper.Assert(null != parentType, "null == parentType");
            // in the case of views we don't need to prefetch all those 
            // properties for scripting
            if (!parentType.Equals(typeof(View)) || !defaultTextMode)
            {
                string[] fields =  {

                        nameof(AnsiPaddingStatus),
                        nameof(Collation),
                        nameof(ColumnEncryptionKeyID),
                        nameof(ColumnEncryptionKeyName),
                        nameof(Computed),
                        nameof(ComputedText),
                        "DataTypeSchema",
                        nameof(Default), 
                        nameof(DefaultConstraintName),
                        nameof(DefaultSchema),
                        nameof(DistributionColumnName),
                        nameof(EncryptionAlgorithm),
                        nameof(EncryptionType),
                        nameof(GeneratedAlwaysType),
                        nameof(GraphType),
                        nameof(Identity),
                        nameof(IdentitySeedAsDecimal),
                        nameof(IdentityIncrementAsDecimal),
                        nameof(IsClassified),
                        nameof(IsColumnSet),
                        nameof(IsDistributedColumn),
                        nameof(IsDroppedLedgerColumn),
                        nameof(IsFileStream),
                        nameof(IsForeignKey),
                        nameof(IsHidden),
                        nameof(IsMasked),
                        nameof(IsPersisted),
                        nameof(IsSparse),
                        "Length",
                        nameof(MaskingFunction),
                        nameof(NotForReplication),
                        nameof(Nullable),
                        "NumericScale",
                        "NumericPrecision",
                        nameof(RowGuidCol),
                        nameof(Rule),
                        nameof(RuleSchema),
                        nameof(SensitivityLabelId),
                        nameof(SensitivityLabelName),
                        nameof(SensitivityInformationTypeId),
                        nameof(SensitivityInformationTypeName),
                        nameof(SensitivityRank),
                        "SystemType",
                        "XmlSchemaNamespace",
                        "XmlSchemaNamespaceSchema",
                        nameof(XmlDocumentConstraint),
                    };
                List<string> list = GetSupportedScriptFields(typeof(Column.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
                list.Add("DataType");
                return list.ToArray();
            }
            else
            {
                return new string[] { };
            }

        }

        /// <summary>
        /// Checks if the column belongs to an external table.
        /// </summary>
        /// <param name="sp">Scripting preferences.</param>
        /// <returns>True if the column belongs to an external table; false otherwise.</returns>
        private bool CheckIsExternalTableColumn(ScriptingPreferences sp)
        {
            bool isExternal = false;

            if (this.Parent.IsSupportedProperty("IsExternal", sp))
            {
                // check if a column is an external table column
                if (this.Parent.GetPropValueOptional("IsExternal", false))
                {
                    isExternal = true;
                }
            }

            return isExternal;
        }

        
        /// <summary>
        /// This method determines if this column is a graph computed column. Graph computed
        /// columns are exposed in select * queries and have graph type identifiers
        /// of 2, 5 and 8.
        /// </summary>
        /// <returns>True if the column is an internal graph column, False otherwise.</returns>
        internal bool IsGraphComputedColumn()
        {
            if (IsSupportedProperty("GraphType"))
            {
                Property type = Properties.Get("GraphType");

                if (type.IsNull)
                {
                    return false;
                }

                return (GraphType)type.Value == GraphType.GraphFromIdComputed ||
                    (GraphType)type.Value == GraphType.GraphToIdComputed ||
                    (GraphType)type.Value == GraphType.GraphIdComputed;
            }

            return false;
        }

        /// <summary>
        /// This method determines if this column is an internal graph column. Internal
        /// graph columns are not exposed in select * queries and have graph type identifiers
        /// of 1, 3, 4, 6, and 7.
        /// </summary>
        /// <returns>True if the column is an internal graph column, False otherwise.</returns>
        internal bool IsGraphInternalColumn()
        {
            if (IsSupportedProperty("GraphType"))
            {
                Property type = Properties.Get("GraphType");

                if (type.IsNull)
                {
                    return false;
                }

                return (GraphType)type.Value == GraphType.GraphId ||
                    (GraphType)type.Value == GraphType.GraphFromId ||
                    (GraphType)type.Value == GraphType.GraphFromObjId ||
                    (GraphType)type.Value == GraphType.GraphToId ||
                    (GraphType)type.Value == GraphType.GraphToObjId;
            }

            return false;
        }

        /// <summary>
        /// This method determines if this column is a dropped ledger column. Dropped
        /// ledger columns are not exposed in select * queries and are not included in
        /// table script generation.
        /// </summary>
        /// <returns>True if the column is a dropped ledger column, False otherwise.</returns>
        internal bool DroppedLedgerColumn()
        {
            return GetPropValueOptional<bool>(nameof(IsDroppedLedgerColumn), false);
        }
    }
}
