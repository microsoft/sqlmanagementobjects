// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing DatabaseOptions properties and scripting
    /// </summary>
    [TestClass]
    public class DatabaseOptions_SmoTestSuite : SqlTestBase
    {

        #region Property Tests

        /// <summary>
        /// Verifies that properties return their expected values when the parent DB is
        /// still in the Creating state
        /// </summary>
        [TestMethod]
        public void SmoDatabaseOptions_Verify_Properties_When_Creating()
        {
            this.ExecuteTest(
                server =>
                {
                    var result = new SqlTestResult();

                    //This test needs to test against a DB in the creating state so we just
                    //create it locally but not on the server
                    var db = new _SMO.Database(server, "CreatingDb");

                    //The engine type should match the type from the server since it's a server level property
                    result &= SqlTestHelpers.TestReadProperty(db.DatabaseOptions, "DatabaseEngineType", server.DatabaseEngineType);
                    //On Azure the EngineEdition is a db level property so we need a connection directly to the db,
                    //but since our DB isn't created yet it'll return Unknown

                    var expectedDbEngineEdition = server.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase
                        ? DatabaseEngineEdition.Unknown
                        : server.DatabaseEngineEdition;
                    result &= SqlTestHelpers.TestReadProperty(db.DatabaseOptions, "DatabaseEngineEdition", expectedDbEngineEdition);


                    Assert.That(result.Succeeded, result.FailureReasons);
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void SmoDatabaseOptions_Verify_DatabaseOptions_DataRetention()
        {
            this.ExecuteFromDbPool(
                db =>
                {
                    //Get the types of that have the DataRetention property so we can initialize them.
                    //
                    Type typ = typeof(_SMO.DatabaseOptions);
                    Type typ2 = typeof(_SMO.Database);

                    //Tell the server to initialize DataRetentionEnabled property when it tries to initialize the DatabaseOption object and Database object.
                    //
                    db.GetServerObject().SetDefaultInitFields(typ2, nameof(_SMO.Database.DataRetentionEnabled));
                    db.GetServerObject().SetDefaultInitFields(typ,nameof(_SMO.Database.DatabaseOptions.DataRetentionEnabled));

                    //Initialize the DatabaseOptions and Database object to get the DataRetentionEnabled property
                    //
                    db.Initialize();
                    db.DatabaseOptions.Initialize();

                    // By default this value is on verify the value is on.
                    //
                    Assert.That(db.DataRetentionEnabled, Is.True, "The default value of data retention property should be true when the object is initialized.");
                    Assert.That(db.DatabaseOptions.DataRetentionEnabled, Is.True, "The default value of data retention property should be true when the object is initialized.");

                    // Change the value of DatabaseOptions
                    //
                    db.DataRetentionEnabled = false;

                    // This should turn it off
                    //
                    db.Alter();
                    db.Refresh();

                    var dbName = db.Name.SqlEscapeSingleQuote();
                    bool result = (bool)db.GetServerObject().ConnectionContext.ExecuteScalar($"select is_data_retention_enabled from sys.databases where name = '{dbName}'");

                    Assert.That(result, Is.False, "The value of data retention directly from the server should be false.");

                    // Confirm that when the DatabaseOption property changed. The Database property also changed.
                    //
                    Assert.That(db.DataRetentionEnabled, Is.False, "The data retention policy was changed to false on the server and this property should have been updated");
                });
        }
        #endregion //Property Tests

    }
}
