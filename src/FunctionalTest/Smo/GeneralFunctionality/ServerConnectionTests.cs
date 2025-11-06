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
