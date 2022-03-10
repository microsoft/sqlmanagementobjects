// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.SqlServer.Management.Smo.Internal;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class MasterKey : SqlSmoObject, Cmn.IDroppable
	{
		internal MasterKey(Database parentdb, ObjectKeyBase key, SqlSmoState state)
			:
			base(key, state)
		{
			singletonParent = parentdb as Database;

			SetServerObject(parentdb.Parent);

            m_comparer = parentdb.StringComparer;
        }

        public MasterKey()
            : base()
        {
        }

		public MasterKey(Database parent)
			: base(new ObjectKeyBase(), SqlSmoState.Creating)
		{
            singletonParent = parent;
			SetServerObject(parent.GetServerObject());
			
			m_comparer = parent.StringComparer;
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
            this.ThrowIfNotSupported(typeof(MasterKey));
		}

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal protected override string GetDBName()
		{
			return ((Database)singletonParent).Name;
		}

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "MasterKey";
            }
        }

        /// password used to create the master key
        SqlSecureString encryptionPwd = null;

        /// password used to decrypt the master key
        SqlSecureString decryptionPwd = null;

        /// <summary>
        /// Creates a masterkey. The password specifies the password the master key 
        /// is encrypted with.
        /// </summary>
        /// <param name="encryptionPassword"></param>
        public void Create(string encryptionPassword)
		{
            this.DoesMkExist();
			try
			{
                this.encryptionPwd = encryptionPassword;
                base.CreateImpl();
            }
            finally
            {
                this.encryptionPwd = null;
            }
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            if (null == this.encryptionPwd)
            {
                throw new ArgumentNullException("encryptionPassword");
            }

            if (null == this.path)
            {
                // we are creating the key directly
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                            "CREATE MASTER KEY ENCRYPTION BY PASSWORD = N'{0}'",
                            SqlString(encryptionPwd.ToString())));
            }
            else
            {
                // we are loading the key from a file
                if (null == this.decryptionPwd)
                {
                    throw new ArgumentNullException("decryptionPassword");
                }

                queries.Add(string.Format(SmoApplication.DefaultCulture,
                            "RESTORE MASTER KEY FROM FILE = N'{0}' DECRYPTION BY PASSWORD = N'{1}' ENCRYPTION BY PASSWORD = N'{2}'",
                            SqlString(this.path),
                            SqlString(this.decryptionPwd.ToString()),
                            SqlString(this.encryptionPwd.ToString())));
            }
        }

        string path = null;
        /// <summary>
        /// Creates a master key from a file. The password specifies the password with 
        /// which the file was encrypted, and it will be thw new password for the key
        /// </summary>
        /// <param name="path"></param>
        /// <param name="decryptionPassword"></param>
        /// <param name="encryptionPassword"></param>
        public void Create(string path, string decryptionPassword, string encryptionPassword)
		{
            this.DoesMkExist();
			try
			{
                this.encryptionPwd = encryptionPassword;
                this.decryptionPwd = decryptionPassword;

                if (null == path)
                {
                    throw new FailedOperationException(ExceptionTemplates.Create, this, new ArgumentNullException("path"));
                }

                this.path = path;

                base.CreateImpl();
            }
            finally
            {
                this.encryptionPwd = null;
                this.decryptionPwd = null;
                this.path = null;
            }
        }

        private void DoesMkExist()
        {
            if (this.IsDesignMode && this.Parent.DoesMasterKeyAlreadyExist())
            {
                throw new FailedOperationException(ExceptionTemplates.ObjectAlreadyExists("Database's", "MasterKey")); ;
            }
        }

        protected override void PostCreate()
        {
            if (this.IsDesignMode)
            {
                this.Parent.SetRefMasterKey(this as MasterKey);
            }
        }

		/// <summary>
		/// Drops the master key from the database.
		/// </summary>
		public void Drop()
        {
            if (this.IsDesignMode)
            {
                this.Parent.SetNullRefMasterKey();
            }
			base.DropImpl();
		}

        internal override void ScriptDrop(StringCollection queries, ScriptingPreferences sp)
        {
            queries.Add("DROP MASTER KEY");
        }

        /// <summary>
        /// Loads the service master key from the specified file. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="decryptionPassword"></param>
        /// <param name="encryptionPassword"></param>
        public void Import(string path, string decryptionPassword, string encryptionPassword)
        {
            Import(path, decryptionPassword, encryptionPassword, false);
        }

        /// <summary>
        /// Loads the service master key from the specified file. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="decryptionPassword"></param>
        /// <param name="encryptionPassword"></param>
        /// <param name="forceRegeneration"></param>
        public void Import(string path, string decryptionPassword, string encryptionPassword, bool forceRegeneration)
        {
            try
            {
                CheckObjectState(true);
                if (null == path)
                {
                    throw new ArgumentNullException("path");
                }

                if (null == decryptionPassword)
                {
                    throw new ArgumentNullException("decryptionPassword");
                }

                if (null == encryptionPassword)
                {
                    throw new ArgumentNullException("encryptionPassword");
                }

                StringCollection queries = new StringCollection();
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                            Scripts.USEDB, SqlBraket(Parent.Name)));
                queries.Add(string.Format(SmoApplication.DefaultCulture,
                            "RESTORE MASTER KEY FROM FILE = N'{0}' DECRYPTION BY PASSWORD = N'{1}' ENCRYPTION BY PASSWORD = N'{2}' {3}",
                            SqlString(path),
                            SqlString(decryptionPassword),
                            SqlString(encryptionPassword),
                            forceRegeneration ? "FORCE" : ""));

                this.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ImportMasterKey, this, e);
            }
        }

        /// <summary>
        /// Saves the service master key to a file, encrypted with the supplied password. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="password"></param>
        public void Export(string path, string password)
        {
            try
            {
                CheckObjectState(true);
                if (null == path)
                {
                    throw new ArgumentNullException("path");
                }

                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add(string.Format(
							SmoApplication.DefaultCulture,
							"BACKUP MASTER KEY TO FILE = N'{0}' ENCRYPTION BY PASSWORD = N'{1}'",
							SqlString(path), SqlString(password)));
				this.ExecutionManager.ExecuteNonQuery(queries);
			}
			catch (Exception e)
			{
				FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ExportMasterKey, this, e);
            }
        }

        /// <summary>
        /// Add encryption by password.
        /// </summary>
        /// <param name="password"></param>
        public void AddPasswordEncryption(string password)
        {
            try
            {
                CheckObjectState(true);
                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							"ALTER MASTER KEY ADD ENCRYPTION BY PASSWORD = N'{0}'",
							SqlString(password)));
				this.ExecutionManager.ExecuteNonQuery(queries);
			}
			catch (Exception e)
			{
				FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddEncryptionMasterKey, this, e);
            }
        }

		/// <summary>
		/// Add encryption by service master key.
		/// </summary>
		public void AddServiceKeyEncryption()
		{
			try
			{
				CheckObjectState(true);
				StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add("ALTER MASTER KEY ADD ENCRYPTION BY SERVICE MASTER KEY");
				this.ExecutionManager.ExecuteNonQuery(queries);

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsEncryptedByServer").SetValue(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AddEncryptionMasterKey, this, e);
            }
        }

		/// <summary>
		/// Closes any opened master keys in the database.
		/// </summary>
		public void Close()
		{
			try
			{
				CheckObjectState(true);
				StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add("CLOSE MASTER KEY");
				this.ExecutionManager.ExecuteNonQuery(queries);

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(false);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Close, this, e);
            }
        }

        /// <summary>
        /// Opens the database master key, using the key that is associated 
        /// with the specified password.
        /// </summary>
        /// <param name="password"></param>
        public void Open(string password)
        {
            try
            {
                CheckObjectState(true);
                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							"OPEN MASTER KEY DECRYPTION BY PASSWORD = N'{0}'",
							SqlString(password)));
				this.ExecutionManager.ExecuteNonQuery(queries);

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsOpen").SetValue(true);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Open, this, e);
            }
        }

        /// <summary>
        /// Removes the encryption with the key associated with the specified password.
        /// </summary>
        /// <param name="password"></param>
        public void DropPasswordEncryption(string password)
        {
            try
            {
                CheckObjectState(true);
                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							"ALTER MASTER KEY DROP ENCRYPTION BY PASSWORD = N'{0}'",
							SqlString(password)));
				this.ExecutionManager.ExecuteNonQuery(queries);
			}
			catch (Exception e)
			{
				FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropEncryptionMasterKey, this, e);
            }
        }

		/// <summary>
		/// Removes the encryption by the service key.
		/// </summary>
		public void DropServiceKeyEncryption()
		{
			try
			{
				CheckObjectState(true);
				StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add("ALTER MASTER KEY DROP ENCRYPTION BY SERVICE MASTER KEY");
				this.ExecutionManager.ExecuteNonQuery(queries);

                // if we are in execution mode update the property bag
                if (!this.ExecutionManager.Recording)
                {
                    this.Properties.Get("IsEncryptedByServer").SetValue(false);
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropEncryptionMasterKey, this, e);
            }
        }

        /// <summary>
        /// Regenerates the database master key using the specified password.
        /// </summary>
        /// <param name="password"></param>
        public void Regenerate(string password)
        {
            Regenerate(password, false);
        }

        /// <summary>
        /// Regenerates the database master key using the specified password. 
        /// If the force parameter is set to true, the database master key 
        /// will be regenerated forcefully. This will cause all secrets 
        /// that cannot be decrypted by the old database master key to be dropped.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="forceRegeneration"></param>
        public void Regenerate(string password, bool forceRegeneration)
        {
            try
            {
                CheckObjectState(true);

                if (null == password)
                {
                    throw new ArgumentNullException("password");
                }

                StringCollection queries = new StringCollection();
				queries.Add(string.Format(SmoApplication.DefaultCulture,
							Scripts.USEDB, SqlBraket(Parent.Name)));
				queries.Add(string.Format(
							SmoApplication.DefaultCulture,
							"ALTER MASTER KEY {0}REGENERATE WITH ENCRYPTION BY PASSWORD = N'{1}'",
							forceRegeneration ? "FORCE " : "", SqlString(password)));
				this.ExecutionManager.ExecuteNonQuery(queries);
			}
			catch (Exception e)
			{
				FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RegenerateMasterKey, this, e);
            }
        }

        /// <summary>
        /// Returns the current set of key encryptions of the database master key.
        /// Note: uses enumerator Urn Server/Database/MasterKey/Encryption
        /// </summary>
        /// <returns>
        /// Name	Type	Description	Source
        /// Urn	System.String	The Urn for this record.	
        /// Thumbprint	System.Byte[]	SHA-1 hash of the certificate with which the key is encrypted OR The guid of the symmetric key with which the key is encrypted.	
        /// SymmetricKeyEncryptionType	SymmetricKeyEncryptionType	Indicates the type of encryption.	
        /// CryptProperty	System.Byte[]	Signed or encrypted bits.	
        /// </returns>
        public DataTable EnumKeyEncryptions()
        {
            try
            {
                CheckObjectState(true);
                return this.ExecutionManager.GetEnumeratorData(new Request(this.Urn + "/Encryption"));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumKeyEncryptions, this, e);
            }
        }
    }
}



