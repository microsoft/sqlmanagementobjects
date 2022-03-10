// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Task to back up a database
    /// </summary>
    public class BackupDatabaseTask : HadrTask, IScriptableTask
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
        /// Backup object from Smo
        /// </summary>
        private Backup _backup;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">backup database name</param>
        /// <param name="availabilityGroupData">AGdata</param>
        public BackupDatabaseTask(string databaseName, AvailabilityGroupData availabilityGroupData)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BackupDatabaseText, databaseName))
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
        /// Method to back up target Database
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {
            var primaryConnection = this._availabilityGroupData.PrimaryServer.ConnectionContext;
            int timeout = primaryConnection.StatementTimeout;

            // Setting the StatementTimeout to 0 to avoid timing out of the default 600 sec limit
            primaryConnection.StatementTimeout = 0;

            // this task executes only once
            policy.Expired = true;

            try
            {
                // Getting a new SMO Server object everytime it's needed so that it can be freed up(garbage collected) after use
                Smo.Server server = HadrModelUtilities.GetNewSmoServerObject(primaryConnection);

                _backup = new Backup()
                {
                    Action = BackupActionType.Database,
                    Database = _databaseName,
                    CompressionOption = BackupCompressionOptions.On,
                    Initialize = true,
                    SkipTapeHeader = true,
                    FormatMedia = true,
                    // Since we are taking the database backup for a special purpose(adhoc), we set CopyOnly to be true
                    CopyOnly = true
                };

                PercentCompleteHandler handler = new PercentCompleteHandler(this.Name);

                handler.TaskProgressEventHandler += this.TaskUpdateEventHandler;

                this._backup.PercentCompleteNotification = PercentCompleteHandler.PercentCompleteNotification;

                this._backup.PercentComplete += new PercentCompleteEventHandler(handler.percentCompleteHandler);

                this._backup.Devices.AddDevice(this.AvailabilityGroupData.GetDatabaseBackupFileFullName(server, _databaseName), DeviceType.File);

                this._backup.SqlBackup(server);

                handler.TaskProgressEventHandler -= this.TaskUpdateEventHandler;
            }
            catch (FailedOperationException ex)
            {
                // We want to wrap 5133 - DSK_DIRECTORY_FAILED with a better error message
                SqlException sqlEx = this.GetSqlException(ex);

                if (sqlEx == null || sqlEx.Number != 5133)
                    throw;

                throw new BackupDatabaseTaskException(_databaseName, ex);
            }
            finally
            {
                this._backup = null;
                primaryConnection.StatementTimeout = timeout;
            }
        }

        /// <summary>
        /// Not Support For roll back this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the inner SQL exception if any
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private SqlException GetSqlException(Exception ex)
        {
            Exception exception = ex;

            while (exception != null)
            {
                if (exception is SqlException)
                {
                    return (SqlException)exception;
                }
                exception = exception.InnerException;
            }
            return null;
        }
    }
}
