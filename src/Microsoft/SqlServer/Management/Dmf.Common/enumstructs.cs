// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Dmf
{

    /// <summary>
    /// Possible Evaluation modes for automated policies
    /// </summary>
    [Flags]
    public enum AutomatedPolicyEvaluationMode
    {
        // TODO: Change this to the AutomatedPolicyEvaluationMode
        /// No action
        None = 0,
        /// enforce the policy by rolling back changes
        /// that break the policy
        Enforce = 1,
        /// check only when changes to the targets occur
        CheckOnChanges = 2,
        /// check on a set schedule
        CheckOnSchedule = 4,
    }

    /// <summary>
    /// The PolicyEffectiveState bit flag enum is used as the data
    /// table for the enumeration of policies on a particular target
    /// </summary>
    [Flags]
    public enum PolicyEffectiveState
    {
        /// Default value
        None = 0,
        /// The policy is enabled.
        Enabled = 1,
        /// The policy filter includes the target.
        InFilter = 2,
        /// The policy is in a category that the database subscribes to.
        InCategory = 4
    }

    /// <summary>
    /// The ImportPolicyEnabledState bit flag enum is used as a parameter to
    /// PolicyStore.ImportPolicy. It is used to set the imported Policy's Enabled property.
    /// </summary>
    public enum ImportPolicyEnabledState
    {
        /// The Policy.Enabled property will not be changed. Whatever the policy's Enabled state
        /// was, it will be retained.
        Unchanged,
        /// The policy is enabled on import.
        Enable,
        /// The policy is disabled on import.
        Disable
    }
}
