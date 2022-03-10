// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This event argument for validator progress status update
    /// </summary>
    public sealed class TaskEventArgs : EventArgs
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="taskName"></param>
        /// <param name="taskDetails"></param>
        public TaskEventArgs(string taskName, string taskDetails)
        {
            this.Name = taskName;
            this.Details = taskDetails;
        }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="taskName">The caller task's Name</param>
        /// <param name="taskDetails">Task Details</param>
        /// <param name="taskStatus">Task Status</param>
        public TaskEventArgs(string taskName, string taskDetails, TaskEventStatus taskStatus)
        {
            this.Name = taskName;
            this.Status = taskStatus;
            this.Details = taskDetails;
        }

        /// <summary>
        /// Task Name
        /// </summary>
        public string Name 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Task execution status
        /// </summary>
        public TaskEventStatus Status
        {
            get;
            set;
        }

        /// <summary>
        /// Task execution details
        /// </summary>
        public string Details
        {
            get;
            set;
        }
    }
}
