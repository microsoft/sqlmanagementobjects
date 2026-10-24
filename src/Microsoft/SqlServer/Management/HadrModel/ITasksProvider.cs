// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Tasks providers interface. A scenario list of tasks to complete
    /// </summary>
    public interface ITasksProvider 
    {
        /// <summary>
        /// Get the list of tasks the provider supports
        /// </summary>
        /// <returns></returns>
        List<HadrTask> Tasks();
    }
}
