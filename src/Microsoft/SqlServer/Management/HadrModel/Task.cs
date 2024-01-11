// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Threading;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// HADR task class
    /// </summary>
    public abstract class HadrTask
    {
        /// <summary>
        /// Task progress event handler
        /// </summary>
        public event TaskUpdateEventHandler TaskProgressEventHandler;

        /// <summary>
        /// ExecutionPolicy for retry or expire the process.
        /// </summary>
        protected IExecutionPolicy policy;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="name">task name</param>
        public HadrTask(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            this.token = CancellationToken.None;
            this.Name = name;
            this.TaskProgressEventHandler = null;
        }

        /// <summary>
        /// Task name
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// cancellation token passed during perform stage;
        /// </summary>
        private CancellationToken token;

        /// <summary>
        /// IsCancelled for derived class to check for long running tasks and termnate early
        /// </summary>
        public bool IsCancelled
        {
            get 
            {
                if (this.token == CancellationToken.None)
                {
                    return false;
                }
                else
                {
                    return this.token.IsCancellationRequested;
                }
            }
        }

        /// <summary>
        /// For isCancelled Check
        /// </summary>
        /// <param name="waitTimeInMs"></param>
        protected void TaskWait(int waitTimeInMs)
        {
            int intervalInMs = 5000;
            int i = waitTimeInMs / intervalInMs;
            while (i-- > 0)
            {
                if (this.IsCancelled)
                {
                    throw new Exception(Resource.TaskEventArgsTaskExecutionCancelled);
                }

                Thread.Sleep(intervalInMs);
            }
        }

        /// <summary>
        /// Execute task steps. If a delegate is passed, then the delegate is executed
        /// with a policy. Otherwise, the derived class logic is executed
        /// </summary>
        /// <param name="policy">execution policy used with default implementation and override delegate</param>
        /// <param name="token">a cancellation token controlled by the task provider </param>
        /// <param name="taskDelegate">optional delegate used to override the provider implementation</param>
        public void Perform(IExecutionPolicy policy, CancellationToken token = default(CancellationToken), ScenarioTaskHandler taskDelegate = null)
        {
            Exception exception = null;

            this.token = token;

            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            if (taskDelegate != null)
            {
                taskDelegate(policy);
            }
            else
            {
                this.UpdateStatus(new TaskEventArgs(this.Name, Resource.TaskEventArgsTaskStarted));
                while (policy.ResumeExecution())
                {
                    try
                    {
                        if (this.IsCancelled)
                        {
                            this.UpdateStatus(new TaskEventArgs(this.Name, Resource.TaskEventArgsTaskExecutionCancelled));
                            break;
                        }

                        this.Perform(policy);
                    }
                    catch (Exception ex)
                    {
                        if (policy.Expired)
                        {
                            this.UpdateStatus(new TaskEventArgs(this.Name, string.Format(CultureInfo.InvariantCulture,Resource.TaskEventArgsTaskExecutionFailed, ex.Message)));
                            exception = ex;
                            break;
                        }
                        else
                        {
                            this.UpdateStatus(new TaskEventArgs(this.Name, string.Format(CultureInfo.InvariantCulture,Resource.TaskEventArgsTaskExecutionFailedWithRetry, ex.Message)));
                        }

                        Thread.Sleep(policy.BackoffInterval());
                    }
                }

                if (exception != null)
                {
                    throw exception;
                }
                else
                {
                    this.UpdateStatus(new TaskEventArgs(this.Name, Resource.TaskEventArgsTaskComplete));
                }
            }
        }

        /// <summary>
        /// Rollback task completed steps. If a delegate is passed, then the delegate is executed
        /// with a policy. Otherwise, the derived class logic is executed
        /// </summary>
        /// <param name="policy">rollback execution logic</param>
        /// <param name="rollbackDelegate">optional delegate used to override the provider implementation</param>
        public void Rollback(IExecutionPolicy policy, ScenarioTaskHandler rollbackDelegate = null)
        {
            Exception exception = null;

            if (policy == null)
            {
                throw new ArgumentNullException("policy");
            }

            if (rollbackDelegate != null)
            {
                rollbackDelegate(policy);
            }
            else
            {
                this.UpdateStatus(new TaskEventArgs(this.Name, Resource.TaskEventArgsTaskStarted));
                while (policy.ResumeExecution())
                {
                    try
                    {
                        this.Rollback(policy);
                    }
                    catch (Exception ex)
                    {
                        if (policy.Expired)
                        {
                            this.UpdateStatus(new TaskEventArgs(this.Name, string.Format(CultureInfo.InvariantCulture, Resource.TaskEventArgsTaskExecutionFailed, ex.Message)));
                            exception = ex;
                            break;
                        }
                        else
                        {
                            this.UpdateStatus(new TaskEventArgs(this.Name, string.Format(CultureInfo.InvariantCulture, Resource.TaskEventArgsTaskExecutionFailedWithRetry, ex.Message)));
                        }

                        Thread.Sleep(policy.BackoffInterval());
                    }
                }

                if (exception != null)
                {
                    throw exception;
                }
                else
                {
                    this.UpdateStatus(new TaskEventArgs(this.Name, Resource.TaskEventArgsTaskComplete));
                }
            }
        }

        /// <summary>
        /// Tasks status event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TaskUpdateEventHandler(object sender, TaskEventArgs e)
        {
            this.UpdateStatus(e);
        }

        /// <summary>
        /// Task implementation
        /// </summary>
        /// <param name="policy">rollback execution logic</param>
        protected abstract void Perform(IExecutionPolicy policy);

        /// <summary>
        /// Task rollback logic
        /// </summary>
        /// <param name="policy">rollback execution logic</param>
        protected abstract void Rollback(IExecutionPolicy policy);

        /// <summary>
        /// Send tasks execution update event
        /// </summary>
        /// <param name="e"></param>
        protected void UpdateStatus(TaskEventArgs e)
        {
            if (this.TaskProgressEventHandler != null)
            {
                this.TaskProgressEventHandler(this, e);
            }
        }
    }
}
