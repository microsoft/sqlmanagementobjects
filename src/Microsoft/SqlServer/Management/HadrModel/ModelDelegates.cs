// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Scenario validator delegate. The delegate is used to override 
    /// a validator implementation
    /// </summary>
    public delegate void ScenarioValidatorHandler(IExecutionPolicy policy);
    
    /// <summary>
    /// Scenario task delete. The delegate is used to override
    /// a taks implementation
    /// </summary>
    public delegate void ScenarioTaskHandler(IExecutionPolicy policy);

    /// <summary>
    /// Validator update event handler. The event is sent during validation
    /// for progress update
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ValidatorUpdateEventHandler(object sender, ValidatorEventArgs e);

    /// <summary>
    /// Task update event handler. The event is sent during task
    /// for progress update
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void TaskUpdateEventHandler(object sender, TaskEventArgs e);
}
