// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is a non-generated part of ObjectSet class.
    /// </summary>
    public partial class ObjectSet : ISfcValidate
    {
        /// <summary>
        /// master validation method
        /// </summary>
        /// <param name="validationMode"></param>
        /// <param name="throwOnFirst"></param>
        /// <param name="validationState"></param>
        void Validate(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("Validate"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                ValidateProperties(validationMode, throwOnFirst, validationState);
                ValidateTargetSets(validationMode, throwOnFirst, validationState);
            }
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
                        // TODO: Change this to ExceptionTemplatesSR.ObjectSet
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
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.ManagementFacet, Name, ex));
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
            }
        }

        // Technically, you shouldn't need this function at all since the TargetSet are created by the ObjectSet, when
        // the Facet property is set
        // However, since the TargetSets collection is a first class ISfcCollection it exposes the Add and Remove methods
        // on the collection. As a result a rogue user could change the contents of the collection and attempt to slip
        // an invalid TargetSet through.
        // This function will prevent that scenario upon create/alter
        void ValidateTargetSets(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            using (MethodTraceContext methodTraceContext = traceContext.GetMethodContext("ValidateTargetSets"))
            {
                // Tracing Input Parameters
                methodTraceContext.TraceParameters(validationMode, throwOnFirst, validationState);
                //we only do target set validation when the ObjectSet actually gets created
                if (validationMode == ValidationMethod.Rename)
                {
                    return;
                }

                // Validate all TargetSets are from the same domain
                foreach (TargetSet ts in this.TargetSets)
                {
                if (0 != String.CompareOrdinal (this.RootLevel, ts.RootLevel))
                {
                    // An illegal Target Set has been defined in the current list of target sets
                    Exception ex = new DmfException(ExceptionTemplatesSR.InvalidUrnSkeleton(ts.TargetTypeSkeleton));
                    if (throwOnFirst)
                    {
                        throw new ObjectValidationException(this.GetType().Name, this.Name, ex);
                    }
                    else
                    {
                        validationState.AddError(ex, "TargetSets");
                    }
                }
            }

            // We don't preserve domain metadata in KJ
            // and have no way to infer it, other than from TargetSet path
            // In KJ we have to hardcode "Utility" domain as a special case

                ObjectSet sampleObjectSet = new ObjectSet(this.Parent, this.Name);
                if (0 != String.CompareOrdinal(this.RootLevel, "Utility"))
            {
                sampleObjectSet.Facet = this.Facet;
            }
            else
            {
                sampleObjectSet.SetFacetWithDomain(this.Facet, "Utility");
            }

                if (this.TargetSets.Count != sampleObjectSet.TargetSets.Count)
                {
                    // Mismatch number of TargetSets
                    Exception ex = new TargetSetCountMismatchException(this.Name, this.Facet);
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(this.GetType().Name, this.Name, ex));
                    }
                    else
                    {
                        validationState.AddError (ex, "TargetSets");
                    }
                }

                bool isSomeTargetSetEnabled = false;
                foreach (TargetSet targetSet in this.TargetSets)
                {
                    isSomeTargetSetEnabled |= targetSet.Enabled;

                    if (!sampleObjectSet.TargetSets.Contains(targetSet.TargetTypeSkeleton))
                    {
                        // An illegal Target Set has been defined in the current list of target sets
                        Exception ex = new UnsupportedTargetSetForFacetException(targetSet.TargetTypeSkeleton, this.Name, this.Facet);
                        if (throwOnFirst)
                        {
                            throw methodTraceContext.TraceThrow(new ObjectValidationException(this.GetType().Name, this.Name, ex));
                        }
                        else
                        {
                            validationState.AddError(ex, "TargetSets");
                        }
                    }

                    foreach (TargetSetLevel tsl in targetSet.Levels)
                    {
                        if (!String.IsNullOrEmpty(tsl.Condition))
                        {
                            Condition c = this.Parent.Conditions[tsl.Condition];
                            if (c == null)
                            {
                                throw new MissingObjectException(ExceptionTemplatesSR.Condition,tsl.Condition);
                            }
                            if (!c.IsEnumerable)
                            {
                                Exception ex = new DmfException(ExceptionTemplatesSR.ConditionCannotBeUsedForFiltering(tsl.Condition));
                                if (throwOnFirst)
                                {
                                    throw methodTraceContext.TraceThrow(new ObjectValidationException(this.GetType().Name, this.Name, ex));
                                }
                                else
                                {
                                    validationState.AddError(ex, "TargetSets");
                                }
                            }
                            if (!FacetRepository.GetFacetSupportedTypes(FacetRepository.GetFacetType(c.Facet)).Contains(tsl.TargetType))
                            {
                                Exception ex = new ConflictingPropertyValuesException(validationMode,
                                    ExceptionTemplatesSR.ManagementFacet, c.Facet,
                                    ExceptionTemplatesSR.ObjectType, tsl.TargetType.ToString());
                                if (throwOnFirst)
                                {
                                    throw methodTraceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.Condition, "TargetSets", ex));
                                }
                                else
                                {
                                    validationState.AddError(ex, "TargetSets");
                                }
                            }
                        }
                    }
                }

                if (isSomeTargetSetEnabled == false)
                {
                    // Not a single target set has been enabled and we need to throw
                    Exception ex = new NoTargetSetEnabledException(this.Name);
                    if (throwOnFirst)
                    {
                        throw methodTraceContext.TraceThrow(new ObjectValidationException(this.GetType().Name, this.Name, ex));
                    }
                    else
                    {
                        validationState.AddError(ex, "TargetSets");
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
    }
}
