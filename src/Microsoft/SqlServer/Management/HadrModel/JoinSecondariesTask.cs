// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Join secondaries to the availability group
    /// </summary>
    public class JoinSecondariesTask : HadrTask, IScriptableTask
    {

        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="availabilityGroupData">AG data</param>
        public JoinSecondariesTask(AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.JoinSecondariesText, availabilityGroupData.GroupName))
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;

            this.ScriptingConnections = new List<ServerConnection>();
            foreach (AvailabilityGroupReplica secondary in this.availabilityGroupData.Secondaries)
            {
                this.ScriptingConnections.Add(secondary.AvailabilityGroupReplicaData.Connection);
            }
        }

        /// <summary>
        /// Connections to use for scripting
        /// </summary>
        public List<ServerConnection> ScriptingConnections
        {
            get; 
            private set;
        }

        /// <summary>
        /// Method for joinning all secondaries to AG
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            foreach (AvailabilityGroupReplica secondary in this.availabilityGroupData.Secondaries)
            {
                if (secondary.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating)
                {
                    Smo.Server server = new Smo.Server(secondary.AvailabilityGroupReplicaData.Connection);
                    server.JoinAvailabilityGroup(this.availabilityGroupData.GroupName, availabilityGroupData.ClusterType);

                    // Create DB privilege is required to perform automatic seeding
                    if (this.availabilityGroupData.WillPerformAutomaticSeeding
                        && secondary.AvailabilityGroupReplicaData.AvailabilityMode != AvailabilityReplicaAvailabilityMode.ConfigurationOnly)
                    {
                        server.GrantAvailabilityGroupCreateDatabasePrivilege(this.availabilityGroupData.GroupName);

                        // Occasionally the seeding might have started before granting the privilege
                        // Setting the seeding mode to automatic will trigger the automatic seeding
                        // This step cannot be executed when scripting as the replica hasn't been created
                        //
                        if (server.ConnectionContext.SqlExecutionModes != SqlExecutionModes.CaptureSql)
                        {
                            var replica = availabilityGroupData.PrimaryServer.AvailabilityGroups[this.availabilityGroupData.GroupName].AvailabilityReplicas[secondary.AvailabilityGroupReplicaData.ReplicaName];
                            replica.SeedingMode = AvailabilityReplicaSeedingMode.Automatic;
                            replica.Alter();
                        }
                    }
                }
            }

            //No Exception was thrown during joinAG, Task Succeeds
            policy.Expired = true;
        }

        /// <summary>
        /// No Support for removing joinned AG
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
