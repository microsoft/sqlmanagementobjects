// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Page Restore Planner
    /// </summary>
    public sealed class PageRestorePlanner
    {
        #region constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PageRestorePlanner"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        public PageRestorePlanner(Database database)
        {
            this.Database = database;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageRestorePlanner"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="tailLogBackupFileName">Name of the tail log backup file.</param>
        public PageRestorePlanner(Database database, String tailLogBackupFileName)
        {
            this.Database = database;
            this.TailLogBackupFile = tailLogBackupFileName;
        }

        #endregion

        #region properties

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>The server.</value>
        private Server Server
        {
            get
            {
                if(this.Database!= null)
                {
                    return this.Database.GetServerObject();
                }
                return null;
            }
        }

        /// <summary>
        /// Gets or sets the database.
        /// </summary>
        /// <value>The database.</value>
        private Database database;
        public Database Database
        {
            get
            {
                return database;
            }
            set
            {
                this.database = value;
                if (this.database != null)
                {
                    this.SuspectPages.Clear();
                    List < SuspectPage > pages = GetSuspectPages(this.database);
                    foreach (SuspectPage page in pages)
                    {
                        try
                        {
                            page.Validate();
                            this.SuspectPages.Add(page);
                        }
                        catch(Exception){} // Don't add pages that can't be Restored.
                    }
                }
            }
        }


        /// <summary>
        /// Gets or sets the tail log backup file.
        /// </summary>
        /// <value>The tail log backup file.</value>
        public string TailLogBackupFile
        {
            get;
            set;
        }

        private List<SuspectPage> suspectPages = new List<SuspectPage>();
        /// <summary>
        /// Gets or sets the suspect pages.
        /// </summary>
        /// <value>The suspect pages.</value>
        public ICollection<SuspectPage> SuspectPages
        {
            get
            {
                return this.suspectPages;
            }
        }

        #endregion 

        #region Create Restore Plan

        /// <summary>
        /// Creates the restore plan.
        /// </summary>
        /// <returns></returns>
        public RestorePlan CreateRestorePlan()
        {
            CheckPageRestorePossible();
            
            RestorePlan plan = new RestorePlan(this.Database);
            plan.RestoreAction = RestoreActionType.OnlinePage;
            if (this.SuspectPages == null || this.SuspectPages.Count < 1)
            {
                return plan;
            }
            
            BackupSetCollection backupsets = this.Database.BackupSets;
            List<BackupSet> selBkSets = CreatePageRestorePlan(backupsets);
            if (selBkSets.Count > 0)
            {
                plan.AddRestoreOperation(selBkSets);
                AddTailLogBackupRestore(plan, backupsets);
                plan.RestoreOperations[0].Action = RestoreActionType.OnlinePage;
                foreach (SuspectPage page in this.SuspectPages)
                {
                    page.Validate();
                    plan.RestoreOperations[0].DatabasePages.Add(page);
                }
                this.CheckDuplicateSuspectPages();
            }
            plan.SetRestoreOptions(new RestoreOptions());
            return plan;
        }

        /// <summary>
        /// Adds the tail log backup.
        /// </summary>
        /// <param name="plan">The plan.</param>
        /// <param name="backupSets"></param>
        private void AddTailLogBackupRestore(RestorePlan plan, BackupSetCollection backupSets)
        {
            if (string.IsNullOrEmpty(this.TailLogBackupFile))
            {
                throw new PropertyNotSetException("TailLogBackupFile");
            }
            //Check if the backup file already exists. 
            // We need to create a new file here for the t-log restore to work properly
                      
            Backup bk = new Backup();
            bk.Database = this.Database.Name;
            bk.Action = BackupActionType.Log;
            bk.BackupSetName = System.IO.Path.GetFileNameWithoutExtension(this.TailLogBackupFile);
            if (BackupRestoreBase.IsBackupDeviceUrl(this.TailLogBackupFile))
            {
                bk.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.Url));
            }
            else
            {
                this.TailLogBackupFile = Path.GetFullPath(this.TailLogBackupFile);
                // Check whether the path is well formed and also make sure that there is no other file with the same name
                if (!BackupRestoreBase.CheckNewBackupFile(this.Server, this.TailLogBackupFile))
                {
                    throw new WrongPropertyValueException(ExceptionTemplates.BackupFileAlreadyExists(this.TailLogBackupFile));
                }
                bk.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.File));
            }
            bk.NoRewind = true;
            plan.TailLogBackupOperation = bk;

            Restore restore = new Restore();
            restore.Database = this.Database.Name;
            restore.Action = RestoreActionType.Log;
            if (BackupRestoreBase.IsBackupDeviceUrl(this.TailLogBackupFile))
            {
                restore.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.Url));
            }
            else
            {
                restore.Devices.Add(new BackupDeviceItem(this.TailLogBackupFile, DeviceType.File));
            }
            plan.RestoreOperations.Add(restore);
        }

        /// <summary>
        /// Determines whether [is tail log backup possible].
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if [is tail log backup possible]; otherwise, <c>false</c>.
        /// </returns>
        internal void CheckPageRestorePossible()
        {
            if (this.Database == null)
            {
                throw new PropertyNotSetException("Database");
            }
            if (this.Server.Version.Major < 9)
            {
                throw new InvalidVersionSmoOperationException(this.Server.ExecutionManager.GetServerVersion());
            }
            if(this.Database.RecoveryModel != RecoveryModel.Full)
            {
                throw new InvalidOperationException(ExceptionTemplates.PageRestoreOnlyForFullRecovery);
            }
            if (this.Database.Status != DatabaseStatus.Normal && this.Database.Status != DatabaseStatus.Suspect && this.Database.Status != DatabaseStatus.EmergencyMode)
            {
                throw new InvalidOperationException(ExceptionTemplates.InvalidDatabaseState);
            }
        }

        /// <summary>
        /// Creates the page restore plan.
        /// </summary>
        /// <param name="backupsets">The backup set collection.</param>
        /// <returns></returns>
        private List<BackupSet> CreatePageRestorePlan(BackupSetCollection backupsets)
        {
            DateTime lastBkStartDate;
            List<BackupSet> selBkSet = new List<BackupSet>();
            int fullBackupIndex = 0, latestBackupSetIndex = -1;

            //Select the latest full and differential backups.
            IEnumerable<BackupSet> item;
            item = (from BackupSet bkset in backupsets
                    where bkset.BackupSetType == BackupSetType.Database ||
                    bkset.BackupSetType == BackupSetType.Differential
                    orderby bkset.BackupStartDate descending, bkset.ID descending
                    select bkset);

            //Select the index of latest full backup
            for (fullBackupIndex = 0; fullBackupIndex < item.Count(); fullBackupIndex++)
            {
                if (item.ElementAt(fullBackupIndex).BackupSetType == BackupSetType.Database)
                {
                    selBkSet.Add(item.ElementAt(fullBackupIndex));
                    latestBackupSetIndex++;
                    break;
                }
            }

            //Return if full backup not found
            if (fullBackupIndex == item.Count())
            {
                selBkSet.Clear();
                return selBkSet;
            }
            else if (fullBackupIndex > 0)
            {
                //If index of full backup > 0, differential backup exists. 
                //Take differential backup at 0 index since it is the latest one
                selBkSet.Add(item.ElementAt(0));
                latestBackupSetIndex++;
            }

            //Return the last backup start date of full backup if no differential backup is present.
            //else return the backup start date of latest differential backup.
            lastBkStartDate = selBkSet[latestBackupSetIndex].BackupStartDate;

            item = from BackupSet bkset in backupsets
                   where bkset.BackupSetType == BackupSetType.Log &&
                   bkset.BackupStartDate >= lastBkStartDate 
                   orderby bkset.BackupStartDate, bkset.ID
                   select bkset;
            foreach (BackupSet bk in item)
            {
                selBkSet.Add(bk);
            }

            for (int i = 0; i < selBkSet.Count - 1; i++)
            {
                if(!BackupSet.IsBackupSetsInSequence(selBkSet[i],selBkSet[i+1]))
                {
                    throw new SmoException(ExceptionTemplates.UnableToCreatePageRestoreSequence);
                }
            }

            //Checking for forked scenarios we don't give any restore Plan in those cases. 
            string query = string.Format(SmoApplication.DefaultCulture,
                     "SELECT TOP(1) restore_date FROM msdb.dbo.restorehistory WHERE destination_database_name = N'{0}' ORDER BY restore_date DESC",
                     SqlSmoObject.SqlString(this.Database.Name));

            DateTime? lastRestorePoint = null;
            DataSet ds = this.Server.ExecutionManager.ExecuteWithResults(query);
            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                if (!(ds.Tables[0].Rows[0]["restore_date"] is DBNull))
                {
                    lastRestorePoint = (DateTime)ds.Tables[0].Rows[0]["restore_date"];
                }
            }
            if (lastRestorePoint != null && selBkSet.Count > 0 && lastRestorePoint > selBkSet[selBkSet.Count -1].BackupStartDate)
            {
                throw new SmoException(ExceptionTemplates.UnableToCreatePlanTakeTLogBackup);
            }
            return selBkSet;
        }

        /// <summary>
        /// Checks for duplicate suspect pages.
        /// </summary>
        internal void CheckDuplicateSuspectPages()
        {
            this.suspectPages.Sort();

            for (int i = 1; i < suspectPages.Count; i++)
            {
                if(suspectPages[i-1].Equals(suspectPages[i]))
                {
                    throw new SmoException(ExceptionTemplates.DuplicateSuspectPage(suspectPages[i].FileID,suspectPages[i].PageID));
                }
            }
        }

        public static List<SuspectPage> GetSuspectPages(Database database)
        {
            List<SuspectPage> ret = new List<SuspectPage>();
            Restore restore = new Restore();
            restore.Database = database.Name;
            DataTable dt = restore.ReadSuspectPageTable(database.GetServerObject());
            if (dt != null)
            {
                foreach (DataRow dr in dt.Rows)
                {
                    int fileid = (int)dr["file_id"];
                    long pageid = (long)dr["page_id"];
                    int errCode = (int)dr["event_type"];

                    if (errCode == 1 || errCode == 2 || errCode == 3)
                    {
                        ret.Add(new SuspectPage(fileid, pageid));
                    }
                }
            }
            return ret;
        }

        #endregion
    }
}
