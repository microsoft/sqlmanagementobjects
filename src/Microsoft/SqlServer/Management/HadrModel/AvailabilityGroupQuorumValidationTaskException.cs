// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

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
            : base(string.Format(CultureInfo.InvariantCulture, Resource.AvailabilityGroupQuorumValidatorException, availabilityGroupName))
        {
        }

        /// <summary>
        /// Exception with AGNAme and Inner Exception
        /// </summary>
        /// <param name="availabilityGroupName"></param>
        /// <param name="inner"></param>
        public AvailabilityGroupQuorumValidationTaskException(string availabilityGroupName, Exception inner)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.AvailabilityGroupQuorumValidatorException, availabilityGroupName), inner)
        {
        }
    }
}
