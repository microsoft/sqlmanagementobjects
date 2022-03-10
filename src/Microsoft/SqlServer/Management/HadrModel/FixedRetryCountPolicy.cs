// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// The class implements a retry execution policy with a fixed retry backoff interval
    /// </summary>
    public class FixedRetryCountPolicy : IExecutionPolicy
    {
        /// <summary>
        /// track number of retries
        /// </summary>
        private int retryCount;

        /// <summary>
        /// maximum number of retries
        /// </summary>
        private int maxRetry;

        /// <summary>
        /// backoff interval between retries
        /// </summary>
        private TimeSpan interval;

        /// <summary>
        /// Create an instance class
        /// </summary>
        /// <param name="interval">wait between retries time interval</param>
        /// <param name="maxRetry">maximum number of retries</param>
        public FixedRetryCountPolicy(TimeSpan interval, int maxRetry)
        {
            this.retryCount = 0;
            this.interval = interval;
            this.maxRetry = maxRetry;
        }

        /// <summary>
        /// The function increments the execution count and until the maximum is reached. 
        /// Execution is allowed until the maximum retry count is reached. 
        /// </summary>
        /// <returns>returns true if execution should resume and false otherwise</returns>
        public bool ResumeExecution()
        {
            if (this.Expired)
            {
                return false;
            }

            ++retryCount;

            return true;
        }

        /// <summary>
        /// Expires when all retris are exhusted
        /// </summary>
        public bool Expired
        {
            get
            {
                if (this.retryCount >= this.maxRetry)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            
            set
            {
                if (value == true)
                {
                    this.maxRetry = 0;
                }
            }
        }

        /// <summary>
        /// Backoff interval between retries
        /// </summary>
        /// <returns>time interval timespan</returns>
        public TimeSpan BackoffInterval()
        {
            return this.interval;
        }
    }
}
