// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    using System.Globalization;

    /// <summary>
    /// Class for generating a <see cref="HadrValidationErrorException"/> related to an invalid set of configuration options 
    /// for creating or altering a BASIC Availability Group.  
    /// </summary>
    public class BasicAvailabilityGroupIncompatibleException : HadrValidationErrorException
    {
        /// <summary>
        /// Standard <see cref="HadrValidationErrorException"/> cause by an invalid set of configuration options 
        /// related to creating or altering a BASIC Availability Group.  
        /// </summary>
        public BasicAvailabilityGroupIncompatibleException(string reason)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BasicAvailabilityGroupIncompatibleException, reason))
        {
        }
    }
}
