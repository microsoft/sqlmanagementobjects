// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This class is a connection to a Sql Server.
    /// </summary>
    public class SqlStoreConnection : SfcConnection
    {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sqlConnection"></param>
		public SqlStoreConnection(SqlConnection sqlConnection)
		{
            sqlServerConnection = new Microsoft.SqlServer.Management.Common.ServerConnection(sqlConnection);
        }

        private SqlStoreConnection(Microsoft.SqlServer.Management.Common.ServerConnection connection)
        {
            sqlServerConnection = connection;
        }
        
        /// Temporary function needed as long as the sql enumerator is
        /// unaware of the SqlStoreConnection type
        public override object ToEnumeratorObject()
        {
            return this.ServerConnection;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public override bool Equals(SfcConnection conn)
        {
            SqlStoreConnection sqlConn = conn as SqlStoreConnection;
            if (sqlConn == null)
            {
                return false;
            }

            return this.ToString() == sqlConn.ToString();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Connect()
        {
            if (!this.IsOpen)
            {
                sqlServerConnection.Connect();
            }
            return this.IsOpen;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Disconnect()
        {
            sqlServerConnection.Disconnect();
            return !this.IsOpen;
        }

        /// <summary>
        /// Performs a deep copy
        /// </summary>
        public override ISfcConnection Copy()
        {
            return new SqlStoreConnection(this.ServerConnection.Copy());
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override bool IsOpen
        {
            get
            {
                return sqlServerConnection.IsOpen;
            }
        }

        public override string ServerInstance
        {
            get
            {
                return sqlServerConnection.ServerInstance;
            }
            set
            {
                sqlServerConnection.ServerInstance = value;
            }
        }

        public override Version ServerVersion
        {
            get
            {
                return ((ISfcConnection)sqlServerConnection).ServerVersion;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override ServerType ConnectionType
        {
            get
            {
                return ServerType.DatabaseEngine;
            }
        }

        public override int ConnectTimeout
        {
            get
            {
                return sqlServerConnection.ConnectTimeout;
            }
            set
            {
                sqlServerConnection.ConnectTimeout = value;
            }
        }
        
        public override int StatementTimeout
        {
            get
            {
                return sqlServerConnection.StatementTimeout;
            }
            set
            {
                sqlServerConnection.StatementTimeout = value;
            }
        }
        
        public Microsoft.SqlServer.Management.Common.ServerConnection ServerConnection
        {
            get
            {
                return this.sqlServerConnection;
            }
        }
        private Microsoft.SqlServer.Management.Common.ServerConnection sqlServerConnection = null;
    }
}
