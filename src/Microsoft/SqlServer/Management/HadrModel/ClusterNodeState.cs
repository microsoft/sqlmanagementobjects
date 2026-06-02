// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Matching MSCluster_Node definition
    /// http://msdn.microsoft.com/en-us/library/aa371446(v=vs.85).aspx
    /// </summary>
    public enum ClusterNodeState
    {
        /// <summary>
        /// unknown state
        /// </summary>
        Unknown = -1,

        /// <summary>
        /// node is up
        /// </summary>
        Up = 0,

        /// <summary>
        /// node is down
        /// </summary>
        Down = 1,

        /// <summary>
        /// node paused
        /// </summary>
        Paused = 2,

        /// <summary>
        /// node joining
        /// </summary>
        Joining = 3
    }
}
