// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
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
        const int INIT_BUFFER_SIZE = 1024;

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage ("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RegisteredServerException ()
            : base ()
        {
            Init ();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage ("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RegisteredServerException (string message)
            : base (message)
        {
            Init ();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage ("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public RegisteredServerException (string message, Exception innerException)
            :
            base (message, innerException)
        {
            Init ();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage ("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected RegisteredServerException(SerializationInfo info, StreamingContext context)
            : base (info, context)
        {
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected virtual void Init ()
        {
            Data["HelpLink.ProdVer"] = ProdVer;
        }

        private static readonly string prodVer = ((AssemblyFileVersionAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true)[0]).Version;

        /// <summary>
        /// Product Version
        /// </summary>
        protected static string ProdVer
        {
            get
            {
                return prodVer;
            }
        }

        /// <summary>
        /// Sets Help Context
        /// </summary>
        /// <param name="resource"></param>
        /// <returns></returns>
        internal protected RegisteredServerException SetHelpContext(string resource)
        {

            Data["HelpLink.EvtSrc"] = (resource);

            return this;
        }

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

       
        /// <summary>
        /// will output a link to the help web site
        /// <!--http://www.microsoft.com/products/ee/transform.aspx?ProdName=Microsoft%20SQL%20Server&ProdVer=09.00.0000.00&EvtSrc=MSSQLServer&EvtID=15401-->
        /// </summary>
        public override string HelpLink
        {
            get
            {
                StringBuilder link = new StringBuilder (INIT_BUFFER_SIZE);
                link.Append (Data["HelpLink.BaseHelpUrl"] as string);
                link.Append ("?");
                link.AppendFormat ("ProdName={0}", Data["HelpLink.ProdName"] as string);

                if (Data.Contains ("HelpLink.ProdVer"))
                    link.AppendFormat ("&ProdVer={0}", Data["HelpLink.ProdVer"] as string);

                if (Data.Contains ("HelpLink.EvtSrc"))
                    link.AppendFormat ("&EvtSrc={0}", Data["HelpLink.EvtSrc"] as string);

                if (Data.Contains ("HelpLink.EvtData1"))
                {
                    link.AppendFormat ("&EvtID={0}", Data["HelpLink.EvtData1"] as string);
                    for (int i = 2; i < 10; i++)
                    {
                        if (Data.Contains ("HelpLink.EvtData" + i))
                        {
                            link.Append ("+");
                            link.Append (Data["HelpLink.EvtData" + i] as string);
                        }
                        else
                            break;
                    }
                }

                // this needs to be the last one so that it appears at the bottom of the
                // list of information displayed in the privacy confirmation dialog.
                link.AppendFormat ("&LinkId={0}", Data["HelpLink.LinkId"] as string);

                return link.ToString ().Replace (' ', '+');
            }
        }
    }


    /// <summary>
    /// Exception thrown when upgrading from SqlServer2005
    /// </summary>
    [Serializable]
    // VBUMP Remove this in V17
    public class InvalidSqlServer2005StoreFormatException : RegisteredServerException
    {
        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public InvalidSqlServer2005StoreFormatException()
            : base()
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public InvalidSqlServer2005StoreFormatException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public InvalidSqlServer2005StoreFormatException(string message, Exception innerException)
            :
            base(message, innerException)
        {
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected InvalidSqlServer2005StoreFormatException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
   }
}

