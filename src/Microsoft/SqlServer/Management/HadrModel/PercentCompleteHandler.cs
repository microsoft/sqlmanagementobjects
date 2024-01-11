// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    public class PercentCompleteHandler
    {
        /// <summary>
        /// Caller Task Name
        /// </summary>
        private string taskName;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="taskName"></param>
        public PercentCompleteHandler(string taskName)
        {
            this.taskName = taskName;
        }

        /// <summary>
        /// BackUp/Restore PercentCompleteNotification number
        /// </summary>
        public const int PercentCompleteNotification = 5;

        /// <summary>
        /// initial value to indicate the percentage of BackUP/Restore process
        /// </summary>
        protected int percentComplete = 0;

        /// <summary>
        /// Task progress event handler
        /// </summary>
        public event TaskUpdateEventHandler TaskProgressEventHandler;

        /// <summary>
        /// Send tasks execution update event
        /// </summary>
        /// <param name="e"></param>
        public void UpdateStatus(TaskEventArgs e)
        {
            if (this.TaskProgressEventHandler != null)
            {
                this.TaskProgressEventHandler(this, e);
            }
        }

        /// <summary>
        /// Backup/Restore PercentComplete Event Handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void percentCompleteHandler(object sender, PercentCompleteEventArgs e)
        {
            this.percentComplete = e.Percent;

            this.UpdateStatus(new TaskEventArgs(this.taskName, "Perform", TaskEventStatus.Running));

        }
    }
}