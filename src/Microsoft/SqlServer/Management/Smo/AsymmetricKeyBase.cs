// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Specifies the source of the certificate, when loading.
    ///</summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum AsymmetricKeySourceType
    {
        /// Load the public key from file.
        File = 1,
        /// Load the public key from executable.
        Executable = 2,
        /// Load the public key from the specified assembly.
        SqlAssembly = 3,
        /// Load the public key from the Cryptographic Provider.
        Provider = 4
    }

    [Facets.StateChangeEvent("CREATE_ASYMMETRIC_KEY", "ASYMMETRICKEY", "ASYMMETRIC KEY")]
    [Facets.StateChangeEvent("ALTER_ASYMMETRIC_KEY", "ASYMMETRICKEY", "ASYMMETRIC KEY")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "ASYMMETRIC KEY")] // For Owner
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class AsymmetricKey : NamedSmoObject, Cmn.IAlterable, Cmn.IDroppable
    {
        /****************************
            public functions
        ****************************/

		///<summary>
		/// Creates an asymmetric key based on supplied encryption algorithm.
		///</summary>
		public void	Create(AsymmetricKeyEncryptionAlgorithm encryptionAlgorithm)
		{
			createInfo = new CreateInfo(encryptionAlgorithm, null);
			CreateImpl();
            this.SetProperties();
		}

		///<summary>
		/// Creates an asymmetric key based on supplied encryption algorithm. The password specifies the password the key is encrypted with.
		///</summary>
		public void	Create(AsymmetricKeyEncryptionAlgorithm encryptionAlgorithm, string password)
		{
			try
			{
				CheckNullArgument(password, "password");
				
				createInfo = new CreateInfo(encryptionAlgorithm, password);                
			}
			catch(Exception e)
			{
				FilterException(e);
				
				throw new FailedOperationException(ExceptionTemplates.Create, this, e);
			}
			CreateImpl();
            this.SetProperties();
		}        

        ///<summary>
        /// Loads the key from the source specified.
        ///</summary>
        public void Create(string keySource, AsymmetricKeySourceType sourceType)
        {
            createInfo = new CreateInfo(keySource, sourceType, null);
            CreateImpl();
        }

        ///<summary>
        /// Loads the key from the source specified. The password specifies the password the key is encrypted with. 
        ///</summary>
        public void Create(string keySource, AsymmetricKeySourceType sourceType, string password)
        {
            try
            {
                CheckNullArgument(password, "password");

                createInfo = new CreateInfo(keySource, sourceType, password);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();

        }

        public void Create(string providerAlgorithm, string providerKeyName, CreateDispositionType createDispositionType, AsymmetricKeySourceType sourceType)
        {
            try
            {
                ThrowIfBelowVersion100();
                CheckNullArgument(providerAlgorithm, "providerAlgorithm");
                CheckNullArgument(providerKeyName, "providerKeyName");
                ValidateAlgorithm(providerAlgorithm);
                createInfo = new CreateInfo(providerAlgorithm, providerKeyName, createDispositionType, sourceType);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        /// <summary>
        /// For design mode setting read only properties
        /// </summary>
        private void SetProperties()
        {
            if (this.ExecutionManager.Recording || !this.IsDesignMode)
            {
                return;
            }

            this.SetEncryptionAlgorithm();
            this.createInfo = null;
        }

        /// <summary>
        /// For design mode setting encryption algorithm which is a read only property
        /// </summary>
        private void SetEncryptionAlgorithm()
        {
            //lookup the property ordinal from name
            int keyEncryptionAlgorithmSet = this.Properties.LookupID("KeyEncryptionAlgorithm", PropertyAccessPurpose.Write);
            //set the new value
            this.Properties.SetValue(keyEncryptionAlgorithmSet, this.createInfo.encryptionAlgorithm);
            //mark the property as retrived
            this.Properties.SetRetrieved(keyEncryptionAlgorithmSet, true);
        }

        private void ValidateAlgorithm(string providerAlgorithm)
        {
            StringCollection validAlgorithms = new StringCollection();
            validAlgorithms.AddRange(new string[] { "RSA_512", "RSA_1024", "RSA_2048", "RSA_3072", "RSA_4096" });
            if (!validAlgorithms.Contains(providerAlgorithm.ToUpper()))
            {
                throw new ArgumentException(ExceptionTemplates.InvalidAlgorithm("AsymmetricKey", providerAlgorithm));
            }
        }

        ///<summary>
        /// Drops the object and removes it from the collection.
        ///</summary>
        public void Drop()
        {
            DropImpl();
        }

        public void Drop(bool removeProviderKey)
        {
            this.removeProviderKey = removeProviderKey;
            DropImpl();
        }

        private bool removeProviderKey = false;

        /// <summary>
        /// Add private key using a password.
        /// </summary>
        /// <param name="password"></param>
        public void AddPrivateKey(string password)
        {
            try
            {
                CheckObjectState();
                if (this.PrivateKeyEncryptionType == PrivateKeyEncryptionType.Provider)
                {
                    throw new Exception(ExceptionTemplates.CannotAlterKeyWithProvider);
                }

                CheckNullArgument(password, "password");

                //build script
                StringBuilder sb = new StringBuilder("ALTER ASYMMETRIC KEY ");
                ScriptingPreferences sp = new ScriptingPreferences();
                sb.Append(FormatFullNameForScripting(sp));
                sb.Append(sp.NewLine);

                sb.Append("WITH PRIVATE KEY (ENCRYPTION BY PASSWORD=");
                sb.Append(MakeSqlString(password));
                sb.Append(")");

                //execute in database
                this.Parent.ExecuteNonQuery(sb.ToString());

                sb.Length = 0;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddPrivateKey, this, e);
            }
        }

        ///<summary>
        /// Removes the private key from the asymmetric key.
        ///</summary>
        public void RemovePrivateKey()
        {
            try
            {
                CheckObjectState();
                if (this.PrivateKeyEncryptionType == PrivateKeyEncryptionType.Provider)
                {
                    throw new Exception(ExceptionTemplates.CannotAlterKeyWithProvider);
                }

                //build script
                StringBuilder sb = new StringBuilder("ALTER ASYMMETRIC KEY ");
                ScriptingPreferences sp = new ScriptingPreferences();
                sb.Append(FormatFullNameForScripting(sp));
                sb.Append(" REMOVE PRIVATE KEY");

                //execute in database
                this.Parent.ExecuteNonQuery(sb.ToString());
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemovePrivateKey, this, e);
            }
        }

        ///<summary>
        /// Persist all changes made in this object.
        ///</summary>
        public void Alter()
        {
            AlterImpl();
        }

        ///<summary>
        /// Changes the password that is used to secure the private key.
        ///</summary>
        public void ChangePrivateKeyPassword(string oldPassword, string newPassword)
        {
            try
            {
                CheckObjectState();
                if (this.PrivateKeyEncryptionType == PrivateKeyEncryptionType.Provider)
                {
                    throw new Exception(ExceptionTemplates.CannotAlterKeyWithProvider);
                }

                CheckNullArgument(oldPassword, "oldPassword");
                CheckNullArgument(newPassword, "newPassword");

                //build script
                StringBuilder sb = new StringBuilder("ALTER ASYMMETRIC KEY ");
                ScriptingPreferences sp = new ScriptingPreferences();
                sb.Append(FormatFullNameForScripting(sp));
                sb.Append(sp.NewLine);

                sb.Append("WITH PRIVATE KEY (DECRYPTION BY PASSWORD=");
                sb.Append(MakeSqlString(oldPassword));
                sb.Append(", ENCRYPTION BY PASSWORD=");
                sb.Append(MakeSqlString(newPassword));
                sb.Append(")");

                //execute in database
                this.Parent.ExecuteNonQuery(sb.ToString());

                sb.Length = 0;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePrivateKeyPassword, this, e);
            }
        }

        /****************************
            implementation functions
        ****************************/

        ///<summary>
        /// internal constructor
        ///</summary>
        internal AsymmetricKey(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        ///<summary>
        /// returns the name of the type in the urn expression
        ///</summary>
        public static string UrnSuffix
        {
            get
            {
                return "AsymmetricKey";
            }
        }

        // holds information necessary fot the CREATE script
        private class CreateInfo
        {
            //true if we create from a source
            public bool createWithSource;

            // create with alghorithm
            public AsymmetricKeyEncryptionAlgorithm encryptionAlgorithm;

            // create by importing from source
            public string keySource;
            public AsymmetricKeySourceType sourceType;

            // used in case of encryption by cryptographic provider
            public string providerAlgorithm;
            public string providerKeyName;
            public CreateDispositionType createDispositionType;

            //used in both methods of creating
            public SqlSecureString password;

            // init data for create with alghorithm
            public CreateInfo(AsymmetricKeyEncryptionAlgorithm encryptionAlgorithm, SqlSecureString password)
            {
                createWithSource = false;
                this.encryptionAlgorithm = encryptionAlgorithm;
                this.password = password;
            }

            // init data for create by importing from source
            public CreateInfo(string keySource, AsymmetricKeySourceType sourceType, SqlSecureString password)
            {
                createWithSource = true;
                this.password = password;
                this.keySource = keySource;
                this.sourceType = sourceType;
            }

            public CreateInfo(string providerAlgorithm, string providerKeyName, CreateDispositionType createDispositionType, AsymmetricKeySourceType sourceType)
            {
                createWithSource = true;
                this.providerAlgorithm = providerAlgorithm;
                this.providerKeyName = providerKeyName;
                this.createDispositionType = createDispositionType;
                this.sourceType = sourceType;
            }
        }

        //member variable holding information necessary fot the CREATE script
        private CreateInfo createInfo;

        /// <summary>
        /// Gets the non alterable properties for Asymmetric Keys
        /// </summary>
        /// <returns></returns>
        internal override string[] GetNonAlterableProperties()
        {
            return new string[] { "ProviderName" };
        }

        ///<summary>
        ///	ArgumentNullException is the argument is null
        /// should be inlined
        ///</summary>
        private void CheckNullArgument(object arg, string argName)
        {
            if (null == arg)
            {
                throw new ArgumentNullException(argName);
            }
        }

        ///<summary>
        /// build the alter script
        ///</summary>
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            // the only thing we can alter is the owner
            ScriptChangeOwner(query, sp);
        }

        ///<summary>
        /// build the drop script
        ///</summary>
        internal override void ScriptDrop(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder("DROP ASYMMETRIC KEY ");
            sb.Append(FormatFullNameForScripting(sp));

            if (removeProviderKey)
            {
                sb.Append(" REMOVE PROVIDER KEY");
            }

            query.Add(sb.ToString());
        }

        ///<summary>
        /// build the create script
        ///</summary>
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            // cleanup the side parameter
            // hide it and copy in local variable
            CreateInfo createInfo = this.createInfo;
            if (!this.IsDesignMode || this.ExecutionManager.Recording)
            {
                this.createInfo = null;
            }

            this.ThrowIfNotSupported(typeof(AsymmetricKey));

            Debug.Assert(null != createInfo, "caller should initialize createInfo it before calling");

            //build script
            StringBuilder sb = new StringBuilder("CREATE ASYMMETRIC KEY ");
            sb.Append(FormatFullNameForScripting(sp));
            sb.Append(sp.NewLine);

            //we add owner if set
            if (sp.IncludeScripts.Owner && (null != GetPropValueOptional("Owner")))
            {
                sb.Append("AUTHORIZATION ");
                sb.Append(MakeSqlBraket((string)GetPropValueOptional("Owner")));
                sb.Append(sp.NewLine);
            }

            if (createInfo.createWithSource)
            {
                // create by importing from source

                sb.Append("FROM ");
                switch (createInfo.sourceType)
                {
                    case AsymmetricKeySourceType.File:
                        Debug.Assert(null != createInfo.keySource, "keySource cannot be null");
                        sb.Append("FILE = ");
                        sb.Append(MakeSqlString(createInfo.keySource));
                        break;

                    case AsymmetricKeySourceType.Executable:
                        Debug.Assert(null != createInfo.keySource, "keySource cannot be null");
                        sb.Append("EXECUTABLE FILE = ");
                        sb.Append(MakeSqlString(createInfo.keySource));
                        break;

                    case AsymmetricKeySourceType.SqlAssembly:
                        Debug.Assert(null != createInfo.keySource, "keySource cannot be null");
                        sb.Append("ASSEMBLY ");
                        sb.Append(MakeSqlString(createInfo.keySource));
                        break;

                    case AsymmetricKeySourceType.Provider:
                        Debug.Assert(null != createInfo.providerKeyName, "providerKeyName cannot be null");
                        Debug.Assert(null != createInfo.providerAlgorithm, "providerAlgorithm cannot be null");
                        sb.Append("PROVIDER ");
                        string providerName = (string)this.GetPropValue("ProviderName");
                        if (string.IsNullOrEmpty(providerName))
                        {
                            throw new PropertyNotSetException("ProviderName");
                        }

                        sb.Append(MakeSqlBraket(providerName));
                        sb.Append(Globals.newline);
                        sb.Append("WITH ");
                        sb.Append(Globals.newline);
                        sb.Append(Globals.tab);
                        sb.Append(string.Format(SmoApplication.DefaultCulture, "PROVIDER_KEY_NAME = '{0}', ", createInfo.providerKeyName));
                        sb.Append(Globals.newline);
                        sb.Append(Globals.tab);
                        sb.Append(string.Format(SmoApplication.DefaultCulture, "ALGORITHM = {0}, ", createInfo.providerAlgorithm));
                        sb.Append(Globals.newline);
                        sb.Append(Globals.tab);
                        sb.Append(string.Format(SmoApplication.DefaultCulture, "CREATION_DISPOSITION = {0}", createInfo.createDispositionType == CreateDispositionType.CreateNew ? "CREATE_NEW" : "OPEN_EXISTING"));
                        break;

                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("AsymmetricKeySourceType"));
                }
            }
            else
            {
                // create with alghorithm
                sb.Append("WITH ALGORITHM = ");
                switch (createInfo.encryptionAlgorithm)
                {
                    case AsymmetricKeyEncryptionAlgorithm.Rsa512:
                        sb.Append("RSA_512");
                        break;
                    case AsymmetricKeyEncryptionAlgorithm.Rsa1024:
                        sb.Append("RSA_1024");
                        break;
                    case AsymmetricKeyEncryptionAlgorithm.Rsa2048:
                        sb.Append("RSA_2048");
                        break;
                    case AsymmetricKeyEncryptionAlgorithm.Rsa3072:
                        sb.Append("RSA_3072");
                        break;
                    case AsymmetricKeyEncryptionAlgorithm.Rsa4096:
                        sb.Append("RSA_4096");
                        break;
                    case AsymmetricKeyEncryptionAlgorithm.CryptographicProviderDefined:
                        throw new ArgumentException(ExceptionTemplates.SourceTypeShouldBeProvider);
                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("AsymmetricKeyEncryptionAlgorithm"));
                }

            }

            if (null != createInfo.password)
            {
                sb.Append(sp.NewLine);
                sb.Append("ENCRYPTION BY PASSWORD = ");
                sb.Append(MakeSqlString(createInfo.password.ToString()));
            }

            query.Add(sb.ToString());
            sb.Length = 0;
        }
    }
}



