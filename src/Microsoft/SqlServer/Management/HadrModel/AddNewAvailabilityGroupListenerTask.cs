// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    ///  The Task for creating a new availability group listener
    /// </summary>
    public class AddNewAvailabilityGroupListenerTask:HadrTask
    {

        /// <summary>
        /// AvailabilityGroup object from smo for the existing target AG
        /// </summary>
        private AvailabilityGroup availabilityGroup;

        /// <summary>
        /// AvailabilityGroupListenerConfiguration object to be add to the AG
        /// </summary>
        private AvailabilityGroupListenerConfiguration availabilityGroupListenerConfiguration;

        #region ctor
        /// <summary>
        /// ctor
        /// </summary>
        public AddNewAvailabilityGroupListenerTask(string name, AvailabilityGroup AvailabilityGroup, AvailabilityGroupListenerConfiguration AvailabilityGroupListenerConfiguration)
            :base(name)
        {
            
            if (AvailabilityGroup == null)
            {
                throw new ArgumentNullException("AvailabilityGroup");
            }

            this.availabilityGroup = AvailabilityGroup;

            if (AvailabilityGroupListenerConfiguration == null)
            {
                throw new ArgumentNullException("AvailabilityGroupListenerConfiguration");
            }

            this.availabilityGroupListenerConfiguration = AvailabilityGroupListenerConfiguration;


        }
        #endregion


        /// <summary>
        /// method to perform adding a new listener to existing AG
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            AvailabilityGroupListener listener = new AvailabilityGroupListener(this.availabilityGroup, this.availabilityGroupListenerConfiguration.ListenerName);
            listener.PortNumber = this.availabilityGroupListenerConfiguration.ListenerPortNumber;

            AvailabilityGroupListenerIPAddressCollection ipAddresses = listener.AvailabilityGroupListenerIPAddresses;
            if (this.availabilityGroupListenerConfiguration.NetworkMode == AGListenerNetworkMode.DHCP)
            {
                AvailabilityGroupListenerIPAddress ipAddress = new AvailabilityGroupListenerIPAddress(listener);
                ipAddress.IsDHCP = true;
                ipAddress.SubnetIP = this.availabilityGroupListenerConfiguration.DhcpSubnetConfigurations.IPAddress;
                // ipAddress.SubnetIPv4Mask = this.SelectedDHCPSubnet.SubnetIPv4Mask;
                ipAddress.SubnetMask = this.availabilityGroupListenerConfiguration.DhcpSubnetConfigurations.SubnetMask;
                ipAddresses.Add(ipAddress);
            }
            else
            {

                foreach (AvailabilityGroupListenerSubnet subnet in this.availabilityGroupListenerConfiguration.StaticIpSubnetConfigurations)
                {
                    AvailabilityGroupListenerIPAddress ipAddress = new AvailabilityGroupListenerIPAddress(listener);
                    ipAddress.IsDHCP = false;
                    ipAddress.SubnetIP = subnet.SubnetIP;
                    //   ipAddress.SubnetIPv4Mask = subnet.SubnetIPv4Mask;
                    ipAddress.SubnetMask = subnet.SubnetMask;
                    ipAddress.IPAddress = subnet.IPAddress;
                    ipAddresses.Add(ipAddress);
                }
            }
            listener.Create();

            policy.Expired = true;

        }

        /// <summary>
        /// Rollback not support in this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
