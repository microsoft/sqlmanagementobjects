//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the Server object
    /// </summary>
    [TestClass]
    public class ServerTests : UnitTestBase
    {
        /// <summary>
        /// Validate we get LCID for all known collations from a hard-coded list, since the list rarely changes.
        /// If there are additional collations not in the list, we get it from the server at runtime.
        /// GetStringComparer will call GetLCIDCollation to get the LCID from the hard-coded collation list
        [TestCategory("Unit")]
        [TestMethod]
        public void GetLcidFromCollationListTest()
        {
            var server = new Microsoft.SqlServer.Management.Smo.Server();
            var comparer = server.GetStringComparer("Japanese_BIN2");
            Assert.That(comparer, Is.Not.Null, "Cannot get valid LCID or comparer");
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Server_name_constructor_sets_State_to_Existing()
        {
            var server = new Management.Smo.Server("someName");
            Assert.That(server.State, Is.EqualTo(SqlSmoState.Existing), "server.State after construction");
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Server_IsSupportedProperty_works_in_DesignMode()
        {
            var server = GetDesignModeServer(12);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<Table>(t => t.LedgerType), Is.False, "LedgerType is not supported on v12");
                Assert.That(server.IsSupportedProperty<Database>(d => d.MirroringPartner), Is.True, "MirroringPartner is supported on v12");
            });
            server = GetDesignModeServer(16);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<Table>(t => t.LedgerType), Is.True, "LedgerType is supported on v16");
                Assert.That(server.IsSupportedProperty<Database>(d => d.MirroringPartner), Is.True, "MirroringPartner is supported on v16");
                Assert.That(server.IsSupportedProperty<Database>(d => d.DataRetentionEnabled), Is.False, "DataRetentionEnabled is not supported on Enterprise edition");
            });
            server = GetDesignModeServer(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<Table>(t => t.LedgerType), Is.True, "LedgerType is supported on v12 Azure");
                Assert.That(server.IsSupportedProperty<Database>(d => d.MirroringPartner), Is.False, "MirroringPartner is not supported on v12 Azure");
                Assert.That(server.IsSupportedProperty<Table>(nameof(Table.LedgerType), DatabaseEngineEdition.SqlDataWarehouse), Is.False, "LedgerType is not supported on v12 DW");
            });
            server = GetDesignModeServer(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<Table>(t => t.LedgerType), Is.False, "LedgerType is not supported on v12 DW");
                Assert.That(server.IsSupportedProperty<Database>(d => d.MirroringPartner), Is.False, "MirroringPartner is not supported on v12 DW");
            });
            // Edge and OnDemand and MI have special cases not handled by the metadata provider
            server = GetDesignModeServer(16, DatabaseEngineType.Standalone, DatabaseEngineEdition.SqlDatabaseEdge);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<Database>(d => d.DataRetentionEnabled), Is.True, "DataRetentionEnabled is supported on Edge edition");
            });
            server = GetDesignModeServer(16, DatabaseEngineType.Standalone, DatabaseEngineEdition.SqlOnDemand);
            Assert.Multiple(() =>
            {
                Assert.That(server.IsSupportedProperty<Database>(d => d.DataRetentionEnabled), Is.False, "DataRetentionEnabled is not supported on SqlOnDemand edition");
                Assert.That(server.IsSupportedProperty<Database>(nameof(Database.AutoClose)), Is.False, "AutoClose is not supported on SqlOnDemand edition");
            });
            Assert.Multiple(() =>
            {
                Assert.Throws<System.ArgumentException>(() => server.IsSupportedProperty(typeof(SqlSmoObject), "someproperty"), "IsSupportedProperty only works for types with metadata providers");
                Assert.Throws<System.ArgumentException>(() => server.IsSupportedProperty<Table>(t => t.GetContextDB()), "IsSupportedProperty requires a Property expression" );
            });
        }

        private Management.Smo.Server GetDesignModeServer(int majorVersion, DatabaseEngineType databaseEngineType = DatabaseEngineType.Standalone,   DatabaseEngineEdition edition = DatabaseEngineEdition.Enterprise)
        {
            var serverConnection = new ServerConnection() { 
                ServerVersion = new ServerVersion(majorVersion, 0), 
                TrueName = "designMode",
                DatabaseEngineEdition = edition,
                DatabaseEngineType = databaseEngineType
            };
            var server = new Management.Smo.Server(serverConnection);
            // design mode requires an offline connection
            (server as ISfcHasConnection).ConnectionContext.Mode = SfcConnectionContextMode.Offline;
            return server;
        }
    }
}
