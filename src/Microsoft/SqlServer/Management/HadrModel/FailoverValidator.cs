// Copyright (c) Microsoft Corporation.
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
    public class FailoverValidator : Validator
    {
        /// <summary>
        /// FailoverData for executing failover task
        /// </summary>
        private FailoverData failoverData;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="failoverData"></param>
        public FailoverValidator(FailoverData failoverData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.ValidatingFailoverSettings, failoverData.TargetAvailabilityReplica.Name))
        {
            if (failoverData == null)
            {
                throw new ArgumentNullException("failoverData");
            }
            this.failoverData = failoverData;
        }

        /// <summary>
        /// Perform the failover primary server validation
        /// </summary>
        /// <param name="policy"></param>
        protected override void Validate(IExecutionPolicy policy)
        {
            if (this.failoverData.TargetReplicaFailoverCategory == FailoverCategory.FailoverWithoutDataLoss)
            {
                //Manual Failover Validation
                if (this.failoverData.TargetAvailabilityReplica.Role == AvailabilityReplicaRole.Primary)
                {
                    throw new FailoverValidationException(this.failoverData.TargetAvailabilityReplica.Name, this.failoverData.TargetAvailabilityReplica.Role.ToString());
                }
            }
            else
            {
                if (this.failoverData.TargetAvailabilityReplica.Role == AvailabilityReplicaRole.Primary)
                {
                    throw new FailoverValidationException(this.failoverData.TargetAvailabilityReplica.Name, this.failoverData.TargetAvailabilityReplica.Role.ToString());
                }
            }

            policy.Expired = true;
        }
    }
}
