// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when there is no Listener defined for the AG.
    /// </summary>
    public class ListenerConfigurationException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ListenerConfigurationException()
            : base(Resource.ListenerConfigurationWarning)
        {
        }
    }
}