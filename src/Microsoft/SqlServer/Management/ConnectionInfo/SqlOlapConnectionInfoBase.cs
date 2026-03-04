// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Xml.Serialization;
using System.ComponentModel;
using System.Text;
using System.Security;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Common
{
    [System.Runtime.InteropServices.ComVisible(false)]
    [Serializable]
    [SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors")]
    public abstract class SqlOlapConnectionInfoBase : ConnectionInfoBase
    {
        private StringBuilder m_sbServerName                = null;
        protected StringBuilder ServerNameInternal
        {
            get { return m_sbServerName; }
            set { m_sbServerName = value; }
        }

        [NonSerialized]
        private SecureString m_sbConnectionString = null;
        protected SecureString ConnectionStringInternal
        {
            get { return m_sbConnectionString; }
            set
            {
                if (value != null)
                {
                     m_sbConnectionString = value.Copy();
                }
                else
                {
                    m_sbConnectionString = null;
                }
            }
        }

        private bool m_fRebuildConnectionString = true;
        protected bool RebuildConnectionStringInternal
        {
            get { return (m_fRebuildConnectionString || (m_sbConnectionString == null)); }
            set { m_fRebuildConnectionString = value; }
        }

        private StringBuilder m_sbUserName              = null;
        internal StringBuilder UserNameInternal
        {
            get { return m_sbUserName; }
            set { m_sbUserName = value; }
        }

        [XmlIgnore()] //don't persist it
        [NonSerialized]
        private SecureString m_password = null;
        internal SecureString PasswordInternal
        {
            get
            {
                SecureString result = (m_password == null) ? new SecureString() : m_password.Copy();
                return result;
            }
            set
            {
                if (value != null)
                {
                     m_password = value.Copy();
                }
                else
                {
                    m_password = null;
                }
            }
        }

        private bool m_fIntegratedSecurity              = true;
        protected bool IntegratedSecurityInternal
        {
            get { return m_fIntegratedSecurity; }
            set { m_fIntegratedSecurity = value; }
        }

        private StringBuilder m_sbDatabaseName          = null;
        protected StringBuilder DatabaseNameInternal
        {
            get { return m_sbDatabaseName; }
            set { m_sbDatabaseName = value; }
        }


        protected const int NoTimeOut = -1;
        public static readonly int DefaultConnTimeout  = NoTimeOut;
        public static readonly int DefaultQueryTimeout = NoTimeOut;

        private int m_iConnectionTimeout                    = DefaultConnTimeout;
        protected int ConnectionTimeoutInternal
        {
            get { return m_iConnectionTimeout; }
            set { m_iConnectionTimeout = value; }
        }

        private int m_iQueryTimeout                     = DefaultQueryTimeout;
        protected int QueryTimeoutInternal
        {
            get { return m_iQueryTimeout; }
            set { m_iQueryTimeout = value; }
        }


        protected SqlOlapConnectionInfoBase()
        {
            throw new InvalidOperationException(StringConnectionInfo.ClassDefaulConstructorCannotBeUsed("SqlOlapConnectionInfoBase"));
        }

        // need to know connection type: SQL, OLAP
        protected SqlOlapConnectionInfoBase(ConnectionType serverType) : base(serverType)
        {
        }


        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected SqlOlapConnectionInfoBase(string serverName, ConnectionType serverType)
            : base(serverType)
        {
            ServerName = serverName;
        }



        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SqlOlapConnectionInfoBase(string serverName, string userName, string password, ConnectionType serverType)
            : base(serverType)
        {
            ServerName = serverName;
            UseIntegratedSecurity = false;
            UserName   = userName;
            Password   = password;
        }

        // copy ctor
        protected SqlOlapConnectionInfoBase(SqlOlapConnectionInfoBase conn) : base((ConnectionInfoBase)conn)
        {
            m_sbServerName              = conn.m_sbServerName;
            m_sbConnectionString        = conn.m_sbConnectionString;
            m_fRebuildConnectionString  = conn.m_fRebuildConnectionString;
            m_sbUserName                = conn.m_sbUserName;
            m_password                  = (conn.m_password != null) ? conn.m_password.Copy() : null;
            m_fIntegratedSecurity       = conn.m_fIntegratedSecurity;
            m_sbDatabaseName            = conn.m_sbDatabaseName;
            m_iConnectionTimeout        = conn.m_iConnectionTimeout;
            m_iQueryTimeout             = conn.m_iQueryTimeout;
        }

        // public properties
        public string ServerName
        {
            get
            {
                if( null == m_sbServerName )
                    return String.Empty;
                else
                    return m_sbServerName.ToString();
            }

            set
            {
                //Call NetCoreHelpers StringCompare method to call the appropriate method for this framework.
                if (null == m_sbServerName || 0 != m_sbServerName.ToString().StringCompare(value, true, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbServerName = new StringBuilder(value);
                    ServerVersion = null;
                    ConnectionParmsChanged();
                }
            }
        }

        [XmlIgnore()] //don't persist it
        public string UserName
        {
            get
            {
                if( null == m_sbUserName )
                    return String.Empty;
                else
                    return m_sbUserName.ToString();
            }

            set
            {
                // track parameter changes with this flag
                bool fParmsChanged = false;

                if( m_fIntegratedSecurity )
                {
                    // Any time user changes UserName we need to reset integrated security flag
                    m_fIntegratedSecurity = false;

                    fParmsChanged = true;
                }

                 //Call NetCoreHelpers method to call the appropriate method for this framework.
                if (null == m_sbUserName || 0 != m_sbUserName.ToString().StringCompare( value, false, ConnectionInfoBase.DefaultCulture))
                {
                    // new user name
                    m_sbUserName = new StringBuilder(value);

                    fParmsChanged = true;
                }

                if( fParmsChanged )
                {
                    ConnectionParmsChanged();
                }
            }
        }

        [
        XmlIgnore(),//don't persist it
        Browsable(false)
        ]
        public string Password
        {
            get
            {
                string result = (this.m_password == null) ? String.Empty : EncryptionUtility.DecryptSecureString(this.m_password);
                return result;
            }

            set
            {
                // track parameter changes with this flag
                bool fParmsChanged = false;

                // if password was set to null
                if ((value == null) || (value.Length == 0))
                {
                    // and the current password isn't empty
                    if (this.m_password != null)
                    {
                        // set password to null
                        this.m_password = null;
                        fParmsChanged   = true;
                    }

                    return;
                }
                else if (m_fIntegratedSecurity)
                {
                    // password being set to non-empty string, so we aren't using integrated security any more
                    m_fIntegratedSecurity   = false;
                    fParmsChanged           = true;
                }

                // if the password is different, change the password
                string oldPassword = (this.m_password == null) ? String.Empty : EncryptionUtility.DecryptSecureString(this.m_password);

                if(oldPassword != value)
                {
                    m_password      = EncryptionUtility.EncryptString(value);
                    fParmsChanged   = true;
                }

                oldPassword = null;

                if( fParmsChanged )
                {
                    ConnectionParmsChanged();
                }
            }
        }

        [
            XmlIgnore(),//don't persist it
            Browsable(false)
        ]
        public SecureString SecurePassword
        {
            get
            {
                return (m_password == null) ? new SecureString() : m_password.Copy();
            }

            set
            {
               // track parameter changes with this flag
                bool fParmsChanged = false;

                // if password was set to null
                if (value == null)
                {
                    // and the current password isn't empty
                    if (this.m_password != null)
                    {
                        // set the new password to null
                        this.m_password = null;
                        fParmsChanged   = true;
                    }

                    return;
                }
                else if (m_fIntegratedSecurity)
                {
                    // password being set to non-empty string, so we aren't using integrated security any more
                    m_fIntegratedSecurity   = false;
                    fParmsChanged           = true;
                }

                // if the password is different, change the password
                string  oldPassword     = (this.m_password == null) ? String.Empty : EncryptionUtility.DecryptSecureString(this.m_password);
                string  newPassword     = EncryptionUtility.DecryptSecureString(value);
                bool    passwordChanged = (oldPassword != newPassword);
                oldPassword = null;
                newPassword = null;

                if(passwordChanged)
                {
                    m_password      = value.Copy();
                    fParmsChanged   = true;
                }

                if( fParmsChanged )
                {
                    ConnectionParmsChanged();
                }
            }
        }

        public bool UseIntegratedSecurity
        {
            get
            {
                return m_fIntegratedSecurity;
            }

            set
            {
                if (m_fIntegratedSecurity != value)
                {
                    m_fIntegratedSecurity = value;
                    ConnectionParmsChanged();
                }
            }
        }

        public string DatabaseName
        {
            get
            {
                if( null == m_sbDatabaseName )
                    return String.Empty;
                else
                    return m_sbDatabaseName.ToString();
            }

            set
            {
                //Call NetCoreHelpers method to call the appropriate method for this framework.
                if (null == m_sbDatabaseName || 0 != m_sbDatabaseName.ToString().StringCompare(value, false, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbDatabaseName = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                return m_iConnectionTimeout;
            }

            set
            {
                if (value != m_iConnectionTimeout)
                {
                    m_iConnectionTimeout = value;
                    ConnectionParmsChanged();
                }
            }
        }

        public int QueryTimeout
        {
            get{ return m_iQueryTimeout; }
            set{ m_iQueryTimeout = value; }
        }

        // abstracts
        [Browsable(false)]
        public abstract string ConnectionString{ get; }

        public abstract IDbConnection CreateConnectionObject();

        // overrides
        protected override void ConnectionParmsChanged()
        {
            m_fRebuildConnectionString = true;
        }

        public override string ToString()
        {
            StringBuilder sbText = new StringBuilder( base.ToString() );
            sbText.AppendFormat(", server name = {0}", ServerName);
            return sbText.ToString();
        }
    }
}
