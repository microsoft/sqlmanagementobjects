// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    public abstract partial class SqlTestBase
    {
        /// <summary>
        /// Async version of <see cref="ExecuteFromDbPool(Action{Database})"/>.
        /// Executes the specified async test method using a database from the pool associated with the test class.
        /// </summary>
        /// <param name="testMethod">The async test method to execute</param>
        public Task ExecuteFromDbPoolAsync(
            Func<Database, Task> testMethod) => ExecuteFromDbPoolAsync(TestContext.FullyQualifiedTestClassName, testMethod);

        /// <summary>
        /// Async version of <see cref="ExecuteFromDbPool(Action{Database}, Action{Database})"/>.
        /// Executes the specified async test method using a database from the pool associated with the test class.
        /// The <paramref name="onDatabaseCreated"/> delegate is invoked once when the database is first created.
        /// </summary>
        /// <param name="testMethod">The async test method to execute</param>
        /// <param name="onDatabaseCreated">Optional delegate invoked once after the database is first created</param>
        public Task ExecuteFromDbPoolAsync(
            Func<Database, Task> testMethod,
            Action<Database> onDatabaseCreated) => ExecuteFromDbPoolAsync(TestContext.FullyQualifiedTestClassName, testMethod, onDatabaseCreated);

        /// <summary>
        /// Async version of <see cref="ExecuteFromDbPool(string, Action{Database}, Action{Database})"/>.
        /// Executes the specified async test method using a database from the named pool.
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        /// <param name="testMethod">The async test method to execute</param>
        /// <param name="onDatabaseCreated">Optional delegate invoked once after the database is first created</param>
        public Task ExecuteFromDbPoolAsync(
            string poolName,
            Func<Database, Task> testMethod,
            Action<Database> onDatabaseCreated = null)
        {
            return ExecuteTestMethodWithFailureRetryAsync(
                async () =>
                {
                    var databaseHandler = DatabaseHandlerFactory.GetDatabaseHandler(TestDescriptorContext);
                    var db = TestServerPoolManager.GetDbFromPool(poolName, databaseHandler, onDatabaseCreated);
                    ServerContext = databaseHandler.ServerContext ?? db.GetServerObject();
                    if (ServerContext?.ConnectionContext != null)
                    {
                        SqlConnectionStringBuilder = new SqlConnectionStringBuilder(ServerContext.ConnectionContext.ConnectionString);
                    }
                    Trace.TraceInformation($"Returning database {db.Name} for pool {poolName}");
                    if (db.UserAccess == DatabaseUserAccess.Single || db.ReadOnly)
                    {
                        Trace.TraceInformation("Prior test set database to single user, setting back to multiple");
                        db.UserAccess = DatabaseUserAccess.Multiple;
                        db.ReadOnly = false;
                        db.Alter();
                    }
                    db.ExecutionManager.ConnectionContext.Disconnect();
                    db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    try
                    {
                        TraceHelper.TraceInformation("Invoking PreExecute for target server {0}", TestDescriptorContext.Name);
                        PreExecuteTest();
                        TraceHelper.TraceInformation("Invoking test method {0} with target server {1} using database from pool {2}",
                             TestContext.TestName, TestDescriptorContext.Name, poolName);
                        await testMethod(db).ConfigureAwait(false);
                        TraceHelper.TraceInformation("Invoking PostExecute for target server {0}",
                             TestDescriptorContext.Name);
                        PostExecuteTest();
                    }
                    catch (Exception e)
                    {
                        var message = string.Format(
                            "Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                            TestContext.TestName,
                            TestDescriptorContext.Name,
                            e.BuildRecursiveExceptionMessage(),
                            e.StackTrace);
                        Trace.TraceError(message);
                        throw new InternalTestFailureException(message, e);
                    }
                    finally
                    {
                        db.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    }
                });
        }

        /// <summary>
        /// Async version of <see cref="ExecuteWithDbDrop(string, Action{Database})"/>.
        /// Creates a new database and calls the given async test method with that database,
        /// then drops the database after execution.
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="testMethod">The async test method to execute</param>
        public virtual Task ExecuteWithDbDropAsync(
            string dbNamePrefix,
            Func<Database, Task> testMethod) => ExecuteWithDbDropAsync(dbNamePrefix, dbBackupFile: null, testMethod: testMethod);

        /// <summary>
        /// Async version of <see cref="ExecuteWithDbDrop(Action{Database}, AzureDatabaseEdition)"/>.
        /// Creates a new database and calls the given async test method with that database,
        /// then drops the database after execution.
        /// </summary>
        /// <param name="testMethod">The async test method to execute</param>
        /// <param name="dbAzureDatabaseEdition">Azure database edition if any</param>
        public virtual Task ExecuteWithDbDropAsync(
            Func<Database, Task> testMethod,
            AzureDatabaseEdition dbAzureDatabaseEdition = AzureDatabaseEdition.NotApplicable)
        {
            var dbNamePrefix =  !string.IsNullOrEmpty(TestContext.TestName)
    ? TestContext.TestName
    : GetType().Name;
            return ExecuteWithDbDropAsync(dbNamePrefix, dbBackupFile: null, testMethod: testMethod, dbAzureDatabaseEdition: dbAzureDatabaseEdition);
        }

        /// <summary>
        /// Async version of <see cref="ExecuteWithDbDrop(string, string, Action{Database}, AzureDatabaseEdition)"/>.
        /// Restores a database from a backup file or creates a new database, then executes the specified async action.
        /// After execution the database is dropped.
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="dbBackupFile">Backup file path or null to create a new database</param>
        /// <param name="testMethod">The async test method to execute</param>
        /// <param name="dbAzureDatabaseEdition">Azure database edition if any</param>
        public virtual Task ExecuteWithDbDropAsync(
            string dbNamePrefix,
            string dbBackupFile,
            Func<Database, Task> testMethod,
            AzureDatabaseEdition dbAzureDatabaseEdition = AzureDatabaseEdition.NotApplicable)
        {
            return ExecuteWithDbDropImplAsync(
                dbNamePrefix: dbNamePrefix,
                dbAzureDatabaseEdition: dbAzureDatabaseEdition,
                dbBackupFile: dbBackupFile,
                createDbSnapshot: false,
                executeTestMethodMethod: testMethod);
        }

        /// <summary>
        /// Async version of <see cref="ExecuteWithDbDrop(string, AzureDatabaseEdition, string, Action{Database})"/>.
        /// </summary>
        public virtual Task ExecuteWithDbDropAsync(
            string dbNamePrefix,
            AzureDatabaseEdition dbAzureEdition,
            string dbBackupFile,
            Func<Database, Task> testMethod)
        {
            return ExecuteWithDbDropImplAsync(
                dbNamePrefix: dbNamePrefix,
                dbAzureDatabaseEdition: dbAzureEdition,
                dbBackupFile: dbBackupFile,
                createDbSnapshot: false,
                executeTestMethodMethod: testMethod);
        }

        /// <summary>
        /// Async version of <see cref="ExecuteWithDbDrop(string, AzureDatabaseEdition, string, bool, Action{Database})"/>.
        /// </summary>
        public virtual Task ExecuteWithDbDropAsync(
            string dbNamePrefix,
            AzureDatabaseEdition dbAzureEdition,
            string dbBackupFile,
            bool createDbSnapshot,
            Func<Database, Task> testMethod)
        {
            return ExecuteWithDbDropImplAsync(
                dbNamePrefix: dbNamePrefix,
                dbAzureDatabaseEdition: dbAzureEdition,
                dbBackupFile: dbBackupFile,
                createDbSnapshot: createDbSnapshot,
                executeTestMethodMethod: testMethod);
        }

        /// <summary>
        /// Async version of <see cref="ExecuteWithDbDrop(DatabaseParameters, Action{Database})"/>.
        /// Creates a new database using the specified parameters and calls the given async test method,
        /// then drops the database after execution.
        /// </summary>
        /// <param name="dbParameters">Database creation parameters</param>
        /// <param name="testMethod">The async test method to execute</param>
        public virtual Task ExecuteWithDbDropAsync(DatabaseParameters dbParameters, Func<Database, Task> testMethod)
        {
            return ExecuteWithDbDropImplAsync(
                dbParameters: dbParameters,
                executeTestMethodMethod: testMethod);
        }

        private Task ExecuteWithDbDropImplAsync(
            string dbNamePrefix,
            AzureDatabaseEdition dbAzureDatabaseEdition,
            string dbBackupFile,
            bool createDbSnapshot,
            Func<Database, Task> executeTestMethodMethod)
        {
            var dbParameters = new DatabaseParameters
            {
                NamePrefix = dbNamePrefix,
                AzureDatabaseEdition = dbAzureDatabaseEdition,
                BackupFile = dbBackupFile,
                CreateSnapshot = createDbSnapshot,
                UseEscapedCharacters = UseEscapedCharactersInDatabaseNames
            };

            return ExecuteWithDbDropImplAsync(dbParameters, executeTestMethodMethod);
        }

        private Task ExecuteWithDbDropImplAsync(
            DatabaseParameters dbParameters,
            Func<Database, Task> executeTestMethodMethod)
        {
            var requestedEdition = dbParameters.AzureDatabaseEdition;
            IDatabaseHandler databaseHandler = null;
            return ExecuteTestMethodWithFailureRetryAsync(
                async () =>
                {
                    var originalEdition = requestedEdition;
                    if (requestedEdition == AzureDatabaseEdition.NotApplicable)
                    {
                        var desiredEdition = ConnectionHelpers.GetDefaultEdition(TargetServerFriendlyName);
                        if (desiredEdition == DatabaseEngineEdition.SqlDataWarehouse)
                        {
                            requestedEdition = dbParameters.AzureDatabaseEdition = AzureDatabaseEdition.DataWarehouse;
                        }
                    }
                    Database db;
                    try
                    {
                        databaseHandler = DatabaseHandlerFactory.GetDatabaseHandler(TestDescriptorContext);
                        db = databaseHandler.HandleDatabaseCreation(dbParameters);
                        ServerContext = databaseHandler.ServerContext;
                        SqlConnectionStringBuilder = new SqlConnectionStringBuilder(ServerContext.ConnectionContext.ConnectionString);
                    }
                    finally
                    {
                        requestedEdition = originalEdition;
                    }
                    var dbSnapshot = dbParameters.CreateSnapshot ? ServerContext.CreateDbSnapshotWithRetry(db) : null;

                    try
                    {
                        TraceHelper.TraceInformation("Invoking PreExecute for target server {0}", ServerContext.Name);
                        PreExecuteTest();
                        TraceHelper.TraceInformation("Invoking test method {0} with target server {1}",
                             TestContext.TestName, ServerContext.Name);
                        await executeTestMethodMethod(db).ConfigureAwait(false);
                        TraceHelper.TraceInformation("Invoking PostExecute for target server {0}",
                             ServerContext.Name);
                        PostExecuteTest();
                    }
                    catch (Exception e)
                    {
                        var message = string.Format(
                            "Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                            TestContext.TestName,
                            TestDescriptorContext.Name,
                            e.BuildRecursiveExceptionMessage(),
                            e.StackTrace);
                        Trace.TraceError(message);
                        throw new InternalTestFailureException(message, e);
                    }
                    finally
                    {
                        if (dbSnapshot != null)
                        {
                            ServerContext.DropKillDatabaseNoThrow(dbSnapshot.Name);
                        }
                        databaseHandler?.HandleDatabaseDrop();
                    }
                });
        }

        /// <summary>
        /// Async version of <see cref="ExecuteTestMethodWithFailureRetry"/>.
        /// Executes the specified async test method against each applicable server,
        /// retrying with backup connections on failure.
        /// </summary>
        private async Task ExecuteTestMethodWithFailureRetryAsync(Func<Task> testMethod)
        {
            var targetServerExceptions = new LinkedList<Tuple<string, Exception>>();
            Trace.TraceInformation($"Server filter:{TestContext.Properties["SqlTestTargetServersFilter"]}");
            var first = true;
            var connections = ConnectionHelpers.GetServerConnections(TestMethod, TestContext.SqlTestTargetServersFilter);
            foreach (var connection in connections)
            {
                using (new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext())
                {
                    try
                    {
                        var passed = false;
                        var exceptions = new LinkedList<Tuple<string, Exception>>();
                        if (!first || TargetServerFriendlyName == null)
                        {
                            TargetServerFriendlyName = connection.FriendlyName;
                        }

                        first = false;
                        passed = await ExecuteTestOnConnectionAsync(connection, testMethod, exceptions).ConfigureAwait(false);

                        if (!passed)
                        {
                            throw new AggregateException(
                                string.Format(
                                    "Test '{0}' failed against all defined server connections for target server name {1}{2}",
                                    TestMethod.Name,
                                    TargetServerFriendlyName,
                                    string.Join("\n",
                                        exceptions.Select(
                                            e =>
                                                string.Format("\n******* {0} *******\n{1}\n{2}", e.Item1,
                                                    e.Item2.BuildRecursiveExceptionMessage(),
                                                    e.Item2.StackTrace))))
                                , exceptions.Select(e => e.Item2));
                        }
                    }
                    catch (Exception e)
                    {
                        targetServerExceptions.AddLast(new Tuple<string, Exception>(TargetServerFriendlyName, e));
                    }
                }
            }

            if (targetServerExceptions.Count > 0)
            {
                throw new AggregateException(
                    string.Format(
                    "Test '{0}' failed against the following TargetServers : {1}\nExceptions : \n{2}",
                    TestMethod.Name,
                    string.Join(",", targetServerExceptions.Select(e => e.Item1)),
                    string.Join("\n", targetServerExceptions.Select(
                    e =>
                        string.Format(
@"******* {0} *******
Message : {1}
{2}",
                            e.Item1,
                            e.Item2.Message,
                            e.Item2.StackTrace)))));
            }
        }

        private async Task<bool> ExecuteTestOnConnectionAsync(ServerConnectionInfo connection, Func<Task> testMethod, LinkedList<Tuple<string, Exception>> exceptions)
        {
            TestDescriptorContext = connection.TestDescriptor;
            try
            {
                await testMethod().ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                exceptions.AddLast(new Tuple<string, Exception>(
                    SqlConnectionStringBuilder?.DataSource ?? TestDescriptorContext.Name,
                    e));
                return false;
            }
        }
    }
}
