// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;


namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the Policy class.
    /// </summary>
    public sealed partial class Policy : ISfcValidate
    {
        /// <summary>
        /// Limit on the length of HelpText property specified by user.
        /// </summary>
        public const int HelpTextStringMaxLength = 4000;

        /// <summary>
        /// Limit on the length of HelpLink property. Note that this is the max hyperlink length accepted by IE.
        /// </summary>
        public const int HelpLinkStringMaxLength = 2083;

        private const AutomatedPolicyEvaluationMode EventingModes = AutomatedPolicyEvaluationMode.CheckOnChanges | AutomatedPolicyEvaluationMode.Enforce;


        /// <summary>
        /// master validation method
        /// </summary>
        /// <param name="validationMode"></param>
        /// <param name="throwOnFirst"></param>
        /// <param name="validationState"></param>
        void Validate(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            ValidateProperties(validationMode, throwOnFirst, validationState);
            ValidateMode(validationMode, throwOnFirst, validationState);
            ValidateCategory(validationMode, throwOnFirst, validationState);
            ValidateObjectSet(validationMode, throwOnFirst, validationState);
            ValidateRootCondition(validationMode, throwOnFirst, validationState);
        }

        void ValidateProperties(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateProperties"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                if (String.IsNullOrEmpty(Name))
                {
                    Exception ex = new SfcPropertyNotSetException("Name");
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, ex));
                    }
                    else
                    {
                        validationState.AddError(ex, "Name");
                    }
                }

                if (validationMode == ValidationMethod.Rename)
                {
                    return;
                }

                if (String.IsNullOrEmpty(Condition))
                {
                    Exception ex = new SfcPropertyNotSetException("Condition");
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, ex));
                    }
                    else
                    {
                        validationState.AddError(ex, "Condition");
                    }
                }

                if (HelpText != null && HelpText.Length > Policy.HelpTextStringMaxLength)
                {
                    Exception ex = new StringPropertyTooLongException(ExceptionTemplatesSR.AdditionalHelpText, Policy.HelpTextStringMaxLength, HelpText.Length);
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, HelpText, ex));
                    }
                    else
                    {
                        validationState.AddError(ex, "HelpText");
                    }
                }

                if (HelpLink != null)
                {
                    // validate that the link is of the correct format
                    if (!string.IsNullOrEmpty(HelpLink) && !this.IsSystemObject)
                    {
                        Exception innerException = null;
                        bool helpLinkOk = false;
                        try
                        {
                            helpLinkOk = Utils.IsValidHelpLink(HelpLink);
                            if (!helpLinkOk)
                            {
                                innerException = new Exception(ExceptionTemplatesSR.InvalidHelpLinkMessage);
                            }
                        }
                        catch (Exception ex)
                        {
                            methodTraceContext.TraceCatch(ex);
                            innerException = ex;
                        }

                        if (innerException != null)
                        {

                            if (throwOnFirst)
                            {
                                throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, HelpLink, innerException));
                            }
                            else
                            {
                                validationState.AddError(innerException, "HelpLink");
                            }
                        }
                    }

                    //validate link length
                    if (HelpLink.Length > Policy.HelpLinkStringMaxLength)
                    {
                        Exception ex = new StringPropertyTooLongException(ExceptionTemplatesSR.AdditionalHelpLink, Policy.HelpLinkStringMaxLength, HelpLink.Length);
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, HelpLink, ex));
                        }
                        else
                        {
                            validationState.AddError(ex, "HelpLink");
                        }
                    }
                }
            }
        }

        void ValidateMode(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateMode"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                if (validationMode == ValidationMethod.Rename)
                {
                    return;
                }

                if (AutomatedPolicyEvaluationMode != AutomatedPolicyEvaluationMode.None)
                {

                    if (AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnSchedule)
                    {
                        if (this.Parent == null || ((ISfcHasConnection)this.Parent).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                        {
                            // In Offline mode, Validate offline mode properties ie. Schedule
                            if (String.IsNullOrEmpty(Schedule))
                            {
                                Exception ex = new SfcPropertyNotSetException("Schedule");
                                if (throwOnFirst)
                                {
                                    throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Schedule, ex));
                                }
                                else
                                {
                                    validationState.AddError(ex, "ScheduleUid");
                                }
                            }
                        }
                        else
                        {
                            if (Guid.Empty != ScheduleUid)
                            {
                                if (!throwOnFirst)
                                {
                                    Smo.Server server = new Smo.Server(Parent.SqlStoreConnection.ServerConnection);
                                    if (null == server.JobServer.SharedSchedules[ScheduleUid])
                                    {
                                        validationState.AddWarning(ExceptionTemplatesSR.ScheduleDoesntExist(ScheduleUid.ToString()), "ScheduleUid");
                                    }
                                }
                            }
                            else
                            {
                                Exception mjse = new MissingJobScheduleException(ExceptionTemplatesSR.MissingJobSchedule);
                                if (throwOnFirst)
                                {
                                    throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, mjse));
                                }
                                else
                                {
                                    validationState.AddError(mjse, "ScheduleUid");
                                }
                            }
                        }
                    }

                    // Verify Evaluation mode is compatible with the facet
                    Condition c = this.Parent.Conditions[this.Condition];
                    if (null == c)
                    {
                        Exception moe = new MissingObjectException(ExceptionTemplatesSR.Condition, this.Condition);
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, moe));
                        }
                        else
                        {
                            validationState.AddError(moe, "Condition");
                        }
                    }

                    // This check also takes care of Scripting in runtime
                    // (condition with script will only return COS)
                    AutomatedPolicyEvaluationMode evaluationMode = c.GetSupportedEvaluationMode();
                    if (0 == (evaluationMode & AutomatedPolicyEvaluationMode))
                    {
                        Exception cpve = new ConflictingPropertyValuesException(
                            validationMode, ExceptionTemplatesSR.Condition, c.Name,
                            ExceptionTemplatesSR.EvaluationMode, Utils.GetDescriptionForEvaluationMode(AutomatedPolicyEvaluationMode));
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, cpve));
                        }
                        else
                        {
                            validationState.AddError(cpve, "AutomatedPolicyEvaluationMode");
                        }
                    }

                    // Check Evaluation mode for root condition
                    AutomatedPolicyEvaluationMode clrModes =
                        AutomatedPolicyEvaluationMode.CheckOnChanges | AutomatedPolicyEvaluationMode.Enforce;
                    if ((!string.IsNullOrEmpty(this.RootCondition)) &&
                        (0 != (this.AutomatedPolicyEvaluationMode & clrModes)))
                    {
                        Exception cpve = new ConflictingPropertyValuesException(
                            validationMode, ExceptionTemplatesSR.RootCondition, this.RootCondition,
                            ExceptionTemplatesSR.EvaluationMode, Utils.GetDescriptionForEvaluationMode(AutomatedPolicyEvaluationMode));
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, cpve));
                        }
                        else
                        {
                            validationState.AddError(cpve, "AutomatedPolicyEvaluationMode");
                        }
                    }

                    if (0 != (this.AutomatedPolicyEvaluationMode & EventingModes) && throwOnFirst)
                    {
                        if (!String.IsNullOrEmpty(ObjectSet))
                        {
                            // If there is no ObjectSet ValidateObjectSet reports an error

                            if (!Parent.ObjectSets[ObjectSet].IsEventingFilter())
                            {
                                Exception cpve = new ConflictingPropertyValuesException(
                                    validationMode, ExceptionTemplatesSR.ObjectSet, this.ObjectSet,
                                    ExceptionTemplatesSR.EvaluationMode, Utils.GetDescriptionForEvaluationMode(AutomatedPolicyEvaluationMode));
                                throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, cpve));
                            }
                        }
                    }
                }
                else
                {
                    if (Enabled)
                    {
                        Exception cpve = new ConflictingPropertyValuesException(
                            validationMode, ExceptionTemplatesSR.EvaluationMode, Utils.GetDescriptionForEvaluationMode(AutomatedPolicyEvaluationMode),
                            ExceptionTemplatesSR.Enabled, Enabled.ToString());
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, cpve));
                        }
                        else
                        {
                            validationState.AddError(cpve, "Enabled");
                        }
                    }
                }
            }
        }

        void ValidateCategory(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateCategory"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                if (String.IsNullOrEmpty(PolicyCategory))
                {
                    return;
                }

                if (!Parent.PolicyCategories.Contains(PolicyCategory))
                {
                    Exception ode = new MissingObjectException("PolicyCategory", PolicyCategory);
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException("Policy", Name, ode));
                    }
                    else
                    {
                        validationState.AddError(ode, "PolicyCategory");
                    }
                }
            }
        }

        void ValidateObjectSet(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateObjectSet"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                if (String.IsNullOrEmpty(ObjectSet))
                {
                    if (this.AutomatedPolicyEvaluationMode != AutomatedPolicyEvaluationMode.None)
                    {
                        Exception pnf = new DmfException(ExceptionTemplatesSR.PolicyWithNoFilters);
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException("Policy", Name, pnf));
                        }
                        else
                        {
                            validationState.AddError(pnf, "ObjectSet");
                        }
                    }
                    return;
                }

                if (!Parent.ObjectSets.Contains(ObjectSet))
                {
                    Exception ode = new MissingObjectException("ObjectSet", ObjectSet);
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException("Policy", Name, ode));
                    }
                    else
                    {
                        validationState.AddError(ode, "ObjectSet");
                    }
                }

                // make sure OS Facet is the same as Condition Facet
                if (!String.IsNullOrEmpty(Condition) && (Parent.Conditions[Condition].Facet != Parent.ObjectSets[ObjectSet].Facet))
                {
                    Exception de = new DmfException(
                        ExceptionTemplatesSR.ObjectSetAndConditionFacetMismatch(ObjectSet, Parent.ObjectSets[ObjectSet].Facet, Condition, Parent.Conditions[Condition].Facet));
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, de));
                    }
                    else
                    {
                        validationState.AddError(de, "ObjectSet");
                    }
                }

                // validate references

                // This is a temporary solution to prevent object reinitialization
                // Should be removed when expected functionality is in place
                ((ISfcCollection)(Parent.Policies)).Initialized = true;

                ReadOnlyCollection<Policy> dependentPolicies = Parent.ObjectSets[ObjectSet].EnumDependentPolicies();
                if (dependentPolicies.Count > 1 || (dependentPolicies.Count == 1 && !dependentPolicies.Contains(this)))
                {
                    Exception de = new DmfException(ExceptionTemplatesSR.ObjectSetAlreadyReferenced(ObjectSet, dependentPolicies[0].Name));
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, de));
                    }
                    else
                    {
                        validationState.AddError(de, "ObjectSet");
                    }
                }
            }
        }

        void ValidateRootCondition(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateRootCondition"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                // Assume UI always select right Root Condition
                // So this check is for final Validation only
                if (!throwOnFirst)
                {
                    return;
                }

                if (validationMode == ValidationMethod.Rename)
                {
                    return;
                }

                if (!String.IsNullOrEmpty(this.RootCondition))
                {
                    Condition c = this.Parent.Conditions[this.RootCondition];
                    if (null == c)
                    {
                        Exception moe = new MissingObjectException(ExceptionTemplatesSR.Condition, this.RootCondition);
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, moe));
                    }

                    Type facet = FacetRepository.GetFacetType(c.Facet);
                    List<Type> types = FacetRepository.GetFacetSupportedTypes(facet);
                    Type rootType = SfcMetadataDiscovery.GetRootFromType(types[0]);
                    if (!FacetRepository.IsRootFacet(rootType, facet))
                    {
                        Exception mtfa = new MissingTypeFacetAssociationException(ExceptionTemplatesSR.RootCondition, this.Parent.Conditions[this.RootCondition].Facet);
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Policy, Name, mtfa));
                    }
                }
            }
        }

        /// <summary>
        /// Policy validation
        /// Unlike ISfcValidate.Validate, this method will throw the first exception it encounters
        /// If Evaluation of this method doesn't produce any exceptions, validation passed
        /// </summary>
        /// <param name="mode"></param>
        public void Validate(string mode)
        {
            // This cannot possibly happen in the UI
            // If it does happen we will throw there anyway the first time we access Parent
            if (null == this.Parent)
            {
                throw traceContext.TraceThrow(new Microsoft.SqlServer.Management.Sdk.Sfc.SfcMissingParentException());
            }

            Validate(mode, true, null);
        }

        #region ISfcValidate implementation

        /// <summary>
        /// ISfcValidate implementation for Policy
        /// </summary>
        /// <param name="validationMethod"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        ValidationState ISfcValidate.Validate(string validationMethod, params object[] arguments)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ISfcValidate.Validate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMethod, arguments);
                ValidationState validationState = new ValidationState();

                Validate(validationMethod, false, validationState);

                methodTraceContext.TraceParameterOut("returnVal", validationState);
                return validationState;
            }
        }
        #endregion ISfcValidate implementation

        #region RunTime (non-DDL) validation
        // TODO: Split this validation between the Policy, Condition and Condition Expression
        Condition GetValidCondition()
        {
            Condition policyCondition = this.Parent.Conditions[this.Condition];
            // TODO: Is this right? To return false when there is no condition expression specified? Isn't it the same as no condition being specified and throwing the
            // exception above?
            if ((policyCondition == null) || (policyCondition.ExpressionNode == null))
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.ConditionIsNull));

            return policyCondition;
        }

        ObjectSet GetValidObjectSet()
        {
            ObjectSet objectSet = this.Parent.ObjectSets[this.ObjectSet];
            if (objectSet == null)
                throw traceContext.TraceThrow(new MissingObjectException("ObjectSet", ObjectSet));

            return objectSet;
        }

        /// <summary>
        /// This method evaluates if supplied connections can be used to retrieve 
        /// objects for the policy
        /// </summary>
        /// <param name="facet"></param>
        /// <param name="targetConnections"></param>
        /// <returns></returns>
        [STraceConfigurationAttribute(SkipAutoTrace = true)]
        void ValidateConnectionCompatibility(string facet, params ISfcConnection[] targetConnections)
        {
            // TODO: Implement this method 

            // The idea of this method is to verify 
            // that facet domain is in domains represented by given connections
            // For example AS policy cannot retrieve any objects from Sql connection

            // This is temporary check to prevent attempts to run 
            // AS and RS policies in database engine
            if (facet == "ISurfaceAreaConfigurationForAnalysisServer"
                || facet == "ISurfaceAreaConfigurationForReportingServices")
            {
                throw traceContext.TraceThrow(new DmfException(ExceptionTemplatesSR.IncompatiblePolicyEvaluationMode));
            }

        }

        #endregion RunTime (non-DDL) validation
    }
}
