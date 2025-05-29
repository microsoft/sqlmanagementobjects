// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using VSTest = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// SMO scripting External Data Source TestSuite.
    /// </summary>
    [VSTest.TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand)]
    public partial class ExternalDataSource_SmoTestSuite : SqlTestBase
    {
        /// Tests creating, altering, dropping and scripting of Polybase external data source objects via SMO.
        /// Positive test steps:
        /// 1. Create a Polybase external data source with type and location properties.
        /// 2. Script create external data source with IncludeIfNotExists option set to true.
        /// 3. Verify the script contains expected information.
        /// 4. Script drop external data source with IncludeIfNotExists option set to true.
        /// 5. Verify the script contains expected information.
        /// 6. Drop the external data source and verify the count is 0.
        /// 7. Verify the script re-creates the external data source.
        /// 8. Alter the external data source properties.
        /// 9. Verify altered properties.
        /// 10. Test dropping external data source using the generated script.  Verify it was dropped correctly.
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13, MaxMajor = 15)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestArea(SqlTestArea.Polybase)]
        // DEVNOTE(MatteoT) 7/7/2019. Should we enable on SQL Azure yet (DBCC TRACEON does not work there).
        //                            Is it needed at all? Always?
        public void VerifyPositiveExternalDataSourceCreateAlterDropPolybase()
        {
            string[] externalDataSourceLocations = { @"hdfs://10.10.10.10:1000", @"hdfs://10.10.10.10:1100", @"wasbs://commondatabases@sqltoolstestsstorage.blob.core.windows.net/", @"wasbs://commondatabases2@sqltoolstestsstorage.blob.core.windows.net/" };
            string[] externalDataSourceResourceManagerLocations = { @"10.10.10.10:1010", @"10.10.10.10:1111" }; // test-only value for the resource manager location; the create/drop DDLs don't connect to in
            string[] externalDataSourceCredentials = { "cred1", "cred]1" };
            string[] externalDataSourceNames = { "eds1", "eds[]1", "eds'1", "eds--1" };

            this.ExecuteWithDbDrop(
                this.TestContext.TestName,
                database =>
                    {

                        database.ExecuteNonQuery(@"DBCC TRACEON(4631,-1)"); // enable the database scoped credential
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], externalDataSourceLocations[1], string.Empty, string.Empty, string.Empty, string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[2], externalDataSourceLocations[0], externalDataSourceLocations[1], externalDataSourceResourceManagerLocations[0], string.Empty, string.Empty, string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[3], externalDataSourceLocations[0], externalDataSourceLocations[1], externalDataSourceResourceManagerLocations[0], externalDataSourceResourceManagerLocations[1], string.Empty, string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[0], externalDataSourceLocations[0], externalDataSourceLocations[1], string.Empty, string.Empty, externalDataSourceCredentials[0], string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], externalDataSourceLocations[1], string.Empty, string.Empty, externalDataSourceCredentials[0], externalDataSourceCredentials[1]);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[2], externalDataSourceLocations[0], externalDataSourceLocations[1], externalDataSourceResourceManagerLocations[0], string.Empty, externalDataSourceCredentials[0], string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[3], externalDataSourceLocations[0], externalDataSourceLocations[1], externalDataSourceResourceManagerLocations[0], externalDataSourceResourceManagerLocations[1], externalDataSourceCredentials[0], string.Empty);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[0], externalDataSourceLocations[0], externalDataSourceLocations[1], externalDataSourceResourceManagerLocations[0], string.Empty, externalDataSourceCredentials[0], externalDataSourceCredentials[1]);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], externalDataSourceLocations[1], externalDataSourceResourceManagerLocations[0], externalDataSourceResourceManagerLocations[1], externalDataSourceCredentials[0], externalDataSourceCredentials[1]);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], externalDataSourceLocations[1], string.Empty, externalDataSourceResourceManagerLocations[1], string.Empty, externalDataSourceCredentials[1]);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], string.Empty, string.Empty, externalDataSourceResourceManagerLocations[1], string.Empty, externalDataSourceCredentials[1]);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[0], string.Empty, string.Empty, string.Empty, string.Empty, externalDataSourceCredentials[1]);
                        VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(database, externalDataSourceNames[1], externalDataSourceLocations[2], externalDataSourceLocations[3], string.Empty, string.Empty, externalDataSourceCredentials[0], externalDataSourceCredentials[1]);
                    });
        }

        /// Tests creating, altering, dropping and scripting of the GQ external data source objects via SMO.
        /// Positive test steps:
        /// 1. Create a GQ external data source.
        /// 2. Script create external data source with IncludeIfNotExists option set to true.
        /// 3. Verify the script contains expected information.
        /// 4. Script drop external data source with IncludeIfNotExists option set to true.
        /// 5. Verify the script contains expected information.
        /// 6. Drop the external data source and verify the count is 0.
        /// 7. Verify the script re-creates the external data source.
        /// 8. Test dropping external data source using the generated script. Verify it was dropped correctly.
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [SqlTestArea(SqlTestArea.Polybase)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void VerifyPositiveExternalDataSourceCreateAlterDropGQ()
        {
            this.ExecuteWithDbDrop("ExternalDataSourceSmo_",
                database =>
                {
                    string[] externalDataSourceLocations = { "abc.xyz.com", "external-server-name", "one's computer", "wasbs://commondatabases2@sqltoolstestsstorage.blob.core.windows.net/" };
                    string[] externalDataSourceCredentials = { "cred1", "cred[]1", "cred'1" };
                    string[] externalDataSourceDatabaseNames = { "database1", "database'1", "database]" };
                    string[] externalDataSourceShardMapNames = { "shardmap1", " shard map '1" };
                    string[] externalDataSourceNames = { "eds1", "eds[]1", "eds'1", "eds--1" };

                    // Create a few SHARD_MAP_MANAGER data sources.
                    VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ShardMapManager, externalDataSourceNames[0], externalDataSourceLocations[0], externalDataSourceDatabaseNames[0], externalDataSourceShardMapNames[0], externalDataSourceCredentials[0]);
                    VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ShardMapManager, externalDataSourceNames[1], externalDataSourceLocations[1], externalDataSourceDatabaseNames[1], externalDataSourceShardMapNames[1], externalDataSourceCredentials[1]);
                    VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ShardMapManager, externalDataSourceNames[2], externalDataSourceLocations[2], externalDataSourceDatabaseNames[2], externalDataSourceShardMapNames[0], externalDataSourceCredentials[2]);
                    VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ShardMapManager, externalDataSourceNames[3], externalDataSourceLocations[3], externalDataSourceDatabaseNames[0], externalDataSourceShardMapNames[1], externalDataSourceCredentials[0]);

                    // Create a few RDBMS data sources.
                    VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.Rdbms, externalDataSourceNames[0], externalDataSourceLocations[0], externalDataSourceDatabaseNames[0], null, externalDataSourceCredentials[0]);
                    VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.Rdbms, externalDataSourceNames[1], externalDataSourceLocations[1], externalDataSourceDatabaseNames[1], null, externalDataSourceCredentials[1]);

                    // Create a few ExternalGenerics data sources.
                    // DEVNOTE(MatteoT) 7/7/2019. These are not in SQL Azure yet?
                    // VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ExternalGenerics, externalDataSourceNames[0], externalDataSourceLocations[0], externalDataSourceDatabaseNames[0], null, externalDataSourceCredentials[0]);
                    // VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ExternalGenerics, externalDataSourceNames[1], externalDataSourceLocations[1], externalDataSourceDatabaseNames[1], null, externalDataSourceCredentials[1]);
                });
        }

        // DEVNOTE(MatteoT) 7/7/2019. The following test is temporarily disabled (bug in new ExternalGenerics stuff? Or just a test bug?)
        // <summary>
        // Same as above, but for ExternalGenerics which are only supported on v150+
        // </summary>
        //[VSTest.TestMethod]
        //[SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Linux)]
        //[SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Windows)]
        //[SqlTestArea(SqlTestArea.Polybase)]
        //public void VerifyPositiveExternalDataSourceSQLv150Plus()
        //{
        //    this.ExecuteWithDbDrop("ExternalDataSourceSmo_",
        //        database =>
        //        {
        //            string[] externalDataSourceLocations = { "sqlserver://127.0.0.1", "wasbs://commondatabases2@sqltoolstestsstorage.blob.core.windows.net/" };
        //            string[] externalDataSourceCredentials = { "cred1", "cred[]1" };
        //            string[] externalDataSourceDatabaseNames = { "database'1", "database]" };
        //            string[] externalDataSourceNames = { "eds[]1", "--eds'1", };
        //            // Create a few ExternalGenerics data sources.
        //            VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ExternalGenerics, externalDataSourceNames[0], externalDataSourceLocations[0], externalDataSourceDatabaseNames[0], null, externalDataSourceCredentials[0]);
        //            VerifyPositiveExternalDataSourceCreateDropHelperGQ(database, ExternalDataSourceType.ExternalGenerics, externalDataSourceNames[1], externalDataSourceLocations[1], externalDataSourceDatabaseNames[1], null, externalDataSourceCredentials[1]);
        //        });
        //}

        /// <summary>
        /// Tests dropping an external data source with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13, MaxMajor = 15, HostPlatform = HostPlatformNames.Windows)]
        // DEVNOTE(MatteoT): SQL v150 on Linux should be supported once I figure out how to enable hadoop.
        //[SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = HostPlatformNames.Linux)]
        public void SmoDropIfExists_ExternalDataSource_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                "ExternalDataSourceSmo_",
                database =>
                {
                    ExternalDataSource eds = new ExternalDataSource(database,
                        "eds_" + (this.TestContext.TestName ?? ""),
                        ExternalDataSourceType.Hadoop,
                        "hdfs://10.10.10.10:1000");

                    // 1. Try to drop external data source before it is created.
                    //
                    eds.DropIfExists();

                    eds.Create();

                    // 2. Verify the script contains expected statement.
                    //
                    ScriptingOptions so = new ScriptingOptions();
                    so.IncludeIfNotExists = true;
                    so.ScriptDrops = true;
                    StringCollection col = eds.Script(so);

                    StringBuilder sb = new StringBuilder();
                    StringBuilder scriptTemplate = new StringBuilder();
                    foreach (string statement in col)
                    {
                        sb.AppendLine(statement);
                    }
                    string dropIfExistsScripts = sb.ToString();
                    string scriptDropIfExistsTemplate = "IF  EXISTS";

                    Assert.That(dropIfExistsScripts.Contains(scriptDropIfExistsTemplate),
                                  "Drop with existence check is not scripted.");

                    // 3. Drop external data source with DropIfExists and check if it is dropped.
                    //
                    eds.DropIfExists();
                    database.ExternalDataSources.Refresh();
                    Assert.IsNull(database.ExternalDataSources[eds.Name],
                                    "Current external data source not dropped with DropIfExists.");

                    // 4. Try to drop already dropped external data source.
                    //
                    eds.DropIfExists();
                });
        }

        /// <summary>
        /// Executes positive tests for the CREATE EXTERNAL DATA SOURCE command for Polybase.
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <param name="externalDataSourceName">The external data source name.</param>
        /// <param name="externalDataSourceLocation">The external data source location property value.</param>
        /// <param name="newExternalDataSourceLocation">The new external data source location property value.</param>
        /// <param name="externalDataSourceResourceManagerLocation">The resource manager location property value.</param>
        /// <param name="newExternalDataSourceResourceManagerLocation">The new resource manager location property value.</param>
        /// <param name="externalDataSourceCredential">The credential property value.</param>
        /// <param name="newExternalDataSourceCredential">The new credential property value.</param>
        private void VerifyPositiveExternalDataSourceCreateAlterDropHelperPolybase(
            Database db,
            string externalDataSourceName,
            string externalDataSourceLocation,
            string newExternalDataSourceLocation,
            string externalDataSourceResourceManagerLocation,
            string newExternalDataSourceResourceManagerLocation,
            string externalDataSourceCredential,
            string newExternalDataSourceCredential)
        {
            // const definitions
            const string ExternalDataSourceCountQuery = @"SELECT COUNT(*) FROM sys.external_data_sources";
            const string ExternalDataSourceLocationQuery = @"SELECT Location FROM sys.external_data_sources";
            const string ExternalDataSourceResourceManagerLocationQuery = @"SELECT Resource_Manager_Location FROM sys.external_data_sources";
            const string ExternalDataSourceCredentialQuery = @"SELECT c.name FROM sys.database_scoped_credentials as c JOIN sys.external_data_sources as eds ON c.credential_id = eds.credential_id WHERE c.name = N'{0}'";
            const string DatabaseCredentialQuery = @"IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = '{0}') BEGIN CREATE DATABASE SCOPED CREDENTIAL [{1}] WITH IDENTITY = 'Test' END";
            const string DatabaseCredentialNameQuery = "SELECT name FROM sys.database_scoped_credentials WHERE name = N'{0}'";
            const string ExternalDataSourceTestName = "External Data Source Testing";

            ExternalDataSourceType dataSourceType = ExternalDataSourceType.Hadoop;

            //
            // Step 1. Create an external data source with type and location properties.
            //
            TraceHelper.TraceInformation("Step 1: {0} - Creating an external data source {1}.", ExternalDataSourceTestName, externalDataSourceName);
            ExternalDataSource externalDataSource = new ExternalDataSource(db, externalDataSourceName, dataSourceType, externalDataSourceLocation);

            // check for optional properties
            if (!string.IsNullOrEmpty(externalDataSourceResourceManagerLocation))
            {
                externalDataSource.ResourceManagerLocation = externalDataSourceResourceManagerLocation;
            }
            if (!string.IsNullOrEmpty(externalDataSourceCredential))
            {
                // create a credential object first
                db.ExecuteNonQuery(
                    string.Format(
                        DatabaseCredentialQuery,
                    SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSourceCredential),
                    SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSourceCredential)));

                // verify credential was created successfully
                Assert.AreEqual(externalDataSourceCredential, (string)db.ExecutionManager.ConnectionContext.ExecuteScalar(string.Format(DatabaseCredentialNameQuery, externalDataSourceCredential)), "Database credential was not found.");

                externalDataSource.Credential = externalDataSourceCredential;
            }

            externalDataSource.Create();
            // verify the external data source was created by querying the external data sources system view
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "External data source was not created.");

            //
            // Step 2. Script create external data source with IncludeIfNotExists option set to true.
            //
            TraceHelper.TraceInformation("Step 2: {0} - Scripting create external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            ScriptingOptions so = new ScriptingOptions();
            so.IncludeIfNotExists = true;
            so.IncludeDatabaseContext = true;
            StringCollection col = externalDataSource.Script(so);

            //
            // Step 3. Verify the script contains expected information.
            //
            TraceHelper.TraceInformation("Step 3: {0} - Verifying generated external data source script.", ExternalDataSourceTestName);
            StringBuilder sb = new StringBuilder();
            StringBuilder scriptTemplate = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
                TraceHelper.TraceInformation(statement);
            }

            const string ExternalDataSourceScriptCreateTemplate = "CREATE EXTERNAL DATA SOURCE [{0}] WITH ({1} LOCATION = N'{2}'"; // not closing the parenthesis to allow for optional parameters
            string createExternalDataSourceScripts = sb.ToString();
            string fullyFormatedNameForScripting = SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSource.Name);
            scriptTemplate.Append(
                string.Format(
                    ExternalDataSourceScriptCreateTemplate,
                    fullyFormatedNameForScripting,
                    this.GetSqlFragmentForDataSourceType(externalDataSource.DataSourceType),
                    SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSource.Location)));

            // check for optional properties
            if (!string.IsNullOrEmpty(externalDataSource.ResourceManagerLocation))
            {
                scriptTemplate.Append(
                    string.Format(", RESOURCE_MANAGER_LOCATION = N'{0}'",
                        SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSource.ResourceManagerLocation)));
            }
            if (!string.IsNullOrEmpty(externalDataSource.Credential))
            {
                scriptTemplate.Append(
                    string.Format(", CREDENTIAL = [{0}]",
                        SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSource.Credential)));
            }

            TraceHelper.TraceInformation(createExternalDataSourceScripts);
            TraceHelper.TraceInformation(scriptTemplate.ToString());
            Assert.That(createExternalDataSourceScripts, Does.Contain(scriptTemplate.ToString()));

            //
            // Step 4. Script drop external data source with IncludeIfNotExists option set to true.
            //
            TraceHelper.TraceInformation("Step 4: {0} - Scripting drop external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            so = new ScriptingOptions();
            so.IncludeIfNotExists = true;
            so.IncludeDatabaseContext = true;
            so.ScriptDrops = true;
            col = externalDataSource.Script(so);

            //
            // Step 5. Verify the script contains expected information.
            //
            TraceHelper.TraceInformation("Step 5: {0} - Verifying generated external data source script.", ExternalDataSourceTestName);
            sb = new StringBuilder();
            scriptTemplate = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
                TraceHelper.TraceInformation(statement);
            }

            const string ExternalDataSourceScriptDropTemplate = "DROP EXTERNAL DATA SOURCE [{0}]";
            string dropExternalDataSourceScripts = sb.ToString();
            scriptTemplate.Append(string.Format(ExternalDataSourceScriptDropTemplate, fullyFormatedNameForScripting));

            TraceHelper.TraceInformation(dropExternalDataSourceScripts);
            TraceHelper.TraceInformation(scriptTemplate.ToString());
            Assert.That(dropExternalDataSourceScripts, Does.Contain(scriptTemplate.ToString()));

            //
            // Step 6. Drop the external data source and verify the count is 0.
            //
            TraceHelper.TraceInformation("Step 6: {0} - Dropping external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            externalDataSource.Drop();
            db.ExternalDataSources.Refresh();
            Assert.AreEqual(0, db.ExternalDataSources.Count, "External data source was not dropped.");

            //
            // Step 7. Verify the script re-creates the external data source.
            //
            TraceHelper.TraceInformation("Step 7: {0} - Creating external data source using the generated script.", ExternalDataSourceTestName);
            db.ExecuteNonQuery(createExternalDataSourceScripts);
            db.ExternalDataSources.Refresh();
            Assert.AreEqual(1, db.ExternalDataSources.Count, "There should be an external data source present in the collection.");
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "There should be an external data source present in the database.");
            externalDataSource = db.ExternalDataSources[externalDataSource.Name];
            Assert.IsNotNull(externalDataSource, "External data source was not recreated by the script.");
            Assert.AreEqual(externalDataSourceName, externalDataSource.Name, "Recreated external data source name does not match the original data source name.");
            Assert.AreEqual(dataSourceType, externalDataSource.DataSourceType, "Recreated external data source does not have the same value for Type.");
            Assert.AreEqual(externalDataSourceLocation, externalDataSource.Location, "Recreated external data souce does not have the same value for Location.");

            // verify optional properties
            if (!string.IsNullOrEmpty(externalDataSourceResourceManagerLocation))
            {
                Assert.AreEqual(externalDataSourceResourceManagerLocation, externalDataSource.ResourceManagerLocation, "Recreated external data source does not have the same value for Resource Manager Location.");
            }
            if (!string.IsNullOrEmpty(externalDataSourceCredential))
            {
                Assert.AreEqual(externalDataSourceCredential, externalDataSource.Credential, "Recreated external data source does not have the same value for Credential.");
            }

            //
            // Step 8.  Alter the external data source properties.
            //
            TraceHelper.TraceInformation("Step 8: {0} - Altering external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);

            // set the external data source properties to alter
            externalDataSource.Location = newExternalDataSourceLocation;
            externalDataSource.ResourceManagerLocation = newExternalDataSourceResourceManagerLocation;

            // create a credential object first
            if (!string.IsNullOrEmpty(newExternalDataSourceCredential))
            {
                db.ExecuteNonQuery(
                    string.Format(
                        DatabaseCredentialQuery,
                        SmoObjectHelpers.SqlEscapeSingleQuote(newExternalDataSourceCredential),
                        SmoObjectHelpers.SqlEscapeClosingBracket(newExternalDataSourceCredential)));
                // verify credential was created successfully
                Assert.AreEqual(newExternalDataSourceCredential, (string)db.ExecutionManager.ConnectionContext.ExecuteScalar(string.Format(DatabaseCredentialNameQuery, newExternalDataSourceCredential)), "Database credential was not found.");
            }

            externalDataSource.Credential = newExternalDataSourceCredential;

            externalDataSource.Alter();
            externalDataSource.Refresh();
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "Unexpected number of external data sources have been altered.");

            //
            // Step 9. Verify altered properties.
            //
            TraceHelper.TraceInformation("Step 9: {0} - Validating altered external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            if (!string.IsNullOrEmpty(newExternalDataSourceLocation))
            {
                TraceHelper.TraceInformation("The new external data source location is: {0}", newExternalDataSourceLocation);
                Assert.AreEqual(newExternalDataSourceLocation, (string)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceLocationQuery), "Unexpected external data source location value.");
            }
            if (!string.IsNullOrEmpty(newExternalDataSourceResourceManagerLocation))
            {
                TraceHelper.TraceInformation("The new external data source resource manager location is: {0}", newExternalDataSourceResourceManagerLocation);
                Assert.AreEqual(newExternalDataSourceResourceManagerLocation, (string)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceResourceManagerLocationQuery), "Unexpected external data source resource manager location value.");
            }
            if (!string.IsNullOrEmpty(newExternalDataSourceCredential))
            {
                TraceHelper.TraceInformation("The new external data source credential is: {0}", newExternalDataSourceCredential);
                Assert.AreEqual(newExternalDataSourceCredential, (string)db.ExecutionManager.ConnectionContext.ExecuteScalar(string.Format(ExternalDataSourceCredentialQuery, newExternalDataSourceCredential)), "Unexpected external data source credential value.");
            }

            //
            // Step 10.  Test dropping external data source using the generated script.  Verify it was dropped correctly.
            //
            TraceHelper.TraceInformation("Step 10: {0} - Dropping external data source {1} using the generated script.", ExternalDataSourceTestName, externalDataSource.Name);
            db.ExecutionManager.ConnectionContext.ExecuteNonQuery(dropExternalDataSourceScripts);
            db.ExternalDataSources.Refresh();
            Assert.AreEqual(0, db.ExternalDataSources.Count, "There should be no external data sources present in the collection.");
            Assert.AreEqual(0, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "There should be no external data sources present in the database.");
        }

        /// <summary>
        /// Executes positive tests for the CREATE EXTERNAL DATA SOURCE command for GQ.
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <param name="dataSourceType">The type of external data source.</param>
        /// <param name="externalDataSourceName">The external data source name.</param>
        /// <param name="externalDataSourceLocation">The external data source location property value.</param>
        /// <param name="externalDataSourceDatabaseName">The database name property value.</param>
        /// <param name="externalDataSourceShardMapName">The shard map name property value.</param>
        /// <param name="externalDataSourceCredential">The credential property value.</param>
        private void VerifyPositiveExternalDataSourceCreateDropHelperGQ(
            Database db,
            ExternalDataSourceType dataSourceType,
            string externalDataSourceName,
            string externalDataSourceLocation,
            string externalDataSourceDatabaseName,
            string externalDataSourceShardMapName,
            string externalDataSourceCredential)
        {
            // Const definitions.
            const string ExternalDataSourceCountQuery = @"SELECT COUNT(*) FROM sys.external_data_sources";
            const string DatabaseCredentialNameQuery = "SELECT name FROM sys.database_scoped_credentials WHERE name = '{0}'";
            const string ExternalDataSourceTestName = "GQ External Data Source Testing";

            //
            // Step 1. Create an external data source with given type.
            //
            TraceHelper.TraceInformation("Step 1: {0} - Creating an external data source {1}.", ExternalDataSourceTestName, externalDataSourceName);
            // ServerConnection connection = SmoTestHelpers.GetServerConnections(SupportedSqlServer.AzureSterlingV12).First();
            ExternalDataSource externalDataSource = new ExternalDataSource(db, externalDataSourceName);

            externalDataSource.DataSourceType = dataSourceType;

            // Change database context to test database, as external data source and database credential require test database context.
            // connection.ChangeDatabase(db.Name);

            externalDataSource.Location = externalDataSourceLocation;
            externalDataSource.DatabaseName = externalDataSourceDatabaseName;

            const string DatabaseCredentialQuery = @"IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = '{0}') BEGIN CREATE DATABASE SCOPED CREDENTIAL [{1}] WITH IDENTITY = 'Test' END";

            db.ExecuteNonQuery(
                string.Format(
                    DatabaseCredentialQuery,
                    SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSourceCredential),
                    SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSourceCredential)));

            // Verify credential was created successfully.
            Assert.AreEqual(externalDataSourceCredential,
                (string)db.ExecutionManager.ConnectionContext.ExecuteScalar(
                    string.Format(
                        DatabaseCredentialNameQuery,
                        SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSourceCredential))),
                "Database credential was not found.");

            externalDataSource.Credential = externalDataSourceCredential;

            // Check for optional properties (GQ).
            if (!string.IsNullOrEmpty(externalDataSourceShardMapName))
            {
                externalDataSource.ShardMapName = externalDataSourceShardMapName;
            }

            externalDataSource.Create();

            // Verify the external data source was created by querying the external data sources system view.
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "External data source was not created.");

            //
            // Step 2. Script create external data source with IncludeIfNotExists option set to true.
            //
            TraceHelper.TraceInformation("Step 2: {0} - Scripting create external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            ScriptingOptions so = new ScriptingOptions();
            so.IncludeIfNotExists = true;
            so.IncludeDatabaseContext = true;
            StringCollection col = externalDataSource.Script(so);

            //
            // Step 3. Verify the script contains expected information.
            //
            TraceHelper.TraceInformation("Step 3: {0} - Verifying generated external data source script.", ExternalDataSourceTestName);
            StringBuilder sb = new StringBuilder();
            StringBuilder scriptTemplate = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
                TraceHelper.TraceInformation(statement);
            }

            const string ExternalDataSourceScriptCreateTemplate = "CREATE EXTERNAL DATA SOURCE [{0}] WITH ({1} LOCATION = N'{2}', CREDENTIAL = [{3}], DATABASE_NAME = N'{4}'"; // not closing the parenthesis to allow for optional parameters

            string createExternalDataSourceScripts = sb.ToString();
            string bracketEscapedExternalDataSourceName = SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSource.Name);
            scriptTemplate.Append(string.Format(ExternalDataSourceScriptCreateTemplate,
                bracketEscapedExternalDataSourceName,
                this.GetSqlFragmentForDataSourceType(externalDataSource.DataSourceType),
                SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSource.Location),
                SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSource.Credential),
                SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSource.DatabaseName)));

            // Check for optional properties (GQ).
            if (!string.IsNullOrEmpty(externalDataSource.ShardMapName))
            {
                scriptTemplate.Append(
                    string.Format(", SHARD_MAP_NAME = N'{0}'", SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSource.ShardMapName)));
            }

            TraceHelper.TraceInformation(createExternalDataSourceScripts);
            TraceHelper.TraceInformation(scriptTemplate.ToString());
            Assert.That(createExternalDataSourceScripts, Does.Contain(scriptTemplate.ToString()));

            //
            // Step 4. Script drop external data source with IncludeIfNotExists option set to true.
            //
            TraceHelper.TraceInformation("Step 4: {0} - Scripting drop external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            so = new ScriptingOptions();
            so.IncludeIfNotExists = true;
            so.IncludeDatabaseContext = true;
            so.ScriptDrops = true;
            col = externalDataSource.Script(so);

            //
            // Step 5. Verify the script contains expected information.
            //
            TraceHelper.TraceInformation("Step 5: {0} - Verifying generated external data source script.", ExternalDataSourceTestName);
            sb = new StringBuilder();
            scriptTemplate = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
                TraceHelper.TraceInformation(statement);
            }

            const string ExternalDataSourceScriptDropTemplate = "DROP EXTERNAL DATA SOURCE [{0}]";

            string dropExternalDataSourceScripts = sb.ToString();
            scriptTemplate.Append(string.Format(ExternalDataSourceScriptDropTemplate, bracketEscapedExternalDataSourceName));

            TraceHelper.TraceInformation(dropExternalDataSourceScripts);
            TraceHelper.TraceInformation(scriptTemplate.ToString());
            Assert.That(dropExternalDataSourceScripts, Does.Contain(scriptTemplate.ToString()));

            //
            // Step 6. Drop the external data source and verify the count is 0.
            //
            TraceHelper.TraceInformation("Step 6: {0} - Dropping external data source {1}.", ExternalDataSourceTestName, externalDataSource.Name);
            externalDataSource.Drop();
            db.ExternalDataSources.Refresh();
            Assert.AreEqual(0, db.ExternalDataSources.Count, "External data source was not dropped.");

            //
            // Step 7. Verify the script re-creates the external data source.
            //
            TraceHelper.TraceInformation("Step 7: {0} - Creating external data source using the generated script.", ExternalDataSourceTestName);
            db.ExecuteNonQuery(createExternalDataSourceScripts);
            db.ExternalDataSources.Refresh();
            Assert.AreEqual(1, db.ExternalDataSources.Count, "There should be an external data source present in the collection.");
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "There should be an external data source present in the database.");
            externalDataSource = db.ExternalDataSources[externalDataSource.Name];
            Assert.IsNotNull(externalDataSource, "External data source was not recreated by the script.");
            Assert.AreEqual(externalDataSourceName, externalDataSource.Name, "Recreated external data source name does not match the original data source name.");
            Assert.AreEqual(dataSourceType, externalDataSource.DataSourceType, "Recreated external data source does not have the same value for Type.");
            Assert.AreEqual(externalDataSourceLocation, externalDataSource.Location, "Recreated external data souce does not have the same value for Location.");
            Assert.AreEqual(externalDataSourceDatabaseName, externalDataSource.DatabaseName, "Recreated external data souce does not have the same value for Database Name.");
            Assert.AreEqual(externalDataSourceCredential, externalDataSource.Credential, "Recreated external data source does not have the same value for Credential.");

            // Verify optional properties (GQ).
            if (!string.IsNullOrEmpty(externalDataSourceShardMapName))
            {
                Assert.AreEqual(externalDataSourceShardMapName, externalDataSource.ShardMapName, "Recreated external data source does not have the same value for Shard Map Name.");
            }

            //
            // Step 8.  Test dropping external data source using the generated script.  Verify it was dropped correctly.
            //
            TraceHelper.TraceInformation("Step 8: {0} - Dropping external data source {1} using the generated script.", ExternalDataSourceTestName, externalDataSource.Name);
            db.ExecuteNonQuery(dropExternalDataSourceScripts);
            db.ExternalDataSources.Refresh();
            Assert.AreEqual(0, db.ExternalDataSources.Count, "There should be no external data sources present in the collection.");
            Assert.AreEqual(0, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalDataSourceCountQuery), "There should be no external data sources present in the database.");
        }

        /// Tests creating, dropping and scripting of Polybase external data source objects via SMO.
        /// Negative test steps:
        /// 1. Create an external data source with no required properties.
        /// 2. Create an external data source with the location property value being an empty string.
        /// 3. Alter an external data source type property value.
        /// 4. Create an external data source with the location on WASBS and the ResourceManagerLocation specified.
        /// 5. Alter an external data source to specify the ResourceManagerLocation property where the external data location is on WASBS.
        /// 6. Alter a polybase external data source to specify a GQ property.
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13, MaxMajor = 15)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestArea(SqlTestArea.Polybase)]
        public void VerifyNegativeExternalDataSourceCreateAlterDropPolybase()
        {

            string[] externalDataSourceLocations = { @"hdfs://10.10.10.10:1000", @"wasbs://commondatabases@sqltoolstestsstorage.blob.core.windows.net/" };
            string[] externalDataSourceResourceManagerLocations = { @"10.10.10.10:1010", @"10.10.10.10:1111" };

            this.ExecuteWithDbDrop(this.TestContext.TestName,
                database =>
                {
                    VerifyNegativeExternalDataSourceCreateAlterDropHelperPolybase(database,
                        externalDataSourceLocations[0], externalDataSourceLocations[1],
                        externalDataSourceResourceManagerLocations[0], externalDataSourceResourceManagerLocations[1]);
                });
        }

        /// Tests creating, dropping and scripting of GQ external data source objects via SMO.
        /// Negative test steps:
        /// 1. Create a GQ external data source with no required properties.
        /// 2. Create a GQ external data source with the location property value being an empty string.
        /// 3. Create a GQ external data source without setting the credential property.        
        /// 4. Create a GQ external data source without setting the database name property.
        /// 5. Create a GQ external data source with/without setting the shard map name property depending on the data source type.
        /// 6. Alter a GQ external data source to specify the data source type property.
        /// 7. Alter a GQ external data source to specify the shard map name type property.
        /// 8. Create a GQ external data source with a Polybase propery.
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [SqlTestArea(SqlTestArea.Polybase)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void VerifyNegativeExternalDataSourceCreateAlterDropGQ()
        {
            string[] externalDataSourceLocations = { "abc.xyz.com" };
            string[] externalDataSourceCredentials = { "cred1" };
            string[] externalDataSourceDatabaseNames = { "database1" };
            string[] externalDataSourceShardMapNames = { "shard-map-1" };

            this.ExecuteWithDbDrop(this.TestContext.TestName,
                database =>
                {
                    VerifyNegativeExternalDataSourceCreateAlterDropHelperGQ(database, ExternalDataSourceType.ShardMapManager, externalDataSourceLocations[0], externalDataSourceCredentials[0], externalDataSourceDatabaseNames[0], externalDataSourceShardMapNames[0]);

                    VerifyNegativeExternalDataSourceCreateAlterDropHelperGQ(database, ExternalDataSourceType.Rdbms, externalDataSourceLocations[0], externalDataSourceCredentials[0], externalDataSourceDatabaseNames[0], null);

                    // DEVNOTE(MatteoT) 7/7/2019. These are not in SQL Azure yet?
                    // VerifyNegativeExternalDataSourceCreateAlterDropHelperGQ(database, ExternalDataSourceType.ExternalGenerics, externalDataSourceLocations[0], externalDataSourceCredentials[0], externalDataSourceDatabaseNames[0], null);
                });
        }

        /// <summary>
        /// Executes negative tests for the CREATE EXTERNAL DATA SOURCE command for Polybase.
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <param name="externalDataSourceLocation">The external data source location property value.</param>
        /// <param name="newExternalDataSourceLocation">The new external data source location property value.</param>
        /// <param name="externalDataSourceResourceManagerLocation">The external data source resource manager location property value.</param>
        /// <param name="newExternalDataSourceResourceManagerLocation">The new external data source resource manager location property value.</param>
        private void VerifyNegativeExternalDataSourceCreateAlterDropHelperPolybase(
            Database db,
            string externalDataSourceLocation,
            string newExternalDataSourceLocation,
            string externalDataSourceResourceManagerLocation,
            string newExternalDataSourceResourceManagerLocation)
        {
            // const definitions
            const string ExternalDataSourceName = "eds1";
            const string ExternalDataSourceTestName = "External Data Source Testing";

            ExternalDataSourceType dataSourceType = ExternalDataSourceType.Hadoop;

            //
            // Step 1. Create an external data source with no required properties.
            //
            TraceHelper.TraceInformation("Step 1: {0} - Creating an external data source {1} with no required properties.", ExternalDataSourceTestName, ExternalDataSourceName);
            ExternalDataSource externalDataSource = new ExternalDataSource(db, ExternalDataSourceName);

            string errorMessage = string.Empty;

            var ex = Assert.Throws<FailedOperationException>(externalDataSource.Create, "verify the external data source was not created with null DataSourceType");
            var innerEx = ex.GetBaseException();
            Assert.Multiple(() =>
            {
                Assert.That(innerEx, Is.InstanceOf<ArgumentNullException>(), "innermost exception for null DataSourceType");
                Assert.That(innerEx.Message, Does.Contain(nameof(externalDataSource.DataSourceType)), "innermost exception message for null DataSourceType");
            });


            //
            // Step 2. Create an external data source with the location property value being an empty string.
            //
            TraceHelper.TraceInformation("Step 2: {0} - Creating an external data source {1} with invalid Location property value.", ExternalDataSourceTestName, ExternalDataSourceName);
            externalDataSource = new ExternalDataSource(db, ExternalDataSourceName, dataSourceType, string.Empty);

            // verify the external data source was not created
            try
            {
                // attempt to create an external data source with the Location property being an empty string
                errorMessage = "To accomplish this action, set property Location.";

                externalDataSource.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 3. Alter an external data source type property value.
            //
            TraceHelper.TraceInformation("Step 3: {0} - Alter an external data source {1} with invalid DataSourceType property value.", ExternalDataSourceTestName, ExternalDataSourceName);
            externalDataSource = new ExternalDataSource(db, ExternalDataSourceName, dataSourceType, externalDataSourceLocation);

            // verify the external data source was not altered
            try
            {
                // attempt to alter an external data source type property value
                errorMessage = "The 'DataSourceType' property is not supported for the alter operation.";

                externalDataSource.Create();
                externalDataSource.DataSourceType = ExternalDataSourceType.Hadoop;
                externalDataSource.Alter();


                // validate expected exception and error message
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                externalDataSource.Drop();

                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 4. Create an external data source with the location on WASBS and the ResourceManagerLocation specified.
            //
            TraceHelper.TraceInformation("Step 4: {0} - Create an external data source {1} with the location on WASBS and the ResourceManagerLocation specified.", ExternalDataSourceTestName, ExternalDataSourceName);
            externalDataSource = new ExternalDataSource(db, ExternalDataSourceName, dataSourceType, newExternalDataSourceLocation);
            externalDataSource.ResourceManagerLocation = externalDataSourceResourceManagerLocation;

            // verify the external data source was not altered
            try
            {
                // attempt to alter an external data source type property value
                errorMessage = "The 'ResourceManagerLocation' property is not supported for the external data stored in WASB, secure WASB, ASV, or secure ASV.";

                externalDataSource.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 5. Alter an external data source to specify the ResourceManagerLocation property where the external data location is on WASBS.
            //
            TraceHelper.TraceInformation("Step 5: {0} - Alter an external data source {1} to specify the ResourceManagerLocation property where the external data location is on WASBS.", ExternalDataSourceTestName, ExternalDataSourceName);
            externalDataSource = new ExternalDataSource(db, ExternalDataSourceName, dataSourceType, newExternalDataSourceLocation);

            // verify the external data source was not altered
            try
            {
                // attempt to alter an external data source type property value
                errorMessage = "The 'ResourceManagerLocation' property is not supported for the external data stored in WASB, secure WASB, ASV, or secure ASV.";

                externalDataSource.Create();
                externalDataSource.ResourceManagerLocation = newExternalDataSourceResourceManagerLocation;
                externalDataSource.Alter();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                externalDataSource.Drop();

                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 6. Alter a polybase external data source to specify a GQ property.
            //
            TraceHelper.TraceInformation("Step 3: {0} - Alter an external data source {1} specifying a GQ property value.", ExternalDataSourceTestName, ExternalDataSourceName);
            externalDataSource = new ExternalDataSource(db, ExternalDataSourceName, dataSourceType, externalDataSourceLocation);

            // verify the external data source was not altered
            try
            {
                // attempt to alter an external data source type property value
                errorMessage = "ShardMapName: unknown property.";

                externalDataSource.Create();
                externalDataSource.ShardMapName = "shard-map-1";
                externalDataSource.Alter();


                // validate expected exception and error message
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Executes negative tests for the CREATE EXTERNAL DATA SOURCE command for GQ.
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <param name="datasourceType">The type of external data source.</param>
        /// <param name="externalDataSourceLocation">The external data source location property value.</param>
        /// <param name="externalDataSourceCredential">The external data source credential property value.</param>
        /// <param name="externalDataSourceDatabaseName">The database name property value.</param>
        /// <param name="externalDataSourceShardMapName">The shard map name propery value</param>
        private void VerifyNegativeExternalDataSourceCreateAlterDropHelperGQ(
            Database db,
            ExternalDataSourceType datasourceType,
            string externalDataSourceLocation,
            string externalDataSourceCredential,
            string externalDataSourceDatabaseName,
            string externalDataSourceShardMapName)
        {
            // Const definitions.
            const string ExternalDataSourceName = "eds1";
            const string ExternalDataSourceTestName = "External Data Source Testing";

            //
            // Step 1. Create an external data source with no required properties.
            //
            TraceHelper.TraceInformation("Step 1: {0} - Creating an external data source {1} with no required properties.", ExternalDataSourceTestName, ExternalDataSourceName);
            ExternalDataSource externalDataSource = new ExternalDataSource(db, ExternalDataSourceName);

            string errorMessage = string.Empty;

            var ex = Assert.Throws<FailedOperationException>(externalDataSource.Create, "verify the external data source was not created with null DataSourceType");
            var innerEx = ex.GetBaseException();
            Assert.Multiple(() =>
            {
                Assert.That(innerEx, Is.InstanceOf<ArgumentNullException>(), "innermost exception for null DataSourceType");
                Assert.That(innerEx.Message, Does.Contain(nameof(externalDataSource.DataSourceType)), "innermost exception message for null DataSourceType");
            });

            //
            // Step 2. Create a relational external data source with the location property value being an empty string.
            //
            TraceHelper.TraceInformation("Step 2: {0} - Creating external data source '{1}' of type {2} with invalid Location property value.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);
            externalDataSource.DataSourceType = datasourceType; // Fix the data source type.
            externalDataSource.Location = string.Empty;

            // Verify the external data source was not created.
            try
            {
                // Attempt to create an external data source with the Location property being an empty string.
                errorMessage = "To accomplish this action, set property Location.";

                externalDataSource.Create();

                // Validate expected exception and error message.
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 3. Create a relational external data source without setting the credential property value.
            //
            TraceHelper.TraceInformation("Step 3: {0} - Creating an external data source '{1}' of type {2} with invalid Credential property value.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);
            externalDataSource.Location = externalDataSourceLocation; // Fix the location, omit credential.

            ex = Assert.Throws<FailedOperationException>(externalDataSource.Create, "verify the external data source was not created with null Credential");
            innerEx = ex.GetBaseException();
            Assert.Multiple(() =>
            {
                Assert.That(innerEx, Is.InstanceOf<ArgumentNullException>(), "innermost exception for null Credential");
                Assert.That(innerEx.Message, Does.Contain(nameof(externalDataSource.Credential)), "innermost exception message for null Credential");
            });

            //
            // Step 4. Create a relational external data source with the database name property value set to an empty string.
            //
            TraceHelper.TraceInformation("Step 4: {0} - Creating an external data source '{1}' of type {2} with invalid DatabaseName property value.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);
            externalDataSource.Credential = externalDataSourceCredential;  // Fix the credential.
            externalDataSource.DatabaseName = string.Empty;

            // Verify the external data source was not created.
            try
            {
                // Attempt to create an external data source with the database name property value set to null.
                errorMessage = "To accomplish this action, set property DatabaseName.";

                externalDataSource.Create();

                // Validate expected exception and error message.
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 5. Create an external data source with invalid ShardMapName.
            //
            if (datasourceType == ExternalDataSourceType.ShardMapManager)
            {
                TraceHelper.TraceInformation("Step 5: {0} - Creating an external data source '{1}' of type {2} with invalid ShardMapName property value.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);
                externalDataSource.DatabaseName = externalDataSourceDatabaseName; // Fix the database name.

                ex = Assert.Throws<FailedOperationException>(externalDataSource.Create, "verify the external data source was not created with null ShardMapName");
                innerEx = ex.GetBaseException();
                Assert.Multiple(() =>
                {
                    Assert.That(innerEx, Is.InstanceOf<ArgumentNullException>(), "innermost exception for null ShardMapName");
                    Assert.That(innerEx.Message, Does.Contain(nameof(externalDataSource.ShardMapName)), "innermost exception message for null ShardMapName");
                });

                externalDataSource.ShardMapName = externalDataSourceShardMapName; // Fix the shard map name.
            }
            else if (datasourceType == ExternalDataSourceType.Rdbms)
            {
                TraceHelper.TraceInformation("Step 5: {0} - Creating an external data source '{1}' of type {2} with invalid ShardMapName property value.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);
                externalDataSource.DatabaseName = externalDataSourceDatabaseName; // Fix the database name.
                externalDataSource.ShardMapName = "shard-map-1";

                // Verify the external data source was not created.
                try
                {
                    // Attempt to create an external data source with setting the shard map name when an rdbms source is being created.
                    errorMessage = "The 'ShardMapName' property is not supported for external data sources of type 'Rdbms'.";

                    externalDataSource.Create();

                    // Validate expected exception and error message.
                    Assert.Fail(errorMessage, externalDataSource.Name);
                }
                catch (SmoException e)
                {
                    if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                    {
                        throw;
                    }
                }

                externalDataSource.ShardMapName = string.Empty; // Fix the shard map name.
            }

            const string DatabaseCredentialQuery = @"IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = '{0}') BEGIN CREATE DATABASE SCOPED CREDENTIAL [{1}] WITH IDENTITY = 'Test' END";

            // Now create the credential needed by the external data source.
            db.ExecuteNonQuery(
                string.Format(
                    DatabaseCredentialQuery,
                    SmoObjectHelpers.SqlEscapeSingleQuote(externalDataSourceCredential),
                    SmoObjectHelpers.SqlEscapeClosingBracket(externalDataSourceCredential)));
            externalDataSource.Create();

            //
            // Step 6. Alter an external data source to specify the DatabaseName property.
            //
            TraceHelper.TraceInformation("Step 6: {0} - Alter external data source '{1}' of type {2} to specify the DatabaseName property.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);

            // Verify the external data source was not altered.
            try
            {
                // Attempt to alter an external data source database name property value.
                errorMessage = string.Format("Altering an external data source of type '{0}' is not supported.", datasourceType);

                externalDataSource.DatabaseName = "new_" + externalDataSourceDatabaseName;
                externalDataSource.Alter();

                // Validate expected exception and error message.
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 7. Alter an external data source to specify the ShardMapName property.
            //
            TraceHelper.TraceInformation("Step 7: {0} - Alter external data source '{1}' of type {2} to specify the ShardMapName property.", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);

            // Verify the external data source was not altered.
            try
            {
                // Attempt to alter an external data source shard map name property value.
                errorMessage = string.Format("Altering an external data source of type '{0}' is not supported.", datasourceType);

                externalDataSource.ShardMapName = "new_" + externalDataSourceShardMapName;
                externalDataSource.Alter();

                // Validate expected exception and error message.
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 8. Create a GQ external data source with a Polybase propery.
            //
            ExternalDataSource externalDataSourceInvalid = new ExternalDataSource(db, ExternalDataSourceName, datasourceType, externalDataSourceLocation);
            externalDataSourceInvalid.DatabaseName = externalDataSource.DatabaseName;
            externalDataSourceInvalid.Credential = externalDataSource.Credential;
            if (datasourceType == ExternalDataSourceType.ShardMapManager)
            {
                externalDataSourceInvalid.ShardMapName = externalDataSource.ShardMapName;
            }

            // Set the Polybase property ResourceManageLocation.
            TraceHelper.TraceInformation("Step 7: {0} - Creating an external data source '{1}' of type {2} by setting ResourceManageLocation property .", ExternalDataSourceTestName, ExternalDataSourceName, datasourceType);

            // Verify the external data source was not created.
            try
            {
                // Attempt to create an external data source with setting the polybase property ResourceManagerLocation.
                errorMessage = "ResourceManagerLocation: unknown property.";

                // Accessing the ResourceManagerLocation property in cloud environment should throw.
                externalDataSourceInvalid.ResourceManagerLocation = "resource-manager-location";

                // Validate expected exception and error message.
                Assert.Fail(errorMessage, externalDataSource.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            // Drop any data source created.
            externalDataSource.Drop();
        }

        /// <summary>
        /// Converts an external data source type to the corresponding T-SQL fragement
        /// to be used to create the datasource. Typically, it is something like
        ///     TYPE = ...
        /// however, it may be empty when the TYPE is optional.
        /// </summary>
        /// <param name="dataSourceType">Type of external data source.</param>
        /// <returns>SQL keyword for the given type. It may </returns>
        private string GetSqlFragmentForDataSourceType(ExternalDataSourceType dataSourceType)
        {
            switch (dataSourceType)
            {
                case ExternalDataSourceType.Rdbms:
                    return "TYPE = RDBMS,";
                case ExternalDataSourceType.ShardMapManager:
                    return "TYPE = SHARD_MAP_MANAGER,";
                case ExternalDataSourceType.Hadoop:
                    return "TYPE = HADOOP,";
                case ExternalDataSourceType.ExternalGenerics:
                    return null;
                default:
                    Assert.Fail("Unexpected value '{0}' for dataSourceType. Please, update the test method to account for it.", dataSourceType);
                    // This is really unreachabled code, but the compiler can't tell that Assert.Fail() actually throws.
                    throw new InvalidArgumentException("dataSourceType");
            }
        }

        /// <summary>
        /// Verifies data source options that exist only in v150 (SQL 2019) and above.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SqlTestArea(SqlTestArea.Polybase)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void VerifyExternalDataSource_v150AndAbove()
        {
            ExecuteWithDbDrop(
                "ExternalDataSourceSmo_",
                database =>
                {
                    ExternalDataSource eds = new ExternalDataSource(database, "eds_" + (TestContext.TestName ?? Guid.NewGuid().ToString()))
                    {
                        DataSourceType = ExternalDataSourceType.ExternalGenerics,
                        Location = "sqlserver://127.0.0.1:1433"
                    };

                    eds.Create();

                    // 1. Check default values
                    Assert.That(eds.PushdownOption, Is.EqualTo(ExternalDataSourcePushdownOption.On));
                    Assert.That(eds.ConnectionOptions, Is.Null.Or.Empty);

                    // 2. Check that default values are not present in scripted output
                    const string pushdownName = "PUSHDOWN";
                    const string connOptionsName = "CONNECTION_OPTIONS";
                    Action<StringCollection> verifyScript = (collection) =>
                    {
                        foreach (var script in collection)
                        {
                            Assert.That(script, Does.Not.Contain(pushdownName));
                            Assert.That(script, Does.Not.Contain(connOptionsName));
                        }
                    };
                    var scriptCol = new StringCollection();
                    var pref = new ScriptingPreferences(eds);

                    eds.ScriptCreate(scriptCol, pref);
                    verifyScript(scriptCol);

                    scriptCol.Clear();
                    eds.ScriptAlter(scriptCol, pref);
                    verifyScript(scriptCol);

                    // 3. Set different values and verify they are scripted out correctly
                    var pushdownOption = ExternalDataSourcePushdownOption.Off;
                    var connOptions = "Server=localhost;Database=NotARealDatabase;";

                    verifyScript = (collection) =>
                    {
                        foreach (var script in collection)
                        {
                            Assert.That(script, Does.Contain($"{pushdownName} = {pushdownOption}").IgnoreCase);
                            Assert.That(script, Does.Contain($"{connOptionsName} = N'{connOptions}'").IgnoreCase);
                        }
                    };

                    eds.PushdownOption = pushdownOption;
                    eds.ConnectionOptions = connOptions;

                    scriptCol.Clear();
                    eds.ScriptAlter(scriptCol, pref);
                    verifyScript(scriptCol);

                    eds.Alter();
                    eds.Refresh();

                    Assert.That(eds.PushdownOption, Is.EqualTo(pushdownOption));
                    Assert.That(eds.ConnectionOptions, Is.EqualTo(connOptions));

                    scriptCol.Clear();
                    eds.ScriptCreate(scriptCol, pref);
                    verifyScript(scriptCol);
                });
        }
    }
}
