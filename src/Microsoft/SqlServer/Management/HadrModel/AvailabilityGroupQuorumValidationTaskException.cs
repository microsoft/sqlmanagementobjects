// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when AvailabilityGroupQuorumValidator fail
    /// </summary>
    public class AvailabilityGroupQuorumValidationTaskException : HadrTaskBaseException
    {
        /// <summary>
        /// Standard Exception with AGName
        /// </summary>
        /// <param name="availabilityGroupName"></param>
        public AvailabilityGroupQuorumValidationTaskException(string availabilityGroupName)
            : base(Resource.FormatAvailabilityGroupQuorumValidatorException(availabilityGroupName))
        {
        }

        /// <summary>
        /// Exception with AGNAme and Inner Exception
        /// </summary>
        /// <param name="availabilityGroupName"></param>
        /// <param name="inner"></param>
        public AvailabilityGroupQuorumValidationTaskException(string availabilityGroupName, Exception inner)
            : base(Resource.FormatAvailabilityGroupQuorumValidatorException(availabilityGroupName), inner)
        {
        }
    }
}
