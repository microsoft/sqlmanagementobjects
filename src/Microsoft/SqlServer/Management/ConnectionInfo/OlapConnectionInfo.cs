// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.ComponentModel;
    using System.Data;
    using System.Data.OleDb;
    using System.Text;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(false)]
    public class OlapConnectionInfo : SqlOlapConnectionInfoBase
    {
        private bool shouldEncryptConnection = false;
        private string applicationName = null;
        private string otherParams = null;
        private string integratedSecurity = null;

        // default constructor
        public OlapConnectionInfo() : base(ConnectionType.Olap)
        {
        }

        //copy ctor
        public OlapConnectionInfo(OlapConnectionInfo conn) : base((SqlOlapConnectionInfoBase)conn)
        {
            this.shouldEncryptConnection = conn.shouldEncryptConnection;
            this.applicationName = conn.applicationName;
        }


        /// <summary>
        /// whether connection should be opened with encryption
        /// </summary>
        /// <value></value>
        public bool EncryptConnection
        {
            get
            {
                return this.shouldEncryptConnection;
            }

            set
            {
                this.shouldEncryptConnection = value;
            }
        }

        public string ApplicationName
        {
            get
            {
                return this.applicationName;
            }

            set
            {
                this.applicationName = value;
            }
        }

        public string IntegratedSecurity
        {
            get
            {
                return this.integratedSecurity;
            }
            set
            {
                this.integratedSecurity = value;
            }
        }

        public string OtherParameters
        {
            get
            {
                return this.otherParams;
            }

            set
            {
                this.otherParams = value;
            }
        }

        // overrides
        [Browsable(false)]
        public override string ConnectionString
        {
            get
            {
                if ( RebuildConnectionStringInternal )
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("Provider=MSOLAP;Data Source={0}", ServerName);

                    // database name
                    if (null != DatabaseNameInternal && DatabaseNameInternal.Length > 0)
                    {
                        sb.AppendFormat(";Initial Catalog={0}", DatabaseNameInternal);
                    }

                    // connection timeout
                    if (ConnectionTimeout != NoTimeOut)
                    {
                        sb.AppendFormat(";Connect Timeout={0}", ConnectionTimeout);
                    }

                    // query timeout
                    if (QueryTimeout != NoTimeOut)
                    {
                        sb.AppendFormat(";Timeout={0}", QueryTimeout);
                    }

                    if (!string.IsNullOrEmpty(UserName))
                    {
                        sb.AppendFormat(";User ID={0}", UserName);
                    }

                    if (!string.IsNullOrEmpty(Password))
                    {
                        sb.AppendFormat(";Password='{0}'", Password);
                    }

                    if (this.shouldEncryptConnection)
                    {
                        sb.AppendFormat(";Use Encryption for Data=true");
                    }

                    if (!string.IsNullOrEmpty(this.applicationName))
                    {
                        sb.AppendFormat(";Application Name={0}", this.applicationName);
                    }

                    if (!string.IsNullOrEmpty(this.integratedSecurity))
                    {
                        sb.AppendFormat(";Integrated Security={0}", this.integratedSecurity);
                    }

                    if (!string.IsNullOrEmpty(this.otherParams))
                    {
                        sb.AppendFormat(";{0}", this.otherParams);
                    }

                    ConnectionStringInternal = EncryptionUtility.EncryptString(sb.ToString());
                    sb = null;
                    RebuildConnectionStringInternal = false;
                }
                return EncryptionUtility.DecryptSecureString(ConnectionStringInternal);
            }
        }

        /// <summary>
        /// Deep copy
        /// </summary>
        /// <returns></returns>
        public OlapConnectionInfo Copy()
        {
            return new OlapConnectionInfo(this);
        }

        public override IDbConnection CreateConnectionObject()
        {
            return(new OleDbConnection(ConnectionString));
        }
    }
}