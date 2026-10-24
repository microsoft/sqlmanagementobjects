// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Linq;
using Microsoft.SqlServer.Management.HadrData;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the folders needed for database-files of the databases 
    /// being added to the  AvailabilityGroup when creating an AvailabiltyGroup 
    /// exist on the secondary.
    /// </summary>
    public class AvailabilityModeValidator : Validator
    {
        /// <summary>
        /// The availability group data.
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public AvailabilityModeValidator(AvailabilityGroupData availabilityGroupData)
            : base(Resource.ValidatingAvailabilityMode)
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// Validates if a folder necessary for creating the database
        /// exists on the secondary.
        /// </summary>
        /// <param name="policy">The policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This validation is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;

            AvailabilityGroupReplica primary = 
                this.availabilityGroupData.AvailabilityGroupReplicas.First(replica => replica.AvailabilityGroupReplicaData.InitialRole == ReplicaRole.Primary);


            if (primary.AvailabilityGroupReplicaData.AvailabilityMode == SMO.AvailabilityReplicaAvailabilityMode.AsynchronousCommit) // Validation needed only if primary has async commit
            {
                // If any replica has synchronous commit mode when primary's commit mode is asynchronous, throw a validation exception
                if (this.availabilityGroupData.AvailabilityGroupReplicas.Any(replica => replica.AvailabilityGroupReplicaData.AvailabilityMode == SMO.AvailabilityReplicaAvailabilityMode.SynchronousCommit))
                {
                    throw new AvailabilityModeIncompatibleException();
                }
            }

        }
    }
}