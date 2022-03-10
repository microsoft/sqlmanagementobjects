// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Text;
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ServiceBrokerPayload : EndpointPayload
    {
        internal ServiceBrokerPayload(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state) :
            base(parentEndpoint, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "ServiceBroker";
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        internal override void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            //// This is how DDL looks like
            ////FOR SERVICE_BROKER (
            ////    [, MESSAGE_FORWARDING = ENABLED | DISABLED*]
            ////    [, MESSAGE_FORWARD_SIZE = forwardSize
            //// 
            ////    Authentication goes here 
            //// 
            ////)


            bool needsComma = false;
            Object o = null;

            o = GetPropValueOptional("IsMessageForwardingEnabled");
            if (null != o)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }
                needsComma = true;

                sb.Append("MESSAGE_FORWARDING = ");
                if ((bool)o)
                {
                    sb.Append("ENABLED");
                }
                else
                {
                    sb.Append("DISABLED");
                }
                sb.Append(Globals.newline);
            }

            o = GetPropValueOptional("MessageForwardingSize");
            if (null != o)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                needsComma = true;
                sb.AppendFormat(SmoApplication.DefaultCulture, "MESSAGE_FORWARD_SIZE = {0}", o);
                sb.Append(Globals.newline);
            }

            ScriptAuthenticationAndEncryption(sb, sp, needsComma);

        }
    }
}

