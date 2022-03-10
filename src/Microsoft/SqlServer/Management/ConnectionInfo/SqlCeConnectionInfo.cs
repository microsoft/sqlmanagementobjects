// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    [Serializable]
    public class SqlCeConnectionInfo : SqlOlapConnectionInfoBase
    {
        private int m_MaxDatabaseSize = -1;
        private int m_DefaultLockEscalation = -1;
        private IDbConnection connection;

        // Default constructor
        //
        public SqlCeConnectionInfo() : base(ConnectionType.SqlCE)
        {
            // nothing
        }

        public SqlCeConnectionInfo(IDbConnection connection) : this(connection.Database, string.Empty)
        {
            this.connection = connection;
        }

        // Special user friendly constructors
        //
        public SqlCeConnectionInfo(string connStr) : base(ConnectionType.SqlCE)
        {
            this.ConnectionStringInternal = EncryptionUtility.EncryptString(connStr);
        }


        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SqlCeConnectionInfo(string database, string password) : base(ConnectionType.SqlCE)
        {
            string tmp = string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append("Data Source = \"");
            sb.Append(EscapeString(database));
            sb.Append("\"; Password=\"");
            sb.Append(EscapeString(password));
            sb.Append("\";");

            this.ServerName = database;
            this.Password = password;

            // This is needed in order to render showplan using Yukon XML schema
            // We are essentially saying that SQLCE is 9.0 compatible here
            //
            this.ServerVersion = new ServerVersion(9, 0);
            this.ConnectionStringInternal = EncryptionUtility.EncryptString(sb.ToString());
            sb = null;
        }


        public SqlCeConnectionInfo(SqlCeConnectionInfo conn) : base((SqlOlapConnectionInfoBase)conn)
        {
        }

        [Browsable(false)]
        public override string ConnectionString
        {
            get
            {
                if ( RebuildConnectionStringInternal )
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat(
                                   "Data Source = \"{0}\"; Password=\"{1}\";",
                                   EscapeString(ServerName), 
                                   EscapeString(Password));

                    if (-1 != ConnectionTimeout)
                    {
                        sb.AppendFormat(
                                       "Timeout = \"{0}\";",
                                       ConnectionTimeout);
                    }

                    if (-1 != this.m_MaxDatabaseSize)
                    {
                        sb.AppendFormat(
                                       "Max Database Size = \"{0}\";",
                                       m_MaxDatabaseSize);
                    }

                    if (-1 != this.m_DefaultLockEscalation)
                    {
                        sb.AppendFormat(
                                       "Default Lock Escalation = \"{0}\";",
                                       m_DefaultLockEscalation);
                    }

                    ConnectionStringInternal = EncryptionUtility.EncryptString(sb.ToString());
                    sb = null;
                    RebuildConnectionStringInternal = false;
                }
                return EncryptionUtility.DecryptSecureString(ConnectionStringInternal); 
            }
        }

        public IDbConnection Connection
        {
            get
            {
                return this.connection;
            }

            set
            {
                this.connection = value;
            }
        }

        public Int32 MaxDatabaseSize
        {
            get
            {
                return m_MaxDatabaseSize;
            }
            set
            {
                if (value != m_MaxDatabaseSize)
                {
                    m_MaxDatabaseSize = value;
                    ConnectionParmsChanged();
                }
            }
        }

        public Int32 DefaultLockEscalation
        {
            get
            {
                return m_DefaultLockEscalation;
            }
            set
            {
                if (value != m_DefaultLockEscalation)
                {
                    m_DefaultLockEscalation = value;
                    ConnectionParmsChanged();
                }
            }
        }

        private string EscapeString(string s)
        {
            s = s.Replace("\"", "\"\"");
            return s;
        }

        /// <summary>
        /// Deep copy
        /// </summary>
        /// <returns></returns>
        public SqlCeConnectionInfo Copy() 
        {
            SqlCeConnectionInfo ci = new SqlCeConnectionInfo(this);            
            ci.MaxDatabaseSize = MaxDatabaseSize;
            ci.DefaultLockEscalation = DefaultLockEscalation;

            return ci;
        }

        public override IDbConnection CreateConnectionObject()
        {
            throw new NotImplementedException();
        }
    }
}
