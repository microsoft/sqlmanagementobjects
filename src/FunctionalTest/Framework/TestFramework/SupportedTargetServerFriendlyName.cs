// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{

    /// <summary>
    /// Checks whether the specified TargetServerFriendlyName (name attribute from ConnectionInfo.xml)
    /// matches one of the ones specified. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class SupportedTargetServerFriendlyNameAttribute : SqlSupportedDimensionAttribute
    {
        private readonly ISet<string> _targetServerFriendlyNames;

        public SupportedTargetServerFriendlyNameAttribute(params string[] targetServerFriendlyNames)
        {
            this._targetServerFriendlyNames = new HashSet<string>(targetServerFriendlyNames.Distinct());
        }

        /// <summary>
        /// The server is supported if the friendly name for the target server (defined in ConnectionInfo.xml)
        /// matches one of the ones we support
        /// </summary>
        /// <param name="server"></param>
        /// <param name="serverDescriptor"></param>
        /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public override bool IsSupported(SMO.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            return _targetServerFriendlyNames.Contains(targetServerFriendlyName);
        }

        public override bool IsSupported(SMO.Server server)
        {
            //This attribute should only ever be called through the override that passes in the targetServerFriendly name, if not
            //we should fail out since that's a test framework error that it got to that state
            throw new InvalidOperationException("The SupportedTargetServerFriendlyName attribute should never have IsSupported called without a TargetServerFriendlyName passed in");
        }

        public override bool IsSupported(TestDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            return _targetServerFriendlyNames.Contains(targetServerFriendlyName);
        }
    }
}
