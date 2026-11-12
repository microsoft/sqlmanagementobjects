// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// Validates that the database-files that will be created on secondary
    /// does not already exist on the secondary.
    public abstract class DatabaseFileExistenceValidator : Validator
    {
        /// <summary>
        /// The availability group data.
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// The replica data
        /// </summary>
        private readonly AvailabilityGroupReplica replica;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name">The name of the validator</param>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        protected DatabaseFileExistenceValidator(string name, AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(name)
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            if (replica == null)
            {
                throw new ArgumentNullException("replica");
            }

            this.availabilityGroupData = availabilityGroupData;
            this.replica = replica;
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
        /// List of databases to validate
        /// </summary>
        public List<PrimaryDatabaseData> DatabasesToValidate
        {
            get; 
            protected set;
        }

        /// <summary>
        /// Validates if the database files does not already exist on the secondary.
        /// </summary>
        /// <param name="policy">The policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This validation is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;

            List<string> existingFileNames = new List<string>();
            SMO.Server primaryServer = this.availabilityGroupData.PrimaryServer;
            SMO.Server secondaryServer = this.replica.GetServer();

            foreach (PrimaryDatabaseData primaryDatabase in this.DatabasesToValidate)
            {
                SMO.Database db = primaryServer.Databases[primaryDatabase.Name];

                foreach (SMO.FileGroup fg in db.FileGroups)
                {
                    foreach (SMO.DataFile df in fg.Files)
                    {
                        if (secondaryServer.FileExists(df.FileName))
                        {
                            existingFileNames.Add(df.FileName);
                        }
                    }
                }

                foreach (SMO.LogFile lf in db.LogFiles)
                {
                    if (secondaryServer.FileExists(lf.FileName))
                    {
                        existingFileNames.Add(lf.FileName);
                    }
                }
            }

            if (existingFileNames.Count > 0)
            {
                throw new DatabaseFileAlreadyExistsOnReplicaException(
                    this.replica.AvailabilityGroupReplicaData.ReplicaName, existingFileNames);
            }
        }
    }
}
