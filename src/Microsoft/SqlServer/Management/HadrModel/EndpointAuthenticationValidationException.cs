// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    public class EndpointAuthenticationValidationException : HadrValidationErrorException
    {
        /// <summary>
        /// Standard Exception with endpointName and authenticationType
        /// </summary>
        public EndpointAuthenticationValidationException(string endpointName, string authenticationType)
            : base(Resource.FormatEndpointAuthenticationValidatorException(endpointName, authenticationType))
        {

        }

        /// <summary>
        /// Exception with endpointName and authenticationType and inner exception
        /// </summary>
        public EndpointAuthenticationValidationException(string endpointName, string authenticationType, Exception inner)
            : base(Resource.FormatEndpointAuthenticationValidatorException(endpointName, authenticationType), inner)
        {

        }  
    }
}

