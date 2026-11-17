// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SfcUnitTests
{
    /// <summary>
    /// Unit tests for async methods on Enumerator, ExecuteSql, and Environment classes
    /// </summary>
    [TestClass]
    public class EnumeratorAsyncTests
    {
        /// <summary>
        /// Verify that EnumObject.GetDataAsync default implementation delegates to sync GetData
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task EnumObject_GetDataAsync_DefaultImplementation_DelegatesToSync()
        {
            // Create a test implementation of EnumObject
            var testEnumObject = new TestEnumObject();
            var erParent = new EnumResult();
            
            // Call GetDataAsync
            var result = await testEnumObject.GetDataAsync(erParent, CancellationToken.None);
            
            // Verify that it called the sync version
            Assert.IsNotNull(result, "GetDataAsync should return a result");
            Assert.IsTrue(testEnumObject.SyncGetDataWasCalled, "Default async implementation should call sync GetData");
        }

        /// <summary>
        /// Verify that ExecuteSql.ConnectAsync can be called without exceptions
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public async Task ExecuteSql_ConnectAsync_CanBeCalledWithoutExceptions()
        {
            // Create ExecuteSql with connection info
            var connInfo = new SqlConnectionInfo("localhost");
            var executeSql = new ExecuteSql(connInfo);
            
            // ConnectAsync should be callable - actual connection will fail in unit test environment
            // but we're verifying the method is accessible and callable
            try
            {
                await executeSql.ConnectAsync(CancellationToken.None);
            }
            catch (SqlException)
            {
                // Expected in unit test environment - connection will fail
                // The point is that the method is callable
            }
            catch (ConnectionFailureException)
            {
                // Also expected - ConnectionFailureException wraps connection errors
            }
            catch (Exception ex)
            {
                // If it's not a SqlException or ConnectionFailureException, it might be a different connection error
                // which is acceptable for this unit test
                Assert.IsTrue(ex.Message.Contains("connection") || ex.Message.Contains("network") || ex.Message.Contains("server"),
                    $"Expected connection-related exception, got: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Verify that Enumerator.GetDataAsync is a public method and can be accessed
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void Enumerator_GetDataAsync_IsPublicStaticMethod()
        {
            // Create a simple request
            var request = new Request();
            var connInfo = new SqlConnectionInfo("localhost");
            
            // Verify GetDataAsync is accessible as a public static method
            // We're not executing it here (that's for integration tests)
            // Just verifying it exists and is public
            var task = Enumerator.GetDataAsync(connInfo, request, CancellationToken.None);
            Assert.IsNotNull(task, "GetDataAsync should return a non-null Task");
            
            // Cancel the task immediately since we're not actually executing
            task.ContinueWith(_ => { }, TaskContinuationOptions.ExecuteSynchronously);
        }

        /// <summary>
        /// Verify that ExecuteSql InitConnection accepts various connection types
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ExecuteSql_InitConnection_AcceptsServerConnection()
        {
            // Test with ServerConnection - use SqlConnectionInfo instead of mocking SqlConnection
            var connInfo = new SqlConnectionInfo("localhost");
            var serverConnection = new ServerConnection(connInfo);
            
            // Should not throw
            var executeSql = new ExecuteSql(serverConnection);
            Assert.IsNotNull(executeSql, "ExecuteSql should be created with ServerConnection");
        }

        /// <summary>
        /// Verify that ExecuteSql InitConnection accepts SqlConnectionInfo
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ExecuteSql_InitConnection_AcceptsSqlConnectionInfo()
        {
            // Test with SqlConnectionInfo
            var connInfo = new SqlConnectionInfo("localhost");
            
            // Should not throw
            var executeSql = new ExecuteSql(connInfo);
            Assert.IsNotNull(executeSql, "ExecuteSql should be created with SqlConnectionInfo");
        }

        /// <summary>
        /// Verify that ExecuteSql InitConnection throws for invalid connection type
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void ExecuteSql_InitConnection_ThrowsForInvalidType()
        {
            // Test with invalid connection type
            Assert.Throws<InternalEnumeratorException>(() =>
            {
                var executeSql = new ExecuteSql("invalid connection");
            }, "ExecuteSql should throw InternalEnumeratorException for invalid connection type");
        }

        /// <summary>
        /// Test implementation of EnumObject for testing default async behavior
        /// </summary>
        private class TestEnumObject : EnumObject
        {
            public bool SyncGetDataWasCalled { get; private set; }

            public override EnumResult GetData(EnumResult erParent)
            {
                SyncGetDataWasCalled = true;
                return erParent;
            }

            public override ResultType[] ResultTypes => new[] { ResultType.DataTable };
        }
    }
}
