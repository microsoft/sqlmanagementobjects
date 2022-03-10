// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Interface to be implemented by tasks that support scripting.
    /// 
    /// Scripting is implemented by changing the ConnectionMode of 
    /// the connection to CaptureSql instead of ExecuteSql.
    /// 
    /// For this to be done by the UI, the task needs to expose the 
    /// connections on which it does work.
    /// </summary>
    public interface IScriptableTask
    {
        /// <summary>
        /// The connections on which the task will do work
        /// </summary>
        List<ServerConnection> ScriptingConnections 
        { 
            get; 
        }
    }
}
