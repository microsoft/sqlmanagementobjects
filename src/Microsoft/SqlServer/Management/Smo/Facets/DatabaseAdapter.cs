// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Diagnostics = Microsoft.SqlServer.Management.Diagnostics;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    public class DatabaseAdapterBase : IRefreshable, IAlterable
    {
        Microsoft.SqlServer.Management.Smo.Database wrappedObject = null;

        public DatabaseAdapterBase(Microsoft.SqlServer.Management.Smo.Database obj)
        {
            this.wrappedObject = obj;
        }

        protected Microsoft.SqlServer.Management.Smo.Database Database
        {
            get
            {
                return this.wrappedObject;
            }
        }

        #region Database Properties
        public double Size
        {
            get
            {
                return this.Database.Size;
            }
        }

        public DatabaseStatus Status
        {
            get
            {
                return this.Database.Status;
            }
        }

        public DateTime LastBackupDate
        {
            get
            {
                return this.Database.LastBackupDate;
            }
        }

        public DateTime LastLogBackupDate
        {
            get
            {
                return this.Database.LastLogBackupDate;
            }
        }

        public bool IsSystemObject
        {
            get
            {
                return this.Database.IsSystemObject;
            }
        }
        #endregion

        #region Database Options
        /// <summary>
        /// </summary>
        public bool Trustworthy
        {
            get
            {
                return this.Database.DatabaseOptions.Trustworthy;
            }
            set
            {
                this.Database.DatabaseOptions.Trustworthy = value;
            }
        }

        /// <summary>
        /// Returns true if the database has Ledger ON.
        /// </summary>
        public bool IsLedger => this.Database.DatabaseOptions.IsLedger;

        /// <summary>
        /// Returns true if the database has auto-close set to ON.
        /// </summary>
        public bool AutoClose
        {
            get
            {
                return this.Database.DatabaseOptions.AutoClose;
            }
            set
            {
                this.Database.DatabaseOptions.AutoClose = value;
            }
        }

        /// <summary>
        /// Returns true if the database has auto-shrink option enabled.
        /// </summary>
        public bool AutoShrink
        {
            get
            {
                return this.Database.DatabaseOptions.AutoShrink;
            }
            set
            {
                this.Database.DatabaseOptions.AutoShrink = value;
            }
        }

        /// <summary>
        /// Returns the database recovery model. 
        /// </summary>
        public RecoveryModel RecoveryModel
        {
            get
            {
                return this.Database.DatabaseOptions.RecoveryModel;
            }
            set
            {
                this.Database.DatabaseOptions.RecoveryModel = value;
            }
        }

        /// <summary>
        /// Returns true if the database is read-only.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                return this.Database.DatabaseOptions.ReadOnly;
            }
            set
            {
                this.Database.DatabaseOptions.ReadOnly = value;
            }
        }
        

        /// <summary>
        /// Returns true if the PAGE_VERIFY setting of a database is set to CHECKSUM.
        /// </summary>
        public PageVerify PageVerify
        {
            get
            {
                return this.Database.DatabaseOptions.PageVerify;
            }
            set
            {
                this.Database.DatabaseOptions.PageVerify = value;
            }
        }


        /// <summary>
        /// Returns target recovery time  setting of a database.
        /// </summary>
        public Int32 TargetRecoveryTime
        {
            get
            {
                return this.Database.TargetRecoveryTime;
            }
            set
            {
                this.Database.TargetRecoveryTime = value;
            }
        }


        /// <summary>
        /// Returns delayed durability setting of a database.
        /// </summary>
        public DelayedDurability DelayedDurability
        {
            get
            {
                return this.Database.DelayedDurability;
            }
            set
            {
                this.Database.DelayedDurability = value;
            }
        }

        #endregion

        #region Refresh and Alter
        public virtual void Refresh()
        {
            this.Database.Refresh();
        }

        public virtual void Alter()
        {
            this.Database.Alter();
        }
        #endregion

        #region Helpers
        public string GetVolume(string file)
        {
            string pathRoot = System.IO.Path.GetPathRoot(file);
            return pathRoot.ToUpperInvariant();
        }


        /// <summary>
        /// This method compares the drive letter from files in the filegroups against the drive letters
        /// in the checkDrives parameter.  If any one file drive letter is in the list of checkVolumes, then the
        /// function returns true otherwise it returns false.
        /// </summary>
        /// <param name="checkVolumes"></param>
        /// <returns></returns>
        protected bool DataFileVolumeNotIn(List<string> checkVolumes)
        {
            Diagnostics.TraceHelper.Assert(checkVolumes != null, "DataFileVolumeNotIn parameter checkVolumes should not be null");

            bool dataNotInCheck = true;
            if (checkVolumes.Count > 0)  // skip the check if there are no dives to compare against.
            {
                // iterate through the filegroup files and lookup the drives in the checkDrives list
                foreach (FileGroup fileGroups in this.Database.FileGroups)
                {
                    foreach (DataFile file in fileGroups.Files)
                    {
                        string volume = GetVolume(file.FileName);
                        if (checkVolumes.Contains(volume))
                        {
                            dataNotInCheck = false;
                            return dataNotInCheck;
                        }
                    }
                }
            }
            return dataNotInCheck;
        }
        #endregion
    }

    public partial class DatabaseAdapter : DatabaseAdapterBase, IDmfAdapter, Sfc.IDmfFacet
    {
        #region Constructors
        public DatabaseAdapter(Microsoft.SqlServer.Management.Smo.Database obj)
            : base(obj)
        {
        }
        #endregion
    }
}
