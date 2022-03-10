// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Validates that the folders needed for database-files of the databases 
    /// being added to the  AvailabilityGroup when creating an AvailabiltyGroup 
    /// exist on the secondary.
    /// </summary>
    public class ListenerConfigurationValidator : Validator
    {
        /// <summary>
        /// The availability group data.
        /// </summary>
        private readonly AvailabilityGroupData availabilityGroupData;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="availabilityGroupData">The availability group data</param>
        public ListenerConfigurationValidator(AvailabilityGroupData availabilityGroupData)
            : base(Resource.CheckingListenerConfiguration)
        {
            if (availabilityGroupData == null)
            {
                throw new ArgumentNullException("availabilityGroupData");
            }

            this.availabilityGroupData = availabilityGroupData;
        }

        /// <summary>
        /// Validates if a listener has been added to the AG.
        /// </summary>
        /// <param name="policy">The policy</param>
        protected override void Validate(IExecutionPolicy policy)
        {            
            // Show a Warning when listener creation is skipped in wizard
            if (this.availabilityGroupData.AvailabilityGroupListener == null)
            {
                throw new ListenerConfigurationException();
            }
        }
    }
}