// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Threading;

namespace Microsoft.SqlServer.Management.HadrModel
{

    /// <summary>
    /// The abstract class represents the base validator class later will be inheranted to different validators with different rules 
    /// </summary>
    public abstract class Validator
    {
        /// <summary>
        /// validator name
        /// </summary>
        public string Name
        {
            get;
            private set;
        }
        /// <summary>
        /// the base constructor
        /// </summary>
        /// <param name="name"></param>
        public Validator(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            this.Name = name;
            this.ValidatorProgressEventHandler = null;
        }

        /// <summary>
        /// Validator progress event handler
        /// </summary>
        public event ValidatorUpdateEventHandler ValidatorProgressEventHandler;


        /// <summary>
        /// If the optional delegate is passed, then it is called with the execution policy. 
        /// Otheriwse, the following logic is executed
        /// While the policy has not expired, the derived validator will keep doing it validation with a back off interval between every validation
        /// The policy will expire due to following reason
        /// 1.the policy reaches its max times of trying
        /// 2.the validator set the policy to be expired due to some fatal exception
        /// 3.the validator set the policy to be exproed due to successful validation
        /// </summary>
        /// <param name="policy">validation retry policy for both default and override delegate</param>
        /// <param name="validationDelegate">optional override delegate</param>
        public void Validate(IExecutionPolicy policy, ScenarioValidatorHandler validationDelegate)
        {
            Exception exception = null;

            if (validationDelegate != null)
            {
                validationDelegate(policy);
            }
            else
            {
                this.UpdateStatus(new ValidatorEventArgs(this.Name, Resource.ValidatorEventArgsValidationStarted));
                while (policy.ResumeExecution())
                {
                    try
                    {
                        this.Validate(policy);
                    }
                    catch (Exception ex)
                    {
                        if (policy.Expired)
                        {
                            this.UpdateStatus(new ValidatorEventArgs(this.Name, string.Format(Resource.ValidatorEventArgsValidationFailed, ex.Message)));
                            exception = ex;
                            break;
                        }
                        else
                        {
                            this.UpdateStatus(new ValidatorEventArgs(this.Name, string.Format(Resource.ValidatorEventArgsValidationFailedWithRetry, ex.Message)));
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
                    this.UpdateStatus(new ValidatorEventArgs(this.Name, Resource.ValidatorEventArgsValidationComplete));
                }
            }
        }

        /// <summary>
        /// Execute validation logic
        /// </summary>
        /// <param name="policy">execution policy</param>
        protected abstract void Validate(IExecutionPolicy policy);

        /// <summary>
        /// Send validation progress event
        /// </summary>
        /// <param name="e"></param>
        protected void UpdateStatus(ValidatorEventArgs e)
        {
            if (this.ValidatorProgressEventHandler != null)
            {
                this.ValidatorProgressEventHandler(this, e);
            }
        }

    }
}
