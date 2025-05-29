// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Smo;
using SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;


namespace Microsoft.SqlServer.Test.Manageability.Utils.TestFramework
{
    /// <summary>
    /// Base class for tests, providing support for executing tests against specified target servers
    /// using the SupportedSqlVersionsAttribute
    /// </summary>
    public abstract class SqlTestBase
    {
        static SqlTestBase()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // We don't control all dependency DLL versions in every environment so just 
            // load whatever version is in the same folder as the test, if possible.
            var an = new AssemblyName(args.Name);
            if (an.Name.ToLower().EndsWith(".resources"))
            {
                // We are not really in the business of loading resources, so avoid unnecessary tracing...
                return null;
            }

            var dll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), an.Name + ".dll");
            if (dll.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            Trace.TraceInformation($"Trying to load dll:{dll}");
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                Trace.TraceInformation($"Loaded {dll}");
                return assembly;
            }
            catch (Exception e)
            {
                Trace.TraceError($"Unable to load {dll}: {e}");
            }
            return null;
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        private TestContext testContextInstance;

        /// <summary>
        /// Name of the server this test is running against
        /// </summary>
        protected string ServerName
        {
            get { return this.SqlConnectionStringBuilder.DataSource; }
        }

        /// <summary>
        /// The user ID to use when connecting to the server for this test (if using SQL Auth)
        /// </summary>
        protected string UserId
        {
            get { return this.SqlConnectionStringBuilder.UserID; }
        }

        /// <summary>
        /// The password to use when connecting to the server for this test (if using SQL Auth)
        /// </summary>
        protected string Password
        {
            get { return this.SqlConnectionStringBuilder.Password; }
        }

        private const string SqlConnectionStringBuilder_PropertyName = "SqlConnectionStringBuilder";

        /// <summary>
        /// The <see cref="SqlConnectionStringBuilder"/> containing the connection string to use for this test
        /// </summary>
        protected SqlConnectionStringBuilder SqlConnectionStringBuilder
        {
            get
            {
                return (SqlConnectionStringBuilder) this.TestContext.Properties[SqlConnectionStringBuilder_PropertyName];
            }
            private set { this.TestContext.Properties[SqlConnectionStringBuilder_PropertyName] = value; }
        }

        /// <summary>
        /// When true, ExecuteWithDbDrop will create databases names that have escaped characters in them
        /// </summary>
        protected bool UseEscapedCharactersInDatabaseNames { get; set; } = true;

        protected TestDescriptor TestDescriptorContext { get; set; }

        protected SMO.Server ServerContext { get; set; }

        protected MethodInfo TestMethod { get; set; }

        /// <summary>
        /// This corresponds to the "name" attribute in ConnectionInfo.xml,
        /// used to allow tests to identify servers without relying on the
        /// name of the actual backing server (in case that was to change).
        /// </summary>
        protected string TargetServerFriendlyName { get; set; }

        protected AzureDatabaseEdition DefaultEdition = AzureDatabaseEdition.NotApplicable;

        // Work around for NUnit 3.x Asserts accumulating failure messages across multiple vstest TestMethod invocations.
        // This IDisposable manages the lifetime of the assert messages raised during our test.
        private IDisposable nUnitDisposable;

        // Specific to Azure databases, represent all edition types
        public enum AzureDatabaseEdition
        {
            NotApplicable = 0, //If not applicable, we don't explicitly specify edition for dbs
            Basic = 1,
            Standard = 2,
            Premium = 3,
            DataWarehouse = 4,
            Hyperscale = 5,
            GeneralPurpose = 6,
            BusinessCritical = 7
        }

        /// <summary>
        /// Initialization method for the test, this contains initialization logic that should be ran before ALL
        /// tests. Test-specific initialization logic should be implemented in an overridden MyTestInitialize()
        /// method
        /// </summary>
        [TestInitialize()]
        public void BaseTestInitialize()
        {
            //We need to get the Assembly containing the implementation of this type so GetType will resolve it correctly
            //as FullyQualifiedTestClassName only contains the type name and not the assembly info (and GetType only
            //looks in mscorlib and the current executing assembly)
            Type testClass = this.GetType().GetTypeInfo().Assembly.GetType(this.TestContext.FullyQualifiedTestClassName);
            this.TestMethod = testClass.GetMethod(this.TestContext.TestName);
            nUnitDisposable = new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext();
            MyTestInitialize();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (nUnitDisposable != null)
            {
                nUnitDisposable.Dispose();
            }
        }
        public virtual void MyTestInitialize()
        {
        }

        /// <summary>
        /// Method ran before each invocation of test method (in case of multiple target servers)
        /// </summary>
        public virtual void PreExecuteTest()
        {
        }

        /// <summary>
        /// Method ran after each invocation of test method (in case of multiple target servers)
        /// </summary>
        public virtual void PostExecuteTest()
        {
        }

        /// <summary>
        /// Executes the specified test method once for each server specified in the SupportedSqlVersion attribute on the
        /// test method. Will call PreExecute() before the test method invocation and PostExecute() afterwards.
        /// </summary>
        /// <param name="testMethod"></param>
        public virtual void ExecuteTest(Action testMethod) => ExecuteTestImpl(server => { testMethod.Invoke(); });

        /// <summary>
        /// Executes the specified test method once for each server specified in the SupportedSqlVersion attribute on the
        /// test method. Will call PreExecute() before the test method invocation and PostExecute() afterwards.
        /// </summary>
        /// <param name="testMethod">Test method to execute with the server object for this test as a parameter</param>
        public virtual void ExecuteTest(Action<SMO.Server> testMethod) => ExecuteTestImpl(testMethod.Invoke);

        /// <summary>
        /// Implementation of the ExecuteTest method, which will execute the specified test method once for each server
        /// supported by the test.
        /// </summary>
        /// <param name="executeTestMethod"></param>
        private void ExecuteTestImpl(Action<SMO.Server> executeTestMethod)
        {
            //We need to get the Assembly containing the implementation of this type so GetType will resolve it correctly
            //as FullyQualifiedTestClassName only contains the type name and not the assembly info (and GetType only
            //looks in mscorlib and the current executing assembly)

            if (this.IsDisconnectedTest())
            {
                this.ServerContext = null;
                this.SqlConnectionStringBuilder = null;
                try
                {
                    TraceHelper.TraceInformation("Invoking PreExecute for Disconnected test");
                    PreExecuteTest();
                    TraceHelper.TraceInformation("Invoking test method {0} for Disconnected test", this.TestContext.TestName);
                    executeTestMethod.Invoke(null);
                    TraceHelper.TraceInformation("Invoking PostExecute for Disconnected test");
                    PostExecuteTest();
                }
                catch (Exception e)
                {
                    // Add in some more information
                    throw new InternalTestFailureException(
                        string.Format("Test '{0}' failed when executing disconnected test. Message:\n{1}\nStack Trace:\n{2}",
                            this.TestContext.TestName,
                            e.BuildRecursiveExceptionMessage(),
                            e.StackTrace), e);
                }

            }
            else
            {
                this.ExecuteTestMethodWithFailureRetry(() =>
                {
                    IDatabaseHandler databaseHandler = null;
                    Database db = null;
                    try
                    {
                        // Initialize the server context
                        databaseHandler = DatabaseHandlerFactory.GetDatabaseHandler(this.TestDescriptorContext);
                        // Fabric databases do not support have server context or master database, hence creating database
                        if (databaseHandler is FabricDatabaseHandler)
                        {
                            var dbParameters = new DatabaseParameters
                            {
                                UseEscapedCharacters = UseEscapedCharactersInDatabaseNames
                            };
                            db = databaseHandler.HandleDatabaseCreation(dbParameters);
                        }
                        this.ServerContext = databaseHandler.ServerContext;
                        this.SqlConnectionStringBuilder = new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString);
                        TraceHelper.TraceInformation("Invoking PreExecute for target server {0}", this.ServerContext.Name);
                        PreExecuteTest();
                        TraceHelper.TraceInformation("Invoking test method {0} with target server {1}",
                             this.TestContext.TestName, this.ServerContext.Name);
                        executeTestMethod.Invoke(this.ServerContext);
                        TraceHelper.TraceInformation("Invoking PostExecute for target server {0}", this.ServerContext.Name);
                        PostExecuteTest();
                    }
                    catch (Exception e)
                    {
                        // Add in some more information
                        throw new InternalTestFailureException(
                            string.Format("Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                                this.TestContext.TestName,
                                this.TestDescriptorContext.Name,
                                e.BuildRecursiveExceptionMessage(),
                                e.StackTrace), e);
                    }
                    finally
                    {
                        if (databaseHandler != null && db != null)
                        {
                            databaseHandler.HandleDatabaseDrop();
                        }
                    }
                });

            }
        }

        /// <summary>
        /// Executes the specified test method from the pool associated with the test class, creating a new Database in the pool if needed
        /// We use the class as the pool scope because test runs are multi-threaded, with test class being the partition for threads.
        /// </summary>
        /// <param name="testMethod">The test method to execute</param>
        public void ExecuteFromDbPool(
            Action<Database> testMethod) => ExecuteFromDbPool(TestContext.FullyQualifiedTestClassName, testMethod);

        /// <summary>
        /// Executes the specified test method from the pool specified, creating a new Database in the pool if needed. Currently only supports
        /// creating basic DBs - if more options are required then this method can be extended to expose those as needed.
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        /// <param name="testMethod">The test method to execute</param>
        public void ExecuteFromDbPool(
            string poolName,
            Action<Database> testMethod) => this.ExecuteTestMethodWithFailureRetry(
                () =>
                {
                    var databaseHandler = DatabaseHandlerFactory.GetDatabaseHandler(this.TestDescriptorContext);
                    var db = TestServerPoolManager.GetDbFromPool(poolName, databaseHandler);
                    this.ServerContext = databaseHandler.ServerContext ?? db.GetServerObject();
                    if(this.ServerContext != null && this.ServerContext.ConnectionContext != null)
                    {
                        this.SqlConnectionStringBuilder = new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString);
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
                        TraceHelper.TraceInformation("Invoking PreExecute for target server {0}", this.TestDescriptorContext.Name);
                        PreExecuteTest();
                        TraceHelper.TraceInformation("Invoking test method {0} with target server {1} using database from pool {2}",
                             this.TestContext.TestName, this.TestDescriptorContext.Name, poolName);
                        testMethod.Invoke(db);
                        TraceHelper.TraceInformation("Invoking PostExecute for target server {0}",
                             this.TestDescriptorContext.Name);
                        PostExecuteTest();
                    }
                    catch (Exception e)
                    {
                        // Add in some more information
                        string message = string.Format(
                            "Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                            this.TestContext.TestName,
                            this.TestDescriptorContext.Name,
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

        /// <summary>
        /// Creates a new database and calls the given test method with that database, then drops
        /// the database after execution if still exists.
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="testMethod">The test method to execute, with the newly created database passed as a parameter</param>
        public virtual void ExecuteWithDbDrop(
            string dbNamePrefix,
            Action<Database> testMethod) => ExecuteWithDbDrop(dbNamePrefix, dbBackupFile: null, testMethod: testMethod);

        /// <summary>
        /// Creates a new database and calls the given test method with that database, then drops
        /// the database after execution if still exists.
        /// </summary>
        /// <param name="testMethod">The test method to execute, with the newly created database passed as parameters</param>
        /// <param name="dbAzureDatabaseEdition">Azure database edition if any</param>
        public virtual void ExecuteWithDbDrop(
            Action<Database> testMethod,
            AzureDatabaseEdition dbAzureDatabaseEdition = AzureDatabaseEdition.NotApplicable)
        {
            string dbNamePrefix = string.IsNullOrEmpty(this.TestContext.TestName)
                ? this.TestContext.TestName
                : this.GetType().Name;
          ExecuteWithDbDrop(dbNamePrefix, dbBackupFile: null, testMethod: testMethod, dbAzureDatabaseEdition: dbAzureDatabaseEdition);
        }

        /// <summary>
        /// Restores a database from a backup file and then executes the specified action method against it for each
        /// server specified. After execution the database is dropped if still exists (regardless out of the outcome)
        ///
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="dbBackupFile">Default to create a new database if a null or empty string is given</param>
        /// <param name="testMethod">The test method to execute, with newly created database passed as parameters</param>
        /// <param name="dbAzureDatabaseEdition">Azure database eidtion if any</param>
        /// <remarks>NOTE : The same backup file is used for ALL servers specified</remarks>
        public virtual void ExecuteWithDbDrop(
            string dbNamePrefix,
            string dbBackupFile,
            Action<Database> testMethod,
            AzureDatabaseEdition dbAzureDatabaseEdition = AzureDatabaseEdition.NotApplicable) => ExecuteWithDbDropImpl(
                dbNamePrefix: dbNamePrefix,
                dbAzureDatabaseEdition: dbAzureDatabaseEdition,
                dbBackupFile: dbBackupFile,
                createDbSnapshot: false,
                executeTestMethodMethod: (database) => { testMethod.Invoke(database); });

        /// <summary>
        /// Restores a database from a backup file OR create a new database, with specific azure db edition if provided,
        /// then executes the specified action method against it for each server specified. After execution the database
        /// is dropped if still exists
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="dbAzureEdition">Edition type specific for Azure SQL databases, if "NotApplicable" server default setting is used</param>
        /// <param name="dbBackupFile">Path of backup file to restore the database, if NULL/EmptyString then create a new database</param>
        /// <param name="testMethod">The test method to execute, with newly created database passed as parameters</param>
        /// <remarks>NOTE : The same backup file is used for ALL servers specified</remarks>
        public virtual void ExecuteWithDbDrop(
            string dbNamePrefix,
            AzureDatabaseEdition dbAzureEdition,
            string dbBackupFile,
            Action<Database> testMethod) => ExecuteWithDbDropImpl(
                dbNamePrefix: dbNamePrefix,
                dbAzureDatabaseEdition: dbAzureEdition,
                dbBackupFile: dbBackupFile,
                createDbSnapshot: false,
                executeTestMethodMethod: testMethod);

        /// <summary>
        /// Restores a database from a backup file OR create a new database, with specific azure db edition if provided,
        /// then executes the specified action method against it for each server specified. After execution the database
        /// is dropped if still exists
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="dbAzureEdition">Edition type specific for Azure SQL databases, if "NotApplicable" server default setting is used</param>
        /// <param name="dbBackupFile">Path of backup file to restore the database, if NULL/EmptyString then create a new database</param>
        /// <param name="createDbSnapshot">Whether to create a snapshot of the DB after creation</param>
        /// <param name="testMethod">The test method to execute, with newly created database passed as parameters</param>
        /// <remarks>NOTE : The same backup file is used for ALL servers specified</remarks>
        public virtual void ExecuteWithDbDrop(
            string dbNamePrefix,
            AzureDatabaseEdition dbAzureEdition,
            string dbBackupFile,
            bool createDbSnapshot,
            Action<Database> testMethod) => ExecuteWithDbDropImpl(
                dbNamePrefix: dbNamePrefix,
                dbAzureDatabaseEdition: dbAzureEdition,
                dbBackupFile: dbBackupFile,
                createDbSnapshot: createDbSnapshot,
                executeTestMethodMethod: testMethod);

        /// <summary>
        /// Creates a new database and calls the given test method with that database, then drops it
        /// </summary>
        /// <param name="dbParameters"></param>
        /// <param name="testMethod"></param>
        public virtual void ExecuteWithDbDrop(DatabaseParameters dbParameters, Action<Database> testMethod) => ExecuteWithDbDropImpl(
                dbParameters: dbParameters,
                executeTestMethodMethod: testMethod);
        /// <summary>
        /// Implementation of the ExecuteWithDbDrop, calls executeTestMethodMethod once for each supported server version
        /// </summary>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="dbAzureDatabaseEdition">Edition type specific for Azure SQL databases, if "NotApplicable" server default setting is used</param>
        /// <param name="dbBackupFile">Path of backup file to restore the database, if NULL/EmptyString then create a new database</param>
        /// <param name="createDbSnapshot"></param>
        /// <param name="executeTestMethodMethod">
        /// The action called to invoke the test method, this should simply just call the test method itself with whatever parameters it needs
        /// </param>
        private void ExecuteWithDbDropImpl(
            string dbNamePrefix,
            AzureDatabaseEdition dbAzureDatabaseEdition,
            string dbBackupFile,
            bool createDbSnapshot,
            Action<Database> executeTestMethodMethod)
        {
            var dbParameters = new DatabaseParameters
            {
                NamePrefix = dbNamePrefix,
                AzureDatabaseEdition = dbAzureDatabaseEdition,
                BackupFile = dbBackupFile,
                CreateSnapshot = createDbSnapshot,
                UseEscapedCharacters = UseEscapedCharactersInDatabaseNames
            };

            // Call the new override
            ExecuteWithDbDropImpl(dbParameters, executeTestMethodMethod);
        }

        /// <summary>
        /// Implementation of the ExecuteWithDbDrop, calls executeTestMethodMethod once for each supported server version
        /// </summary>
        /// <param name="dbParameters">Encapsulates database parameters such as name prefix, Azure edition, and backup file</param>
        /// <param name="executeTestMethodMethod">
        /// The action called to invoke the test method, this should simply just call the test method itself with whatever parameters it needs
        /// </param>
        private void ExecuteWithDbDropImpl(
            DatabaseParameters dbParameters,
            Action<Database> executeTestMethodMethod)
        {
            var requestedEdition = dbParameters.AzureDatabaseEdition;
            IDatabaseHandler databaseHandler = null;
            this.ExecuteTestMethodWithFailureRetry(
                () =>
                {
                    var originalEdition = requestedEdition;
                    if (requestedEdition == AzureDatabaseEdition.NotApplicable)
                    {
                        // if the default edition specified in the XML for the current server is DW, 
                        // pass that along to the helper
                        var desiredEdition = ConnectionHelpers.GetDefaultEdition(TargetServerFriendlyName);
                        if (desiredEdition == DatabaseEngineEdition.SqlDataWarehouse)
                        {
                            requestedEdition = AzureDatabaseEdition.DataWarehouse;
                        }
                    }
                    Database db;
                    try
                    {
                        databaseHandler = DatabaseHandlerFactory.GetDatabaseHandler(this.TestDescriptorContext);
                        db = databaseHandler.HandleDatabaseCreation(dbParameters);
                        this.ServerContext = databaseHandler.ServerContext;
                        this.SqlConnectionStringBuilder = new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString);
                    }
                    finally
                    {
                        requestedEdition = originalEdition;
                    }
                    Database dbSnapshot = dbParameters.CreateSnapshot ? this.ServerContext.CreateDbSnapshotWithRetry(db) : null;

                    try
                    {
                        TraceHelper.TraceInformation("Invoking PreExecute for target server {0}", this.ServerContext.Name);
                        PreExecuteTest();
                        TraceHelper.TraceInformation("Invoking test method {0} with target server {1}",
                             this.TestContext.TestName, this.ServerContext.Name);
                        executeTestMethodMethod.Invoke(db);
                        TraceHelper.TraceInformation("Invoking PostExecute for target server {0}",
                             this.ServerContext.Name);
                        PostExecuteTest();
                    }
                    catch (Exception e)
                    {
                        // Add in some more information
                        string message = string.Format(
                            "Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                            this.TestContext.TestName,
                            this.TestDescriptorContext.Name,
                            e.BuildRecursiveExceptionMessage(),
                            e.StackTrace);
                        Trace.TraceError(message);
                        throw new InternalTestFailureException(message, e);
                    }
                    finally
                    {
                        // snapshots have to be deleted first
                        if (dbSnapshot != null)
                        {
                            ServerContext.DropKillDatabaseNoThrow(dbSnapshot.Name);
                        }
                        if(databaseHandler != null)
                        {
                            // Drop the database
                            databaseHandler.HandleDatabaseDrop();
                        }
                    }
                });
        }
            
        /// <summary>
        /// Defines a new database on specific server, and then executes the specified action method on this database.
        /// After execution, the database is dropped if exists.
        /// </summary>
        /// <param name="server">The server object</param>
        /// <param name="dbNamePrefix">Name prefix for new database</param>
        /// <param name="executeMethod">The test method to execute, with newly created database passed as parameters</param>
        public void ExecuteMethodWithDbDrop(
            SMO.Server server,
            string dbNamePrefix,
            Action<Database> executeMethod)
        {
            string databaseName = SmoObjectHelpers.GenerateUniqueObjectName(dbNamePrefix);
            Database database;
            try
            {
                TraceHelper.TraceInformation("Creating new database '{0}' on server '{1}'", databaseName, server.Name);
                database = new Database(server, databaseName);
            }
            catch (Exception e)
            {
                // Add in some more information
                throw new InternalTestFailureException(
                    string.Format(
                    "Test setup for Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                    this.TestContext.TestName,
                    server.Name,
                    e.BuildRecursiveExceptionMessage(),
                    e.StackTrace), e);
            }

            try
            {
                TraceHelper.TraceInformation("Invoking test method {0} with target server {1}",
                     this.TestContext.TestName, this.ServerContext.Name);
                executeMethod.Invoke(database);
            }
            catch (Exception e)
            {
                // Add in some more information
                throw new InternalTestFailureException(
                    string.Format("Test '{0}' failed when targeting server {1}. Message:\n{2}\nStack Trace:\n{3}",
                        this.TestContext.TestName,
                        this.ServerContext.Name,
                        e.BuildRecursiveExceptionMessage(),
                        e.StackTrace), e);
            }
            finally
            {
                server.DropKillDatabaseNoThrow(databaseName);
            }
        }

        /// <summary>
        /// Defines the specified test method against multiple servers
        /// </summary>
        /// <param name="numOfServers">The number of required servers to run the test</param>
        /// <param name="requiresSameHostPlatform">A boolean value indicating whether the required servers need to have the same host platform</param>
        /// <param name="requiresSameMajorVersion">A boolean value indicating whether the required servers need to have the same version</param>
        /// <param name="action">The test action</param>
        public void ExecuteWithMultipleServers(int numOfServers, bool requiresSameHostPlatform, bool requiresSameMajorVersion, Action<IEnumerable<SMO.Server>> action)
        {
            if (numOfServers < 1)
            {
                throw new ArgumentException(string.Format("Invalid value provided: {0}", numOfServers), "numOfServers");
            }

            TraceHelper.TraceInformation("Executing test against multiple servers. numOfServers: {0}, requiresSameHostPlatform, value: {1}, requiresSameMajorVersion: {2}", numOfServers, requiresSameHostPlatform, requiresSameMajorVersion);

            var connections = ConnectionHelpers.GetServerConnections(this.TestMethod, TestContext.SqlTestTargetServersFilter);

            var servers = connections
                .Where(c => !c.IsFabricWorkspace) // Exclude FabricWorkspaces
                .SelectMany(c => c.ConnectionStrings
                    .Select(connString =>
                        new SMO.Server(new ServerConnection(new SqlConnection(connString.ConnectionString)))))
                        .ToArray();

            TraceHelper.TraceInformation("Number of target servers for the test before grouping: {0}", servers.Length);

            var groupResults = servers.GroupBy(server =>
            {
                string groupKey = "ServerGroup";

                if (requiresSameHostPlatform)
                {
                    groupKey += server.HostPlatform;
                }

                if (requiresSameMajorVersion)
                {
                    groupKey += server.VersionMajor;
                }

                return groupKey;
            }).ToArray();

            TraceHelper.TraceInformation("Number of server groups for the test: {0}", groupResults.Count());

            foreach (var groupResult in groupResults)
            {
                if (groupResult.Count() >= numOfServers)
                {
                    var targetServers = groupResult.Take(numOfServers).ToArray();
                    TraceHelper.TraceInformation("Server group: {0}, target servers: {1}", groupResult.Key, string.Join(",", targetServers.Select(srv => srv.NetNameWithInstance())));
                    this.SqlConnectionStringBuilder = new SqlConnectionStringBuilder(targetServers[0].ConnectionContext.ConnectionString);

                    try
                    {
                        PreExecuteTest();
                        action(targetServers);
                    }
                    finally
                    {
                        PostExecuteTest();
                    }
                }
                else
                {
                    Trace.TraceWarning("Server group '{0}' doesn't have enough servers for the test, number of servers in the group: {1}", groupResult.Key, groupResult.Count());
                }
            }
        }

        /// <summary>
        /// Returns the master database in the given server context
        /// </summary>
        /// <param name="testMethod">The test method to execute, with the newly created database passed as a parameter</param>
        public void ExecuteWithMasterDb(Action<Database> testMethod) => ExecuteWithMasterDbImpl(AzureDatabaseEdition.NotApplicable,
                (database) => { testMethod.Invoke(database); });


        /// <summary>
        /// Implementation of the ExecuteWithMaster, calls executeTestMethodMethod once for each supported server version
        /// </summary>
        /// <param name="edition">Edition type specific for Azure SQL databases, if "NotApplicable" server default setting is used</param>
        /// <param name="executeTestMethodMethod">
        /// The action called to invoke the test method, this should simply just call the test method itself with whatever parameters it needs
        /// </param>
        public void ExecuteWithMasterDbImpl(AzureDatabaseEdition edition, Action<Database> executeTestMethodMethod) => this.ExecuteTestMethodWithFailureRetry(
               () =>
               {
                   // Initialize the server context
                   var databaseHandler = DatabaseHandlerFactory.GetDatabaseHandler(this.TestDescriptorContext);
                   this.ServerContext = databaseHandler.ServerContext;
                   this.SqlConnectionStringBuilder = new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString);
                   Database database = this.ServerContext.Databases["master"];
                   executeTestMethodMethod(database);
               });

        #region Private Helper Methods

        /// <summary>
        /// Executes the specified test method against each applicable server. For each server it will
        /// first try the "primary" connection, and if that fails will move on to try again with any backup
        /// servers defined until no more servers are left for that target (in which case the test will be
        /// marked as a failure) or the method passes successfully (at which point it will move on to the
        /// next server target)
        /// </summary>
        /// <param name="testMethod"></param>
        private void ExecuteTestMethodWithFailureRetry(Action testMethod)
        {
            var targetServerExceptions = new LinkedList<Tuple<string, Exception>>();
            Trace.TraceInformation($"Server filter:{TestContext.Properties["SqlTestTargetServersFilter"]}");
            var first = true;
            var connections = ConnectionHelpers.GetServerConnections(this.TestMethod, TestContext.SqlTestTargetServersFilter);
            foreach (var connection in connections)
            {
                // Prevent nunit assert messages from accumulating between server version iterations
                using (new NUnit.Framework.Internal.TestExecutionContext.IsolatedContext())
                {
                    try
                    {
                        bool passed = false;
                        var exceptions = new LinkedList<Tuple<string, Exception>>();
                        // some tests target a specific server but use a hard coded friendly name
                        // any test that matches multiple servers isn't doing that.
                        if (!first || TargetServerFriendlyName == null)
                        {
                            TargetServerFriendlyName = connection.FriendlyName;
                        }

                        first = false;
                        passed = ExecuteTestOnConnection(connection, testMethod, exceptions);

                        if (!passed)
                        {
                            //Build up the aggregate exception of all the exceptions we gathered during the run. We put the message and stack trace for each
                            //one into the message itself so it's easily visible from the test failure (AggregateException.ToString doesn't print out that
                            //information by default)
                            throw new AggregateException(
                                String.Format(
                                    "Test '{0}' failed against all defined server connections for target server name {1}{2}",
                                    this.TestMethod.Name,
                                    this.TargetServerFriendlyName,
                                    string.Join("\n",
                                        exceptions.Select(
                                            e =>
                                                String.Format("\n******* {0} *******\n{1}\n{2}", e.Item1,
                                                    e.Item2.Message,
                                                    e.Item2.StackTrace))))
                                , exceptions.Select(e => e.Item2));
                        }
                    }
                    catch (Exception e)
                    {
                        //We failed against one of our target servers, record it and then move on to the next (we'll fail the test once all
                        //target servers have been ran against)
                        targetServerExceptions.AddLast(new Tuple<string, Exception>(this.TargetServerFriendlyName, e));
                    }
                }
            }

            //We got some errors against one or more of the target servers, throw an exception with the aggregate information to
            //fail the test
            if (targetServerExceptions.Count > 0)
            {
                throw new AggregateException(
                    string.Format(
                    "Test '{0}' failed against the following TargetServers : {1}\nExceptions : \n{2}",
                    this.TestMethod.Name,
                    string.Join(",", targetServerExceptions.Select(e => e.Item1)), //List of all failed server friendly names
                                                                                   //Formatted exception infor for each target server failure
                    string.Join("\n", targetServerExceptions.Select(
                    e =>
                        String.Format(
@"******* {0} *******
Message : {1}
{2}",
                            e.Item1, //ServerName
                            e.Item2.Message,
                            e.Item2.StackTrace)))));
            }
        }

        private bool ExecuteTestOnConnection(ServerConnectionInfo connection, Action testMethod, LinkedList<Tuple<string, Exception>> exceptions)
        {
            this.TestDescriptorContext = connection.TestDescriptor;
            try
            {
                testMethod.Invoke();
                return true; // Test passed successfully
            }
            catch (Exception e)
            {
                exceptions.AddLast(new Tuple<string, Exception>(
                    this.SqlConnectionStringBuilder?.DataSource ?? this.TestDescriptorContext.Name,
                    e));
                return false;
            }
        }

        #endregion // Private Helper Methods

        #region Helper Methods

        /// <summary>
        /// Returns TRUE if the test is marked with the <see cref="DisconnectedTestAttribute"/> attribute,
        /// indicating that it is a disconnected test (will be ran without actually connecting to a server)
        /// </summary>
        /// <returns></returns>
        protected bool IsDisconnectedTest() => this.TestMethod.GetCustomAttribute<DisconnectedTestAttribute>() != null;

        #endregion //Helper Methods
    }
}
