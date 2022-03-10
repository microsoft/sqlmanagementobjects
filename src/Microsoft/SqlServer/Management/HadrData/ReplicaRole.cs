// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.HadrData
{

    [LocalizedPropertyResources("Microsoft.SqlServer.Management.HadrData.Resource")]
    public enum ReplicaRole
    {
        /// <summary>
        /// Replica is in primary role
        /// </summary>
        [DisplayNameKey("Primary")]
        Primary = 0,

        /// <summary>
        /// Replica is in secondary role
        /// </summary>
        [DisplayNameKey("Secondary")]
        Secondary = 1
    };
}
