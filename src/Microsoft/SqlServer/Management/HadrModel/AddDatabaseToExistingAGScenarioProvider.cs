// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Add a database to an existing Availability Group scenario provider
    /// </summary>
    public class AddDatabaseToExistingAgScenarioProvider : ScenarioProvider<IValidatorProvider, ITasksProvider>
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">AvailabilityGroupData</param>
        public AddDatabaseToExistingAgScenarioProvider(AvailabilityGroupData data)
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

        #region Properties
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

        #region PublicMethods
        /// <summary>
        /// Scenario validators
        /// </summary>
        /// <returns>validationt tasks</returns>
        public override List<Validator> Validators()
        {
            List<Validator> validators = new List<Validator>();

            if (this.AvailabilityGroupData.WillPerformBackupRestore)
            {
                validators.Add(new BackupLocationValidator(this.AvailabilityGroupData));
            }

            if (AvailabilityGroupData.IsBasic)
            {
                validators.Add(new BasicAvailabilityGroupValidator(this.AvailabilityGroupData));
            }

            if (FailoverUtilities.HasDbNeedToBeDecrypted(this.AvailabilityGroupData.PrimaryServer, this.AvailabilityGroupData.NewAvailabilityDatabases))
            {
                validators.Add(new DatabaseMasterKeyValidator(this.AvailabilityGroupData));
            }

            foreach (AvailabilityGroupReplica replicaData in this.AvailabilityGroupData.AvailabilityGroupReplicas)
            {
                if (replicaData.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Secondary)
                {
                    validators.Add(new FreeDiskSpaceValidator(this.AvailabilityGroupData, replicaData));
                    validators.Add(new CreateAvailabilityGroupDatabaseExistenceValidator(this.AvailabilityGroupData, replicaData));
                    validators.Add(new CreateAvailabilityGroupDatabaseFileExistenceValidator(this.AvailabilityGroupData, replicaData));
                    validators.Add(new CreateAvailabilityGroupDatabaseFileCompatibilityValidator(this.AvailabilityGroupData, replicaData));
                }
            }
            return validators;
        }

        /// <summary>
        /// Get the list of tasks the provider supports
        /// </summary>
        /// <returns>List of tasks for this scenario</returns>
        public override List<HadrTask> Tasks()
        {
            var tasks = new List<HadrTask>();

            tasks.Add(new AddDatabaseToExistingAvailabilityGroupTask(this.AvailabilityGroupData));

            if (this.AvailabilityGroupData.Secondaries.Any() && this.AvailabilityGroupData.PerformDataSynchronization != DataSynchronizationOption.AutomaticSeeding)
            {
                foreach (PrimaryDatabaseData databaseData in this.AvailabilityGroupData.NewAvailabilityDatabases)
                {
                    if(this.AvailabilityGroupData.WillPerformBackupRestore)
                    {
                        tasks.Add(new BackupDatabaseTask(databaseData.Name, this.AvailabilityGroupData));

                        foreach(AvailabilityGroupReplica availabilityGroupReplica in this.AvailabilityGroupData.Secondaries)
                        {
                            tasks.Add(new RestoreDatabaseTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));
                        }
                    }

                    tasks.Add(new BackupLogTask(databaseData.Name, this.AvailabilityGroupData));
                    var database = this.AvailabilityGroupData.PrimaryServer.Databases[databaseData.Name];
                    bool credentialNeeded = database.IsAccessible && database.MasterKey != null && databaseData.DBMKPassword != null;
                    foreach (AvailabilityGroupReplica availabilityGroupReplica in this.AvailabilityGroupData.Secondaries)
                    {
                        if (this.AvailabilityGroupData.WillPerformBackupRestore || this.AvailabilityGroupData.WillPerformDatabaseJoin)
                        {
                            tasks.Add(new RestoreLogTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));
                            tasks.Add(new JoinDatabaseToAvailabilityGroupTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));

                            // For each encrypted secondary replica for the current database adds a credential containing the password needed to open a database master key.
                            if (credentialNeeded)
                            {
                                tasks.Add(new AddDBCredentialTask(databaseData.Name, this.AvailabilityGroupData, availabilityGroupReplica));
                            }
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

            return tasks;
        }

        /// <summary>
        /// Scenario rollback task
        /// </summary>
        /// <returns>List of rollback tasks</returns>
        public override List<HadrTask> RollbackTasks()
        {
            return null;
        }
        #endregion
    }
}
