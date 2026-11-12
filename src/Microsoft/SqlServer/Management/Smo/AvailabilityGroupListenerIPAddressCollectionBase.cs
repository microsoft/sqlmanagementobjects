// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Collection of AvailabilityGroupListenerIPAddress objects associated with an AvailabilityGroupListener
    /// </summary>
    public class AvailabilityGroupListenerIPAddressCollectionBase : SortedListCollectionBase<AvailabilityGroupListenerIPAddress, AvailabilityGroupListener>
    {
        internal AvailabilityGroupListenerIPAddressCollectionBase(SqlSmoObject parent)
            : base((AvailabilityGroupListener)parent)
        {
        }

        protected override string UrnSuffix => AvailabilityGroupListenerIPAddress.UrnSuffix;

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList<AvailabilityGroupListenerIPAddress>(new AvailabilityGroupListenerIPAddressObjectComparer(StringComparer));
        }


        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var ipAddress = urn.GetAttribute("IPAddress");
            var subnetMask = urn.GetAttribute("SubnetMask");
            var subnetIP = urn.GetAttribute("SubnetIP");

            return new AvailabilityGroupListenerIPAddressObjectKey(ipAddress, subnetMask, subnetIP);
        }

        internal override AvailabilityGroupListenerIPAddress GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new AvailabilityGroupListenerIPAddress(this, key, state);
    }

    internal class AvailabilityGroupListenerIPAddressObjectComparer : ObjectComparerBase
    {
        public AvailabilityGroupListenerIPAddressObjectComparer(IComparer stringComparer) 
            : base(stringComparer)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            var dbr1 = obj1 as AvailabilityGroupListenerIPAddressObjectKey;
            var dbr2 = obj2 as AvailabilityGroupListenerIPAddressObjectKey;

            if (dbr1 == null && dbr2 == null)
            {
                return 0;
            }
            else if (dbr1 == null)
            {
                return -1;
            }
            else if (dbr2 == null)
            {
                return 1;
            }

            // We order first by IP address, SubnetIP and then SubnetMask.
            var ipAddressComparison = stringComparer.Compare(dbr1.IPAddress, dbr2.IPAddress);
            if (ipAddressComparison == 0)
            {
                var subnetMaskComparison = stringComparer.Compare(dbr1.SubnetMask, dbr2.SubnetMask);
                if (subnetMaskComparison == 0)
                {
                    return stringComparer.Compare(dbr1.SubnetIP, dbr2.SubnetIP);
                }

                return subnetMaskComparison;
            }
            else
            {
                return ipAddressComparison;
            }
        }
    }

    internal class AvailabilityGroupListenerIPAddressObjectKey : ObjectKeyBase
    {
        internal static StringCollection fields;

        public AvailabilityGroupListenerIPAddressObjectKey()
        {
            // Note, we don't add any fields here because that's done in the static 
            // constructor below.
        }

        public AvailabilityGroupListenerIPAddressObjectKey(string ipAddress, string subnetMask, string subnetIP)
        {
            IPAddress = ipAddress;
            SubnetMask = subnetMask;
            SubnetIP = subnetIP;
        }

        static AvailabilityGroupListenerIPAddressObjectKey()
        {
            fields = new StringCollection();
            fields.Add("IPAddress");
            fields.Add("SubnetMask");
            fields.Add("SubnetIP");
        }

        public string IPAddress
        {
            get;
            set;
        }

        public string SubnetMask
        {
            get;
            set;
        }

        public string SubnetIP
        {
            get;
            set;
        }

        public override string UrnFilter
        {
            get
            {
                return string.Format(SmoApplication.DefaultCulture, "@IPAddress='{0}' and @SubnetMask='{1}' and @SubnetIP='{2}'",
                    SqlSmoObject.SqlString(IPAddress), SqlSmoObject.SqlString(SubnetMask), SqlSmoObject.SqlString(SubnetIP));
            }
        }

        public override StringCollection GetFieldNames()
        {
            return AvailabilityGroupListenerIPAddressObjectKey.fields;
        }

        internal override void Validate(Type objectType)
        {
        }

        public override bool IsNull
        {
            get
            {
                return null == IPAddress || null == SubnetMask || null == SubnetIP;
            }
        }

        public override string GetExceptionName()
        {
            return string.Format(SmoApplication.DefaultCulture, "IPaddress {0} of Subnet mask {1} and SubnetIP {2}", IPAddress, SubnetMask, SubnetIP);
        }

        public override ObjectKeyBase Clone()
        {
            return new AvailabilityGroupListenerIPAddressObjectKey(IPAddress, SubnetMask, SubnetIP);
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new AvailabilityGroupListenerIPAddressObjectComparer(stringComparer);
        }

        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}/{1}/{2}", IPAddress, SubnetMask, SubnetIP);
        }
    }
}
