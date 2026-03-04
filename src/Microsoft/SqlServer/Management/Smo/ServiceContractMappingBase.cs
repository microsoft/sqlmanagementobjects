// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo.Broker
{
    public partial class ServiceContractMapping : NamedSmoObject, Cmn.IMarkForDrop
    {
        internal ServiceContractMapping(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "ServiceContractMapping";
            }
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        /// <summary>
        /// Name of service contract mapping
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Standalone)]
        [SfcReference(typeof(ServiceContract), "Server[@Name = '{0}']/Database[@Name = '{1}']/ServiceBroker/ServiceContract[@Name='{2}']", "Parent.Parent.Parent.Parent.ConnectionContext.TrueName", "Parent.Parent.Parent.Name", "Name")]
        [CLSCompliant(false)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }
    }


}


