// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.SqlScriptPublish
{
    /// <summary>
    /// Exception that is raised when script generation fails
    /// </summary>
    public class SqlScriptPublishException : Exception
    {
        /// <summary>
        /// Constructs a new SqlScriptPublishException with the given message
        /// </summary>
        /// <param name="message"/>
        public SqlScriptPublishException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a new SqlScriptPublishException with the given message and inner exception
        /// </summary>
        /// <param name="message" />
        /// <param name="innerException" />
        public SqlScriptPublishException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
