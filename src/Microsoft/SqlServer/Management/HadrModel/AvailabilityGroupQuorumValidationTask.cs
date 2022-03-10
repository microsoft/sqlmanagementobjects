// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates the quorum vote configuration of the given availability group.
    /// Nodes participating in an AG should only have a quroum vote if they 
    /// can host the primary replica or if the can host a automatic secondary
    /// partnered with the primary. Note the use of 'can' is due to the potential
    /// presence of FCIs.
    /// Although this task is like a validator but it must be derived from task to fit into the task provider
    /// </summary>
    public class AvailabilityGroupQuorumValidationTask :HadrTask 
    {
        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="availabilityGroupData"></param>
        public AvailabilityGroupQuorumValidationTask(AvailabilityGroupData availabilityGroupData)
            :base(Resource.QuorumValidationTaskText)
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// Validates the quorum vote configuration of the given availability group.
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            //This Validation executes only once
            policy.Expired = true;

            try
            {

                if (!QuorumHelper.ValidateQuorumVoteConfiguration(this.availabilityGroupData.SqlAvailabilityGroup))
                {

                    throw new AvailabilityGroupQuorumValidationTaskException(this.availabilityGroupData.SqlAvailabilityGroup.Name);
                }
            }
            catch (QuorumHelperException e)
            {
                throw new AvailabilityGroupQuorumValidationTaskException(this.availabilityGroupData.SqlAvailabilityGroup.Name, e);
            }
        }


        /// <summary>
        /// No support for rolling back in this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
