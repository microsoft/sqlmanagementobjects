﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Base exception class for all XEvent exception classes
    /// </summary>
    [Serializable]
    public class XEventException : SqlServerManagementException
    {
        const int INIT_BUFFER_SIZE = 1024;

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XEventException()
            : base()
        {
            Init();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XEventException(string message)
            : base(message)
        {
            Init();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public XEventException(string message, Exception innerException)
            :
            base(message, innerException)
        {
            Init();
        }

        /// <summary>
        /// Base constructor
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected XEventException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Initializes instance properties
        /// </summary>
        protected virtual void Init()
        {
            Data.Add("HelpLink.ProdVer", ProdVer);
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
        internal protected XEventException SetHelpContext(string resource)
        {

            Data["HelpLink.EvtSrc"] = (resource);

            return this;
        }



        /// <summary>
        /// will output a link to the help web site
        /// <!--http://www.microsoft.com/products/ee/transform.aspx?ProdName=Microsoft%20SQL%20Server&ProdVer=09.00.0000.00&EvtSrc=MSSQLServer&EvtID=15401-->
        /// </summary>
        public override string HelpLink
        {
            get
            {
                StringBuilder link = new StringBuilder(INIT_BUFFER_SIZE);
                link.Append(Data["HelpLink.BaseHelpUrl"] as string);
                link.Append("?");
                link.AppendFormat("ProdName={0}", Data["HelpLink.ProdName"] as string);

                if (Data.Contains("HelpLink.ProdVer"))
                    link.AppendFormat("&ProdVer={0}", Data["HelpLink.ProdVer"] as string);

                if (Data.Contains("HelpLink.EvtSrc"))
                    link.AppendFormat("&EvtSrc={0}", Data["HelpLink.EvtSrc"] as string);

                if (Data.Contains("HelpLink.EvtData1"))
                {
                    link.AppendFormat("&EvtID={0}", Data["HelpLink.EvtData1"] as string);
                    for (int i = 2; i < 10; i++)
                    {
                        if (Data.Contains("HelpLink.EvtData" + i))
                        {
                            link.Append("+");
                            link.Append(Data["HelpLink.EvtData" + i] as string);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // this needs to be the last one so that it appears at the bottom of the
                // list of information displayed in the privacy confirmation dialog.
                link.AppendFormat("&LinkId={0}", Data["HelpLink.LinkId"] as string);

                return link.ToString().Replace(' ', '+');
            }
        }
    }

}
