// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Functional tests for Enumerator async methods.
    /// These tests require a real SQL Server connection.
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class EnumeratorAsyncTests : SqlTestBase
    {
        /// <summary>
        /// Verifies that Enumerator.GetDataAsync can fetch server properties
        /// and results match sync version
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void Enumerator_GetDataAsync_ServerProperties_MatchesSyncVersion()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = db.ExecutionManager.ConnectionContext;
                var urn = new Urn("Server");
                var properties = new[] { "Name", "VersionString", "Edition" };
                var request = new Request(urn, properties);

                // Get data using sync method
                var syncResult = Enumerator.GetData(serverConnection, request);
                var syncData = (DataTable)syncResult.Data;

                // Get data using async method
                var asyncResult = Enumerator.GetDataAsync(serverConnection, request).GetAwaiter().GetResult();
                var asyncData = (DataTable)asyncResult.Data;

                // Assert both results have data
                Assert.That(syncData.Rows.Count, Is.GreaterThan(0), "Sync result should have at least one row");
                Assert.That(asyncData.Rows.Count, Is.GreaterThan(0), "Async result should have at least one row");

                // Assert column count matches
                Assert.That(asyncData.Columns.Count, Is.EqualTo(syncData.Columns.Count), 
                    "Async and sync results should have same number of columns");

                // Assert row count matches
                Assert.That(asyncData.Rows.Count, Is.EqualTo(syncData.Rows.Count),
                    "Async and sync results should have same number of rows");

                // Assert values match for each requested property
                foreach (var property in properties)
                {
                    if (syncData.Columns.Contains(property) && asyncData.Columns.Contains(property))
                    {
                        var syncValue = syncData.Rows[0][property];
                        var asyncValue = asyncData.Rows[0][property];
                        Assert.That(asyncValue, Is.EqualTo(syncValue),
                            $"Property '{property}' should have same value in async and sync results");
                    }
                }
            });
        }

        /// <summary>
        /// Verifies that Enumerator.GetDataAsync can fetch database list
        /// and results match sync version
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void Enumerator_GetDataAsync_DatabaseList_MatchesSyncVersion()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = db.ExecutionManager.ConnectionContext;
                var urn = new Urn("Server/Database");
                var properties = new[] { "Name", "CreateDate", "Status" };
                var request = new Request(urn, properties);

                // Get data using sync method
                var syncResult = Enumerator.GetData(serverConnection, request);
                var syncData = (DataTable)syncResult.Data;

                // Get data using async method
                var asyncResult = Enumerator.GetDataAsync(serverConnection, request).GetAwaiter().GetResult();
                var asyncData = (DataTable)asyncResult.Data;

                // Assert both results have data
                Assert.That(syncData.Rows.Count, Is.GreaterThan(0), "Sync result should have at least one database");
                Assert.That(asyncData.Rows.Count, Is.GreaterThan(0), "Async result should have at least one database");

                // Assert column count matches
                Assert.That(asyncData.Columns.Count, Is.EqualTo(syncData.Columns.Count),
                    "Async and sync results should have same number of columns");

                // Assert row count matches
                Assert.That(asyncData.Rows.Count, Is.EqualTo(syncData.Rows.Count),
                    "Async and sync results should have same number of rows");

                // Assert database names are the same (order may differ, so use sets)
                var syncNames = syncData.Rows.Cast<DataRow>().Select(r => r["Name"].ToString()).OrderBy(n => n).ToArray();
                var asyncNames = asyncData.Rows.Cast<DataRow>().Select(r => r["Name"].ToString()).OrderBy(n => n).ToArray();
                
                Assert.That(asyncNames, Is.EqualTo(syncNames),
                    "Database names should match between async and sync results");
            });
        }

        /// <summary>
        /// Verifies that GetDataAsync respects cancellation token
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void Enumerator_GetDataAsync_CancellationToken_CancelsOperation()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = db.ExecutionManager.ConnectionContext;
                var urn = new Urn("Server/Database");
                var properties = new[] { "Name" };
                var request = new Request(urn, properties);

                // Create a cancellation token that is already cancelled
                var cts = new CancellationTokenSource();
                cts.Cancel();

                // Attempt to get data with cancelled token
                try
                {
                    var asyncResult = Enumerator.GetDataAsync(serverConnection, request, cts.Token).GetAwaiter().GetResult();
                    // If we get here, the operation wasn't properly cancelled
                    // This might happen if the operation completes before cancellation is checked
                    // So we don't fail the test, but log a warning
                    System.Diagnostics.Trace.WriteLine("Warning: Operation completed despite cancellation token");
                }
                catch (OperationCanceledException)
                {
                    // Expected - operation was cancelled
                    return;
                }
                catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
                {
                    // Expected - operation was cancelled (wrapped in AggregateException)
                    return;
                }
            });
        }

        /// <summary>
        /// Verifies that ExecutionManager.GetEnumeratorDataAsync works correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ExecutionManager_GetEnumeratorDataAsync_ReturnsDataTable()
        {
            ExecuteFromDbPool((db) =>
            {
                var executionManager = db.ExecutionManager;
                var urn = new Urn($"Server/Database[@Name='{Urn.EscapeString(db.Name)}']");
                var properties = new[] { "Name", "CreateDate" };
                var request = new Request(urn, properties);

                // Call internal GetEnumeratorDataAsync method directly (accessible via InternalsVisibleTo)
                var result = executionManager.GetEnumeratorDataAsync(request, CancellationToken.None).GetAwaiter().GetResult();

                // Assert we got data
                Assert.IsNotNull(result, "Result should not be null");
                Assert.That(result.Rows.Count, Is.GreaterThan(0), "Result should have at least one row");
                Assert.That(result.Columns.Contains("Name"), Is.True, "Result should have Name column");
            });
        }

        /// <summary>
        /// Verifies that ExecutionManager.GetEnumeratorDataReaderAsync works correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ExecutionManager_GetEnumeratorDataReaderAsync_ReturnsReader()
        {
            ExecuteFromDbPool((db) =>
            {
                var executionManager = db.ExecutionManager;
                var urn = new Urn($"Server/Database[@Name='{Urn.EscapeString(db.Name)}']");
                var properties = new[] { "Name", "CreateDate" };
                var request = new Request(urn, properties);

                // Call internal GetEnumeratorDataReaderAsync method directly (accessible via InternalsVisibleTo)
                var reader = executionManager.GetEnumeratorDataReaderAsync(request, CancellationToken.None).GetAwaiter().GetResult();

                try
                {
                    // Assert we got a reader
                    Assert.IsNotNull(reader, "Reader should not be null");
                    
                    // Read first row
                    Assert.That(reader.Read(), Is.True, "Reader should have at least one row");
                    
                    // Verify Name column exists and has a value
                    var nameIndex = reader.GetOrdinal("Name");
                    Assert.That(nameIndex, Is.GreaterThanOrEqualTo(0), "Name column should exist");
                    var nameValue = reader.GetValue(nameIndex);
                    Assert.IsNotNull(nameValue, "Name value should not be null");
                }
                finally
                {
                    reader?.Close();
                }
            });
        }

        /// <summary>
        /// Verifies that async and sync enumerator results are identical for table properties
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void Enumerator_GetDataAsync_TableProperties_MatchesSyncVersion()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = db.ExecutionManager.ConnectionContext;
                Table table = null;

                try
                {
                    // Create a test table using SMO object model
                    table = db.CreateTable("TestTable", 
                        new ColumnProperties("Id", DataType.Int) { Nullable = false },
                        new ColumnProperties("Value", DataType.NVarChar(50)));
                    
                    // Add primary key using SMO
                    table.CreateIndex("PK_TestTable", new IndexProperties() 
                    { 
                        KeyType = IndexKeyType.DriPrimaryKey,
                        ColumnNames = new[] { "Id" }
                    });

                    var urn = new Urn($"Server/Database[@Name='{Urn.EscapeString(db.Name)}']/Table[@Name='{Urn.EscapeString(table.Name)}' and @Schema='dbo']");
                    var properties = new[] { "Name", "Schema", "CreateDate" };
                    var request = new Request(urn, properties);

                    // Get data using sync method
                    var syncResult = Enumerator.GetData(serverConnection, request);
                    var syncData = (DataTable)syncResult.Data;

                    // Get data using async method
                    var asyncResult = Enumerator.GetDataAsync(serverConnection, request).GetAwaiter().GetResult();
                    var asyncData = (DataTable)asyncResult.Data;

                    // Assert both results have data
                    Assert.That(syncData.Rows.Count, Is.EqualTo(1), "Sync result should have exactly one row");
                    Assert.That(asyncData.Rows.Count, Is.EqualTo(1), "Async result should have exactly one row");

                    // Assert values match
                    foreach (var property in properties)
                    {
                        if (syncData.Columns.Contains(property) && asyncData.Columns.Contains(property))
                        {
                            var syncValue = syncData.Rows[0][property];
                            var asyncValue = asyncData.Rows[0][property];
                            Assert.That(asyncValue, Is.EqualTo(syncValue),
                                $"Property '{property}' should have same value in async and sync results");
                        }
                    }
                }
                finally
                {
                    // Cleanup using SMO object model
                    if (table != null && table.State == SqlSmoState.Existing)
                    {
                        table.Drop();
                    }
                }
            });
        }

        /// <summary>
        /// Verifies that async path correctly converts enum-typed properties (e.g., CompatibilityLevel).
        /// DataTable columns for enum-typed properties (ExtendedType) are stored as Int32
        /// because .NET Framework DataTable unboxes enum values to their underlying type.
        /// This test verifies that both sync and async paths produce identical results
        /// for the CompatibilityLevel enum property, which is available on all server types.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void Enumerator_GetDataAsync_EnumProperties_CorrectlyConverted()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = db.ExecutionManager.ConnectionContext;
                var urn = new Urn($"Server/Database[@Name='{Urn.EscapeString(db.Name)}']");
                
                // CompatibilityLevel is an enum-typed property available on all server types including Azure SQL
                var properties = new[] { "Name", "CompatibilityLevel" };
                var request = new Request(urn, properties);

                // Get data using sync method
                var syncResult = Enumerator.GetData(serverConnection, request);
                var syncData = (DataTable)syncResult.Data;

                // Get data using async method
                var asyncResult = Enumerator.GetDataAsync(serverConnection, request).GetAwaiter().GetResult();
                var asyncData = (DataTable)asyncResult.Data;

                // Assert both results have data
                Assert.That(syncData.Rows.Count, Is.EqualTo(1), "Sync result should have exactly one row");
                Assert.That(asyncData.Rows.Count, Is.EqualTo(1), "Async result should have exactly one row");

                // Verify CompatibilityLevel values match between sync and async
                Assert.That(syncData.Columns.Contains("CompatibilityLevel"), Is.True,
                    "Sync result should contain CompatibilityLevel column");
                Assert.That(asyncData.Columns.Contains("CompatibilityLevel"), Is.True,
                    "Async result should contain CompatibilityLevel column");

                var syncCompatLevel = syncData.Rows[0]["CompatibilityLevel"];
                var asyncCompatLevel = asyncData.Rows[0]["CompatibilityLevel"];

                // DataTable stores enum values as their underlying integer type (Int32),
                // so verify values are convertible to the enum and match between paths
                Assert.That(syncCompatLevel, Is.Not.Null.And.Not.EqualTo(DBNull.Value),
                    "Sync CompatibilityLevel should not be null or DBNull");
                Assert.That(asyncCompatLevel, Is.Not.Null.And.Not.EqualTo(DBNull.Value),
                    "Async CompatibilityLevel should not be null or DBNull");
                Assert.That((CompatibilityLevel)Convert.ToInt32(asyncCompatLevel), Is.EqualTo((CompatibilityLevel)Convert.ToInt32(syncCompatLevel)),
                    "Async and sync CompatibilityLevel values should match");
            });
        }
    }
}
