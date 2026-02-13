// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Restore a database on the secondary server.
    /// Requires that the backup was successful. Will not move database device files, and so requires that the
    /// the directory layout on the secondary is compatible with the primary.
    /// Will not run if the database already exists on the secondary
    /// </summary>
    public class RestoreDatabaseTask : HadrTask, IScriptableTask
    {
        /// <summary>
        /// Restore Object from smo
        /// </summary>
        private Restore restore;

        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;
        
        /// <summary>
        /// PrimaryReplica ServerConnection
        /// </summary>
        private ServerConnection secondaryConnection;

        /// <summary>
        /// Target Database Name
        /// </summary>
        private string databaseName;

        /// <summary>
        /// AvailabilityGroupReplica for restoring database
        /// </summary>
        private AvailabilityGroupReplica availabilityGroupReplica;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">database Name</param>
        /// <param name="availabilityGroupData">AGdata</param>
        /// <param name="availabilityGroupReplica">Replica object for restoring</param>
        public RestoreDatabaseTask(string databaseName,
            AvailabilityGroupData availabilityGroupData,
            AvailabilityGroupReplica availabilityGroupReplica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.RestoreDatabaseText, databaseName, availabilityGroupReplica.AvailabilityGroupReplicaData.ReplicaName))
        {
            if (String.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("databaseName");
            }

            this.databaseName = databaseName;

            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            if (availabilityGroupReplica == null)
            {
                throw new ArgumentNullException("availabilityGroupReplica");
            }

            this.availabilityGroupReplica = availabilityGroupReplica;

            this.availabilityGroupData = availabilityGroupData;

            this.secondaryConnection = this.availabilityGroupReplica.AvailabilityGroupReplicaData.Connection;

            this.ScriptingConnections = new List<ServerConnection>
            {
                this.secondaryConnection
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
        /// The replica data for the replica on which the restore happens
        /// </summary>
        public AvailabilityGroupReplica ReplicaData
        {
            get
            {
                return this.availabilityGroupReplica;
            }
        }

        /// <summary>
        /// Used by caller to abort the restore
        /// </summary>
        public void Abort()
        {
            if (this.restore != null)
            {
                this.restore.Abort();
            }
        }

        /// <summary>
        /// Method to restoring a database
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            int timeout = this.secondaryConnection.StatementTimeout;

            // Setting the StatementTimeout to 0 to avoid timing out of the default 600 sec limit
            this.secondaryConnection.StatementTimeout = 0;

            try
            {
                // Getting a new SMO Server object everytime it's needed so that it can be freed up(garbage collected) after use
                Smo.Server server = HadrModelUtilities.GetNewSmoServerObject(this.secondaryConnection);

                var backupDeviceLocation = this.AvailabilityGroupData.GetDatabaseBackupFileFullName(server, databaseName);
                this.restore = new Restore()
                {
                    Action = RestoreActionType.Database,
                    Database = databaseName,
                    FileNumber = 0,
                    ReplaceDatabase = false,
                    NoRecovery = true,
                };

                PercentCompleteHandler handler = new PercentCompleteHandler(this.Name);

                handler.TaskProgressEventHandler += this.TaskUpdateEventHandler;

                this.restore.PercentCompleteNotification = PercentCompleteHandler.PercentCompleteNotification;

                this.restore.PercentComplete += new PercentCompleteEventHandler(handler.percentCompleteHandler);

                this.restore.Devices.AddDevice(backupDeviceLocation, DeviceType.File);

                // Bug Fix 11591498 - Only relocate the file when the replica hosting server and the primary server are on different OS platforms
                //
                if (server.HostPlatform != this.availabilityGroupData.PrimaryServer.HostPlatform)
                {
                    var db = this.AvailabilityGroupData.PrimaryServer.Databases[databaseName];
                    foreach (FileGroup fg in db.FileGroups)
                    {
                        foreach (DataFile datafile in fg.Files)
                        {
                            var relocateFile = new RelocateFile(datafile.Name, PathWrapper.Combine(server.DefaultFile, Path.GetFileName(datafile.FileName)));
                            restore.RelocateFiles.Add(relocateFile);
                        }
                    }

                    foreach (LogFile logFile in db.LogFiles)
                    {
                        var relocateFile = new RelocateFile(logFile.Name, PathWrapper.Combine(server.DefaultLog, Path.GetFileName(logFile.FileName)));
                        restore.RelocateFiles.Add(relocateFile);
                    }
                }
                this.restore.SqlRestore(server);

                handler.TaskProgressEventHandler -= this.TaskUpdateEventHandler;

                // restore succeeded
                policy.Expired = true;
            }
            catch (Exception e)
            {
                throw new RestoreDatabaseTaskException(this.databaseName, e);
            }
            finally
            {
                this.restore = null;

                this.secondaryConnection.StatementTimeout = timeout;
            }
        }

        /// <summary>
        /// Not support for rolling back this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
