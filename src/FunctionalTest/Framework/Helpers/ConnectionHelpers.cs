// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Linq;
using System.Reflection;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Xml.XPath;
using Microsoft.SqlServer.ADO.Identity;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods and vars for getting connections used for tests
    /// </summary>
    public static class ConnectionHelpers
    {
#if MICROSOFTDATA
        static ConnectionHelpers()
        {
            _ = SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryServicePrincipal, new AzureDevOpsSqlAuthenticationProvider());
            _ = SqlAuthenticationProvider.SetProvider(SqlAuthenticationMethod.ActiveDirectoryDefault, new AzureDevOpsSqlAuthenticationProvider());
        }
#endif
        private class ConnectionData
        {
            public AzureKeyVaultHelper AzureKeyVaultHelper;
            public readonly IDictionary<string, Tuple<SMO.Server, TestServerDescriptor>> ServerDescriptors = new Dictionary<string, Tuple<SMO.Server, TestServerDescriptor>>();
        }
        // ThreadStatic enables us to run tests in parallel
        [ThreadStatic]
        private static ConnectionData _serverConnections;

        // Fabric workspaces
        [ThreadStatic]
        private static List<FabricWorkspaceDescriptor> _fabricWorkspaces;

        private static ConnectionData ServerConnections =>
            _serverConnections ?? (_serverConnections = LoadConnStrings());

        /// <summary>
        /// Reads the connection strings defined in the appropriate JSON or XML file
        /// and puts them into a dictionary for easy access
        /// </summary>
        /// <returns></returns>
        private static ConnectionData LoadConnStrings()
        {
            var serverConnections = new ConnectionData();
            IEnumerable<TestDescriptor> testServerdescriptors = JsonTestServerSource.TryLoadServerConnections();
            if (!testServerdescriptors.Any())
            {
                var connectionDocument = LoadConnectionDocument();
                var akvElement = connectionDocument.XPathSelectElement(@"//AkvAccess");
                if (akvElement != null)
                {
                    serverConnections.AzureKeyVaultHelper = new AzureKeyVaultHelper(akvElement.Element("VaultName")?.Value ?? "");
                    serverConnections.AzureKeyVaultHelper.AzureApplicationId = akvElement.Element("AzureApplicationId")?.Value ?? serverConnections.AzureKeyVaultHelper.AzureApplicationId;
                    serverConnections.AzureKeyVaultHelper.AzureTenantId = akvElement.Element("AzureTenantId")?.Value ?? serverConnections.AzureKeyVaultHelper.AzureTenantId;
                    var storageElement = akvElement.Element("AzureStorage");
                    if (storageElement != null )
                    {
                        serverConnections.AzureKeyVaultHelper.StorageHelper = new AzureStorageHelper(storageElement.Value,
                            serverConnections.AzureKeyVaultHelper);
                    }
                }
                testServerdescriptors = TestServerDescriptor.GetTestDescriptors(connectionDocument, serverConnections.AzureKeyVaultHelper);
            }
            foreach (var descriptor in testServerdescriptors)
            {
                if (descriptor is FabricWorkspaceDescriptor workspaceDescriptor)
                {
                    if (_fabricWorkspaces == null)
                    {
                        _fabricWorkspaces = new List<FabricWorkspaceDescriptor>();
                    }
                    _fabricWorkspaces.Add(workspaceDescriptor);
                    continue;
                }
                else if (descriptor is TestServerDescriptor testServerDescriptor)
                {
                    //SqlTestTargetServersFilter env variable was empty/didn't exist or it contained this server, add to our overall list
                    var svr = new SMO.Server(new ServerConnection(new SqlConnection(testServerDescriptor.ConnectionString)));
                    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(testServerDescriptor.ConnectionString);
                    TraceHelper.TraceInformation("Loaded connection string '{0}' = '{1}'{2}",
                         descriptor.Name,
                         sqlConnectionStringBuilder.DataSource,
                     testServerDescriptor.BackupConnectionStrings.Any() ?
                         "Backups = " + testServerDescriptor.BackupConnectionStrings :
                         string.Empty);
                    // If the connectionString has database information, we need to reuse the database
                    testServerDescriptor.ReuseExistingDatabase = !string.IsNullOrEmpty(sqlConnectionStringBuilder.InitialCatalog) && sqlConnectionStringBuilder.InitialCatalog != "master";
                    serverConnections.ServerDescriptors.Add(testServerDescriptor.Name, new Tuple<SMO.Server, TestServerDescriptor>(svr, testServerDescriptor));
                }
            }

            return serverConnections;
        }

        private static XDocument LoadConnectionDocument()
        {
            //Load up the connection string values from the embedded resource
            using (var connStringsStream = GetConnectionXml())
            {
                return XDocument.Load(XmlReader.Create(connStringsStream));
            }
        }

        private static FileStream GetConnectionXml()
        {
            
            var defaultConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
            var privateConfigPath = Environment.GetEnvironmentVariable("TestPath", EnvironmentVariableTarget.Process) ??
                    Environment.GetEnvironmentVariable("TestPath", EnvironmentVariableTarget.User) ??
                    Environment.GetEnvironmentVariable("TestPath", EnvironmentVariableTarget.Machine) ??
                    defaultConfigPath;

            // If the path is a file, use it directly
            var privateXmlPath = privateConfigPath.EndsWith(".xml", StringComparison.OrdinalIgnoreCase)
                ? File.Exists(privateConfigPath) ? privateConfigPath : Path.Combine(defaultConfigPath, privateConfigPath)
                : Path.Combine(privateConfigPath, "ToolsConnectionInfo.xml");

            if (File.Exists(privateXmlPath))
            {
                TraceHelper.TraceInformation("Using private connection data from {0}", privateXmlPath);
                return File.OpenRead(privateXmlPath);
            }
            throw new InvalidOperationException($"No file found at {privateXmlPath}");
        }

        /// <summary>
        /// Returns a list of SqlConnectionStringBuilders for servers from ConnectionInfo.xml
        /// that pass all the SqlSupportedDimensionAttribute criteria specified on the Method
        /// passed in. 
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="filter">An optional filter that accepts the server friendly name and returns true if it should be included in the search</param>
        /// <returns></returns>
        public static IList<ServerConnectionInfo> GetServerConnections(MethodInfo mi, Func<string, bool> filter = null)
        {
            var requiredFeatureAttributes =
                mi.GetCustomAttributes<SqlRequiredFeatureAttribute>(true)
                    .Concat(mi.DeclaringType.GetCustomAttributes<SqlRequiredFeatureAttribute>()).ToArray();

            var requiredFeatures = requiredFeatureAttributes.SelectMany(feature => feature.RequiredFeatures).Distinct().ToArray();

            var serverConnections = new List<ServerConnectionInfo>();
            //We need to check each of the defined servers to see if they're flagged
            foreach (KeyValuePair<string, Tuple<SMO.Server, TestServerDescriptor>> serverConnectionPair in ServerConnections.ServerDescriptors.Where(kvp => filter?.Invoke(kvp.Key) ?? true))
            {
                // Make sure the required features for the test are enabled on the server
                if (requiredFeatures.Any(feature => !serverConnectionPair.Value.Item2.EnabledFeatures.Contains(feature)))
                {
                    continue;
                }

                // Make sure the test requires at least a feature the server is reserved for
                if (serverConnectionPair.Value.Item2.ReservedFor.Any() && !serverConnectionPair.Value.Item2.ReservedFor.Intersect(requiredFeatures).Any())
                {
                    continue;
                }

                var exceptions = new List<Exception>();

                bool isSupported = EvaluateSupportedDimensions(
                    mi,
                    serverConnectionPair.Value.Item1, // SMO.Server
                    serverConnectionPair.Key,        // Server friendly name
                    exceptions,
                    (attribute, server, name) =>
                        attribute.IsSupported(server,
                        serverConnectionPair.Value.Item2, // TestServerDescriptor
                        name)
                );

                if (isSupported)
                {
                    //For SqlUnsupportedDimensionAttributes we consider the server supported only
                    //if ALL of the unsupported attributes return IsSupported = true

                    //Note we look at attributes on both the method and the class the method is declared in
                    isSupported &= EvaluateUnsupportedDimensions(
                            mi,
                            serverConnectionPair.Value.Item1, // SMO.Server
                            serverConnectionPair.Key,        // Server friendly name
                            exceptions,
                            (attribute, server, name) => 
                                attribute.IsSupported(server, 
                                serverConnectionPair.Value.Item2, // TestServerDescriptor
                                name)
                        );
                    if (isSupported)
                    {
                        if (exceptions.Any())
                        {
                            // As far as we know we should have added this server, but exceptions occured during processing of the
                            // Supported/Unsupported attributes so we're in an unknown state - rethrow those exceptions here since 
                            // we want to err on the side of assuming that this server was supposed to be included but something went wrong
                            throw new AggregateException(
                                "Exceptions thrown when determining Supported/Unsupported status for server " + serverConnectionPair.Key, exceptions);
                        }
                        //Create a copy of the builder so clients can modify it as they wish without affecting other tests
                        serverConnections.Add(new ServerConnectionInfo
                        {
                            FriendlyName = serverConnectionPair.Key,
                            ConnectionStrings = serverConnectionPair.Value.Item2.AllConnectionStrings.Select(
                               connString => new SqlConnectionStringBuilder(connString)),
                            TestDescriptor = serverConnectionPair.Value.Item2,
                        });
                    }
                }
            }

            // Process Fabric workspaces
            if (_fabricWorkspaces != null && _fabricWorkspaces.Any())
            {
                ProcessFabricWorkspaces(mi, requiredFeatures, filter)
                    .ForEach(fabricConnection => serverConnections.Add(fabricConnection));
            }

            return serverConnections;
        }

        /// <summary>
        ///  Returns the default database edition to use for the given friendly name
        /// </summary>
        /// <param name="targetServerFriendlyName"></param>
        /// <returns></returns>
        public static DatabaseEngineEdition GetDefaultEdition(string targetServerFriendlyName)
        {
            return ServerConnections.ServerDescriptors.ContainsKey(targetServerFriendlyName)
                ? ServerConnections.ServerDescriptors[targetServerFriendlyName].Item2.DatabaseEngineEdition
                : DatabaseEngineEdition.Unknown;
        }

        /// <summary>
        /// Returns connection strings for TestServerDescriptors that match the filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetMatchingConnections(Func<TestServerDescriptor, bool> filter)
        {
            return ServerConnections.ServerDescriptors.Where(d => filter(d.Value.Item2)).Select(d => d.Value.Item2.ConnectionString);
        }

        /// <summary>
        /// Returns an AzureKeyVaultHelper defined by the AkvAccess element of ToolsConnectionInfo.xml
        /// </summary>
        /// <returns></returns>
        public static AzureKeyVaultHelper GetAzureKeyVaultHelper()
        {
            return ServerConnections.AzureKeyVaultHelper;
        }

        private static bool EvaluateSupportedDimensions<T>(
            MethodInfo mi,
            T target,
            string targetName,
            IList<Exception> exceptions,
            Func<SqlSupportedDimensionAttribute, T, string, bool> evaluateSupportedDimension)
        {
            //For SqlSupportedDimensionAttributes we consider the server supported if
            //ANY of the attributes return IsSupported is true
            var supportedDimensions =
                    mi.GetCustomAttributes<SqlSupportedDimensionAttribute>(true)
                    .Concat(mi.DeclaringType.GetCustomAttributes<SqlSupportedDimensionAttribute>()).ToArray();
            //If we don't have any SupportedDimensionAttributes we default to it being supported for all servers

            bool isSupported = supportedDimensions.Length == 0 ||
                supportedDimensions.Any(a =>
                {
                    try
                    {
                        return evaluateSupportedDimension(
                            a,
                            target,
                            targetName);
                    }
                    catch (Exception e)
                    {
                        // Something went wrong, continue on for now to see if any of the UnsupportedAttributes exclude
                        // this server from even being included (in which case we'll just ignore the error anyways). If
                        // we DON'T exclude the server though we'll rethrow the error further down so the test still fails
                        // since we can't tell if the server is actually supported.
                        exceptions.Add(e);
                        return true;
                    }
                });
            return isSupported;
        }
        private static bool EvaluateUnsupportedDimensions<T>(
            MethodInfo mi,
            T target,
            string targetName,
            IList<Exception> exceptions,
            Func<SqlUnsupportedDimensionAttribute, T, string, bool> evaluateUnsupportedDimension)
        {
            return mi.GetCustomAttributes<SqlUnsupportedDimensionAttribute>(true)
                .Concat(mi.DeclaringType.GetCustomAttributes<SqlUnsupportedDimensionAttribute>())
                .Aggregate(true, (current, unsupportedDimensionAttribute) =>
                {
                    try
                    {
                        return current & evaluateUnsupportedDimension(unsupportedDimensionAttribute, target, targetName);
                    }
                    catch (Exception e)
                    {
                        // Something went wrong, continue on for now to see if any of the other UnsupportedAttributes exclude
                        // this server from even being included (in which case we'll just ignore the error anyways). If
                        // we DON'T exclude the server though we'll rethrow the error further down so the test still fails
                        // since we can't tell if the server is actually supported.
                        exceptions.Add(e);
                        return current;
                    }
                });
        }
        private static List<ServerConnectionInfo> ProcessFabricWorkspaces(MethodInfo mi, SqlFeature[] requiredFeatures, Func<string, bool> filter = null)
        {
            var fabricConnections = new List<ServerConnectionInfo>();
            foreach (var workspace in _fabricWorkspaces.Where(workspace => filter?.Invoke(workspace.Name) ?? true))
            {
                // Make sure the test needs fabric workspace
                if (requiredFeatures.Any(feature => !workspace.EnabledFeatures.Contains(feature)))
                {
                    continue;
                }

                // Make sure the test requires at least a feature the workspace is reserved for
                if (workspace.ReservedFor.Any() && !workspace.ReservedFor.Intersect(requiredFeatures).Any())
                {
                    continue;
                }

                // Check if the workspace is unsupported based on SqlUnsupportedDimensionAttribute
                var exceptions = new List<Exception>();
                bool isWorkspaceSupported = EvaluateSupportedDimensions(
                    mi,
                    workspace,
                    workspace.Name,
                    exceptions,
                    (attribute, targetWorkspace, name) => attribute.IsSupported(targetWorkspace, name)
                );


                if(isWorkspaceSupported)
                {
                    isWorkspaceSupported &= EvaluateUnsupportedDimensions(
                        mi,
                        workspace,
                        workspace.Name,
                        exceptions,
                        (attribute, targetWorkspace, name) => attribute.IsSupported(targetWorkspace, name)
                    );
                }
                
                if (!isWorkspaceSupported)
                {
                    // Skip this workspace if it is unsupported
                    continue;
                }

                if (exceptions.Any())
                {
                    // If exceptions occurred during evaluation, rethrow them
                    throw new AggregateException(
                        $"Exceptions thrown when determining Supported/Unsupported status for Fabric workspace {workspace.Name}",
                        exceptions);
                }
                try
                {
                    fabricConnections.Add(new ServerConnectionInfo
                    {
                        FriendlyName = workspace.Name,
                        IsFabricWorkspace = true,
                        TestDescriptor = workspace,
                    });
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to process Fabric workspace {workspace.WorkspaceName}: {ex.Message}");
                }
            }
            return fabricConnections;
        }
    }
}
