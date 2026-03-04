// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to create a login on the sql-server secondary
    /// </summary>
    public class CreateLoginTask : HadrTask
    {
        /// <summary>
        /// logins to create on the replica
        /// </summary>
        private readonly IEnumerable<string> windowsLogins;

        /// <summary>
        /// The replica on which the logins will be created.
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replica">Replica data</param>
        /// <param name="windowsLogins">Logins to create</param>
        public CreateLoginTask(AvailabilityGroupReplica replica, IEnumerable<string> windowsLogins)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.CreateLoginText, replica.AvailabilityGroupReplicaData.ReplicaName))
        {
            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            if (windowsLogins == null)
            {
                throw new ArgumentNullException("windowsLogins");
            }

            this.replica = replica;
            this.windowsLogins = windowsLogins;
        }

        /// <summary>
        /// Create logins for the service accounts of other replicas, which do not exist.
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            this.UpdateStatus(new TaskEventArgs(this.Name, 
                string.Format(Resource.CreatingLoginsOnReplica, 
                                string.Join(",", this.windowsLogins), 
                                this.replica.AvailabilityGroupReplicaData.ReplicaName), 
                TaskEventStatus.Running));

            // This task will only be tried once.
            policy.Expired = true;

            this.replica.CreateLogins(this.windowsLogins);


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
