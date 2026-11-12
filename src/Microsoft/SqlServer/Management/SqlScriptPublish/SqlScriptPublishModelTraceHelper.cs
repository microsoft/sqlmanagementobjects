// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


namespace Microsoft.SqlServer.Management.Diagnostics
{
    internal static class SqlScriptPublishModelTraceHelper
    {
        private const uint TraceLevelAssert = 0x80000000;       // Assertion failed - always goes to the log file with special prefix
        private const uint TraceLevelException = 0x00200000;    // Exception is being thrown/was caught

        public static void SetDefaultLevel(string strComponentName, uint level)
        {
#if STRACE
            STrace.SetDefaultLevel(strComponentName, level);            
#endif
        }


        public static void Assert(bool condition)
        {
#if STRACE
            STrace.Assert(condition);
#else
            System.Diagnostics.Debug.Assert(condition);
#endif
        }

        public static void Assert(bool condition, string strFormat)
        {
#if STRACE
            STrace.Assert(condition, strFormat);
#else
            System.Diagnostics.Debug.Assert(condition, strFormat);
#endif
        }

    }
}