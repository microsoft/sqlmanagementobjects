// Copyright (c) Microsoft.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Statistics about the Remote database created for Remote Data Archive
    /// </summary>
    public class RemoteDatabaseMigrationStatistics
    {
        /// <summary>
        /// ctor
        /// </summary>
        internal RemoteDatabaseMigrationStatistics()
        {
            this.RemoteDatabaseSizeInMB = 0;
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="remoteDatabaseSizeInMB">Remote database size in MB</param>
        internal RemoteDatabaseMigrationStatistics(double remoteDatabaseSizeInMB)
        {
            this.RemoteDatabaseSizeInMB = remoteDatabaseSizeInMB;
        }

        /// <summary>
        /// Remote database size in MB
        /// </summary>
        public double RemoteDatabaseSizeInMB
        {
            get;
            private set;
        }
    }
}
