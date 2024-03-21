﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Sql provider for the ServerXEStore.
    /// </summary>    
    internal class ServerXEStoreProvider : IXEStoreProvider
    {
        private BaseXEStore store = null;
        private ServerConnection serverConnection;

        public ServerXEStoreProvider(BaseXEStore store)
        {
            this.store = store;
        }

        /// <summary>
        /// Gets an execution engine associated with Store's connection.
        /// </summary>
        /// <returns>Execution engine.</returns>
        public ISfcExecutionEngine GetExecutionEngine()
        {
            return new SfcTSqlExecutionEngine(this.ServerConnection);
        }

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting either a single serial query or multiple simultaneously open queries as requested.
        /// </summary>
        /// <param name="mode">Query mode.</param>
        /// <returns>The connection to use, or null to use Cache mode. Cache mode avoids connection and open data reader issues.</returns>
        public ISfcConnection GetConnection(SfcObjectQueryMode mode)
        {
            // TODO:: Take into account Single User mode when honoring MultipleActiveQueries mode       

            switch (mode)
            {
                case SfcObjectQueryMode.SingleActiveQuery:
                    return this.ServerConnection;

                case SfcObjectQueryMode.MultipleActiveQueries:
                    if (this.ServerConnection.MultipleActiveResultSets)
                    {
                        return this.ServerConnection;
                    }

                    // If we are in single user mode, we must throw now so we don't hide the problem until the next query is attempted.
                    // Note: PolicyStore needs to implement a property much like Smo.Server.Information.IsSingleUser boolean.
                    // In v1, Sfc ObjectQuery will internally cache the data reader anyhow if we throw or return null.
                    ////if (this.IsSingleUser)
                    ////{
                    ////    throw new SfcQueryConnectionUnavailableException();
                    ////}

                    // TODO:: Return a clone of our connection, since it is telling us that our connection is going to probably be "in use" shortly.
                    // Fallback on cached mode for now by returning null.
                    return null;

                default:
                    // Indicate we don't know what to do here, and let the caller maybe cache things for us (like OQ will do)
                    return null;
            }
        }

        /// <summary>
        /// Gets the name of the domain instance.
        /// </summary>
        /// <value>The name of the domain instance.</value>        
        public string DomainInstanceName
        {
            get
            {
                return this.ServerConnection.TrueName;
            }
        }

        /// <summary>
        ///  Gets a comparer for the child collections.
        /// </summary>
        /// <returns>Requested comparer.</returns>
        public IComparer<string> GetComparer()
        {
            return new ServerComparer(this.ServerConnection);
        }

        /// <summary>
        /// Gets the underlying ServerConnection
        /// </summary>
        private ServerConnection ServerConnection
        {
            get
            {
                if (this.serverConnection == null)
                {
                    // XEStore accepts SqlStoreConnection in its Constructor.
                    // However Powershell provider sets it to ServerConnection.
                    SqlStoreConnection connection = this.store.OriginalConnection as SqlStoreConnection;
                    if (connection != null)
                    {
                        this.serverConnection = connection.ServerConnection;
                    }
                    else
                    {
                        this.serverConnection = (ServerConnection)this.store.OriginalConnection;
                    }
                }                

                return this.serverConnection;
            }
        }
    }
}