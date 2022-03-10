// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Microsoft.SqlServer.Management.Common
{
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class SqlServerManagementException : Exception
    {
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SqlServerManagementException()
        {
            Init();
        }

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public SqlServerManagementException(string message)
            : base(message)
        {
            Init();
        }

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

                //Whidbey 3.2: base Exception class already has property with the same name for the same purpose
                /*
        private SortedList data = new SortedList();

        public IDictionary Data
        {
            get { return data; }
        }
        } */

        public static string ProductName
        {
            get
            {
                return "Microsoft SQL Server";
            }
        }
    }
}
