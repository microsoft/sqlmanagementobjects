// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliant(false)]
    public class DatabaseEncryptionKeyExtender : SmoObjectExtender<DatabaseEncryptionKey>, ISfcValidate
    {
        StringCollection certificateNames;
        StringCollection asymmetricKeyNames;
        bool reencrypt = false;
        bool regenerate = false;
        private string certificateName = string.Empty;
        private string asymmetricKeyName = string.Empty;
        private Hashtable certificateNameBackupDateHash = new Hashtable();
        
        
        public DatabaseEncryptionKeyExtender() : base() { }

        public DatabaseEncryptionKeyExtender(DatabaseEncryptionKey dek) : base(dek) { }
        
        [ExtendedPropertyAttribute()]
        public StringCollection CertificateNames
        {
            get
            {
                if (this.certificateNames == null)
                {
                    this.certificateNames = new StringCollection();
                    Server svr = this.Parent.Parent.GetServerObject();
                    if (svr != null)
                    {
                        Urn urn = "Server/Database[@Name='master']/Certificate[@ID > 256]";
                        string[] fields = new string[] { "Name", "LastBackupDate" };
                        Request req = new Request(urn, fields);
                        DataTable dt = new Enumerator().Process(svr.ConnectionContext, req);
                        foreach (DataRow dr in dt.Rows)
                        {
                            string name = dr["Name"].ToString();
                            if (!name.StartsWith("##MS",StringComparison.Ordinal))
                            {
                                certificateNames.Add(dr["Name"].ToString());
                                certificateNameBackupDateHash.Add(dr["Name"].ToString(), dr["LastBackupDate"].ToString());
                            }
                        }
                    }
                }
                return this.certificateNames;
            }
        }

        [ExtendedPropertyAttribute()]
        public StringCollection AsymmetricKeyNames
        {
            get
            {
                if (this.asymmetricKeyNames == null)
                {
                    this.asymmetricKeyNames = new StringCollection();
                    Server svr = this.Parent.Parent.GetServerObject();
                    if (svr != null)
                    {
                        Urn urn = "Server/Database[@Name='master']/AsymmetricKey";
                        string[] fields = new string[] { "Name" };
                        Request req = new Request(urn, fields);
                        DataTable dt = new Enumerator().Process(svr.ConnectionContext, req);
                        foreach (DataRow dr in dt.Rows)
                        {
                            asymmetricKeyNames.Add(dr["Name"].ToString());
                        }
                    }
                }
                return this.asymmetricKeyNames;
            }
        }

        [ExtendedPropertyAttribute()]
        public bool DatabaseEncryptionEnabled
        {
            get
            {
                Database db = this.Parent.Parent;
                if (db != null)
                {
                    return db.EncryptionEnabled;
                }
                return false;
            }
            set
            {
                Database db = this.Parent.Parent;
                if (db != null)
                {
                    db.EncryptionEnabled = value;
                }
            }
        }

        [ExtendedPropertyAttribute()]
        public SqlSmoState State
        {
            get
            {
                return this.Parent.State;
            }
        }

        [ExtendedPropertyAttribute()]
        public DatabaseEncryptionState EncryptionState
        {
            get
            {
                Property state = this.Parent.Properties.Get("EncryptionState");
                if (state.Value == null)
                {
                    return DatabaseEncryptionState.None;
                }

                return (DatabaseEncryptionState)state.Value;
            }
        }

        [ExtendedPropertyAttribute()]
        public bool Regenerate
        {
            get { return regenerate; }
            set { regenerate = value; }
        }

        [ExtendedPropertyAttribute()]
        public bool ReEncrypt
        {
            get { return reencrypt; }
            set { reencrypt = value; }
        }

        [ExtendedPropertyAttribute()]
        public string CertificateName
        {
            get { return this.certificateName; }
            set { this.certificateName = value; }
        }

        [ExtendedPropertyAttribute()]
        public string AsymmetricKeyName
        {
            get { return this.asymmetricKeyName; }
            set { this.asymmetricKeyName = value; }
        }

        #region ISfcValidate Members

        public ValidationState Validate(string methodName, params object[] arguments)
        {
            if (this.State == SqlSmoState.Creating || (this.State == SqlSmoState.Existing && this.ReEncrypt))
            {
                if (string.IsNullOrEmpty(this.Parent.EncryptorName))
                {
                    if (this.Parent.EncryptionType == DatabaseEncryptionType.ServerCertificate)
                    {
                        return new ValidationState(ExceptionTemplates.EnterServerCertificate, "EncryptorName");
                    }
                    if (this.Parent.EncryptionType == DatabaseEncryptionType.ServerAsymmetricKey)
                    {
                        return new ValidationState(ExceptionTemplates.EnterServerAsymmetricKey, "EncryptorName");
                    }
                }
                else
                {
                    if (this.Parent.EncryptionType == DatabaseEncryptionType.ServerCertificate 
                        && string.IsNullOrEmpty((string)certificateNameBackupDateHash[this.Parent.EncryptorName]))
                    {
                        string certificateBackupError = string.Format(CultureInfo.InvariantCulture, ExceptionTemplates.CertificateNotBackedUp, this.Parent.EncryptorName);
                        return new ValidationState(certificateBackupError, "EncryptorName", true);
                    }
                }
            }
            return this.Parent.Validate(methodName, arguments);
        }

        #endregion
    }
}
