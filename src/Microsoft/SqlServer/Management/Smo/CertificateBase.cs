// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Diagnostics = Microsoft.SqlServer.Management.Diagnostics;
namespace Microsoft.SqlServer.Management.Smo
{
    ///Specifies the source of the certificate, when loading.

    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum CertificateSourceType
    {
        ///Load the certificate from file.
        File = 1,
        ///Load the certificate from executable.
        Executable = 2,
        ///Load the certificate from the specified assembly.
        SqlAssembly = 3
    }

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Certificate : NamedSmoObject, ICreatable, IDroppable, IAlterable
	{
		/****************************
			public functions
		****************************/

        ///<summary>
        /// Creates the object on the server.
        ///</summary>
        public void Create()
        {
            try
            {
                this.ThrowIfNotSupported(typeof(Certificate));

                CreateInternal(null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        ///<summary>
        /// Creates the object on the server. The encryption password is the password 
        /// with which the private key is stored.
        ///</summary>
        public void Create(string encryptionPassword)
        {
            try
            {
                this.ThrowIfNotSupported(typeof(Certificate));
                CheckNullArgument(encryptionPassword, "encryptionPassword");

                CreateInternal(encryptionPassword);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        ///<summary>
        /// Persist all changes made in this object.
        ///</summary>
        public void Alter()
        {
            this.AlterImpl();
        }

        ///<summary>
        /// Adds a private key to the certificate. The privateKeyPath parameter specifies 
        /// the full path to the certificate. The decryption password is the password that 
        /// is needed to access the private key. 
        /// The private key will be encrypted by the database master key.
        ///</summary>
        public void AddPrivateKey(string privateKeyPath, string decryptionPassword)
        {
            try
            {
                CheckObjectState();
                CheckNullArgument(privateKeyPath, "privateKeyPath");
                CheckNullArgument(decryptionPassword, "decryptionPassword");

                AddPrivateKeyInternal(privateKeyPath, decryptionPassword, null);
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("PrivateKeyEncryptionType").SetValue(PrivateKeyEncryptionType.MasterKey);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddPrivateKey, this, e);
            }
        }

        ///<summary>
        /// Adds a private key to the certificate. The privateKeyPath parameter specifies 
        /// the full path to the certificate. The decryption password is the password that
        /// is needed to access the private key. The encryption password is the password 
        /// with which the private key is stored
        ///</summary>
        public void AddPrivateKey(string privateKeyPath, string decryptionPassword, string encryptionPassword)
        {
            try
            {
                CheckObjectState();
                CheckNullArgument(privateKeyPath, "privateKeyPath");
                CheckNullArgument(decryptionPassword, "decryptionPassword");
                CheckNullArgument(encryptionPassword, "encryptionPassword");

                AddPrivateKeyInternal(privateKeyPath, decryptionPassword, encryptionPassword);
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("PrivateKeyEncryptionType").SetValue(PrivateKeyEncryptionType.Password);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddPrivateKey, this, e);
            }
        }

        ///<summary>
        /// Drops the object and removes it from the collection
        ///</summary>
        public void Drop()
        {
            this.DropImpl();
        }

        ///<summary>
        /// Loads the certificate from the source specified.
        /// 
        /// It has the same semantics as a Create() operation: adds it in the collection, change state
        ///
        ///</summary>
        public void Create(string certificateSource, CertificateSourceType sourceType)
        {
            try
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(Certificate));
                CheckNullArgument(certificateSource, "certificateSource");

                ImportInternal(certificateSource, sourceType, null, null, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        ///<summary>
        /// Loads the certificate from the specified source, and private key from the specified 
        /// file. The decryption password is the password that is needed to access the private key. 
        /// 
        /// It has the same semantics as a Create() operation: adds it in the collection, change state
        ///
        ///</summary>
        public void Create(string certificateSource, CertificateSourceType sourceType, string privateKeyPath
                            , string privateKeyDecryptionPassword)
        {
            try
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(Certificate));
                CheckNullArgument(certificateSource, "certificateSource");
                CheckNullArgument(privateKeyPath, "privateKeyPath");
                CheckNullArgument(privateKeyDecryptionPassword, "privateKeyDecryptionPassword");

                ImportInternal(certificateSource, sourceType, privateKeyPath, privateKeyDecryptionPassword, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        ///<summary>
        /// Loads the certificate from the specified source, and private key from the specified 
        /// file. The decryption password is the password that is needed to access the private key. 
        /// The encryption password specifies the password with which the private key will be encrypted.
        /// 
        /// It has the same semantics as a Create() operation: adds it in the collection, change state
        ///
        ///</summary>
        public void Create(string certificateSource, CertificateSourceType sourceType, string privateKeyPath
                        , string privateKeyDecryptionPassword, string privateKeyEncryptionPassword)
        {
            try
            {
                CheckObjectState();
                this.ThrowIfNotSupported(typeof(Certificate));
                CheckNullArgument(certificateSource, "certificateSource");
                CheckNullArgument(privateKeyPath, "privateKeyPath");
                CheckNullArgument(privateKeyDecryptionPassword, "privateKeyDecryptionPassword");
                CheckNullArgument(privateKeyEncryptionPassword, "privateKeyEncryptionPassword");

                ImportInternal(certificateSource, sourceType, privateKeyPath
                            , privateKeyDecryptionPassword, privateKeyEncryptionPassword);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Create, this, e);
            }
        }

        ///<summary>
        /// Saves the certificate the specified certificatePath
        ///</summary>
        public void Export(string certificatePath)
        {
            try
            {
                CheckObjectState();
                CheckNullArgument(certificatePath, "certificatePath");

                ExportInternal(certificatePath, null, null, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExportCertificate, this, e);
            }
        }

        /// <summary>
        /// Saves the certificate in the specified certificatePath.
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <param name="privateKeyPath">specifies the path of the private key</param>
        /// <param name="encryptionPassword">specifies the encryption for the private key</param>
        public void Export(string certificatePath, string privateKeyPath, string encryptionPassword)
        {
            try
            {
                CheckObjectState();
                CheckNullArgument(certificatePath, "certificatePath");
                CheckNullArgument(privateKeyPath, "privateKeyPath");
                CheckNullArgument(encryptionPassword, "encryptionPassword");

                ExportInternal(certificatePath, privateKeyPath, encryptionPassword, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExportCertificate, this, e);
            }
        }

        /// <summary>
        /// Saves the certificate in the specified certificatePath.
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <param name="privateKeyPath">Specifies the path of the private key</param>
        /// <param name="encryptionPassword">Specifies the encryption for the private key</param>
        /// <param name="decryptionPassword">The password used to decrypt the certificate</param>
        public void Export(string certificatePath, string privateKeyPath,
                                string encryptionPassword, string decryptionPassword)
        {
            try
            {
                CheckObjectState();
                CheckNullArgument(certificatePath, "certificatePath");
                CheckNullArgument(privateKeyPath, "privateKeyPath");
                CheckNullArgument(encryptionPassword, "encryptionPassword");
                CheckNullArgument(decryptionPassword, "decryptionPassword");

                ExportInternal(certificatePath, privateKeyPath, encryptionPassword, decryptionPassword);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExportCertificate, this, e);
            }
        }

        /// <summary>
        /// Changes the password that is used to secure the private key.
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        public void ChangePrivateKeyPassword(string oldPassword, string newPassword)
        {
            try
            {
                CheckObjectState();
                CheckNullArgument(oldPassword, "oldPassword");
                CheckNullArgument(newPassword, "newPassword");

                StringBuilder sb = GetCertificateBuilder("ALTER");

                sb.Append(" WITH PRIVATE KEY (");
                if (AddToStringBuilderIfNotNull(sb, "DECRYPTION BY PASSWORD=", oldPassword, false))
                {
                    AddToStringBuilderIfNotNull(sb, ", ENCRYPTION BY PASSWORD=", newPassword, false);
                }
                sb.Append(")");

                this.Parent.ExecuteNonQuery(sb.ToString());
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ChangePrivateKeyPassword, this, e);
            }
        }

        /// <summary>
        /// Removes the private key from the certificate
        /// </summary>
        public void RemovePrivateKey()
        {
            try
            {
                CheckObjectState();

                StringBuilder sb = GetCertificateBuilder("ALTER");
                sb.Append(" REMOVE PRIVATE KEY");

                this.Parent.ExecuteNonQuery(sb.ToString());
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("PrivateKeyEncryptionType").SetValue(PrivateKeyEncryptionType.NoKey);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RemovePrivateKey, this, e);
            }
        }

        /****************************
            implementation functions
        ****************************/

        internal Certificate(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
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
                return "Certificate";
            }
        }

        ///<summary>
        /// returns a initialized StringBuilder 
        ///</summary>
        private StringBuilder GetCertificateBuilder(string operationName)
        {
            return GetCertificateBuilder(operationName, new ScriptingPreferences());
        }

        ///<summary>
        /// returns a initialized StringBuilder based on the requested operation ( CREATE, ALTER etc. )
        ///</summary>
        private StringBuilder GetCertificateBuilder(string operationName, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder();

            ScriptIncludeHeaders(sb, sp, UrnSuffix);

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(Scripts.INCLUDE_EXISTS_CERTIFICATE,
                    (0 == string.Compare("CREATE", operationName, StringComparison.OrdinalIgnoreCase)) ? "NOT" : "",
                    this.ID);
                sb.Append(sp.NewLine);
            }

            sb.Append(operationName);
            sb.Append(" CERTIFICATE ");
            if (this.Name != null)
            {
                sb.Append(MakeSqlBraket(this.Name));
            }
            sb.Append(sp.NewLine);
            return sb;
        }

        ///<summary>
        ///	ArgumentNullException is the argument is null
        /// should be inlined
        ///</summary>
        private void CheckNullArgument(string arg, string argName)
        {
            if (null == arg)
            {
                throw new ArgumentNullException(argName);
            }
        }

        ///<summary>
        /// generate script in sb if data is not null
        /// use the specified prefix
        ///</summary>
        private bool AddToStringBuilderIfNotNull(StringBuilder sb, string prefix, object data, bool braket)
        {
            if (null == data)
            {
                return false;
            }

            sb.Append(prefix);

            //we will deal almost exclusively with strings
            if (!(data is string))
            {
                if (data is DateTime) //cannot use 'as' it is a value type
                {
                    DateTime dt = (DateTime)data;
                    data = dt.ToString("MM/dd/yyyy", DateTimeFormatInfo.InvariantInfo);
                }
                else if (data is bool)
                {
                    sb.Append((bool)data ? "ON" : "OFF");
                    return true;
                }
            }

            if (braket)
            {
                sb.Append(MakeSqlBraket(data.ToString()));
            }
            else
            {
                sb.Append(MakeSqlString(data.ToString()));
            }
            return true;
        }

        ///<summary>		
        /// Creates the object on the server. The encryption password is the password 
        /// with which the private key is stored.
        ///</summary>		
        private void CreateInternal(string encryptionPassword)
        {
            StringCollection createQuery;
            ScriptingPreferences sp;

            //
            // create initialize
            //

            CreateImplInit(out createQuery, out sp);

            //
            //build script
            //

            StringBuilder sb = GetCertificateBuilder("CREATE", sp);
            if (sp.IncludeScripts.Owner)
            {
                if (AddToStringBuilderIfNotNull(sb, " AUTHORIZATION ", GetPropValueOptional("Owner"), true))
                {
                    sb.Append(sp.NewLine);
                } 
            }

            if (AddToStringBuilderIfNotNull(sb, " ENCRYPTION BY PASSWORD = ", encryptionPassword, false))
            {
                sb.Append(sp.NewLine);
            }

            AddToStringBuilderIfNotNull(sb, "WITH SUBJECT = ", GetPropValue("Subject"), false);
            sb.Append(sp.NewLine);
            bool startDateSpecified = AddToStringBuilderIfNotNull(sb, ", START_DATE = ", GetPropValueOptional("StartDate"), false);
            bool expirationDateSpecified = AddToStringBuilderIfNotNull(sb, ", EXPIRY_DATE = ", GetPropValueOptional("ExpirationDate"), false);
            if (startDateSpecified || expirationDateSpecified)
            {
                sb.Append(sp.NewLine);
            }
            AddToStringBuilderIfNotNull(sb, "ACTIVE FOR BEGIN_DIALOG = ", GetPropValueOptional("ActiveForServiceBrokerDialog"), false);
            createQuery.Add(sb.ToString());

            //
            // create execute, cleanup and state update.
            //

            CreateImplFinish(createQuery, sp);
        }

        ///<summary>
        /// generates the scripts to persist changes made in this object.
        ///</summary>
        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            Property prop = this.Properties.Get("ActiveForServiceBrokerDialog");
            if (prop.Dirty)
            {
                StringBuilder sb = GetCertificateBuilder("ALTER", sp);

                //cannot be null if dirty
                Diagnostics.TraceHelper.Assert(null != prop.Value);

                AddToStringBuilderIfNotNull(sb, " WITH ACTIVE FOR BEGIN_DIALOG = ", prop.Value, false);

                query.Add(sb.ToString());
            }

            //script change owner if dirty
            ScriptChangeOwner(query, sp);
        }

        ///<summary>
        /// Adds a private key to the certificate. The privateKeyPath parameter specifies 
        /// the full path to the certificate. The decryption password is the password that
        /// is needed to access the private key. The encryption password is the password 
        /// with which the private key is stored
        ///</summary>
        private void AddPrivateKeyInternal(string privateKeyPath, string decryptionPassword, string encryptionPassword)
        {
            StringBuilder sb = GetCertificateBuilder("ALTER");

            if (AddToStringBuilderIfNotNull(sb, " WITH PRIVATE KEY (FILE = ", privateKeyPath, false))
            {
                AddToStringBuilderIfNotNull(sb, " , DECRYPTION BY PASSWORD = ", decryptionPassword, false);
                AddToStringBuilderIfNotNull(sb, ",  ENCRYPTION BY PASSWORD = ", encryptionPassword, false);
                sb.Append(")");
            }

            this.Parent.ExecuteNonQuery(sb.ToString());
        }
        ///<summary>
        /// build script for dropping a certificate
        ///</summary>
        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            dropQuery.Add(GetCertificateBuilder("DROP", sp).ToString());
        }

        ///<summary>
        /// Loads the certificate from the specified source, and private key from the specified 
        /// file. The decryption password is the password that is needed to access the private key. 
        /// The encryption password specifies the password with which the private key will be encrypted.
        /// 
        /// It has the same semantics as a Create() operation: adds it in the collection, change state
        ///
        ///</summary>
        private void ImportInternal(string certificateSource, CertificateSourceType sourceType, string privateKeyPath
                        , string decryptionPassword, string encryptionPassword)
        {
            StringCollection createQuery;
            ScriptingPreferences sp;

            //
            // create initialize
            //

            CreateImplInit(out createQuery, out sp);

            //
            //build script
            //

            StringBuilder sb = GetCertificateBuilder("CREATE", sp);

            //we add owner if set, the other properties 
            //that the user might have set are ignored though
            if (sp.IncludeScripts.Owner)
            {
                AddToStringBuilderIfNotNull(sb, " AUTHORIZATION ", GetPropValueOptional("Owner"), true);
                sb.Append(sp.NewLine); 
            }

            // add the source
            Diagnostics.TraceHelper.Assert(null != certificateSource, "certificateSource cannot be null");
            sb.Append(" FROM ");
            switch (sourceType)
            {
                case CertificateSourceType.File:
                    AddToStringBuilderIfNotNull(sb, "FILE = ", certificateSource, false);
                    break;

                case CertificateSourceType.Executable:
                    AddToStringBuilderIfNotNull(sb, "EXECUTABLE FILE = ", certificateSource, false);
                    break;

                case CertificateSourceType.SqlAssembly:
                    AddToStringBuilderIfNotNull(sb, "ASSEMBLY ", certificateSource, false);
                    break;

                default:
                    throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("CertificateSourceType"));
            }

            // add the private key part
            sb.Append(sp.NewLine);
            if (AddToStringBuilderIfNotNull(sb, " WITH PRIVATE KEY ( FILE=", privateKeyPath, false))
            {
                if (AddToStringBuilderIfNotNull(sb, ", DECRYPTION BY PASSWORD=", decryptionPassword, false))
                {
                    AddToStringBuilderIfNotNull(sb, ", ENCRYPTION BY PASSWORD=", encryptionPassword, false);
                }
                sb.Append(")");
            }

            createQuery.Add(sb.ToString());

            //
            // create execute, cleanup and state update.
            //

            CreateImplFinish(createQuery, sp);

        }

        /// <summary>
        /// Saves the certificate the specified certificatePath. The privateKeyPath specifies 
        /// the path of the private key. The password specifies the encryption for the private key.
        /// </summary>
        /// <param name="certificatePath"></param>
        /// <param name="privateKeyPath"></param>
        /// <param name="encryptionPassword"></param>
        /// <param name="decryptionPassword"></param>
        private void ExportInternal(string certificatePath, string privateKeyPath,
                                    string encryptionPassword, string decryptionPassword)
        {
            StringBuilder sb = GetCertificateBuilder("BACKUP");

            AddToStringBuilderIfNotNull(sb, " TO FILE=", certificatePath, false);

            if (AddToStringBuilderIfNotNull(sb, " WITH PRIVATE KEY( FILE =", privateKeyPath, false))
            {
                AddToStringBuilderIfNotNull(sb, ", ENCRYPTION BY PASSWORD=", encryptionPassword, false);
                AddToStringBuilderIfNotNull(sb, ", DECRYPTION BY PASSWORD=", decryptionPassword, false);
                sb.Append(")");
            }

            this.Parent.ExecuteNonQuery(sb.ToString());
        }
    }
}


