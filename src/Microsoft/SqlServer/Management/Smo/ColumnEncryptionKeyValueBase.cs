// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server column encryption key value object.
    ///</summary>
    [Facets.StateChangeEvent("CREATE_COLUMN_ENCRYPTION_KEY", "COLUMN_ENCRYPTION_KEY", "COLUMN ENCRYPTION KEY")]
    [Facets.StateChangeEvent("ALTER_COLUMN_ENCRYPTION_KEY", "COLUMN_ENCRYPTION_KEY", "COLUMN ENCRYPTION KEY")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ColumnEncryptionKeyValue : SqlSmoObject, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, Cmn.IMarkForDrop
    {
        internal ColumnEncryptionKeyValue(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// </summary>
        /// <param name="parent">Parent Column Encryption Key</param>
        /// <param name="cmk">Column Master Key  object</param>
        /// <param name="encryptionAlgorithm">Encryption Algorithm</param>
        /// <param name="encryptedValue">Encrypted CEK value as a byte array</param>
        public ColumnEncryptionKeyValue(ColumnEncryptionKey parent, ColumnMasterKey cmk, string encryptionAlgorithm, byte[] encryptedValue)
        {
            this.key = new ColumnEncryptionKeyValueObjectKey(cmk.ID);
            this.SetParentImpl(parent);
            this.ColumnMasterKeyName = cmk.Name;
            this.EncryptionAlgorithm = encryptionAlgorithm;
            this.EncryptedValue = encryptedValue;
            this.ColumnEncryptionKeyName = parent.Name;
            this.ColumnMasterKeyID = cmk.ID;
        }

        /// <summary>
        /// Returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ColumnEncryptionKeyValue";
            }
        }

        /// <summary>
        /// Returns encrypted value as a string with format 0x[0-9A-F]
        /// </summary>
        public string EncryptedValueAsSqlBinaryString
        {
            get
            {
                byte[] encryptedValue = this.EncryptedValue;
                StringBuilder hexString = new StringBuilder(2 + encryptedValue.Length * 2);
                hexString.Append(@"0x");

                foreach (byte b in encryptedValue)
                {
                    hexString.AppendFormat("{0:X2}", b);
                }

                return hexString.ToString();
            }
        }

        /// <summary>
        /// Marks or unmarks the Column encryption key value for drop on the next alter called on the parent policy
        /// </summary>
        /// <param name="dropOnAlter">Whether the column encryption key value should be marked for drop.</param>
        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        /// <summary>
        /// Drop the Column encryption key value
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

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            string fullyFormattedCekName = this.Parent.FullQualifiedName;

            // Check if this feature is supported on the SQL Server
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                ThrowIfBelowVersion130(sp.TargetServerVersionInternal,
                    ExceptionTemplates.ColumnEncryptionKeyDownlevel(
                        fullyFormattedCekName,
                        GetSqlServerName(sp)
                        ));
            }

            CheckObjectState();

            StringBuilder sb = new StringBuilder();
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName()));
                sb.Append(sp.NewLine);
            }

            // Add existency check
            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_CEK_VALUE, String.Empty, this.Parent.ID, this.ColumnMasterKeyID);
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // Construct the Drop statement
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER COLUMN ENCRYPTION KEY {0}", fullyFormattedCekName);
            sb.Append(sp.NewLine);
            sb.AppendLine("DROP VALUE");
            sb.AppendFormat(SmoApplication.DefaultCulture, 
                            "({0}{1}COLUMN_MASTER_KEY = {2}{3})",
                            sp.NewLine,
                            Globals.tab, 
                            MakeSqlBraket(ColumnMasterKeyName),
                            sp.NewLine);

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
        /// Create the column encryption key value
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        //  ALTER COLUMN ENCRYPTION KEY cek_name
        //       ADD VALUE
        //        (
        //        COLUMN_MASTER_KEY = column_master_key_name, 
        //        ALGORITHM = 'algorithm_name',
        //        ENCRYPTED_VALUE = varbinary_literal
        //        )
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            string fullyFormattedCekName = this.Parent.FullQualifiedName;

            // Check if this feature is supported on the SQL Server
            if (Cmn.DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                ThrowIfBelowVersion130(sp.TargetServerVersionInternal,
                    ExceptionTemplates.ColumnEncryptionKeyDownlevel(
                        fullyFormattedCekName,
                        GetSqlServerName(sp)
                        ));
            }

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
                    "ColumnEncryptionKey", 
                    fullyFormattedCekName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Add existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_CEK_VALUE, String.Empty, this.Parent.ID, this.ColumnMasterKeyID);
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // Construct the ADD VALUE statement
            sb.AppendFormat(SmoApplication.DefaultCulture, "ALTER COLUMN ENCRYPTION KEY {0}", fullyFormattedCekName);
            sb.Append(sp.NewLine);
            sb.AppendLine("ADD VALUE");
            sb.AppendFormat(SmoApplication.DefaultCulture,
                            "({0}{1}COLUMN_MASTER_KEY = {2},{3}{4}ALGORITHM = '{5}',{6}{7}ENCRYPTED_VALUE = {8}{9})",
                            sp.NewLine,
                            Globals.tab,
                            MakeSqlBraket(ColumnMasterKeyName),
                            sp.NewLine,
                            Globals.tab,
                            EncryptionAlgorithm.Replace("'", "''"),
                            sp.NewLine,
                            Globals.tab,
                            EncryptedValueAsSqlBinaryString,
                            sp.NewLine);

            // End existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object. This is used by transfer.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">DatabaseEngineType of the server</param>
        /// <param name="databaseEngineEdition">DatabaseEngineEdition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {
                                   nameof(ColumnMasterKeyID),
                                   nameof(ColumnEncryptionKeyName),
                                   nameof(EncryptionAlgorithm),
                                   nameof(EncryptedValue),
                                   nameof(ColumnMasterKeyName)
                              };
            List<string> list = GetSupportedScriptFields(typeof(ColumnEncryptionKeyValue.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}