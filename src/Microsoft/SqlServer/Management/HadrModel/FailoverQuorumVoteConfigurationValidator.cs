// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to Validater Quorum Vote Configuration
    /// </summary>
    public class FailoverQuorumVoteConfigurationValidator : Validator
    {
        /// <summary>
        /// FailoverData for executing failover task
        /// </summary>
        private FailoverData failoverData;
        
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="failoverData"></param>
        public FailoverQuorumVoteConfigurationValidator(FailoverData failoverData)
            : base(Resource.ValidatingWSFCQuorumConfiguration)
        {
            if (failoverData == null) 
            {
                throw new ArgumentNullException("failoverData");
            }
            this.failoverData = failoverData;
        }

        /// <summary>
        /// Validation
        /// </summary>
        /// <param name="policy"></param>
        protected override void Validate(IExecutionPolicy policy)
        {
            if (QuorumHelper.ValidateQuorumVoteConfiguration(this.failoverData.TargetAvailabilityReplica.Parent))
            {
                policy.Expired = true;
                return;
            }
        }
    }
}
