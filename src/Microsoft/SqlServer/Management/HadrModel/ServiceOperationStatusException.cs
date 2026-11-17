// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Service Operation Status Exception For CheckServiceOperationStatus operation in many validators
    /// </summary>
    public class ServiceOperationStatusException : HadrValidationBaseException
    {
        /// <summary>
        /// Standard Exception with Message
        /// </summary>
        /// <param name="message">Service Operation Details with Status</param>
        public ServiceOperationStatusException(string message)
            : base(message)
        {
        }
        /// <summary>
        /// Standard Exception with Message
        /// </summary>
        /// <param name="message">Service Operation Details with Status</param>
        /// <param name="inner">the inner Exception</param>
        public ServiceOperationStatusException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
