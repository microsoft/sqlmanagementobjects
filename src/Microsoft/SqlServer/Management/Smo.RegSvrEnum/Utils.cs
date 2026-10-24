//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------
using System;
using System.IO;

namespace Microsoft.SqlServer.Management.Smo.RegSvrEnum
{
    /// <summary>
    /// Various utility functions
    /// </summary>
    internal class Utils
    {
        // Suffix that we add to the %APPDATA% variable to construct location
        // where we're persisting SQL Replication tools settings.
        // SqlMonitor.exe, for example, uses this file.
        private const string yukonSavePathSuffixFormat = @"Microsoft\Microsoft SQL Server\{0}\Tools\Shell";

        /// <summary>
        /// Get the full path to the directory with the SqlRepl regsvr file (typically, RegReplSrvr.xml)
        /// </summary>
        /// <param name="version">The version of the SqlRepl being used. Typically, something like "150", etc...</param>
        /// <returns>The full path to the directory with the SqlRepl regsvr file </returns>
        public static string GetYukonSettingsDirName(string version) =>
            Path.Combine(
                Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                string.Format(yukonSavePathSuffixFormat, version));

        /// <summary>
        /// Make sure that the directory where SqlRepl regsvr persists its settings exists
        /// </summary>
        /// <param name="version">The version of the SqlRepl being used. Typically, something like "150", etc...</param>
        public static void EnsureSettingsDirExists(string version)
        {
            // Note: CreateDirectory() is a no-op if the directory already exists.
            Directory.CreateDirectory(GetYukonSettingsDirName(version));
        }
    }
}
