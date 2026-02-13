// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.Dmf
{
    // TODO: Low priority: Should object set support rename? Probably should similar to the condition object, because it is referenced by
    // the object_set_id in the condition. However, it's a low priority because we aren't going to expose it in the OE for people to rename
    // NOTE: The Policy API object references the ObjectSet via name, so double check what the Condition API rename is doing.
    /// <summary>
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class ObjectSet : SfcInstance, ISfcCreatable, ISfcDroppable, ISfcAlterable
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "ObjectSet");
        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();

        static ObjectSet()
        {
            scriptCreateAction.Procedure = "msdb.dbo.sp_syspolicy_add_object_set";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_set_name", "Name", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("facet", "Facet", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_set_id", "ID", false, true));

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_syspolicy_delete_object_set";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_set_id", "ID", true, false));
        }

        /// <summary>
        /// This is the non-generated part of the ObjectSet class.
        /// </summary>
        public ObjectSet()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public ObjectSet(PolicyStore parent, string name)
        {
            traceContext.TraceMethodEnter("ObjectSet");

            SetName(name);
            this.Parent = parent;
            traceContext.TraceMethodExit("ObjectSet");
        }

        private static void PopulateTargetSetBasedOnType(ObjectSet objectSet, Type type)
        {
            traceContext.TraceMethodEnter("PopulateTargetSetBasedOnType");
            // Tracing Input Parameters
            traceContext.TraceParameters(objectSet, type);
            foreach (string skeleton in SfcMetadataDiscovery.GetUrnSkeletonsFromType(type))
            {
                TargetSet targetSet = new TargetSet(objectSet, skeleton);
                targetSet.Enabled = false;
                objectSet.TargetSets.Add(targetSet);
            }
            traceContext.TraceMethodExit("PopulateTargetSetBasedOnType");
        }

        /// <summary>
        /// Populates TargetSets based on path information supplied by target types ('physical' path)
        /// </summary>
        /// <param name="objectSet"></param>
        /// <param name="facetType"></param>
        private static void PopulateTargetSetsBasedOnFacet(ObjectSet objectSet, Type facetType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PopulateTargetSetsBasedOnFacet"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(objectSet, facetType);
                if (!FacetRepository.IsRegisteredFacet(facetType))
                {
                    throw methodTraceContext.TraceThrow(new NullFacetException(facetType.Name));
                }

                foreach (Type type in FacetRepository.GetFacetSupportedTypes(facetType))
                {
                    PopulateTargetSetBasedOnType(objectSet, type);
                }
            }
        }

        /// <summary>
        /// Populates TargetSets based on path information supplied by specified domain ('view' path)
        /// if the domain implements ISfcDomain2, otherwise it defaults to 'physical' path
        /// </summary>
        /// <param name="objectSet"></param>
        /// <param name="facetType"></param>
        /// <param name="domainInfo"></param>
        private static void PopulateTargetSetsBasedOnFacet(ObjectSet objectSet, Type facetType, SfcDomainInfo domainInfo)
        {
            if (!FacetRepository.IsRegisteredFacet(facetType))
            {
                throw new NullFacetException(facetType.Name);
            }

            object obj = Activator.CreateInstance (domainInfo.RootType);

            if (obj is ISfcDomain2)
            {
                foreach (Type type in FacetRepository.GetFacetSupportedTypes (facetType))
                {
                    foreach (string skeleton in ((ISfcDomain2)obj).GetUrnSkeletonsFromType (type))
                    {
                        TargetSet targetSet = new TargetSet (objectSet, skeleton);
                        targetSet.Enabled = false;
                        objectSet.TargetSets.Add (targetSet);
                    }
                }
            }
            else
            {
                foreach (Type type in FacetRepository.GetFacetSupportedTypes (facetType))
                {
                    PopulateTargetSetBasedOnType (objectSet, type);
                }
            }
        }

        TargetSetCollection m_TargetSets;

        /// <summary>
        /// Collection of TargetSet objects.
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(TargetSet))]
        public TargetSetCollection TargetSets
        {
            get
            {
                if (m_TargetSets == null)
                {
                    ServerComparer cmp = null;

                    // if state is not pending and we are not disconnected connected
                    if (this.State != SfcObjectState.Pending && this.Parent != null)
                    {
                        cmp = new ServerComparer(this.Parent.SqlStoreConnection == null ? null : this.Parent.SqlStoreConnection.ServerConnection, "msdb");
                    }

                    m_TargetSets = new TargetSetCollection(this, cmp);
                }
                return m_TargetSets;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetConnection"></param>
        /// <returns></returns>
        public IEnumerable CalculateTargets(ISfcConnection targetConnection)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CalculateTargets", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(targetConnection);
                SfcObjectQuery targetSetQuery = Utils.GetQueryFromDomainInfo(GetTargetDomain(), targetConnection);

                foreach (TargetSet targetSet in this.TargetSets)
                {
                    if (targetSet.Enabled)
                    {
                        string filter = targetSet.GetFilter();
                        SfcQueryExpression sfcQueryExpression = new SfcQueryExpression(filter);

                        IEnumerable targetObjects = targetSetQuery.ExecuteIterator(sfcQueryExpression, null, null);
                        foreach (object targetObject in targetObjects)
                        {
                            yield return targetObject;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The following function gets the domain of the targets belonging to the target set.
        /// We assume all targets in a targetset have the same domain.
        /// </summary>
        /// <returns></returns>
        internal SfcDomainInfo GetTargetDomain()
        {
            foreach (TargetSet ts in TargetSets)
            {
                return Utils.GetDomainFromUrnSkeleton((string)ts.Properties["TargetTypeSkeleton"].Value);
            }

            traceContext.DebugAssert(false, "TargetSet must include at least one type");
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetConnection"></param>
        /// <param name="sfcQueryExpression"></param>
        /// <returns></returns>
        public static IEnumerable CalculateTargets(ISfcConnection targetConnection, SfcQueryExpression sfcQueryExpression)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CalculateTargets", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(targetConnection, sfcQueryExpression);
                SqlStoreConnection sqlStoreTargetConnection = Utils.GetSqlStoreConnection(targetConnection, "ObjectSet.CalculateTargets");
                SMO.Server domainRoot = new SMO.Server(sqlStoreTargetConnection.ServerConnection);
                SfcObjectQuery targetSetQuery = new SfcObjectQuery(domainRoot);

                IEnumerable targetObjects = targetSetQuery.ExecuteIterator(sfcQueryExpression, null, null);
                foreach (object targetObject in targetObjects)
                {
                    yield return targetObject;
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlStoreConnection"></param>
        /// <param name="policyCategory"></param>
        /// <returns></returns>
        public IEnumerable CalculateTargets(SqlStoreConnection sqlStoreConnection, string policyCategory)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("CalculateTargets", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(sqlStoreConnection, policyCategory);
                if (sqlStoreConnection == null)
                {
                    // One of the first things the code does below is get the ServerConnection from this object, thus it should not be null
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("sqlStoreConnection"));
                }

                PolicyCategory pc = null;
                if (!String.IsNullOrEmpty(policyCategory))
                {

                    if (this.Parent.SqlStoreConnection == null || this.Parent.SqlStoreConnection != sqlStoreConnection)
                    {
                        // when we are executing against a remote server we need to use the remote category, if it is there
                        // when we run from file every connection treated as remote

                        PolicyStore ps = new PolicyStore(sqlStoreConnection);
                        if (ps.PolicyCategories.Contains(policyCategory))
                        {
                            pc = ps.PolicyCategories[policyCategory];
                        }

                    }
                    else
                    {
                        // on local instance we expect Category to be there

                        if (!this.Parent.PolicyCategories.Contains(policyCategory))
                        {
                            throw methodTraceContext.TraceThrow(new MissingObjectException(ExceptionTemplatesSR.Category, policyCategory));
                        }

                        pc = this.Parent.PolicyCategories[policyCategory];

                    }
                }


                SfcDomainInfo domainInfo = GetTargetDomain();
                SfcObjectQuery policyQuery = Utils.GetQueryFromDomainInfo(domainInfo, sqlStoreConnection);
                SfcQueryExpression filter = null;
                SMO.Server server = null;


                foreach (TargetSet targetSet in this.TargetSets)
                {
                    if (targetSet.Enabled)
                    {
                        if (Utils.IsSmoDomain(domainInfo))
                        {
                            server = new Smo.Server(sqlStoreConnection.ServerConnection);
                            filter = GetAdjustedFilter(targetSet, server, pc);
                        }
                        else
                        {
                            filter = new SfcQueryExpression(targetSet.GetFilter());
                        }


                        if (null != filter)
                        {
                            // WARNING Temporary solution: The below change is made because of a bug in SMO query processor
                            // The database type has a special code path that looks for the Status property and
                            // has special logic around the Status attribute.  Since we are not confident to change
                            // the SMO code so close to the release, we are introducing this temporary solution to get around
                            // SMO limitations for the database object.  A specific example of the broken functionality
                            // is that SMO will not return AutoClose databases when the Status property is requested
                            // as the feilds to the object query.
                            // The SMO bug is being tracked in VSTS 217697.
                            string[] nonExpensiveProperties = null;
                            if (Utils.IsSmoDomain(domainInfo) && !targetSet.TargetsDatabases)
                            {
                                traceContext.DebugAssert(server != null, "Server object shouldn't be null");
                                nonExpensiveProperties = GetNonExpensiveProps(filter, server);
                            }

                            foreach (object o in policyQuery.ExecuteIterator(filter, nonExpensiveProperties, null))
                            {
                                yield return o;
                            }

                        }
                    }
                }
            }
        }


        /// <summary>
        /// Helper function that calculates what are the non-expensive properties 
        /// that are associated with a certain Urn.
        /// Note that in the event of an empty set we are returning null.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static string[] GetNonExpensiveProps(SfcQueryExpression filter, Smo.Server server)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetNonExpensiveProps"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(filter, server);
                RequestObjectInfo roi = new RequestObjectInfo(filter.ToString(), RequestObjectInfo.Flags.Properties);
                ObjectInfo oi = new Enumerator().Process(server.ConnectionContext, roi);
                List<string> props = new List<string>(oi.Properties.Length);
                foreach (ObjectProperty op in oi.Properties)
                {
                    if (!op.Expensive)
                    {
                        props.Add(op.Name);
                    }
                }

                string[] requestProps = props.ToArray();
                methodTraceContext.TraceParameterOut("returnVal", requestProps.Length > 0 ? requestProps : null);
                return requestProps.Length > 0 ? requestProps : null;
            }
        }

        [SuppressMessage("Microsoft.Security", "CA2116:AptcaMethodsShouldOnlyCallAptcaMethods",
     Justification = "Filed VSTS 242487")]
        internal static void CalculateTargets(SqlStoreConnection targetConnection,
                                              SfcQueryExpression sfcQueryExpression,
                                              Condition condition,
                                              AdHocPolicyEvaluationMode evaluationMode,
                                              out object[] conforming,
                                              out TargetEvaluation[] violating)
        {
            conforming = null;
            violating = null;

            SfcDomainInfo domainInfo = SfcRegistration.Domains[sfcQueryExpression.Expression[0].Name];
            traceContext.DebugAssert(domainInfo != null, "domain can't be null");

            SfcObjectQuery targetSetQuery = Utils.GetQueryFromDomainInfo(domainInfo, targetConnection);

            IEnumerable targetObjects = targetSetQuery.ExecuteIterator(sfcQueryExpression, null, null);

            CalculateTargets(targetObjects, condition, evaluationMode, out conforming, out violating);
        }

        internal static void CalculateTargets(IEnumerable objectSet,
                                              Condition condition,
                                              AdHocPolicyEvaluationMode evaluationMode,
                                              out object[] conforming,
                                              out TargetEvaluation[] violating)
        {
            traceContext.TraceMethodEnter("CalculateTargets");
            // Tracing Input Parameters
            traceContext.TraceParameters(objectSet, condition, evaluationMode);
            conforming = null;
            violating = null;

            bool evaluationResult = false;

            ArrayList conformingTargets = new ArrayList();
            ArrayList violatingTargets = new ArrayList();

            foreach (object currTarget in objectSet)
            {
                evaluationResult = condition.Evaluate(currTarget, evaluationMode);
                if (evaluationResult == true)
                    conformingTargets.Add(currTarget);
                else
                    violatingTargets.Add(new TargetEvaluation(currTarget, condition.ExpressionNode.DeepClone()));
            }

            if (conformingTargets.Count > 0)
                conforming = conformingTargets.ToArray();
            if (violatingTargets.Count > 0)
                violating = (TargetEvaluation[])violatingTargets.ToArray(typeof(TargetEvaluation));

            traceContext.TraceParameterOut("conforming", conforming);
            traceContext.TraceParameterOut("violating", violating);
            traceContext.TraceMethodExit("CalculateTargets");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetConnection"></param>
        /// <param name="condition"></param>
        /// <param name="evaluationMode"></param>
        /// <param name="policyCategory"></param>
        /// <param name="conforming"></param>
        /// <param name="violating"></param>
        public void CalculateTargets(SqlStoreConnection targetConnection,
                                     Condition condition,
                                     AdHocPolicyEvaluationMode evaluationMode,
                                     string policyCategory,
                                     out object[] conforming,
                                     out TargetEvaluation[] violating)
        {
            conforming = null;
            violating = null;

            if ((condition == null) || (condition.ExpressionNode == null))
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ConditionIsNull));

            if (this.Facet != condition.Facet)
            {
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ObjectSetAndConditionFacetMismatch(
                    this.Name, this.Facet, condition.Name, condition.Facet)));
            }

            IEnumerable objectSet = CalculateTargets(targetConnection, policyCategory);

            CalculateTargets(objectSet, condition, evaluationMode, out conforming, out violating);
        }

        /// <summary>
        /// Verifies that all TargetSets target Databases or objects under Databases
        /// </summary>
        internal bool TargetsDatabaseObjects
        {
            get
            {
                foreach (TargetSet tf in TargetSets)
                {
                    if (!tf.TargetsDatabaseObjects)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Verifies that all TargetSets can be used in Enforce and CoC modes
        /// </summary>
        internal bool IsEventingFilter()
        {
            foreach (TargetSet tf in TargetSets)
            {
                if (!tf.IsEventingFilter())
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// DO NOT USE outside of ObjectSet!
        /// This is essentially private method, exposed as INTERNAL for testing purposes only.
        /// Generates ObjectQuery for TargetSet honoring category subscriptions, system object and database accessibility rules
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="server"></param>
        /// <param name="pc"></param>
        /// <returns></returns>
        internal SfcQueryExpression GetAdjustedFilter(TargetSet ts, Smo.Server server, PolicyCategory pc)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetAdjustedFilter"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(ts, server, pc);
                ExpressionNode dbExpression = null;
                Dictionary<string, ExpressionNode> adjustments = new Dictionary<string, ExpressionNode>();
                bool targetsDb = ts.TargetsDatabaseObjects;
                bool dbIsLeaf = false;
                bool targetsSingleDb = false;
                bool targetsSpecificObjects = false;
                bool checkSystem = false;
                bool systemRequested = false;
                bool accessibilityRequested = false;

                SfcQueryExpression originalFilter = new SfcQueryExpression(ts.GetFilter());

                if (ts.RootLevel == ts.TargetTypeSkeleton)
                {
                    // if TS on Root Categories and System don't apply
                    methodTraceContext.TraceParameterOut("returnVal", originalFilter);
                    return originalFilter;
                }

                // first deal with Categories
                if (targetsDb)
                {
                    TargetSetLevel tsl = ts.GetLevel(TargetSet.DatabaseLevel);
                    traceContext.DebugAssert(tsl != null, "Null Database Level in TargetSet targeting databases");
                    if (!String.IsNullOrEmpty(tsl.Condition))
                    {
                        dbExpression = this.Parent.Conditions[tsl.Condition].ExpressionNode;
                    }

                    if ((pc != null) && (!pc.MandateDatabaseSubscriptions))
                    {
                        // adjust the filter to include the category

                        // Check the simple case first
                        // See if database filter is of type {@Name = 'dbname'} 
                        // and database 'dbname' subscribes to policy's category
                        if (null != dbExpression)
                        {
                            if (dbExpression.NameConditionType == NameConditionType.Equal)
                            {
                                foreach (string target in pc.Parent.PolicyCategorySubscriptions.EnumCategorySubscribers (pc.Name, "DATABASE"))
                                {
                                    if (dbExpression.ObjectName == target)
                                    {
                                        targetsSingleDb = true;
                                        break;
                                    }
                                }

                                // couldn't find a match
                                if (!targetsSingleDb)
                                {
                                    methodTraceContext.TraceParameterOut ("returnVal", null);
                                    return null;
                                }
                            }
                        }

                        // If simple options don't work, build expression for all subscriptions
                        if (!targetsSingleDb)
                        {
                            ExpressionNode allSubNode = null;
                            int subCount = 0;

                            foreach (string target in pc.Parent.PolicyCategorySubscriptions.EnumCategorySubscribers (pc.Name, "DATABASE"))
                            {
                                ++subCount;

                                ExpressionNode subnode = new ExpressionNodeOperator(OperatorType.EQ,
                                    new ExpressionNodeAttribute("Name"),
                                    new ExpressionNodeConstant(target));

                                if (null == allSubNode)
                                {
                                    allSubNode = subnode;
                                }
                                else
                                {
                                    ExpressionNodeOperator orop = new ExpressionNodeOperator(OperatorType.OR,
                                        subnode, allSubNode);
                                    allSubNode = orop;
                                }
                            }

                            if (null == allSubNode)
                            {
                                // there are no subscriptions

                                methodTraceContext.TraceParameterOut("returnVal", null);
                                return null;
                            }

                            if (null == dbExpression)
                            {
                                // There is no DB filter in TS 
                                // use subscriptions QE

                                adjustments.Add(TargetSet.DatabaseLevel, allSubNode);
                                targetsSingleDb = (subCount == 1);
                            }
                            else
                            {
                                // Combine TS filter with subscriptions filter
                                // use Group to make sure logical operators work as intended

                                ExpressionNode filter = new ExpressionNodeOperator(OperatorType.AND,
                                    new ExpressionNodeGroup(dbExpression),
                                    new ExpressionNodeGroup(allSubNode));

                                adjustments.Add(TargetSet.DatabaseLevel, filter);

                                // Don't set targetsSingleDb here, because even if we have 1 db, there is some property in the filter
                            }
                        }
                    }
                }


                // Look at System and Accessibility

                ExpressionNode lastFilterNode = null;
                ExpressionNode dbFilterNode = null;

                // Check leaf level
                if (targetsDb && ts.TargetsDatabases)
                {
                    dbIsLeaf = true;

                    if (!adjustments.ContainsKey(TargetSet.DatabaseLevel))
                    {
                        // there are no adjustemts - use original db filter

                        lastFilterNode = dbExpression;
                    }
                    else
                    {
                        // else use composed filter

                        lastFilterNode = adjustments[TargetSet.DatabaseLevel];
                    }

                    dbFilterNode = lastFilterNode;
                }
                else
                {
                    TargetSetLevel ltsl = ts.GetLevel(ts.TargetTypeSkeleton);
                    if (!String.IsNullOrEmpty(ltsl.Condition))
                    {
                        lastFilterNode = this.Parent.Conditions[ltsl.Condition].ExpressionNode;
                    }

                    if (targetsDb)
                    {
                        if (adjustments.ContainsKey(TargetSet.DatabaseLevel))
                        {
                            dbFilterNode = adjustments[TargetSet.DatabaseLevel];
                        }
                        else
                        {
                            dbFilterNode = dbExpression;
                        }
                    }
                }

                if (!targetsSingleDb && null != dbFilterNode)
                {
                    // if we didn't set it above (no category)
                    // let's verify current dbFilter

                    targetsSingleDb = (dbFilterNode.NameConditionType == NameConditionType.Equal);
                }

                // Analyize the last filter
                if (null != lastFilterNode)
                {
                    if (!(targetsDb && dbIsLeaf && targetsSingleDb))
                    {
                        int idObjectCount = 0;
                        foreach (AttributeOperatorPair pair in lastFilterNode.EnumAttributeOperatorPairs())
                        {
                            if (pair.Attribute == "IsSystemObject")
                            {
                                systemRequested = true;
                            }
                            else if (pair.Attribute == "IsAccessible")
                            {
                                accessibilityRequested = true;
                            }
                            else if ((pair.Attribute == "Name" && pair.OpType == OperatorType.EQ)
                                || (pair.Attribute == "ID" && pair.OpType == OperatorType.EQ))
                            {
                                targetsSpecificObjects = true;
                                ++idObjectCount;
                            }
                        }

                        checkSystem = !(systemRequested || targetsSpecificObjects);

                        if (targetsDb && dbIsLeaf && !targetsSingleDb && idObjectCount == 1)
                        {
                            targetsSingleDb = true;
                        }
                    }
                }
                else
                {
                    checkSystem = true;
                }

                // If there are levels under DB we need to analyze DB level as well
                if (targetsDb && !dbIsLeaf && !targetsSingleDb && null != dbFilterNode)
                {
                    int idObjectCount = 0;
                    foreach (AttributeOperatorPair pair in dbFilterNode.EnumAttributeOperatorPairs())
                    {
                        if (pair.Attribute == "IsAccessible")
                        {
                            accessibilityRequested = true;
                        }
                        else if ((pair.Attribute == "Name" && pair.OpType == OperatorType.EQ)
                            || (pair.Attribute == "ID" && pair.OpType == OperatorType.EQ))
                        {
                            ++idObjectCount;
                        }
                    }

                    targetsSingleDb = (idObjectCount == 1);
                }


                if (checkSystem)
                {
                    // calculate if the object has this property
                    bool hasIsSystemObject = false;
                    RequestObjectInfo roi = new RequestObjectInfo(originalFilter.ToString(), RequestObjectInfo.Flags.Properties);
                    ObjectInfo oi = new Enumerator().Process(server.ConnectionContext, roi);
                    foreach (ObjectProperty op in oi.Properties)
                    {
                        if (op.Name == "IsSystemObject")
                        {
                            hasIsSystemObject = true;
                            break;
                        }
                    }

                    // if the object does not have this property then 
                    if (hasIsSystemObject)
                    {
                        ExpressionNode nonSystemObject = new ExpressionNodeOperator(OperatorType.EQ,
                            new ExpressionNodeAttribute("IsSystemObject"),
                            ExpressionNode.ConstructNode(false));

                        if (adjustments.ContainsKey(ts.TargetTypeSkeleton))
                        {
                            adjustments.Remove(ts.TargetTypeSkeleton);
                        }

                        // add the @IsSystemObject=false() to the filter
                        if (null == lastFilterNode)
                        {
                            adjustments.Add(ts.TargetTypeSkeleton, nonSystemObject);
                        }
                        else
                        {
                            // newFilter = oldFilter AND nonSystemObject
                            ExpressionNode filterNode = new ExpressionNodeOperator(OperatorType.AND,
                                nonSystemObject,
                                new ExpressionNodeGroup(lastFilterNode));

                            adjustments.Add(ts.TargetTypeSkeleton, filterNode);
                        }
                    }
                }

                // Database Accessibility
                if (targetsDb && !targetsSingleDb && !accessibilityRequested)
                {
                    ExpressionNode dbNode = null;
                    ExpressionNode accessibleDatabase = new ExpressionNodeOperator(OperatorType.EQ,
                        new ExpressionNodeAttribute("IsAccessible"),
                        ExpressionNode.ConstructNode(true));

                    if (adjustments.ContainsKey(TargetSet.DatabaseLevel))
                    {
                        dbNode = adjustments[TargetSet.DatabaseLevel];
                        adjustments.Remove(TargetSet.DatabaseLevel);
                    }
                    else
                    {
                        dbNode = dbFilterNode;
                    }

                    if (null != dbNode)
                    {
                        ExpressionNode newNode = new ExpressionNodeOperator(OperatorType.AND,
                            accessibleDatabase,
                            new ExpressionNodeGroup(dbNode));

                        adjustments.Add(TargetSet.DatabaseLevel, newNode);
                    }
                    else
                    {
                        adjustments.Add(TargetSet.DatabaseLevel, accessibleDatabase);
                    }
                }

                return new SFC.SfcQueryExpression(ts.GetFilterWithNodeReplacement(adjustments));
            }
        }

        /// <summary>
        /// ! This method is used exclusively by PolicyEvaluationHelper and works in SQLCLR only !
        /// 
        /// Adjusts given QueryExpression to exclude System objects
        /// unless object name specified explicitly in policy's filter (currently could only be a DB)
        /// </summary>
        /// <param name="ver"></param>
        /// <param name="policy"></param>
        /// <param name="targetQueryExpression"></param>
        /// <returns></returns>
        internal static SfcQueryExpression AdjustForSystem (Version ver, Policy policy, SfcQueryExpression targetQueryExpression)
        {
            // protect against callers not in SQLCLR
            if (!Microsoft.SqlServer.Server.SqlContext.IsAvailable)
            {
                throw new DmfException(ExceptionTemplatesSR.OnlyInSqlClr);
            }

            SfcQueryExpression adjustedExpression = targetQueryExpression;

            ServerVersion v = new ServerVersion (ver.Major, ver.Minor);


            ObjectSet os = policy.Parent.ObjectSets[policy.ObjectSet];
            TargetSet ts = null;

            // We assume ObjectSet can only have 1 TargetSet
            traceContext.DebugAssert (1 == os.TargetSets.Count, "Only 1 TS expected");
            foreach (TargetSet hts in os.TargetSets)
            {
                ts = hts;
                break;
            }

            bool targetsDb = ts.TargetsDatabases;

            if (targetsDb)
            {
                TargetSetLevel tsl = ts.GetLevel (TargetSet.DatabaseLevel);
                if (!String.IsNullOrEmpty (tsl.Condition))
                {
                    Condition c = policy.Parent.Conditions[tsl.Condition];
                    if (c.ExpressionNode.NameConditionType == NameConditionType.Equal)
                    {
                        // If we have filter requiring particular database we shouldn't do anything
                        return targetQueryExpression;
                    }
                }
            }


            // calculate if the object has System property
            RequestObjectInfo roi = new RequestObjectInfo (targetQueryExpression.ToString (), RequestObjectInfo.Flags.Properties);
            ObjectInfo oi = new Enumerator ().Process (v, roi);

            foreach (ObjectProperty op in oi.Properties)
            {
                if (op.Name == "IsSystemObject")
                {
                    ExpressionNode nonSystemObject = new ExpressionNodeOperator (OperatorType.EQ,
                        new ExpressionNodeAttribute ("IsSystemObject"),
                        ExpressionNode.ConstructNode (false));

                    ExpressionNode leafNode = ExpressionNode.ConvertFromFilterNode (targetQueryExpression.Expression[targetQueryExpression.Expression.Length-1].Filter);
                    ExpressionNode adjustedNode = new ExpressionNodeOperator (OperatorType.AND,
                        new ExpressionNodeGroup (leafNode),
                        nonSystemObject);
                    targetQueryExpression.Expression[targetQueryExpression.Expression.Length-1].Filter = adjustedNode.ConvertToFilterNode ();
                    adjustedExpression = new SfcQueryExpression (targetQueryExpression.Expression.ToString ());
                    break;
                }
            }

            return adjustedExpression;
        }

        internal bool DependsOnCondition(string conditionName)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("DependsOnCondition"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(conditionName);
                foreach (TargetSet ts in this.TargetSets)
                {
                    foreach (TargetSetLevel tsLevel in ts.Levels)
                    {
                        if (tsLevel.Condition == conditionName)
                        {
                            methodTraceContext.TraceParameterOut("returnVal", true);
                            return true;
                        }
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", false);
                return false;
            }
        }

        internal ReadOnlyCollection<Policy> EnumDependentPolicies()
        {
            List<Policy> dependentPolicies = new List<Policy>();
            foreach (Policy p in this.Parent.Policies)
            {
                if (p.ObjectSet == this.Name)
                {
                    dependentPolicies.Add(p);
                }
            }

            return dependentPolicies.AsReadOnly();
        }

        internal ReadOnlyCollection<string> EnumReferencedConditionNames()
        {
            List<string> referencedConditions = new List<string>();
            foreach (TargetSet ts in this.TargetSets)
            {
                foreach (TargetSetLevel level in ts.Levels)
                {
                    if (!string.IsNullOrEmpty(level.Condition))
                    {
                        if (!referencedConditions.Contains(level.Condition))
                        {
                            referencedConditions.Add(level.Condition);
                        }
                    }
                }
            }

            return referencedConditions.AsReadOnly();
        }

        internal bool HasScript
        {
            get
            {
                foreach (TargetSet ts in this.TargetSets)
                {
                    foreach (TargetSetLevel level in ts.Levels)
                    {
                        if (!string.IsNullOrEmpty(level.Condition) &&
                            this.Parent.Conditions[level.Condition].HasScript)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        internal string RootLevel
        {
            get
            {
                foreach (TargetSet ts in this.TargetSets)
                {
                    return ts.RootLevel;
                }

                return String.Empty;
            }
        }


        /// <summary>
        /// Generates unique (at the moment of request) ObjectSet name based on Policy name.
        /// Policy has to be parented (otherwise uniqueness cannot be verified)
        /// </summary>
        /// <param name="policy"></param>
        /// <returns>generated name, NULL if policy has no Parent</returns>
        public static string GenerateUniqueName(Policy policy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GenerateUniqueName"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policy);
                if (null == policy.Parent)
                {
                    methodTraceContext.TraceParameterOut("returnVal", null);
                    return null;
                }

                string name, nameBase;
                int suffix = 1;
                if (String.IsNullOrEmpty(policy.Name))
                {
                    nameBase = "policy_ObjectSet";
                }
                else
                {
                    nameBase = policy.Name.Length > 112 ? policy.Name.Substring(0, 112) + "_ObjectSet" : policy.Name + "_ObjectSet";
                }

                name = nameBase;

                while (policy.Parent.ObjectSets.Contains(name))
                {
                    // we should be safe running up to 99999 in case of long name and to maxint in case of short one
                    name = nameBase + "_" + suffix.ToString("#", System.Globalization.CultureInfo.InvariantCulture);
                    suffix++;
                }

                methodTraceContext.TraceParameterOut("returnVal", name);
                return name;
            }
        }


        #region ISfcDiscoverObject Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException("sink");
            }

            if (sink.Action == SfcDependencyAction.Serialize)
            {
                TargetSetCollection targetSetColl = this.TargetSets;
                if (targetSetColl != null)
                {
                    sink.Add(SfcDependencyDirection.Inbound, targetSetColl.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                }
            }

            return;
        }

        #endregion

        #region ISfcCreatable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptCreate()
        {
            SfcTSqlScript dmfScript = new SfcTSqlScript();

            List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(this.Name.GetType(), this.Name));
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(this.Facet.GetType(), this.Facet));
            args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), 0));
            dmfScript.AddBatch(scriptCreateAction.GenerateScript(this, args));

            bool declareArguments = true;
            foreach (TargetSet ts in this.TargetSets)
            {
                dmfScript.AddBatch(ts.ScriptCreate(declareArguments).ToString());
                declareArguments = false;
            }

            return dmfScript;
        }

        #endregion

        #region ICreatable Members

        /// <summary>
        /// 
        /// </summary>
        public void Create()
        {
            traceContext.TraceMethodEnter("Create");
            Validate(ValidationMethod.Create);

            base.CreateImpl();
            this.TargetSets.Refresh();
            traceContext.TraceMethodExit("Create");
        }

        /// <summary>
        /// Perform post-create action
        /// </summary>
        protected override void PostCreate(object executionResult)
        {
            traceContext.TraceMethodEnter("PostCreate");
            // Tracing Input Parameters
            traceContext.TraceParameters(executionResult);
            this.Properties["ID"].Value = executionResult;
            traceContext.TraceMethodExit("PostCreate");
        }
        #endregion

        /// <summary>
        /// Scripts Create with all referenced Conditions, excluding policyCondition,
        /// in case it's refernced by both Policy and OS
        /// </summary>
        /// <param name="policyCondition">Policy's Condition name</param>
        /// <returns></returns>
        internal ISfcScript ScriptCreateWithDependencies(string policyCondition)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptCreateWithDependencies"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policyCondition);
                SfcTSqlScript sfcScript = new SfcTSqlScript();

                foreach (string condition in this.EnumReferencedConditionNames())
                {
                    // EnumReferencedConditionNames returns unique names
                    // so we only need to check the condition is not PolicyCondition
                    // we do binary comparisons of names to avoid unnecessary complexity
                    if (condition != policyCondition)
                    {
                        sfcScript.AddBatch(this.Parent.Conditions[condition].ScriptCreate().ToString());
                    }
                }

                sfcScript.AddBatch(this.ScriptCreate().ToString());

                methodTraceContext.TraceParameterOut("returnVal", sfcScript);
                return sfcScript;
            }
        }


        #region ISfcDroppable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptDrop()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptDrop", System.Diagnostics.TraceEventType.Information))
            {
                string script = scriptDropAction.GenerateScript(this);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                SfcTSqlScript dmfScript = new SfcTSqlScript(script);
                methodTraceContext.TraceParameterOut("returnVal", dmfScript);
                return dmfScript;
            }
        }

        #endregion

        #region IDroppable Members

        /// <summary>
        /// 
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        #endregion

        #region ISfcAlterable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptAlter()
        {
            SfcTSqlScript dmfScript = new SfcTSqlScript();

            foreach (TargetSet ts in this.TargetSets)
            {
                dmfScript.AddBatch(ts.ScriptAlter().ToString());
            }

            return dmfScript;
        }

        #endregion

        #region IAlterable Members

        /// <summary>
        /// 
        /// </summary>
        public void Alter()
        {
            traceContext.TraceMethodEnter("Alter");
            Validate(ValidationMethod.Alter);
            base.AlterImpl();
            traceContext.TraceMethodExit("Alter");
        }

        #endregion

        #region Generated Part To Be Removed
        internal const string typeName = "ObjectSet";
        /// <summary>
        /// 
        /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
        public sealed class Key : SfcKey
        {
            /// <summary>
            /// Properties
            /// </summary>
            private string keyName;

            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public Key()
            {
            }

            /// <summary>
            /// Constructors
            /// </summary>
            /// <param name="other"></param>
            public Key(Key other)
            {
                keyName = other.Name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            public Key(string name)
            {
                keyName = name;
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                keyName = (string)filedDict["Name"];
            }

            /// <summary>
            /// 
            /// </summary>
            public string Name
            {
                get
                {
                    return this.keyName;
                }
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

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return this.Name.GetHashCode();
            }

            private bool IsEqual(Key key)
            {
                return string.CompareOrdinal(this.Name, key.Name) == 0;
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format("{0}[@Name='{1}']", ObjectSet.typeName, SfcSecureString.EscapeSquote(Name));
            }

        } // public class Key

        // Singleton factory class
        sealed class ObjectFactory : SfcObjectFactory
        {
            static readonly ObjectFactory instance = new ObjectFactory();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static ObjectFactory() { }

            ObjectFactory() { }

            public static ObjectFactory Instance
            {
                get { return instance; }
            }

            protected override SfcInstance CreateImpl()
            {
                return new ObjectSet();
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new PolicyStore Parent
        {
            get { return (PolicyStore)base.Parent; }
            set
            {
                traceContext.TraceVerbose("Setting Parent to: {0}", value);
                base.Parent = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override SfcKey CreateIdentityKey()
        {
            Key key = null;
            // if we don't have our key values we can't create a key
            if (this.Name != null)
            {
                key = new Key(this.Name);
            }
            return key;
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
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
        [SfcKey(0)]
        public string Name
        {
            get
            {
                return (string)this.Properties["Name"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void SetName(string name)
        {
            traceContext.TraceMethodEnter("SetName");
            // Tracing Input Parameters
            traceContext.TraceParameters(name);
            this.Properties["Name"].Value = name;
            traceContext.TraceMethodExit("SetName");
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public int ID
        {
            get
            {
                object value = this.Properties["ID"].Value;
                if (value == null)
                    return 0;
                return (int)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string Facet
        {
            get
            {
                return (string)this.Properties["Facet"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Facet to: {0}", value);
                if (!String.IsNullOrEmpty(this.Facet))
                {
                    // TODO: Throw exception that you can't update the Facet on an object set once it has been
                    // set, you have to drop it and recreate it
                    // TODO: Check if there is an SFC property settting that allows us to make this ready only once set.
                    // TODO: Make sure you accomodate the scenario where you create a policy with a given name and object set
                    // and then you want to change the facet (condition and object set) of the policy. You should be able to
                    // do that.
                    throw traceContext.TraceThrow(new Exception("Cannot update the Facet for a given ObjectSet. Drop and create a new one"));
                }

                this.Properties["Facet"].Value = value;

                PopulateTargetSetsBasedOnFacet(this, FacetRepository.GetFacetType(value));

                // Automatically enable the first TargetSet if it is the only one
                if (this.TargetSets.Count == 1)
                {
                    foreach (TargetSet ts in TargetSets)
                    {
                        ts.Enabled = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Constructs ObjectSet, using domain to generate TargetSet paths
        /// </summary>
        /// <param name="facet"></param>
        /// <param name="domain"></param>
        public void SetFacetWithDomain(string facet, string domain)
        {
            if (String.IsNullOrEmpty(domain))
            {
                throw new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("domain"));
            }

            if (!String.IsNullOrEmpty(this.Facet))
            {
                throw new DmfException(ExceptionTemplatesSR.CannotChangeFacetForObjectSet);
            }

            SfcDomainInfo di = SfcRegistration.Domains[domain];

            PopulateTargetSetsBasedOnFacet(this, FacetRepository.GetFacetType(facet), di);

            this.Properties["Facet"].Value = facet;

            // Automatically enable the first TargetSet if it is the only one
            if (this.TargetSets.Count == 1)
            {
                foreach (TargetSet ts in TargetSets)
                {
                    ts.Enabled = true;
                    break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public bool IsSystemObject
        {
            get
            {
                object value = this.Properties["IsSystemObject"].Value;
                if (value == null)
                    return false;
                return (bool)value;
            }
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
                case TargetSet.typeName:
                    return this.TargetSets;
                default: throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
            }
        }
        #endregion

    }
}
