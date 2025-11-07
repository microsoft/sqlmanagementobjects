// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Reads server connections from a JSON file
    /// </summary>
    public class JsonTestServerSource
    {
        static public IList<TestServerDescriptor> TryLoadServerConnections()
        {
            var jsonFile = Environment.GetEnvironmentVariable("EnvironmentJsonFilePath");
            if (jsonFile == null)
            {
                Trace.TraceInformation("EnvironmentJsonFilePath not set. Skipping json load path.");
                return new List<TestServerDescriptor>();
            }
            List<JsonSqlEnvironment> environments;
            using (var textReader = new StreamReader(jsonFile, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                environments = (List<JsonSqlEnvironment>)new JsonSerializer().Deserialize(new JsonTextReader(textReader), typeof(List<JsonSqlEnvironment>));
            }
            var index = 1;
            return environments.Select(environment =>
            {
                var connectionString = GetBaseConnectionString(environment);
                if (!string.IsNullOrEmpty(environment.UserName))
                {
                    connectionString.UserID = environment.UserName;
                    connectionString.Password = environment.Password;
                }
                else
                {
                    connectionString.IntegratedSecurity = true;
                }
                var stringToTrace = new SqlConnectionStringBuilder(connectionString.ConnectionString)
                {
                    Password = string.IsNullOrEmpty(connectionString.Password) ? string.Empty : "<redacted>"
                };
                Trace.TraceInformation($"Connection string from JSON file:{stringToTrace.ConnectionString}");
                var descriptor = new TestServerDescriptor()
                {
                    Name = "TestServer" + index++,
                    ConnectionString = connectionString.ConnectionString
                };
                using (var connection = new SqlConnection(descriptor.ConnectionString))
                {
                    var serverConnection = new ServerConnection(connection);
                    descriptor.HostPlatform = serverConnection.HostPlatform;
                    descriptor.DatabaseEngineEdition = serverConnection.DatabaseEngineEdition;
                    descriptor.MajorVersion = serverConnection.ServerVersion.Major;
                    descriptor.DatabaseEngineType = serverConnection.DatabaseEngineType;
                    var realEdition = Convert.ToInt32(serverConnection.ExecuteScalar("select serverproperty('EngineEdition')"));
                    // 12 is the Fabric Native edition. We can't use TSQL to drop or create the database.
                    if (realEdition == 12)
                    {
                        // For Fabric database, when connection string is specified, database shouldn't be dropped.
                        descriptor.EnabledFeatures = new[] { SqlFeature.Fabric, SqlFeature.NoDropCreate };
                    }
                    else
                    {
                        // This JSON input should only be used for SQL2022 and newer
                        descriptor.EnabledFeatures = new[] { SqlFeature.Hekaton, SqlFeature.AzureLedger, SqlFeature.SqlClr };
                    }
                }
                return descriptor;
            }).ToList();
        }
        private static SqlConnectionStringBuilder GetBaseConnectionString(JsonSqlEnvironment environment)
        {
            var strictEncryptAvailable = string.Compare(Environment.GetEnvironmentVariable("FS_GenerateTdsCertificate") ?? "false", "true", ignoreCase: true) == 0;
            var connectionString = new SqlConnectionStringBuilder()
            {
                DataSource = strictEncryptAvailable ? $"tcp:{environment.ServerName}" : environment.ServerName,
                ConnectTimeout = environment.Timeout,
                TrustServerCertificate = !strictEncryptAvailable,
#if MICROSOFTDATA
                Encrypt = strictEncryptAvailable ? SqlConnectionEncryptOption.Strict : SqlConnectionEncryptOption.Optional,
#endif
            };
#if MICROSOFTDATA
            // if the servername isn't a full domain name, we assume it's the local machine and might have an alternative subject name
            if (!environment.ServerName.Contains('.') && !connectionString.TrustServerCertificate)
            {
                connectionString.HostNameInCertificate = GetSubjectAlternativeName();
            }
#endif
            return connectionString;
        }

        private static string GetSubjectAlternativeName()
        {
            var result = (majorVersion: int.MinValue, SAN: string.Empty);

            var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);

            using (baseKey)
            using (var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server"))
            {
                foreach (var subKeyName in key.GetSubKeyNames())
                {
                    // We are only interested in the default instance of SQL Server
                    var m = Regex.Match(subKeyName, @"^MSSQL(\d+)\.MSSQLSERVER");
                    if (!m.Success)
                    {
                        continue;
                    }

                    // In the unlikely scenario where there's multiple versions of SQL Server, assumes we are
                    // connecting to the one with highest major version.
                    var majorVersion = int.Parse(m.Groups[1].Value);
                    if (majorVersion <= result.majorVersion)
                    {
                        continue;
                    }

                    using (var key2 = key.OpenSubKey($@"{subKeyName}\MSSQLServer\SuperSocketNetLib"))
                    { 
                        result = (majorVersion, key2.GetValue("SubjectAlternativeName", string.Empty) as string);
                    }
                }
            }

            return result.SAN;
        }
    }

    internal class JsonSqlEnvironment
    {
        /// <summary>
        /// Gets or sets Database name
        /// </summary>
        public string Database { get; set; }

        /// <summary>
        /// Gets or sets InitialCatalog 
        /// </summary>
        public string InitialCatalog { get; set; }

        /// <summary>
        /// Gets or sets Password
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets ServerName
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Gets or sets Timeout
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// Gets or sets UserName
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Initialize default values if any. Note: Default constructor is used while deserializing unless specified otherwise.
        /// </summary>
        public JsonSqlEnvironment()
        {
            ServerName = ".";
            Database = "master";
            InitialCatalog = "master";
            Timeout = 60;
        }
    }
}
