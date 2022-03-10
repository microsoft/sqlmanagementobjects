// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Fixed retry timeout is an execution policy that allow task execution
    /// and retry based on a timeout rather a retry count. 
    /// A fixed wait interval is used beteween retries. 
    /// </summary>
    public class FixedTimeoutRetryPolicy : IExecutionPolicy
    {
        /// <summary>
        /// total time to execute the task for
        /// </summary>
        private TimeSpan timeout;

        /// <summary>
        /// wait interval between retires
        /// </summary>
        private TimeSpan interval;

        /// <summary>
        /// a start time to track when the actual execution is started
        /// </summary>
        private DateTime startTime;

        /// <summary>
        /// ctor create an instance of the polict
        /// </summary>
        /// <param name="timeout">execution timeout</param>
        /// <param name="interval">wait interval between retires</param>
        public FixedTimeoutRetryPolicy(TimeSpan timeout, TimeSpan interval)
        {
            if (timeout == null)
            {
                throw new ArgumentNullException("timeout");
            }

            if (interval == null)
            {
                throw new ArgumentNullException("interval");
            }

            this.timeout = timeout;
            this.interval = interval;
            this.startTime = DateTime.MaxValue;
        }

        /// <summary>
        /// Resume execution function is called to check if the policy allows the execution to resume. 
        /// It also setup the internal state and updates on subsequent calls.
        /// </summary>
        /// <returns></returns>
        public bool ResumeExecution()
        {
            if (this.startTime == DateTime.MaxValue)
            {
                this.startTime = DateTime.Now;
            }

            TimeSpan executionTime = this.startTime - DateTime.Now;

            if (executionTime < this.timeout)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Policy expired
        /// </summary>
        public bool Expired
        {
            get
            {
                if (this.startTime == DateTime.MaxValue)
                {
                    return false;
                }

                return (this.startTime - DateTime.Now) > this.timeout;
            }
            set
            {
                if (value == true)
                {
                    this.timeout = TimeSpan.Zero; 
                }
            }
        }

        /// <summary>
        /// Wait time between retries
        /// </summary>
        /// <returns></returns>
        public TimeSpan BackoffInterval()
        {
            return this.interval;
        }
    }
}
