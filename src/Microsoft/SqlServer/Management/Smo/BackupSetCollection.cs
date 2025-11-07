// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Backupset collection class
    /// </summary>
    public sealed class BackupSetCollection : ICollection
    {
        internal List<BackupSet> backupsetList = new List<BackupSet>();
        private readonly ICollection backupsets;
        private readonly Database parent;
        private readonly string databaseName;
        private readonly Server server;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupSetCollection"/> class.
        /// </summary>
        /// <param name="parent">The parent database.</param>
        internal BackupSetCollection(Database parent)
        {
            this.parent = parent;
            this.databaseName = parent.Name;
            this.server = parent.GetServerObject();
            backupsets = backupsetList as ICollection;
            GetBackupSetsFromMsdb(true);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackupSetCollection"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="DatabaseName">Name of the database.</param>
        /// <param name="PopulateFromMsdb">if set to <c>true</c> [populate from MSDB].</param>
        /// <param name="includeSnapshotBackups">Whether to include snapshots in the backup list</param>
        internal BackupSetCollection(Server server,String DatabaseName,bool PopulateFromMsdb, bool includeSnapshotBackups )
        {
            this.server = server;
            databaseName = DatabaseName;
            backupsets = backupsetList as ICollection;
            if (PopulateFromMsdb)
            {
                GetBackupSetsFromMsdb(includeSnapshotBackups);
            }
        }

        /// <summary>
        /// Gets the <see cref="Microsoft.SqlServer.Management.Smo.BackupSet"/> at the specified index.
        /// </summary>
        /// <value>Backup set</value>
        public BackupSet this[int index]
        {
            get
            {
                return backupsetList[index];
            }
        }

        
        /// <summary>
        /// Gets the backup sets from MSDB.
        /// Reads Only fetches the guid of the backupsets,
        /// rest of the properties are populated when
        /// any of the properties is accessed.
        /// </summary>
        private void GetBackupSetsFromMsdb(bool includeSnapshotBackups)
        {
            DataSet ds = null;
            String query = null;
            var snapshotClause = (this.server.ServerVersion.Major < 9 || includeSnapshotBackups) ? string.Empty : "and bkps.is_snapshot = 0";

            if (this.server.ServerVersion.Major < 9 || parent == null || this.parent.Status == DatabaseStatus.Offline)
            {
                query = string.Format(SmoApplication.DefaultCulture,
                    "SELECT bkps.backup_set_uuid FROM msdb.dbo.backupset bkps WHERE bkps.database_name = {0} {1} ORDER BY bkps.backup_set_id ASC",
                    SqlSmoObject.MakeSqlString(this.databaseName), snapshotClause);
            }
            else
            {
                query = string.Format(SmoApplication.DefaultCulture,
                    "SELECT bkps.backup_set_uuid FROM msdb.dbo.backupset bkps WHERE bkps.database_guid = {0} {1} ORDER BY bkps.backup_set_id ASC",
                    SqlSmoObject.MakeSqlString(this.parent.DatabaseGuid.ToString()), snapshotClause);
            }
            ds = server.ExecutionManager.ExecuteWithResults(query);
            if (ds.Tables.Count > 0)
            {
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    Guid bkguid = (Guid)dr[0];
                    BackupSet bkset = new BackupSet(this.server, bkguid);
                    backupsetList.Add(bkset);
                }
            }
        }
                
        #region ICollection Members

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="T:System.ArgumentNullException">
        /// 	<paramref name="array"/> is null.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// 	<paramref name="index"/> is less than zero.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// 	<paramref name="array"/> is multidimensional.
        /// -or-
        /// <paramref name="index"/> is equal to or greater than the length of <paramref name="array"/>.
        /// -or-
        /// The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>.
        /// </exception>
        /// <exception cref="T:System.ArgumentException">
        /// The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.
        /// </exception>
        public void CopyTo(Array array, int index)
        {
            backupsets.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
        /// </returns>
        public int Count
        {
            get
            {
                return backupsets.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
        /// </summary>
        /// <value></value>
        /// <returns>true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.
        /// </returns>
        public bool IsSynchronized
        {
            get
            {
                return backupsets.IsSynchronized;
            }
        }

        /// <summary>
        /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
        /// </returns>
        public object SyncRoot
        {
            get
            {
                return backupsets.SyncRoot;
            }
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator GetEnumerator()
        {
            return backupsets.GetEnumerator();
        }

        #endregion
    }
}
