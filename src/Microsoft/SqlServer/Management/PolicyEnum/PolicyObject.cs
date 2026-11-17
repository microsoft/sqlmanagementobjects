// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Dmf
{
    using System;
    using System.Reflection;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;

    internal sealed class PolicyObject : SqlObject, Microsoft.SqlServer.Management.Sdk.Sfc.ISupportVersions
    {
        public override Assembly ResourceAssembly
        {
            get
            {
                return Assembly.GetExecutingAssembly();
            }
        }

        public ServerVersion GetServerVersion(Object conn)
        {
            return ExecuteSql.GetServerVersion(conn);
        }
    }
}
