// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed collection of AvailabilityGroupListenerIPAddress objects
    /// Supports indexing objects by their IPAddress and SubnetMask and SubnetIP properties
    ///</summary>
    public sealed class AvailabilityGroupListenerIPAddressCollection : AvailabilityGroupListenerIPAddressCollectionBase
    {

        internal AvailabilityGroupListenerIPAddressCollection(SqlSmoObject parentInstance) : base(parentInstance)
        {
        }


        public AvailabilityGroupListener Parent
        {
            get
            {
                return this.ParentInstance as AvailabilityGroupListener;
            }
        }


        public AvailabilityGroupListenerIPAddress this[string ipAddress, string subnetMask, string subnetIP]
        {
            get
            {

                return GetObjectByKey(new AvailabilityGroupListenerIPAddressObjectKey(ipAddress, subnetMask, subnetIP)) as AvailabilityGroupListenerIPAddress;
            }
        }

        public void Add(AvailabilityGroupListenerIPAddress availabilityGroupListenerIPAddress)
        {
            if (null == availabilityGroupListenerIPAddress)
                throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException(nameof(availabilityGroupListenerIPAddress)));

            AddImpl(availabilityGroupListenerIPAddress);
        }
    }
}
