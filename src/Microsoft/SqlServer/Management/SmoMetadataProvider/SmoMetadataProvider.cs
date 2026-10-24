// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Diagnostics;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    /// <summary>
    /// Represents an <see cref="IMetadataProvider"/> object that uses SMO to retrieve
    /// server metadata objects.
    /// </summary>
    public abstract partial class SmoMetadataProvider : MetadataProviderBase
    {
        private readonly Smo.Server m_smoServer;
        private readonly Server m_server;

        private SmoMetadataProvider(Smo.Server server, bool isConnected)
            : base(SmoBuiltInFunctionLookup.Instance, SmoCollationLookup.Instance,
            SmoSystemDataTypeLookup.Instance, SmoMetadataFactory.Instance)
        {
            Debug.Assert(server != null, "SmoMetadataProvider Assert", "server != null");

            this.m_smoServer = server;
            this.m_server = new Server(this.m_smoServer, isConnected);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Microsoft.SqlServer.Management.SqlParser.MetadataProvider"/> 
        /// that uses the specified SMO connection to retrieve server metadata objects over the SMO.
        /// </summary>
        /// <param name="connection">A <see cref="Microsoft.SqlServer.Management.Common.ServerConnection"/> object that
        /// is used to retrieve server metadata objects.</param>
        /// <returns>A <see cref="T:Microsoft.SqlServer.Management.SmoMetaDataProvider.SmoMetadataProvider"/> object that 
        /// uses the specified connection to retrive server metadata.</returns>
        public static SmoMetadataProvider CreateConnectedProvider(ServerConnection connection)
        {
            return ConnectedSmoMetadataProvider.Create(connection);
        }
        
        /// <summary>
        /// Creates a new instance of the <see cref="Microsoft.SqlServer.Management.SqlParser.MetadataProvider"/> 
        /// that uses the specified connection to retrieve server metadata objects over the SMO.
        /// </summary>
        /// <param name="connection">A <see cref="Microsoft.SqlServer.Management.Common.ServerConnection"/> object that
        /// is used to retrieve server metadata objects.</param>
        /// <param name="refreshDbListMillisecond">
        /// An <see cref="System.Int32"/> value that specifies the time interval, in milliseconds, between database list refreshes.
        /// The database refresh updates the list of accessible databases, basing this evaluation on whether the database is
        /// on or offline, and whether the database is in single or multi-user mode.
        /// </param>
        /// <returns>A <see cref="T:Microsoft.SqlServer.Management.SmoMetaDataProvider.SmoMetadataProvider"/> object that 
        /// uses the specified connection to retrive server metadata.</returns>
        public static SmoMetadataProvider CreateConnectedProvider(ServerConnection connection, int refreshDbListMillisecond)
        {
            return ConnectedSmoMetadataProvider.Create(connection, refreshDbListMillisecond);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Microsoft.SqlServer.Management.SqlParser.MetadataProvider"/> 
        /// that uses the specified SMO server object to retrieve metadata objects.
        /// </summary>
        /// <param name="server">A SMO <see cref="Microsoft.SqlServer.Management.Smo.Server"/> object that is used to retrieve 
        /// metadata objects.</param>
        /// <returns>A <see cref="T:Microsoft.SqlServer.Management.SmoMetaDataProvider.SmoMetadataProvider"/> object that 
        /// uses the specified connection to retrive server metadata.</returns>
        public static SmoMetadataProvider CreateDisconnectedProvider(Smo.Server server)
        {
            return DisconnectedSmoMetadataProvider.Create(server);
        }

        /// <summary>
        /// Gets an <see cref="IServer"/> object which allows for accessing server-side
        /// metadata objects such as databases, schemas, and tables.
        /// </summary>
        public override IServer Server
        {
            get { return this.m_server; }
        }

        /// <summary>
        /// Gets a <see cref="Smo.Server"/> object associated with the metadata provider.
        /// </summary>
        public Smo.Server SmoServer
        {
            get { return this.m_smoServer; }
        }
    }
}
