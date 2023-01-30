// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// SqlClientEventRecorder captures SqlClient traces
    /// </summary>
    public class SqlClientEventRecorder : EventListener
    {
        private bool isRecording = false;
        /// <summary>
        /// The managed thread id to monitor for events. When set to -1, records events for all threads
        /// </summary>
        public int ThreadId { get; private set; }

        /// <summary>
        /// The list of recorded events
        /// </summary>
        public IList<EventWrittenEventArgs> Events { get; private set; }

        /// <summary>
        /// Constructs a new SqlClientEventRecorder that captures events on all threads
        /// </summary>
        public SqlClientEventRecorder() : this(-1)
        {

        }

        /// <summary>
        /// Constructs a new SqlClientEventRecorder that captures events for a specific managed thread
        /// </summary>
        /// <param name="managedThreadId"></param>
        public SqlClientEventRecorder(int managedThreadId)
        {
            ThreadId = managedThreadId;
            Events = new List<EventWrittenEventArgs>();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("Microsoft.Data.SqlClient.EventSource"))
            {
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)3);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (isRecording && (ThreadId == -1 || Environment.CurrentManagedThreadId == ThreadId))
            {
                base.OnEventWritten(eventData);
                Events.Add(eventData);
                TraceHelper.TraceInformation(string.Join(Environment.NewLine, eventData.Payload.Select(p => $"[{Environment.CurrentManagedThreadId}]: {p}")) + Environment.NewLine);
            }
        }

        /// <summary>
        /// Begins or resumes event recording
        /// </summary>
        public void Start()
        {
            if (!isRecording)
            {
                TraceHelper.TraceInformation($"SqlClientEventRecorder.Start [{ThreadId}]");
            }
            isRecording = true;
            
        }

        /// <summary>
        /// Stops event recording
        /// </summary>
        public void Stop()
        {
            if (isRecording)
            {
                TraceHelper.TraceInformation($"SqlClientEventRecorder.Stop [{ThreadId}]");
            }
            isRecording = false;
        }

        public override void Dispose()
        {
            Stop();
            base.Dispose();
        }
    }
}
