// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
#if STRACE
using Microsoft.SqlServer.Management.Diagnostics;
#else
#endif

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// This API supports the product infrastructure and is not intended to be used directly from your code.
    /// Defines values used for tracing.
    /// </summary>
#if STRACE
    [CLSCompliant(false)]
#endif
    public struct SQLToolsCommonTraceLvl
    {
        public const uint L1 = 0x1; //common first level
        public const uint L2 = 0x2; //common second level
        public const uint L3 = 0x4; //common third level
        public const uint L4 = 0x8; //common fourth level
        public const uint Always = 0x10000000; //message that user always wants to see
        public const uint Warning = 0x20000000; //warning message - always goes to the log file with special preffix
        public const uint Error = 0x40000000; //error message - always goes to the log file with special preffix
    }

    /// <summary>
    /// This API supports the product infrastructure and is not intended to be used directly from your code.
    /// This class defines methods used for tracing.
    /// </summary>
#if STRACE
    [CLSCompliant(false)]
#endif
    public static class TraceHelper
    {
        private const uint TraceLevelAssert = 0x80000000;       // Assertion failed - always goes to the log file with special preffix
        private const uint TraceLevelException = 0x00200000;    // Exception is being thrown/was caught

        private static string AddDate(string str)
        {
            return String.Format("{0} - {1}", DateTime.Now.ToString("o"), str);
        }
        /// <summary>
        /// This API supports the product infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static void Trace(string strComponentName, string strFormat, params object[] args)
        {
#if STRACE
            STrace.Trace(strComponentName, AddDate(strFormat), args);
#else
            System.Diagnostics.Trace.TraceInformation(AddDate(strFormat), args);
#endif
        }

        /// <summary>
        /// This API supports the product infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static void Trace(string strComponentName, uint traceLevel, string strFormat, params object[] args)
        {
#if STRACE
            STrace.Trace(strComponentName, traceLevel, strFormat, args);
#else
            if ((SQLToolsCommonTraceLvl.Error == (traceLevel & SQLToolsCommonTraceLvl.Error)) ||
                (TraceLevelException == (traceLevel & TraceLevelException)))
            {
                // Error or exception trace levels
                System.Diagnostics.Trace.TraceError(AddDate(strFormat), args);
            }
            else if (TraceLevelAssert == (traceLevel & TraceLevelAssert))
            {
                // Assert trace level
                System.Diagnostics.Debug.Assert(false, strFormat);
            }
            else if (SQLToolsCommonTraceLvl.Warning == (traceLevel & SQLToolsCommonTraceLvl.Warning))
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

        /// <summary>
        /// This API supports the product infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static void Assert(bool condition)
        {
#if STRACE
            STrace.Assert(condition);
#else
            System.Diagnostics.Debug.Assert(condition);
#endif
        }

        /// <summary>
        /// This API supports the product infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static void Assert(bool condition, string strFormat)
        {
#if STRACE
            STrace.Assert(condition, strFormat);
#else
            System.Diagnostics.Debug.Assert(condition, strFormat);
#endif
        }

        /// <summary>
        /// This API supports the product infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public static void LogExCatch(System.Exception ex)
        {
#if STRACE
            STrace.LogExCatch(ex);
#else
            System.Diagnostics.Trace.TraceError(ex.Message);
#endif
        }
    }
}
