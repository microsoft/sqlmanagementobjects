// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.RegisteredServers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.RegisteredServersUnitTests
{
    [TestClass]
    public class RegisteredServersTests
    {
        [TestInitialize]
        public void Initialize()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var tempFile = Path.GetTempFileName();
            using (var stream = assembly.GetManifestResourceStream("testregsrvr"))
            {
                using (var reader = new StreamReader(stream))
                {
                    File.WriteAllText(tempFile, reader.ReadToEnd());
                }
            }
            TestContext.Properties["storeFile"] = tempFile;
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(StoreFile);
        }

        public VisualStudio.TestTools.UnitTesting.TestContext TestContext {get;set;}

        private string StoreFile => (string)this.TestContext.Properties["storeFile"];

        [TestMethod]
        [TestCategory("Unit")]
        public void RegisteredServersStore_initializes_from_valid_file()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var groups = store.DatabaseEngineServerGroup.ServerGroups;
            var server = store.DatabaseEngineServerGroup.RegisteredServers["myserver"];
            Assert.Multiple(() =>
            {
                Assert.That(groups.Cast<ServerGroup>().Select(sg => sg.Name), Is.EqualTo(new[] { "Group1" }), "DatabaseEngineServerGroup.ServerGroups");
                Assert.That(groups["Group1"].RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "sqltools2019-3" }), "Group1 servers");
                Assert.That(store.DatabaseEngineServerGroup.RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "myserver" }), "Root database engine servers");
                Assert.That(store.CentralManagementServerGroup.RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "myserver" }), "Central management servers");
                Assert.That(store.DatabaseEngineServerGroup.GetDescendantRegisteredServers().Select(r => r.Name), Is.EquivalentTo(new[] { "myserver", "sqltools2019-3" }), "GetDescendantRegisteredServers");
                Assert.That(store.IsLocal, Is.True, "store.IsLocal");
                Assert.That(store.DatabaseEngineServerGroup.IsSystemServerGroup, Is.True, "DatabaseEngineServerGroup.IsSystemServerGroup");
                Assert.That(groups["Group1"].Description, Is.EqualTo("Group1 description"), "Group1 description");
                Assert.That(server.UseCustomConnectionColor, Is.True, "UseCustomConnectionColor");
                Assert.That(server.Tag, Is.EqualTo("Sample Tag"), "myserver Tag");
                int color = unchecked((int)0xFF00FF00);
                Assert.That(server.CustomConnectionColorArgb, Is.EqualTo(color), "CustomConnectionColorArgb");
                Assert.That(server.ConnectionStringWithEncryptedPassword, Is.EqualTo("data source=myserver;initial catalog=mydatabase;integrated security=True;pooling=False;multipleactiveresultsets=False;connect timeout=90;encrypt=True;trustservercertificate=False;packet size=4096;column encryption setting=Enabled"), "ConnectionStringWithEncryptedPassword");
            });
        }

        const int TrustedAuthenticationType = 0;
        const int SqlAuthenticationType = 1;
        const int ActiveDirectoryPasswordAuthenticationType = 2;
        const int ActiveDirectoryIntegratedAuthenticationType = 3;
        const int ActiveDirectoryUniversalAuthenticationType = 4;
        const int ActiveDirectoryInteractiveAuthenticationType = 5;
        [TestMethod]
        [TestCategory("Unit")]
        public void ServerGroup_Alter_saves_file_with_added_server()
        {
            var tempFile = Path.GetTempFileName();
            File.Delete(tempFile);
            try
            {
                var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(tempFile);
                Assert.That(store.DatabaseEngineServerGroup.GetDescendantRegisteredServers(), Is.Empty, "Store should be empty for missing file");
                var pwdGuid = Guid.NewGuid();
                var server = new RegisteredServer(store.DatabaseEngineServerGroup, "newserver")
                {
                    ServerName = "someServer",
                    Description = "somedescription",
                    AuthenticationType = SqlAuthenticationType,
                    CredentialPersistenceType = CredentialPersistenceType.PersistLoginNameAndPassword,
                    ConnectionString = $"Data Source=someServer;User Id=someid;Password={pwdGuid};Integrated Security=false"
                };
                store.DatabaseEngineServerGroup.RegisteredServers.Add(server);
                store.DatabaseEngineServerGroup.Alter();
                Assert.That(File.Exists(tempFile), Is.True, "ServerGroup.Alter should save the file");
                var savedStore = RegisteredServersStore.InitializeLocalRegisteredServersStore(tempFile);
                Assert.That(savedStore.DatabaseEngineServerGroup.RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "newserver" }), "Root database engine servers after Alter");
                var savedServer = savedStore.DatabaseEngineServerGroup.RegisteredServers.Cast<RegisteredServer>().Single();
                Assert.Multiple(() =>
                {
                    Assert.That(savedServer.ServerName, Is.EqualTo(server.ServerName), "ServerName");
                    Assert.That(savedServer.Description, Is.EqualTo(server.Description), "Description");
                    Assert.That(savedServer.AuthenticationType, Is.EqualTo(server.AuthenticationType), "AuthenticationType");
                    // the well known identifiers like "data source" and "user id" get lower cased when persisted
                    Assert.That(savedServer.ConnectionString, Is.EqualTo(server.ConnectionString).IgnoreCase, "ConnectionString");
                });
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void RegisteredServersStore_initialize_returns_empty_store_on_handled_parse_error()
        {
            Trace.TraceInformation("Creating a zero length file that will cause a parsing exception");
            var tempFile = Path.GetTempFileName();
            bool handledException = false;
            RegisteredServersStore.ExceptionDelegates = (o) => { handledException = true; return true; };
            try
            {
                var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(tempFile);
                Assert.Multiple(() =>
                {
                    Assert.That(handledException, Is.True, "Initialize should have called ExceptionDelegate");
                    Assert.That(store.IntegrationServicesServerGroup.GetDescendantRegisteredServers(), Is.Empty, "IntegrationServicesServerGroup");
                    Assert.That(store.DatabaseEngineServerGroup.GetDescendantRegisteredServers(), Is.Empty, "DatabaseEngineServerGroup");
                });
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerGroup_Export_creates_valid_file()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            var outputFile = Path.GetTempFileName();
            File.Delete(outputFile);
            Trace.TraceInformation($"Exporting DatabaseEngineServerGroup content to {outputFile}");
            store.DatabaseEngineServerGroup.Export(outputFile, CredentialPersistenceType.PersistLoginNameAndPassword);
            try
            {
                var newGroup = new ServerGroup(store.DatabaseEngineServerGroup, "importGroup") { Description = "importGroup description" };
                store.DatabaseEngineServerGroup.ServerGroups.Add(newGroup);
                Assert.That(newGroup.IsSystemServerGroup, Is.False, "newGroup.IsSystemServerGroup");
                Assert.That(newGroup.DisplayName, Is.EqualTo(newGroup.Name), "custom group Name should match its DisplayName");
                newGroup.Import(outputFile);
                var groups = newGroup.ServerGroups;
                Assert.That(groups.Cast<ServerGroup>().Select(sg => sg.Name), Is.EqualTo(new[] { "Group1" }), "importedGroup.ServerGroups");
                Assert.That(newGroup.Description, Is.EqualTo("importGroup description"), "import group Description");
                Assert.That(groups["Group1"].RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "sqltools2019-3" }), "importedGroup.Group1 servers");
                Assert.That(newGroup.RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "myserver" }), "importedGroup engine servers");
                Assert.That(newGroup.GetDescendantRegisteredServers().Select(r => r.Name), Is.EquivalentTo(new[] { "myserver", "sqltools2019-3" }), "importedGroup GetDescendantRegisteredServers");
            }
            finally
            {
                File.Delete(outputFile);
            }
            Trace.TraceInformation("Exporting a new RegisteredServer named myserver to replace the one in the current store");
            var newServer = new RegisteredServer(store.DatabaseEngineServerGroup, "myserver")
            {
                AuthenticationType = ActiveDirectoryIntegratedAuthenticationType,
                Description = "replacement",
                ServerName = "replacementServer"
            };
            newServer.Export(outputFile, CredentialPersistenceType.PersistLoginName);
            Assert.That(store.DatabaseEngineServerGroup.RegisteredServers["myserver"].AuthenticationType, Is.EqualTo(TrustedAuthenticationType), "AuthenticationType of existing myserver");
            Trace.TraceInformation("Assigning a handler to DuplicateFound in order to allow deletion of the duplicate server already in the store");
            store.DatabaseEngineServerGroup.DuplicateFound += (o, e) => e.Confirm = true;
            store.DatabaseEngineServerGroup.Import(outputFile);
            var importedServer = store.DatabaseEngineServerGroup.RegisteredServers["myserver"];
            Assert.Multiple(() =>
            {
                Assert.That(importedServer.Name, Is.EqualTo(newServer.Name), "importedServer.Name");
                Assert.That(importedServer.Description, Is.EqualTo(newServer.Description), "importedServer.Description");
                Assert.That(importedServer.ServerName, Is.EqualTo(newServer.ServerName), "importedServer.ServerName");
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerGroup_Drop_removes_the_group()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            Assert.That(store.AnalysisServicesServerGroup.Drop, Throws.InstanceOf<RegisteredServerException>(), "Should not allow drop of system groups");
            var group = store.DatabaseEngineServerGroup.ServerGroups["Group1"];
            group.Drop();
            Assert.That(store.DatabaseEngineServerGroup.ServerGroups, Is.Empty, "Drop should remove the group");
            Assert.That(group.IsDropped, Is.True, "group.IsDropped after Drop");
            var server = store.DatabaseEngineServerGroup.RegisteredServers["myserver"];
            server.Drop();
            Assert.That(store.DatabaseEngineServerGroup.RegisteredServers, Is.Empty, "Drop should remove the registeredserver");
            Assert.That(server.IsDropped, Is.True, "server.IsDropped after Drop");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerGroup_Rename_changes_the_name()
        {
            var store = RegisteredServersStore.InitializeLocalRegisteredServersStore(StoreFile);
            Assert.That(() => store.AnalysisServicesServerGroup.Rename("somename"), Throws.InstanceOf<RegisteredServerException>(), "Should not allow Rename of system groups");
            store.DatabaseEngineServerGroup.ServerGroups["Group1"].Rename("somegroup");
            Assert.That(store.DatabaseEngineServerGroup.ServerGroups.Cast<ServerGroup>().Select(sg => sg.Name), Is.EqualTo(new[] { "somegroup" }), "DatabaseEngineServerGroup.ServerGroups after rename");
            store.DatabaseEngineServerGroup.RegisteredServers["myserver"].Rename("someserver");
            Assert.That(store.DatabaseEngineServerGroup.RegisteredServers.Cast<RegisteredServer>().Select(r => r.Name), Is.EqualTo(new[] { "someserver" }), "Root database engine servers after rename");
        }
    }
}
