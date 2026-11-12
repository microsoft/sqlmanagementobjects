// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

using System.Runtime.Serialization;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.RegisteredServers
{
    /// <summary>
    /// Types of Registered Server Exceptions
    /// </summary>
    public enum RegisteredServerExceptionType
    {
        /// Base type
        RegisteredServerException = 0,
    }

    /// <summary>
    /// Base exception class for all Registered Server exception classes
    /// </summary>
    [Serializable]
    public class RegisteredServerException : SqlServerManagementException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public RegisteredServerException ()
            : base ()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public RegisteredServerException (string message)
            : base (message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public RegisteredServerException (string message, Exception innerException)
            :
            base (message, innerException)
        {
        }
#if !NETCOREAPP && !NETSTANDARD2_0

        /// <summary>
        /// Base constructor
        /// </summary>
        protected RegisteredServerException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }
#endif
        /// <summary>
        /// Exception Type
        /// </summary>
        public virtual RegisteredServerExceptionType RegisteredServerExceptionType
        {
            get
            {
                return RegisteredServerExceptionType.RegisteredServerException;
            }
        }
    }
}
