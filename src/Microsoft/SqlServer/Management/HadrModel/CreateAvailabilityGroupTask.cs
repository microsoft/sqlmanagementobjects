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
    /// Task to create an Availability Group
    /// </summary>
    public class CreateAvailabilityGroupTask : HadrTask, IScriptableTask
    {
        /// <summary>
        /// The availability group data
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public CreateAvailabilityGroupTask(AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.CreateAvailabilityGroupText, availabilityGroupData.GroupName))
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
        /// Creates an availability group
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Perform(IExecutionPolicy policy)
        {
            this.UpdateStatus(new TaskEventArgs(this.Name,
                                    string.Format(Resource.CreatingAvailabilityGroup,this.availabilityGroupData.GroupName),
                                    TaskEventStatus.Running));

            SMO.AvailabilityGroup availabilityGroup = 
                new SMO.AvailabilityGroup(
                    this.availabilityGroupData.PrimaryServer,
                    this.availabilityGroupData.GroupName)
                { 
                    ReuseSystemDatabases = this.availabilityGroupData.ReuseSystemDatabases
                };

            availabilityGroup.AutomatedBackupPreference = this.availabilityGroupData.BackupPreference;

              // Check if connected version is 13 or greater before accessing newly added SMO properties.
            if (Utils.IsSql13OrLater(this.availabilityGroupData.PrimaryServer.VersionMajor))
            {
                availabilityGroup.BasicAvailabilityGroup = this.availabilityGroupData.IsBasic;
                availabilityGroup.DatabaseHealthTrigger = this.availabilityGroupData.IsDatabaseHealthTriggerOn;
                availabilityGroup.DtcSupportEnabled = this.availabilityGroupData.IsDtcSupportEnabled;
            }

            if (Utils.IsSql14OrLater(this.availabilityGroupData.PrimaryServer.VersionMajor))
            {
                availabilityGroup.ClusterType = this.availabilityGroupData.ClusterType;
                availabilityGroup.RequiredSynchronizedSecondariesToCommit = this.availabilityGroupData.RequiredSynchronizedSecondariesToCommit;
            }

            if (availabilityGroup.IsSupportedProperty(nameof(availabilityGroup.IsContained)))
            {
                availabilityGroup.IsContained = this.availabilityGroupData.IsContained;
            }

            foreach (var db in this.availabilityGroupData.NewAvailabilityDatabases)
            {
                availabilityGroup.AvailabilityDatabases.Add(new SMO.AvailabilityDatabase(availabilityGroup, db.Name));
            }

            foreach (var replica in this.availabilityGroupData.AvailabilityGroupReplicas)
            {
                var availabilityReplica = new SMO.AvailabilityReplica(availabilityGroup, replica.AvailabilityGroupReplicaData.ReplicaName);
                availabilityReplica.EndpointUrl = replica.AvailabilityGroupReplicaData.EndpointUrl;
                availabilityReplica.FailoverMode = replica.AvailabilityGroupReplicaData.GetFailoverMode(this.availabilityGroupData.ClusterType);
                availabilityReplica.AvailabilityMode = replica.AvailabilityGroupReplicaData.AvailabilityMode;
                availabilityReplica.ConnectionModeInSecondaryRole = replica.ReadableSecondaryRole;
                availabilityReplica.BackupPriority = replica.BackupPriority;
                availabilityReplica.ReadonlyRoutingConnectionUrl = replica.AvailabilityGroupReplicaData.ReadOnlyRoutingUrl ?? string.Empty;
                availabilityReplica.SetLoadBalancedReadOnlyRoutingList(replica.AvailabilityGroupReplicaData.ReadOnlyRoutingList);

                if (Utils.IsSql13OrLater(this.availabilityGroupData.PrimaryServer.VersionMajor))
                {
                    availabilityReplica.SeedingMode = this.availabilityGroupData.PerformDataSynchronization == DataSynchronizationOption.AutomaticSeeding ? SMO.AvailabilityReplicaSeedingMode.Automatic : SMO.AvailabilityReplicaSeedingMode.Manual;
                }

                availabilityGroup.AvailabilityReplicas.Add(availabilityReplica);
            }

            availabilityGroup.Create();

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
