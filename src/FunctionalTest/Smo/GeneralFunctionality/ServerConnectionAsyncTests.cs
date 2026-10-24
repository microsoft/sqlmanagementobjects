// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
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
        /// Verifies that ConnectAsync opens the SQL connection under the configured Windows identity.
        /// This test requires a specifically named generic credential to be available in the local windows credential manager for 
        /// impersonation. The credential has to be for a user with interactive logon rights and who has integrated security access
        /// to the test server. 
        /// Create a Generic Credential named smotests. The user name should be in either "domain\user" format or SPN "user@domain" format.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = "Windows", MaxMajor = 15, MinMajor = 15)]
        public async Task ServerConnection_ConnectAsync_WithConnectAsUser_UsesImpersonatedIdentity()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
            {
                if (!TryReadImpersonationCredential(out var userName, out var password))
                {
                    Trace.TraceInformation("smotests credential not found for impersonation, skipping test");
                    return;
                }

                var serverConnection = new ServerConnection(ServerContext.ConnectionContext.ServerInstance)
                {
                    ConnectAsUser = true,
                    ConnectAsUserName = userName,
                    ConnectAsUserPassword = password,
                    NonPooledConnection = true
                };

                try
                {
                    await serverConnection.ConnectAsync().ConfigureAwait(false);
                    var actualUserName = (string)await serverConnection.ExecuteScalarAsync("SELECT SUSER_SNAME()").ConfigureAwait(false);

                    Assert.That(actualUserName, Is.EqualTo(userName).IgnoreCase,
                        "SUSER_SNAME() should match the identity configured by ConnectAsUserName");
                }
                finally
                {
                    serverConnection.Disconnect();
                }
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies that ExecuteNonQueryAsync can execute a simple command and return row count.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 11)]
        public async Task ServerConnection_ExecuteNonQueryAsync_SingleCommand_ReturnsRowCount()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Value", DataType.NVarChar(50)));
                table.Create();

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Insert some rows
                    var rowsAffected = await serverConnection.ExecuteNonQueryAsync(
                        $"INSERT INTO [{table.Name}] VALUES (1, 'Test1'), (2, 'Test2'), (3, 'Test3')");

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
        public async Task ServerConnection_ExecuteNonQueryAsync_BatchCommands_ExecutesSequentially()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
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

                    var totalAffected = await serverConnection.ExecuteNonQueryAsync(commands);

                    // Verify data was inserted
                    var dataTable = await serverConnection.ExecuteWithResultsAsync(
                        $"SELECT COUNT(*) AS TotalRows FROM [{table.Name}]");
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
        public async Task ServerConnection_ExecuteWithResultsAsync_ReturnsDataTable()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
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
                    var dataTable = await serverConnection.ExecuteWithResultsAsync(
                        $"SELECT Id, Name FROM [{table.Name}] ORDER BY Id");

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
        public async Task ServerConnection_ExecuteScalarAsync_ReturnsScalarValue()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
            {
                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                try
                {
                    // Execute scalar query
                    var result = await serverConnection.ExecuteScalarAsync("SELECT 42");

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
        public async Task ServerConnection_ExecuteReaderAsync_ReturnsOpenReader()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
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
                    using var reader = await serverConnection.ExecuteReaderAsync(
                        $"SELECT Id, Value FROM [{table.Name}] ORDER BY Id");

                    // Assert
                    Assert.That(reader, Is.Not.Null, "Reader should not be null");
                    Assert.That(reader.IsClosed, Is.False, "Reader should be open");

                    // Read first row
                    var hasRows = await reader.ReadAsync();
                    Assert.That(hasRows, Is.True, "Reader should have at least one row");
                    Assert.That(reader.GetInt32(0), Is.EqualTo(1), "First row Id should be 1");
                    Assert.That(reader.GetString(1), Is.EqualTo("Data1"), "First row Value should be 'Data1'");

                    // Read second row
                    hasRows = await reader.ReadAsync();
                    Assert.That(hasRows, Is.True, "Reader should have a second row");
                    Assert.That(reader.GetInt32(0), Is.EqualTo(2), "Second row Id should be 2");
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
        public async Task ServerConnection_ExecuteNonQueryAsync_CancellationToken_CancelsLongRunningQuery()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
            {
                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                using var cts = new CancellationTokenSource();

                try
                {
                    // Schedule cancellation after 500ms
                    cts.CancelAfter(500);

                    // This query uses WAITFOR to simulate a long-running operation
                    var longRunningQuery = "WAITFOR DELAY '00:00:10'"; // 10 second wait

                    // Assert that the query is cancelled
                    try
                    {
                        await serverConnection.ExecuteNonQueryAsync(longRunningQuery, cts.Token);
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
        public async Task ServerConnection_ExecuteNonQueryAsync_CancellationDuringBatch_ThrowsAndStops()
        {
            await ExecuteFromDbPoolAsync(async (db) =>
            {
                var table = new Table(db, $"TestTable_{Guid.NewGuid():N}");
                table.Columns.Add(new Column(table, "Id", DataType.Int));
                table.Columns.Add(new Column(table, "Timestamp", DataType.DateTime));
                table.Create();

                var serverConnection = CreateServerConnection(db, out var sqlConnection);
                using var cts = new CancellationTokenSource();

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
                        await serverConnection.ExecuteNonQueryAsync(commands, cts.Token);
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
                        var dataTable = await verifyConnection.ExecuteWithResultsAsync(
                            $"SELECT COUNT(*) AS TotalRows FROM [{table.Name}]");
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
                    serverConnection.Disconnect();
                    sqlConnection.Dispose();
                    table.Drop();
                }
            });
        }

        private static bool TryReadImpersonationCredential(out string userName, out string password)
        {
            userName = null;
            password = null;

            if (!CredRead("smotests", CredentialType.Generic, 0, out var credentialPointer))
            {
                return false;
            }

            try
            {
                var credential = Marshal.PtrToStructure<Credential>(credentialPointer);
                userName = credential.UserName;
                password = credential.CredentialBlob == IntPtr.Zero
                    ? string.Empty
                    : Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / 2);
                return !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password);
            }
            finally
            {
                CredFree(credentialPointer);
            }
        }

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, CredentialType type, int reservedFlag, out IntPtr credentialPointer);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        private static extern bool CredFree(IntPtr credential);

        private enum CredentialType
        {
            Generic = 1
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public uint Flags;
            public CredentialType Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public string UserName;
        }
    }
}
