// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;

namespace GetSmoObject

{
    internal class SqlClientEventLogger : EventListener
    {
        private StringBuilder logs= new StringBuilder();
        public SqlClientEventLogger()
        {
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
            if (eventData.Payload != null)
            {
                var now = DateTime.UtcNow.ToString("o");
                logs = logs.Append(string.Join(Environment.NewLine, eventData.Payload.Select(p => $"{now} [{Environment.CurrentManagedThreadId}]: {p}")) + Environment.NewLine);
            }
        }
        public override string ToString()
        {
            return logs.ToString();
        }
    }
}
