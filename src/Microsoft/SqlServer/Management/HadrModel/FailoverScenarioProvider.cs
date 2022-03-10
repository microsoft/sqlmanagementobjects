// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.HadrData;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Failover from secondary scenario provider
    /// </summary>
    public class FailoverScenarioProvider : ScenarioProvider<IValidatorProvider, ITasksProvider>
    {
        #region fields
        /// <summary>
        /// Failover Data Object
        /// </summary>
        private FailoverData failoverData; 
        #endregion

        #region ctor
        /// <summary>
        /// ctor
        /// </summary>
        public FailoverScenarioProvider(FailoverData failoverData)
            : base()
        {
            if (failoverData == null)
            {
                throw new ArgumentNullException("failoverData");
            }
            this.failoverData = failoverData;
        }
        #endregion

        #region ScenarioProvider

        /// <summary>
        /// Scenario validators
        /// </summary>
        /// <returns>validationt tasks</returns>
        public override List<Validator> Validators()
        {
            var validators = new List<Validator>();

            validators.Add(new FailoverValidator(this.failoverData));
            validators.Add(new FailoverWaitRoleChangeValidator(this.failoverData));
            validators.Add(new FailoverQuorumVoteConfigurationValidator(this.failoverData));

            return validators;
        }

        /// <summary>
        /// Scenario rollback task
        /// </summary>
        /// <returns>list of rollback task</returns>
        public override List<HadrTask> RollbackTasks()
        {
            return null;
        }

        /// <summary>
        /// Get the list of tasks the provider supports
        /// </summary>
        /// <returns></returns>
        public override List<HadrTask> Tasks()
        {
            List<HadrTask> tasks = new List<HadrTask>();

            tasks.Add(new FailoverTask(this.failoverData));
     
            return tasks;
        }
        #endregion
    }
}
