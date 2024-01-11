// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Collections;
using System.Diagnostics.CodeAnalysis;

using Microsoft.SqlServer.Management.Common;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo
{
    public sealed class Restore : BackupRestoreBase
    {
        bool m_bVerifySuccess;

        public Restore() : base()
        {
            // initialize some default member values
            m_RestoreAction = RestoreActionType.Database;
            m_Partial = false;
            m_RestrictedUser = false;
            m_FileNumber = 0;
            m_KeepReplication = false;
            m_KeepTemporalRetention = false;
            m_ReplaceDatabase = false;
            m_bVerifySuccess = false;
            m_RelocateFiles = new ArrayList();
        }

        /// <summary>
        /// The restore backup set
        /// </summary>
        public BackupSet BackupSet
        {
            get; private set;
        }
        /// <summary>
        /// Creates a Restore object
        /// </summary>
        /// <param name="DestinationDatabaseName">Name of the database to be restored.</param>
        /// <param name="backupSet">The backup set.</param>
        public Restore(string DestinationDatabaseName, BackupSet backupSet) : base()
        {
            this.Database = DestinationDatabaseName;
            this.m_Partial = false;
            this.m_RestrictedUser = false;
            this.m_KeepReplication = false;
            this.m_KeepTemporalRetention = false;
            this.m_ReplaceDatabase = false;
            this.m_bVerifySuccess = false;
            this.m_RelocateFiles = new ArrayList();
            switch (backupSet.BackupSetType)
            {
                case BackupSetType.Database:
                case BackupSetType.Differential:
                    this.m_RestoreAction = RestoreActionType.Database;
                    break;

                case BackupSetType.Log:
                    this.m_RestoreAction = RestoreActionType.Log;
                    break;

                case BackupSetType.FileOrFileGroup:
                case BackupSetType.FileOrFileGroupDifferential:
                    this.m_RestoreAction = RestoreActionType.Files;
                    break;
            }
            backupSet.BackupMediaSet.CheckMediaSetComplete();
            for (int i = 1; i <= backupSet.BackupMediaSet.FamilyCount; i++)
            {
                var item = from BackupMedia bkMedia in backupSet.BackupMediaSet.BackupMediaList
                           where bkMedia.FamilySequenceNumber == i
                           orderby bkMedia.MirrorSequenceNumber ascending
                           select bkMedia;
                if (item.Count() > 0)
                {
                    BackupMedia bkMedia = item.ElementAt(0);
                    this.Devices.Add(new BackupDeviceItem(bkMedia.MediaName, bkMedia.MediaType));
                }
            }
            this.FileNumber = backupSet.Position;
            this.BackupSet = backupSet;
        }

        internal Restore(bool checkForHADRMaintPlan)
            : this()
        {
            this.m_checkForHADRMaintPlan = checkForHADRMaintPlan;
        }

        /// <summary>
        /// An event raised at the end of SqlVerify operation.
        /// It indicates if verification succeeded or not.
        /// </summary>

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public VerifyCompleteEventHandler VerifyComplete;

        /// <summary>
        /// Runs Verify operation in a synchronous way, i.e. 
        /// the call blocks until verification is completed.
        /// </summary>
        /// <param name="srv">Server to run verification on.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerify(Server srv)
        {
            string errorMessage; // ignore this
            return SqlVerify(srv, false, out errorMessage);
        }

        /// <summary>
        /// Runs Verify operation in a synchronous way, i.e. 
        /// the call blocks until verification is completed.
        /// </summary>
        /// <param name="srv">Server to run verification on.</param>
        /// <param name="loadHistory">Load history.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerify(Server srv, bool loadHistory)
        {
            string errorMessage; // ignore this
            return SqlVerify(srv, loadHistory, out errorMessage);
        }

        /// <summary>
        /// Runs Verify operation in a synchronous way, i.e. 
        /// the call blocks until verification is completed.
        /// </summary>
        /// <param name="srv">Server to run verification on.</param>
        /// <param name="errorMessage">Returns a detailed error message if verification failed.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerify(Server srv, out string errorMessage)
        {
            return SqlVerify(srv, false, out errorMessage);
        }
        /// <summary>
        /// Runs Verify operation in a synchronous way, i.e. 
        /// the call blocks until verification is completed.
        /// </summary>
        /// <param name="srv">Server to run verification on.</param>
        /// <param name="loadHistory">Load history.</param>
        /// <param name="errorMessage">Returns a detailed error message if verification failed.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerify(Server srv, bool loadHistory, out string errorMessage)
        {
            // Prepare T-SQL statement
            StringCollection queries = new StringCollection();
            queries.Add(ScriptVerify(srv, loadHistory));

            return SqlVerifyWorker(srv, queries, out errorMessage);
        }

        internal bool SqlVerifyWorker(Server srv, StringCollection queries, out string errorMessage)
        {
            try
            {
                // Initialize output argument
                errorMessage = null;

                // Register for events
                //  OnBeforeSqlVerify - initializes m_bVerifySuccess variable
                //  OnInfoMessage     - sets m_bVerifySuccess if verify succeeded
                srv.ExecutionManager.BeforeExecuteSql += new EventHandler(OnBeforeSqlVerify);
                this.Information += new ServerMessageEventHandler(OnInfoMessage);

                // Run T-SQL
                ExecuteSql(srv, queries);
            }
            catch (SmoException exception)
            {
                errorMessage = exception.Message;
            }
            catch (ConnectionException exception)
            {
                // Provide error message to caller
                if (exception.InnerException != null && exception.InnerException is SqlException)
                {
                    errorMessage = exception.InnerException.Message;
                }
                else
                {
                    errorMessage = exception.Message;
                }
            }
            finally
            {
                this.Information -= new ServerMessageEventHandler(OnInfoMessage);
                srv.ExecutionManager.BeforeExecuteSql -= new EventHandler(OnBeforeSqlVerify);
            }

            if (null != VerifyComplete)
            {
                VerifyComplete(this, new VerifyCompleteEventArgs(this.m_bVerifySuccess));
            }
            return m_bVerifySuccess;
        }

        /// <summary>
        /// Performs a verify on the last backup recorded in the backup history. FileNumber 
        /// will be ignored, as the latest file number will be automatically determined.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerifyLatest(Server srv)
        {
            string errorMessage;    // ignore this
            return SqlVerifyLatest(srv, out errorMessage);
        }


        /// <summary>
        /// Performs a verify on the last backup recorded in the backup history. FileNumber 
        /// will be ignored, as the latest file number will be automatically determined.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        /// <param name="errorMessage">Returns a detailed error message if verification failed.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerifyLatest(Server srv, out string errorMessage)
        {
            // Prepare T-SQL statement
            StringCollection queries = new StringCollection();

            /*
            declare @backupSetId as int
            -- The message can be set by SMO, so it is localizable
            select @msg = 'Verify failed. Backup information for database ' + @db + ' (type ' + @type + ') not found.'


            select @i = position
                from msdb..backupset
            where
                database_name=@db
                and backup_set_id=(select max(backup_set_id) from msdb..backupset where database_name=@db)

            if @i is null
            begin
                raiserror(@msg, 16, 1)
            end
            */

            StringBuilder verifyStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            verifyStmt.Append("declare @backupSetId as int");
            verifyStmt.Append(Globals.newline);
            verifyStmt.AppendFormat(SmoApplication.DefaultCulture,
                                "select @backupSetId = position from msdb..backupset where database_name={0} and backup_set_id=(select max(backup_set_id) from msdb..backupset where database_name={0} )",
                                SqlSmoObject.MakeSqlString(this.Database));
            verifyStmt.Append(Globals.newline);
            verifyStmt.AppendFormat(SmoApplication.DefaultCulture,
                                "if @backupSetId is null begin raiserror({0}, 16, 1) end",
                                SqlSmoObject.MakeSqlString(ExceptionTemplates.VerifyFailed0(this.Database)));
            verifyStmt.Append(Globals.newline);
            verifyStmt.Append(ScriptVerify(srv, false, true));

            try
            {
                verifyStmt = this.CheckForHADRMaintPlan(srv, verifyStmt);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Restore, srv, e);
            }

            queries.Add(verifyStmt.ToString());
            return SqlVerifyWorker(srv, queries, out errorMessage);
        }

        /// <summary>
        /// Performs a verify on the last backup recorded in the backup history. FileNumber 
        /// will be ignored, as the latest file number will be automatically determined.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        /// <param name="sqlVerifyAction">specifies what is the action whose latest results</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerifyLatest(Server srv, SqlVerifyAction sqlVerifyAction)
        {
            string errorMessage;    // ignore this
            return SqlVerifyLatest(srv, sqlVerifyAction, out errorMessage);
        }


        /// <summary>
        /// Performs a verify on the last backup recorded in the backup history. FileNumber 
        /// will be ignored, as the latest file number will be automatically determined.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        /// <param name="sqlVerifyAction">specifies what is the action whose latest results</param>
        /// <param name="errorMessage">Returns a detailed error message if verification failed.</param>
        /// <returns>true if verification succeeded, false otherwise</returns>
        public bool SqlVerifyLatest(Server srv, SqlVerifyAction sqlVerifyAction, out string errorMessage)
        {
            // Prepare T-SQL statement
            StringCollection queries = new StringCollection();

            /*
            declare @backupSetId as int
            select @type = 'D'      -- D, I, L, F
            -- The message can be set by SMO, so it is localizable
            select @msg = 'Verify failed. Backup information for database ' + @db + ' (type ' + @type + ') not found.'


            select @i = position
                from msdb..backupset
            where
                database_name=@db
                and  type=@type
                and backup_set_id=(select max(backup_set_id) from msdb..backupset where database_name=@db)

            if @i is null
            begin
                raiserror(@msg, 16, 1)
            end
            */

            StringBuilder verifyStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            verifyStmt.Append("declare @backupSetId as int");
            verifyStmt.Append(Globals.newline);
            verifyStmt.AppendFormat(SmoApplication.DefaultCulture,
                                "select @backupSetId = position from msdb..backupset where database_name={0} and  type={1} and backup_set_id=(select max(backup_set_id) from msdb..backupset where database_name={0} and  type={1})",
                                SqlSmoObject.MakeSqlString(this.Database),
                                SqlSmoObject.MakeSqlString(GetBackupTypeName(sqlVerifyAction)));
            verifyStmt.Append(Globals.newline);
            verifyStmt.AppendFormat(SmoApplication.DefaultCulture,
                                "if @backupSetId is null begin raiserror({0}, 16, 1) end",
                                SqlSmoObject.MakeSqlString(ExceptionTemplates.VerifyFailed(this.Database, sqlVerifyAction.ToString())));
            verifyStmt.Append(Globals.newline);
            verifyStmt.Append(ScriptVerify(srv, false, true));

            try
            {
                verifyStmt = this.CheckForHADRMaintPlan(srv, verifyStmt);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Restore, srv, e);
            }

            queries.Add(verifyStmt.ToString());

            return SqlVerifyWorker(srv, queries, out errorMessage);
        }

        private string GetBackupTypeName(SqlVerifyAction sqlVerifyAction)
        {
            /*
            D = Database.
            I = Database Differential.
            L = Log.
            F = File or Filegroup.
            */

            switch (sqlVerifyAction)
            {
                case SqlVerifyAction.VerifyDatabase: return "D";

                case SqlVerifyAction.VerifyFile: return "F";

                case SqlVerifyAction.VerifyIncremental: return "I";

                case SqlVerifyAction.VerifyLog: return "L";

                default: throw new InternalSmoErrorException(ExceptionTemplates.UnknownEnumeration("SqlVerifyAction"));
            }
        }

        /// <summary>
        /// Runs Verify operation in an asynchronous way, i.e.
        /// the call returns immediately and verify operation
        /// continues in background.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        public void SqlVerifyAsync(Server srv)
        {
            SqlVerifyAsync(srv, false);
        }

        /// <summary>
        /// Runs Verify operation in an asynchronous way, i.e.
        /// the call returns immediately and verify operation
        /// continues in background.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        /// <param name="loadHistory">Load history</param>
        public void SqlVerifyAsync(Server srv, bool loadHistory)
        {
            // Prepare T-SQL statement
            StringCollection queries = new StringCollection();
            queries.Add(ScriptVerify(srv, loadHistory));

            // Register for events
            //  OnBeforeSqlVerify - initializes m_bVerifySuccess variable
            //  OnInfoMessage     - sets m_bVerifySuccess if verify succeeded
            //  OnExecuteNonQueryCompleted - cleans up and sends external event with final result
            srv.ExecutionManager.ExecuteNonQueryCompleted += new ExecuteNonQueryCompletedEventHandler(OnExecuteSqlVerifyCompleted);
            srv.ExecutionManager.BeforeExecuteSql += new EventHandler(OnBeforeSqlVerify);
            this.Information += new ServerMessageEventHandler(OnInfoMessage);

            // Run T-SQL
            ExecuteSqlAsync(srv, queries);
        }

        private string ScriptVerify(Server srv, bool loadHistory)
        {
            return ScriptVerify(srv, loadHistory, false);
        }

        private string ScriptVerify(Server srv, bool loadHistory, bool ignoreFileNumber)
        {
            ServerVersion targetVersion = srv.ServerVersion;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            sb.Append("RESTORE VERIFYONLY");

            sb.Append(" FROM ");
            GetDevicesScript(sb, Devices, targetVersion);
            sb.Append(" WITH ");

            this.AddCredential(targetVersion, sb, false, true);

            if (!ignoreFileNumber)
            {
                if (0 < this.FileNumber)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " FILE = {0}, ", m_FileNumber);
                }
            }
            else
            {
                sb.Append(" FILE = @backupSetId, ");
            }


            if (UnloadTapeAfter)
            {
                sb.Append(" UNLOAD, ");
            }
            else
            {
                sb.Append(" NOUNLOAD, ");
            }

            if (loadHistory)
            {
                sb.Append(" LOADHISTORY, ");
            }

            AddPassword(targetVersion, sb, false, true);

            AddMediaPassword(targetVersion, sb, false, true);


            if (NoRewind && 7 != targetVersion.Major)
            {
                sb.Append(" NOREWIND, ");
            }

            RemoveLastComma(sb);

            return sb.ToString();
        }

        private void OnBeforeSqlVerify(object sender, EventArgs args)
        {
            this.m_bVerifySuccess = false;
        }

        private void OnInfoMessage(object sender, ServerMessageEventArgs e)
        {
            if (3262 == e.Error.Number)
            {
                this.m_bVerifySuccess = true;
            }
        }

        private void OnExecuteSqlVerifyCompleted(object sender, ExecuteNonQueryCompletedEventArgs args)
        {
            // Unregister from our internal events
            this.Information -= new ServerMessageEventHandler(OnInfoMessage);
            ExecutionManager em = sender as ExecutionManager;
            if (null != em)
            {
                em.ExecuteNonQueryCompleted -= new ExecuteNonQueryCompletedEventHandler(OnExecuteSqlVerifyCompleted);
                em.BeforeExecuteSql -= new EventHandler(OnBeforeSqlVerify);
            }

            // Check if we need to send final notification to external callers
            if (null != VerifyComplete)
            {
                VerifyComplete(this, new VerifyCompleteEventArgs(this.m_bVerifySuccess));
            }
        }

        public DataTable ReadFileList(Server srv)
        {
            ServerVersion targetVersion = srv.ServerVersion;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            sb.Append("RESTORE FILELISTONLY");

            sb.Append(" FROM ");
            GetDevicesScript(sb, Devices, targetVersion);
            sb.Append(" WITH ");

            this.AddCredential(targetVersion, sb, false, true);

            if (UnloadTapeAfter)
            {
                sb.Append(" UNLOAD, ");
            }
            else
            {
                sb.Append(" NOUNLOAD, ");
            }

            if (0 < this.FileNumber)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, " FILE = {0}, ", m_FileNumber);
            }

            AddPassword(targetVersion, sb, false, true);

            AddMediaPassword(targetVersion, sb, false, true);


            RemoveLastComma(sb);

            return ExecuteSqlWithResults(srv, sb.ToString()).Tables[0];
        }

        public DataTable ReadMediaHeader(Server srv)
        {
            ServerVersion targetVersion = srv.ServerVersion;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            sb.Append("RESTORE LABELONLY");

            sb.Append(" FROM ");
            GetDevicesScript(sb, Devices, targetVersion);
            sb.Append(" WITH ");

            this.AddCredential(targetVersion, sb, false, true);

            if (UnloadTapeAfter)
            {
                sb.Append(" UNLOAD, ");
            }
            else
            {
                sb.Append(" NOUNLOAD, ");
            }

            AddMediaPassword(targetVersion, sb, false, true);

            RemoveLastComma(sb);

            return ExecuteSqlWithResults(srv, sb.ToString()).Tables[0];
        }

        public DataTable ReadBackupHeader(Server srv)
        {
            ServerVersion targetVersion = srv.ServerVersion;
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            sb.Append("RESTORE HEADERONLY");

            sb.Append(" FROM ");
            GetDevicesScript(sb, Devices, targetVersion);
            sb.Append(" WITH ");

            this.AddCredential(targetVersion, sb, false, true);

            if (UnloadTapeAfter)
            {
                sb.Append(" UNLOAD, ");
            }
            else
            {
                sb.Append(" NOUNLOAD, ");
            }

            if (0 < this.FileNumber)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, " FILE = {0}, ", m_FileNumber);
            }

            AddPassword(targetVersion, sb, false, true);

            AddMediaPassword(targetVersion, sb, false, true);


            RemoveLastComma(sb);

            return ExecuteSqlWithResults(srv, sb.ToString()).Tables[0];
        }

        public DataTable ReadSuspectPageTable(Server server)
        {
            if (server.ServerVersion.Major < 9)
            {
                throw new FailedOperationException(ExceptionTemplates.UnsupportedVersion(server.ServerVersion.ToString())).SetHelpContext("UnsupportedVersion");
            }
            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            statement.Append("select * from msdb.dbo.suspect_pages");
            GetDbFileFilter(statement);

            return ExecuteSqlWithResults(server, statement.ToString()).Tables[0];
        }

        public void ClearSuspectPageTable(Server srv)
        {
            if (srv.ServerVersion.Major < 9)
            {
                throw new FailedOperationException(ExceptionTemplates.UnsupportedVersion(srv.ServerVersion.ToString())).SetHelpContext("UnsupportedVersion");
            }
            StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            statement.Append("delete from msdb.dbo.suspect_pages");
            GetDbFileFilter(statement);

            StringCollection queries = new StringCollection();
            queries.Add(statement.ToString());
            ExecuteSql(srv, queries);
        }

        private void GetDbFileFilter(StringBuilder selectStmt)
        {
            if (null != this.Database)
            {
                selectStmt.AppendFormat(SmoApplication.DefaultCulture, " where database_id in (select dbid from master.dbo.sysdatabases where name = N'{0}')",
                    SqlSmoObject.SqlString(this.Database));

                if (this.DatabaseFiles.Count > 1 && null != this.Offset && 0 < this.Offset.Length)
                {
                    throw new FailedOperationException(ExceptionTemplates.OneFilePageSupported).SetHelpContext("OneFilePageSupported");
                }
                if (this.DatabaseFiles.Count > 0)
                {
                    selectStmt.AppendFormat(SmoApplication.DefaultCulture, " and file_id in ( select fileid from [{0}].dbo.sysfiles where name in ( ",
                        SqlSmoObject.SqlBraket(this.Database));

                    int idx = 0;
                    foreach (string filename in this.DatabaseFiles)
                    {
                        if (idx++ > 0)
                        {
                            selectStmt.Append(Globals.commaspace);
                        }
                        selectStmt.AppendFormat(SmoApplication.DefaultCulture, "N'{0}'", SqlSmoObject.SqlString(filename));
                    }

                    selectStmt.Append(" )  )");
                }

                if (null != this.Offset && 0 < this.Offset.Length)
                {
                    selectStmt.Append(" and page_id in (");
                    int commaIdx = 0;
                    foreach (long offset in this.Offset)
                    {
                        if (commaIdx++ > 0)
                        {
                            selectStmt.Append(Globals.commaspace);
                        }
                        selectStmt.AppendFormat(SmoApplication.DefaultCulture, "0x{0:x}", offset);
                    }
                    selectStmt.Append(" ) ");
                }
            }

        }

        public StringCollection Script(Server server)
        {
            ServerVersion targetVersion = server.ServerVersion;
            StringCollection retColl = new StringCollection();

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (null == Database || Database.Length == 0)
            {
                //throw new SmoException(ExceptionTemplates.PropertyNotSet("Database", "Resrtore"));
                throw new PropertyNotSetException("Database");
            }

            bool onlineFiles = false;

            // get the header of the statements
            switch (m_RestoreAction)
            {
                case RestoreActionType.Database:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "RESTORE DATABASE [{0}]", SqlSmoObject.SqlBraket(Database));
                    break;
                case RestoreActionType.OnlineFiles:
                    onlineFiles = true;
                    goto case RestoreActionType.Files;
                case RestoreActionType.Files:
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "RESTORE DATABASE [{0}]", SqlSmoObject.SqlBraket(Database));
                        int ncnt = 0;
                        foreach (string filename in this.DatabaseFiles)
                        {
                            if (ncnt++ > 0)
                            {
                                sb.Append(Globals.commaspace);
                            }
                            sb.AppendFormat(SmoApplication.DefaultCulture, " FILE = N'{0}'", SqlSmoObject.SqlString(filename));
                        }

                        foreach (string fgname in this.DatabaseFileGroups)
                        {
                            if (ncnt++ > 0)
                            {
                                sb.Append(Globals.commaspace);
                            }
                            sb.AppendFormat(SmoApplication.DefaultCulture, " FILEGROUP = N'{0}'", SqlSmoObject.SqlString(fgname));
                        }
                        break;
                    }
                case RestoreActionType.Log:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "RESTORE LOG [{0}]", SqlSmoObject.SqlBraket(Database));
                    break;
                case RestoreActionType.OnlinePage:
                    {
                        Database db = server.Databases[this.Database];
                        if (null == db)
                        {
                            throw new PropertyNotSetException("Database");
                        }
                        if (this.DatabasePages.Count == 0)
                        {
                            throw new PropertyNotSetException("DatabasePages");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture, "RESTORE DATABASE [{0}] PAGE=", SqlSmoObject.SqlBraket(Database));

                        StringBuilder pageStrBuilder = new StringBuilder();
                        foreach (SuspectPage dbPage in this.DatabasePages)
                        {
                            pageStrBuilder.Append(dbPage.ToString() + ", ");
                        }
                        RemoveLastComma(pageStrBuilder);
                        sb.AppendFormat(SmoApplication.DefaultCulture, "'{0}'", pageStrBuilder.ToString());

                    }
                    break;

            }

            // get backupdevices, do some validations first
            if (Devices.Count == 0)
            {
                if (m_RestoreAction == RestoreActionType.Log && this.FileNumber == 0)
                {
                    retColl.Add(sb.ToString());
                    return retColl;
                }
                throw new PropertyNotSetException("Devices");
            }

            sb.Append(" FROM ");
            GetDevicesScript(sb, Devices, targetVersion);

            // Managed Instances currently do not support any of the WITH clause statements.
            // We're skipping this entire block of code.
            //
            if (server.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
            {
                sb.Append(" WITH ");

                this.AddCredential(targetVersion, sb, false, true);

                if (onlineFiles)
                {
                    sb.Append(" ONLINE, ");
                }

                bool bPartialRestore = false;

                if (this.Partial && 7 != targetVersion.Major && RestoreActionType.Log != m_RestoreAction)
                {
                    bPartialRestore = true;
                    sb.Append(" PARTIAL, ");
                }

                if (m_RestrictedUser && 7 != targetVersion.Major) //cannot have partial and restricted_user
                {
                    sb.Append(" RESTRICTED_USER, ");
                }

                if (m_RestrictedUser && 7 == targetVersion.Major)
                {
                    sb.Append(" DBO_ONLY, ");
                }

                if (0 < this.FileNumber)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " FILE = {0}, ", m_FileNumber);
                }

                AddPassword(targetVersion, sb, false, true);

                if (IsStringValid(MediaName))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " MEDIANAME = N'{0}', ", SqlSmoObject.SqlString(MediaName));
                }

                AddMediaPassword(targetVersion, sb, false, true);


                if (m_RelocateFiles.Count > 0)
                {
                    foreach (RelocateFile rf in m_RelocateFiles)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " MOVE N'{0}' TO N'{1}', ", SqlSmoObject.SqlString(rf.LogicalFileName), SqlSmoObject.SqlString(rf.PhysicalFileName));
                    }
                }

                if (!bPartialRestore && RestoreActionType.Files != m_RestoreAction)
                {
                    if (m_KeepReplication && 7 != targetVersion.Major)
                    {
                        sb.Append(" KEEP_REPLICATION, ");
                    }
                }

                if (!bPartialRestore && RestoreActionType.Files != m_RestoreAction)
                {
                    if (m_KeepTemporalRetention && 12 < targetVersion.Major)
                    {
                        // Available in SQL2016+ (v13+)
                        //
                        sb.Append(" KEEP_TEMPORAL_RETENTION, ");
                    }
                }

                bool norecSet = false;
                // STANDBY can be set for database and log backups, and takes precedence over 
                // NORECOVERY
                if (RestoreActionType.Database == m_RestoreAction || RestoreActionType.Log == m_RestoreAction)
                {
                    if (IsStringValid(m_StandbyFile))
                    {
                        norecSet = true;
                        sb.AppendFormat(SmoApplication.DefaultCulture, " STANDBY = N'{0}', ", SqlSmoObject.SqlString(m_StandbyFile));
                    }

                }
                if (!norecSet && NoRecovery)
                {
                    sb.Append(" NORECOVERY, ");
                }

                if (NoRewind && 7 != targetVersion.Major)
                {
                    sb.Append(" NOREWIND, ");
                }

                if (UnloadTapeAfter)
                {
                    sb.Append(" UNLOAD, ");
                }
                else
                {
                    sb.Append(" NOUNLOAD, ");
                }

                if (m_ReplaceDatabase && RestoreActionType.Log != m_RestoreAction)
                {
                    sb.Append(" REPLACE, ");
                }

                if (Restart)
                {
                    sb.Append(" RESTART, ");
                }

                if (PercentCompleteNotification > 0 && PercentCompleteNotification <= 100)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " STATS = {0}, ", PercentCompleteNotification);
                }

                // STOPAT*** are valid for log restores or for database restore on 9.0
                if (RestoreActionType.Log == m_RestoreAction ||
                    (targetVersion.Major >= 9 && RestoreActionType.Database == m_RestoreAction))
                {
                    if (IsStringValid(m_ToPointInTime))
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, " STOPAT = N'{0}', ", SqlSmoObject.SqlString(m_ToPointInTime));
                    }
                    else
                    {
                        if (this.BackupSet != null && this.BackupSet.StopAtLsn != 0m)
                        {
                            m_StopBeforeMarkName = string.Format(SmoApplication.DefaultCulture, "lsn:{0}", this.BackupSet.StopAtLsn);
                        }
                        if (7 < targetVersion.Major)
                        {
                            if (IsStringValid(m_StopAtMarkName))
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, " STOPATMARK = N'{0}'", SqlSmoObject.SqlString(m_StopAtMarkName));

                                if (IsStringValid(m_StopAtMarkAfterDate))
                                {
                                    sb.AppendFormat(SmoApplication.DefaultCulture, " AFTER N'{0}'", SqlSmoObject.SqlString(m_StopAtMarkAfterDate));
                                }
                                sb.Append(", ");
                            }
                            else if (IsStringValid(m_StopBeforeMarkName))
                            {
                                sb.AppendFormat(SmoApplication.DefaultCulture, " STOPBEFOREMARK = N'{0}'", SqlSmoObject.SqlString(m_StopBeforeMarkName));

                                if (IsStringValid(m_StopBeforeMarkAfterDate))
                                {
                                    sb.AppendFormat(SmoApplication.DefaultCulture, " AFTER N'{0}'", SqlSmoObject.SqlString(m_StopBeforeMarkAfterDate));
                                }
                                sb.Append(", ");
                            }
                        }
                    }
                }

                // BlockSize
                if (this.BlockSize > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " BLOCKSIZE = {0}", this.BlockSize);
                    sb.Append(Globals.commaspace);
                }

                // BufferCount
                if (this.BufferCount > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " BUFFERCOUNT = {0}", this.BufferCount);
                    sb.Append(Globals.commaspace);
                }

                // MaxTransferSize
                if (this.MaxTransferSize > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, " MAXTRANSFERSIZE = {0}", this.MaxTransferSize);
                    sb.Append(Globals.commaspace);
                }

                // Checksum
                if (9 <= targetVersion.Major && Checksum)
                {
                    sb.Append(" CHECKSUM, ");
                }

                // ContinueAfterError
                if (9 <= targetVersion.Major && ContinueAfterError)
                {
                    sb.Append(" CONTINUE_AFTER_ERROR, ");
                }

                // Restore options
                if (!String.IsNullOrEmpty(Options) && 16 <= targetVersion.Major)
                {
                    sb.Append(FormattableString.Invariant($" RESTORE_OPTIONS = '{Options}', "));
                }

                RemoveLastComma(sb);
            }

            retColl.Add(sb.ToString());

            // Add another statement to clean up suspect page table (if requested)
            if (this.clearSuspectPageTableAfterRestore)
            {
                if (server.ServerVersion.Major < 9)
                {
                    throw new FailedOperationException(ExceptionTemplates.UnsupportedVersion(server.ServerVersion.ToString())).SetHelpContext("UnsupportedVersion");
                }

                StringBuilder statement = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                statement.Append("delete from msdb.dbo.suspect_pages");
                GetDbFileFilter(statement);

                retColl.Add(statement.ToString());
            }

            return retColl;
        }

        /// <summary>
        /// Runs Restore operation in a synchronous way, i.e. 
        /// the call blocks until verification is completed.
        /// </summary>
        /// <param name="srv">Server to run restore on.</param>
        public void SqlRestore(Server srv)
        {
            try
            {
                // Run T-SQL
                ExecuteSql(srv, Script(srv));

                if (!srv.ExecutionManager.Recording && (Action == RestoreActionType.Database || Action == RestoreActionType.Log))
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(srv,
                                new DatabaseEventArgs(srv.Databases[this.Database].Urn,
                                                srv.Databases[this.Database],
                                                this.Database,
                                                DatabaseEventType.Restore));
                    }
                }

            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Restore, srv, e);
            }
        }

        /// <summary>
        /// Runs Restore operation in an asynchronous way, i.e.
        /// the call returns immediately and verify operation
        /// continues in background.
        /// </summary>
        /// <param name="srv">Server to run the operation on.</param>
        public void SqlRestoreAsync(Server srv)
        {
            try
            {
                currentAsyncOperation = AsyncOperation.Restore;

                // Run T-SQL
                ExecuteSqlAsync(srv, Script(srv));
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.RestoreAsync, srv, e);
            }
        }

        void RemoveLastComma(StringBuilder sb)
        {
            if (0 <= sb.Length - 2 && ',' == sb[sb.Length - 2])
            {
                sb.Remove(sb.Length - 2, 2);
            }
        }

        bool m_Partial;
        public bool Partial
        {
            get
            {
                return m_Partial;
            }
            set
            {
                m_Partial = value;
            }
        }

        bool m_RestrictedUser;
        public bool RestrictedUser
        {
            get
            {
                return m_RestrictedUser;
            }
            set
            {
                m_RestrictedUser = value;
            }
        }

        int m_FileNumber;
        public int FileNumber
        {
            get
            {
                return m_FileNumber;
            }
            set
            {
                m_FileNumber = value;
            }
        }

        ArrayList m_RelocateFiles;
        public ArrayList RelocateFiles
        {
            get
            {
                return m_RelocateFiles;
            }
        }

        bool m_KeepReplication;
        public bool KeepReplication
        {
            get
            {
                return m_KeepReplication;
            }
            set
            {
                m_KeepReplication = value;
            }
        }

        bool m_KeepTemporalRetention;
        public bool KeepTemporalRetention
        {
            get
            {
                return m_KeepTemporalRetention;
            }
            set
            {
                m_KeepTemporalRetention = value;
            }
        }

        string m_StandbyFile;
        public string StandbyFile
        {
            get
            {
                return m_StandbyFile;
            }
            set
            {
                m_StandbyFile = value;
            }
        }

        bool m_ReplaceDatabase;
        public bool ReplaceDatabase
        {
            get
            {
                return m_ReplaceDatabase;
            }
            set
            {
                m_ReplaceDatabase = value;
            }
        }

        string m_ToPointInTime;
        public string ToPointInTime
        {
            get
            {
                return m_ToPointInTime;
            }
            set
            {
                m_ToPointInTime = value;
            }
        }

        string m_StopAtMarkName;
        public string StopAtMarkName
        {
            get
            {
                return m_StopAtMarkName;
            }
            set
            {
                m_StopAtMarkName = value;
            }
        }

        string m_StopAtMarkAfterDate;
        public string StopAtMarkAfterDate
        {
            get
            {
                return m_StopAtMarkAfterDate;
            }
            set
            {
                m_StopAtMarkAfterDate = value;
            }
        }

        string m_StopBeforeMarkName;
        public string StopBeforeMarkName
        {
            get
            {
                return m_StopBeforeMarkName;
            }
            set
            {
                m_StopBeforeMarkName = value;
            }
        }

        string m_StopBeforeMarkAfterDate;
        public string StopBeforeMarkAfterDate
        {
            get
            {
                return m_StopBeforeMarkAfterDate;
            }
            set
            {
                m_StopBeforeMarkAfterDate = value;
            }
        }

        public RestoreActionType Action
        {
            get
            {
                return m_RestoreAction;
            }
            set
            {
                m_RestoreAction = value;
            }
        }

        private List<SuspectPage> databasePages = new List<SuspectPage>();
        /// <summary>
        /// Gets the restore pages.
        /// </summary>
        /// <value>The restore pages.</value>
        public List<SuspectPage> DatabasePages
        {
            get
            {
                return this.databasePages;
            }
        }

        long[] offset = null;
        public long[] Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        bool clearSuspectPageTableAfterRestore = false;
        public bool ClearSuspectPageTableAfterRestore
        {
            get { return clearSuspectPageTableAfterRestore; }
            set { clearSuspectPageTableAfterRestore = value; }
        }
    }

    public enum RestoreActionType
    {
        Database,
        Files,
        OnlinePage,
        OnlineFiles,
        Log
    }

    /// <summary>
    /// Specifies what needs to be verified 
    /// </summary>
    public enum SqlVerifyAction
    {
        /// <summary>
        /// Verify latest database backup
        /// </summary>
        VerifyDatabase = 0,
        /// <summary>
        /// Verify latest log backup
        /// </summary>
        VerifyLog = 1,
        /// <summary>
        /// Verify latest File or FileGroup backup
        /// </summary>
        VerifyFile = 2,
        /// <summary>
        /// Verify latest incremental backup
        /// </summary>
        VerifyIncremental = 3
    }



    public class RelocateFile
    {
        private string m_LogicalFileName;
        private string m_PhysicalFileName;

        public RelocateFile()
        {
        }

        public RelocateFile(string logicalFileName, string physicalFileName)
        {
            m_LogicalFileName = logicalFileName;
            m_PhysicalFileName = physicalFileName;
        }

        public string LogicalFileName
        {
            get
            {
                return m_LogicalFileName;
            }
            set
            {
                m_LogicalFileName = value;
            }
        }

        public string PhysicalFileName
        {
            get
            {
                return m_PhysicalFileName;
            }
            set
            {
                m_PhysicalFileName = value;
            }
        }
    }

    /// <summary>
    /// Event signature for VerifyComplete event
    /// </summary>
    public delegate void VerifyCompleteEventHandler(object sender, VerifyCompleteEventArgs args);

    /// <summary>
    /// Event argument class for VerifyCompleteEventHandler
    /// </summary>
    public sealed class VerifyCompleteEventArgs : EventArgs
    {
        internal VerifyCompleteEventArgs(bool verifySuccess)
        {
            this.verifySuccess = verifySuccess;
        }

        /// <summary>
        /// Indicates whether the verify operation was successful
        /// </summary>
        public bool VerifySuccess
        {
            get { return this.verifySuccess; }
        }

        private bool verifySuccess;
    }

}

