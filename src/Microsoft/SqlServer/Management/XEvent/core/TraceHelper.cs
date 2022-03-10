// Copyright (c) Microsoft.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Diagnostics.STrace
{
}

namespace Microsoft.SqlServer.Management.XEvent

{
    using Microsoft.SqlServer.Diagnostics.STrace;

    internal static class TraceHelper
    {
        private const string ComponentName = "SMO XEvent Management";
        private static TraceContext traceContext;

        static TraceHelper()
        {
            traceContext = TraceContext.GetTraceContext(TraceHelper.ComponentName, "Microsoft.SqlServer.Management.XEvent");
        }

        public static TraceContext TraceContext
        {
            get { return traceContext; }
        }
    }
}

#if !STRACE

//
// Stubbing out the tracing methods in the STrace namespace for .NET Core.
//


namespace Microsoft.SqlServer.Management.XEvent
{
    using System;

    internal class MethodTraceContext : IDisposable
    {
        public void TraceParameterIn(params object[] args)
        {
        }

        public MethodContext GetActivityContext(params object[] args)
        {
            return new MethodContext();
        }

        public void TraceVerbose(params object[] args)
        {

        }

        public void TraceCatch(params object[] args)
        {

        }

        public void TraceError(params object[] args)
        {

        }
        
        public void Assert(params object[] args)
        {

        }

        public void TraceCriticalError(params object[] args)
        {
        }
        
        public void Dispose()
        {
        }
    }

    internal class MethodContext : MethodTraceContext
    { 
    }

    internal class TraceContext
    {
        public static TraceContext GetTraceContext(string name, string component)
        {
            return new TraceContext();
        }

        public void Assert(bool assertion, string message, params object[] args)
        {

        }

        public void TraceCriticalError(params object[] args)
        {
        }

        public void TraceMethodEnter(params object[] args)
        {
        }
        
        public void TraceMethodExit(params object[] args)
        {
        }
        
        public void TraceParameterIn(params object[] args)
        {
        }

        public MethodContext GetMethodContext(params object[] args)
        {
            return new MethodContext();
        }

        public MethodContext GetActivityContext(params object[] args)
        {
            return new MethodContext();
        }

        public void TraceVerbose(params object[] args)
        {

        }

        public void TraceCatch(params object[] args)
        {

        }

        public void TraceError(params object[] args)
        {

        }
    }
}
#endif