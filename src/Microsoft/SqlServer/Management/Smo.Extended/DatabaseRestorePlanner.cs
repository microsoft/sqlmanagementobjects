// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
#if NETSTANDARD2_0
using Microsoft.SqlServer.SmoExtended;
#endif
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Create restore plan status event args
    /// </summary>
    public class CreateRestorePlanEventArgs
    {
        /// <summary>
        /// status
        /// </summary>
        public string Status;
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateRestorePlanEventArgs"/> class.
        /// </summary>
        /// <param name="status">status</param>
        public CreateRestorePlanEventArgs(string status)
        {
            this.Status = status;
        }
    }

    /// <summary>
    /// Database Restore Planner.
    /// </summary>
    public sealed class DatabaseRestorePlanner
    {
#region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRestorePlanner"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public DatabaseRestorePlanner(Server server)
        {
            if (null == server)
            {
                throw new ArgumentNullException("Server");
            }
            this.Server = server;
            this.RestoreToLastBackup = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRestorePlanner"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="databaseName">Name of the database.</param>
        public DatabaseRestorePlanner(Server server, String databaseName)
            : this(server)
        {
            if (null == databaseName)
            {
                throw new ArgumentNullException("DatabaseName");
            }
            this.DatabaseName = databaseName;
            this.RestoreToLastBackup = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseRestorePlanner"/> class.
        /// For point in time recovery.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="databaseName">Name of the database.</param>
        /// <param name="pointInTime">The point in time to restore.</param>
        /// <param name="tailLogBackupFile">The tail log backup device item.</param>
        public DatabaseRestorePlanner(Server server, String databaseName, DateTime pointInTime, string tailLogBackupFile)
            : this(server, databaseName)
        {
            this.RestoreToLastBackup = false;
            this.RestoreToPointInTime = pointInTime;
            this.TailLogBackupFile = tailLogBackupFile;
        }
#endregion

#region properties

        private Server server;
        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>The server.</value>
        public Server Server
        {
            get
            {
                return server;
            }
            set
            {
                if (this.server != value)
                {
                    this.server = value;
                    this.backupSets = null;
                }
            }
        }

        private string databaseName;
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>The name of the database.</value>
        public String DatabaseName
        {
            get
            {
                return databaseName;
            }
            set
            {
                if (value == null || !value.Equals(this.databaseName))
                {
                    this.databaseName = value;
                    this.backupSets = null;
                }
            }
        }

        /// <summary>
        /// Whether to include snapshot backups in the enumeration.
        /// Must be set before retrieving BackupSets property.
        /// </summary>
        public bool IncludeSnapshotBackups = false;

        /// <summary>
        /// Gets or sets a value indicating whether [restore to last backup].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [restore to last backup]; otherwise, <c>false</c>.
        /// </value>
        public bool RestoreToLastBackup
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the restore to point in time.
        /// </summary>
        /// <value>The restore to point in time.</value>
        public DateTime RestoreToPointInTime
        {
            get;
            set;
        }

        private bool readHeaderFromMedia;
        /// <summary>
        /// Gets or sets a value indicating whether [read header from devices].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [read header from devices]; otherwise, <c>false</c>.
        /// </value>
        public bool ReadHeaderFromMedia
        {
            get
            {
                return readHeaderFromMedia;
            }
            set
            {
                if (this.readHeaderFromMedia != value)
                {
                    this.readHeaderFromMedia = value;
                    this.backupSets = null;
                }
            }
        }

        private BackupDeviceCollection backupMediaList = new BackupDeviceCollection();
        /// <summary>
        /// Gets the backup device item list.
        /// </summary>
        /// <value>The backup device item list.</value>
        public ICollection<BackupDeviceItem> BackupMediaList
        {
            get { return backupMediaList; }
        }


        /// <summary>
        /// Gets or sets a value indicating whether to [backup tail log].
        /// </summary>
        /// <value><c>true</c> if [backup tail log]; otherwise, <c>false</c>.</value>
        public bool BackupTailLog
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to backup tail log with NoRecovery.
        /// </summary>
        /// <value><c>true</c> if NoRecovery is set; otherwise, <c>false</c>.</value>
        public bool TailLogWithNoRecovery
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the tail log backup file.
        /// </summary>
        /// <value>New tail log backup file.</value>
        public string TailLogBackupFile
        {
            get;
            set;
        }

        /// <summary>
        /// Create restore plan status update delegate
        /// </summary>
        public delegate void CreateRestorePlanEventHandler(object sender, CreateRestorePlanEventArgs e);

        /// <summary>
        /// Create restore plan status update event handler
        /// </summary>
        public event CreateRestorePlanEventHandler CreateRestorePlanUpdates;

        private BackupSetCollection backupSets;

        /// <summary>
        /// Gets the backup sets.
        /// </summary>
        /// <returns>Backup sets collection</returns>
        public BackupSetCollection BackupSets
        {
            get
            {
                if ((this.backupSets == null && this.ReadHeaderFromMedia) || (this.ReadHeaderFromMedia && this.backupMediaList.Dirty))
                {
                    backupSets = GetBackupSetFromDevices();
                    this.backupMediaList.Dirty = false;
                }
                else if (this.backupSets == null && !this.ReadHeaderFromMedia)
                {
                    backupSets = GetBackupSetFromMSDB();
                }
                return backupSets;
            }
        }

#endregion

#region Create Methods

        /// <summary>
        /// Creates the restore plan.
        /// </summary>
        /// <returns>Restore plan</returns>
        public RestorePlan CreateRestorePlan()
        {
            return CreateRestorePlan(new RestoreOptions());
        }

        /// <summary>
        /// Creates the restore plan.
        /// </summary>
        /// <param name="ro">The restore options.</param>
        /// <returns>Restore Plan</returns>
        public RestorePlan CreateRestorePlan(RestoreOptions ro)
        {
            RestorePlan plan = new RestorePlan(this.Server);
            plan.RestoreAction = RestoreActionType.Database;
            if (this.DatabaseName == null)
            {
                throw new SmoException(ExceptionTemplates.PropertyNotSetExceptionText("DatabaseName"));
            }
            plan.DatabaseName = this.DatabaseName;
            //Get the backupsets for that database.
            if (this.Server.Version.Major < 9)
            {
                plan.AddRestoreOperation(createShilohPlan(this.BackupSets));
                if (plan.RestoreOperations.Count > 0 && this.RestoreToLastBackup == false)
                {
                    Restore res = plan.RestoreOperations.Last();
                    if (res.Action == RestoreActionType.Log)
                    {
                        res.ToPointInTime = Smo.SqlSmoObject.SqlDateString(this.RestoreToPointInTime);
                    }
                }
            }
            else
            {
                SelectBackupSetsForPlan(plan);
            }

            if (this.BackupTailLog && plan.TailLogBackupOperation == null)
            {
                TakeTailLogBackup(plan);
            }
            plan.SetRestoreOptions(ro);
            return plan;
        }

        /// <summary>
        /// Creates the plan for shiloh server .
        /// </summary>
        /// <param name="backupsets">The backupsets.</param>
        /// <returns>Backup sets to restore.</returns>
        private List<BackupSet> createShilohPlan(BackupSetCollection backupsets)
        {
            List<BackupSet> ret = new List<BackupSet>();
            BackupSet fullBackupSet;
            BackupSet diffBackupSet;
            DateTime lastBkStartDate;

            //Select full backup.
            IEnumerable<BackupSet> item;
            if (this.RestoreToLastBackup)
            {
                item = (from BackupSet bkset in backupsets
                        where bkset.BackupSetType == BackupSetType.Database
                        orderby bkset.BackupStartDate descending, bkset.ID descending
                        select bkset).Take(1);
            }
            else
            {
                item = (from BackupSet bkset in backupsets
                        where bkset.BackupSetType == BackupSetType.Database &&
                        bkset.BackupStartDate <= this.RestoreToPointInTime
                        orderby bkset.BackupStartDate descending, bkset.ID descending
                        select bkset).Take(1);
            }

            if (item.Count() == 0)
            {
                return ret;
            }
            fullBackupSet = item.ElementAt(0);
            lastBkStartDate = fullBackupSet.BackupStartDate;
            ret.Add(fullBackupSet);

            //Select Differential backup.
            if (this.RestoreToLastBackup)
            {
                item = (from BackupSet bkset in backupsets
                        where bkset.BackupSetType == BackupSetType.Differential &&
                        bkset.BackupStartDate >= lastBkStartDate
                        orderby bkset.BackupStartDate descending, bkset.ID descending
                        select bkset).Take(1);
            }
            else
            {
                item = (from BackupSet bkset in backupsets
                        where bkset.BackupSetType == BackupSetType.Differential &&
                        bkset.BackupStartDate <= this.RestoreToPointInTime &&
                        bkset.BackupStartDate >= lastBkStartDate
                        orderby bkset.BackupStartDate descending, bkset.ID descending
                        select bkset).Take(1);
            }
            if (item.Count() > 0)
            {
                diffBackupSet = item.ElementAt(0);
                lastBkStartDate = diffBackupSet.BackupStartDate;
                ret.Add(diffBackupSet);
            }

            //Select T-Log backups
            if (this.RestoreToLastBackup)
            {
                item = from BackupSet bkset in backupsets
                       where bkset.BackupSetType == BackupSetType.Log &&
                       bkset.BackupStartDate >= lastBkStartDate
                       orderby bkset.BackupStartDate, bkset.ID
                       select bkset;
            }
            else
            {
                item = from BackupSet bkset in backupsets
                       where bkset.BackupSetType == BackupSetType.Log &&
                       bkset.BackupStartDate <= this.RestoreToPointInTime &&
                       bkset.BackupStartDate >= lastBkStartDate
                       orderby bkset.BackupStartDate, bkset.ID
                       select bkset;
            }
            foreach (BackupSet bk in item)
            {
                ret.Add(bk);
                lastBkStartDate = bk.BackupStartDate;
            }

            if (!this.RestoreToLastBackup && this.RestoreToPointInTime > lastBkStartDate)
            {
                item = (from BackupSet bkset in backupsets
                        where bkset.BackupSetType == BackupSetType.Log &&
                        bkset.BackupStartDate > this.RestoreToPointInTime
                        orderby bkset.BackupStartDate ascending, bkset.ID ascending
                        select bkset).Take(1);
                if (item.Count() > 0)
                {
                    ret.Add(item.ElementAt(0));
                }
            }
            return ret;
        }

        /// <summary>
        /// Creates the plan assuming the recovery path is forked.
        /// </summary>
        /// <param name="plan"></param>
        /// <returns> Backup sets to restore</returns>
        private void SelectBackupSetsForPlan(RestorePlan plan)
        {
            List<BackupSet> selBackupSet = new List<BackupSet>();
            BackupSet lastSelBackupSet = null;
            bool tailLogRestoreRequired = false;

            if (this.CreateRestorePlanUpdates != null)
            {
                this.CreateRestorePlanUpdates(null, new CreateRestorePlanEventArgs(DatabaseRestorePlannerSR.SelectingBackupSets));
            }

            //Find the last backupset to restore

            //See if a backupset exists for that exact datetime
            if (!this.RestoreToLastBackup)
            {
                var item = (from BackupSet b in this.BackupSets
                            where b.BackupStartDate == this.RestoreToPointInTime &&
                            b.BackupSetType != BackupSetType.FileOrFileGroup &&
                            b.BackupSetType != BackupSetType.FileOrFileGroupDifferential
                            orderby b.LastLsn descending, b.ID descending, b.BackupStartDate descending
                            select b).Take(1);
                if (item != null && item.Count() > 0)
                {
                    lastSelBackupSet = item.ElementAt(0);
                    lastSelBackupSet.StopAtLsn = 0m;
                    selBackupSet.Add(lastSelBackupSet);
                }
            }

            if (lastSelBackupSet == null)
            {
                // Does the point-in-time require a Tail-Log backup and restore.
                DateTime? tailLogStart = TailLogStartTime();
                if (!this.ReadHeaderFromMedia && !this.RestoreToLastBackup && tailLogStart.HasValue && this.RestoreToPointInTime > tailLogStart)
                {
                    tailLogRestoreRequired = true;
                }

                // Or the point-in-time lies in a T-Log backup
                else if (!this.RestoreToLastBackup)
                {
                    // find the greatest LSN from before the point-in-time since we need to cover the range from this Last LSN to then 
                    BackupSet lastLsn = (from BackupSet b in this.BackupSets
                                         where b.BackupStartDate < this.RestoreToPointInTime
                                         orderby b.LastLsn descending, b.ID descending, b.BackupStartDate descending
                                         select b).FirstOrDefault();
                    if (lastLsn != null)
                    {
                        // find the backups that covers the greatest range of LSNs starting from at or before the last LSN
                        // this maximizes the odds of covering the point-in-time in cases where additional backups where the backup times might be out of order relative to the LSNs covered 
                        BackupSet[] backups = (from BackupSet b in this.BackupSets
                                               where b.BackupSetType == BackupSetType.Log &&
                                               b.BackupStartDate > this.RestoreToPointInTime &&
                                               b.FirstLsn <= lastLsn.LastLsn
                                               orderby b.LastLsn descending, b.ID ascending, b.BackupStartDate ascending
                                               select b).Take(4).ToArray(); //take the top 4 in case the top results incase there are forked Log issues. In most cases this query will only return 1 or 2 results.
                        if (backups.Length > 0)
                        {
                            DateTime? logStartTime = null;
                            for (int i = 0; (i < backups.Length) && (lastSelBackupSet == null); i++)
                            {
                                logStartTime = LogStartTime(backups[i]);
                                if (logStartTime != null)
                                {
                                    if (logStartTime < this.RestoreToPointInTime)
                                    {
                                        lastSelBackupSet = backups[i];
                                        lastSelBackupSet.StopAtLsn = 0m;
                                        selBackupSet.Add(lastSelBackupSet);
                                    }
                                }
                            }
                        }
                    }
                }


                if (lastSelBackupSet == null)
                {
                    BackupSet item;
                    if (this.RestoreToLastBackup)
                    {
                        item = (from BackupSet b in this.BackupSets
                                where b.BackupSetType != BackupSetType.FileOrFileGroup &&
                                b.BackupSetType != BackupSetType.FileOrFileGroupDifferential
                                orderby b.LastLsn descending, b.ID descending, b.BackupStartDate descending
                                select b).FirstOrDefault();

                    }
                    else
                    {
                        item = (from BackupSet b in this.BackupSets
                                where b.BackupSetType != BackupSetType.FileOrFileGroup &&
                                b.BackupSetType != BackupSetType.FileOrFileGroupDifferential &&
                                b.BackupStartDate <= this.RestoreToPointInTime
                                orderby b.LastLsn descending, b.ID descending, b.BackupStartDate descending
                                select b).FirstOrDefault();
                    }
                    if (item == null )
                    {
                        //No backups found.
                        return;
                    }
                    lastSelBackupSet = item;
                    lastSelBackupSet.StopAtLsn = 0m;
                    selBackupSet.Add(lastSelBackupSet);
                }
            }


            //We have found the last backup to be restored now construct the sequence.
            BackupSet currentBakupSet = lastSelBackupSet;
            HashSet<Guid> visitedBksetGuids = new HashSet<Guid>();
            visitedBksetGuids.Add(currentBakupSet.BackupSetGuid);

            // Keep selecting backups until reaching a database backup or
            // exhausting the backup set. Search favoring the most recent LastLsn/ID to 
            // prevent issues where longer running differential database backups are read 
            // out of sequence with shorter transactional logs. When favoring StartDate 
            // transaction log items are read first since they started more recently and
            // then the LastLSN of the differential backup is out of sequence. See below example
            //  <---more recent      older -->
            //       L1         L2         L3     
            // [----------][----------][----------]
            //    [-------------------------]
            //                D1
            // reading by start date would read L1, L2 and then fail to read D1 because it 
            // is out of order with L2 (LastLSN D1 > LastLSN L2) then L3 and continues until 
            // a valid Database backup is found (or throws an error is none are found)
            // reading by LastLSN returns L1 then D1 and then is complete.
            // Mock Data Set (sorted by Start Date)
            // Type | Start Date Time | ID | LastLSN
            //  L   | 06-18 15:27:47  | 80 | 20800
            //  L   | 06-18 15:27:46  | 79 | 20700
            //  L   | 06-18 15:27:45  | 78 | 20600
            //  D   | 06-18 15:27:43  | 81 | 20857
            //  L   | 06-18 15:27:40  | 77 | 20500
            // In this case the database backup was not used since the LastLSN didnt fall in 
            // the range of LSNs covered by backup with ID 78. The backup wizard will then keep
            // going until a useable database backup is found or fail if there isnt one
            // Same Dataset ordered by LastLSN 
            // Type | Start Date Time | ID | LastLSN
            //  D   | 06-18 15:27:43  | 81 | 20857
            //  L   | 06-18 15:27:47  | 80 | 20800
            //  L   | 06-18 15:27:46  | 79 | 20700
            //  L   | 06-18 15:27:45  | 78 | 20600
            //  L   | 06-18 15:27:40  | 77 | 20500
            // The Wizard finds the Differential Database Backup first (ID 81) and that contains 
            // everything the restore needs restore the DB

            while (lastSelBackupSet.BackupSetType != BackupSetType.Database &&
                   visitedBksetGuids.Count < this.BackupSets.Count)
            {
                BackupSet currentBackupSet = (from BackupSet b in this.BackupSets
                            where b.LastLsn <= lastSelBackupSet.LastLsn &&
                            !visitedBksetGuids.Contains(b.BackupSetGuid) &&
                            b.ID <= lastSelBackupSet.ID
                            orderby b.LastLsn descending, b.ID descending, b.BackupStartDate descending
                            select b).FirstOrDefault();
                if (currentBackupSet == null)
                {
                    // This is a situation where we failed to construct the restore sequence.
                    throw new SmoException(ExceptionTemplates.UnableToCreateRestoreSequence);
                }
                currentBackupSet.StopAtLsn = 0m;
                visitedBksetGuids.Add(currentBackupSet.BackupSetGuid);
                Decimal stopLsn = 0m;
                if (BackupSet.IsBackupSetsInSequence(currentBackupSet, lastSelBackupSet, ref stopLsn))
                {
                    if (stopLsn != 0m)
                    {
                        currentBackupSet.StopAtLsn = stopLsn;
                    }
                    lastSelBackupSet = currentBackupSet;
                    selBackupSet.Add(lastSelBackupSet);
                }
            }

            selBackupSet.Reverse();
            //Remove an redundant Forked T-Log Backup
            for (int i = 1; i < selBackupSet.Count; i++)
            {
                if (selBackupSet[i].StopAtLsn != 0m)
                {
                    if (selBackupSet[i - 1].LastLsn == selBackupSet[i].StopAtLsn)
                    {
                        selBackupSet.RemoveAt(i);
                        break;
                    }
                }
            }
            plan.AddRestoreOperation(selBackupSet);

            if (!this.RestoreToLastBackup && plan.RestoreOperations.Count() > 0
                && plan.RestoreOperations.Last().Action == RestoreActionType.Log
                && plan.RestoreOperations.Last().BackupSet.BackupStartDate > this.RestoreToPointInTime)
            {
                plan.RestoreOperations.Last().ToPointInTime = Smo.SqlSmoObject.SqlDateString(this.RestoreToPointInTime);
            }

            if (tailLogRestoreRequired)
            {
                this.BackupTailLog = true;
                TakeTailLogBackup(plan);
                TakeTailLogRestore(plan);
            }
        }

        /// <summary>
        /// Determines whether it's possible to do a tail-log backup before restoring the database.
        /// </summary>
        /// <param name="databaseName">The database.</param>
        /// <returns>
        /// 	<c>true</c> if it's possible to do a tail-log backup before restoring the database; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTailLogBackupPossible(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return false;
            }
            if (this.Server.Version.Major < 9 || String.IsNullOrEmpty(this.DatabaseName))
            {
                return false;
            }

            Database db = this.Server.Databases[databaseName];
            if (db == null)
            {
                return false;
            }
            else
            {
                db.Refresh();
            }

            if (db.Status != DatabaseStatus.Normal && db.Status != DatabaseStatus.Suspect && db.Status != DatabaseStatus.EmergencyMode)
            {
                return false;
            }
            if (db.RecoveryModel == RecoveryModel.Full || db.RecoveryModel == RecoveryModel.BulkLogged)
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// Determines whether it's possible to do a tail-log backup with NORECOVERY before restoring the database.
        /// </summary>
        /// <param name="databaseName">The database.</param>
        /// <returns>
        /// 	<c>true</c> if it's possible to do a tail-log backup with NORECOVERY before restoring the database; otherwise, <c>false</c>.
        /// </returns>
        public bool IsTailLogBackupWithNoRecoveryPossible(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
            {
                return false;
            }
            if (!IsTailLogBackupPossible(databaseName))
            {
                return false;
            }

            Database db = this.Server.Databases[databaseName];
            if (db == null)
            {
                return false;
            }
            if (db.ServerVersion.Major > 10 && db.DatabaseEngineType == DatabaseEngineType.Standalone && !String.IsNullOrEmpty(db.AvailabilityGroupName))
            {
                return false;
            }
            if (db.DatabaseEngineType == DatabaseEngineType.Standalone && db.IsMirroringEnabled)
            {
                return false;
            }
            return true;
        }


        private DateTime? GetLastRestoreDateTime()
        {
            string query = string.Format(SmoApplication.DefaultCulture,
                 "SELECT TOP(1) restore_date FROM msdb.dbo.restorehistory WHERE destination_database_name = N'{0}' ORDER BY restore_date DESC",
                 SqlSmoObject.SqlString(this.DatabaseName));

            DateTime? lastRestoreTime = null;
            DataSet ds = this.Server.ExecutionManager.ExecuteWithResults(query);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                if (!(ds.Tables[0].Rows[0]["restore_date"] is DBNull))
                {
                    lastRestoreTime = (DateTime)ds.Tables[0].Rows[0]["restore_date"];
                }
            }
            return lastRestoreTime;
        }

        /// <summary>
        /// Start time of the coverage of the Log backup.
        /// </summary>
        /// <param name="bkset">The bkset.</param>
        /// <returns></returns>
        internal DateTime? LogStartTime(BackupSet bkset)
        {
            //Not forked
            if (bkset.ForkPointLsn == 0m)
            {
                //First occuring backupset that is in sequence.
                var item = (from BackupSet b in this.BackupSets
                            where BackupSet.IsBackupSetsInSequence(b, bkset)
                            orderby b.BackupStartDate ascending, b.ID ascending
                            select b.BackupStartDate).Take(1);
                if (item != null && item.Count() > 0)
                {
                    return item.ElementAt(0);
                }
                return null; //Can't find any compatible backupset that preceeds
            }

            //Forked
            if (this.ReadHeaderFromMedia)
            {
                //We don't have the restore history in this case so the best we can say is from the backup taken before this backup.
                var item = (from BackupSet b in this.BackupSets
                            where b.BackupStartDate <= bkset.BackupStartDate &&
                            b.BackupSetGuid != bkset.BackupSetGuid
                            orderby b.BackupStartDate descending, b.ID descending
                            select b.BackupStartDate).Take(1);
                if (item != null && item.Count() > 0)
                {
                    return item.ElementAt(0);
                }
                return null; //Can't find any compatible backupset that preceeds
            }
            else
            {
                //Read from msdb so we can look for the restore history.
                string query = string.Format(SmoApplication.DefaultCulture,
                 "SELECT TOP(1) restore_date FROM msdb.dbo.restorehistory WHERE destination_database_name = N'{0}' AND restore_date < N'{1}' ORDER BY restore_date DESC",
                 SqlSmoObject.SqlString(this.DatabaseName), bkset.BackupStartDate);

                DateTime? lastRestorePoint = null;
                DataSet ds = this.Server.ExecutionManager.ExecuteWithResults(query);
                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    if (!(ds.Tables[0].Rows[0]["restore_date"] is DBNull))
                    {
                        lastRestorePoint = (DateTime)ds.Tables[0].Rows[0]["restore_date"];
                        return lastRestorePoint;
                    }
                }
                return null; //Can't find any compatible backupset that preceeds
            }
        }

        /// <summary>
        /// Gives the Tail-log start time coverage.
        /// </summary>
        /// <returns></returns>
        public DateTime? TailLogStartTime()
        {
            if (this.IsTailLogBackupPossible(this.DatabaseName) && !this.ReadHeaderFromMedia)
            {
                DateTime? lastRestoreDateTime = GetLastRestoreDateTime();

                if (lastRestoreDateTime == null)
                {
                    lastRestoreDateTime = DateTime.MinValue;
                }

                var item = (from BackupSet bkset in this.BackupSets
                            where
                                bkset.BackupStartDate >= lastRestoreDateTime &&
                                bkset.BackupSetType == BackupSetType.Log
                            orderby
                                bkset.BackupStartDate descending
                            select
                                bkset.BackupStartDate).Take(1);

                if (item.Count() == 0)
                {
                    item = (from BackupSet bkset in this.BackupSets
                            where
                                bkset.BackupStartDate >= lastRestoreDateTime &&
                                (bkset.BackupSetType == BackupSetType.Differential ||
                                 bkset.BackupSetType == BackupSetType.Database) &&
                                 bkset.IsCopyOnly == false
                            orderby
                                bkset.BackupStartDate ascending
                            select
                                bkset.BackupStartDate).Take(1);
                }
                if (item.Count() > 0)
                {
                    return item.ElementAt(0);
                }
            }
            return null;
        }

        /// <summary>
        /// Takes the tail log backup.
        /// </summary>
        /// <param name="plan">The plan.</param>
        private void TakeTailLogBackup(RestorePlan plan)
        {
            if (string.IsNullOrEmpty(this.TailLogBackupFile))
            {
                throw new PropertyNotSetException("TailLogBackupFile");
            }
            Backup bk = new Backup();
            bk.Database = this.DatabaseName;
            bk.Action = BackupActionType.Log;
            bk.NoRecovery = this.TailLogWithNoRecovery;
            bk.BackupSetName = System.IO.Path.GetFileNameWithoutExtension(this.TailLogBackupFile);
            if (this.IsBackupDeviceUrl())
            {
                bk.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.Url));
            }
            else
            {
                bk.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.File));
            }
            bk.NoRewind = true;
            plan.TailLogBackupOperation = bk;
        }

        /// <summary>
        /// Takes the tail log restore.
        /// </summary>
        /// <param name="plan">The plan.</param>
        private void TakeTailLogRestore(RestorePlan plan)
        {
            if (string.IsNullOrEmpty(this.TailLogBackupFile))
            {
                throw new PropertyNotSetException("TailLogBackupFile");
            }
            Restore restore = new Restore();
            restore.Database = this.DatabaseName;
            restore.Action = RestoreActionType.Log;
            if (this.IsBackupDeviceUrl())
            {
                restore.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.Url));
            }
            else
            {
                restore.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.File));
            }
            restore.ToPointInTime = Smo.SqlSmoObject.SqlDateString(this.RestoreToPointInTime);
            plan.RestoreOperations.Add(restore);
        }

        /// <summary>
        /// Check whether the backup media is in URL
        /// </summary>
        private bool IsBackupDeviceUrl()
        {
            Uri testUri;
            return (Uri.TryCreate(this.TailLogBackupFile, UriKind.Absolute, out testUri) && (testUri.Scheme == Uri.UriSchemeHttps || testUri.Scheme == Uri.UriSchemeHttp));
        }

#endregion

#region private and internal methods

        /// <summary>
        /// Gets the backup set from MSDB.
        /// </summary>
        /// <returns>BackupSetCollection</returns>
        private BackupSetCollection GetBackupSetFromMSDB()
        {
            return new BackupSetCollection(this.Server, this.DatabaseName, true, IncludeSnapshotBackups);
        }

        /// <summary>
        /// Gets the error that occurred while reading the bachup devices
        /// </summary>
        /// <returns>An Exception including the error message</returns>
        public Exception GetBackupDeviceReadErrors()
        {
            if (this.backupDeviceReadErrors.Count() == 0)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            foreach (Exception e in this.backupDeviceReadErrors)
            {
                sb.Append(e.Message + "\t");
            }
            return new Exception(sb.ToString());
        }

        private List<Exception> backupDeviceReadErrors = new List<Exception>();
        /// <summary>
        /// Gets the backup set from devices.
        /// </summary>
        /// <returns>BackupSetCollection</returns>
        private BackupSetCollection GetBackupSetFromDevices()
        {
            this.backupDeviceReadErrors.Clear();
            BackupSetCollection bkSetColl = new BackupSetCollection(this.Server, this.DatabaseName, false, IncludeSnapshotBackups);
            List<BackupMediaSet> bkMediaSetList = new List<BackupMediaSet>();
            Dictionary<Guid, List<BackupDeviceItem>> dict = new Dictionary<Guid, List<BackupDeviceItem>>();
            foreach (BackupDeviceItem bkDeviceItem in this.BackupMediaList)
            {
                try
                {
                    if (this.CreateRestorePlanUpdates != null)
                    {
                        string bkDeviceName = (bkDeviceItem.Name != null)? bkDeviceItem.Name : string.Empty;
                        this.CreateRestorePlanUpdates(null, new CreateRestorePlanEventArgs(
                            string.Format(CultureInfo.InvariantCulture,"{0} - {1}", DatabaseRestorePlannerSR.IdentifyingMediaSets,bkDeviceName)));
                    }
                    Guid tguid = GetMediaSetGuid(bkDeviceItem);
                    if (tguid == new Guid())
                    {
                        tguid = System.Guid.NewGuid();
                        while (dict.ContainsKey(tguid))
                        {
                            tguid = System.Guid.NewGuid();
                        }
                    }
                    if (dict.ContainsKey(tguid))
                    {
                        dict[tguid].Add(bkDeviceItem);
                    }
                    else
                    {
                        List<BackupDeviceItem> list = new List<BackupDeviceItem>();
                        list.Add(bkDeviceItem);
                        dict.Add(tguid, list);
                    }
                }
                catch (ExecutionFailureException ex)
                {
                    backupDeviceReadErrors.Add(ex);
                }
            }

            foreach (List<BackupDeviceItem> list in dict.Values)
            {
                try
                {
                    List<BackupMedia> bkMediaList = backupMediaObjectList(list);
                    BackupMediaSet bkMediaSet = new BackupMediaSet(this.Server, bkMediaList);
                    if (this.CreateRestorePlanUpdates != null)
                    {
                        string mediaName = (bkMediaList.Count > 0 && bkMediaList[0].MediaName != null) ? bkMediaList[0].MediaName : string.Empty;
                        this.CreateRestorePlanUpdates(null, new CreateRestorePlanEventArgs(
                            string.Format(CultureInfo.InvariantCulture, "{0} - {1}", DatabaseRestorePlannerSR.ReadingBackupSetHeader, mediaName)));
                    }
                    List<BackupSet> bkSetList = bkMediaSet.ReadBackupSetHeader();
                    foreach (BackupSet bkSet in bkSetList)
                    {
                        if (this.Server.GetStringComparer().Compare(bkSet.DatabaseName, this.DatabaseName) == 0)
                        {
                            bkSetColl.backupsetList.Add(bkSet);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is BackupMediaSet.IncompleteBackupMediaSetException)
                    {
                        backupDeviceReadErrors.Add(ex);
                    }
                }

            }
            return bkSetColl;
        }

        private List<BackupMedia> backupMediaObjectList(List<BackupDeviceItem> list)
        {
            List<BackupMedia> ret = new List<BackupMedia>();
            foreach (BackupDeviceItem bkdevice in list)
            {
                try
                {
                    ret.Add(bkdevice.BackupMedia);
                }
                catch (ExecutionFailureException) { } //Ignore the devices whose headers are unreadable.
            }
            return ret;
        }

        /// <summary>
        /// Gets the media set GUID.
        /// </summary>
        /// <param name="bkDeviceItem">The backup device item.</param>
        /// <returns></returns>
        internal Guid GetMediaSetGuid(BackupDeviceItem bkDeviceItem)
        {
            Guid guid = new Guid();
            DataTable dt = bkDeviceItem.DeviceLabel(this.Server);
            if (dt != null && dt.Rows.Count > 0)
            {
                if (dt.Rows[0]["MediaSetId"] is Guid)
                {
                    guid = (Guid)dt.Rows[0]["MediaSetId"];
                }
                else if(dt.Rows[0]["MediaSetId"] is string)
                {
                    guid = new Guid(dt.Rows[0]["MediaSetId"] as string);
                }
            }
            return guid;
        }

        /// <summary>
        /// Refreshes the backup sets.
        /// </summary>
        /// <returns></returns>
        public bool RefreshBackupSets()
        {
            this.backupSets = null;
            if (this.BackupSets != null)
            {
                return true;
            }
            return false;
        }

#endregion


#region private classes

        private class BackupDeviceCollection : ICollection<BackupDeviceItem>
        {
            public bool Dirty = true;
            ICollection<BackupDeviceItem> items = new List<BackupDeviceItem>();

            public void Add(BackupDeviceItem item)
            {
                var match = from BackupDeviceItem bkdev in items
                            where bkdev.Name.Equals(item.Name) &&
                            bkdev.DeviceType == item.DeviceType
                            select bkdev;
                if (match.Count() == 0)
                {
                    this.Dirty = true;
                    items.Add(item);
                }
            }

            public void Clear()
            {
                this.Dirty = true;
                items.Clear();
            }

            public bool Remove(BackupDeviceItem item)
            {
                this.Dirty = true;
                return items.Remove(item);
            }

            public bool Contains(BackupDeviceItem item)
            {
                return items.Contains(item);
            }

            public void CopyTo(BackupDeviceItem[] array, int arrayIndex)
            {
                items.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return items.Count; }
            }

            public bool IsReadOnly
            {
                get { return items.IsReadOnly; }
            }

            public IEnumerator<BackupDeviceItem> GetEnumerator()
            {
                return items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((List<BackupDeviceItem>)items as IEnumerable).GetEnumerator();
            }
        }

#endregion
    }
}
