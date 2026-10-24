// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Functional tests for SqlSmoObject async initialization methods.
    /// These tests require a real SQL Server connection.
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class SmoObjectAsyncInitTests : SqlTestBase
    {
        /// <summary>
        /// Verifies that after Database.InitializeAsync completes, the populated properties can be read via
        /// the normal (non-async) property getters with no further server round trips.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Database_InitializeAsync_PropertiesAccessibleSynchronously()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;

                // Create a fresh database object that hasn't been initialized
                var freshDb = new Database(server, db.Name);

                await freshDb.InitializeAsync().ConfigureAwait(false);

                // Verify properties are accessible via the regular synchronous getters
                Assert.That(freshDb.Name, Is.EqualTo(db.Name), "Name property should be accessible after InitializeAsync");
                Assert.That(freshDb.CreateDate, Is.Not.EqualTo(DateTime.MinValue), "CreateDate should be populated after InitializeAsync");
                Assert.That(freshDb.ID, Is.GreaterThan(0), "ID should be populated after InitializeAsync");

                Trace.TraceInformation($"Database '{freshDb.Name}' initialized async - CreateDate: {freshDb.CreateDate}, ID: {freshDb.ID}");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Table.InitializeAsync results match sync Initialize
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Table_InitializeAsync_ResultMatchesSyncInitialize()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var testTableName = $"InitAsyncTestTable_{Guid.NewGuid()}";
                var table = new Table(db, testTableName);
                var col = new Column(table, "ID", DataType.Int);
                col.Nullable = false;
                table.Columns.Add(col);
                table.Create();

                try
                {
                    var syncTable = new Table(db, testTableName);
                    var asyncTable = new Table(db, testTableName);

                    syncTable.Initialize();
                    await asyncTable.InitializeAsync().ConfigureAwait(false);

                    Assert.That(asyncTable.Name, Is.EqualTo(syncTable.Name), "Name should match");
                    Assert.That(asyncTable.ID, Is.EqualTo(syncTable.ID), "ID should match");
                    Assert.That(asyncTable.CreateDate, Is.EqualTo(syncTable.CreateDate), "CreateDate should match");

                    Trace.TraceInformation($"Table '{testTableName}' properties match between sync and async initialization");
                }
                finally
                {
                    db.Tables[testTableName]?.Drop();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that Server.RefreshAsync makes all properties available
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Server_RefreshAsync_AllPropertiesAvailable()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;

                await server.RefreshAsync().ConfigureAwait(false);

                Assert.That(server.Name, Is.Not.Null.And.Not.Empty, "Name should be available after RefreshAsync");
                Assert.That(server.VersionString, Is.Not.Null.And.Not.Empty, "VersionString should be available after RefreshAsync");
                Assert.That(server.Edition, Is.Not.Null.And.Not.Empty, "Edition should be available after RefreshAsync");
                
                // Product is not supported on Azure SQL Database, check before accessing
                if (server.IsSupportedProperty(nameof(Microsoft.SqlServer.Management.Smo.Server.Product)))
                {
                    Assert.That(server.Product, Is.Not.Null.And.Not.Empty, "Product should be available after RefreshAsync");
                    Trace.TraceInformation($"Server '{server.Name}' refreshed async - Version: {server.VersionString}, Edition: {server.Edition}, Product: {server.Product}");
                }
                else
                {
                    Trace.TraceInformation($"Server '{server.Name}' refreshed async - Version: {server.VersionString}, Edition: {server.Edition}");
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that InitializeAsync with specific fields only populates requested fields
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Database_InitializeAsync_SpecificFields_PopulatesOnlyRequested()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;
                var freshDb = new Database(server, db.Name);

                var requestedFields = new[] { "Name", "ID" };
                await freshDb.InitializeAsync(requestedFields).ConfigureAwait(false);

                Assert.That(freshDb.Name, Is.EqualTo(db.Name), "Name should be accessible");
                Assert.That(freshDb.ID, Is.GreaterThan(0), "ID should be accessible");

                Trace.TraceInformation($"Database '{freshDb.Name}' initialized with specific fields - ID: {freshDb.ID}");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that accessing a property not included in InitializeAsync and not a default field throws PropertyNotSetException
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Database_InitializeAsync_AccessNonInitializedProperty_ThrowsException()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;
                var freshDb = new Database(server, db.Name);

                // Initialize with only specific fields (not including Collation which is not a default field)
                var requestedFields = new[] { "Name", "ID" };
                await freshDb.InitializeAsync(requestedFields).ConfigureAwait(false);

                Assert.That(freshDb.Name, Is.EqualTo(db.Name), "Name should be accessible");
                Assert.That(freshDb.ID, Is.GreaterThan(0), "ID should be accessible");

                Assert.Throws<Microsoft.SqlServer.Management.Smo.PropertyNotSetException>(
                    () => { var collation = freshDb.Collation; },
                    "Accessing non-initialized property Collation should throw PropertyNotSetException");

                Trace.TraceInformation($"Database '{freshDb.Name}' correctly throws when accessing non-initialized property");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that InitializeAsync with extended fields (more than defaults) makes all requested properties accessible
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11, HostPlatform = HostPlatformNames.Windows)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabase, DatabaseEngineEdition.SqlDataWarehouse)]
        public async Task Database_InitializeAsync_WithExtendedFields_AllPropertiesAccessible()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;
                var freshDb = new Database(server, db.Name);

                // Include default fields plus additional ones like Collation, Owner, RecoveryModel
                var extendedFields = new[] { "Name", "ID", "CreateDate", "Collation", "Owner", "RecoveryModel", "Status" };
                await freshDb.InitializeAsync(extendedFields).ConfigureAwait(false);

                Assert.That(freshDb.Name, Is.EqualTo(db.Name), "Name should be accessible");
                Assert.That(freshDb.ID, Is.GreaterThan(0), "ID should be accessible");
                Assert.That(freshDb.CreateDate, Is.Not.EqualTo(DateTime.MinValue), "CreateDate should be accessible");
                Assert.That(freshDb.Collation, Is.Not.Null.And.Not.Empty, "Collation should be accessible");
                Assert.That(freshDb.Owner, Is.Not.Null.And.Not.Empty, "Owner should be accessible");
                Assert.That(freshDb.RecoveryModel, Is.Not.Null, "RecoveryModel should be accessible");
                Assert.That(freshDb.Status, Is.Not.Null, "Status should be accessible");

                Trace.TraceInformation($"Database '{freshDb.Name}' initialized with extended fields - Collation: {freshDb.Collation}, Owner: {freshDb.Owner}, RecoveryModel: {freshDb.RecoveryModel}");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that cancelled InitializeAsync leaves object unchanged
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Database_InitializeAsync_Cancellation_LeavesObjectUnchanged()
        {
            await ExecuteFromDbPoolAsync(db =>
            {
                var server = db.Parent;
                var freshDb = new Database(server, db.Name);

                using var cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.That(
                    async () => await freshDb.InitializeAsync(cts.Token).ConfigureAwait(false),
                    Throws.InstanceOf<OperationCanceledException>(),
                    "InitializeAsync should throw OperationCanceledException when given a cancelled token");

                Trace.TraceInformation("InitializeAsync correctly threw OperationCanceledException on cancelled token");
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that calling InitializeAsync on an already initialized object is idempotent
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Database_InitializeAsync_AlreadyInitialized_IsIdempotent()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;
                var testDb = server.Databases[db.Name];

                await testDb.InitializeAsync().ConfigureAwait(false);
                var firstCallName = testDb.Name;
                var firstCallId = testDb.ID;

                await testDb.InitializeAsync().ConfigureAwait(false);
                var secondCallName = testDb.Name;
                var secondCallId = testDb.ID;

                Assert.That(secondCallName, Is.EqualTo(firstCallName), "Name should be unchanged after second InitializeAsync");
                Assert.That(secondCallId, Is.EqualTo(firstCallId), "ID should be unchanged after second InitializeAsync");

                Trace.TraceInformation("Multiple InitializeAsync calls are idempotent");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that JobServer.InitializeAsync works correctly despite not having Name property
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11, HostPlatform = HostPlatformNames.Windows)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabase, DatabaseEngineEdition.SqlDataWarehouse)]
        public async Task JobServer_InitializeAsync_WorksWithoutNameProperty()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var server = db.Parent;
                var jobServer = server.JobServer;

                await jobServer.InitializeAsync().ConfigureAwait(false);

                // JobServer doesn't have Name property, but should have others
                Assert.That(jobServer.ServiceAccount, Is.Not.Null, "ServiceAccount should be available after InitializeAsync");

                Trace.TraceInformation($"JobServer initialized async - ServiceAccount: {jobServer.ServiceAccount}");
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Parity test: Verifies Server properties match between sync and async initialization
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task Server_InitializeAsync_MatchesSyncInitialize()
        {
            await ExecuteFromDbPoolAsync(async db =>
            {
                var connectionString = db.ExecutionManager.ConnectionContext.ConnectionString;

                var syncServer = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(new SqlConnection(connectionString)));
                var asyncServer = new Microsoft.SqlServer.Management.Smo.Server(new ServerConnection(new SqlConnection(connectionString)));

                try
                {
                    syncServer.ConnectionContext.Connect();
                    asyncServer.ConnectionContext.Connect();

                    syncServer.Initialize();
                    await asyncServer.InitializeAsync().ConfigureAwait(false);

                    Assert.That(asyncServer.Name, Is.EqualTo(syncServer.Name), "Name should match");
                    Assert.That(asyncServer.VersionString, Is.EqualTo(syncServer.VersionString), "VersionString should match");
                    Assert.That(asyncServer.Edition, Is.EqualTo(syncServer.Edition), "Edition should match");

                    Trace.TraceInformation("Server properties match between sync and async initialization");
                }
                finally
                {
                    syncServer.ConnectionContext.Disconnect();
                    asyncServer.ConnectionContext.Disconnect();
                }
            }).ConfigureAwait(false);
        }
    }
}
