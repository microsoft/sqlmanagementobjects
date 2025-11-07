// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Security;
using System.Security.Cryptography;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
#if !STRACE
using STrace = System.Diagnostics.Trace;
#endif

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// Represents a server connection saved to a registered server file. Used by SQL Server Management Studio's Registered Servers feature.
    /// </summary>
    public sealed partial class RegisteredServer : SfcInstance, ISfcValidate, ISfcCreatable, ISfcAlterable, ISfcDroppable, ISfcRenamable, IRenamable, ISfcMovable
    {
        #region Script generation
        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptRenameAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptMoveAction = new SfcTsqlProcFormatter();

        static RegisteredServer()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_sysmanagement_add_shared_registered_server";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_group_id", true));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_name", "ServerName", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_type", true));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_id", "ID", false, true));

            // Update script
            scriptAlterAction.Procedure = "msdb.dbo.sp_sysmanagement_update_shared_registered_server";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_id", "ID", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_name", "ServerName", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_sysmanagement_delete_shared_registered_server";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_id", "ID", true, false));

            // Rename script
            scriptRenameAction.Procedure = "msdb.dbo.sp_sysmanagement_rename_shared_registered_server";
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_id", "ID", true, false));
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_name", true));

            // Move script
            scriptMoveAction.Procedure = "msdb.dbo.sp_sysmanagement_move_shared_registered_server";
            scriptMoveAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("server_id", "ID", true, false));
            scriptMoveAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_parent_id", true));
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        public RegisteredServer()
        {
            SetDefaultCredentialPersistenceType();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public RegisteredServer(string name)
        {
            SetName(name);
            SetDefaultCredentialPersistenceType();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public RegisteredServer(ServerGroup parent, string name)
        {
            SetName(name);
            SetDefaultCredentialPersistenceType();
            this.Parent = parent;
        }

        private SecureString secureConnectionString = null;
        /// <summary>
        /// Gets or set the connection related data, specific to this
        /// server.
        /// </summary>
        [SfcIgnore]
        public SecureString SecureConnectionString
        {
            get
            {
                if (null == secureConnectionString)
                {
                    DbConnectionStringBuilder builder = new DbConnectionStringBuilder();

                    if (!string.IsNullOrEmpty(ConnectionStringWithEncryptedPassword))
                    {
                        // if the connection string has not been set by the user 
                        // we will extract it from the serialized version

                        builder.ConnectionString = ConnectionStringWithEncryptedPassword;

                        // the connection string may or may not contain a password
                        // it can be that we're using WinAuth, or it hasn't been saved
                        if (builder.ContainsKey("password"))
                        {
                            // decrypt the password using DPAPI after we read it from the file.
                            string password = builder["password"] as string;
                            if (password != null)
                            {
                                builder["password"] = ProtectData(password, false);
                            }
                        }
                        secureConnectionString = EncryptionUtility.EncryptString(builder.ConnectionString);
                    }
                    else 
                    {
                        // It is possible that the Parent is null since you can ask for this
                        // property on an non-created RegisteredServer instance. Case in point
                        // being call a default constructor which sets CredentialPersistenceType
                        // to None
                        if ((null != this.Parent) && (!this.Parent.IsLocal))
                        {
                            // If this is a shared server, all we know is that we must connect
                            // through integrated security and that we may know the server name
                            // the user wants to connect to. (We go straight to the property bag to
                            // avoid infinite recursion on the this.ServerName property checking
                            // the connection string.)
                            builder["server"] = Properties[nameof(ServerName)].Value as string;
                            builder["integrated security"] = "true";
                            // If the user set "trust server certificate" to true on the central connection, add it to all connections from that server
                            if (Parent.GetDomain().GetConnection() is ServerConnection server && server.TrustServerCertificate)
                            {
                                builder["trust server certificate"] = "true";
                            }
                            secureConnectionString = EncryptionUtility.EncryptString(builder.ConnectionString);
                        }
                    }
                }

                return this.secureConnectionString;
            }
            set
            {
                this.secureConnectionString = value;

                UpdateConnectionStringWithEncryptedPassword();
            }
        }

        /// <summary>
        /// Updates ConnectionStringWithEncryptedPassword based on changes made to 
        /// SecureConnectionString.
        /// </summary>
        private void UpdateConnectionStringWithEncryptedPassword()
        {
            if (this.Properties["CredentialPersistenceType"].Value == null)
            {
                throw new PropertyNotSetException("CredentialPersistenceType");
            }

            ConnectionStringWithEncryptedPassword = GetConnectionStringWithEncryptedPassword(CredentialPersistenceType);
        }

        internal string GetConnectionStringWithEncryptedPassword(CredentialPersistenceType cpt)
        {
            if (this.SecureConnectionString == null)
            {
                return null;
            }
            else
            {
                DbConnectionStringBuilder builder = new DbConnectionStringBuilder()
                {
                    ConnectionString = EncryptionUtility.DecryptSecureString(secureConnectionString)
                };


                switch (cpt)
                {
                    case CredentialPersistenceType.None:
                        // strip the password 
                        builder["password"] = null;
                        // strip the user name
                        // there are two possible synonyms for login name
                        builder["user id"] = null;
                        builder["uid"] = null;
                        break;
                    case CredentialPersistenceType.PersistLoginName:
                        // strip the password 
                        builder["password"] = null;
                        break;
                    case CredentialPersistenceType.PersistLoginNameAndPassword:
                        // encrypt the password using DPAPI before it gets
                        // saved into the file.
                        if (builder.ContainsKey("password"))
                        {
                            string password = builder["password"] as string;
                            if (password != null)
                            {
                                builder["password"] = ProtectData(password, true);
                            }
                        }
                        break;
                    default:
                        throw new RegisteredServerException(
                            RegSvrStrings.UnknownEnumeration("CredentialPersistenceType"));
                }

                return builder.ConnectionString;
            }
        }

        /// <summary>
        /// Encrypts the input string using the ProtectedData class.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encrypt"></param>
        /// <returns></returns>
        internal string ProtectData(string input, bool encrypt)
        {
            // convert the input to byte[] as this is what the underlying 
            // function expects
            byte[] inputBytes = null;
            if (encrypt)
            {
                inputBytes = Encoding.Unicode.GetBytes(input);
            }
            else
            {
                inputBytes = System.Convert.FromBase64String(input);
            }
            STrace.Assert(inputBytes != null);

            int tryCount = 0;
            bool success = false;

            byte[] outputBytes = null;
            while (tryCount < 10 && !success)
            {
                // we have retry logic because DPAPI can fail on high load 
                try
                {
                    if (encrypt)
                    {
                        outputBytes = ProtectedData.Protect(inputBytes, null, DataProtectionScope.LocalMachine);
                    }
                    else
                    {
                        outputBytes = ProtectedData.Unprotect(inputBytes, null, DataProtectionScope.LocalMachine);
                    }
                    success = true;
                }
                catch (CryptographicException)
                {
                    if (tryCount == 9)
                    {
                        // if we are at the last iteration
                        // we want to let the exception go through
                        throw;
                    }
                }
                catch (OutOfMemoryException)
                {
                    if (tryCount == 9)
                    {
                        // if we are at the last iteration
                        // we want to let the exception go through
                        throw;
                    }
                }

                tryCount++;
            }

            if (null == outputBytes)
            {
                return string.Empty;
            }
            else
            {
                if (encrypt)
                {
                    return Convert.ToBase64String(outputBytes);
                }
                else
                {
                    return Encoding.Unicode.GetString(outputBytes);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetDefaultCredentialPersistenceType()
        {
            //Set default as None - as a secure option
            this.CredentialPersistenceType = CredentialPersistenceType.None;
        }



        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public string ConnectionString
        {
            get
            {
                if (null != SecureConnectionString)
                {
                    return EncryptionUtility.DecryptSecureString(SecureConnectionString);
                }

                return string.Empty;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    string svrName = GetServerNameFromConnectionString(value);

                    //ServerName is not set in the connection string but ServerName property is set; use it in the connection string
                    if(string.IsNullOrEmpty(svrName)) 
                    {
                          if(!string.IsNullOrEmpty(this.ServerName)) 
                          {  
                              DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                              builder.ConnectionString = value;
                              this.SetServerNameInConnectionString(this.ServerName);
                           }
                    }
                    else 
                    {
                        if(!svrName.Equals((string)this.Properties["ServerName"].Value, StringComparison.OrdinalIgnoreCase))
                                this.Properties["ServerName"].Value = svrName;

                        SecureConnectionString = EncryptionUtility.EncryptString(value);
                    }
                }
                else
                {
                    SecureConnectionString = null;
                }
            }
        }

        /// <summary>
        /// Returns the connection object that corresponds to the connection string
        /// </summary>
        /// <returns></returns>
        public ISfcConnection GetConnectionObject()
        {
            switch (this.ServerType)
            {
                case ServerType.DatabaseEngine:
                    if (string.IsNullOrEmpty(this.ConnectionString))
                    {
                        // if the connection string has not been set we 
                        // will not attempt to return a default connection
                        return null;
                    }

                    SqlConnection conn = new SqlConnection(this.ConnectionString);
                    return new ServerConnection(conn);
                default:
                    // only the DatabaseEngine case is implemented
                    return null;
            }
        }


        #region SFC Boiler Plate
        internal const string typeName = "RegisteredServer";
        /// <summary>
        /// 
        /// </summary>
        public sealed class Key : SfcKey
        {
            /// <summary>
            /// Properties
            /// </summary>
            private string keyName;

            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public Key()
            {
            }

            /// <summary>
            /// Constructors
            /// </summary>
            /// <param name="other"></param>
            public Key(Key other)
            {
                keyName = other.Name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            public Key(string name)
            {
                keyName = name;
            }

            /// <summary>
            /// 
            /// </summary>
            public string Name
            {
                get
                {
                    return keyName;
                }
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                keyName = (string)filedDict["Name"];
            }

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return this == obj;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj1"></param>
            /// <param name="obj2"></param>
            /// <returns></returns>
            public static new bool Equals(object obj1, object obj2)
            {
                return (obj1 as Key) == (obj2 as Key);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public override bool Equals(SfcKey key)
            {
                return this == key;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(object obj, Key rightOperand)
            {
                if (obj == null || obj is Key)
                    return (Key)obj == rightOperand;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, object obj)
            {
                if (obj == null || obj is Key)
                    return leftOperand == (Key)obj;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, Key rightOperand)
            {
                // If both are null, or both are same instance, return true.
                if (System.Object.ReferenceEquals(leftOperand, rightOperand))
                    return true;

                // If one is null, but not both, return false.
                if (((object)leftOperand == null) || ((object)rightOperand == null))
                    return false;

                return leftOperand.IsEqual(rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(object obj, Key rightOperand)
            {
                return !(obj == rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, object obj)
            {
                return !(leftOperand == obj);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, Key rightOperand)
            {
                return !(leftOperand == rightOperand);
            }

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return this.Name.GetHashCode();
            }

            private bool IsEqual(Key key)
            {
                return string.CompareOrdinal(this.Name, key.Name) == 0;
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format("{0}[@Name='{1}']", RegisteredServer.typeName, SfcSecureString.EscapeSquote(Name));
            }

        } // public class Key

        // Singleton factory class
        private sealed class ObjectFactory : SfcObjectFactory
        {
            static readonly ObjectFactory instance = new ObjectFactory();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static ObjectFactory() { }

            ObjectFactory() { }

            public static ObjectFactory Instance
            {
                get { return instance; }
            }

            protected override SfcInstance CreateImpl()
            {
                return new RegisteredServer();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new ServerGroup Parent
        {
            get { return (ServerGroup)base.Parent; }
            set { base.Parent = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected internal override SfcKey CreateIdentityKey()
        {
            Key key = null;
            // if we don't have our key values we can't create a key
            if (this.Name != null)
            {
                key = new Key(this.Name);
            }
            return key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [SfcIgnore]
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
        [SfcKey(0)]
        public string Name
        {
            get
            {
                return (string)this.Properties["Name"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetName(string name)
        {
            this.Properties["Name"].Value = name;
        }

        /// <summary>
        /// There are 3 possible values to ServerName depending on the flavor of SQL and Server Type
        /// Hence checking for the possible values. The 3 possible values are picked up from   
        /// the method Microsoft.SqlServer.Management.UI.ConnectionDlg.UIConnectionInfoUtil.GetUIConnectionInfoFromConnectionString
        /// </summary>
        private string GetServerNameFromConnectionString(string connString)
        {
                DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                builder.ConnectionString = connString;
                string svrNameKey = this.GetServerNameKeyInConnectionString(connString);

                if(!string.IsNullOrEmpty(svrNameKey))
                    return (string)builder[svrNameKey];

                return string.Empty;
        }


        /// <summary>
        /// Helper function to Update connectionstring with new servername 
        /// </summary>
        private void SetServerNameInConnectionString(string svrName)
        {
              if(!string.IsNullOrEmpty(svrName)) 
              {  
                    DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                    builder.ConnectionString = this.ConnectionString;
                    string svrNameKey = this.GetServerNameKeyInConnectionString(this.ConnectionString);
                    if(!string.IsNullOrEmpty(svrNameKey))
                    {
                            builder[svrNameKey] = svrName;
                    }
                    else
                    {
                          //Set the ServerName equivalent inside the connection string  
                          switch (this.ServerType)
                          {
                                case ServerType.IntegrationServices:
                                        builder.Add("server", svrName);
                                        break;
                                case ServerType.ReportingServices:
                                         builder.Add("address", svrName);
                                         break;      
                                default:
                                         builder.Add("data source", svrName);
                                         break;      
                           }
                    }

                    //Update the Connection string
                    SecureConnectionString = EncryptionUtility.EncryptString(builder.ConnectionString);   
                }
        }
        

        /// <summary>
        /// Helper function to find the servername equivalent in connectionstring for different server type
        /// </summary>
        private string GetServerNameKeyInConnectionString(string connString)
        {
               if(!string.IsNullOrEmpty(connString))
               {
                   DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
                   builder.ConnectionString = connString;

                    foreach (string key in builder.Keys)
                    {
                        switch (key.ToLower())
                        {
                            case "server":      // Used by SQL, SSIS
                            case "data source": // Used by SQL, SQLCE, AS
                            case "address":     // Used by RS
                                    return key;
                            default:
                                    continue;
                        }
                    }
               }

               return string.Empty;  
        }


        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public int ID
        {
            get
            {
                return (int)this.Properties["ID"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string Description
        {
            get
            {
                return (string)this.Properties["Description"].Value;
            }
            set
            {
                this.Properties["Description"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string ServerName
        {
            get
            {
                //In case user passed in a connectionstring - return the servername from connectionstring
                string svrName = this.GetServerNameFromConnectionString(this.ConnectionString);
                if(!string.IsNullOrEmpty(svrName))
                    return svrName;

                return (string)this.Properties["ServerName"].Value;
            }
            set
            {
                if (String.IsNullOrEmpty(value))
                {
                    throw new ArgumentException(RegSvrStrings.ArgumentNullOrEmpty("ServerName"));
                }
                
                string svrName = this.GetServerNameFromConnectionString(this.ConnectionString);
                if(!svrName.Equals(value, StringComparison.OrdinalIgnoreCase))
                        this.SetServerNameInConnectionString(value);

                this.Properties["ServerName"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public bool UseCustomConnectionColor
        {
            get
            {
                SfcProperty p = this.Properties["UseCustomConnectionColor"];
                if (p.Value != null)
                {
                    return (bool) p.Value;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                this.Properties["UseCustomConnectionColor"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public int CustomConnectionColorArgb
        {
            get
            {
                SfcProperty p = this.Properties["CustomConnectionColorArgb"];
                if (p.Value != null)
                {
                    return (int)p.Value;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                this.Properties["CustomConnectionColorArgb"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public ServerType ServerType
        {
            get
            {
                SfcProperty p = this.Properties["ServerType"];

                if(p.Value == null)
                {
                    p.Value = Parent.ServerType;
                }

                return (ServerType)p.Value;
            }
        }

        /// <summary>
        /// Connection string that contains the password in encrypted form.
        /// Will always return empty string for shared servers.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string ConnectionStringWithEncryptedPassword
        {
            get
            {
                return (string)this.Properties["ConnectionStringWithEncryptedPassword"].Value;
            }
            internal set
            {
                this.Properties["ConnectionStringWithEncryptedPassword"].Value = value;
            }
        }

        /// <summary>
        /// Indicates whether the login name and the password will be saved. 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public CredentialPersistenceType CredentialPersistenceType
        {
            get
            {
                SfcProperty p = this.Properties["CredentialPersistenceType"];
                STrace.Assert(null != p);

                if ( null == p.Value && this.State == SfcObjectState.Pending)
                {
                    p.Value = CredentialPersistenceType.None;
                }

                return (CredentialPersistenceType)p.Value;
            }
            set
            {
                this.Properties["CredentialPersistenceType"].Value = value;
                // update ConnectionStringWithEncryptedPassword since it's based on 
                // CredentialPersistenceType
                UpdateConnectionStringWithEncryptedPassword();
            }
        }

        /// <summary>
        /// Additional parameters to append to the connection string
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string OtherParams
        {
            get
            {
                SfcProperty p = this.Properties["OtherParams"];
                if (p.Value != null)
                {
                    return p.Value.ToString();
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                this.Properties["OtherParams"].Value = value;
            }
        }

        /// <summary>
        /// Authentication type for connections where the connection string isn't sufficient to discover it
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public int AuthenticationType
        {
            get
            {
                var sfcProperty = this.Properties["AuthenticationType"];
                return Convert.ToInt32(sfcProperty.Value ?? Int32.MinValue);
            }
            set
            {
                this.Properties["AuthenticationType"].Value = value;
            }
        }

        /// <summary>
        /// Active Directory User id
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string ActiveDirectoryUserId
        {
            get
            {
                var sfcProperty = this.Properties["ActiveDirectoryUserId"];
                return Convert.ToString(sfcProperty.Value ?? string.Empty);
            }
            set { this.Properties["ActiveDirectoryUserId"].Value = value; }
        }

        /// <summary>
        /// Active Directory Tenant
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string ActiveDirectoryTenant
        {
            get
            {
                var sfcProperty = this.Properties["ActiveDirectoryTenant"];
                return Convert.ToString(sfcProperty.Value ?? string.Empty);
            }
            set { this.Properties["ActiveDirectoryTenant"].Value = value; }
        }

        /// <summary>
        /// Tag value that is managed by the host application
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string Tag
        {
            get
            {
                var sfcProperty = Properties[nameof(Tag)];
                return Convert.ToString(sfcProperty.Value ?? string.Empty);
            }
            set => Properties[nameof(Tag)].Value = value; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        protected internal override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                default:
                    throw new RegisteredServerException(RegSvrStrings.NoSuchCollection(elementType));
            }
        }
        #endregion

        #region ISfcValidate Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public ValidationState Validate(string methodName, params object[] arguments)
        {
            ValidationState validationState = new ValidationState();

            Validate(methodName, false, validationState);

            return validationState;
        }

        internal void Validate(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            ValidateStringProperty(validationMode, throwOnFirst, validationState, "Name", Name);
            ValidateStringProperty(validationMode, throwOnFirst, validationState, "ServerName", ServerName);
        }

        void ValidateStringProperty(string validationMode, bool throwOnFirst, ValidationState validationState, string propName, string propVal)
        {
            if (String.IsNullOrEmpty(propVal))
            {
                Exception ex = new SfcPropertyNotSetException(propName);
                if (throwOnFirst)
                {
                    throw ex;
                }
                else
                {
                    validationState.AddError(ex, propName);
                }
            }
        }

        #endregion

        #region Create

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ISfcScript ISfcCreatable.ScriptCreate()
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                this.Parent.ID.GetType(), this.Parent.ID));
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                this.Parent.ServerType.GetType(), this.Parent.ServerType));

            return new SfcTSqlScript(scriptCreateAction.GenerateScript(this, args));
        }

        /// <summary>
        /// 
        /// </summary>
        public void Create()
        {
            Validate(ValidationMethod.Create);
            base.CreateImpl();

            if (IsLocal && GetStore().IsSerializeOnCreation)
            {
                GetStore().Serialize();
            }
        }

        /// <summary>
        /// Perform post-create action
        /// </summary>
        protected override void PostCreate(object executionResult)
        {
            if (!IsLocal)
            {
                this.Properties["ID"].Value = executionResult;
            }
            this.Properties["ServerType"].Value = this.Parent.ServerType;
        }

        #endregion


        #region Alter
        /// <summary>
        /// 
        /// </summary>
        public void Alter()
        {
            Validate(ValidationMethod.Alter);
            base.AlterImpl();

            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }

        ISfcScript ISfcAlterable.ScriptAlter()
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            string script = scriptAlterAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }

        #endregion

        #region Drop

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ISfcScript ISfcDroppable.ScriptDrop()
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            string script = scriptDropAction.GenerateScript(this);
            return new SfcTSqlScript(script);
        }

        /// <summary>
        /// 
        /// </summary>
        public void Drop()
        {
            base.DropImpl();

            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }

        #endregion

        #region Rename
        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        public void Rename(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException(RegSvrStrings.ArgumentNullOrEmpty("Name"));
            }

            Rename(new Key(name));
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        /// <param name="key"></param>
        public void Rename(SfcKey key)
        {
            base.RenameImpl(key);
            
            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }
        
        ISfcScript ISfcRenamable.ScriptRename(SfcKey key)
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            Key tkey = (Key)key;
            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();

            args.Add(new SfcTsqlProcFormatter.RuntimeArg(tkey.Name.GetType(), tkey.Name));

            string script = scriptRenameAction.GenerateScript(this, args);
            return new SfcTSqlScript(script);
        }

        #endregion

        #region Move

        /// <summary>
        /// Moves the RegisteredServer to be a child of another ServerGroup.
        /// </summary>
        /// <param name="newParent"></param>
        public void Move(ServerGroup newParent)
        {
            ((ISfcMovable)this).Move(newParent);
        }
        
        void ISfcMovable.Move(SfcInstance newParent)
        {
            if (newParent == null)
            {
                throw new ArgumentNullException("newParent");
            }

            if (!(newParent is ServerGroup))
            {
                throw new InvalidArgumentException("newParent");
            }
            base.MoveImpl(newParent);
            STrace.Assert(this.Parent == newParent);
            STrace.Assert(this.Parent.RegisteredServers.Contains(this));
            
            if (IsLocal)
            {
                GetStore().Serialize();
            }
        }
        
        ISfcScript ISfcMovable.ScriptMove(SfcInstance newParent)
        {
            if (IsLocal)
            {
                return new SfcTSqlScript();
            }

            STrace.Assert(newParent is ServerGroup);
            ServerGroup newParentGroup = (ServerGroup) newParent;
            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                                                         newParentGroup.ID.GetType(), newParentGroup.ID));

            string moveScript = scriptMoveAction.GenerateScript(this, args);
            return new SfcTSqlScript(moveScript);
        }

        #endregion

        /// <summary>
        /// Returns the IsLocal property of the RegisteredServersStore
        /// that this instance is associated with.
        /// </summary>
        [SfcIgnore]
        public bool IsLocal
        {
            get
            {
                ISfcDomain domain = this.KeyChain.RootKey.Domain;
                STrace.Assert(domain is RegisteredServersStore);
                return ((RegisteredServersStore)domain).IsLocal;
            }
        }

        /// <summary>
        /// Returns if SFC believes this is a dropped object.
        /// </summary>
        [SfcIgnore]
        public bool IsDropped
        {
            get
            {
                return this.State == SfcObjectState.Dropped;
            }
        }
        
        internal RegisteredServersStore GetStore()
        {
            RegisteredServersStore store = KeyChain.RootKey.Domain as RegisteredServersStore;
            STrace.Assert(store != null);
            return store;
        }

        /// <summary>
        /// Exports the content of the group to a file.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cpt"></param>
        public void Export(string file, CredentialPersistenceType cpt)
        {
            RegisteredServersStore.Export(this, file, cpt);
        }
    }

    /// <summary>
    /// Directs what credentials will be persisted in the local store or
    /// when serializing a shared server.
    /// </summary>
    public enum CredentialPersistenceType
    {
        /// No credentials will be persisted
        None = 0,
        /// Login name is going to be presisted
        PersistLoginName,
        /// Login name and password are both going to be persisted
        PersistLoginNameAndPassword
    }

}
