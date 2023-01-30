// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Class that stores information about an availability group subnet
    /// </summary>
    public class AvailabilityGroupListenerSubnet
    {
        /// <summary>
        /// Checks if subnet is Dhcp
        /// </summary>
        public bool IsDHCP { get; set; }

        /// <summary>
        /// The Subnet ip address
        /// </summary>
        public string SubnetIP { get; set; }

        /// <summary>
        /// The subnet mask
        /// </summary>
        public string SubnetMask { get; set; }

        /// <summary>
        /// The ip address
        /// </summary>
        public string IPAddress { get; set; }
    }
}
