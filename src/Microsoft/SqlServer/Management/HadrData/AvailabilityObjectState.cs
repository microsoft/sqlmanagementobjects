// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Enum which tells the state of the AvailabilityGroup object
    /// </summary>
    public enum AvailabilityObjectState
    {
        /// <summary>
        /// The object is in unknown state
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// The object is being created on server
        /// </summary>
        Creating = 1,

        /// <summary>
        /// The object already exists on server
        /// </summary>
        Existing = 2
    }
}
