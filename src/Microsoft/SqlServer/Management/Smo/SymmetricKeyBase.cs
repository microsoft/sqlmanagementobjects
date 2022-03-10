// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;


#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Specifies the type of encryption of a key.
    ///</summary>
    public enum KeyEncryptionType
    {
        /// Encrypted by symmetric key.
        SymmetricKey = 0,
        /// Encrypted by certificate.
        Certificate = 1,
        /// Encrypted by password.
        Password = 2,
        /// Encrypted by asymmetric key.
        AsymmetricKey = 3,
        /// Encryption by provider.
        Provider = 4
    }

    ///<summary>
    /// This object is used to specify an encryption type.
    ///</summary>
    public class SymmetricKeyEncryption
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public SymmetricKeyEncryption()
        {
        }

        /// <summary>
        /// Parameterized constructor - populates properties from parameter values
        /// </summary>
        /// <param name="encryptionType"></param>
        /// <param name="value"></param>
        public SymmetricKeyEncryption(KeyEncryptionType encryptionType, string value)
        {
            this.KeyEncryptionType = encryptionType;
            this.objectNameOrPassword = value;
        }

        private SqlSecureString objectNameOrPassword;

        ///Encryption type.
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public KeyEncryptionType KeyEncryptionType;
        ///Name of certificate, symmetric key, asymmetric key, or password, or provider name 
        ///depending on the EncryptionType property. 
        public string ObjectNameOrPassword
        {
            get { return objectNameOrPassword.ToString(); }
            set { objectNameOrPassword = value; }
        }
    }

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class SymmetricKey : NamedSmoObject, Cmn.IAlterable, Cmn.IDroppable
    {
        /****************************
            public functions
        ****************************/

        ///<summary>
        /// Persist all changes made in this object.        
        ///</summary>
        public void Alter()
        {
            AlterImpl();
        }

        ///<summary>
        /// Creates the object on the server. The keyEncryption parameter specifies the encryption type. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method.
        ///</summary>
        public void Create(SymmetricKeyEncryption keyEncryption,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));

                createInfo = new CreateInfo(new SymmetricKeyEncryption[] { keyEncryption }
                                            , keyEncryptionAlgorithm, null, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        ///<summary>
        /// Creates the object on the server. The keyEncryption parameter specifies the encryption type. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase 
        /// parameter specifies a pass phrase from which the symmetric key can be derived.
        ///</summary>
        public void Create(SymmetricKeyEncryption keyEncryption,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           string passPhrase)
        {
            Create(keyEncryption,
                   keyEncryptionAlgorithm,
                   passPhrase != null ? new SqlSecureString(passPhrase) : null);
        }

        ///<summary>
        /// Creates the object on the server. The keyEncryption parameter specifies the encryption type. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase 
        /// parameter specifies a pass phrase from which the symmetric key can be derived.
        ///</summary>
        public void Create(SymmetricKeyEncryption keyEncryption,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           System.Security.SecureString passPhrase)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckNullArgument(passPhrase, "passPhrase");

                if (passPhrase.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.PassPhraseNotSpecified);
                }

                createInfo = new CreateInfo(new SymmetricKeyEncryption[] { keyEncryption },
                                            keyEncryptionAlgorithm,
                                            passPhrase,
                                            null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        ///<summary>
        /// Creates the object on the server. The keyEncryption parameter specifies the encryption type. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase 
        /// parameter specifies a pass phrase from which the symmetric key can be derived. 
        /// The identityPhrase parameter is used to tag data (using a GUID based on the identity 
        /// phrase) that is encrypted with the key.
        ///</summary>
        public void Create(SymmetricKeyEncryption keyEncryption,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           string passPhrase,
                           string identityPhrase)
        {
            Create(keyEncryption,
                   keyEncryptionAlgorithm,
                   passPhrase != null ? new SqlSecureString(passPhrase) : null,
                   identityPhrase);
        }

        ///<summary>
        /// Creates the object on the server. The keyEncryption parameter specifies the encryption type. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase 
        /// parameter specifies a pass phrase from which the symmetric key can be derived. 
        /// The identityPhrase parameter is used to tag data (using a GUID based on the identity 
        /// phrase) that is encrypted with the key.
        ///</summary>
        public void Create(SymmetricKeyEncryption keyEncryption,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           System.Security.SecureString passPhrase,
                           string identityPhrase)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckNullArgument(passPhrase, "passPhrase");
                CheckNullArgument(identityPhrase, "identityPhrase");

                if (passPhrase.Length == 0 && identityPhrase.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.PassPhraseAndIdentityNotSpecified);
                }

                createInfo = new CreateInfo(new SymmetricKeyEncryption[] { keyEncryption },
                                            keyEncryptionAlgorithm,
                                            passPhrase.Length > 0 ? passPhrase : null,
                                            identityPhrase.Length > 0 ? identityPhrase : null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();

        }

        public void Create(SymmetricKeyEncryption keyEncryption, string providerAlgorithm, string providerKeyName, CreateDispositionType createDispositionType)
        {
            try
            {
                ThrowIfBelowVersion100();
                CheckNullArgument(providerAlgorithm, "providerAlgorithm");
                CheckNullArgument(providerKeyName, "providerKeyName");
                ValidateAlgorithm(providerAlgorithm);
                createInfo = new CreateInfo(keyEncryption, providerAlgorithm, providerKeyName, createDispositionType);
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        private void ValidateAlgorithm(string providerAlgorithm)
        {
            StringCollection validAlgorithms = new StringCollection();
            validAlgorithms.AddRange(new string[] { "DES", "TRIPLE_DES", "RC2", "RC4", "RC4_128", "DESX", "TRIPLE_DES_3KEY", "AES_128", "AES_192", "AES_256" });
            if (!validAlgorithms.Contains(providerAlgorithm.ToUpper()))
            {
                throw new ArgumentException(ExceptionTemplates.InvalidAlgorithm("SymmetricKey", providerAlgorithm));
            }
        }

        //
        // Array versions of above Create() methods 
        //

        ///<summary>
        /// Creates the object on the server. The keyEncryption parameter specifies an array of encryption types. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method.
        ///</summary>
        public void Create(SymmetricKeyEncryption[] keyEncryptions,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckNullArgument(keyEncryptions, "keyEncryptions");

                createInfo = new CreateInfo(keyEncryptions, keyEncryptionAlgorithm, null, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        ///<summary>
        /// Creates the object on the server. . The keyEncryption parameter specifies an array of encryption types. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase parameter 
        /// specifies a pass phrase from which the symmetric key can be derived.
        ///</summary>
        public void Create(SymmetricKeyEncryption[] keyEncryptions,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           string passPhrase)
        {
            Create(keyEncryptions,
                   keyEncryptionAlgorithm,
                   passPhrase != null ? new SqlSecureString(passPhrase) : null);
        }

        ///<summary>
        /// Creates the object on the server. . The keyEncryption parameter specifies an array of encryption types. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase parameter 
        /// specifies a pass phrase from which the symmetric key can be derived.
        ///</summary>
        public void Create(SymmetricKeyEncryption[] keyEncryptions,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           System.Security.SecureString passPhrase)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckNullArgument(keyEncryptions, "keyEncryptions");
                CheckNullArgument(passPhrase, "passPhrase");

                if (passPhrase.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.PassPhraseNotSpecified);
                }

                createInfo = new CreateInfo(keyEncryptions,
                                            keyEncryptionAlgorithm,
                                            passPhrase,
                                            null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        ///<summary>
        /// Creates the object on the server. . The keyEncryption parameter specifies an array of encryption types. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase parameter 
        /// specifies a pass phrase from which the symmetric key can be derived. The identityPhrase parameter 
        /// is used to tag data (using a GUID based on the identity phrase) that is encrypted with the key.
        ///</summary>
        public void Create(SymmetricKeyEncryption[] keyEncryptions,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           string passPhrase,
                           string identityPhrase)
        {
            Create(keyEncryptions,
                   keyEncryptionAlgorithm,
                   passPhrase != null ? new SqlSecureString(passPhrase) : null,
                   identityPhrase);
        }

        ///<summary>
        /// Creates the object on the server. . The keyEncryption parameter specifies an array of encryption types. 
        /// The keyEncryptionAlgorithm parameter specifies the encryption method. The passPhrase parameter 
        /// specifies a pass phrase from which the symmetric key can be derived. The identityPhrase parameter 
        /// is used to tag data (using a GUID based on the identity phrase) that is encrypted with the key.
        ///</summary>
        public void Create(SymmetricKeyEncryption[] keyEncryptions,
                           SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                           System.Security.SecureString passPhrase,
                           string identityPhrase)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckNullArgument(keyEncryptions, "keyEncryptions");
                CheckNullArgument(passPhrase, "passPhrase");
                CheckNullArgument(identityPhrase, "identityPhrase");

                if (passPhrase.Length == 0 && identityPhrase.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.PassPhraseAndIdentityNotSpecified);
                }

                createInfo = new CreateInfo(keyEncryptions,
                                            keyEncryptionAlgorithm,
                                            passPhrase.Length > 0 ? new SqlSecureString(passPhrase) : null,
                                            identityPhrase.Length > 0 ? identityPhrase : null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
            CreateImpl();
        }

        ///<summary>
        /// Drops the object and removes it from the collection.        
        ///</summary>
        public void Drop()
        {
            this.DropImpl();
        }


        public void Drop(bool removeProviderKey)
        {
            this.removeProviderKey = removeProviderKey;
            DropImpl();
        }

        private bool removeProviderKey = false;

        ///<summary>
        /// Key encryptions for this symmetric key.
        ///</summary>
        public DataTable EnumKeyEncryptions()
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);

                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/KeyEncryption"));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumKeyEncryptions, this, e);
            }
        }

        ///<summary>
        /// Adds a key encryption to the symmetric key.
        ///</summary>
        public void AddKeyEncryption(SymmetricKeyEncryption keyEncryption)
        {
            AddKeyEncryption(new SymmetricKeyEncryption[] { keyEncryption });
        }

        ///<summary>
        /// Adds one or more key encryptions to the symmetric key.
        ///</summary>
        public void AddKeyEncryption(SymmetricKeyEncryption[] keyEncryptions)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);
                if (this.EncryptionAlgorithm == SymmetricKeyEncryptionAlgorithm.CryptographicProviderDefined)
                {
                    throw new Exception(ExceptionTemplates.CannotAlterKeyWithProvider);
                }

                CheckNullArgument(keyEncryptions, "keyEncryptions");

                Diagnostics.TraceHelper.Assert(keyEncryptions.Length > 0);

                if (keyEncryptions.Length > 0)
                {
                    StringBuilder sb = new StringBuilder("ALTER SYMMETRIC KEY ");
                    ScriptingPreferences sp = new ScriptingPreferences();
                    sb.Append(FormatFullNameForScripting(sp));
                    sb.Append(" ADD ");

                    string keyEncryptionsScript = ScriptSymmetricKeyEncryptions(keyEncryptions);
                    Diagnostics.TraceHelper.Assert(keyEncryptionsScript.Length > 0);

                    sb.Append(keyEncryptionsScript);

                    this.Parent.ExecuteNonQuery(sb.ToString());
                }

            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddKeyEncryption, this, e);
            }
        }


        ///<summary>
        /// Removes a key encryption to the symmetric key.
        ///</summary>
        public void DropKeyEncryption(SymmetricKeyEncryption keyEncryption)
        {
            DropKeyEncryption(new SymmetricKeyEncryption[] { keyEncryption });
        }

        ///<summary>
        /// Removes one or more key encryptions to the symmetric key.
        ///</summary>
        public void DropKeyEncryption(SymmetricKeyEncryption[] keyEncryptions)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);

                if (this.EncryptionAlgorithm == SymmetricKeyEncryptionAlgorithm.CryptographicProviderDefined)
                {
                    throw new Exception(ExceptionTemplates.CannotAlterKeyWithProvider);
                }

                CheckNullArgument(keyEncryptions, "keyEncryptions");

                Diagnostics.TraceHelper.Assert(keyEncryptions.Length > 0);

                if (keyEncryptions.Length > 0)
                {
                    StringBuilder sb = new StringBuilder("ALTER SYMMETRIC KEY ");
                    ScriptingPreferences sp = new ScriptingPreferences();
                    sb.Append(FormatFullNameForScripting(sp));
                    sb.Append(" DROP ");

                    string keyEncryptionsScript = ScriptSymmetricKeyEncryptions(keyEncryptions);
                    Diagnostics.TraceHelper.Assert(keyEncryptionsScript.Length > 0);

                    sb.Append(keyEncryptionsScript);

                    this.Parent.ExecuteNonQuery(sb.ToString());
                }

            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropKeyEncryption, this, e);
            }
        }

        /// <summary>
        /// Opens the symmetric key, decrypted by a certificate.
        /// </summary>
        /// <param name="certificateName"></param>
        public void OpenWithCertificate(string certificateName)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);
                CheckNullArgument(certificateName, "certificate name");

                StringBuilder sb = new StringBuilder("OPEN SYMMETRIC KEY ");
                sb.Append(FormatFullNameForScripting(new ScriptingPreferences()));
                sb.Append(" DECRYPTION BY CERTIFICATE ");
                sb.Append(MakeSqlBraket(certificateName));

                this.Parent.ExecuteNonQuery(sb.ToString());

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SymmetricKeyOpen, this, e);
            }
        }

        /// <summary>
        /// Opens the symmetric key, decrypted by a certificate with a password.
        /// </summary>
        /// <param name="certificateName"></param>
        /// <param name="privateKeyPassword"></param>
        public void OpenWithCertificate(string certificateName, string privateKeyPassword)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);
                CheckNullArgument(certificateName, "certificate name");
                CheckNullArgument(privateKeyPassword, "private key password");

                StringBuilder sb = new StringBuilder("OPEN SYMMETRIC KEY ");
                sb.Append(FormatFullNameForScripting(new ScriptingPreferences()));
                sb.Append(" DECRYPTION BY CERTIFICATE ");
                sb.Append(MakeSqlBraket(certificateName));
                sb.Append(" WITH PASSWORD = ");
                sb.Append(MakeSqlString(privateKeyPassword));

                this.Parent.ExecuteNonQuery(sb.ToString());

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.SymmetricKeyOpen, this, e);
            }
        }

        /// <summary>
        /// Opens the symmetric key, decrypted by a symmetric key.
        /// </summary>
        /// <param name="symmetricKeyName"></param>
        public void OpenWithSymmetricKey(string symmetricKeyName)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);
                CheckNullArgument(symmetricKeyName, "symmetric key name");

                StringBuilder sb = new StringBuilder("OPEN SYMMETRIC KEY ");
                sb.Append(FormatFullNameForScripting(new ScriptingPreferences()));
                sb.Append(" DECRYPTION BY SYMMETRIC KEY ");
                sb.Append(MakeSqlBraket(symmetricKeyName));

                this.Parent.ExecuteNonQuery(sb.ToString());

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.SymmetricKeyOpen, this, e);
            }
        }

        /// <summary>
        /// Opens the symmetric key, using the key that is associated 
        /// with the specified password.
        /// </summary>
        /// <param name="password"></param>
        public void Open(string password)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);
                CheckNullArgument(password, "password");

                StringBuilder sb = new StringBuilder("OPEN SYMMETRIC KEY ");
                sb.Append(FormatFullNameForScripting(new ScriptingPreferences()));
                sb.Append(" DECRYPTION BY PASSWORD = ");
                sb.Append(MakeSqlString(password));

                this.Parent.ExecuteNonQuery(sb.ToString());

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SymmetricKeyOpen, this, e);
            }
        }

        ///<summary>
        /// Closes the symmetric key
        ///</summary>
        public void Close()
        {
            try
            {
                this.ThrowIfNotSupported(typeof(SymmetricKey));
                CheckObjectState(true);

                StringBuilder sb = new StringBuilder("CLOSE SYMMETRIC KEY ");
                sb.Append(FormatFullNameForScripting(new ScriptingPreferences()));

                this.Parent.ExecuteNonQuery(sb.ToString());

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(false);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.SymmetricKeyClose, this, e);
            }
        }


        /****************************
            implementation functions
        ****************************/

        ///<summary>
        /// internal constructor
        ///</summary>
        internal SymmetricKey(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // holds information necessary fot the CREATE script
        private class CreateInfo
        {
            public SymmetricKeyEncryption[] keyEncryptions;
            public SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm;
            public SqlSecureString password;
            public string identityPhrase;

            // used in case of encryption by cryptographic provider
            public string providerAlgorithm;
            public string providerKeyName;
            public CreateDispositionType createDispositionType;

            public CreateInfo(SymmetricKeyEncryption[] keyEncryptions,
                              SymmetricKeyEncryptionAlgorithm keyEncryptionAlgorithm,
                              SqlSecureString password,
                              string identityPhrase)
            {
                this.keyEncryptions = keyEncryptions;
                this.keyEncryptionAlgorithm = keyEncryptionAlgorithm;
                this.password = password;
                this.identityPhrase = identityPhrase;
            }

            public CreateInfo(SymmetricKeyEncryption keyEncryption, string providerAlgorithm, string providerKeyName, CreateDispositionType createDispositionType)
            {
                this.keyEncryptions = new SymmetricKeyEncryption[] { keyEncryption };
                this.keyEncryptionAlgorithm = SymmetricKeyEncryptionAlgorithm.CryptographicProviderDefined;
                this.providerAlgorithm = providerAlgorithm;
                this.providerKeyName = providerKeyName;
                this.createDispositionType = createDispositionType;
            }
        }

        //member variable holding information necessary fot the CREATE script
        private CreateInfo createInfo;

        ///<summary>
        /// returns the name of the type in the urn expression
        ///</summary>
        public static string UrnSuffix
        {
            get
            {
                return "SymmetricKey";
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
        /// ArgumentNullException is the argument is null
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
        /// build the create script
        ///</summary>
        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            // cleanup the side parameter
            // hide it and copy in local variable
            CreateInfo createInfo = this.createInfo;
            this.createInfo = null;

            Diagnostics.TraceHelper.Assert(null != createInfo, "caller should initialize createInfo it before calling");

            StringBuilder sb = new StringBuilder("CREATE SYMMETRIC KEY ");
            sb.Append(FormatFullNameForScripting(sp));
            sb.Append(sp.NewLine);

            if (sp.IncludeScripts.Owner && (null != GetPropValueOptional("Owner")))
            {
                sb.Append("AUTHORIZATION ");
                sb.Append(MakeSqlBraket((string)GetPropValueOptional("Owner")));
                sb.Append(sp.NewLine);
            }


            if (createInfo.keyEncryptionAlgorithm == SymmetricKeyEncryptionAlgorithm.CryptographicProviderDefined)
            {
                ThrowIfBelowVersion100();
                Diagnostics.TraceHelper.Assert(createInfo.keyEncryptions.Length == 1, "There should be only one keyEncryptionType which is the provider");
                SymmetricKeyEncryption keyEncryption = createInfo.keyEncryptions[0];
                CheckNullArgument(keyEncryption, "keyEncryption");
                CheckNullArgument(keyEncryption.ObjectNameOrPassword, "keyEncryption.ObjectNameOrPassword");

                // sanity check
                Diagnostics.TraceHelper.Assert(keyEncryption.KeyEncryptionType == KeyEncryptionType.Provider);

                sb.Append(string.Format(SmoApplication.DefaultCulture, "FROM PROVIDER {0}", MakeSqlBraket(keyEncryption.ObjectNameOrPassword)));
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
            }
            else
            {
                sb.Append("WITH ALGORITHM = ");

                switch (createInfo.keyEncryptionAlgorithm)
                {
                    case SymmetricKeyEncryptionAlgorithm.RC2:
                        sb.Append("RC2");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.RC4:
                        sb.Append("RC4");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.Des:
                        sb.Append("DES");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.TripleDes:
                        sb.Append("TRIPLE_DES");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.DesX:
                        sb.Append("DESX");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.TripleDes3Key:
                        sb.Append("TRIPLE_DES_3KEY");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.Aes128:
                        sb.Append("AES_128");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.Aes192:
                        sb.Append("AES_192");
                        break;
                    case SymmetricKeyEncryptionAlgorithm.Aes256:
                        sb.Append("AES_256");
                        break;
                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("SymmetricKeyEncryptionAlgorithm"));
                }

                if (null != createInfo.password)
                {
                    sb.Append(", KEY_SOURCE = ");
                    sb.Append(MakeSqlString((string)createInfo.password));
                }

                if (null != createInfo.identityPhrase)
                {
                    sb.Append(", IDENTITY_VALUE = ");
                    sb.Append(MakeSqlString(createInfo.identityPhrase));
                }

                sb.Append(sp.NewLine);
                sb.Append(ScriptSymmetricKeyEncryptions(createInfo.keyEncryptions));
            }
            query.Add(sb.ToString());
        }

        ///<summary>
        /// builds the script for a list of SymmetricKeyEncryptions
        ///</summary>
        private string ScriptSymmetricKeyEncryptions(SymmetricKeyEncryption[] keyEncryptions)
        {
            //condition also verified by the caller
            Diagnostics.TraceHelper.Assert(null != keyEncryptions);

            //if we have key encriptions, prepare to script them
            if (keyEncryptions.Length <= 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder("ENCRYPTION BY ");

            bool useDelimiter = false;
            foreach (SymmetricKeyEncryption keyEncryption in keyEncryptions)
            {
                CheckNullArgument(keyEncryption, "keyEncryption");
                CheckNullArgument(keyEncryption.ObjectNameOrPassword, "keyEncryption.ObjectNameOrPassword");

                if (useDelimiter)
                {
                    sb.Append(", ");
                }

                switch (keyEncryption.KeyEncryptionType)
                {
                    case KeyEncryptionType.SymmetricKey:
                        sb.Append("SYMMETRIC KEY ");
                        sb.Append(MakeSqlBraket(keyEncryption.ObjectNameOrPassword));
                        break;
                    case KeyEncryptionType.Certificate:
                        sb.Append("CERTIFICATE ");
                        sb.Append(MakeSqlBraket(keyEncryption.ObjectNameOrPassword));
                        break;
                    case KeyEncryptionType.Password:
                        sb.Append("PASSWORD = ");
                        sb.Append(MakeSqlString(keyEncryption.ObjectNameOrPassword));
                        break;
                    case KeyEncryptionType.AsymmetricKey:
                        sb.Append("ASYMMETRIC KEY ");
                        sb.Append(MakeSqlBraket(keyEncryption.ObjectNameOrPassword));
                        break;
                    default:
                        throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("KeyEncryptionType"));
                }
                useDelimiter = true;
            }
            return sb.ToString();
        }

        ///<summary>
        /// build the drop script
        ///</summary>
        internal override void ScriptDrop(StringCollection query, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder("DROP SYMMETRIC KEY ");
            sb.Append(FormatFullNameForScripting(sp));
            if (removeProviderKey)
            {
                ThrowIfBelowVersion100();
                sb.Append(" REMOVE PROVIDER KEY");
            }
            query.Add(sb.ToString());
        }

        internal override string[] GetNonAlterableProperties()
        {
            return new string[] { "ProviderName" };
        }
    }
}



