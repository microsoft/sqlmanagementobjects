// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server column encryption key object
    ///</summary>
    [Facets.StateChangeEvent("CREATE_COLUMN_ENCRYPTION_KEY", "COLUMN_ENCRYPTION_KEY", "COLUMN_ENCRYPTION_KEY")]
    [Facets.StateChangeEvent("ALTER_COLUMN_ENCRYPTION_KEY", "COLUMN_ENCRYPTION_KEY", "COLUMN_ENCRYPTION_KEY")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ColumnEncryptionKey : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        internal ColumnEncryptionKey(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ColumnEncryptionKey";
            }
        }

        internal static string ParentType
        {
            get
            {
                return "DATABASE";
            }
        }

        /// <summary>
        /// The name of the column encryption key
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
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

        ColumnEncryptionKeyValueCollection m_cekValues = null;
        /// <summary>
        /// The collection of CEK Values for this CEK.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ColumnEncryptionKeyValue))]
        public ColumnEncryptionKeyValueCollection ColumnEncryptionKeyValues
        {
            get
            {
                if (m_cekValues == null)
                {
                    m_cekValues = new ColumnEncryptionKeyValueCollection(this);
                }

                return m_cekValues;
            }
        }

        /// <summary>
        /// Drop the column encryption key.
        /// </summary>
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

        /// <summary>
        /// Drop the column encryption key. Syntax of the statement is 
        ///     DROP COLUMN ENCRYPTION KEY key_name
        /// </summary>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            string fullyFormattedName = FormatFullNameForScripting(sp);

            this.ThrowIfNotSupported(this.GetType(),
                ExceptionTemplates.ColumnEncryptionKeyDownlevel(
                    fullyFormattedName,
                    GetSqlServerName(sp)),
                    sp: sp);

            CheckObjectState();

            StringBuilder sb = new StringBuilder();
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName()));
                sb.Append(sp.NewLine);
            }

            // Add header information
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "ColumnEncryptionKey", fullyFormattedName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Add existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_COLUMN_ENCRYPTION_KEY, String.Empty, FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // Construct the DROP statement
            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP COLUMN ENCRYPTION KEY {0}", fullyFormattedName);

            // End existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            dropQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Create the CEK
        /// </summary>
        public void Create()
        {
            if (this.ColumnEncryptionKeyValues.Count == 0)
            {
                throw new InvalidOperationException(ExceptionTemplates.ColumnEncryptionKeyNoValues(this.FullQualifiedName));
            }

            base.CreateImpl();
        }

        /// <summary>
        /// Create the column encryption key. Syntax of the statement is 
        ///         CREATE COLUMN ENCRYPTION KEY key_name 
        ///         WITH VALUES
        ///         (
        ///             COLUMN_MASTER_KEY = column_master_key_name, 
        ///             ALGORITHM = 'algorithm_name', 
        ///             ENCRYPTED_VALUE =  varbinary_literal
        ///         ) [,
        ///         (
        ///             COLUMN_MASTER_KEY = column_master_key_name, 
        ///             ALGORITHM = 'algorithm_name', 
        ///             ENCRYPTED_VALUE =  varbinary_literal
        ///         ) 
        ///         ]
        /// </summary>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            string fullyFormattedName = FormatFullNameForScripting(sp);

            this.ThrowIfNotSupported(this.GetType(),
                ExceptionTemplates.ColumnEncryptionKeyDownlevel(
                    fullyFormattedName,
                    GetSqlServerName(sp)),
                    sp: sp);

            StringBuilder sb = new StringBuilder();

            // Add header
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "ColumnEncryptionKey", fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Add existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_COLUMN_ENCRYPTION_KEY, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // Construct the create statement
            sb.AppendFormat(SmoApplication.DefaultCulture, "CREATE COLUMN ENCRYPTION KEY {0}", fullyFormattedName);
            sb.Append(sp.NewLine);
            sb.Append("WITH VALUES");
            sb.Append(sp.NewLine);

            bool firstCekValue = true;
            foreach (ColumnEncryptionKeyValue cekval in this.ColumnEncryptionKeyValues)
            {
                if (firstCekValue)
                {
                    firstCekValue = false;
                }
                else
                {
                    sb.AppendLine(",");
                }

                sb.AppendFormat(SmoApplication.DefaultCulture,
                                "({0}{1}COLUMN_MASTER_KEY = {2},{3}{4}ALGORITHM = '{5}',{6}{7}ENCRYPTED_VALUE = {8}{9})",
                                sp.NewLine,
                                Globals.tab,
                                MakeSqlBraket(cekval.ColumnMasterKeyName),
                                sp.NewLine,
                                Globals.tab,
                                cekval.EncryptionAlgorithm.Replace("'", "''"),
                                sp.NewLine,
                                Globals.tab,
                                cekval.EncryptedValueAsSqlBinaryString,
                                sp.NewLine);
            }

            // Close existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Alter the CEK
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Alter CEK statement only allows on addition or drop at a time. 
        /// Here is the syntax for it
        /// 
        /// ALTER COLUMN ENCRYPTION KEY key_name
        /// ADD VALUE
        /// (
        ///         COLUMN_MASTER_KEY = column_master_key_name, 
        ///         ALGORITHM = 'algorithm_name', 
        ///         ENCRYPTED_VALUE =  varbinary_literal 
        /// )
        /// 
        /// OR
        /// 
        /// ALTER COLUMN ENCRYPTION KEY key_name
        /// DROP VALUE
        /// (
        ///         COLUMN_MASTER_KEY = column_master_key_name
        /// ) 
        /// </summary>
        /// <param name="alterQuery"></param>
        /// <param name="sp"></param>
        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (this.State == SqlSmoState.Creating)
            {
                return;
            }

            string fullyFormattedName = FormatFullNameForScripting(sp);

            this.ThrowIfNotSupported(this.GetType(),
                ExceptionTemplates.ColumnEncryptionKeyDownlevel(
                    fullyFormattedName,
                    GetSqlServerName(sp)),
                    sp: sp);
            
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName()));
                sb.Append(sp.NewLine);
            }

            // Add header information
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "ColumnEncryptionKey", fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Construct the alter statements
            if (this.ColumnEncryptionKeyValues.Count > 0)
            {
                // Alter CEK values.
                //
                foreach (ColumnEncryptionKeyValue cekValue in this.ColumnEncryptionKeyValues)
                {
                    if (cekValue.State == SqlSmoState.Creating)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                            "ALTER COLUMN ENCRYPTION KEY {0}{1}ADD VALUE{1}",
                            fullyFormattedName,
                            sp.NewLine
                            );
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                        "({0}{1}COLUMN_MASTER_KEY = {2},{3}{4}ALGORITHM = '{5}',{6}{7}ENCRYPTED_VALUE = {8}{9})",
                                        sp.NewLine,
                                        Globals.tab,
                                        MakeSqlBraket(cekValue.ColumnMasterKeyName),
                                        sp.NewLine,
                                        Globals.tab,
                                        cekValue.EncryptionAlgorithm.Replace("'", "''"),
                                        sp.NewLine,
                                        Globals.tab,
                                        cekValue.EncryptedValueAsSqlBinaryString,
                                        sp.NewLine);
                    }
                    else if (cekValue.State == SqlSmoState.ToBeDropped)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER COLUMN ENCRYPTION KEY {0}", fullyFormattedName);
                        sb.Append(sp.NewLine);
                        sb.AppendLine("DROP VALUE");
                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                        "({0}{1}COLUMN_MASTER_KEY = {2}{3})",
                                        sp.NewLine,
                                        Globals.tab,
                                        MakeSqlBraket(cekValue.ColumnMasterKeyName),
                                        sp.NewLine);
                    }
                }

                sb.Append(sp.NewLine);
            }

            alterQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Script the Column Encryption Key
        /// </summary>
        public StringCollection Script()
        {
            return ScriptImpl();
        }

        /// <summary>
        /// Script object with specific scripting options
        /// </summary>
        /// <param name="scriptingOptions"></param>
        /// <returns></returns>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Propagate states, but not actions to ColumnEncryptionKeyValue.
        /// </summary>
        /// <returns>The collection of child ColumnEncryptionKeyValue to update. </returns>
        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            ArrayList propInfo = new ArrayList();
            propInfo.Add(new PropagateInfo(ColumnEncryptionKeyValues, false /* don't propagate actions */, false /* don't propagate actions */));
            PropagateInfo[] retArr = new PropagateInfo[propInfo.Count];
            propInfo.CopyTo(retArr, 0);

            return retArr;
        }

        /// <summary>
        /// Retrieves the list of all columns encrypted with this column encryption key
        /// </summary>
        /// <returns></returns>
        public IList<Column> GetColumnsEncrypted()
        {
            IList<Column> output = new List<Column>();

            string cekInfoQuery = @"select [schem].[name] [schemaName], [tab].[name] [tableName], [col].[name] [columnName]
                                    from [sys].[columns] [col]
                                    join [sys].[tables] [tab]
                                    on [col].[object_id] = [tab].[object_id]
                                    join [sys].[schemas] [schem]
                                    on [schem].[schema_id] = [tab].[schema_id]
                                    where [column_encryption_key_id] = " + this.ID;

            DataSet set = this.Parent.ExecuteWithResults(cekInfoQuery);

            this.Parent.Tables.Refresh();

            foreach (DataRow row in set.Tables[0].Rows)
            {
                string schemaName = (string)row["schemaName"];
                string tableName = (string)row["tableName"];
                string columnName = (string)row["columnName"];

                output.Add(this.Parent.Tables[tableName, schemaName].Columns[columnName]);
            }

            return output;
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object. This is used by transfer.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">DatabaseEngineType of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[] { };
        }
    }
}