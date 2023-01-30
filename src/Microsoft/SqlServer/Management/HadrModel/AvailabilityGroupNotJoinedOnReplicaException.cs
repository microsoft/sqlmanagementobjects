// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when Availability Group is not joined on the secondary replica
    /// </summary>
    public class AvailabilityGroupNotJoinedOnReplicaException : HadrTaskBaseException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupName">The availability group name</param>
        /// <param name="replicaServerName">The replica server name</param>
        public AvailabilityGroupNotJoinedOnReplicaException(string availabilityGroupName, string replicaServerName) : 
            base(string.Format(CultureInfo.InvariantCulture,
                    Resource.AvailabilityGroupNotJoined,
                    availabilityGroupName,
                    replicaServerName))
        {
        }
    }
}
