// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the folders needed for database-files of the databases 
    /// exist on the secondary.
    /// </summary>
    public abstract class DatabaseFileCompatibilityValidator : Validator
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
        /// <param name="name">The name of the validator</param>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        protected DatabaseFileCompatibilityValidator(string name, AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(name)
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
            get { return this.availabilityGroupData; }
        }

        /// <summary>
        /// List of databases to validate
        /// </summary>
        public List<PrimaryDatabaseData> DatabasesToValidate { get; protected set; }

        /// <summary>
        /// Validates if a folder necessary for creating the database
        /// exists on the secondary.
        /// </summary>
        /// <param name="policy">The policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This validation is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;
            
            if (availabilityGroupData.PerformDataSynchronization != DataSynchronizationOption.AutomaticSeeding 
                && availabilityGroupData.PerformDataSynchronization != DataSynchronizationOption.Full)
            {
                return;
            }

            var primaryServer = this.availabilityGroupData.PrimaryServer;
            var secondaryServer = this.replica.GetServer();

            var missingFolderLocation = new List<string>();
            var dataFiles = new List<string>();
            var logFiles = new List<string>();

            foreach (PrimaryDatabaseData primaryDatabase in this.DatabasesToValidate)
            {
                var db = primaryServer.Databases[primaryDatabase.Name];
                foreach (Smo.FileGroup fg in db.FileGroups)
                {
                    dataFiles.AddRange(from DataFile df in fg.Files select df.FileName);
                }

                logFiles.AddRange(from LogFile lf in db.LogFiles select lf.FileName);
            }


            if (availabilityGroupData.PerformDataSynchronization == DataSynchronizationOption.AutomaticSeeding)
            {
                var dataFilesNotInDefaultDirectory = dataFiles.Where(file => !file.StartsWith(primaryServer.DefaultFile)).ToArray();
                var logFilesNotInDefaultDirectory = logFiles.Where(file => !file.StartsWith(primaryServer.DefaultLog)).ToArray();

                // For cross platorm AG, if the user want to use automatic seeding, all the source database files must be under the default directories,
                // including data files and log files
                //
                if (AvailabilityGroupData.IsCrossPlatform && (logFilesNotInDefaultDirectory.Any() || dataFilesNotInDefaultDirectory.Any()))
                {
                    // We will only throw this validation exception for primary replica.
                    // Validation on secondaries will only make sense once the user fixed the error on primary server.
                    //
                    if (replica.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Primary)
                    {
                        throw new DatabaseFileNotInDefaultDirectoryException(primaryServer.DefaultFile, primaryServer.DefaultLog, dataFilesNotInDefaultDirectory.Union(logFilesNotInDefaultDirectory));
                    }
                }
                else
                {
                    // There is a change introduced in SQL Server 2017 CTP 2.1
                    // Previously, you will need to make sure the folder structure is the same on all servers
                    // Now you only need to make sure the sub-folder structure if the file is in the default directory
                    //
                    // In SQL Server 2017 RC2, SQL Server introduced a trace flag 9571, when the trace flag is enabled the path requirement will be the same as in SQL Server CTP 2.0 and earlier 
                    //
                    const int traceFlag = 9571;

                    if (Utils.IsSql14OrLater(primaryServer.VersionMajor) && !primaryServer.IsTraceFlagOn(traceFlag, isGlobalTraceFlag: true))
                    {
                        dataFiles = dataFiles.Select(file => file.Replace(primaryServer.DefaultFile, secondaryServer.DefaultFile)).ToList();
                        logFiles = logFiles.Select(file => file.Replace(primaryServer.DefaultLog, secondaryServer.DefaultLog)).ToList();
                    }
                }
            }

            foreach (var file in dataFiles.Union(logFiles))
            {
                var directoryName = PathWrapper.GetDirectoryName(file);

                if (!missingFolderLocation.Contains(directoryName))
                {
                    if (!secondaryServer.ParentDirectoryExists(file))
                    {
                        missingFolderLocation.Add(directoryName);
                    }
                }
            }

            if (missingFolderLocation.Count > 0)
            {
                throw new DatabaseFileLocationMissingOnReplicaException(
                    this.replica.AvailabilityGroupReplicaData.ReplicaName, missingFolderLocation);
            }
        }
    }
    
    internal class Utils
    {

        public static bool IsSql13OrLater(int versionMajor)
        {
            return (versionMajor >= 13);
        }


        public static bool IsSql14OrLater(int versionMajor)
        {
            return (versionMajor >= 14);
        }
    
    }
}
