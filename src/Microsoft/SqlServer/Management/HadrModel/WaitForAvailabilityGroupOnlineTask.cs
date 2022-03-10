// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to wait for an Availability Group to come online
    /// </summary>
    public class WaitForAvailabilityGroupOnlineTask : HadrTask
    {
        /// <summary>
        /// The availability group data
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public WaitForAvailabilityGroupOnlineTask(AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.WaitForAvailabilityGroupOnlineText, availabilityGroupData.GroupName))
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// Waits for availability group to come online
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            this.UpdateStatus(new TaskEventArgs(this.Name,
                                    string.Format(Resource.WaitForAvailabilityGroupOnline, this.availabilityGroupData.GroupName),
                                    TaskEventStatus.Running));

            SMO.AvailabilityGroup availabilityGroup =
                this.availabilityGroupData.PrimaryServer.AvailabilityGroups[this.availabilityGroupData.GroupName];

            if (availabilityGroup != null)
            {
                availabilityGroup.Refresh();

                // Task is not retried once we are successfully able to query the Primary Server Name
                policy.Expired = !string.IsNullOrEmpty(availabilityGroup.PrimaryReplicaServerName);
            }
        }

        /// <summary>
        /// Rollback is not supported for this task
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
