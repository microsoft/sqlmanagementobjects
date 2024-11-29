// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
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
                var connectionString = new SqlConnectionStringBuilder()
                {
                    DataSource = environment.ServerName,
                    ConnectTimeout = environment.Timeout,
                    TrustServerCertificate = true
                };
                if (!string.IsNullOrEmpty(environment.UserName))
                {
                    connectionString.UserID = environment.UserName;
                    connectionString.Password = environment.Password;
                }
                else
                {
                    connectionString.IntegratedSecurity = true;
                }
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
                        descriptor.EnabledFeatures = new[] { SqlFeature.NoDropCreate };
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
