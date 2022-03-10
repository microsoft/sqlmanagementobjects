// Copyright (c) Microsoft.
using System;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Soap")]
    public partial class SoapPayload : EndpointPayload
    {
        internal SoapPayload(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state) :
            base(parentEndpoint, key, state)
        {
        }


        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Soap";
            }
        }

        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(SmoApplication.DefaultCulture, "/{0}", UrnSuffix);
        }

        SoapPayloadMethodCollection m_soapPayloadMethodCollection = null;
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(SoapPayloadMethod))]
        public SoapPayloadMethodCollection SoapPayloadMethods
        {
            get
            {
                if (m_soapPayloadMethodCollection == null)
                {
                    m_soapPayloadMethodCollection = new SoapPayloadMethodCollection(this);
                }

                return m_soapPayloadMethodCollection;
            }
        }

        protected override void MarkDropped()
        {
            // mark the object itself as dropped 
            base.MarkDropped();
            if (null != m_soapPayloadMethodCollection)
            {
                m_soapPayloadMethodCollection.MarkAllDropped();
            }
        }

        internal override void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            bool needsComma = false;

            if (this.SoapPayloadMethods.Count > 0)
            {
                foreach (SoapPayloadMethod method in this.SoapPayloadMethods)
                {
                    if (!method.IgnoreForScripting)
                    {
                        if (needsComma)
                        {
                            sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                        }

                        needsComma = true;
                        sb.Append(Globals.newline);
                        method.Script(sb, sp);
                    }
                }
            }

            object batches = GetPropValueOptional("IsSqlBatchesEnabled");
            if (null != batches)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                needsComma = true;
                sb.AppendFormat(SmoApplication.DefaultCulture, "BATCHES={0}",
                                (bool)batches ? "ENABLED" : "DISABLED");
            }

            object wsdl = GetPropValueOptional("WsdlGeneratorOption");
            if (null != wsdl)
            {
                string wsdlName = string.Empty;
                switch ((WsdlGeneratorOption)wsdl)
                {
                    case WsdlGeneratorOption.None: wsdlName = "NONE"; break;
                    case WsdlGeneratorOption.DefaultProcedure: wsdlName = "DEFAULT"; break;
                    case WsdlGeneratorOption.Procedure:
                        wsdlName = MakeSqlString(GetPropValue("WsdlGeneratorProcedure") as string);
                        break;

                    default:
                        throw new WrongPropertyValueException(this.Properties.Get("WsdlGeneratorOption"));
                }

                if (wsdlName.Length > 0)
                {
                    if (needsComma)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }

                    needsComma = true;
                    sb.AppendFormat(SmoApplication.DefaultCulture, "WSDL={0}", wsdlName);
                }
            }

            object sessions = GetPropValueOptional("IsSessionEnabled");

            if (null != sessions)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                needsComma = true;
                sb.AppendFormat(SmoApplication.DefaultCulture, "SESSIONS={0}",
                                (bool)sessions ? "ENABLED" : "DISABLED");
            }

            if (true == (bool)GetPropValueOptional("SessionNeverTimesOut", false))
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                sb.Append("SESSION_TIMEOUT=NEVER");
            }
            else
            {
                object sessionsTimeout = GetPropValueOptional("SessionTimeout");
                if (null != sessionsTimeout)
                {
                    if (needsComma)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }

                    needsComma = true;
                    if ((Int32)sessionsTimeout < 0)
                    {
                        sb.Append("SESSION_TIMEOUT=NEVER");
                    }
                    else
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, "SESSION_TIMEOUT={0}",
                            (Int32)sessionsTimeout);
                    }
                }
            }

            object database = GetPropValueOptional("DefaultDatabase");
            if (null != database)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                needsComma = true;
                sb.AppendFormat(SmoApplication.DefaultCulture, "DATABASE={0}",
                                ((string)database).Length > 0 ? MakeSqlString((string)database) : "DEFAULT");
            }

            object defNamespace = GetPropValueOptional("DefaultNamespace");
            if (null != defNamespace)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                needsComma = true;
                sb.AppendFormat(SmoApplication.DefaultCulture, "NAMESPACE={0}",
                                ((string)defNamespace).Length > 0 ? MakeSqlString((string)defNamespace) : "DEFAULT");
            }

            object xsd = GetPropValueOptional("XsdSchemaOption");
            if (null != xsd)
            {
                string xsdName = string.Empty;
                switch ((XsdSchemaOption)xsd)
                {
                    case XsdSchemaOption.None: xsdName = "NONE"; break;
                    case XsdSchemaOption.Standard: xsdName = "STANDARD"; break;
                }

                if (xsdName.Length > 0)
                {
                    if (needsComma)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }

                    needsComma = true;
                    sb.AppendFormat(SmoApplication.DefaultCulture, "SCHEMA={0}", xsdName);
                }
            }

            object xmlformat = GetPropValueOptional("XmlFormatOption");
            if (null != xmlformat)
            {
                string xmlformatName = string.Empty;
                switch ((XmlFormatOption)xmlformat)
                {
                    case XmlFormatOption.XmlFormat: xmlformatName = "XML"; break;
                    case XmlFormatOption.SqlFormat: xmlformatName = "SQL"; break;
                }

                if (xmlformatName.Length > 0)
                {
                    if (needsComma)
                    {
                        sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                    }

                    needsComma = true;
                    sb.AppendFormat(SmoApplication.DefaultCulture, "CHARACTER_SET={0}", xmlformatName);
                }
            }

        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] { new PropagateInfo(SoapPayloadMethods, false) };
        }

    }
}

