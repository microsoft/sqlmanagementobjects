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
    /// Backup the log on the primary server.
    /// Requires that the backup database was successful.
    /// </summary>
    public class BackupLogTask : HadrTask, IScriptableTask
    {
        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private readonly AvailabilityGroupData _availabilityGroupData;

        /// <summary>
        /// Target Database Name
        /// </summary>
        private readonly string _databaseName;

        /// <summary>
        /// Backup object from smo
        /// </summary>
        private Backup _backup;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">database name</param>
        /// <param name="availabilityGroupData">agData</param>
        public BackupLogTask(string databaseName, AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BackupLogText, databaseName))
        {
            if (String.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("databaseName");
            }

            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this._databaseName = databaseName;
            this._availabilityGroupData = availabilityGroupData;
            this.ScriptingConnections = new List<ServerConnection>
            {
                this._availabilityGroupData.PrimaryServer.ConnectionContext
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
                return this._availabilityGroupData;
            }
        }

        /// <summary>
        /// Used by caller to abort the backup
        /// </summary>
        public void Abort()
        {
            if (this._backup != null)
            {
                this._backup.Abort();
            }
        }

        /// <summary>
        /// method to backup target database log
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            var primaryConnection = this._availabilityGroupData.PrimaryServer.ConnectionContext;
            int timeout = primaryConnection.StatementTimeout;

            // Setting the StatementTimeout to 0 to avoid timing out of the default 600 sec limit
            primaryConnection.StatementTimeout = 0;

            //only try once
            policy.Expired = true;

            try
            {
                // Getting a new SMO Server object everytime it's needed so that it can be freed up(garbage collected) after use
                Smo.Server server = HadrModelUtilities.GetNewSmoServerObject(primaryConnection);

                var backupDevicePath = this.AvailabilityGroupData.GetLogBackupFileFullName(server, this._databaseName);

                this._backup = new Backup()
                {
                    Action = BackupActionType.Log,
                    Database = _databaseName,
                    CompressionOption = BackupCompressionOptions.On,
                    Initialize = true
                };

                PercentCompleteHandler handler = new PercentCompleteHandler(this.Name);

                handler.TaskProgressEventHandler += this.TaskUpdateEventHandler;

                this._backup.PercentCompleteNotification = PercentCompleteHandler.PercentCompleteNotification;

                this._backup.PercentComplete += new PercentCompleteEventHandler(handler.percentCompleteHandler);

                this._backup.Devices.AddDevice(backupDevicePath, DeviceType.File);

                this._backup.SqlBackup(server);

                handler.TaskProgressEventHandler -= this.TaskUpdateEventHandler;
            }
            catch (Exception e)
            {
                throw new BackupLogTaskException(this._databaseName, e);
            }
            finally
            {
                this._backup = null;
                primaryConnection.StatementTimeout = timeout;
            }
        }

        /// <summary>
        /// No Rollback Support for Backup
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
