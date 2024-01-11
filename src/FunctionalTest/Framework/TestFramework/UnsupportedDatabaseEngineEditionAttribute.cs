// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Attribute to mark a test method with DatabaseEngineEdition values that it doesn't
    /// support. 
    /// </summary>
    /// <remarks>Note this means that by default a test will run against all servers
    /// regardless of their DatabaseEngineEdition</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class UnsupportedDatabaseEngineEditionAttribute : SqlUnsupportedDimensionAttribute
    {

        private readonly ISet<DatabaseEngineEdition> _unsupportedDatabaseEngineEditions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unsupportedDatabaseEngineEditions"></param>
        public UnsupportedDatabaseEngineEditionAttribute(params DatabaseEngineEdition[] unsupportedDatabaseEngineEditions)
        {
            _unsupportedDatabaseEngineEditions = new HashSet<DatabaseEngineEdition>(unsupportedDatabaseEngineEditions);
        }

        /// <summary>
        /// Bypass the server query if the descriptor has a valid edition
        /// </summary>
        /// <param name="server"></param>
        /// <param name="serverDescriptor"></param>
        /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            if (serverDescriptor.DatabaseEngineEdition == DatabaseEngineEdition.Unknown)
            {
                return IsSupported(server);
            }
            return !_unsupportedDatabaseEngineEditions.Contains(serverDescriptor.DatabaseEngineEdition);
        }

        /// <summary>
        /// Returns true if the server is supported, which in this case means that server.DatabaseEngineEdition is
        /// NOT one of the unsupported values. 
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server)
        {
            return _unsupportedDatabaseEngineEditions.Contains(server.DatabaseEngineEdition) == false;
        }
    }
}
