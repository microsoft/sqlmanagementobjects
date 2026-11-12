// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Primary Database Data
    /// </summary>
    public class PrimaryDatabaseData
    {
        private SMO.Database smoDatabase;
        private string existingAvailabilityGroupName;

        /// <summary>
        /// Database Name
        /// </summary>
        public string Name 
        {
            get { return this.smoDatabase.Name; }
            }

        /// <summary>
        /// Gets the size of the database
        /// </summary>
        public double DatabaseSize
        {
            get { return this.smoDatabase.Size; }
        }

        /// <summary>
        /// The availability group to which the database belongs
        /// This will be null if the database is not part of an
        /// existing availability group
        /// </summary>
        public string AvailabiliyGroupName
        {
            get { return this.existingAvailabilityGroupName; }
        }

        /// <summary>
        /// True if the database is already a part of Availability Group
        /// </summary>
        public bool IsPartOfExistingAvailabilityGroup
        {
            get
            {
                return !string.IsNullOrEmpty(AvailabiliyGroupName);
            }
        }

        /// <summary>
        /// The file where the wizard will take a full-backup of the database
        /// </summary>
        public string DatabaseFullBackupFile
        {
            get; 
            set;
        }

        /// <summary>
        /// The file where the wizard will take a log-backup of the database
        /// </summary>
        public string DatabaseLogBackupFile
        {
            get; 
            set;
        }

        /// <summary>
        /// Constructor to use in case the database is part of an
        /// existing availability group
        /// </summary>
        /// <param name="database">The database</param>
        /// <param name="availabilityGroupName">The name of the availability group</param>
        public PrimaryDatabaseData(SMO.Database database, string availabilityGroupName)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (availabilityGroupName == null)
            {
                throw new ArgumentNullException("availabilityGroupName");
            }

            this.smoDatabase = database;
            this.existingAvailabilityGroupName = availabilityGroupName;
        }

        /// <summary>
        /// The constructor to use when the database needs to be added
        /// to a new availability group
        /// </summary>
        /// <param name="database">The database</param>
        public PrimaryDatabaseData(SMO.Database database)
            {
            if (database == null)
                {
                throw new ArgumentNullException("database");
        }

            this.smoDatabase = database;
        }

        /// <summary>
        /// The password used to encrypt database master key
        /// </summary>
        public string DBMKPassword
        {
            get;
            set;
        }
    }
}
