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

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods and vars for getting connections used for tests
    /// </summary>
    public static class ConnectionHelpers
    {
        private class ConnectionData
        {
            public AzureKeyVaultHelper AzureKeyVaultHelper;
            public readonly IDictionary<string, Tuple<SMO.Server, TestServerDescriptor>> ServerDescriptors = new Dictionary<string, Tuple<SMO.Server, TestServerDescriptor>>();
        }
        // ThreadStatic enables us to run tests in parallel
        [ThreadStatic]
        private static ConnectionData _serverConnections;

        private static ConnectionData ServerConnections =>
            _serverConnections ?? (_serverConnections = LoadConnStrings());

        /// <summary>
        /// Reads the connection strings defined in the embedded ConnectionStrings.xml resource
        /// and puts them into a dictionary for easy access
        /// </summary>
        /// <returns></returns>
        private static ConnectionData LoadConnStrings()
        {
            var serverConnections = new ConnectionData();
            var connectionDocument = LoadConnectionDocument();
            var akvElement = connectionDocument.XPathSelectElement(@"//AkvAccess");
            if (akvElement != null && akvElement.Element("VaultName") != null)
            {
                serverConnections.AzureKeyVaultHelper = new AzureKeyVaultHelper(akvElement.Element("VaultName").Value)
                {
                    AzureApplicationId = akvElement.Element("AzureApplicationId")?.Value,
                    AzureTenantId = akvElement.Element("AzureTenantId")?.Value,
                    CertificateThumbprints = akvElement.Elements("Thumbprint").Select(s => s.Value).ToArray()
                };
            }
            foreach (var descriptor in TestServerDescriptor.GetServerDescriptors(connectionDocument, serverConnections.AzureKeyVaultHelper))
            {
                //SqlTestTargetServersFilter env variable was empty/didn't exist or it contained this server, add to our overall list
                var svr = new SMO.Server(new ServerConnection(new SqlConnection(descriptor.ConnectionString)));
               TraceHelper.TraceInformation("Loaded connection string '{0}' = '{1}'{2}", 
                    descriptor.Name, 
                    new SqlConnectionStringBuilder(descriptor.ConnectionString).DataSource,
                descriptor.BackupConnnectionStrings.Any() ? 
                    "Backups = " + descriptor.BackupConnnectionStrings :
                    string.Empty);
                serverConnections.ServerDescriptors.Add(descriptor.Name, new Tuple<SMO.Server, TestServerDescriptor>(svr, descriptor));
            }

            return serverConnections;
        }

        private static XDocument LoadConnectionDocument()
        {
            //Load up the connection string values from the embedded resource
            using (Stream connStringsStream = GetConnectionXml())
            {
                return XDocument.Load(XmlReader.Create(connStringsStream));
            }
        }

        private static Stream GetConnectionXml()
        {
            var privateConfigPath =
                    Environment.GetEnvironmentVariable("TestPath", EnvironmentVariableTarget.Process) ??
                    Environment.GetEnvironmentVariable("TestPath", EnvironmentVariableTarget.User) ??
                    Environment.GetEnvironmentVariable("TestPath", EnvironmentVariableTarget.Machine) ??
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? Environment.CurrentDirectory;
            Trace.TraceInformation($"Using '{privateConfigPath}' to look for ToolsConnectionInfo.xml");
            var privateXmlPath = Path.Combine(privateConfigPath, "ToolsConnectionInfo.xml");
            if (File.Exists(privateXmlPath))
            {
                TraceHelper.TraceInformation("Using private connection data from {0}", privateXmlPath);
                return File.OpenRead(privateXmlPath);
            }
            throw new InvalidOperationException("No ToolsConnectionInfo.xml file found");
        }

        /// <summary>
        /// Returns a list of SqlConnectionStringBuilders for servers from ConnectionInfo.xml
        /// that pass all the SqlSupportedDimensionAttribute criteria specified on the Method
        /// passed in. 
        /// </summary>
        /// <param name="mi"></param>
        /// <param name="filter">An optional filter that accepts the server friendly name and returns true if it should be included in the search</param>
        /// <returns></returns>
        public static IDictionary<string, IEnumerable<SqlConnectionStringBuilder>> GetServerConnections(MethodInfo mi, Func<string,bool> filter = null)
        {
            var requiredFeatureAttributes =
                mi.GetCustomAttributes<SqlRequiredFeatureAttribute>(true)
                    .Concat(mi.DeclaringType.GetCustomAttributes<SqlRequiredFeatureAttribute>()).ToArray();

            var requiredFeatures = requiredFeatureAttributes.SelectMany(feature => feature.RequiredFeatures).Distinct().ToArray();

            var serverConnections = new Dictionary<string, IEnumerable<SqlConnectionStringBuilder>>();
            //We need to check each of the defined servers to see if they're flagged
            foreach (KeyValuePair<string, Tuple<SMO.Server, TestServerDescriptor>> serverConnectionPair in ServerConnections.ServerDescriptors.Where(kvp => filter?.Invoke(kvp.Key) ?? true))
            {
                // Make sure the required features for the test are enabled on the server
                if (requiredFeatures.Any(feature => !serverConnectionPair.Value.Item2.EnabledFeatures.Contains(feature)))
                {
                    continue;
                }

                // Make sure the test requires at least a feature the server is researved for
                if (serverConnectionPair.Value.Item2.ReservedFor.Any() && !serverConnectionPair.Value.Item2.ReservedFor.Intersect(requiredFeatures).Any())
                {
                    continue;
                }

                //For SqlSupportedDimensionAttributes we consider the server supported if
                //ANY of the attributes return IsSupported is true

                //Note we look at attributes on both the method and the class the method is declared in
                var supportedDimensions =
                    mi.GetCustomAttributes<SqlSupportedDimensionAttribute>(true)
                    .Concat(mi.DeclaringType.GetCustomAttributes<SqlSupportedDimensionAttribute>()).ToArray();
                //If we don't have any SupportedDimensionAttributes we default to it being supported for all servers
                
                var exceptions = new List<Exception>();

                bool isSupported = supportedDimensions.Length == 0 ||
                    supportedDimensions.Any(a =>
                    {
                        try
                        {
                            return a.IsSupported(serverConnectionPair.Value.Item1, serverConnectionPair.Value.Item2,
                                serverConnectionPair.Key);
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
                if (isSupported)
                {
                    //For SqlUnsupportedDimensionAttributes we consider the server supported only
                    //if ALL of the unsupported attributes return IsSupported = true

                    //Note we look at attributes on both the method and the class the method is declared in
                    isSupported &= mi.GetCustomAttributes<SqlUnsupportedDimensionAttribute>(true)
                        .Concat(mi.DeclaringType.GetCustomAttributes<SqlUnsupportedDimensionAttribute>())
                        .Aggregate(true, (current, unsupportedDimensionAttribute) =>
                        {
                            try
                            {
                                return current & unsupportedDimensionAttribute.IsSupported(
                                           serverConnectionPair.Value.Item1, serverConnectionPair.Value.Item2,
                                           serverConnectionPair.Key);
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
                        serverConnections.Add(serverConnectionPair.Key,
                            serverConnectionPair.Value.Item2.AllConnectionStrings.Select(
                                connString => new SqlConnectionStringBuilder(connString)));
                    }
                }
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
    }
}
