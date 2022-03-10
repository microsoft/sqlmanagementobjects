// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif


namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// SqlDirectConnection is a simple ConnectionInfoBase that wraps a SqlConnection object.
    /// </summary>
    public class SqlDirectConnection : ConnectionInfoBase
    {
        private string m_sbServerName;
        private SqlConnection m_sqlConnection;

        /// <summary>
        /// Constructs a new SqlDirectConnection with no SqlConnection
        /// </summary>
        public SqlDirectConnection() : base(ConnectionType.SqlConnection)
        {
        }

        /// <summary>
        /// Constructs a new SqlDirectConnection whose properties are calculated from the given SqlConnection
        /// </summary>
        /// <param name="sqlConnection"></param>
        public SqlDirectConnection(SqlConnection sqlConnection) : base(ConnectionType.SqlConnection)
        {
            m_sqlConnection = sqlConnection;
        }

        SqlDirectConnection(SqlDirectConnection conn) : base(ConnectionType.SqlConnection)
        {
            m_sqlConnection = conn.SqlConnection;
            m_sbServerName = conn.ServerName;
        }

        /// <summary>
        /// Returns the ServerName from the wrapped SqlConnection
        /// </summary>
        public string ServerName
        {
            get
            {
                if( null == m_sbServerName )
                    return String.Empty;
                else
                    return m_sbServerName;
            }

            set
            {
                //Call NetCoreHelpers StringCompare method to call the appropriate method for this framework.
                if (null == m_sbServerName || 0 != m_sbServerName.ToString().StringCompare( value, true, ConnectionInfoBase.DefaultCulture))
                {
                    m_sbServerName = value;
                    ServerVersion = null;
                    ConnectionParmsChanged();
                }
            }
        }

        /// <summary>
        /// Returns the wrapped SqlConnection
        /// </summary>
        public SqlConnection SqlConnection
        {
            get
            {
                return m_sqlConnection;
            }

            set
            {
                m_sqlConnection = value;
                ConnectionParmsChanged();
            }
        }

        /// <summary>
        /// Deep copy
        /// </summary>
        /// <returns></returns>
        public SqlDirectConnection Copy()
        {
            return new SqlDirectConnection(this);
        }

        protected override void ConnectionParmsChanged()
        {
        }
    }
}
