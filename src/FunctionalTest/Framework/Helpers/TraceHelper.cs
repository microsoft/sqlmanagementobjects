// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    using System.Diagnostics;

    /// <summary>
    /// Helper for configuring Trace
    /// </summary>
    public class TraceHelper
    {
        /// <summary>
        /// Enable autoflush so that flush is called on trace after every write
        /// </summary>
        public static void EnableAutoFlush()
        {
            Trace.AutoFlush = true;
        }
        
        /// <summary>
        /// Disable autoflush so that flush is not called on trace after every write
        /// </summary>
        public static void DisableAutoFlush()
        {
            Trace.AutoFlush = false;
        }

        /// <summary>
        /// The default trace output lacks timestamps, this adds it
        /// </summary>
        /// <param name="message"></param>
        public static void TraceInformation(string message)
        {
           Trace.TraceInformation($"{DateTime.Now.ToString("o")} - {message}");
        }

        /// <summary>
        /// The default trace output lacks timestamps, this adds it
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void TraceInformation(string format, params object[] args)
        {
           Trace.TraceInformation($"{DateTime.Now.ToString("o")} - {format}", args);
        }
    }
}
