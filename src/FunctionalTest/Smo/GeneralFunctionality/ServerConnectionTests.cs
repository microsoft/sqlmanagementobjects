// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Linq;
using System.Security;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.ConstrainedExecution;
using System.Diagnostics;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    /// <summary>
    /// Tests for the various classes in ConnectionInfo that require a live connection
    /// Tests that don't need a live connection should go into the unit tests
    /// </summary>
    [TestClass]
    public class ServerConnectionTests : SqlTestBase
    {
        [TestMethod]
        public void ServerConnection_server_information_properties_match_connected_server()
        {
            ExecuteTest(() =>
            {
                var connectionString = new SqlConnectionStringBuilder(ServerContext.ConnectionContext.ConnectionString)
                {
                    Pooling = false
                };

                using (var sqlConnection = new SqlConnection(connectionString.ConnectionString))
                {
                    sqlConnection.Open();
                    var serverConnection = new ServerConnection(sqlConnection);
                    var expectedValues = serverConnection.ExecuteWithResults(
                        @"DECLARE @edition sysname = CONVERT(sysname, SERVERPROPERTY(N'Edition'));
SELECT @edition AS Edition,
    CONVERT(bigint, SERVERPROPERTY(N'EditionID')) AS EditionID,
       CONVERT(int, SERVERPROPERTY(N'EngineEdition')) AS DatabaseEngineEdition,
       CONVERT(nvarchar(128), SERVERPROPERTY(N'ProductVersion')) AS ProductVersion,
       CONVERT(sysname, SERVERPROPERTY(N'Collation')) AS Collation,
       @@MICROSOFTVERSION AS MicrosoftVersion,
       CASE WHEN SERVERPROPERTY(N'EngineEdition') = 12 THEN 1
            WHEN SERVERPROPERTY(N'EngineEdition') = 11 AND @@VERSION LIKE N'Microsoft Azure SQL Data Warehouse%' THEN 1
            ELSE 0 END AS IsFabricServer;").Tables[0].Rows[0];

                    var expectedEdition = Convert.ToString(expectedValues["Edition"]);
                    var expectedEngineType = expectedEdition == "SQL Azure"
                        ? DatabaseEngineType.SqlAzureDatabase
                        : DatabaseEngineType.Standalone;
                    var expectedEngineEdition = (DatabaseEngineEdition)Convert.ToInt32(expectedValues["DatabaseEngineEdition"]);
                    const DatabaseEngineEdition dynamicsCrmEdition = (DatabaseEngineEdition)1000;
                    if (expectedEngineType == DatabaseEngineType.SqlAzureDatabase &&
                        !Enum.IsDefined(typeof(DatabaseEngineEdition), expectedEngineEdition) &&
                        expectedEngineEdition != dynamicsCrmEdition)
                    {
                        expectedEngineEdition = DatabaseEngineEdition.SqlDatabase;
                    }

                    var connectionVersion = new Version(sqlConnection.ServerVersion);
                    var expectedServerVersion = new ServerVersion(connectionVersion.Major, connectionVersion.Minor, connectionVersion.Build);
                    if (expectedEngineEdition == DatabaseEngineEdition.SqlManagedInstance ||
                        expectedEngineEdition == DatabaseEngineEdition.SqlOnDemand)
                    {
                        var microsoftVersion = Convert.ToUInt32(expectedValues["MicrosoftVersion"]);
                        expectedServerVersion = new ServerVersion(
                            (int)(microsoftVersion / 0x01000000),
                            (int)(microsoftVersion / 0x010000 & 15),
                            (int)microsoftVersion & 255);
                    }

                    if (expectedEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                    {
                        expectedEngineType = DatabaseEngineType.Standalone;
                    }

                    var expectedHostPlatform = connectionVersion.Major >= 14
                        ? Convert.ToString(serverConnection.ExecuteScalar("SELECT host_platform FROM sys.dm_os_host_info"))
                        : HostPlatformNames.Windows;
                    var expectedConnectionProtocol = NetworkProtocol.TcpIp;
                    if (expectedEdition != "SQL Azure")
                    {
                        var netTransport = Convert.ToString(serverConnection.ExecuteScalar("SELECT CONVERT(nvarchar(40), CONNECTIONPROPERTY('net_transport'))"));
                        switch (netTransport.ToLowerInvariant())
                        {
                            case "named pipe":
                                expectedConnectionProtocol = NetworkProtocol.NamedPipes;
                                break;
                            case "shared memory":
                                expectedConnectionProtocol = NetworkProtocol.SharedMemory;
                                break;
                            case "via":
                                expectedConnectionProtocol = NetworkProtocol.Via;
                                break;
                            case "tcp":
                            case "http":
                            case "ssl":
                                expectedConnectionProtocol = NetworkProtocol.TcpIp;
                                break;
                            default:
                                expectedConnectionProtocol = NetworkProtocol.NotSpecified;
                                break;
                        }
                    }

                    Assert.That(serverConnection.Edition, Is.EqualTo(expectedEdition), "Unexpected Edition");
                    Assert.That(serverConnection.EditionID, Is.EqualTo(Convert.ToInt64(expectedValues["EditionID"])), "Unexpected EditionID");
                    Assert.That(serverConnection.ProductVersion, Is.EqualTo(new Version(Convert.ToString(expectedValues["ProductVersion"]))), "Unexpected ProductVersion");
                    Assert.That(serverConnection.DatabaseEngineType, Is.EqualTo(expectedEngineType), "Unexpected DatabaseEngineType");
                    Assert.That(serverConnection.DatabaseEngineEdition, Is.EqualTo(expectedEngineEdition), "Unexpected DatabaseEngineEdition");
                    Assert.That(serverConnection.ServerVersion.ToString(), Is.EqualTo(expectedServerVersion.ToString()), "Unexpected ServerVersion");
                    Assert.That(serverConnection.HostPlatform, Is.EqualTo(expectedHostPlatform), "Unexpected HostPlatform");
                    Assert.That(serverConnection.ConnectionProtocol, Is.EqualTo(expectedConnectionProtocol), "Unexpected ConnectionProtocol");
                    Assert.That(serverConnection.IsFabricServer, Is.EqualTo(Convert.ToBoolean(expectedValues["IsFabricServer"])), "Unexpected IsFabricServer");
                    Assert.That(serverConnection.Collation, Is.EqualTo(Convert.ToString(expectedValues["Collation"])), "Unexpected Collation");
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDatabase)]
        public void GetDatabaseConnection_uses_SqlCredential_from_SqlConnection()
        {
            ExecuteWithDbDrop((db) =>
            {
                if (SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.NotSpecified && SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.SqlPassword)
                {
                    Trace.TraceWarning($"Skipping SqlCredential test on {SqlConnectionStringBuilder.DataSource} because SQL logins are not available");
                    return;
                }
                var pwd = this.SqlConnectionStringBuilder.Password;
                var secureString = new SecureString();
                foreach (var c in pwd)
                {
                    secureString.AppendChar(c);
                }
                secureString.MakeReadOnly();
                using (
                    var sqlConnection =
                        new SqlConnection(
                            string.Format("Pooling=false;data source={0}", this.SqlConnectionStringBuilder.DataSource),
                            new SqlCredential(this.SqlConnectionStringBuilder.UserID, secureString)))
                {
                    var masterConnection = new ServerConnection(sqlConnection);
                    var dbConnection = masterConnection.GetDatabaseConnection(db.Name, poolConnection: false);
                    object retval = 0;
                    // the credential isn't preserved directly
                    Assert.That(dbConnection.SqlConnectionObject.Credential, Is.Null,
                        "dbscoped connection Credential");
                    var dbString = new SqlConnectionStringBuilder(dbConnection.SqlConnectionObject.ConnectionString);
                    Assert.That(dbString.UserID,
                        Is.EqualTo(this.SqlConnectionStringBuilder.UserID), "dbscoped connection UserID");
                    Assert.DoesNotThrow(() => retval = dbConnection.ExecuteScalar("select 1"));
                    Assert.That((int)retval, Is.EqualTo(1), "select 1");
                    Assert.That(dbConnection.SqlConnectionObject.Database, Is.EqualTo(db.Name),
                        "dbConnection.SqlConnectionObject.Database");
                }
            });
        }

        class ThreadData
        {
            public ManualResetEvent waitHandle;
            public Exception exception;
            public string dbName;
            public CountdownEvent completeHandle;
        }

        /// <summary>
        /// Regression test for Defect 12574078:Azure database connections in SMO are not multi-thread safe
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Databases_enumeration_is_thread_safe_on_Azure()
        {
            ExecuteWithDbDrop((db) =>
            {
                var threads = new List<ThreadData>();
                var startHandle = new ManualResetEvent(false);
                var finishHandle = new CountdownEvent(10);
                try
                {

                    for (int i = 0; i < 10; ++i)
                    {
                        var threadData = new ThreadData()
                        {
                            waitHandle = startHandle,
                            dbName = db.Name,
                            completeHandle = finishHandle
                        };
                        var thread = new Thread(DatabaseEnumerationThread);
                        thread.Start(threadData);
                        threads.Add(threadData);
                    }
                    startHandle.Set();
                    finishHandle.Wait();
                    Assert.That(threads.Select(t => t.exception), Is.EquivalentTo(new Exception[10]), "No exception should be thrown");
                }
                finally
                {
                    startHandle.Close();
                    foreach (var thread in threads)
                    {
                        thread.waitHandle.Close();
                    }
                }
            });
        }

        void DatabaseEnumerationThread(object data)
        {
            var threadData = (ThreadData)data;

            threadData.waitHandle.WaitOne();
            try
            {
                for (var i = 0; i < 10; ++i)
                {
                    var connectionString = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ConnectionString);
                    connectionString.InitialCatalog = threadData.dbName;
                    using (var sqlConnection = new SqlConnection(connectionString.ConnectionString))
                    {
                        var serverConnection = new ServerConnection(sqlConnection);
                        var server = new Microsoft.SqlServer.Management.Smo.Server(serverConnection);
                        Database db = null;
                        Assert.DoesNotThrow(() => { db = server.Databases[threadData.dbName]; },
                            "Databases enumeration shouldn't throw");
                    }
                }
            }
            catch (Exception e)
            {
                threadData.exception = e;
            }
            threadData.completeHandle.Signal();
        }

        /// <summary>
        /// This test requires a specifically named generic credential to be available in the local windows credential manager for 
        /// impersonation. The credential has to be for a user with interactive logon rights and who has integrated security access
        /// to the test server. 
        /// Create a Generic Credential named smotests. The user name should be in either "domain\user" format or SPN "user@domain" format.
        /// </summary>
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = "Windows", MaxMajor = 15, MinMajor = 15)]
        [TestMethod]
        public void ConnectAsUser_succeeds_with_impersonation()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }
            ExecuteTest(() =>
            {
                string userName = null;
                string password = null;

                if (CredRead("smotests", CRED_TYPE.GENERIC, 0, out IntPtr pCredential))
                {
                    try
                    {
                        var credential = (Credential)Marshal.PtrToStructure(pCredential, typeof(Credential));
                        // CredentialBlob can be null somehow, perhaps if the user tried to edit it externally.
                        password = credential.CredentialBlob == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(credential.CredentialBlob,
                           (int)credential.CredentialBlobSize / 2);
                        userName = credential.UserName;
                    }
                    finally
                    {
                        CredFree(pCredential);
                    }
                }
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
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
                serverConnection.Connect();
                var server = new Management.Smo.Server(serverConnection);
                Assert.That(server.Databases.Count, Is.AtLeast(1), "impersonated user can fetch databases");
                var actualUserName = (string)server.ExecutionManager.ExecuteScalar("select SUSER_SNAME()");
                Assert.That(actualUserName.ToUpperInvariant(), Is.EqualTo(userName.ToUpperInvariant()), "TSQL SUSER_SNAME() should match impersonated user");
            });
        }

        /// <summary>
        /// This test case checks if the Server Name (ServerInstance) property is initialized with the correct hostname.
        /// </summary>
        [TestMethod]
        public void GetDatabaseConnection_Initializes_Server_Name()
        {
            ExecuteFromDbPool((db) =>
            {
                var server = new Management.Smo.Server(ServerContext.ConnectionContext.GetDatabaseConnection(db.Name, poolConnection: false));
                var expected = ServerContext.ConnectionContext.ServerInstance == "." ? Environment.MachineName : ServerContext.ConnectionContext.ServerInstance;
                Assert.That(server.Name, Is.EqualTo(expected),
                    "Server.Name was not initialzed correctly, check DataSource property of connection string in ConnectionFactory.CreateServerConnection() in ServerConnection.cs");
            });

        }

        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr CredentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredWrite([In] ref Credential userCredential, [In] UInt32 flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        static extern bool CredFree([In] IntPtr cred);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredDelete(
            string targetName,
            CRED_TYPE type,
            int flags
            );

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredEnumerate(
            string targetName,
            int flags,
            [Out] out int count,
            [Out] out IntPtr pCredential
            );

        enum CRED_TYPE
        {
            GENERIC = 1,
            DOMAIN_PASSWORD = 2,
            DOMAIN_CERTIFICATE = 3,
            DOMAIN_VISIBLE_PASSWORD = 4
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public UInt32 Flags;
            public CRED_TYPE Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public UInt32 CredentialBlobSize;
            public IntPtr CredentialBlob;
            public UInt32 Persist;
            public UInt32 AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public string UserName;
        }

        [TestMethod]
        public void ServerConnection_SqlExecutionMode_is_preserved_after_lazy_fetch_queries()
        {
            ExecuteFromDbPool((db) =>
            {
                var conn = new SqlConnection(ServerContext.ConnectionContext.ConnectionString);
                var server = new Management.Smo.Server(new ServerConnection(conn));
                server.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                db = server.Databases[db.Name];
                // It's important to trigger a full property dictionary lazy fetch to reproduce the issue being tested
                var temp = !db.IsSupportedProperty(nameof(db.IsDatabaseSnapshot)) || db.IsDatabaseSnapshot;
                Trace.TraceInformation($"Tracing otherwise unused value {temp}");
                Assert.That(server.ConnectionContext.SqlExecutionModes, Is.EqualTo(SqlExecutionModes.CaptureSql), "SqlExecutionModes should be preserved");
            });
        }
    }
}
