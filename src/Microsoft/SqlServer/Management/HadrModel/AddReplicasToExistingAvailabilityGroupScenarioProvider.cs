// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This class implements the add database to existing availability group scenario provider
    /// </summary>
    public class AddReplicasToExistingAvailabilityGroupScenarioProvider : ScenarioProvider<IValidatorProvider, ITasksProvider>
    {
        #region properties
        /// <summary>
        /// The availability group data with which the class was
        /// initialized
        /// </summary>
        public AvailabilityGroupData AvailabilityGroupData
        {
            get;
            private set;
        }

        #endregion

        #region ctor
        /// <summary>
        /// Ctor
        /// </summary>
        public AddReplicasToExistingAvailabilityGroupScenarioProvider(AvailabilityGroupData data)
            : base()
        {
            // Make sure data is not null
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            // Verify AlwaysOn is enabled
            if (!data.PrimaryServer.IsHadrEnabled)
            {
                throw new ArgumentException(string.Format(Resource.PrimaryServerNotHadrEnabled, data.PrimaryServer.Name));
            }

            // Verify that the user has permissions to query DMV's
            if (!data.HasViewServerStatePermission(data.PrimaryServer))
            {
                throw new ArgumentException(Resource.UserDoesNotHaveViewServerStatePermission);
            }

            // Verify that the primary is part of quorum.
            if (!data.IsPrimaryInQuorum)
            {
                throw new ArgumentException(Resource.WizardLaunchFailureQuorumLoss);
            }

            this.AvailabilityGroupData = data;
        }
        #endregion

        #region ScenarioProvider

        /// <summary>
        /// Scenario validators
        /// </summary>
        /// <returns>validationt tasks</returns>
        public override List<Validator> Validators()
        {
            List<Validator> validators = new List<Validator>();
            
            #region On-Premise Replica specific validators
            foreach (AvailabilityGroupReplica replicaData in this.AvailabilityGroupData.AvailabilityGroupReplicas)
            {
                if (replicaData.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Secondary &&
                    replicaData.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating)
                {
                    validators.Add(new FreeDiskSpaceValidator(this.AvailabilityGroupData, replicaData));
                    validators.Add(new AddReplicaDatabaseExistenceValidator(this.AvailabilityGroupData, replicaData));
                    validators.Add(new AddReplicaDatabaseFileExistenceValidator(this.AvailabilityGroupData, replicaData));
                    validators.Add(new AddReplicaDatabaseFileCompatibilityValidator(this.AvailabilityGroupData, replicaData));
                }
            }
            #endregion

            #region General Validators
            validators.Add(new CompatibleEncryptionValidator(this.AvailabilityGroupData));

            if(this.AvailabilityGroupData.WillPerformBackupRestore)
            {
                validators.Add(new BackupLocationValidator(this.AvailabilityGroupData));
            }

            validators.Add(new AvailabilityModeValidator(this.AvailabilityGroupData));

            if (AvailabilityGroupData.IsBasic)
            {
                validators.Add(new BasicAvailabilityGroupValidator(this.AvailabilityGroupData));
            }

            if (FailoverUtilities.HasDbNeedToBeDecrypted(this.AvailabilityGroupData.PrimaryServer, this.AvailabilityGroupData.ExistingAvailabilityDatabases))
            {
                validators.Add(new DatabaseMasterKeyValidator(this.AvailabilityGroupData) { ValidateExistingDbMode = true });
            }

            validators.Add(new ListenerConfigurationValidator(this.AvailabilityGroupData));
            #endregion

            return validators;
        }

        /// <summary>
        /// Scenario rollback task
        /// </summary>
        /// <returns>list of rollback task</returns>
        public override List<HadrTask> RollbackTasks()
        {
            return null;
        }

        /// <summary>
        /// Get the list of tasks the provider supports
        /// </summary>
        /// <returns></returns>
        public override List<HadrTask> Tasks()
        {
            List<HadrTask> tasks = new List<HadrTask>();

            foreach (AvailabilityGroupReplica replica in this.AvailabilityGroupData.AvailabilityGroupReplicas)
            {
                List<KeyValuePair<string, bool>> loginsRequiredOnReplica = this.AvailabilityGroupData.GetLoginNames(replica).ToList();

                List<string> loginsToCreateOnReplica = loginsRequiredOnReplica.Where(pair => pair.Value == false).Select(pair => pair.Key).ToList();

                if (loginsToCreateOnReplica.Any())
                {
                    tasks.Add(new CreateLoginTask(replica, loginsToCreateOnReplica));
                }

                tasks.Add(new ConfigureEndpointsTask(replica, loginsRequiredOnReplica.Select(x => x.Key).ToList()));

                if (replica.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating)
                {
                    tasks.Add(new StartAlwaysOnXeventSessionTask(replica));
                }
            }

            tasks.Add(new AddReplicaTask(this.AvailabilityGroupData));

            tasks.Add(new JoinSecondariesTask(this.AvailabilityGroupData));

            if (this.AvailabilityGroupData.DataSecondaries.Any()
                && this.AvailabilityGroupData.PerformDataSynchronization != DataSynchronizationOption.AutomaticSeeding)
            {
                foreach (PrimaryDatabaseData databaseData in this.AvailabilityGroupData.ExistingAvailabilityDatabases)
                {
                    // For each database add a backup database task
                    tasks.Add(new BackupDatabaseTask(databaseData.Name, this.AvailabilityGroupData));

                    // For each secondary replica for the current database add a restore database task
                    foreach (AvailabilityGroupReplica availabilityGroupReplica in this.AvailabilityGroupData.DataSecondaries)
                    {
                        if (availabilityGroupReplica.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating)
                        {
                            tasks.Add(new RestoreDatabaseTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));
                        }
                    }

                    // For each database add a backup log task
                    tasks.Add(new BackupLogTask(databaseData.Name, this.AvailabilityGroupData));
                    var database = this.AvailabilityGroupData.PrimaryServer.Databases[databaseData.Name];
                    bool credentialNeeded = database.IsAccessible && database.MasterKey != null && databaseData.DBMKPassword != null;

                    foreach (AvailabilityGroupReplica availabilityGroupReplica in this.AvailabilityGroupData.DataSecondaries)
                    {
                        if (availabilityGroupReplica.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating)
                        {
                            // For each secondary replica for the current database add a restore log task
                            tasks.Add(new RestoreLogTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));

                            // For each secondary replica for the current database add a join availability group task
                            if (this.AvailabilityGroupData.PerformDataSynchronization == DataSynchronizationOption.JoinOnly)
                            {
                                tasks.Add(new JoinDatabaseToAvailabilityGroupTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));

                                // If current database has the DBMK, adds a credential containing the password of the DBMK for each secondary replica 
                                if (credentialNeeded)
                                {
                                    tasks.Add(new AddDBCredentialTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica) {UseExistingAvailabilityDatabases = true});
                                }
                            }
                        }
                    }

                }
            }

            return tasks;
        }
        #endregion

    }
}
