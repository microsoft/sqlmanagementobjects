// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// The interface represents the base validator provider class which consists of a list of Validator
    /// The provider will provide a sequence of validation rules with certain input parameter, 
    /// eg. a list of WAVM validator with WAVM configuration data object or a WA subscription info validator with WA subscription data 
    /// </summary>
    public interface IValidatorProvider
    {
        /// <summary>
        /// List of valiatior rules to ensure all requirements for the validator operation(s) are meet. 
        /// </summary>
        List<Validator> Validators();
    }
}
