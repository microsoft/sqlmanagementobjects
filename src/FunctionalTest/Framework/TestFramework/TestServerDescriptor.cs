// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Data about a server used for Data Tools test runs
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class TestServerDescriptor : TestDescriptor
    {
        /// <summary>
        /// Connection string for the connection
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Backup connection string to use in case of a failure while running
        /// the test using the first connection string
        /// </summary>
        public IEnumerable<string> BackupConnectionStrings { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// Iterates through all the connection strings defined for this server target, starting with the
        /// primary connection strings and then all of the defined backup connection strings. 
        /// </summary>
        public IEnumerable<string> AllConnectionStrings
        {
            get
            {
                yield return ConnectionString;

                foreach (string connString in BackupConnectionStrings)
                {
                    yield return connString;
                }
            }
        }

        public bool ReuseExistingDatabase { get; set; } = false;

        /// <summary>
        /// Returns the set of server connection strings allotted for the current test.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<TestDescriptor> GetTestDescriptors(XDocument connStringsDoc, AzureKeyVaultHelper azureKeyVaultHelper)
        {
            string targetServersEnvVar =
                Environment.GetEnvironmentVariable("SqlTestTargetServersFilter", EnvironmentVariableTarget.Process) ??
                Environment.GetEnvironmentVariable("SqlTestTargetServersFilter", EnvironmentVariableTarget.User) ??
                Environment.GetEnvironmentVariable("SqlTestTargetServersFilter", EnvironmentVariableTarget.Machine);

            var targetServers =
                !string.IsNullOrWhiteSpace(targetServersEnvVar) ? new HashSet<string>(
                    targetServersEnvVar.Split(';').Select(l => l.Trim()),
                    StringComparer.OrdinalIgnoreCase)
                : null;

            if (targetServers != null)
            {
                TraceHelper.TraceInformation("Limiting tests to these servers based on environment: {0}", targetServersEnvVar);
                }
            
            var workspaces = connStringsDoc.XPathSelectElements(@"//FabricWorkspaces/FabricWorkspace")
                   .Select(workspaceElement => new FabricWorkspaceDescriptor
                   {
                       Name = workspaceElement.GetStringAttribute("name"),
                       Environment = workspaceElement.GetStringAttribute("environment"),
                       WorkspaceName = workspaceElement.GetStringAttribute("workspaceName"),
                       DatabaseEngineType = workspaceElement.GetAttribute("databaseenginetype",
                            (s) => (DatabaseEngineType)Enum.Parse(typeof(DatabaseEngineType), s)),
                       DatabaseEngineEdition =
                            workspaceElement.GetAttribute("db_engine_edition",
                                (s) =>
                                    s == null
                                        ? DatabaseEngineEdition.Unknown
                                        : (DatabaseEngineEdition)Enum.Parse(typeof(DatabaseEngineEdition), s)),
                       Description = workspaceElement.GetStringAttribute("description"),
                       EnabledFeatures = workspaceElement.GetAttribute("enabled_features", GetFeaturesFromString), // For now, all workspaces are Fabric
                       MajorVersion = workspaceElement.GetAttribute("majorversion",
                        (s) => string.IsNullOrEmpty(s) ? 0 : int.Parse(s)),
                       DbNamePrefix = workspaceElement.GetStringAttribute("dbNamePrefix"),
                   }).Where((d) => targetServers == null || targetServers.Contains(d.Name)).ToList();
           
            var servers = connStringsDoc.XPathSelectElements(@"//ConnectionString")
                    .Select(connStringElement => new TestServerDescriptor
                    {
                        ConnectionString = GetConnectionString(connStringElement, azureKeyVaultHelper),
                        BackupConnectionStrings = new List<string>(new[] { connStringElement.GetStringAttribute("backupConnectionString") }.Where(c => !string.IsNullOrEmpty(c))),
                        Name = connStringElement.GetStringAttribute("name"),
                        HostPlatform = connStringElement.GetStringAttribute("hostplatform"),
                        DatabaseEngineType = connStringElement.GetAttribute("databaseenginetype",
                            (s) => (DatabaseEngineType)Enum.Parse(typeof(DatabaseEngineType), s)),
                        DatabaseEngineEdition =
                            connStringElement.GetAttribute("db_engine_edition",
                                (s) =>
                                    s == null
                                        ? DatabaseEngineEdition.Unknown
                                        : (DatabaseEngineEdition)Enum.Parse(typeof(DatabaseEngineEdition), s)),
                        EnabledFeatures = connStringElement.GetAttribute("enabled_features", GetFeaturesFromString),
                        MajorVersion = connStringElement.GetAttribute("majorversion",
                        (s) => string.IsNullOrEmpty(s) ? 0 : int.Parse(s)),
                        ReservedFor = connStringElement.GetAttribute("reserved_for", GetFeaturesFromString)
                    }).Where((d) => targetServers == null || targetServers.Contains(d.Name)).ToList();

            return servers.Cast<TestDescriptor>().Concat(workspaces);
        }

        private static string GetConnectionString(XElement connStringElement, AzureKeyVaultHelper azureKeyVaultHelper)
        {
            var baseString = connStringElement.GetStringAttribute("connectionString");
            var connStringBuilder = new SqlConnectionStringBuilder(baseString);
            var credentialName = connStringElement.GetStringAttribute("passwordCredential");

            // Fall back to SQL auth on Linux test hosts
            if (Environment.OSVersion.Platform != PlatformID.Win32NT && connStringBuilder.IntegratedSecurity)
            {
                connStringBuilder.IntegratedSecurity = false;
                if (string.IsNullOrEmpty(connStringBuilder.UserID))
                {
                    connStringBuilder.UserID = "sa";
                }
            }

            if (!connStringBuilder.IntegratedSecurity && !string.IsNullOrEmpty(connStringBuilder.UserID) &&
                credentialName != null)
            {
                if (azureKeyVaultHelper == null)
                {
                    throw new InvalidOperationException("AzureKeyVaultHelper must be provided to fetch passwords");
                }
                connStringBuilder.Password = azureKeyVaultHelper.GetDecryptedSecret(credentialName);
            }

            return connStringBuilder.ConnectionString;
        }

        

        private static IEnumerable<SqlFeature> GetFeaturesFromString(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return new SqlFeature[] { };
            }

            string[] features = source.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return features.Select(feature => (SqlFeature)Enum.Parse(typeof(SqlFeature), feature)).ToArray();
        }
    }

    static class XElementExtensions
    {
        internal static string GetStringAttribute(this XElement xElement, string attrName)
        {
            return xElement.GetAttribute(attrName, (s) => s);
        }

        internal static T GetAttribute<T>(this XElement xElement, string attrName, Func<string,T> converter = null )
        {
            XAttribute attr = xElement.Attribute(attrName);
            if (attr == null && converter == null)
            {
                throw new InvalidOperationException(
                    string.Format("ConnectionString node {0} missing {1} attribute",
                        xElement, attrName));
            }

            return converter == null ? DefaultConvert<T>(attr.Value) : converter(attr == null ? null : attr.Value);
        }

        static T DefaultConvert<T>(string s)
        {
            return (T) Convert.ChangeType(s, typeof (T));
        }
    }
}
