// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Http")]
    public partial class HttpProtocol : EndpointProtocol
    {
        internal HttpProtocol(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state) :
            base(parentEndpoint, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Http";
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            sb.AppendFormat(SmoApplication.DefaultCulture, "PATH=N'{0}'", SqlString((string)GetPropValue("WebSiteUrlPath")));

            bool needsComma = false;
            HttpPortTypes ports = (HttpPortTypes)GetPropValue("HttpPortTypes");

            sb.Append(", PORTS = (");

            bool bGoodVal = false;
            if (0 != (HttpPortTypes.Ssl & ports))
            {
                bGoodVal = true;
                needsComma = true;
                sb.Append("SSL");
            }

            if (0 != (HttpPortTypes.Clear & ports))
            {
                bGoodVal = true;
                if (needsComma)
                {
                    sb.Append(Globals.commaspace);
                }

                sb.Append("CLEAR");
            }
            if (!bGoodVal)
            {
                throw new WrongPropertyValueException(this.Properties.Get("HttpPortTypes"));
            }
            sb.Append(Globals.RParen);	// close the port settings

            HttpAuthenticationModes auth = (HttpAuthenticationModes)GetPropValue("HttpAuthenticationModes");
            sb.Append(", AUTHENTICATION = (");
            needsComma = false;
            if (0 != (HttpAuthenticationModes.Anonymous & auth))
            {
                needsComma = true;
                sb.Append("ANONYMOUS");
            }

            if (0 != (HttpAuthenticationModes.Basic & auth))
            {
                if (needsComma)
                {
                    sb.Append(Globals.commaspace);
                }

                needsComma = true;
                sb.Append("BASIC");
            }

            if (0 != (HttpAuthenticationModes.Digest & auth))
            {
                if (needsComma)
                {
                    sb.Append(Globals.commaspace);
                }

                needsComma = true;
                sb.Append("DIGEST");
            }

            if (0 != (HttpAuthenticationModes.Ntlm & auth))
            {
                if (needsComma)
                {
                    sb.Append(Globals.commaspace);
                }

                needsComma = true;
                sb.Append("NTLM");
            }

            if (0 != (HttpAuthenticationModes.Kerberos & auth))
            {
                if (needsComma)
                {
                    sb.Append(Globals.commaspace);
                }

                needsComma = true;
                sb.Append("KERBEROS");
            }

            if (0 != (HttpAuthenticationModes.Integrated & auth))
            {
                if (needsComma)
                {
                    sb.Append(Globals.commaspace);
                }

                needsComma = true;
                sb.Append("INTEGRATED");
            }
            sb.Append(Globals.RParen);	// close the authentication settings

            object site = GetPropValueOptional("WebSite");
            if (null != site)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, ", SITE=N'{0}'", SqlString((string)site));
            }

            if (0 != (HttpPortTypes.Clear & ports))
            {
                object clearPort = GetPropValueOptional("ClearPort");
                if (null != clearPort && (!sp.ScriptForAlter || Properties.Get("ClearPort").Dirty))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", CLEAR_PORT = {0}", (Int32)clearPort);
                }
            }

            if (0 != (HttpPortTypes.Ssl & ports))
            {
                object sslPort = GetPropValueOptional("SslPort");
                if (null != sslPort && (!sp.ScriptForAlter || Properties.Get("SslPort").Dirty))
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", SSL_PORT = {0}", (Int32)sslPort);
                }
            }

            if (0 != (HttpAuthenticationModes.Digest & auth))
            {
                string propAuthRealm = GetPropValueOptional("AuthenticationRealm") as string;
                if (null != propAuthRealm && propAuthRealm.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", AUTH_REALM=N'{0}'", SqlString(propAuthRealm));
                }
            }

            if (0 != (HttpAuthenticationModes.Basic & auth))
            {
                string propDefDomain = GetPropValueOptional("DefaultLogonDomain") as string;
                if (null != propDefDomain && propDefDomain.Length > 0)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, ", DEFAULT_LOGON_DOMAIN=N'{0}'", SqlString(propDefDomain));
                }
            }

            object compression = GetPropValueOptional("IsCompressionEnabled");
            if (null != compression)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, ", COMPRESSION={0}",
                                (bool)compression ? "ENABLED" : "DISABLED");
            }
        }
    }
}


