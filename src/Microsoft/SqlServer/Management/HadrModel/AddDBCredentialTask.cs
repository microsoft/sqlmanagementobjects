// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Globalization;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Prevision the password for database master key on a Secondary server.
    /// Depends on the succesful backup of the log on the primary.
    /// </summary>
    public class AddDBCredentialTask : HadrTask
    {
        /// <summary>
        /// The value of whether add database credential for existing availability databases with default value "false"
        /// If not, add database credential for new availability databases
        /// </summary>
        private bool useExistingAvailabilityDatabases = false;

        /// <summary>
        /// AvailabilityGroupData object contains the whole ag group
        /// </summary>
        private AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Target Database Name
        /// </summary>
        private readonly string databaseName;

        /// <summary>
        /// The secondary replica in which to restore the logs
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="databaseName">target database name</param>
        /// <param name="availabilityGroupData">agData</param>
        /// <param name="replica">the secondary replica in which to restore the logs</param>
        public AddDBCredentialTask(string databaseName, AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.AddDatabaseCredentialText, databaseName, replica.AvailabilityGroupReplicaData.ReplicaName))
        {
            if (String.IsNullOrEmpty(databaseName))
            {
                throw new ArgumentNullException("databaseName");
            }

            this.databaseName = databaseName;

            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            this.replica = replica;

            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// The availability group data
        /// </summary>
        public AvailabilityGroupData AvailabilityGroupData
        {
            get
            {
                return this.availabilityGroupData;
            }
        }

        /// <summary>
        /// The replica data for the replica on which need to add credential
        /// </summary>
        public AvailabilityGroupReplica ReplicaData
        {
            get
            {
                return this.replica;
            }
        }

        /// <summary>
        /// The value of whether add database credential for existing availability databases 
        /// If not, add database credential for new availability databases
        /// </summary>
        public bool UseExistingAvailabilityDatabases
        {
            get 
            { 
                return this.useExistingAvailabilityDatabases; 
            }
            set 
            { 
                this.useExistingAvailabilityDatabases = value; 
            }
        }

        /// <summary>
        /// Method to performing restore Log
        /// </summary>
        /// <param name="policy"></param>
        protected override void Perform(IExecutionPolicy policy)
        {   
            SMO.Server server = HadrModelUtilities.GetNewSmoServerObject(this.replica.AvailabilityGroupReplicaData.Connection);

            List<PrimaryDatabaseData> databases = this.useExistingAvailabilityDatabases ? 
                this.AvailabilityGroupData.ExistingAvailabilityDatabases : 
                this.AvailabilityGroupData.NewAvailabilityDatabases;
            PrimaryDatabaseData dbData = databases.FirstOrDefault(c => c.Name.Equals(databaseName));
            if (dbData!= null && dbData.DBMKPassword != null)
            {
                FailoverUtilities.ProvisionCredential(server, databaseName, dbData.DBMKPassword);   
            }
        }

        /// <summary>
        /// Not Support for rolling back this task
        /// </summary>
        /// <param name="policy"></param>
        protected override void Rollback(IExecutionPolicy policy)
        {
            throw new NotSupportedException();
        }

    }
}
