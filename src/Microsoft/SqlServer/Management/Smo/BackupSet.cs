// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Backupset class
    /// </summary>
    public sealed class BackupSet
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupSet"/> class.
        /// Populates the properties by reading the msdb tables.
        /// </summary>
        /// <param name="parentServer">The parent server.</param>
        /// <param name="BackupSetGuid">The backup set GUID.</param>
        internal BackupSet(Server parentServer, Guid BackupSetGuid)
        {
            server = parentServer;
            backupSetGuid = BackupSetGuid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupSet"/> class.
        /// Populates the proerties by reading the media headers.
        /// </summary>
        /// <param name="parentServer">The parent server.</param>
        /// <param name="mediaSet">The media set.</param>
        /// <param name="dr">The datarow.</param>
        internal BackupSet(Server parentServer, BackupMediaSet mediaSet, DataRow dr)
        {
            server = parentServer;
            backupMediaSet = mediaSet;
            PopulateFromDevice(dr);
        }

        /// <summary>
        /// Used by DatabaseRestorePlanner to specify that this
        /// backupset restore should stop at this LSN.
        /// </summary>
        internal Decimal StopAtLsn = 0m;
        /// <summary>
        /// ID used by the Database Restore Planner. 
        /// </summary>
        internal int ID = 0;

        /// <summary>
        /// Is the properties populated
        /// </summary>
        private bool isPopulated = false;

        /// <summary>
        /// Gets the target server version.
        /// </summary>
        /// <value>The target server version.</value>
        private int targetServerVersion
        {
            get { return server.Version.Major; }
        }

        internal Server server;
        /// <summary>
        /// Gets Parent of the object
        /// </summary>
        /// <value>The parent.</value>
        public Server Parent
        {
            get
            {
                //CheckObjectState();
                return server;
            }
        }

        internal string name;
        /// <summary>
        /// Gets name of the backup set.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                Populate();
                return name;
            }
        }

        internal string description;
        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public string Description
        {
            get
            {
                Populate();
                return description;
            }
        }

        internal BackupSetType backupSetType;
        /// <summary>
        /// Gets the Backup set type. Can be:
        /// Database, Differential, Log, FileOrFilegroup
        /// </summary>
        /// <value>The type of the backup set.</value>
        public BackupSetType BackupSetType
        {
            get
            {
                Populate();
                return backupSetType;
            }
        }

        internal DateTime backupStartDate;
        /// <summary>
        /// Gets Datetime when the backup operation started.
        /// </summary>
        /// <value>The backup start date.</value>
        public DateTime BackupStartDate
        {
            get
            {
                Populate();
                return backupStartDate;
            }
        }

        internal DateTime backupFinishDate;
        /// <summary>
        /// Date and time the backup operation finished.
        /// </summary>
        /// <value>The backup finish date.</value>
        public DateTime BackupFinishDate
        {
            get
            {
                Populate();
                return backupFinishDate;
            }
        }

        internal DateTime expirationDate;
        /// <summary>
        /// Date and time the backup set expires.
        /// </summary>
        /// <value>The expiration date.</value>
        public DateTime ExpirationDate
        {
            get
            {
                Populate();
                return expirationDate;
            }
        }

        internal int position;
        /// <summary>
        /// Backup set position used in the restore operation
        /// to locate the position of appropriate backup set in the file.
        /// </summary>
        /// <value>The position.</value>
        public int Position
        {
            get
            {
                Populate();
                return position;
            }
        }

        internal string databaseName;
        /// <summary>
        /// Name of the database involved in the backup operation.
        /// </summary>
        /// <value>The name of the database.</value>
        public string DatabaseName
        {
            get
            {
                Populate();
                return databaseName;
            }
        }

        internal string serverName;
        /// <summary>
        /// Name of the server where the Backup was taken.
        /// </summary>
        /// <value>The name of the server.</value>
        public string ServerName
        {
            get
            {
                Populate();
                return serverName;
            }
        }

        internal string machineName;
        /// <summary>
        /// Name of the computer where the Backup was taken.
        /// </summary>
        /// <value>The name of the machine.</value>
        public string MachineName
        {
            get
            {
                Populate();
                return machineName;
            }
        }

        internal string userName;
        /// <summary>
        /// Name of the user who performed the backup operation.
        /// </summary>
        /// <value>The name of the user.</value>
        public string UserName
        {
            get
            {
                Populate();
                return userName;
            }
        }

        internal ServerVersion serverVersion;
        /// <summary>
        /// Microsoft SQL Server version where the backup was taken.
        /// </summary>
        /// <value>The server version.</value>
        public ServerVersion ServerVersion
        {
            get
            {
                Populate();
                return serverVersion;
            }
        }

        internal bool isSnapshot;
        /// <summary>
        /// Was Backup taken using the SNAPSHOT option.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is snapshot backup; otherwise, <c>false</c>.
        /// </value>
        public bool IsSnapshot
        {
            get
            {
                VersionCheck(9, "IsSnapshot");
                Populate();
                return isSnapshot;
            }
        }

        internal bool isReadOnly;
        /// <summary>
        /// Was database read-only at the time of backup.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is read only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get
            {
                VersionCheck(9, "IsReadOnly");
                Populate();
                return isReadOnly;
            }
        }

        internal bool isDamaged;
        /// <summary>
        /// Was damage to the database detected when this backup was created,
        /// and the backup operation was requested to continue despite errors.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is damaged; otherwise, <c>false</c>.
        /// </value>
        public bool IsDamaged
        {
            get
            {
                VersionCheck(9, "IsDamaged");
                Populate();
                return isDamaged;
            }
        }

        internal bool isCopyOnly;
        /// <summary>
        /// Is the backupset Copy-only.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is copy only; otherwise, <c>false</c>.
        /// </value>
        public bool IsCopyOnly
        {
            get
            {
                VersionCheck(9, "IsCopyOnly");
                Populate();
                return isCopyOnly;
            }
        }

        internal bool isForceOffline;
        /// <summary>
        /// Was the Database offline when the backup was taken.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this database was offline when the backup was taken; otherwise, <c>false</c>.
        /// </value>
        public bool IsForceOffline
        {
            get
            {
                VersionCheck(9, "IsForceOffline");
                Populate();
                return isForceOffline;
            }
        }

        internal bool hasIncompleteMetaData;
        /// <summary>
        /// Is the backup a tail log backup with incomplete metadata.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance has incomplete meta data; otherwise, <c>false</c>.
        /// </value>
        public bool HasIncompleteMetaData
        {
            get
            {
                VersionCheck(9, "HasIncompleteMetaData");
                Populate();
                return hasIncompleteMetaData;
            }
        }

        internal bool hasBulkLoggedData;
        /// <summary>
        /// Does the Backup contain bulk-logged data.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance has bulk logged data; otherwise, <c>false</c>.
        /// </value>
        public bool HasBulkLoggedData
        {
            get
            {
                VersionCheck(9, "HasBulkLoggedData");
                Populate();
                return hasBulkLoggedData;
            }
        }

        internal bool beginsLogChain;
        /// <summary>
        /// Is this the first in a continuous chain of log backups.
        /// A log chain begins with the first log backup taken after
        /// the database is created or when it is switched from the
        /// simple to the full or bulk-logged recovery model.
        /// </summary>
        /// <value><c>true</c> if [begins log chain]; otherwise, <c>false</c>.</value>
        public bool BeginsLogChain
        {
            get
            {
                VersionCheck(9, "BeginsLogChain");
                Populate();
                return beginsLogChain;
            }
        }

        internal int softwareVendorId;
        /// <summary>
        /// Identification number of the software
        /// vendor writing the backup media header.
        /// </summary>
        /// <value>The software vendor id.</value>
        public int SoftwareVendorId
        {
            get
            {
                Populate();
                return softwareVendorId;
            }
        }

        internal Decimal firstLsn = 0m;
        /// <summary>
        /// Log sequence number of the first
        /// or oldest log record in the backup set.
        /// </summary>
        /// <value>The first LSN.</value>
        public Decimal FirstLsn
        {
            get
            {
                Populate();
                return firstLsn;
            }
        }

        internal Decimal lastLsn = 0m;
        /// <summary>
        /// Log sequence number of the next log
        /// record after the backup set.
        /// </summary>
        /// <value>The last LSN.</value>
        public Decimal LastLsn
        {
            get
            {
                Populate();
                return lastLsn;
            }
        }

        internal Decimal checkpointLsn = 0m;
        /// <summary>
        /// Log sequence number of the log
        /// record where redo must start.
        /// </summary>
        /// <value>The checkpoint LSN.</value>
        public Decimal CheckpointLsn
        {
            get
            {
                Populate();
                return checkpointLsn;
            }
        }

        internal Decimal databaseBackupLsn = 0m;
        /// <summary>
        /// Log sequence number of the most
        /// recent full database backup.
        /// </summary>
        /// <value>The database backup LSN.</value>
        public Decimal DatabaseBackupLsn
        {
            get
            {
                Populate();
                return databaseBackupLsn;
            }
        }

        internal Decimal forkPointLsn = 0m;
        /// <summary>
        /// If first_recovery_fork_guid is not equal
        /// to last_recovery_fork_guid, this is the log
        /// sequence number of the fork point.
        /// </summary>
        /// <value>The fork point LSN.</value>
        public Decimal ForkPointLsn
        {
            get
            {
                VersionCheck(9, "ForkPointLsn");
                Populate();
                return forkPointLsn;
            }
        }

        internal Decimal differentialBaseLsn = 0m;
        /// <summary>
        /// Base LSN for differential backups. Changes
        /// with LSNs greater than or equal to
        /// differential_base_lsn are included in the
        /// differential backup.
        /// </summary>
        /// <value>The differential base LSN.</value>
        public Decimal DifferentialBaseLsn
        {
            get
            {
                VersionCheck(9, "DifferentialBaseLsn");
                Populate();
                return differentialBaseLsn;
            }
        }

        internal Decimal backupSize = 0m;
        /// <summary>
        /// Size of the backup set, in bytes.
        /// </summary>
        /// <value>The size of the backup.</value>
        public Decimal BackupSize
        {
            get
            {
                Populate();
                return backupSize;
            }
        }

        internal Decimal compressedBackupSize = 0m;
        /// <summary>
        /// Total Byte count of the backup stored on disk.
        /// </summary>
        /// <value>The size of the compressed backup.</value>
        public Decimal CompressedBackupSize
        {
            get
            {
                VersionCheck(10, "CompressedBackupSize");
                Populate();
                return compressedBackupSize;
            }
        }

        internal Guid backupSetGuid = new Guid();
        /// <summary>
        /// Unique backup set identification number that
        /// identifies the backup set.
        /// </summary>
        /// <value>The backup set GUID.</value>
        public Guid BackupSetGuid
        {
            get
            {
                return backupSetGuid;
            }
        }

        internal Guid databaseGuid = new Guid();
        /// <summary>
        /// Unique ID of the database where the backup was taken.
        /// When the database is restored, a new value is assigned.
        /// </summary>
        /// <value>The database GUID.</value>
        public Guid DatabaseGuid
        {
            get
            {
                VersionCheck(9, "DatabaseGuid");
                Populate();
                return databaseGuid;
            }
        }

        internal Guid familyGuid = new Guid();
        /// <summary>
        /// Unique ID of the original database at creation.
        /// This value remains the same when the database
        /// is restored, even to a different name.
        /// </summary>
        /// <value>The family GUID.</value>
        public Guid FamilyGuid
        {
            get
            {
                VersionCheck(9, "FamilyGuid");
                Populate();
                return familyGuid;
            }
        }

        internal Guid differentialBaseGuid = new Guid();
        /// <summary>
        /// For a single-based differential backup, the value
        /// is the unique identifier of the differential base.
        /// </summary>
        /// <value>The differential base GUID.</value>
        public Guid DifferentialBaseGuid
        {
            get
            {
                VersionCheck(9, "DifferentialBaseGuid");
                Populate();
                return differentialBaseGuid;
            }
        }

        internal Guid recoveryForkID = new Guid();
        /// <summary>
        /// ID of the ending recovery fork.
        /// </summary>
        /// <value>The recovery fork ID.</value>
        public Guid RecoveryForkID
        {
            get
            {
                VersionCheck(9, "RecoveryForkID");
                Populate();
                return recoveryForkID;
            }
        }

        internal Guid firstRecoveryForkID = new Guid();
        /// <summary>
        /// ID of the starting recovery fork.
        /// </summary>
        /// <value>The first recovery fork ID.</value>
        public Guid FirstRecoveryForkID
        {
            get
            {
                VersionCheck(9, "FirstRecoveryForkID");
                Populate();
                return firstRecoveryForkID;
            }
        }

        internal BackupMediaSet backupMediaSet;
        /// <summary>
        /// Gets the backup media set.
        /// </summary>
        /// <value>The backup media set.</value>
        public BackupMediaSet BackupMediaSet
        {
            get { return backupMediaSet; }
        }

        /// <summary>
        /// Checks the backup files existence.
        /// </summary>
        public void CheckBackupFilesExistence()
        {
            var item = ((from BackupMedia bkMedia in BackupMediaSet.BackupMediaList
                         orderby bkMedia.MirrorSequenceNumber ascending
                         select bkMedia).GroupBy(x => x.FamilySequenceNumber)).Select(x => x.First());
            foreach (BackupMedia bkMedia in item)
            {
                if (bkMedia.MediaType == DeviceType.File)
                {
                    Request req = new Request();
                    req.Urn = "Server/File[@FullName='" + Urn.EscapeString(bkMedia.MediaName) + "']";
                    DataTable dt = server.ExecutionManager.GetEnumeratorData(req);
                    Boolean isFile = false;
                    if (dt != null && dt.Rows != null && dt.Rows.Count > 0)
                    {
                        isFile = (Convert.ToBoolean(dt.Rows[0]["IsFile"], System.Globalization.CultureInfo.InvariantCulture));
                    }
                    if (!isFile)
                    {
                        throw new SmoException(ExceptionTemplates.BackupFileNotFound(bkMedia.MediaName));
                    }
                }
            }
        }

        /// <summary>
        /// Verifies the backupset
        /// </summary>
        public void Verify()
        {
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
            sb.Append("RESTORE VERIFYONLY FROM");
            BackupMedia bkMediaForCredential = null;
            for (int i = 1; i <= BackupMediaSet.FamilyCount; i++)
            {
                var item = from BackupMedia bkMedia in BackupMediaSet.BackupMediaList
                           where bkMedia.FamilySequenceNumber == i
                           orderby bkMedia.MirrorSequenceNumber ascending
                           select bkMedia;
                if (item.Count() > 0)
                {
                    BackupMedia bkMedia = item.ElementAt(0);
                    if (bkMediaForCredential == null && !string.IsNullOrEmpty(bkMedia.CredentialName))
                    {
                        bkMediaForCredential = bkMedia;
                    }
                    if (i != 1)
                    {
                        sb.Append(",");
                    }
                    sb.Append(BackupMedia.GetBackupMediaNameForScript(bkMedia.MediaName, bkMedia.MediaType));
                }
            }
            sb.Append(" WITH ");
            if (bkMediaForCredential != null && bkMediaForCredential.AddCredential(server.ServerVersion, sb))
            {
                sb.Append(Globals.commaspace);
            }
            sb.AppendFormat(SmoApplication.DefaultCulture, " FILE = {0}, NOUNLOAD, NOREWIND", Position);
            StringCollection queries = new StringCollection();
            queries.Add(sb.ToString());

            try
            {
                server.ExecutionManager.ExecuteNonQuery(queries);
            }
            catch (Exception e) when (e.InnerException is SqlException se && se.Errors.Cast<SqlError>().Any(err => err.Number == 3014))
            {
                // We've got a list of errors in the exception. If there is 3014 among them, then we know
                // that backup/restore has succeeded, the errors can be ignored and we can eat up the error
            }
        }


        /// <summary>
        /// Populates this instance.
        /// </summary>
        private void Populate()
        {
            if (isPopulated)
            {
                return;
            }
            DataTable dt = null;
            Request req = new Request(string.Format(SmoApplication.DefaultCulture,
                "Server/BackupSet[@BackupSetUuid='{0}']", backupSetGuid.ToString()));
            dt = server.ExecutionManager.GetEnumeratorData(req);

            if (dt.Rows.Count < 1)
            {
                return;
            }
            DataRow dr = dt.Rows[0];
            Populate(dr);
        }

        private void Populate(DataRow dr)
        {
            if (targetServerVersion >= 7)
            {
                PopulateV7Properties(dr);
            }
            if (targetServerVersion >= 9)
            {
                PopulateV9Properties(dr);
            }
            if (targetServerVersion >= 10)
            {
                PopulateV10Properties(dr);
            }

            //set the flag true.
            isPopulated = true;
        }

        private void PopulateFromDevice(DataRow dr)
        {
            if (targetServerVersion >= 7)
            {
                PopulateV7PropertiesFromDevices(dr);
            }
            if (targetServerVersion >= 9)
            {
                PopulateV9PropertiesFromDevices(dr);
            }
            if (targetServerVersion >= 10)
            {
                PopulateV10PropertiesFromDevices(dr);
            }

            //set the flag true.
            isPopulated = true;
        }

        /// <summary>
        /// Populates the shiloh properties.
        /// </summary>
        /// <param name="dr">The datarow.</param>
        private void PopulateV7Properties(DataRow dr)
        {
            //Name
            if (!(dr["Name"] is DBNull))
            {
                name = (string)dr["Name"];
            }
            //Description
            if (!(dr["Description"] is DBNull))
            {
                description = (string)dr["Description"];
            }

            //BackupSetType
            if (!(dr["BackupSetType"] is DBNull))
            {
                int type = (int)dr["BackupSetType"];
                switch (type)
                {
                    case 1:
                        backupSetType = BackupSetType.Database;
                        break;
                    case 2:
                        backupSetType = BackupSetType.Differential;
                        break;
                    case 3:
                        backupSetType = BackupSetType.Log;
                        break;
                    case 4:
                        backupSetType = BackupSetType.FileOrFileGroup;
                        break;
                    case 5:
                        backupSetType = BackupSetType.FileOrFileGroupDifferential;
                        break;
                }
            }
            //ID
            if (!(dr["ID"] is DBNull))
            {
                ID = (int)dr["ID"];
            }
            //SoftwareVendorId
            if (!(dr["SoftwareVendorId"] is DBNull))
            {
                softwareVendorId = (int)dr["SoftwareVendorId"];
            }
            //BackupStartDate 
            if (!(dr["BackupStartDate"] is DBNull))
            {
                backupStartDate = (DateTime)dr["BackupStartDate"];
            }
            //BackupFinishDate
            if (!(dr["BackupFinishDate"] is DBNull))
            {
                backupFinishDate = (DateTime)dr["BackupFinishDate"];
            }
            //ExpirationDate
            if (!(dr["ExpirationDate"] is DBNull))
            {
                expirationDate = (DateTime)dr["ExpirationDate"];
            }

            //Position
            if (!(dr["Position"] is DBNull))
            {
                position = (int)dr["Position"];
            }
            //DatabaseName
            if (!(dr["DatabaseName"] is DBNull))
            {
                databaseName = (string)dr["DatabaseName"];
            }
            //ServerName
            if (!(dr["ServerName"] is DBNull))
            {
                serverName = (string)dr["ServerName"];
            }
            //MachineName
            if (!(dr["MachineName"] is DBNull))
            {
                machineName = (string)dr["MachineName"];
            }
            //UserName
            if (!(dr["UserName"] is DBNull))
            {
                userName = (string)dr["UserName"];
            }

            //ServerVersion
            if (!(dr["SoftwareMajorVersion"] is DBNull))
            {
                int svMajor, svMinor, svBuild;
                svMajor = (byte)dr["SoftwareMajorVersion"];
                svMinor = (byte)dr["SoftwareMinorVersion"];
                svBuild = (short)dr["SoftwareBuildVersion"];
                serverVersion = new ServerVersion(svMajor, svMinor, svBuild);
            }
            //FirstLsn
            if (!(dr["FirstLsn"] is DBNull))
            {
                firstLsn = (Decimal)dr["FirstLsn"];
            }
            //LastLsn
            if (!(dr["LastLsn"] is DBNull))
            {
                lastLsn = (Decimal)dr["LastLsn"];
            }
            //CheckpointLsn
            if (!(dr["CheckpointLsn"] is DBNull))
            {
                checkpointLsn = (Decimal)dr["CheckpointLsn"];
            }
            //DatabaseBackupLsn
            if (!(dr["DatabaseBackupLsn"] is DBNull))
            {
                databaseBackupLsn = (Decimal)dr["DatabaseBackupLsn"];
            }
            //Size 
            if (!(dr["BackupSize"] is DBNull))
            {
                backupSize = (Decimal)dr["BackupSize"];
            }

            //backupMediaSet
            backupMediaSet = new BackupMediaSet(server, (int)dr["MediaSetId"]);
        }

        private void PopulateV7PropertiesFromDevices(DataRow dr)
        {
            //Name
            if (!(dr["BackupName"] is DBNull))
            {
                name = (string)dr["BackupName"];
            }
            //Description
            if (!(dr["BackupDescription"] is DBNull))
            {
                description = (string)dr["BackupDescription"];
            }

            byte type = (byte)dr["BackupType"];
            switch (type)
            {
                case 1:
                    backupSetType = BackupSetType.Database;
                    break;
                case 2:
                    backupSetType = BackupSetType.Log;
                    break;
                case 4:
                    backupSetType = BackupSetType.FileOrFileGroup;
                    break;
                case 5:
                    backupSetType = BackupSetType.Differential;
                    break;
                case 6:
                    backupSetType = BackupSetType.FileOrFileGroupDifferential;
                    break;
            }

            //SoftwareVendorId
            if (!(dr["SoftwareVendorId"] is DBNull))
            {
                softwareVendorId = (int)dr["SoftwareVendorId"];
            }
            //BackupStartDate 
            if (!(dr["BackupStartDate"] is DBNull))
            {
                backupStartDate = (DateTime)dr["BackupStartDate"];
            }
            //BackupFinishDate
            if (!(dr["BackupFinishDate"] is DBNull))
            {
                backupFinishDate = (DateTime)dr["BackupFinishDate"];
            }
            //ExpirationDate
            if (!(dr["ExpirationDate"] is DBNull))
            {
                expirationDate = (DateTime)dr["ExpirationDate"];
            }

            //Position
            if (!(dr["Position"] is DBNull))
            {
                position = (int)(short)dr["Position"];
            }
            //DatabaseName
            if (!(dr["DatabaseName"] is DBNull))
            {
                databaseName = (string)dr["DatabaseName"];
            }
            //ServerName
            if (!(dr["ServerName"] is DBNull))
            {
                serverName = (string)dr["ServerName"];
            }
            //MachineName
            if (!(dr["MachineName"] is DBNull))
            {
                machineName = (string)dr["MachineName"];
            }
            //UserName
            if (!(dr["UserName"] is DBNull))
            {
                userName = (string)dr["UserName"];
            }

            //ServerVersion
            if (!(dr["SoftwareVersionMajor"] is DBNull))
            {
                int svMajor, svMinor, svBuild;
                svMajor = (int)dr["SoftwareVersionMajor"];
                svMinor = (int)dr["SoftwareVersionMinor"];
                svBuild = (int)dr["SoftwareVersionBuild"];
                serverVersion = new ServerVersion(svMajor, svMinor, svBuild);
            }
            //FirstLsn
            if (!(dr["FirstLSN"] is DBNull))
            {
                firstLsn = (Decimal)dr["FirstLSN"];
            }
            //LastLsn
            if (!(dr["LastLSN"] is DBNull))
            {
                lastLsn = (Decimal)dr["LastLSN"];
            }
            //CheckpointLsn
            if (!(dr["CheckpointLSN"] is DBNull))
            {
                checkpointLsn = (Decimal)dr["CheckpointLSN"];
            }

            //Size 
            if (!(dr["BackupSize"] is DBNull))
            {
                if (dr["BackupSize"] is long)
                {
                    backupSize = (Decimal)(long)dr["BackupSize"];
                }
                else
                {
                    backupSize = (Decimal)dr["BackupSize"];
                }
            }
        }

        /// <summary>
        /// Populates the yukon properties.
        /// </summary>
        /// <param name="dr">The datarow.</param>
        private void PopulateV9Properties(DataRow dr)
        {
            //differential_base_guid
            if (!(dr["DifferentialBaseGuid"] is DBNull))
            {
                differentialBaseGuid = (Guid)dr["DifferentialBaseGuid"];
            }
            //first_recovery_fork_guid 
            if (!(dr["FirstRecoveryForkID"] is DBNull))
            {
                firstRecoveryForkID = (Guid)dr["FirstRecoveryForkID"];
            }
            //last_recovery_fork_guid
            if (!(dr["RecoveryForkID"] is DBNull))
            {
                recoveryForkID = (Guid)dr["RecoveryForkID"];
            }
            //family_guid
            if (!(dr["FamilyGuid"] is DBNull))
            {
                familyGuid = (Guid)dr["FamilyGuid"];
            }
            //family_guid
            if (!(dr["DatabaseGuid"] is DBNull))
            {
                databaseGuid = (Guid)dr["DatabaseGuid"];
            }

            //fork_point_Lsn
            if (!(dr["ForkPointLsn"] is DBNull))
            {
                forkPointLsn = (Decimal)dr["ForkPointLsn"];
            }
            //differential_base_Lsn
            if (!(dr["DifferentialBaseLsn"] is DBNull))
            {
                differentialBaseLsn = (Decimal)dr["DifferentialBaseLsn"];
            }

            //has_incomplete_metadata
            hasIncompleteMetaData = (bool)dr["HasIncompleteMetaData"];

            //has_bulk_logged_data
            hasBulkLoggedData = (bool)dr["HasBulkLoggedData"];

            //is_copy_only
            isCopyOnly = (bool)dr["IsCopyOnly"];

            //is_force_offline
            isForceOffline = (bool)dr["IsForceOffline"];

            //is_damaged
            isDamaged = (bool)dr["IsDamaged"];

            //is_readonly
            isReadOnly = (bool)dr["IsReadOnly"];

            //is_snapshot
            isSnapshot = (bool)dr["IsSnapshot"];

            //begins_log_chain
            beginsLogChain = (bool)dr["BeginsLogChain"];

        }

        private void PopulateV9PropertiesFromDevices(DataRow dr)
        {

            //BackupSetGuid
            if (!(dr["BackupSetGUID"] is DBNull))
            {
                backupSetGuid = (Guid)dr["BackupSetGUID"];
            }
            //differential_base_guid
            if (!(dr["DifferentialBaseGUID"] is DBNull))
            {
                differentialBaseGuid = (Guid)dr["DifferentialBaseGUID"];
            }
            //first_recovery_fork_guid 
            if (!(dr["FirstRecoveryForkID"] is DBNull))
            {
                firstRecoveryForkID = (Guid)dr["FirstRecoveryForkID"];
            }
            //last_recovery_fork_guid
            if (!(dr["RecoveryForkID"] is DBNull))
            {
                recoveryForkID = (Guid)dr["RecoveryForkID"];
            }
            //family_guid
            if (!(dr["FamilyGUID"] is DBNull))
            {
                familyGuid = (Guid)dr["FamilyGUID"];
            }
            //family_guid
            if (!(dr["BindingID"] is DBNull))
            {
                databaseGuid = (Guid)dr["BindingID"];
            }

            //fork_point_Lsn
            if (!(dr["ForkPointLSN"] is DBNull))
            {
                forkPointLsn = (Decimal)dr["ForkPointLSN"];
            }
            //differential_base_Lsn
            if (!(dr["DifferentialBaseLSN"] is DBNull))
            {
                differentialBaseLsn = (Decimal)dr["DifferentialBaseLSN"];
            }
            //DatabaseBackupLsn
            if (!(dr["DatabaseBackupLSN"] is DBNull))
            {
                databaseBackupLsn = (Decimal)dr["DatabaseBackupLSN"];
            }

            //has_incomplete_metadata
            hasIncompleteMetaData = (bool)dr["HasIncompleteMetaData"];

            //has_bulk_logged_data
            hasBulkLoggedData = (bool)dr["HasBulkLoggedData"];

            //is_copy_only
            isCopyOnly = (bool)dr["IsCopyOnly"];

            //is_force_offline
            isForceOffline = (bool)dr["IsForceOffline"];

            //is_damaged
            isDamaged = (bool)dr["IsDamaged"];

            //is_readonly
            isReadOnly = (bool)dr["IsReadOnly"];

            //is_snapshot
            isSnapshot = (bool)dr["IsSnapshot"];

            //begins_log_chain
            beginsLogChain = (bool)dr["BeginsLogChain"];

        }

        /// <summary>
        /// Populates the V10 properties.
        /// </summary>
        /// <param name="dr">The dr.</param>
        private void PopulateV10Properties(DataRow dr)
        {
            //CompressedBackupSize
            if (!(dr["CompressedBackupSize"] is DBNull))
            {
                compressedBackupSize = (Decimal)dr["CompressedBackupSize"];
            }
        }

        private void PopulateV10PropertiesFromDevices(DataRow dr)
        {
            //CompressedBackupSize
            if (!(dr["CompressedBackupSize"] is DBNull))
            {
                compressedBackupSize = (Decimal)(long)dr["CompressedBackupSize"];
            }
        }

        /// <summary>
        /// Checks the version.
        /// </summary>
        /// <param name="minSupportedVersion">The minimum supported version.</param>
        /// <param name="propertyName">Name of the property.</param>
        private void VersionCheck(int minSupportedVersion, string propertyName)
        {
            if (Parent.ServerVersion.Major < minSupportedVersion)
            {
                string exText = ExceptionTemplates.CannotReadProp + " " + propertyName + ". "
                    + ExceptionTemplates.PropertyAvailable + SqlSmoObject.GetSqlServerName(Parent);
                throw new UnsupportedVersionException(exText);
            }
        }

        private DataSet fileList;
        internal DataSet FileList
        {
            get
            {
                if (fileList == null)
                {
                    BackupMedia bkdev = BackupMediaSet.BackupMediaList.ElementAt(0);
                    String query = string.Format(SmoApplication.DefaultCulture,
                        "RESTORE FILELISTONLY FROM {0} WITH FILE = {1}",
                        BackupMedia.GetBackupMediaNameForScript(bkdev.MediaName, bkdev.MediaType), Position);
                    fileList = server.ExecutionManager.ExecuteWithResults(query);
                }
                return fileList;
            }
        }

        /// <summary>
        /// Returns a the set of files associated with the BackupSet
        /// </summary>
        /// <returns></returns>
        public DataSet GetFileList()
        {
            return FileList;
        }

        #region static functions
        /// <summary>
        /// Determines whether [is backups forked] [the specified bk set list].
        /// </summary>
        /// <param name="backupSetList">The backupset list.</param>
        /// <returns>
        /// 	<c>true</c> if [is backups forked] [the specified bk set list]; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsBackupsForked(List<BackupSet> backupSetList)
        {
            if (backupSetList == null || backupSetList.Count == 0)
            {
                return false;
            }
            var item = (from bkSet in backupSetList
                        select bkSet.recoveryForkID).Distinct().Count();
            if (item == 1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Determines whether [backup sets in sequence].
        /// </summary>
        /// <param name="first">The first BackupSet.</param>
        /// <param name="second">The second BackupSet.</param>
        /// <param name="stopAtLsn">The stop at LSN.</param>
        /// <returns>
        /// <c>true</c> if [backup sets in sequence]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBackupSetsInSequence(BackupSet first, BackupSet second, ref Decimal stopAtLsn)
        {
            string errMsg;
            object errSource;
            return IsBackupSetsInSequence(first, second, out errMsg, out errSource, ref stopAtLsn);
        }

        /// <summary>
        /// Determines whether [backup sets in sequence].
        /// </summary>
        /// <param name="first">The first BackupSet.</param>
        /// <param name="second">The second BackupSet.</param>
        /// <returns>
        /// <c>true</c> if [backup sets in sequence]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsBackupSetsInSequence(BackupSet first, BackupSet second)
        {
            string errMsg;
            object errSource;
            Decimal stopAtLsn = 0m;
            return IsBackupSetsInSequence(first, second, out errMsg, out errSource, ref stopAtLsn);
        }

        /// <summary>
        /// Determines whether [backup sets in sequence].
        /// </summary>
        /// <param name="first">The first BackupSet.</param>
        /// <param name="second">The second BackupSet.</param>
        /// <param name="errMsg">The err MSG.</param>
        /// <param name="errSource">The err source.</param>
        /// <param name="stopAtLsn">The stop at LSN.</param>
        /// <returns>
        /// 	<c>true</c> if [backup sets in sequence]; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsBackupSetsInSequence(BackupSet first, BackupSet second, out string errMsg, out object errSource, ref Decimal stopAtLsn)
        {
            //Not supported for version less than 9 

            Diagnostics.TraceHelper.Assert(first != null && second != null);
            stopAtLsn = 0m;
            errMsg = null;
            errSource = null;
            //check if any of the backupset is FileOrFileGroup backup
            //these are not supported for verification currently
            if (first.BackupSetType == BackupSetType.FileOrFileGroup
                || first.BackupSetType == BackupSetType.FileOrFileGroupDifferential)
            {
                errMsg = ExceptionTemplates.FileGroupNotSupported;
                errSource = first;
                return false;
            }
            if (second.BackupSetType == BackupSetType.FileOrFileGroup
                || second.BackupSetType == BackupSetType.FileOrFileGroupDifferential)
            {
                errMsg = ExceptionTemplates.FileGroupNotSupported;
                errSource = second;
                return false;
            }

            //Check if they belong to the same Family of database
            if (first.FamilyGuid != second.FamilyGuid)
            {
                errMsg = ExceptionTemplates.BackupsOfDifferentDb;
                errSource = second;
                return false;
            }

            //Full backupset cannot be restored after any other backup
            if (second.BackupSetType == BackupSetType.Database)
            {
                errMsg = ExceptionTemplates.FullBackupShouldBeFirst;
                errSource = second;
                return false;
            }
            //CheckpointLSN should be in increasing order 
            if (first.CheckpointLsn > second.CheckpointLsn)
            {
                errMsg = ExceptionTemplates.BackupsNotInSequence;
                errSource = second;
                return false;
            }

            //Diff should be compatible with the full
            if (second.BackupSetType == BackupSetType.Differential)
            {
                if (first.BackupSetType != BackupSetType.Database)
                {
                    errMsg = ExceptionTemplates.WrongDiffbackup;
                    errSource = second;
                    return false;
                }
                if (first.BackupSetGuid != second.DifferentialBaseGuid)
                {
                    errMsg = ExceptionTemplates.DiffBackupNotCompatible;
                    errSource = second;
                    return false;
                }
                return true;
            }

            if (second.BackupSetType == BackupSetType.Log)
            {
                if (first.BackupSetType == BackupSetType.Database || first.BackupSetType == BackupSetType.Differential)
                {
                    if (first.LastLsn >= second.FirstLsn && first.LastLsn <= second.LastLsn)
                    {
                        if (second.ForkPointLsn == 0m && first.FamilyGuid == second.FamilyGuid)
                        {
                            return true;
                        }
                        if (second.ForkPointLsn != 0m)
                        {
                            if (first.LastLsn <= second.ForkPointLsn && first.RecoveryForkID == second.FirstRecoveryForkID)
                            {
                                return true;
                            }
                            if (first.LastLsn > second.ForkPointLsn && first.RecoveryForkID == second.RecoveryForkID)
                            {
                                return true;
                            }
                        }
                    }
                }
                else if (first.BackupSetType == BackupSetType.Log)
                {
                    if (first.RecoveryForkID == second.FirstRecoveryForkID)
                    {
                        if (first.LastLsn == second.FirstLsn)
                        {
                            return true;
                        }
                    }
                    if (second.ForkPointLsn != 0m)
                    {
                        if (first.FirstLsn < second.FirstLsn && first.LastLsn > second.FirstLsn)
                        {
                            stopAtLsn = second.FirstLsn;
                            return true;
                        }
                    }
                }
                errMsg = ExceptionTemplates.WrongTLogbackup;
                errSource = second;
                return false;
            }
            return false;
        }

        #endregion
    }
}
