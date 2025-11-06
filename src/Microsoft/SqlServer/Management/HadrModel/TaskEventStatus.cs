// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.HadrModel
{
    public enum TaskEventStatus
    {
        /// <summary>
        /// Not Start state
        /// </summary>
        NotStart,
        
        /// <summary>
        /// Started  
        /// </summary>
        Started,

        /// <summary>
        /// Cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Running
        /// </summary>
        Running,

        /// <summary>
        /// Failed
        /// </summary>
        Failed,

        /// <summary>
        /// Complete
        /// </summary>
        Completed
    }
}
