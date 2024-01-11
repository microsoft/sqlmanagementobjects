// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// SqlServerManagementException is the base class for all SQL Management Objects exceptions. 
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    // VBUMP: For V17, make this class abstract or the constructors all protected
    public class SqlServerManagementException : Exception
    {
        /// <summary>
        /// Constructs a new SqlServerManagementException with an empty message and no inner exception
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SqlServerManagementException()
        {
            Init();
        }

        /// <summary>
        /// Constructs a new SqlServerManagementException with the given message and no inner exception
        /// </summary>
        /// <param name="message"></param>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
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
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
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
