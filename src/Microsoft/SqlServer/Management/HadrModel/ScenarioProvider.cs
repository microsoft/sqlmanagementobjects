// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Every HADR scenarion e.g. Add Replica to existing AG
    /// must provide a list of sceanrio tasks and validators. 
    /// 
    /// The list of validator ensure all required state is present to
    /// complete the scenario
    /// 
    /// THe list of tasks ensure the scenario is completed
    /// </summary>
    public abstract class ScenarioProvider<IValidatorProvider, ITasksProvider>
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public ScenarioProvider()
        {
            this.TaskTokenSource = new CancellationTokenSource();
        }
        
        /// <summary>
        /// Tasks token source provider
        /// </summary>
        public CancellationTokenSource TaskTokenSource
        {
            get;
            private set;
        }

        /// <summary>
        /// Scenario validators
        /// </summary>
        /// <returns>validationt tasks</returns>
        public abstract List<Validator> Validators();

        /// <summary>
        /// Scenario implementation tasks
        /// </summary>
        /// <returns>list of implementation task</returns>
        public abstract List<HadrTask> Tasks();

        /// <summary>
        /// Scenario rollback task
        /// </summary>
        /// <returns>list of rollback task</returns>
        public abstract List<HadrTask> RollbackTasks();
    }
}
