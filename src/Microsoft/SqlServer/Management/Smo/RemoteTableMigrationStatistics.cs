// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Statistics about the Remote table created for Remote Data Archive
    /// </summary>
    public class RemoteTableMigrationStatistics
    {
        /// <summary>
        /// ctor
        /// </summary>
        internal RemoteTableMigrationStatistics()
        {
            this.SizeInKB = 0;
            this.RowCount = 0;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sizeInKB">Remote table size in KB</param>
        /// <param name="rowCount">Number of rows migrated to the remote table</param>
        internal RemoteTableMigrationStatistics(double sizeInKB, long rowCount)
        {
            this.SizeInKB = sizeInKB;
            this.RowCount = rowCount;
        }

        /// <summary>
        /// Remote table size in KB
        /// </summary>
        public double SizeInKB
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of rows migrated to the remote table
        /// </summary>
        public long RowCount
        {
            get;
            private set;
        }
    }
}
