// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to Validater Role Change After Failing Over 
    /// </summary>
    public class FailoverWaitRoleChangeValidator : Validator
    {
        /// <summary>
        /// FailoverData for executing failover task
        /// </summary>
        private FailoverData failoverData;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="failoverData"></param>
        public FailoverWaitRoleChangeValidator(FailoverData failoverData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.WaitForRoleChange, failoverData.TargetAvailabilityReplica.Name))
        {
            if (failoverData == null)
            {
                throw new ArgumentNullException("failoverData");
            }
            this.failoverData = failoverData;
        }

        /// <summary>
        /// Validate Role Change 
        /// </summary>
        /// <param name="policy"></param>
        protected override void Validate(IExecutionPolicy policy)
        {
            this.failoverData.TargetAvailabilityReplica.Refresh();

            if (this.failoverData.TargetAvailabilityReplica.Role == AvailabilityReplicaRole.Primary)
            {
                policy.Expired = true;
                return;
            }
        }
    }
}
