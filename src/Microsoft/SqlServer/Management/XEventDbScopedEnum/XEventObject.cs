// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.XEventDbScoped
{
    /// <summary>
    /// This is the Enumerator object for XEvnet object model. It derived from SqlObject, the
    /// base class for all enumerator in SFC enabled object model.Override the ResourceAssembly
    /// to provide the correct assembly that contains the resources.
    /// </summary>
    internal sealed class XEventObject : SqlObject, ISupportVersions, ISupportDatabaseEngineTypes
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

        /// <summary>
        /// Return the databse engine type for the given connection.
        /// </summary>
        /// <param name="conn">connetion to the server we want to know the type</param>
        /// <returns>engine type of the server on the connection</returns>
        public DatabaseEngineType GetDatabaseEngineType(Object conn)
        {
            return ExecuteSql.GetDatabaseEngineType(conn);
        }
    }
}
