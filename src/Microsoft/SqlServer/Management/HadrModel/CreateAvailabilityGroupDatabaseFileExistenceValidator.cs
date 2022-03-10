// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the database-files that will be created on secondary
    /// as part of Create Availability Group Scenario
    /// does not already exist on the secondary.
    /// </summary>
    public class CreateAvailabilityGroupDatabaseFileExistenceValidator : DatabaseFileExistenceValidator
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        public CreateAvailabilityGroupDatabaseFileExistenceValidator(AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(Resource.ValidatingDatabaseFilesNotExistsOnSecondary, availabilityGroupData, replica)
        {
            this.DatabasesToValidate = availabilityGroupData.NewAvailabilityDatabases;
        }
    }
}