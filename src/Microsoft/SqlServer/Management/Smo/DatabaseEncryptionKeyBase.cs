// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class DatabaseEncryptionKey : SqlSmoObject, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
	//Designmode support for this object was added in changelist 1489385 with Masterkey and Asymmetric Key objects
	//I have removed all the changes related to DatabaseEncryptionKey object done in that changelist as part of fix for bug. 280404. 
	//One should look at the changes done in changelist 1489385 before adding designmode support for this object.
    {
        internal DatabaseEncryptionKey(Database parentdb, ObjectKeyBase key, SqlSmoState state)
            : base(key, state)
        {
            singletonParent = parentdb;
            SetServerObject(parentdb.Parent);
            m_comparer = parentdb.StringComparer;
        }

        public DatabaseEncryptionKey()
            : base()
        {
        }

		[SfcObject(SfcObjectRelationship.ParentObject)]
		public Database Parent
		{
			get
			{
				CheckObjectState();
                return singletonParent as Database;
			}
			set 
			{ 
				SetParentImpl(value);
				SetState(SqlSmoState.Creating);
			}
		}

        internal override void ValidateParent(SqlSmoObject newParent)
        {
            singletonParent = (Database)newParent;
            m_comparer = newParent.StringComparer;
            SetServerObject(newParent.GetServerObject());
            this.ThrowIfNotSupported(this.GetType());
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        //returns the name of the parent database
        internal protected override string GetDBName()
        {
            return ((Database)singletonParent).Name;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "DatabaseEncryptionKey";
            }
        }

        /// <summary>
        /// Creates a new DEK object
        /// </summary>
        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection createQuery, ScriptingPreferences sp)
        {
            if (!SmoUtility.IsSupportedObject(this.GetType(), this.ServerVersion, this.DatabaseEngineType, this.DatabaseEngineEdition))
            {
                throw new UnsupportedFeatureException(ExceptionTemplates.UnsupportedFeature(ExceptionTemplates.DatabaseEncryptionKey));
            }
            
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100 && ServerVersion.Major >= 10)
            {
                if (sp.IncludeScripts.Header)
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, String.Empty, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE_ENCRYPTION_KEY, "NOT");
                    sb.Append(sp.NewLine);
                    sb.Append(Scripts.BEGIN);
                    sb.Append(sp.NewLine);
                }

                //We need this if we try to create the DEK object from the Create method of the Database as it needs the database's context
                if (sp.IncludeScripts.DatabaseContext)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName()));
                    sb.Append(sp.NewLine);
                }

                // CREATE DATABASE ENCRYPTION KEY
                sb.Append("CREATE DATABASE ENCRYPTION KEY");
                sb.Append(sp.NewLine);

                // WITH ALGORITHM = <dek_algorithm>
                DatabaseEncryptionAlgorithm encryptionAlgorithm = (DatabaseEncryptionAlgorithm)this.GetPropValue("EncryptionAlgorithm");
                String strAlgo = GetEncryptionAlgorithm(encryptionAlgorithm);
                sb.AppendFormat("WITH ALGORITHM = {0}", strAlgo);
                sb.Append(sp.NewLine);

                // ENCRYPTION BY <encryption_type> <encryptor_name>

                DatabaseEncryptionType encryptionType = (DatabaseEncryptionType)this.GetPropValue("EncryptionType");
                String strEncryptType = GetEncryptionType(encryptionType);
                sb.AppendFormat("ENCRYPTION BY {0}", strEncryptType);

                sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(this.EncryptorName));
                sb.Append(sp.NewLine);

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.Append(Scripts.END);
                    sb.Append(sp.NewLine);
                }

                createQuery.Add(sb.ToString());
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        /// <summary>
        ///  Alters an already existing DEK object
        /// </summary>
        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            bool scriptAlter = false;
            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100 && ServerVersion.Major >= 10)
            {
                //We need this if we try to alter the DEK object from the Alter method of the Database as it needs the database's context
                if (sp.IncludeScripts.DatabaseContext)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName()));
                    sb.Append(sp.NewLine);
                }

                // ALTER DATABASE ENCRYPTION KEY
                sb.Append("ALTER DATABASE ENCRYPTION KEY");
                sb.Append(sp.NewLine);

                // REGENERATE WITH ALGORITHM = <dek_algorithm>
                Property property = this.Properties.Get("EncryptionAlgorithm");
                if ((null != property.Value) && (property.Dirty))
                {
                    String strAlgo = GetEncryptionAlgorithm((DatabaseEncryptionAlgorithm)property.Value);
                    sb.AppendFormat("REGENERATE WITH ALGORITHM = {0}", strAlgo);
                    sb.Append(sp.NewLine);
                    scriptAlter = true;
                }

                // ENCRYPTION BY <encryption_type> <encryptor_name>
                Property propertyEncryptName = this.Properties.Get("EncryptorName");
                Property propertyEncryptType = this.Properties.Get("EncryptionType");

                if (!String.IsNullOrEmpty((string)propertyEncryptName.Value) && (propertyEncryptName.Dirty) ||
                    ((null != propertyEncryptType.Value) && (propertyEncryptType.Dirty)))
                {
                    String strEncryptType = GetEncryptionType((DatabaseEncryptionType)propertyEncryptType.Value);
                    sb.AppendFormat("ENCRYPTION BY {0}", strEncryptType);

                    String encryptorName = propertyEncryptName.Value.ToString();
                    sb.AppendFormat(SmoApplication.DefaultCulture, "{0}", MakeSqlBraket(encryptorName));
                    sb.Append(sp.NewLine);
                    scriptAlter = true;
                }

                if (scriptAlter)
                {
                    alterQuery.Add(sb.ToString());
                }
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        /// <summary>
        /// Drops an already existing DEK object
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
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            if (sp.TargetServerVersionInternal >= SqlServerVersionInternal.Version100 && ServerVersion.Major >= 10)
            {
                if (sp.IncludeScripts.Header)
                {
                    sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, String.Empty, DateTime.Now.ToString(GetDbCulture())));
                    sb.Append(sp.NewLine);
                }

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_DATABASE_ENCRYPTION_KEY, String.Empty);
                    sb.Append(sp.NewLine);
                    sb.Append(Scripts.BEGIN);
                    sb.Append(sp.NewLine);
                }

                // DROP DATABASE ENCRYPTION KEY
                sb.Append("DROP DATABASE ENCRYPTION KEY");
                sb.Append(sp.NewLine);

                if (sp.IncludeScripts.ExistenceCheck)
                {
                    sb.Append(Scripts.END);
                    sb.Append(sp.NewLine);
                }

                dropQuery.Add(sb.ToString());
            }
            else
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedVersion(ServerVersion.ToString()));
            }
        }

        /// <summary>
        /// Scripts object with default scripting options
        /// </summary>
        public StringCollection Script()
        {
            return base.ScriptImpl();
        }

        /// <summary>
        /// Scripts object with specific scripting options
        /// </summary>
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return base.ScriptImpl(scriptingOptions);
        }

        /// <summary>
        /// Regenerates the DEK with the specified Encryption Algorithm
        /// </summary>
        /// <param name="encryptAlgo"></param>
        public void Regenerate(DatabaseEncryptionAlgorithm encryptAlgo)
        {
            StringCollection queries = new StringCollection();
            String strAlgo = GetEncryptionAlgorithm(encryptAlgo);

            queries.Add(String.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            queries.Add(String.Format("ALTER DATABASE ENCRYPTION KEY REGENERATE WITH ALGORITHM = {0}", strAlgo));
            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        /// <summary>
        /// Reencrypts teh DEK with the specified Encryptor
        /// </summary>
        /// <param name="encryptorName"></param>
        /// <param name="encryptionType"></param>
        public void Reencrypt(string encryptorName, DatabaseEncryptionType encryptionType)
        {
            StringCollection queries = new StringCollection();
            String strEncryptionType = GetEncryptionType(encryptionType);

            queries.Add(String.Format(SmoApplication.DefaultCulture, Scripts.USEDB, SqlBraket(GetDBName())));
            queries.Add(String.Format(SmoApplication.DefaultCulture, "ALTER DATABASE ENCRYPTION KEY ENCRYPTION BY {0} {1}", strEncryptionType, MakeSqlBraket(encryptorName)));
            this.ExecutionManager.ExecuteNonQuery(queries);
        }

        /// <summary>
        /// Returns the string corresponding to the specified Encryption Algorithm
        /// </summary>
        /// <param name="encryptAlgo"></param>
        /// <returns></returns>
        private String GetEncryptionAlgorithm(DatabaseEncryptionAlgorithm encryptAlgo)
        {
            String strEncryptAlgo = String.Empty;
            switch (encryptAlgo)
            {
                case DatabaseEncryptionAlgorithm.Aes128: strEncryptAlgo = "AES_128";
                    break;
                case DatabaseEncryptionAlgorithm.Aes192: strEncryptAlgo = "AES_192";
                    break;
                case DatabaseEncryptionAlgorithm.Aes256: strEncryptAlgo = "AES_256";
                    break;
                case DatabaseEncryptionAlgorithm.TripleDes: strEncryptAlgo = "TRIPLE_DES_3KEY";
                    break;
                default:
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("EncryptionAlgorithm"));
            }
            return strEncryptAlgo;
        }

        /// <summary>
        /// Returns the string corresponding to the specified Encryptor type
        /// </summary>
        /// <param name="encryptionType"></param>
        /// <returns></returns>
        private String GetEncryptionType(DatabaseEncryptionType encryptionType)
        {
            String strEncryptionType = String.Empty;
            switch (encryptionType)
            {
                case DatabaseEncryptionType.ServerCertificate: strEncryptionType = "SERVER CERTIFICATE ";
                    break;
                case DatabaseEncryptionType.ServerAsymmetricKey: strEncryptionType = "SERVER ASYMMETRIC KEY ";
                    break;
                default:
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("EncryptionType"));
            }
            return strEncryptionType;
        }
    }
}

