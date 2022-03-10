// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to join a database in the secondary to the availability group
    /// </summary>
    public class JoinDatabaseToAvailabilityGroupTask : HadrTask
    {
        #region Fields
        /// <summary>
        /// Number of retries for the joining the availability group
        /// </summary>
        private const int JoinAvailabilityGroupRetryTimes = 10;

        /// <summary>
        /// Sleep duration between retries
        /// </summary>
        private const int JoinAvailabilityGroupSleepBetweenRetries = 10;

        /// <summary>
        /// The availability group data
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// The replica data
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// The name of the database to join
        /// </summary>
        private readonly string databaseName;

        /// <summary>
        /// Gets the database name
        /// </summary>
        public string DatabaseName
        {
            get { return this.databaseName; }
        }

        /// <summary>
        /// The replica data for the replica on which the join database happens
        /// </summary>
        public AvailabilityGroupReplica ReplicaData
        {
            get
            {
                return this.replica;
            }
        }

        /// <summary>
        /// The availability group data
        /// </summary>
        public AvailabilityGroupData AvailabilityGroupData
        {
            get
            {
                return this.availabilityGroupData;
            }
        }
        #endregion

        #region Constuctor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="databaseName">Database Name</param>
        /// <param name="availabilityGroupData">Availability Group Data</param>
        /// <param name="replica">Information about the replica</param>
        public JoinDatabaseToAvailabilityGroupTask(string databaseName, AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(string.Format(Resource.JoinDatabaseToAvailabilityGroupText, databaseName, availabilityGroupData.GroupName, replica.AvailabilityGroupReplicaData.ReplicaName))
        {
            if (databaseName == null)
            {
                throw new ArgumentNullException("databaseName");
            }

            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            this.databaseName = databaseName;
            this.availabilityGroupData = availabilityGroupData;
            this.replica = replica;
        }
        #endregion

        #region Task
        /// <summary>
        /// Joins a database on the secondary to the availability group
        /// </summary>
        /// <param name="policy">the execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            SMO.Server replicaServer = HadrModelUtilities.GetNewSmoServerObject(replica.AvailabilityGroupReplicaData.Connection);

            SMO.AvailabilityGroup availabilityGroup = replicaServer.AvailabilityGroups[this.availabilityGroupData.GroupName];

            if (availabilityGroup == null)
            {
                throw new AvailabilityGroupNotJoinedOnReplicaException(this.availabilityGroupData.GroupName, replicaServer.Name);
            }

            SMO.AvailabilityDatabase availabilityDatabase = availabilityGroup.AvailabilityDatabases[this.databaseName];

            availabilityDatabase.JoinAvailablityGroup();

            if (this.JoinDatabaseToAvailabilityGroup(availabilityDatabase))
            {
                // This task is expired after join database to availability group succeeds.
                policy.Expired = true;
            }
        }

        /// <summary>
        /// Currently rollback is not supported for this task
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
        #endregion

        #region Helpers


        /// <summary>
        /// Returns true if the database is joined to the availability group
        /// Joins the database to the availability group otherwise
        /// </summary>
        /// <param name="availabilityDatabase">Database to join to the AG</param>
        /// <returns>true if the database is already joined to AG. false otherwise</returns>
        private bool JoinDatabaseToAvailabilityGroup(SMO.AvailabilityDatabase availabilityDatabase)
        {
            if (!availabilityDatabase.IsJoined)
            {
                availabilityDatabase.JoinAvailablityGroup();
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion
    }
}
