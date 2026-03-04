// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the folders needed for database-files of the databases 
    /// being added to the  AvailabilityGroup when creating an AvailabiltyGroup 
    /// exist on the secondary.
    /// </summary>
    public class CreateAvailabilityGroupDatabaseFileCompatibilityValidator : DatabaseFileCompatibilityValidator
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        public CreateAvailabilityGroupDatabaseFileCompatibilityValidator(AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.ValidatingDatabaseFileLocationCompatibility, replica.AvailabilityGroupReplicaData.ReplicaName), availabilityGroupData, replica)
        {
            this.DatabasesToValidate = availabilityGroupData.NewAvailabilityDatabases;
        }
    }
}