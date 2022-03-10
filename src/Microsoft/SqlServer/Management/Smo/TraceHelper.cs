// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.SqlServer.Management.Diagnostics
{

#if !STRACE
    internal struct SQLToolsCommonTraceLvl
    {
        public const uint L1 = 0x1; //common first level
        public const uint L2 = 0x2; //common second level
        public const uint L3 = 0x4; //common third level
        public const uint L4 = 0x8; //common fourth level
        public const uint Always = 0x10000000; //message that user always wants to see
        public const uint Warning = 0x20000000; //warning message - always goes to the log file with special preffix
        public const uint Error = 0x40000000; //error message - always goes to the log file with special preffix
    }
#endif

    internal static class TraceHelper
    {
        private const uint TraceLevelAssert = 0x80000000;       // Assertion failed - always goes to the log file with special preffix
        private const uint TraceLevelException = 0x00200000;    // Exception is being thrown/was caught

        private static string AddDate(string str)
        {
            return String.Format("{0} - {1}", DateTime.Now.ToString("o"), str);
        }
        public static void Trace(string strComponentName, string strFormat, params object[] args)
        {
#if STRACE
            STrace.Trace(strComponentName, AddDate(strFormat), args);
#else
            System.Diagnostics.Trace.TraceInformation(AddDate(strFormat), args);
#endif
        }

        public static void Trace(string strComponentName, uint traceLevel, string strFormat, params object[] args)
        {
#if STRACE
            STrace.Trace(strComponentName, traceLevel, AddDate(strFormat), args);
#else
            if ((TraceLvl.Error == (traceLevel & TraceLvl.Error)) ||
                (TraceLevelException == (traceLevel & TraceLevelException)))
            {
                // Error or exception trace levels
                System.Diagnostics.Trace.TraceError(strFormat, args);
            }
            else if (TraceLevelAssert == (traceLevel & TraceLevelAssert))
            {
                // Assert trace level
               Debug.Assert(false, string.Format(strFormat, args));
            }
            else if (TraceLvl.Warning == (traceLevel & TraceLvl.Warning))
            {
                // Warning trace level
                System.Diagnostics.Trace.TraceWarning(AddDate(strFormat), args);
            }
            else
            {
                System.Diagnostics.Trace.TraceInformation(AddDate(strFormat), args);
            }
#endif
        }

        public static void Assert(bool condition)
        {
#if STRACE
            STrace.Assert(condition);
#else
            Debug.Assert(condition);
#endif
        }

        public static void Assert(bool condition, string strFormat)
        {
#if STRACE
            STrace.Assert(condition, strFormat);
#else
           Debug.Assert(condition, strFormat);
#endif
        }

        public static void Implies(bool a, bool b, string s)
        {
            if (a)
            {
                Assert(b, s);
            }
        }

        public static void LogExCatch(System.Exception ex)
        {
#if STRACE
            STrace.LogExCatch(ex);
#else
            System.Diagnostics.Trace.TraceError(ex.Message);
#endif
        }
    }

    internal static class TraceLvl
    {
        public const uint L1 = 1u;
        public const uint L2 = 2u;
        public const uint L3 = 4u;
        public const uint L4 = 8u;
        public const uint Always = 268435456u;
        public const uint Warning = 536870912u;
        public const uint Error = 1073741824u;
    }
}

#if !STRACE
namespace Microsoft.SqlServer.Diagnostics.STrace
{
    internal class STraceConfigurationAttribute : Attribute
    {
        public STraceConfigurationAttribute()
        { }

        public bool SkipAutoTrace { get; set; }
    }
    
    internal class TraceContext : IDisposable
    {
        public static TraceContext GetTraceContext(string componentName, string s)
        {
            return new TraceContext();
        }

        public ActivityContext GetActivityContext(string s)
        {
            return new ActivityContext(s);
        }

        public MethodTraceContext GetMethodContext(string s)
        {
            return new MethodTraceContext(s);
        }

        public MethodTraceContext GetMethodContext(string methodName, TraceEventType traceEventType)
        {
            return new MethodTraceContext(methodName);
        }

        public void Assert(bool condition, string message)
        {
            System.Diagnostics.Debug.Assert(condition, message);
        }

        public void Dispose()
        { }

        public void TraceActivityStart(string s)
        {
            System.Diagnostics.Trace.TraceInformation("TraceActivityStart - {0}", s);
        }

        public void TraceActivityEnd(string s)
        {
            System.Diagnostics.Trace.TraceInformation("TraceActivityEnd - {0}", s);
        }

        public void TraceCatch(Exception ex)
        {
            System.Diagnostics.Trace.TraceError("TraceCatch - {0}", ex);
        }

        public void TraceInformation(string format, params object[] info)
        {
            System.Diagnostics.Trace.TraceInformation("TraceInformation - ", string.Format(format, info));
        }

        public Exception TraceThrow(Exception exception)
        {
            System.Diagnostics.Trace.TraceInformation("TraceThrow - {0}", exception);
            return exception;
        }

        public Exception TraceThrow(Exception exception, TraceEventType traceEventType)
        {
            System.Diagnostics.Trace.TraceInformation("TraceThrow - {0}:{1}", exception, traceEventType);
            return exception;
        }

        public void TraceVerbose(string message)
        {
            System.Diagnostics.Trace.TraceInformation("TraceVerbose - {0}", message);
        }

        public void TraceVerbose(int id, string message)
        {
            System.Diagnostics.Trace.TraceInformation("TraceVerbose - {0}:{1}", id, message);
        }

        public void TraceVerbose(string format, params object[] args)
        {
            System.Diagnostics.Trace.TraceInformation("TraceVerbose - " + string.Format(format, args));
        }

        public void TraceVerbose(int id, string format, params object[] args)
        {
            System.Diagnostics.Trace.TraceInformation("TraceVerbose - {0}" + string.Format(format, args), id);
        }

        public virtual void TraceParameterIn(string parameterName, object parameterValue)
        {
            Trace.TraceInformation("Parameter in:{0}:{1}", parameterName, parameterValue);
        }

        public virtual void TraceParameters(params object[] parameterValues)
        {
            System.Diagnostics.Trace.TraceInformation(string.Format("TraceParameters - {0}", string.Join(",", parameterValues.Select(traceValue => traceValue ?? "").ToArray())));
        }

        protected void TraceParameters(TraceEventType traceEventType, params object[] parameterValues)
        {
            System.Diagnostics.Trace.TraceInformation(string.Format("TraceParameters - {0}:{1}", traceEventType, string.Join(",", parameterValues.Select(traceValue => traceValue ?? "").ToArray())));
        }

        public void TraceMethodEnter(string methodName)
        {
            System.Diagnostics.Trace.TraceInformation("MethodEnter - {0}", methodName);
        }

        public void TraceMethodExit(string methodName)
        {
            System.Diagnostics.Trace.TraceInformation("MethodExit - {0}", methodName);
        }

        public void DebugAssert(bool test)
        {
            System.Diagnostics.Debug.Assert(test);
        }

        public void DebugAssert(bool test, string message)
        {
            System.Diagnostics.Debug.Assert(test, message);
        }

        public void DebugAssert(bool test, int id, string message)
        {
            System.Diagnostics.Debug.Assert(test, Convert.ToString(id), message);
        }

        public void DebugAssert(bool test, string format, params object[] args)
        {
            System.Diagnostics.Debug.Assert(test, string.Format(format, args));
        }

        public void DebugAssert(bool test, int id, string format, params object[] args)
        {
            System.Diagnostics.Debug.Assert(test, Convert.ToString(id), string.Format(format, args));
        }

        public void TraceError(string message)
        {
            System.Diagnostics.Trace.TraceError("TraceError - {0}", message);
        }

        public void TraceError(int id, string message)
        {
            System.Diagnostics.Trace.TraceError("TraceError - {0}:{1}", id, message);
        }

        public void TraceError(string format, params object[] args)
        {
            System.Diagnostics.Trace.TraceError("TraceError - " + string.Format(format, args));
        }

        public void TraceError(int id, string format, params object[] args)
        {
            System.Diagnostics.Trace.TraceError("TraceError - {0}" + string.Format(format, args), id);
        }

        public virtual void TraceParameterOut(string parameterName, object parameterValue)
        {
            System.Diagnostics.Trace.TraceInformation("TraceParameterOut - {0}:{1}", parameterName, parameterValue);
        }
    }

    internal class ActivityContext : IDisposable
    {
        public ActivityContext(string parameterName)
        { }

        public void Dispose()
        { }
    }

    internal class MethodContext : IDisposable
    {
        public void Dispose()
        { }

        public ActivityContext GetActivityContext(string s)
        {
            return new ActivityContext(s);
        }

        public void TraceCatch(Exception ex)
        {
            System.Diagnostics.Trace.TraceError("TraceCatch - {0}", ex);
        }
    }

    internal class MethodTraceContext : TraceContext
    {
        public MethodTraceContext(string parameterName)
        { }

    }
}
#endif