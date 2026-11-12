// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    public sealed partial class AvailabilityReplicaCollection : RemovableCollectionBase<AvailabilityReplica, AvailabilityGroup>
    {

        //has custom string comparer
        private readonly StringComparer m_comparer;

        //must initialize in constructor
        internal AvailabilityReplicaCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base((AvailabilityGroup)parentInstance)
        {
            m_comparer = comparer;
        }

        internal override StringComparer StringComparer
        {
            get 
            {
                return m_comparer;
            }
        }
    }
}
