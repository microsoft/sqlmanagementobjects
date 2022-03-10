// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This class validates that the backup location provided can be used 
    /// by the wizard to take backups and restore on the secondaries.
    /// </summary>
    public class BackupLocationValidator : Validator
    {
        #region Fields

        /// <summary>
        /// The primary-server where the temporary database is created
        /// and backup is taken to the backuplocation.
        /// </summary>
        private SMO.Server primaryServer;

        /// <summary>
        /// The list of secondary servers where the backup of the
        /// temporary database is restored
        /// </summary>
        private List<SMO.Server> onPremiseSecondaryServers;

        /// <summary>
        /// The format of the database-name
        /// </summary>
        private const string DatabaseNameFormat = "BackupLocDb_{0}";

        /// <summary>
        /// The name of the temporary database that will be created, backed-up from primary
        /// and read from the secondary.
        /// </summary>
        private string testDatabaseName;

        /// <summary>
        /// The availability group data
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
        public BackupLocationValidator(AvailabilityGroupData data)
            : base(Resource.ValidatingBackupLocation)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.AvailabilityGroupData = data;
            this.Initialize(data);
        }

        /// <summary>
        /// Initializes the class structures (PrimaryServer, OnPremiseSecondaryServers, BackupLocation)
        /// from the data.
        /// </summary>
        /// <param name="data">The Availability Group Data</param>
        private void Initialize(AvailabilityGroupData data)
        {
            this.primaryServer = data.PrimaryServer;
            this.onPremiseSecondaryServers = new List<SMO.Server>();

            foreach (var replica in data.AvailabilityGroupReplicas)
            {
                if (replica.AvailabilityGroupReplicaData.State == AvailabilityObjectState.Creating
                    && replica.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Secondary)
                {
                    this.onPremiseSecondaryServers.Add(replica.GetServer());
                }
            }

            if (string.IsNullOrWhiteSpace(data.BackupLocationInWindowsFormat) && string.IsNullOrWhiteSpace(data.BackupLocationInLinuxFormat))
            {
                throw new ArgumentException(Resource.BackupLocationNotProvidedErrorMessage);
            }

            this.testDatabaseName = string.Format(DatabaseNameFormat, Guid.NewGuid());
        }

        #endregion

        #region Validate
        /// <summary>
        /// Validates that the primary server can write to a share location, and that all of the secondaries can read from the location.
        /// It does this by creating an empty database on the primary and attempting to back this up to the share.
        /// On each secondary we will attempt to read the header of this backup to verify it exist.
        /// </summary>
        /// <param name="policy">execution policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This task is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;

            var backupFileName = Path.ChangeExtension(this.testDatabaseName, ".bak");

            SMO.Database temporaryDatabase = null;
            try
            {
                // Create the database
                temporaryDatabase = CreateTemporaryDatabaseOnPrimary();

                // Backup the database
                this.BackupTemporaryDatabase(temporaryDatabase, backupFileName);

                // Read the backup from the secondaries.
                this.ValidateBackupOnSecondaries(backupFileName);
            }
            finally
            {
                if (temporaryDatabase != null)
                {
                    temporaryDatabase.DropBackupHistory();
                    temporaryDatabase.Drop();
                }

                // best effort to cleanup any files created
                try
                {
                    if (!string.IsNullOrWhiteSpace(this.AvailabilityGroupData.BackupLocationInWindowsFormat))
                    {
                        File.Delete(SMO.PathWrapper.Combine(this.AvailabilityGroupData.BackupLocationInWindowsFormat, backupFileName));
                    }
                }
                catch (DirectoryNotFoundException)
                { }
                catch (IOException)
                { }
                catch (UnauthorizedAccessException)
                { }
            }
        }

        /// <summary>
        /// Creates a temporary database on the primary server
        /// </summary>
        /// <returns>Smo database object corresponding to the database just created</returns>
        private SMO.Database CreateTemporaryDatabaseOnPrimary()
        {
            this.UpdateStatus(new ValidatorEventArgs("BackupLocationValidator", "CreateTemporaryDatabaseOnPrimary", validatorStatus: "Started"));
            SMO.Database temporaryDatabase = new SMO.Database(this.primaryServer, this.testDatabaseName);
            temporaryDatabase.RecoveryModel = SMO.RecoveryModel.Full;
            temporaryDatabase.UserAccess = SMO.DatabaseUserAccess.Restricted;
            temporaryDatabase.Create();

            this.UpdateStatus(new ValidatorEventArgs("BackupLocationValidator", "CreateTemporaryDatabaseOnPrimary", validatorStatus: "Completed"));
            return temporaryDatabase;
        }

        /// <summary>
        /// Backs up the temporary database to the device path specified
        /// </summary>
        /// <param name="database">The smo database object of database to backup</param>
        /// <param name="backupFileName">The backup file name</param>
        private void BackupTemporaryDatabase(SMO.Database database, string backupFileName)
        {
            var backupDevicePath = SMO.PathWrapper.Combine(this.AvailabilityGroupData.GetBackupPathForServer(this.primaryServer), backupFileName);

            SMO.Backup backup = new SMO.Backup()
            {
                Action = SMO.BackupActionType.Database,
                Database = database.Name,
                CompressionOption = SMO.BackupCompressionOptions.On,
                Initialize = true,
                SkipTapeHeader = true,
                FormatMedia = true
            };

            backup.Devices.AddDevice(backupDevicePath, SMO.DeviceType.File);

            try
            {
                backup.SqlBackup(this.primaryServer);
            }
            catch (Exception e)
            {
                throw new PrimaryCannotWriteToLocationException(this.primaryServer.ConnectionContext.TrueName, backupDevicePath, e);
            }
        }

        /// <summary>
        /// Attempts to read the database backup in the file specified by backupDevicePath
        /// </summary>
        /// <param name="backupFileName">The backup file name</param>
        private void ValidateBackupOnSecondaries(string backupFileName)
        {
            foreach (SMO.Server secondaryServer in this.onPremiseSecondaryServers)
            {
                var backupLocation = SMO.PathWrapper.Combine(this.AvailabilityGroupData.GetBackupPathForServer(secondaryServer), backupFileName);
                SMO.Restore restore = new SMO.Restore();
                restore.Devices.AddDevice(backupLocation, SMO.DeviceType.File);
                DataTable dt = null;
                try
                {
                    if (secondaryServer != null)
                    {
                        dt = restore.ReadBackupHeader(secondaryServer);
                    }
                    else
                    {
                        continue;
                    }
                }
                catch (Exception e)
                {
                    throw new SecondaryCannotReadLocationException(secondaryServer.ConnectionContext.TrueName, backupLocation, e);
                }

                if (dt.Rows.Count != 1 || dt.Rows[0]["DatabaseName"].ToString() != this.testDatabaseName)
                {
                    throw new SecondaryCannotReadLocationException(secondaryServer.ConnectionContext.TrueName, backupLocation);
                }
            }
        }
        #endregion
    }
}
