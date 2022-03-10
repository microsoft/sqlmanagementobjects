// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    [Flags]
    public enum SqlHostPlatforms
    {
        Windows,
        Linux
    }

    /// <summary>
    /// Attribute to mark a test method with HostPlatform values that it doesn't
    /// support. 
    /// </summary>
    /// <remarks>Note this means that by default a test will run against all servers
    /// regardless of their DatabaseEngineType</remarks>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class UnsupportedHostPlatformAttribute : SqlUnsupportedDimensionAttribute
    {
        private readonly ISet<string> _unsupportedHostPlatforms;

        public UnsupportedHostPlatformAttribute(params SqlHostPlatforms[] unsupportedHostPlatforms)
        {
            _unsupportedHostPlatforms = new HashSet<string>(unsupportedHostPlatforms.Select(p => p.ToString()));
        }

        /// <summary>
        /// Returns true if the server is supported, which in this case means that server.HostPlatform is
        /// NOT one of the unsupported values. 
        /// </summary>
        /// <param name="server"></param>
        /// <param name="serverDescriptor"></param>
        /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            return _unsupportedHostPlatforms.Contains(serverDescriptor.HostPlatform) == false;
        }

        /// <summary>
        /// Returns true if the server is supported, which in this case means that server.HostPlatform is
        /// NOT one of the unsupported values. 
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server)
        {
            return _unsupportedHostPlatforms.Contains(server.HostPlatform) == false;
        }
    }
}
