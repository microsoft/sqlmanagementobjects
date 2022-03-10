// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.IO;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This static class provides utilities for all HadrModel
    /// </summary>
    public static class HadrModelUtilities
    {
        /// <summary>
        /// disk part Log File Suffix
        /// a change in this source of context would require testing. 
        /// Therefore, we don't allow the user to configure the values.
        /// </summary>
        public static string diskpartLogFileSuffix = "_diskpart.log";

        /// <summary>
        /// Alwayson String
        /// a change in this source of context would require testing. 
        /// Therefore, we don't allow the user to configure the values.
        /// </summary>
        public static string wizardAlwaysonStr = "AlwaysOn";

        /// <summary>
        /// used to replace characters that are illegal for file names with underscores
        /// </summary>
        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            string result = name;
            string illegalCharacters = "\\/:*?\"<>|";
            foreach (char c in illegalCharacters)
            {
                result = result.Replace(c, '_');
            }

            return result;
        }

        /// <summary>
        /// Returns a new SMO Server object everytime it's called
        /// </summary>
        /// <returns></returns>
        public static Smo.Server GetNewSmoServerObject(ServerConnection primaryConnection)
        {
            primaryConnection.Connect();
            primaryConnection.Disconnect();
            return new Smo.Server(primaryConnection);
        }

        public static string GetDatabaseBackupFileFullName(this AvailabilityGroupData agData, Smo.Server server, string databaseName)
        {
            return agData.GetBackupFileFullName(server, databaseName, ".bak");
        }

        public static string GetLogBackupFileFullName(this AvailabilityGroupData agData, Smo.Server server, string databaseName)
        {
            return agData.GetBackupFileFullName(server, databaseName, ".trn");
        }

        private static string GetBackupFileFullName(this AvailabilityGroupData agData, Smo.Server server, string databaseName, string fileExtension)
        {
            return PathWrapper.Combine(agData.GetBackupPathForServer(server), Path.ChangeExtension(SanitizeFileName(databaseName), fileExtension));
        }
    }
}
