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
            Init();
        }

        /// <summary>
        /// Constructs a new SqlServerManagementException with the given message and no inner exception
        /// </summary>
        /// <param name="message"></param>
        public SqlServerManagementException(string message)
            : base(message)
        {
            Init();
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
            Init();
        }

        protected SqlServerManagementException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        private void Init()
        {
            Data.Add("HelpLink.ProdName", ProductName);
            Data.Add("HelpLink.BaseHelpUrl", "https://go.microsoft.com/fwlink");
            Data.Add("HelpLink.LinkId", "20476");
        }

        /// <summary>
        /// ProductName specifies the ProdName value used in the HelpLink property of SqlServerManagementException instances.
        /// </summary>
        public static string ProductName
        {
            get
            {
                return "Microsoft SQL Server";
            }
        }
    }
}
