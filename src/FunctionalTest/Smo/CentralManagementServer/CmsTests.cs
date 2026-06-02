// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
#if MICROSOFTDATA
using System;
using System.IO;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
namespace Microsoft.SqlServer.Test.SMO.CentralManagementServer
{
    [TestClass]
    public class CmsTests : SqlTestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var tempFile = Path.GetTempFileName();
            using (var stream = assembly.GetManifestResourceStream("testsrvr"))
            {
                using (var reader = new StreamReader(stream))
                {
                    File.WriteAllText(tempFile, reader.ReadToEnd());
                }
            }
            TestContext.Properties["storeFile"] = tempFile;
        }

        [TestCleanup]
        public void Cleanup() => File.Delete(StoreFile);

        private string StoreFile => (string)TestContext.Properties["storeFile"];

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void RegisteredServer_ConnectionString_inherits_cert_trust_from_CMS()
        {
            ExecuteTest(() =>
            {

                var originalString = SqlConnectionStringBuilder.ConnectionString;
                var newBuilder = new SqlConnectionStringBuilder(originalString)
                {
                    TrustServerCertificate = !SqlConnectionStringBuilder.TrustServerCertificate,
                    Encrypt = SqlConnectionEncryptOption.Optional
                };
                TestInheritTrust(SqlConnectionStringBuilder);
                TestInheritTrust(newBuilder);
            });
        }

        static private void TestInheritTrust(SqlConnectionStringBuilder sqlConnectionStringBuilder)
        {
            var serverConnection = new ServerConnection(new SqlConnection(sqlConnectionStringBuilder.ConnectionString));
            var store = new RegisteredServersStore(new ServerConnection(new SqlConnection(sqlConnectionStringBuilder.ConnectionString)));
            var registeredServer = new RegisteredServer(store.DatabaseEngineServerGroup, Guid.NewGuid().ToString())
            {
                ServerName = "server"
            };

            registeredServer.Create();
            try
            {
                store.DatabaseEngineServerGroup.Refresh();
                var builder = new SqlConnectionStringBuilder(registeredServer.ConnectionString);
                registeredServer = store.DatabaseEngineServerGroup.RegisteredServers[registeredServer.Name];
                Assert.That(builder.TrustServerCertificate, Is.EqualTo(sqlConnectionStringBuilder.TrustServerCertificate), $"TrustServerCertificate not inherited from {sqlConnectionStringBuilder}");
                Assert.That(builder.DataSource, Is.EqualTo("server"), "Wrong server name in the connection");
            }
            finally
            {
                registeredServer.Drop();
            }
        }
    }
}
#endif