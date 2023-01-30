// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that databases being added to the
    /// AvailabilityGroup do not exist on the secondary replica
    /// </summary>
    public abstract class DatabaseExistenceValidator : Validator
    {
        /// <summary>
        /// The availability group data.
        /// </summary>
        public AvailabilityGroupData AvailabilityGroupData
        {
            get; 
            private set;
        }

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
        protected DatabaseExistenceValidator(string name, AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
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

            this.AvailabilityGroupData = availabilityGroupData;
            this.replica = replica;
            this.DatabasesToValidate = availabilityGroupData.NewAvailabilityDatabases;
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
        /// Validates if a selecteddatabase already exists on the 
        /// secondary replica.
        /// </summary>
        /// <param name="policy">The policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This validation is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;
            
            List<string> existingDatabases = new List<string>();
            Smo.Server secondaryReplica = this.replica.GetServer();

            // collect the names of all the SelectedDatabases that exist on the replica
            // so that we can give a complete list of databases that are conflicting 
            // when throwing the exception.
            existingDatabases.AddRange(
                    from PrimaryDatabaseData db in this.DatabasesToValidate
                    where secondaryReplica.Databases.Contains(db.Name)
                    select db.Name);

            if (existingDatabases.Count > 0)
            {
                throw new DatabaseAlreadyExistsException(this.replica.AvailabilityGroupReplicaData.ReplicaName, existingDatabases);
            }
        }
    }
}
