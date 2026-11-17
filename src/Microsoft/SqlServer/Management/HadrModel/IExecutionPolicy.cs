// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// An interface for task execution policy basic function.
    /// An exection policy mainly dictates if execution should resume
    /// if how long to wait before retries. 
    /// The implemetnation of this interface may have different flavors:
    /// time based policy, count baed, never expires execution etc...
    /// </summary>
    public interface IExecutionPolicy
    {    
        /// <summary>
        /// Task execution expired
        /// </summary>
        /// <returns></returns>
        bool Expired
        {
            get;
            set;
        }

        /// <summary>
        /// This function is called by the task that owns the policy to determine
        /// if the execution should resume or not.
        /// </summary>
        /// <returns></returns>
        bool ResumeExecution();

        /// <summary>
        /// Calculates a time interval to wait between execution
        /// </summary>
        /// <returns></returns>
        TimeSpan BackoffInterval();
    }
}
