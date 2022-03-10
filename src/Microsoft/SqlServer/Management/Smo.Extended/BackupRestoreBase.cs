// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Text;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Collections.Specialized;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Internal;
using System.IO;
using Microsoft.SqlServer.Management.Sdk.Sfc;


namespace Microsoft.SqlServer.Management.Smo
{
    public class BackupRestoreBase
    {
        protected enum AsyncOperation
        {
            None,
            Backup,
            Restore
        };

        protected AsyncOperation currentAsyncOperation = AsyncOperation.None;
        protected BackupTruncateLogType m_LogTruncation;
        protected BackupActionType m_BackupAction;
        protected RestoreActionType m_RestoreAction;

        /// <summary>
        /// base class constructor
        /// </summary>
        public BackupRestoreBase()
        {
            backupDevices = new BackupDeviceList();
            databaseFiles = new StringCollection();
            databaseFileGroups = new StringCollection();
            server = null;
            m_NoRecovery = false;

            blockSize = -1;
            bufferCount = -1;
            maxTransferSize = -1;
        }

        private Server server;

        /// <summary>
        /// Specifies block size option for backup/restore
        /// </summary>
        private int blockSize;
        public int BlockSize
        {
            get
            {
                return blockSize;
            }
            set
            {
                blockSize = value;
            }
        }

        /// <summary>
        /// Specifies buffer count option for backup/restore
        /// </summary>
        private int bufferCount;
        public int BufferCount
        {
            get
            {
                return bufferCount;
            }
            set
            {
                bufferCount = value;
            }
        }

        /// <summary>
        /// Specifies max transfer size option for backup/restore
        /// </summary>
        private int maxTransferSize;
        public int MaxTransferSize
        {
            get
            {
                return maxTransferSize;
            }
            set
            {
                maxTransferSize = value;
            }
        }

        /// <summary>
        /// Whether we retry queries that fail with an exception that closes the connection
        /// </summary>
        private bool m_retryFailedQueries = true;
        public bool RetryFailedQueries
        {
            get
            {
                return m_retryFailedQueries;
            }
            set
            {
                m_retryFailedQueries = value;
            }
        }

        /// <summary>
        /// Called to set a server object 
        /// for the duration of an backup/restore operation.
        /// </summary>
        /// <param name="server"></param>
        private void SetServer(Server server)
        {
            lock (this.syncRoot)
            {
                if (null == this.server)
                {
                    this.server = server;
                }
                else
                {
                    throw new InvalidOperationException(ExceptionTemplates.OperationInProgress);
                }
            }
        }

        /// <summary>
        /// Called to reset server object after
        /// backup/restore operation.
        /// </summary>
        private void ResetServer()
        {
            lock (this.syncRoot)
            {
                this.server = null;
            }
        }

        // This flag indicates whether the backup/restore process has received "Complete" notification
        private bool processCompleted = false;

        protected void ExecuteSql(Server server, StringCollection queries)
        {
            SetServer(server);
            try
            {
                processCompleted = false;
                // if any notification is requested by the clients, hook into the server's notification messages
                if (null != Complete || null != PercentComplete || null != Information || null != NextMedia)
                {
                    this.server.ExecutionManager.ExecuteNonQueryWithMessage(queries,
                        new ServerMessageEventHandler(OnInfoMessage),
                        errorsAsMessages: true,
                        retry: this.RetryFailedQueries);
                }
                else
                {
                    this.server.ExecutionManager.ExecuteNonQuery(queries, this.RetryFailedQueries);
                }
            }
            catch (Exception e)
            {
                // We've got a list of errors in the exception. If there is 3014 among them, then we know
                // that backup/restore has succeeded, the errors can be ignored and we can eat up the error
                if (!processCompleted && e.InnerException != null)
                {
                    var se = e.InnerException as SqlException;
                    if (se != null)
                    {
                        foreach (SqlError err in se.Errors)
                        {
                            if (err.Number == 3014 /* Complete notification */ )
                            {
                                processCompleted = true;
                                break;
                            }
                        }
                    }
                }

                if (processCompleted)
                {
                    ; // swallow up the exception: we have succeeded
                }
                else
                {
                    throw; // rethrow
                }
            }
            finally
            {
                ResetServer();
            }
        }

        protected void ExecuteSqlAsync(Server server, StringCollection queries)
        {
            SetServer(server);

            // Register handle for async query completion event
            this.server.ExecutionManager.ExecuteNonQueryCompleted += new ExecuteNonQueryCompletedEventHandler(OnExecuteNonQueryCompleted);
            this.asyncStatus = new AsyncStatus(ExecutionStatus.InProgress, null);

            // if any notification is requested by the clients, hook into the server's notification messages
            if (null != Complete || null != PercentComplete || null != Information || null != NextMedia)
            {
                this.server.ExecutionManager.ExecuteNonQueryWithMessageAsync(queries,
                    new ServerMessageEventHandler(OnInfoMessage),
                    errorsAsMessages: true,
                    retry: this.RetryFailedQueries);
            }
            else
            {
                this.server.ExecutionManager.ExecuteNonQueryAsync(queries, this.RetryFailedQueries);
            }
        }

        protected DataSet ExecuteSqlWithResults(Server server, string cmd)
        {
            SetServer(server);
            try
            {
                // if any notification is requested by the clients, hook into the server's notification messages
                if (null != Complete || null != PercentComplete || null != Information || null != NextMedia)
                {
                    return this.server.ExecutionManager.ExecuteWithResultsAndMessages(cmd,
                        new ServerMessageEventHandler(OnInfoMessage),
                        errorsAsMessages: true,
                        retry: this.RetryFailedQueries);
                }
                else
                {
                    return this.server.ExecutionManager.ExecuteWithResults(cmd, this.RetryFailedQueries);
                }
            }
            finally
            {
                ResetServer();
            }
        }

        /// <summary>
        /// If invoked from Maintenance plan Check if HADR is enabled
        /// </summary>
        /// <param name="targetServer"></param>
        /// <param name="sb"></param>
        /// <returns></returns>
        protected StringBuilder CheckForHADRMaintPlan(Server targetServer, StringBuilder sb)
        {
            ServerVersion targetVersion = targetServer.ServerVersion;
            DatabaseEngineType targetEngineType = targetServer.DatabaseEngineType;
            //For a HADR Maint Plan wrap the BACKUP/RESTORE VERIFYONLY T-SQL inside a IF clause
            if ((this.m_checkForHADRMaintPlan) && //Set by the maintplan ui - this should be sufficient
                (11 <= targetVersion.Major) && //HADR supported from SQL11
                (targetEngineType == DatabaseEngineType.Standalone) && //HADR applicable only in standalone
                (targetServer.IsHadrEnabled) && //If the server is not HADR enabled
                (!string.IsNullOrEmpty(targetServer.Databases[this.Database].AvailabilityGroupName))) //DB is part of AvailablityGroup
            {
                if (!this.m_ignoreReplicaType)
                {
                    sb = this.GetMaintPlanTSQLForRightReplica(sb);
                }
            }
            return sb;
        }

        /// <summary>
        /// Add a condition in the Backup/Restore TSQL to check if it is executed on the preferred backup replica
        /// </summary>
        /// <param name="SqlStatement"></param>
        /// <returns></returns>
        private StringBuilder GetMaintPlanTSQLForRightReplica(StringBuilder SqlStatement)
        {
            if (string.IsNullOrEmpty(SqlStatement.ToString()))
            {
                return SqlStatement;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"DECLARE @preferredReplica int");
            sb.AppendFormat(@"
SET @preferredReplica = (SELECT [master].sys.fn_hadr_backup_is_preferred_replica('{0}'))", this.Database);
            sb.AppendLine();
            sb.AppendFormat(@"
IF (@preferredReplica = 1)
BEGIN
    {0}
END", SqlStatement.ToString());

            return sb;
        }

        /// <summary>
        /// Aborts the current action, if any
        /// </summary>
        public void Abort()
        {
            try
            {
                if (null != this.server)
                {
                    lock (this.syncRoot)
                    {
                        if (null != this.server)
                        {
                            this.server.ExecutionManager.Abort();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Abort, this, e);
            }
        }

        /// <summary>
        /// Waits for the current asynchronous action to complete.
        /// </summary>
        public void Wait()
        {
            Server localServer = null;

            if (null != this.server)
            {
                lock (this.syncRoot)
                {
                    if (null != this.server)
                    {
                        localServer = this.server;
                    }
                }
            }

            //
            // We need to use local variable because this.server
            // can be released while we are waiting for the event
            // to get signaled.
            //
            if (localServer != null)
            {
                localServer.ExecutionManager.AsyncWaitHandle.WaitOne();
            }
        }

        private void OnExecuteNonQueryCompleted(object sender, ExecuteNonQueryCompletedEventArgs args)
        {
            // Save results
            this.asyncStatus = new AsyncStatus(args.ExecutionStatus, args.LastException);

            // Unregister event
            this.server.ExecutionManager.ExecuteNonQueryCompleted -= new ExecuteNonQueryCompletedEventHandler(OnExecuteNonQueryCompleted);

            // Send event
            if (currentAsyncOperation == AsyncOperation.Backup)
            {
                if (!server.ExecutionManager.Recording && m_BackupAction == BackupActionType.Log && m_LogTruncation == BackupTruncateLogType.NoTruncate)
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(server,
                                new DatabaseEventArgs(server.Databases[this.Database].Urn,
                                                server.Databases[this.Database],
                                                this.Database,
                                                DatabaseEventType.Backup));
                    }
                }
            }
            else if (currentAsyncOperation == AsyncOperation.Restore)
            {
                if (!server.ExecutionManager.Recording && (m_RestoreAction == RestoreActionType.Database || m_RestoreAction == RestoreActionType.Log))
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(server,
                                new DatabaseEventArgs(server.Databases[this.Database].Urn,
                                                    server.Databases[this.Database],
                                                    this.Database,
                                                    DatabaseEventType.Restore));
                    }
                }
            }

            // Forget what operation we had
            currentAsyncOperation = AsyncOperation.None;

            ResetServer();
        }

        /// <summary>
        /// A status of most recent asynchronous operation,
        /// including possible errors.
        /// </summary>
        public AsyncStatus AsyncStatus
        {
            get { return this.asyncStatus; }
        }

        private readonly object syncRoot = new object();
        private AsyncStatus asyncStatus = new AsyncStatus();

        /// <summary>
        /// PercentComplete event
        /// </summary>
        public event PercentCompleteEventHandler PercentComplete;

        /// <summary>
        /// NextMedia event
        /// </summary>
        public event ServerMessageEventHandler NextMedia;

        /// <summary>
        /// Complete event
        /// </summary>
        public event ServerMessageEventHandler Complete;

        /// <summary>
        /// Information event
        /// </summary>
        public event ServerMessageEventHandler Information;

        private void OnInfoMessage(object sender, ServerMessageEventArgs e)
        {
            switch (e.Error.Number)
            {
                case 3211:
                    // Percent complete notification
                    if (null != PercentComplete)
                    {
                        PercentComplete(this, new PercentCompleteEventArgs(e.Error));
                    }
                    break;
                case 3014:
                    // Complete notification
                    if (null != Complete)
                    {
                        Complete(this, e);
                    }
                    processCompleted = true; // set the "Completed" flag. This indicates successful completion of
                    // of the process despite other errors
                    break;
                case 3247: goto case 4028;
                case 3249: goto case 4028;
                case 4027: goto case 4028;
                case 4028:
                    // Mount next volume/tape notification
                    if (null != NextMedia)
                    {
                        NextMedia(this, e);
                    }
                    break;

                default:
                    // information events
                    if (null != Information)
                    {
                        Information(this, e);
                    }
                    break;
            }
        }

        // helper function for scripting
#if CLSCOMPLIANT
        [CLSCompliant(false)]
#endif
        protected void GetDevicesScript(StringBuilder query, BackupDeviceList devices, ServerVersion targetVersion)
        {
            Sdk.Sfc.TraceHelper.Assert(null != devices);
            string format = string.Empty;
            bool isIdentifier = false;
            bool first = true;

            foreach (BackupDeviceItem bd in devices)
            {
                switch (bd.DeviceType)
                {
                    case DeviceType.Tape:
                        format = " TAPE = N'{0}'";
                        isIdentifier = false;
                        break;
                    case DeviceType.File:
                        format = " DISK = N'{0}'";
                        isIdentifier = false;
                        break;
                    case DeviceType.LogicalDevice:
                        format = " [{0}]";
                        isIdentifier = true;
                        break;
                    case DeviceType.VirtualDevice:
                        format = " VIRTUAL_DEVICE = N'{0}'";
                        isIdentifier = false;
                        break;
                    case DeviceType.Pipe:
                        if (targetVersion.Major >= 9)
                        {
                            throw new WrongPropertyValueException(ExceptionTemplates.BackupToPipesNotSupported(targetVersion.ToString()));
                        }
                        format = " PIPE = N'{0}'";
                        isIdentifier = false;
                        break;
                    case DeviceType.Url:
                        // Backup to url is not supported on versions less than SQL 11 PCU1
                        // $ISSUE - VSTS# 1040958 -  Update SQL 11 PCU1 Version in SMO's VersionUtils.cs
                        if (!IsBackupUrlDeviceSupported(targetVersion))
                        {
                            throw new WrongPropertyValueException(ExceptionTemplates.BackupToUrlNotSupported(targetVersion.ToString(),
                                BackupUrlDeviceSupportedServerVersion.ToString()));
                        }
                        format = " URL = N'{0}'";

                        //$ISSUE - VSTS# 1040954 -Backup To URL - Investigate Supporting URL, CredentialName as an identifier
                        isIdentifier = false;

                        break;
                    default:
                        throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("DeviceType"));
                }

                if (!first)
                {
                    query.Append(Globals.commaspace);
                }
                else
                {
                    first = false;
                }

                Sdk.Sfc.TraceHelper.Assert(null != bd.Name);
                query.AppendFormat(SmoApplication.DefaultCulture, format,
                            isIdentifier ? SqlSmoObject.SqlBraket(bd.Name) : SqlSmoObject.SqlString(bd.Name));

            }
        }

        private BackupDeviceList backupDevices;

        /// <summary>
        /// A list of devices used as a target for backup
        /// </summary>
#if CLSCOMPLIANT
        [CLSCompliant(false)]
#endif
       public BackupDeviceList Devices
        {
            get
            {
                return backupDevices;
            }
        }

        private StringCollection databaseFiles;
        public StringCollection DatabaseFiles
        {
            get
            {
                return databaseFiles;
            }
        }

        private StringCollection databaseFileGroups;
        public StringCollection DatabaseFileGroups
        {
            get
            {
                return databaseFileGroups;
            }
        }

        private string database = null;
        public string Database
        {
            get
            {
                return database;
            }
            set
            {
                database = value;
            }
        }

        private string credentialName = null;
        /// <summary>
        /// Gets or sets the credential name that is used by Backup to Url
        /// </summary>
        public string CredentialName
        {
            get
            {
                return this.credentialName;
            }
            set
            {
                this.credentialName = value;
            }
        }

        private bool checksum = false;  // default undocumented behavior
        public bool Checksum
        {
            get
            {
                return checksum;
            }
            set
            {
                checksum = value;
            }
        }

        private bool continueAfterError;
        public bool ContinueAfterError
        {
            get
            {
                return continueAfterError;
            }
            set
            {
                continueAfterError = value;
            }
        }

        private string mediaName = null;
        public string MediaName
        {
            get
            {
                return mediaName;
            }
            set
            {
                mediaName = value;
            }
        }

        /// <summary>
        /// SQL 11 PCU1 CU2 version is 11.0.3339.0
        /// Reference: http://hotfix.partners.extranet.microsoft.com/search.aspx?search=2790947
        /// </summary>
        internal static ServerVersion BackupUrlDeviceSupportedServerVersion
        {
            get
            {
                return new ServerVersion(11, 0, 3339);
            }
        }

        /// <summary>
        /// Check if backup/restore scripting is invoked from Maintenance plan
        /// </summary>
        internal bool m_checkForHADRMaintPlan = false;

        //Ignore replica type when scripting for HADR DB
        private bool m_ignoreReplicaType = false;
        /// <summary>
        /// IgnoreReplicaType property get or sets flag to ignore replica when scripting for HADR Database
        /// </summary>
        internal bool IgnoreReplicaType
        {
            get { return this.m_ignoreReplicaType; }
            set { this.m_ignoreReplicaType = value; }
        }

        /// <summary>
        /// Helper to check if BackupToUrl is supported on the connected server version
        /// </summary>
        /// <param name="currentServerVersion"></param>
        /// <returns></returns>
        public static bool IsBackupUrlDeviceSupported(ServerVersion currentServerVersion)
        {
            bool urlDeviceSupported = false;

            if (currentServerVersion.Major > BackupUrlDeviceSupportedServerVersion.Major) // If Major version greater than sql 11
            {
                urlDeviceSupported = true;
            }
            else if (currentServerVersion.Major == BackupUrlDeviceSupportedServerVersion.Major) // if SQL 11
            {
                // Compare minor version and build number 
                if (currentServerVersion.Minor >= BackupUrlDeviceSupportedServerVersion.Minor &&
                    currentServerVersion.BuildNumber >= BackupUrlDeviceSupportedServerVersion.BuildNumber)
                {
                    urlDeviceSupported = true;
                }
            }

            return urlDeviceSupported;
        }

        /// <summary>
        /// Helper to check if BackupToFile is supported on the connected server edition.
        /// SQL Managed Instances do not support backup to file.
        /// </summary>
        /// <param name="serverEdition"></param>
        /// <returns></returns>
        public static bool IsBackupFileDeviceSupported(DatabaseEngineEdition serverEdition)
        {
            return serverEdition != DatabaseEngineEdition.SqlManagedInstance;
        }

        private bool noRewind = false;
        internal bool NorewindValueSetByUser = false;
        public bool NoRewind
        {
            get
            {
                return noRewind;
            }
            set
            {
                this.NorewindValueSetByUser = true;
                noRewind = value;
            }
        }

        private int percentCompleteNotification = 10;
        public int PercentCompleteNotification
        {
            get
            {
                return percentCompleteNotification;
            }
            set
            {
                percentCompleteNotification = value;
            }
        }

        private SqlSecureString password;
        public void SetPassword(string value)
        {
            password = value != null ? new SqlSecureString(value) : null;
        }

        public void SetPassword(System.Security.SecureString value)
        {
            password = value;
        }

        internal SqlSecureString GetPassword()
        {
            return password;
        }

        private bool restart = false;
        public bool Restart
        {
            get
            {
                return restart;
            }
            set
            {
                restart = value;
            }
        }

        private bool unloadTapeAfter = false;
        internal bool UnloadValueSetByUser = false;
        public bool UnloadTapeAfter
        {
            get
            {
                return unloadTapeAfter;
            }
            set
            {
                this.UnloadValueSetByUser = true;
                unloadTapeAfter = value;
            }
        }

        protected bool IsStringValid(String s)
        {
            return (null != s && s.Length > 0);
        }

        internal bool IsStringValid(SqlSecureString s)
        {
            return (null != s && s.Length > 0);
        }

        bool m_NoRecovery;
        public bool NoRecovery
        {
            get
            {
                return m_NoRecovery;
            }
            set
            {
                m_NoRecovery = value;
            }
        }

        /// <summary>
        /// Add Credential Info to BACKUP DDL, Only supported for versions greater than SQL 11 PCU1 
        /// </summary>
        /// <param name="targetVersion"></param>
        /// <param name="sb"></param>
        /// <param name="withCommaStart"></param>
        /// <param name="withCommaEnd"></param>
        /// <returns></returns>
        internal bool AddCredential(ServerVersion targetVersion, StringBuilder sb, bool withCommaStart, bool withCommaEnd)
        {
            bool stringAdded = false;

            if (!String.IsNullOrEmpty(CredentialName))
            {
                // throw if version is less than SQL 11 PCU1
                if (!IsBackupUrlDeviceSupported(targetVersion))
                {
                    throw new UnsupportedFeatureException(ExceptionTemplates.CredentialNotSupportedError(CredentialName,
                        targetVersion.ToString(),
                        BackupUrlDeviceSupportedServerVersion.ToString()));
                }

                if (withCommaStart)
                {
                    sb.Append(Globals.commaspace);
                }

                // $ISSUE - VSTS# 1040954 -Backup To URL - Investigate Supporting URL, CredentialName as an identifier
                sb.AppendFormat(SmoApplication.DefaultCulture,
                        " CREDENTIAL = N'{0}' ",
                        SqlSmoObject.SqlString(CredentialName));

                if (withCommaEnd)
                {
                    sb.Append(Globals.commaspace);
                }

                stringAdded = true;
            }

            return stringAdded;
        }

        internal bool AddMediaPassword(ServerVersion targetVersion, StringBuilder sb, bool withCommaStart, bool withCommaEnd)
        {
            return false;
        }

        internal bool AddPassword(ServerVersion targetVersion, StringBuilder sb, bool withCommaStart, bool withCommaEnd)
        {
            bool stringAdded = false;
            //check if Password is set
            if (IsStringValid(GetPassword()) && 7 != targetVersion.Major)
            {
                if (VersionUtils.IsSql11OrLater(targetVersion))
                {
                    throw new UnsupportedFeatureException(ExceptionTemplates.PasswordError);
                }

                if (withCommaStart)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " PASSWORD = N'{0}'", SqlSmoObject.SqlString((string)GetPassword()));
                if (withCommaEnd)
                {
                    sb.Append(Globals.commaspace);
                }
                stringAdded = true;
            }
            return stringAdded;
        }


        internal static bool CheckNewBackupFile(Server server, string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }
            Enumerator en = new Enumerator();
            try
            {
                if (!file.StartsWith(@"\\", StringComparison.Ordinal) && string.IsNullOrEmpty(Path.GetDirectoryName(file)))
                {
                    file = server.BackupDirectory + "\\" + file;
                }
            }
            catch (Exception)
            {
                return false;
            }
            Request reqFile = new Request(new Urn("Server/File[@FullName='" + Urn.EscapeString(Path.GetFullPath(file)) + "']"));
            DataSet dsFile = en.Process(server.ConnectionContext, reqFile);
            if (dsFile.Tables.Count == 0 || dsFile.Tables[0].Rows.Count == 0)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether a string is an URL
        /// </summary>
        internal static bool IsBackupDeviceUrl(string url)
        {
            Uri testUri;
            return (Uri.TryCreate(url, UriKind.Absolute, out testUri) && (testUri.Scheme == Uri.UriSchemeHttps || testUri.Scheme == Uri.UriSchemeHttp));
        }
    }


    /// <summary>
    /// the prototype of the callback method for next restore
    /// </summary>
    public delegate void NextRestoreEventHandler(object sender, NextRestoreEventArgs e);

    /// <summary>
    /// Next Restore Event arguments
    /// </summary>
    public sealed class NextRestoreEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NextRestoreEventArgs"/> class.
        /// </summary>
        /// <param name="backupSetName">Name of the backup set.</param>
        /// <param name="backupSetDescription">The backup set description.</param>
        /// <param name="deviceName">Name of the device.</param>
        /// <param name="count">The count.</param>
        public NextRestoreEventArgs(string backupSetName, string backupSetDescription, String deviceName, int count)
        {
            this.Continue = true;
            this.BackupSetName = backupSetName;
            this.BackupSetDescription = backupSetDescription;
            this.DevicesName = deviceName;
            this.Count = count;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to continue next Restore Operation.
        /// </summary>
        /// <value><c>true</c> if continue; otherwise, <c>false</c>.</value>
        public bool Continue
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the backup set.
        /// </summary>
        /// <value>The name of the backup set.</value>
        public string BackupSetName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the backup set description.
        /// </summary>
        /// <value>The backup set description.</value>
        public string BackupSetDescription
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the name of the backup media.
        /// </summary>
        /// <value>The name of the backup media.</value>
        public string DevicesName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get;
            private set;
        }
    }

    /// <summary>
    /// the prototype of the callback method for percent complete 
    /// </summary>
    public delegate void PercentCompleteEventHandler(object sender, PercentCompleteEventArgs e);

    /// <summary>
    /// Arguments for the event handler of the percent complete
    /// </summary>
    public sealed class PercentCompleteEventArgs : ServerMessageEventArgs
    {
        internal PercentCompleteEventArgs(SqlError error)
            : base(error)
        {
            // parse the error message to get the percentage 
            // First skip eventual prefixes thet come from the driver
            this.message = error.Message;
            int lastBracket = this.message.LastIndexOf(']');
            StringBuilder sbPercent = new StringBuilder();

            int i = lastBracket + 1;

            while (i < this.message.Length)
            {
                if (!Char.IsNumber(this.message[i++]))
                {
                    sbPercent.Append(this.message, lastBracket + 1, i - lastBracket - 1);
                    break;
                }
            }

            percent = Convert.ToInt32(sbPercent.ToString(), SmoApplication.DefaultCulture);
        }

        internal PercentCompleteEventArgs(SqlError error, string message)
            : this(error)
        {
            this.message = message;
        }

        private int percent = 0;
        /// <summary>
        /// Percent
        /// </summary>
        public int Percent
        {
            get { return percent; }
        }

        private string message = string.Empty;
        /// <summary>
        /// Gets the message.
        /// </summary>
        /// <value>The message.</value>
        public string Message
        {
            get { return message; }
        }
    }

    /// <summary>
    /// A helper class that describes status and last exception
    /// from an asynchronous operation.
    /// </summary>
    public sealed class AsyncStatus
    {
        internal AsyncStatus()
        {
        }

        internal AsyncStatus(ExecutionStatus executionStatus, Exception lastException)
        {
            this.executionStatus = executionStatus;
            this.lastException = lastException;
        }

        /// <summary>
        /// ExecutionStatus
        /// </summary>
        public ExecutionStatus ExecutionStatus
        {
            get { return this.executionStatus; }
        }

        /// <summary>
        /// LastException
        /// </summary>
        public Exception LastException
        {
            get { return this.lastException; }
        }

        private ExecutionStatus executionStatus;
        private Exception lastException;
    }

    /// <summary>
    /// Represents a device that will be used to backup to or restore from
    /// </summary>
    public class BackupDeviceItem : IComparable
    {
        BackupMedia backupMedia;
        internal BackupMedia BackupMedia
        {
            get
            {
                return backupMedia;
            }
        }

        /// <summary>
        /// Creates a BackupDeviceItem 
        /// </summary>
        public BackupDeviceItem()
        {
            this.backupMedia = new BackupMedia();
        }

        /// <summary>
        /// Creates a BackupDeviceItem object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="deviceType"></param>
        public BackupDeviceItem(string name, DeviceType deviceType)
        {
            if (null == name)
            {
                throw new FailedOperationException(ExceptionTemplates.SetName, this, new ArgumentNullException("Name"));
            }
            this.backupMedia = new BackupMedia(name, deviceType);
        }

        /// <summary>
        /// Creates a BackupDeviceItem object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="deviceType"></param>
        /// <param name="credentialName"></param>
        public BackupDeviceItem(string name, DeviceType deviceType, string credentialName)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new FailedOperationException(ExceptionTemplates.SetName, this, new ArgumentNullException("Name"));
            }
            this.backupMedia = new BackupMedia(name, deviceType, credentialName);
        }

        /// <summary>
        /// The name of the backup object
        /// </summary>
        public string Name
        {
            get
            {
                return this.BackupMedia.MediaName;
            }
            set
            {
                if (null == value)
                {
                    throw new FailedOperationException(ExceptionTemplates.SetName, this, new ArgumentNullException("Name"));
                }
                BackupMedia bkMedia = new BackupMedia(value, this.BackupMedia.MediaType);
                this.backupMedia = bkMedia;
            }
        }

        /// <summary>
        /// Type of the backup object
        /// </summary>
        public DeviceType DeviceType
        {
            get
            {
                return this.BackupMedia.MediaType;
            }
            set
            {
                BackupMedia bkMedia = new BackupMedia(this.BackupMedia.MediaName, value);
                this.backupMedia = bkMedia;
            }
        }

        /// <summary>
        /// Credential of the backup object
        /// </summary>
        public string CredentialName
        {
            get
            {
                return this.BackupMedia.CredentialName;
            }
            set
            {
                BackupMedia bkMedia = new BackupMedia(this.BackupMedia.MediaName, this.BackupMedia.MediaType, value);
                this.backupMedia = bkMedia;
            }
        }

        /// <summary>
        /// Returns the Device header.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>header</returns>
        internal DataTable DeviceHeader(Server server)
        {
            return this.BackupMedia.MediaHeader(server);
        }

        /// <summary>
        /// Returns the Device label.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>label</returns>
        internal DataTable DeviceLabel(Server server)
        {
            return this.BackupMedia.MediaLabel(server);
        }

        /// <summary>
        /// IComparable implementation
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            if (null == obj)
                return 1;

            CheckType(obj, ExceptionTemplates.Compare, this);

            return string.Compare(this.Name, ((BackupDeviceItem)obj).Name, StringComparison.OrdinalIgnoreCase);
        }

        // Omitting Equals violates rule: OverrideMethodsOnComparableTypes.
        public override bool Equals(Object obj)
        {
            if (!(obj is BackupDeviceItem))
                return false;
            return (this.CompareTo(obj) == 0);
        }

        // Omitting getHashCode violates rule: OverrideGetHashCodeOnOverridingEquals.
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        // Omitting any of the following operator overloads 
        // violates rule: OverrideMethodsOnComparableTypes.
        public static bool operator ==(BackupDeviceItem r1, BackupDeviceItem r2)
        {
            return r1.Equals(r2);
        }
        public static bool operator !=(BackupDeviceItem r1, BackupDeviceItem r2)
        {
            return !(r1 == r2);
        }
        public static bool operator <(BackupDeviceItem r1, BackupDeviceItem r2)
        {
            return (r1.CompareTo(r2) < 0);
        }
        public static bool operator >(BackupDeviceItem r1, BackupDeviceItem r2)
        {
            return (r1.CompareTo(r2) > 0);
        }

        internal static void CheckType(object obj, string operation, object thisptr)
        {
            Sdk.Sfc.TraceHelper.Assert(null != operation && operation.Length > 0);
            Sdk.Sfc.TraceHelper.Assert(null != thisptr);
            if (null == obj)
                throw new FailedOperationException(operation, thisptr, new ArgumentNullException());

            if (!(obj is BackupDeviceItem))
            {
                throw new FailedOperationException(operation, thisptr,
                        new NotSupportedException(ExceptionTemplates.InvalidType(obj.GetType().ToString())));
            }

        }

    }

    /// <summary>
    /// Database page used for Page Restore.
    /// </summary>
    public class SuspectPage : IComparable<SuspectPage>
    {
        /// <summary>
        /// Gets or sets the file ID.
        /// </summary>
        /// <value>The file ID.</value>
        public int FileID
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the page ID.
        /// </summary>
        /// <value>The page ID.</value>
        public long PageID
        {
            get;
            private set;
        }

#region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SuspectPage"/> class.
        /// </summary>
        /// <param name="fileID">The file ID.</param>
        /// <param name="pageID">The page ID.</param>
        public SuspectPage(int fileID, long pageID)
        {
            this.FileID = fileID;
            this.PageID = pageID;
        }
#endregion

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}:{1}", this.FileID, this.PageID);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="T:System.NullReferenceException">
        /// The <paramref name="obj"/> parameter is null.
        /// </exception>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is SuspectPage))
            {
                return false;
            }

            SuspectPage page = (obj as SuspectPage);
            return (this.FileID == page.FileID && this.PageID == page.PageID);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.PageID.GetHashCode() ^ this.FileID.GetHashCode();
        }
        /// <summary>
        /// Determines whether suspect page is valid  .
        /// </summary>
        /// <returns>
        /// <c>true</c> if valid; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="SmoException"></exception>
        public void Validate()
        {
            if (this.FileID <= 0 || this.PageID < 0)
            {
                throw new SmoException(ExceptionTemplates.InvalidSuspectpage);
            }
            if (this.PageID == 0)
            {
                throw new SmoException(ExceptionTemplates.CannotRestoreFileBootPage(this.FileID, this.PageID));
            }
            if (this.FileID == 1 && this.PageID == 9)
            {
                throw new SmoException(ExceptionTemplates.CannotRestoreDatabaseBootPage(this.FileID, this.PageID));
            }
        }

        /// <summary>
        /// Compares to other SuspectPage
        /// </summary>
        /// <param name="other">The other SuspectPage.</param>
        /// <returns>
        /// Less than zero - This object is less than the object specified by the CompareTo method.
        /// Zero - This object is equal to the object specified by the CompareTo method. 
        /// Greater than zero - This object is greater than the object specified by the CompareTo method. 
        /// </returns>
        public int CompareTo(SuspectPage other)
        {
            if (this.Equals(other))
            {
                return 0;
            }
            if (this.FileID > other.FileID)
            {
                return 1;
            }
            else if (this.FileID < other.FileID)
            {
                return -1;
            }

            if (this.PageID > other.PageID)
            {
                return 1;
            }
            else if (this.PageID < other.PageID)
            {
                return -1;
            }

            return 0;
        }
    }

    /// <summary>
    /// Strongly typed list of BackupDeviceItem objects
    /// </summary> 
#if CLSCOMPLIANT
    [CLSCompliant(false)]
#endif
    public class BackupDeviceList : List<BackupDeviceItem>
    {
#region Constructors

        public BackupDeviceList()
            : base()
        {
        }

        public BackupDeviceList(System.Collections.Generic.IEnumerable<BackupDeviceItem> collection)
            : base(collection)
        {
        }

        public BackupDeviceList(int capacity)
            : base(capacity)
        {
        }
#endregion

        /// <summary>
        /// Adds a new BackupDeviceItem to the collection
        /// </summary>
        /// <param name="name"></param>
        /// <param name="deviceType"></param>
        public void AddDevice(string name, DeviceType deviceType)
        {
            if (null == name)
                throw new FailedOperationException(ExceptionTemplates.AddDevice, this,
                                                new ArgumentNullException("name"));

            base.Add(new BackupDeviceItem(name, deviceType));
        }

    }

}

