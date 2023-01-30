// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Base attribute class for adding supported server metadata to tests. This allows
    /// tests to specify what servers they support by using the different implementations
    /// of this class (such as SupportedServerVersionRange to specify what range of versions
    /// a test supports)
    /// </summary>
    public abstract class SqlTestDimensionAttribute : Attribute
    {

        protected SqlTestDimensionAttribute()
        {
            
        }

        /// <summary>
        /// Checks whether the specified server is "supported", that is whether it meets the
        /// criteria of the dimension that is being defined.
        /// 
        /// Override this only if you need to use the EngineEdition or TargetServerFriendlyName, otherwise just
        /// implement IsSupported(Server)
        /// </summary>
        /// <param name="server"></param>
        /// <param name="serverDescriptor"></param>
        /// /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public virtual bool IsSupported(SMO.Server server, TestServerDescriptor serverDescriptor, string targetServerFriendlyName)
        {
            //Default is to ignore dbEngineEdition and targetServerFriendlyName since most of the dimensions won't care about those. 
            return IsSupported(server);
        }

        /// <summary>
        /// Checks whether the specified server is "supported", that is whether it meets the
        /// criteria of the dimension that is being defined.
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public abstract bool IsSupported(SMO.Server server);

    }

    /// <summary>
    /// More specialized version of the TestDimension for use by positive dimensions (inclusive
    /// check, if any of these are true then the server is considered supported)
    /// </summary>
    public abstract class SqlSupportedDimensionAttribute : SqlTestDimensionAttribute
    { }

    /// <summary>
    /// More specialized version of the SqlTestDimension for use by negative dimensions
    /// (exclusive check, if any of these are false then the server is considered NOT supported)
    /// </summary>
    public abstract class SqlUnsupportedDimensionAttribute : SqlTestDimensionAttribute
    { }
}
