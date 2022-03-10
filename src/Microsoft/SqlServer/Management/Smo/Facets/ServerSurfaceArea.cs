// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Facets;
using Dmf = Microsoft.SqlServer.Management.Dmf;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    [StateChangeEvent("AUDIT_SERVER_OPERATION_EVENT", "SERVER")]
    [StateChangeEvent("CREATE_ENDPOINT", "SERVER")]
    [StateChangeEvent("ALTER_ENDPOINT", "SERVER")]
    [StateChangeEvent("SAC_ENDPOINT_CHANGE", "SERVER")]
    [EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("ServerSurfaceAreaConfigurationName")]
    [Sfc.DisplayDescriptionKey("ServerSurfaceAreaConfigurationDesc")]
    [CLSCompliantAttribute(false)]
    public interface ISurfaceAreaFacet : Sfc.IDmfFacet
    {
        /// <summary>
        /// Ad-hoc Remote Queries Enabled
        /// </summary>
        [Sfc.DisplayNameKey("AdHocRemoteQueriesEnabledName")]
        [Sfc.DisplayDescriptionKey("AdHocRemoteQueriesEnabledDesc")]
        bool AdHocRemoteQueriesEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Database Mail Enabled
        /// </summary>
        [Sfc.DisplayNameKey("DatabaseMailEnabledName")]
        [Sfc.DisplayDescriptionKey("DatabaseMailEnabledDesc")]
        bool DatabaseMailEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// CLR Integration Enabled
        /// </summary>
        [Sfc.DisplayNameKey("ClrIntegrationEnabledName")]
        [Sfc.DisplayDescriptionKey("ClrIntegrationEnabledDesc")]
        bool ClrIntegrationEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// OLE Automation Enabled
        /// </summary>
        [Sfc.DisplayNameKey("OleAutomationEnabledName")]
        [Sfc.DisplayDescriptionKey("OleAutomationEnabledDesc")]
        bool OleAutomationEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Remote DAC Enabled
        /// </summary>
        [Sfc.DisplayNameKey("RemoteDacEnabledName")]
        [Sfc.DisplayDescriptionKey("RemoteDacEnabledDesc")]
        bool RemoteDacEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// SqlMail Enabled
        /// </summary>
        [Sfc.DisplayNameKey("SqlMailEnabledName")]
        [Sfc.DisplayDescriptionKey("SqlMailEnabledDesc")]
        bool SqlMailEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Web Assistant Enabled
        /// </summary>
        [Sfc.DisplayNameKey("WebAssistantEnabledName")]
        [Sfc.DisplayDescriptionKey("WebAssistantEnabledDesc")]
        bool WebAssistantEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// xp_cmdshell Enabled
        /// </summary>
        [Sfc.DisplayNameKey("XPCmdShellEnabledName")]
        [Sfc.DisplayDescriptionKey("XPCmdShellEnabledDesc")]
        bool XPCmdShellEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("ServiceBrokerEndpointActiveName")]
        [Sfc.DisplayDescriptionKey("ServiceBrokerEndpointActiveDesc")]
        bool ServiceBrokerEndpointActive
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("SoapEndpointsEnabledName")]
        [Sfc.DisplayDescriptionKey("SoapEndpointsEnabledDesc")]
        bool SoapEndpointsEnabled
        {
            get;
            set;
        }
    }


    /// <summary>
    /// This facet is entirely a subset of configuration options, so it is just an interface on the ServerConfigurationAdapter
    /// </summary>
    public partial class ServerSurfaceAreaAdapter : ServerAdapterBase, IDmfAdapter, ISurfaceAreaFacet
    {
        #region Constructors
        public ServerSurfaceAreaAdapter(Microsoft.SqlServer.Management.Smo.Server obj)
            : base(obj)
        {
        }
        #endregion

        #region Endpoint properties are logical

        // need to have these two because altering state is an operation 
        // on the Endpoint object
        private EndpointState desiredBrokerEndpointState;
        private bool brokerEndpointStateAltered = false;

        /// <summary>
        /// Returns the Broker endpoint defined on the server, if it exists.
        /// At eny point the server can have at most one broker endpoint, 
        /// if there isn't such an endpoint the function returns null.
        /// </summary>
        /// <returns></returns>
        private Endpoint GetBrokerEndpoint()
        {
            Endpoint brokerEndpoint = null;
            foreach (Endpoint endpoint in this.Server.Endpoints)
            {
                if (endpoint.EndpointType == EndpointType.ServiceBroker)
                {
                    brokerEndpoint = endpoint;
                    break;
                }
            }

            return brokerEndpoint;
        }

        /// <summary>
        /// Returns TRUE when a Service Broker endpoint exists and is in the 
        /// STARTED state. Returns FALSE when Service Broker does not exist 
        /// on the computer or it exists but is not started. Configure the 
        /// endpoint to TRUE to start a stopped or disabled endpoint. The 
        /// operation will fail if the endpoint does not exist on the server. 
        /// Configure the endpoint to FALSE to disable the Service Broker endpoint.
        /// </summary>
        public bool ServiceBrokerEndpointActive
        {
            get
            {
                Endpoint endpoint = GetBrokerEndpoint();
                if (null != endpoint)
                {
                    return endpoint.EndpointState == EndpointState.Started;
                }

                return false;
            }
            set
            {
                if (value)
                {
                    desiredBrokerEndpointState = EndpointState.Started;
                }
                else
                {
                    desiredBrokerEndpointState = EndpointState.Stopped;
                }
                this.brokerEndpointStateAltered = true;
            }
        }

        // need to have these two because altering state is an operation 
        // on the Endpoint object
        private bool disableSoapEndpoints = false;

        /// <summary>
        /// Returns TRUE if at least one SOAP endpoint is responding to SOAP 
        /// requests. This means that the endpoint is either in the Started
        /// or Stopped state as the endpoint responds to requests in both states.
        /// Returns FALSE if all SOAP endpoints are disabled or if 
        /// there are no SOAP endpoints. Configuring it to FALSE will disable 
        /// all the soap endpoints.
        /// </summary>
        public bool SoapEndpointsEnabled
        {
            get
            {
                foreach (Endpoint endpoint in this.Server.Endpoints)
                {
                    if (endpoint.EndpointType == EndpointType.Soap &&
                        endpoint.EndpointState != EndpointState.Disabled)
                    {
                        return true;
                    }
                }

                return false;
            }
            set
            {
                if (value)
                {
                    throw new SmoException(ExceptionTemplates.CannotEnableSoapEndpoints);
                }
                else
                {
                    disableSoapEndpoints = true;
                }
            }
        }

        protected void RefreshEndpoints()
        {
            this.Server.Endpoints.Refresh();
            foreach (Endpoint endpoint in this.Server.Endpoints)
            {
                endpoint.Refresh();
            }

            brokerEndpointStateAltered = false;
            disableSoapEndpoints = false;
        }

        /// <summary>
        /// This function implements the delayed setters for the
        /// endpoints properties.
        /// </summary>
        protected void AlterEndpoints()
        {
            if (brokerEndpointStateAltered)
            {
                // If the broker endpoint exists we will Start/Stop it to set the
                // property to TRUE/FALSE. 
                // If the broker endpoint does not exist and we try to set the 
                // property to true we will throw an exception.

                brokerEndpointStateAltered = false;
                Endpoint endpoint = GetBrokerEndpoint();
                if (null != endpoint)
                {
                    if (desiredBrokerEndpointState == EndpointState.Stopped)
                    {
                        endpoint.Stop();
                    }
                    else if (desiredBrokerEndpointState == EndpointState.Started)
                    {
                        endpoint.Start();
                    }
                    else
                    {
                        throw new SmoException(ExceptionTemplates.UnknownEnumeration("EndpointState"));
                    }
                }
                else if (desiredBrokerEndpointState != EndpointState.Stopped)
                {
                    throw new SmoException(ExceptionTemplates.MissingBrokerEndpoint);
                }
            }

            if (disableSoapEndpoints)
            {
                // disable all SOAP endpoints 
                foreach (Endpoint endpoint in this.Server.Endpoints)
                {
                    if (endpoint.EndpointType == EndpointType.Soap &&
                        endpoint.EndpointState != EndpointState.Disabled)
                    {
                        endpoint.Disable();
                    }
                }

                disableSoapEndpoints = false;
            }
        }

        #endregion

        public override void Refresh()
        {
            base.Refresh();
            RefreshEndpoints();
        }

        public override void Alter()
        {
            base.Alter();
            AlterEndpoints();
        }

    }
}
