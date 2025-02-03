// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Attribute to mark a test method with DatabaseEngineType values that it doesn't
    /// support. 
    /// </summary>
    /// <remarks>Note this means that by default a test will run against all servers
    /// regardless of their DatabaseEngineType</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class UnsupportedDatabaseEngineTypeAttribute : SqlUnsupportedDimensionAttribute
    {

        private readonly ISet<DatabaseEngineType> _unsupportedDatabaseEngineTypes;

        public UnsupportedDatabaseEngineTypeAttribute(params DatabaseEngineType[] unsupportedDatabaseEngineTypes)
        {
            _unsupportedDatabaseEngineTypes = new HashSet<DatabaseEngineType>(unsupportedDatabaseEngineTypes);
        }


        public override bool IsSupported(SMO.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            return _unsupportedDatabaseEngineTypes.Contains(serverDescriptor.DatabaseEngineType) == false;
        }

        /// <summary>
        /// Returns true if the server is supported, which in this case means that server.DatabaseEngineType is
        /// NOT one of the unsupported values. 
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server)
        {
            return _unsupportedDatabaseEngineTypes.Contains(server.DatabaseEngineType) == false;
        }
    }
}
