// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public sealed class Backup : BackupRestoreBase
    {
        public Backup() : base()
        {
            // initialize some default member values
            m_BackupAction = BackupActionType.Database;
            m_RetainDays = -1;

            m_Initialize = false;
            m_SkipTapeHeader = false;
            m_ExpirationDate = DateTime.MinValue;
            m_LogTruncation = BackupTruncateLogType.Truncate;
            m_Incremental = false;

            copyOnly = false;

            // on Yukon we'll have just three mirrors
            this.mirrors = new BackupDeviceList[MIRRORS_COUNT];
            for( int i = 0; i < this.mirrors.Length; ++i )
            {
                this.mirrors[i] = new BackupDeviceList();
            }
        }

        
        internal Backup(bool checkForHADRMaintPlan)
            : this()
        {
            this.m_checkForHADRMaintPlan = checkForHADRMaintPlan;
        }

        private void ThrowIfUsingRemovedFeature(Server srv)
        {
            if (BackupTruncateLogType.TruncateOnly == m_LogTruncation && 10 <= srv.ServerVersion.Major)
            {
                throw new UnsupportedVersionException(
                                ExceptionTemplates.InvalidPropertyValueForVersion(
                                this.GetType().Name,
                                "LogTruncation",
                                BackupTruncateLogType.TruncateOnly.ToString(),
                                SqlSmoObject.GetSqlServerName(srv)));
            }
        }

        /// <summary>
        /// Runs backup operation synchronously, i.e. the function call
        /// blocks untill the backup is done.
        /// </summary>
        /// <param name="srv">Server to run backup on.</param>

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public void SqlBackup(Server srv)
        {
            if (null == srv)
            {
                throw new FailedOperationException(ExceptionTemplates.BackupFailed, new ArgumentException("srv"));
            }
            ThrowIfUsingRemovedFeature(srv);

            try
            {
                StringCollection queries = new StringCollection();
                queries.Add(Script(srv));

                ExecuteSql(srv, queries);
                if (!srv.ExecutionManager.Recording && Action == BackupActionType.Log && LogTruncation == BackupTruncateLogType.NoTruncate)
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(srv,
                                new DatabaseEventArgs(srv.Databases[this.Database].Urn,
                                                srv.Databases[this.Database],
                                                this.Database,
                                                DatabaseEventType.Backup));
                    }
                }
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Backup, srv, e);
            }
        }

        /// <summary>
        /// Runs backup operation asynchronously, i.e. the call returns
        /// immediately, and the backup operation runs in the background
        /// </summary>
        /// <param name="srv">Server to run backup on</param>

        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public void SqlBackupAsync(Server srv)
        {
            if (null == srv)
            {
                throw new FailedOperationException(ExceptionTemplates.BackupFailed, new ArgumentException("srv"));
            }
            ThrowIfUsingRemovedFeature(srv);

            try
            {
                StringCollection queries = new StringCollection();
                queries.Add(Script(srv));

                currentAsyncOperation = AsyncOperation.Backup;

                ExecuteSqlAsync(srv, queries);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Backup, srv, e);
            }
        }

        /// <summary>
        /// Generates script for the current backup operation
        /// </summary>
        /// <param name="targetServer"></param>
        /// <returns></returns>
        public string Script(Server targetServer)
        {
            ThrowIfUsingRemovedFeature(targetServer);

            ServerVersion targetVersion = targetServer.ServerVersion;
            DatabaseEngineType targetEngineType = targetServer.DatabaseEngineType;
            DatabaseEngineEdition targetEngineEdition = targetServer.DatabaseEngineEdition;

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            if (null == Database || Database.Length == 0)
            {
                throw new PropertyNotSetException("Database");
            }

            // get the header of the statements
            switch (m_BackupAction)
            {
                case BackupActionType.Database:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "BACKUP DATABASE [{0}]", SqlSmoObject.SqlBraket(Database));
                    break;
                case BackupActionType.Files:
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "BACKUP DATABASE [{0}]", SqlSmoObject.SqlBraket(Database));
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
                /*
                case BackupActionType.Incremental : 
                    sb.AppendFormat(SmoApplication.DefaultCulture, "BACKUP DATABASE [{0}]", SqlSmoObject.SqlBraket(Database));
                    break;
                    */
                case BackupActionType.Log:
                    sb.AppendFormat(SmoApplication.DefaultCulture, "BACKUP LOG [{0}]", SqlSmoObject.SqlBraket(Database));
                    if (LogTruncation == BackupTruncateLogType.TruncateOnly)
                    {
                        sb.Append(" WITH NO_LOG ");
                        return sb.ToString();
                    }
                    break;
            }

            // get backupdevices, do some validations first
            if (Devices.Count == 0)
            {
                throw new PropertyNotSetException("Devices");
            }

            sb.Append(" TO ");

            GetDevicesScript(sb, Devices, targetVersion);

            // script Mirrors, if it the case
            if (targetVersion.Major >= 9)
            {
                foreach (BackupDeviceList bd in Mirrors)
                {
                    if (null == bd)
                    {
                        continue;
                    }
                    if (bd.Count > 0)
                    {
                        // when the mirror does not have enough devices throw exception
                        if (bd.Count != Devices.Count)
                        {
                            throw new WrongPropertyValueException(ExceptionTemplates.MismatchingNumberOfMirrors(Devices.Count, bd.Count));
                        }
                        sb.Append(" MIRROR TO ");
                        GetDevicesScript(sb, bd, targetVersion);
                    }
                }
            }

            sb.Append(" WITH ");
            int nWithCnt = 0;

            // Backup to Cloud in SQL 11 - Add credential name if CredentialName was set
            if (this.AddCredential(targetVersion, sb, nWithCnt > 0, false))
            {
                nWithCnt++;
            }

            // add some more things depending on the action type
            if ((m_BackupAction == BackupActionType.Database || m_BackupAction == BackupActionType.Files) &&
                m_Incremental)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append(" DIFFERENTIAL ");
            }

            if (m_BackupAction == BackupActionType.Log &&
                m_LogTruncation == BackupTruncateLogType.NoTruncate)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append(" NO_TRUNCATE ");
            }

            // BlockSize
            //
            if (this.BlockSize <= 0 && targetEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                // Managed Instances support only backups to URL (blob) destinations. Transfer speed can be
                // greatly increased if proper values are set for BLOCKSIZE and MAXTRANSFERSIZE arguments.
                // If user hasn't specify them explicitly, let's set them here as good default values.
                //
                // https://docs.microsoft.com/sql/relational-databases/backup-restore/sql-server-backup-to-url
                //
                this.BlockSize = 65536;
            }

            if (this.BlockSize > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " BLOCKSIZE = {0}", this.BlockSize);
            }

            // BufferCount
            if (this.BufferCount > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " BUFFERCOUNT = {0}", this.BufferCount);
            }

            // MaxTransferSize
            //
            if (this.MaxTransferSize <= 0 && targetEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
            {
                // Managed Instances support only backups to URL (blob) destinations. Transfer speed can be
                // greatly increased if proper values are set for BLOCKSIZE and MAXTRANSFERSIZE arguments.
                // If user hasn't specify them explicitly, let's set them here as good default values.
                //
                // https://docs.microsoft.com/en-us/sql/relational-databases/backup-restore/sql-server-backup-to-url?view=sql-server-2017
                //
                this.MaxTransferSize = 4194304;
            }

            if (this.MaxTransferSize > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " MAXTRANSFERSIZE = {0}", this.MaxTransferSize);
            }

            // COPY_ONLY Option
            if (9 <= targetVersion.Major && this.copyOnly)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " COPY_ONLY");
            }

            // BackupSetDescription
            if (null != m_BackupSetDescription && m_BackupSetDescription.Length > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " DESCRIPTION = N'{0}'", SqlSmoObject.SqlString(m_BackupSetDescription));
            }

            // ExpirationDate - takes precedence over retain days
            if (m_ExpirationDate != DateTime.MinValue)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " EXPIREDATE = N'{0}'", m_ExpirationDate);
            }
            else if (m_RetainDays > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " RETAINDAYS = {0}", m_RetainDays);
            }

            // Password
            if (AddPassword(targetVersion, sb, nWithCnt > 0, false))
            {
                nWithCnt++;
            }

            // FormatMedia
            if (nWithCnt++ > 0)
            {
                sb.Append(Globals.commaspace);
            }
            sb.Append(m_FormatMedia ? "FORMAT" : "NOFORMAT");

            // FormatMedia==true conflicts with Initialize and SkipTapeHeader == false
            if (m_FormatMedia)
            {
                if (!m_Initialize)
                {
                    throw new SmoException(ExceptionTemplates.ConflictingSwitches("FormatMedia", "Initialize"));
                }

                if (!m_SkipTapeHeader)
                {
                    throw new SmoException(ExceptionTemplates.ConflictingSwitches("FormatMedia", "SkipTapeHeader"));
                }
            }

            // Initialize

            if (nWithCnt++ > 0)
            {
                sb.Append(Globals.commaspace);
            }
            sb.Append(m_Initialize ? "INIT" : "NOINIT");

            // MediaDescription
            if (null != m_MediaDescription && m_MediaDescription.Length > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " MEDIADESCRIPTION = N'{0}'", SqlSmoObject.SqlString(m_MediaDescription));
            }

            // MediaName
            if (null != MediaName && MediaName.Length > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " MEDIANAME = N'{0}'", SqlSmoObject.SqlString(MediaName));
            }

            // MediaPassword
            if (AddMediaPassword(targetVersion, sb, nWithCnt > 0, false))
            {
                nWithCnt++;
            }


            // BackupSetName
            if (null != m_BackupSetName && m_BackupSetName.Length > 0)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " NAME = N'{0}'", SqlSmoObject.SqlString(m_BackupSetName));
            }

            // SkipTapeHeader
            if (nWithCnt++ > 0)
            {
                sb.Append(Globals.commaspace);
            }
            sb.Append(m_SkipTapeHeader ? "SKIP" : "NOSKIP");

            // NoRewind
            if (7 != targetVersion.Major)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append(NoRewind ? "NOREWIND" : "REWIND");
            }


            // UnloadTapeAfter

            if (nWithCnt++ > 0)
            {
                sb.Append(Globals.commaspace);
            }
            sb.Append(UnloadTapeAfter ? "UNLOAD" : "NOUNLOAD");

            bool norecSet = false;
            // STANDBY can be set for database and log backups, and takes precedence over 
            // NORECOVERY
            if (BackupActionType.Log == m_BackupAction)
            {
                if (IsStringValid(m_UndoFileName))
                {
                    norecSet = true;
                    if (nWithCnt++ > 0)
                    {
                        sb.Append(Globals.commaspace);
                    }
                    sb.AppendFormat(SmoApplication.DefaultCulture, " STANDBY = N'{0}' ", SqlSmoObject.SqlString(m_UndoFileName));
                }

            }
            if (BackupActionType.Log == m_BackupAction && !norecSet && true == NoRecovery)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append(" NORECOVERY ");
            }

            // Restart
            if (Restart)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append("RESTART");
            }

            if (10 <= targetVersion.Major)
            {
                if (!Enum.IsDefined(typeof(BackupCompressionOptions), m_CompressionOption))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.UnknownEnumeration("BackupCompressionOptions"));
                }

                if (BackupCompressionOptions.Default != m_CompressionOption)
                {
                    sb.Append(Globals.commaspace);
                    sb.AppendFormat((m_CompressionOption == BackupCompressionOptions.On) ? "COMPRESSION" : "NO_COMPRESSION");
                }
            }
            else
            {
                if (this.backupCompValueSetByUser)
                {
                    string version = LocalizableResources.ServerYukon;
                    if (targetVersion.Major == 8)
                    {
                        version = LocalizableResources.ServerShiloh;
                    }

                    throw new UnknownPropertyException("CompressionOption", ExceptionTemplates.PropertyNotAvailableToWrite("CompressionOption", version));
                }
            }

            // Encryption
            if (VersionUtils.IsSql12OrLater(targetVersion))
            {
                if (null != m_EncryptionOption)
                {
                    if (nWithCnt++ > 0)
                    {
                        sb.Append(Globals.commaspace);
                    }

                    sb.Append(m_EncryptionOption.Script());
                }
            }
            else
            {
                // encryption is not supported on a version earlier than SQL14
                if (null != m_EncryptionOption)
                {
                    throw new UnsupportedVersionException(ExceptionTemplates.BackupEncryptionNotSupported);
                }
            }

            // PercentCompleteNotification
            if (PercentCompleteNotification > 0 && PercentCompleteNotification <= 100)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.AppendFormat(SmoApplication.DefaultCulture, " STATS = {0}", PercentCompleteNotification);
            }

            // Checksum
            if (9 <= targetVersion.Major && Checksum)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append("CHECKSUM");
            }

            // ContinueAfterError
            if (9 <= targetVersion.Major && ContinueAfterError)
            {
                if (nWithCnt++ > 0)
                {
                    sb.Append(Globals.commaspace);
                }
                sb.Append("CONTINUE_AFTER_ERROR");
            }

            try
            {
                sb = this.CheckForHADRMaintPlan(targetServer, sb);
            }
            catch (Exception e)
            {
                SqlSmoObject.FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Backup, targetServer, e);
            }

            return sb.ToString();
        }

        public BackupActionType Action
        {
            get 
            {
                return m_BackupAction;
            }
            set
            {
                m_BackupAction = value;
            }
        }

        private  string m_BackupSetDescription;
        public string BackupSetDescription
        {
            get
            {
                return m_BackupSetDescription;
            }
            set
            {
                m_BackupSetDescription = value;
            }
        }

        private  string m_BackupSetName;
        public string BackupSetName
        {
            get
            {
                return m_BackupSetName;
            }
            set
            {
                m_BackupSetName = value;
            }
        }

        private  DateTime m_ExpirationDate;
        public DateTime ExpirationDate
        {
            get
            {
                return m_ExpirationDate;
            }
            set
            {
                m_ExpirationDate = value;
            }
        }

        private  bool m_FormatMedia;
        public bool FormatMedia
        {
            get
            {
                return m_FormatMedia;
            }
            set
            {
                m_FormatMedia = value;
            }
        }

        private  bool m_Initialize;
        public bool Initialize
        {
            get
            {
                return m_Initialize;
            }
            set
            {
                m_Initialize = value;
            }
        }

        private  string m_MediaDescription;
        public string MediaDescription
        {
            get
            {
                return m_MediaDescription;
            }
            set
            {
                m_MediaDescription = value;
            }
        }

        private  int m_RetainDays;
        public int RetainDays
        {
            get
            {
                return m_RetainDays;
            }
            set
            {
                m_RetainDays = value;
            }
        }

        private  bool m_SkipTapeHeader;
        public bool SkipTapeHeader
        {
            get
            {
                return m_SkipTapeHeader;
            }
            set
            {
                m_SkipTapeHeader = value;
            }
        }

        public BackupTruncateLogType LogTruncation
        {
            get
            {
                return m_LogTruncation;
            }
            set
            {
                m_LogTruncation = value;
            }
        }

        /// <summary>
        /// The CopyOnly property specifies backup CopyOnly option
        /// </summary>
        private bool copyOnly;
        public bool CopyOnly
        {
            get
            {
                return this.copyOnly;
            }
            set
            {
                this.copyOnly = value;
            }
        }


        private bool m_Incremental;
        public bool Incremental
        {
            get { return m_Incremental; }
            set { m_Incremental = value; }
        }
        
        // Mirrors
        private BackupDeviceList[] mirrors;

        /// <summary>
        /// Mirrors
        /// </summary>
#if CLSCOMPLIANT
        [CLSCompliant(false)]
#endif
        public BackupDeviceList[] Mirrors
        {
            get
            {
                return mirrors;
            }
            set
            {
                if (null == value)
                {
                    throw new FailedOperationException(ExceptionTemplates.SetMirrors, this,
                                new ArgumentNullException("Mirrors"));
                }

                this.mirrors = value;
            }
        }

        // this is the hardcoded number of mirrors supported by the current version
        private static int MIRRORS_COUNT = 3;

        string m_UndoFileName;
        public string UndoFileName
        {
            get
            {
                return m_UndoFileName;
            }
            set
            {
                m_UndoFileName = value;
            }
        }

        // Always set srver default value as backup compression option
        private BackupCompressionOptions m_CompressionOption = BackupCompressionOptions.Default;
        private bool backupCompValueSetByUser = false;

        /// <summary>
        /// The CompressionOption property specifies backup compression option
        /// </summary>
        public BackupCompressionOptions CompressionOption
        {
            get
            {
                return m_CompressionOption;
            }
            set
            {
                this.backupCompValueSetByUser = true;
                m_CompressionOption = value;
            }
        }

        private BackupEncryptionOptions m_EncryptionOption;
        /// <summary>
        /// The EncryptionOption property specifies backup encryption option.
        /// </summary>
        public BackupEncryptionOptions EncryptionOption
        {
            get
            {
                return m_EncryptionOption;
            }
            set
            {
                m_EncryptionOption = value;
            }
        }
    }

    public enum BackupTruncateLogType
    {
        TruncateOnly,
        NoTruncate,
        Truncate
    }

    public enum BackupActionType
    {
        Database,
        Files,
        Log
    }

    /// <summary>
    /// The BackupCompressionOptions enumeration contains values that are
    /// used to specify a backup compression option
    /// </summary>
    public enum BackupCompressionOptions
    {
        Default,
        On,
        Off
    }
}
