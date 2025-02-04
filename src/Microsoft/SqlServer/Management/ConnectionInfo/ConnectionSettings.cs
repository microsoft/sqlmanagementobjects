// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
    using System.Diagnostics;
    using System.Security;
    using Microsoft.SqlServer.Server;
    using System.Runtime.CompilerServices;

    ///<summary>
    ///connection settings and creation
    ///</summary>
    public class ConnectionSettings
    {
        bool m_BlockUpdates;
        bool m_ResetConnectionString;

        string m_ServerInstance;
        string m_Login;
        SecureString m_Password;
        bool m_LoginSecure;
        string m_ConnectAsUserName;
        SecureString m_ConnectAsUserPassword;
        bool m_ConnectAsUser;
        bool m_NonPooledConnection;
        bool m_TrustServerCertificate;
        int m_PooledConnectionLifetime;
        int m_MinPoolSize;
        int m_MaxPoolSize;
        int m_ConnectTimeout;
        NetworkProtocol m_NetworkProtocol;
        string m_ApplicationName;
        string m_WorkstationId;
        string m_DatabaseName;
        int m_PacketSize;
        private SecureString m_ConnectionString;
        private bool m_MultipleActiveResultSets;
        private bool shouldEncryptConnection = false;
        private bool strictEncryption;
#if MICROSOFTDATA
        private string hostNameInCertificate;
        private string serverCertificate;
#endif
        private string additionalParameters = null;
        private SqlConnectionInfo.AuthenticationMethod m_Authentication;
        private string m_ApplicationIntent;

        public IRenewableToken AccessToken { get; set; }

        /// <summary>
        /// Infinite wait
        /// </summary>
        public const int NoConnectionTimeout = 0;

        //connection defaults
        private const int ConnectionTimeout_Default = 15;
        private const int PooledConnectionLifetime_Default = 0;
        private const int MinPoolSize_Default = 0;
        private const int MaxPoolSize_Default = 100;
        private const NetworkProtocol NetworkProtocol_Default = NetworkProtocol.NotSpecified;
        private const int PacketSize_Default = 8192;
        private const bool NonPooledConnection_Default = false;
        private const bool MultipleActiveResultSets_Default = false;

        /// <summary>
        /// TBD
        /// </summary>
        internal ConnectionSettings()
        {
            InitDefaults();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal ConnectionSettings(SqlConnectionInfo sci)
        {
            InitDefaults();
            InitFromSqlConnectionInfo(sci);
        }

        /// <summary>
        /// TBD
        /// </summary>
        private void InitDefaults()
        {
            m_BlockUpdates = false;
            m_ResetConnectionString = true;

            m_ServerInstance = "(local)";
            m_Login = String.Empty;
            m_Password = null;
            m_LoginSecure = true;
            m_ConnectAsUserName = String.Empty;
            m_ConnectAsUserPassword = null;
            m_ConnectAsUser = false;
            m_NonPooledConnection = NonPooledConnection_Default;
            m_PooledConnectionLifetime = PooledConnectionLifetime_Default;
            m_MinPoolSize = MinPoolSize_Default;
            m_MaxPoolSize = MaxPoolSize_Default;
            m_ConnectTimeout = ConnectionTimeout_Default;
            m_NetworkProtocol = NetworkProtocol_Default;
            m_ApplicationName = String.Empty;
            m_WorkstationId = String.Empty;
            m_DatabaseName = String.Empty;
            m_PacketSize = PacketSize_Default;
            m_MultipleActiveResultSets = MultipleActiveResultSets_Default;
            m_TrustServerCertificate = false;
            m_Authentication = SqlConnectionInfo.AuthenticationMethod.NotSpecified;
            m_ApplicationIntent = null;
        }

        internal void CopyConnectionSettings(ConnectionSettings cs)
        {
            cs.m_ServerInstance = m_ServerInstance;
            cs.m_Login = m_Login;
            cs.m_Password = m_Password?.Copy();
            cs.m_LoginSecure = m_LoginSecure;
            cs.m_ConnectAsUserName = m_ConnectAsUserName;
            cs.m_ConnectAsUserPassword = m_ConnectAsUserPassword?.Copy();
            cs.m_ConnectAsUser = m_ConnectAsUser;
            cs.m_NonPooledConnection = m_NonPooledConnection;
            cs.m_PooledConnectionLifetime = m_PooledConnectionLifetime;
            cs.m_MinPoolSize = m_MinPoolSize;
            cs.m_MaxPoolSize = m_MaxPoolSize;
            cs.m_ConnectTimeout = m_ConnectTimeout;
            cs.m_NetworkProtocol = m_NetworkProtocol;
            cs.m_ApplicationName = m_ApplicationName;
            cs.m_WorkstationId = m_WorkstationId;
            cs.m_DatabaseName = m_DatabaseName;
            cs.m_PacketSize = m_PacketSize;
            cs.m_MultipleActiveResultSets = m_MultipleActiveResultSets;
            cs.shouldEncryptConnection = shouldEncryptConnection;
            cs.strictEncryption = StrictEncryption;
#if MICROSOFTDATA
            cs.hostNameInCertificate = hostNameInCertificate;
            cs.serverCertificate = serverCertificate;
#endif
            cs.additionalParameters = additionalParameters;
            cs.m_TrustServerCertificate = m_TrustServerCertificate;
            cs.m_Authentication = m_Authentication;
            cs.m_ApplicationIntent = m_ApplicationIntent;
            cs.AccessToken = AccessToken;
            cs.m_ConnectionString = m_ConnectionString;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sci"></param>
        internal void InitFromSqlConnectionInfo(SqlConnectionInfo sci)
        {
            if (IsValidString(sci.ApplicationName))
            {
                m_ApplicationName = sci.ApplicationName;
            }
            if (IsValidString(sci.WorkstationId))
            {
                m_WorkstationId = sci.WorkstationId;
            }
            m_NetworkProtocol = (NetworkProtocol)(int)sci.ConnectionProtocol;

            if (sci.PoolConnectionLifeTime >= 0)
            {
                m_PooledConnectionLifetime = sci.PoolConnectionLifeTime;
            }

            if (sci.MaxPoolSize > 0)
            {
                m_MaxPoolSize = sci.MaxPoolSize;
            }
            if (sci.MinPoolSize >= 0)
            {
                m_MinPoolSize = sci.MinPoolSize;
            }
            if (false == sci.Pooled)
            {
                m_NonPooledConnection = true;
            }

            if (IsValidString(sci.ServerName, false))
            {
                m_ServerInstance = sci.ServerName;
            }
            LoginSecure = sci.UseIntegratedSecurity;
            if (!m_LoginSecure)
            {
                if (IsValidString(sci.UserName))
                {
                    m_Login = sci.UserName;
                }
                if (IsValidString(sci.Password, false))
                {
                    Password = sci.Password;
                }
            }
            if (IsValidString(sci.DatabaseName, false))
            {
                m_DatabaseName = sci.DatabaseName;
            }
            if (sci.ConnectionTimeout >= 0)
            {
                m_ConnectTimeout = sci.ConnectionTimeout;
            }

            if (sci.PacketSize >= 0)
            {
                m_PacketSize = sci.PacketSize;
            }

            if (sci.EncryptConnection)
            {
                shouldEncryptConnection = true;
            }

            if (sci.StrictEncryption)
            {
                StrictEncryption = sci.StrictEncryption;
            }
#if MICROSOFTDATA
            hostNameInCertificate = sci.HostNameInCertificate;
            serverCertificate = sci.ServerCertificate;
#endif
            m_TrustServerCertificate = sci.TrustServerCertificate;

            m_Authentication = sci.Authentication;

            m_ApplicationIntent = sci.ApplicationIntent;

            AccessToken = sci.AccessToken;

            additionalParameters = sci.AdditionalParameters;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sc"></param>
        internal void InitFromSqlConnection(SqlConnection sc)
        {
            NonPooledConnection = true;
            if (!SqlContext.IsAvailable)
            {
                ServerInstance = sc.DataSource;
                PacketSize = sc.PacketSize;
                ConnectTimeout = sc.ConnectionTimeout;
                WorkstationId = sc.WorkstationId ?? string.Empty;
            }
            DatabaseName = sc.Database;
            var connectionStringBuilder = new SqlConnectionStringBuilder(sc.ConnectionString);
            Authentication = (SqlConnectionInfo.AuthenticationMethod)Enum.Parse(typeof(SqlConnectionInfo.AuthenticationMethod), connectionStringBuilder.Authentication.ToString());
            // Rather than keep track of the SqlCredential separately, we'll just merge the user name and password into our
            // connectionstring, which we store as a SecureString. There are too many random pieces of code that grab our ConnectionString
            // property directly to create a SqlConnection to try to make them all aware of SqlCredential too. 
            if (sc.Credential != null)
            {
                connectionStringBuilder.UserID = sc.Credential.UserId;
                connectionStringBuilder.Password = EncryptionUtility.DecryptSecureString(sc.Credential.Password);
            }
            m_LoginSecure = connectionStringBuilder.IntegratedSecurity;
            m_Login = connectionStringBuilder.UserID;
            m_Password = EncryptionUtility.EncryptString(connectionStringBuilder.Password);
            shouldEncryptConnection = connectionStringBuilder.Encrypt;
#if MICROSOFTDATA
            StrictEncryption = connectionStringBuilder.Encrypt == SqlConnectionEncryptOption.Strict;
            hostNameInCertificate = connectionStringBuilder.HostNameInCertificate;
            serverCertificate = connectionStringBuilder.ServerCertificate;
#endif
            TrustServerCertificate = connectionStringBuilder.TrustServerCertificate;
            // Assigning ConnectionString must be the last line of this method
            ConnectionString = connectionStringBuilder.ConnectionString;
        }

        ///<summary>
        /// The name of the SQL Server the object will connect to.
        /// If not set, the local server is implied.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public string ServerInstance
        {
            get
            {
                return m_ServerInstance;
            }
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("ServerInstance");
                ThrowIfInvalidValue(value, "ServerInstance", false);
                m_ServerInstance = value;
            }
        }

        /// <summary>
        /// true if Login property has been initialized
        /// </summary>
        internal bool IsLoginInitialized
        {
            get { return IsValidString(m_Login); }
        }

        ///<summary>
        /// The SQL Server Login name that is used to connect to the server.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// PropertyNotSetException
        /// </summary>
        public string Login
        {
            get => m_Login;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("Login");
                ThrowIfLoginSecure("Login");
                ThrowIfInvalidValue(value, "Login");
                m_Login = value;
            }
        }

        /// <summary>
        /// true if Password property has been initialized
        /// </summary>
        internal bool IsPasswordInitialized
        {
            get => IsValidString(Password, false);
        }

        ///<summary>
        /// The password that is used with Login to connect to the server.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// PropertyNotSetException
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public string Password
        {
            get => m_Password != null ? EncryptionUtility.DecryptSecureString(m_Password) : string.Empty;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("Password");
                ThrowIfLoginSecure("Password");
                ThrowIfInvalidValue(value, "Password", false);

                if ((value != null) && (value.Length != 0))
                {
                    m_Password = EncryptionUtility.EncryptString(value);
                }
                else
                {
                    m_Password = null;
                }
            }
        }

        ///<summary>
        /// The password that is used with Login to connect to the server.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// PropertyNotSetException
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public SecureString SecurePassword
        {
            get => m_Password == null ? new SecureString() : m_Password.Copy();
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("Password");
                ThrowIfLoginSecure("Password");

                if (value != null)
                {
                    ThrowIfInvalidValue(EncryptionUtility.DecryptSecureString(value), "Password", false);
                    m_Password = value.Copy();
                }
                else
                {
                    ThrowIfInvalidValue((String)null, "Password", false);
                    m_Password = null;
                }
            }
        }


        internal void ForceSetPassword(string value) =>
            m_Password = ((value != null) && (value.Length != 0))
                ? EncryptionUtility.EncryptString(value)
                : null;

        ///<summary>
        /// If set to true, Windows integrated security is used and Login and Password are ignored.
        /// If not set, Sql Server Authentication is used.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// </summary>
        public bool LoginSecure
        {
            get => m_LoginSecure;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("LoginSecure");
                m_LoginSecure = value;
            }
        }

        ///<summary>
        /// Specifies the Windows user that is used when creating an impersonated connection. 
        /// The user must have interactive logon privileges.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// PropertyNotSetException
        /// </summary>
        /// <remarks>Two user name formats are supported: "domain\user" and "user@domain"</remarks>
        public string ConnectAsUserName
        {
            get => m_ConnectAsUserName;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("ConnectAsUserName");
                ThrowIfInvalidValue(value, "ConnectAsUserName");
                m_ConnectAsUserName = value;
            }
        }

        ///<summary>
        /// Specifies password of the Windows user that is used when creating an impersonated connection.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// PropertyNotSetException
        /// </summary>
        public string ConnectAsUserPassword
        {
            get => m_ConnectAsUserPassword != null ? EncryptionUtility.DecryptSecureString(m_ConnectAsUserPassword) : string.Empty;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("ConnectAsUserPassword");
                ThrowIfInvalidValue(value, "ConnectAsUserPassword");

                if (!string.IsNullOrEmpty(value))
                {
                    m_ConnectAsUserPassword = EncryptionUtility.EncryptString(value);
                }
                else
                {
                    m_ConnectAsUserPassword = null;
                }
            }
        }

        ///<summary>
        /// If set to true, the connection will be made with the credentials
        /// of the specified user. This will assume impersonation, however
        /// the LoginSecure flags and Login and Password fields will
        /// not be affected.
        /// This setting is only usable on Windows.
        /// </summary>
        /// <exception cref="ConnectionCannotBeChangedException" />
        public bool ConnectAsUser
        {
            get => m_ConnectAsUser;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("ConnectAsUser");
                m_ConnectAsUser = value;
            }
        }

        ///<summary>
        /// By default, all connections to SQL Server are taken from an ADO.NET connection
        /// pool.If set to true, this allows users to create a non-pooled connection
        /// (will not be reused when closed).
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// </summary>
        public bool NonPooledConnection
        {
            get => m_NonPooledConnection;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("NonPooledConnection");
                m_NonPooledConnection = value;
            }
        }

        ///<summary>
        /// When a connection is returned to the pool, its creation time is compared with
        /// the current time, and the connection is destroyed if that time span (in seconds)
        /// exceeds the value specified by connection lifetime.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public int PooledConnectionLifetime
        {
            get => m_PooledConnectionLifetime;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("PooledConnectionLifetime");
                ThrowIfInvalidValue(value, 0, "PooledConnectionLifetime");
                m_PooledConnectionLifetime = value;
            }
        }

        ///<summary>
        /// The minimum number of connections maintained in the pool.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public int MinPoolSize
        {
            get => m_MinPoolSize;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("MinPoolSize");
                ThrowIfInvalidValue(value, 0, "MinPoolSize");
                m_MinPoolSize = value;
            }
        }

        ///<summary>
        /// The maximum number of connections allowed in the pool.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public int MaxPoolSize
        {
            get => m_MaxPoolSize;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("MaxPoolSize");
                ThrowIfInvalidValue(value, 2, "MaxPoolSize");
                m_MaxPoolSize = value;
            }
        }

        ///<summary>
        /// The length of time (in seconds) to wait for a connection to the server before
        /// terminating the attempt and throwing an exception.
        /// Default is 30 seconds (same as Shiloh).
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public int ConnectTimeout
        {
            get => m_ConnectTimeout;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("ConnectTimeout");
                ThrowIfInvalidValue(value, 0, "ConnectTimeout");
                m_ConnectTimeout = value;
            }
        }

        ///<summary>
        /// The authentication type of the connection
        /// </summary>
        /// <value></value>
        public SqlConnectionInfo.AuthenticationMethod Authentication
        {
            get => m_Authentication;
            set
            {
                ThrowIfUpdatesAreBlocked();
                m_Authentication = value;
            }
        }


        ///<summary>
        /// The application intent of the connection
        /// Valid values are ReadWrite and ReadOnly
        /// </summary>
        /// <value></value>
        public string ApplicationIntent
        {
            get => m_ApplicationIntent;
            set
            {
                ThrowIfUpdatesAreBlocked();
                m_ApplicationIntent = value;
            }
        }

        ///<summary>
        /// Indicate whether the client trusts the server certificate
        /// </summary>
        /// <value></value>
        public bool TrustServerCertificate
        {
            get => m_TrustServerCertificate;
            set
            {
                ThrowIfUpdatesAreBlocked();
                m_TrustServerCertificate = value;
            }
        }

        ///<summary>
        /// The property will return either the user specified connection string or it will
        /// return the computed connection string based on object settings.
        /// If explicitly set, All properties in the ServerConnection object will be populated to reflect the passed in connection string.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public string ConnectionString
        {
            get => null == m_ConnectionString ? GetConnectionString() : EncryptionUtility.DecryptSecureString(m_ConnectionString);
            set
            {
                ThrowIfInvalidValue(value, "ConnectionString", false);

                if (!string.IsNullOrEmpty(value))
                {
                    m_ConnectionString = EncryptionUtility.EncryptString(value);
                    var connStr = new SqlConnectionStringBuilder(value);
                    m_DatabaseName = connStr.InitialCatalog;
                }
                else
                {
                    m_ConnectionString = null;
                    m_DatabaseName = string.Empty;
                }

                m_ResetConnectionString = true;
            }
        }

        ///<summary>
        /// The property will return a SecureString version of either
        /// the user specified connection string or it will return the
        /// computed connection string based on object settings.  If
        /// explicitly set, All properties in the ServerConnection
        /// object will be populated to reflect the passed in
        /// connection string.  Exceptions:
        /// ConnectionCannotBeChangedException
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        public SecureString SecureConnectionString
        {
            get => m_ConnectionString ?? EncryptionUtility.EncryptString(GetConnectionString());
            set
            {
                ThrowIfUpdatesAreBlocked();
                if ((value != null) && (value.Length != 0))
                {
                    m_ConnectionString = value;
                }
                else
                {
                    m_ConnectionString = null;
                }

                m_ResetConnectionString = true;
            }
        }

        ///<summary>
        /// Identifies the client network protocol that is used to connect to SQL Server.
        /// If you do not specify a network and you use a local server, shared memory is used.
        /// If you do not specify a network and you use a remote server, the one of the
        /// configured client protocols is used.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public NetworkProtocol NetworkProtocol
        {
            get => m_NetworkProtocol;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("NetworkProtocol");
                m_NetworkProtocol = value;
            }
        }

        /// <summary>
        /// true if ApplicationName property has been initialized
        /// </summary>
        internal bool IsApplicationNameInitialized
        {
            get => IsValidString(m_ApplicationName);
        }

        ///<summary>
        /// The name of the application. 'SQL Management' if no application name
        /// has been provided.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public string ApplicationName
        {
            get => m_ApplicationName;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("ApplicationName");
                ThrowIfInvalidValue(value, "ApplicationName");
                m_ApplicationName = value;
            }
        }

        /// <summary>
        /// true if WorkstationId property has been initialized
        /// </summary>
        internal bool IsWorkstationIdInitialized
        {
            get => IsValidString(m_WorkstationId);
        }

        ///<summary>
        /// The name of the workstation connecting to SQL Server.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public string WorkstationId
        {
            get => m_WorkstationId;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("WorkstationId");
                ThrowIfInvalidValue(value, "WorkstationId", false);
                m_WorkstationId = value;
            }
        }

        /// <summary>
        /// true if DatabaseName property has been initialized
        /// </summary>
        internal bool IsDatabaseNameInitialized
        {
            get => IsValidString(m_DatabaseName);
        }

        /// <summary>
        /// TBD
        /// </summary>
        public string DatabaseName
        {
            get => m_DatabaseName;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("DatabaseName");
                ThrowIfInvalidValue(value, "DatabaseName", false);
                m_DatabaseName = value;
            }
        }

        ///<summary>
        /// Size in bytes of the network packets used to communicate with an instance
        /// of SQL Server. Default is 8192.
        /// Exceptions:
        /// ConnectionCannotBeChangedException
        /// InvalidPropertyValueException
        /// </summary>
        public int PacketSize
        {
            get => m_PacketSize;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("PacketSize");
                ThrowIfInvalidValue(value, 0, "PacketSize");
                m_PacketSize = value;
            }
        }

        /// <summary>
        /// Enable MARS from the connection. Default is false.
        /// </summary>
        /// <value></value>
        public bool MultipleActiveResultSets
        {
            get => m_MultipleActiveResultSets;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("MultipleActiveResultSets");
                m_MultipleActiveResultSets = value;
            }

        }

        /// <summary>
        /// Returns true if the current values for LoginSecure and Authentication use the Login property
        /// </summary>
        public bool AcceptsLogin
        {
            get
            {
                if (LoginSecure)
                {
                    return false;
                }
                switch (Authentication)
                {
                    case SqlConnectionInfo.AuthenticationMethod.SqlPassword:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDefault:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryManagedIdentity:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryServicePrincipal:
                    case SqlConnectionInfo.AuthenticationMethod.NotSpecified:
                        return true;
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDeviceCodeFlow:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated:
                        return false;
                }
                Debug.Assert(false, $"Unknown Authentication method {Authentication}");
                return true;
            }
        }

        /// <summary>
        /// Returns true if the current values for LoginSecure and Authentication require a Login property to be set
        /// </summary>
        public bool RequiresLogin
        {
            get
            {
                switch (Authentication)
                {
                    case SqlConnectionInfo.AuthenticationMethod.SqlPassword:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword:
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryServicePrincipal:
#if !MICROSOFTDATA
                    case SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive:
#endif
                        return true;
                    case SqlConnectionInfo.AuthenticationMethod.NotSpecified:
                        return AccessToken == null && !LoginSecure;
                }
                return false;
            }
        }
        /// <summary>
        /// whether "encrypt=true" is specified in the connection string
        /// </summary>
        /// <value></value>
        public bool EncryptConnection
        {
            get => shouldEncryptConnection;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet("EncryptConnection");
                shouldEncryptConnection = value;
            }
        }

        /// <summary>
        /// Whether "encrypt=strict" is specified in the connection string. 
        /// When true, the value of <see cref="EncryptConnection"/> is ignored.
        /// </summary>
        public bool StrictEncryption
        {
            get => strictEncryption;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet();
                strictEncryption = value;
            }
        }

#if MICROSOFTDATA
        /// <summary>
        /// The host name provided in certificate to be used for certificate validation.
        /// </summary>
        public string HostNameInCertificate
        {
            get => hostNameInCertificate;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet();
                hostNameInCertificate = value;
            }
        }

        /// <summary>
        /// The path to server certificate to be used for certificate validation.
        /// </summary>
        public string ServerCertificate
        {
            get => serverCertificate;
            set
            {
                ThrowIfUpdatesAreBlocked();
                ThrowIfConnectionStringIsSet();
                serverCertificate = value;
            }
        }
#endif

        /// <summary>
        /// Returns whether additional parameters have been specified in connection string
        /// </summary>
        internal string AdditionalParameters
        {
            get => additionalParameters;
        }

        private void ThrowIfConnectionStringIsSet([CallerMemberName] string propertyName = "")
        {
            if ((m_ConnectionString != null) && IsValidString(ConnectionString))
            {
                throw new PropertyNotAvailableException(StringConnectionInfo.PropertyNotAvailable(propertyName));
            }
            else
            {
                //If any of the field of Connection String is changed that means the user's intent is to reset the connection string.
                m_ResetConnectionString = true;
            }
        }

        internal bool IsReadAccessBlocked
        {
            get => (m_ConnectionString != null) && IsValidString(ConnectionString);
        }

        /// <summary>
        /// check that we are not already connected
        /// </summary>
        private void ThrowIfUpdatesAreBlocked()
        {
            if (BlockUpdates)
            {
                throw new ConnectionCannotBeChangedException(StringConnectionInfo.ConnectionCannotBeChanged);
            }
        }

        private void ThrowIfLoginSecure(string propertyName)
        {
            if (true == LoginSecure)
            {
                throw new InvalidPropertyValueException(
                    StringConnectionInfo.CannotSetWhenLoginSecure(propertyName));
            }
        }

        private void ThrowIfInvalidValue(string str, string propertyName) => ThrowIfInvalidValue(str, propertyName, true);

        /// <summary>
        /// check if the input value is valid ( != null )
        /// </summary>
        /// <param name="str">string to be checked</param>
        /// <param name="propertyName"></param>
        /// <param name="checkEmpty"></param>
        private void ThrowIfInvalidValue(string str, string propertyName, bool checkEmpty)
        {
            if (!IsValidString(str, checkEmpty))
            {
                throw new InvalidPropertyValueException(
                    StringConnectionInfo.InvalidPropertyValue(str ?? "null", propertyName, StringConnectionInfo.InvalidPropertyValueReasonString));
            }
        }

        protected string ThrowIfPropertyNotSet(string propertyName, string str)
            => ThrowIfPropertyNotSet(propertyName, str, true);

        protected string ThrowIfPropertyNotSet(string propertyName, string str, bool checkEmpty)
        {
            if (!IsValidString(str, checkEmpty))
            {
                throw new PropertyNotSetException(StringConnectionInfo.PropertyNotSetException(propertyName));
            }
            return str;
        }

        /// <summary>
        /// check if the input value is valid ( > 0 )
        /// </summary>
        /// <param name="n">integer to be checked</param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        private void ThrowIfInvalidValue(int n, int value, string propertyName)
        {
            if (n < value)
            {
                throw new InvalidPropertyValueException(
                    StringConnectionInfo.InvalidPropertyValue(n.ToString(ConnectionInfoBase.DefaultCulture),
                                                        propertyName,
                                                        StringConnectionInfo.InvalidPropertyValueReasonInt(value.ToString(ConnectionInfoBase.DefaultCulture))));
            }
        }

        private bool IsValidString(string str) => IsValidString(str, true);

        private bool IsValidString(string str, bool checkEmpty) => null != str && !(checkEmpty && str.Length <= 0);

        private string GetNetworkProtocolString()
        {
            string strNetProtocol = String.Empty;
            switch (NetworkProtocol)
            {
                case NetworkProtocol.TcpIp:
                    strNetProtocol = "dbmssocn";
                    break;

                case NetworkProtocol.NamedPipes:
                    strNetProtocol = "dbnmpntw";
                    break;

                case NetworkProtocol.Multiprotocol:
                    strNetProtocol = "dbmsrpcn";
                    break;

                case NetworkProtocol.AppleTalk:
                    strNetProtocol = "dbmsadsn";
                    break;

                case NetworkProtocol.BanyanVines:
                    strNetProtocol = "dbmsvinn";
                    break;

                case NetworkProtocol.Via:
                    strNetProtocol = "dbmsgnet";
                    break;

                case NetworkProtocol.SharedMemory:
                    strNetProtocol = "dbmslpcn";
                    break;

                case NetworkProtocol.NWLinkIpxSpx:
                    strNetProtocol = "dbmsspxn";
                    break;
            }
            return strNetProtocol;
        }

        /// <summary>
        /// builds the connection string
        /// </summary>
        private string GetConnectionString()
        {
            SqlConnectionStringBuilder sbConnectionString = new SqlConnectionStringBuilder();
            string stringToReturn;

            if (SqlContext.IsAvailable)
            {
                // Internal data access - we're running inside SQLCLR.
#if !MICROSOFTDATA
                // SqlContext.IsAvailable should never be true except when running inside the SQL engine.
                // Even though ContextConnection property is deprecated in M.D.S 3.0, we need this to be used
                // for SQL CLR support in Sql Server, hence we are still using it when building against S.D.S
                sbConnectionString.ContextConnection = true;
#endif
                stringToReturn = sbConnectionString.ToString();
            }
            else
            {
                sbConnectionString.DataSource = ServerInstance;
                if (LoginSecure)   // Trusted login
                {
                    sbConnectionString.IntegratedSecurity = true;
                }
                else if (AcceptsLogin)
                {
                    if (RequiresLogin)
                    {
                        sbConnectionString.UserID = ThrowIfPropertyNotSet(nameof(Login), Login);
                        if (Authentication != SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive || !string.IsNullOrEmpty(Password))
                        {
                            sbConnectionString.Password = ThrowIfPropertyNotSet(nameof(Password), Password, checkEmpty: false);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(Login))
                        {
                            sbConnectionString.UserID = Login;
                        }
                        if (!string.IsNullOrEmpty(Password))
                        {
                            sbConnectionString.Password = Password;
                        }
                    }
                }

                if (ConnectTimeout != ConnectionTimeout_Default)
                {
                    sbConnectionString.ConnectTimeout = ConnectTimeout;
                }
                if (NetworkProtocol != NetworkProtocol.NotSpecified)
                {
#if !NETCOREAPP
                    //NetworkLibrary property not implemented in .NetCore
                    sbConnectionString.NetworkLibrary = GetNetworkProtocolString();
#endif
                }
                // database name
                if (IsValidString(m_DatabaseName))
                {
                    sbConnectionString.InitialCatalog = DatabaseName;
                }

                // workstation id
                if (IsValidString(m_WorkstationId))
                {
                    sbConnectionString.WorkstationID = WorkstationId;
                }

                // application name
                if (IsValidString(m_ApplicationName))
                {
                    sbConnectionString.ApplicationName = ApplicationName;
                }

                if (PooledConnectionLifetime != PooledConnectionLifetime_Default)
                {
                    // The property LoadBalnceTimeout corresponds to the minimum time, in seconds, for the connection to live in the connection pool before being destroyed
                    sbConnectionString.LoadBalanceTimeout = PooledConnectionLifetime;
                }

                if (MaxPoolSize != MaxPoolSize_Default && MaxPoolSize > 0)
                {
                    sbConnectionString.MaxPoolSize = MaxPoolSize;
                }

                if (MinPoolSize != MinPoolSize_Default)
                {
                    sbConnectionString.MinPoolSize = MinPoolSize;
                }

                if (NonPooledConnection != NonPooledConnection_Default)
                {
                    sbConnectionString.Pooling = false;
                }

                if (PacketSize != PacketSize_Default)
                {
                    sbConnectionString.PacketSize = PacketSize;
                }

                if (StrictEncryption)
                {
#if MICROSOFTDATA
                    sbConnectionString.Encrypt = SqlConnectionEncryptOption.Strict;
#else
                    sbConnectionString.Encrypt = true;
#endif
                }
                else
                {
                    sbConnectionString.Encrypt = shouldEncryptConnection;
                }

#if MICROSOFTDATA
                if (!string.IsNullOrEmpty(hostNameInCertificate))
                {
                    sbConnectionString.HostNameInCertificate = hostNameInCertificate;
                }
                if (!string.IsNullOrEmpty(serverCertificate))
                {
                    sbConnectionString.ServerCertificate = serverCertificate;
                }
#endif
                sbConnectionString.TrustServerCertificate = TrustServerCertificate;

                if (AccessToken == null && Authentication != SqlConnectionInfo.AuthenticationMethod.NotSpecified)
                {
                    SetAuthentication(sbConnectionString);
                }

                if (IsValidString(ApplicationIntent))
                {
                    SetApplicationIntent(sbConnectionString);
                }

                sbConnectionString.MultipleActiveResultSets = MultipleActiveResultSets;

                stringToReturn = sbConnectionString.ToString();

                // Append the additional parameters to existing connection string
                if (!String.IsNullOrEmpty(additionalParameters))
                {
                    stringToReturn += ";" + additionalParameters;
                }
            }

            return stringToReturn;
        }

        /// <summary>
        /// Set Authentication using reflection
        /// </summary>
        private void SetAuthentication(SqlConnectionStringBuilder sbConnectionString)
        {
            if (Enum.TryParse(Authentication.ToString(), true, out SqlAuthenticationMethod sqlAuthentication))
            {
                sbConnectionString.Authentication = sqlAuthentication;
            }
            else
            {
                throw new InvalidPropertyValueException("Authentication property has invalid value");
            }
        }

        /// <summary>
        /// Set the ApplicationIntent of the SqlConnectionStringBuilder. If the property is not
        /// supported or we have an invalid ApplicationIntent the value is ignored.
        /// </summary>
        private void SetApplicationIntent(SqlConnectionStringBuilder sbConnectionString)
        {
            if (Enum.TryParse(ApplicationIntent, true, out ApplicationIntent applicationIntent))
            {
                sbConnectionString.ApplicationIntent = applicationIntent;
            }
            else
            {
                Trace.TraceWarning("Unable to set ApplicationIntent property because it is not supported");
            }
        }

        /// <summary>
        /// Gets the InitialCatalog.
        /// </summary>
        internal string InitialCatalog
        {
            get
            {
                string initialCatalog = string.Empty;
                if (!string.IsNullOrEmpty(ConnectionString))
                {
                    SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder(ConnectionString);
                    initialCatalog = scsb.InitialCatalog;
                }

                return initialCatalog;
            }
        }

        internal bool BlockUpdates
        {
            get => m_BlockUpdates;
            set
            {
                m_BlockUpdates = value;
            }
        }

        /// <summary>
        /// Tells us whether ConnectionString needs to be reset in
        /// SqlConnectionObject property of ConnectionManager or not.
        /// </summary>
        protected bool ResetConnectionString
        {
            get => m_ResetConnectionString;
            set
            {
                m_ResetConnectionString = value;
            }
        }

        public override string ToString() => ConnectionString;
    }
}
