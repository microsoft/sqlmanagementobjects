// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This class checks a registry key and adds prefix and postfix statements to the current query
    /// to set and restore isolation level if specified by the registry key
    /// </summary>
    internal static class QueryIsolation
    {

        internal const string RegPathFormat = @"Software\Microsoft\Microsoft SQL Server\SMO\QueryIsolation\{0}";

        private const string PrefixValue = "Prefix";
        private const string PostfixValue = "Postfix";
        private const string IsolationFormat = @"SET TRANSACTION ISOLATION LEVEL {0};";        
        private static string[] IsolationLevels = new[]
        {"read uncommitted", "read committed", "serializable", "snapshot", "repeatable read"};

        internal static string cachedPrefix = null;
        internal static string cachedPostfix = null;

        /// <summary>
        /// the static constructor gives us atomic initialization.
        /// </summary>
        static QueryIsolation()
        {
            InitIfNeeded();
        }

        private static void InitIfNeeded()
        {
            if (cachedPrefix == null)
            {
                cachedPrefix = GetIsolationLevel(PrefixValue);
                cachedPostfix = GetIsolationLevel(PostfixValue);
            }
        }

        private static string GetIsolationLevel(string regValue)
        {
            string isolationValue = string.Empty;
            try
            {
#if !NETSTANDARD2_0 && !NETCOREAPP
                // 2016-08-19 sgreen: Address for VSTS 8239489 - Functional parity between SMO on .NET Core and SMO on .NET Framework
                using (var isolationKey = Registry.CurrentUser.OpenSubKey(string.Format(RegPathFormat, RegKeyName),  writable: false))
                {
                    isolationValue = isolationKey != null ? isolationKey.GetValue(regValue, string.Empty).ToString() : string.Empty;
                }
#endif
            }
            catch (Exception)
            {
            }
            return IsolationLevels.Contains(isolationValue.ToLowerInvariant()) ? string.Format(IsolationFormat, isolationValue) : String.Empty;
        }

        private static string RegKeyName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
            }
        }

        /// <summary>
        /// Returns a script to run before running the main query, or string.empty if no query is needed
        /// </summary>
        /// <returns></returns>
        public static string GetQueryPrefix()
        {
            InitIfNeeded();
            
            if (string.IsNullOrEmpty(cachedPrefix) || string.IsNullOrEmpty(cachedPostfix))
            {
                return String.Empty;                
            }
            return cachedPrefix;
        }

        /// <summary>
        /// Returns a script to run after running the main query, or string.empty if no query is needed
        /// </summary>
        /// <returns></returns>
        public static string GetQueryPostfix()
        {
            InitIfNeeded();
            if (string.IsNullOrEmpty(cachedPostfix) || string.IsNullOrEmpty(cachedPrefix))
            {
                return String.Empty;
            }
            return cachedPostfix;
        }
    }
}
