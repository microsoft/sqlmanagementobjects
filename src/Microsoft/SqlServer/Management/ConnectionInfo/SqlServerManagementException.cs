// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// SqlServerManagementException is the base class for all SQL Management Objects exceptions. 
    /// </summary>
    public class SqlServerManagementException : Exception
    {
        /// <summary>
        /// Constructs a new SqlServerManagementException with an empty message and no inner exception
        /// </summary>
        public SqlServerManagementException()
        {
        }

        /// <summary>
        /// Constructs a new SqlServerManagementException with the given message and no inner exception
        /// </summary>
        /// <param name="message"></param>
        public SqlServerManagementException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Constructs a new SqlServerManagementException with the given message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public SqlServerManagementException(string message, Exception innerException)
            :
            base(message, innerException)
        {
        }

#if !NETCOREAPP && !NETSTANDARD2_0 // This overload is obsolete and will be removed in a future version of .NET
        protected SqlServerManagementException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        /// <summary>
        /// The fwlink LinkId used to build <see cref="HelpLink"/>. Override in derived
        /// exceptions to target a feature-area-specific help page.
        /// </summary>
        protected virtual string HelpLinkId => "20476";

        /// <summary>
        /// Gets a help link for the exception.
        /// </summary>
        public override string HelpLink =>
            // LinkId is last so that it appears at the bottom of the information
            // displayed in the privacy confirmation dialog.
            $"https://go.microsoft.com/fwlink?LinkId={HelpLinkId}";
    }
}
