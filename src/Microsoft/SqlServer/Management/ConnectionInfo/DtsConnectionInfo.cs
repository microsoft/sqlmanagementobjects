// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Data;
using System.Text;

#if !NETSTANDARD2_0

namespace Microsoft.SqlServer.Management.Common
{
    [Serializable]
    public class DtsConnectionInfo : SqlOlapConnectionInfoBase
    {
        protected StringBuilder applicationNameBuilder = null;
        protected StringBuilder workstationIdBuilder = null;

        // default constructor
        public DtsConnectionInfo()
            : base(ConnectionType.IntegrationServer)
        {
        }

        // special user friendly constructors
        public DtsConnectionInfo(string serverName)
        :
        base(serverName, ConnectionType.IntegrationServer)
        {
        }

        //copy ctor
        public DtsConnectionInfo(DtsConnectionInfo conn)
        : base(conn)
        {
            this.applicationNameBuilder = conn.applicationNameBuilder;
            this.workstationIdBuilder = conn.workstationIdBuilder;
        }
        
        [Browsable(false)]
        public override string ConnectionString
        {
            get
            {
                if (RebuildConnectionStringInternal)
                {
                    ConnectionStringInternal = EncryptionUtility.EncryptString(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                                                                                             "server={0};", 
                                                                                             this.ServerName));
                    RebuildConnectionStringInternal = false;
                }
                return EncryptionUtility.DecryptSecureString(ConnectionStringInternal);
            }
        }

        public string ApplicationName
        {
            get
            {
                if (null == applicationNameBuilder)
                    return String.Empty;
                else
                    return this.applicationNameBuilder.ToString();
            }

            set
            {
                if (null == this.applicationNameBuilder ||
                    0 != string.Compare(this.applicationNameBuilder.ToString(), value, StringComparison.Ordinal))
                {
                    this.applicationNameBuilder = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public string WorkstationID
        {
            get
            {
                if (null == this.workstationIdBuilder)
                    return String.Empty;
                else
                    return this.workstationIdBuilder.ToString();
            }

            set
            {
                if (null == this.workstationIdBuilder ||
                    0 != string.Compare(this.workstationIdBuilder.ToString(), value, StringComparison.Ordinal))
                {
                    this.workstationIdBuilder = new StringBuilder(value);
                    ConnectionParmsChanged();
                }
            }
        }

        public override IDbConnection CreateConnectionObject()
        {
            Type connectionType = Type.GetType("Microsoft.SqlServer.Dts.SmoEnum.DtsDbConnection, Microsoft.SqlServer.DTEnum, Version=" + AssemblyVersionInfo.VersionString + ", Culture=neutral, PublicKeyToken=89845dcd8080cc91", true);
            IDbConnection connection = (IDbConnection)Activator.CreateInstance(connectionType);
            connection.ConnectionString = ConnectionString;
            return connection;
        }
    }
}

#endif

