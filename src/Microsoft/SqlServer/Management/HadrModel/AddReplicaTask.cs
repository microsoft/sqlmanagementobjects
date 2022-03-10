// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to Add replicas to the existing availability group
    /// </summary>
    public class AddReplicaTask : HadrTask, IScriptableTask
    {
        /// <summary>
        /// The availability group data
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public AddReplicaTask(AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.AddReplicaText, availabilityGroupData.GroupName))
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
            this.ScriptingConnections = new List<ServerConnection>
                {
                    this.availabilityGroupData.PrimaryServer.ConnectionContext
                };
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
        /// Add an availability group
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            this.UpdateStatus(new TaskEventArgs(this.Name,
                string.Format(Resource.CreatingAvailabilityGroup, this.availabilityGroupData.GroupName),
                TaskEventStatus.Running));

            //Discover the AG
            SMO.Server server = new SMO.Server(this.availabilityGroupData.PrimaryServer.ConnectionContext);

            SMO.AvailabilityGroup availabilityGroup = server.AvailabilityGroups[this.availabilityGroupData.GroupName];

            var newReplicas = this.availabilityGroupData.AvailabilityGroupReplicas.Where(ar => ar.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating).ToList();
            var existingReplicas = this.availabilityGroupData.AvailabilityGroupReplicas.Where(ar => ar.AvailabilityGroupReplicaData.State != AvailabilityObjectState.Creating).ToList();
            foreach (AvailabilityGroupReplica replica in newReplicas)
            {
                SMO.AvailabilityReplica availabilityReplica = new SMO.AvailabilityReplica(availabilityGroup, replica.AvailabilityGroupReplicaData.ReplicaName);
                availabilityReplica.EndpointUrl = replica.AvailabilityGroupReplicaData.EndpointUrl;
                availabilityReplica.FailoverMode = replica.AvailabilityGroupReplicaData.GetFailoverMode(this.availabilityGroupData.ClusterType);
                availabilityReplica.AvailabilityMode = replica.AvailabilityGroupReplicaData.AvailabilityMode;
                availabilityReplica.ConnectionModeInSecondaryRole = replica.ReadableSecondaryRole;
                availabilityReplica.BackupPriority = replica.BackupPriority;
                availabilityReplica.ReadonlyRoutingConnectionUrl = replica.AvailabilityGroupReplicaData.ReadOnlyRoutingUrl ?? string.Empty;
                availabilityReplica.SetLoadBalancedReadOnlyRoutingList(replica.AvailabilityGroupReplicaData.ReadOnlyRoutingList);
                availabilityGroup.AvailabilityReplicas.Add(availabilityReplica);

                if (this.availabilityGroupData.PerformDataSynchronization == DataSynchronizationOption.AutomaticSeeding)
                {
                    availabilityReplica.SeedingMode = SMO.AvailabilityReplicaSeedingMode.Automatic;
                }

                availabilityReplica.Create();
            }

            foreach (AvailabilityGroupReplica replica in existingReplicas)
            {
                if (availabilityGroup.AvailabilityReplicas.Contains(replica.AvailabilityGroupReplicaData.ReplicaName))
                {
                    var smoReplica = availabilityGroup.AvailabilityReplicas[replica.AvailabilityGroupReplicaData.ReplicaName];
                    smoReplica.SetLoadBalancedReadOnlyRoutingList(replica.AvailabilityGroupReplicaData.ReadOnlyRoutingList);
                    smoReplica.Alter();
                }
            }

            this.availabilityGroupData.SqlAvailabilityGroup = availabilityGroup;

            // Expire the policy after success.
            policy.Expired = true;
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
