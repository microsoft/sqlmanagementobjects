// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Functional tests for ServerConnection async methods.
    /// These tests require a real SQL Server connection.
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ServerConnectionAsyncTests : SqlTestBase
    {
        /// <summary>
        /// Creates a new <see cref="ServerConnection"/> targeting the specified database,
        /// independent from the pooled connection used by the SMO object model.
        /// The caller must dispose the returned <paramref name="sqlConnection"/> after
        /// disconnecting the <see cref="ServerConnection"/>.
        /// </summary>
        private ServerConnection CreateServerConnection(Database db, out SqlConnection sqlConnection)
        {
            var connStr = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ToString())
            {
                InitialCatalog = db.Name,
                Pooling = false
            };
            sqlConnection = new SqlConnection(connStr.ToString());
            return new ServerConnection(sqlConnection);
        }

        /// <summary>
        /// Verifies that ExecuteNonQueryAsync can execute a simple command and return row count.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteNonQueryAsync_SingleCommand_ReturnsRowCount()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Value", DataType.NVarChar(50)));
                table.Create();

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Insert some rows
                    var rowsAffected = serverConnection.ExecuteNonQueryAsync(
                        $"INSERT INTO [{table.Name}] VALUES (1, 'Test1'), (2, 'Test2'), (3, 'Test3')").GetAwaiter().GetResult();

                    // Assert
                    Assert.That(rowsAffected, Is.EqualTo(3), "Expected 3 rows to be affected by INSERT");
                }
                finally
                {
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                    table.Drop();
                }
            });
        }

        /// <summary>
        /// Verifies that ExecuteNonQueryAsync with a batch of commands executes them sequentially.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteNonQueryAsync_BatchCommands_ExecutesSequentially()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Value", DataType.NVarChar(50)));
                table.Create();

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Insert data in batch
                    var commands = new List<string>
                    {
                        $"INSERT INTO [{table.Name}] VALUES (1, 'First')",
                        $"INSERT INTO [{table.Name}] VALUES (2, 'Second')",
                        $"INSERT INTO [{table.Name}] VALUES (3, 'Third')"
                    };

                    var totalAffected = serverConnection.ExecuteNonQueryAsync(commands).GetAwaiter().GetResult();

                    // Verify data was inserted
                    var dataTable = serverConnection.ExecuteWithResultsAsync(
                        $"SELECT COUNT(*) AS TotalRows FROM [{table.Name}]").GetAwaiter().GetResult();
                    var rowCount = Convert.ToInt32(dataTable.Rows[0]["TotalRows"]);

                    // Assert
                    Assert.That(rowCount, Is.EqualTo(3), "Expected 3 rows in the table");
                    Assert.That(totalAffected, Is.EqualTo(3), "Expected 3 total rows affected by INSERT commands");
                }
                finally
                {
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                    table.Drop();
                }
            });
        }

        /// <summary>
        /// Verifies that ExecuteWithResultsAsync returns a properly populated DataTable.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteWithResultsAsync_ReturnsDataTable()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Name", DataType.NVarChar(50)));
                table.Create();

                // Populate test data via SMO
                db.ExecuteNonQuery($"INSERT INTO [{table.Name}] VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie')");

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Query data
                    var dataTable = serverConnection.ExecuteWithResultsAsync(
                        $"SELECT Id, Name FROM [{table.Name}] ORDER BY Id").GetAwaiter().GetResult();

                    // Assert
                    Assert.That(dataTable, Is.Not.Null, "DataTable should not be null");
                    Assert.That(dataTable.Rows.Count, Is.EqualTo(3), "Expected 3 rows in result");
                    Assert.That(dataTable.Columns.Count, Is.EqualTo(2), "Expected 2 columns in result");
                    Assert.That(dataTable.Rows[0]["Name"].ToString(), Is.EqualTo("Alice"), "First row should be Alice");
                    Assert.That(dataTable.Rows[1]["Name"].ToString(), Is.EqualTo("Bob"), "Second row should be Bob");
                    Assert.That(dataTable.Rows[2]["Name"].ToString(), Is.EqualTo("Charlie"), "Third row should be Charlie");
                }
                finally
                {
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                    table.Drop();
                }
            });
        }

        /// <summary>
        /// Verifies that ExecuteScalarAsync returns the correct scalar value.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteScalarAsync_ReturnsScalarValue()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Execute scalar query
                    var result = serverConnection.ExecuteScalarAsync("SELECT 42").GetAwaiter().GetResult();

                    // Assert
                    Assert.That(result, Is.Not.Null, "Scalar result should not be null");
                    Assert.That(Convert.ToInt32(result), Is.EqualTo(42), "Expected scalar value to be 42");
                }
                finally
                {
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                }
            });
        }

        /// <summary>
        /// Verifies that ExecuteReaderAsync returns an open SqlDataReader.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteReaderAsync_ReturnsOpenReader()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Value", DataType.NVarChar(50)));
                table.Create();

                // Populate test data via SMO
                db.ExecuteNonQuery($"INSERT INTO [{table.Name}] VALUES (1, 'Data1'), (2, 'Data2')");

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Execute reader
                    using (var reader = serverConnection.ExecuteReaderAsync(
                        $"SELECT Id, Value FROM [{table.Name}] ORDER BY Id").GetAwaiter().GetResult())
                    {
                        // Assert
                        Assert.That(reader, Is.Not.Null, "Reader should not be null");
                        Assert.That(reader.IsClosed, Is.False, "Reader should be open");

                        // Read first row
                        var hasRows = reader.ReadAsync().GetAwaiter().GetResult();
                        Assert.That(hasRows, Is.True, "Reader should have at least one row");
                        Assert.That(reader.GetInt32(0), Is.EqualTo(1), "First row Id should be 1");
                        Assert.That(reader.GetString(1), Is.EqualTo("Data1"), "First row Value should be 'Data1'");

                        // Read second row
                        hasRows = reader.ReadAsync().GetAwaiter().GetResult();
                        Assert.That(hasRows, Is.True, "Reader should have a second row");
                        Assert.That(reader.GetInt32(0), Is.EqualTo(2), "Second row Id should be 2");
                    }
                }
                finally
                {
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                    table.Drop();
                }
            });
        }

        /// <summary>
        /// Verifies that cancellation token can cancel a long-running query.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteNonQueryAsync_CancellationToken_CancelsLongRunningQuery()
        {
            ExecuteFromDbPool((db) =>
            {
                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                var cts = new CancellationTokenSource();

                try
                {
                    // Schedule cancellation after 500ms
                    cts.CancelAfter(500);

                    // This query uses WAITFOR to simulate a long-running operation
                    var longRunningQuery = "WAITFOR DELAY '00:00:10'"; // 10 second wait

                    // Assert that the query is cancelled
                    try
                    {
                        serverConnection.ExecuteNonQueryAsync(longRunningQuery, cts.Token).GetAwaiter().GetResult();
                        Assert.Fail("Expected OperationCanceledException or ExecutionFailureException to be thrown");
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected - cancellation was successful
                        Trace.TraceInformation("Query was successfully cancelled via CancellationToken");
                    }
                    catch (ExecutionFailureException ex)
                    {
                        // Also acceptable - SQL Server may report the cancellation as an execution failure
                        Trace.TraceInformation($"Query cancellation resulted in ExecutionFailureException: {ex.Message}");
                    }
                }
                finally
                {
                    cts.Dispose();
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                }
            });
        }

        /// <summary>
        /// Verifies that cancellation mid-batch stops execution of remaining commands.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public void ServerConnection_ExecuteNonQueryAsync_CancellationDuringBatch_ThrowsAndStops()
        {
            ExecuteFromDbPool((db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Timestamp", DataType.DateTime));
                table.Create();

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                var cts = new CancellationTokenSource();

                try
                {
                    // Batch with a long-running command in the middle
                    var commands = new List<string>
                    {
                        $"INSERT INTO [{table.Name}] VALUES (1, GETDATE())",
                        "WAITFOR DELAY '00:00:10'", // This will be cancelled
                        $"INSERT INTO [{table.Name}] VALUES (2, GETDATE())" // This should not execute
                    };

                    // Schedule cancellation
                    cts.CancelAfter(500);

                    // Execute batch with cancellation
                    try
                    {
                        serverConnection.ExecuteNonQueryAsync(commands, cts.Token).GetAwaiter().GetResult();
                        Assert.Fail("Expected cancellation exception");
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                    catch (ExecutionFailureException)
                    {
                        // Also acceptable
                    }

                    // Verify that only the first command executed using a separate connection
                    // since the cancelled connection may be in a bad state
                    var verifyConnection = CreateServerConnection(db, out var verifySqlConnection);
                    try
                    {
                        var dataTable = verifyConnection.ExecuteWithResultsAsync(
                            $"SELECT COUNT(*) AS TotalRows FROM [{table.Name}]").GetAwaiter().GetResult();
                        var rowCount = Convert.ToInt32(dataTable.Rows[0]["TotalRows"]);

                        // The third INSERT (after WAITFOR) must not have executed.
                        // The first INSERT may or may not persist depending on whether the
                        // cancellation rolled back the implicit transaction, so 0 or 1 is valid.
                        Assert.That(rowCount, Is.LessThanOrEqualTo(1),
                            "Expected at most 1 row (the third INSERT should not have executed after cancellation)");
                    }
                    finally
                    {
                        verifyConnection.Disconnect();
                        verifySqlConnection.Dispose();
                    }
                }
                finally
                {
                    cts.Dispose();
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                    table.Drop();
                }
            });
        }
    }
}
