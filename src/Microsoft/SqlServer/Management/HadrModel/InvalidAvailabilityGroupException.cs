// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when an availbility group
    /// with the specified name is not found on the server.
    /// </summary>
    public class InvalidAvailabilityGroupException : HadrTaskBaseException
    {
        /// <summary>
        /// Standard Exception with availability group name, and server name
        /// </summary>
        /// <param name="availabilityGroupName"></param>
        /// <param name="serverName"></param>
        public InvalidAvailabilityGroupException(string availabilityGroupName, string serverName)
            : base(Resource.FormatInvalidAvailabilityGroupException(availabilityGroupName, serverName)) { }
    }
}
