// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Represents a server connection, which can either be a traditional SQL connection or a Fabric workspace connector.
    /// </summary>
    public class ServerConnectionInfo
    {
        /// <summary>
        /// The friendly name of the server or workspace.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// The collection of SQL connection strings for traditional servers.
        /// This will be null for Fabric workspaces.
        /// </summary>
        public IEnumerable<SqlConnectionStringBuilder> ConnectionStrings { get; set; }

        /// <summary>
        /// Indicates whether this connection info is for a Fabric workspace.
        /// </summary>
        public bool IsFabricWorkspace;

        public TestDescriptor TestDescriptor { get; set; }
    }

}
