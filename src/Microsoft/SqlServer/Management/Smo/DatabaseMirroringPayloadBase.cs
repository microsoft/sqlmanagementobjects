// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("DatabaseMirroring")]
    public partial class DatabaseMirroringPayload : EndpointPayload
    {
        internal DatabaseMirroringPayload(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state) :
            base(parentEndpoint, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "DatabaseMirroring";
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal override void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            switch ((ServerMirroringRole)GetPropValue("ServerMirroringRole"))
            {
                case ServerMirroringRole.All:
                    sb.Append("ROLE = ALL");
                    break;
                case ServerMirroringRole.Partner:
                    sb.Append("ROLE = PARTNER");
                    break;
                case ServerMirroringRole.Witness:
                    sb.Append("ROLE = WITNESS");
                    break;
                default:
                    throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(ServerMirroringRole).Name));
            }

            ScriptAuthenticationAndEncryption(sb, sp, true);
        }
    }
}

