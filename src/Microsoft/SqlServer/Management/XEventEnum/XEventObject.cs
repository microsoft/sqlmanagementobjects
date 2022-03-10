// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.XEvent
{
    using System;
    using System.Reflection;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    /// <summary>
    /// This is the Enumerator object for XEvnet object model. It derived from SqlObject, the
    /// base class for all enumerator in SFC enabled object model.Override the ResourceAssembly
    /// to provide the correct assembly that contains the resources.
    /// </summary>
    internal sealed class XEventObject : SqlObject, Microsoft.SqlServer.Management.Sdk.Sfc.ISupportVersions
    {
        /// <summary>
        /// Return the assebmly that contains the resources.
        /// </summary>
        public override Assembly ResourceAssembly
        {
            get
            {
                return Assembly.GetExecutingAssembly();
            }
        }

        /// <summary>
        /// Return the server version for the given connection.
        /// </summary>
        /// <param name="conn">connetion to the server we want to know the version</param>
        /// <returns>server version on the connection</returns>
        public ServerVersion GetServerVersion(Object conn)
        {
            return ExecuteSql.GetServerVersion(conn);
        }
    }
}
