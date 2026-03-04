// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to add Availability Group Listener to an Availability Group
    /// </summary>
    public class UpdateAvailabilityGroupListenerTask : HadrTask, IScriptableTask
    {
        /// <summary>
        /// The availability group data
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public UpdateAvailabilityGroupListenerTask(AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.UpdateAvailabilityGroupListenerText, availabilityGroupData.AvailabilityGroupListener.ListenerName))
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
            this.ScriptingConnections = new List<ServerConnection>
                {
                    this.availabilityGroupData.PrimaryServer.ConnectionContext
                };
        }

        /// <summary>
        /// Connections to use for scripting
        /// </summary>
        public List<ServerConnection> ScriptingConnections
        {
            get;
            private set;
        }

        /// <summary>
        /// Adds an availability group listener to the availability group
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            this.UpdateStatus(new TaskEventArgs(this.Name,
                                    string.Format(Resource.CreatingAvailabilityGroup, this.availabilityGroupData.GroupName),
                                    TaskEventStatus.Running));

            // This task is only tried once.
            policy.Expired = true;

            if (this.availabilityGroupData.SqlAvailabilityGroup == null)
            {
                throw new InvalidAvailabilityGroupException(
                    this.availabilityGroupData.GroupName,
                    this.availabilityGroupData.PrimaryServer.ConnectionContext.TrueName);
            }

            // Add all listener configurations

            SMO.AvailabilityGroupListener listener =
                new SMO.AvailabilityGroupListener(this.availabilityGroupData.SqlAvailabilityGroup, this.availabilityGroupData.AvailabilityGroupListener.ListenerName);

            listener.PortNumber = this.availabilityGroupData.AvailabilityGroupListener.ListenerPortNumber;

            switch (this.availabilityGroupData.AvailabilityGroupListener.NetworkMode)
            {
                case AGListenerNetworkMode.DHCP:
                    {
                        SMO.AvailabilityGroupListenerIPAddress address = new SMO.AvailabilityGroupListenerIPAddress(listener)
                        {
                            IsDHCP = true,
                            SubnetIP = this.availabilityGroupData.AvailabilityGroupListener.DhcpSubnetConfigurations.SubnetIP,
                            SubnetMask = this.availabilityGroupData.AvailabilityGroupListener.DhcpSubnetConfigurations.SubnetMask
                        };

                        listener.AvailabilityGroupListenerIPAddresses.Add(address);
                    }
                    break;
                case AGListenerNetworkMode.StaticIP:
                    foreach (AvailabilityGroupListenerSubnet staticIpSubnetConfiguration in this.availabilityGroupData.AvailabilityGroupListener.StaticIpSubnetConfigurations)
                    {
                        SMO.AvailabilityGroupListenerIPAddress address = new SMO.AvailabilityGroupListenerIPAddress(listener)
                        {
                            IsDHCP = false,
                            SubnetIP = staticIpSubnetConfiguration.SubnetIP,
                            SubnetMask = staticIpSubnetConfiguration.SubnetMask,
                            IPAddress = staticIpSubnetConfiguration.IPAddress
                        };

                        listener.AvailabilityGroupListenerIPAddresses.Add(address);
                    }
                    break;
            }
            listener.Alter();

        }

        /// <summary>
        /// Rollback is not supported for this task
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
