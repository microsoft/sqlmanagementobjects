// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.SqlServer.Management.Common;
using System.Collections.Specialized;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Database Restore Plan is a sequence of Database Restore
    /// operations which will recover a Database to a particular
    /// state in a point in time.
    /// </summary>
    public class RestorePlan
    {
        #region constructor

        public RestorePlan(Server server)
        {
            this.Server = server;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="RestorePlan"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="databaseName">Name of the database.</param>
        public RestorePlan(Server server, String databaseName)
        {
            this.Server = server;
            this.DatabaseName = databaseName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestorePlan"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        public RestorePlan(Database database)
            : this(database.GetServerObject(), database.Name)
        {
        }

        #endregion

        #region Private variables

        private RestoreOptions restoreOptions;

        #endregion

        #region properties
        private Server server;
        /// <summary>
        /// Server where the Restore plan executes.
        /// </summary>
        /// <value>The server.</value>
        public Server Server
        {
            get { return this.server; }
            private set
            {
                if (null == value)
                {
                    throw new FailedOperationException(ExceptionTemplates.InitObject, this,
                                                        new ArgumentNullException("Server"));
                }
                this.server = value;
                this.Server.ExecutionManager.ConnectionContext.StatementTimeout = 0;
            }
        }

        private String databaseName;
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>The name of the database.</value>
        public String DatabaseName
        {
            get
            {
                return this.databaseName;
            }

            set
            {
                if (this.RestoreOperations != null)
                {
                    foreach (Restore res in this.RestoreOperations)
                    {
                        res.Database = value;
                    }
                }
                this.databaseName = value;
            }
        }

        private RestoreActionType restoreAction = RestoreActionType.Database;
        /// <summary>
        /// Gets and sets the type of the Restore action:
        /// Database,File,Log,Page
        /// </summary>
        /// <value>The restore action.</value>
        public RestoreActionType RestoreAction
        {
            get { return restoreAction; }
            set { restoreAction = value; }
        }

        /// <summary>
        /// Gets or sets the tail log backup operation.
        /// </summary>
        /// <value>The tail log backup operation.</value>
        public Backup TailLogBackupOperation
        {
            get;
            set;
        }

        private List<Restore> restoreOperations = new List<Restore>();
        /// <summary>
        /// Gets or sets the restore operations.
        /// </summary>
        /// <value>The restore operations.</value>
        public List<Restore> RestoreOperations
        {
            get { return restoreOperations; }
        }

        private AsyncStatus asyncStatus = new AsyncStatus(ExecutionStatus.Inactive, null);
        /// <summary>
        /// Gets the status of most recent asynchronous operation
        /// including possible errors.
        /// </summary>
        /// <value>The async status.</value>
        public AsyncStatus AsyncStatus
        {
            get { return this.asyncStatus; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [close existing connections].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [close existing connections]; otherwise, <c>false</c>.
        /// </value>
        public bool CloseExistingConnections
        {
            get;
            set;
        }

        // This variable indicates the number of backup/restore operation completed - 1
        private int processCompleted = -1;
        //this variable indicated the number of process expected to be completed
        private int maxProcessCompleted = 0;
        #endregion

        #region Public Events
        /// <summary>
        /// Occurs when [Server sends percent complete information].
        /// </summary>
        public event PercentCompleteEventHandler PercentComplete;

        /// <summary>
        /// Occurs when [Next media needs to be loaded].
        /// </summary>
        public event ServerMessageEventHandler NextMedia;

        /// <summary>
        /// Occurs when [Restore operation gets completed].
        /// </summary>
        public event ServerMessageEventHandler Complete;

        /// <summary>
        /// Occurs when [Server sends information].
        /// </summary>
        public event ServerMessageEventHandler Information;

        /// <summary>
        /// Occurs when [next restore].
        /// </summary>
        public event NextRestoreEventHandler NextRestore;
        #endregion

        #region Execute & Script

        /// <summary>
        /// Verifies and executes the Restore Plan.
        /// <exeception>
        /// InvalidRestorePlanException is thrown when verification fails.
        /// </exeception>
        /// </summary>
        public void Execute()
        {
            bool tailLogBackupNotTaken = true;
            processCompleted = -1;
            maxProcessCompleted = this.RestoreOperations.Count - 1;
            if(this.TailLogBackupOperation != null)
            {
                maxProcessCompleted++;
            }
            this.executingRestoreOperationIndex = 0;
            StringCollection script = new StringCollection();
            try
            {
                DatabaseUserAccess? dbUserAccessState = this.ScriptPreRestore(script);
                this.server.ExecutionManager.ExecuteNonQueryWithMessage(script, new ServerMessageEventHandler(OnInfoMessage), true);
                script.Clear();
                int count = 1;
                bool pageRestore = (restoreOperations[0].Action == RestoreActionType.OnlinePage);
                foreach (Restore restoreObj in restoreOperations)
                {
                    //Condition is true When we encounter the first Log backup after restoring from Full and Differential backups in Page Restore.
                    //In this case backup of tailLog is taken and tailLogBackupNotTaken is set to false so that tail log is not repetitively
                    //backed up for subsequent log restores.
                    if (pageRestore && restoreObj.Action == RestoreActionType.Log
                        && tailLogBackupNotTaken && this.TailLogBackupOperation != null)
                    {
                        StringCollection backupLogScript = new StringCollection();
                        backupLogScript.Add(TailLogBackupOperation.Script(this.server));
                        this.server.ExecutionManager.ExecuteNonQueryWithMessage(backupLogScript, new ServerMessageEventHandler(OnInfoMessage), true);
                        tailLogBackupNotTaken = false;
                    }

                    StringCollection restoreScript = restoreObj.Script(this.server);

                    NextRestoreEventArgs nextRestoreEventArgs = (restoreObj.BackupSet != null) ?
                        new NextRestoreEventArgs(restoreObj.BackupSet.Name, restoreObj.BackupSet.Description, restoreObj.Devices[0].Name, count) :
                        new NextRestoreEventArgs(restoreObj.Devices[0].Name, string.Empty, restoreObj.Devices[0].Name, count);
                    count++;
                    // Prompt Event
                    if (this.NextRestore != null)
                    {
                        this.NextRestore(this, nextRestoreEventArgs);
                    }
                    if (nextRestoreEventArgs.Continue)
                    {
                        this.server.ExecutionManager.ExecuteNonQueryWithMessage(restoreScript, new ServerMessageEventHandler(OnInfoMessage), true);
                    }
                    else
                    {
                        throw new SmoException(ExceptionTemplates.OperationCancelledByUser);
                    }
                }

                ScriptPostRestore(script, dbUserAccessState);
                this.server.ExecutionManager.ExecuteNonQueryWithMessage(script, new ServerMessageEventHandler(OnInfoMessage), true);
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                {
                    var se = e.InnerException as SqlException;
                    if (se != null)
                    {
                        bool completeNotificationFound = false;
                        foreach (SqlError err in se.Errors)
                        {
                            if (err.Number == 3014 /* Complete notification */ )
                            {
                                completeNotificationFound = true;
                            }
                        }
                        if (!completeNotificationFound)
                        {
                            throw e;
                        }
                    }
                }
                else
                {
                    throw e;
                }
            }
            finally
            {
                if (!this.server.ExecutionManager.Recording && restoreOperations[0].Action != RestoreActionType.OnlinePage)
                {
                    //Refresh the destination Database node in Object Explorer
                    RefreshOENode(this.DatabaseName);
                }
                if (!this.server.ExecutionManager.Recording && restoreOperations[0].Action != RestoreActionType.OnlinePage && this.TailLogBackupOperation != null)
                {
                    //Refresh the source database node since it may go into restoring state after tail log backup
                    RefreshOENode(TailLogBackupOperation.Database);
                }

            }
        }

        /// <summary>
        /// The method Raises a database event on Smo that causes Object
        /// Explorer node corresponding to string databaseName to refresh
        /// </summary>
        private void RefreshOENode(string databaseName)
        {
            try
            {
                this.server.Databases.Refresh();

                if (this.server.Databases[databaseName] != null)
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(this.server,
                                new DatabaseEventArgs(this.server.Databases[databaseName].Urn,
                                                this.server.Databases[databaseName],
                                                databaseName,
                                                DatabaseEventType.Restore));
                    }
                }
            }
            catch { /*Leaving the catch block empty */}
        }

        /// <summary>
        /// Verifies and executes the Restore Plan async.
        /// </summary>
        public void ExecuteAsync()
        {
            this.executingRestoreOperationIndex = 0;
            StringCollection script = this.Script();
            try
            {
                // Register handle for async query completion event
                this.server.ExecutionManager.ExecuteNonQueryCompleted += new ExecuteNonQueryCompletedEventHandler(OnExecuteNonQueryCompleted);
                this.asyncStatus = new AsyncStatus(ExecutionStatus.InProgress, null);

                // if any notification is requested by the clients, hook into the server's notification messages
                if (null != Complete || null != PercentComplete || null != Information || null != NextMedia)
                {
                    this.server.ExecutionManager.ExecuteNonQueryWithMessageAsync(script, new ServerMessageEventHandler(OnInfoMessage), true);
                }
                else
                {
                    this.server.ExecutionManager.ExecuteNonQueryAsync(script);
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.RestoreAsync, this.server, e);
            }
        }

        /// <summary>
        /// Verifies the Restore plan and scripts the operation.
        /// </summary>
        /// <returns>StringColection of the T-SQL script for the operation. </returns>
        public StringCollection Script()
        {
            if (this.Server.Version.Major > 8)
            {
                Verify(false);
            }

            StringCollection script = new StringCollection();
            DatabaseUserAccess? dbUserAccess = ScriptPreRestore(script);
            ScriptRestore(script);
            ScriptPostRestore(script, dbUserAccess);

            return script;
        }

        /// <summary>
        /// Scripts the pre restore.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns></returns>
        private DatabaseUserAccess? ScriptPreRestore(StringCollection script)
        {
            bool isManagedInstance = this.Server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance;

            script.Add(Scripts.USEMASTER);

            Nullable<DatabaseUserAccess> dbUserAccess = null;
            bool backupScriptAdded = false;

            Database db = this.Server.Databases[this.DatabaseName];

            if (db != null && this.CloseExistingConnections && CanDropExistingConnections(db.Name) &&
                db.DatabaseOptions.UserAccess != DatabaseUserAccess.Single)
            {
                dbUserAccess = db.DatabaseOptions.UserAccess;
                // If source Database != target Database first take the T-Log backup and then set destination to single- user mode
                // This prevents the destination database to go into single user mode if T-Log backup fails on source database.
                // However, taking T-Log backups is not supported on Managed Instances, so skip this block of code altogether.
                //
                if (!isManagedInstance)
                {
                    if (restoreOperations[0].Action != RestoreActionType.OnlinePage && this.TailLogBackupOperation != null && this.TailLogBackupOperation.Database != this.DatabaseName)
                    {
                        script.Add(TailLogBackupOperation.Script(this.server));
                        backupScriptAdded = true;
                    }
                    // force the database to be single user
                    string query = string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET {1} WITH ROLLBACK IMMEDIATE", this.DatabaseName, "SINGLE_USER");
                    script.Add(query);
                }
            }

            //tail Log script is only generated if scenario is database restore.
            //In Page Restore scenario tail log is backed up after the full and differential database backups are applied on given page.
            // Taking T-Log backups is not supported on Managed Instances, so skip this block of code altogether.
            //
            if (!isManagedInstance && !backupScriptAdded && restoreOperations[0].Action != RestoreActionType.OnlinePage && this.TailLogBackupOperation != null)
            {
                script.Add(TailLogBackupOperation.Script(this.server));
            }
            return dbUserAccess;
        }

        /// <summary>
        /// Scripts the restore.
        /// </summary>
        /// <param name="script">The script.</param>
        private void ScriptRestore(StringCollection script)
        {
            bool tailLogBackupNotTaken = true;
            bool pageRestore = (restoreOperations[0].Action == RestoreActionType.OnlinePage); 
            foreach (Restore restoreObj in restoreOperations)
            {
                //Condition is true When we encounter the first Log backup after creating script for Full and Differential backups in Page Restore.
                //In this case backup script of tailLog is generated and tailLogBackupNotTaken is set to false so that taillog backup script is not repetitively
                //generated up for subsequent log restores.
                if (pageRestore && restoreObj.Action == RestoreActionType.Log &&
                    tailLogBackupNotTaken && this.TailLogBackupOperation != null)
                {
                    script.Add(TailLogBackupOperation.Script(this.server));
                    tailLogBackupNotTaken = false;
                }

                StringCollection strcoll = restoreObj.Script(this.server);
                foreach (String query in strcoll)
                {
                    if (!string.IsNullOrEmpty(query))
                    {
                        script.Add(query);
                    }
                }
            }
        }

        /// <summary>
        /// Scripts the post restore.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="dbUserAccess">The db user access.</param>
        private void ScriptPostRestore(StringCollection script, DatabaseUserAccess? dbUserAccess)
        {
            // Not converting back the database from SINGLE_USER mode to previous "restrict access" if target database is desired to be
            // in restoring state and close existing connections is enabled.
            if (dbUserAccess != null && this.restoreOptions!=null && this.restoreOptions.RecoveryState != DatabaseRecoveryState.WithNoRecovery)
            {
                string oldUserAccessStr = "MULTI_USER";
                if (dbUserAccess == DatabaseUserAccess.Restricted)
                {
                    oldUserAccessStr = "RESTRICTED_USER";
                }
                string query = string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE [{0}] SET {1}", this.DatabaseName, oldUserAccessStr);
                script.Add(query);
            }
        }

        /// <summary>
        /// Checks if the given database supports dropping connections. 
        /// The ones that are in 9.0 (SQL 2005) compatibility mode, or in a status that is not normal 
        /// don't support dropping connections. 
        /// </summary>
        /// <param name="dbName">The database.</param>
        /// <returns>
        /// <c>true</c> if this instance can drop existing connections of the specified database; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDropExistingConnections(string dbName)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                return false;
            }
            Database database = this.Server.Databases[dbName];
            if (database == null)
            {
                return false;
            }
            CompatibilityLevel commpatibilityLevel = database.GetCompatibilityLevel();
            if (commpatibilityLevel < CompatibilityLevel.Version90)
            {
                return false;
            }
            if (database.Status != DatabaseStatus.Normal)
            {
                return false;
            }
            return true;
        }

        
        #endregion

        #region Eventhandlers

        /// <summary>
        /// Called when [execute non query completed].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="Microsoft.SqlServer.Management.Smo.ExecuteNonQueryCompletedEventArgs"/> instance containing the event data.</param>
        private void OnExecuteNonQueryCompleted(object sender, ExecuteNonQueryCompletedEventArgs args)
        {
            // Save results
            this.asyncStatus = new AsyncStatus(args.ExecutionStatus, args.LastException);

            // Unregister event
            this.server.ExecutionManager.ExecuteNonQueryCompleted -= new ExecuteNonQueryCompletedEventHandler(OnExecuteNonQueryCompleted);
            if (!server.ExecutionManager.Recording)
            {
                if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                {
                    SmoApplication.eventsSingleton.CallDatabaseEvent(server,
                            new DatabaseEventArgs(server.Databases[this.DatabaseName].Urn,
                                                server.Databases[this.DatabaseName],
                                                this.DatabaseName,
                                                DatabaseEventType.Restore));
                }
            }
        }

        /// <summary>
        /// Called when Sever sends messages .
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="Microsoft.SqlServer.Management.Common.ServerMessageEventArgs"/> instance containing the event data.</param>
        private void OnInfoMessage(object sender, ServerMessageEventArgs e)
        {
            switch (e.Error.Number)
            {
                case 3211:
                    // Percent complete notification
                    if (null != PercentComplete)
                    {
                        string message = string.Empty;
                        if (this.TailLogBackupOperation != null && processCompleted == -1)
                        {
                            message = ExceptionTemplates.BackupTailLog;
                        }
                        else if (this.TailLogBackupOperation != null && this.RestoreOperations[processCompleted].BackupSet == null)
                        {
                            message = ExceptionTemplates.Restoring(ExceptionTemplates.TailLog);
                        }
                        else if (this.TailLogBackupOperation != null)
                        {
                            message = ExceptionTemplates.Restoring(this.RestoreOperations[processCompleted].BackupSet.Name);
                        }
                        else
                        {
                            message = ExceptionTemplates.Restoring(this.RestoreOperations[processCompleted + 1].BackupSet.Name);
                        }
                        
                        this.PercentComplete(this, new PercentCompleteEventArgs(e.Error, message));
                    }
                    break;
                case 3014:
                    // Complete notification
                    processCompleted++;
                    if (null != Complete && processCompleted == maxProcessCompleted)
                    {
                        this.Complete(this, e);
                    }
                    break;
                case 3247: goto case 4028;
                case 3249: goto case 4028;
                case 4027: goto case 4028;
                case 4028:
                    // Mount next volume/tape notification
                    if (null != NextMedia)
                    {
                        this.NextMedia(this, e);
                    }
                    break;

                default:
                    // information events
                    if (null != Information)
                    {
                        this.Information(this, e);
                        if (e != null)
                        {
                            this.executingRestoreOperationIndex++;
                        }
                    }
                    break;
            }
        }

        private int executingRestoreOperationIndex = 0;
        #endregion

        #region Verify Methods
        /// <summary>
        /// Verifies the restore plan.
        /// InvalidRestorePlanException is thrown when verification fails.
        /// Supported only for SQL Server 2005 or later.
        /// </summary>
        /// <param name="checkBackupMediaIntegrity">if set to <c>true</c> [check backup media integrity].</param>
        public void Verify(bool checkBackupMediaIntegrity)
        {
            //Version check
            if (this.server.Version.Major < 9)
            {
                throw new UnsupportedVersionException(ExceptionTemplates.UnsupportedServerVersion);
            }
            if (RestoreOperations == null || RestoreOperations.Count == 0)
            {
                throw new InvalidRestorePlanException(this, ExceptionTemplates.EmptyRestorePlan);
            }

            if (string.IsNullOrEmpty(this.databaseName))
            {
                this.databaseName = this.RestoreOperations[0].Database;
            }

            foreach (Restore res in this.RestoreOperations)
            {
                if (this.Server.StringComparer.Compare(this.databaseName, res.Database) != 0)
                {
                    throw new InvalidRestorePlanException(res, ExceptionTemplates.MultipleDatabaseSelectedToRestore);
                }
            }

            //Check that only the last restore operation can be WITH RECOVERY 
            var resOpRecoveryCount = (from restoreObj in RestoreOperations
                                      where restoreObj.NoRecovery == false
                                      select restoreObj).Count();
            if (resOpRecoveryCount > 1 ||
                (resOpRecoveryCount == 1 && restoreOperations[restoreOperations.Count - 1].NoRecovery != false))
            {
                throw new InvalidRestorePlanException(this, ExceptionTemplates.OnlyLastRestoreWithNoRecovery);
            }

            //For Database restore, first restore operation should be of Full backup 
            if (this.RestoreAction == RestoreActionType.Database
                && RestoreOperations[0].BackupSet.BackupSetType != BackupSetType.Database)
            {
                throw new InvalidRestorePlanException(this, ExceptionTemplates.NoFullBackupSelected);
            }

            //Verify the sequence 
            if (this.RestoreAction == RestoreActionType.Database || this.RestoreAction == RestoreActionType.Log || this.RestoreAction == RestoreActionType.OnlinePage)
            {
                for (int i = 0; i < (RestoreOperations.Count - 1); i++)
                {
                    string errMsg;
                    Object errSource;
                    Decimal stopAtLsn = 0m;
                    if (RestoreOperations[i].BackupSet != null && RestoreOperations[i + 1].BackupSet != null)
                    {
                        bool inSequence = BackupSet.IsBackupSetsInSequence(RestoreOperations[i].BackupSet, RestoreOperations[i + 1].BackupSet, out errMsg, out errSource, ref stopAtLsn);
                        if (stopAtLsn != 0m && stopAtLsn != RestoreOperations[i].BackupSet.StopAtLsn)
                        {
                            inSequence = false;
                        }
                        if (!inSequence)
                        {
                            InvalidRestorePlanException ex = new InvalidRestorePlanException(errSource, errMsg);
                        }
                    }
                }
            }
            
            //Check backup media integrity if required. 
            if (checkBackupMediaIntegrity)
            {
                foreach (Restore res in this.RestoreOperations)
                {
                    if (res.BackupSet != null)
                    {
                        res.BackupSet.Verify();
                    }
                }
            }
        }


        /// <summary>
        /// Checks the backup sets existence.
        /// </summary>
        public void CheckBackupSetsExistence()
        {
            foreach (Restore restore in this.RestoreOperations)
            {
                if (restore.BackupSet != null)
                {
                    restore.BackupSet.CheckBackupFilesExistence();
                }
            }
        }

        #endregion

        #region public methods
        /// <summary>
        /// Sets the restore options.
        /// </summary>
        /// <param name="restoreOptions">The restore options.</param>
        public void SetRestoreOptions(RestoreOptions restoreOptions)
        {
            this.restoreOptions = restoreOptions;

            if (this.RestoreOperations == null || this.RestoreOperations.Count == 0)
            {
                return;
            }
            foreach (Restore restoreObj in this.RestoreOperations)
            {
                restoreObj.PercentCompleteNotification = restoreOptions.PercentCompleteNotification;
                restoreObj.ContinueAfterError = restoreOptions.ContinueAfterError;
                restoreObj.ClearSuspectPageTableAfterRestore = restoreOptions.ClearSuspectPageTableAfterRestore;
                restoreObj.BlockSize = restoreOptions.Blocksize;
                restoreObj.BufferCount = restoreOptions.BufferCount;
                restoreObj.MaxTransferSize = restoreOptions.MaxTransferSize;
                restoreObj.RestrictedUser = restoreOptions.SetRestrictedUser;
                restoreObj.NoRecovery = true;
            }
            if (this.TailLogBackupOperation != null)
            {
                this.TailLogBackupOperation.PercentCompleteNotification = restoreOptions.PercentCompleteNotification;
                this.TailLogBackupOperation.ContinueAfterError = restoreOptions.ContinueAfterError;
                this.TailLogBackupOperation.BlockSize = restoreOptions.Blocksize;
                this.TailLogBackupOperation.BufferCount = restoreOptions.BufferCount;
                this.TailLogBackupOperation.MaxTransferSize = restoreOptions.MaxTransferSize;
            }
            Restore firstRestoreObject = restoreOperations[0];
            firstRestoreObject.ReplaceDatabase = restoreOptions.ReplaceDatabase;

            int lastSelectedIndex = this.restoreOperations.Count - 1;
            Restore lastRestoreObject = restoreOperations[lastSelectedIndex];
            switch (restoreOptions.RecoveryState)
            {
                case DatabaseRecoveryState.WithNoRecovery:
                    lastRestoreObject.NoRecovery = true;
                    break;
                case DatabaseRecoveryState.WithRecovery:
                    lastRestoreObject.NoRecovery = false;
                    break;
                case DatabaseRecoveryState.WithStandBy:
                    lastRestoreObject.NoRecovery = false;
                    lastRestoreObject.StandbyFile = restoreOptions.StandByFile;
                    break;
            }
            lastRestoreObject.KeepReplication = restoreOptions.KeepReplication;
            lastRestoreObject.KeepTemporalRetention = restoreOptions.KeepTemporalRetention;
        }

        /// <summary>
        /// Adds the restore operation.
        /// </summary>
        /// <param name="backupSet">The backup set to be restored.</param>
        public void AddRestoreOperation(BackupSet backupSet)
        {
            Restore res = new Restore(this.DatabaseName, backupSet);
            this.RestoreOperations.Add(res);
        }

        /// <summary>
        /// Adds the restore operations.
        /// </summary>
        /// <param name="backupSets">The backup sets.</param>
        public void AddRestoreOperation(List<BackupSet> backupSets)
        {
            foreach (BackupSet backupSet in backupSets)
            {
                try
                {
                    Restore res = new Restore(this.DatabaseName, backupSet);
                    this.RestoreOperations.Add(res);
                }
                catch (SmoException) { } //We are ignoring the exception  when the backupMediaSet is not complete. 
                //Only complete backupMediaSets will be used. Rest is ignored. 
            }
        }

        #endregion
    }

    #region InvalidRestorePlanException class
    /// <summary>
    /// Exception thrown on trying to execute or verify an invalid Restore plan.
    /// </summary>
    public sealed class InvalidRestorePlanException : SmoException
    {
        internal InvalidRestorePlanException(Object source, String reason)
            : base(reason)
        {
        }
    }
    #endregion
}
