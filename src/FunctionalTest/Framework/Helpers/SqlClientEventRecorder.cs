// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using static System.FormattableString;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{

    /// <summary>
    /// Base class for recording events from EventSource providers.
    /// Provides common functionality for filtering by thread and managing event recording lifecycle.
    /// </summary>
    [DebuggerDisplay("Listener {ListenerName} <{EventSourceName}:{ThreadId}>")]
    public abstract class EventRecorderBase : EventListener
    {
        private bool isRecording = false;
        private readonly EventLevel eventLevel;
        private readonly EventKeywords eventKeywords;

        /// <summary>
        /// Optional name of the listener
        /// </summary>
        public string ListenerName { get; }


        /// <summary>
        /// The managed thread id to monitor for events. When set to -1, records events for all threads
        /// </summary>
        public int ThreadId { get; }

        /// <summary>
        /// The list of recorded events
        /// </summary>
        public IList<EventWrittenEventArgs> Events { get; }

        /// <summary>
        /// Gets or sets whether to log events to TraceHelper when they arrive.
        /// When true (default), events are logged via TraceHelper.TraceInformation.
        /// When false, events are only collected in the Events list without logging.
        /// </summary>
        public bool EnableTraceLogging { get; set; }

        /// <summary>
        /// Gets the name of the EventSource being recorded
        /// </summary>
        protected string EventSourceName { get; }

        /// <summary>
        /// Initializes a new instance of EventRecorderBase
        /// </summary>
        /// <param name="eventSourceName">Name of the EventSource to record</param>
        /// <param name="eventLevel">Verbosity level for events to record</param>
        /// <param name="eventKeywords">Keyword mask for filtering events</param>
        /// <param name="managedThreadId">Thread ID to filter events by, or -1 for all threads</param>
        /// <param name="listenerName">Optional name for the listener</param>
        protected EventRecorderBase(string eventSourceName, EventLevel eventLevel, EventKeywords eventKeywords, int managedThreadId = -1, string listenerName = "")
        {
            if (string.IsNullOrEmpty(eventSourceName))
            {
                throw new ArgumentNullException(nameof(eventSourceName));
            }

            EventSourceName = eventSourceName;
            this.eventLevel = eventLevel;
            this.eventKeywords = eventKeywords;
            ListenerName = listenerName;
            ThreadId = managedThreadId;
            Events = new List<EventWrittenEventArgs>();
            EnableTraceLogging = true; // Default to enabled for backward compatibility
            EventSourceCreated += OnEventSourceCreated;
        }

        private void OnEventSourceCreated(object sender, EventSourceCreatedEventArgs e)
        {
            if (e.EventSource.Name.Equals(EventSourceName, StringComparison.Ordinal))
            {
                EnableEvents(e.EventSource, eventLevel, eventKeywords);
            }
        }

        /// <summary>
        /// Called when an event is written. Records the event if recording is active and thread filter matches.
        /// </summary>
        /// <param name="eventData">The event data</param>
        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (isRecording && (ThreadId == -1 || Environment.CurrentManagedThreadId == ThreadId))
            {
                base.OnEventWritten(eventData);
                Events.Add(eventData);
                if (EnableTraceLogging)
                {
                    LogEventData(eventData);
                }
            }
        }

        /// <summary>
        /// Logs event data. Override to customize logging behavior.
        /// This method is only called when EnableTraceLogging is true.
        /// </summary>
        /// <param name="eventData">The event data to log</param>
        public virtual void LogEventData(EventWrittenEventArgs eventData)
        {

            // use same format as SSMS output window
            var baseMessage = $"[{Environment.CurrentManagedThreadId}]: {eventData.EventName}: ";
            string message;
            if (!string.IsNullOrEmpty(eventData.Message))
            {
                try
                {
                    message = baseMessage + string.Format(CultureInfo.InvariantCulture, eventData.Message, eventData.Payload.ToArray()) + Environment.NewLine;
                }
                catch (FormatException)
                {
                    message = baseMessage + Environment.NewLine;
                }
            }
            else
            {
                message = baseMessage + string.Join($"{Environment.NewLine}\t", eventData.Payload.Select((p, i) => Invariant($"{eventData.PayloadNames[i]}:{p}"))) + Environment.NewLine;
            }
            switch (eventData.Level)
            {
                case EventLevel.Critical:
                case EventLevel.Error:
                case EventLevel.Warning:
                    TraceHelper.TraceWarning(message);
                    break;
                default:
                    TraceHelper.TraceInformation(message);
                    break;
            }
        }

        /// <summary>
        /// Gets a descriptive name for this recorder type, used in logging
        /// </summary>
        protected virtual string RecorderTypeName => GetType().Name;

        /// <summary>
        /// Begins or resumes event recording
        /// </summary>
        public void Start()
        {
            if (!isRecording)
            {
                TraceHelper.TraceInformation($"{RecorderTypeName}.Start [{ThreadId}] for EventSource '{EventSourceName}'");
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
                TraceHelper.TraceInformation($"{RecorderTypeName}.Stop [{ThreadId}] for EventSource '{EventSourceName}'");
            }
            isRecording = false;
        }

        /// <summary>
        /// Disposes the recorder and stops recording
        /// </summary>
        public override void Dispose()
        {
            Stop();
            base.Dispose();
        }
    }

    /// <summary>
    /// SqlClientEventRecorder captures SqlClient traces
    /// </summary>
    public class SqlClientEventRecorder : EventRecorderBase
    {
        /// <summary>
        /// Constructs a new SqlClientEventRecorder that captures events on all threads
        /// </summary>
        public SqlClientEventRecorder([CallerMemberName] string name = "") : this(-1, 3, name)
        {
        }

        /// <summary>
        /// Constructs a new SqlClientEventRecorder that captures events for a specific managed thread
        /// </summary>
        /// <param name="managedThreadId">Thread ID to filter events by, or -1 for all threads</param>
        /// <param name="keywords">The SqlClient event keywords from https://learn.microsoft.com/sql/connect/ado-net/enable-eventsource-tracing. 
        /// Defaults to ExecutionTrace | Trace. </param>
        /// <param name="name"></param>
        public SqlClientEventRecorder(int managedThreadId, int keywords = 3, [CallerMemberName] string name = "")
            : base("Microsoft.Data.SqlClient.EventSource", EventLevel.Verbose, (EventKeywords)keywords, managedThreadId, name)
        {
        }
    }

    /// <summary>
    /// SmoTraceEventRecorder captures SQL Server Management Objects (SMO) traces
    /// </summary>
    public class SmoTraceEventRecorder : EventRecorderBase
    {
        /// <summary>
        /// Constructs a new SmoTraceEventRecorder that captures all SMO events on all threads
        /// </summary>
        public SmoTraceEventRecorder([CallerMemberName] string name = "") : this(-1, name)
        {
        }

        /// <summary>
        /// Constructs a new SmoTraceEventRecorder that captures all SMO events for a specific managed thread
        /// </summary>
        /// <param name="managedThreadId">Thread ID to filter events by, or -1 for all threads</param>
        /// <param name="name"></param>
        public SmoTraceEventRecorder(int managedThreadId, [CallerMemberName] string name = "")
            : base("Microsoft-SqlServer-Management-Smo", EventLevel.Verbose, EventKeywords.All, managedThreadId, name)
        {
        }

        /// <summary>
        /// Constructs a new SmoTraceEventRecorder with custom event level and keywords
        /// </summary>
        /// <param name="eventLevel">Verbosity level for events to record</param>
        /// <param name="eventKeywords">Keyword mask for filtering events by functional area</param>
        /// <param name="managedThreadId">Thread ID to filter events by, or -1 for all threads</param>
        public SmoTraceEventRecorder(EventLevel eventLevel, EventKeywords eventKeywords, int managedThreadId = -1)
            : base("Microsoft-SqlServer-Management-Smo", eventLevel, eventKeywords, managedThreadId)
        {
        }
    }
}
