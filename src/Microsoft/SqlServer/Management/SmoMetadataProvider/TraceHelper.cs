// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Diagnostics.STrace;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    internal static class TraceHelper
    {
        private const string ComponentName = "Transact-SQL Language Service";
        private static TraceContext traceContext;

        static TraceHelper()
        {
            traceContext = TraceContext.GetTraceContext(TraceHelper.ComponentName, "SmoMetadataProvider");
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
namespace Microsoft.SqlServer.Diagnostics.STrace
{
    using System;

    internal class MethodContext : IDisposable
    {
        public void Dispose()
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