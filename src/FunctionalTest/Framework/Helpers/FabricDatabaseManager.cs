// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Fabric database types
    /// </summary>
    public enum FabricDatabaseType
    {
        /// <summary>
        /// Fabric SQL database
        /// </summary>
        SQLDatabase,
        
        /// <summary>
        /// Fabric data warehouse
        /// </summary>
        Warehouse
    }

    public class FabricDatabaseManager
    {
        // Path to the fabric-cli executable
        private static readonly string FabricCliPath = "fab.exe";
        // Default authentication method
        private static readonly string DefaultAuthMethod = ";Authentication=ActiveDirectoryDefault";
        // Cache LogIn Status
        private bool? CachedLogInStatus = null;
        private DateTime LogInStatusCacheExpirationTime;
        private static readonly TimeSpan CacheExpirationTime = TimeSpan.FromHours(1);

        /// <summary>
        /// Constructs a FabricDatabaseManager for the specified environment.
        /// </summary>
        /// <param name="environment"></param>
        public FabricDatabaseManager(string environment = "prod") => Environment = environment;

        /// <summary>
        /// Gets or sets the environment for the FabricDatabaseManager.
        /// </summary>
        public string Environment { get; }

        /// <summary>
        /// Creates a fabric database using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">The workspace Name where fabric database needs to be created.</param>
        /// <param name="dbName">The name of the database to create.</param>
        /// <returns>The connection string of the created fabric database.</returns>
        public string CreateDatabase(string workspaceName, string dbName)
        {
            return CreateFabricResource(workspaceName, dbName, FabricDatabaseType.SQLDatabase);
        }

        /// <summary>
        /// Creates a fabric warehouse using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">The workspace Name where fabric warehouse needs to be created.</param>
        /// <param name="warehouseName">The name of the warehouse to create.</param>
        /// <returns>The connection string of the created fabric warehouse.</returns>
        public string CreateWarehouse(string workspaceName, string warehouseName)
        {
            var baseStr = CreateFabricResource(workspaceName, warehouseName, FabricDatabaseType.Warehouse);
            return $"Data Source={baseStr};Initial Catalog={warehouseName}";
        }

        /// <summary>
        /// Drops a fabric database using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">Workspace Name</param>
        /// <param name="dbName">The name of the database to drop.</param>
        public void DropDatabase(string workspaceName, string dbName)
        {
            DropFabricResource(workspaceName, dbName, FabricDatabaseType.SQLDatabase);
        }

        /// <summary>
        /// Drops a fabric warehouse using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">Workspace Name</param>
        /// <param name="warehouseName">The name of the warehouse to drop.</param>
        public void DropWarehouse(string workspaceName, string warehouseName)
        {
            DropFabricResource(workspaceName, warehouseName, FabricDatabaseType.Warehouse);
        }

        /// <summary>
        /// Creates a fabric resource (database or warehouse) using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">The workspace Name where fabric resource needs to be created.</param>
        /// <param name="resourceName">The name of the resource to create.</param>
        /// <param name="resourceType">The type of fabric resource to create.</param>
        /// <returns>The connection string of the created fabric resource.</returns>
        private string CreateFabricResource(string workspaceName, string resourceName, FabricDatabaseType resourceType)
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                throw new ArgumentException("Workspace name cannot be null or empty.", nameof(workspaceName));
            }
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException("Resource name cannot be null or empty.", nameof(resourceName));
            }

            // Make sure we are logged in to Fabric CLI
            EnsureFabricCliLogin();
            bool created = false;
            try
            {
                string resourceTypeString = GetResourceTypeString(resourceType);
                Trace.TraceInformation($"Creating fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}' using fabric-cli.");
                var resourcePath = GetResourcePath(workspaceName, resourceName, resourceType);
                string output = ExecuteFabricCliCommand($"create {resourcePath}");
                created = true;
                Trace.TraceInformation($"Fabric {resourceTypeString.ToLowerInvariant()} created: {output}");
                // Get the connection string for the newly created resource
                string connectionString = ExecuteFabricCliCommand($"get {resourcePath} -q properties.connectionString")+DefaultAuthMethod;
                return connectionString;
            }
            catch (Exception ex)
            {
                var resourceTypeString = GetResourceTypeString(resourceType);
                Trace.TraceError($"Failed to create fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}': {ex.Message}");
                if (created)
                {
                    // If creation partially succeeded, attempt to clean up by dropping the resource
                    try
                    {
                        DropFabricResource(workspaceName, resourceName, resourceType);
                        Trace.TraceInformation($"Cleaned up partially created fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}'.");
                    }
                    catch (Exception cleanupEx)
                    {
                        Trace.TraceError($"Failed to clean up fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}': {cleanupEx.Message}");
                    }
                }
                throw new InvalidOperationException($"Error creating fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}'", ex);
            }
        }

        /// <summary>
        /// Drops a fabric resource (database or warehouse) using the fabric-cli tool.
        /// </summary>
        /// <param name="workspaceName">Workspace Name</param>
        /// <param name="resourceName">The name of the resource to drop.</param>
        /// <param name="resourceType">The type of fabric resource to drop.</param>
        private void DropFabricResource(string workspaceName, string resourceName, FabricDatabaseType resourceType)
        {
            if (string.IsNullOrWhiteSpace(workspaceName))
            {
                throw new ArgumentException("Workspace name cannot be null or empty.", nameof(workspaceName));
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException("Resource name cannot be null or empty.", nameof(resourceName));
            }

            // Make sure we are logged in to Fabric CLI
            EnsureFabricCliLogin();

            try
            {
                string resourceTypeString = GetResourceTypeString(resourceType);
                Trace.TraceInformation($"Dropping fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}' using fabric-cli.");
                var resourcePath = GetResourcePath(workspaceName, resourceName, resourceType);
                ExecuteFabricCliCommand($"rm {resourcePath} -f");
                Trace.TraceInformation($"Fabric {resourceTypeString.ToLowerInvariant()} dropped: {resourceName}");
            }
            catch (Exception ex)
            {
                string resourceTypeString = GetResourceTypeString(resourceType);
                Trace.TraceError($"Failed to drop fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}': {ex.Message}");
                throw new InvalidOperationException($"Error dropping fabric {resourceTypeString.ToLowerInvariant()} '{resourceName}'", ex);
            }
        }

        private void EnsureFabricCliLogin()
        {
            if(System.Environment.UserInteractive && !IsLoggedInToFabricCli())
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
        private bool IsLoggedInToFabricCli()
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
        private string ExecuteFabricCliCommand(string arguments, bool interactiveInputNeeded = false)
        {
            string output = string.Empty;
            var error = string.Empty;
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = FabricCliPath,
                Arguments = arguments,
                RedirectStandardOutput = !interactiveInputNeeded,
                RedirectStandardError = !interactiveInputNeeded,
                UseShellExecute = interactiveInputNeeded,
                CreateNoWindow = !interactiveInputNeeded,
            };
            // Set FAB_API_ENDPOINT_FABRIC if Environment == "daily"
            if (string.Equals(Environment, "daily", StringComparison.OrdinalIgnoreCase))
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.Arguments = $"/c set FAB_API_ENDPOINT_FABRIC=dailyapi.fabric.microsoft.com&{FabricCliPath} {arguments}";
            }
            Trace.WriteLine($"Executing Fabric CLI command: {processStartInfo.FileName} {processStartInfo.Arguments}");
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

        /// <summary>
        /// Gets the resource path for the specified resource type.
        /// </summary>
        /// <param name="workspaceName">The workspace name.</param>
        /// <param name="resourceName">The resource name.</param>
        /// <param name="resourceType">The type of fabric resource.</param>
        /// <returns>The formatted resource path.</returns>
        private string GetResourcePath(string workspaceName, string resourceName, FabricDatabaseType resourceType)
        {
            string resourceTypeString = GetResourceTypeString(resourceType);
            return $"/{workspaceName}.Workspace/{resourceName}.{resourceTypeString}";
        }

        /// <summary>
        /// Gets the string representation of the resource type for use in resource paths.
        /// </summary>
        /// <param name="resourceType">The fabric database type.</param>
        /// <returns>The string representation of the resource type.</returns>
        private string GetResourceTypeString(FabricDatabaseType resourceType)
        {
            return resourceType.ToString();
        }

        private string GetDatabasePath(string workspaceName, string dbName)
        {
            return GetResourcePath(workspaceName, dbName, FabricDatabaseType.SQLDatabase);
        }

    }
}
