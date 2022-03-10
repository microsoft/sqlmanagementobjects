// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Globalization;
using System.IO;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This class validates if the availabe disk-space in the replica
    /// can accomodate the data and log files of the databases present
    /// in the primary server.
    /// </summary>
    public class FreeDiskSpaceValidator : Validator
    {
        /// <summary>
        /// The availability group data.
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// The replica data
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        public FreeDiskSpaceValidator(AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.ValidatingDiskSpace, replica.AvailabilityGroupReplicaData.ReplicaName))
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            this.availabilityGroupData = availabilityGroupData;
            this.replica = replica;
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

        /// <summary>
        /// Validates if the disks in the replica have enough free-space to
        /// accomodate the databases selected to participate in the AG
        /// </summary>
        /// <param name="policy">The execution policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This validation is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;

            double databaseFileSizeOnPrimary;
            double logFileSizeOnPrimary;
            this.availabilityGroupData.GetPrimaryTotalDataFileAndLogSize(out databaseFileSizeOnPrimary, out logFileSizeOnPrimary);

            long freeSpaceOnDataDriveOfReplica = GetFreeSpaceOnReplicaDrive(GetDataDriveLetterOfReplica());
            long freeSpaceOnLogDriveOfReplica = GetFreeSpaceOnReplicaDrive(GetLogDriveLetterOfReplica());
            

            if (freeSpaceOnDataDriveOfReplica < databaseFileSizeOnPrimary)
            {
                throw new InSufficientFreeSpaceForDatabaseFilesException(this.replica.AvailabilityGroupReplicaData.ReplicaName);
            }

            if (freeSpaceOnLogDriveOfReplica < logFileSizeOnPrimary)
            {
                throw new InSufficientFreeSpaceForDatabaseFilesException(this.replica.AvailabilityGroupReplicaData.ReplicaName);
            }
        }

        /// <summary>
        /// Gets the drive letter corresponding to the data-drive
        /// </summary>
        /// <returns>the drive letter</returns>
        private char GetDataDriveLetterOfReplica()
        {
            SMO.Server secondaryReplicaServer = this.replica.GetServer();
            string replicaDefaultDataFileLocation = secondaryReplicaServer.Settings.DefaultFile;
            if (string.IsNullOrEmpty(replicaDefaultDataFileLocation))
            {
                replicaDefaultDataFileLocation = secondaryReplicaServer.MasterDBPath;
            }
            string dataFileRootDirectory = Directory.GetDirectoryRoot(replicaDefaultDataFileLocation);
            return Char.ToUpper(dataFileRootDirectory[0]);
        }

        /// <summary>
        /// Gets the drive letter corresponding to the log-drive
        /// </summary>
        /// <returns>the drive letter</returns>
        private char GetLogDriveLetterOfReplica()
        {
            SMO.Server secondaryReplicaServer = this.replica.GetServer();
            string replicaDefaultLogFileLocation = secondaryReplicaServer.Settings.DefaultLog;
            if (string.IsNullOrEmpty(replicaDefaultLogFileLocation))
            {
                replicaDefaultLogFileLocation = secondaryReplicaServer.MasterDBLogPath;
            }
            string logFileRootDirectory = Directory.GetDirectoryRoot(replicaDefaultLogFileLocation);
            return Char.ToUpper(logFileRootDirectory[0]);
        }

        /// <summary>
        /// Computes the available disk space on the drive corresponding to the drive-letter of the replica
        /// </summary>
        /// <param name="driveLetter">The firve letter</param>
        /// <returns>available disk space in Mb</returns>
        private long GetFreeSpaceOnReplicaDrive(char driveLetter)
        {
            SMO.Server secondaryReplicaServer = this.replica.GetServer();
            DataSet ds = secondaryReplicaServer.ConnectionContext.ExecuteWithResults("exec master..xp_fixeddrives");
            if (ds == null)
            {
                throw new DriveNotFoundOnReplicaException(driveLetter, this.replica.AvailabilityGroupReplicaData.ReplicaName);
            }
            
            bool driveLetterFound = false;
            long freeSpaceOnDataDriveOfReplica = 0;
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                // Each datarow has a format as follows
                // 'Drive'    'MB free'
                //   C          110    
                if (Char.ToUpper(driveLetter) == Char.ToUpper(Convert.ToChar(dr[0])))
                {
                    freeSpaceOnDataDriveOfReplica = Convert.ToInt64(dr[1]);
                    driveLetterFound = true;
                    break;
                }
            }

            if (!driveLetterFound)
            {
                throw new DriveNotFoundOnReplicaException(driveLetter, this.replica.AvailabilityGroupReplicaData.ReplicaName);
            }
            return freeSpaceOnDataDriveOfReplica;
        }
    }
}
