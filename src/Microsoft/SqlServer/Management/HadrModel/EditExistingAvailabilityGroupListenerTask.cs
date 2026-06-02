// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    ///  The Task for editing a existing availability group listener
    /// </summary>
    public class EditExistingAvailabilityGroupListenerTask:HadrTask
    {

        /// <summary>
        /// AvailabilityGroup object from smo for the existing target AG
        /// </summary>
        private AvailabilityGroup availabilityGroup;

        /// <summary>
        /// AvailabilityGroupListenerConfiguration object to be altered to the AG
        /// </summary>
        private AvailabilityGroupListenerConfiguration availabilityGroupListenerConfiguration;

        #region ctor
        /// <summary>
        /// ctor
        /// </summary>
        public EditExistingAvailabilityGroupListenerTask(string name, AvailabilityGroup AvailabilityGroup, AvailabilityGroupListenerConfiguration AvailabilityGroupListenerConfiguration)
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
        /// method to perform edit an existing listener from AG
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            bool needUpdate = false;

            AvailabilityGroupListener listener = this.availabilityGroup.AvailabilityGroupListeners[this.availabilityGroupListenerConfiguration.ListenerName];

            if (listener.PortNumber != this.availabilityGroupListenerConfiguration.ListenerPortNumber)
            {
                listener.PortNumber = this.availabilityGroupListenerConfiguration.ListenerPortNumber;
                needUpdate = true;
            }

            AvailabilityGroupListenerIPAddressCollection ipAddresses = listener.AvailabilityGroupListenerIPAddresses;
            if (this.availabilityGroupListenerConfiguration.NetworkMode == AGListenerNetworkMode.StaticIP)
            {
                foreach (AvailabilityGroupListenerSubnet subnet in this.availabilityGroupListenerConfiguration.StaticIpSubnetConfigurations)
                {
                    AvailabilityGroupListenerIPAddress ipAddress = new AvailabilityGroupListenerIPAddress(listener);
                    ipAddress.IsDHCP = false;
                    ipAddress.SubnetIP = subnet.SubnetIP;
                    //ipAddress.SubnetIPv4Mask = subnet.SubnetIPv4Mask;
                    ipAddress.SubnetMask = subnet.SubnetMask;
                    ipAddress.IPAddress = subnet.IPAddress;
                    ipAddresses.Add(ipAddress);

                    ipAddress.Create();
                }
            }

            if (needUpdate)
            {
                listener.Alter();
            }

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
