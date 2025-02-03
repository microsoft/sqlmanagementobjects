// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Create a new availability group scenario provider
    /// </summary>
    public class CreateAvailabilityGroupScenarioProvider : ScenarioProvider<IValidatorProvider, ITasksProvider>
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

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">AvailabilityGroupData</param>
        public CreateAvailabilityGroupScenarioProvider(AvailabilityGroupData data)
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

            foreach (AvailabilityGroupReplica replicaData in this.AvailabilityGroupData.DataSecondaries)
            {
                validators.Add(new FreeDiskSpaceValidator(this.AvailabilityGroupData, replicaData));
                validators.Add(new CreateAvailabilityGroupDatabaseExistenceValidator(this.AvailabilityGroupData, replicaData));
                validators.Add(new CreateAvailabilityGroupDatabaseFileExistenceValidator(this.AvailabilityGroupData, replicaData));
                validators.Add(new CreateAvailabilityGroupDatabaseFileCompatibilityValidator(this.AvailabilityGroupData, replicaData));
            }

            #endregion

            #region General Validators

            validators.Add(new CompatibleEncryptionValidator(this.AvailabilityGroupData));

            if (this.AvailabilityGroupData.WillPerformBackupRestore)
            {
                validators.Add(new BackupLocationValidator(this.AvailabilityGroupData));
            }

            validators.Add(new AvailabilityModeValidator(this.AvailabilityGroupData));

            if (FailoverUtilities.HasDbNeedToBeDecrypted(this.AvailabilityGroupData.PrimaryServer, this.AvailabilityGroupData.NewAvailabilityDatabases))
            {
                validators.Add(new DatabaseMasterKeyValidator(this.AvailabilityGroupData));
            }

            if (AvailabilityGroupData.IsBasic)
            {
                validators.Add(new BasicAvailabilityGroupValidator(this.AvailabilityGroupData));
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

            foreach(AvailabilityGroupReplica replica in this.AvailabilityGroupData.AvailabilityGroupReplicas)
            {
                List<KeyValuePair<string, bool>> loginsRequiredOnReplica = this.AvailabilityGroupData.GetLoginNames(replica).ToList();

                List<string> loginsToCreateOnReplica = loginsRequiredOnReplica.Where(pair => pair.Value == false).Select(pair => pair.Key).ToList();

                if(loginsToCreateOnReplica.Any())
                {
                    tasks.Add(new CreateLoginTask(replica, loginsToCreateOnReplica));
                }

                tasks.Add(new ConfigureEndpointsTask(replica, loginsRequiredOnReplica.Select(x => x.Key).ToList()));

                tasks.Add(new StartAlwaysOnXeventSessionTask(replica));
            }

            tasks.Add(new CreateAvailabilityGroupTask(this.AvailabilityGroupData));

            tasks.Add(new WaitForAvailabilityGroupOnlineTask(this.AvailabilityGroupData));

            if (this.AvailabilityGroupData.AvailabilityGroupListener != null)
            {
                tasks.Add(new CreateAvailabilityGroupListenerTask(this.AvailabilityGroupData));
            }

            tasks.Add(new JoinSecondariesTask(this.AvailabilityGroupData));

            if(this.AvailabilityGroupData.ClusterType == AvailabilityGroupClusterType.Wsfc)
            {
                tasks.Add(new AvailabilityGroupQuorumValidationTask(this.AvailabilityGroupData));
            }

            #region database tasks

            if (this.AvailabilityGroupData.DataSecondaries.Any() 
                && this.AvailabilityGroupData.PerformDataSynchronization != DataSynchronizationOption.AutomaticSeeding)
            {
                AddInitialAgSeedingTasks(tasks);
                foreach (PrimaryDatabaseData databaseData in this.AvailabilityGroupData.NewAvailabilityDatabases)
                {
                    tasks.Add(new BackupDatabaseTask(databaseData.Name, this.AvailabilityGroupData));
                    foreach (AvailabilityGroupReplica availabilityGroupReplica in this.AvailabilityGroupData.DataSecondaries)
                    {
                        tasks.Add(new RestoreDatabaseTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));
                    }

                    tasks.Add(new BackupLogTask(databaseData.Name, this.AvailabilityGroupData));
                    var database = this.AvailabilityGroupData.PrimaryServer.Databases[databaseData.Name];
                    bool credentialNeeded = database.IsAccessible && database.MasterKey != null && databaseData.DBMKPassword != null;
                    foreach (AvailabilityGroupReplica availabilityGroupReplica in this.AvailabilityGroupData.DataSecondaries)
                    {
                        // For each secondary replica for the current database add a restore log action
                        tasks.Add(new RestoreLogTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));

                        // For each secondary replica for the current database add a join availability group action
                        tasks.Add(new JoinDatabaseToAvailabilityGroupTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));

                        // For each encrypted secondary replica for the current database adds a credential containing the password needed to open a database master key.
                        if (credentialNeeded)
                        {
                            tasks.Add(new AddDBCredentialTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));
                        }
                    }

                    // For the primary adds a credential containing the password needed to open a database master key.
                    if (credentialNeeded)
                    {
                        var primary = this.AvailabilityGroupData.AvailabilityGroupReplicas.Single(replica => replica.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Primary);
                        tasks.Add(new AddDBCredentialTask(databaseData.Name, this.AvailabilityGroupData, primary));
                    }
                }
            }

            #endregion

            return tasks;
        }

        private void AddInitialAgSeedingTasks(IList<HadrTask> tasks)
        {
            // For a contained AG we have to backup and restore the <agname>_master and <agname>_msdb and join them to the AG
            // before joining any user databases
            if (AvailabilityGroupData.IsContained)
            {
                foreach (var systemDatabase in new[] { $"{AvailabilityGroupData.GroupName}_master", $"{AvailabilityGroupData.GroupName}_msdb" })
                {
                    tasks.Add(new BackupDatabaseTask(systemDatabase, AvailabilityGroupData));
                    foreach (var availabilityGroupReplica in AvailabilityGroupData.DataSecondaries)
                    {
                        tasks.Add(new RestoreDatabaseTask(systemDatabase, AvailabilityGroupData, availabilityGroupReplica));
                    }
                    tasks.Add(new BackupLogTask(systemDatabase, AvailabilityGroupData));

                    foreach (var availabilityGroupReplica in AvailabilityGroupData.DataSecondaries)
                    {
                        // For each secondary replica for the current database add a restore log action
                        tasks.Add(new RestoreLogTask(systemDatabase, AvailabilityGroupData, availabilityGroupReplica));

                        // For each secondary replica for the current database add a join availability group action
                        tasks.Add(new JoinDatabaseToAvailabilityGroupTask(systemDatabase, AvailabilityGroupData, availabilityGroupReplica));
                    }
                }
            }
        }
        #endregion
    }
}
