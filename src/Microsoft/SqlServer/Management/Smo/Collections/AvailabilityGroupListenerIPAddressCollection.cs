// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed collection of MAPPED_TYPE objects
    /// Supports indexing objects by their IPAddress and SubnetMask and SubnetIP properties
    ///</summary>
    public sealed class AvailabilityGroupListenerIPAddressCollection : AvailabilityGroupListenerIPAddressCollectionBase
    {

        internal AvailabilityGroupListenerIPAddressCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }


        public AvailabilityGroupListener Parent
        {
            get
            {
                return this.ParentInstance as AvailabilityGroupListener;
            }
        }


        public AvailabilityGroupListenerIPAddress this[Int32 index]
        {
            get
            { 
                return GetObjectByIndex(index) as AvailabilityGroupListenerIPAddress;
            }
        }

        public AvailabilityGroupListenerIPAddress this[string ipAddress, string subnetMask, string subnetIP]
        {
            get
            {

                return GetObjectByKey(new AvailabilityGroupListenerIPAddressObjectKey(ipAddress, subnetMask, subnetIP)) as AvailabilityGroupListenerIPAddress;
            }
        }

        public void CopyTo(AvailabilityGroupListenerIPAddress[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }


        public AvailabilityGroupListenerIPAddress ItemById(int id)
        {
            return (AvailabilityGroupListenerIPAddress)GetItemById(id);
        }


        protected override Type GetCollectionElementType()
        {
            return typeof(AvailabilityGroupListenerIPAddress);
        }

        internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return new AvailabilityGroupListenerIPAddress(this, key, state);
        }


















            public void Add(AvailabilityGroupListenerIPAddress AvailabilityGroupListenerIPAddress) 
            {
                if( null == AvailabilityGroupListenerIPAddress )
                    throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("AvailabilityGroupListenerIPAddress"));
            
                AddImpl(AvailabilityGroupListenerIPAddress);
            }
    }
}
