// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Failover Category Enum
    /// </summary>
    public enum FailoverCategory
    {
        /// <summary>
        /// Failover with data loss
        /// </summary>
        FailoverWithDataLoss = 0,

        /// <summary>
        /// Failover without data loss
        /// </summary>
        FailoverWithoutDataLoss = 1,
    }
}
