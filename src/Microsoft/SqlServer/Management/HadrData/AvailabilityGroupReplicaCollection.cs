// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// The Replica Collection Data contains a BindingList of AvailabilityGroupReplica
    /// </summary>
    internal class AvailabilityGroupReplicaCollection : SortableBindingList<AvailabilityGroupReplica>
    {
        public AvailabilityGroupReplicaCollection()
        {
        }

        public AvailabilityGroupReplicaCollection(IList<AvailabilityGroupReplica> replicaList)
            : base(replicaList)
        {
        }
    }
}
