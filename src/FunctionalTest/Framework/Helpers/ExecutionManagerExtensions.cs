// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Extension methods on the SMO Server object to simplify some common test patterns
    /// </summary>
    public static class ExecutionManagerExtensions
    {
        /// <summary>
        /// Runs the given action and returns the captured query text.
        /// The CapturedSql property of the underlying ServerConnection is cleared on return.
        /// </summary>
        /// <param name="executionManager"></param>
        /// <param name="action"></param>
        /// <param name="alsoExecute"></param>
        public static StringCollection RecordQueryText(this ExecutionManager executionManager, Action action, bool alsoExecute = false)
        {
            var currentMode = executionManager.ConnectionContext.SqlExecutionModes;
            executionManager.ConnectionContext.SqlExecutionModes = alsoExecute ? SqlExecutionModes.ExecuteAndCaptureSql : SqlExecutionModes.CaptureSql;
            var results = new StringCollection();
            try
            {
                action();
                results.AddCollection(executionManager.ConnectionContext.CapturedSql.Text);
                return results;
            }
            finally
            {
                executionManager.ConnectionContext.SqlExecutionModes = currentMode;
                executionManager.ConnectionContext.CapturedSql.Clear();
            }
        }
    }
}
