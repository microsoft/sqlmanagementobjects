// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Diagnostics.STrace;
using System;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Data.SqlTypes;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Server;
using SMO = Microsoft.SqlServer.Management.Smo;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// The PolicyStore object is the root object for the DMF hierarchy.
    /// </summary>
    [TypeConverter(typeof(LocalizableTypeConverter))]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class PolicyStore : SfcInstance, ISfcAlterable, ISfcDomain, ISfcSerializableUpgrade
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyStore");
        internal static readonly Version KatmaiVersion = new Version(10, 0);
        internal static readonly Version KilimanjaroVersion = new Version(10, 50);

        SfcObjectQuery objectQuery = null;
        SfcObjectQuery smoObjectQuery = null;

        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter purgeHealthStateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter markSystemObjectAction = new SfcTsqlProcFormatter();

        private const string DomainName = "DMF";

        static PolicyStore()
        {
            // Alter script
            scriptAlterAction.Procedure = "msdb.dbo.sp_syspolicy_configure";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", true));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("value", true));

            purgeHealthStateAction.Procedure = "msdb.dbo.sp_syspolicy_purge_health_state";
            purgeHealthStateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_tree_root_with_id", false));

            markSystemObjectAction.Procedure = "msdb.dbo.sp_syspolicy_mark_system";
            markSystemObjectAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("type", true));
            markSystemObjectAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_id", true));
            markSystemObjectAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("marker", true));
        }

        /// <summary>
        /// Don't ever call this, or if you do remember to set SqlStoreConnection
        /// </summary>
        public PolicyStore()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connection"></param>
        public PolicyStore(SfcConnection connection)
        {
            traceContext.TraceMethodEnter("PolicyStore");

            traceContext.DebugAssert(connection is SqlStoreConnection);
            this.SqlStoreConnection = (SqlStoreConnection)connection;
            MarkRootAsConnected(); // setting a connection makes the server "live"
            traceContext.TraceMethodExit("PolicyStore");
        }

        #region IAlterable
        /// <summary>
        /// 
        /// </summary>
        public void Alter()
        {
            traceContext.TraceMethodEnter("Alter");
            Validate();
            base.AlterImpl();
            traceContext.TraceMethodExit("Alter");
        }

        private const string PropNameEnabled = "Enabled";
        private const string PropNameHistoryRetentionInDays = "HistoryRetentionInDays";
        private const string PropNameLogOnSuccess = "LogOnSuccess";

        /// <summary>
        /// 
        /// </summary>
        public ISfcScript ScriptAlter()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptAlter", System.Diagnostics.TraceEventType.Information))
            {
                SfcTSqlScript alterScript = new SfcTSqlScript();

                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                    this.Enabled.GetType(), PropNameEnabled));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                    typeof(int), this.Enabled ? 1 : 0));

                alterScript.AddBatch(scriptAlterAction.GenerateScript(this, args));

                args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                    PropNameHistoryRetentionInDays.GetType(), PropNameHistoryRetentionInDays));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                    typeof(int), this.HistoryRetentionInDays));

                alterScript.AddBatch(scriptAlterAction.GenerateScript(this, args));

                args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                    PropNameLogOnSuccess.GetType(), PropNameLogOnSuccess));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                    typeof(int), this.LogOnSuccess ? 1 : 0));

                alterScript.AddBatch(scriptAlterAction.GenerateScript(this, args));

                methodTraceContext.TraceParameterOut("returnVal", alterScript);
                return alterScript;
            }
        }
        #endregion

        /// <summary>
        /// The string identity of a policy store is the associated Server name.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            // ToString is overriden here since the default from SfcInstance is to use whatever the Key.ToString() does,
            // and since PolicyStore doesn't have any Key fields per se, we override it on the class itself and make our own string.
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0} (Smo.Server='{1}')", PolicyStore.typeName,
                this.SqlStoreConnection != null && this.SqlStoreConnection.ServerConnection != null
                    ? SfcSecureString.EscapeSquote(this.SqlStoreConnection.ServerConnection.TrueName) : "");

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcKey CreateIdentityKey()
        {
            return new Key(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [SfcIgnore]
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case Condition.typeName:
                    return this.Conditions;
                case Policy.typeName:
                    return this.Policies;
                case PolicyCategorySubscription.typeName:
                    return this.PolicyCategorySubscriptions;
                case PolicyCategory.typeName:
                    return this.PolicyCategories;
                case ObjectSet.typeName:
                    return this.ObjectSets;
                default: throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
            }
        }

        SfcConnectionContext m_context = null;
        SqlStoreConnection m_connection = null;

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting a single serial query, so the query must end before another one may begin.
        /// </summary>
        /// <returns></returns>
        ISfcConnection ISfcHasConnection.GetConnection()
        {
            return this.SqlStoreConnection;
        }

        /// <summary>
        /// Sets the active connection.
        /// </summary>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        void ISfcHasConnection.SetConnection(ISfcConnection connection)
        {
            if (connection is ServerConnection)
            {
                this.SqlStoreConnection = new SqlStoreConnection(((ServerConnection)connection).SqlConnectionObject);
            }
            else if (connection is SqlStoreConnection)
            {
                this.SqlStoreConnection = (SqlStoreConnection)connection;
            }
            else
            {
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.UnsupportedConnectionType(connection.GetType().FullName)));
            }
        }

        // TODO:: Take into account Single User mode when honoring MultipleActiveQueries mode
        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting either a single serial query or multiple simultaneously open queries as requested.
        /// </summary>
        /// <param name="mode"></param>
        /// <returns>The connection to use, or null to use Cache mode. Cache mode avoids connection and open data reader issues.</returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        ISfcConnection ISfcHasConnection.GetConnection(SfcObjectQueryMode mode)
        {
            switch (mode)
            {
                case SfcObjectQueryMode.SingleActiveQuery:
                    return this.SqlStoreConnection;

                case SfcObjectQueryMode.MultipleActiveQueries:
                    if (this.SqlStoreConnection.ServerConnection.MultipleActiveResultSets)
                    {
                        return this.SqlStoreConnection;
                    }

                    // If we are in single user mode, we must throw now so we don't hide the problem until the next query is attempted.
                    // Note: PolicyStore needs to implement a property much like Smo.Server.Information.IsSingleUser boolean.
                    // In v1, Sfc ObjectQuery will internally cache the data reader anyhow if we throw or return null.
                    //if (this.IsSingleUser)
                    //{
                    //    throw new SfcQueryConnectionUnavailableException();
                    //}

                    // TODO:: Return a clone of our connection, since it is telling us that our connection is going to probably be "in use" shortly.
                    // Fallback on cached mode for now by returning null.
                    return null;

                default:
                    // Indicate we don't know what to do here, and let the caller maybe cache things for us (like OQ will do)
                    return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        SfcConnectionContext ISfcHasConnection.ConnectionContext
        {
            get
            {
                if (m_context == null)
                {
                    // If our SqlStoreConnection is still null when this is called, we are forced into Offline mode.
                    m_context = new SfcConnectionContext(this);
                }
                return m_context;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcIgnore]
        public SqlStoreConnection SqlStoreConnection
        {
            get
            {
                return m_connection;
            }
            set
            {
                traceContext.TraceVerbose("Setting SqlStoreConnection to: {0}", value);
                m_connection = value;
                MarkRootAsConnected(); // setting a connection makes the server "live"
            }
        }

        ConditionCollection m_Conditions;

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Condition))]
        public ConditionCollection Conditions
        {
            get
            {
                if (m_Conditions == null)
                {
                    m_Conditions = new ConditionCollection(this);
                }
                return m_Conditions;
            }
        }

        ObjectSetCollection m_ObjectSets;
        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ObjectSet))]
        public ObjectSetCollection ObjectSets
        {
            get
            {
                if (m_ObjectSets == null)
                {
                    m_ObjectSets = new ObjectSetCollection(this);
                }
                return m_ObjectSets;
            }
        }

        PolicyCollection m_Policies;

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Policy))]
        public PolicyCollection Policies
        {
            get
            {
                if (m_Policies == null)
                {
                    m_Policies = new PolicyCollection(this, new ServerComparer((this.SqlStoreConnection == null) ? null : this.SqlStoreConnection.ServerConnection, "msdb"));
                }
                return m_Policies;
            }
        }

        PolicyCategoryCollection m_PolicyCategories;
        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(PolicyCategory))]
        public PolicyCategoryCollection PolicyCategories
        {
            get
            {
                if (m_PolicyCategories == null)
                {
                    m_PolicyCategories = new PolicyCategoryCollection(this, new ServerComparer((this.SqlStoreConnection == null) ? null : this.SqlStoreConnection.ServerConnection, "msdb"));
                }
                return m_PolicyCategories;
            }
        }

        static FacetInfoCollection m_facets;

        /// <summary>
        /// Return a Collection of FacetInfo.
        /// </summary>
        [SfcIgnore]
        public static FacetInfoCollection Facets
        {
            get
            {
                if (null == m_facets)
                {
                    PolicyStore.m_facets = new FacetInfoCollection();
                }

                if (PolicyStore.m_facets.Count != FacetRepository.RegisteredFacetsCount)
                {
                    PolicyStore.m_facets.Clear();

                    //Have to sort the facets separately since RegisteredFacets are actually
                    //key collection on some hashtable and FacetInfoCollection is not sortable
                    List<FacetInfo> list = new List<FacetInfo>();

                    foreach (object type in FacetRepository.RegisteredFacets)
                    {
                        list.Add(new FacetInfo((Type)type));
                    }

                    list.Sort();

                    foreach (FacetInfo info in list)
                    {
                        m_facets.Add(info);
                    }
                }

                return m_facets;
            }
        }

        /// <summary>
        /// Return a Collection of FacetInfo for listed domains.
        /// Pass String.Empty for Facets, not associated with SFC Domain
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static FacetInfoCollection EnumDomainFacets(params string [] args)
        {
            if (null == args || args.Length == 0)
            {
                throw new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("args"));
            }

            FacetInfoCollection facets = new FacetInfoCollection();
            List<string> domains = new List<string>(args);

            //Have to sort the facets separately since RegisteredFacets are actually
            //key collection on some hashtable and FacetInfoCollection is not sortable
            List<FacetInfo> list = new List<FacetInfo>();

            foreach (Type type in FacetRepository.RegisteredFacets)
            {
                string domainName = SfcRegistration.GetRegisteredDomainForType(type.FullName, false);
                if (!String.IsNullOrEmpty(domainName))
                {
                    string domain = SfcRegistration.Domains[domainName].NamespaceQualifier;
                    if (!domains.Contains(domain))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!domains.Contains(null))
                    {
                        continue;
                    }
                }
                list.Add(new FacetInfo(type));
            }

            list.Sort();

            foreach (FacetInfo info in list)
            {
                facets.Add(info);
            }

            return facets;
        }

        /// <summary>
        /// Returns a collection of Root facets
        /// </summary>
        public static FacetInfoCollection EnumRootFacets(Type rootType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EnumRootFacets", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(rootType);
                List<FacetInfo> list = new List<FacetInfo>();
                foreach (Type type in FacetRepository.GetRootFacets(rootType))
                {
                    list.Add(new FacetInfo(type));
                }
                list.Sort();

                FacetInfoCollection rootFacets = new FacetInfoCollection();
                foreach (FacetInfo info in list)
                {
                    rootFacets.Add(info);
                }
                methodTraceContext.TraceParameterOut("returnVal", rootFacets);
                return rootFacets;

            }
        }

        PolicyCategorySubscriptionCollection m_CategorySubscriptions;

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(PolicyCategorySubscription))]
        public PolicyCategorySubscriptionCollection PolicyCategorySubscriptions
        {
            get
            {
                if (m_CategorySubscriptions == null)
                {
                    m_CategorySubscriptions = new PolicyCategorySubscriptionCollection(this);
                }
                return m_CategorySubscriptions;
            }
        }
        [SfcIgnore]
        internal SfcObjectQuery SfcObjectQuery
        {
            get
            {
                if (this.objectQuery == null)
                {
                    this.objectQuery = new SfcObjectQuery(this);
                }
                return this.objectQuery;
            }
        }
        [SfcIgnore]
        internal SfcObjectQuery SmoObjectQuery
        {
            get
            {
                if (this.smoObjectQuery == null)
                {
                    this.smoObjectQuery = new SfcObjectQuery(new SMO.Server(this.SqlStoreConnection.ServerConnection));
                }
                return this.smoObjectQuery;
            }
        }


        #region Object Enumeration Helpers

        /// <summary>
        /// Defines behavior towards IsSystemObject flag
        /// </summary>
        [Flags]
        public enum EnumerationMode
        {
            /// <summary>
            /// Include only objects with IsSystemObject=false
            /// </summary>
            NonSystemOnly = 1,
            /// <summary>
            /// Include only objects with IsSystemObject=true
            /// </summary>
            SystemOnly = 2,
            /// <summary>
            /// Include all objects regardless of IsSystemObject state
            /// </summary>
            All = 3
        }

        /// <summary>
        /// Enumerate Conditions associated with the given facet
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        public StringCollection EnumConditionsOnFacet(string facet)
        {
            StringCollection sc = new StringCollection();

            // Don't throw on bad facet argument, return nothing
            if (String.IsNullOrEmpty(facet) || null == FacetRepository.GetFacetType(facet))
            {
                return sc;
            }
            
            List<string> list = new List<string>();
            foreach (Condition c in this.Conditions)
            {
                if (c.Facet == facet)
                {
                    list.Add(c.Name);
                }
            }
            list.Sort();

            sc.AddRange(list.ToArray());

            return sc;
        }

        /// <summary>
        /// Enumerate Conditions associated with the given facet, honoring IsSystemObject flag
        /// </summary>
        /// <param name="facet"></param>
        /// <param name="enumerationMode"></param>
        /// <returns></returns>
        public StringCollection EnumConditionsOnFacet(string facet, EnumerationMode enumerationMode)
        {
            // If request is ALL, just use the old method to make it a bit faster
            if (enumerationMode == EnumerationMode.All)
            {
                return EnumConditionsOnFacet(facet);
            }

            StringCollection sc = new StringCollection();

            // Don't throw on bad facet argument, return nothing
            if (String.IsNullOrEmpty(facet) || null == FacetRepository.GetFacetType(facet))
            {
                return sc;
            }
            
            List<string> list = new List<string>();
            foreach (Condition c in this.Conditions)
            {
                if (c.Facet == facet)
                {
                    switch (enumerationMode)
                    {
                        case EnumerationMode.NonSystemOnly:
                            if (c.IsSystemObject)
                            {
                                continue;
                            }
                            break;
                        case EnumerationMode.SystemOnly:
                            if (!c.IsSystemObject)
                            {
                                continue;
                            }
                            break;
                    }

                    list.Add(c.Name);
                }
            }
            list.Sort();

            sc.AddRange(list.ToArray());

            return sc;
        }

        /// <summary>
        /// Enmerate root conditions for a given type
        /// </summary>
        /// <param name="rootType"></param>
        /// <returns></returns>
        public StringCollection EnumRootConditions(Type rootType)
        {
            StringCollection sc = new StringCollection();

            List<Type> rootFacets = FacetRepository.GetRootFacets(rootType);

            foreach (Condition c in this.Conditions)
            {
                if (rootFacets.Contains(FacetRepository.GetFacetType(c.Facet)))
                {
                    sc.Add(c.Name);
                    continue;
                }
            }

            return sc;
        }


        /// <summary>
        /// Enumerate Policies associated with the given facet
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        public StringCollection EnumPoliciesOnFacet(string facet)
        {
            StringCollection sc = new StringCollection();
            
            // Don't throw on bad facet argument, return nothing
            if (String.IsNullOrEmpty(facet) || null == FacetRepository.GetFacetType(facet))
            {
                return sc;
            }

            StringCollection csc = EnumConditionsOnFacet(facet);

            if (csc.Count == 0)
            {
                return sc;
            }

            foreach (Policy p in this.Policies)
            {
                if (csc.Contains(p.Condition))
                {
                    sc.Add(p.Name);
                }
            }
            return sc;
        }

        /// <summary>
        /// Enumerate Policies associated with the given facet, honoring IsSystemObject flag
        /// </summary>
        /// <param name="facet"></param>
        /// <param name="enumerationMode"></param>
        /// <returns></returns>
        public StringCollection EnumPoliciesOnFacet(string facet, EnumerationMode enumerationMode)
        {
            // If request is ALL, just use the old method to make it a bit faster
            if (enumerationMode == EnumerationMode.All)
            {
                return EnumPoliciesOnFacet(facet);
            }

            StringCollection sc = new StringCollection();

            // Don't throw on bad facet argument, return nothing
            if (String.IsNullOrEmpty(facet) || null == FacetRepository.GetFacetType(facet))
            {
                return sc;
            }

            StringCollection csc = EnumConditionsOnFacet(facet);

            if (csc.Count == 0)
            {
                return sc;
            }

            foreach (Policy p in this.Policies)
            {
                switch (enumerationMode)
                {
                    case EnumerationMode.NonSystemOnly:
                        if (p.IsSystemObject)
                        {
                            continue;
                        }
                        break;
                    case EnumerationMode.SystemOnly:
                        if (!p.IsSystemObject)
                        {
                            continue;
                        }
                        break;
                }

                if (csc.Contains(p.Condition))
                {
                    sc.Add(p.Name);
                }
            }
            return sc;
        }

        /// <summary>
        /// Enumerate Conditions for a given Type that can be use in TargetSet filters
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public StringCollection EnumTargetSetConditions(Type type)
        {
            StringCollection conditions = new StringCollection();

            // Don't throw on bad facet argument, return nothing
            if (null == type || !FacetRepository.IsRegisteredFacet(type))
            {
                return conditions;
            }

            string facet = type.Name;

            foreach (Condition c in this.Conditions)
            {
                if (c.IsEnumerable)
                {
                    // Since Physical facet is type itself
                    // we only need to check if the target type is the facet type
                    if (String.Equals (facet, c.Facet, StringComparison.Ordinal))
                    {
                        conditions.Add(c.Name);
                    }
                }
            }

            return conditions;
        }

        /// <summary>
        /// Enumerate Conditions for a given Type that can be use in TargetSet filters, honoring IsSystemObject flag
        /// </summary>
        /// <param name="type"></param>
        /// <param name="enumerationMode"></param>
        /// <returns></returns>
        public StringCollection EnumTargetSetConditions(Type type, EnumerationMode enumerationMode)
        {
            // If request is ALL, just use the old method to make it a bit faster
            if (enumerationMode == EnumerationMode.All)
            {
                return EnumTargetSetConditions(type);
            }

            StringCollection conditions = new StringCollection();

            // Don't throw on bad facet argument, return nothing
            if (null == type || !FacetRepository.IsRegisteredFacet(type))
            {
                return conditions;
            }
            
            string facet = type.Name;

            foreach (Condition c in this.Conditions)
            {

                switch (enumerationMode)
                {
                    case EnumerationMode.NonSystemOnly:
                        if (c.IsSystemObject)
                        {
                            continue;
                        }
                        break;
                    case EnumerationMode.SystemOnly:
                        if (!c.IsSystemObject)
                        {
                            continue;
                        }
                        break;
                }

                if (c.IsEnumerable)
                {
                    // Since Physical facet is type itself
                    // we only need to check if the target type is the facet type
                    if (String.Equals (facet,c.Facet, StringComparison.Ordinal))
                    {
                        conditions.Add(c.Name);
                    }
                }
            }

            return conditions;
        }

        #endregion Object Enumeration Helpers

        #region ISfcDomain Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        Type ISfcDomain.GetType(string typeName)
        {
            switch (typeName)
            {
                case PolicyStore.typeName: return typeof(PolicyStore);
                case Policy.typeName: return typeof(Policy);
                case PolicyCategorySubscription.typeName: return typeof(PolicyCategorySubscription);
                case PolicyCategory.typeName: return typeof(PolicyCategory);
                case ObjectSet.typeName: return typeof(ObjectSet);
                case TargetSet.typeName: return typeof(TargetSet);
                case Condition.typeName: return typeof(Condition);
                case TargetSetLevel.typeName: return typeof(TargetSetLevel);
                case EvaluationHistory.typeName: return typeof(EvaluationHistory);
                case ConnectionEvaluationHistory.typeName: return typeof(ConnectionEvaluationHistory);
                case EvaluationDetail.typeName: return typeof(EvaluationDetail);
            }
            return null;
        }

        /// <summary>
        /// returns the Key object given Urn fragment
        /// </summary>
        /// <param name="urnFragment"></param>
        /// <returns>SfcKey</returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        SfcKey ISfcDomain.GetKey(IUrnFragment urnFragment)
        {
            switch (urnFragment.Name)
            {
                case PolicyStore.typeName: return new PolicyStore.Key(this);
                case Policy.typeName: return new Policy.Key(urnFragment.FieldDictionary);
                case PolicyCategorySubscription.typeName: return new PolicyCategorySubscription.Key(urnFragment.FieldDictionary);
                case PolicyCategory.typeName: return new PolicyCategory.Key(urnFragment.FieldDictionary);
                case ObjectSet.typeName: return new ObjectSet.Key(urnFragment.FieldDictionary);
                case TargetSet.typeName: return new TargetSet.Key(urnFragment.FieldDictionary);
                case Condition.typeName: return new Condition.Key(urnFragment.FieldDictionary);
                case TargetSetLevel.typeName: return new TargetSetLevel.Key(urnFragment.FieldDictionary);
                case EvaluationHistory.typeName: return new EvaluationHistory.Key(urnFragment.FieldDictionary);
                case ConnectionEvaluationHistory.typeName: return new ConnectionEvaluationHistory.Key(urnFragment.FieldDictionary);
                case EvaluationDetail.typeName: return new EvaluationDetail.Key(urnFragment.FieldDictionary);
            }
            throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.UnsupportedKey(urnFragment.Name)));
        }

        ISfcExecutionEngine ISfcDomain.GetExecutionEngine()
        {
            return new SfcTSqlExecutionEngine(this.SqlStoreConnection.ServerConnection);
        }

        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        SfcTypeMetadata ISfcDomain.GetTypeMetadata(string typeName)
        {
            switch (typeName)
            {
                // TODO: Why do we need this?
                case TargetSetLevel.typeName: return TargetSetLevel.GetTypeMetadata();
                default:
                    // Not implemented for other types yet, and not yet needed
                    return null;
            }
        }

        bool ISfcDomain.UseSfcStateManagement()
        {
            return true;    // DMF uses SFC-provided state management
        }

        /// <summary>
        /// Returns the logical version of the domain
        /// </summary>
        /// <returns></returns>
        int ISfcDomainLite.GetLogicalVersion()
        {
            // 1 = Katmai CTP5
            // 2 = Katmai CTP6
            return 3;      // logical version changes only when the schema of domain changes
        }

        [SfcIgnore]
        string ISfcDomainLite.DomainName
        {
            get { return PolicyStore.DomainName; }
        }
        [SfcIgnore]
        string ISfcDomainLite.DomainInstanceName
        {
            get
            {
                if ((this as ISfcHasConnection).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return PolicyStore.DomainName;
                }
                else
                {
                    return this.m_connection.ServerConnection.TrueName;
                }
            }
        }

        #endregion

        #region ISfcDiscoverObject Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            //sink.Add(SfcDependencyDirection.Inbound, Policies.GetEnumerator(), SfcTypeRelation.ContainedChild, false);
            return;
        }

        #endregion

        /// <summary>
        /// Returns a DataTable containing the
        /// policies that apply to the same target type as the
        /// supplied target. In other words, if a Database target is
        /// given then policies that apply to Database, Table and any
        /// other types under database will be considered.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>A DataTable object containing the policies.
        /// The schema for this is as follows:
        /// Column                          Type                
        /// Name                            string              
        /// PolicyCategory                  String
        /// Effective                       bool
        /// AutomatedPolicyEvaluationMode    enum
        /// PolicyEffectiveState            enum
        /// PolicyHealthState               enum
        /// LastExecutionTime               DateTime
        /// </returns>
        public DataTable EnumApplicablePolicies(SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EnumApplicablePolicies", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                // create the data table containing the results
                DataTable applicablePolicies = new DataTable();
                applicablePolicies.Locale = CultureInfo.InvariantCulture;
                applicablePolicies.Columns.Add("Name", typeof(System.String));
                applicablePolicies.Columns.Add("PolicyCategory", typeof(System.String));
                applicablePolicies.Columns.Add("Effective", typeof(System.Boolean));
                applicablePolicies.Columns.Add("AutomatedPolicyEvaluationMode", typeof(AutomatedPolicyEvaluationMode));
                applicablePolicies.Columns.Add("PolicyEffectiveState", typeof(PolicyEffectiveState));
                applicablePolicies.Columns.Add("PolicyHealthState", typeof(PolicyHealthState));
                applicablePolicies.Columns.Add("LastExecutionTime", typeof(DateTime));

                try
                {
                    if (null == target)
                    {
                        throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                    }

                    DataTable policiesTable = GetPoliciesHealthStateTable(target);
                    // there is potential for a race condition here, because we 
                    // want the query results and the collection to be in sync, but 
                    // we cannot avoid it. We will attempt to mitigate later in the code.
                    this.Policies.Refresh();

                    foreach (DataRow dr in policiesTable.Rows)
                    {
                        Policy policy = this.Policies[dr[nameColIndex].ToString()];
                        bool isApplicable = false;
                        if (null == policy)
                        {
                            // the policy can only be null if we have a mismatch between
                            // the query and the collection, so we will skip this record
                            // because if it's not in the collection it means it's already 
                            // been deleted
                            continue;
                        }

                        PolicyHealthState policyHealthState;
                        // the bit is flipped in the health table
                        if (Convert.ToBoolean(dr[resultColIndex]))
                        {
                            policyHealthState = PolicyHealthState.Critical;
                        }
                        else
                        {
                            // if the result is 0 this means that no policy 
                            // execution was able to detect problems with this 
                            // target, but it could also be that no policy
                            // was ever run on the target, hence the status is unknown
                            policyHealthState = PolicyHealthState.Unknown;
                        }

                        PolicyEffectiveState effectiveStatus = PolicyEffectiveState.None;
                        // looping over the target filters
                        if (IsPolicyApplicableForGivenTarget(policy, target))
                        {
                            isApplicable = true;
                            if (IsTargetInCategory(policy.PolicyCategory, target))
                            {
                                effectiveStatus |= PolicyEffectiveState.InCategory;
                            }

                            // See if the object matches the TargetSet
                            if (policy.IsInTargetSet(policy.Parent.SqlStoreConnection, target, false))
                            {
                                effectiveStatus |= PolicyEffectiveState.InFilter;
                            }

                            if (policy.Enabled)
                            {
                                effectiveStatus |= PolicyEffectiveState.Enabled;
                            }
                        }

                        bool isEffective = (effectiveStatus == (PolicyEffectiveState.Enabled |
                                            PolicyEffectiveState.InFilter |
                                            PolicyEffectiveState.InCategory));

                        object lastRunDate = dr[lastRunDateColIndex];
                        if (!(lastRunDate is DBNull))
                        {
                            lastRunDate = (DateTime)dr[lastRunDateColIndex];
                        }

                        //Applicable through filter
                        if (isApplicable)
                        {
                            applicablePolicies.Rows.Add(
                            policy.Name,                        // Name
                            policy.PolicyCategory,                 // PolicyCategory
                            isEffective,                        // Effective
                            policy.AutomatedPolicyEvaluationMode,// AutomatedPolicyEvaluationMode
                            effectiveStatus,                    // PolicyEffectiveState
                            policyHealthState,                  // PolicyHealthState
                            lastRunDate                         // LastExecutionTime
                            );
                        }
                    }
                }
                catch (Exception e)
                {
                    methodTraceContext.TraceCatch(e);
                    if (!Utils.ShouldProcessException(e))
                    {
                        // If the exception is unrecoverable we have to let it go
                        throw;
                    }

                    throw traceContext.TraceThrow(new FailedOperationException(
                        ExceptionTemplatesSR.EnumApplicablePolicies,
                        string.Empty,
                        this.GetType().Name,
                        e));
                }
                methodTraceContext.TraceParameterOut("returnVal", applicablePolicies);
                return applicablePolicies;
            }
        }



        /// <summary>
        /// This function returns a DataTable that contains information about
        /// the health state of all policies relative to one single target.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private DataTable GetPoliciesHealthStateTable(SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetPoliciesHealthStateTable"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                // transform the target from a filter which is based on 
                // string properties into a id-based filter
                SMO.SqlSmoObject smoTarget = Policy.GetOneTarget(this.SqlStoreConnection, target) as SMO.SqlSmoObject;
                if (null == smoTarget)
                {
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyOneTarget(target.ToString())));
                }

                string lookupUrn = smoTarget.UrnOnlyId;

                string policiesQuery = String.Format(policyHealthQuery,
                    SfcTsqlProcFormatter.EscapeString(EscapeLikePattern(lookupUrn), '\''));
                DataTable policiesTable = this.SqlStoreConnection.ServerConnection.ExecuteWithResults(policiesQuery).Tables[0];
                methodTraceContext.TraceParameterOut("returnVal", policiesTable);
                return policiesTable;
            }
        }

        // indexes to access the query results. Must be kept in sync 
        // with the query
        const int nameColIndex = 0;
        const int resultColIndex = 1;
        const int lastRunDateColIndex = 2;

        // this is the query used to retrieve policies and their health 
        // information; the first parameter is the target query expression
        const string policyHealthQuery =
                        @"select p.name as [Name], 
                            case when exists (select * from msdb.dbo.syspolicy_system_health_state h where h.policy_id = p.policy_id and h.target_query_expression_with_id like '{0}%' ESCAPE '\' ) then 1 else 0 end as [PolicyHealthState],
                            (select top 1 end_date from msdb.dbo.syspolicy_policy_execution_history h2 where h2.policy_id = p.policy_id ) as [LastExecutionTime]
                        from msdb.dbo.syspolicy_policies p";

        // this is the query used to retrieve aggregated health state
        // when we do not need to join with targets, i.e. when we want the health 
        // state for all tables 
        const string aggregatedStateQuery =
            @"SELECT CASE WHEN EXISTS (SELECT * FROM msdb.dbo.syspolicy_system_health_state s WHERE s.target_query_expression_with_id LIKE @target_path_mask ESCAPE '\')  THEN 1 ELSE 0 END";

        /// <summary>
        /// Returns the aggregated health information of the target query, which 
        /// can describe either a single object or a collection of objects.
        /// </summary>
        /// <param name="target"></param>
        /// <returns>If there is a violation for this object or one of its descendents
        /// we will return Critical, otherwise Unknown.</returns>
        public PolicyHealthState GetAggregatedHealthState(SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetAggregatedHealthState", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                if (!this.Enabled)
                {
                    methodTraceContext.TraceParameterOut("returnVal", PolicyHealthState.Unknown);
                    return PolicyHealthState.Unknown;
                }

                try
                {
                    if (null == target)
                    {
                        throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                    }

                    if (!target.ToString().StartsWith("Server", StringComparison.Ordinal))
                    {
                        methodTraceContext.TraceParameterOut("returnVal", PolicyHealthState.Unknown);
                        return PolicyHealthState.Unknown;
                    }

                    // see if the query expression points to an object for which 
                    // we can calculate the health state
                    RequestObjectInfo roi = new RequestObjectInfo(target.ToString(), RequestObjectInfo.Flags.Properties);
                    ObjectInfo oi = new Enumerator().Process(this.SqlStoreConnection.ServerConnection, roi);
                    bool hasPolicyHealthState = false;
                    foreach (ObjectProperty op in oi.Properties)
                    {
                        hasPolicyHealthState |= (op.Name == "PolicyHealthState");
                    }
                    if (!hasPolicyHealthState)
                    {
                        // we don't calculate health state for this type of 
                        // object so return Unknown
                        methodTraceContext.TraceParameterOut("returnVal", PolicyHealthState.Unknown);
                        return PolicyHealthState.Unknown;
                    }

                    Smo.Server server = new Smo.Server(this.SqlStoreConnection.ServerConnection);
                    SfcObjectQuery policyQuery = new SfcObjectQuery(server);

                    if (target.Expression[target.Expression.Length - 1].Filter == null)
                    {
                        // now we need to transform this query so that it contains 
                        // ID instead of names as an object identifier

                        string queryWithId = string.Empty;
                        // special case for Server as there is no parent
                        if (target.Expression.Length == 1 && target.Expression[0].Name == "Server")
                        {
                            queryWithId = "Server";
                        }
                        else
                        {
                            SfcQueryExpression parent = new SfcQueryExpression(new Urn(target.ToString()).Parent.ToString());

                            foreach (SMO.SqlSmoObject obj in policyQuery.ExecuteIterator(parent, null, null))
                            {
                                if (queryWithId.Length > 0)
                                {
                                    // so we tried to optimize, but it does not work because the 
                                    // parent query is returning multiple objects
                                    // in this case fall back to the join via enumerator
                                    return GetAggregatedHealthStateWithFilter(target, policyQuery);
                                }

                                string targetType = target.Expression[target.Expression.Length - 1].Name;
                                queryWithId = obj.UrnOnlyId + "/" + targetType;
                                // User is an ambiguous suffix in some cases, so we need to add an 
                                // extra filter. Note that we can't just add '[' to the 
                                // pattern all the time because singletons won't satisfy it
                                // and enumerator does not offer info on what paths are singletons
                                if (targetType == "User")
                                {
                                    queryWithId += "[";
                                }
                            }

                            if (queryWithId.Length == 0)
                            {
                                // the query will not return any objects
                                methodTraceContext.TraceParameterOut("returnVal", PolicyHealthState.Unknown);
                                return PolicyHealthState.Unknown;
                            }
                        }

                        // now do a direct select from the system health table, it is faster
                        // than the join via enumerator

                        SqlCommand cmd = new SqlCommand(aggregatedStateQuery);
                        cmd.Connection = this.SqlStoreConnection.ServerConnection.SqlConnectionObject;
                        SqlParameter param = new SqlParameter("@target_path_mask",
                        SfcTsqlProcFormatter.EscapeString(EscapeLikePattern(queryWithId), '\'') + "%");
                        cmd.Parameters.Add(param);

                        return (PolicyHealthState)cmd.ExecuteScalar();
                    }
                    else
                    {
                        return GetAggregatedHealthStateWithFilter(target, policyQuery);
                    }
                }
                catch (Exception e)
                {
                    methodTraceContext.TraceCatch(e);
                    if (!Utils.ShouldProcessException(e))
                    {
                        // If the exception is unrecoverable we have to let it go
                        throw;
                    }

                    throw traceContext.TraceThrow(new FailedOperationException(
                        ExceptionTemplatesSR.GetAggregatedHealthState,
                        string.Empty,
                        this.GetType().Name,
                        e));
                }

            }
        }

        /// <summary>
        /// Returns aggregated health state information for a query that contains 
        /// a filter on the last level.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="policyQuery"></param>
        /// <returns></returns>
        private PolicyHealthState GetAggregatedHealthStateWithFilter(SfcQueryExpression target, SfcObjectQuery policyQuery)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetAggregatedHealthStateWithFilter"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, policyQuery);
                // we can't process expressions that already filters 
                // on health state
                if (target.ToString().Contains("PolicyHealthState"))
                {
                    throw new ArgumentException("target",
                        ExceptionTemplatesSR.UnsupportedTqeProp(target.ToString(), "PolicyHealthState"));
                }

                // special case for Server - enumerator's semantics do not
                // permit filtering on server properties so we need to go and 
                // get the value directly
                if (target.Expression.Length == 1 && target.Expression[0].Name == "Server")
                {
                    DataTable dt = policyQuery.ExecuteDataTable(
                        target, new string[] { "PolicyHealthState" }, null);
                    traceContext.DebugAssert(dt.Rows.Count == 1);
                    traceContext.DebugAssert(dt.Columns.Count == 1);
                    return (PolicyHealthState)dt.Rows[0][0];
                }

                // the general algorithm is to add the @policyHealthState=1 to 
                // the user request - that will filter out whatever the user 
                // needs originally in their filter and will only return 
                // records if any of the records are breaking any policy.
                FilterNode lastFilter = target.Expression[target.Expression.Length - 1].Filter;
                FilterNode criticalHealth = new FilterNodeOperator(FilterNodeOperator.Type.EQ,
                    new FilterNodeAttribute("PolicyHealthState"),
                    new FilterNodeConstant("1", FilterNodeConstant.ObjectType.Number));
                if (null == lastFilter)
                {
                    target.Expression[target.Expression.Length - 1].Filter = criticalHealth;
                }
                else
                {
                    target.Expression[target.Expression.Length - 1].Filter =
                        new FilterNodeOperator(
                            FilterNodeOperator.Type.And,
                            criticalHealth,
                            new FilterNodeGroup(lastFilter));
                }

                target = new SfcQueryExpression(target.Expression.ToString());

                foreach (Object o in policyQuery.ExecuteIterator(
                    target, new string[] { "PolicyHealthState" }, null))
                {
                    methodTraceContext.TraceParameterOut("returnVal", PolicyHealthState.Critical);
                    return PolicyHealthState.Critical;
                }

                methodTraceContext.TraceParameterOut("returnVal", PolicyHealthState.Unknown);
                return PolicyHealthState.Unknown;
            }
        }

        /// <summary>
        /// Escapes the pattern that is supplied into a t-sql query doing 
        /// pattern matching with the LIKE keyword.
        /// </summary>
        /// <param name="pattern"></param>
        /// <returns></returns>
        private string EscapeLikePattern(string pattern)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EscapeLikePattern"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(pattern);
                StringBuilder result = new StringBuilder(pattern.Length * 2);
                foreach (char c in pattern)
                {
                    if (c == '[' || c == ']' || c == '?' || c == '\\')
                    {
                        result.Append('\\');
                    }
                    result.Append(c);
                }

                return result.ToString();
            }
        }

        /// <summary>
        /// Returns a read-only collection containing all of the
        /// policy categories.
        /// </summary>
        public ReadOnlyCollection<PolicyCategoryInformation> EnumPolicyCategories()
        {
            List<PolicyCategoryInformation> policyCategoryInfos = new List<PolicyCategoryInformation>(this.PolicyCategories.Count);
            foreach (PolicyCategory category in this.PolicyCategories)
            {
                policyCategoryInfos.Add(new PolicyCategoryInformation(category));
            }
            policyCategoryInfos.Sort(PolicyCategoryInformation.CompareByCategoryIDPolicyName);
            return new ReadOnlyCollection<PolicyCategoryInformation>(policyCategoryInfos);
        }

        /// <summary>
        /// Returns a read-only collection containing each policy
        /// category, repeated for each of the policies in that category,
        /// and a flag if the target subscribes to the category, all of
        /// the database and database descendent policies in the
        /// category, and the execution mode of the policy. Note: this
        /// method is currently only valid on database targets.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public ReadOnlyCollection<PolicyCategoryInformation> EnumApplicablePolicyCategories(SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EnumApplicablePolicyCategories", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                List<PolicyCategoryInformation> policyCategoryInfos = new List<PolicyCategoryInformation>();

                string tSqlCommand = @"select pc.policy_category_id, pc.name as PCName, p.name as PName
                                   from msdb.dbo.syspolicy_policy_categories pc FULL OUTER JOIN msdb.dbo.syspolicy_policies p 
                                   on pc.policy_category_id = p.policy_category_id
                                   group by pc.policy_category_id, p.name, pc.name
                                   union  
                                   select null, null, null";

                DataSet dataSet = this.SqlStoreConnection.ServerConnection.ExecuteWithResults(tSqlCommand);
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    bool subscribes = false;
                    string policyCategoryName = row["PCName"] as string;
                    Policy policy = null;
                    if (row["PName"] != DBNull.Value)
                    {
                        policy = this.Policies[row["PName"] as string];
                    }

                    // if we are in the default category or if the target is not
                    // filtered out by the catgegory 
                    if (policyCategoryName == null ||
                        (target != null &&
                        IsTargetInCategory(policyCategoryName, target)))
                    {
                        subscribes = true;
                    }
                    policyCategoryInfos.Add(new PolicyCategoryInformation(
                        policyCategoryName == null ? null : this.PolicyCategories[policyCategoryName],
                        policy,
                        subscribes));
                }

                policyCategoryInfos.Sort(PolicyCategoryInformation.CompareByCategoryIDPolicyName);
                return new ReadOnlyCollection<PolicyCategoryInformation>(policyCategoryInfos);
            }
        }


        /// <summary>
        /// This function checks to see if the current target belongs to the category 
        /// It is used to define effective policies
        /// </summary>
        /// <param name="target"></param>
        /// <param name="policyCategory"></param>
        /// <returns></returns>
        internal bool IsTargetInCategory(string policyCategory, SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("IsTargetInCategory"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policyCategory, target);
                if (string.IsNullOrEmpty(policyCategory))
                {
                    // empty string means that the policy is subscribed to the 
                    // default category, so in this case all targets will do
                    methodTraceContext.TraceParameterOut("returnVal", true);
                    return true;
                }

                if (null == target)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("targetFilter"));
                }

                PolicyCategory pc = this.PolicyCategories[policyCategory];
                if (null == pc)
                {
                    throw methodTraceContext.TraceThrow(new MissingObjectException(ExceptionTemplatesSR.Category, policyCategory));
                }

                if (pc.MandateDatabaseSubscriptions)
                {
                    methodTraceContext.TraceParameterOut("returnVal", true);
                    return true;
                }

                // If Category is MandateDatabaseSubscriptions, need to intersect with subscriptions

                if (!target.ExpressionSkeleton.StartsWith(TargetSet.DatabaseLevel, StringComparison.Ordinal))
                {
                    // target is neither DB nor under DB
                    methodTraceContext.TraceParameterOut("returnVal", true);
                    return true;
                }

                if (null == target.Expression[1].Filter)
                {
                    // there is no filter on DB level - all databases
                    methodTraceContext.TraceParameterOut("returnVal", true);
                    return true;
                }

                ExpressionNode dbfilter = ExpressionNode.ConvertFromFilterNode(target.Expression[1].Filter);

                // iterate through all the subscriptions to see if we find one for our database
                foreach (PolicyCategorySubscription sub in this.PolicyCategorySubscriptions)
                {
                    if (sub.PolicyCategory == policyCategory &&
                        sub.TargetType == "DATABASE" &&
                        sub.Target == dbfilter.ObjectName)
                    {
                        methodTraceContext.TraceParameterOut("returnVal", true);
                        return true;
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", false);
                return false;
            }
        }

        /// <summary>
        /// Creates a subscription for the provided target to the
        /// specified policy category. This will throw if the
        /// subscription already exists.
        /// </summary>
        /// <param name="target">An expression that SfcObjectQuery will
        /// evaluate to a specific instance.</param>
        /// <param name="policyCategory">The category name to subscribe to.</param>
        /// <returns>PolicyCategorySubscription</returns>
        public PolicyCategorySubscription SubscribeToPolicyCategory(SfcQueryExpression target, string policyCategory)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("SubscribeToPolicyCategory", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, policyCategory);
                if (target == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                }

                string[] fields = { "Urn" };
                DataTable dt = this.SmoObjectQuery.ExecuteDataTable(target, fields, null);
                if (dt.Rows.Count == 0)
                {
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyOneTarget(target.ToString())));
                }
                PolicyCategorySubscription sub = new PolicyCategorySubscription(this, target, policyCategory);
                sub.Create();
                methodTraceContext.TraceParameterOut("returnVal", sub);
                return sub;
            }
        }

        /// <summary>
        /// Unsubscribe the provided target from the specified policy
        /// category. This will throw if the target does not subscribe to
        /// the category.
        /// </summary>
        /// <param name="target">An expression that SfcObjectQuery will
        /// evaluate to a specific instance.</param>
        /// <param name="policyCategory">The category name to unsubscribe from.</param>
        /// <returns>void</returns>
        public void UnsubscribeFromPolicyCategory(SfcQueryExpression target, string policyCategory)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("UnsubscribeFromPolicyCategory", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, policyCategory);
                if (target == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                }

                PolicyCategorySubscription sub = new PolicyCategorySubscription(this, target, policyCategory);

                foreach (PolicyCategorySubscription existingSub in this.PolicyCategorySubscriptions)
                {
                    if ((sub.TargetType == existingSub.TargetType) &&
                        (sub.Target == existingSub.Target) &&
                        (sub.PolicyCategory == existingSub.PolicyCategory))
                    {
                        existingSub.Drop();
                        return;
                    }
                }
                throw methodTraceContext.TraceThrow(new DmfException(String.Format(ExceptionTemplatesSR.PolicyCategoryNotSubscribed, target, policyCategory)));
            }
        }

        #region CREATE POLICY FROM FACET

        private object GetTarget(SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetTarget"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target);
                IEnumerable targets = null;
                string targetString = target.ToString();
                string[] fields = null;
                // If the target is a smo object, we have to query
                // against SMO
                if (targetString.StartsWith("Server", StringComparison.Ordinal) && ((targetString.Length == 6) || (targetString[6] == '[') || (targetString[6] == '/')))
                {
                    targets = this.SmoObjectQuery.ExecuteIterator(target, fields, null);
                }
                else
                {
                    // I don't think this is reachable since only SMO target are possible. We should just assert tha the name starts with Server -- it should be validated already
                    targets = this.SfcObjectQuery.ExecuteIterator(target, fields, null);
                }

                // Confirm there is only one target
                Object targetObj = null;
                foreach (Object obj in targets)
                {
                    if (targetObj != null)
                    {
                        throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyOneTarget(target.ToString())));
                    }
                    targetObj = obj;
                }

                // Must match at least one target
                if (null == targetObj)
                {
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyOneTarget(target.ToString())));
                }

                methodTraceContext.TraceParameterOut("returnVal", targetObj);
                return targetObj;
            }
        }

        private PolicyStore CreateVirtualStore()
        {
            PolicyStore ps;

            ps = new PolicyStore();

            return ps;
        }

        /// <summary>
        /// This method instantiates a Policy object in memory which
        /// applies to the given target. The policy is
        /// associated with a Condition whose ConditionExpression
        /// objects reflect the properties of the target as seen
        /// through the given facet at the time this method is
        /// called. None of these objects have had Create called on
        /// them yet so they are only in memory. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="facetName"></param>
        /// <param name="policyName"></param>
        /// <param name="conditionName"></param>
        /// <param name="objectsToValidate"></param>
        internal Policy GeneratePolicyFromFacet(object target, string facetName, string policyName, string conditionName, List<ObjectToValidate> objectsToValidate)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GeneratePolicyFromFacet"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, facetName, policyName, conditionName, objectsToValidate);
                Type facetType = FacetRepository.GetFacetType(facetName);
                if (null == facetType)
                {
                    throw methodTraceContext.TraceThrow(new NullFacetException(facetName));
                }

                // non-null objectToValidate indicates we save to the store
                if (null != objectsToValidate)
                {
                    // Validate names
                    if (Policies.Contains(policyName))
                    {
                        throw methodTraceContext.TraceThrow(new ObjectAlreadyExistsException(ExceptionTemplatesSR.Policy, policyName));
                    }
                    if (Conditions.Contains(conditionName))
                    {
                        throw methodTraceContext.TraceThrow(new ObjectAlreadyExistsException(ExceptionTemplatesSR.Condition, conditionName));
                    }
                }

                FacetEvaluationContext facet = FacetEvaluationContext.GetFacetEvaluationContext(facetType, target);

                Policy policy = new Policy(this, policyName);
                Condition cond = new Condition(this, conditionName);
                ObjectSet os = null;

                cond.Facet = facetName;
                if ((target is SMO.SqlSmoObject) || (target is SfcInstance))
                {
                    // Generate unique name for ObjectSet 
                    // in case we save to the store it will prevent failure
                    // It won't hurt if we save to file
                    os = new ObjectSet(this, ObjectSet.GenerateUniqueName(policy));
                    os.Facet = facetName;
                    policy.ObjectSet = os.Name;
                }

                policy.Condition = cond.Name;

                // Foreach property on the facet, add an ExpressionNodeOperator
                // that checks equality between the property name and
                // its value at this moment.
                ExpressionNode node = null;

                PropertyInfo[] props = FacetRepository.GetFacetProperties(facetType);
                PropertyInfoNameComparer pic = new PropertyInfoNameComparer();
                Array.Sort(props, pic);

                foreach (PropertyInfo pi in props)
                {
                    string name = pi.Name;

                    try
                    {
                        object propValue = facet.GetPropertyValue(name);

                        if (null != propValue)
                        {
                            try
                            {
                                // SMO may have one type in property bag
                                // and another type returned by custom implementation of the property
                                // (most common case Enums, stored as int, then cast to Enum)

                                if (propValue.GetType() != pi.PropertyType)
                                {
                                    if (pi.PropertyType.IsEnum)
                                    {
                                        propValue = Enum.Parse(pi.PropertyType, propValue.ToString());
                                    }
                                    else
                                    {
                                        propValue = Convert.ChangeType(propValue, pi.PropertyType, CultureInfo.InvariantCulture);
                                    }
                                }
                            }
                            catch (System.InvalidCastException ice)
                            {
                                methodTraceContext.TraceCatch(ice);
                                // This shouldn't happen
                                // If it does we want to know about it
                                // We may choose to handle that particular type or look at the property

                                traceContext.DebugAssert(true, ice.Message);
                                propValue = null;
                            }
                            if (null != propValue) // Invalid cast might make it null again
                            {

                                ExpressionNode curNode = new ExpressionNodeOperator(OperatorType.EQ,
                                                                                    new ExpressionNodeAttribute(name, facetType),
                                                                                    ExpressionNode.ConstructNode(propValue));
                                // Since there is only one ConditionExpression per
                                // condition, we build up a bigger and bigger
                                // ExpressionNode that concatenates all of the
                                // ExpressionNodeOperators together.
                                if (null == node)
                                {
                                    node = curNode;
                                }
                                else
                                {
                                    node = new ExpressionNodeOperator(OperatorType.AND,
                                                                       node,
                                                                       curNode);
                                }
                            }
                        }
                    }
                    catch (NonRetrievablePropertyException)
                    {
                        methodTraceContext.TraceError("Caught a general Exception of type NonRetrievablePropertyException");
                        continue; //A Condition is created on a Facet based on the available properties. Un-available properties are ignored.
                    }
                    catch (MissingPropertyException)
                    {
                        methodTraceContext.TraceError("Caught a general Exception of type MissingPropertyException");
                        continue; //A property not available because it does not exist in the down-level object from which we are creating this facet.
                    }
                }
                cond.ExpressionNode = node;

                this.Conditions.Add(cond);
                if (null != objectsToValidate)
                {
                    objectsToValidate.Add(new ObjectToValidate(cond, typeof(Condition), ValidationMethod.Create));
                }

                if (os != null)
                {
                    this.ObjectSets.Add(os);
                    if (null != objectsToValidate)
                    {
                        objectsToValidate.Add(new ObjectToValidate(os, typeof(ObjectSet), ValidationMethod.Create));
                    }
                }

                this.Policies.Add(policy);
                if (null != objectsToValidate)
                {
                    objectsToValidate.Add(new ObjectToValidate(policy, typeof(Policy), ValidationMethod.Create));
                }

                SfcQueryExpression qe = null;
                // find out if the target object has a query expression
                if (target is SMO.SqlSmoObject)
                {
                    qe = new SfcQueryExpression((target as SMO.SqlSmoObject).Urn);
                }
                else if (target is SfcInstance)
                {
                    qe = new SfcQueryExpression((target as SfcInstance).Urn);
                }

                if ((null != qe) && (null != os))
                {
                    // if we have a valid query expression then we should 
                    // enable the corresponding target set
                    if (!os.TargetSets.Contains(qe.ExpressionSkeleton))
                    {
                        throw traceContext.TraceThrow(new ConflictingPropertyValuesException(ValidationMethod.Create,
                            ExceptionTemplatesSR.ManagementFacet, facetName,
                            ExceptionTemplatesSR.TargetSet, qe.ExpressionSkeleton));
                    }
                    os.TargetSets[qe.ExpressionSkeleton].Enabled = true;
                }

                ((ISfcCollection)(this.Conditions)).Initialized = true;
                ((ISfcCollection)(this.ObjectSets)).Initialized = true;
                ((ISfcCollection)(this.Policies)).Initialized = true;

                methodTraceContext.TraceParameterOut("returnVal", policy);
                return policy;
            }
        }

        /// <summary>
        /// This method does the actual creation of the policy from the facet.
        /// This method does validation of the created policy regardless of whether the policy is created
        /// to be persisted in the store, or just to be serialized.
        /// 
        /// If the caller requested committing the policy, it means the policy is to be
        /// created in the current policy store backend, otherwise, a temp store is created the policy 
        /// and the policy object and temp store are discareded after the policy is serialized. 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="facetName"></param>
        /// <param name="policyName"></param>
        /// <param name="conditionName"></param>
        /// <param name="writer"></param>
        /// <param name="commit">If true, the caller requires the policy to be committed to the policy store.</param>
        private void DoCreatePolicyFromFacet(Object target, string facetName, string policyName, string conditionName, XmlWriter writer, bool commit)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("DoCreatePolicyFromFacet", System.Diagnostics.TraceEventType.Information))
            {
                PolicyStore ps = commit ? this : this.CreateVirtualStore(); //if we are not committing to the current store, get a virtual temp one.
                List<ObjectToValidate> objectsToValidate = new List<ObjectToValidate>();

                try
                {
                    Policy policy = ps.GeneratePolicyFromFacet(target, facetName, policyName, conditionName, objectsToValidate);

                    ObjectToValidate.ValidateObjects(objectsToValidate);

                    if (commit)
                    {
                        ObjectToValidate.CommitObjects(this, objectsToValidate);
                    }
                    else
                    {  //we will serialize the policy to the writer without committing
                        methodTraceContext.Assert(null != writer, "Create policy from facet requested without a valid xml writer.");

                        policy.Serialize(writer);
                    }
                }
                catch (Exception ex)
                {
                    methodTraceContext.TraceCatch(ex);
                    if (!Utils.ShouldProcessException(ex))
                    {
                        throw;
                    }

                    // We haven't committed any changes yet - simply refresh affected objects and collections

                    if (commit)
                    {
                        ObjectToValidate.RestoreCollections(this, objectsToValidate);
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// This method instantiates a Policy object which applies to
        /// the given target, and then add the policy to permanent
        /// storage. The policy is associated with a Condition whose
        /// ConditionExpression objects reflect the properties of the
        /// target as seen through the given facet at the time this
        /// method is called. You can access the newly created policy
        /// through this PolicyStore's Policies[policyName] collection.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="facetName"></param>
        /// <param name="policyName"></param>
        /// <param name="conditionName"></param>
        public void CreatePolicyFromFacet(SfcQueryExpression target, string facetName, string policyName, string conditionName)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CreatePolicyFromFacet", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, facetName, policyName, conditionName);
                if (null == target)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                }
                if (String.IsNullOrEmpty(facetName))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("facetName")));
                }
                if (String.IsNullOrEmpty(policyName))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("policyName")));
                }
                if (String.IsNullOrEmpty(conditionName))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("conditionName")));
                }

                object targetObj = GetTarget(target);
                this.DoCreatePolicyFromFacet(targetObj, facetName, policyName, conditionName, null, true);
            }
        }

        /// <summary>
        /// This method instantiates a temporary Policy object and
        /// then serializes it through the given XmlWriter. The policy
        /// is associated with a Condition whose ConditionExpression
        /// objects reflect the properties of the target as seen
        /// through the given facet at the time this method is called.
        /// This PolicyStore object is not changed by this call.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="facetName"></param>
        /// <param name="policyName"></param>
        /// <param name="conditionName"></param>
        /// <param name="writer"></param>
        public void CreatePolicyFromFacet(SfcQueryExpression target, string facetName, string policyName, string conditionName, XmlWriter writer)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CreatePolicyFromFacet", System.Diagnostics.TraceEventType.Information))
            {
                if (null == target)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                }
                object targetObj = GetTarget(target);

                this.CreatePolicyFromFacet(targetObj, facetName, policyName, conditionName, writer);
            }
        }

        /// <summary>
        /// This method instantiates a Policy object which applies to
        /// the given target, and then add the policy to permanent
        /// storage. The policy is associated with a Condition whose
        /// ConditionExpression objects reflect the properties of the
        /// target as seen through the given facet at the time this
        /// method is called. You can access the newly created policy
        /// through this PolicyStore's Policies[policyName] collection.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="facetName"></param>
        /// <param name="policyName"></param>
        /// <param name="conditionName"></param>
        /// <param name="writer"></param>
        public void CreatePolicyFromFacet(object target, string facetName, string policyName, string conditionName, XmlWriter writer)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CreatePolicyFromFacet", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(target, facetName, policyName, conditionName, writer);
                if (null == target)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("target"));
                }
                if (String.IsNullOrEmpty(facetName))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("facetName")));
                }
                if (String.IsNullOrEmpty(policyName))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("policyName")));
                }
                if (String.IsNullOrEmpty(conditionName))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("conditionName")));
                }
                if (writer == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("writer"));
                }

                this.DoCreatePolicyFromFacet(target, facetName, policyName, conditionName, writer, false);
            }
        }

        #endregion CREATE POLICY FROM FACET

        #region IMPORT & DESERIALIZE
        /// <summary>
        /// Import a single policy from the given XML reader. The reader should be previously created by a supported Export method.
        /// The imported Policy is also created in the PolicyStore on the server.
        /// </summary>
        /// <param name="xmlReader">The XML reader to import from.</param>
        /// <param name="importEnabledState">The Policy.Enabled flag will be set based on this
        /// enum value. The exception is if the policy's execution mode is Check on Schedule but
        /// there is no schedule information associated with the policy, in which case the policy
        /// is automatically disabled and its execution mode is reset to None.</param>
        /// <param name="overwriteExistingPolicy">If true, then if a Policy of the same name already
        /// exists in the Store then it will be overwritten.</param>
        /// <param name="overwriteExistingCondition">If true, then if any Condition associated
        /// with the Policy, either directly or through a TargetSet, already exists in the Store
        /// then it will be overwritten. Otherwise the existing condition will remain unchanged
        /// and the imported policy will use the pre-existing condition rather than the version in
        /// the XmlReader.</param>
        /// <returns>The Policy object imported. If the XML reader fails to validate properly or is missing it will throw an exception.</returns>
        public Policy ImportPolicy(XmlReader xmlReader, ImportPolicyEnabledState importEnabledState, bool overwriteExistingPolicy, bool overwriteExistingCondition)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ImportPolicy", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(importEnabledState, overwriteExistingPolicy, overwriteExistingCondition);
                Policy policy = DeserializePolicy(xmlReader, overwriteExistingPolicy, overwriteExistingCondition, true, importEnabledState);

                methodTraceContext.TraceParameterOut("returnVal", policy);
                return policy;
            }
        }

        /// <summary>
        /// Deserialize a single policy from the given XML reader. The reader should be previously created by a supported Export method.
        /// The deserialized Policy exists in-memory only and must be created in the PolicyStore on the server to persist it.
        /// </summary>
        /// <param name="xmlReader">The XML reader to import from.</param>
        /// <param name="overwriteExistingPolicy">If true, then if a Policy of the same name already
        /// exists in the Store then it will be overwritten.</param>
        /// <param name="overwriteExistingCondition">If true, then if any Condition associated
        /// with the Policy, either directly or through a TargetSet already exists in the Store
        /// then it will be overwritten.</param>
        /// <returns>The Policy object imported. If the XML reader fails to validate properly it may throw an exception.</returns>
        public Policy DeserializePolicy(XmlReader xmlReader, bool overwriteExistingPolicy, bool overwriteExistingCondition)
        {
            return DeserializePolicy(xmlReader, overwriteExistingPolicy, overwriteExistingCondition, false, ImportPolicyEnabledState.Unchanged);
        }

        internal Policy DeserializePolicy(XmlReader xmlReader, bool overwriteExistingPolicy, bool overwriteExistingCondition, bool import, ImportPolicyEnabledState importEnabledState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("DeserializePolicy"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(xmlReader, overwriteExistingPolicy, overwriteExistingCondition, import, importEnabledState);
                if (null == xmlReader)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("xmlReader"));
                }

                Policy policy;
                List<ObjectToValidate> objectsToValidate = new List<ObjectToValidate>();

                try
                {
                    policy = InstantiateObjectsFromReader(xmlReader, overwriteExistingPolicy, overwriteExistingCondition, objectsToValidate);

                    if (null == policy)
                    {  //serialization succeeded, but no policy was found
                        throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.PolicyImportFileDoesNotHaveAnyPolicy));
                    }

                    if (import)
                    {
                        switch (importEnabledState)
                        {
                            case ImportPolicyEnabledState.Unchanged:
                                break;
                            case ImportPolicyEnabledState.Enable:
                                policy.Enabled = true;
                                break;
                            case ImportPolicyEnabledState.Disable:
                                policy.Enabled = false;
                                break;
                        }

                        // we need to take special action for CoS - create the schedule 
                        // automatically from properties of the Policy object
                        if (policy.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnSchedule)
                        {
                            PolicyScheduleHelper.FixPolicySchedule(policy);
                        }
                        else //otherwise, remove any references to a schedule in the policy, since it is not needed
                        {
                            policy.Schedule = null;
                            policy.ScheduleUid = Guid.Empty;
                        }
                    }

                    ObjectToValidate.ValidateObjects(objectsToValidate);

                    if (import)
                    {
                        ObjectToValidate.CommitObjects(this, objectsToValidate);
                    }

                    methodTraceContext.TraceParameterOut("returnVal", policy);
                    return policy;
                }
                catch (Exception ex)
                {
                    methodTraceContext.TraceCatch(ex);
                    if (!Utils.ShouldProcessException(ex))
                    {
                        throw;
                    }

                    // We haven't committed any changes yet - simply refresh affected objects and collections

                    ObjectToValidate.RestoreCollections(this, objectsToValidate);

                    if (ex is SfcSerializationException)
                    {
                        throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.SinglePolicyDeserializationFailed, ex));
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        internal Policy InstantiateObjectsFromReader(XmlReader xmlReader, bool overwriteExistingPolicy, bool overwriteExistingCondition,
            List<ObjectToValidate> objectsToValidate)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("InstantiateObjectsFromReader"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(xmlReader, overwriteExistingPolicy, overwriteExistingCondition, objectsToValidate);
                ReadOnlyCollection<string> referencedConditions = null;

                Policy policy = null;

                SfcSerializer serializer = new SfcSerializer();
                object root = null;
                if (((ISfcHasConnection)this).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    // if we are offline all objects are coming from the 
                    // backing store therefore their state is Existing
                    root = serializer.Deserialize(xmlReader, SfcObjectState.Existing);
                }
                else
                {
                    root = serializer.Deserialize(xmlReader);
                }
                IList<object> rootList = serializer.UnParentedReferences;

                Policy existingPolicy = null;
                ObjectSet existingObjectSet = null;
                string existingObjectSetName = null;

                // Exactly one Policy and any number of Conditions must exist in the file
                // We need to add them to Parent ourselves as deserialization would not

                if (root is Policy)
                {
                    Policy p2 = (Policy)root;
                    if (policy == null)
                    {
                        policy = p2;
                        policy.Parent = this;
                        if (Policies.Contains(policy.IdentityKey))
                        {
                            if (!overwriteExistingPolicy)
                            {
                                throw methodTraceContext.TraceThrow(new ObjectAlreadyExistsException(ExceptionTemplatesSR.Policy, policy.Name));
                            }

                            existingPolicy = Policies[policy.IdentityKey];
                            existingObjectSetName = existingPolicy.ObjectSet;

                            Utils.ReplaceSfcProperties(existingPolicy, policy);

                            policy = existingPolicy;

                            objectsToValidate.Add(new ObjectToValidate(existingPolicy, typeof(Policy), ValidationMethod.Alter));
                        }
                        else
                        {
                            Policies.Add(policy);
                            objectsToValidate.Add(new ObjectToValidate(policy, typeof(Policy), ValidationMethod.Create));
                        }
                    }
                    else
                    {
                        throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.SinglePolicyExpected(
                                                                                         policy != null && policy.Name != null ? policy.Name : ExceptionTemplatesSR.UnknownPolicy,
                                                                                         p2 != null && p2.Name != null ? p2.Name : ExceptionTemplatesSR.UnknownPolicy)));
                    }
                }

                foreach (SfcInstance sfcObj in rootList)
                {
                    if (sfcObj is ObjectSet)
                    {
                        // !!!
                        // This code assumes 1 - {0;1} relationship between Policy and ObjectSet

                        ObjectSet deserializedObjectSet = (ObjectSet)sfcObj;
                        if (string.Compare(deserializedObjectSet.Name, policy.ObjectSet, StringComparison.Ordinal) != 0)
                        {
                            // TODO: Change this to referenced objectset expected
                            throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ReferencedObjectExpected(ExceptionTemplatesSR.ObjectSet, deserializedObjectSet.Name)));
                        }

                        deserializedObjectSet.Parent = this;

                        existingObjectSet = ObjectSets[deserializedObjectSet.IdentityKey];

                        if (existingObjectSet != null)
                        {
                            // Since ObjectSets are controlled by Policies, overwrite along with Policy
                            if (overwriteExistingPolicy)
                            {
                                if (String.IsNullOrEmpty(existingObjectSetName))
                                {
                                    // The policy we overwrite didn't have ObjectSet, so we're overwriting somebody else's
                                    // (assuming we can't have 'free' ObjectSets)
                                    throw methodTraceContext.TraceThrow(new ObjectAlreadyExistsException(ExceptionTemplatesSR.ObjectSet, existingObjectSet.Name));
                                }

                                // FUTURE: we could ommit drop/create if we know OS doesn't change
                                // need to implement Equals on ObjectSet
                                ObjectSet objectSetToDrop = existingObjectSet;
                                objectsToValidate.Add(new ObjectToValidate(objectSetToDrop, typeof(ObjectSet), ObjectToValidate.Drop));
                                existingObjectSet = deserializedObjectSet;
                                objectsToValidate.Add(new ObjectToValidate(deserializedObjectSet, typeof(ObjectSet), ValidationMethod.Create));
                                referencedConditions = (existingObjectSet).EnumReferencedConditionNames();
                            }
                            else
                            {
                                // We treat ObjectSets differently comparing to Conditions
                                // Currently ObjectSets are not supposed to be shared and cannot just stay there
                                throw methodTraceContext.TraceThrow(new ObjectAlreadyExistsException(ExceptionTemplatesSR.ObjectSet, existingObjectSet.Name));
                            }
                        }
                        else
                        {
                            ObjectSets.Add(deserializedObjectSet);
                            objectsToValidate.Add(new ObjectToValidate(deserializedObjectSet, typeof(ObjectSet), ValidationMethod.Create));
                            referencedConditions = (deserializedObjectSet).EnumReferencedConditionNames();
                        }
                    }
                    if (sfcObj is Condition)
                    {
                        Condition cond = (Condition)sfcObj;

                        // the condition must be either referred directly by the policy
                        // or must be used by the target set
                        if (cond.Name != policy.Condition && cond.Name != policy.RootCondition
                            && !(null == referencedConditions || referencedConditions.Contains(cond.Name)))
                        {
                            throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ReferencedObjectExpected(ExceptionTemplatesSR.Condition, cond.Name)));
                        }

                        Condition existingCondition = Conditions[cond.IdentityKey];
                        if (existingCondition != null)
                        {
                            if ((existingCondition.Facet != cond.Facet) || (!existingCondition.ExpressionNode.Equals(cond.ExpressionNode)))
                            {
                                if (!overwriteExistingCondition)
                                {
                                    // If we're not overwriting and either the facets don't match or
                                    // the Expressions don't match, then we can't use this
                                    // existing condition.
                                    throw methodTraceContext.TraceThrow(new ObjectAlreadyExistsException(ExceptionTemplatesSR.Condition, cond.Name));
                                }
                            }

                            if (overwriteExistingCondition)
                            {
                                Utils.ReplaceSfcProperties(existingCondition, cond);
                            }
                            cond = existingCondition;

                            objectsToValidate.Add(new ObjectToValidate(existingCondition, typeof(Condition), ValidationMethod.Alter));
                        }
                        else
                        {
                            cond.Parent = this;
                            Conditions.Add(cond);
                            objectsToValidate.Add(new ObjectToValidate(cond, typeof(Condition), ValidationMethod.Create));
                        }
                    }
                    else if (sfcObj is PolicyCategory)
                    {
                        PolicyCategory pc = (PolicyCategory)sfcObj;
                        pc.Parent = this;
                        if (!PolicyCategories.Contains(pc.Name))
                        {
                            // Treat the category as active by default.
                            pc.MandateDatabaseSubscriptions = true;
                            PolicyCategories.Add(pc);
                            objectsToValidate.Add(new ObjectToValidate(pc, typeof(PolicyCategory), ValidationMethod.Create));
                        }
                    }
                }

                ((ISfcCollection)(this.Conditions)).Initialized = true;
                ((ISfcCollection)(this.ObjectSets)).Initialized = true;
                ((ISfcCollection)(this.Policies)).Initialized = true;
                methodTraceContext.TraceParameterOut("returnVal", policy);
                return policy;
            }
        }

        #endregion IMPORT & DESERIALIZE

        /// <summary>
        /// Function to take a policy and a target and then check whether a given policy is applicable on this target
        /// or not.
        /// </summary>
        /// <param name="policy"></param>
        /// <param name="target"></param>
        /// <returns>True if applicable, otherwise false</returns>
        private bool IsPolicyApplicableForGivenTarget(Policy policy, SfcQueryExpression target)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("IsPolicyApplicableForGivenTarget"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policy, target);
                // Get the target leaf Type name first. Sfc hides the SMO hackiness.

                string targetTypeName;

            if (target.ToString().StartsWith("Server", StringComparison.Ordinal))
            {
                // This is a SMO Urn so call the SqlSMoObject mapping from string skeleton (for ancestor context) to Type
                // Some types use a different name spelling when used in a Urn. See SMO\CodeGen\cfg.xml.
                Type t = Utils.GetTypeFromUrnSkeleton(new Urn(target.ToString()));

                targetTypeName = t.Name;
            }
            else
            {
                targetTypeName = target.GetLeafTypeName();
            }

                FacetInfo policyFacet = new FacetInfo(this.Conditions[policy.Condition].Facet);
                foreach (Type t in policyFacet.TargetTypes)
                {
                    if (string.Compare(targetTypeName, t.Name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        methodTraceContext.TraceParameterOut("returnVal", true);
                        return true;
                    }
                }
                methodTraceContext.TraceParameterOut("returnVal", false);
                return false;
            }
        }

        #region Generated Part to be Removed
        internal const string typeName = "PolicyStore";

        /// Internal key class
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class Key : DomainRootKey
        {
            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public Key()
                : base(null) // Caller has to remember to set Root!
            {
            }

            internal Key(ISfcDomain root)
                : base(root)
            {
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return this.GetType().GetHashCode();
            }

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return this == obj;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj1"></param>
            /// <param name="obj2"></param>
            /// <returns></returns>
            public new static bool Equals(object obj1, object obj2)
            {
                return (obj1 as Key) == (obj2 as Key);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public override bool Equals(SfcKey key)
            {
                return this == key;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(object obj, Key rightOperand)
            {
                if (obj == null || obj is Key)
                    return (Key)obj == rightOperand;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, object obj)
            {
                if (obj == null || obj is Key)
                    return leftOperand == (Key)obj;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, Key rightOperand)
            {
                // If both are null, or both are same instance, return true.
                if (System.Object.ReferenceEquals(leftOperand, rightOperand))
                    return true;

                // If one is null, but not both, return false.
                if (((object)leftOperand == null) || ((object)rightOperand == null))
                    return false;

                return leftOperand.IsEqual(rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(object obj, Key rightOperand)
            {
                return !(obj == rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, object obj)
            {
                return !(leftOperand == obj);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, Key rightOperand)
            {
                return !(leftOperand == rightOperand);
            }

            private bool IsEqual(Key other)
            {
                return (this.Domain == other.Domain);
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                //PolicyStore has valid name only in connected mode, so we add the Name attribute only in that case
                if (this.Domain.ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return PolicyStore.typeName;
                }
                else
                {
                    return String.Format("{0}[@Name='{1}']", PolicyStore.typeName, this.Domain.DomainInstanceName);
                }
            }

        }
        #endregion

        /// <summary>
        /// The name of the server connected to
        /// </summary>
        public string Name
        {
            get
            {
                if (this.SqlStoreConnection == null)
                    return null;
                return this.SqlStoreConnection.ServerInstance;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(Data = true)]
        public bool Enabled
        {
            get
            {
                SfcProperty p = this.Properties["Enabled"];

                return (Boolean)p.Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Enabled to: {0}", value);
                this.Properties["Enabled"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(Data = true)]
        public int HistoryRetentionInDays
        {
            get
            {
                SfcProperty p = this.Properties["HistoryRetentionInDays"];

                return (int)p.Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting HistoryRetentionInDays to: {0}", value);
                this.Properties["HistoryRetentionInDays"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(Data = true)]
        public bool LogOnSuccess
        {
            get
            {
                SfcProperty p = this.Properties["LogOnSuccess"];

                return (Boolean)p.Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting LogOnSuccess to: {0}", value);
                this.Properties["LogOnSuccess"].Value = value;
            }
        }

        /// <summary>
        /// Restores all artifacts that have been created by policy-based 
        /// management to support policy automation. 
        /// </summary>
        public void RepairPolicyAutomation()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("RepairPolicyAutomation", System.Diagnostics.TraceEventType.Information))
            {
                try
                {
                    this.SqlStoreConnection.ServerConnection.ExecuteNonQuery(
                        "EXEC msdb.dbo.sp_syspolicy_repair_policy_automation");
                }
                catch (Exception e)
                {
                    methodTraceContext.TraceCatch(e);
                    SMO.SqlSmoObject.FilterException(e);

                    throw methodTraceContext.TraceThrow(new FailedOperationException(ExceptionTemplatesSR.ConsistencyRepair, this.Name, this.GetType().Name, e));
                }
            }
        }

        /// <summary>
        /// This function erases the phantom records from the system health table
        /// by iterating through all the violations and verifying that they 
        /// correspond to an existing object.
        /// </summary>
        public void EraseSystemHealthPhantomRecords()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EraseSystemHealthPhantomRecords", System.Diagnostics.TraceEventType.Information))
            {
                int HealthStateIdIndex = 0;
                int TargetQueryExpressionWithIdIndex = 1;

                try
                {
                    List<Int64> phantoms = new List<Int64>();

                    // get all the violation records id and target query expression with id
                    DataSet violations = this.SqlStoreConnection.ServerConnection.ExecuteWithResults("SELECT health_state_id, target_query_expression_with_id FROM msdb.dbo.syspolicy_system_health_state");

                    if (violations.Tables.Count == 0)
                    {
                        return;
                    }

                    Smo.Server server = new Smo.Server(this.SqlStoreConnection.ServerConnection);
                    SfcObjectQuery policyQuery = new SfcObjectQuery(server);

                    foreach (DataRow dr in violations.Tables[0].Rows)
                    {
                        if (null == dr[TargetQueryExpressionWithIdIndex])
                        {
                            continue;
                        }

                        // Make a call to the enumerator to check if this 
                        // object still exists. If so, there will be at least 
                        // one row in the result table.
                        // Note that we will do the existance check using a 
                        // Urn with ID. Therefore we are vulnerable to ID recycling 
                        // that is the situation where a violation is recorded, then 
                        // the object is dropped and recreated and it ends up having 
                        // the same ID. The same problem exists for names, btw. 
                        // When this happens, records will not be treated as phantoms, 
                        // and users down the stream will have to reason about it 
                        // and either run the policies or delete the records manually.
                        DataTable dt = policyQuery.ExecuteDataTable(new SFC.SfcQueryExpression((string)dr[TargetQueryExpressionWithIdIndex]),
                            new string[] { "Urn" }, null);

                        if (null == dt || dt.Rows.Count == 0)
                        {
                            phantoms.Add((Int64)dr[HealthStateIdIndex]);
                        }
                    }

                    // once we have the list of phantoms we will delete them one by one
                    using (SqlCommand deleteCmd = new SqlCommand())
                    {
                        deleteCmd.Connection = this.SqlStoreConnection.ServerConnection.SqlConnectionObject;
                        deleteCmd.CommandText = "DELETE FROM msdb.dbo.syspolicy_system_health_state WHERE health_state_id = @health_state_id_param;";

                        SqlParameter paramHealthStateId = new SqlParameter("@health_state_id_param", 0);
                        paramHealthStateId.SqlDbType = SqlDbType.BigInt;
                        deleteCmd.Parameters.Add(paramHealthStateId);

                        const int paramIndex = 0;

                        foreach (Int64 healthStateId in phantoms)
                        {
                            deleteCmd.Parameters[paramIndex].Value = healthStateId;

                            deleteCmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception e)
                {
                    methodTraceContext.TraceCatch(e);
                    SMO.SqlSmoObject.FilterException(e);

                    throw methodTraceContext.TraceThrow(new FailedOperationException(ExceptionTemplatesSR.EraseSystemHealthPhantomRecords, this.Name, this.GetType().Name, e));
                }
            }
        }

        #region ISfcSerializableUpgrade Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UpgradeSession StartSerializationUpgrade()
        {
            return new PolicyStoreUpgradeSession();
        }

        #endregion

        /// <summary>
        /// Purge all records from the policy health state table.
        /// </summary>
        public void PurgeHealthState()
        {
            PurgeHealthState(null);
        }

        /// <summary>
        /// Purge records from the policy health state table that are 
        /// associated with the nodes in the tree, starting with the value 
        /// that is passed to targetTreeRoot.
        /// </summary>
        /// <param name="targetTreeRoot"></param>
        public void PurgeHealthState(SfcQueryExpression targetTreeRoot)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PurgeHealthState", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(targetTreeRoot);
                try
                {
                    string purgeScript = string.Empty;
                    if (null != targetTreeRoot)
                    {
                        // if the target root is specified we need to transform the 
                        // query expression into a query expression that uses ID 
                        // because this is how records are identified in the system
                        // health table
                        SMO.Server domainRoot = new SMO.Server(this.SqlStoreConnection.ServerConnection);
                        SfcObjectQuery targetRootQuery = new SfcObjectQuery(domainRoot);

                        string targetTreeRootUrn = string.Empty;
                        foreach (SMO.SqlSmoObject obj in targetRootQuery.ExecuteIterator(targetTreeRoot, null, null))
                        {
                            if (targetTreeRootUrn.Length > 0)
                            {
                                // if the input query expression is referring to 
                                // more than one object this is considered an error
                                throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyOneTarget(targetTreeRoot.ToString())));
                            }

                            targetTreeRootUrn = obj.UrnOnlyId;
                        }

                        // If the targetTreeRoot returned no objects, then we should do
                        // nothing.
                        if (!String.IsNullOrEmpty(targetTreeRootUrn))
                        {
                            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                            args.Add(new SfcTsqlProcFormatter.RuntimeArg(
                                typeof(string), targetTreeRootUrn));
                            purgeScript = purgeHealthStateAction.GenerateScript(this, args);
                        }
                    }
                    else
                    {
                        purgeScript = "EXEC " + purgeHealthStateAction.Procedure;
                    }

                    if (!String.IsNullOrEmpty(purgeScript))
                    {
                        this.SqlStoreConnection.ServerConnection.ExecuteNonQuery(
                            purgeScript, ExecutionTypes.NoCommands);
                    }
                }
                catch (Exception e)
                {
                    methodTraceContext.TraceCatch(e);
                    if (!Utils.ShouldProcessException(e))
                    {
                        // If the exception is unrecoverable we have to let it go
                        throw;
                    }

                    throw methodTraceContext.TraceThrow(new FailedOperationException(ExceptionTemplatesSR.PurgeHealthState, this.Name, this.GetType().Name, e));
                }
            }
        }

        /// <summary>
        /// Sets System flag on objects in the store
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="marker"></param>
        public void MarkSystemObject(object obj, bool marker)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            if (this.SqlStoreConnection.ServerVersion < KilimanjaroVersion)
            {
                throw new DmfException(ExceptionTemplatesSR.VersionNotSupported(this.SqlStoreConnection.ServerVersion.ToString(), KilimanjaroVersion.ToString()));
            }

            int objectId;
            string objectType;
            string objectName;

            if (obj.GetType() == typeof(Policy))
            {
                Policy p = (Policy)obj;
                // Make sure update is necessary
                p.Refresh();
                if (p.IsSystemObject == marker)
                {
                    return;
                }
                objectId = p.ID;
                objectType = "POLICY";
                objectName = p.Name;
            }
            else if (obj.GetType() == typeof(Condition))
            {
                Condition c = (Condition)obj;
                c.Refresh();
                if (c.IsSystemObject == marker)
                {
                    return;
                }
                objectId = c.ID;
                objectType = "CONDITION";
                objectName = c.Name;
            }
            else if (obj.GetType() == typeof(ObjectSet))
            {
                ObjectSet os = (ObjectSet)obj;
                os.Refresh();
                if (os.IsSystemObject == marker)
                {
                    return;
                }
                objectId = os.ID;
                objectType = "OBJECTSET";
                objectName = os.Name;
            }
            else
            {
                throw new DmfException(ExceptionTemplatesSR.CannotMarkSystemObject(obj.GetType().Name));
            }

            try
            {
                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), objectType));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), objectId));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), marker ? 1 : 0));
                string script = markSystemObjectAction.GenerateScript(this, args);

                if (!String.IsNullOrEmpty(script))
                {
                    this.SqlStoreConnection.ServerConnection.ExecuteNonQuery(
                        script, ExecutionTypes.NoCommands);
                }

                // ensure the state is read back into memory
                ((SfcInstance)obj).Refresh();
            }
            catch (Exception e)
            {
                if (!Utils.ShouldProcessException(e))
                {
                    // If the exception is unrecoverable we have to let it go
                    throw;
                }

                throw new FailedOperationException(ExceptionTemplatesSR.MarkSystemObject, objectName, obj.GetType().Name, e);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PolicyStoreUpgradeSession : UpgradeSession
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyStoreUpgradeSession");

        private const int CTP6VersionNumber = 2;
        private const int CTP6RefreshVersionNumber = 3;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfcCache"></param>
        /// <param name="fileVersion"></param>
        public override void PostProcessUpgrade(Dictionary<String, object> sfcCache, int fileVersion)
        {
            // Make the upgrade process serial. We always go from version to version and don't upgrade across more than one version at a time.
            // Thus we go from CTP5 -> CTP6 -> CTP6 Refresh -> RTM
            if (fileVersion < PolicyStoreUpgradeSession.CTP6VersionNumber)
            {   // 1 -> 2
                CTP5ToCTP6PostProcessUpgrade(sfcCache);
            }
            if (fileVersion < PolicyStoreUpgradeSession.CTP6RefreshVersionNumber)
            {
                // 2 -> 3
                CTP6ToCTP6RefreshPostProcessUpgrade(sfcCache);
            }
        }

        /// <summary>
        /// For CTP6 to CTP6 Refresh so far we only have Facet's whos evaluation mode have removed CoC and Enforce.
        /// So, we find out if the evaluation mode for the policy is still valid for the target facet.  If it is not
        /// valid for the facet, then the evaluation mode is set to none.
        /// </summary>
        /// <param name="sfcCache"></param>
        private void CTP6ToCTP6RefreshPostProcessUpgrade(Dictionary<String, object> sfcCache)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CTP6ToCTP6RefreshPostProcessUpgrade"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(sfcCache);
                if (sfcCache == null)
                {
                    return;
                }
                // Find the policy object that is being deserialized
                Policy policyBeingImported = null;
                foreach (string uriKey in sfcCache.Keys)
                {
                    if (sfcCache[uriKey] is Policy)
                    {
                        policyBeingImported = (Policy)sfcCache[uriKey];
                        break;
                    }
                }

                // if we found the policy go ahead and find out the details of the EvaluationMode
                if (policyBeingImported != null)
                {
                    // Optimization: If the policy's evaluation mode is none, then nothing more to do, otherwise investigate more
                    if (policyBeingImported.AutomatedPolicyEvaluationMode != AutomatedPolicyEvaluationMode.None)
                    {
                        // Find the condition corresponding to the policy
                        Condition policyCondition = null;
                        foreach (string uriKey in sfcCache.Keys)
                        {
                            if (sfcCache[uriKey] is Condition)
                            {
                                policyCondition = (Condition)sfcCache[uriKey];
                                if (policyBeingImported.Condition == policyCondition.Name)
                                {
                                    AutomatedPolicyEvaluationMode allowedMode = policyCondition.GetSupportedEvaluationMode();
                                    // And the modes together to find out if the allowedModes intersect with the given mode
                                    // If they do intersect, then the correct mode is chosen, otherwise the mode is None and it is not enabled
                                    if ((allowedMode & policyBeingImported.AutomatedPolicyEvaluationMode) == AutomatedPolicyEvaluationMode.None)
                                    {
                                        policyBeingImported.AutomatedPolicyEvaluationMode = AutomatedPolicyEvaluationMode.None;
                                        policyBeingImported.Enabled = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfcCache"></param>
        private void CTP5ToCTP6PostProcessUpgrade(Dictionary<String, object> sfcCache)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CTP5ToCTP6PostProcessUpgrade"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(sfcCache);
                if (sfcCache == null)
                {
                    return;
                }

                // Find the policy object that is being deserialized
                Policy policyBeingImported = null;
                string policyBeingImportedUri = string.Empty;
                ObjectSet objectSet = null;
                Condition policyCondition = null;
                foreach (string uriKey in sfcCache.Keys)
                {
                    if (sfcCache[uriKey] is Policy)
                    {
                        policyBeingImported = (Policy)sfcCache[uriKey];
                        policyBeingImportedUri = uriKey;
                    }
                    if (sfcCache[uriKey] is ObjectSet)
                    {
                        objectSet = (ObjectSet)sfcCache[uriKey];
                    }
                }

                Hashtable targetSets = new Hashtable();
                Hashtable targetSetLevels = new Hashtable();
                Hashtable otherObjects = new Hashtable();

                if ((policyBeingImported != null) && (objectSet == null))
                {
                    string objectSetUri = string.Empty;
                    string[] uriKeysBeingImported = new string[sfcCache.Keys.Count];
                    sfcCache.Keys.CopyTo(uriKeysBeingImported, 0);

                    object objectBeingImported = null;
                    foreach (string uriKey in uriKeysBeingImported)
                    {
                        objectBeingImported = sfcCache[uriKey];
                        int targetSetLocation = targetSetLocation = uriKey.IndexOf("/TargetSet/", StringComparison.Ordinal);
                        if ((uriKey.StartsWith("/PolicyStore/Policy", StringComparison.Ordinal)) &&
                            (targetSetLocation > 0))
                        {
                            // Now you know you have some target sets so you should go ahead and create your ObjectSet
                            if (objectSet == null)
                            {
                                // TODO: Standardize the way we create the object set name.
                                objectSet = new ObjectSet(policyBeingImported.Parent, policyBeingImported.Name + "ObjectSet");
                                objectSetUri = string.Format(CultureInfo.InvariantCulture, "/PolicyStore/ObjectSet/{0}", objectSet.Name);

                                // TODO: Figure out a standardized way to get the Uri for the object set.Sfc Util method?
                                sfcCache.Add(objectSetUri, objectSet);
                                policyBeingImported.ObjectSet = objectSet.Name;
                            }

                            string targetSetUri = uriKey.Substring(targetSetLocation);
                            string newObjectSetRelatedUri = string.Format(CultureInfo.InvariantCulture, "/PolicyStore/ObjectSet/{0}{1}", objectSet.Name, targetSetUri);
                            // Dissociate the TargetSet or the TargetSetLevel object from the old uri and associate it to the new uri
                            sfcCache.Remove(uriKey);

                            if (objectBeingImported is TargetSet)
                            {
                                // TODO: Ensure you fill-in the "disabled" target sets into the object set using a dummy object set and removing from the collection those that you find in the serialization format
                                ((TargetSet)objectBeingImported).Enabled = true;
                                targetSets.Add(newObjectSetRelatedUri, (TargetSet)objectBeingImported);
                            }
                            if (objectBeingImported is TargetSetLevel)
                            {
                                targetSetLevels.Add(newObjectSetRelatedUri, (TargetSetLevel)objectBeingImported);
                            }
                        }
                        else
                        {
                            if (!(objectBeingImported is Policy))
                            {
                                otherObjects.Add(uriKey, objectBeingImported);
                            }
                            if ((objectBeingImported is Condition) && (((Condition)objectBeingImported).Name == policyBeingImported.Condition))
                            {
                                policyCondition = (Condition)objectBeingImported;
                            }
                        }
                    }

                    sfcCache.Clear();
                    sfcCache.Add(policyBeingImportedUri, policyBeingImported);
                    if (objectSet != null)
                    {
                        objectSet.Properties["Facet"].Value = policyCondition.Facet;
                        sfcCache.Add(objectSetUri, objectSet);
                    }

                    foreach (string newObjectSetRelatedUri in targetSets.Keys)
                    {
                        sfcCache.Add(newObjectSetRelatedUri, targetSets[newObjectSetRelatedUri]);
                    }
                    foreach (string newObjectSetRelatedUri in targetSetLevels.Keys)
                    {
                        sfcCache.Add(newObjectSetRelatedUri, targetSetLevels[newObjectSetRelatedUri]);
                    }

                    foreach (string newObjectSetRelatedUri in otherObjects.Keys)
                    {
                        sfcCache.Add(newObjectSetRelatedUri, otherObjects[newObjectSetRelatedUri]);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="fileVersion"></param>
        /// <returns></returns>
        public override bool IsUpgradeRequiredOnType(String instanceType, int fileVersion)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("IsUpgradeRequiredOnType", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(instanceType, fileVersion);
                if (fileVersion < (((ISfcDomain)new PolicyStore()).GetLogicalVersion()))
                {
                    switch (instanceType)
                    {
                        case "Policy":
                        case "Condition":
                            methodTraceContext.TraceParameterOut("returnVal", true);
                            return true;
                        default:
                            methodTraceContext.TraceParameterOut("returnVal", false);
                            return false;
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", false);
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfcInstanceData"></param>
        /// <param name="fileVersion"></param>
        /// <param name="smlUri"></param>
        /// <param name="sfcCache"></param>
        /// <returns></returns>
        public override List<KeyValuePair<string, object>> UpgradeInstance(List<SfcInstanceSerializedData> sfcInstanceData, int fileVersion, string smlUri, Dictionary<string, object> sfcCache)
        {
            // We only get smlUri to identify the object - have to compare strings
            // Will switch to a better option if we get one

            List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>();

            if (smlUri.StartsWith("/PolicyStore/Policy/", StringComparison.Ordinal))
            {
                object obj = base.UpgradeInstance(typeof(Policy), Policy.UpgradeInstance(sfcInstanceData, fileVersion));
                list.Add(new KeyValuePair<string, object>(smlUri, obj));
            }
            else if (smlUri.StartsWith("/PolicyStore/Condition/", StringComparison.Ordinal))
            {
                object obj = base.UpgradeInstance(typeof(Condition), Condition.UpgradeInstance(sfcInstanceData, fileVersion));
                list.Add(new KeyValuePair<string, object>(smlUri, obj));
            }
            else
            {
                traceContext.DebugAssert(true, "We are not supposed to get other than for Policy or Condition");
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        public PolicyStoreUpgradeSession()
        {
        }
    }

    internal sealed class ObjectToValidate
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ObjectToValidate");
        object obj;
        Type type;
        string validationMethod;

        public static readonly string Drop = "Drop";

        public ObjectToValidate(object obj, Type type, string validationMethod)
        {
            traceContext.TraceMethodEnter("ObjectToValidate");
            // Tracing Input Parameters
            traceContext.TraceParameters(obj, type, validationMethod);
            this.obj = obj;
            this.type = type;
            this.validationMethod = validationMethod;
            traceContext.TraceMethodExit("ObjectToValidate");
        }

        public object Object { get { return this.obj; } }
        public Type Type { get { return this.type; } }
        public string ValidationMethod { get { return this.validationMethod; } }

        internal static void ValidateObjects(List<ObjectToValidate> objectsToValidate)
        {
            traceContext.TraceMethodEnter("ValidateObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(objectsToValidate);
            // This code assumes there is only 1 Policy in the List!

            // Do 2 passes if there are Conditions for Alter
            // 1st collect Policy and process other objects
            // 2nd process Conditions
                
            Policy policy = null;
            bool do2pass = false;

            foreach (ObjectToValidate otv in objectsToValidate)
            {
                if (otv.Type == typeof(Policy))
                {
                    ((Policy)(otv.Object)).Validate(otv.ValidationMethod);
                    policy = (Policy)(otv.Object);
                }
                else if (otv.Type == typeof(ObjectSet))
                {
                    if (otv.ValidationMethod != ObjectToValidate.Drop)
                    {
                        ((ObjectSet)(otv.Object)).Validate(otv.ValidationMethod);
                    }
                }
                else if (otv.Type == typeof(Condition))
                {
                    if (otv.ValidationMethod == SFC.ValidationMethod.Alter)
                    {
                        do2pass = true;
                    }
                    else
                    {
                        ((Condition)(otv.Object)).Validate(otv.ValidationMethod);
                    }
                }
                else if (otv.Type == typeof(PolicyCategory))
                {
                    // just ignore, we keep categories for cleanup onnly
                }
                else
                {
                    traceContext.DebugAssert(true, "Policy deserialization - unknown object type");
                }
            }

            // 2nd pass - Alter conditions (override existing conditions)
            if (do2pass)
            {
                traceContext.DebugAssert(null != policy);
                foreach (ObjectToValidate otv in objectsToValidate)
                {
                    if (otv.Type == typeof(Condition))
                    {
                        if (otv.ValidationMethod == SFC.ValidationMethod.Alter)
                        {
                            ((Condition)(otv.Object)).ValidateDeserialized(policy);
                        }
                    }
                }
            }
            traceContext.TraceMethodExit("ValidateObjects");
        }

        internal static void CommitObjects(PolicyStore ps, List<ObjectToValidate> objectsToValidate)
        {
            traceContext.TraceMethodEnter("CommitObjects");
            // Tracing Input Parameters
            traceContext.TraceParameters(ps, objectsToValidate);
            ObjectToValidate otv_policy = null;
            ObjectSet objectSetToDrop = null;
            ObjectSet objectSetToCreate = null;

            // pass1 - conditions
            foreach (ObjectToValidate otv in objectsToValidate)
            {
                if (otv.Type == typeof(Policy))
                {
                    otv_policy = otv;
                }
                else if (otv.Type == typeof(ObjectSet))
                {
                    if (otv.ValidationMethod == SFC.ValidationMethod.Create)
                    {
                        objectSetToCreate = ((ObjectSet)(otv.Object));
                    }
                    else if (otv.ValidationMethod == ObjectToValidate.Drop)
                    {
                        objectSetToDrop = ((ObjectSet)(otv.Object));
                    }
                }
                else if (otv.Type == typeof(Condition))
                {
                    if (otv.ValidationMethod == SFC.ValidationMethod.Create)
                    {
                        ((Condition)(otv.Object)).Create();
                    }
                    else if (otv.ValidationMethod == SFC.ValidationMethod.Alter)
                    {
                        ((Condition)(otv.Object)).AlterNoValidation();
                    }
                }
                else if (otv.Type == typeof(PolicyCategory))
                {
                    ((PolicyCategory)(otv.Object)).Create();
                }
            }

            if (otv_policy.ValidationMethod == SFC.ValidationMethod.Create)
            {
                traceContext.DebugAssert(objectSetToDrop == null, "only drop ObjectSet for policy overwrite");
                if (null != objectSetToCreate)
                {
                    objectSetToCreate.Create();
                }
                ((Policy)(otv_policy.Object)).Create();
            }
            else if (otv_policy.ValidationMethod == SFC.ValidationMethod.Alter)
            {
                // In order to drop an exising OS we have to remove policy reference to it
                if (null != objectSetToDrop)
                {
                    bool enabled = ((Policy)(otv_policy.Object)).Enabled;
                    ((Policy)(otv_policy.Object)).Enabled = false;
                    AutomatedPolicyEvaluationMode mode = ((Policy)(otv_policy.Object)).AutomatedPolicyEvaluationMode;
                    ((Policy)(otv_policy.Object)).AutomatedPolicyEvaluationMode = AutomatedPolicyEvaluationMode.None;
                    ((Policy)(otv_policy.Object)).ObjectSet = String.Empty;
                    // This action drops ObjectSet
                    ((Policy)(otv_policy.Object)).Alter();
                    ps.ObjectSets.Refresh();
                    traceContext.DebugAssert(!ps.ObjectSets.Contains(objectSetToDrop), "ObjectSet should have been dropped by a trigger");
                    ((Policy)(otv_policy.Object)).ObjectSet = objectSetToCreate.Name;
                    ((Policy)(otv_policy.Object)).AutomatedPolicyEvaluationMode = mode;
                    ((Policy)(otv_policy.Object)).Enabled = enabled;
                }
                if (null != objectSetToCreate)
                {
                    objectSetToCreate.Create();
                }
                ((Policy)(otv_policy.Object)).Alter();
            }
            traceContext.TraceMethodExit("CommitObjects");
        }

        internal static void RestoreCollections(PolicyStore ps, List<ObjectToValidate> objectsToValidate)
        {
            traceContext.TraceMethodEnter("RestoreCollections");
            // Tracing Input Parameters
            traceContext.TraceParameters(ps, objectsToValidate);

            if (((ISfcHasConnection)ps).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
            {
                //For disconnected stores, we need to remove all the objects that were to be validated from the store collections
                foreach (ObjectToValidate otv in objectsToValidate)
                {
                    if (otv.Type == typeof(Policy))
                    {
                        ((Policy)(otv.Object)).Drop();
                        ps.Policies.Remove(((Policy)(otv.Object)));
                    }
                    else if (otv.Type == typeof(Condition))
                    {
                        ((Condition)(otv.Object)).Drop();
                        ps.Conditions.Remove((Condition)(otv.Object));
                    }
                    else if (otv.Type == typeof(ObjectSet))
                    {
                        ((ObjectSet)(otv.Object)).Drop();
                        ps.ObjectSets.Remove((ObjectSet)(otv.Object));
                    }
                    else if (otv.Type == typeof(PolicyCategory))
                    {
                        ((PolicyCategory)(otv.Object)).Drop();
                        ps.PolicyCategories.Remove((PolicyCategory)(otv.Object));
                    }
                }
            }
            else
            {
                //For connected stores, we only need to refresh the collections and restore the altered objects
                ps.Policies.Refresh();
                ps.ObjectSets.Refresh();
                ps.Conditions.Refresh();
                ps.PolicyCategories.Refresh();

                foreach (ObjectToValidate otv in objectsToValidate)
                {
                    if (otv.ValidationMethod == SFC.ValidationMethod.Alter &&
                        (otv.Type == typeof(Policy) || otv.Type == typeof(Condition)))
                    {
                        ((SfcInstance)(otv.Object)).Refresh();
                    }
                }
            }
            traceContext.TraceMethodExit("RestoreCollections");
        }

    }

    internal sealed class PolicyScheduleHelper
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyScheduleHelper");
        /// <summary>
        /// Makes sure that a CoS policy has a valid schedule to run with. 
        /// </summary>
        /// <param name="policy"></param>
        public static void FixPolicySchedule(Policy policy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("FixPolicySchedule"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policy);
                traceContext.DebugAssert(policy.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnSchedule);

                // if there is no schedule information we need to disable the policy
                // because we can't create it with a valid schedule
                if (string.IsNullOrEmpty(policy.Schedule) )
                {
                    policy.AutomatedPolicyEvaluationMode = AutomatedPolicyEvaluationMode.None;
                    policy.Enabled = false;
                    return;
                }

                SMO.Server server = new Microsoft.SqlServer.Management.Smo.Server(policy.Parent.SqlStoreConnection.ServerConnection);
                SMO.Agent.JobSchedule js = server.JobServer.SharedSchedules[policy.ScheduleUid];

                bool scheduleUidExists = (null != js);
                // we have found the schedule, check to see if all properties 
                // are identical to our saved properties
                if (scheduleUidExists && JobScheduleMatchesPolicy(js, policy))
                {
                    return;
                }

                // if we did not find the guid look up for schedules with the same name
                foreach (SMO.Agent.JobSchedule js1 in server.JobServer.SharedSchedules)
                {
                    if (JobScheduleMatchesPolicy(js1, policy))
                    {
                        policy.ScheduleUid = js1.ScheduleUid;
                        return;
                    }
                }

                // if searching for guid or name failed then we need to create 
                // a new schedule, with the same name as the schedule that was originally 
                // referenced by the policy. this is important because when moving 
                // a group of policies from a machine to another machine
                // we are keeping all the policies referencing the same schedule.
                SMO.Agent.JobSchedule newSchedule = new Microsoft.SqlServer.Management.Smo.Agent.JobSchedule(
                    server.JobServer, policy.Schedule);

                // if possible recreate the schedule with the same guid
                if (!scheduleUidExists && policy.ScheduleUid != Guid.Empty)
                {
                    newSchedule.ScheduleUid = policy.ScheduleUid;
                }

                // set the schedule properties based on the proeprties of 
                // the policy, where they are available
                if (!policy.Properties["ActiveEndDate"].IsNull)
                {
                    newSchedule.ActiveEndDate = policy.ActiveEndDate;
                }

                if (!policy.Properties["ActiveEndTimeOfDay"].IsNull)
                {
                    newSchedule.ActiveEndTimeOfDay = new TimeSpan(policy.ActiveEndTimeOfDay);
                }

                if (!policy.Properties["ActiveStartDate"].IsNull)
                {
                    newSchedule.ActiveStartDate = policy.ActiveStartDate;
                }

                if (!policy.Properties["ActiveStartTimeOfDay"].IsNull)
                {
                    newSchedule.ActiveStartTimeOfDay = new TimeSpan(policy.ActiveStartTimeOfDay);
                }

                if (!policy.Properties["FrequencyInterval"].IsNull)
                {
                    newSchedule.FrequencyInterval = policy.FrequencyInterval;
                }

                if (!policy.Properties["FrequencyRecurrenceFactor"].IsNull)
                {
                    newSchedule.FrequencyRecurrenceFactor = policy.FrequencyRecurrenceFactor;
                }

                if (!policy.Properties["FrequencyRelativeIntervals"].IsNull)
                {
                    newSchedule.FrequencyRelativeIntervals = policy.FrequencyRelativeIntervals;
                }

                if (!policy.Properties["FrequencySubDayInterval"].IsNull)
                {
                    newSchedule.FrequencySubDayInterval = policy.FrequencySubDayInterval;
                }

                if (!policy.Properties["FrequencySubDayTypes"].IsNull)
                {
                    newSchedule.FrequencySubDayTypes = policy.FrequencySubDayTypes;
                }

                if (!policy.Properties["FrequencyTypes"].IsNull)
                {
                    newSchedule.FrequencyTypes = policy.FrequencyTypes;
                }

                newSchedule.Create();
                newSchedule.Refresh();
                policy.ScheduleUid = newSchedule.ScheduleUid;
            }
        }

        private static bool JobScheduleMatchesPolicy(SMO.Agent.JobSchedule js, Policy policy)
        {
            return (
            policy.Schedule == js.Name &&
            policy.ActiveEndDate == js.ActiveEndDate &&
            policy.ActiveEndTimeOfDay == js.ActiveEndTimeOfDay.Ticks &&
            policy.ActiveStartDate == js.ActiveStartDate &&
            policy.ActiveStartTimeOfDay == js.ActiveStartTimeOfDay.Ticks &&
            policy.FrequencyInterval == js.FrequencyInterval &&
            policy.FrequencyRecurrenceFactor == js.FrequencyRecurrenceFactor &&
            policy.FrequencyRelativeIntervals == js.FrequencyRelativeIntervals &&
            policy.FrequencySubDayInterval == js.FrequencySubDayInterval &&
            policy.FrequencySubDayTypes == js.FrequencySubDayTypes &&
            policy.FrequencyTypes == js.FrequencyTypes);
        }
    }

    /// <summary>
    /// Helper class that contains the policy evaluation functions exposed 
    /// through SQLCLR
    /// </summary>
    internal sealed class PolicyEvaluationHelper
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "PolicyEvaluationHelper");
        /// <summary>
        /// Helper for policy automation. Executes a policy for one target, if 
        /// specified through the target XML blob. 
        /// </summary>
        /// <param name="policy">Policy name</param>
        /// <param name="eventData">XML blob identifying the target. Its schema is
        /// identical to the one returned by EVENTDATA() function.</param>
        /// <param name="historyId">ID of the entry in history table 
        /// generated by executing this policy</param>
        /// <returns>0 for success, 1 for failure. This method
        /// can throw an exception in some failure cases.</returns>
        public static int EvaluateAutomatedPolicy (string policy, SqlXml eventData, ref Int64 historyId)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EvaluateAutomatedPolicy"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policy, eventData, historyId);
                string command = String.Empty;

                // protect against callers not in SQLCLR
                if (!SqlContext.IsAvailable)
                {
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyInSqlClr));
                }

                bool result = false;
                using (SqlConnection innerConn = new SqlConnection("context connection=true"))
                {
                    if (null == eventData)
                    {
                        LogExceptionMessage(ExceptionTemplatesSR.NullArgInPolicyEvaluation, innerConn);
                        methodTraceContext.TraceParameterOut("returnVal", 1);
                        return 1;
                    }

                    if (null == policy)
                    {
                        LogExceptionMessage(ExceptionTemplatesSR.NullArgInPolicyEvaluation, innerConn);
                        methodTraceContext.TraceParameterOut("returnVal", 1);
                        return 1;
                    }

                    try
                    {
                        innerConn.Open();
                        Version version = new Version (innerConn.ServerVersion);

                        // make sure we are called from msdb
                        string dbName = new SqlCommand("SELECT DB_NAME()", innerConn).ExecuteScalar() as string;
                        traceContext.DebugAssert(null != dbName);
                        if (dbName != "msdb")
                        {
                            throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyInMsdb));
                        }

                        // restrict this to PolicyAdministrator or db_owner 
                        int memberCheck = (int)(new SqlCommand("DECLARE @ret int; EXECUTE @ret = [msdb].[dbo].[sp_syspolicy_check_membership] 'PolicyAdministratorRole'; SELECT @ret",
                                                        innerConn).ExecuteScalar());
                        if (memberCheck != 0)
                        {
                            throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyRoleMember("PolicyAdministratorRole")));
                        }

                        PolicyStore ps = new PolicyStore(new SqlStoreConnection(innerConn));

                        Policy p = ps.Policies[policy];
                        if (null == p)
                        {
                            throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.MissingPolicy(policy)));
                        }

                        if (p.HasScript)
                        {
                            // if the policy references a condition with script
                            // we will log the message and return success
                            // if we return failure we risk being in a DoS situation 
                            // where somebody creates rogue policies that reject
                            // all valid DDL
                            SqlContext.Pipe.Send(ExceptionTemplatesSR.NoScriptInSqlClr(policy));
                            methodTraceContext.TraceParameterOut("returnVal", 0);
                            return 0;
                        }

                        StringBuilder targetUri = new StringBuilder();
                        SfcQueryExpression targetQueryExpression = null;
                        if (!eventData.IsNull)
                        {
                            // calculate what object triggered this
                            targetQueryExpression = GetTargetQueryExpression(eventData, p, targetUri, out command);

                            // execute the policy for that target
                            SfcQueryExpression adjustedExpression = ObjectSet.AdjustForSystem (version, p, targetQueryExpression);
                            result = p.Evaluate(AdHocPolicyEvaluationMode.Check, adjustedExpression, ref historyId, new ISfcConnection[] { ps.SqlStoreConnection });
                        }
                        else
                        {
                            // if no event data was passed in we want to evaluate the policy
                            // for the entire target set
                            result = p.Evaluate(AdHocPolicyEvaluationMode.Check, ref historyId, ps.SqlStoreConnection);
                        }

                        if (p.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.Enforce &&
                            !result &&
                            null != targetQueryExpression)
                        {
                            // If the policy failed and we are enforcing it we will  
                            // roll back the transaction in the calling sprocs.

                            if (command.Length > 150)
                            {
                                command = command.TrimStart(new char[] { ' ' }).Substring(0, 147) + "...";
                            }
                            Condition c = ps.Conditions[p.Condition];
                            SqlContext.Pipe.Send(ExceptionTemplatesSR.RollBack(policy, targetUri.ToString(), c.ExpressionNode.ToString(), p.Description, p.HelpText, p.HelpLink, command));
                        }
                    }
                    catch (Exception e)
                    {
                        methodTraceContext.TraceCatch(e);
                        LogExceptionMessage(e.ToString(), innerConn);
                    }
                }

                return Convert.ToInt32(!result);
            }
        }

        // preallocated buffer to format RAISERROR commands
        private static StringBuilder raiserrorBuilder;
        // preallocated command execution
        private static SqlCommand exceptionLoggingCommand;
        // size of the message buffer
        private const int RaiserrorBufferSize = 1024;

        /// <summary>
        /// Static ctor for this class, initializes the error message
        /// buffer 
        /// </summary>
        static PolicyEvaluationHelper ()
        {
            raiserrorBuilder = new StringBuilder (RaiserrorBufferSize);
            exceptionLoggingCommand = new SqlCommand ();
        }

        /// <summary>
        /// Logs exception via a RAISERROR WITH LOG statement. 
        /// The message will appear as a Sev 1 State 1 informational message
        /// and it will also be logged to the Windows event log.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="connection"></param>
        internal static void LogExceptionMessage (string message, SqlConnection connection)
        {
            traceContext.TraceMethodEnter("LogExceptionMessage");
            // Tracing Input Parameters
            traceContext.TraceParameters(message, connection);
            raiserrorBuilder.Length = 0;
            raiserrorBuilder.AppendFormat ("RAISERROR({0}, 1,1) WITH LOG",
                        SfcTsqlProcFormatter.MakeSqlString (message));
            exceptionLoggingCommand.CommandText = raiserrorBuilder.ToString ();
            exceptionLoggingCommand.Connection = connection;
            exceptionLoggingCommand.ExecuteNonQuery();
            traceContext.TraceMethodExit("LogExceptionMessage");
        }

        /// <summary>
        /// Returns a SfcQueryExpression representing the object defined in eventData
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="p"></param>
        /// <param name="targetPsPathBuilder"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private static SfcQueryExpression GetTargetQueryExpression (SqlXml eventData, Policy p, StringBuilder targetPsPathBuilder, out String command)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetTargetQueryExpression"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(eventData, p, targetPsPathBuilder);
                // Example of eventData
                /*
                <EVENT_INSTANCE>
                  <EventType>CREATE_TABLE</EventType>
                  <PostTime>2008-02-25T14:34:30.073</PostTime>
                  <SPID>55</SPID>
                  <ServerName>myserver</ServerName>
                  <LoginName>mylogin</LoginName>
                  <UserName>dbo</UserName>
                  <DatabaseName>test</DatabaseName>
                  <SchemaName>dbo</SchemaName>
                  <ObjectName>badname</ObjectName>
                  <ObjectType>TABLE</ObjectType>
                  <TSQLCommand>
                    <SetOptions ANSI_NULLS="ON" ANSI_NULL_DEFAULT="ON" ANSI_PADDING="ON" QUOTED_IDENTIFIER="ON" ENCRYPTED="FALSE" />
                    <CommandText>create table badname (id int)</CommandText>
                  </TSQLCommand>
                </EVENT_INSTANCE>
                 */

                command = String.Empty;
                XmlDocument doc = new XmlDocument();
                XmlNode node = doc.ReadNode(eventData.CreateReader());
                traceContext.DebugAssert(node.Name == "EVENT_INSTANCE");

                SortedDictionary<string, string> eventValues = new SortedDictionary<string, string>();
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    XmlNode firstChild = childNode.FirstChild;

                    if (null != firstChild && null != firstChild.Value)
                    {
                        eventValues.Add(childNode.Name, childNode.FirstChild.Value);
                    }
                    else if (null != firstChild && null != firstChild.NextSibling && null != firstChild.NextSibling.FirstChild && null != firstChild.NextSibling.FirstChild.Value)
                    {
                        // We're targeting TSQLCommand node here - it's firstChild is SetOptions; SetOptions NextSibling is CommandText

                        eventValues.Add(firstChild.NextSibling.Name, firstChild.NextSibling.FirstChild.Value);
                    }
                    else
                    {
                        eventValues.Add(childNode.Name, string.Empty);
                    }
                }

                // Find statement
                if (eventValues.ContainsKey("CommandText"))
                {
                    command = eventValues["CommandText"];
                }

                // now try to calculate the Urn
                StringBuilder targetUrn = new StringBuilder();
                targetUrn.AppendFormat("Server[@Name='{0}']",
                    Urn.EscapeString(p.Parent.SqlStoreConnection.ServerConnection.TrueName));

                // all event blobs should have this field
                traceContext.DebugAssert(eventValues.ContainsKey("EventType"));

                switch (eventValues["EventType"])
                {
                    case "CREATE_DATABASE":
                    case "ALTER_DATABASE":
                        if (eventValues.ContainsKey("DatabaseName"))
                        {
                            targetUrn.AppendFormat("/Database[@Name='{0}']",
                                        Urn.EscapeString(eventValues["DatabaseName"]));
                        }
                        break;
                    case "CREATE_RESOURCE_POOL":
                    case "ALTER_RESOURCE_POOL":
                        // ResourceGovernor is a singleton object.  We add it to the Urn before adding the ResourcePool information
                        targetUrn.AppendFormat("/ResourceGovernor");
                        GetExpressionGeneric(eventValues, targetUrn, eventData, p);
                        break;
                    case "CREATE_WORKLOAD_GROUP":
                    case "ALTER_WORKLOAD_GROUP":
                        // The following code has special knowledge of the WorkloadGroup object.
                        // WorkloadGroups are uniquely named across all ResourcePools.  Thus, 
                        // the following code takes advanatage of this knowledge to retrive the WorkloadGroup
                        // being targeted by this event by issuing the query "Server[@Name=<serverName>]/ResourceGovernor/ResourcePool/WorkloadGroup[@Name=<workloadgroup>]"
                        // This will return one single object, and using that object we can get the unique Urn that contains the name
                        // of the ResourcePool.  With the Urn, we are able to retrive the Powershell path.
                        //
                        // Typically the parent object key, in this case ResourcePool, would be included in the event.
                        // However, the workload group events are missing the parent object key.
                        // This is being tracked by Engine bug: VSTS 222405
                        targetUrn.Length = 0;
                        SMO.Server server = new SMO.Server(p.Parent.SqlStoreConnection.ServerConnection);
                        SMO.WorkloadGroup workloadGroup = SMO.WorkloadGroup.GetWorkloadGroup(server, eventValues["ObjectName"]);
                        targetUrn.Append(workloadGroup.Urn);
                        break;
                    default:
                        GetExpressionGeneric(eventValues, targetUrn, eventData, p);
                        break;
                }

                targetPsPathBuilder.Length = 0;
                targetPsPathBuilder.Append(SfcSqlPathUtilities.ConvertUrnToPath(targetUrn.ToString()));

                return new SfcQueryExpression(targetUrn.ToString());
            }
        }

        /// <summary>
        /// Builds an Urn from the pairs of name and values in the dictionary.
        /// </summary>
        /// <param name="eventValues"></param>
        /// <param name="targetUrn"></param>
        /// <param name="eventData"></param>
        /// <param name="p"></param>
        private static void GetExpressionGeneric(SortedDictionary<string, string> eventValues,
                                                StringBuilder targetUrn,
                                                SqlXml eventData,
                                                Policy p)
        {
            traceContext.TraceMethodEnter("GetExpressionGeneric");
            // Tracing Input Parameters
            traceContext.TraceParameters(eventValues, targetUrn, eventData, p);
            if (eventValues.ContainsKey("DatabaseName"))
            {
                targetUrn.AppendFormat ("/Database[@Name='{0}']", Urn.EscapeString (eventValues["DatabaseName"]));
            }

            string eventType = string.Empty;
            eventType = GetEventValue (eventValues, eventData, "EventType");

            string newName = string.Empty;
            if (eventType == "RENAME")
            {
                // on RENAME events the actual name of the object is in 
                // the NewObjectName element
                newName = GetEventValue (eventValues, eventData, "NewObjectName");
            }

            string objectType = eventValues["ObjectType"];
            if (objectType != null && !Char.IsDigit (objectType[0]))
            {
                string schemaName = String.Empty;
                if ((objectType != "SCHEMA") &&
                    eventValues.ContainsKey ("SchemaName"))
                {
                    schemaName = eventValues["SchemaName"];
                }

                // If there is a parent level, then include it and its
                // schema in the URN.
                if (eventValues.ContainsKey ("TargetObjectType") &&
                    eventValues.ContainsKey ("TargetObjectName"))
                {
                    string targetType = GetTypeCorrectCase (eventValues["TargetObjectType"]);
                    string targetName = eventValues["TargetObjectName"];

                    // TargetObjectType and TargetObjectName can be empty for 
                    // certain events
                    if (targetType.Length > 0 &&
                        targetName.Length > 0 &&
                        schemaName.Length > 0)
                    {
                        targetUrn.AppendFormat("/{0}[@Name='{1}' and @Schema='{2}']",
                            targetType,
                            Urn.EscapeString(targetName),
                            Urn.EscapeString(schemaName));

                        // Don't include the schema in the child level
                        schemaName = String.Empty;
                    }
                    else if (targetType.Length > 0 && targetName.Length > 0)
                    {
                        targetUrn.AppendFormat ("/{0}[@Name='{1}']", targetType, Urn.EscapeString (targetName));
                    }
                }

                string realType = GetTypeCorrectCase (objectType.ToString ());
                string objName = GetEventValue (eventValues, eventData, "ObjectName");
                if (objName.Length > 0 && newName.Length > 0)
                {
                    // if the object has been renamed adjust the name here.
                    objName = newName;
                }

                if (schemaName != String.Empty)
                {
                    targetUrn.AppendFormat ("/{0}[@Name='{1}' and @Schema='{2}']", realType, Urn.EscapeString (objName), Urn.EscapeString (schemaName));
                }
                else
                {
                    targetUrn.AppendFormat ("/{0}[@Name='{1}']", realType, Urn.EscapeString (objName));
                }
            }
            else
            {
                // Special case - if we get here DB level should be ignored
                targetUrn.Length = 0;
                targetUrn.AppendFormat ("Server[@Name='{0}']",
                    Urn.EscapeString (p.Parent.SqlStoreConnection.ServerInstance));
            }
            traceContext.TraceMethodExit("GetExpressionGeneric");
        }

        /// <summary>
        /// Retrieves the value for an element from the name-value
        /// pairs read from the eventData
        /// </summary>
        /// <param name="eventValues"></param>
        /// <param name="eventData"></param>
        /// <param name="elementName"></param>
        /// <returns></returns>
        private static string GetEventValue(SortedDictionary<string, string> eventValues,
                                            SqlXml eventData,
                                            string elementName)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetEventValue"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(eventValues, eventData, elementName);
                string elementValue = string.Empty;

                if (!eventValues.ContainsKey(elementName) ||
                    null == (elementValue = eventValues[elementName]))
                {
                    // make sure that we have the object type
                    throw traceContext.TraceThrow(new BadEventDataException(
                        ExceptionTemplatesSR.BadEventData(elementName, eventData.ToString())));
                }

                methodTraceContext.TraceParameterOut("returnVal", elementValue);
                return elementValue;
            }
        }

        /// <summary>
        /// Returns a version of the string with the correct case that
        /// is understood by the Enumerator. Using the wrong case will
        /// result in an exception inside of the Enumerator.
        /// </summary>
        private static string GetTypeCorrectCase (string type)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetTypeCorrectCase"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(type);
                if (type.Length == 0)
                {
                    methodTraceContext.TraceParameterOut("returnVal", type);
                    return type;
                }

                string typeLower = type.ToLowerInvariant();
                switch (typeLower)
                {
                    case "procedure":
                        return "StoredProcedure";
                    case "function":
                        return "UserDefinedFunction";
                    case "type":
                        return "UserDefinedType";
                    case "sql user":
                    case "certificate user":
                    case "asymmetric key user":
                    case "windows user":
                    case "group user":
                        return "User";
                    case "server role": 
                        return "Role"; //SMO URN type for server role is "Role" but server role's events' ObjectType is "SERVER ROLE".
                    default:
                        // Camel-case the type since that is correct for most types. For
                        // example, 'table' should be 'Table', 'application role' should be 'ApplicationRole'
                        StringBuilder ret = new StringBuilder();
                        string[] multiPartName = typeLower.Split(' ');
                        foreach (string name in multiPartName)
                        {
                            ret.Append(name[0].ToString().ToUpperInvariant()[0]);
                            ret.Append(name.Substring(1));
                        }
                        return ret.ToString();
                }
            }   
        }
    }

    internal class PropertyInfoNameComparer : IComparer<PropertyInfo>
    {
        public int Compare(PropertyInfo x, PropertyInfo y)
        {
            return String.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }
    }
}
