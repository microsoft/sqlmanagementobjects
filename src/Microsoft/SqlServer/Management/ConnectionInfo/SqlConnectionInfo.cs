// Copyright (c) Microsoft.
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

        /// <summary>
        /// The Authentication Method used to log in
        /// </summary>
        public enum AuthenticationMethod
        {
            NotSpecified = 0,
            SqlPassword = 1,
            ActiveDirectoryPassword = 2,
            ActiveDirectoryIntegrated =  3,
            //skipping 4 as that maps to Microsoft.SqlServer.Management.UI.ConnectionDlg.SqlServerType.ActiveDirectoryUniversalAuthenticationType (in SqlServerType.cs).
            //This was a bug in SSDT where we were using this enum to set the UIConnectionInfo.AuthenticationType (which is an int, so it stored the int value)
            //and when it went through UIConnectionInfoUtil.GetCoreConnectionInfo, it was set to "4" which made it go down the UniversalAuth code path
            ActiveDirectoryInteractive = 5
        }
		
        private StringBuilder m_sbApplicationName           = null;
        private StringBuilder m_sbWorkstationID         = null;
        private NetworkProtocol m_eNetworkProtocol      = DefaultNetworkProtocol;

        private Int32 m_PoolConnectionLifeTime = -1;
        private Int32 m_MaxPoolSize = -1;
        private Int32 m_MinPoolSize = -1;
        private Int32 m_PacketSize = -1;
        private bool shouldEncryptConnection = false;
        private string additionalParameters = null;
        private bool trustServerCertificate = false;
        private AuthenticationMethod m_Authentication = AuthenticationMethod.NotSpecified;
        private string m_ApplicationIntent = null;


        [NonSerialized]
            private SqlBoolean m_Pooled = SqlBoolean.Null;

        /// <summary>
        /// Checks whether "Authentication" is supported in the runtime environment
        /// </summary>
        /// <returns></returns>
        public static Boolean IsAuthenticationKeywordSupported()
        {
            return true;
        }

        /// <summary>
        /// Retrieve the Authentication value from SqlConnectionStringBuilder and convert it to SqlConnectionInfo.AuthenticationMethod
        /// </summary>
        /// <param name="connectionStringBuilder"></param>
        /// <returns>SqlConnectionInfo.AuthenticationMethod</returns>
        public static AuthenticationMethod GetAuthenticationMethod(SqlConnectionStringBuilder connectionStringBuilder)
        {
            if (!IsAuthenticationKeywordSupported())
            {
                return AuthenticationMethod.NotSpecified;
            }
            
            object value = connectionStringBuilder.Authentication;

            if (value == null)
            {
                return AuthenticationMethod.NotSpecified;
            }
            string strVal = value.ToString();
            // SqlConnectionInfo.AuthenticationMethod is a different object than SqlConnectionStringBuilder.SqlAuthenticationMethod.
            // However, we make SqlConnectionInfo.AuthenticationMethod to have same string value as SqlConnectionStringBuilder.SqlAuthenticationMethod.
            // And the mapping is one-to-one.
            if (strVal == AuthenticationMethod.ActiveDirectoryIntegrated.ToString())
            {
                return AuthenticationMethod.ActiveDirectoryIntegrated;
            }
            else if (strVal == AuthenticationMethod.ActiveDirectoryPassword.ToString())
            {
                return AuthenticationMethod.ActiveDirectoryPassword;
            }
            else if (strVal == AuthenticationMethod.SqlPassword.ToString())
            {
                return AuthenticationMethod.SqlPassword;
            }
            else if (strVal == AuthenticationMethod.ActiveDirectoryInteractive.ToString())
            {
                return AuthenticationMethod.ActiveDirectoryInteractive;
            }
            else if (strVal == AuthenticationMethod.NotSpecified.ToString())
            {
                return AuthenticationMethod.NotSpecified;
            }
            Trace.Assert(false, "Unknown Authentication Method: {0}", strVal);
            return AuthenticationMethod.NotSpecified;
        }

        // default constructor
        public SqlConnectionInfo() : base(ConnectionType.Sql)
        {
            // nothing
        }

        // special user friendly constructors
        public SqlConnectionInfo( string serverName ) : base( serverName, ConnectionType.Sql){}

        public SqlConnectionInfo( string serverName, string userName, string password ) :
        base(serverName, userName, password, ConnectionType.Sql) {}

        //copy ctor
        public SqlConnectionInfo(SqlConnectionInfo conn) : base((SqlOlapConnectionInfoBase)conn)
        {
            m_sbApplicationName         = conn.m_sbApplicationName;
            m_sbWorkstationID           = conn.m_sbWorkstationID;
            m_eNetworkProtocol          = conn.m_eNetworkProtocol;
            m_PacketSize                = conn.m_PacketSize;
            this.shouldEncryptConnection = conn.shouldEncryptConnection;
            this.additionalParameters   = conn.additionalParameters;
            this.m_Authentication       = conn.Authentication;
            this.m_ApplicationIntent    = conn.ApplicationIntent;
            this.trustServerCertificate = conn.TrustServerCertificate;
            this.AccessToken = conn.AccessToken;
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
                this.m_sbApplicationName = new StringBuilder(serverConnection.ApplicationName);
            }
            if (serverConnection.IsWorkstationIdInitialized)
            {
                this.m_sbWorkstationID = new StringBuilder(serverConnection.WorkstationId);
            }
            this.m_eNetworkProtocol = serverConnection.NetworkProtocol;
            this.m_PoolConnectionLifeTime = serverConnection.PooledConnectionLifetime;
            this.m_MaxPoolSize = serverConnection.MaxPoolSize;
            this.m_MinPoolSize = serverConnection.MinPoolSize;
            this.m_Pooled = !serverConnection.NonPooledConnection;
            this.ServerNameInternal = new StringBuilder(serverConnection.ServerInstance);
            if (serverConnection.IsLoginInitialized)
            {
                this.UserNameInternal = new StringBuilder(serverConnection.Login);
            }
            if (serverConnection.IsPasswordInitialized)
            {
                this.PasswordInternal = EncryptionUtility.EncryptString(serverConnection.Password);
            }
            this.IntegratedSecurityInternal = serverConnection.LoginSecure;
            if (serverConnection.IsDatabaseNameInitialized)
            {
                this.DatabaseNameInternal = new StringBuilder(serverConnection.DatabaseName);
            }
            this.ConnectionTimeoutInternal = serverConnection.ConnectTimeout;
            this.EncryptConnection = serverConnection.EncryptConnection;
            this.additionalParameters = serverConnection.AdditionalParameters;
            this.AccessToken = serverConnection.AccessToken;
        }

        public string ApplicationName
        {
            get
            {
                if ( null == m_sbApplicationName )
                    return String.Empty;
                else
                    return m_sbApplicationName.ToString();
            }

            set
            {
                //Call NetCoreHelpers StringCompare method to call the appropriate method for this framework.
                if (null == m_sbApplicationName || 0 != m_sbApplicationName.ToString().StringCompare( value, false, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbApplicationName = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public string WorkstationId
        {
            get
            {
                if ( null == m_sbWorkstationID )
                    return String.Empty;
                else
                    return m_sbWorkstationID.ToString();
            }

            set
            {
                //Call NetCoreHelpers StringCompare method to call the appropriate method for this framework.
                if (null == m_sbWorkstationID || 0 != m_sbWorkstationID.ToString().StringCompare( value, false, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbWorkstationID = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public NetworkProtocol ConnectionProtocol
        {
            get
            {
                return m_eNetworkProtocol;
            }

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
            get
            {
               return m_Authentication;
            }

            set
            {
                if (value == m_Authentication)
                {
                    return;
                }

                m_Authentication = value;

                //Simialr to UserId: Any time user changes Authentication to Active Directory Integrated we need to reset integrated security flag
                if(UseIntegratedSecurity && value == AuthenticationMethod.ActiveDirectoryIntegrated)
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
            get
            {
                return m_ApplicationIntent;
            }

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
            get
            {
                return trustServerCertificate;
            }

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
        public IRenewableToken AccessToken
        {
            get;
            set;
        }

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
                if ( RebuildConnectionStringInternal )
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
        public SqlConnectionInfo Copy()
        {
            return new SqlConnectionInfo(this);
        }

        public override string ToString()
        {
            StringBuilder sbText = new StringBuilder( base.ToString() );
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
            if (this.AccessToken != null)
            {
                ConnectionInfoHelper.SetTokenOnConnection(conn, this.AccessToken.GetAccessToken());
            }

            return conn;
        }

        public Int32 PoolConnectionLifeTime
        {
            get
            {
                return m_PoolConnectionLifeTime;
            }
            set
            {
                m_PoolConnectionLifeTime = value;
            }
        }

        public Int32 PacketSize
        {
            get
            {
                return m_PacketSize;
            }
            set
            {
                m_PacketSize = value;
            }
        }

        public Int32 MaxPoolSize
        {
            get
            {
                return m_MaxPoolSize;
            }
            set
            {
                m_MaxPoolSize = value;
            }
        }

        public Int32 MinPoolSize
        {
            get
            {
                return m_MinPoolSize;
            }
            set
            {
                m_MinPoolSize = value;
            }
        }

        public SqlBoolean Pooled
        {
            get
            {
                return m_Pooled;
            }
            set
            {
                m_Pooled = value;
            }
        }

        public bool EncryptConnection
        {
            get
            {
                return this.shouldEncryptConnection;
            }

            set
            {
                this.shouldEncryptConnection = value;
            }
        }

        public String AdditionalParameters
        {
            get
            {
                return this.additionalParameters;
            }
            set
            {
                this.additionalParameters = value;

                //Recreate the Connection string since some new parameters are added here
                RebuildConnectionStringInternal = true;
            }
        }

        /// <summary>
        /// Checks whether "ApplicationIntent" is supported in the runtime environment
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// "ApplicationIntent" is not supported until .Net4.5</remarks>
        public static Boolean IsApplicationIntentKeywordSupported()
        {
            return true;
        }
    }
}
