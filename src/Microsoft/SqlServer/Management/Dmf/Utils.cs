// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Class that provides various utilities. Public because UI modules also needs some methods here
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class Utils
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "Utils");
        //for all external display, we do not directly use evaluation mode enum names, rather we have
        //corresponding localized descriptive strings. These maps will hold the mapping of enum to strings
        private static Dictionary<AdHocPolicyEvaluationMode, String> AdHocEvaluationModeToDescriptionMap;
        private static Dictionary<String, AutomatedPolicyEvaluationMode> DescriptionToEvaluationModeMap;
        private static Dictionary<AutomatedPolicyEvaluationMode, String> EvaluationModeToDescriptionMap;


        static Utils()
        {
            Utils.DescriptionToEvaluationModeMap = new Dictionary<string, AutomatedPolicyEvaluationMode>();
            Utils.EvaluationModeToDescriptionMap = new Dictionary<AutomatedPolicyEvaluationMode, string>();
            Utils.AdHocEvaluationModeToDescriptionMap = new Dictionary<AdHocPolicyEvaluationMode, string>();

            //mode -> description mappings
            Utils.EvaluationModeToDescriptionMap[AutomatedPolicyEvaluationMode.None] = ExceptionTemplatesSR.EvaluationModeNoneDescription;
            Utils.EvaluationModeToDescriptionMap[AutomatedPolicyEvaluationMode.Enforce] = ExceptionTemplatesSR.EvaluationModeEnforceDescription;
            Utils.EvaluationModeToDescriptionMap[AutomatedPolicyEvaluationMode.CheckOnChanges] = ExceptionTemplatesSR.EvaluationModeCoCDescription;
            Utils.EvaluationModeToDescriptionMap[AutomatedPolicyEvaluationMode.CheckOnSchedule] = ExceptionTemplatesSR.EvaluationModeCoSDescription;

            //description -> mode mappings
            Utils.DescriptionToEvaluationModeMap[ExceptionTemplatesSR.EvaluationModeNoneDescription] = AutomatedPolicyEvaluationMode.None;
            Utils.DescriptionToEvaluationModeMap[ExceptionTemplatesSR.EvaluationModeEnforceDescription] = AutomatedPolicyEvaluationMode.Enforce;
            Utils.DescriptionToEvaluationModeMap[ExceptionTemplatesSR.EvaluationModeCoCDescription] = AutomatedPolicyEvaluationMode.CheckOnChanges;
            Utils.DescriptionToEvaluationModeMap[ExceptionTemplatesSR.EvaluationModeCoSDescription] = AutomatedPolicyEvaluationMode.CheckOnSchedule;

            Utils.AdHocEvaluationModeToDescriptionMap[AdHocPolicyEvaluationMode.Check] = ExceptionTemplatesSR.CheckMode;
            Utils.AdHocEvaluationModeToDescriptionMap[AdHocPolicyEvaluationMode.Configure] = ExceptionTemplatesSR.ConfigureMode;
            Utils.AdHocEvaluationModeToDescriptionMap[AdHocPolicyEvaluationMode.CheckSqlScriptAsProxy] = ExceptionTemplatesSR.CheckSqlScriptAsProxyMode;
        }

        /// <summary>
        /// Provides descriptive localized names for evaluation mode enums
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static String GetDescriptionForEvaluationMode(AutomatedPolicyEvaluationMode mode)
        {
            String descString = String.Empty;
            Utils.EvaluationModeToDescriptionMap.TryGetValue(mode, out descString);

            return descString;
        }

        /// <summary>
        /// Provides descriptive localized names for ad hoc policy evaluation mode enums
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        public static String GetDescriptionForAdHocEvaluationMode(AdHocPolicyEvaluationMode mode)
        {
            String descString = String.Empty;
            Utils.AdHocEvaluationModeToDescriptionMap.TryGetValue(mode, out descString);

            return descString;
        }

        /// <summary>
        /// Given descriptive names, provides the evaluation mode enum
        /// </summary>
        /// <param name="execModeDescription"></param>
        /// <returns></returns>
        public static AutomatedPolicyEvaluationMode GetEvaluationModeByDescription(String execModeDescription)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetEvaluationModeByDescription", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(execModeDescription);
                AutomatedPolicyEvaluationMode mode = AutomatedPolicyEvaluationMode.None;
                Utils.DescriptionToEvaluationModeMap.TryGetValue(execModeDescription, out mode);

                methodTraceContext.TraceParameterOut("returnVal", mode);
                return mode;
            }
        }

        /// <summary>
        /// Validates the link string for policy help link.
        /// </summary>
        /// <param name="link"> The link string</param>
        public static bool IsValidHelpLink(string link)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("IsValidHelpLink", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(link);
                Uri helpLinkUri = new Uri(link);
                if (!(helpLinkUri.Scheme == Uri.UriSchemeHttp ||
                      helpLinkUri.Scheme == Uri.UriSchemeHttps ||
                      helpLinkUri.Scheme == Uri.UriSchemeMailto))
                {
                    methodTraceContext.TraceParameterOut("returnVal", false);
                    return false;
                }

                methodTraceContext.TraceParameterOut("returnVal", true);
                return true;
            }
        }

        /// <summary>
        /// Replaces the read/write properties in lhs with the read/write properties in rhs. The
        /// list of old properties are returned.
        /// </summary>
        internal static Dictionary<string, object> ReplaceSfcProperties(SfcInstance lhs, SfcInstance rhs)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ReplaceSfcProperties"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(lhs, rhs);
                // Use the meta data to replace the r/w properties in the left hand side object
                SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(lhs.GetType());
                List<SfcMetadataRelation> properties = metaData.Properties;
                Dictionary<string, object> oldProperties = new Dictionary<string, object>();

                foreach (SfcMetadataRelation propertyRelation in properties)
                {
                    foreach (Attribute attribute in propertyRelation.RelationshipAttributes)
                    {
                        if (attribute is SfcPropertyAttribute)
                        {
                            SfcPropertyAttribute property = attribute as SfcPropertyAttribute;
                            if (!(property.Data || property.Computed || property.ReadOnlyAfterCreation))
                            {
                                object rhsPropertyVal = rhs.Properties[propertyRelation.PropertyName].Value;

                                if (lhs.Properties[propertyRelation.PropertyName].Value != rhsPropertyVal)
                                {
                                    oldProperties.Add(propertyRelation.PropertyName, lhs.Properties[propertyRelation.PropertyName].Value);
                                    lhs.Properties[propertyRelation.PropertyName].Value = rhsPropertyVal;
                                }
                            }
                        }
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", oldProperties);
                return oldProperties;
            }
        }

        /// <summary>
        /// Sets the properties in lhs with the properties in the given Dictionary.
        /// </summary>
        internal static void ReplaceSfcProperties(SfcInstance lhs, Dictionary<string, object> props)
        {
            traceContext.TraceMethodEnter("ReplaceSfcProperties");
            // Tracing Input Parameters
            traceContext.TraceParameters(lhs, props);
            foreach (string propName in props.Keys)
            {
                lhs.Properties[propName].Value = props[propName];
            }
            traceContext.TraceMethodExit("ReplaceSfcProperties");
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static SqlStoreConnection GetSqlStoreConnection(ISfcConnection targetConnection, string methodName)
        {
            SqlStoreConnection sqlStoreConnection = targetConnection as SqlStoreConnection;
            if (null == sqlStoreConnection)
            {
                // we only support connections to SqlServer for the moment,
                // due to limitations in SfcObjectQuery
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.UnsupportedObjectType(targetConnection.GetType().Name, methodName)));
            }
            return sqlStoreConnection;
        }

        /// <summary>
        /// This function decides whether the exception needs to be processed. 
        /// If the exception is considered to be recoverable it is processed,
        /// otherwise if the exception is unrecoverable we should be rethrowing it.
        /// </summary>
        /// <param name="e"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal static bool ShouldProcessException(Exception e)
        {
            if ((e is OutOfMemoryException) ||
                 (e is StackOverflowException))
                return false;

            return true;
        }

        /// <summary>
        /// This function uses reflection to return the SfcProperties and non DmfIgnore.
        /// Since we support non SMO domains we fork the code to check the domain and 
        /// access the Facet's properties accordignly
        /// </summary>
        /// <param name="managementFacet"></param>
        /// <returns></returns>
        internal static PropertyInfo[] GetPhysicalFacetProperties(Type managementFacet)
        {
            traceContext.DebugAssert(managementFacet != null, "management facet can't be null");
            if (managementFacet == null)
            {
                return null;
            }

            if (managementFacet.IsSubclassOf(typeof(Smo.SqlSmoObject)))
            {
                return Smo.SmoDmfAdapter.GetTypeProperties(managementFacet).Where(pi => EvaluationFactory.IsTypeSupported (pi.PropertyType)).ToArray();
            }
            else
            {
                List<PropertyInfo> properties = new List<PropertyInfo>();
                foreach (PropertyInfo pi in managementFacet.GetProperties())
                {
                    if (pi.GetCustomAttributes(typeof(SfcPropertyAttribute), false).Length != 0 &&
                        pi.GetCustomAttributes(typeof(DmfIgnorePropertyAttribute), false).Length == 0)
                    {
                        properties.Add(pi);
                    }
                }
                return properties.ToArray();
                        
            }

        }

        internal static bool IsSmoDomain(SfcDomainInfo domainInfo)
        {
            if (domainInfo == null)
            {
                return false;
            }

            return domainInfo.NamespaceQualifier == "SMO";
        }

        /// <summary>
        /// checks if the string path is reffering to an SMO object
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static bool IsSmoPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            Urn urn = new Urn(path);
            traceContext.DebugAssert(urn.XPathExpression.Length > 0, "Xpath expression should contain non zero elements");
            return string.Equals(urn.XPathExpression[0].Name, "Server", StringComparison.Ordinal);
        }

        /// <summary>
        /// this function retieves the domain for the specific skeleton by enumerating the Registered domains.
        /// It is used to GetTypeFromUrnSkeleton and GetTargetDomain functions
        /// </summary>
        /// <param name="skeleton"></param>
        internal static SfcDomainInfo GetDomainFromUrnSkeleton(string skeleton)
        {
            Urn urn = new Urn(skeleton);
            XPathExpression xpe = urn.XPathExpression;
            traceContext.DebugAssert(xpe != null, "skeleton doesn't describe a valid urn");
            traceContext.DebugAssert(xpe.Length > 0, "xpe length should be non zero");
            string rootName = xpe[0].Name;

            SfcDomainInfo domain = SfcRegistration.Domains[rootName];
            traceContext.DebugAssert(domain != null);
            return domain;
        }

        /// <summary>
        /// This function returns the object type from the skeleton string
        /// NOTE: This is a temporary solution, we have to move that code to SfcMetadata
        /// </summary>
        /// <param name="skeleton"></param>
        /// <returns></returns>
        internal static Type GetTypeFromUrnSkeleton(string skeleton)
        {
            if (string.IsNullOrEmpty(skeleton))
            {
                return null;
            }

            SfcDomainInfo domain = GetDomainFromUrnSkeleton(skeleton);

            if (domain != null)
            {
                if (domain.NamespaceQualifier == "SMO")
                {
                    return Smo.SqlSmoObject.GetTypeFromUrnSkeleton(skeleton);
                }
                else
                {
                    if (domain.Name == skeleton)
                    {
                        return domain.RootType;
                    }
                    else
                    {
                        Urn urn = new Urn(skeleton);
                        string typeName = urn.Type;
                        object obj = Activator.CreateInstance(domain.RootType);
                        Type type = ((ISfcDomain)obj).GetType(typeName);
                        return type;
                    }

                }
            }

            return null;
        }

        /// <summary>
        /// The following function Instantiates an object query through a domainInfo. It instantiates the domain root
        /// from the domain Info and then uses it to create a new query. I tried to move this to 
        /// SFC but I had problems instantiating an SMO server due to Partially trusted caller issue.
        /// </summary>
        /// <param name="domainInfo"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        internal static SfcObjectQuery GetQueryFromDomainInfo(SfcDomainInfo domainInfo, ISfcConnection connection)
        {           
            traceContext.DebugAssert(domainInfo != null, "DomainInfo can't be null");

            SqlStoreConnection sqlStoreConnection = Utils.GetSqlStoreConnection(connection, "Utils.GetQueryFromDomainInfo");

            if (domainInfo.NamespaceQualifier == "SMO")
            {
                SMO.Server server = new SMO.Server(sqlStoreConnection.ServerConnection);
                return new SfcObjectQuery(server);
            }
            else
            {
                //here we assume that all other domain root constructor expect sqlstoreconnection as parameter
                //this doesn't work with all domain constructors (ReportingServices for example)
                ISfcDomain domain = Activator.CreateInstance(domainInfo.RootType, sqlStoreConnection) as ISfcDomain;
                return new SfcObjectQuery(domain);
            }
        }

    }

}
