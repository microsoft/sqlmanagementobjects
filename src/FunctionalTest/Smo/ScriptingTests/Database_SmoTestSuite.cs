// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Threading;
using System;
using System.Diagnostics;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Database properties and scripting
    /// </summary>
    [TestClass]
    public class Database_SmoTestSuite : SmoObjectTestBase
    {

        #region Property Tests

        /// <summary>
        /// Tests accessing the Size property of a Database object
        /// This is a regression test for bug #9871452.
        /// The actual target server is irrelevant: for that reason,
        /// I'm restricting the execution against on-prem servers.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        public void SmoDatabase_Can_Access_Size_Property_When_Database_Name_Has_Special_GB18030_Chars()
        {
            // This special string would be incorrectly interpreted by the server
            // when the conversion VARCHAR <-> NVARCHAR happens (i.e. if SMO does
            // not build a query using 'N
            var gb18030 = "㐀㒣㕴㕵㙉㙊䵯䵰䶴䶵";

            this.ExecuteWithDbDrop(
                gb18030,
                database =>
                {
                    Assert.DoesNotThrow(() =>
                    {
                        if (database.Size > 0) return;
                    }, "Unable to access the 'Size' property on database object.");
                });
        }

        /// <summary>
        /// Tests accessing and setting temporal history retention property on Azure v12 (Sterling)
        /// The property is expected to be TRUE by default and can be changed via ALTER
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void SmoDatabase_TemporalRetentionProperty_AzureSterlingV12()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    Assert.That(database.TemporalHistoryRetentionEnabled,
                        "Temporal history retention not ON for the newly created database.");

                    database.TemporalHistoryRetentionEnabled = false;
                    database.Alter();

                    Assert.That(database.TemporalHistoryRetentionEnabled, Is.False,
                        "Temporal history retention should be disabled post-alter.");

                    // Connect to Sterling and check if the value is really 'false' as reported by SMO.
                    //
                    bool res = (bool)database.ExecutionManager.ConnectionContext.ExecuteScalar(
                        string.Format(System.Globalization.CultureInfo.InvariantCulture,
                            "SELECT [is_temporal_history_retention_enabled] FROM SYS.DATABASES WHERE NAME = '{0}'",
                            Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(database.Name)));
                    Assert.That(res, Is.False,
                        "Retention value not correctly changed through SMO - expected FALSE, but the actual value in the database is TRUE.");
                });
        }

        /// <summary>
        /// Tests accessing AvailabilityDatabaseSynchronizationState property on SQL Server 2012+
        /// The property is expected to be Synchronized when database is added to an availability group
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 11, HostPlatform = HostPlatformNames.Windows)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDatabase_AvailabilityDatabaseSynchronizationState_OnPremV11OrNewer()
        {
            ExecuteWithDbDrop(
                database =>
                {
                    database.TakeFullBackup();
                    var server = database.Parent;
                    var agName = "ag" + Guid.NewGuid();
                    var ag = new _SMO.AvailabilityGroup(server, agName);

                    if (server.VersionMajor >= 14)
                    {
                        ag.ClusterType = _SMO.AvailabilityGroupClusterType.None;
                    }

                    try
                    {
                        AlwaysOnTestHelper.CreateAvailabilityGroupForDatabase(server, ag, database.Name);
                        Assert.That(database.AvailabilityDatabaseSynchronizationState,
                            Is.EqualTo(AvailabilityDatabaseSynchronizationState.Synchronized),
                            "AvailabilityDatabaseSynchronizationState should be Synchronized.");
                    }
                    finally
                    {
                        if (server.VersionMajor >= 13)
                        {
                            ag.DropIfExists();
                        }
                        else
                        {
                            // Drop if exists doesn't work for SQL Server 2014 or less
                            ag.Drop();
                        }
                    }
                });
        }


        /// <summary>
        /// Tests accessing AvailabilityDatabaseSynchronizationState property on Azure Sql Managed Instance
        /// Accessing property is expected to throw PropertyCannotBeRetrievedException when the property is not applicable
        /// and return Synchronized if the database is in Managed Instance Link
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDatabase_AvailabilityDatabaseSynchronizationState_ManagedInstance()
        {
            ExecuteTest(
                server =>
                {
                    var databasesWithNonNullSyncState = GetDatabasesWithNonNullSynchronizationState(server);

                    foreach (_SMO.Database db in server.Databases)
                    {
                        if (databasesWithNonNullSyncState.Contains(db.Name))
                        {
                            Assert.That(() => db.AvailabilityDatabaseSynchronizationState,
                                Is.EqualTo(AvailabilityDatabaseSynchronizationState.Synchronized),
                                "AvailabilityDatabaseSynchronizationState should be Synchronized.");
                        }
                        else
                        {
                            Assert.Throws<PropertyCannotBeRetrievedException>(() => { var state = db.AvailabilityDatabaseSynchronizationState; },
                                "Accessing the property AvailabilityDatabaseSynchronizationState should throw an exception when it's not applicable.");
                        }
                    }

                });
        }

        private static List<string> GetDatabasesWithNonNullSynchronizationState(_SMO.Server server)
        {
            var dbs = new List<string>();

            // This query returns synchronization state for the given database
            // If database is not in availability group, no rows are returned
            //
            try
            {
                string getDatabasesWithNonNullSynchronizationStatesQuery = $@"
                                select dtb.name as name
                                from sys.dm_hadr_database_replica_states hadrd
                                join sys.databases as dtb on dtb.database_id = hadrd.database_id
                                join sys.availability_groups avag on hadrd.group_id = avag.group_id
                                join sys.availability_replicas avar on avag.name = avar.replica_server_name
                                join sys.availability_groups avag2 on avar.group_id = avag2.group_id
                                where hadrd.is_local = 1";

                var result = server.ConnectionContext.ExecuteWithResults(getDatabasesWithNonNullSynchronizationStatesQuery);

                var databasesWithNonNullSynchronizationStates =
                    (from row in result.Tables[0].AsEnumerable()
                     select row["name"].ToString()).ToList();

                dbs.AddRange(databasesWithNonNullSynchronizationStates);
            }
            catch
            {
                // This is likely due to user lacking VIEW DATABASE PERFORMANCE STATE
                //
            }

            return dbs;
        }

        /// <summary>
        /// Verifies that properties return their expected values for a DB
        /// still in the Creating state
        /// </summary>
        [TestMethod]
        public void SmoDatabase_Verify_Properties_When_Creating()
        {
            this.ExecuteTest(
                server =>
                {
                    var result = new SqlTestResult();

                    //This test needs to test against a DB in the creating state so we just
                    //create it locally but not on the server
                    var db = new _SMO.Database(server, "CreatingDb");

                    //The engine type should match the type from the server since it's a server level property
                    result &= SqlTestHelpers.TestReadProperty(db, "DatabaseEngineType", server.DatabaseEngineType);
                    //On Azure the EngineEdition is a db level property so we need a connection directly to the db,
                    //but since our DB isn't created yet it'll return Unknown

                    var expectedDbEngineEdition = server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase
                        ? DatabaseEngineEdition.Unknown
                        : server.DatabaseEngineEdition;
                    result &= SqlTestHelpers.TestReadProperty(db, "DatabaseEngineEdition", expectedDbEngineEdition);


                    Assert.That(result.Succeeded, result.FailureReasons);
                });

        }

        /// <summary>
        /// The bug itself manifested in the "Import Data-tier Application" wizard, but the root cause of the issue
        /// is really in the way SMO handle the retrieval of SMO.Server.Databases. Currently, in order to fetch such
        /// information, SMO ends up trying to get the DatabaseEngineEdition on each DB it sees on the server. Some databases
        /// may just not be accessible (for assorted reasons) in which case we just throw an exception instead of just
        /// giving up on discovering the DatabaseEngineEdition.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Trying_To_Access_AzureDB_With_DBManager_Role_Only_Does_Not_Throw()
        {
            // We use the ExecuteWithMasterDb() overload to get back "master", so we can create a "low-privileged" login/user
            // (e.g. we just grant it the dbmanager role)
            ExecuteWithMasterDb((master) =>
            {
                if (SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.NotSpecified && SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.SqlPassword)
                {
                    Trace.TraceWarning($"Skipping DBManager test on {SqlConnectionStringBuilder.DataSource} because SQL logins are not available");
                    return;
                }
                // Create a new database which the low-priv user won't be able to connect to, yet it would be able to "see" it
                var db = ServerContext.CreateDatabaseWithRetry("DbManagerOnly");
                try
                {
                    var pwd = SqlTestRandom.GeneratePassword();
                    Login login = null;
                    User user = null;
                    SqlConnection sqlConn = null;
                    try
                    {
                        // Here we creata a login/user...
                        login = master.Parent.CreateLogin(GenerateUniqueSmoObjectName("login_low_priv", maxLength: 128),
                            _SMO.LoginType.SqlLogin, pwd);
                        user = master.CreateUser(login.Name, login.Name);

                        // ... and we grant it limited permissions (certainly a lot less than "cloudsa").
                        // Note: the "dbmanager" role seems to only exist in SQL Azure, so for this reason
                        // this test is restricted to that EngineType.
                        master.ExecutionManager.ExecuteNonQuery(
                            string.Format("ALTER ROLE dbmanager ADD member {0}",
                                SmoObjectHelpers.SqlBracketQuoteString(login.Name)));

                        // Now, we connect using the low-priv login.
                        // Disclosure: the code is very similar to the one you see below in another test.
                        var connStr =
                            new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString)
                            {
                                Pooling = false,
                                UserID = user.Name,
                                // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="not secret")]
                                Password = pwd
                            };

                        using (sqlConn = new SqlConnection(connStr.ToString()))
                        {
                            sqlConn.Open();
                            var dbScopedConn = new ServerConnection(sqlConn);
                            var server = new _SMO.Server(dbScopedConn);
                            server.SetDefaultInitFields(typeof(Database), nameof(Database.AzureEdition));
                            Assert.That(server.Databases.Cast<Database>().Select(d => d.DatabaseEngineEdition), Has.None.EqualTo(DatabaseEngineEdition.Unknown), "Low privileged user should get DatabaseEngineEdition of all databases");
                            Assert.That(server.Databases.Cast<Database>().Where(d => d.DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabase).Select(d => d.AzureEdition), Has.None.EqualTo(""), "Low privileged user should get AzureEdition of all Azure DB databases");
                            Assert.Throws<ConnectionFailureException>(() => { var x = server.Databases[db.Name].Size; }, "Accessing other properties of inaccessible Database should throw");
                        }
                    }
                    finally
                    {
                        if (sqlConn != null && sqlConn.State == ConnectionState.Open)
                        {
                            sqlConn.Close();
                        }

                        if (login != null)
                        {
                            master.ExecutionManager.ExecuteNonQuery(
                                string.Format("ALTER ROLE dbmanager DROP member {0}",
                                    SmoObjectHelpers.SqlBracketQuoteString(login.Name)));
                            user?.Drop();
                            login.Drop();
                        }
                    }
                }
                finally
                {
                    db.Parent.DropKillDatabaseNoThrow(db.Name);
                }
            });
        }

        /// <summary>
        /// If a ServerConnection is contructed with a SqlConnection that is already open and it uses Sql Auth,
        /// it may attempt to clone the SqlConnection using the connection string (for postprocessing, etc).
        /// That clone will fail to copy the password so the connection will fail.
        /// This test attempts to ensure we don't add "non expensive" properties that rely on these extra connections succeeding.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        public void SmoDatabase_does_not_generate_SqlException_accessing_default_properties_when_SqlConnection_is_open()
        {
            ExecuteWithDbDrop((db) =>
            {
                if (ServerContext.LoginMode == ServerLoginMode.Integrated)
                {
                    Trace.TraceInformation($"Skipping SqlException test because SQL Auth is not enabled on {ServerContext.Name}");
                    return;
                }
                var pwd = SqlTestRandom.GeneratePassword();
                _SMO.Login login = null;
                SqlConnection sqlConn = null;
                try
                {
                    login = db.Parent.CreateLogin(GenerateUniqueSmoObjectName("login", maxLength: 128),
                        _SMO.LoginType.SqlLogin, pwd);
                    var user = db.CreateUser(login.Name, login.Name);

                    db.ExecutionManager.ExecuteNonQuery(
                        string.Format("grant view definition on database::{0} TO {1}",
                            SmoObjectHelpers.SqlBracketQuoteString(db.Name),
                            SmoObjectHelpers.SqlBracketQuoteString(user.Name)));
                    var connStr =
                        new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString)
                        {
                            IntegratedSecurity=false,
                            Pooling = false,
                            UserID = user.Name,
                            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="not secret")]
                            Password = pwd,
                            Authentication = SqlAuthenticationMethod.SqlPassword
                        };

                    using (sqlConn = new SqlConnection(connStr.ToString()))
                    {
                        sqlConn.Open();
                        var dbScopedConn = new ServerConnection(sqlConn);
                        var server = new _SMO.Server(dbScopedConn);
                        bool isSnapShot = true;
                        Assert.DoesNotThrow(() => isSnapShot = server.Databases[db.Name].IsDatabaseSnapshot);

                        Assert.That(isSnapShot, Is.False, "IsDatabaseSnapshot");
                    }
                }
                finally
                {
                    if (sqlConn != null && sqlConn.State == ConnectionState.Open)
                    {
                        sqlConn.Close();
                    }

                    if (login != null)
                    {
                        login.Drop();
                    }
                }
            });
        }

        #endregion //Property Tests

        #region Scripting Tests

        /// <summary>
        /// Tests altering a database through SMO for all server versions
        /// </summary>
        [TestMethod]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void SmoDatabaseScriptAlter_AllServers()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    database.ReadOnly = true;
                    if (database.DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabase)
                    {
                        database.MaxSizeInBytes = 250.0 * 1024 * 1024 * 1024;
                        database.AzureServiceObjective = database.AzureEdition == "Standard" ? "P0" : "S0";
                        database.AzureEdition = database.AzureEdition == "Standard" ? "Premium" : "Standard";
                    }
                    database.Alter();
                });
        }

        /// <summary>
        /// Tests that the compatibility level can be scripted correctly on all the supported servers.
        /// The test was motivated by the fact that we missed to upgrade SMO during vBump in this
        /// area (see "$/Data Tools/SSMS_Main/Sql/ssms/smo/SMO/Main/src/DatabaseBase.cs", method
        /// AddCompatibilityLevel()).
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Compatibility_Level_Can_Be_Scripted_Correctly_On_AllServers()
        {
            this.ExecuteTest(
                server =>
                {
                    var serverVersion = server.Version;

                    // Get the highest compatibility level for this server by looking at the compatibility level of 'master'.
                    // This is an assumption. But it may be close enough to the reality.
                    // Also, gets the default compatibility level of new databases from the 'model' DB.
                    var masterCompatLevel = GetDbCompatibilityLevelFromServer(server, "master").Value;
                    var modelCompatLevel = GetDbCompatibilityLevelFromServer(server, "model") ?? masterCompatLevel;
                    // Flag that tells we got an 'UnsupportedVersionException': this is assumed to mean that
                    // SMO tried to script using compatibility level that is too low. As far as this test is concerned
                    // this is fine (because we tried to script correctly). We just stop trying with lower compatibility
                    // levels (because they would just fail the same way).
                    var keepTryingLowerCompatLevel = true;

                    // Assumption: compat levels are incremented by 10 at every release of SQL Server
                    // We stop going back at '90', since that's really ancient stuff.
                    for (byte cl = masterCompatLevel; keepTryingLowerCompatLevel && cl >= 90; cl -= 10)
                    {
                        _SMO.Database db = null;

                        try
                        {
                            var dbName = GenerateUniqueSmoObjectName("db_" + cl);

                            db = new _SMO.Database(server, dbName);

                            // The default compat level for Azure is not tied to the master compat level, so always set it
                            if (cl == modelCompatLevel)
                            {
                                TraceHelper.TraceInformation(
                                    "Created db object '{0}' with default compatLevel = '{1}'.", dbName,
                                    modelCompatLevel);
                            }
                            else
                            {
                                db.CompatibilityLevel = (_SMO.CompatibilityLevel)cl;
                                TraceHelper.TraceInformation(
                                    "Created db object '{0}' with compatLevel = '{1}'{2}.",
                                    dbName,
                                    cl,
                                    string.Format(" other than default '{0}'", modelCompatLevel));
                            }

                            var clExpected = UpgradeCompatibilityValueIfRequired(serverVersion, cl);

                            if (cl != clExpected)
                            {
                                TraceHelper.TraceInformation(
                                    "Adjusting expected compatibility level from '{0}' to '{1}', since we are targeting server version '{2}'",
                                    cl, clExpected, serverVersion);
                            }

                            // Check if the generated script has the line that sets the compatibility level
                            var hasLineWithCompatLevel = db.Script().Cast<string>().Any(line =>
                                line.EndsWith("SET COMPATIBILITY_LEVEL = " + clExpected));

                            if (cl == modelCompatLevel)
                            {
                                Assert.That(hasLineWithCompatLevel, Is.False,
                                    string.Format(
                                        "There should be no 'SET COMPATIBILITY_LEVEL = {0}' in generated script, since the compatibility level is the default one.",
                                        clExpected));
                            }
                            else
                            {
                                Assert.That(hasLineWithCompatLevel,
                                    string.Format(
                                        "There should be a 'SET COMPATIBILITY_LEVEL = {0}' in generated script, since the compatibility level is not the default one.",
                                        clExpected));
                            }

                            // Now let's create the DB to make sure the compatibility level is valid.
                            var isCreated = false;

                            try
                            {
                                db.Create();
                                isCreated = true;
                                db.Refresh();
                            }
                            catch (Exception e)
                            {
                                if (clExpected <= modelCompatLevel)
                                {
                                    Assert.Fail("Unable to create database with compatibility level {0} on {1}: {2}",
                                        clExpected, server.Name, e);
                                }
                            }
                            finally
                            {
                                if (isCreated)
                                {

                                    Assert.That(db.CompatibilityLevel, Is.EqualTo((_SMO.CompatibilityLevel)clExpected),
                                        "Created database has wrong compat level");
                                    db.Parent.DropKillDatabaseNoThrow(db.Name);
                                }
                            }
                        }
                        catch (_SMO.UnsupportedVersionException ex)
                        {
                            TraceHelper.TraceInformation(
                                "Caught exception '{0}'. Nothing to worry about because SMO scripting is working as expected.",
                                ex.Message);
                            keepTryingLowerCompatLevel = false;
                        }
                    }
                });
        }

        /// <summary>
        /// Verifies that the catalog_collation option is scripted correctly when scripting create for both new and existing databases.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void CatalogCollation_IsScriptedCorrectly()
        {
            this.ExecuteTest(server => TestCatalogCollationType(server, CatalogCollationType.DatabaseDefault));
            this.ExecuteTest(server => TestCatalogCollationType(server, CatalogCollationType.SQLLatin1GeneralCP1CIAS));
            this.ExecuteTest(server =>
            {
                _SMO.Database catalogCollationDb = server.CreateDatabaseDefinition("catalogCollationDb");

                Assert.Throws<SmoException>(
                    () =>
                    {
                        catalogCollationDb.CatalogCollation = CatalogCollationType.ContainedDatabaseFixedCollation;
                    },
                    "Should not have been able to set the catalog collation type to ContainedDatabaseFixedCollation");
            });
        }

        /// <summary>
        /// Verifies that database creation script is correct and can be successfully executed
        /// on a Managed Instance
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void ScriptCreateDatabaseForManagedInstance()
        {
            this.ExecuteTest(
                server =>
                {
                    Database db = null;
                    var dbName = Guid.NewGuid().ToString();
                    var newDbName = Guid.NewGuid().ToString();

                    try
                    {
                        db = new Database(server, dbName);
                        db.Create();

                        // Generate database creation script
                        //
                        var createDatabaseScript = db.Script().ToSingleString();

                        // Replace database name with a new one in a generated script
                        //
                        createDatabaseScript = createDatabaseScript.Replace(dbName, newDbName);

                        // Try to create a new database
                        //
                        server.ExecutionManager.ExecuteNonQuery(createDatabaseScript);
                    }
                    finally
                    {
                        // Drop created databases
                        //
                        db.Drop();
                        server.DropKillDatabaseNoThrow(newDbName);
                    }
                });
        }

        /// <summary>
        /// Tests that IsVarDecimalStorageFormatEnabled can be scripted properly when the
        /// database name contains single quotes characters.
        /// Doing so will prevent T-SQL injection attacks, which is what the related bug
        /// was about.
        /// <remarks>
        /// </remarks>
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
        public void IsVarDecimalStorageFormatEnabled_Is_Scripted_Correctly_WhenThe_DB_Name_Contains_Quotes()
        {
            // Couple of remarks:
            // - It is critical for the test to make sure that the db namme has quotes in it
            //   (while the currently generated names contain those characters we do not want to
            //   reply on that assumption, so I'm going to be explicit here)
            // - We need to create the DB, because IsVarDecimalStorageFormatEnabled is only scripted
            //   when the DB exists.
            this.ExecuteWithDbDrop(
                "db ' [ ]", /* dbname prefix */
                db =>
                {
                    if (db.IsVarDecimalStorageFormatSupported)
                    {
                        db.Refresh();

                        // Now we "dirty" the property to force the scripting...
                        db.IsVarDecimalStorageFormatEnabled = !db.IsVarDecimalStorageFormatEnabled;

                        // By default, this property is not scripted, so we explicitly ask for it...
                        var scriptingOptions = new _SMO.ScriptingOptions() { NoVardecimal = false };

                        // The key element here is that the 1st argument of the SP is escaped properly...
                        // Alternatively, instead of inspecting the generated script, we could have
                        // altered the db and observe some sort of failure. But that does not seem
                        // necessary for the purpose of this test.
                        var expectedLine =
                            string.Format(
                                "EXEC sys.sp_db_vardecimal_storage_format N'{0}', N'{1}'",
                                db.Name.Replace("'", "''"),
                                db.IsVarDecimalStorageFormatEnabled ? "ON" : "OFF");

                        var hasEscapedDbName =
                            db.Script(scriptingOptions).Cast<string>().Any(line => line == expectedLine);

                        //Assert.That(
                        //    scriptCollection,
                        //    Contains.Item(expectedLine),
                        //    "The expected line was not found in generated script.");

                        Assert.That(
                            hasEscapedDbName,
                            Is.True,
                            string.Format(
                                "The expected script fragment '{0}' was not found in generated script. Generated script is: {1}{2}",
                                expectedLine,
                                Environment.NewLine,
                                string.Join(Environment.NewLine, db.Script().Cast<string>())));
                    }
                    else
                    {
                        Assert.Fail("VarDecimal Compression is not supported on server '{0}'. This is unexpected.",
                            ServerName);
                    }
                });
        }

        /// <summary>
        /// Tests dropping a database with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Database_Sql16AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    _SMO.Database dropDB = new _SMO.Database(server, GenerateUniqueSmoObjectName("dropDb"));

                    string databaseScriptDropIfExistsTemplate = "DROP DATABASE IF EXISTS [{0}]";
                    string databaseScriptDropIfExists = string.Format(databaseScriptDropIfExistsTemplate,
                        dropDB.Name);

                    try
                    {
                        VerifySmoObjectDropIfExists(dropDB, server, databaseScriptDropIfExists);
                    }
                    catch (Exception)
                    {
                        // Drop database if exception occured.
                        //
                        server.KillDatabase(dropDB.Name);
                        throw;
                    }
                });
        }

        /// <summary>
        /// Scripting a database should not require access to sys.master_files.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14, MaxMajor = 14)]
        public void When_user_is_DbOwner_FileGroup_Files_are_accessible()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    if (ServerContext.LoginMode == ServerLoginMode.Integrated)
                    {
                        Trace.TraceInformation($"Skipping DbOwner_FileGroup test on {ServerContext.Name} because the server doesn't support SQL auth");
                        return;
                    }
                    string password = SqlTestRandom.GeneratePassword();
                    var dboLogin = database.Parent.CreateLogin(GenerateUniqueSmoObjectName("login"),
                        _SMO.LoginType.SqlLogin, password);
                    ServerConnection serverConnection = null;
                    try
                    {
                        serverConnection = new ServerConnection(
                            database.Parent.Name, dboLogin.Name, password) { NonPooledConnection = true };
                        database.SetOwner(dboLogin.Name);
                        var server = new _SMO.Server(serverConnection);
                        var testDatabase = server.Databases[database.Name];
                        Assert.That("PRIMARY", Is.EqualTo(testDatabase.DefaultFileGroup),
                            "Unable to see primary file group");
                        Assert.That(testDatabase.FileGroups[0].Files.Count, Is.EqualTo(1),
                            "Unable to see file in primary file group");
                    }
                    finally
                    {
                        // We can't drop the temp login without resetting the owner
                        //We use sa because the original owner may not necessarily have a login
                        //(for example if they were a part of a SG) and since we're dropping this DB
                        //after the test anyways we don't really care what the owner is
                        database.SetOwner("sa");
                        if (serverConnection != null)
                        {
                            serverConnection.Disconnect();
                        }
                        try
                        {
                            dboLogin.Drop();
                        } catch
                        {

                        }
                    }
                });
        }

        private static readonly string[] onOffPrimary = new String[] { "ON", "OFF", "PRIMARY" };

        private static readonly string[] earlySqlPropertyNames =
        {
            "MAXDOP",
            "LEGACY_CARDINALITY_ESTIMATION",
            "PARAMETER_SNIFFING",
            "QUERY_OPTIMIZER_HOTFIXES"
        };

        /**
         * Properties that can't be altered on secondaries. The name is a substring of the config name - so any configs
         * which have that substring in their name will not be used to target secondaries.
         */
        private static readonly string[] nonAlterablePropertiesOnSecondary =
        {
            "IDENTITY_CACHE",
            "DISABLE_GLOBAL_TEMP_TABLE_AUTODROP",
            "GLOBAL_TEMPORARY_TABLE_AUTO_DROP"
        };


        /// <summary>
        /// Test the generic database scoped configurations of SMO on SQL16 and later and on Azure v12 (Sterling).
        /// Currently disabled for 150+ because the new configs don't follow the "ON" and "OFF" value convention
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13, MaxMajor = 14)]
        public void DatabaseScopedConfiguration_VerifyConfigurationOptions_CanBeReadAndModified()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    Assert.That(database.DatabaseScopedConfigurations.Count, Is.GreaterThanOrEqualTo(4),
                        "The database scoped configurations at least have 4 existed options.");

                    // Normal case. Verify the alter operation of database scoped configurations could be executed as expected.
                    // Here, only includes the MaxDop and other ON/OFF type configurations.
                    foreach (_SMO.DatabaseScopedConfiguration config in database.DatabaseScopedConfigurations)
                    {
                        // ELEVATE_ONLINE and ELEVATE_RESTORE don't use "ON"
                        if (config.Name == "MAXDOP" || (onOffPrimary.Contains(config.Value) && !config.Name.ToUpperInvariant().Contains("ELEVATE_")))
                        {
                            // Get the configuration value and set the target value opposite.
                            string originalValue = config.Value;
                            string targetValue;
                            if (config.Name.Contains("MAXDOP"))
                            {
                                // Because 0 and 1 are also used for on/off options, set it as 2.
                                targetValue = "2";
                            }
                            else
                            {
                                targetValue = originalValue == "ON" ? "OFF" : "ON";
                            }

                            // Verify the configuration for the primary and secondary on the offline and online scenarios.
                            // Here, the identity cache is a special configuration that's only supported by the primary not secondary.
                            VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: false, isEarly: false, isOnline: true, shouldSucceed: true);
                            VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: false, isEarly: false, isOnline: false, shouldSucceed: true);

                            var shouldSuccessOnSecondaries = !nonAlterablePropertiesOnSecondary.Any(p => config.Name.Contains(p));
                            VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: true, isEarly: false, isOnline: true, shouldSucceed: shouldSuccessOnSecondaries);
                            VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: true, isEarly: false, isOnline: false, shouldSucceed: shouldSuccessOnSecondaries);

                            // Before the generic options, there are 4 early APIs for MAXDOP, LEGACY_CARDINALITY_ESTIMATION, PARAMETER_SNIFFING and QUERY_OPTIMIZER_HOTFIXES.
                            // Here, verify the alter configuration through the early API to make sure the consistence between the early and generic APIs.
                            if (earlySqlPropertyNames.Contains(config.Name))
                            {
                                VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: false, isEarly: true, isOnline: true, shouldSucceed: true);
                                VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: false, isEarly: true, isOnline: false, shouldSucceed: true);

                                VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: true, isEarly: true, isOnline: true, shouldSucceed: true);
                                VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, config.Name, targetValue, forSecondary: true, isEarly: true, isOnline: false, shouldSucceed: true);
                            }
                        }
                    }

                    // Corner Case. The configuration name and value should be case-insensitive.
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "legacy_cardinality_estimation", "oFF", forSecondary: false, isEarly: false, isOnline: true, shouldSucceed: true);
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "legAcy_cardiNality_Estimation", "on", forSecondary: false, isEarly: false, isOnline: false, shouldSucceed: true);

                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "LEGACY_CARDINALITY_ESTIMATION", "Off", forSecondary: true, isEarly: false, isOnline: true, shouldSucceed: true);
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "legAcy_cardiNality_Estimation", "On", forSecondary: true, isEarly: false, isOnline: false, shouldSucceed: true);

                    // Negative Case. Test the unknown configuration names for the primary and secondary on the offline and online scenarios.
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "unknown_config_name", "unknown_value", forSecondary: false, isEarly: false, isOnline: true, shouldSucceed: false);
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "unknown_config_name", "unknown_value", forSecondary: false, isEarly: false, isOnline: false, shouldSucceed: false);

                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "unknown_config_name", "unknown_value", forSecondary: true, isEarly: false, isOnline: true, shouldSucceed: false);
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "unknown_config_name", "unknown_value", forSecondary: true, isEarly: false, isOnline: false, shouldSucceed: false);

                    // Negative Case. Test the illegal configuration values for the primary and secondary on the offline and online scenarios.
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "MAXDOP", "illegal_value", forSecondary: false, isEarly: false, isOnline: true, shouldSucceed: false);
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "MAXDOP", "illegal_value", forSecondary: false, isEarly: false, isOnline: false, shouldSucceed: false);

                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "QUERY_OPTIMIZER_HOTFIXES", "illegal_value", forSecondary: true, isEarly: false, isOnline: true, shouldSucceed: false);
                    VerifyMaxdopOrOnOffOptionConfiguration(database.Parent, "QUERY_OPTIMIZER_HOTFIXES", "illegal_value", forSecondary: true, isEarly: false, isOnline: false, shouldSucceed: false);
                });
        }


        /// <summary>
        /// When scripting CREATE DATABASE targeting Managed Instances, some clauses should not be scripted,
        /// as they are not supported. Those are:
        /// ... ON...., ...LOG ON..., COLLATE, CATALOG_COLLATION, ALTER DATABASE SET READ_WRITE/READONLY
        ///
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, Edition = DatabaseEngineEdition.Enterprise)]
        public void DontScript_UnsupportedOptions_on_ManagedServer()
        {
            this.ExecuteTest(
                server =>
                {
                    _SMO.Database testDb = server.CreateDatabaseWithRetry("TestDb_" + new Random().Next());

                    // Filegroups do not get scripted as part of CREATE DATABASE
                    // but later with ALTER DB ADD FILEGROUP
                    //
                    FileGroup fg1 = new FileGroup(testDb, "File_Group_1", false);
                    FileGroup fg2 = new FileGroup(testDb, "File_Group_2", false);

                    // Use non-default filegroup
                    //
                    testDb.FileGroups["PRIMARY"].IsDefault = false;
                    fg2.IsDefault = true;

                    fg1.Files.Add(new DataFile(fg1, "file_1"));
                    fg1.Files.Add(new DataFile(fg1, "file_2"));
                    fg2.Files.Add(new DataFile(fg2, "file_3"));
                    fg2.Files.Add(new DataFile(fg2, "file_4"));

                    testDb.FileGroups.Add(fg1);
                    testDb.FileGroups.Add(fg2);

                    var script = new StringCollection();
                    var opts = new _SMO.ScriptingOptions
                    {
                        TargetDatabaseEngineEdition = DatabaseEngineEdition.SqlManagedInstance,
                        DriAll = true
                    };

                    script = testDb.Script(opts);
                    bool createStatementFound = false;
                    bool alterStatementFound = false;
                    bool fileGroupOneFound = false;
                    bool fileGroupTwoFound = false;
                    bool defaultFileGroupSet = false;

                    foreach (String str in script)
                    {
                        string strLowerCase = str.ToLower();

                        if (strLowerCase.Contains("create database"))
                        {
                            Assert.That(!createStatementFound, "CREATE DATABASE statement found more than once!");

                            createStatementFound = true;

                            Assert.That(!strLowerCase.Contains(" on "), " ON clause found in the statement: " + str);
                            Assert.That(!strLowerCase.Contains("log on"), " LOG ON clause found in the statement: " + str);
                            Assert.That(!strLowerCase.Contains(" catalog_collation "), " CATALOG_COLLATION clause found in the statement: " + str);
                        }
                        else if (strLowerCase.Contains("alter database"))
                        {
                            alterStatementFound = true;

                            if (strLowerCase.Contains("add filegroup [file_group_1]"))
                            {
                                Assert.That(!fileGroupOneFound, "File group 1 found");
                                fileGroupOneFound = true;
                            }

                            if (strLowerCase.Contains("add filegroup [file_group_2]"))
                            {
                                Assert.That(!fileGroupTwoFound, "File group 2 found");
                                fileGroupTwoFound = true;
                            }

                            if (strLowerCase.Contains("modify filegroup [file_group_2] default"))
                            {
                                Assert.That(!defaultFileGroupSet, "Default File group found");
                                defaultFileGroupSet = true;
                            }

                            if (!strLowerCase.Contains("filestream") && !strLowerCase.Contains("query_store"))
                            {
                                Assert.That(!strLowerCase.Contains("read_only"), " READ_ONLY clause found in the statement: " + str);
                                Assert.That(!strLowerCase.Contains("read_write"), " READ_WRITE clause found in the statement: " + str);
                            }
                        }
                    }

                    Assert.That(createStatementFound, "CREATE DATABASE statement not found! Generated script: " + script.ToSingleString());
                    Assert.That(alterStatementFound, "ALTER DATABASE statement not found! Generated script: " + script.ToSingleString());
                    Assert.That(defaultFileGroupSet, "Default file group not set correctly! Generated script: " + script.ToSingleString());
                    Assert.That(fileGroupOneFound, "First file group not scripted! Generated script: " + script.ToSingleString());
                    Assert.That(fileGroupTwoFound, "Second file group not scripted! Generated script: " + script.ToSingleString());

                    testDb.Drop();
                });
        }

        /// <summary>
        /// Tests altering the DataFile property growth for database through SMO for all Server.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void SmoDatabase_Alter_DataFile_Property_Growth()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    //FILEGROWTH > 2048 GB
                    var fileGrowth = 2049.0 * 1024.0 * 1024.0;
                    var maxSize = 16373 * 1024.0 * 1024.0;

                    database.FileGroups[0].Files[0].MaxSize = maxSize;
                    database.FileGroups[0].Files[0].GrowthType = FileGrowthType.KB;
                    database.FileGroups[0].Files[0].Growth = fileGrowth;

                    database.Alter();

                    Assert.That(database.FileGroups[0].Files[0].Growth, Is.EqualTo(fileGrowth), "Unexpected value of Growth property");
                    Assert.That(database.FileGroups[0].Files[0].MaxSize, Is.EqualTo(maxSize), "Unexpected value of MaxSize property");
                    Assert.That(database.FileGroups[0].Files[0].GrowthType, Is.EqualTo(FileGrowthType.KB), "Unexpected value of GrowthType property");
                });
        }

        /// <summary>
        /// Tests adding files and filegroups to Managed Instances.
        /// </summary>
        /// <remarks>This method is mostly designed to cover the logic in
        /// %BASEDIR%\src\Microsoft\SqlServer\Management\Smo\files.cs
        /// </remarks>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise | DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void SmoDatabase_AddFilesAndFilegroups()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    var filenameFileWithProperties =
                        database.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance
                        ? "FileName_is_ignored_on_managed_instances."
                        : System.IO.Path.ChangeExtension(database.FileGroups[0].Files[0].FileName, ".newfile.1");
                    var filenameFileWithDefaultProperties =
                        database.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance
                        ? "FileName_is_ignored_on_managed_instances."
                        : System.IO.Path.ChangeExtension(database.FileGroups[0].Files[0].FileName, ".newfile.2");

                    // Add new filegroup
                    FileGroup newFileGroup = new FileGroup(database, "NewFileGroup", isFileStream: false);
                    database.FileGroups.Add(newFileGroup);
                    database.Alter();
                    Assert.That(database.FileGroups["NewFileGroup"], Is.Not.Null, "Filegroup not properly created");

                    // Add new file with a couple of properties set
                    var fileWithProperties = new DataFile(database.FileGroups["NewFileGroup"], "FileWithProperties", filenameFileWithProperties)
                    {
                        Size = 0x2000,
                        MaxSize = 0x20000
                    };
                    database.FileGroups["NewFileGroup"].Files.Add(fileWithProperties);
                    database.Alter();
                    Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithProperties"], Is.Not.Null, "File with properties set not added.");
                    Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithProperties"].MaxSize, Is.EqualTo(0x20000), "File MaxSize property not scripted properly.");
                    Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithProperties"].Size, Is.EqualTo(0x2000), "File Size property not scripted properly.");

                    // Add new file with default values for properties
                    DataFile fileWithDefaultProperties = new DataFile(database.FileGroups["NewFileGroup"], "FileWithDefaultProperties", filenameFileWithDefaultProperties);
                    database.FileGroups["NewFileGroup"].Files.Add(fileWithDefaultProperties);
                    database.Alter();
                    Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithDefaultProperties"], Is.Not.Null, "File with default properties not properly created.");
                    Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithDefaultProperties"].MaxSize, Is.EqualTo(-1), "File MaxSize property not scripted properly.");
                    Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithDefaultProperties"].Size, Is.GreaterThan(0), "File Size property not scripted properly.");

                    // Alter an existing file
                    {
                        var fileToBeAltered = database.FileGroups["NewFileGroup"].Files["FileWithDefaultProperties"];
                        var expectedMaxSize = fileToBeAltered.Size + fileToBeAltered.Growth;
                        fileToBeAltered.MaxSize = expectedMaxSize;
                        fileToBeAltered.Alter();
                        Assert.That(database.FileGroups["NewFileGroup"].Files["FileWithDefaultProperties"].MaxSize, Is.EqualTo(expectedMaxSize), "File MaxSize property not not altered properly.");
                    }

                    // Alter an existing file without changing it => nothing is scripted
                    {
                        var previousSqlExecutionMode = database.ExecutionManager.ConnectionContext.SqlExecutionModes;
                        try
                        {
                            var fileToBeAltered = database.FileGroups["NewFileGroup"].Files["FileWithDefaultProperties"];
                            database.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;

                            fileToBeAltered.Alter();

                            var script = database.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                            Assert.That(script, Is.Empty, "Alter should be a no-op");
                        }
                        finally
                        {
                            database.ExecutionManager.ConnectionContext.SqlExecutionModes = previousSqlExecutionMode;
                        }
                    }
                });
        }

        #endregion //Scripting Tests

        #region Functionality Tests

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoDatabase_Offline_Create_with_dbscoped_config_does_not_set_secondary_value()
        {
            this.ExecuteTest(() =>
                {
                    ExecuteMethodWithDbDrop(this.ServerContext, "offlineCreate",
                        database =>
                        {
                            database.DatabaseScopedConfigurations["BATCH_MODE_ADAPTIVE_JOINS"].Value = "OFF";
                            database.Parent.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;
                            Assert.DoesNotThrow(database.Create, "Create should succeed");
                            var text = database.Parent.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                            Assert.That(text, Has.No.Member("ALTER DATABASE SCOPED CONFIGURATION FOR SECONDARY SET BATCH_MODE_ADAPTIVE_JOINS = OFF"), "Script should not set secondary value");
                        });
                });
        }
        /// <summary>
        /// Verifies that we can create and drop databases after populating a SMO Database object from the server
        /// </summary>
        /// <remarks>Fix for TFS#4482442</remarks>
        [TestMethod]
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        public void SmoDatabase_WhenModelDatabaseObjectIsOpened_CanCreateOtherUserDatabases()
        {
            this.ExecuteTest(
                server =>
                {
                    //First populate Database object. The regression is that a property in the Database object holds a
                    //connection to every database (including model) but create requires exclusive access to the model
                    //DB and so will fail if there's an open connection at that point
                    _SMO.Database modelDb = server.Databases["model"];

                    _SMO.Database db = null;
                    try
                    {
                        db = server.CreateDatabaseWithRetry(this.TestContext.TestName);
                        //Don't use SMOTestHelpers here since that kills all connections before dropping
                        //(and the bug is that SMO keeps a connection to the DB open so it can't be dropped)
                        db.Drop();
                    }

                    finally
                    {
                        //In case of an error we still want to try cleaning up (especially since that's what we're testing for)
                        if (db != null)
                        {
                            server.DropKillDatabaseNoThrow(db.Name);
                        }

                    }

                });

        }

        /// <summary>
        /// Verify that querying the HasMemoryOptimizedObjects property does not throw when
        /// the property is supported, even if no memory optimized objects exist in the database.
        /// Regression test for TFS#9853110
        /// </summary>
        /// <remarks>Note - this test does NOT catch the regression if any FILESTREAM files
        /// exist on the server at all (even for other DBs). This is something
        /// that will be investigated along with the other issues outlined in TFS#9891601</remarks>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12)]
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Database_HasMemoryOptimizedObjectsDoesNotThrow_WhenNoMemoryOptimizedObjectsExist()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    Assert.DoesNotThrow(() => { var temp = database.HasMemoryOptimizedObjects; }, "Querying HasMemoryOptimizedObjects on a DB shouldn't throw even if no memory optimized objects exist");

                    // Managed Instances have pre-created in-memory optimized filegroup
                    //
                    if (database.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                    {
                        Assert.That(database.HasMemoryOptimizedObjects, Is.False, "The database shouldn't have any memory optimized objects");
                    }
                });
        }

        /// <summary>
        ///Verifies that we can use SMO to successfully change the owner of a DB
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_CanChangeOwner()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    if (SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.NotSpecified && SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.SqlPassword)
                    {
                        Trace.TraceWarning($"Skipping CanChangeOwner test on {SqlConnectionStringBuilder.DataSource} because SQL logins are not available");
                        return;
                    }
                    string originalOwner = database.Owner;
                    var login = database.Parent.CreateLogin(GenerateUniqueSmoObjectName("login"), _SMO.LoginType.SqlLogin, SqlTestRandom.GeneratePassword());
                    try
                    {

                        database.SetOwner(login.Name);
                        database.Refresh();
                        Assert.That(database.Owner, Is.EqualTo(login.Name), "Owner of database was not changed to '{0}' after calling SetOwner");
                    }
                    finally
                    {
                        //Cleanup the login. Have to set owner to something else first though otherwise we can't drop the login
                        database.SetOwner(originalOwner);
                        login.Drop();
                    }
                });
        }

        /// <summary>
        ///Verifies that we can use SMO to successfully change the owner of a DB,
        /// with the login specified already being mapped to a non-dbo user in the DB
        /// but using overrideIfAlreadyUser to have SMO drop that user before changing
        /// the owner.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_CanChangeOwner_WithExistingUser()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    if (SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.NotSpecified && SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.SqlPassword)
                    {
                        Trace.TraceWarning($"Skipping CanChangeOwner test on {SqlConnectionStringBuilder.DataSource} because SQL logins are not available");
                        return;
                    }
                    string originalOwner = database.Owner;
                    var login = database.Parent.CreateLogin(GenerateUniqueSmoObjectName("login"), _SMO.LoginType.SqlLogin, SqlTestRandom.GeneratePassword());
                    database.CreateUser(GenerateUniqueSmoObjectName("user"), login.Name);
                    try
                    {
                        database.SetOwner(login.Name, dropExistingUser: true);
                        database.Refresh();
                        Assert.That(database.Owner, Is.EqualTo(login.Name), "Owner of database was not changed to '{0}' after calling SetOwner");
                    }
                    finally
                    {
                        //Cleanup the login. Have to set owner to something else first though otherwise we can't drop the login
                        database.SetOwner(originalOwner);
                        login.Drop();
                    }
                });
        }

        /// <summary>
        ///Verifies that we can use SMO to successfully change the owner of a DB,
        /// with the login specified already being mapped to a non-dbo user. This does
        /// NOT specify overrideIfAlreadyUser, which means this operation will fail due
        /// to the existing User mapping (expected).
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_ChangeOwnerWithExistingUserAndNoOverride_ThrowsException()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    if (SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.NotSpecified && SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.SqlPassword)
                    {
                        Trace.TraceWarning($"Skipping ChangeOwner test on {SqlConnectionStringBuilder.DataSource} because SQL logins are not available");
                        return;
                    }
                    string originalOwner = database.Owner;
                    var login = database.Parent.CreateLogin(GenerateUniqueSmoObjectName("login"), _SMO.LoginType.SqlLogin, SqlTestRandom.GeneratePassword());
                    database.CreateUser(GenerateUniqueSmoObjectName("user"), login.Name);
                    try
                    {
                        //We expect this to throw since you cannot have a login mapped to multiple users in a db, and
                        //changing the owner will automatically set the dbo user to the specified login so if a non-dbo
                        //user already exists the statement will fail
                        Assert.That(
                            () => { database.SetOwner(login.Name, dropExistingUser: false); },
                            Throws.TypeOf<_SMO.FailedOperationException>().
                            With.InnerException.InnerException.Property("Message").Contains("The proposed new database owner is already a user or aliased in the database."));
                    }
                    finally
                    {
                        //Cleanup the login. Have to set owner to something else first though otherwise we can't drop the login
                        database.SetOwner(originalOwner);
                        login.Drop();
                    }
                });
        }

        /// <summary>
        /// Tests the database comparer is selected correctly based on either catalog collation type, or by default in unsupported versions
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_DBComparerTest()
        {
            this.ExecuteTest(
                server =>
                {
                    // DATABASE_DEFAULT catalog collation type is testable on all versions, as this is the default when not explicitly specified.
                    //
                    TestDBComparer(server, CatalogCollationType.DatabaseDefault);

                    // Only test the explicit Catalog_Collation peroperty on supported versions.
                    //
                    if ((server.DatabaseEngineType.Equals(DatabaseEngineType.Standalone) && server.VersionMajor >= 15) ||
                        server.DatabaseEngineType.Equals(DatabaseEngineType.SqlAzureDatabase))
                    {
                        TestDBComparer(server, CatalogCollationType.SQLLatin1GeneralCP1CIAS);
                    }

                    // Contained DB is only supported in standalone. Except from SqlDatabaseEdge
                    //
                    if (server.DatabaseEngineType.Equals(DatabaseEngineType.Standalone) && server.VersionMajor >= 11 && !server.DatabaseEngineEdition.Equals(DatabaseEngineEdition.SqlDatabaseEdge))
                    {
                        // Enabled Containment if necessary.
                        //
                        bool isContainmentEnabled = server.Configuration.ContainmentEnabled.ConfigValue != 0;
                        if (!isContainmentEnabled)
                        {
                            server.Configuration.ContainmentEnabled.ConfigValue = 1;
                            server.Configuration.Alter();
                        }

                        TestDBComparer(server, CatalogCollationType.ContainedDatabaseFixedCollation);

                        // Disable containment if necessary.
                        //
                        if (!isContainmentEnabled)
                        {
                            server.Configuration.ContainmentEnabled.ConfigValue = 0;
                            server.Configuration.Alter();
                        }
                    }
                });
        }

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 14, DatabaseEngineType = DatabaseEngineType.Standalone)]
        //[SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.Express)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_CheckTables_sets_LastGoodDbCheckTime()
        {
            ExecuteWithDbDrop((db) =>
            {
                var time1 = (DateTime)db.ExecutionManager.ConnectionContext.ExecuteScalar("select GETDATE()");
                try
                {
                    _ = db.CheckTables(RepairType.None);
                    var time2 = (DateTime)db.ExecutionManager.ConnectionContext.ExecuteScalar("select GETDATE()");
                    db.Refresh();
                    db.Parent.SetDefaultInitFields(typeof(_SMO.Database), "LastGoodCheckDbTime");
                    var dt = db.LastGoodCheckDbTime;
                    Assert.That(dt, Is.InRange(time1, time2), "LastGoodCheckDbTime");
                }
                catch (FailedOperationException foe) when (foe.GetBaseException() is SqlException sqlex && sqlex.Number == 5030)
                {
                    TraceHelper.TraceInformation("DBCC failed because the database couldn't be locked");
                }
            });
        }

        /// <summary>
        /// Tests that renaming a database doesn't break functionality of the SMO object. Test for bug#12773209 where
        /// renaming a DB on an Azure server would cause the DB object to cease working until a new server connection
        /// was made.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_Rename_Does_Not_Break_Functionality()
        {
            ExecuteWithDbDrop((db) =>
            {
                // Renaming a DB shouldn't break funcionality - in this case we test that by just trying to call script
                // and verifying that it contains the new name.
                string newName = db.Name + "_New";
                db.Rename(newName);
                Assert.That(db.Script().ToSingleString(), Contains.Substring(_SMO.Util.EscapeString(newName, ']')));
                db.Parent.Databases.ClearAndInitialize($"[@Name='{Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(newName)}']", new[] { "Name" });
                Assert.That(db.Parent.Databases.OfType<Database>().Select(parentDb => parentDb.Name), Has.Member(newName));
                Assert.That(db.Parent.Databases[newName].Script().ToSingleString(), Contains.Substring(_SMO.Util.EscapeString(newName, ']')));
            });
        }

        [TestMethod]
        public void Database_EnumObjectPermissions_works()
        {
            ExecuteFromDbPool(db =>
            {
                ObjectPermissionInfo[] permissions = new ObjectPermissionInfo[0];
                Assert.DoesNotThrow(() => permissions = db.EnumObjectPermissions(), $"{nameof(db.EnumObjectPermissions)}");
                Assert.That(permissions, Is.Not.Null.And.Not.Empty, "Permissions should have entries");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void Database_Test_Encryption_Objects()
        {
            ExecuteWithDbDrop((db) =>
            {
                CreateTestMasterKey(db);
                String password1 = "password_1234";
                String password2 = "password_2345";
                String password3 = "password_3456";

                String keyName = GenerateUniqueSmoObjectName("asym_key_name", maxLength: 12);
                AsymmetricKey key = new AsymmetricKey(db, keyName);
                AsymmetricKeyEncryptionAlgorithm encrAlgo = AsymmetricKeyEncryptionAlgorithm.Rsa2048;

                key.Create(encrAlgo, password1);

                db.Refresh();

                Assert.That(db.AsymmetricKeys[keyName], Is.Not.Null, "Asymmetric key should be present in the db");
                Assert.That(db.AsymmetricKeys[keyName].Name, Is.EqualTo(keyName), "Key name should be equal to the value provided in key initialization");
                Assert.That(db.AsymmetricKeys[keyName].KeyEncryptionAlgorithm, Is.EqualTo(encrAlgo), "Encryption algorithm should be equal to the value provided in key initialization");

                String certName = GenerateUniqueSmoObjectName("cert_name", maxLength: 12);
                Certificate cert = new Certificate(db, certName);
                cert.Subject = "SMO test cert subject";

                cert.Create(password2);

                db.Refresh();

                Assert.That(db.Certificates[certName], Is.Not.Null, "Certificate should be present in the db");
                Assert.That(db.Certificates[certName].Name, Is.EqualTo(certName), "Certificate name should be equal to the value provided in initialization");

                keyName = GenerateUniqueSmoObjectName("sym_key_name", maxLength: 12);
                SymmetricKey symKey = new SymmetricKey(db, keyName);
                SymmetricKeyEncryption keyParams = new SymmetricKeyEncryption(KeyEncryptionType.Password, password3);
                SymmetricKeyEncryptionAlgorithm symEncrAlgo = SymmetricKeyEncryptionAlgorithm.Aes256;

                symKey.Create(keyParams, symEncrAlgo);

                db.Refresh();

                Assert.That(db.SymmetricKeys[keyName], Is.Not.Null, "Symmetric key should be present in the db");
                Assert.That(db.SymmetricKeys[keyName].Name, Is.EqualTo(keyName), "Key name should be equal to the value provided in key initialization");
                Assert.That(db.SymmetricKeys[keyName].EncryptionAlgorithm, Is.EqualTo(symEncrAlgo), "Encryption algorithm should be equal to the value provided in key initialization");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Database_EnumCandidateKeys()
        {

            ExecuteFromDbPool(db =>
            {
                var cols = new ColumnProperties[]
                {
                    new ColumnProperties("col1", DataType.Int){Identity = true, Nullable = false},
                    new ColumnProperties("col2", DataType.Int)
                };
                var idx = new IndexProperties[]
                {
                    new IndexProperties(){Name=SmoObjectHelpers.GenerateUniqueObjectName("pk") , IndexType = IndexType.ClusteredIndex, KeyType= IndexKeyType.DriPrimaryKey, ColumnNames = new[]{"col1"} }
                };
                var table = db.CreateTableDefinition("eck", schemaName: null, tableProperties: null, columnProperties: cols, indexProperties: idx, includeNameUniqueifier: true);
                table.Create();
                var index = table.Indexes[0];
                var dt = db.EnumCandidateKeys();
                var rows = dt.Rows.Cast<DataRow>().Select(r => (string)r["Table_Name"] + r["Name"]);
                Assert.That(rows, Has.One.EqualTo(table.Name + index.Name), "EnumCandidateKeys should include primary key index");
            });
        }

        /// <summary>
        /// Method to test Creating a new DB when Ledger = True.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.Express)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_IsLedger_On()
        {
            this.ExecuteTest(
                  server =>
                  {
                      var dbName = GenerateUniqueSmoObjectName("ledgerDB");
                      try
                      {
                          // Adding parameters to create Ledger Db
                          Database db = new Database(server, dbName, DatabaseEngineEdition.SqlDatabase)
                          {
                              IsLedger = true
                          };

                          // Creating database and making it sleep for 15s to retrieve the ledger property
                          // Clears old objects and initializes the collection
                          db.Create();
                          Thread.Sleep(15000);
                          server.Databases.ClearAndInitialize($"[@Name='{Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(dbName)}']", new[] { nameof(Database.IsLedger) });
                          var isLedgerDb = server.Databases[dbName].IsLedger;
                          Assert.That(isLedgerDb, Is.True, "IsLedger");
                      }
                      finally
                      {
                          server.DropKillDatabaseNoThrow(dbName);
                      }
                  });
        }

        /// <summary>
        /// Method to test Creating a new DB when Ledger = OFF.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.Express)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        public void Database_IsLedger_OFF()
        {
            this.ExecuteTest(
                  server =>
                  {
                      var dbName = GenerateUniqueSmoObjectName("ledgerDB");
                      try
                      {
                          // Adding parameters to create Ledger Db
                          Database db = new Database(server, dbName, DatabaseEngineEdition.SqlDatabase)
                          {
                              IsLedger = false
                          };

                          // Creating database and making it sleep for 15s to retrieve the ledger property
                          // Clears old objects and initializes the collection
                          db.Create();
                          Thread.Sleep(15000);
                          server.Databases.ClearAndInitialize($"[@Name='{Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(dbName)}']", new[] { nameof(Database.IsLedger) });
                          var isLedgerDb = server.Databases[dbName].IsLedger;
                          Assert.That(isLedgerDb, Is.False, "IsLedger");
                      }
                      finally
                      {
                          server.DropKillDatabaseNoThrow(dbName);
                      }
                  });
        }
        #endregion //Functionality Tests

        #region Helpers

        /// <summary>
        /// Verify the operations of the Maxdop and others ON/OFF configurations for the early and generic API.
        /// The operations of the database scoped configuration support the online and offline scenarios,
        /// where the database is existing for the online, or the database is creating for the offline. </summary>
        /// <param name="server">The server object</param>
        /// <param name="configName">The configuration name</param>
        /// <param name="targetValue"></param>
        /// <param name="forSecondary">For secondary or primary</param>
        /// <param name="isEarly">The early API or generic API</param>
        /// <param name="isOnline">The online or offline scenarios</param>
        /// <param name="shouldSucceed">Expect to run the operation successfully or not</param>
        private void VerifyMaxdopOrOnOffOptionConfiguration(_SMO.Server server, string configName, string targetValue, bool forSecondary, bool isEarly, bool isOnline, bool shouldSucceed)
        {
            // The offline scenario on Azure Databse is disabled, so skip its corresponding test.
            if (!isOnline && server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                return;
            }

            this.ExecuteMethodWithDbDrop(
               server,
               isOnline ? "online" : "offline",
               database =>
               {
                   // Set the DB collation to the case-sensitive one to prove it doesn't have an effect to the scoped configurations.
                   database.Collation = "SQL_Latin1_General_CP1_CS_AS";

                   // For the online scenario, the database is created before the alter of the database scoped configuration.
                   // For the offline scenario, the database will be created after the alter of the database scoped configuration.
                   if (isOnline)
                   {
                       database.Create();
                   }

                   try
                   {
                       TraceHelper.TraceInformation("Alter the database scoped configuration {0}{1} with Value {2}, Database {3}, Scenario {4} and API: {5}",
                           configName,
                           forSecondary ? " for secondary" : "",
                           targetValue,
                           database.Name,
                           isOnline ? "Online" : "Offline",
                           isEarly ? "Early" : "Generic");

                       AlterMaxdopOrOnOffOptionConfiguration(database, configName, forSecondary, isEarly, targetValue);

                       if (isOnline)
                       {
                           database.Alter();
                       }
                       else
                       {
                           database.Create();
                       }

                       // Throw an error if the operation is successful but it is expected to fail.
                       Assert.That(shouldSucceed, Is.True, String.Format("Alter database scoped configuration {0}{1} with value {2} should not be successful.",
                                   configName, forSecondary ? " for secondary" : "", targetValue));

                       // Verify the configuration values for the early and generic API.
                       Assert.That(targetValue, Is.EqualTo(GetMaxdopOrOnOffOptionConfiguration(database, configName, forSecondary, isEarly: false)).IgnoreCase);
                       if (isEarly)
                       {
                           Assert.That(targetValue, Is.EqualTo(GetMaxdopOrOnOffOptionConfiguration(database, configName, forSecondary, isEarly: true)).IgnoreCase);
                       }

                       // Verify the t-sql script of the database script configuration.
                       StringCollection createQuery = new StringCollection();
                       _SMO.ScriptingPreferences sp = new _SMO.ScriptingPreferences();
                       sp.ScriptForAlter = false;
                       database.ScriptAlter(createQuery, sp);

                       Assert.That(createQuery.ToSingleString(), Does.Contain(String.Format("ALTER DATABASE SCOPED CONFIGURATION{0}SET {1} = {2};",
                           forSecondary ? " FOR SECONDARY " : " ", configName, targetValue)).IgnoreCase);
                   }
                   catch (_SMO.SmoException e)
                   {
                       // Throw an error if the operation is failed but it is expected to success.
                       Assert.That(shouldSucceed, Is.False, String.Format("Alter database scoped configuration {0}{1} with value {2} should be successful.\nException:{3}",
                                   configName, forSecondary ? " for secondary" : "", targetValue, e.BuildRecursiveExceptionMessage()));
                   }
               });
        }

        /// <summary>
        /// Get the values of the Maxdop or other ON/OFF configurations from the early or generic APIs. </summary>
        /// <param name="database">The server object</param>
        /// <param name="configName">The configuration name</param>
        /// <param name="forSecondary">For secondary or primary</param>
        /// <param name="isEarly">From the early API or generic APIs</param>
        private string GetMaxdopOrOnOffOptionConfiguration(_SMO.Database database, string configName, bool forSecondary, bool isEarly)
        {
            if (!isEarly)
            {
                return !forSecondary ? database.DatabaseScopedConfigurations[configName].Value : database.DatabaseScopedConfigurations[configName].ValueForSecondary;
            }
            else
            {
                switch (configName)
                {
                    case "MAXDOP":
                        return !forSecondary ? database.MaxDop.ToString() : (database.MaxDopForSecondary == null ? "PRIMARY" : database.MaxDopForSecondary.ToString());
                    case "LEGACY_CARDINALITY_ESTIMATION":
                        return !forSecondary ? database.LegacyCardinalityEstimation.ToString() : database.LegacyCardinalityEstimationForSecondary.ToString();
                    case "PARAMETER_SNIFFING":
                        return !forSecondary ? database.ParameterSniffing.ToString() : database.ParameterSniffingForSecondary.ToString();
                    case "QUERY_OPTIMIZER_HOTFIXES":
                        return !forSecondary ? database.QueryOptimizerHotfixes.ToString() : database.QueryOptimizerHotfixesForSecondary.ToString();
                    default:
                        Assert.Fail(string.Format("{0} is not the early supported configuration", configName));
                        return "";
                }
            }
        }

        /// <summary>
        /// Alter the values of the Maxdop or other ON/OFF configurations from the early or generic APIs. </summary>
        /// <param name="database">The server object</param>
        /// <param name="configName">The configuration name</param>
        /// <param name="forSecondary">For secondary or primary</param>
        /// <param name="isEarly">From the early API or generic APIs</param>
        /// <param name="targetValue">The target configuration value</param>
        private void AlterMaxdopOrOnOffOptionConfiguration(_SMO.Database database, string configName, bool forSecondary, bool isEarly, string targetValue)
        {
            if (!isEarly)
            {
                if (!forSecondary)
                {
                    database.DatabaseScopedConfigurations[configName].Value = targetValue;
                }
                else
                {
                    database.DatabaseScopedConfigurations[configName].ValueForSecondary = targetValue;
                }
            }
            else
            {
                _SMO.DatabaseScopedConfigurationOnOff onOffValue = (_SMO.DatabaseScopedConfigurationOnOff)Enum.Parse(
                                                                        typeof(_SMO.DatabaseScopedConfigurationOnOff),
                                                                        (targetValue == "1" || targetValue == "0") ? "ON" : targetValue,
                                                                        ignoreCase: true);
                switch (configName)
                {
                    case "MAXDOP":
                        if (!forSecondary)
                        {
                            database.MaxDop = Convert.ToInt32(targetValue);
                        }
                        else
                        {
                            if (targetValue == "PRIMARY")
                            {
                                database.MaxDopForSecondary = null;
                            }
                            else
                            {
                                database.MaxDopForSecondary = Convert.ToInt32(targetValue);
                            }
                        }
                        break;
                    case "LEGACY_CARDINALITY_ESTIMATION":
                        if (!forSecondary)
                        {
                            database.LegacyCardinalityEstimation = onOffValue;
                        }
                        else
                        {
                            database.LegacyCardinalityEstimationForSecondary = onOffValue;
                        }
                        break;
                    case "PARAMETER_SNIFFING":
                        if (!forSecondary)
                        {
                            database.ParameterSniffing = onOffValue;
                        }
                        else
                        {
                            database.ParameterSniffingForSecondary = onOffValue;
                        }
                        break;
                    case "QUERY_OPTIMIZER_HOTFIXES":
                        if (!forSecondary)
                        {
                            database.QueryOptimizerHotfixes = onOffValue;
                        }
                        else
                        {
                            database.QueryOptimizerHotfixesForSecondary = onOffValue;
                        }
                        break;
                    default:
                        Assert.Fail(string.Format("{0} is not the early supported configuration", configName));
                        break;
                }
            }
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Database dropDB = (_SMO.Database)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Databases.Refresh();
            Assert.That(server.Databases[dropDB.Name], Is.Null, "Current database not dropped with DropIfExists.");
        }

        /// <summary>
        /// Helper method to query the server and get the CompatibilityLevel of the given database
        /// It is used to validate that SMO does "the right thing" when it comes to Compatibility Levels
        /// </summary>
        /// <param name="server">The SMO server object that represent the server we want to connect to</param>
        /// <param name="dbName">The name of the database whose CompatibilityLevel we want to get</param>
        /// <returns>The CompatibilityLevel or null if it could not be determined.</returns>
        private byte? GetDbCompatibilityLevelFromServer(_SMO.Server server, string dbName)
        {
            var cl =
                server.ExecutionManager.ExecuteScalar(
                    string.Format("SELECT compatibility_level FROM sys.databases WHERE name = '{0}'",
                    Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(dbName)));

            return cl != null ? (byte?)cl : (byte?)null;
        }

        /// <summary>
        /// Adjusts the compatibilityLevel based on the target server.
        /// This is odd, but that's what SMO does for really old compatibility levels.
        /// </summary>
        /// <param name="serverVersion"></param>
        /// <param name="compatibilityLevel"></param>
        /// <returns></returns>
        private byte UpgradeCompatibilityValueIfRequired(Version serverVersion, byte compatibilityLevel)
        {
            if (serverVersion.Major >= 11 && compatibilityLevel <= 80)
            {
                return 90;
            }

            if (serverVersion.Major == 10 && compatibilityLevel <= 70)
            {
                return 80;
            }

            if (serverVersion.Major == 9 && compatibilityLevel <= 65)
            {
                return 70;
            }

            return compatibilityLevel;
        }

        private void TestCatalogCollationType(_SMO.Server server, CatalogCollationType targetCatalogCollationType)
        {
            this.ExecuteMethodWithDbDrop(
                server,
                "catalogCollationDB_",
                database =>
            {
                database.CatalogCollation = targetCatalogCollationType;

                // Script the create command, and verify it matches the expected.
                //
                ScriptingOptions so = new ScriptingOptions();
                so.IncludeIfNotExists = true;
                so.IncludeDatabaseContext = true;
                so.ExtendedProperties = true;
                StringCollection sc = database.Script(so);
                String statement = sc.ToSingleString();
                TraceHelper.TraceInformation(statement);

                string expectedCatalogCollationValue = targetCatalogCollationType == CatalogCollationType.DatabaseDefault ? "DATABASE_DEFAULT" : "SQL_Latin1_General_CP1_CI_AS";
                string expectedCatalogCollationClause = String.Format(" WITH CATALOG_COLLATION = {0}", expectedCatalogCollationValue);

                // Managed instances don't support CATALOG_COLLATION but currently a bug in SMO (12800141) makes it so we can't use IsSupportedProperty
                if (database.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                {
                    Assert.That(statement, Does.Contain(expectedCatalogCollationClause), "Script does not contain the the correct CATALOG_COLLATION option.");
                }
                else
                {
                    Assert.That(statement, Does.Not.Contain(expectedCatalogCollationClause), "Script contains a CATALOG_COLLATION option even though it isn't supported on this engine edition.");
                }


                // Now perform the actual create, and verify that it matches the expected.
                //
                database.Create();
                database.Refresh();

                if (database.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                {
                    Assert.That(database.CatalogCollation, Is.EqualTo(targetCatalogCollationType),"Incorrect value for CatalogCollationType after refresh.");
                }
                else
                {
                    Assert.That(database.CatalogCollation, Is.EqualTo(CatalogCollationType.DatabaseDefault),"Incorrect value for CatalogCollationType after refresh. This edition only supports DATABASE_DEFAULT");
                }

                // From the existing object script create, and validate it contains the correct option.
                //
                sc = database.Script(so);
                statement = sc.ToSingleString();
                TraceHelper.TraceInformation(statement);
                if (database.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                {
                    Assert.That(statement, Does.Contain(expectedCatalogCollationClause),"create script does not contain the the correct CATALOG_COLLATION option.");
                }
                else
                {
                    Assert.That(statement, Does.Not.Contain(expectedCatalogCollationClause),"create script contains a CATALOG_COLLATION option even though it isn't supported on this engine edition.");
                }
            });
        }

        private void TestDBComparer(_SMO.Server server, CatalogCollationType targetCatalogCollationType)
        {
            this.ExecuteMethodWithDbDrop(
                server,
                "comparisonDB_",
                database =>
                {
                    if (targetCatalogCollationType.Equals(CatalogCollationType.ContainedDatabaseFixedCollation))
                    {
                        database.ContainmentType = ContainmentType.Partial;
                    }
                    else if (database.IsSupportedProperty("CatalogCollation"))
                    {
                        database.CatalogCollation = targetCatalogCollationType;
                    }

                    // Set a case sensitive collation for the DB.  For DATABASE_DEFAULT collations we expect case sensitivity.
                    //
                    database.Collation = "SQL_Latin1_General_CP1_CS_AS";

                    // Create the database, and test the comparer.
                    //
                    database.Create();
                    database.Refresh();

                    _SMO.StringComparer comp = database.GetDbComparer(false);
                    int result = comp.Compare("test", "TEST");

                    // For contained DBs and catalog collations set to SQLLatin1GeneralCP1CIAS we expect a case insensitive comparer.
                    // For DatabaseDefault catalog collations we expect a case sensitive comparer.
                    //
                    if ((database.IsSupportedProperty("ContainmentType") && database.ContainmentType.Equals(ContainmentType.Partial)) ||
                        (database.IsSupportedProperty("CatalogCollation") && database.CatalogCollation.Equals(CatalogCollationType.SQLLatin1GeneralCP1CIAS)))
                    {
                        Assert.That(result, Is.EqualTo(0), "Contained DB or case insensitive catalog collation should see interpret 'test' and 'TEST' as equal.");
                    }
                    else
                    {
                        Assert.That(result, Is.Not.EqualTo(0), "DATABASE_DEFAULT catalog collation should see interpret 'test' and 'TEST' as equal.");
                    }
                });
        }

        private void CreateTestMasterKey(_SMO.Database db)
        {
            if (db.MasterKey == null)
            {
                MasterKey mk = new MasterKey(db);
                mk.Create(SqlTestRandom.GeneratePassword());

                db.Refresh();

                mk = db.MasterKey;
                Assert.That(mk, Is.Not.Null, "Master key creation should succeed");
            }
        }

        #endregion Helpers
    }
}
