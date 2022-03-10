// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Globalization;

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
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.InvalidAvailabilityGroupException,
                availabilityGroupName, serverName)) { }
    }
}
