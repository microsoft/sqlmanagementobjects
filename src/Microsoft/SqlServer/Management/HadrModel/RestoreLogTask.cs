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
    /// Restore the log on a Secondary server.
    /// Depends on the succesful backup of the log on the primary.
    /// </summary>
    public class RestoreLogTask : HadrTask, IScriptableTask
    {

        /// <summary>
        /// Restore object from Smo
        /// </summary>
        private Restore restore;

        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Target Database Name
        /// </summary>
        private readonly string databaseName;

        /// <summary>
        /// The secondary replica in which to restore the logs
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">target database name</param>
        /// <param name="availabilityGroupData">agData</param>
        /// <param name="replica">the secondary replica in which to restore the logs</param>
        public RestoreLogTask(string databaseName, AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.RestoreLogText, databaseName, replica.AvailabilityGroupReplicaData.ReplicaName))
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

            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            this.replica = replica;

            this.availabilityGroupData = availabilityGroupData;

            this.ScriptingConnections = new List<ServerConnection>
            {
                this.replica.AvailabilityGroupReplicaData.Connection
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
                return this.replica;
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
        /// Method to performing restore Log
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            int timeout = this.replica.AvailabilityGroupReplicaData.Connection.StatementTimeout;
            // Setting the StatementTimeout to 0 to avoid timing out of the default 600 sec limit

            this.replica.AvailabilityGroupReplicaData.Connection.StatementTimeout = 0;

            try
            {
                Smo.Server server = HadrModelUtilities.GetNewSmoServerObject(this.replica.AvailabilityGroupReplicaData.Connection);

                this.restore = new Restore()
                {
                    Action = RestoreActionType.Log,
                    Database = this.databaseName,
                    FileNumber = 0,
                    NoRecovery = true,
                };

                var logBackupFileLocation = this.AvailabilityGroupData.GetLogBackupFileFullName(server, databaseName);

                PercentCompleteHandler handler = new PercentCompleteHandler(this.Name);

                handler.TaskProgressEventHandler += this.TaskUpdateEventHandler;

                this.restore.PercentCompleteNotification = PercentCompleteHandler.PercentCompleteNotification;

                this.restore.PercentComplete += new PercentCompleteEventHandler(handler.percentCompleteHandler);

                this.restore.Devices.AddDevice(logBackupFileLocation, DeviceType.File);

                this.restore.SqlRestore(server);

                handler.TaskProgressEventHandler -= this.TaskUpdateEventHandler;

                // restore succeed
                policy.Expired = true;
            }
            catch (Exception e)
            {
                throw new RestoreLogTaskException(this.databaseName, e);
            }
            finally
            {
                this.restore = null;

                this.replica.AvailabilityGroupReplicaData.Connection.StatementTimeout = timeout;
            }
        }

        /// <summary>
        /// Not Support for rolling back this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }
    }
}
