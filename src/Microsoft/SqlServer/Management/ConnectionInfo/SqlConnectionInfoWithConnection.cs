// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Server;

namespace Microsoft.SqlServer.Management.Common
{
    [Serializable]
	public sealed class SqlConnectionInfoWithConnection : SqlConnectionInfo, IDisposable, IRestrictedAccess
    {
       [NonSerialized()]
        private ServerConnection serverConnection = null;

        public SqlConnectionInfoWithConnection()
            : base()
        {
        }

        public void Dispose()
        {
            if (this.closeConnectionOnDispose && this.serverConnection != null)
            {
                this.serverConnection.Disconnect();
                OnConnectionClosed(EventArgs.Empty);
            }
            this.serverConnection = null;
        }

        public SqlConnectionInfoWithConnection(string serverName)
            : base(serverName)
        {
        }

        public SqlConnectionInfoWithConnection(string serverName, string userName, string password)
            : base(serverName, userName, password)
        {
        }

        public SqlConnectionInfoWithConnection(SqlConnection sqlConnection)
            : base(sqlConnection.DataSource)
        {
            this.serverConnection = new ServerConnection(sqlConnection);
            this.closeConnectionOnDispose = false;
        }

        SqlConnectionInfoWithConnection(SqlConnectionInfoWithConnection conn)
            : base(conn)
        {
            this.singleConnection = conn.singleConnection;
            if (this.singleConnection == true)
            {
                this.serverConnection = conn.serverConnection;
                this.closeConnectionOnDispose = false;
            }
        }

        [NonSerialized()]
        bool closeConnectionOnDispose = false;

        [System.ComponentModel.Browsable(false)]
        public ServerConnection ServerConnection
        {
            get
            {
                if (this.serverConnection == null)
                {
                    this.serverConnection = new ServerConnection(this);
                    this.serverConnection.NonPooledConnection = true;
                    this.serverConnection.AutoDisconnectMode = AutoDisconnectMode.NoAutoDisconnect;
                    this.closeConnectionOnDispose = true;
                }

                var accessToken = this.AccessToken;
                if (accessToken != null)
                {
                    this.serverConnection.AccessToken = accessToken;
                }

                return this.serverConnection;
            }

            set
            {
                this.serverConnection = value;
                this.closeConnectionOnDispose = false;
                ConnectionParmsChanged();
            }
        }

        /// <summary>
        /// Deep copy
        /// </summary>
        /// <returns></returns>
        public new SqlConnectionInfoWithConnection Copy()
        {
            return new SqlConnectionInfoWithConnection(this);
        }

        protected override void ConnectionParmsChanged()
        {
        }

        [NonSerialized()]
        private bool singleConnection = false;
        public bool SingleConnection
        {
            get
            {
                return this.singleConnection;
            }
            set
            {
                this.singleConnection = value;
            }
        }

        private EventHandler connectionClosedHandler;
        public event EventHandler ConnectionClosed
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                    return;
                connectionClosedHandler += value;
            }
            remove
            {
                connectionClosedHandler -= value;
            }
        }
        private void OnConnectionClosed(EventArgs args)
        {
            if (connectionClosedHandler != null)
            {
                connectionClosedHandler(this, args);
            }
        }
    }
}
