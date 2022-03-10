// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Diagnostics.STrace;
using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Collections.Generic;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Smo;
using System.Text.RegularExpressions;
using SFC = Microsoft.SqlServer.Management.Sdk.Sfc;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the Policy class.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed partial class Policy : SfcInstance, ISfcCreatable, ISfcDroppable, ISfcAlterable, ISfcRenamable, IRenamable
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "Policy");

        static readonly SfcTsqlProcFormatter scriptCreateAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptAlterAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptDropAction = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptRenameAction = new SfcTsqlProcFormatter();

        static Policy()
        {
            // Create script
            scriptCreateAction.Procedure = "msdb.dbo.sp_syspolicy_add_policy";
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_name", "Condition", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category", "PolicyCategory", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("help_text", "HelpText", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("help_link", "HelpLink", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("schedule_uid", "ScheduleUid", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("execution_mode", "AutomatedPolicyEvaluationMode", true, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("is_enabled", "Enabled", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_id", "ID", false, true));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("root_condition_name", "RootCondition", false, false));
            scriptCreateAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_set", "ObjectSet", false, false));

            // Update script
            scriptAlterAction.Procedure = "msdb.dbo.sp_syspolicy_update_policy";
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_id", "ID", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("condition_name", "Condition", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("execution_mode", "AutomatedPolicyEvaluationMode", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_category", "PolicyCategory", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("schedule_uid", "ScheduleUid", true, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("description", "Description", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("help_text", "HelpText", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("help_link", "HelpLink", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("is_enabled", "Enabled", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("root_condition_name", "RootCondition", false, false));
            scriptAlterAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("object_set", "ObjectSet", false, false));

            // Drop script
            scriptDropAction.Procedure = "msdb.dbo.sp_syspolicy_delete_policy";
            scriptDropAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_id", "ID", true, false));

            // Rename script
            scriptRenameAction.Procedure = "msdb.dbo.sp_syspolicy_rename_policy";
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("name", "Name", true, false));
            scriptRenameAction.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("new_name", true));
        }

        //cache used only for Alter, to remember the previous value of execmode before Alter may change it. 
        //Semantics is that this cache value will always be same as what is in backend for this policy. 
        //No semantics guaranteed in any scenario other than Alter.
        internal AutomatedPolicyEvaluationMode execModeInBackend = AutomatedPolicyEvaluationMode.None;
        private bool execModeCacheSet = false;
        private const string EvaluationModePropertyName = "AutomatedPolicyEvaluationMode";

        /// <summary>
        /// Default constructor used for deserialization. VSTS 55852.
        /// </summary>
        public Policy()
        {
            InitializeEventSubscription();
        }

        private void InitializeEventSubscription()
        {
            this.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(PolicyPropertyChangedEventHandler);
        }

        // Handles changes to properties in property bag that we are interested in.
        void PolicyPropertyChangedEventHandler(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            traceContext.TraceMethodEnter("PolicyPropertyChangedEventHandler");
            //At some point, the exec mode property has to be
            //retrieved from backend and set in property bag. At that point, property changed event will be fired, for
            //the first time and we cache. After that, the backend value changes only when Alter is issued, we refresh the
            //cache then.
            if (!execModeCacheSet &&
                 e.PropertyName == Policy.EvaluationModePropertyName &&
                 this.Properties[Policy.EvaluationModePropertyName].Retrieved)
            {
                //this also goes to the Properties, but does casting etc for us.
                this.execModeInBackend = this.AutomatedPolicyEvaluationMode;
                this.execModeCacheSet = true;
            }
            traceContext.TraceMethodExit("PolicyPropertyChangedEventHandler");
        }
        /// <summary>
        /// Instantiates a new Policy object.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        public Policy(PolicyStore parent, string name)
        {
            traceContext.TraceMethodEnter("Policy");

            this.Parent = parent;
            SetName(name);

            InitializeEventSubscription();
            traceContext.TraceMethodExit("Policy");
        }

        /// <summary>
        /// Indicates when the log is available
        /// </summary>
        private bool IsLogAvailable
        {
            get
            {
                return (this.ID != 0);
            }
        }

        private bool m_bStatesInitialized = false;

        /// <summary>
        /// 
        /// </summary>
        protected override void InitializeUIPropertyState()
        {
            //Prevent the cyclic calling between this function and the SetEnabled function in case this one is called directly
            if (m_bStatesInitialized)
                return;
            m_bStatesInitialized = true;
            UpdateUIPropertyState();
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void UpdateUIPropertyState()
        {
            /*
             * In case the user is not interested in the states changes, in this case the initialization function was not called, and so
             * we are not interested in updating the states, the whole process will start when the users try to set or get any the value
             * of the "Enabled" property.
             * 
             */
            if (!m_bStatesInitialized)
                return;

            SfcProperty enabled = this.Properties["Enabled"];
            SfcProperty name = this.Properties["Name"];
            SfcProperty automatedPolicyEvaluationMode = this.Properties["AutomatedPolicyEvaluationMode"];
            SfcProperty scheduleUid = this.Properties["ScheduleUid"];
            SfcProperty condition = this.Properties["Condition"];
            SfcProperty rootCondition = this.Properties["RootCondition"];

            //States rules
            if (this.State == SfcObjectState.Pending)
                name.Enabled = true;
            else
                name.Enabled = false;

            if (condition.IsAvailable && condition.Value != null)
            {
                automatedPolicyEvaluationMode.Enabled = true;
            }
            else
            {
                automatedPolicyEvaluationMode.Enabled = false;
            }

            if (automatedPolicyEvaluationMode.IsAvailable && this.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnSchedule)
                scheduleUid.Enabled = true;
            else
                scheduleUid.Enabled = false;


            if ((!string.IsNullOrEmpty(this.Name) && condition.Value != null && this.AutomatedPolicyEvaluationMode != AutomatedPolicyEvaluationMode.None))
            {
                if (this.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnSchedule)
                {
                    if (scheduleUid.IsAvailable && this.ScheduleUid != Guid.Empty)
                    {
                        enabled.Enabled = true;
                    }
                    else
                        enabled.Enabled = false;
                }
                else
                    enabled.Enabled = true;
            }
            else
                enabled.Enabled = false;
        }


        #region CRUD support


        /// <summary>
        /// Creates the object on the server.
        /// </summary>
        public void Create()
        {
            traceContext.TraceMethodEnter("Create");
            Validate(ValidationMethod.Create);
            base.CreateImpl();

            traceContext.TraceMethodExit("Create");
        }

        /// <summary>
        /// Scripts creation of the object on the server.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptCreate()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptCreate", System.Diagnostics.TraceEventType.Information))
            {
                string script = scriptCreateAction.GenerateScript(this);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// Perform post-create action
        /// </summary>
        protected override void PostCreate(object executionResult)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("PostCreate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(executionResult);
                // Guard against disconnected (Offline) mode, which always returns a null executionResult since there is no server communication.
                if (this.GetDomain().ConnectionContext.Mode != SfcConnectionContextMode.Offline)
                {
                    this.Properties["ID"].Value = executionResult;
                }
                this.SetCategoryID();
            }
        }

        /// <summary>
        /// Scripts Create Policy with all dependencies, including ObjectSet and all referenced Conditions
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptCreateWithDependencies()
        {
            SfcTSqlScript sfcScript = new SfcTSqlScript();

            if (!String.IsNullOrEmpty(this.Condition))
            {
                sfcScript.AddBatch(this.Parent.Conditions[this.Condition].ScriptCreate().ToString());
            }
            else
            {
                // Condition is not set 
                // call regular script - that would produce scripting Exception
                // we do it here, so we don't spend time scripting other things
                this.ScriptCreate();
            }

            // There is probability that Condition == RootCondition, so we check
            if (!String.IsNullOrEmpty(this.RootCondition) && this.RootCondition != this.Condition)
            {
                sfcScript.AddBatch(this.Parent.Conditions[this.RootCondition].ScriptCreate().ToString());
            }

            if (!String.IsNullOrEmpty(this.ObjectSet))
            {
                sfcScript.AddBatch(this.Parent.ObjectSets[this.ObjectSet].ScriptCreateWithDependencies(this.Condition).ToString());
            }

            sfcScript.AddBatch(this.ScriptCreate().ToString());

            return sfcScript;
        }

        /// <summary>
        /// Scripts Create Policy with dependent ObjectSet. Doesn't include referenced Conditions.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptCreateWithObjectSet()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptCreateWithObjectSet", System.Diagnostics.TraceEventType.Information))
            {
                SfcTSqlScript sfcScript = new SfcTSqlScript();

                if (!String.IsNullOrEmpty(this.ObjectSet))
                {
                    sfcScript.AddBatch(this.Parent.ObjectSets[this.ObjectSet].ScriptCreate().ToString());
                }

                sfcScript.AddBatch(this.ScriptCreate().ToString());

                methodTraceContext.TraceParameterOut("returnVal", sfcScript);
                return sfcScript;
            }
        }

        /// <summary>
        /// Persists all changes made to this object.
        /// </summary>
        public void Alter()
        {
            traceContext.TraceMethodEnter("Alter");
            Validate(ValidationMethod.Alter);

            base.AlterImpl();

            //refresh the cache now that the value has been persisted to backend
            this.execModeInBackend = this.AutomatedPolicyEvaluationMode;
            traceContext.TraceMethodExit("Alter");
        }

        /// <summary>
        /// Scripts all changes made to this object.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptAlter()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptAlter", System.Diagnostics.TraceEventType.Information))
            {
                string script = scriptAlterAction.GenerateScript(this);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// Perform post-alter action
        /// </summary>
        protected override void PostAlter(object executionResult)
        {
            this.SetCategoryID();
        }

        /// <summary>
        /// Scripts Alter Policy with dependent ObjectSet. Doesn't include referenced Conditions.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptAlterWithObjectSet()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptAlterWithObjectSet", System.Diagnostics.TraceEventType.Information))
            {
                SfcTSqlScript sfcScript = new SfcTSqlScript();

                if (!String.IsNullOrEmpty(this.ObjectSet))
                {
                    sfcScript.AddBatch(this.Parent.ObjectSets[this.ObjectSet].ScriptAlter().ToString());
                }

                sfcScript.AddBatch(this.ScriptAlter().ToString());

                methodTraceContext.TraceParameterOut("returnVal", sfcScript);
                return sfcScript;
            }
        }


        private void SetCategoryID()
        {
            if (String.IsNullOrEmpty(this.PolicyCategory))
            {
                this.Properties["CategoryId"].Value = 0;
            }
            else
            {
                PolicyCategory pc = this.Parent.PolicyCategories[this.PolicyCategory];
                traceContext.DebugAssert(pc != null);
                this.Properties["CategoryId"].Value = pc.ID;
            }
        }

        /// <summary>
        /// Drops the object and removes it from the collection.
        /// </summary>
        public void Drop()
        {
            base.DropImpl();
        }

        /// <summary>
        /// Scripts deletion of the object
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptDrop()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptDrop", System.Diagnostics.TraceEventType.Information))
            {
                string script = scriptDropAction.GenerateScript(this);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        /// <summary>
        /// Scripts Drop Policy with dependent ObjectSet. Doesn't include referenced Conditions.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptDropWithObjectSet()
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ScriptDropWithObjectSet", System.Diagnostics.TraceEventType.Information))
            {
                SfcTSqlScript sfcScript = new SfcTSqlScript();

                sfcScript.AddBatch(this.ScriptDrop().ToString());

                if (!String.IsNullOrEmpty(this.ObjectSet))
                {
                    sfcScript.AddBatch(this.Parent.ObjectSets[this.ObjectSet].ScriptDrop().ToString());
                }

                methodTraceContext.TraceParameterOut("returnVal", sfcScript);
                return sfcScript;
            }
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        public void Rename(string name)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Rename", System.Diagnostics.TraceEventType.Information))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(name);
                if (String.IsNullOrEmpty(name))
                {
                    throw methodTraceContext.TraceThrow(new ArgumentException(ExceptionTemplatesSR.ArgumentNullOrEmpty("Name")));
                }

                base.RenameImpl(new Policy.Key(name));
            }
        }

        /// <summary>
        /// Renames the object on the server.
        /// </summary>
        /// <param name="key"></param>
        void ISfcRenamable.Rename(SfcKey key)
        {
            base.RenameImpl(key);
        }

        ISfcScript ISfcRenamable.ScriptRename(SfcKey key)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ISfcRenamable.ScriptRename"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(key);
                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();

                Policy.Key tkey = (key as Policy.Key);
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(tkey.Name.GetType(), tkey.Name));

                string script = scriptRenameAction.GenerateScript(this, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                return new SfcTSqlScript(script);
            }
        }

        #endregion

        #region EVALUATE
        private bool EvaluateRootCondition(SqlStoreConnection sqlStoreConnection,
            bool checkSqlScriptAsProxy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EvaluateRootCondition"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(sqlStoreConnection, checkSqlScriptAsProxy);
                // This code uses SMO directly
                // It should switch to SFC when it's ready

                object target = null;

                if (!String.IsNullOrEmpty(this.RootCondition))
                {
                    Smo.Server server = new Smo.Server(sqlStoreConnection.ServerConnection);

                    Condition c = this.Parent.Conditions[this.RootCondition];
                    Type facet = FacetRepository.GetFacetType(c.Facet);

                    List<Type> types = FacetRepository.GetFacetSupportedTypes(facet);
                    traceContext.DebugAssert(types.Count > 0);

                    if (types.Count > 1)
                    {
                        throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.TooManyURNsReturned(this.RootCondition)));
                    }

                    if (types[0] != typeof(Smo.Server))
                    {
                        List<string> urns = SfcMetadataDiscovery.GetUrnSkeletonsFromType(types[0]);
                        if (urns.Count > 1)
                        {
                            throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.TooManyURNsReturned(this.RootCondition)));
                        }

                        // SFC doesn't handle singleton requests yet (like "Server/Information")
                        // see VSTS #109114 
                        // Have to use SMO directly
                        //
                        target = server.GetSmoObject(urns[0]);
                    }
                    else
                    {
                        target = server;
                    }

                    if (target == null)
                    {
                        throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.RootConditionFailed(this.RootCondition)));
                    }

                    FacetEvaluationContext context = FacetEvaluationContext.GetFacetEvaluationContext(facet, target);

                    bool result = (bool)c.ExpressionNode.Evaluate(context, checkSqlScriptAsProxy);

                    methodTraceContext.TraceParameterOut("returnVal", result);
                    return result;
                }

                methodTraceContext.TraceParameterOut("returnVal", true);
                return true;
            }
        }

        /// <summary>
        /// Helper function that handles logging and configuration 
        /// of violating targets
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="evaluationMode"></param>
        /// <param name="violating"></param>
        /// <param name="logPolicyEvents"></param>
        /// <returns></returns>
        private bool ProcessViolators(
            Condition condition,
            AdHocPolicyEvaluationMode evaluationMode,
            TargetEvaluation[] violating,
            LogPolicyEvents logPolicyEvents)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ProcessViolators"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(condition, evaluationMode, violating, logPolicyEvents);
                // Irrespective of the evaluation mode there are no objects violating the policy condition.
                // The evaluation has succeeded for this connection
                if ((violating == null) || (violating.Length <= 0))
                {
                    methodTraceContext.TraceParameterOut("returnVal", true);
                    return true;
                }

                // If it's an ad-hoc check then you don't need to do anything else, just return the result
                if (evaluationMode == AdHocPolicyEvaluationMode.Check || evaluationMode == AdHocPolicyEvaluationMode.CheckSqlScriptAsProxy)
                    return (violating.Length == 0);

                traceContext.DebugAssert(evaluationMode == AdHocPolicyEvaluationMode.Configure, "You've already returned the result if you're in Check mode and can't be here other than if you're configuring");

                raiseConditionEvaluationEvent = true;

                bool configureEvaluationResult = true;
                bool targetEvaluationResult = true;
                bool isConfigurable = false;
                string message = String.Empty;
                foreach (TargetEvaluation initialEvaluation in violating)
                {
                    isConfigurable = false;
                    message = String.Empty;
                    try
                    {
                        // We must evaluate the condition against the result of the expression against
                        // this particular target, rather than the status of the last target that we
                        // looped through. This allows condition.Configure to know which properties to
                        // change for this target.
                        condition.ExpressionNode = initialEvaluation.Result;
                        condition.Configure(initialEvaluation.Target);
                        targetEvaluationResult = condition.Evaluate(initialEvaluation.Target, evaluationMode);
                        configureEvaluationResult &= targetEvaluationResult;
                        if (!targetEvaluationResult && !SqlContext.IsAvailable)
                        {
                            // Do not need to check if evaluation succeeded
                            // It's questionable in this context if we have to check it anyway, but let's do it to be consistent with output
                            isConfigurable = condition.CanBeConfigured(ref message);
                        }
                        if ((logPolicyEvents != null) && (configureEvaluationResult == false || this.Parent.LogOnSuccess))
                            logPolicyEvents.LogPolicyEvaluationDetail(condition, new ConditionEvaluationEventArgs(null, condition.Facet, initialEvaluation.Target, targetEvaluationResult, isConfigurable, message, null));
                        if (configureEvaluationResult == false)
                            throw methodTraceContext.TraceThrow(new ExpressionNodeConfigurationException());
                    }
                    catch (Exception e)
                    {
                        methodTraceContext.TraceCatch(e);
                        configureEvaluationResult = false;
                        targetEvaluationResult = false;
                        if (!Utils.ShouldProcessException(e))
                            throw;

                        Exception resultException = e;
                        if (e is SMO.PropertyReadOnlyException)
                        {
                            NonConfigurableReadOnlyPropertyException readOnlyException = new NonConfigurableReadOnlyPropertyException(e.Message, ((SMO.PropertyReadOnlyException)e).PropertyName, e);
                            resultException = readOnlyException;
                        }

                        PolicyEvaluationException evaluationException = new PolicyEvaluationException(ExceptionTemplatesSR.PolicyEvaluationFailed(this.Name), resultException);

                        // TODO: Figure out how to reliably get the server name. The server name is a policy logging thing only
                        // and perhaps shouldn't be in the condition evaluation args at all and could stay within the policy class.
                        string serverName = string.Empty;
                        this.FireTargetProcessed(new TargetProcessedEventArgs(initialEvaluation.Target,
                                                SfcSqlPathUtilities.ConvertUrnToPath(SfcUtility.GetUrn(initialEvaluation.Target)),
                                                targetEvaluationResult,
                                                isConfigurable,
                                                message,
                                                initialEvaluation.Result,
                                                evaluationException,
                                                serverName));
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", configureEvaluationResult);
                return configureEvaluationResult;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluationMode"></param>
        /// <param name="targetObjects"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public bool Evaluate(AdHocPolicyEvaluationMode evaluationMode, params object[] targetObjects)
        {
            if (targetObjects == null)
                throw traceContext.TraceThrow(new System.ArgumentNullException("targetObjects"));

            Condition policyCondition = GetValidCondition();

            this.FirePolicyEvaluationStarted();


            policyCondition.EvaluateCondition += new Condition.ConditionEvaluationEventHandler(this.EvaluateConditionEventHandler);

            try
            {
                object[] conforming;
                TargetEvaluation[] violating;

                bool evaluationResult = true;
                PolicyEvaluationException policyEvaluationException = null;
                foreach (object target in targetObjects)
                {
                    int connectionTargetsEvaluated = 0;
                
                    this.FireConnectionProcessingStarted(new ConnectionProcessingStartedEventArgs(null));
                
                    try
                    {
                        Dmf.ObjectSet.CalculateTargets(new object[] { target }, policyCondition, evaluationMode, out conforming, out violating);

                        int conformants = (null == conforming) ? 0 : conforming.Length;
                        int violators = (null == violating) ? 0 : violating.Length;
                        connectionTargetsEvaluated = conformants + violators;

                        evaluationResult &= ProcessViolators (policyCondition, evaluationMode, violating, null);
                    }
                    catch (Exception e)
                    {
                        traceContext.TraceCatch(e);

                        evaluationResult = false;

                        if (Utils.ShouldProcessException(e) == false)
                        {
                            throw;
                        }

                        // TODO: Ensure we have the right message here for the execution exception message.
                        policyEvaluationException = new PolicyEvaluationException(string.Empty, e);
                    }

                    this.FireConnectionProcessingFinished(new ConnectionProcessingFinishedEventArgs(evaluationResult, policyEvaluationException, null, connectionTargetsEvaluated));
                }

                this.FirePolicyEvaluationFinished(new PolicyEvaluationFinishedEventArgs(evaluationResult, null));

                return evaluationResult;
            }
            finally
            {
                policyCondition.EvaluateCondition -= new Condition.ConditionEvaluationEventHandler(this.EvaluateConditionEventHandler);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluationMode"></param>
        /// <param name="targetConnections"></param>
        /// <returns></returns>
        public bool Evaluate(AdHocPolicyEvaluationMode evaluationMode, params ISfcConnection[] targetConnections)
        {
            Int64 historyId = 0;
            return Evaluate(evaluationMode, ref historyId, targetConnections);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluationMode"></param>
        /// <param name="historyId"></param>
        /// <param name="targetConnections"></param>
        /// <returns></returns>
        internal bool Evaluate(AdHocPolicyEvaluationMode evaluationMode, ref Int64 historyId, params ISfcConnection[] targetConnections)
        {
            return EvaluatePolicyUsingConnections(evaluationMode, null, ref historyId, targetConnections);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluationMode"></param>
        /// <param name="targetQueryExpression"></param>
        /// <param name="targetConnections"></param>
        /// <returns></returns>
        public bool Evaluate(AdHocPolicyEvaluationMode evaluationMode, SfcQueryExpression targetQueryExpression, params ISfcConnection[] targetConnections)
        {
            Int64 historyId = 0;
            return Evaluate(evaluationMode, targetQueryExpression, ref historyId, targetConnections);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluationMode"></param>
        /// <param name="targetQueryExpression"></param>
        /// <param name="historyId"></param>
        /// <param name="targetConnections"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        internal bool Evaluate(AdHocPolicyEvaluationMode evaluationMode, SfcQueryExpression targetQueryExpression, ref Int64 historyId, params ISfcConnection[] targetConnections)
        {
            if (null == targetQueryExpression)
            {
                throw traceContext.TraceThrow(new ArgumentNullException("targetQueryExpression"));
            }

            return EvaluatePolicyUsingConnections(evaluationMode, targetQueryExpression, ref historyId, targetConnections);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="evaluationMode"></param>
        /// <param name="targetQueryExpression"></param>
        /// <param name="historyId"></param>
        /// <param name="targetConnections"></param>
        /// <returns></returns>
        private bool EvaluatePolicyUsingConnections(AdHocPolicyEvaluationMode evaluationMode, SfcQueryExpression targetQueryExpression, ref Int64 historyId, params ISfcConnection[] targetConnections)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("EvaluatePolicyUsingConnections"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(evaluationMode, targetQueryExpression, historyId, targetConnections);
                bool runningAgainstObjectSet = (null == targetQueryExpression);
                if (runningAgainstObjectSet)
                {
                    // If we're running against ObjectSet do not raise errors for every target
                    // raise just one for the policy after it finishes evaluating

                    this.raiseConditionEvaluationEventToWindowsLog = false;
                }

                if ((targetConnections == null) || (targetConnections.Length <= 0))
                    throw methodTraceContext.TraceThrow(new System.ArgumentNullException("targetConnections"));

                Condition policyCondition = GetValidCondition();

                ValidateConnectionCompatibility(policyCondition.Facet, targetConnections);

                this.FirePolicyEvaluationStarted();

                if (evaluationMode == AdHocPolicyEvaluationMode.Configure)
                {
                    raiseConditionEvaluationEvent = false;
                }

                policyCondition.EvaluateCondition += new Condition.ConditionEvaluationEventHandler(this.EvaluateConditionEventHandler);

                try
                {
                    bool policyEvaluationResult = true;
                    Exception evaluationException = null;

                    SqlStoreConnection sqlStoreTargetConnection;

                    foreach (ISfcConnection sfcConnection in targetConnections)
                    {
                        bool connectionEvaluationResult = true;
                        int connectionTargetsEvaluated = 0;

                        if ((this.State == SfcObjectState.Existing) &&
                            (this.Parent.SqlStoreConnection != null) &&
                            (this.Parent.SqlStoreConnection.ServerInstance.Equals(sfcConnection.ServerInstance, StringComparison.OrdinalIgnoreCase)))
                        {
                            logPolicyEvents = new LogPolicyEvents(this.Parent.SqlStoreConnection.ServerConnection);
                            logPolicyEvents.LogPolicyEvaluationStart(this.ID, targetQueryExpression == null);
                            historyId = logPolicyEvents.HistoryId;
                        }

                        bool initiallyOpenConnection = sfcConnection.IsOpen;
                        sqlStoreTargetConnection = Utils.GetSqlStoreConnection(sfcConnection, "Policy.Evaluate");

                        this.FireConnectionProcessingStarted(new ConnectionProcessingStartedEventArgs(sfcConnection));

                        if (false == this.EvaluateRootCondition(sqlStoreTargetConnection,
                            (evaluationMode == AdHocPolicyEvaluationMode.CheckSqlScriptAsProxy)))
                        {
                            // Root condition failed - Policy succeeded (by design)
                            this.FireConnectionProcessingFinished(new ConnectionProcessingFinishedEventArgs(true, null, sfcConnection, 0, false));
                            continue; // Move on to the next connection
                        }

                        try
                        {
                            object[] conforming = null;
                            TargetEvaluation[] violating = null;

                            if (targetQueryExpression == null)
                            {
                                if (String.IsNullOrEmpty(this.ObjectSet))
                                {
                                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ObjectSetIsNull));
                                }
                                Dmf.ObjectSet objectSet = GetValidObjectSet();

                                objectSet.CalculateTargets(sqlStoreTargetConnection, policyCondition, evaluationMode, this.PolicyCategory, out conforming, out violating);
                            }
                            else
                            {
                                Dmf.ObjectSet.CalculateTargets(sqlStoreTargetConnection, targetQueryExpression, policyCondition, evaluationMode, out conforming, out violating);
                            }

                            int conformants = (null == conforming) ? 0 : conforming.Length;
                            int violators = (null == violating) ? 0 : violating.Length;
                            connectionTargetsEvaluated = conformants + violators;

                            // if this is an enabled policy, and we're running against 
                            // individual targets we need to update the policy health state
                            // for success as there may be violations that have to be erased
                            // note that if parent directs logging for success we've already 
                            // logged this and we don't need to do it here
                            if (targetQueryExpression != null &&
                                this.Enabled &&
                                conforming != null &&
                                conforming.Length > 0 &&
                                !this.Parent.LogOnSuccess)
                            {
                                foreach (object o in conforming)
                                {
                                    if (logPolicyEvents != null)
                                    {
                                        logPolicyEvents.LogPolicyEvaluationDetail(
                                            null,
                                            new ConditionEvaluationEventArgs(null, policyCondition.Facet, o, true, false, null, null));
                                    }
                                }
                            }

                            connectionEvaluationResult &= ProcessViolators(policyCondition, evaluationMode, violating, logPolicyEvents);
                        }
                        catch (Exception e)
                        {
                            methodTraceContext.TraceCatch(e);
                            // Policy failed
                            connectionEvaluationResult = false;

                            if (Utils.ShouldProcessException(e) == false)
                                throw;

                            evaluationException = e;
                        }
                        finally
                        {
                            try
                            {
                                // We can't leave the connection in just any database, because leaving it will put locks
                                // in the db interfere with dba actions against their dB.
                                // In particular a connection in the model database won't allow anyone to create a database or do almost anything.
                                // The safest place is master.  We put it there.
                                sqlStoreTargetConnection.ServerConnection.ExecuteNonQuery("use [master]", ExecutionTypes.NoCommands);
                            }
                            catch (Exception e)
                            {
                                methodTraceContext.TraceCatch(e);
                                // If the exception is unrecoverable we have to let it go
                                if (Utils.ShouldProcessException(e) == false)
                                    throw;
                            }

                            if ((initiallyOpenConnection == false) && (sfcConnection.IsOpen))
                                sfcConnection.Disconnect();
                        }

                        // TODO: Double check if the result needs to be specific to the connection or the overall progressing result
                        this.FireConnectionProcessingFinished(new ConnectionProcessingFinishedEventArgs(
                                                                            connectionEvaluationResult,
                                                                            evaluationException == null ? null : new PolicyEvaluationException(ExceptionTemplatesSR.PolicyEvaluationFailed(this.Name), evaluationException),
                                                                            sfcConnection,
                                                                            connectionTargetsEvaluated));
                        if (logPolicyEvents != null)
                        {
                            logPolicyEvents.LogPolicyEvaluationEnd(connectionEvaluationResult, evaluationException);
                            logPolicyEvents = null;
                        }

                        policyEvaluationResult &= connectionEvaluationResult;
                    }

                    this.FirePolicyEvaluationFinished(new PolicyEvaluationFinishedEventArgs(policyEvaluationResult, null));

                    if (!policyEvaluationResult && runningAgainstObjectSet && (null != this.Parent.SqlStoreConnection))
                    {
                        // if policy failed and we're running against ObjectSet
                        // we need to raise a policy-wide error (as opposed per target error)

                        RaisePolicyResultEvent(null);
                    }
                    // restore event log logging flag
                    this.raiseConditionEvaluationEventToWindowsLog = true;

                    methodTraceContext.TraceParameterOut("returnVal", policyEvaluationResult);
                    return policyEvaluationResult;
                }
                finally
                {
                    policyCondition.EvaluateCondition -= new Condition.ConditionEvaluationEventHandler(this.EvaluateConditionEventHandler);
                }
            }
        }

        private LogPolicyEvents logPolicyEvents = null;
        private bool raiseConditionEvaluationEvent = true;
        private bool raiseConditionEvaluationEventToWindowsLog = true;
        internal void EvaluateConditionEventHandler(Condition policyCondition, ConditionEvaluationEventArgs eventArgs)
        {
            traceContext.TraceMethodEnter("EvaluateConditionEventHandler");
            // Tracing Input Parameters
            traceContext.TraceParameters(policyCondition, eventArgs);
            if ((raiseConditionEvaluationEvent == true) &&
                (logPolicyEvents != null) &&
                (eventArgs.EvaluationResult == false || policyCondition.Parent.LogOnSuccess))
            {
                if (eventArgs.EvaluationResult == false && this.raiseConditionEvaluationEventToWindowsLog)
                {
                    RaisePolicyResultEvent(eventArgs.TargetPsPath);
                }
                logPolicyEvents.LogPolicyEvaluationDetail(policyCondition, eventArgs);
            }

            if (raiseConditionEvaluationEvent == true)
            {
                PolicyEvaluationException evaluationException = null;
                if ((eventArgs.EvaluationResult == false) && (eventArgs.EvaluationException != null))
                {
                    evaluationException = new PolicyEvaluationException(ExceptionTemplatesSR.PolicyEvaluationFailed(this.Name), eventArgs.EvaluationException);
                }

                this.FireTargetProcessed(new TargetProcessedEventArgs(eventArgs.Target,
                                        !string.IsNullOrEmpty(eventArgs.TargetPsPath) ? eventArgs.TargetPsPath : eventArgs.TargetUrnOnlyId,
                                        eventArgs.EvaluationResult,
                                        eventArgs.IsConfigurable,
                                        eventArgs.ConfigurationErrorMessage,
                                        policyCondition.ExpressionNode.DeepClone(),
                                        evaluationException,
                                        eventArgs.ServerName));
            }
            traceContext.TraceMethodExit("EvaluateConditionEventHandler");
        }

        #endregion EVALUATE

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public string ProduceConfigureScript(object target)
        {
            // We assume this function can be called in 'vacuum'
            // so we faithfully try to get the script, reevaluating condition to get correct state of the object

            if (null == target)
            {
                throw new ArgumentNullException("target");
            }

            if (String.IsNullOrEmpty(this.Condition))
            {
                return String.Empty;
            }

            Condition c = this.Parent.Conditions[this.Condition];
            string script = c.ProduceConfigureScript(target);
            return script;
        }

        // TODO: This method should go away and replaced with a call to the ObjectSet
        internal static object GetOneTarget(ISfcConnection targetConnection, SfcQueryExpression targetQueryExpression)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetOneTarget"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(targetConnection, targetQueryExpression);
                traceContext.DebugAssert(targetConnection != null);
                if (targetQueryExpression == null)
                {
                    throw methodTraceContext.TraceThrow(new ArgumentNullException("targetQueryExpression"));
                }
                // TODO: use the Utils.GetSqlStoreConnection instead.
                SqlStoreConnection sqlStoreConnection = targetConnection as SqlStoreConnection;
                if (null == sqlStoreConnection)
                {
                    // we only support connections to SqlServer for the moment,
                    // due to limitations in SfcObjectQuery
                    throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.UnsupportedObjectType(targetConnection.GetType().Name, "Policy.Execute")));
                }


                SfcObjectQuery policyQuery = new SfcObjectQuery(new Smo.Server(sqlStoreConnection.ServerConnection));
                object target = null;
                string[] fields = null;
                foreach (object t in policyQuery.ExecuteIterator(targetQueryExpression, fields, null))
                {
                    if (null == target)
                    {
                        target = t;
                    }
                    else
                    {
                        throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.OnlyOneTarget(targetQueryExpression.ToString())));
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", target);
                return target;
            }
        }

        // TODO: Should this method be here or should go to the ObjectSet
        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetConnection"></param>
        /// <param name="targetQE"></param>
        /// <param name="checkSqlScriptAsProxy"></param>
        /// <returns></returns>
        internal bool IsInTargetSet(SfcConnection targetConnection,
            SfcQueryExpression targetQE,
            bool checkSqlScriptAsProxy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("IsInTargetSet"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(targetConnection, targetQE, checkSqlScriptAsProxy);
                SfcQueryExpression curTargetQE;
                object targetObj;

                // If there is no ObjectSet, nothing can be in it
                if (String.IsNullOrEmpty(this.ObjectSet))
                {
                    methodTraceContext.TraceParameterOut("returnVal", false);
                    return false;
                }

                // Evaluate RootCondition as a part to TargetSet definition
                SqlStoreConnection sqlStoreTargetConnection = Utils.GetSqlStoreConnection(targetConnection, "Policy.Execute");
                if (!EvaluateRootCondition(sqlStoreTargetConnection, checkSqlScriptAsProxy))
                {
                    methodTraceContext.TraceParameterOut("returnVal", false);
                    return false;
                }

                TargetSet ts = Parent.ObjectSets[ObjectSet].TargetSets[targetQE.ExpressionSkeleton];
                if (null == ts || false == ts.Enabled)
                {
                    methodTraceContext.TraceParameterOut("returnVal", false);
                    return false;
                }
                if (ts.RootLevel == ts.TargetTypeSkeleton)
                {
                    methodTraceContext.TraceParameterOut("returnVal", true);
                    return true;
                }

                SFC.Urn urn = targetQE.ToString();
                while (null != urn)
                {
                    curTargetQE = new SfcQueryExpression(urn.ToString());

                    TargetSetLevel tsl = ts.GetLevel(curTargetQE.ExpressionSkeleton);

                    if (!String.IsNullOrEmpty(tsl.Condition))
                    {

                        targetObj = GetOneTarget(targetConnection, curTargetQE);
                        if (null == targetObj)
                        {
                            methodTraceContext.TraceParameterOut("returnVal", false);
                            return false;
                        }

                        FacetEvaluationContext context = FacetEvaluationContext.GetFacetEvaluationContext(tsl.TargetType, targetObj);
                        Condition c = this.Parent.Conditions[tsl.Condition];

                        if (false == (bool)c.ExpressionNode.Evaluate(context, checkSqlScriptAsProxy))
                        {
                            methodTraceContext.TraceParameterOut("returnVal", false);
                            return false;
                        }
                    }

                    urn = urn.Parent;
                    if (null != urn && urn.Type == ts.RootLevel)
                    {
                        urn = null;
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", true);
                return true;
            }
        }

        // These are the message numbers to be used for generating events
        // in case of policy failure.
        private const int messageNumberEnforceAutomatic = 34050;
        private const int messageNumberEnforceAdHoc = 34051;
        private const int messageNumberCheckOnChanges = 34053;
        private const int messageNumberCheckOnSchedule = 34052;

        /// <summary>
        /// Call RAISERROR to generate an event in case of policy execution failure.
        /// </summary>
        /// <param name="targetUri">Uri expression pointing to the target 
        /// that generated the event. If null it means that the policy has 
        /// been executed for all the targets in the target set.</param>
        internal void RaisePolicyResultEvent(string targetUri)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("RaisePolicyResultEvent"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(targetUri);
                if (this.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.None ||
                    !this.Enabled)
                {
                    // we do not raise events for policies that are not automated
                    return;
                }

                int messageNumber = 0;
                string messageText = string.Empty;

                // calculate the message number
                switch (this.AutomatedPolicyEvaluationMode)
                {
                    case AutomatedPolicyEvaluationMode.Enforce:
                        if (SqlContext.IsAvailable)
                        {
                            messageNumber = messageNumberEnforceAutomatic;
                        }
                        else
                        {
                            messageNumber = messageNumberEnforceAdHoc;
                        }
                        break;
                    case AutomatedPolicyEvaluationMode.CheckOnChanges:
                        messageNumber = messageNumberCheckOnChanges;
                        break;
                    case AutomatedPolicyEvaluationMode.CheckOnSchedule:
                        messageNumber = messageNumberCheckOnSchedule;
                        break;
                    default:
                        throw traceContext.TraceThrow(new DmfException(ExceptionTemplates.UnknownEnumeration(
                            typeof(AutomatedPolicyEvaluationMode).Name)));
                }

                // build the message text
                if (null == targetUri || targetUri.Length == 0)
                {
                    messageText = ExceptionTemplatesSR.PolicyViolated(this.Name);
                }
                else
                {
                    messageText = ExceptionTemplatesSR.PolicyViolatedTarget(
                        SfcTsqlProcFormatter.EscapeString(this.Name, '\''),
                        targetUri);
                }

                try
                {
                    // run the RAISERROR command against the current server
                    // we will make sure to invoke this function only in situations
                    // where the target server is identical with the server of the
                    // target connection
                    this.Parent.SqlStoreConnection.ServerConnection.ExecuteNonQuery(
                        string.Format("RAISERROR ({0}, 16, 1, N'{1}') WITH LOG",
                        messageNumber,
                        SfcTsqlProcFormatter.EscapeString(messageText, '\'')), ExecutionTypes.NoCommands);
                }
                catch (ExecutionFailureException e)
                {
                    methodTraceContext.TraceCatch(e);
                    SqlException se = e.InnerException as SqlException;

                    // ignore the error if it's the same one that we threw or 
                    // if there current connection does not have enough 
                    // permissions (2778)
                    if (null == se || (se.Number != messageNumber && se.Number != 2778))
                    {
                        throw;
                    }
                }
            }
        }

        #region ISfcDiscoverObject Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sink"></param>
        public override void Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            if (sink == null)
            {
                throw traceContext.TraceThrow(new ArgumentNullException("sink"));
            }
            if (sink.Action == SfcDependencyAction.Serialize)
            {
                // ********************** STOP   ---    THINK     ---    CODE *************:
                // BE VERY CAREFUL IF YOU WANT TO CHANGE THE ORDER OF HOW THE OBJECTS ARE ADDED TO THE SERIALIZATION SINK.
                // Q: Why?
                // Ans: The TargetSets within an ObjectSet refer to a bunch of conditions. During deserialization, we
                // check if the condition in the XML is actually being referenced by either a Policy condition, root condition
                // or any TargetSet within the ObjectSet that the policy references.
                // To successfully complete the check when the condition is being analyzed (during deserialization) we need
                // all the TargetSets completely deserialized.
                // Hence we need to serialize the TargetSets BEFORE serializing the condition.
                // The only way to currently influence the order of serialization is by adding the objects to the dependency
                // discovery sink in the desired order.
                // ********************** STOP   ---    THINK     ---    CODE *************:
                if (!String.IsNullOrEmpty(this.ObjectSet))
                {
                    ObjectSet referencedObjectSet = this.Parent.ObjectSets[ObjectSet];
                    if (referencedObjectSet != null)
                    {
                        sink.Add(SfcDependencyDirection.Outbound, referencedObjectSet, SfcTypeRelation.StrongReference, false);
                    }
                }

                // Condition reference
                ConditionCollection condColl = this.Parent.Conditions;
                if (!String.IsNullOrEmpty(this.Condition) && condColl.Contains(this.Condition))
                {
                    sink.Add(SfcDependencyDirection.Outbound, condColl[this.Condition], SfcTypeRelation.StrongReference, false);
                }
                if (!String.IsNullOrEmpty(this.RootCondition) && condColl.Contains(this.RootCondition))
                {
                    sink.Add(SfcDependencyDirection.Outbound, condColl[this.RootCondition], SfcTypeRelation.StrongReference, false);
                }

                if (!String.IsNullOrEmpty(this.PolicyCategory))
                {
                    // PolicyCategory reference
                    PolicyCategoryCollection categoryColl = this.Parent.PolicyCategories;
                    // BUGBUG: Why are we double checking the PolicyCategory null/emptyness? We already checked it in the outer if statement
                    if (!String.IsNullOrEmpty(this.PolicyCategory) && categoryColl.Contains(this.PolicyCategory))
                    {
                        sink.Add(SfcDependencyDirection.Outbound, categoryColl[this.PolicyCategory], SfcTypeRelation.StrongReference, false);
                    }
                }

                // fill in the Schedule properties. We don't want to bring them
                // in via a query because we only need for serialization
                UpdateScheduleProperties();
            }

            return;
        }

        /// <summary>
        /// This function fills in the schedule properties
        /// based on the ScheduleUid, which is the only persisted 
        /// property for this policy.
        /// </summary>
        internal void UpdateScheduleProperties()
        {
            bool updated = false;

            if (this.Parent != null)
            {

                if (this.ScheduleUid != Guid.Empty)
                {
                    SMO.Server srv = new Microsoft.SqlServer.Management.Smo.Server(this.Parent.SqlStoreConnection.ServerConnection);
                    SMO.Agent.JobSchedule js = srv.JobServer.SharedSchedules[this.ScheduleUid];
                    if (null != js)
                    {
                        updated = true;
                        this.Schedule = js.Name;
                        this.ActiveEndDate = js.ActiveEndDate;
                        this.ActiveEndTimeOfDay = js.ActiveEndTimeOfDay.Ticks;
                        this.ActiveStartDate = js.ActiveStartDate;
                        this.ActiveStartTimeOfDay = js.ActiveStartTimeOfDay.Ticks;
                        this.FrequencyInterval = js.FrequencyInterval;
                        this.FrequencyRecurrenceFactor = js.FrequencyRecurrenceFactor;
                        this.FrequencyRelativeIntervals = js.FrequencyRelativeIntervals;
                        this.FrequencySubDayInterval = js.FrequencySubDayInterval;
                        this.FrequencySubDayTypes = js.FrequencySubDayTypes;
                        this.FrequencyTypes = js.FrequencyTypes;
                    }
                }
            }

            // we will need to add default values for some properties
            // because they can't be serialized otherwise
            if (!updated)
            {
                if (this.Properties["ActiveEndDate"].IsNull ||
                    this.Properties["ActiveEndDate"].Value is DBNull)
                {
                    this.ActiveEndDate = DateTime.MinValue;
                }

                if (this.Properties["ActiveStartDate"].IsNull ||
                    this.Properties["ActiveStartDate"].Value is DBNull)
                {
                    this.ActiveStartDate = DateTime.MinValue;
                }
            }

        }

        #endregion

        internal bool DependsOnCondition(string conditionName)
        {
            if ((this.Condition == conditionName) ||
                (this.RootCondition == conditionName) ||
                (!String.IsNullOrEmpty(this.ObjectSet) && Parent.ObjectSets[this.ObjectSet].DependsOnCondition(conditionName)))
            {
                return true;
            }
            return false;
        }

        internal const string typeName = "Policy";
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
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }
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

            /// <summary>
            /// 
            /// </summary>
            public string Name
            {
                get
                {
                    return keyName;
                }
            }

            // Create Key from the set of name-value pairs that represent Urn fragment
            internal Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                keyName = (string)filedDict["Name"];
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
                return String.Format("{0}[@Name='{1}']", Policy.typeName, SfcSecureString.EscapeSquote(Name));
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
                return new Policy();
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
                return (int)this.Properties["ID"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public string Description
        {
            get
            {
                return (string)this.Properties["Description"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Description to: {0}", value);
                this.Properties["Description"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public DateTime CreateDate
        {
            get
            {
                return (DateTime)this.Properties["CreateDate"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        [SfcReference(typeof(Condition), "PolicyStore/Condition[@Name='{0}']", new string[] { "Condition" })]
        public string Condition
        {
            get
            {
                return (string)this.Properties["Condition"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Condition to: {0}", value);
                this.Properties["Condition"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        [SfcReference(typeof(ObjectSet), "PolicyStore/ObjectSet[@Name='{0}']", new string[] { "ObjectSet" })]
        public string ObjectSet
        {
            get
            {
                return (string)this.Properties["ObjectSet"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting ObjectSet to: {0}", value);
                this.Properties["ObjectSet"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        [SfcReference(typeof(Condition), "PolicyStore/Condition[@Name='{0}']", new string[] { "RootCondition" })]
        public string RootCondition
        {
            get
            {
                return (string)this.Properties["RootCondition"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting RootCondition to: {0}", value);
                this.Properties["RootCondition"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        [SfcReference(typeof(PolicyCategory), "PolicyStore/PolicyCategory[@Name='{0}']", new string[] { "PolicyCategory" })]
        public string PolicyCategory
        {
            get
            {
                return (string)this.Properties["PolicyCategory"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting PolicyCategory to: {0}", value);
                this.Properties["PolicyCategory"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public Boolean Enabled
        {
            get
            {
                object enabled = this.Properties["Enabled"].Value;
                if (enabled == null)
                    return false;
                return (Boolean)enabled;
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
        [SfcProperty(SfcPropertyFlags.Required)]
        public AutomatedPolicyEvaluationMode AutomatedPolicyEvaluationMode
        {
            get
            {
                object value = this.Properties["AutomatedPolicyEvaluationMode"].Value;
                if (value == null)
                {
                    return AutomatedPolicyEvaluationMode.None;
                }
                return (AutomatedPolicyEvaluationMode)value;
            }
            set
            {
                traceContext.TraceVerbose("Setting AutomatedPolicyEvaluationMode to: {0}", value);
                this.Properties["AutomatedPolicyEvaluationMode"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.None)]
        public Guid ScheduleUid
        {
            get
            {
                object value = this.Properties["ScheduleUid"].Value;
                if (value == null)
                    return Guid.Empty;
                return (Guid)value;
            }
            set
            {
                traceContext.TraceVerbose("Setting ScheduleUid to: {0}", value);
                this.Properties["ScheduleUid"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public string CreatedBy
        {
            get
            {
                return (string)this.Properties["CreatedBy"].Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public DateTime DateModified
        {
            get
            {
                object value = this.Properties["DateModified"].Value;
                if ((value == null) || (value is DBNull))
                    return DateTime.MinValue;
                return (DateTime)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public string ModifiedBy
        {
            get
            {
                object value = this.Properties["ModifiedBy"].Value;
                if (value == null)
                    return String.Empty;
                return (string)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Data)]
        public int CategoryId
        {
            get
            {
                object value = this.Properties["CategoryId"].Value;
                if (value == null)
                    return 0;
                return (int)value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string HelpText
        {
            get
            {
                return (string)this.Properties["HelpText"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting HelpText to: {0}", value);
                this.Properties["HelpText"].Value = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string HelpLink
        {
            get
            {
                return (string)this.Properties["HelpLink"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting HelpLink to: {0}", value);
                this.Properties["HelpLink"].Value = value;
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
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("GetChildCollection"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(elementType);
                switch (elementType)
                {
                    case EvaluationHistory.typeName:
                        methodTraceContext.TraceParameterOut("returnVal", this.EvaluationHistories);
                        return this.EvaluationHistories;
                    default: throw methodTraceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.NoSuchCollection(elementType)));
                }
            }
        }

        EvaluationHistoryCollection evaluationHistories;

        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(EvaluationHistory))]
        [SfcNonSerializable()]
        public EvaluationHistoryCollection EvaluationHistories
        {
            get
            {
                if (this.evaluationHistories == null)
                {
                    this.evaluationHistories = new EvaluationHistoryCollection(this);
                }
                return this.evaluationHistories;
            }
        }

        /// <summary>
        /// Shows if any of the conditions referenced by this policy references 
        /// a t-sql or wql script
        /// </summary>
        public bool HasScript
        {
            get
            {
                bool hasScript = false;
                if (this.Parent.Conditions[this.Condition].HasScript ||
                    (!string.IsNullOrEmpty(this.RootCondition) && this.Parent.Conditions[this.RootCondition].HasScript) ||
                    (!string.IsNullOrEmpty(this.ObjectSet) && Parent.ObjectSets[this.ObjectSet].HasScript))
                {
                    hasScript = true;
                }

                return hasScript;
            }
        }

        /// <summary>
        /// Returns a boolean indicating if this policy's Condition uses the given facet.
        /// </summary>
        /// <param name="facet"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        public bool UsesFacet(string facet)
        {
            Condition c = this.Parent.Conditions[this.Condition];
            return (c != null && c.Facet == facet);
        }

        #region Schedule properties

        [SfcProperty(SfcPropertyFlags.None)]
        internal System.DateTime ActiveEndDate
        {
            get
            {
                return (System.DateTime)this.Properties["ActiveEndDate"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting ActiveEndDate to: {0}", value);
                this.Properties["ActiveEndDate"].Value = value;
            }
        }


        [SfcProperty(SfcPropertyFlags.None)]
        internal long ActiveEndTimeOfDay
        {
            get
            {
                return (long)this.Properties["ActiveEndTimeOfDay"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting ActiveEndTimeOfDay to: {0}", value);
                this.Properties["ActiveEndTimeOfDay"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal System.DateTime ActiveStartDate
        {
            get
            {
                return (System.DateTime)this.Properties["ActiveStartDate"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting ActiveStartDate to: {0}", value);
                this.Properties["ActiveStartDate"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal long ActiveStartTimeOfDay
        {
            get
            {
                return (long)this.Properties["ActiveStartTimeOfDay"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting ActiveStartTimeOfDay to: {0}", value);
                this.Properties["ActiveStartTimeOfDay"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal System.Int32 FrequencyInterval
        {
            get
            {
                return (System.Int32)this.Properties["FrequencyInterval"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting FrequencyInterval to: {0}", value);
                this.Properties["FrequencyInterval"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal System.Int32 FrequencyRecurrenceFactor
        {
            get
            {
                return (System.Int32)this.Properties["FrequencyRecurrenceFactor"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting FrequencyRecurrenceFactor to: {0}", value);
                this.Properties["FrequencyRecurrenceFactor"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal Microsoft.SqlServer.Management.Smo.Agent.FrequencyRelativeIntervals FrequencyRelativeIntervals
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.Agent.FrequencyRelativeIntervals)this.Properties["FrequencyRelativeIntervals"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting FrequencyRelativeIntervals to: {0}", value);
                this.Properties["FrequencyRelativeIntervals"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal System.Int32 FrequencySubDayInterval
        {
            get
            {
                return (System.Int32)this.Properties["FrequencySubDayInterval"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting FrequencySubDayInterval to: {0}", value);
                this.Properties["FrequencySubDayInterval"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal Microsoft.SqlServer.Management.Smo.Agent.FrequencySubDayTypes FrequencySubDayTypes
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.Agent.FrequencySubDayTypes)this.Properties["FrequencySubDayTypes"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting FrequencySubDayTypes to: {0}", value);
                this.Properties["FrequencySubDayTypes"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal Microsoft.SqlServer.Management.Smo.Agent.FrequencyTypes FrequencyTypes
        {
            get
            {
                return (Microsoft.SqlServer.Management.Smo.Agent.FrequencyTypes)this.Properties["FrequencyTypes"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting FrequencyTypes to: {0}", value);
                this.Properties["FrequencyTypes"].Value = value;
            }
        }

        [SfcProperty(SfcPropertyFlags.None)]
        internal string Schedule
        {
            get
            {
                return (string)this.Properties["Schedule"].Value;
            }
            set
            {
                traceContext.TraceVerbose("Setting Schedule to: {0}", value);
                this.Properties["Schedule"].Value = value;
            }
        }

        #endregion

        internal static List<SfcInstanceSerializedData> UpgradeInstance(List<SfcInstanceSerializedData> sfcInstanceData, int fileVersion)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("UpgradeInstance"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(sfcInstanceData, fileVersion);
                List<SfcInstanceSerializedData> list = new List<SfcInstanceSerializedData>();

                if (fileVersion < 3)
                {
                    foreach (SfcInstanceSerializedData sfcInstanceSerializedData in sfcInstanceData)
                    {
                        if (sfcInstanceSerializedData.Name == "AutomatedPolicyExecutionMode")
                        {
                            string value = (string)sfcInstanceSerializedData.Value;
                            Regex regex = new Regex("AutomatedPolicyExecutionMode", RegexOptions.IgnoreCase);
                            value = regex.Replace(value, "AutomatedPolicyEvaluationMode");
                            SfcInstanceSerializedData correctedInstance = new SfcInstanceSerializedData(SfcSerializedTypes.Property,
                                "AutomatedPolicyEvaluationMode", "AutomatedPolicyEvaluationMode", value);
                            list.Add(correctedInstance);
                        }
                        else
                        {
                            list.Add(sfcInstanceSerializedData);
                        }
                    }
                }

                methodTraceContext.TraceParameterOut("returnVal", list);
                return list;
            }
        }
    }

    /// <summary>
    /// The AdHocPolicyEvaluationMode bit flag enum provides the execution 
    /// mode for a policy that is "run now".
    /// </summary>
    public enum AdHocPolicyEvaluationMode
    {
        /// Immediately runs the policy in check mode.
        Check = 0,
        /// Reconfigures the target(s) to comply with the policy.
        Configure = 1,
        /// Check the policy, executing any Sql scripts under the special
        /// ##MS_PolicyTsqlExecutionLogin## proxy login
        CheckSqlScriptAsProxy = 2
    }

    class LogPolicyEvents
    {
        static TraceContext traceContext = TraceContext.GetTraceContext("DMF", "LogPolicyEvents");
        static readonly SfcTsqlProcFormatter scriptLogEvaluationStart = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptLogEvaluationEnd = new SfcTsqlProcFormatter();
        static readonly SfcTsqlProcFormatter scriptLogEvaluationDetail = new SfcTsqlProcFormatter();

        private long historyId;

        internal long HistoryId
        {
            get { return historyId; }
        }

        private ServerConnection serverConnection;

        static LogPolicyEvents()
        {

            // Log start
            scriptLogEvaluationStart.Procedure = "msdb.dbo.sp_syspolicy_log_policy_execution_start";
            scriptLogEvaluationStart.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("history_id", true, true));
            scriptLogEvaluationStart.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("policy_id", true));
            scriptLogEvaluationStart.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("is_full_run", true));

            // Log end
            scriptLogEvaluationEnd.Procedure = "msdb.dbo.sp_syspolicy_log_policy_execution_end";
            scriptLogEvaluationEnd.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("history_id", true));
            scriptLogEvaluationEnd.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("result", true));
            scriptLogEvaluationEnd.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("exception_message", true));
            scriptLogEvaluationEnd.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("exception", true));

            // Log detail
            scriptLogEvaluationDetail.Procedure = "msdb.dbo.sp_syspolicy_log_policy_execution_detail";
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("history_id", true));
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_query_expression", true));
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("target_query_expression_with_id", true));
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("result", true));
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("result_detail", true));
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("exception_message", true));
            scriptLogEvaluationDetail.Arguments.Add(new SfcTsqlProcFormatter.SprocArg("exception", true));
        }

        internal LogPolicyEvents(ServerConnection serverConnection)
        {
            this.serverConnection = serverConnection;
        }

        internal void LogPolicyEvaluationStart(int policyId, bool isFullRun)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("LogPolicyEvaluationStart"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policyId, isFullRun);
                if (serverConnection == null)
                    return;

                historyId = 0;
                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();

                args.Add(new SfcTsqlProcFormatter.RuntimeArg(historyId.GetType(), historyId));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(policyId.GetType(), policyId));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(isFullRun.GetType(), isFullRun));

                string script = scriptLogEvaluationStart.GenerateScript(null, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                historyId = (long)serverConnection.ExecuteScalar(script);
            }
        }

        internal void LogPolicyEvaluationDetail(Condition policyCondition, ConditionEvaluationEventArgs eventArgs)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("LogPolicyEvaluationDetail"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policyCondition, eventArgs);
                if (serverConnection == null)
                    return;

                // We only log when the object is of type SqlSmoObject or SfcInstance
                if ((!(eventArgs.Target is SqlSmoObject)) && (!(eventArgs.Target is SfcInstance)))
                    return;

                string xmlResult = string.Empty;
                if (policyCondition != null)
                {
                    xmlResult = ExpressionNode.SerializeNodeWithResult(policyCondition.ExpressionNode);
                }

                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(historyId.GetType(), historyId));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(eventArgs.TargetPsPath.GetType(), eventArgs.TargetPsPath));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(eventArgs.TargetUrnOnlyId.GetType(), eventArgs.TargetUrnOnlyId));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), eventArgs.EvaluationResult ? 1 : 0));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(xmlResult.GetType(), xmlResult));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), eventArgs.EvaluationException != null ? eventArgs.EvaluationException.Message : string.Empty));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), eventArgs.EvaluationException != null ? eventArgs.EvaluationException.ToString() : string.Empty));

                string script = scriptLogEvaluationDetail.GenerateScript(null, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                serverConnection.ExecuteNonQuery(script, ExecutionTypes.NoCommands);
            }
        }

        internal void LogPolicyEvaluationEnd(bool result, Exception exception)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("LogPolicyEvaluationEnd"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(result, exception);
                if (serverConnection == null)
                    return;

                List<SfcTsqlProcFormatter.RuntimeArg> args = new List<SfcTsqlProcFormatter.RuntimeArg>();

                args.Add(new SfcTsqlProcFormatter.RuntimeArg(historyId.GetType(), historyId));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(int), result ? 1 : 0));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), exception != null ? exception.Message : string.Empty));
                args.Add(new SfcTsqlProcFormatter.RuntimeArg(typeof(string), exception != null ? exception.ToString() : string.Empty));

                string script = scriptLogEvaluationEnd.GenerateScript(null, args);
                methodTraceContext.TraceVerbose("Script generated: " + script);
                serverConnection.ExecuteNonQuery(script, ExecutionTypes.NoCommands);
            }
        }
    }
}
