// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Reflection;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is a non-generated part of Condition class.
    /// </summary>
    public partial class Condition : ISfcValidate
    {
        #region FacetProperties cache
        // This cache is a perf optimization
        // Validation is called very frequently
        // Extracting Facet properties is expensive, so we avoid it if nothing changed

        /// <summary>
        /// Hashtable to cache Facet Properties for Validation purposes
        /// </summary>
        Hashtable facetProperties = null;
        /// <summary>
        /// last validated Facet 
        /// </summary>
        string validatedFacet = String.Empty;

        /// <summary>
        /// Maintains Facet Properties cache for Validation
        /// </summary>
        void RefreshFacetProperties()
        {
            if (!String.IsNullOrEmpty(this.Facet) && this.Facet != validatedFacet)
            {
                validatedFacet = this.Facet;

                if (null == facetProperties)
                {
                    facetProperties = new Hashtable();
                }
                else
                {
                    facetProperties.Clear();
                }

                Type facet = FacetRepository.GetFacetType(this.Facet);

                foreach (PropertyInfo pi in FacetRepository.GetFacetProperties(facet))
                {
                    facetProperties.Add(pi.Name, null);
                }
            }
        }
        #endregion FacetProperties cache

        /// <summary>
        /// master validation method
        /// </summary>
        /// <param name="validationMode"></param>
        /// <param name="throwOnFirst"></param>
        /// <param name="validationState"></param>
        void Validate(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            ValidateProperties(validationMode, throwOnFirst, validationState);
            ValidateExpression(validationMode, throwOnFirst, validationState);
            ValidateReferences(validationMode, throwOnFirst, validationState);
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
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
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

                if (String.IsNullOrEmpty(Facet))
                {
                    Exception ex = new SfcPropertyNotSetException("Facet");
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                    }
                    else
                    {
                        validationState.AddError(ex, "Facet");
                    }
                }
                else
                {
                    if (!PolicyStore.Facets.Contains(Facet))
                    {
                        Exception mox = new MissingObjectException(ExceptionTemplatesSR.ManagementFacet, Facet);
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, mox));
                        }
                        else
                        {
                            validationState.AddError(mox, "Facet");
                        }
                    }
                }

                if (null == ExpressionNode)
                {
                    Exception ex = new SfcPropertyNotSetException("ExpressionNode");
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                    }
                    else
                    {
                        validationState.AddError(ex, "ExpressionNode");
                    }
                }
            }
        }

        void ValidateExpression(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateExpression"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                if (null == ExpressionNode || String.IsNullOrEmpty(Facet))
                {
                    return;
                }

                RefreshFacetProperties();

                foreach (string attr in this.ExpressionNode.EnumAttributes())
                {
                    traceContext.DebugAssert(null != facetProperties);
                    if (!facetProperties.Contains(attr))
                    {
                        Exception ex = new ConflictingPropertyValuesException(validationMode,
                            ExceptionTemplatesSR.ManagementFacet, this.Facet,
                            ExceptionTemplatesSR.Property, attr);
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                        }
                        else
                        {
                            validationState.AddError(ex, "ExpressionNode");
                        }
                    }
                }
            }
        }

        void ValidateReferences(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateReferences"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                if (validationMode == ValidationMethod.Alter)
                {
                    // It's important to get this.Facet first as it populates this.cachedFacet (if it's empty)
                    if (this.Facet != this.cachedFacet || this.HasScript)
                    {
                        foreach (Policy p in this.Parent.Policies)
                        {
                            if (p.DependsOnCondition(this.Name))
                            {
                                if (this.Facet != this.cachedFacet)
                                {
                                    Exception ex = new DmfException(ExceptionTemplatesSR.CannotChangeFacet);
                                    if (throwOnFirst)
                                    {
                                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                                    }
                                    else
                                    {
                                        validationState.AddError(ex, "Facet");
                                    }
                                    return;
                                }

                                if ((p.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnChanges ||
                                    p.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.Enforce) &&
                                    this.HasScript)
                                {
                                    Exception ex = new DmfException(ExceptionTemplatesSR.ReferencedConditionsCannotContainScript);
                                    if (throwOnFirst)
                                    {
                                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                                    }
                                    else
                                    {
                                        validationState.AddError(ex, "Facet");
                                    }
                                    return;
                                }
                            }
                        }
                    }

                    foreach (ObjectSet os in this.Parent.ObjectSets)
                    {
                        if (os.EnumReferencedConditionNames().Contains(this.Name))
                        {
                            if (this.Facet != this.cachedFacet)
                            {
                                Exception ex = new DmfException(ExceptionTemplatesSR.CannotChangeFacet);
                                if (throwOnFirst)
                                {
                                    throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                                }
                                else
                                {
                                    validationState.AddError(ex, "Facet");
                                }
                                return;
                            }
                            else if (!this.IsEnumerable)
                            {
                                Exception ex = new DmfException(ExceptionTemplatesSR.ConditionCannotBeUsedForFiltering(this.Name));
                                if (throwOnFirst)
                                {
                                    throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                                }
                                else
                                {
                                    validationState.AddError(ex, "Facet");
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Policy validation
        /// Unlike ISfcValidate.Validate, this method will throw the first exception it encounters
        /// If execution of this method doesn't produce any exceptions, validation passed
        /// </summary>
        /// <param name="mode"></param>
        public void Validate(string mode)
        {
            // This cannot possibly happen in the UI
            // If it does happen we will throw there anyway the first time we access Parent
            if (null == this.Parent)
            {
                throw new Microsoft.SqlServer.Management.Sdk.Sfc.SfcMissingParentException();
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
            ValidationState validationState = new ValidationState();

            Validate(validationMethod, false, validationState);

            return validationState;
        }
        #endregion ISfcValidate implementation

        /// <summary>
        /// Validate condition altered as a part of policy deserialization process
        /// </summary>
        /// <param name="policy">policy being deserialized</param>
        internal void ValidateDeserialized(Policy policy)
        {
            traceContext.TraceMethodEnter("ValidateDeserialized");
            // Tracing Input Parameters
            traceContext.TraceParameters(policy);
            ValidateProperties(ValidationMethod.Alter, true, null);
            ValidateExpression(ValidationMethod.Alter, true, null);
            ValidateReferencesDeserialized(policy);
            traceContext.TraceMethodExit("ValidateDeserialized");
        }

        /// <summary>
        /// The following function is validating the way that this condition is used by
        /// all the DMF objects that reference it.
        /// For example overwritten deserialized conditions can change their Facet 
        /// as long as their referenced only by deserialized Policy and OS.
        /// </summary>
        /// <param name="policy"></param>
        void ValidateReferencesDeserialized(Policy policy)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateReferencesDeserialized"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(policy);
                // It's important to get this.Facet first as it populates this.cachedFacet (if it's empty)
                if (this.Facet != this.cachedFacet || this.HasScript)
                {
                    foreach (Policy p in this.Parent.Policies)
                    {
                        if (p.DependsOnCondition(this.Name))
                        {
                            if (this.Facet != this.cachedFacet && p != policy)
                            {
                                Exception ex = new DmfException(ExceptionTemplatesSR.CannotChangeFacet);
                                throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                            }
    
                            if ((p.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.CheckOnChanges ||
                                p.AutomatedPolicyEvaluationMode == AutomatedPolicyEvaluationMode.Enforce) &&
                                this.HasScript)
                            {
                                // We don't allow dynamic script conditions with CoC or enforce evaluation mode
                                Exception ex = new DmfException(ExceptionTemplatesSR.ReferencedConditionsCannotContainScript);
                                throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                            }
                        }
                    }
                }

                foreach (ObjectSet os in this.Parent.ObjectSets)
                {
                    if (os.Name == policy.ObjectSet)
                    {
                        continue;
                    }

                    if (os.EnumReferencedConditionNames().Contains(this.Name))
                    {
                        if (this.Facet != this.cachedFacet)
                        {
                            Exception ex = new DmfException(ExceptionTemplatesSR.CannotChangeFacet);
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                        }
                        else if (!this.IsEnumerable)
                        {
                            Exception ex = new DmfException(ExceptionTemplatesSR.ConditionCannotBeUsedForFiltering(this.Name));
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, Name, ex));
                        }
                    }
                }

            }
        }

    }
}
