// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.RegisteredServersUnitTests
{
    [TestClass]
    public class AzureDataStudioConnectionStoreTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void AzureDataStudioConnectionStore_parses_basic_json()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var tempFile = Path.GetTempFileName();
            using (var stream = assembly.GetManifestResourceStream("samplejson"))
            {
                using (var reader = new StreamReader(stream))
                {
                    File.WriteAllText(tempFile, reader.ReadToEnd());
                }
            }

            try
            {
                var connectionStore = AzureDataStudioConnectionStore.LoadAzureDataStudioConnections(tempFile);
                Assert.That(connectionStore.Groups.Select(g => g.Name),
                    Is.EquivalentTo(new[] {"ROOT", "Group1", "Subgroup1", "Group2"}), "Incorrect groups list");
                var connection =
                    connectionStore.Connections.Single(c => c.Id == "a22f922d-3708-4236-9fa5-c68e4af48cd2");
                Assert.That(connection.Options.Keys,
                    Is.EquivalentTo(new[]
                    {
                        "connectionName", "server", "database", "authenticationType", "user", "password",
                        "applicationName", "databaseDisplayName"
                    }), "Options keys for a connection");
                Assert.That(connection.Options["server"], Is.EqualTo("sqltools2016-3"), "Unexpected server property");
            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (IOException)
                {
                }
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void AzureDataStudioConnectionStore_returns_empty_lists_for_invalid_input()
        {
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "This is not json");
            var store = AzureDataStudioConnectionStore.LoadAzureDataStudioConnections(tempFile);
            Assert.That(store.Connections, Is.Empty, "Connections from bogus input");
            Assert.That(store.Groups, Is.Empty, "Groups from bogus input");
        }
    }
}