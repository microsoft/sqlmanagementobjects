// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// The AG Listener Configuration Data 
    /// This object contains information for the Listenner of an AG
    /// </summary>
    public class AvailabilityGroupListenerConfiguration
    {

        #region Fields
        /// <summary>
        /// Default Listener Port Number
        /// </summary>
        private int listenerPortNumber = 1433;

        /// <summary>
        /// The subnet data
        /// </summary>
        private readonly List<AvailabilityGroupListenerSubnet> subnetConfigurations; 

        #endregion

        #region Constructors
        /// <summary>
        /// the base ctor sets all defualt value for components
        /// </summary>
        public AvailabilityGroupListenerConfiguration()
        {
            this.ListenerName = String.Empty;
            this.ListenerPortNumber = 0;
            this.NetworkMode = AGListenerNetworkMode.StaticIP;
            this.subnetConfigurations = new List<AvailabilityGroupListenerSubnet>();
        }

        /// <summary>
        /// Constructor to use in case of existing Availability Group
        /// </summary>
        /// <param name="ag">The availability group</param>
        public AvailabilityGroupListenerConfiguration(AvailabilityGroupListener ag)
        {
            this.ListenerName = ag.Name;
            this.ListenerPortNumber = ag.PortNumber;

            //Hybird IP(DHCP+Static IP) will not work, so we can determine mode by the first subnets
            this.NetworkMode = ag.AvailabilityGroupListenerIPAddresses[0].IsDHCP ? AGListenerNetworkMode.DHCP : AGListenerNetworkMode.StaticIP;

            //copy the subnets list from ag to our data model
            this.subnetConfigurations = new List<AvailabilityGroupListenerSubnet>();
            foreach (Smo.AvailabilityGroupListenerIPAddress subnet in ag.AvailabilityGroupListenerIPAddresses)
            {
                this.subnetConfigurations.Add(new AvailabilityGroupListenerSubnet()
                {
                    IsDHCP = subnet.IsDHCP,
                    SubnetIP = subnet.SubnetIP,
                    SubnetMask = subnet.SubnetMask,
                    IPAddress = subnet.IPAddress
                });
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// the AG Listener Port Number
        /// </summary>
        public int ListenerPortNumber
        {
            get
            {
                return listenerPortNumber;
            }
            set
            {
                listenerPortNumber = value;
            }
        }

        /// <summary>
        /// the listener Network Mode
        /// </summary>
        public AGListenerNetworkMode NetworkMode
        {
            get;
            set;
        }

        /// <summary>
        /// Listener Name
        /// </summary>
        public string ListenerName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the list of all subnets
        /// </summary>
        public IList<AvailabilityGroupListenerSubnet> SubnetConfigurations
        {
            get
            {
                return this.subnetConfigurations;
            }
        }

        /// <summary>
        /// The DHCP IP Subnet data
        /// We can have only one Dhcp subnet data
        /// </summary>
        public AvailabilityGroupListenerSubnet DhcpSubnetConfigurations
        {
            get
            {
                return this.subnetConfigurations.FirstOrDefault(s => s.IsDHCP);
            }
        }

        /// <summary>
        /// Gets the static ip Subnet data
        /// An availability group listener can have more than one static ip-addresses
        /// </summary>
        public IList<AvailabilityGroupListenerSubnet> StaticIpSubnetConfigurations
        {
            get
            {
                return this.subnetConfigurations.Where(s => !s.IsDHCP).ToList().AsReadOnly();
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a subnet configuration to the data model
        /// If there is already one subnet with dhcp, adding 
        /// another dhcp subnet configuration is not allowed.
        /// </summary>
        /// <param name="subnetConfiguration">The subnet to add</param>
        public void AddSubnetConfiguration(AvailabilityGroupListenerSubnet subnetConfiguration)
        {
            if (subnetConfiguration == null)
            {
                throw new ArgumentNullException("subnetConfiguration");
            }

            if (subnetConfiguration.IsDHCP && this.subnetConfigurations.FirstOrDefault(s => s.IsDHCP) != null)
            {
                throw new ArgumentException("Listener configuration already contains a DHCP subnet.");
            }

            this.subnetConfigurations.Add(subnetConfiguration);
        }
        #endregion
        
    }
}
