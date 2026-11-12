// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Base exception class for all XEvent exception classes
    /// </summary>
    [Serializable]
    public class XEventException : SqlServerManagementException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        public XEventException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public XEventException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        public XEventException(string message, Exception innerException)
            :
            base(message, innerException)
        {
        }
#if !NETCOREAPP && !NETSTANDARD2_0

        /// <summary>
        /// Base constructor
        /// </summary>
        protected XEventException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }

}
