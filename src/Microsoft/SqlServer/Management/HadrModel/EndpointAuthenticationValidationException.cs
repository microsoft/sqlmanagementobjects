// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    public class EndpointAuthenticationValidationException : HadrValidationErrorException
    {
        /// <summary>
        /// Standard Exception with endpointName and authenticationType
        /// </summary>
        public EndpointAuthenticationValidationException(string endpointName, string authenticationType)
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.EndpointAuthenticationValidatorException,
                endpointName))
        {

        }

        /// <summary>
        /// Exception with endpointName and authenticationType and inner exception
        /// </summary>
        public EndpointAuthenticationValidationException(string endpointName, string authenticationType, Exception inner)
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.EndpointAuthenticationValidatorException,
                endpointName, authenticationType), inner)
        {

        }  
    }
}

