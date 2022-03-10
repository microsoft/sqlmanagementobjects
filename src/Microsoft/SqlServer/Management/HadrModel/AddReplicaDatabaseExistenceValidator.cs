// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that databases existing in an
    /// AvailabilityGroup when adding a replica to an AvailabiltyGroup 
    /// do not exist on the secondary replica
    /// </summary>
    public class AddReplicaDatabaseExistenceValidator : DatabaseExistenceValidator
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        /// <param name="replica">The replica data</param>
        public AddReplicaDatabaseExistenceValidator(AvailabilityGroupData availabilityGroupData, AvailabilityGroupReplica replica)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.ValidatingDatabaseNotExistOnSecondary, replica.AvailabilityGroupReplicaData.ReplicaName), availabilityGroupData, replica)
        {
            this.DatabasesToValidate = availabilityGroupData.ExistingAvailabilityDatabases;
        }
    }
}
