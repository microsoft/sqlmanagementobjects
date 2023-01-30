// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task that adds a database to an existing availability group.
    /// </summary>
    public class AddDatabaseToExistingAvailabilityGroupTask : HadrTask, IScriptableTask
    {
        #region Fields
        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Connections to use for scripting
        /// </summary>
        public List<ServerConnection> ScriptingConnections
        {
            get; 
            private set;
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public AddDatabaseToExistingAvailabilityGroupTask(AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.AddDatabaseToExistingAvailabilityGroupText, availabilityGroupData.GroupName))
        {
            if(availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
            this.ScriptingConnections = new List<ServerConnection>
            {
                this.availabilityGroupData.PrimaryServer.ConnectionContext
            };

            foreach(AvailabilityGroupReplica availabilityReplica in availabilityGroupData.Secondaries)
            {
                this.ScriptingConnections.Add(availabilityReplica.AvailabilityGroupReplicaData.Connection);
            }
        }

        #endregion

        #region Task 
        /// <summary>
        /// Adds a database to the AvailabilityGroup object
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            //This Validation executes only once
            policy.Expired = true;

            SMO.AvailabilityGroup availabilityGroup = 
                this.availabilityGroupData.PrimaryServer.AvailabilityGroups[this.availabilityGroupData.GroupName];
            if (availabilityGroup == null)
            {
                throw new InvalidAvailabilityGroupException(
                    this.availabilityGroupData.GroupName,
                    this.availabilityGroupData.PrimaryServer.ConnectionContext.TrueName);
            }

            // Update the seeding mode for all the secondary replicas
            // 
            foreach (SMO.AvailabilityReplica replica in availabilityGroup.AvailabilityReplicas)
            {
                if (replica.Role != SMO.AvailabilityReplicaRole.Primary && replica.IsSeedingModeSupported)
                {
                    replica.SeedingMode = availabilityGroupData.WillPerformAutomaticSeeding ? SMO.AvailabilityReplicaSeedingMode.Automatic : SMO.AvailabilityReplicaSeedingMode.Manual;
                    replica.Alter();
                }
            }

            // Need to make sure the AG has create database permission if automatic seeding is selected
            if (availabilityGroupData.WillPerformAutomaticSeeding)
            {
                foreach (AvailabilityGroupReplica availabilityReplica in availabilityGroupData.Secondaries)
                {
                    SMO.Server server = new SMO.Server(availabilityReplica.AvailabilityGroupReplicaData.Connection);
                    server.GrantAvailabilityGroupCreateDatabasePrivilege(this.availabilityGroupData.GroupName);
                }
            }


            foreach (PrimaryDatabaseData databaseData in this.availabilityGroupData.NewAvailabilityDatabases)
            {
                SMO.AvailabilityDatabase availabilityDatabase = new SMO.AvailabilityDatabase(availabilityGroup, databaseData.Name);

                availabilityGroup.AvailabilityDatabases.Add(availabilityDatabase);

                availabilityDatabase.Create();
            }
        }

        /// <summary>
        /// Rollback is not supported
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
        #endregion

    }
}
