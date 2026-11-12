// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Provides helper methods for TSql Scripting
    /// </summary>
    public static class TSqlScriptingHelper
    {
        /// <summary>
        /// Records and returns the T-SQL script for the given action.
        /// </summary>
        /// <param name="server">Server to set the CaptureSql execution mode on.</param>
        /// <param name="action">The action whose script should be recorded.</param>
        /// <returns>The recorded script</returns>
        public static string GenerateScriptForAction(SMO.Server server, Action action)
        {
            string result = null;

            server.ExecuteWithModes(SqlExecutionModes.CaptureSql, () =>
            {
                action();
                result = server.ExecutionManager.ConnectionContext.CapturedSql.Text.ToSingleString();
                server.ExecutionManager.ConnectionContext.CapturedSql.Clear();
            });

            Assert.IsNotNull(result, "Could not record script");

            return result;
        }
    }
}
