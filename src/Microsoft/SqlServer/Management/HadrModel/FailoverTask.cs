// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to Execute Failover Action
    /// </summary>
    public class FailoverTask : HadrTask
    {
        /// <summary>
        /// FailoverData for executing failover task
        /// </summary>
        private FailoverData failoverData;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="failoverData"></param>
        public FailoverTask(FailoverData failoverData)
            : base(failoverData.TargetReplicaFailoverCategory == FailoverCategory.FailoverWithDataLoss ?
                string.Format(CultureInfo.InvariantCulture, Resource.ForcedFailoverTaskText, failoverData.TargetAvailabilityReplica.Name)
                : string.Format(CultureInfo.InvariantCulture, Resource.ManualFailoverTaskText, failoverData.TargetAvailabilityReplica.Name))
        {
            if (failoverData == null)
            {
                throw new ArgumentNullException("failoverData");
            }
            this.failoverData = failoverData;
        }

        /// <summary>
        /// Perform the failover action
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            if (this.failoverData.TargetReplicaFailoverCategory == FailoverCategory.FailoverWithoutDataLoss)
            {
                this.failoverData.TargetAvailabilityGroup.Failover();
            }
            else
            {
                this.failoverData.TargetAvailabilityGroup.FailoverWithPotentialDataLoss();
            }

            // For cluster type NONE, we have to do a 2-step failover: failover (to the target secondary replica) and demote (of the previous 
            // Note: the order of the operation *may* feel backward, but it's actually
            //       what is documented on DOCS at
            //       https://docs.microsoft.com/sql/database-engine/availability-groups/windows/perform-a-planned-manual-failover-of-an-availability-group-sql-server?view=sql-server-ver15#fail-over-the-primary-replica-on-a-read-scale-availability-group.
            if (failoverData.AvailabilityGroup.ClusterTypeWithDefault == AvailabilityGroupClusterType.None)
            {
                if (this.failoverData.PrimaryServer != null)
                {
                    AvailabilityGroup primaryAvailabilityGroup = this.failoverData.PrimaryServer.AvailabilityGroups[this.failoverData.AvailabilityGroupName];

                    if (primaryAvailabilityGroup != null)
                    {
                        primaryAvailabilityGroup.DemoteAsSecondary();
                    }
                }
            }

            policy.Expired = true;
        }

        /// <summary>
        /// No Support for fail over in rolling back
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
