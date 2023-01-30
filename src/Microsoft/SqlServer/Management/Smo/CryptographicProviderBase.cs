// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class CryptographicProvider : NamedSmoObject, ICreatable, IAlterable, IDroppable, IDropIfExists, IScriptable
    {
        /// <summary>
        /// Constructs Credential object.
        /// </summary>
        /// <param name="parentColl">Parent Collection</param>
        /// <param name="key">Object key</param>
        /// <param name="state">Object state</param>
        internal CryptographicProvider(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
            this.Enabled = true;
        }

        /// <summary>
        /// Returns name of the type in the Urn Expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "CryptographicProvider";
            }
        }

        /// <summary>
        /// Name of CryptographicProvider
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
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
        /// Returns the Version of the External Provider
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Version Version
        {
            get
            {
                string versionString = (string)this.GetPropValue("VersionString");
                if (!string.IsNullOrEmpty(versionString))
                {
                    return new Version(versionString);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Returns the SqlCryptographic Version of the External Provider
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Version SqlCryptographicVersion
        {
            get
            {
                string sqlCryptographicVersionString = (string)this.GetPropValue("SqlCryptographicVersionString");
                if (!string.IsNullOrEmpty(sqlCryptographicVersionString))
                {
                    return new Version(sqlCryptographicVersionString);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Creates the Cryptographic Provider
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        /// <summary>
        /// Generates the script that creates the Cryptographic Provider
        /// </summary>
        /// <param name="queries">Collection of query lines of text</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            SqlSmoObject.ThrowIfBelowVersion100(sp.TargetServerVersionInternal);
            string dllPath = (string)this.GetPropValue("DllPath");
            if (string.IsNullOrEmpty(dllPath))
            {
                throw new PropertyNotSetException("DllPath");
            }

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(Scripts.INCLUDE_EXISTS_CRYPTOGRAPHIC_PROVIDER, "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture,
                "CREATE CRYPTOGRAPHIC PROVIDER {0} FROM FILE = {1}",
                FullQualifiedName, MakeSqlString(dllPath));

            queries.Add(sb.ToString());

            Property enable = Properties.Get("Enabled");
            if (enable.Dirty)
            {
                queries.Add(ScriptEnableDisable((bool)enable.Value));
            }
        }

        /// <summary>
        /// Alters the Cryptographic Provider
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        /// <summary>
        /// Generates the script to alter the Cryptographic Provider
        /// </summary>
        /// <param name="queries">Collection of query lines of text</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptAlter(StringCollection queries, ScriptingPreferences sp)
        {
            SqlSmoObject.ThrowIfBelowVersion100(sp.TargetServerVersionInternal);
            Property dllPath = Properties.Get("DllPath");
            if (dllPath.Dirty)
            {
                if (string.IsNullOrEmpty((string)dllPath.Value))
                {
                    throw new PropertyNotSetException("DllPath");
                }
                queries.Add(ScriptUpgrade((string)dllPath.Value));
            }

            Property enable = Properties.Get("Enabled");
            if (enable.Dirty)
            {
                queries.Add(ScriptEnableDisable((bool)enable.Value));
            }
        }

        /// <summary>
        /// Drops the Cryptographic Provider
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
        /// Generates script that drops the Cryptographic Provider
        /// </summary>
        /// <param name="queries">Collection of query lines of text</param>
        /// <param name="so">Scripting Options</param>
        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            SqlSmoObject.ThrowIfBelowVersion100(sp.TargetServerVersionInternal);
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            ScriptIncludeHeaders(sb, sp, UrnSuffix);
            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(Scripts.INCLUDE_EXISTS_CRYPTOGRAPHIC_PROVIDER, "", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP CRYPTOGRAPHIC PROVIDER {0}", FullQualifiedName);
            queries.Add(sb.ToString());
        }


        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        public void Upgrade(string pathToDll)
        {
            CheckObjectState(true);
            this.ThrowIfNotSupported(typeof(CryptographicProvider));
            try
            {
                if (!this.IsDesignMode)
                {
                    this.ExecutionManager.ExecuteNonQuery(ScriptUpgrade(pathToDll));
                }

                if (!this.ExecutionManager.Recording)
                {
                    this.SetDllPath(pathToDll);
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.UpgradeDll, this, e);
            }
        }

        private void SetDllPath(string pathToDll)
        {
            //lookup the property ordinal from name
            int dllPathSet = this.Properties.LookupID("DllPath", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(dllPathSet, pathToDll);
            //mark the property as retrived
            this.Properties.SetRetrieved(dllPathSet, true);
        }

        private void SetEnable(bool isEnabled)
        {
            //lookup the property ordinal from name
            int enableSet = this.Properties.LookupID("Enabled", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(enableSet, isEnabled);
            //mark the property as retrived
            this.Properties.SetRetrieved(enableSet, true);
        }

        private string ScriptUpgrade(string pathToDll)
        {
            string query = String.Format(SmoApplication.DefaultCulture, "ALTER CRYPTOGRAPHIC PROVIDER {0} FROM FILE = {1}", FullQualifiedName, MakeSqlString(pathToDll));
            return query;
        }

        /// <summary>
        /// Enables the Cryptographic Provider
        /// </summary>
        public void Enable()
        {
            CheckObjectState(true);
            this.ThrowIfNotSupported(typeof(CryptographicProvider));
            try
            {
                if (!this.IsDesignMode)
                {
                    this.ExecutionManager.ExecuteNonQuery(ScriptEnableDisable(true));
                }

                if (!this.ExecutionManager.Recording)
                {
                    if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                    {
                        SmoApplication.eventsSingleton.CallObjectAltered(GetServerObject(), new ObjectAlteredEventArgs(this.Urn, this));
                    }

                    this.SetEnable(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.ProviderEnable, this, e);
            }
        }

        /// <summary>
        /// Disables the Cryptographic Provider
        /// </summary>
        public void Disable()
        {
            CheckObjectState(true);
            this.ThrowIfNotSupported(typeof(CryptographicProvider));
            try
            {
                if (!this.IsDesignMode)
                {
                    this.ExecutionManager.ExecuteNonQuery(ScriptEnableDisable(false));
                }

                if (!this.ExecutionManager.Recording)
                {
                    if (!SmoApplication.eventsSingleton.IsNullObjectAltered())
                    {
                        SmoApplication.eventsSingleton.CallObjectAltered(GetServerObject(), new ObjectAlteredEventArgs(this.Urn, this));
                    }

                    this.SetEnable(false);
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.ProviderDisable, this, e);
            }
        }

        /// <summary>
        /// Returns the script to enable/disable Cryptographic Provider
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        private string ScriptEnableDisable(bool enable)
        {
            string query = string.Format(SmoApplication.DefaultCulture, "ALTER CRYPTOGRAPHIC PROVIDER {0} {1}", FullQualifiedName, enable ? "ENABLE" : "DISABLE");
            return query;
        }

        /// <summary>
        /// Returns inromation and values about encryption algorithms supported by External Provider.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumEncryptionAlgorithms()
        {
            CheckObjectState(true);
            this.ThrowIfNotSupported(typeof(CryptographicProvider));
            try
            {
                string query = string.Format(SmoApplication.DefaultCulture, "SELECT * from sys.dm_cryptographic_provider_algorithms({0})", this.ID);

                // execute the script and return the DataTable
                return this.ExecutionManager.ExecuteWithResults(query).Tables[0];
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumEncryptionAlgorithms, this, e);
            }
        }

        /// <summary>
        /// Returns the provider keys mapped to the External Provider
        /// </summary>
        /// <returns></returns>
        public DataTable EnumProviderKeys()
        {
            CheckObjectState(true);
            this.ThrowIfNotSupported(typeof(CryptographicProvider));
            try
            {
                string query = string.Format(SmoApplication.DefaultCulture, "SELECT * from sys.dm_cryptographic_provider_keys({0})", this.ID);

                // execute the script and return the DataTable
                return this.ExecutionManager.ExecuteWithResults(query).Tables[0];
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.EnumProviderKeys, this, e);
            }
        }
    }
}

