// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Microsoft.SqlServer.Management.Common
{

    [System.Runtime.InteropServices.ComVisible(false)]
    [Serializable]
    public abstract class ConnectionInfoBase 
    {
        ServerVersion m_sv;
        ServerCaseSensitivity m_scs;

        private ConnectionType m_eServerType;

        // default constructor cannot be used
        protected ConnectionInfoBase()
        {
            throw new InvalidOperationException(StringConnectionInfo.ClassDefaulConstructorCannotBeUsed("ConnectionInfoBase"));
        }

        // need to know connection type
        protected ConnectionInfoBase(ConnectionType serverType)
        {
            m_eServerType = serverType;
            m_sv  = null;
            m_scs = ServerCaseSensitivity.Unknown;
        }

        //copy ctor
        protected ConnectionInfoBase(ConnectionInfoBase conn)
        {
            m_eServerType = conn.m_eServerType;
            m_sv  = conn.m_sv;
            m_scs = conn.ServerCaseSensitivity;
        }

        // connection type read only, set by the derived class
        public ConnectionType ServerType
        {
            get{ return m_eServerType; }
        }
        public ServerVersion ServerVersion
        {
            get { return m_sv; }
            set { m_sv = value; }
        }

        public ServerCaseSensitivity ServerCaseSensitivity
        {
            get { return m_scs; }
            set { m_scs = value; }
        }

        protected abstract void ConnectionParmsChanged();

        // overrides
        public override string ToString()
        {
            StringBuilder sbText = new StringBuilder();
            sbText.AppendFormat("server type = {0}", ServerType);
            return sbText.ToString();
        }

        readonly private static CultureInfo defaultCulture = CultureInfo.InvariantCulture;
        internal static CultureInfo DefaultCulture 
        {
            get
            {
                return defaultCulture;
            }
        }
    }
}
