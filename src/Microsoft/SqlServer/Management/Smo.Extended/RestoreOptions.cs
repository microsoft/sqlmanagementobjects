// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    public class RestoreOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestoreOptions"/> class.
        /// </summary>
        public RestoreOptions()
        { }

        private bool keepReplication = false;
        /// <summary>
        /// KEEP_REPLICATION
        /// KEEP_REPLICATION prevents replication settings
        /// from being removed when a database backup or log
        /// backup is restored on a warm standby server and
        /// the database is recovered.
        /// </summary>
        /// <value><c>true</c> if [keep replication]; otherwise, <c>false</c>.</value>
        public bool KeepReplication
        {
            get { return keepReplication; }
            set 
            {
                if (this.RecoveryState == DatabaseRecoveryState.WithNoRecovery && value)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.ConflictWithNoRecovery);
                }
                this.keepReplication = value; 
            }
        }

        private bool keepTemporalRetention = false;
        /// <summary>
        /// KEEP_TEMPORAL_RETENTION
        /// KEEP_TEMPORAL_RETENTION prevents temporal history tables retention
        /// policy setting from being removed when a database backup
        /// is restored on a warm standby server and
        /// the database is recovered.
        /// </summary>
        /// <value><c>true</c> if [keep temporal retention]; otherwise, <c>false</c>.</value>
        public bool KeepTemporalRetention
        {
            get { return keepTemporalRetention; }
            set
            {
                if (this.RecoveryState == DatabaseRecoveryState.WithNoRecovery && value)
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.ConflictWithNoRecovery);
                }
                this.keepTemporalRetention = value;
            }
        }

        private bool setRestrictedUser = false;
        /// <summary>
        /// RESTRICTED_USER
        /// Restricts access for the newly restored database to
        /// members of the db_owner, dbcreator, or sysadmin roles.
        /// </summary>
        /// <value><c>true</c> if [set restricted user]; otherwise, <c>false</c>.</value>
        public bool SetRestrictedUser
        {
            get { return setRestrictedUser; }
            set 
            {
                this.setRestrictedUser = value; 
            }
        }

        /// <summary>
        /// CONTINUE_AFTER_ERROR
        /// Specifies that the restore operation is to continue
        /// after an error is encountered.
        /// </summary>
        /// <value><c>true</c> if [continue after error]; otherwise, <c>false</c>.</value>
        public bool ContinueAfterError
        {
            get;
            set;
        }

        /// <summary>
        /// Deletes entries in the suspect page table.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [clear suspect page table after restore]; otherwise, <c>false</c>.
        /// </value>
        public bool ClearSuspectPageTableAfterRestore
        {
            get;
            set;
        }

        
        /// <value><c>true</c> if [with no recovery]; otherwise, <c>false</c>.</value>
        public DatabaseRecoveryState RecoveryState
        {
            get;
            set;
        }

        /// <summary>
        /// STANDBY
        /// Undo uncommitted transactions, but save the undo
        /// actions in a standby file that allows the recovery
        /// effects to be reversed.
        /// </summary>
        /// <value>The recovery with stand by file.</value>
        public string StandByFile
        {
            get;
            set;
        }

        private int blocksize = -1;
        /// <summary>
        /// BLOCKSIZE
        /// Specifies the physical block size, in bytes. The
        /// supported sizes are 512, 1024, 2048, 4096, 8192,
        /// 16384, 32768, and 65536 (64 KB) bytes.
        /// The default is 65536 for tape devices and 512 otherwise.
        /// If you are restoring a backup from a CD-ROM, specify BLOCKSIZE=2048.
        /// </summary>
        /// <value>The blocksize.</value>
        public int Blocksize 
        {
            get { return blocksize; }
            set { this.blocksize = value; }
        }

        private int bufferCount = -1;
        /// <summary>
        /// BUFFERCOUNT
        /// Specifies the total number of I/O buffers to be used
        /// for the restore operation. Large numbers of buffers
        /// might cause "out of memory" errors because of inadequate
        /// virtual address space in the Sqlservr.exe process.
        /// </summary>
        /// <value>The buffer count.</value>
        public int BufferCount
        {
            get { return bufferCount; }
            set { this.bufferCount = value; }
        }

        private int maxTransferSize = -1;
        /// <summary>
        /// MAXTRANSFERSIZE
        /// Specifies the largest unit of transfer in bytes to be
        /// used between the backup media and SQL Server. The
        /// possible values are multiples of 65536 bytes (64 KB)
        /// ranging up to 4194304 bytes (4 MB).
        /// </summary>
        /// <value>The size of the max transfer.</value>
        public int MaxTransferSize
        {
            get { return maxTransferSize; }
            set { this.maxTransferSize = value; }
        }

        /// <summary>
        /// REPLACE
        /// Specifies that SQL Server should create the specified
        /// database and its related files even if another database
        /// already exists with the same name. In such a case, the
        /// existing database is deleted. When the REPLACE option
        /// is not specified, a safety check occurs. This prevents
        /// overwriting a different database by accident.
        /// </summary>
        /// <value><c>true</c> if [replace database]; otherwise, <c>false</c>.</value>
        public bool ReplaceDatabase
        {
            get;
            set;
        }
        
        private int percentCompleteNotification = -1;
        /// <summary>
        /// Gets or sets the percentage interval for
        /// PercentCompleteEventHandler event handler calls for
        /// individual Restore Operations.
        /// </summary>
        /// <value>The percent complete notification.</value>
        public int PercentCompleteNotification
        {
            get { return percentCompleteNotification; }
            set { this.percentCompleteNotification = value; }
        }
    }

    /// <summary>
    /// Database recovery state.
    /// </summary>
    public enum DatabaseRecoveryState
    {
        /// <summary>
        /// RECOVERY
        /// Leave the database ready to use by rolling back 
        /// uncommitted transactions. Additional transaction 
        /// logs cannot be restored.
        /// </summary>
        WithRecovery,
        
        /// <summary>
        /// NORECOVERY
        /// Instructs the restore operation to not roll back any
        /// uncommitted transactions for another transaction log
        /// backup to be restored later. The database remains in
        /// the restoring state after the restore operation.
        /// </summary>
        WithNoRecovery,
        
        /// <summary>
        /// STANDBY
        /// Undo uncommitted transactions, but save the undo
        /// actions in a standby file that allows the recovery
        /// effects to be reversed.
        /// </summary>
        WithStandBy
    }
}
