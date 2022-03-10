// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Tcp")]
    public partial class TcpProtocol : EndpointProtocol
    {
        internal TcpProtocol(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state) :
            base(parentEndpoint, key, state)
        {
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Tcp";
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            object o = GetPropValue("ListenerPort");
            sb.AppendFormat(SmoApplication.DefaultCulture, "LISTENER_PORT = {0}", o);

            o = GetPropValueOptional("ListenerIPAddress");
            if (null != o)
            {
                sb.Append(Globals.commaspace);

                System.Net.IPAddress ip = this.ListenerIPAddress;

                if (System.Net.IPAddress.Any == ip)
                {
                    sb.Append("LISTENER_IP = ALL");
                }
                else
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, "LISTENER_IP = ({0})", ip.ToString());
                }
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public System.Net.IPAddress ListenerIPAddress
        {
            get
            {
                System.Net.IPAddress ip = null;
                object o = this.Properties.GetValueWithNullReplacement("ListenerIPAddress");

                if (null != o)
                {
                    ip = o as System.Net.IPAddress;

                    if (null != ip)
                    {
                        return ip;
                    }

                    string s = o as string;
                    if (null != s && s.Length > 0)
                    {
                        ip = System.Net.IPAddress.Parse(s);
                    }
                }
                if (null == ip)
                {
                    ip = System.Net.IPAddress.Any;
                }
                this.ListenerIPAddress = ip;
                return ip;
            }
            set
            {
                Properties.SetValue(Properties.LookupID("ListenerIPAddress", PropertyAccessPurpose.Read), value);
                Properties.SetDirty(Properties.LookupID("ListenerIPAddress", PropertyAccessPurpose.Read), true);
            }
        }
    }
}


