// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when one of the secondary replicas
    /// has an availabilitymode of Synchronous Commit, when the primary's
    /// availabilitymode is not Synchronous.
    /// </summary>
    public class AvailabilityModeIncompatibleException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public AvailabilityModeIncompatibleException()
            : base(Resource.AvailabilityModeCompatibilityWarning)
        {
        }
    }
}
