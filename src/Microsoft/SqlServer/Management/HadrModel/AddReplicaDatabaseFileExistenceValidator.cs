// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the database-files that will be created on secondary
    /// as part of Add Replica Scenario
    /// does not already exist on the secondary.
    /// </summary>
    public class AddReplicaDatabaseFileExistenceValidator : DatabaseFileExistenceValidator
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        public AddReplicaDatabaseFileExistenceValidator(AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(Resource.ValidatingDatabaseFilesNotExistsOnSecondary, availabilityGroupData, replica)
        {
            this.DatabasesToValidate = availabilityGroupData.ExistingAvailabilityDatabases;
        }
    }
}
