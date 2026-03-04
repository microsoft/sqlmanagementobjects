// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Represents a sql server Column Master Key object
    ///</summary>
    [Facets.StateChangeEvent("CREATE_COLUMN_MASTER_KEY", "COLUMN_MASTER_KEY", "COLUMN_MASTER_KEY")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class ColumnMasterKey : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        internal ColumnMasterKey(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// </summary>
        /// <param name="parent">The parent database</param>
        /// <param name="name">The name of the column master key</param>
        /// <param name="keyStoreProviderName">Key store provider name</param>
        /// <param name="keyPath">Column master key path</param>
        /// <param name="signature">Column master key signature, this is ignored if AllowEnclaveComputations are not supported</param>
        /// <param name="allowEnclaveComputations">If this key can be used for enclave computations</param>
        public ColumnMasterKey(Database parent, string name, string keyStoreProviderName, string keyPath, bool allowEnclaveComputations, byte[] signature)
            : this(parent, name, keyStoreProviderName, keyPath)
        {
            if (!IsSupportedProperty("AllowEnclaveComputations") && allowEnclaveComputations)
            {
                throw new InvalidVersionSmoOperationException(ExceptionTemplates.PropertyCannotBeSetForVersion("AllowEnclaveComputations",
                                                                                                                "ColumnMasterKey",
                                                                                                                parent.Version.ToString()));
            }

            if (!IsSupportedProperty("Signature") && signature != null)
            {
                throw new InvalidVersionSmoOperationException(ExceptionTemplates.PropertyCannotBeSetForVersion("Signature",
                                                                                                                "ColumnMasterKey",
                                                                                                                parent.Version.ToString()));
            }

            this.AllowEnclaveComputations = allowEnclaveComputations;

            // ignore signature if enclave computations are not allowed
            if (allowEnclaveComputations)
            {
                this.Signature = signature;
            }
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values.
        /// </summary>
        /// <param name="parent">The parent database</param>
        /// <param name="name">The name of the column master key</param>
        /// <param name="keyStoreProviderName">Key store provider name</param>
        /// <param name="keyPath">Column master key path</param>
        public ColumnMasterKey(Database parent, string name, string keyStoreProviderName, string keyPath) : base()
        {
            this.Parent = parent;
            this.Name = name;
            this.KeyStoreProviderName = keyStoreProviderName;
            this.KeyPath = keyPath;

            if (IsSupportedProperty("AllowEnclaveComputations"))
            {
                this.AllowEnclaveComputations = false;
            }
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "ColumnMasterKey";
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
        /// The name of the column master key
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

        /// <summary>
        /// Returns Signature Value as a string with format 0x[0-9A-F]
        /// </summary>
        internal string SignatureAsSqlBinaryString
        {
            get
            {
                StringBuilder hexString = new StringBuilder(2 + this.Signature.Length * 2);
                hexString.Append(@"0x");

                foreach (byte b in this.Signature)
                {
                    hexString.AppendFormat("{0:X2}", b);
                }

                return hexString.ToString();
            }
        }

        /// <summary>
        /// Drop the column master key.
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
        /// Drop the column master key. Syntax of the statement is 
        ///     DROP COLUMN MASTER KEY key_name
        /// </summary>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            string fullyFormattedName = FormatFullNameForScripting(sp);

            this.ThrowIfNotSupported(this.GetType(),
                ExceptionTemplates.ColumnMasterKeyDownlevel(
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
                    "ColumnMasterKey", fullyFormattedName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Add existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(string.Format(SmoApplication.DefaultCulture,
                   Scripts.INCLUDE_EXISTS_COLUMN_MASTER_KEY, String.Empty, FormatFullNameForScripting(sp, false)));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            // Construct the drop statement
            sb.Append("DROP COLUMN MASTER KEY " + fullyFormattedName);

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
        /// Create the Security Policy
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Create the column master key. Syntax of the statement is 
        ///     CREATE COLUMN MASTER KEY key_name
        ///     WITH 
        ///     (
        ///         KEY_STORE_PROVIDER_NAME = 'key store provider name',  
        ///         KEY_PATH = 'key path
        ///         [, ENCLAVE_COMPUTATIONS (SIGNATURE = signature)]
        ///     )
        /// </summary>
        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            string fullyFormattedName = FormatFullNameForScripting(sp);

            this.ThrowIfNotSupported(this.GetType(),
                ExceptionTemplates.ColumnMasterKeyDownlevel(
                    fullyFormattedName,
                    GetSqlServerName(sp)),
                    sp: sp);

            StringBuilder sb = new StringBuilder();

            // Add header information
            if (sp.IncludeScripts.Header)
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    "ColumnMasterKey", fullyFormattedName,
                    DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            // Add existency check
            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_COLUMN_MASTER_KEY, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            var ssb = new ScriptStringBuilder(String.Format(SmoApplication.DefaultCulture, "CREATE COLUMN MASTER KEY {0}{1}WITH{1}", fullyFormattedName, sp.NewLine), sp);
            ssb.SetParameter("KEY_STORE_PROVIDER_NAME", KeyStoreProviderName, ParameterValueFormat.NVarCharString);
            ssb.SetParameter("KEY_PATH", KeyPath, ParameterValueFormat.NVarCharString);

            if (IsSupportedProperty("AllowEnclaveComputations", sp))
            {
                if (this.AllowEnclaveComputations)
                {
                    var parameterList = new List<IScriptStringBuilderParameter>();
                    parameterList.Add(new ScriptStringBuilderParameter("SIGNATURE", SignatureAsSqlBinaryString, ParameterValueFormat.NotString));
                    ssb.SetParameter("ENCLAVE_COMPUTATIONS", parameterList);
                }
            }

            sb.Append(ssb.ToString(scriptSemiColon: false, pretty: true));

            // End the existency check
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
                sb.Append(sp.NewLine);
            }

            createQuery.Add(sb.ToString());
        }

        /// <summary>
        /// Script the column master key
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
        /// Retrieves all column encryption key values encrypted with this column master key
        /// </summary>
        /// <returns></returns>
        public IList<ColumnEncryptionKeyValue> GetColumnEncryptionKeyValuesEncrypted()
        {
            IList<ColumnEncryptionKeyValue> output = new List<ColumnEncryptionKeyValue>();

            string cmkInfoQuery = @"select [cek].[name] [cekName]
                                    from [sys].[column_encryption_keys] [cek]
                                    join [sys].[column_encryption_key_values] [cekv]
                                    on [cek].[column_encryption_key_id] = [cekv].[column_encryption_key_id]
                                    where [column_master_key_id] = " + this.ID;

            DataSet set = this.Parent.ExecuteWithResults(cmkInfoQuery);

            this.Parent.ColumnEncryptionKeys.Refresh();

            foreach (DataRow row in set.Tables[0].Rows)
            {
                foreach (ColumnEncryptionKeyValue value in this.Parent.ColumnEncryptionKeys[(string)row["cekName"]].ColumnEncryptionKeyValues)
                {
                    if (value.ColumnMasterKeyID == this.ID)
                    {
                        output.Add(value);
                        break;
                    }
                }
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
            string[] fields = {     
                                    "KeyStoreProviderName",
                                    "KeyPath",
                                    "AllowEnclaveComputations",
                                    "Signature"
                              };
            List<string> list = GetSupportedScriptFields(typeof(ColumnMasterKey.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}

