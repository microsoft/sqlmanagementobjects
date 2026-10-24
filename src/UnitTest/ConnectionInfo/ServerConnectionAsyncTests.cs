// Copyright (c) Microsoft.
// Licensed under the MIT license.
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assert=NUnit.Framework.Assert;

namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    /// <summary>
    /// Unit tests for ServerConnection async methods.
    /// These tests verify the async API surface without requiring a real SQL Server connection.
    /// </summary>
    [TestClass]
    public class ServerConnectionAsyncTests
    {
#if !NATIVEBATCHPARSER
        /// <summary>
        /// Verifies that ExecuteNonQueryAsync with a single command respects CapturedSql mode
        /// and does not execute the command when in capture mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteNonQueryAsync_CapturedSqlMode_CapturesWithoutExecuting()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var testCommand = "SELECT 1";

            // Act
            var result = await serverConnection.ExecuteNonQueryAsync(testCommand).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.EqualTo(0), "Expected 0 rows affected in capture mode");
            Assert.That(serverConnection.CapturedSql.Text.Contains(testCommand), Is.True, 
                "Expected command to be captured in CapturedSql");
        }
#endif


        /// <summary>
        /// Verifies that ExecuteScalarAsync respects CapturedSql mode
        /// and does not execute the command when in capture mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteScalarAsync_CapturedSqlMode_CapturesWithoutExecuting()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var testCommand = "SELECT @@VERSION";

            // Act
            var result = await serverConnection.ExecuteScalarAsync(testCommand).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.Null, "Expected null result in capture mode");
            Assert.That(serverConnection.CapturedSql.Text.Contains(testCommand), Is.True, 
                "Expected command to be captured in CapturedSql");
        }

        /// <summary>
        /// Verifies that ExecuteWithResultsAsync respects CapturedSql mode
        /// and does not execute the command when in capture mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteWithResultsAsync_CapturedSqlMode_CapturesWithoutExecuting()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var testCommand = "SELECT 1 AS TestColumn";

            // Act
            var result = await serverConnection.ExecuteWithResultsAsync(testCommand).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.Not.Null, "Expected non-null DataTable in capture mode");
            Assert.That(result.Rows.Count, Is.EqualTo(0), "Expected empty DataTable in capture mode");
            Assert.That(serverConnection.CapturedSql.Text.Contains(testCommand), Is.True, 
                "Expected command to be captured in CapturedSql");
        }

        /// <summary>
        /// Verifies that ExecuteReaderAsync respects CapturedSql mode
        /// and does not execute the command when in capture mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteReaderAsync_CapturedSqlMode_CapturesWithoutExecuting()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var testCommand = "SELECT 1 AS TestColumn";

            // Act
            var result = await serverConnection.ExecuteReaderAsync(testCommand).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.Null, "Expected null reader in capture mode");
            Assert.That(serverConnection.CapturedSql.Text.Contains(testCommand), Is.True, 
                "Expected command to be captured in CapturedSql");
        }

#if !NATIVEBATCHPARSER
        /// <summary>
        /// Verifies that ExecuteNonQueryAsync with IEnumerable overload processes commands in capture mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteNonQueryAsync_BatchCommands_CapturesInCaptureMode()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var commands = new List<string> { "SELECT 1", "SELECT 2", "SELECT 3" };

            // Act
            var result = await serverConnection.ExecuteNonQueryAsync(commands).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.EqualTo(0), "Expected 0 rows affected in capture mode");
            foreach (var cmd in commands)
            {
                Assert.That(serverConnection.CapturedSql.Text.Contains(cmd), Is.True, 
                    $"Expected command '{cmd}' to be captured in CapturedSql");
            }
        }
#endif


        /// <summary>
        /// Verifies that ExecuteWithResultsAsync with IEnumerable overload processes commands in capture mode.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteWithResultsAsync_BatchCommands_CapturesInCaptureMode()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var commands = new List<string> { "SELECT 1 AS Col1", "SELECT 2 AS Col2" };

            // Act
            var result = await serverConnection.ExecuteWithResultsAsync(commands).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.Not.Null, "Expected non-null DataTable in capture mode");
            Assert.That(result.Rows.Count, Is.EqualTo(0), "Expected empty DataTable in capture mode");
            foreach (var cmd in commands)
            {
                Assert.That(serverConnection.CapturedSql.Text.Contains(cmd), Is.True, 
                    $"Expected command '{cmd}' to be captured in CapturedSql");
            }
        }

#if !NATIVEBATCHPARSER
        /// <summary>
        /// Verifies that a cancelled CancellationToken can be passed to ExecuteNonQueryAsync
        /// and appropriate exception handling occurs (though actual cancellation requires connection).
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteNonQueryAsync_CancelledToken_HandledGracefully()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql; // Use capture mode to avoid actual connection
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Pre-cancel the token
            var testCommand = "SELECT 1";

            // Act & Assert
            // In capture mode, no actual async operation occurs, so cancellation won't throw
            // This test verifies the API accepts a CancellationToken parameter
            var result = await serverConnection.ExecuteNonQueryAsync(testCommand, cts.Token).ConfigureAwait(false);
            Assert.That(result, Is.EqualTo(0), "Expected 0 rows affected in capture mode even with cancelled token");
        }
#endif

#if !NATIVEBATCHPARSER
        /// <summary>
        /// Verifies that ExecuteNonQueryAsync throws ArgumentNullException for null command string.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ServerConnection_ExecuteNonQueryAsync_NullCommand_ThrowsException()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));

            // Act & Assert
            // Null commands should throw ArgumentNullException, consistent with sync behavior
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await serverConnection.ExecuteNonQueryAsync((string)null).ConfigureAwait(false),
                "ExecuteNonQueryAsync should throw ArgumentNullException for null command");
        }
#endif

#if !NATIVEBATCHPARSER
        /// <summary>
        /// Verifies that ExecuteNonQueryAsync with empty IEnumerable returns 0.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ServerConnection_ExecuteNonQueryAsync_EmptyBatch_Returns0()
        {
            // Arrange
            var connectionString = "Data Source=localhost;Initial Catalog=tempdb;Integrated Security=true;";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            serverConnection.SqlExecutionModes = SqlExecutionModes.CaptureSql;
            var commands = new List<string>();

            // Act
            var result = await serverConnection.ExecuteNonQueryAsync(commands).ConfigureAwait(false);

            // Assert
            Assert.That(result, Is.EqualTo(0), "Expected 0 rows affected for empty batch");
        }
#endif
    }
}
