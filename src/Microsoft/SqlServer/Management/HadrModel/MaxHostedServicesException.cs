// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This excpetion will be thrown if we reach the max of Hosted Service Count
    /// </summary>
    public class MaxHostedServicesException : HadrValidationBaseException
    {
        /// <summary>
        /// Standard Exception with hosted Service Name and Current Hosted Service Count.
        /// </summary>
        /// <param name="hostedServiceName">Current Hosted Service Name</param>
        /// <param name="currentHostedService">Current Hosted Service Count</param>
        public MaxHostedServicesException(string hostedServiceName,string currentHostedService)
            : base(Resource.FormatMaxHostedServicesException(hostedServiceName, currentHostedService))
        {
        }

        /// <summary>
        /// Exception with hosted Service Name and Current Hosted Service Count and inner exception.
        /// </summary>
        /// <param name="hostedServiceName">Current Hosted Service Name</param>
        /// <param name="currentHostedService">Current Hosted Service Count</param>
        /// <param name="inner"></param>
        public MaxHostedServicesException(string hostedServiceName, string currentHostedService, Exception inner)
            : base(Resource.FormatMaxHostedServicesException(hostedServiceName, currentHostedService),inner)
        {
        }
    }
}
