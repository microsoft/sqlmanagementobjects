// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    public static class FabricDatabaseManager
    {
        // Path to the fabric-cli executable
        private static readonly string FabricCliPath = "fab.exe";
        // Default authentication method
        private static readonly string DefaultAuthMethod = ";Authentication=ActiveDirectoryDefault";
        // Cache LogIn Status
        private static bool? CachedLogInStatus = null;
        private static DateTime LogInStatusCacheExpirationTime;
        private static TimeSpan CacheExpirationTime = TimeSpan.FromHours(1);

        /// <summary>
        /// Creates a fabric database using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">The workspace Name where fabric database needs to be created.</param>
        /// <param name="dbName">The name of the database to create.</param>
        /// <returns>The connection string of the created fabric database.</returns>
        public static string CreateDatabase(string workspaceName, string dbName)
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                throw new ArgumentException("Workspace name cannot be null or empty.", nameof(workspaceName));
            }
            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new ArgumentException("Database name cannot be null or empty.", nameof(dbName));
            }

            // Make sure we are logged in to Fabric CLI
            EnsureFabricCliLogin();

            try
            {
                Trace.TraceInformation($"Creating fabric database '{dbName}' using fabric-cli.");
                var dbPath = GetDatabasePath(workspaceName, dbName);
                string output = ExecuteFabricCliCommand($"create {dbPath}");
                Trace.TraceInformation($"Fabric database created: {output}");
                // Get the connection string for the newly created database
                string connectionString = ExecuteFabricCliCommand($"get {dbPath} -q properties.connectionString")+DefaultAuthMethod;
                return connectionString;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to create fabric database '{dbName}': {ex.Message}");
                throw new InvalidOperationException($"Error creating fabric database '{dbName}'", ex);
            }
        }

        /// <summary>
        /// Drops a fabric database using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">Workspace Name</param>
        /// <param name="dbName">The name of the database to drop.</param>
        public static void DropDatabase(string workspaceName, string dbName)
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                throw new ArgumentException("Workspace name cannot be null or empty.", nameof(workspaceName));
            }

            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new ArgumentException("Database name cannot be null or empty.", nameof(dbName));
            }

            // Make sure we are logged in to Fabric CLI
            EnsureFabricCliLogin();

            try
            {
                Trace.TraceInformation($"Dropping fabric database '{dbName}' using fabric-cli.");
                var dbPath = $"/{workspaceName}.Workspace/{dbName}.SQLDatabase";
                ExecuteFabricCliCommand($"rm {dbPath} -f");
                Trace.TraceInformation($"Fabric database dropped: {dbName}");
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to drop fabric database '{dbName}': {ex.Message}");
                throw new InvalidOperationException($"Error dropping fabric database '{dbName}'", ex);
            }
        }

        private static void EnsureFabricCliLogin()
        {
            if(Environment.UserInteractive && !IsLoggedInToFabricCli())
            {
                ExecuteFabricCliCommand("auth login", true);
                // After successful login, update the cached status to true
                CachedLogInStatus = true;
                LogInStatusCacheExpirationTime = DateTime.UtcNow.Add(CacheExpirationTime);
                Trace.TraceInformation("Logged in to Fabric CLI Interactively.");
            }
            else
            {
                Trace.TraceInformation("Already logged in to Fabric CLI.");
            }
        }
        private static bool IsLoggedInToFabricCli()
        {
            // Check if the login status is cached and not expired
            if (CachedLogInStatus.HasValue && DateTime.UtcNow < LogInStatusCacheExpirationTime)
            {
                return CachedLogInStatus.Value;
            }
            // If Cached status is null or expired, check the login status
            try
            {
                ExecuteFabricCliCommand("auth status", true);
                CachedLogInStatus = true;
            }
            catch (Exception ex)
            {
                // When not logged in, the command throws an exception - x [AuthenticationFailed] Failed to get access token
                Trace.TraceWarning($"Fabric CLI auth status: {ex.Message}");
                return false;
            }

            // Set the cache expiration time to 1 hour from now
            LogInStatusCacheExpirationTime = DateTime.UtcNow.Add(CacheExpirationTime);
            return CachedLogInStatus.Value;
        }

        /// <summary>
        /// Executes Fabric-Cli Command and returns the output.
        /// </summary>
        private static string ExecuteFabricCliCommand(string arguments, bool interactiveInputNeeded = false)
        {
            string output = string.Empty;
            string error = string.Empty;
            
                var processStartInfo = new ProcessStartInfo
            {
                FileName = FabricCliPath,
                Arguments = arguments,
                RedirectStandardOutput = !interactiveInputNeeded,
                RedirectStandardError = !interactiveInputNeeded,
                UseShellExecute = interactiveInputNeeded,
                CreateNoWindow = !interactiveInputNeeded,
            };
            using (var process = Process.Start(processStartInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start Fabric CLI");
                }

                if (!interactiveInputNeeded)
                {
                    output = process.StandardOutput.ReadToEnd();
                    error = process.StandardError.ReadToEnd();
                }
               
                process.WaitForExit();
                
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Fabric CLI command {processStartInfo.Arguments} failed with exit code {process.ExitCode}. Details: {output} {error}");
                }
                return output?.Trim();
            }
        }

        private static string GetDatabasePath(string workspaceName, string dbName)
        {
            return $"/{workspaceName}.Workspace/{dbName}.SQLDatabase";
        }

    }
}
