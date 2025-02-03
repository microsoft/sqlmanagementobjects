// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Execution policy for running a task once
    /// </summary>
    public class RunOncePolicy : IExecutionPolicy
    {
        /// <summary>
        /// Track number of times task has run
        /// </summary>
        private int runCount = 0;

        /// <summary>
        /// The default backoff interval.
        /// 
        /// This interval is required to implement the IExecutionPolicy interface's
        /// BackoffInterval() function.
        /// </summary>
        private int DefaultBackoffIntervalInSeconds = 0;

        /// <summary>
        /// Expires after one run
        /// </summary>
        public bool Expired
        {
            get
            {
                return this.runCount > 0;
            }

            set
            {
                if (value == true)
                {
                    this.runCount = 1;
                }
            }
        }

        /// <summary>
        /// The function increases the runCount by one
        /// each time its called. 
        /// 
        /// It returns false if the policy is Expired.
        /// </summary>
        /// <returns></returns>
        public bool ResumeExecution()
        {
            if (this.Expired)
            {
                return false;
            }

            ++runCount;

            return true;
        }

        /// <summary>
        /// Backoff interval between retries.
        /// 
        /// Returns the <see cref="DefaultBackoffIntervalInSeconds"/>
        /// </summary>
        public TimeSpan BackoffInterval()
        {
            return TimeSpan.FromSeconds(DefaultBackoffIntervalInSeconds);
        }
    }
}
