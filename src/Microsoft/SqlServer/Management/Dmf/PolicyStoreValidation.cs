// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the non-generated part of the PolicyStore class.
    /// </summary>
    public sealed partial class PolicyStore : ISfcValidate
    {
        /// <summary>
        /// master validation method
        /// </summary>
        /// <param name="validationMode"></param>
        /// <param name="throwOnFirst"></param>
        /// <param name="validationState"></param>
        void Validate(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            ValidateProperties(validationMode, throwOnFirst, validationState);
        }

        void ValidateProperties(string validationMode, bool throwOnFirst, ValidationState validationState)
        {
            if (HistoryRetentionInDays < 0)
            {
                Exception ex = new DmfException(ExceptionTemplatesSR.ValuePositive);

                if (throwOnFirst)
                {
                    throw traceContext.TraceThrow(new ObjectValidationException(ExceptionTemplatesSR.PolicyStore, Name, ex));
                }
                else
                {
                    validationState.AddError(ex, "HistoryRetentionInDays");
                }

            }
        }

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

    }
}
