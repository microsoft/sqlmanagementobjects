// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the folders needed for database-files of the databases 
    /// that exist in the AvailabilityGroup when adding a replica to AvailabiltyGroup 
    /// exist on the secondary.
    /// </summary>
    public class AddReplicaDatabaseFileCompatibilityValidator : DatabaseFileCompatibilityValidator
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        public AddReplicaDatabaseFileCompatibilityValidator(AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(Resource.FormatValidatingDatabaseFileLocationCompatibility(replica.AvailabilityGroupReplicaData.ReplicaName), availabilityGroupData, replica)
        {
            this.DatabasesToValidate = availabilityGroupData.ExistingAvailabilityDatabases;
        }
    }
}
