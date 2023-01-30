// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    #region payload_stub
    public class Payload
    {
        Endpoint m_endpoint = null;

        internal Payload(Endpoint endpoint)
        {
            m_endpoint = endpoint;
        }

        private SoapPayload m_soapPayload = null;
        public SoapPayload Soap
        {
            get
            {
                if (null == m_soapPayload)
                {
                    m_soapPayload = new SoapPayload(m_endpoint, new ObjectKeyBase(), GetStateForType(EndpointType.Soap));
                }

                return m_soapPayload;
            }
        }

        private ServiceBrokerPayload m_serviceBrokerPayload = null;
        public ServiceBrokerPayload ServiceBroker
        {
            get
            {
                if (null == m_serviceBrokerPayload)
                {
                    m_serviceBrokerPayload = new ServiceBrokerPayload(m_endpoint, new ObjectKeyBase(), GetStateForType(EndpointType.ServiceBroker));
                }

                return m_serviceBrokerPayload;
            }
        }

        private DatabaseMirroringPayload m_databaseMirroringPayload = null;
        public DatabaseMirroringPayload DatabaseMirroring
        {
            get
            {
                if (null == m_databaseMirroringPayload)
                {
                    m_databaseMirroringPayload = new DatabaseMirroringPayload(m_endpoint, new ObjectKeyBase(), GetStateForType(EndpointType.DatabaseMirroring));
                }

                return m_databaseMirroringPayload;
            }
        }

        SqlSmoState GetStateForType(EndpointType et)
        {
            //init the property
            Object o = m_endpoint.GetPropValueOptional("EndpointType");
            //get original value
            Property p = m_endpoint.Properties.Get("EndpointType");
            o = p.Dirty ? m_endpoint.oldEndpointTypeValue : p.Value;

            if (null == o)
            {
                return SqlSmoState.Creating;
            }
            if ((EndpointType)o == et)
            {
                return m_endpoint.State;
            }
            return SqlSmoState.Creating;
        }

        internal EndpointPayload EndpointPayload
        {
            get
            {
                switch ((EndpointType)m_endpoint.GetPropValue("EndpointType"))
                {
                    case EndpointType.Soap:
                        return this.Soap;

                    case EndpointType.ServiceBroker:
                        return this.ServiceBroker;

                    case EndpointType.DatabaseMirroring:
                        return this.DatabaseMirroring;

                    case EndpointType.TSql:
                        return null;

                    default:
                        throw new WrongPropertyValueException(m_endpoint.Properties.Get("EndpointType"));
                }
            }
        }

        internal void MarkDropped()
        {
            if (null != this.Soap)
            {
                this.Soap.MarkDroppedInternal();
            }

            if (null != this.ServiceBroker)
            {
                this.ServiceBroker.MarkDroppedInternal();
            }

            if (null != this.DatabaseMirroring)
            {
                this.DatabaseMirroring.MarkDroppedInternal();
            }
        }

        string PayloadDdlName
        {
            get
            {
                switch ((EndpointType)m_endpoint.GetPropValue("EndpointType"))
                {
                    case EndpointType.Soap:
                        return "SOAP";

                    case EndpointType.ServiceBroker:
                        return "SERVICE_BROKER";

                    case EndpointType.DatabaseMirroring:
                        return "DATA_MIRRORING";

                    case EndpointType.TSql:
                        return "TSQL";

                    default:
                        throw new WrongPropertyValueException(m_endpoint.Properties.Get("EndpointType"));
                }
            }
        }

        internal void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            sb.Append(this.PayloadDdlName);
            EndpointPayload payloadBase = this.EndpointPayload;
            sb.Append(" (");
            if (null != payloadBase)
            {
                payloadBase.Script(sb, sp);
            }
            sb.Append(")");
        }
    }
    #endregion

    #region protocol_stub
    public class Protocol
    {
        Endpoint m_endpoint = null;

        internal Protocol(Endpoint endpoint)
        {
            m_endpoint = endpoint;
        }

        private HttpProtocol m_httpProtocol = null;
        public HttpProtocol Http
        {
            get
            {
                if (null == m_httpProtocol)
                {
                    m_httpProtocol = new HttpProtocol(m_endpoint, null, GetStateForType(ProtocolType.Http));
                }

                return m_httpProtocol;
            }
        }

        private TcpProtocol m_tcpProtocol = null;
        public TcpProtocol Tcp
        {
            get
            {
                if (null == m_tcpProtocol)
                {
                    m_tcpProtocol = new TcpProtocol(m_endpoint, null, GetStateForType(ProtocolType.Tcp));
                }

                return m_tcpProtocol;
            }
        }

        SqlSmoState GetStateForType(ProtocolType et)
        {
            //init the property
            Object o = m_endpoint.GetPropValueOptional("ProtocolType");
            //get original value
            Property p = m_endpoint.Properties.Get("ProtocolType");
            o = p.Dirty ? m_endpoint.oldEndpointTypeValue : p.Value;

            if (null == o)
            {
                return SqlSmoState.Creating;
            }
            if ((ProtocolType)o == et)
            {
                return m_endpoint.State;
            }
            return SqlSmoState.Creating;
        }

        internal EndpointProtocol EndpointProtocol
        {
            get
            {
                switch ((ProtocolType)m_endpoint.GetPropValue("ProtocolType"))
                {
                    case ProtocolType.Http: return this.Http;

                    case ProtocolType.Tcp: return this.Tcp;

                    case ProtocolType.NamedPipes: return null;

                    case ProtocolType.SharedMemory: return null;

                    case ProtocolType.Via: return null;

                    default:
                        throw new WrongPropertyValueException(m_endpoint.Properties.Get("ProtocolType"));
                }
            }
        }

        internal void MarkDropped()
        {
            if (null != this.Http)
            {
                this.Http.MarkDroppedInternal();
            }

            if (null != this.Tcp)
            {
                this.Http.MarkDroppedInternal();
            }
        }

        string ProtocolDdlName
        {
            get
            {
                switch ((ProtocolType)m_endpoint.GetPropValue("ProtocolType"))
                {
                    case ProtocolType.Http: return "HTTP";

                    case ProtocolType.Tcp: return "TCP";

                    case ProtocolType.NamedPipes: return "NAMEDPIPES";

                    case ProtocolType.SharedMemory: return "SHAREDMEMORY";

                    case ProtocolType.Via: return "Via";

                    default:
                        throw new WrongPropertyValueException(m_endpoint.Properties.Get("ProtocolType"));
                }
            }
        }

        internal void Script(StringBuilder sb, ScriptingPreferences sp)
        {
            EndpointProtocol protocol = this.EndpointProtocol;
            if (null != protocol)
            {
                sb.Append(this.ProtocolDdlName);
                sb.Append(" (");
                protocol.Script(sb, sp);
                sb.Append(")");
            }
            else
            {
                throw new InvalidSmoOperationException(ExceptionTemplates.IncorrectEndpointProtocol);
            }
        }
    }
    #endregion

    #region EndpointProtocol
    public abstract class EndpointProtocol : SqlSmoObject
    {
        internal EndpointProtocol(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state)
            : base(key, state)        
        {
            singletonParent = parentEndpoint as Endpoint;

            // WATCH OUT! we are setting the m_server value here
            //eithewise GetServerObject() wouldn't work becouse we are not under a collection
            SetServerObject(parentEndpoint.GetServerObject());
            m_comparer = ((Endpoint)singletonParent).Parent.Databases["master"].StringComparer;
        }


        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Endpoint Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Endpoint;
            }
        }

        public override string ToString()
        {
            return Parent.ToString();
        }

        abstract internal void Script(StringBuilder sb, ScriptingPreferences sp);
    }
    #endregion

    #region EndpointPayload
    public abstract class EndpointPayload : SqlSmoObject
    {
        internal EndpointPayload(Endpoint parentEndpoint, ObjectKeyBase key, SqlSmoState state) : base(key, state)
        {
            singletonParent = parentEndpoint as Endpoint;

            // WATCH OUT! we are setting the m_server value here
            //eithewise GetServerObject() wouldn't work becouse we are not under a collection
            SetServerObject(parentEndpoint.GetServerObject());
        }


        [SfcObject(SfcObjectRelationship.ParentObject)]
        public Endpoint Parent
        {
            get
            {
                CheckObjectState();
                return singletonParent as Endpoint;
            }
        }

        public override string ToString()
        {
            return Parent.ToString();
        }

        abstract internal void Script(StringBuilder sb, ScriptingPreferences sp);

        /// <summary>
        /// Generates a script for the AUTHENTICATION and ENCRYPTION clauses
        /// in the payload DDL. this is shared by ServiceBroker and DatabaseMirroring payloads
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="so"></param>
        /// <param name="needsComma"></param>
        internal void ScriptAuthenticationAndEncryption(StringBuilder sb, ScriptingPreferences sp, bool needsComma)
        {
            ////    [ AUTHENTICATION =  {  WINDOWS*[ { NTLM | KERBEROS | NEGOTIATE* } ] |  
            ////        CERTIFICATE CertificateName |
            ////WINDOWS[ { NTLM|KERBEROS|NEGOTIATE* } ] CERTIFICATE CertificateName |
            ////CERTIFICATE CertificateName WINDOWS[ { NTLM|KERBEROS|NEGOTIATE* } ]}]
            ////    [, ENCRYPTION = { DISABLED  | SUPPORTED | REQUIRED* } [ ALGORITHM  {RC4* | AES | AES RC4 | RC4 AES } ]  ]

            Object o = GetPropValueOptional("EndpointAuthenticationOrder");
            if (null != o)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }

                needsComma = true;

                sb.Append("AUTHENTICATION = ");
                string certificate = string.Empty;
                switch ((EndpointAuthenticationOrder)o)
                {
                    case EndpointAuthenticationOrder.Ntlm:
                        sb.Append("WINDOWS NTLM");
                        break;
                    case EndpointAuthenticationOrder.Kerberos:
                        sb.Append("WINDOWS KERBEROS");
                        break;
                    case EndpointAuthenticationOrder.Negotiate:
                        sb.Append("WINDOWS NEGOTIATE");
                        break;
                    case EndpointAuthenticationOrder.Certificate:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "CERTIFICATE {0}", MakeSqlBraket(certificate));
                        break;
                    case EndpointAuthenticationOrder.CertificateNtlm:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "CERTIFICATE {0} WINDOWS NTLM", MakeSqlBraket(certificate));
                        break;
                    case EndpointAuthenticationOrder.CertificateKerberos:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "CERTIFICATE {0} WINDOWS KERBEROS", MakeSqlBraket(certificate));
                        break;
                    case EndpointAuthenticationOrder.CertificateNegotiate:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "CERTIFICATE {0} WINDOWS NEGOTIATE", MakeSqlBraket(certificate));
                        break;
                    case EndpointAuthenticationOrder.NtlmCertificate:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "WINDOWS NTLM CERTIFICATE {0} ", MakeSqlBraket(certificate));
                        break;
                    case EndpointAuthenticationOrder.KerberosCertificate:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "WINDOWS KERBEROS CERTIFICATE {0} ", MakeSqlBraket(certificate));
                        break;
                    case EndpointAuthenticationOrder.NegotiateCertificate:
                        certificate = (string)GetPropValue("Certificate");
                        if (certificate.Length == 0)
                        {
                            throw new PropertyNotSetException("Certificate");
                        }

                        sb.AppendFormat(SmoApplication.DefaultCulture,
                                "WINDOWS NEGOTIATE CERTIFICATE {0} ", MakeSqlBraket(certificate));
                        break;
                    default:
                        throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(EndpointAuthenticationOrder).Name));
                }
                sb.Append(Globals.newline);
            }

            Object oAlg = GetPropValueOptional("EndpointEncryptionAlgorithm");
            Object oEnc = GetPropValueOptional("EndpointEncryption");
            if (null != oAlg || null != oEnc)
            {
                if (needsComma)
                {
                    sb.AppendFormat(SmoApplication.DefaultCulture, Globals.commaspace);
                }
                needsComma = true;

                // If EndpointEncryptionAlgorithm has been set, 
                // the EndpointEncryption needs to be set as well. 
                // Not doing so will cause PropertyNotSetException). 
                EndpointEncryption encryption = (EndpointEncryption)GetPropValue("EndpointEncryption");
                EndpointEncryptionAlgorithm algorithm = GetPropValueOptional("EndpointEncryptionAlgorithm", EndpointEncryptionAlgorithm.None);

                sb.Append("ENCRYPTION = ");
                switch (encryption)
                {
                    case EndpointEncryption.Disabled:
                        sb.Append("DISABLED");
                        if (EndpointEncryptionAlgorithm.None != algorithm)
                        {
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(EndpointEncryptionAlgorithm).Name));
                        }
                        break;
                    case EndpointEncryption.Supported:
                        sb.Append("SUPPORTED");
                        algorithm = (EndpointEncryptionAlgorithm)GetPropValue("EndpointEncryptionAlgorithm");
                        if (EndpointEncryptionAlgorithm.None == algorithm)
                        {
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(EndpointEncryptionAlgorithm).Name));
                        }
                        break;
                    case EndpointEncryption.Required:
                        sb.Append("REQUIRED");
                        algorithm = (EndpointEncryptionAlgorithm)GetPropValue("EndpointEncryptionAlgorithm");
                        if (EndpointEncryptionAlgorithm.None == algorithm)
                        {
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(EndpointEncryptionAlgorithm).Name));
                        }
                        break;
                    default:
                        throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(EndpointEncryption).Name));
                }

                switch (algorithm)
                {
                    case EndpointEncryptionAlgorithm.None:
                        break;
                    case EndpointEncryptionAlgorithm.RC4:
                        sb.Append(" ALGORITHM RC4");
                        break;
                    case EndpointEncryptionAlgorithm.Aes:
                        sb.Append(" ALGORITHM AES");
                        break;
                    case EndpointEncryptionAlgorithm.AesRC4:
                        sb.Append(" ALGORITHM AES RC4");
                        break;
                    case EndpointEncryptionAlgorithm.RC4Aes:
                        sb.Append(" ALGORITHM RC4 AES");
                        break;
                    default:
                        throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(EndpointEncryptionAlgorithm).Name));
                }
            }
        }
    }

    #endregion


    [Facets.StateChangeEvent("CREATE_ENDPOINT", "ENDPOINT")]
    [Facets.StateChangeEvent("ALTER_ENDPOINT", "ENDPOINT")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_SERVER", "ENDPOINT")] // For Owner
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    public partial class Endpoint : ScriptNameObjectBase, Cmn.ICreatable, Cmn.IAlterable, Cmn.IDroppable, Cmn.IDropIfExists, IScriptable
    {
        internal Endpoint(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Endpoint";
            }
        }

        Payload m_payload = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public Payload Payload
        {
            get
            {
                if (null == m_payload)
                {
                    m_payload = new Payload(this);
                }

                return m_payload;
            }
        }

        Protocol m_protocol = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public Protocol Protocol
        {
            get
            {
                if (null == m_protocol)
                {
                    m_protocol = new Protocol(this);
                }

                return m_protocol;
            }
        }

        public void Create()
        {
            base.CreateImpl();
        }

        internal override void ScriptCreate(StringCollection queries, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);

            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full table name for scripting
            string fullEndpointName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                    UrnSuffix, fullEndpointName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_ENDPOINT,
                    "NOT", FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
                sb.Append(Scripts.BEGIN);
                sb.Append(sp.NewLine);
            }

            ScriptEndpoint(sb, sp);

            if (sp.IncludeScripts.ExistenceCheck) // do we need to perform existing object check?
            {
                sb.Append(sp.NewLine);
                sb.Append(Scripts.END);
            }

            queries.Add(sb.ToString());
        }

        private void ScriptEndpoint(StringBuilder sb, ScriptingPreferences sp)
        {
            string fullEndpointName = FormatFullNameForScripting(sp);

            sb.AppendFormat(SmoApplication.DefaultCulture, "{0} ENDPOINT {1} ", sp.ScriptForAlter ? "ALTER" : "CREATE", fullEndpointName);

            //AUTHORIZATION supported only for create
            if (!sp.ScriptForAlter && sp.IncludeScripts.Owner)
            {
                object owner = GetPropValueOptional("Owner");

                if (null != owner)
                {
                    sb.Append(Globals.newline);
                    sb.Append(Globals.tab);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "AUTHORIZATION {0}", MakeSqlBraket((string)owner));
                }
            }

            object endpointState = GetPropValueOptional("EndpointState");

            if (null != endpointState)
            {
                string stateString = string.Empty;

                switch ((EndpointState)endpointState)
                {
                    case EndpointState.Started: stateString = "STARTED"; break;

                    case EndpointState.Stopped: stateString = "STOPPED"; break;

                    case EndpointState.Disabled: stateString = "DISABLED"; break;
                }
                if (stateString.Length > 0)
                {
                    sb.Append(Globals.newline);
                    sb.Append(Globals.tab);
                    sb.AppendFormat(SmoApplication.DefaultCulture, "STATE={0}", stateString);
                }
            }

            //script payload
            sb.Append(Globals.newline);
            sb.Append(Globals.tab);
            sb.Append("AS ");
            this.Protocol.Script(sb, sp);

            //script protocol
            sb.Append(Globals.newline);
            sb.Append(Globals.tab);
            sb.Append("FOR ");
            this.Payload.Script(sb, sp);
        }

        public void Alter()
        {
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection alterQuery, ScriptingPreferences sp)
        {
            if (IsDllDirty() || IsCollectionDirty(this.Payload.Soap.SoapPayloadMethods))
            {
                StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                ScriptEndpoint(sb, sp);
                string result = sb.ToString();
                if (!string.IsNullOrEmpty(result))
                {
                    alterQuery.Add(result);
                }
            }
            ScriptChangeOwner(alterQuery, sp);
        }

        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Drops the object with IF EXISTS option. If object is invalid for drop function will
        /// return without exception.
        /// </summary>
        public void DropIfExists()
        {
            base.DropImpl(true);
        }

        internal override void ScriptDrop(StringCollection dropQuery, ScriptingPreferences sp)
        {
            ThrowIfBelowVersion90(sp.TargetServerVersionInternal);
            StringBuilder sb = new StringBuilder(Globals.INIT_BUFFER_SIZE);

            // format full table name for scripting
            string sFullName = FormatFullNameForScripting(sp);

            if (sp.IncludeScripts.Header) // need to generate commentary headers
            {
                sb.Append(ExceptionTemplates.IncludeHeader(
                        UrnSuffix, sFullName, DateTime.Now.ToString(GetDbCulture())));
                sb.Append(sp.NewLine);
            }

            if (sp.IncludeScripts.ExistenceCheck)
            {
                sb.AppendFormat(SmoApplication.DefaultCulture, Scripts.INCLUDE_EXISTS_ENDPOINT, "",
                    FormatFullNameForScripting(sp, false));
                sb.Append(sp.NewLine);
            }

            sb.AppendFormat(SmoApplication.DefaultCulture, "DROP ENDPOINT {0}", sFullName);

            dropQuery.Add(sb.ToString());
        }

        // 	Starts the endpoint (listens and process requests).
        public void Start()
        {
            try
            {
                SetEndpointState(EndpointState.Started);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Start, this, e);
            }
        }

        // 	Stop the endpoint (listens but will not process requests).
        public void Stop()
        {
            try
            {
                SetEndpointState(EndpointState.Stopped);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Stop, this, e);
            }
        }

        // Disables the endpoint.
        public void Disable()
        {
            try
            {
                SetEndpointState(EndpointState.Disabled);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.Disable, this, e);
            }
        }

        private void SetEndpointState(EndpointState newState)
        {
            // Any endpoint DDL results in a change notification. Upon this change notification, the 
            // old listener is shutdown, all active connections are closed, and the endpoint is restarted 
            // with the new settings. In this case, this means if I issue an alter to, for example start 
            // an already-started endpoint, the endpoint is stopped and restarted.  This is undesirable 
            // since existing connections are disrupted for no reason. To prevent this, we introduce a 
            // guard in the query issued below to prevent the ALTER when it has no effect on the 
            // endpoint state. The whole query then looks like this:
            //      IF (SELECT STATE FROM sys.endpoints WHERE NAME = N'endpointName' ) <> 0 | 1 | 2 
            //      BEGIN
            //              ALTER ENDPOINT [HADR_ENDPOINT] STATE = STARTED | STOPPED | DISABLED 
            //      END
            // Here 0=Started, 1=Stopped, 2=Disabled
            string stateStr;
            switch (newState)
            {
                case EndpointState.Started: stateStr = "STARTED"; break;
                case EndpointState.Stopped: stateStr = "STOPPED"; break;
                case EndpointState.Disabled: stateStr = "DISABLED"; break;
                default:
                    throw new InternalSmoErrorException(ExceptionTemplates.UnknownEnumeration("EndpointState"));
            }
       
            StringBuilder script = new StringBuilder();
            script.Append(
                String.Format(
                    SmoApplication.DefaultCulture,
                    "IF (SELECT state FROM sys.endpoints WHERE name = {0}) <> {1}",
                    MakeSqlString(this.Name),
                    (int)newState));
            script.Append(Globals.newline);
            script.Append(Scripts.BEGIN);
            script.Append(Globals.newline);
            script.Append(Globals.tab);
            script.Append(
                String.Format(
                    SmoApplication.DefaultCulture,
                    "ALTER ENDPOINT {0} STATE = {1}", 
                    this.FullQualifiedName, 
                    stateStr));
            script.Append(Globals.newline);
            script.Append(Scripts.END);
            script.Append(Globals.newline);

            StringCollection queries = new StringCollection();
            queries.Add(script.ToString());
            this.ExecutionManager.ExecuteNonQuery(queries);

            // update state if we're not in recording mode
            if (!this.ExecutionManager.Recording)
            {
                this.Properties["EndpointState"].SetValue(newState);
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        bool IsDllDirty()
        {
            foreach (Property p in this.Properties)
            {
                if (p.Name != "Owner" && p.Dirty)
                {
                    return true;
                }
            }

            if ((null != this.Protocol.EndpointProtocol && this.Protocol.EndpointProtocol.InternalIsObjectDirty) ||
                (null != this.Payload.EndpointPayload && this.Payload.EndpointPayload.InternalIsObjectDirty))
            {
                return true;
            }

            return false;
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            return new PropagateInfo[] {
                new PropagateInfo(this.Protocol.EndpointProtocol, false),
                new PropagateInfo(this.Payload.EndpointPayload, false) };
        }

        protected override void MarkDropped()
        {
            this.Protocol.MarkDropped();
            this.Payload.MarkDropped();

            // mark the object itself as dropped 
            base.MarkDropped();
        }

        // old EndpointType value
        internal object oldEndpointTypeValue = null;

        /// <summary>
        /// Validate property values that are coming from the users.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="value"></param>
        internal override void ValidateProperty(Property prop, object value)
        {
            if (prop.Name == "EndpointType" && !prop.Dirty)
            {
                oldEndpointTypeValue = prop.Value;
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            oldEndpointTypeValue = null;
        }
    }

    public sealed partial class EndpointCollection : SimpleObjectCollectionBase
    {
        public Endpoint[] EnumEndpoints(EndpointType endpointType)
        {
            ArrayList list = new ArrayList();
            foreach (Endpoint e in this)
            {
                if (endpointType == e.EndpointType)
                {
                    list.Add(e);
                }
            }

            Endpoint[] array = new Endpoint[list.Count];
            list.CopyTo(array);
            return array;
        }
    }
}

