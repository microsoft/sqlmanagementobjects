// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.ComponentModel;
    using System.Data;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
    using System.Data.SqlTypes;
    using System.Diagnostics;
    using System.Text;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class SqlConnectionInfo : SqlOlapConnectionInfoBase
    {
        public static readonly NetworkProtocol DefaultNetworkProtocol = NetworkProtocol.NotSpecified;

        // AuthenticationMethod enum names must match SqlAuthenticationMethod enum names
        /// <summary>
        /// The Authentication Method used to log in
        /// </summary>
        public enum AuthenticationMethod
        {
            /// <summary>
            /// NotSpecified implies the real authentication type is inferred from other connection string parameters
            /// </summary>
            NotSpecified = 0,
            /// <summary>
            /// User id and password are used for SQL login authentication
            /// </summary>
            SqlPassword = 1,
            /// <summary>
            /// User id is an Azure AD principal
            /// </summary>
            ActiveDirectoryPassword = 2,
            /// <summary>
            /// The current AD or Kerberos principal credentials are used to connect
            /// </summary>
            ActiveDirectoryIntegrated = 3,
            //skipping 4 as that maps to Microsoft.SqlServer.Management.UI.ConnectionDlg.SqlServerType.ActiveDirectoryUniversalAuthenticationType (in SqlServerType.cs).
            //This was a bug in SSDT where we were using this enum to set the UIConnectionInfo.AuthenticationType (which is an int, so it stored the int value)
            //and when it went through UIConnectionInfoUtil.GetCoreConnectionInfo, it was set to "4" which made it go down the UniversalAuth code path
            /// <summary>
            /// Uses an interactive UI flow to acquire a token to authenticate. User id is optional.
            /// </summary>
            ActiveDirectoryInteractive = 5,
            /// <summary>
            /// Prompt the user to acquire a token from an external device
            /// </summary>
            ActiveDirectoryDeviceCodeFlow = 6,
            /// <summary>
            /// Use system assigned or user assigned managed identity to acquire a token.
            /// </summary>
            ActiveDirectoryManagedIdentity = 7,
            /// <summary>
            /// Alias for ActiveDirectoryManagedIdentity
            /// </summary>
            ActiveDirectoryMSI = ActiveDirectoryManagedIdentity,
            /// <summary>
            /// User id is the client id of an Azure service principal, and password is the client secret.
            /// </summary>
            ActiveDirectoryServicePrincipal = 8,
            /// <summary>
            /// Attempts multiple non-interactive authentication methods tried
            /// sequentially to acquire an access token. This method does not fallback to the
            /// Active Directory Interactive authentication method.
            /// </summary>
            ActiveDirectoryDefault = 9
        }

        private StringBuilder m_sbApplicationName = null;
        private StringBuilder m_sbWorkstationID = null;
        private NetworkProtocol m_eNetworkProtocol = DefaultNetworkProtocol;

        private Int32 m_PoolConnectionLifeTime = -1;
        private Int32 m_MaxPoolSize = -1;
        private Int32 m_MinPoolSize = -1;
        private Int32 m_PacketSize = -1;
        private bool shouldEncryptConnection = false;
        private string additionalParameters = null;
#if MICROSOFTDATA
        private string hostNameInCertificate = null;
#endif
        private bool trustServerCertificate = false;
        private AuthenticationMethod m_Authentication = AuthenticationMethod.NotSpecified;
        private string m_ApplicationIntent = null;


        [NonSerialized]
        private SqlBoolean m_Pooled = SqlBoolean.Null;

        /// <summary>
        /// Checks whether "Authentication" is supported in the runtime environment
        /// </summary>
        /// <returns></returns>
        public static bool IsAuthenticationKeywordSupported() => true;

        /// <summary>
        /// Retrieve the Authentication value from SqlConnectionStringBuilder and convert it to SqlConnectionInfo.AuthenticationMethod
        /// </summary>
        /// <param name="connectionStringBuilder"></param>
        /// <returns>SqlConnectionInfo.AuthenticationMethod</returns>
        public static AuthenticationMethod GetAuthenticationMethod(SqlConnectionStringBuilder connectionStringBuilder)
        {

            object value = connectionStringBuilder.Authentication;

            if (value == null)
            {
                return AuthenticationMethod.NotSpecified;
            }
            var strVal = value.ToString();
            if (Enum.TryParse<AuthenticationMethod>(strVal, out var val))
            {
                return val;
            }

            return AuthenticationMethod.NotSpecified;
        }

        // default constructor
        public SqlConnectionInfo() : base(ConnectionType.Sql)
        {
            // nothing
        }

        // special user friendly constructors
        public SqlConnectionInfo(string serverName) : base(serverName, ConnectionType.Sql) { }

        public SqlConnectionInfo(string serverName, string userName, string password) :
        base(serverName, userName, password, ConnectionType.Sql)
        { }

        //copy ctor
        public SqlConnectionInfo(SqlConnectionInfo conn) : base((SqlOlapConnectionInfoBase)conn)
        {
            m_sbApplicationName = conn.m_sbApplicationName;
            m_sbWorkstationID = conn.m_sbWorkstationID;
            m_eNetworkProtocol = conn.m_eNetworkProtocol;
            m_PacketSize = conn.m_PacketSize;
            shouldEncryptConnection = conn.shouldEncryptConnection;
            StrictEncryption = conn.StrictEncryption;
            additionalParameters = conn.additionalParameters;
            m_Authentication = conn.Authentication;
            m_ApplicationIntent = conn.ApplicationIntent;
            trustServerCertificate = conn.TrustServerCertificate;
            AccessToken = conn.AccessToken;
#if MICROSOFTDATA
            HostNameInCertificate = conn.HostNameInCertificate;
#endif
        }

        /// <summary>
        /// Initializes SqlConnectionInfo from ServerConnection object
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <param name="connectionType"></param>
        public SqlConnectionInfo(ServerConnection serverConnection, ConnectionType connectionType)
        : base(connectionType)
        {
            if (serverConnection.IsApplicationNameInitialized)
            {
                m_sbApplicationName = new StringBuilder(serverConnection.ApplicationName);
            }
            if (serverConnection.IsWorkstationIdInitialized)
            {
                m_sbWorkstationID = new StringBuilder(serverConnection.WorkstationId);
            }
            m_eNetworkProtocol = serverConnection.NetworkProtocol;
            m_PoolConnectionLifeTime = serverConnection.PooledConnectionLifetime;
            m_MaxPoolSize = serverConnection.MaxPoolSize;
            m_MinPoolSize = serverConnection.MinPoolSize;
            m_Pooled = !serverConnection.NonPooledConnection;
            ServerNameInternal = new StringBuilder(serverConnection.ServerInstance);
            if (serverConnection.IsLoginInitialized)
            {
                UserNameInternal = new StringBuilder(serverConnection.Login);
            }
            if (serverConnection.IsPasswordInitialized)
            {
                PasswordInternal = EncryptionUtility.EncryptString(serverConnection.Password);
            }
            IntegratedSecurityInternal = serverConnection.LoginSecure;
            if (serverConnection.IsDatabaseNameInitialized)
            {
                DatabaseNameInternal = new StringBuilder(serverConnection.DatabaseName);
            }
            ConnectionTimeoutInternal = serverConnection.ConnectTimeout;
            EncryptConnection = serverConnection.EncryptConnection;
            additionalParameters = serverConnection.AdditionalParameters;
            AccessToken = serverConnection.AccessToken;
            trustServerCertificate = serverConnection.TrustServerCertificate;
            StrictEncryption = serverConnection.StrictEncryption;
#if MICROSOFTDATA
            HostNameInCertificate = serverConnection.HostNameInCertificate;
#endif
        }

        public string ApplicationName
        {
            get => (null == m_sbApplicationName) ? string.Empty : m_sbApplicationName.ToString();
            set
            {
                //Call NetCoreHelpers StringCompare method to call the appropriate method for this framework.
                if (null == m_sbApplicationName || 0 != m_sbApplicationName.ToString().StringCompare(value, false, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbApplicationName = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public string WorkstationId
        {
            get => null == m_sbWorkstationID ? string.Empty : m_sbWorkstationID.ToString();
            set
            {
                //Call NetCoreHelpers StringCompare method to call the appropriate method for this framework.
                if (null == m_sbWorkstationID || 0 != m_sbWorkstationID.ToString().StringCompare(value, false, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbWorkstationID = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public NetworkProtocol ConnectionProtocol
        {
            get => m_eNetworkProtocol;
            set
            {
                if (value != m_eNetworkProtocol)
                {
                    m_eNetworkProtocol = value;
                    ConnectionParmsChanged();
                }
            }
        }

        /// <summary>
        /// return SqlConnectionInfo.AuthenticationMethod
        /// </summary>
        public AuthenticationMethod Authentication
        {
            get => m_Authentication;
            set
            {
                if (value == m_Authentication)
                {
                    return;
                }

                m_Authentication = value;

                //Simialr to UserId: Any time user changes Authentication to Active Directory Integrated we need to reset integrated security flag
                if (UseIntegratedSecurity && value == AuthenticationMethod.ActiveDirectoryIntegrated)
                {
                    UseIntegratedSecurity = false;
                }

                ConnectionParmsChanged();
            }
        }

        /// <summary>
        /// ApplicationIntent for the connection
        /// </summary>
        public string ApplicationIntent
        {
            get => m_ApplicationIntent;
            set
            {
                if (value == m_ApplicationIntent)
                {
                    return;
                }

                m_ApplicationIntent = value;

                ConnectionParmsChanged();
            }
        }

        /// <summary>
        /// return whether to trust server certificate
        /// </summary>
        public bool TrustServerCertificate
        {
            get => trustServerCertificate;
            set
            {
                if (value != trustServerCertificate)
                {
                    trustServerCertificate = value;
                    ConnectionParmsChanged();
                }
            }
        }

        /// <summary>
        /// The access token value to use for universal auth
        /// </summary>
        public IRenewableToken AccessToken { get; set; }

        string NetworkProtocolString
        {
            get
            {
                string strNetProtocol = String.Empty;
                switch (m_eNetworkProtocol)
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
        }

        // overrides
        [Browsable(false)]
        public override string ConnectionString
        {
            get
            {
                if (RebuildConnectionStringInternal)
                {
                    ConnectionSettings cs = new ConnectionSettings(this);
                    ConnectionStringInternal = EncryptionUtility.EncryptString(cs.ConnectionString);
                    RebuildConnectionStringInternal = false;
                }
                return EncryptionUtility.DecryptSecureString(ConnectionStringInternal);
            }
        }

        /// <summary>
        /// Deep copy
        /// </summary>
        /// <returns></returns>
        public SqlConnectionInfo Copy() => new SqlConnectionInfo(this);

        public override string ToString()
        {
            StringBuilder sbText = new StringBuilder(base.ToString());
            sbText.AppendFormat(", timeout = {0}, database = {1}, protocol = {2}, workstation = {3}, integrated security = {4}",
                                ConnectionTimeout, DatabaseName, ConnectionProtocol, WorkstationId, UseIntegratedSecurity);
            return sbText.ToString();
        }

        /// <summary>
        /// Returns a new IDbConnection implementation. Callers should use this object and release the reference to it
        /// in a short amount of time, as the associated access token may have a limited lifetime.
        /// </summary>
        /// <returns></returns>
        public override IDbConnection CreateConnectionObject()
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            if (AccessToken != null)
            {
                ConnectionInfoHelper.SetTokenOnConnection(conn, AccessToken.GetAccessToken());
            }

            return conn;
        }

        public int PoolConnectionLifeTime
        {
            get => m_PoolConnectionLifeTime;
            set => m_PoolConnectionLifeTime = value;
        }

        public int PacketSize
        {
            get => m_PacketSize;
            set => m_PacketSize = value;
        }

        public int MaxPoolSize
        {
            get => m_MaxPoolSize;
            set => m_MaxPoolSize = value;
        }

        public int MinPoolSize
        {
            get => m_MinPoolSize;
            set => m_MinPoolSize = value;
        }

        public SqlBoolean Pooled
        {
            get => m_Pooled;
            set => m_Pooled = value;
        }

        /// <summary>
        /// Whether to set Encrypt=true in the connection string
        /// </summary>
        public bool EncryptConnection
        {
            get => shouldEncryptConnection;
            set
            {
                shouldEncryptConnection = value;
                ConnectionParmsChanged();
            }
        }

        /// <summary>
        /// Whether to set Encrypt=Strict in the connection string. 
        /// If Strict is not supported by the current SqlClient, when true this value will set Encrypt=true
        /// </summary>
        public bool StrictEncryption { get; set; }

#if MICROSOFTDATA
        /// <summary>
        /// Sets host name provided in certificate to be used for certificate validation.
        /// </summary>
        public string HostNameInCertificate
        {
            get => hostNameInCertificate;
            set
            {
                hostNameInCertificate = value;
                ConnectionParmsChanged();
            }
        }
#endif

        public string AdditionalParameters
        {
            get => additionalParameters;
            set
            {
                additionalParameters = value;
                ConnectionParmsChanged();
            }
        }

        /// <summary>
        /// Checks whether "ApplicationIntent" is supported in the runtime environment
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// "ApplicationIntent" is not supported until .Net4.5</remarks>
        public static bool IsApplicationIntentKeywordSupported() => true;
    }
}
