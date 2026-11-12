// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.RegisteredServersUnitTests
{
    [TestClass]
    public class MruConnectionsTests
    {
        [TestInitialize]
        public void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var tempFile = Path.GetTempFileName();
            using (var stream = assembly.GetManifestResourceStream("testregsrvr"))
            using (var reader = new StreamReader(stream))
            {
                File.WriteAllText(tempFile, reader.ReadToEnd());
            }
            TestContext.Properties["storeFile"] = tempFile;
        }

        [TestCleanup]
        public void Cleanup() => File.Delete(StoreFile);

        public VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }

        private string StoreFile => (string)this.TestContext.Properties["storeFile"];

        [TestMethod]
        [TestCategory("Unit")]
        public void RegisteredServersStore_MruSqlConnectionsGroup_LoadsFromTestXml()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var mruGroup = store.MruSqlConnectionsGroup;
            Assert.Multiple(() =>
            {
                Assert.That(mruGroup, Is.Not.Null, "MruSqlConnectionsGroup should not be null for a local store");
                Assert.That(mruGroup.Name, Is.EqualTo("MruSqlConnectionsGroup"), "MruSqlConnectionsGroup.Name");
                Assert.That(mruGroup.ServerType, Is.EqualTo(Microsoft.SqlServer.Management.Common.ServerType.DatabaseEngine), "MruSqlConnectionsGroup.ServerType");
                Assert.That(mruGroup.IsMruGroup, Is.True, "MruSqlConnectionsGroup.IsMruGroup");
                Assert.That(mruGroup.IsSystemServerGroup, Is.True, "MruSqlConnectionsGroup.IsSystemServerGroup");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void RegisteredServersStore_MruSqlConnectionsGroup_AutoCreatesOnFreshStore()
        {
            var tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            try
            {
                var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(tempFile);
                var mruGroup = store.MruSqlConnectionsGroup;
                Assert.Multiple(() =>
                {
                    Assert.That(mruGroup, Is.Not.Null, "MruSqlConnectionsGroup should auto-create for a local store");
                    Assert.That(mruGroup.Name, Is.EqualTo("MruSqlConnectionsGroup"), "Auto-created MruSqlConnectionsGroup.Name");
                    Assert.That(mruGroup.ServerGroups, Is.Empty, "Auto-created MruSqlConnectionsGroup should have no installation groups");
                });
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void RegisteredServersStore_MruSqlConnectionsGroup_HasInstallationSubGroup()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var mruGroup = store.MruSqlConnectionsGroup;
            var installations = mruGroup.ServerGroups;
            Assert.Multiple(() =>
            {
                Assert.That(installations.Count, Is.EqualTo(1), "Should have one installation sub-group");
                Assert.That(installations.Cast<ServerGroup>().Select(g => g.Name),
                    Is.EqualTo(new[] { @"C:\Program Files\SSMS20" }),
                    "Installation group name should be the SSMS path");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MruSqlConnectionsGroup_InstallationGroup_ContainsMruServers()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var installGroup = store.MruSqlConnectionsGroup.ServerGroups[@"C:\Program Files\SSMS20"];
            Assert.That(installGroup, Is.Not.Null, "Installation sub-group should exist");
            var servers = installGroup.RegisteredServers;
            Assert.Multiple(() =>
            {
                Assert.That(servers.Cast<RegisteredServer>().Select(r => r.Name),
                    Is.EquivalentTo(new[] { "mru-server1", "mru-server2" }),
                    "MRU servers in installation group");
                Assert.That(servers["mru-server1"].ServerName, Is.EqualTo("mru-server1.database.windows.net"),
                    "mru-server1.ServerName");
                Assert.That(servers["mru-server1"].Tag, Is.EqualTo("2026-03-06T10:30:00.0000000Z"),
                    "mru-server1 Tag should store LastConnectionTime");
                Assert.That(servers["mru-server2"].Tag, Is.EqualTo("2026-03-05T08:15:00.0000000Z"),
                    "mru-server2 Tag should store LastConnectionTime");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerGroup_IsMruGroup_TrueForMruRootAndInstallationChildren()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var mruRoot = store.MruSqlConnectionsGroup;
            var installGroup = mruRoot.ServerGroups[@"C:\Program Files\SSMS20"];
            Assert.Multiple(() =>
            {
                Assert.That(mruRoot.IsMruGroup, Is.True, "MRU root group should be an MRU group");
                Assert.That(installGroup.IsMruGroup, Is.True, "Installation sub-group should be an MRU group");
                Assert.That(store.DatabaseEngineServerGroup.IsMruGroup, Is.False,
                    "DatabaseEngineServerGroup should not be an MRU group");
                Assert.That(store.CentralManagementServerGroup.IsMruGroup, Is.False,
                    "CentralManagementServerGroup should not be an MRU group");
                Assert.That(store.AnalysisServicesServerGroup.IsMruGroup, Is.False,
                    "AnalysisServicesServerGroup should not be an MRU group");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void DatabaseEngineServerGroup_GetDescendantRegisteredServers_DoesNotIncludeMruEntries()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var dbEngineServers = store.DatabaseEngineServerGroup.GetDescendantRegisteredServers();
            Assert.Multiple(() =>
            {
                Assert.That(dbEngineServers.Select(r => r.Name),
                    Is.EquivalentTo(new[] { "myserver", "sqltools2019-3" }),
                    "DatabaseEngineServerGroup descendants should not contain MRU entries");
                Assert.That(dbEngineServers.Select(r => r.Name),
                    Has.No.Member("mru-server1").And.No.Member("mru-server2"),
                    "MRU servers must not appear in DatabaseEngineServerGroup descendants");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MruSqlConnectionsGroup_CannotBeDroppedOrRenamed()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var mruGroup = store.MruSqlConnectionsGroup;
            Assert.Multiple(() =>
            {
                Assert.That(mruGroup.Drop, Throws.InstanceOf<RegisteredServerException>(),
                    "Should not allow drop of MRU connections group");
                Assert.That(() => mruGroup.Rename("somename"), Throws.InstanceOf<RegisteredServerException>(),
                    "Should not allow rename of MRU connections group");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MruSqlConnectionsGroup_DisplayName_ReturnsLocalizedName()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var mruGroup = store.MruSqlConnectionsGroup;
            Assert.That(mruGroup.DisplayName, Is.EqualTo("MRU SQL Connections"),
                "MruSqlConnectionsGroup.DisplayName should return the localized display name");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MruSqlConnectionsGroup_PartitionedByInstallPath_RoundtripsToXml()
        {
            var tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            try
            {
                // Create store with MRU data for two installations
                var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(tempFile);
                var mruRoot = store.MruSqlConnectionsGroup;

                var install1 = new ServerGroup(mruRoot, @"C:\Program Files\SSMS20");
                install1.Create();

                var install2 = new ServerGroup(mruRoot, @"C:\Program Files\SSMS21");
                install2.Create();

                var server1 = new RegisteredServer(install1, "server-a")
                {
                    ServerName = "server-a.database.windows.net",
                    CredentialPersistenceType = CredentialPersistenceType.PersistLoginName,
                    Tag = DateTime.UtcNow.ToString("o")
                };
                server1.Create();

                var server2 = new RegisteredServer(install2, "server-b")
                {
                    ServerName = "server-b.database.windows.net",
                    CredentialPersistenceType = CredentialPersistenceType.PersistLoginName,
                    Tag = DateTime.UtcNow.AddDays(-1).ToString("o")
                };
                server2.Create();

                // Reload store from the serialized file
                var reloaded = RegisteredServersStore.InitializeLocalRegisteredServersStore(tempFile);
                var reloadedMru = reloaded.MruSqlConnectionsGroup;
                Assert.Multiple(() =>
                {
                    Assert.That(reloadedMru.ServerGroups.Count, Is.EqualTo(2),
                        "Should have two installation groups after roundtrip");
                    Assert.That(reloadedMru.ServerGroups[@"C:\Program Files\SSMS20"], Is.Not.Null,
                        "SSMS20 installation group should survive roundtrip");
                    Assert.That(reloadedMru.ServerGroups[@"C:\Program Files\SSMS21"], Is.Not.Null,
                        "SSMS21 installation group should survive roundtrip");
                    Assert.That(reloadedMru.ServerGroups[@"C:\Program Files\SSMS20"]
                        .RegisteredServers["server-a"].ServerName,
                        Is.EqualTo("server-a.database.windows.net"),
                        "server-a ServerName should survive roundtrip");
                    Assert.That(reloadedMru.ServerGroups[@"C:\Program Files\SSMS21"]
                        .RegisteredServers["server-b"].ServerName,
                        Is.EqualTo("server-b.database.windows.net"),
                        "server-b ServerName should survive roundtrip");
                });

                // Verify regular groups are unaffected
                Assert.That(reloaded.DatabaseEngineServerGroup.GetDescendantRegisteredServers(), Is.Empty,
                    "DatabaseEngineServerGroup should have no servers in this fresh store");
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void MruSqlConnectionsGroup_InstallationGroup_DropRemovesConnections()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var mruRoot = store.MruSqlConnectionsGroup;
            var installGroup = mruRoot.ServerGroups[@"C:\Program Files\SSMS20"];
            Assert.That(installGroup, Is.Not.Null, "Installation group should exist before drop");
            installGroup.Drop();
            Assert.Multiple(() =>
            {
                Assert.That(installGroup.IsDropped, Is.True, "Installation group should be dropped");
                Assert.That(mruRoot.ServerGroups.Count, Is.EqualTo(0),
                    "MruSqlConnectionsGroup should have no installation groups after drop");
            });

            // Verify the drop persists through re-serialization
            var reloaded = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            Assert.That(reloaded.MruSqlConnectionsGroup.ServerGroups.Count, Is.EqualTo(0),
                "MruSqlConnectionsGroup should have no installation groups after reload");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void BackwardCompat_ExistingServerGroups_UnaffectedByMruGroup()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            Assert.Multiple(() =>
            {
                // Verify all standard groups still work as before
                Assert.That(store.DatabaseEngineServerGroup.GetDescendantRegisteredServers().Select(r => r.Name),
                    Is.EquivalentTo(new[] { "myserver", "sqltools2019-3" }),
                    "DatabaseEngineServerGroup descendants should be unchanged");
                Assert.That(store.CentralManagementServerGroup.RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name),
                    Is.EqualTo(new[] { "myserver" }),
                    "CentralManagementServerGroup servers should be unchanged");
                Assert.That(store.DatabaseEngineServerGroup.IsSystemServerGroup, Is.True,
                    "DatabaseEngineServerGroup.IsSystemServerGroup should still be true");
                Assert.That(store.DatabaseEngineServerGroup.IsMruGroup, Is.False,
                    "DatabaseEngineServerGroup.IsMruGroup should be false");

                // Verify MRU group exists in ServerGroups but is isolated
                Assert.That(store.ServerGroups.Cast<ServerGroup>().Any(g => g.Name == "MruSqlConnectionsGroup"),
                    Is.True, "MruSqlConnectionsGroup should be present in ServerGroups collection");
                Assert.That(store.ServerGroups.Cast<ServerGroup>().Where(g => !g.IsMruGroup).Select(g => g.Name),
                    Is.EquivalentTo(new[] {
                        "AnalysisServicesServerGroup",
                        "CentralManagementServerGroup",
                        "DatabaseEngineServerGroup",
                        "IntegrationServicesServerGroup",
                        "ReportingServicesServerGroup",
                        "SqlServerCompactEditionServerGroup"
                    }),
                    "Filtering by !IsMruGroup should yield only standard groups");
            });
        }
    }
}
