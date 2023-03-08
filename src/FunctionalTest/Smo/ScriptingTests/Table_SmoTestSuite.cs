// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using _NU = NUnit.Framework;
using _UT = Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Table properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Table_SmoTestSuite : SmoObjectTestBase
    {
        /// <summary>
        /// Tests accessing and setting table properties on Azure v12 (Sterling)
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void SmoTableProperties_AzureSterlingV12()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    var result = new SqlTestResult();

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);

                    //read-only properties
                    result &= SqlTestHelpers.TestReadProperty(table, "RowCount", (long)0);

                    //read-only after creation properties
                    result &= SqlTestHelpers.TestReadProperty(table, "TextFileGroup", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsFileTable", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableNameColumnCollation", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsMemoryOptimized", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "Durability", _SMO.DurabilityType.SchemaAndData);

                    //read/write properties
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableDirectoryName", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableNamespaceEnabled", false);

                    _NU.Assert.IsTrue(result.Succeeded, result.FailureReasons);
                });

        }

        /// <summary>
        /// Tests accessing and setting table properties on SQL2014
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12, MaxMajor = 12)]
        public void SmoTableProperties_Sql2014()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    var result = new SqlTestResult();

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);

                    //read-only properties
                    result &= SqlTestHelpers.TestReadProperty(table, "IsVarDecimalStorageFormatEnabled", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "RowCount", (long)0);

                    //read-only after creation properties
                    result &= SqlTestHelpers.TestReadProperty(table, "TextFileGroup", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsFileTable", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableNameColumnCollation", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsMemoryOptimized", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "Durability", _SMO.DurabilityType.SchemaAndData);

                    //read/write properties
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableDirectoryName", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableNamespaceEnabled", false);

                    _NU.Assert.IsTrue(result.Succeeded, result.FailureReasons);
                });

        }

        /// <summary>
        /// Tests accessing and setting table properties on SQL2016 and after
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoTableProperties_Sql15()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    var result = new SqlTestResult();

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);

                    //read-only properties
                    result &= SqlTestHelpers.TestReadProperty(table, "IsVarDecimalStorageFormatEnabled", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "RowCount", (long)0);
                    result &= SqlTestHelpers.TestReadProperty(table, "HistoryTableID", 0);
                    result &= SqlTestHelpers.TestReadProperty(table, "HasSystemTimePeriod", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "SystemTimePeriodStartColumn", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "SystemTimePeriodEndColumn", "");

                    //read-only after creation properties
                    result &= SqlTestHelpers.TestReadProperty(table, "TextFileGroup", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsFileTable", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableNameColumnCollation", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsMemoryOptimized", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "Durability", _SMO.DurabilityType.SchemaAndData);
                    result &= SqlTestHelpers.TestReadProperty(table, "TemporalType", _SMO.TableTemporalType.None);
                    result &= SqlTestHelpers.TestReadProperty(table, "HistoryTableName", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "HistoryTableSchema", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "IsSystemVersioned", false);

                    //read/write properties
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableDirectoryName", "");
                    result &= SqlTestHelpers.TestReadProperty(table, "FileTableNamespaceEnabled", false);

                    _NU.Assert.IsTrue(result.Succeeded, result.FailureReasons);
                });

        }

        /// <summary>
        /// Tests accessing and setting table properties on SQL2017 and higher.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [SupportedServerVersionRange(MinMajor = 14)]
        public void SmoTableProperties_SQL2017()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    SqlTestResult result = new SqlTestResult();

                    _SMO.Table table = database.CreateTable(TestContext.TestName);

                    // Read only table properties.
                    //
                    result &= SqlTestHelpers.TestReadProperty(table, "IsNode", false);
                    result &= SqlTestHelpers.TestReadProperty(table, "IsEdge", false);

                    _NU.Assert.IsTrue(result.Succeeded, result.FailureReasons);
                });
        }

        #region Scripting Tests

        /// <summary>
        /// Tests altering a table through SMO on Azure V12 (Sterling) and after
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void SmoTableAlter_AzureSterlingV12()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    //Need to enable ChangeTracking on DB before it can be enabled on table
                    // setting all the change tracking properties for code coverage
                    database.ChangeTrackingEnabled = true;
                    database.ChangeTrackingAutoCleanUp = true;
                    database.ChangeTrackingRetentionPeriod = 1;
                    database.ChangeTrackingRetentionPeriodUnits = RetentionPeriodUnits.Hours;
                    database.Alter();

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName, new ColumnProperties("c1") { Nullable = false });
                    table.CreateIndex(this.TestContext.TestName, new IndexProperties() { KeyType = _SMO.IndexKeyType.DriPrimaryKey });
                    table.ChangeTrackingEnabled = true;
                    table.Alter();
                    database.Refresh();
                    table.Refresh();
                    Assert.That(database.ChangeTrackingEnabled, Is.True, "database ChangeTrackingEnabled");
                    Assert.That(database.ChangeTrackingRetentionPeriodUnits, Is.EqualTo(RetentionPeriodUnits.Hours), "database ChangeTrackingRetentionPeriodUnits");
                    Assert.That(database.ChangeTrackingRetentionPeriod, Is.EqualTo(1), "database ChangeTrackingRetentionPeriod");
                    Assert.That(database.ChangeTrackingAutoCleanUp, Is.True, "database ChangeTrackingAutoCleanup");
                    Assert.That(table.ChangeTrackingEnabled, Is.True, "table ChangeTrackingEnabled");
                });
        }

        /// <summary>
        /// Tests altering a table through SMO on SQL2014
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12, MaxMajor = 12)]
        public void SmoTableAlter_Sql2014()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    //Need to enable ChangeTracking on DB before it can be enabled on table
                    database.ChangeTrackingEnabled = true;
                    database.Alter();

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName, new ColumnProperties("c1") { Nullable = false });
                    table.CreateIndex(this.TestContext.TestName,
                        new IndexProperties() { KeyType = _SMO.IndexKeyType.DriPrimaryKey });
                    table.ChangeTrackingEnabled = true;
                    table.Alter();
                    table.Refresh();
                    Assert.That(table.ChangeTrackingEnabled, Is.True, "table ChangeTrackingEnabled");
                });
        }

        /// <summary>
        /// Tests altering a table through SMO on SQL2016 and after onprem
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoTableAlter_Sql2016AndAfterOnPrem()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    //Need to enable ChangeTracking on DB before it can be enabled on table
                    database.ChangeTrackingEnabled = true;
                    database.Alter();

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName, new ColumnProperties("c1") { Nullable = false });
                    table.CreateIndex(this.TestContext.TestName, new IndexProperties() { KeyType = _SMO.IndexKeyType.DriPrimaryKey });
                    table.ChangeTrackingEnabled = true;
                    table.Alter();
                    table.Refresh();
                    Assert.That(table.ChangeTrackingEnabled, Is.True, "table ChangeTrackingEnabled");
                });
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Table table = (_SMO.Table)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Tables.Refresh();
            _NU.Assert.IsNull(database.Tables[table.Name],
                          "Current table not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a table with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void SmoDropIfExists_Table()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    _SMO.Table table = new _SMO.Table(database, GenerateSmoObjectName("tbl"));
                    _SMO.Column column = new _SMO.Column(table, GenerateSmoObjectName("col"),
                                                         new _SMO.DataType(_SMO.SqlDataType.Int));
                    table.Columns.Add(column);

                    const string tableScriptDropIfExistsTemplate = "DROP TABLE IF EXISTS [{0}].[{1}]";
                    string tableScriptDropIfExists = string.Format(tableScriptDropIfExistsTemplate,
                                                        table.Schema, table.Name);

                    VerifySmoObjectDropIfExists(table, database, tableScriptDropIfExists);
                });
        }

        #region temporal retention tests

        // Checking temporal history retention policy aspects.
        //
        // The policy is represented by two table properties:
        // 1) 'HistoryRetentionPeriod' - an integer indicating the retention period value
        // 2) 'HistoryRetentionPeriodUnit' - enum value indication the retention unit (day, week, month, year, infinite, undefined).
        //
        // Temporal table may or may not have this policy defined.
        // If not defined, properties should be 0 for 1) and 'Infinite' for the 2)
        // If defined, it can be any valid value: any positive integer for 1) and any value other than 'Undefined' and 'Infinite' for 2)
        //
        // In the case of non-temporal tables, the values are 0 for 1) and 'Undefined' for 2)
        //
        // For the sake of completness, here's the full temporal table DDL syntax with retention period:
        //
        //  CREATE TABLE tblTemporal
        //  (
        //     [a] int primary key,
        //     [st] datetime2 generated always as row start,
        //     [et] datetime2 generated always as row end,
        //     period for system_time ([st],[et])
        //  )
        //  with
        //  (
        //     system_versioning = on (history_table = [dbo].[history], history_retention_period = 7 days)
        //  )
        //
        // These options are valid for system_versioning clause:
        //   system_versioning = on ([history_table = dbo.history] [,history_retention_period = 7 days|weeks|months|years] [,data_consistency_check = ON|OFF])
        //

        /// <summary>
        /// This test is creating regular, non-temporal table and validating
        /// that properties that are 'temporal history retention policy'-specific
        /// always have their default values
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_RegularTable()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    // create non-temporal table and validate retention field values are at defaults
                    //
                    var columns = new ColumnProperties[]
                    {
                        new ColumnProperties("c1"),
                        new ColumnProperties("c2", _SMO.DataType.DateTime2(1))
                    };

                   _SMO.Table nonTemporalTable = DatabaseObjectHelpers.CreateTable(
                       database: db,
                       tableNamePrefix: "NonTemporalTbl_VerifyTemporalRetentionPeriod",
                       schemaName: "dbo",
                       columnProperties: columns);

                    _UT.Assert.AreEqual<int>(0, nonTemporalTable.HistoryRetentionPeriod, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriod]");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, nonTemporalTable.HistoryRetentionPeriodUnit, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriodUnit]");

                    // Scripting should not contain anything related to retention
                    //
                    foreach ( string s in nonTemporalTable.Script() )
                    {
                        _NU.Assert.IsFalse(s.Contains("RETENTION"), "Error scripting non-temporal table. Temporal-specific keyword 'RETENTION' found.");
                        _NU.Assert.IsFalse(s.Contains("SYSTEM_VERSIONING"), "Error scripting non-temporal table. Temporal-specific keyword 'SYSTEM_VERSIONING' found.");
                    }

                    // Alter should have no effect as we changed nothing
                    //
                    nonTemporalTable.Alter();
                    nonTemporalTable.Refresh();

                    // Check that temporal properties are still at defaults post-alter
                    //
                    _UT.Assert.AreEqual<int>(0, nonTemporalTable.HistoryRetentionPeriod, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriod]");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, nonTemporalTable.HistoryRetentionPeriodUnit, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriodUnit]");
                    _UT.Assert.AreEqual<string>(string.Empty, nonTemporalTable.HistoryTableName, "Non-temporal tables should have default values for temporal properties. Property: [HistoryTableName]");
                    _UT.Assert.AreEqual<int>(0, nonTemporalTable.HistoryTableID, "Non-temporal tables should have default values for temporal properties. Property: [HistoryTableID]");
                    _UT.Assert.AreEqual<bool>(false, nonTemporalTable.IsSystemVersioned, "Non-temporal tables should have default values for temporal properties. Property: [IsSystemVersioned]");

                    // Contact the server and check the values SMO has are correct for this table
                    //
                    bool tableExists;
                    bool serverReturnedIsSystemVersioned;
                    int serverReturnedRetentionPeriod;
                    _SMO.TemporalHistoryRetentionPeriodUnit serverReturnedRetentionPeriodUnit;

                    tableExists = GetTableTemporalProperties(db.Name, nonTemporalTable.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                    _NU.Assert.IsTrue(tableExists, "Table not found on the SQL Server as expected.");
                    _NU.Assert.IsFalse(serverReturnedIsSystemVersioned, "Table should not be marked as system-versioned temporal table.");
                    _UT.Assert.AreEqual<int>(0, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");
                }
            );
        }

        /// <summary>
        /// This test is creating regular, non-temporal table and validating
        /// that properties that are 'temporal history retention policy'-specific
        /// always have their default values
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_AddDropColumns()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    // create non-temporal table and validate retention field values are at defaults
                    //
                    var columns = new ColumnProperties[]
                    {
                        new ColumnProperties("c1"),
                        new ColumnProperties("c2", _SMO.DataType.DateTime2(1))
                    };

                    _SMO.Table nonTemporalTable = DatabaseObjectHelpers.CreateTable(
                        database: db,
                        tableNamePrefix: "NonTemporalTbl_VerifyTemporalRetentionPeriod",
                        schemaName: "dbo",
                        columnProperties: columns);

                    _UT.Assert.AreEqual<int>(0, nonTemporalTable.HistoryRetentionPeriod, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriod]");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, nonTemporalTable.HistoryRetentionPeriodUnit, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriodUnit]");

                    // Scripting should not contain anything related to retention
                    //
                    foreach ( string s in nonTemporalTable.Script() )
                    {
                        _NU.Assert.IsFalse(s.Contains("RETENTION"), "Error scripting non-temporal table. Temporal-specific keyword 'RETENTION' found.");
                        _NU.Assert.IsFalse(s.Contains("SYSTEM_VERSIONING"), "Error scripting non-temporal table. Temporal-specific keyword 'SYSTEM_VERSIONING' found.");
                    }

                    // Alter should have no effect as we changed nothing
                    //
                    nonTemporalTable.Alter();
                    nonTemporalTable.Refresh();

                    // Check that temporal properties are still at defaults post-alter
                    //
                    _UT.Assert.AreEqual<int>(0, nonTemporalTable.HistoryRetentionPeriod, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriod]");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, nonTemporalTable.HistoryRetentionPeriodUnit, "Non-temporal tables should have default values for temporal properties. Property: [HistoryRetentionPeriodUnit]");
                    _UT.Assert.AreEqual<string>(string.Empty, nonTemporalTable.HistoryTableName, "Non-temporal tables should have default values for temporal properties. Property: [HistoryTableName]");
                    _UT.Assert.AreEqual<int>(0, nonTemporalTable.HistoryTableID, "Non-temporal tables should have default values for temporal properties. Property: [HistoryTableID]");
                    _UT.Assert.AreEqual<bool>(false, nonTemporalTable.IsSystemVersioned, "Non-temporal tables should have default values for temporal properties. Property: [IsSystemVersioned]");

                    // Contact the server and check the values SMO has are correct for this table
                    //
                    bool tableExists;
                    bool serverReturnedIsSystemVersioned;
                    int serverReturnedRetentionPeriod;
                    _SMO.TemporalHistoryRetentionPeriodUnit serverReturnedRetentionPeriodUnit;

                    tableExists = GetTableTemporalProperties(db.Name, nonTemporalTable.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                    _NU.Assert.IsTrue(tableExists, "Table not found on the SQL Server as expected.");
                    _NU.Assert.IsFalse(serverReturnedIsSystemVersioned, "Table should not be marked as system-versioned temporal table.");
                    _UT.Assert.AreEqual<int>(0, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");
                }
            );
        }

        /// <summary>
        /// Default temporal table should have default value for the retention policy
        /// e.g. infinite retention.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_DefaultValues()
        {
            ExecuteFromDbPool(
               this.TestContext.FullyQualifiedTestClassName,
               db =>
               {
                   _SMO.Table t = CreateSimpleTemporalTable(db);

                   // Validate table actually got created
                   //
                   int res = (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(string.Format(CultureInfo.InvariantCulture, "SELECT COUNT(*) FROM SYS.TABLES WHERE NAME = '{0}'", Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(t.Name)));
                   _UT.Assert.AreEqual<int>(1, res, "Temporal table not created in the database.");

                   // Check defaults for the retention policy for the non-temporal tables
                   //
                   _UT.Assert.AreEqual<int>(-1, t.HistoryRetentionPeriod, "Invalid default value for the retention period");
                   _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Infinite, t.HistoryRetentionPeriodUnit, "Invalid default value for the retention period unit");
               });
        }

         /// <summary>
         /// Script, drop and re-create this table in order to validate that
         /// scripting does not accidently mess with retention-specific properties.
         /// These should stay at defaults as long as nothing specified
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_ScriptRoundTrip()
        {
            ExecuteWithDbDrop(
                 db =>
                 {
                     _SMO.Scripter scripter = new _SMO.Scripter(db.Parent);
                     scripter.Options.ScriptData = true;
                     scripter.Options.ScriptDrops = false;
                     scripter.Options.WithDependencies = true;
                     scripter.Options.ScriptSchema = true;
                     scripter.Options.Statistics = true;
                     scripter.Options.OptimizerData = true;
                     scripter.Options.Indexes = true;
                     scripter.Options.NonClusteredIndexes = true;
                     scripter.Options.ScriptBatchTerminator = false;

                     _SMO.Table t = CreateSimpleTemporalTable(db);
                     string tableName = t.Name;

                     System.Collections.Generic.IEnumerable<string> scripts = scripter.EnumScript(new _SMO.SqlSmoObject[] { t });

                     _SMO.Table historyTable = db.Tables[t.HistoryTableName, t.HistoryTableSchema];
                     t.Drop();
                     historyTable.Drop();

                     foreach ( string s in scripts )
                     {
                         db.ExecuteNonQuery(s);
                     }

                     db.Refresh();
                     db.Tables.Refresh();
                     t = db.Tables[tableName];
                     _NU.Assert.IsNotNull(t, "Temporal table not re-created");
                     _NU.Assert.AreEqual(-1, t.HistoryRetentionPeriod, "Invalid default value for the retention period");
                     _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Infinite, t.HistoryRetentionPeriodUnit, "Invalid default value for the retention period unit");

                     // Contact the server and check the values SMO has are correct - table should be a temporal one with infinite retention
                     //
                     bool serverReturnedIsSystemVersioned;
                     bool serverReturnedTableExists;
                     int serverReturnedRetentionPeriod;
                     _SMO.TemporalHistoryRetentionPeriodUnit serverReturnedRetentionPeriodUnit;

                     serverReturnedTableExists = GetTableTemporalProperties(db.Name, t.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                     _NU.Assert.IsTrue(serverReturnedTableExists, "Table not found on the SQL Server as expected.");
                     _NU.Assert.IsTrue(serverReturnedIsSystemVersioned, "Table should be marked as system-versioned temporal table.");
                     _UT.Assert.AreEqual<int>(-1, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                     _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Infinite, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");
             });
        }

        /// <summary>
        /// Script, drop and re-create this table in order to validate that
        /// retention properties are properly taken into account.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_ScriptRoundTrip_Retention()
        {
            ExecuteFromDbPool(
                 this.TestContext.FullyQualifiedTestClassName,
                 db =>
                 {
                     _SMO.Scripter scripter = new _SMO.Scripter(db.Parent);
                     scripter.Options.ScriptData = true;
                     scripter.Options.ScriptDrops = false;
                     scripter.Options.WithDependencies = true;
                     scripter.Options.ScriptSchema = true;
                     scripter.Options.Statistics = true;
                     scripter.Options.OptimizerData = true;
                     scripter.Options.Indexes = true;
                     scripter.Options.NonClusteredIndexes = true;
                     scripter.Options.ScriptBatchTerminator = false;

                     _SMO.Table t = CreateSimpleTemporalTable(db);
                     string tableName = t.Name;

                     // Configure the retention to non-default values
                     //
                     t.HistoryRetentionPeriod = 10;
                     t.HistoryRetentionPeriodUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Year;
                     t.Alter();

                     System.Collections.Generic.IEnumerable<string> scripts = scripter.EnumScript(new _SMO.SqlSmoObject[] { t });
                     db.Tables.Refresh();
                     _SMO.Table historyTable = db.Tables[t.HistoryTableName, t.HistoryTableSchema];
                     t.Drop();
                     historyTable.Drop();

                     foreach ( string s in scripts )
                     {
                         db.ExecuteNonQuery(s);
                     }

                     db.Refresh();
                     db.Tables.Refresh();
                     t = db.Tables[tableName];
                     _NU.Assert.IsNotNull(t, "Temporal table not re-created");
                     _NU.Assert.AreEqual(10, t.HistoryRetentionPeriod, "Invalid default value for the retention period");
                     _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Year, t.HistoryRetentionPeriodUnit, "Invalid default value for the retention period unit");

                     // History table should not have retention fields set (only current table does)
                     //
                     historyTable = db.Tables[t.HistoryTableName, t.HistoryTableSchema];
                     _NU.Assert.AreEqual(0, historyTable.HistoryRetentionPeriod, "Invalid value for the retention period for the history table");
                     _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, historyTable.HistoryRetentionPeriodUnit, "Invalid value for the retention period unit for the history table");

                     // Contact the server and check the values SMO has are correct - table should be a temporal one with infinite retention
                     //
                     bool serverReturnedIsSystemVersioned;
                     bool serverReturnedTableExists;
                     int serverReturnedRetentionPeriod;
                     _SMO.TemporalHistoryRetentionPeriodUnit serverReturnedRetentionPeriodUnit;

                     serverReturnedTableExists = GetTableTemporalProperties(db.Name, t.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                     _NU.Assert.IsTrue(serverReturnedTableExists, "Table not found on the SQL Server as expected.");
                     _NU.Assert.IsTrue(serverReturnedIsSystemVersioned, "Table should be marked as system-versioned temporal table.");
                     _UT.Assert.AreEqual<int>(10, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                     _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Year, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");
              });
        }

         /// <summary>
         /// Attempt to provide invalid values for the retention policy
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_InvalidRetentionValues()
        {
            ExecuteFromDbPool(
               this.TestContext.FullyQualifiedTestClassName,
               db =>
               {
                   _SMO.Table t = CreateSimpleTemporalTable(db);

                   // Try to change retention period to invalid value, while keeping INFINITE retention.
                   // This should not work as the value we allow for this field is -1, 0, 1, 2...
                   //
                   t.HistoryRetentionPeriod = -12; // Invalid value, will be re-setted to the default (-1) once we alter and refresh the table object

                   try
                   {
                       t.Alter();
                       _NU.Assert.Fail("It should not be possible to set history retention period to a value less than -1.");
                   }
                   catch ( _SMO.SmoException e )
                   {
                       _NU.Assert.IsTrue(e.InnerException.Message.Contains("History retention period value was not specified correctly"));
                   }

                   // values should be still at defaults
                   //
                   _NU.Assert.AreEqual(-12, t.HistoryRetentionPeriod, "Retention value changed post failed ALTER.");
                   _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Infinite, t.HistoryRetentionPeriodUnit, "Invalid value for the retention period unit");

                   try
                   {
                       t.HistoryRetentionPeriodUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Undefined;
                       t.Alter();
                       _NU.Assert.Fail("It should not be possible to set history retention period unit to a value 'undefined'");
                   }
                   catch ( _SMO.SmoException e )
                   {
                       _NU.Assert.IsTrue(e.InnerException.Message.Contains("History retention"));
                   }

                   // values should be still as before we attempted to ALTER
                   //
                   _NU.Assert.AreEqual(-12, t.HistoryRetentionPeriod, "Invalid value for the retention period");
                   _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Undefined, t.HistoryRetentionPeriodUnit, "Invalid value for the retention period unit");

                   // Contact the server and check the values SMO has are correct - table should be a temporal one with infinite retention
                   //
                   bool serverReturnedIsSystemVersioned;
                   bool serverReturnedTableExists;
                   int serverReturnedRetentionPeriod;
                   _SMO.TemporalHistoryRetentionPeriodUnit serverReturnedRetentionPeriodUnit;

                   serverReturnedTableExists = GetTableTemporalProperties(db.Name, t.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                   _NU.Assert.IsTrue(serverReturnedTableExists, "Table not found on the SQL Server as expected.");
                   _NU.Assert.IsTrue(serverReturnedIsSystemVersioned, "Table should be marked as system-versioned temporal table.");
                   _UT.Assert.AreEqual<int>(-1, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                   _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Infinite, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");
             });
        }

        /// <summary>
        /// Validates that ALTER-ing temporal table (by adding, dropping, renaming columns)
        /// does not affect properties specific to history retention policy
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalRetentionPeriod_AlterTable()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    bool serverReturnedTableExists;
                    bool serverReturnedIsSystemVersioned;
                    int serverReturnedRetentionPeriod;
                    _SMO.TemporalHistoryRetentionPeriodUnit serverReturnedRetentionPeriodUnit;

                    _SMO.Scripter scripter = new _SMO.Scripter(db.Parent);
                    scripter.Options.ScriptData = true;
                    scripter.Options.ScriptDrops = false;
                    scripter.Options.WithDependencies = true;
                    scripter.Options.ScriptSchema = true;
                    scripter.Options.Statistics = true;
                    scripter.Options.OptimizerData = true;
                    scripter.Options.Indexes = true;
                    scripter.Options.NonClusteredIndexes = true;
                    scripter.Options.ScriptBatchTerminator = false;

                    string primaryKeyName = "PK_temporal_current_" + new System.Random().Next().ToString();
                    string tableName = "CurrentTable_" + new System.Random().Next().ToString();
                    _SMO.Table t = new _SMO.Table(db, tableName);

                    _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
                    _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
                    _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

                    t.Columns.Add(c1);
                    t.Columns.Add(c2);
                    t.Columns.Add(c3);

                    _SMO.Index index = new _SMO.Index(t, primaryKeyName);
                    index.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;

                    index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
                    t.Indexes.Add(index);

                    c2.Nullable = false;
                    c3.Nullable = false;
                    c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
                    c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

                    t.AddPeriodForSystemTime(c2.Name, c3.Name, true);
                    t.DataConsistencyCheck = false;
                    t.HistoryRetentionPeriodUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Day;
                    t.HistoryRetentionPeriod = 3;
                    t.IsSystemVersioned = true;

                    t.Create();
                    t.Refresh();

                    // Add a column to this table and make sure retention fields are still at expected values
                    //
                    _SMO.Column c4 = new _SMO.Column(t, "c4", _SMO.DataType.Int);
                    t.Columns.Add(c4);
                    t.Alter();
                    db.Tables.Refresh();
                    t = db.Tables[tableName];
                    t.Refresh();
                    _NU.Assert.IsNotNull(t, "Temporal table not re-created");
                    _NU.Assert.AreEqual(3, t.HistoryRetentionPeriod, "Invalid value for the retention period");
                    _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Day, t.HistoryRetentionPeriodUnit, "Invalid value for the retention period unit");

                    // Contact the server and check the values SMO has are correct - table should be a temporal one with infinite retention
                    //
                    serverReturnedTableExists = GetTableTemporalProperties(db.Name, t.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                    _NU.Assert.IsTrue(serverReturnedTableExists, "Table not found on the SQL Server as expected.");
                    _NU.Assert.IsTrue(serverReturnedIsSystemVersioned, "Table should be marked as system-versioned temporal table.");
                    _UT.Assert.AreEqual<int>(3, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Day, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");

                    // Drop the column from this table and make sure retention fields are still at expected values
                    //
                    c4.Drop();
                    db.Refresh();
                    db.Tables.Refresh();
                    t = db.Tables[tableName];
                    _NU.Assert.IsNotNull(t, "Temporal table not re-created");
                    _NU.Assert.AreEqual(3, t.HistoryRetentionPeriod, "Invalid value for the retention period");
                    _NU.Assert.AreEqual(_SMO.TemporalHistoryRetentionPeriodUnit.Day, t.HistoryRetentionPeriodUnit, "Invalid value for the retention period unit");

                    // Contact the server and check the values SMO has are correct - table should be a temporal one with infinite retention
                    //
                    serverReturnedTableExists = GetTableTemporalProperties(db.Name, t.Name, out serverReturnedIsSystemVersioned, out serverReturnedRetentionPeriod, out serverReturnedRetentionPeriodUnit);
                    _NU.Assert.IsTrue(serverReturnedTableExists, "Table not found on the SQL Server as expected.");
                    _NU.Assert.IsTrue(serverReturnedIsSystemVersioned, "Table should be marked as system-versioned temporal table.");
                    _UT.Assert.AreEqual<int>(3, serverReturnedRetentionPeriod, "Unexpected value for the history retention period, -1 expected as the table does not have retention policy defined");
                    _UT.Assert.AreEqual<_SMO.TemporalHistoryRetentionPeriodUnit>(_SMO.TemporalHistoryRetentionPeriodUnit.Day, serverReturnedRetentionPeriodUnit, "Unexpected value for the history retention period unit, 'Infinite' expected as the table does not have retention policy defined");
                }
            );
        }

        #endregion

        #region Temporal DDL Tests

        /// <summary>
        /// Check if system-versioned temporal table can be successfully created and if
        /// metadata is properly retrieved by SMO
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyCreateTemporalSystemTimeTable()
        {
            var consistencyCheckValues = new List<bool>() { true, false };
            var defaultHistoryTableValues = new List<bool>() { true, false };
            var isPeriodStartColumnHidden = new List<bool>() { true, false };
            var isPeriodEndColumnHidden = new List<bool>() { true, false };

            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    db.Parent.SetDefaultInitFields(typeof(Table), allFields:true);
                    db.Parent.SetDefaultInitFields(typeof(Column), allFields: true);
                    foreach ( bool withConsistencyCheck in consistencyCheckValues )
                    {
                        foreach ( bool withDefaultHistoryTable in defaultHistoryTableValues )
                        {
                            foreach ( bool startColHidden in isPeriodStartColumnHidden )
                            {
                                foreach ( bool endColHidden in isPeriodEndColumnHidden )
                                {
                                    string primaryKeyName = "PK_temporal_current_" + new Random().Next().ToString();
                                    _SMO.Table t = new _SMO.Table(db, "CurrentTable_" + new Random().Next().ToString());
                                    _SMO.Table t_history = new _SMO.Table(db, "HistoryTable_" + new Random().Next().ToString());

                                    _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
                                    _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
                                    _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

                                    _SMO.Column c1_hist = new _SMO.Column(t_history, "c1", _SMO.DataType.Int);
                                    _SMO.Column c2_hist = new _SMO.Column(t_history, "SysStart", _SMO.DataType.DateTime2(5));
                                    _SMO.Column c3_hist = new _SMO.Column(t_history, "SysEnd", _SMO.DataType.DateTime2(5));

                                    t.Columns.Add(c1);
                                    t.Columns.Add(c2);
                                    t.Columns.Add(c3);

                                    t_history.Columns.Add(c1_hist);
                                    t_history.Columns.Add(c2_hist);
                                    t_history.Columns.Add(c3_hist);

                                    _SMO.Index index = new _SMO.Index(t, primaryKeyName);
                                    index.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;

                                    index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
                                    t.Indexes.Add(index);

                                    c2.Nullable = false;
                                    c3.Nullable = false;

                                    c1_hist.Nullable = false;
                                    c2_hist.Nullable = false;
                                    c3_hist.Nullable = false;

                                    c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
                                    c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

                                    if ( startColHidden )
                                    {
                                        c2.IsHidden = true;
                                    }
                                    if ( endColHidden )
                                    {
                                        c3.IsHidden = true;
                                    }

                                    t.AddPeriodForSystemTime(c2.Name, c3.Name, true);

                                    if ( !withDefaultHistoryTable )
                                    {
                                        t_history.Create();
                                        t.HistoryTableName = t_history.Name;
                                        t.HistoryTableSchema = t_history.Schema;
                                    }

                                    t.DataConsistencyCheck = withConsistencyCheck;
                                    t.IsSystemVersioned = true;

                                    t.Create();
                                    t.Refresh();

                                    // If no history table is given, we need to determine which one got auto-created
                                    //
                                    if ( withDefaultHistoryTable )
                                    {
                                        db.Tables.Refresh();
                                        t_history = db.Tables[t.HistoryTableName, t.HistoryTableSchema];
                                    }

                                    // validate metadata stuff is propagated to SMO correctly
                                    //
                                    ValidateSystemVersionedTables(t, t_history, startColHidden, endColHidden);

                                    // script both tables, re-create them and validate
                                    // (round-trip test)
                                    //
                                    DuplicateTemporalTablePairAndValidate(t, t_history, startColHidden, endColHidden);

                                    // Script DROP statement but don't drop system versioning.
                                    // ALTER TABLE SET (SYSTEM_VERSIONING = OFF) should happen
                                    // prior to dropping
                                    //
                                    t.Drop();
                                    t_history.Drop();
                                    db.Tables.Refresh();
                                    _NU.Assert.IsNull(db.Tables[t.Name], "Current table not dropped");
                                    _NU.Assert.IsNull(db.Tables[t_history.Name], "History table not dropped");
                                }
                            }
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Check if system-versioned temporal table can be memory-optimized as well
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlRequiredFeature(SqlFeature.Hekaton)]
        public void VerifyCreateHekatonTemporalTable()
        {
            ExecuteWithDbDrop(
                database =>
                {
                    // We may be running against Express, where AUTO_CLOSE is on.
                    // That option does not work for in-memory databases, so turning it off,
                    // just to be on the safe side.
                    //
                    database.DatabaseOptions.AutoClose = false;
                    database.Alter();

                    // Add Hekaton support
                    //
                    _SMO.FileGroup memoryOptimizedFg = new _SMO.FileGroup(database, String.Format("{0}_hkfg", database.Name),
                            Microsoft.SqlServer.Management.Smo.FileGroupType.MemoryOptimizedDataFileGroup);

                    memoryOptimizedFg.Create();

                    _SMO.DataFile dataFile = new _SMO.DataFile(memoryOptimizedFg, String.Format("{0}_hkfg", database.Name))
                    {
                        FileName =
                            PathWrapper.Combine(PathWrapper.GetDirectoryName(database.FileGroups[0].Files[0].FileName),
                                String.Format("{0}_hkfg", database.Name))
                    };

                    dataFile.Create();
                    database.FileGroups.Refresh();

                    List<bool> defaultHistoryTableValues = new List<bool>() { true, false };

                    foreach ( bool withDefaultHistoryTable in defaultHistoryTableValues )
                    {
                        string primaryKeyName = "PK_temporal_current_" + new Random().Next().ToString();
                        _SMO.Table t = new _SMO.Table(database, "CurrentTable_" + new Random().Next().ToString());
                        _SMO.Table t_history = new _SMO.Table(database, "HistoryTable_" + new Random().Next().ToString());

                        _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
                        _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
                        _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

                        _SMO.Column c1_hist = new _SMO.Column(t_history, "c1", _SMO.DataType.Int);
                        _SMO.Column c2_hist = new _SMO.Column(t_history, "SysStart", _SMO.DataType.DateTime2(5));
                        _SMO.Column c3_hist = new _SMO.Column(t_history, "SysEnd", _SMO.DataType.DateTime2(5));

                        t.IsMemoryOptimized = true;
                        t.Durability = _SMO.DurabilityType.SchemaAndData;
                        t.Columns.Add(c1);
                        t.Columns.Add(c2);
                        t.Columns.Add(c3);

                        t_history.Columns.Add(c1_hist);
                        t_history.Columns.Add(c2_hist);
                        t_history.Columns.Add(c3_hist);

                        _SMO.Index index = new _SMO.Index(t, primaryKeyName)
                        {
                            IsClustered = false,
                            IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey,
                            IndexType = _SMO.IndexType.NonClusteredHashIndex,
                            BucketCount = 100,
                        };

                        index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
                        t.Indexes.Add(index);

                        c2.Nullable = false;
                        c3.Nullable = false;

                        c1_hist.Nullable = false;
                        c2_hist.Nullable = false;
                        c3_hist.Nullable = false;

                        c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
                        c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

                        c2.IsHidden = true;
                        c3.IsHidden = false;

                        t.AddPeriodForSystemTime(c2.Name, c3.Name, true);

                        if ( !withDefaultHistoryTable )
                        {
                            t_history.Create();
                            t.HistoryTableName = t_history.Name;
                            t.HistoryTableSchema = t_history.Schema;
                        }

                        t.DataConsistencyCheck = true;
                        t.IsSystemVersioned = true;

                        t.Create();
                        t.Refresh();

                        // If no history table is given, we need to determine which one got auto-created
                        //
                        if ( withDefaultHistoryTable )
                        {
                            database.Tables.Refresh();
                            t_history = database.Tables[t.HistoryTableName, t.HistoryTableSchema];
                        }

                        // validate metadata stuff is propagated to SMO correctly
                        //
                        ValidateSystemVersionedTables(t, t_history, true, false);

                        // script both tables, re-create them and validate
                        // (round-trip test)
                        //
                        DuplicateTemporalTablePairAndValidate(t, t_history, true, false);

                        // Check that history table is not a Hekaton table,
                        // only the 'current' is
                        //
                        _NU.Assert.IsTrue(t.IsMemoryOptimized, "Current table should be in-memory table");
                        _NU.Assert.IsFalse(t_history.IsMemoryOptimized, "History table should not be memory-optimized");

                        // Script DROP statement but don't drop system versioning.
                        // ALTER TABLE SET (SYSTEM_VERSIONING = OFF) should happen
                        // prior to dropping
                        //
                        t.Drop();
                        t_history.Drop();
                        database.Tables.Refresh();
                        _NU.Assert.IsNull(database.Tables[t.Name], "Current table not dropped");
                        _NU.Assert.IsNull(database.Tables[t_history.Name], "History table not dropped");
                    }
                }
             );
        }

        /// <summary>
        /// Check if turning system-versioning on/off through ALTER works fine
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlRequiredFeature(SqlFeature.Hekaton)]
        public void VerifyAlterTemporalTable()
        {
            List<bool> isInMemoryList = new List<bool>() { true, false };

            foreach ( bool isInMemory in isInMemoryList )
            {
                ExecuteWithDbDrop(
                    database =>
                    {
                        if ( isInMemory )
                        {
                            // We may be running against Express, where AUTO_CLOSE is on.
                            // That option does not work for in-memory databases, so turning it off,
                            // just to be on the safe side.
                            //
                            database.DatabaseOptions.AutoClose = false;
                            database.Alter();

                            _SMO.FileGroup memoryOptimizedFg = new _SMO.FileGroup(database,
                                String.Format("{0}_hkfg", database.Name),
                                _SMO.FileGroupType.MemoryOptimizedDataFileGroup);

                            memoryOptimizedFg.Create();

                            _SMO.DataFile dataFile = new _SMO.DataFile(memoryOptimizedFg, String.Format("{0}_hkfg", database.Name))
                            {
                                FileName =
                                    PathWrapper.Combine(PathWrapper.GetDirectoryName(database.FileGroups[0].Files[0].FileName),
                                        String.Format("{0}_hkfg", database.Name))
                            };

                            dataFile.Create();
                            database.FileGroups.Refresh();
                        }

                        string primaryKeyName = "PK_temporal_current_" + new Random().Next().ToString();

                        _SMO.Table t = new _SMO.Table(database, "CurrentTable");
                        _SMO.Table t_history = new _SMO.Table(database, "HistoryTable");

                        _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
                        _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
                        _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

                        _SMO.Column c1_hist = new _SMO.Column(t_history, "c1", _SMO.DataType.Int);
                        _SMO.Column c2_hist = new _SMO.Column(t_history, "SysStart", _SMO.DataType.DateTime2(5));
                        _SMO.Column c3_hist = new _SMO.Column(t_history, "SysEnd", _SMO.DataType.DateTime2(5));

                        t.Columns.Add(c1);
                        t.Columns.Add(c2);
                        t.Columns.Add(c3);

                        t_history.Columns.Add(c1_hist);
                        t_history.Columns.Add(c2_hist);
                        t_history.Columns.Add(c3_hist);

                        if ( isInMemory )
                        {
                            t.IsMemoryOptimized = true;
                            t.Durability = _SMO.DurabilityType.SchemaAndData;
                        }

                        _SMO.Index index = new _SMO.Index(t, primaryKeyName)
                        {
                            IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey,
                            BucketCount = 100,
                        };

                        index.IndexType = isInMemory ? _SMO.IndexType.NonClusteredHashIndex : _SMO.IndexType.ClusteredIndex;

                        index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
                        t.Indexes.Add(index);

                        c2.Nullable = false;
                        c3.Nullable = false;

                        c1_hist.Nullable = false;
                        c2_hist.Nullable = false;
                        c3_hist.Nullable = false;

                        c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
                        c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

                        t.AddPeriodForSystemTime(c2.Name, c3.Name, true);

                        t.DataConsistencyCheck = true;
                        t.IsSystemVersioned = false;

                        t.Create();
                        t.Refresh();
                        t_history.Create();

                        // Now, alter the table to turn on system-versioning
                        t.HistoryTableName = t_history.Name;
                        t.HistoryTableSchema = t_history.Schema;
                        t.IsSystemVersioned = true;

                        t.Alter();
                        t.Refresh();

                        // validate metadata stuff is propagated to SMO correctly
                        //
                        ValidateSystemVersionedTables(t, t_history);

                        // Drop system-versioning
                        //
                        t.IsSystemVersioned = false;
                        t.Alter();
                        t.Refresh();

                        _NU.Assert.IsFalse(t.IsSystemVersioned, "Table should not be system-versioned");
                        _NU.Assert.IsTrue(string.IsNullOrEmpty(t.HistoryTableName), "There should be no history table");
                        _NU.Assert.IsTrue(string.IsNullOrEmpty(t.HistoryTableSchema),
                            "There should be no history table schema");
                        _UT.Assert.AreEqual<int>(0, t.HistoryTableID,
                            "History table ID should be 0 for a non-system versioned table");
                        _NU.Assert.IsTrue(t.HasSystemTimePeriod, "Period should still be present on the table");

                        // Dropping the ex-history table should be ok now
                        //
                        t_history.Drop();

                        database.Tables.Refresh();
                        _NU.Assert.IsNull(database.Tables[t_history.Name], "History table should not exist");

                        // Drop the PERIOD and validate
                        //
                        t.DropPeriodForSystemTime();
                        t.Alter();
                        t.Refresh();
                        t.Columns.Refresh();

                        _NU.Assert.IsFalse(t.IsSystemVersioned, "Table should not be system-versioned");
                        _NU.Assert.IsFalse(t.HasSystemTimePeriod, "Period should not be present on the table");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.None);

                        foreach ( _SMO.Column c in t.Columns )
                        {
                            c.Refresh();
                            _NU.Assert.IsTrue(c.GeneratedAlwaysType == _SMO.GeneratedAlwaysType.None,
                                "Column " + c.Name + " not marked as non-generated-always");
                        }

                        // Add the period back and validate
                        //
                        t.AddPeriodForSystemTime(c2.Name, c3.Name, true);
                        t.Alter();
                        t.Refresh();

                        _NU.Assert.IsFalse(t.IsSystemVersioned, "Table should not be system-versioned");
                        _NU.Assert.IsTrue(t.HasSystemTimePeriod, "Period should be present on the table");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.None);
                        _UT.Assert.AreEqual<int>(0, t.HistoryTableID,
                            "Unexpected history table ID for the non-temporal table");
                        _NU.Assert.IsTrue(String.IsNullOrEmpty(t.HistoryTableName), "Non-empty history table name");
                        _NU.Assert.IsTrue(String.IsNullOrEmpty(t.HistoryTableSchema), "Non-empty history table schema");

                        // Adding history table references and calling ALTER should have no effect
                        // since system versioning is not being turned on
                        //
                        t.HistoryTableName = "AnyName";
                        t.HistoryTableSchema = "AnySchema";
                        t.DataConsistencyCheck = false;
                        t.Alter();
                        t.Refresh();
                        _NU.Assert.IsTrue(String.IsNullOrEmpty(t.HistoryTableName), "Non-empty history table name");
                        _NU.Assert.IsTrue(String.IsNullOrEmpty(t.HistoryTableSchema), "Non-empty history table schema");
                        _UT.Assert.AreEqual<int>(0, t.HistoryTableID,
                            "History table ID should be 0 for a non-system versioned table");
                    });
            }
        }

        /// <summary>
        /// Various negative cases when specifying history table name/schema
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void InvalidTemporalHistoryTableSpecs()
        {
            ExecuteFromDbPool(
               this.TestContext.FullyQualifiedTestClassName,
               database =>
               {
                   string primaryKeyName = "PK_temporal_current_" + new Random().Next().ToString();

                   _SMO.Table t = new _SMO.Table(database, "CurrentTable");
                   _SMO.Table t_history = new _SMO.Table(database, "HistoryTable");

                   _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
                   _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
                   _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

                   _SMO.Column c1_hist = new _SMO.Column(t_history, "c1", _SMO.DataType.Int);
                   _SMO.Column c2_hist = new _SMO.Column(t_history, "SysStart", _SMO.DataType.DateTime2(5));
                   _SMO.Column c3_hist = new _SMO.Column(t_history, "SysEnd", _SMO.DataType.DateTime2(5));

                   t.Columns.Add(c1);
                   t.Columns.Add(c2);
                   t.Columns.Add(c3);

                   t_history.Columns.Add(c1_hist);
                   t_history.Columns.Add(c2_hist);
                   t_history.Columns.Add(c3_hist);

                   _SMO.Index index = new _SMO.Index(t, primaryKeyName);
                   index.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;

                   index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
                   t.Indexes.Add(index);

                   c2.Nullable = false;
                   c3.Nullable = false;

                   c1_hist.Nullable = false;
                   c2_hist.Nullable = false;
                   c3_hist.Nullable = false;

                   c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
                   c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

                   t_history.Create();

                   t.AddPeriodForSystemTime(c2.Name, c3.Name, true);

                   t.DataConsistencyCheck = false;
                   t.IsSystemVersioned = true;

                   // Provide non-existing schema
                   //
                   t.HistoryTableName = t_history.Name;
                   t.HistoryTableSchema = String.Empty;

                   try
                   {
                       t.Create();
                       _NU.Assert.Fail("Non-existing schema provided, table creation should fail.");
                   }
                   catch { };

                   // Provide non-existing table name
                   //
                   t.HistoryTableName = String.Empty;
                   t.HistoryTableSchema = t_history.Schema;

                   try
                   {
                       t.Create();
                       _NU.Assert.Fail("Should not be possible to create a table with non-existing name");
                   }
                   catch { };
               });
        }

        /// <summary>
        /// Various negative cases for specifying PERIOD for system time
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.SqlAzureDatabase)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlRequiredFeature(SqlFeature.Hekaton)]
        public void InvalidPeriodSpecifications()
        {
            List<bool> isInMemoryList = new List<bool>() { true, false };

            foreach ( bool isInMemory in isInMemoryList )
            {
                ExecuteWithDbDrop(
                    database =>
                    {
                        if ( isInMemory )
                        {
                            // We may be running against Express, where AUTO_CLOSE is on.
                            // That option does not work for in-memory databases, so turning it off,
                            // just to be on the safe side.
                            //
                            database.DatabaseOptions.AutoClose = false;
                            database.Alter();
                            DatabaseObjectHelpers.CreateMemoryOptimizedFileGroup(database, database.Name + "_hkfg");
                        }

                        _SMO.Table t = new _SMO.Table(database, "TestTableNegative");
                        _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
                        _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
                        _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(7));
                        _SMO.Column c4 = new _SMO.Column(t, "SysEnd2", _SMO.DataType.Int);
                        _SMO.Column c5 = new _SMO.Column(t, "SysEnd3", _SMO.DataType.Int);
                        _SMO.Column c6 = new _SMO.Column(t, "SysStart2", _SMO.DataType.DateTime2(7));

                        c2.Nullable = false;
                        c3.Nullable = false;
                        c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
                        c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

                        t.Columns.Add(c1);
                        t.Columns.Add(c2);
                        t.Columns.Add(c3);
                        t.Columns.Add(c4);
                        t.Columns.Add(c5);
                        t.Columns.Add(c6);

                        if ( isInMemory )
                        {
                            t.IsMemoryOptimized = true;
                            t.Durability = _SMO.DurabilityType.SchemaAndData;
                        }

                        AttemptCreatingPeriod(t, "ABCD", c3.Name, true,
                            "Should be possible to specify period with non-existing columns");
                        AttemptCreatingPeriod(t, c2.Name, String.Empty, false,
                            "Should not be able to specify period with NULL or empty column names");
                        AttemptCreatingPeriod(t, String.Empty, c1.Name, false,
                            "Should not be able to specify period with NULL or empty column names");
                        AttemptCreatingPeriod(t, c1.Name, c6.Name, true,
                            "Should be able to specify period with invalid column types");
                        AttemptCreatingPeriod(t, c1.Name, c1.Name, true,
                            "Should be able to specify period with any column name");
                        AttemptCreatingPeriod(t, null, c3.Name, false, "Nulls not allowed as column names");
                        AttemptCreatingPeriod(t, c2.Name, null, false, "Nulls not allowed as column names");

                        // attempt to drop (non-existing) period, should not work, since the table is not yet created
                        AttemptDroppingPeriod(t, false,
                            "Should not be possible to drop a period when table is not yet created");

                        // create and cancel creation of a valid period
                        t.AddPeriodForSystemTime(c2.Name, c3.Name, true);
                        t.AddPeriodForSystemTime(c2.Name, c3.Name, false);

                        // finally, create the table and check the metadata
                        c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.None;
                        c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.None;

                        if ( isInMemory )
                        {
                            _SMO.Index index = new _SMO.Index(t, "pk_" + c1.Name + "_" + t.Name)
                            {
                                IsClustered = false,
                                IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey,
                                IndexType = _SMO.IndexType.NonClusteredHashIndex,
                                BucketCount = 100,
                            };

                            index.IndexedColumns.Add(new _SMO.IndexedColumn(index, c1.Name));
                            t.Indexes.Add(index);
                        }

                        t.Create();

                        _NU.Assert.IsFalse(t.IsSystemVersioned, "System versioning should be off");
                        _NU.Assert.IsFalse(t.HasSystemTimePeriod, "Period should not be created.");
                        _UT.Assert.AreEqual<string>(String.Empty, t.HistoryTableName, "Invalid history table name");
                        _UT.Assert.AreEqual<string>(String.Empty, t.HistoryTableSchema, "Invalid history table schema");

                        // cannot drop a period when there's no period
                        AttemptDroppingPeriod(t, false, "Cannot drop a period when there's no period");

                        // drop the table and attempt to create + drop period afterwards
                        t.Drop();
                        AttemptCreatingPeriod(t, c2.Name, c3.Name, false, "Cannot create a period when the table is dropped");
                        AttemptDroppingPeriod(t, false, "Cannot drop a period when the table is already dropped");
                    });
            }
        }

        /// <summary>
        /// Check if HIDDEN columns can be created on Sql15+ (2016+)
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void VerifyTemporalHiddenColumns_Sql16_And_After()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                   database =>
                   {
                       VerifyTemporalHiddenColumnsInternal(database);
                   });
        }

        /// <summary>
        /// Check if HIDDEN columns can be created on Sql Azure Sterling+ (v12+)
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void VerifyTemporalHiddenColumns_AzureSterlingV12_And_After()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                   database =>
                   {
                       VerifyTemporalHiddenColumnsInternal(database);
                   });
        }

        /// <summary>
        /// Dependency ordered scripting should always script history table before current table
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlRequiredFeature(SqlFeature.SqlClr)]
        public void VerifyCorrectScriptingOrderWithTemporalTables()
        {
            _SMO.Table[] listOfTables = null;
            System.Collections.Generic.IEnumerable<string> scripts = null;

            // Extract the test script
            //
            const string scriptName = "DependencyOrderScriptingDb.sql";

            string script = string.Empty;

            using ( var scriptStream = typeof(Table_SmoTestSuite).GetTypeInfo().Assembly.GetManifestResourceStream(scriptName) )
            using ( var reader = new StreamReader(scriptStream) )
            {
                script = reader.ReadToEnd();
            }

            ExecuteWithDbDrop(
                database =>
                {
                    try
                    {
                        database.ExecuteNonQuery(script);
                    }
                    catch ( _SMO.FailedOperationException se )
                    {
                        //Throw a new exception here since FailedOperationExceptions have a couple nested exceptions, so to
                        //avoid having to iterate through them ourselves and append the messages we let the test framework
                        //handle that
                        throw new _SMO.FailedOperationException(String.Format("Failed to execute script {0}", scriptName), se);
                    }

                    database.Refresh();

                    // Now script all the tables including dependent objects
                    //
                    _SMO.Scripter scripter = new _SMO.Scripter(database.Parent);
                    scripter.Options.ScriptData = true;
                    scripter.Options.ScriptDrops = false;
                    scripter.Options.WithDependencies = true;
                    scripter.Options.ScriptSchema = true;
                    scripter.Options.Statistics = true;
                    scripter.Options.OptimizerData = true;
                    scripter.Options.Indexes = true;
                    scripter.Options.NonClusteredIndexes = true;
                    scripter.Options.ScriptBatchTerminator = false;

                    listOfTables = new _SMO.Table[database.Tables.Count];
                    database.Tables.CopyTo(listOfTables, 0);
                    scripts = scripter.EnumScript(listOfTables);

                    // Create a new database and re-create all the tables and other dependent objects.
                    // This is done on the same instance/version since the generated script may not work on an older version.
                    //
                    this.ExecuteMethodWithDbDrop(this.ServerContext, this.TestContext.TestName,
                    database2 =>
                    {
                        database2.Create();

                        foreach ( string str in scripts )
                        {
                            database2.ExecuteNonQuery(str);
                        }

                        // Validate temporal properties
                        //
                        Assert.IsTrue(database2.Parent.VersionMajor >= 13);

                        database2.Refresh();
                        database2.Tables.Refresh();

                        _SMO.Table t;

                        t = database2.Tables["Person_Temporal_History"];
                        _NU.Assert.IsTrue(t != null, "Table Person_Temporal_History does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.HistoryTable, "Table not of temporal history type as expected");

                        t = database2.Tables["Person_Temporal"];
                        _NU.Assert.IsTrue(t != null, "Table Person_Temporal does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.SystemVersioned, "Table not of temporal system-versioned type as expected");

                        t = database2.Tables["Employee_Temporal_History"];
                        _NU.Assert.IsTrue(t != null, "Table Employee_Temporal_History does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.HistoryTable, "Table not of temporal history type as expected");

                        t = database2.Tables["Employee_Temporal"];
                        _NU.Assert.IsTrue(t != null, "Table Employee_Temporal does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.SystemVersioned, "Table not of temporal system-versioned type as expected");

                        t = database2.Tables["A"];
                        _NU.Assert.IsTrue(t != null, "Table A does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.HistoryTable, "Table not of temporal history type as expected");

                        t = database2.Tables["B"];
                        _NU.Assert.IsTrue(t != null, "Table B does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.SystemVersioned, "Table not of temporal system-versioned type as expected");

                        t = database2.Tables["C"];
                        _NU.Assert.IsTrue(t != null, "Table C does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.HistoryTable, "Table not of temporal history type as expected");

                        t = database2.Tables["D"];
                        _NU.Assert.IsTrue(t != null, "Table D does not exist");
                        _NU.Assert.IsTrue(t.TemporalType == _SMO.TableTemporalType.SystemVersioned, "Table not of temporal system-versioned type as expected");

                    }
                 );
                });
        }

        /// <summary>
        /// Partitions of a table should be discovered by a dependency walker
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void VerifyPartitionDependenciesDiscovered()
        {
            // Extract the test script
            //
            const string scriptName = "PartitionedTable.sql";

            string script = string.Empty;

            using (var scriptStream = typeof(Table_SmoTestSuite).GetTypeInfo().Assembly.GetManifestResourceStream(scriptName))
            using (var reader = new StreamReader(scriptStream))
            {
                script = reader.ReadToEnd();
            }

            ExecuteWithDbDrop(
                database =>
                {
                    try
                    {
                        database.ExecuteNonQuery(script);
                    }
                    catch (_SMO.FailedOperationException se)
                    {
                        //Throw a new exception here since FailedOperationExceptions have a couple nested exceptions, so to
                        //avoid having to iterate through them ourselves and append the messages we let the test framework
                        //handle that
                        throw new _SMO.FailedOperationException(String.Format("Failed to execute script {0}", scriptName), se);
                    }

                    database.Refresh();

                    SqlSmoObject[] tables = database.Tables.Cast<SqlSmoObject>().ToArray();
                    var dependencyWalker = new DependencyWalker(database.Parent);
                    var dependencyTree = dependencyWalker.DiscoverDependencies(tables, DependencyType.Parents);
                    var dependencies = dependencyWalker.WalkDependencies(dependencyTree);

                    Assert.That(dependencies.Count, Is.EqualTo(3), "Not all dependencies are discovered");

                    Assert.That(dependencies[0].Urn.Type, Is.EqualTo(nameof(PartitionFunction)), "First dependency is not a partition function as expected");
                    Assert.That(dependencies[0].Urn.GetNameForType(nameof(PartitionFunction)), Is.EqualTo("AgePartFunc"), "Partition function URN not retrieved correctly");
                    
                    Assert.That(dependencies[1].Urn.Type, Is.EqualTo(nameof(PartitionScheme)), "Second dependency is not a partition scheme as expected");
                    Assert.That(dependencies[1].Urn.GetNameForType(nameof(PartitionScheme)), Is.EqualTo("AgePartScheme"), "Partition scheme URN not retrieved correctly");

                    Assert.That(dependencies[2].Urn.Type, Is.EqualTo(nameof(Table)), "Third dependency is not a table as expected");
                    Assert.That(dependencies[2].Urn.GetNameForType(nameof(Table)), Is.EqualTo("Customers"), "Table URN not retrieved correctly");
                });
        }





        #endregion

        #region Ledger Tests

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerExpectedFailures()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string ledgerTableName = SmoObjectHelpers.GenerateUniqueObjectName("ledger_table");
                    var t = new Table(db, ledgerTableName);
                    t.Columns.Add(new Column(t, "c1", DataType.Int));

                    // try to create ledger table without LedgerType defined
                    t.IsLedger = true;
                    Assert.Throws<FailedOperationException>(t.Create, "LedgerType not set, ledger creation should fail.");

                    // try with system versioned property set as well
                    t.IsSystemVersioned = true;
                    Assert.Throws<FailedOperationException>(t.Create, "LedgerType not set, ledger creation should fail.");

                    // try creating a ledger table with a custom view name but no schema defined
                    t.LedgerType = LedgerTableType.UpdatableLedgerTable;
                    t.LedgerViewName = "test_view_name";
                    Assert.Throws<FailedOperationException>(t.Create, "If ledger view name is specified, so too must be the ledger view schema");

                    // try creating a ledger table with a custom view schema but no view name defined
                    t.LedgerViewName = string.Empty;
                    t.LedgerViewSchema = "test_schema";
                    Assert.Throws<FailedOperationException>(t.Create, "If ledger view schema is specified, so too must be the ledger view name");

                    // try creating a ledger table with a custom view column name but the view itself not named
                    t.LedgerViewSchema = string.Empty;
                    t.LedgerViewOperationTypeColumnName = "col_name";
                    Assert.Throws<FailedOperationException>(t.Create, "If ledger view name is not specified, neither can the ledger view column names be");
                }
            );
        }

        [DataTestMethod]
        [DataRow(false, false, false, false, false)]    // append-only, no optional definitions
        [DataRow(false, false, true, false, false)]     // append-only, define view
        [DataRow(false, false, true, false, true)]      // append-only, define view with different schema
        [DataRow(false, false, true, true, false)]      // append-only, define view with view columns
        [DataRow(false, false, true, true, true)]       // append-only, define view with view columns and different schema
        [DataRow(true, false, false, false, false)]     // updatable, no optional definitions
        [DataRow(true, false, true, false, false)]      // updatable, define view
        [DataRow(true, false, true, false, true)]       // updatable, define view with different schema
        [DataRow(true, false, true, true, false)]       // updatable, define view with view columns
        [DataRow(true, false, true, true, true)]        // updatable, define view with view columns and different schema
        [DataRow(true, true, false, false, false)]      // updatable, define history table
        [DataRow(true, true, false, false, true)]       // updatable, define history table with different schema
        [DataRow(true, true, true, true, true)]         // updatable, define history table and view with view columns and different schema
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_LedgerTableScripting(bool systemVersioned, bool defineHistoryTable, bool defineLedgerView, bool defineLedgerViewColumns, bool differentSchema)
        {
            ExecuteFromDbPool(
                db =>
                {
                    var s = new Schema(db, SmoObjectHelpers.GenerateUniqueObjectName("ledger_schema"));
                    s.Create();

                    string ledgerTableName = SmoObjectHelpers.GenerateUniqueObjectName("ledger_table");
                    var t = new Table(db, ledgerTableName);

                    t.Columns.Add(new Column(t, "c1", DataType.Char(10)));

                    t.IsLedger = true;
                    t.IsSystemVersioned = systemVersioned;
                    t.LedgerType = systemVersioned ? LedgerTableType.UpdatableLedgerTable : LedgerTableType.AppendOnlyLedgerTable;
                    
                    if (defineHistoryTable)
                    {
                        t.HistoryTableName = SmoObjectHelpers.GenerateUniqueObjectName("ledger_history");
                        t.HistoryTableSchema = differentSchema ? s.Name : t.Schema;
                    }

                    if (defineLedgerView)
                    {
                        t.LedgerViewName = SmoObjectHelpers.GenerateUniqueObjectName("ledger_view");
                        t.LedgerViewSchema = differentSchema ? s.Name : t.Schema;
                    }

                    if (defineLedgerViewColumns && defineLedgerView)
                    {
                        t.LedgerViewTransactionIdColumnName = "trans_id_col_name";
                        t.LedgerViewSequenceNumberColumnName = "seq_num_col_name";
                        t.LedgerViewOperationTypeColumnName = "op_type_col_name";
                        t.LedgerViewOperationTypeDescColumnName = "op_type_desc_col_name";
                    }

                    // create the table
                    t.Create();

                    var t_fields = new string[] {
                        nameof(Table.LedgerType),
                        nameof(Table.LedgerViewName),
                        nameof(Table.LedgerViewSchema),
                        nameof(Table.IsDroppedLedgerTable),
                        nameof(Table.LedgerViewTransactionIdColumnName),
                        nameof(Table.LedgerViewSequenceNumberColumnName),
                        nameof(Table.LedgerViewOperationTypeColumnName),
                        nameof(Table.LedgerViewOperationTypeDescColumnName),
                        nameof(Table.HistoryTableName),
                        nameof(Table.HistoryTableSchema)};
                    db.Tables.ClearAndInitialize("", t_fields);

                    var tab = db.Tables[ledgerTableName];
                    var view = db.Views[tab.LedgerViewName, tab.LedgerViewSchema];

                    // Set the expected script based on the inputs
                    var systemVersionedScript =
                        systemVersioned
                        ? $"SYSTEM_VERSIONING = ON (HISTORY_TABLE = [{SqlSmoObject.EscapeString(tab.HistoryTableSchema, ']')}].[{SqlSmoObject.EscapeString(tab.HistoryTableName, ']')}]), {Environment.NewLine}"
                        : string.Empty;

                    var appendOnlyScript = !systemVersioned ? $"APPEND_ONLY = ON, " : string.Empty;

                    // set generated always columns for the table
                    var generatedAlwaysColumns = string.Empty;
                    if (systemVersioned)
                    {
                        // add all 4 expected generated always columns for updatable
                        generatedAlwaysColumns =
                            $"\t[ledger_start_transaction_id] [bigint] GENERATED ALWAYS AS transaction_id START HIDDEN NOT NULL,{Environment.NewLine}" +
                            $"\t[ledger_end_transaction_id] [bigint] GENERATED ALWAYS AS transaction_id END HIDDEN NULL,{Environment.NewLine}" +
                            $"\t[ledger_start_sequence_number] [bigint] GENERATED ALWAYS AS sequence_number START HIDDEN NOT NULL,{Environment.NewLine}" +
                            $"\t[ledger_end_sequence_number] [bigint] GENERATED ALWAYS AS sequence_number END HIDDEN NULL{Environment.NewLine}";
                    } else
                    {
                        // add the 2 start generated always columns for append-only
                        generatedAlwaysColumns =
                            $"\t[ledger_start_transaction_id] [bigint] GENERATED ALWAYS AS transaction_id START HIDDEN NOT NULL,{Environment.NewLine}" +
                            $"\t[ledger_start_sequence_number] [bigint] GENERATED ALWAYS AS sequence_number START HIDDEN NOT NULL{Environment.NewLine}";
                    }

                    var expectedScript = new string[]
                    {
                        "SET ANSI_NULLS ON",
                        "SET QUOTED_IDENTIFIER ON",
                        $"CREATE TABLE [{tab.Schema}].[{SqlSmoObject.EscapeString(tab.Name, ']')}]({Environment.NewLine}" +
                        $"\t[c1] [char](10) COLLATE {db.Collation} NULL,{Environment.NewLine}" +
                        generatedAlwaysColumns +
                        $") ON [PRIMARY]{Environment.NewLine}WITH{Environment.NewLine}({Environment.NewLine}" +
                        systemVersionedScript +
                        $"LEDGER = ON ({appendOnlyScript}LEDGER_VIEW = [{SqlSmoObject.EscapeString(tab.LedgerViewSchema, ']')}].[{SqlSmoObject.EscapeString(tab.LedgerViewName, ']')}] (" +
                        $"TRANSACTION_ID_COLUMN_NAME = [{tab.LedgerViewTransactionIdColumnName}], " +
                        $"SEQUENCE_NUMBER_COLUMN_NAME = [{tab.LedgerViewSequenceNumberColumnName}], " + 
                        $"OPERATION_TYPE_COLUMN_NAME = [{tab.LedgerViewOperationTypeColumnName}], " +
                        $"OPERATION_TYPE_DESC_COLUMN_NAME = [{tab.LedgerViewOperationTypeDescColumnName}]" +
                        $")){Environment.NewLine}){Environment.NewLine}"
                    };
                    var generatedScript = tab.Script();
                    Assert.That(expectedScript, _NU.Is.EqualTo(generatedScript));
                }
            );
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerAppendOnly()
        {
            CreateAndVerifyLedgerTable(systemVersioned: false, temporal: false);
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerAppendOnlyWithOptionalColumnsDefined()
        {
            CreateAndVerifyLedgerTable(systemVersioned: false, temporal: false, defineOptionalColumns: true);
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerSystemVersioned()
        {
            CreateAndVerifyLedgerTable(systemVersioned: true, temporal: false);
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerSystemVersionedWithOptionalColumnsDefined()
        {
            CreateAndVerifyLedgerTable(systemVersioned: true, temporal: false, defineOptionalColumns: true);
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerTemporal()
        {
            CreateAndVerifyLedgerTable(systemVersioned: true, temporal: true);
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoTable_CreateLedgerTemporalWithOptionalColumnsDefined()
        {
            CreateAndVerifyLedgerTable(systemVersioned: true, temporal: true, defineOptionalColumns: true);
        }

        private void CreateAndVerifyLedgerTable(bool systemVersioned, bool temporal, bool defineOptionalColumns = false)
        {
            ExecuteFromDbPool(
                db =>
                {
                    Assert.That(temporal && !systemVersioned, Is.False, "temporal and append only are incompatible ledger types");

                    string ledgerTableName = SmoObjectHelpers.GenerateUniqueObjectName("ledger_table");
                    var t = new Table(db, ledgerTableName);

                    var c1 = new Column(t, "c1", DataType.Int);
                    t.Columns.Add(c1);

                    // generated always columns that exist no matter what type of ledger table you have
                    if (defineOptionalColumns)
                    {
                        var c2 = new Column(t, "TransactionStart", DataType.BigInt) { Nullable = false, GeneratedAlwaysType = GeneratedAlwaysType.AsTransactionIdStart };
                        var c3 = new Column(t, "SequenceStart", DataType.BigInt) { Nullable = false, GeneratedAlwaysType = GeneratedAlwaysType.AsSequenceNumberStart };
                        t.Columns.Add(c2);
                        t.Columns.Add(c3);
                    }

                    // LEDGER = ON cannot be specified with PERIOD FOR SYSTEM_TIME and APPEND_ONLY = ON
                    // APPEND_ONLY = ON cannot be specified with generated always end columns
                    //
                    if (systemVersioned)
                    {
                        // System-versioned ledger tables can either be temporal or not. The difference in definition
                        // is whether a period for system time and the SysStart and SysEnd columns are defined
                        //
                        if (temporal)
                        {
                            var c4 = new Column(t, "SysStart", DataType.DateTime2(5)) { Nullable = false, GeneratedAlwaysType = GeneratedAlwaysType.AsRowStart };
                            var c5 = new Column(t, "SysEnd", DataType.DateTime2(5)) { Nullable = false, GeneratedAlwaysType = GeneratedAlwaysType.AsRowEnd };
                            t.Columns.Add(c4);
                            t.Columns.Add(c5);
                            t.AddPeriodForSystemTime(periodStartColumn: c4.Name, periodEndColumn: c5.Name, addPeriod: true);

                            string primaryKeyName = SmoObjectHelpers.GenerateUniqueObjectName("pk_ledger_table");
                            var index = new _SMO.Index(t, primaryKeyName) { IndexKeyType = IndexKeyType.DriPrimaryKey };
                            index.IndexedColumns.Add(new IndexedColumn(index, "c1"));
                            t.Indexes.Add(index);
                            t.DataConsistencyCheck = true;
                        }

                        // TransactionStart & SequenceStart are NOT Null columns
                        // TransactionEnd & SequenceEnd can be NULL
                        //
                        if (defineOptionalColumns)
                        {
                            var c6 = new Column(t, "TransactionEnd", DataType.BigInt) { Nullable = true, GeneratedAlwaysType = GeneratedAlwaysType.AsTransactionIdEnd };
                            var c7 = new Column(t, "SequenceEnd", DataType.BigInt) { Nullable = true, GeneratedAlwaysType = GeneratedAlwaysType.AsSequenceNumberEnd };
                            t.Columns.Add(c6);
                            t.Columns.Add(c7);
                        }

                        t.IsSystemVersioned = true;
                        t.LedgerType = LedgerTableType.UpdatableLedgerTable;
                    }
                    else
                    {
                        // Append-only ledger tables have system versioning turned off
                        //
                        t.IsSystemVersioned = false;
                        t.LedgerType = LedgerTableType.AppendOnlyLedgerTable;
                    }

                    t.IsLedger = true;
                    t.LedgerViewName = SmoObjectHelpers.GenerateUniqueObjectName("ledger_view");
                    t.LedgerViewSchema = t.Schema;
                    if (defineOptionalColumns)
                    {
                        t.LedgerViewTransactionIdColumnName = SmoObjectHelpers.GenerateUniqueObjectName("LedgerViewTransactionIdColumnName");
                        t.LedgerViewSequenceNumberColumnName = SmoObjectHelpers.GenerateUniqueObjectName("LedgerViewSequenceNumberColumnName");
                        t.LedgerViewOperationTypeColumnName = SmoObjectHelpers.GenerateUniqueObjectName("LedgerViewOperationTypeColumnName");
                        t.LedgerViewOperationTypeDescColumnName = SmoObjectHelpers.GenerateUniqueObjectName("LedgerViewOperationTypeDescColumnName");
                    }

                    t.Create();

                    var t_fields = new string[] {
                        nameof(Table.LedgerType),
                        nameof(Table.LedgerViewName),
                        nameof(Table.LedgerViewSchema),
                        nameof(Table.IsDroppedLedgerTable),
                        nameof(Table.LedgerViewTransactionIdColumnName),
                        nameof(Table.LedgerViewSequenceNumberColumnName),
                        nameof(Table.LedgerViewOperationTypeColumnName),
                        nameof(Table.LedgerViewOperationTypeDescColumnName) };
                    db.Tables.ClearAndInitialize("", t_fields);

                    var v_fields = new string[] { nameof(View.LedgerViewType), nameof(View.IsDroppedLedgerView) };
                    db.Views.ClearAndInitialize("", v_fields);

                    var tab = db.Tables[ledgerTableName];

                    // Default history table can't be given for ledger, we need to determine which one got auto-created
                    //
                    var t_history = db.Tables[tab.HistoryTableName, tab.HistoryTableSchema];
                    var l_view = db.Views[tab.LedgerViewName, tab.LedgerViewSchema];

                    int l_view_id = l_view.ID;
                    int table_id = t.ID;

                    // Validate metadata stuff is propagated to SMO correctly
                    // if the optional columns aren't defined, they are defaulted to hidden, which is checked as well
                    //
                    ValidateLedgerTables(current: tab, history: t_history, ledger_view: l_view, systemVersioned, temporal, !defineOptionalColumns);

                    ValidateLedgerScriptProperties(tab.Script(), systemVersioned, temporal);

                    t.Drop();
                    db.Tables.ClearAndInitialize("", t_fields);
                    db.Views.ClearAndInitialize("", v_fields);

                    // get the dropped ledger table
                    var dropped_ledger = db.Tables.ItemById(table_id);
                    Assert.That(dropped_ledger.IsDroppedLedgerTable, Is.True, "Table should be marked as a dropped ledger table");
                    Assert.That(dropped_ledger.Name, Does.Contain("MSSQL_DroppedLedgerTable"), "Table name should have been updated to include dropped identifier");

                    // get the dropped ledger view
                    var dropped_ledger_view = db.Views.ItemById(l_view_id);
                    Assert.That(dropped_ledger_view.IsDroppedLedgerView, Is.True, "View should be marked as a dropped ledger view");
                    Assert.That(dropped_ledger_view.Name, Does.Contain("MSSQL_DroppedLedgerView"), "View name should have been updated to include dropped identifier");

                    // get the dropped ledger history table
                    if (systemVersioned)
                    {
                        var dropped_history = db.Tables.ItemById(dropped_ledger.HistoryTableID);
                        Assert.That(dropped_history.Name, Does.Contain("MSSQL_DroppedLedgerHistory"), "History table name should have been updated to include dropped identifier");
                    }
                }
            );
        }

        #endregion

        #region GraphDB Tests

        private const string NodeId = "$node_id";
        private const string EdgeId = "$edge_id";
        private const string FromId = "$from_id";
        private const string ToId = "$to_id";

        /// <summary>
        /// Validates a table can be created as a node table with a single column. Node tables
        /// are normal sql tables created with the 'AS NODE' syntax marking them in metadata as
        /// node tables.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateTableAsNode()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable:true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1");

                        // The table as defined by the user above only appears to have one column, however
                        // the table will be created with two additional columns by sql automatically. These columns
                        // have system generated names.
                        //
                        VerifySmoCollectionCount(table.Columns, 3, "The two graph internal columns and the user defined column should be present.");

                        ValidateNodeTableInternalColumns(table);

                        _NU.Assert.That(table.Columns[2].Name, _NU.Does.Contain("c1"), "The user defined column name should be 'c1'.");
                    });
                });
        }

        /// <summary>
        /// Validates a table can be created as an edge table, with and without columns. Edge tables
        /// do not require additional user defined columns.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateTableAsEdge()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table tableNoColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns");

                        _SMO.Table tableWithColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns",
                            columnName: "c1");

                        // The table above has no user defined columns. The sql engine will create eight
                        // columns on behalf of the user when an edge table is created.
                        //
                        _NU.Assert.That(tableNoColumns.Columns.Count, _NU.Is.EqualTo(8), "There should be eight internal graph columns.");

                        ValidateEdgeTableInternalColumns(tableNoColumns);

                        // The 'tableWithColumns' has a single user defined column. The sql engine will create
                        // eight columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableWithColumns.Columns, 9, "There should be eight internal columns, and one user defined column.");

                        ValidateEdgeTableInternalColumns(tableWithColumns);

                        _NU.Assert.That(tableWithColumns.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name should be 'c1'.");
                    });
                });
        }

        /// <summary>
        /// Validates a table can be created as a node table when ANSI padding is enabled. The scripting with ANSI
        /// padding enabled goes through a different code path in SMO, but should make no difference
        /// when scripting node tables.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateNodeTableWithAnsiPadding()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1");

                        // The table as defined by the user above only appears to have one column, however
                        // the table will be created with two additional columns by sql automatically.
                        //
                        VerifySmoCollectionCount(table.Columns, 3, "The two graph internal columns and the user defined column should be present.");

                        ValidateNodeTableInternalColumns(table);

                        _NU.Assert.That(table.Columns[2].Name, _NU.Does.Contain("c1"), "The user defined column name should be 'c1'.");
                    });
                });
        }

        /// <summary>
        /// Validates a table can be created as an edge table, with and without columns with ANSI padding enabled.
        /// The scripting with ANSI padding enabled goes through a different code path in SMO, but should make no difference
        /// when scripting edge tables.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateEdgeTableWithAnsiPadding()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table tableNoColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns");

                        _SMO.Table tableWithColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns",
                            columnName: "c1");

                        // The table above has no user defined columns. The sql engine will create eight
                        // columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableNoColumns.Columns, 8, "There should be eight internal graph columns.");

                        ValidateEdgeTableInternalColumns(tableNoColumns);

                        // The 'tableWithColumns' has a single user defined column. The sql engine will create
                        // eight columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableWithColumns.Columns, 9, "There should be eight internal columns, and one user defined column.");

                        ValidateEdgeTableInternalColumns(tableWithColumns);

                        _NU.Assert.That(tableWithColumns.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name should be 'c1'.");
                    });
                });
        }

        /// <summary>
        /// This test validates the pseudo columns can be used as indexed columns. Pseudo columns are named
        /// with a leading '$' character and should not be quoted when passed to the server or when scripted
        /// from a SMO object that can contain them.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateIndexOnNodeTablePseudoColumns()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1",
                            indexName: "idx",
                            indexColumnName: NodeId);

                        ValidateNodeTableInternalColumns(table);

                        // Above the user defined the name of the indexed column as the pseudo column name '$node_id'.
                        // Internally this column name gets mapped to the 'graph_id' column.
                        //
                        VerifySmoCollectionCount(table.Indexes, 1, "There should be a single user defined index.");

                        VerifySmoCollectionCount(table.Indexes[0].IndexedColumns, 1, "The user defined index should have a single column.");

                        _NU.Assert.That(table.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The index name should be the underlying graph column.");
                    });
                });
        }

        /// <summary>
        /// This test validates the pseudo columns can be used as indexed columns on an ncci index. Pseudo columns are named
        /// with a leading '$' character and should not be quoted when passed to the server or when scripted
        /// from a SMO object that can contain them.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateNCCIIndexOnNodeTablePseudoColumns()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1",
                            indexName: "idx",
                            indexColumnName: NodeId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex);

                        ValidateNodeTableInternalColumns(table);

                        // Above the user defined the name of the indexed column as the pseudo column name '$node_id'.
                        // Internally this column name gets mapped to the 'graph_id' column.
                        //
                        VerifySmoCollectionCount(table.Indexes, 1, "There should be a single user defined index.");

                        VerifySmoCollectionCount(table.Indexes[0].IndexedColumns, 1, "The user defined index should have a single column.");

                        _NU.Assert.That(table.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The index name should be the underlying graph column.");
                    });
                });
        }

        /// <summary>
        /// This test validates that pseudo columns can be used as indexed columns on edge tables. Pseudo columns are named
        /// with a leading '$' character and should not be quoted when passed to the server or when scripted
        /// from a SMO object that can contain them.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateIndexOnEdgeTablePseudoColumns()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table tableNoColumnsEdge = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns_edge",
                            columnName: null,
                            indexName: "no_col_edge_edge",
                            indexColumnName: EdgeId);

                        _SMO.Table tableNoColumnsFrom = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns_from",
                            columnName: null,
                            indexName: "no_col_edge_from",
                            indexColumnName: FromId);

                        _SMO.Table tableNoColumnsTo = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns_to",
                            columnName: null,
                            indexName: "no_col_edge_to",
                            indexColumnName: ToId);

                        _SMO.Table tableWithColumnsEdge = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns_edge",
                            columnName: "c1",
                            indexName: "col_edge_edge",
                            indexColumnName: EdgeId);

                        _SMO.Table tableWithColumnsFrom = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns_from",
                            columnName: "c1",
                            indexName: "col_edge_from",
                            indexColumnName: FromId);

                        _SMO.Table tableWithColumnsTo = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns_to",
                            columnName: "c1",
                            indexName: "col_edge_to",
                            indexColumnName: ToId);

                        // The table above has no user defined columns. The sql engine will create eight
                        // columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableNoColumnsEdge.Columns, 8, "There must be eight internal columns.");
                        VerifySmoCollectionCount(tableNoColumnsFrom.Columns, 8, "There must be eight internal columns.");
                        VerifySmoCollectionCount(tableNoColumnsTo.Columns, 8, "There must be eight internal columns.");

                        ValidateEdgeTableInternalColumns(tableNoColumnsEdge);
                        ValidateEdgeTableInternalColumns(tableNoColumnsFrom);
                        ValidateEdgeTableInternalColumns(tableNoColumnsTo);

                        VerifySmoCollectionCount(tableNoColumnsEdge.Indexes, 1, "There must only be three indexes on the edge table.");
                        VerifySmoCollectionCount(tableNoColumnsFrom.Indexes, 1, "There must only be three indexes on the edge table.");
                        VerifySmoCollectionCount(tableNoColumnsTo.Indexes, 1, "There must only be three indexes on the edge table.");

                        ValidateEdgePseudoColumnIndex(tableNoColumnsEdge.Indexes[0], EdgeId);
                        ValidateEdgePseudoColumnIndex(tableNoColumnsFrom.Indexes[0], FromId);
                        ValidateEdgePseudoColumnIndex(tableNoColumnsTo.Indexes[0], ToId);

                        // The 'tableWithColumns' has a single user defined column. The sql engine will create
                        // eight columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableWithColumnsEdge.Columns, 9, "There must be eight internal columns and one user defined column.");
                        VerifySmoCollectionCount(tableWithColumnsFrom.Columns, 9, "There must be eight internal columns and one user defined column.");
                        VerifySmoCollectionCount(tableWithColumnsTo.Columns, 9, "There must be eight internal columns and one user defined column.");

                        ValidateEdgeTableInternalColumns(tableWithColumnsEdge);
                        ValidateEdgeTableInternalColumns(tableWithColumnsFrom);
                        ValidateEdgeTableInternalColumns(tableWithColumnsTo);

                        _NU.Assert.That(tableWithColumnsEdge.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name must be 'c1'.");
                        _NU.Assert.That(tableWithColumnsFrom.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name must be 'c1'.");
                        _NU.Assert.That(tableWithColumnsTo.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name must be 'c1'.");

                        VerifySmoCollectionCount(tableWithColumnsEdge.Indexes, 1, "There must be three indexes.");
                        VerifySmoCollectionCount(tableWithColumnsFrom.Indexes, 1, "There must be three indexes.");
                        VerifySmoCollectionCount(tableWithColumnsTo.Indexes, 1, "There must be three indexes.");

                        ValidateEdgePseudoColumnIndex(tableWithColumnsEdge.Indexes[0], EdgeId);
                        ValidateEdgePseudoColumnIndex(tableWithColumnsFrom.Indexes[0], FromId);
                        ValidateEdgePseudoColumnIndex(tableWithColumnsTo.Indexes[0], ToId);
                    });
                });
        }

        /// <summary>
        /// This test validates that pseudo columns can be used as indexed columns in an ncci index on edge tables.
        /// Pseudo columns are named with a leading '$' character and should not be quoted when passed to the server or when scripted
        /// from a SMO object that can contain them.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_CreateNCCIIndexOnEdgeTablePseudoColumns()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table tableNoColumns1 = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns1",
                            columnName: null,
                            indexName: "no_col_edge_edge",
                            indexColumnName: EdgeId);

                        _SMO.Table tableNoColumns2 = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns2",
                            columnName: null,
                            indexName: "no_col_edge_from",
                            indexColumnName: FromId);

                        _SMO.Table tableNoColumns3 = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns3",
                            columnName: null,
                            indexName: "no_col_edge_to",
                            indexColumnName: ToId);

                        _SMO.Table tableWithColumns1 = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns1",
                            columnName: "c1",
                            indexName: "col_edge_edge",
                            indexColumnName: EdgeId);

                        _SMO.Table tableWithColumns2 = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns2",
                            columnName: "c1",
                            indexName: "col_edge_from",
                            indexColumnName: FromId);

                        _SMO.Table tableWithColumns3 = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns3",
                            columnName: "c1",
                            indexName: "col_edge_to",
                            indexColumnName: ToId);

                        // The table above has no user defined columns. The sql engine will create eight
                        // columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableNoColumns1.Columns, 8, "There must be eight internal columns.");
                        VerifySmoCollectionCount(tableNoColumns2.Columns, 8, "There must be eight internal columns.");
                        VerifySmoCollectionCount(tableNoColumns3.Columns, 8, "There must be eight internal columns.");

                        ValidateEdgeTableInternalColumns(tableNoColumns1);
                        ValidateEdgeTableInternalColumns(tableNoColumns2);
                        ValidateEdgeTableInternalColumns(tableNoColumns3);

                        VerifySmoCollectionCount(tableNoColumns1.Indexes, 1, "There must only be three indexes on the edge table.");
                        VerifySmoCollectionCount(tableNoColumns2.Indexes, 1, "There must only be three indexes on the edge table.");
                        VerifySmoCollectionCount(tableNoColumns3.Indexes, 1, "There must only be three indexes on the edge table.");

                        ValidateEdgePseudoColumnIndex(tableNoColumns1.Indexes[0], EdgeId);
                        ValidateEdgePseudoColumnIndex(tableNoColumns2.Indexes[0], FromId);
                        ValidateEdgePseudoColumnIndex(tableNoColumns3.Indexes[0], ToId);

                        // The 'tableWithColumns' has a single user defined column. The sql engine will create
                        // eight columns on behalf of the user when an edge table is created.
                        //
                        VerifySmoCollectionCount(tableWithColumns1.Columns, 9, "There must be eight internal columns and one user defined column.");
                        VerifySmoCollectionCount(tableWithColumns2.Columns, 9, "There must be eight internal columns and one user defined column.");
                        VerifySmoCollectionCount(tableWithColumns3.Columns, 9, "There must be eight internal columns and one user defined column.");

                        ValidateEdgeTableInternalColumns(tableWithColumns1);
                        ValidateEdgeTableInternalColumns(tableWithColumns2);
                        ValidateEdgeTableInternalColumns(tableWithColumns3);

                        _NU.Assert.That(tableWithColumns1.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name must be 'c1'.");
                        _NU.Assert.That(tableWithColumns2.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name must be 'c1'.");
                        _NU.Assert.That(tableWithColumns3.Columns[8].Name, _NU.Does.Contain("c1"), "The user defined column name must be 'c1'.");

                        VerifySmoCollectionCount(tableWithColumns1.Indexes, 1, "There must be three indexes.");
                        VerifySmoCollectionCount(tableWithColumns2.Indexes, 1, "There must be three indexes.");
                        VerifySmoCollectionCount(tableWithColumns3.Indexes, 1, "There must be three indexes.");

                        ValidateEdgePseudoColumnIndex(tableWithColumns1.Indexes[0], EdgeId);
                        ValidateEdgePseudoColumnIndex(tableWithColumns2.Indexes[0], FromId);
                        ValidateEdgePseudoColumnIndex(tableWithColumns3.Indexes[0], ToId);
                    });
                });
        }

        /// <summary>
        /// This test validates the script that the table creates as a node. Node tables have system defined columns that should
        /// not be scripted. These columns are visible in the SMO table objects (as they are real columns) however the system
        /// will generate new ones when the script is run. There are two columns added by the system for node tables.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptNodeTable()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1");

                        string ExpectedNodeTable = @"CREATE TABLE [dbo]." + table.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS NODE";

                        string ExpectedNodeTableAfterCreation = @"CREATE TABLE [dbo]." + table.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS NODE ON [PRIMARY]";

                        // Validating the script before and after table creation.
                        //
                        ValidateScript(table.Script(), ExpectedNodeTable);

                        table.Create();

                        ValidateScript(table.Script(), ExpectedNodeTableAfterCreation);
                    });
                });
        }

        /// <summary>
        /// This test validates the script that the table creates as an edge. Edge tables have system defined columns that should
        /// not be scripted. These columns are visible in the SMO table objects (as they are real columns) however the system
        /// will generate new ones when the script is run. There are eight columns added by the system for edge table.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptEdgeTable()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table tableNoColumns = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns");

                        _SMO.Table tableWithColumns = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns",
                            columnName: "c1");

                        string ExpectedEdgeNoColumnsScript = @"CREATE TABLE [dbo]." + tableNoColumns.Name.SqlBracketQuoteString() + @"
AS EDGE";

                        string ExpectedEdgeNoColumnsScriptAfterCreate = @"CREATE TABLE [dbo]." + tableNoColumns.Name.SqlBracketQuoteString() + @"
AS EDGE ON [PRIMARY]";

                        string ExpectedEdgeWithColumnsScript = @"CREATE TABLE [dbo]." + tableWithColumns.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS EDGE";

                        string ExpectedEdgeWithColumnsScriptAfterCreate = @"CREATE TABLE [dbo]." + tableWithColumns.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS EDGE ON [PRIMARY]";

                        // Validating the script before and after table creation.
                        //
                        ValidateScript(tableNoColumns.Script(), ExpectedEdgeNoColumnsScript);
                        ValidateScript(tableWithColumns.Script(), ExpectedEdgeWithColumnsScript);

                        tableNoColumns.Create();
                        tableWithColumns.Create();

                        ValidateScript(tableNoColumns.Script(), ExpectedEdgeNoColumnsScriptAfterCreate);
                        ValidateScript(tableWithColumns.Script(), ExpectedEdgeWithColumnsScriptAfterCreate);
                    });
                });
        }

        /// <summary>
        /// This test validates the script the table creates as a node with an index. Pseudo columns require special handling
        /// as they don't exist in the table object and cannot be quoted when scripted.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptNodeTableIndex()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {

                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1",
                            indexName: "idx",
                            indexColumnName: NodeId);

                        string IndexScript = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + table.Name.SqlBracketQuoteString() + @"
(
	$node_id
)";

                        string IndexScriptAfterCreate = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + table.Name.SqlBracketQuoteString() + @"
(
	$node_id
)";

                        // Validating the script before and after table creation.
                        //
                        ValidateScript(table.Indexes[0].Script(), IndexScript);

                        table.Create();

                        _NU.Assert.That(table.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The first column should be the graph internal column.");

                        ValidateScript(table.Indexes[0].Script(), IndexScriptAfterCreate);
                    });
                });
        }

        /// <summary>
        /// This test validates the script the table creates as a node with an ncci index. Pseudo columns require special handling
        /// as they don't exist in the table and cannot be quoted when scripted.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptNodeTableNCCIIndex()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1",
                            indexName: "idx",
                            indexColumnName: NodeId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex);

                        string IndexScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx] ON [dbo]." + table.Name.SqlBracketQuoteString() + @"
(
	$node_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)
";

                        string IndexScriptAfterCreate = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx] ON [dbo]." + table.Name.SqlBracketQuoteString() + @"
(
	$node_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
";

                        // Validating the script before and after table creation.
                        //
                        ValidateScript(table.Indexes[0].Script(), IndexScript);

                        table.Create();

                        _NU.Assert.That(table.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The first column should be the graph internal column.");

                        ValidateScript(table.Indexes[0].Script(), IndexScriptAfterCreate);
                    });
                });
        }

        /// <summary>
        /// This test validates that indexes on edge tables can be scripted correctly. Pseudo columns require special handling
        /// as they don't exist in the table and cannot be quoted when scripted.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptEdgeTableIndex()
        {
            const string columnName = "c1";

            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table tableNoColumnsEdge = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns_edge",
                            columnName: null,
                            indexName: "no_col_edge_edge",
                            indexColumnName: EdgeId);

                        _SMO.Table tableNoColumnsFrom = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns_from",
                            columnName: null,
                            indexName: "no_col_edge_from",
                            indexColumnName: FromId);

                        _SMO.Table tableNoColumnsTo = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns_to",
                            columnName: null,
                            indexName: "no_col_edge_to",
                            indexColumnName: ToId);

                        _SMO.Table tableWithColumnsEdge = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns_edge",
                            columnName: "c1",
                            indexName: "col_edge_edge",
                            indexColumnName: EdgeId,
                            indexKeyListIncludesTableColumn: true);

                        _SMO.Table tableWithColumnsFrom = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns_from",
                            columnName: "c1",
                            indexName: "col_edge_from",
                            indexColumnName: FromId,
                            indexKeyListIncludesTableColumn: true);

                        _SMO.Table tableWithColumnsTo = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns_to",
                            columnName: "c1",
                            indexName: "col_edge_to",
                            indexColumnName: ToId,
                            indexKeyListIncludesTableColumn: true);

                        string edgeNoColumnsIdIdxScript = @"CREATE NONCLUSTERED INDEX [no_col_edge_edge] ON [dbo]." + tableNoColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)";

                        string edgeNoColumnsIdIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [no_col_edge_edge] ON [dbo]." + tableNoColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)";

                        string edgeNoColumnsFromIdxScript = @"CREATE NONCLUSTERED INDEX [no_col_edge_from] ON [dbo]." + tableNoColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id
)";

                        string edgeNoColumnsFromIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [no_col_edge_from] ON [dbo]." + tableNoColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id
)";

                        string edgeNoColumnsToIdxScript = @"CREATE NONCLUSTERED INDEX [no_col_edge_to] ON [dbo]." + tableNoColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id
)";

                        string edgeNoColumnsToIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [no_col_edge_to] ON [dbo]." + tableNoColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id
)";

                        string edgeWithColumnsIdIdxScript = @"CREATE NONCLUSTERED INDEX [col_edge_edge] ON [dbo]." + tableWithColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id,
	[c1]
)";

                        string edgeWithColumnsIdIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [col_edge_edge] ON [dbo]." + tableWithColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id,
	[c1] ASC
)";

                        string edgeWithColumnsFromIdxScript = @"CREATE NONCLUSTERED INDEX [col_edge_from] ON [dbo]." + tableWithColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id,
	[c1]
)";

                        string edgeWithColumnsFromIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [col_edge_from] ON [dbo]." + tableWithColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id,
	[c1] ASC
)";

                        string edgeWithColumnsToIdxScript = @"CREATE NONCLUSTERED INDEX [col_edge_to] ON [dbo]." + tableWithColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id,
	[c1]
)";

                        string edgeWithColumnsToIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [col_edge_to] ON [dbo]." + tableWithColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id,
	[c1] ASC
)";

                        ValidateScript(tableNoColumnsEdge.Indexes[0].Script(), edgeNoColumnsIdIdxScript);
                        ValidateScript(tableNoColumnsFrom.Indexes[0].Script(), edgeNoColumnsFromIdxScript);
                        ValidateScript(tableNoColumnsTo.Indexes[0].Script(), edgeNoColumnsToIdxScript);

                        ValidateScript(tableWithColumnsEdge.Indexes[0].Script(), edgeWithColumnsIdIdxScript);
                        ValidateScript(tableWithColumnsFrom.Indexes[0].Script(), edgeWithColumnsFromIdxScript);
                        ValidateScript(tableWithColumnsTo.Indexes[0].Script(), edgeWithColumnsToIdxScript);

                        tableNoColumnsEdge.Create();
                        tableNoColumnsFrom.Create();
                        tableNoColumnsTo.Create();

                        tableWithColumnsEdge.Create();
                        tableWithColumnsFrom.Create();
                        tableWithColumnsTo.Create();

                        VerifySmoCollectionCount(tableNoColumnsEdge.Indexes[0].IndexedColumns, 1, "This index should have a single column.");
                        _NU.Assert.That(tableNoColumnsEdge.Indexes[0].IndexedColumns[0].Name.Contains("graph_id"), _NU.Is.True, "The first column should be the graph identifier column.");

                        VerifySmoCollectionCount(tableNoColumnsFrom.Indexes[0].IndexedColumns, 2, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableNoColumnsFrom.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("from_obj_id"), "The first column should be the from object id.");
                        _NU.Assert.That(tableNoColumnsFrom.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("from_id"), "The second column should be the object id.");

                        VerifySmoCollectionCount(tableNoColumnsTo.Indexes[0].IndexedColumns, 2, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableNoColumnsTo.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("to_obj_id"), "The first column should be the to object id.");
                        _NU.Assert.That(tableNoColumnsTo.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("to_id"), "The second column should be the object id.");

                        VerifySmoCollectionCount(tableWithColumnsEdge.Indexes[0].IndexedColumns, 2, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableWithColumnsEdge.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The first column should be the graph identifier column.");
                        _NU.Assert.That(tableWithColumnsEdge.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain(columnName), "The second column should be the user defined column.");

                        VerifySmoCollectionCount(tableWithColumnsFrom.Indexes[0].IndexedColumns, 3, "This index should have exactly three columns.");
                        _NU.Assert.That(tableWithColumnsFrom.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("from_obj_id"), "The first column should be the from object id.");
                        _NU.Assert.That(tableWithColumnsFrom.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("from_id"), "The second column should be the object id.");
                        _NU.Assert.That(tableWithColumnsFrom.Indexes[0].IndexedColumns[2].Name, _NU.Does.Contain(columnName), "The third column should be the user defined column.");

                        VerifySmoCollectionCount(tableWithColumnsTo.Indexes[0].IndexedColumns, 3, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableWithColumnsTo.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("to_obj_id"), "The first column should be the to object id.");
                        _NU.Assert.That(tableWithColumnsTo.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("to_id"), "The second column should be the object id.");
                        _NU.Assert.That(tableWithColumnsTo.Indexes[0].IndexedColumns[2].Name, _NU.Does.Contain(columnName), "The third column should be the user defined column.");

                        ValidateScript(tableNoColumnsEdge.Indexes[0].Script(), edgeNoColumnsIdIdxAfterCreateScript);
                        ValidateScript(tableNoColumnsFrom.Indexes[0].Script(), edgeNoColumnsFromIdxAfterCreateScript);
                        ValidateScript(tableNoColumnsTo.Indexes[0].Script(), edgeNoColumnsToIdxAfterCreateScript);

                        ValidateScript(tableWithColumnsEdge.Indexes[0].Script(), edgeWithColumnsIdIdxAfterCreateScript);
                        ValidateScript(tableWithColumnsFrom.Indexes[0].Script(), edgeWithColumnsFromIdxAfterCreateScript);
                        ValidateScript(tableWithColumnsTo.Indexes[0].Script(), edgeWithColumnsToIdxAfterCreateScript);
                    });
                });
        }

        /// <summary>
        /// This test validates that indexes on edge tables can be scripted correctly. Pseudo columns require special handling
        /// as they don't exist in the table and cannot be quoted when scripted.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptEdgeTableNCCIIndex()
        {
            const string columnName = "c1";

            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table tableNoColumnsEdge = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns_edge",
                            columnName: null,
                            indexName: "no_col_edge_edge",
                            indexColumnName: EdgeId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex);

                        _SMO.Table tableNoColumnsFrom = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns_from",
                            columnName: null,
                            indexName: "no_col_edge_from",
                            indexColumnName: FromId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex);

                        _SMO.Table tableNoColumnsTo = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_no_columns_to",
                            columnName: null,
                            indexName: "no_col_edge_to",
                            indexColumnName: ToId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex);

                        _SMO.Table tableWithColumnsEdge = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns_edge",
                            columnName: "c1",
                            indexName: "col_edge_edge",
                            indexColumnName: EdgeId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex,
                            indexKeyListIncludesTableColumn: true);

                        _SMO.Table tableWithColumnsFrom = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns_from",
                            columnName: "c1",
                            indexName: "col_edge_from",
                            indexColumnName: FromId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex,
                            indexKeyListIncludesTableColumn: true);

                        _SMO.Table tableWithColumnsTo = CreateGraphTable(
                            database: database,
                            createTable: false,
                            isNode: false,
                            tableName: "edge_with_columns_to",
                            columnName: "c1",
                            indexName: "col_edge_to",
                            indexColumnName: ToId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex,
                            indexKeyListIncludesTableColumn: true);

                        string edgeNoColumnsIdIdxScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [no_col_edge_edge] ON [dbo]." + tableNoColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)";

                        string edgeNoColumnsIdIdxAfterCreateScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [no_col_edge_edge] ON [dbo]." + tableNoColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]";

                        string edgeNoColumnsFromIdxScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [no_col_edge_from] ON [dbo]." + tableNoColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)";

                        string edgeNoColumnsFromIdxAfterCreateScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [no_col_edge_from] ON [dbo]." + tableNoColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
";

                        string edgeNoColumnsToIdxScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [no_col_edge_to] ON [dbo]." + tableNoColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)";

                        string edgeNoColumnsToIdxAfterCreateScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [no_col_edge_to] ON [dbo]." + tableNoColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
";

                        string edgeWithColumnsIdIdxScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [col_edge_edge] ON [dbo]." + tableWithColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)";

                        string edgeWithColumnsIdIdxAfterCreateScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [col_edge_edge] ON [dbo]." + tableWithColumnsEdge.Name.SqlBracketQuoteString() + @"
(
	$edge_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
";

                        string edgeWithColumnsFromIdxScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [col_edge_from] ON [dbo]." + tableWithColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)";

                        string edgeWithColumnsFromIdxAfterCreateScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [col_edge_from] ON [dbo]." + tableWithColumnsFrom.Name.SqlBracketQuoteString() + @"
(
	$from_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
";

                        string edgeWithColumnsToIdxScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [col_edge_to] ON [dbo]." + tableWithColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0)";

                        string edgeWithColumnsToIdxAfterCreateScript = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [col_edge_to] ON [dbo]." + tableWithColumnsTo.Name.SqlBracketQuoteString() + @"
(
	$to_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]
";

                        ValidateScript(tableNoColumnsEdge.Indexes[0].Script(), edgeNoColumnsIdIdxScript);
                        ValidateScript(tableNoColumnsFrom.Indexes[0].Script(), edgeNoColumnsFromIdxScript);
                        ValidateScript(tableNoColumnsTo.Indexes[0].Script(), edgeNoColumnsToIdxScript);

                        ValidateScript(tableWithColumnsEdge.Indexes[0].Script(), edgeWithColumnsIdIdxScript);
                        ValidateScript(tableWithColumnsFrom.Indexes[0].Script(), edgeWithColumnsFromIdxScript);
                        ValidateScript(tableWithColumnsTo.Indexes[0].Script(), edgeWithColumnsToIdxScript);

                        tableNoColumnsEdge.Create();
                        tableNoColumnsFrom.Create();
                        tableNoColumnsTo.Create();
                        tableWithColumnsEdge.Create();
                        tableWithColumnsFrom.Create();
                        tableWithColumnsTo.Create();

                        VerifySmoCollectionCount(tableNoColumnsEdge.Indexes[0].IndexedColumns, 1, "This index should have a single column.");
                        _NU.Assert.That(tableNoColumnsEdge.Indexes[0].IndexedColumns[0].Name.Contains("graph_id"), _NU.Is.True, "The first column should be the graph identifier column.");

                        VerifySmoCollectionCount(tableNoColumnsFrom.Indexes[0].IndexedColumns, 2, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableNoColumnsFrom.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("from_obj_id"), "The first column should be the from object id.");
                        _NU.Assert.That(tableNoColumnsFrom.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("from_id"), "The second column should be the object id.");

                        VerifySmoCollectionCount(tableNoColumnsTo.Indexes[0].IndexedColumns, 2, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableNoColumnsTo.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("to_obj_id"), "The first column should be the to object id.");
                        _NU.Assert.That(tableNoColumnsTo.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("to_id"), "The second column should be the object id.");

                        VerifySmoCollectionCount(tableWithColumnsEdge.Indexes[0].IndexedColumns, 2, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableWithColumnsEdge.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The first column should be the graph identifier column.");
                        _NU.Assert.That(tableWithColumnsEdge.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain(columnName), "The second column should be the user defined column.");

                        VerifySmoCollectionCount(tableWithColumnsFrom.Indexes[0].IndexedColumns, 3, "This index should have exactly three columns.");
                        _NU.Assert.That(tableWithColumnsFrom.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("from_obj_id"), "The first column should be the from object id.");
                        _NU.Assert.That(tableWithColumnsFrom.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("from_id"), "The second column should be the object id.");
                        _NU.Assert.That(tableWithColumnsFrom.Indexes[0].IndexedColumns[2].Name, _NU.Does.Contain(columnName), "The third column should be the user defined column.");

                        VerifySmoCollectionCount(tableWithColumnsTo.Indexes[0].IndexedColumns, 3, "There should be exactly two columns in this index.");
                        _NU.Assert.That(tableWithColumnsTo.Indexes[0].IndexedColumns[0].Name, _NU.Does.Contain("to_obj_id"), "The first column should be the to object id.");
                        _NU.Assert.That(tableWithColumnsTo.Indexes[0].IndexedColumns[1].Name, _NU.Does.Contain("to_id"), "The second column should be the object id.");
                        _NU.Assert.That(tableWithColumnsTo.Indexes[0].IndexedColumns[2].Name, _NU.Does.Contain(columnName), "The third column should be the user defined column.");

                        ValidateScript(tableNoColumnsEdge.Indexes[0].Script(), edgeNoColumnsIdIdxAfterCreateScript);
                        ValidateScript(tableNoColumnsFrom.Indexes[0].Script(), edgeNoColumnsFromIdxAfterCreateScript);
                        ValidateScript(tableNoColumnsTo.Indexes[0].Script(), edgeNoColumnsToIdxAfterCreateScript);

                        ValidateScript(tableWithColumnsEdge.Indexes[0].Script(), edgeWithColumnsIdIdxAfterCreateScript);
                        ValidateScript(tableWithColumnsFrom.Indexes[0].Script(), edgeWithColumnsFromIdxAfterCreateScript);
                        ValidateScript(tableWithColumnsTo.Indexes[0].Script(), edgeWithColumnsToIdxAfterCreateScript);
                    });
                });
        }

        /// <summary>
        /// This test validates that node tables can have columns added and removed. Alter table should work correctly
        /// for node tables on the user defined columns. There are more extensive lockdown tests in DS_Main that validate
        /// the system defined columns cannot be altered.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_NodeTableAlterAddDropColumn()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1");

                        string ExpectedResultAfterAdd = @"CREATE TABLE [dbo]." + table.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL,
	[c2] [int] NULL
)
AS NODE ON [PRIMARY]";

                        string ExpectedResultAfterDrop = @"CREATE TABLE [dbo]." + table.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS NODE ON [PRIMARY]";

                        _SMO.Column c2 = new _SMO.Column(table, "c2", _SMO.DataType.Int);
                        table.Columns.Add(c2);

                        table.Alter();
                        table.Refresh();

                        ValidateScript(table.Script(), ExpectedResultAfterAdd);

                        table.Columns[3].Drop();
                        table.Refresh();

                        ValidateScript(table.Script(), ExpectedResultAfterDrop);
                    });
                });
        }

        /// <summary>
        /// This test validates that edge tables can have columns added and removed. Alter table should work correctly
        /// for edge tables on the user defined columns. There are more extensive lockdown tests in DS_Main that validate
        /// the system defined columns cannot be altered.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_EdgeTableAlterAddDropColumn()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table edgeWithColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_table",
                            columnName: "c1");

                        _SMO.Table edgeWithoutColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_without_table");

                        string EdgeWithColumnsAfterAlter = @"CREATE TABLE [dbo]." + edgeWithColumns.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL,
	[c2] [int] NULL
)
AS EDGE ON [PRIMARY]";

                        string EdgeWithColumnsAfterDrop = @"CREATE TABLE [dbo]." + edgeWithColumns.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS EDGE ON [PRIMARY]";

                        string EdgeWithoutColumnsAfterAlter = @"CREATE TABLE [dbo]." + edgeWithoutColumns.Name.SqlBracketQuoteString() + @"(
	[c3] [int] NULL
)
AS EDGE ON [PRIMARY]";

                        string EdgeWithoutColumnsAfterDrop = @"CREATE TABLE [dbo]." + edgeWithoutColumns.Name.SqlBracketQuoteString() + @"
AS EDGE ON [PRIMARY]";

                        _SMO.Column c2 = new _SMO.Column(edgeWithColumns, "c2", _SMO.DataType.Int);
                        edgeWithColumns.Columns.Add(c2);

                        edgeWithColumns.Alter();
                        edgeWithColumns.Refresh();

                        ValidateScript(edgeWithColumns.Script(), EdgeWithColumnsAfterAlter);

                        edgeWithColumns.Columns[9].Drop();
                        edgeWithColumns.Refresh();

                        ValidateScript(edgeWithColumns.Script(), EdgeWithColumnsAfterDrop);

                        _SMO.Column c3 = new _SMO.Column(edgeWithoutColumns, "c3", _SMO.DataType.Int);

                        edgeWithoutColumns.Columns.Add(c3);
                        edgeWithoutColumns.Alter();
                        edgeWithoutColumns.Refresh();

                        ValidateScript(edgeWithoutColumns.Script(), EdgeWithoutColumnsAfterAlter);

                        edgeWithoutColumns.Columns[8].Drop();
                        edgeWithoutColumns.Refresh();

                        ValidateScript(edgeWithoutColumns.Script(), EdgeWithoutColumnsAfterDrop);
                    });
                });
        }

        /// <summary>
        /// This test validates that an index can be added and removed from a node table. Alter index should work
        /// correctly for node tables regardless of the key columns the index includes.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void GraphDb_NodeTableAlterAddDropIndex()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1");

                        _SMO.Index idx = new _SMO.Index(table, "idx")
                        {
                            IndexType = _SMO.IndexType.NonClusteredIndex,
                            IndexKeyType = _SMO.IndexKeyType.None,
                        };

                        idx.IndexedColumns.Add(new _SMO.IndexedColumn(idx, NodeId));
                        idx.IndexedColumns.Add(new _SMO.IndexedColumn(idx, "c1"));

                        table.Indexes.Add(idx);

                        table.Alter();

                        string IndexScriptAfterCreate = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + table.Name.SqlBracketQuoteString() + @"
(
	$node_id,
	[c1] ASC
)";

                        VerifySmoCollectionCount(idx.IndexedColumns, 2, "There should be exactly two columns in the index.");
                        _NU.Assert.That(idx.IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The first columns should be the graph identifier.");
                        _NU.Assert.That(idx.IndexedColumns[1].Name, _NU.Does.Contain("c1"), "The second column should be the user defined column.");

                        ValidateScript(idx.Script(), IndexScriptAfterCreate);

                        table.Indexes[idx.Name].Drop();
                    });
                });
        }

        /// <summary>
        /// This test validates that indexes can be added and dropped from edge tables. Alter index should work correctly
        /// for edge tables regardless of the key columns the index includes.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_EdgeTableAlterAddDropIndex()
        {
            const string columnName = "c1";

            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        ((_SMO.Database)database).AnsiPaddingEnabled = true;

                        _SMO.Table tableNoColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_no_columns");

                        _SMO.Table tableWithColumns = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_with_columns",
                            columnName: "c1");

                        _SMO.Index edgeNoColumnsIdIdx = CreatePseudoColumnIndex(tableNoColumns, EdgeId, "no_col_edge");
                        _SMO.Index edgeNoColumnsFromIdx = CreatePseudoColumnIndex(tableNoColumns, FromId, "no_col_from");
                        _SMO.Index edgeNoColumnsToIdx = CreatePseudoColumnIndex(tableNoColumns, ToId, "no_col_to");

                        _SMO.Index edgeWithColumnsIdIdx = CreatePseudoColumnIndex(tableWithColumns, EdgeId, "col_edge", columnName);
                        _SMO.Index edgeWithColumnsFromIdx = CreatePseudoColumnIndex(tableWithColumns, FromId, "col_from", columnName);
                        _SMO.Index edgeWithColumnsToIdx = CreatePseudoColumnIndex(tableWithColumns, ToId, "col_to", columnName);

                        tableNoColumns.Indexes.Add(edgeNoColumnsIdIdx);

                        tableNoColumns.Alter();

                        tableWithColumns.Indexes.Add(edgeWithColumnsIdIdx);

                        tableWithColumns.Alter();

                        edgeNoColumnsFromIdx.Create();
                        edgeNoColumnsToIdx.Create();

                        edgeWithColumnsFromIdx.Create();
                        edgeWithColumnsToIdx.Create();

                        string edgeNoColumnsIdIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [no_col_edge] ON [dbo]." + tableNoColumns.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)";

                        string edgeNoColumnsFromIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [no_col_from] ON [dbo]." + tableNoColumns.Name.SqlBracketQuoteString() + @"
(
	$from_id
)";

                        string edgeNoColumnsToIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [no_col_to] ON [dbo]." + tableNoColumns.Name.SqlBracketQuoteString() + @"
(
	$to_id
)";

                        string edgeWithColumnsIdIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [col_edge] ON [dbo]." + tableWithColumns.Name.SqlBracketQuoteString() + @"
(
	$edge_id,
	[c1] ASC
)";

                        string edgeWithColumnsFromIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [col_from] ON [dbo]." + tableWithColumns.Name.SqlBracketQuoteString() + @"
(
	$from_id,
	[c1] ASC
)";

                        string edgeWithColumnsToIdxAfterCreateScript = @"CREATE NONCLUSTERED INDEX [col_to] ON [dbo]." + tableWithColumns.Name.SqlBracketQuoteString() + @"
(
	$to_id,
	[c1] ASC
)";

                        ValidateScript(edgeNoColumnsIdIdx.Script(), edgeNoColumnsIdIdxAfterCreateScript);
                        ValidateScript(edgeNoColumnsFromIdx.Script(), edgeNoColumnsFromIdxAfterCreateScript);
                        ValidateScript(edgeNoColumnsToIdx.Script(), edgeNoColumnsToIdxAfterCreateScript);

                        ValidateScript(edgeWithColumnsIdIdx.Script(), edgeWithColumnsIdIdxAfterCreateScript);
                        ValidateScript(edgeWithColumnsFromIdx.Script(), edgeWithColumnsFromIdxAfterCreateScript);
                        ValidateScript(edgeWithColumnsToIdx.Script(), edgeWithColumnsToIdxAfterCreateScript);
                    });
                });
        }

        /// <summary>
        /// Verifies indexes can be scripted that contain pseudo columns. Scripting indexes using the scripter
        /// objects should produce a script just like calling .Script() on the object.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        public void GraphDb_ScriptPseudoColumnIndex()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table nodeTable = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1",
                            indexName: "idx",
                            indexColumnName: NodeId);

                        _SMO.Table edgeTable = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_table",
                            indexName: "idx",
                            indexColumnName: EdgeId);

                        string expectedNodeResults = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + nodeTable.Name.SqlBracketQuoteString() + @"
(
	$node_id
)";

                        string expectedEdgeResults = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + edgeTable.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)";

                        ValidateUrnScripting(
                            database,
                            new[] { nodeTable.Indexes[0].Urn, edgeTable.Indexes[0].Urn },
                            new[] { expectedNodeResults, expectedEdgeResults },
                            null,
                            doContainComparison: true);

                        ValidateObjectScripting(
                            database,
                            new[] { nodeTable.Indexes[0], edgeTable.Indexes[0] },
                            new[] { expectedNodeResults, expectedEdgeResults },
                            doContainComparison: true);
                    });
                });
        }

        /// <summary>
        /// Verifies NCCI indexes can be scripted that contain pseudo columns. Scripting indexes using the scripter
        /// objects should produce a script just like calling .Script() on the object.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptPseudoColumnNCCIIndex()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table nodeTable = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1",
                            indexName: "idx",
                            indexColumnName: NodeId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex,
                            indexKeyListIncludesTableColumn: true);

                        _SMO.Table edgeTable = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: false,
                            tableName: "edge_table",
                            indexName: "idx",
                            indexColumnName: EdgeId,
                            indexType: _SMO.IndexType.NonClusteredColumnStoreIndex);

                        string expectedNodeResults = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx] ON [dbo]." + nodeTable.Name.SqlBracketQuoteString() + @"
(
	$node_id,
	[c1]
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]";
                        string expectedEdgeResults = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx] ON [dbo]." + edgeTable.Name.SqlBracketQuoteString() + @"
(
	$edge_id
)WITH (DROP_EXISTING = OFF, COMPRESSION_DELAY = 0, DATA_COMPRESSION = COLUMNSTORE) ON [PRIMARY]";

                        ValidateUrnScripting(
                            database,
                            new[] { nodeTable.Indexes[0].Urn, edgeTable.Indexes[0].Urn },
                            new[] { expectedNodeResults, expectedEdgeResults },
                            null,
                            doContainComparison: true);

                        ValidateObjectScripting(
                            database,
                            new[] { nodeTable.Indexes[0], edgeTable.Indexes[0] },
                            new[] { expectedNodeResults, expectedEdgeResults },
                            doContainComparison: true);
                    });
                });
        }

        /// <summary>
        /// Indexes that are scripted from objects not cached should still script the pseudo columns correctly. There
        /// was a regression where these indexes did not script correctly through SSMS because the parent object was not cached
        /// causing the underlying columns to be scripted instead of the pseudo columns.
        /// </summary>
        [TestMethod]
        [SqlTestArea(SqlTestArea.GraphDb)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void GraphDb_ScriptNodeEdgeWhenNotCreatedThroughSMOObjects()
        {
            ExecuteWithDbDrop(
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        string nodeTableName = SmoObjectHelpers.GenerateUniqueObjectName("node_table");
                        database.ExecuteNonQuery("CREATE TABLE " + nodeTableName.SqlBracketQuoteString() + " (c1 int) AS NODE;");
                        database.ExecuteNonQuery("CREATE INDEX idx ON " + nodeTableName.SqlBracketQuoteString() + "($node_id, c1);");
                        database.ExecuteNonQuery("CREATE NONCLUSTERED COLUMNSTORE INDEX idx2 ON " + nodeTableName.SqlBracketQuoteString() + "($node_id, c1);");

                        string edgeTableNoColumnsName = SmoObjectHelpers.GenerateUniqueObjectName("edge_no_columns");
                        database.ExecuteNonQuery("CREATE TABLE " + edgeTableNoColumnsName.SqlBracketQuoteString() + " AS EDGE;");
                        string edgeTableWithColumnsName = SmoObjectHelpers.GenerateUniqueObjectName("edge_with_columns");
                        database.ExecuteNonQuery("CREATE TABLE " + edgeTableWithColumnsName.SqlBracketQuoteString() + " (c1 int) AS EDGE;");

                        database.ExecuteNonQuery("CREATE INDEX idx ON " + edgeTableNoColumnsName.SqlBracketQuoteString() + "($edge_id, $from_id, $to_id);");
                        database.ExecuteNonQuery("CREATE NONCLUSTERED COLUMNSTORE INDEX idx2 on " + edgeTableNoColumnsName.SqlBracketQuoteString() + " ($edge_id, $from_id, $to_id);");

                        database.ExecuteNonQuery("CREATE INDEX idx ON " + edgeTableWithColumnsName.SqlBracketQuoteString() + "($edge_id, $from_id, $to_id, c1);");
                        database.ExecuteNonQuery("CREATE NONCLUSTERED COLUMNSTORE INDEX idx2 on " + edgeTableWithColumnsName.SqlBracketQuoteString() + " ($edge_id, $from_id, $to_id, c1);");

                        string databaseUrn = database.Urn;

                        var urnList = new List<Management.Sdk.Sfc.Urn>
                        {
                            CreateUrn(databaseUrn, nodeTableName, "idx"),
                            CreateUrn(databaseUrn, nodeTableName, "idx2"),
                            CreateUrn(databaseUrn, edgeTableNoColumnsName, "idx"),
                            CreateUrn(databaseUrn, edgeTableNoColumnsName, "idx2"),
                            CreateUrn(databaseUrn, edgeTableWithColumnsName, "idx"),
                            CreateUrn(databaseUrn, edgeTableWithColumnsName, "idx2")
                        };

                        string expectedNodeTableIdx = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + nodeTableName.SqlBracketQuoteString() + @"
(
	$node_id,
	[c1] ASC
)";

                        string expectedNodeTableIdx2 = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx2] ON [dbo]." + nodeTableName.SqlBracketQuoteString() + @"
(
	$node_id,
	[c1]
)";

                        string expectedEdgeNoColumnsIdx = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + edgeTableNoColumnsName.SqlBracketQuoteString() + @"
(
	$edge_id,
	$from_id,
	$to_id
)";

                        string expectedEdgeNoColumnsIdx2 = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx2] ON [dbo]." + edgeTableNoColumnsName.SqlBracketQuoteString() + @"
(
	$edge_id,
	$from_id,
	$to_id
)";

                        string expectedEdgeWithColumnsIdx = @"CREATE NONCLUSTERED INDEX [idx] ON [dbo]." + edgeTableWithColumnsName.SqlBracketQuoteString() + @"
(
	$edge_id,
	$from_id,
	$to_id,
	[c1] ASC
)";

                        string expectedEdgeWithColumnsIdx2 = @"CREATE NONCLUSTERED COLUMNSTORE INDEX [idx2] ON [dbo]." + edgeTableWithColumnsName.SqlBracketQuoteString() + @"
(
	$edge_id,
	$from_id,
	$to_id,
	[c1]
)";

                        ValidateUrnScripting(
                            database,
                            urnList.ToArray(),
                            new[]
                            {
                                expectedNodeTableIdx,
                                expectedEdgeNoColumnsIdx,
                                expectedEdgeWithColumnsIdx,
                                expectedNodeTableIdx2,
                                expectedEdgeNoColumnsIdx2,
                                expectedEdgeWithColumnsIdx2,
                            },
                            null,
                            doContainComparison: true);
                    });
                });
        }

        /// <summary>
        /// Creates a URN from the provided strings. The tables are assumed to live in the 'dbo' schema.
        /// </summary>
        /// <param name="databaseUrn">The full database urn.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="indexName">The index name.</param>
        /// <returns>The urn.</returns>
        private Management.Sdk.Sfc.Urn CreateUrn(string databaseUrn, string tableName, string indexName)
        {
            return new Management.Sdk.Sfc.Urn(
                String.Format("{0}/Table[@Name='{1}' and @Schema='dbo']/Index[@Name='{2}']",
                databaseUrn,
                tableName.SqlEscapeSingleQuote(),
                indexName.SqlEscapeSingleQuote()));
        }

        /// <summary>
        /// This method executes an action with the graph TF enabled and tries
        /// to disable the TF in the event of an exception.
        /// </summary>
        /// <param name="database">The Database to execute against.</param>
        /// <param name="action">The action.</param>
        private void ExecuteWithGraphTF(_SMO.Database database, Action action)
        {
            try
            {
                ToggleGraphTraceFlag(database, toEnable: true);

                action();
            }
            finally
            {
                ToggleGraphTraceFlag(database, toEnable: false);
            }
        }

        /// <summary>
        /// This method enables or disables the trace flag necessary to enable
        /// the graph database features.
        /// </summary>
        /// <param name="database">The SMO database to execute the query.</param>
        /// <param name="toEnable">TRUE to turn on the TF, FALSE to turn off the TF.</param>
        private void ToggleGraphTraceFlag(_SMO.Database database, bool toEnable)
        {
            if ( toEnable )
            {
                database.Parent.ExecutionManager.ExecuteNonQuery("dbcc traceon(11100, -1)");
            }
            else
            {
                database.Parent.ExecutionManager.ExecuteNonQuery("dbcc traceoff(11100, -1)");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a node or edge graph table in the default schema with the provided column and index options.
        /// </summary>
        /// <param name="database">The database to create the table in.</param>
        /// <param name="createTable">True to create the table on the server.</param>
        /// <param name="isNode">True indicates a node table, False indicates an edge table.</param>
        /// <param name="tableName">The table name.</param>
        /// <param name="columnName">The optional single column</param>
        /// <param name="indexName">The optional index name.</param>
        /// <param name="indexColumnName">The optional index column name.</param>
        /// <param name="indexType">The optional index type, defaults to non-clustered index.</param>
        /// <param name="indexKeyListIncludesTableColumn">True indicates the index key column list contains 'columnName'.</param>
        /// <returns></returns>
        private _SMO.Table CreateGraphTable(
            _SMO.Database database,
            bool createTable,
            bool isNode,
            string tableName,
            string columnName = null,
            string indexName = null,
            string indexColumnName = null,
            _SMO.IndexType indexType = _SMO.IndexType.NonClusteredIndex,
            bool indexKeyListIncludesTableColumn = false)
        {
            IndexProperties[] indexProperties = new IndexProperties[]
            {
                CreatePseudoColumnIndexProperties(
                    indexColumnName,
                    indexName,
                    indexKeyListIncludesTableColumn ? columnName : String.Empty,
                    indexType),
            };

            TableProperties tableProperties = new TableProperties()
            {
                IsNode = isNode,
                IsEdge = !isNode,
            };

            ColumnProperties[] columnProperties = new ColumnProperties[]
            {
                new ColumnProperties(columnName),
            };

            return createTable ?
                DatabaseObjectHelpers.CreateTable(
                    database,
                    tableName,
                    null,
                    tableProperties,
                    columnName == null ? null : columnProperties,
                    indexName == null ? null : indexProperties,
                    includeNameUniqueifier: true) :
                DatabaseObjectHelpers.CreateTableDefinition(
                    database,
                    tableName,
                    null,
                    tableProperties,
                    columnName == null ? null : columnProperties,
                    indexName == null ? null : indexProperties,
                    includeNameUniqueifier: true);
        }

        /// <summary>
        /// Helper for validating scripting.
        /// </summary>
        /// <param name="stringCollection">The string collection</param>
        /// <param name="expectedScript">The expected script.</param>
        private void ValidateScript(StringCollection stringCollection, string expectedScript)
        {
            _NU.StringAssert.Contains(expectedScript.FixNewLines(), stringCollection.ToSingleString(), "Did not find expected scripted settings");
        }

        /// <summary>
        /// This method validates some known properties of a pseudo column index.
        /// </summary>
        /// <param name="index">The pseudo column index.</param>
        /// <param name="pseudoColumnName">The pseudo column name.</param>
        private void ValidateEdgePseudoColumnIndex(_SMO.Index index, string pseudoColumnName)
        {
            switch ( pseudoColumnName )
            {
                case "$edge_id":
                    _NU.Assert.That(index.IndexedColumns.Count, _NU.Is.EqualTo(1), "There must be one column for the edge id index.");
                    _NU.Assert.That(index.IndexedColumns[0].Name, _NU.Does.Contain("graph_id"), "The index must be on the underlying column.");
                    break;
                case "$from_id":
                    _NU.Assert.That(index.IndexedColumns.Count, _NU.Is.EqualTo(2), "There must be two columns for the from id index.");
                    _NU.Assert.That(index.IndexedColumns[0].Name, _NU.Does.Contain("from_obj_id"), "The first column should be the object id.");
                    _NU.Assert.That(index.IndexedColumns[1].Name, _NU.Does.Contain("from_id"), "The second column should be the graph id.");
                    break;
                case "$to_id":
                    _NU.Assert.That(index.IndexedColumns.Count, _NU.Is.EqualTo(2), "There must be two columns for the from id index.");
                    _NU.Assert.That(index.IndexedColumns[0].Name, _NU.Does.Contain("to_obj_id"), "The first column should be the object id.");
                    _NU.Assert.That(index.IndexedColumns[1].Name, _NU.Does.Contain("to_id"), "The second column should be the graph id.");
                    break;
                default:
                    _NU.Assert.Fail("Invalid pseudocolumn name: {0}", pseudoColumnName);
                    break;
            }
        }

        /// <summary>
        /// This method creates a simple index to help validate node and edge psuedo column indexes.
        /// </summary>
        /// <param name="table">The table to add the index to.</param>
        /// <param name="pseudoColumnName">The particular pseudo column name.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="additionalColumnName">An additional key column name.</param>
        /// <returns>The index.</returns>
        private _SMO.Index CreatePseudoColumnIndex(_SMO.Table table, string pseudoColumnName, string indexName, string additionalColumnName = "")
        {
            _SMO.Index idx = new _SMO.Index(table, indexName)
            {
                IndexType = Management.Smo.IndexType.NonClusteredIndex,
                IndexKeyType = Management.Smo.IndexKeyType.None,
            };

            idx.IndexedColumns.Add(new _SMO.IndexedColumn(idx, pseudoColumnName));

            if ( additionalColumnName != String.Empty )
            {
                idx.IndexedColumns.Add(new _SMO.IndexedColumn(idx, additionalColumnName));
            }

            return idx;
        }

        /// <summary>
        /// This method creates an 'IndexProperties' object for the specific pseudo column.
        /// </summary>
        /// <param name="pseudoColumnName">The pseudo column name.</param>
        /// <param name="indexName">The index name.</param>
        /// <param name="additionalColumn">An additional column if desired.</param>
        /// <param name="indexType">The type of index to create.</param>
        /// <returns>The index properties.</returns>
        private IndexProperties CreatePseudoColumnIndexProperties(string pseudoColumnName, string indexName, string additionalColumn= "", _SMO.IndexType indexType = _SMO.IndexType.NonClusteredIndex)
        {
            IndexProperties indexProperties = new IndexProperties()
            {
                IndexType = indexType,
                KeyType = _SMO.IndexKeyType.None,
                Name = indexName,
            };

            if (!String.IsNullOrEmpty(additionalColumn))
            {
                indexProperties.ColumnNames = new string[] { pseudoColumnName, additionalColumn };
            }
            else
            {
                indexProperties.ColumnNames = new string[] { pseudoColumnName };
            }

            return indexProperties;
        }

        /// <summary>
        /// Verifies the pre-created columns for edge tables.
        /// </summary>
        /// <param name="table">The SMO table.</param>
        private void ValidateEdgeTableInternalColumns(_SMO.Table table)
        {
            _NU.Assert.That(table.IsEdge, _NU.Is.True, "Edge property must be set for edge tables.");
            _NU.Assert.That(table.Columns[0].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphId), "The first edge column must be the graph identifier column.");
            _NU.Assert.That(table.Columns[1].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphIdComputed), "The second edge column must be the graph identifier 'computed' column.");
            _NU.Assert.That(table.Columns[2].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphFromObjId), "The third edge column must be the from object id column.");
            _NU.Assert.That(table.Columns[3].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphFromId), "The fourth edge column must be the from graph identifier column.");
            _NU.Assert.That(table.Columns[4].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphFromIdComputed), "The fifth edge column must be the from identifier 'computed' column.");
            _NU.Assert.That(table.Columns[5].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphToObjId), "The sixth edge column must be the graph to object identifier column.");
            _NU.Assert.That(table.Columns[6].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphToId), "The seventh edge column must be the graph to identifier column.");
            _NU.Assert.That(table.Columns[7].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphToIdComputed), "The eighth edge column must be the graph to identifier 'computed' column.");
        }

        /// <summary>
        /// Verifies the pre-created columns for node tables.
        /// </summary>
        /// <param name="table">The SMO table.</param>
        private void ValidateNodeTableInternalColumns(_SMO.Table table)
        {
            _NU.Assert.That(table.IsNode, _NU.Is.True, "Node property must be set for node tables.");
            _NU.Assert.That(table.Columns[0].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphId), "The graph type of the column is incorrect, this is a product bug.");
            _NU.Assert.That(table.Columns[1].GraphType, _NU.Is.EqualTo(_SMO.GraphType.GraphIdComputed), "The graph type of the column is incorrect, this is a product bug.");
        }

        /// <summary>
        /// Create a copy of system-versioned table pair and validates everything's ok
        /// </summary>
        /// <param name="currentTable">current table</param>
        /// <param name="historyTable">history table</param>
        /// <param name="startColumnIsHidden">is period start column marked as hidden</param>
        /// <param name="endColumnIsHidden">is period end column marked as hidden</param>
        private void DuplicateTemporalTablePairAndValidate(_SMO.Table currentTable, _SMO.Table historyTable, bool startColumnIsHidden = false, bool endColumnIsHidden = false)
        {
            // script both tables and try to recreate them using different names
            string newHistoryTableName = historyTable.Name + "_NewHist";
            string newCurrentTableName = currentTable.Name + "_NewCurr";
            string primaryKeyName = String.Empty;

            System.Collections.Specialized.StringCollection ddlHistoryTable = historyTable.Script();
            System.Collections.Specialized.StringCollection ddlCurrentTable = currentTable.Script(new _SMO.ScriptingOptions() { DriAll = true });

            // determine the primary key name for the current table. We have to give
            // a new name for the primary key of the new current table
            //
            foreach ( _SMO.Index idx in currentTable.Indexes )
            {
                if ( idx.IndexKeyType == _SMO.IndexKeyType.DriPrimaryKey )
                {
                    primaryKeyName = idx.Name;
                    break;
                }
            }

            foreach ( string s in ddlHistoryTable )
            {
                string query = s.Replace(historyTable.Name, newHistoryTableName);
                currentTable.Parent.ExecuteNonQuery(s.Replace(historyTable.Name, newHistoryTableName));
            }

            foreach ( string s in ddlCurrentTable )
            {
                string query = s.Replace(currentTable.Name, newCurrentTableName);
                query = query.Replace(primaryKeyName, primaryKeyName + "_new");
                query = query.Replace(historyTable.Name, newHistoryTableName);
                currentTable.Parent.ExecuteNonQuery(query);
            }
            currentTable.Parent.Tables.Refresh();

            // newly created set of tables should be ok
            //
            ValidateSystemVersionedTables(
                currentTable.Parent.Tables[newCurrentTableName],
                currentTable.Parent.Tables[newHistoryTableName],
                startColumnIsHidden,
                endColumnIsHidden
                );
        }

        /// <summary>
        /// This method validates that metadata retrieved by SMO about system-versioned table pair (current, history)
        /// is corrent. The following stuff is checked for:
        ///
        /// 1. Current table must have a PERIOD defined (table.HasSystemTimePeriod == true)
        /// 2. Current table must be system-versioned table (table.IsSystemVersioned == true)
        /// 3. History table name must match the 'HistoryTableName' property of a Current table
        /// 4. History table schema must match the 'HistoryTableSchema' property of a Current table
        /// 5. Current table has to be System-versioned table (table.TableTemporalType == SystemVersioned)
        /// 6. ID of a history table must match the 'HistoryTableID' property of a current table
        /// 7. Current table must have one and only one column that has property GeneratedAlwaysType = AsRowStart
        /// 8. Current table must have one and only one column that has property GeneratedAlwaysType = AsRowEnd
        /// 9. 'Start column' name must match 'SystemTimePeriodStartColumn' property of a current table
        /// 10. 'End column' name must match 'SystemTimePeriodEndColumn' property of a current table
        /// </summary>
        private void ValidateSystemVersionedTables(Table current, Table history, bool startColumnIsHidden = false, bool endColumnIsHidden = false, bool isLedger = false)
        {
            Assert.IsTrue(current.HasSystemTimePeriod, "Table should have a period defined.");
            Assert.IsTrue(current.IsSystemVersioned, "Table should be a system-versioned table.");
            Assert.AreEqual(history.Name, current.HistoryTableName, "Invalid history table name");
            Assert.AreEqual(history.Schema, current.HistoryTableSchema, "Invalid history table schema");
            Assert.AreEqual(TableTemporalType.SystemVersioned, current.TemporalType, "Invalid temporal type property");
            Assert.AreEqual(history.ID, current.HistoryTableID, "Invalid history table ID.");

            // count generated always columns
            _SMO.Column sysStart = null;
            _SMO.Column sysEnd = null;

            Assert.Multiple(() =>
            {
                foreach (Column c in current.Columns)
                {
                    if (c.GeneratedAlwaysType == GeneratedAlwaysType.AsRowStart)
                    {
                        if (sysStart != null)
                        {
                            Assert.Fail($"More than one column marked as 'AsRowStart' ({sysStart.Name} and {c.Name})");
                        }
                        else
                        {
                            sysStart = c;
                            Assert.AreEqual(c.IsHidden, startColumnIsHidden, $"Invalid value of HIDDEN flag : {c.InternalName}, {startColumnIsHidden}");
                        }
                    }
                    else if (c.GeneratedAlwaysType == GeneratedAlwaysType.AsRowEnd)
                    {
                        if (sysEnd != null)
                        {
                            Assert.Fail($"More than one column marked as 'AsRowEnd' ({sysEnd.Name} and {c.Name})");
                        }
                        else
                        {
                            sysEnd = c;
                            Assert.AreEqual(c.IsHidden, endColumnIsHidden, $"Invalid value of HIDDEN flag: {c.InternalName}, {endColumnIsHidden}");
                        }
                    }
                    else if (!isLedger)
                    {
                        // Only period columns can be hidden (unless the table is also ledger)
                        Assert.IsFalse(c.IsHidden, "Only PERIOD columns can be hidden");
                    }
                }
            });

            Assert.NotNull(sysStart, "Expected to find start column for period.");
            Assert.NotNull(sysEnd, "Expected to find end column for period.");

            Assert.AreEqual(sysStart.Name, current.SystemTimePeriodStartColumn, "Unexpected start column");
            Assert.AreEqual(sysEnd.Name, current.SystemTimePeriodEndColumn, "Unexpected start column");
        }

        /// <summary>
        /// This method validates that metadata retrieved by SMO about ledger tables is correct. The following is checked:
        /// 
        ///  1. Ledger view properties match ledger table view properties
        ///  2. History table properties match Ledger table properties
        ///  3. Ledger Table Type property matches with the table definition
        ///  4. Ledger History Table Type property is LedgerHistoryTable
        ///  5. Append only ledger tables do not have a history table
        ///  6. If the ledger table is also temporal, verifies the temporal properties in ValidateSystemVersionedTables
        ///  7. All ledger tables must have one and only one column that has property GeneratedAlwaysType = AsTransactionIdStart
        ///  8. Updatable ledger tables must have one and only one column that has property GeneratedAlwaysType = AsTransactionIdEnd
        ///  9. All ledger tables must have one and only one column that has property GeneratedAlwaysType = AsSequenceNumberStart
        /// 10. Updatable ledger tables must have one and only one column that has property GeneratedAlwaysType = AsSequenceNumberEnd
        /// 11. All ledger tables have at least one user-defined column
        /// </summary>
        /// <param name="current">the current ledger table to be verified</param>
        /// <param name="history">the history table for the ledger table to be verified (can be null)</param>
        /// <param name="ledger_view">the view for the ledger table to be verified.</param>
        /// <param name="systemVersioned">whether the table is system-versioned or append-only</param>
        /// <param name="temporal">whether the table is temporal or not</param>
        /// <param name="genAlwaysHidden">whether the generated always columns are hidden</param>
        private void ValidateLedgerTables(Table current, Table history, View ledger_view, bool systemVersioned, bool temporal, bool genAlwaysHidden)
        {

            // Validate ledger view properties
            Assert.AreEqual(current.LedgerViewName, ledger_view.Name, "Invalid ledger view name");
            Assert.AreEqual(LedgerViewType.LedgerView, ledger_view.LedgerViewType, "Ledger view is not marked as ledger");

            if (systemVersioned)
            {
                _UT.Assert.AreEqual(history.Name, current.HistoryTableName, "Invalid history table name");
                _UT.Assert.AreEqual(history.Schema, current.HistoryTableSchema, "Invalid history table schema");
                _UT.Assert.AreEqual(history.ID, current.HistoryTableID, "Invalid history table ID");
                _UT.Assert.AreEqual(LedgerTableType.UpdatableLedgerTable, current.LedgerType, "Invalid ledger table type property");
                _UT.Assert.AreEqual(LedgerTableType.HistoryTable, history.LedgerType, "Invalid ledger history table type property");
            }
            else
            {
                Assert.That(history, Is.Null, "Append-only ledger tables do not have a history table");
                Assert.AreEqual(current.HistoryTableID, 0, "Append-only ledger tables do not have a history table");
                Assert.AreEqual(LedgerTableType.AppendOnlyLedgerTable, current.LedgerType, "Invalid ledger table type property");
            }

            if (temporal)
            {
                ValidateSystemVersionedTables(current, history, isLedger: true);
                _UT.Assert.AreEqual(TableTemporalType.SystemVersioned, current.TemporalType, "Invalid temporal type property");
                _UT.Assert.AreEqual(TableTemporalType.HistoryTable, history.TemporalType, "Invalid temporal history table type property");
            }

            Column trxStart = null;
            Column trxEnd = null;
            Column seqStart = null;
            Column seqEnd = null;
            Column userCol = null;

            // Validate columns
            Assert.Multiple(() =>
            {
                // columns collection needs to be reinitialized to include generated always columns that weren't explicitly defined
                // in table creation
                current.Columns.ClearAndInitialize("", null);

                foreach (Column c in current.Columns)
                {
                    switch (c.GeneratedAlwaysType)
                    {
                        case GeneratedAlwaysType.AsTransactionIdStart:
                            Assert.That(trxStart, Is.Null, $"More than one column marked as 'AsTransactionIdStart'");
                            trxStart = c;
                            Assert.AreEqual(c.IsHidden, genAlwaysHidden, $"'AsTransactionIdStart' generated always column {c.InternalName} must be hidden.");
                            break;
                        case GeneratedAlwaysType.AsTransactionIdEnd:
                            Assert.That(trxEnd, Is.Null, $"More than one column marked as 'AsTransactionIdEnd'");
                            trxEnd = c;
                            Assert.AreEqual(c.IsHidden, genAlwaysHidden, $"'AsTransactionIdEnd' generated always column {c.InternalName} must be hidden.");
                            break;
                        case GeneratedAlwaysType.AsSequenceNumberStart:
                            Assert.That(seqStart, Is.Null, $"More than one column marked as 'AsSequenceNumberStart'");
                            seqStart = c;
                            Assert.AreEqual(c.IsHidden, genAlwaysHidden, $"'AsSequenceNumberStart' generated always column {c.InternalName} must be hidden.");
                            break;
                        case GeneratedAlwaysType.AsSequenceNumberEnd:
                            Assert.That(seqEnd, Is.Null, $"More than one column marked as 'AsSequenceNumberEnd'");
                            seqEnd = c;
                            Assert.AreEqual(c.IsHidden, genAlwaysHidden, $"'AsSequenceNumberEnd' generated always column {c.InternalName} must be hidden.");
                            break;
                        case GeneratedAlwaysType.AsRowStart:
                        case GeneratedAlwaysType.AsRowEnd:
                            Assert.That(temporal, Is.True, "Only temporal ledger tables have 'AsRowStart' and 'AsRowEnd' columns");
                            break;
                        case GeneratedAlwaysType.None:
                            // assert there's at least one user column
                            userCol = c;
                            break;
                    }
                }
            });

            Assert.That(userCol, Is.Not.Null, "Ledger table must have at least one user column defined");
            Assert.That(trxStart, Is.Not.Null, "Ledger table must have a transaction start column");
            Assert.That(seqStart, Is.Not.Null, "Ledger table must have a sequence number start column");

            // System versioned ledger tables
            if (systemVersioned)
            {
                Assert.That(trxEnd, Is.Not.Null, "Expected to find end transaction id column in system-versioned ledger table.");
                Assert.That(seqEnd, Is.Not.Null, "Expected to find end sequence number column in system-versioned ledger table.");
            }
        }

        /// <summary>
        /// Helper for validating ledger table scripting.
        /// </summary>
        /// <param name="generatedScript">The script generated for the table</param>
        /// <param name="systemVersioned">Whether the table is system-versioned or append only</param>
        /// <param name="temporal">Whether the table is temporal or not</param>
        private void ValidateLedgerScriptProperties(StringCollection generatedScript, bool systemVersioned, bool temporal)
        {
            // ledger specific
            string LedgerClause = "LEDGER = ON";
            string LedgerViewClause = "LEDGER_VIEW";

            // append only specific
            string AppendOnlyClause = "APPEND_ONLY";

            // system versioned specific
            string SystemVersionedClause = "SYSTEM_VERSIONING";
            string HistoryTableClause = "HISTORY_TABLE";

            // temporal specific
            string TemporalPeriodClause = "PERIOD FOR SYSTEM_TIME";

            // first 2 paramaters of the generated script contain "SET ANSI_NULLS ON" "SET QUOTED_IDENTIFIER ON", the third parameter contains the table script
            //
            Assert.That(generatedScript[2], Does.Contain(LedgerClause), "Did not find expected scripted setting LEDGER = ON");
            Assert.That(generatedScript[2], Does.Contain(LedgerViewClause), "Did not find expected scripted setting LEDGER_VIEW");
            
            if (systemVersioned)
            {
                Assert.That(generatedScript[2], Does.Contain(SystemVersionedClause), "SYSTEM_VERSIONING tag is expected in a system versioned ledger table");
                Assert.That(generatedScript[2], Does.Contain(HistoryTableClause), "HISTORY_TABLE tag is expected in a system versioned ledger table");
                Assert.That(generatedScript[2], Does.Not.Contain(AppendOnlyClause), "APPEND_ONLY tag is NOT expected in a system versioned ledger table");
            }
            else
            {
                Assert.That(generatedScript[2], Does.Contain(AppendOnlyClause), "APPEND_ONLY tag is expected in an append only ledger table");
                Assert.That(generatedScript[2], Does.Not.Contain(SystemVersionedClause), "SYSTEM_VERSIONING tag is NOT expected in an append only ledger table");
                Assert.That(generatedScript[2], Does.Not.Contain(TemporalPeriodClause), "PERIOD FOR SYSTEM_TIMNE tag is NOT expected in an append only ledger table");
                Assert.That(generatedScript[2], Does.Not.Contain(HistoryTableClause), "HISTORY_TABLE tag is NOT expected in an append only ledger table");
            }
            if (temporal)
            {
                Assert.That(generatedScript[2], Does.Contain(SystemVersionedClause), $"{SystemVersionedClause} SYSTEM_VERSIONING tag is expected in a temporal ledger table");
                Assert.That(generatedScript[2], Does.Contain(TemporalPeriodClause), "PERIOD FOR SYSTEM_TIME tag is expected in a temporal ledger table");
                Assert.That(generatedScript[2], Does.Not.Contain(AppendOnlyClause), "APPEND_ONLY tag is NOT expected in a temporal ledger table");
            }
        }

        private void VerifyTemporalHiddenColumnsInternal(_SMO.Database database)
        {
            _SMO.Table t = new _SMO.Table(database, "CurrentTable");

            _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
            _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
            _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

            t.Columns.Add(c1);
            t.Columns.Add(c2);
            t.Columns.Add(c3);

            _SMO.Index index = new _SMO.Index(t, "pk_current");
            index.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;

            index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
            t.Indexes.Add(index);

            c2.Nullable = false;
            c3.Nullable = false;

            // mark both columns as hidden
            //
            c2.IsHidden = true;
            c3.IsHidden = true;

            c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
            c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;
            t.AddPeriodForSystemTime(c2.Name, c3.Name, true);
            t.DataConsistencyCheck = false;
            t.IsSystemVersioned = true;

            t.Create();
            t.Refresh();

            _NU.Assert.IsTrue(c2.IsHidden, "Start column should be hidden");
            _NU.Assert.IsTrue(c3.IsHidden, "End column should be hidden");

            // try to remove hidden flag from the column, nothing should happen
            //
            c2.IsHidden = false;
            t.Alter();
            t.Refresh();
            t.Columns.Refresh();

            // try to create non-temporal table that has hidden columns
            // should not work
            //
            _SMO.Table t2 = new _SMO.Table(database, "InvalidTable");
            _SMO.Column t2_c = new _SMO.Column(t2, "c1", _SMO.DataType.Int);
            t2_c.IsHidden = true;
            t2.Columns.Add(t2_c);

            try
            {
                t2.Create();
                _NU.Assert.Fail("Should not be possible to have non-temporal HIDDEN columns.");
            }
            catch { };
        }

        /// <summary>
        /// Adds PERIOD for system time to the given table and validates if the operation's
        /// outcome was as expected
        /// </summary>
        private void AttemptCreatingPeriod(_SMO.Table t, string startCol, string endCol, bool shouldSucceed, string errorMessage)
        {
            try
            {
                t.AddPeriodForSystemTime(startCol, endCol, true);
                if ( !shouldSucceed )
                {
                    _NU.Assert.Fail(String.Format("Adding PERIOD should not have succeeded. Table: {0}, start column: {1}, end column: {2}", t.Name, startCol, endCol));
                }
            }
            catch ( _SMO.SmoException e )
            {
                if ( shouldSucceed )
                {
                    _NU.Assert.Fail(String.Format("Adding PERIOD has thrown an exception. Table: {0}, start column: {1}, end column: {2}\nException: {3}", t.Name, startCol, endCol, e.Message));
                }
            }
        }

        /// <summary>
        /// DROPs PERIOD for system time and validates if the operation's
        /// outcome was as expected
        /// </summary>
        private void AttemptDroppingPeriod(_SMO.Table t, bool shouldSucceed, string errorMessage)
        {
            try
            {
                t.DropPeriodForSystemTime();
                if ( !shouldSucceed )
                {
                    _NU.Assert.Fail(String.Format("Dropping PERIOD should not succeeded. Table: {0}", t.Name));
                }
            }
            catch ( _SMO.SmoException e )
            {
                if ( shouldSucceed )
                {
                    _NU.Assert.Fail(String.Format("Unexpected exception thrown when dropping PERIOD. Table: {0}\nException: {1}", t.Name, e.Message));
                }
            }
        }

        /// <summary>
        /// Helper method that validates the existance of a given table on a server
        /// and retrieves various temporal-specific properties if table is found
        /// </summary>
        private bool GetTableTemporalProperties(string database, string tablename, out bool isSystemVersioned, out int retentionPeriod, out _SMO.TemporalHistoryRetentionPeriodUnit retentionUnit)
        {
            string strTemporalType = "temporal_type";
            string strRetentionPeriod = "history_retention_period";
            string strRetentionPeriodUnitDesc = "history_retention_period_unit_desc";

            isSystemVersioned = false;
            retentionPeriod = 0;
            retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Undefined;

            bool retValue = false;

            SqlConnectionStringBuilder.InitialCatalog = database;

            using ( var conn = new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString) )
            {
                conn.Open();

                using ( var cmd = conn.CreateCommand() )
                {
                    cmd.CommandText = string.Format(CultureInfo.InvariantCulture, "SELECT * FROM SYS.TABLES WHERE NAME = '{0}'", Microsoft.SqlServer.Management.Sdk.Sfc.Urn.EscapeString(tablename));

                    using ( SqlDataReader reader = cmd.ExecuteReader() )
                    {
                        reader.Read();

                        if ( !reader.HasRows )
                        {
                            return false;
                        }
                        else
                        {
                            retValue = true;
                        }

                        if ( Int32.Parse(reader[strTemporalType].ToString()) == 2 )
                        {
                            isSystemVersioned = true;
                        }
                        else
                        {
                            isSystemVersioned = false;
                        }

                        // if 'period' is null, 'period unit' is null as well
                        // For temporal table, this means 'infinite' retention
                        // For non-temporal table, it means 'undefined' retention
                        //
                        if ( reader.IsDBNull(reader.GetOrdinal(strRetentionPeriod)) )
                        {
                            retentionPeriod = 0;

                            if ( isSystemVersioned )
                            {
                                retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Infinite;
                            }
                            else
                            {
                                retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Undefined;
                            }
                        }
                        else
                        {
                            retentionPeriod = Int32.Parse(reader[strRetentionPeriod].ToString());

                            switch ( reader[strRetentionPeriodUnitDesc].ToString().ToUpperInvariant() )
                            {
                                case "DAY":
                                    retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Day;
                                    break;
                                case "WEEK":
                                    retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Week;
                                    break;
                                case "MONTH":
                                    retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Month;
                                    break;
                                case "YEAR":
                                    retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Year;
                                    break;
                                case "INFINITE":
                                    retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Infinite;
                                    break;
                                case "UNDEFINED":
                                default:
                                    retentionUnit = _SMO.TemporalHistoryRetentionPeriodUnit.Undefined;
                                    break;
                            }
                        }
                    }
                }
            }

            return retValue;
        }

        private _SMO.Table CreateSimpleTemporalTable(_SMO.Database db)
        {
            string primaryKeyName = "PK_temporal_current_" + new System.Random().Next().ToString();
            string tableName = "CurrentTable_" + new System.Random().Next().ToString();
            _SMO.Table t = new _SMO.Table(db, tableName);

            _SMO.Column c1 = new _SMO.Column(t, "c1", _SMO.DataType.Int);
            _SMO.Column c2 = new _SMO.Column(t, "SysStart", _SMO.DataType.DateTime2(5));
            _SMO.Column c3 = new _SMO.Column(t, "SysEnd", _SMO.DataType.DateTime2(5));

            t.Columns.Add(c1);
            t.Columns.Add(c2);
            t.Columns.Add(c3);

            _SMO.Index index = new _SMO.Index(t, primaryKeyName);
            index.IndexKeyType = _SMO.IndexKeyType.DriPrimaryKey;

            index.IndexedColumns.Add(new _SMO.IndexedColumn(index, "c1"));
            t.Indexes.Add(index);

            c2.Nullable = false;
            c3.Nullable = false;
            c2.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowStart;
            c3.GeneratedAlwaysType = _SMO.GeneratedAlwaysType.AsRowEnd;

            t.AddPeriodForSystemTime(c2.Name, c3.Name, true);
            t.DataConsistencyCheck = false;
            t.IsSystemVersioned = true;

            t.Create();

            return t;
        }

        /// <summary>
        /// This test verifies that internal graph columns are skipped during scripting of graph tables.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void ScriptingGraphTables_skips_internal_graph_columns()
        {
            string queryOneMatch = string.Format(@"INSERT \[.*\]\.\[.*\] \(\[\$node_id.*\], \[c1\]\) VALUES \(.*, {0}\)", int.MaxValue);
            string queryTwoMatch = string.Format(@"INSERT \[.*\]\.\[.*\] \(\[\$node_id.*\], \[c1\]\) VALUES \(.*, {0}\)", int.MinValue);

            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                (database) =>
                {
                    ExecuteWithGraphTF(database, () =>
                    {
                        _SMO.Table table = CreateGraphTable(
                            database: database,
                            createTable: true,
                            isNode: true,
                            tableName: "node_table",
                            columnName: "c1");
                        string expectedNodeTableDdl = @"CREATE TABLE [dbo]." + table.Name.SqlBracketQuoteString() + @"(
	[c1] [int] NULL
)
AS NODE ON [PRIMARY]
";

                        string insertQuery1 = "INSERT INTO " + table.Name.SqlBracketQuoteString() + string.Format(" values({0})", int.MaxValue);
                        string insertQuery2 = "INSERT INTO " + table.Name.SqlBracketQuoteString() + string.Format(" values({0})", int.MinValue);

                        database.ExecuteNonQuery(insertQuery1);
                        database.ExecuteNonQuery(insertQuery2);

                        // Verify that internal columns are created post
                        // instantiation of the table object.
                        //
                        VerifySmoCollectionCount(table.Columns, 3, "The two graph internal columns and the user defined column should be present.");

                        _SMO.Scripter scripter = new _SMO.Scripter(database.Parent);
                        scripter.Options.ScriptData = true;
                        scripter.Options.WithDependencies = true;
                        scripter.Options.ScriptSchema = true;

                        IEnumerable<string> scripts = scripter.EnumScript(new Urn[] { table.Urn });
                        Assert.That(scripts, Has.Member(expectedNodeTableDdl.FixNewLines()), "Scripting of graph tables is expected to generate a valid DDL statement that skips over internal graph columns");

                        bool isRecordOneInserted = false;
                        bool isRecordTwoInserted = false;

                        foreach(string script in scripts)
                        {
                            if(script.StartsWith(string.Format("INSERT {0}", table.FullQualifiedName), ignoreCase:true, culture: CultureInfo.InvariantCulture))
                            {
                                if(Regex.IsMatch(script, queryOneMatch, RegexOptions.IgnoreCase))
                                {
                                    isRecordOneInserted = true;
                                }
                                else if (Regex.IsMatch(script, queryTwoMatch, RegexOptions.IgnoreCase))
                                {
                                    isRecordTwoInserted = true;
                                }
                                else
                                {
                                    Assert.Fail(string.Format("Unexpected data row found during script generation of graph tables that fails match with expected pattern, the data row generated was {0}", script));
                                }
                            }
                        }

                        Assert.True(isRecordOneInserted && isRecordTwoInserted, string.Format("The number of expected data rows in the node table were incorrect, contents of table DDLs are as follows: \n {0}", string.Join("\n", scripts)));

                        // Drop and re-create table object with generated script to double ensure its validity.
                        //
                        table.DropIfExists();
                        string tableCreateScript = (from string script in scripts where script.StartsWith("CREATE", StringComparison.InvariantCultureIgnoreCase) select script).First();

                        Assert.DoesNotThrow(() => database.ExecuteNonQuery(tableCreateScript), string.Format("The generated table DDL was invalid and failed to create the table, script generated was {0}", tableCreateScript));
                    });
                });
        }

        #endregion

        #endregion // Scripting Tests

        #region Sparse column tests

        /// <summary>
        /// we rely on the baselines to cover the default false case for HasSparseColumn
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void When_table_has_sparse_column_HasSparseColumn_is_true()
        {
            ExecuteWithDbDrop(
                db =>
                {
                    var table = new _SMO.Table(db, "myTable");
                    var sparseColumn = new _SMO.Column(table, "sparseColumn",
                    new _SMO.DataType(_SMO.SqlDataType.NVarChar, 10)) {IsSparse = true, Nullable = true};
                    var sparseColumn1 = new _SMO.Column(table, "sparseColumn1",
                    new _SMO.DataType(_SMO.SqlDataType.NVarChar, 10)) { IsSparse = true, Nullable = true };
                    var column = new _SMO.Column(table, "normalColumn", new _SMO.DataType(_SMO.SqlDataType.Int))
                    {
                        IsSparse = false,
                        Nullable = false,
                        Identity = true
                    };
                    table.Columns.Add(sparseColumn);
                    table.Columns.Add(sparseColumn1);
                    table.Columns.Add(column);
                    table.Create();
                    var index = new _SMO.Index(table, "sparseIndex") {IndexType = _SMO.IndexType.NonClusteredIndex};
                    var indexedColumn = new _SMO.IndexedColumn(index, "sparseColumn");
                    index.IndexedColumns.Add(indexedColumn);
                    index.Create();
                    _NU.Assert.That(table.HasSparseColumn, _NU.Is.True, "Table should have sparse column");
                    _NU.Assert.That(index.HasSparseColumn, _NU.Is.True, "Index should have sparse column");
                });
        }


        #endregion

        /// <summary>
        /// Regression test for TFS 11509818
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12, MaxMajor = 12)]
        public void SmoTableTruncateData_succeeds_on_Sql2014()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                database =>
                {
                    var table = database.CreateTable(TestContext.TestName);
                    table.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                    Assert.DoesNotThrow(() => table.TruncateData());
                    Assert.That(table.ExecutionManager.ConnectionContext.CapturedSql.Text.OfType<string>(), _NU.Is.EquivalentTo(new []
                    {
                        string.Format("USE [{0}]", SqlSmoObject.EscapeString(database.Name, ']')),
                        string.Format("TRUNCATE TABLE [{0}].[{1}]", SqlSmoObject.EscapeString(table.Schema, ']'), SqlSmoObject.EscapeString(table.Name, ']'))
                    }).IgnoreCase, "Incorrect SQL statements");
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    Assert.DoesNotThrow(() => table.TruncateData(1));
                    Assert.That(table.ExecutionManager.ConnectionContext.CapturedSql.Text.OfType<string>(), _NU.Is.EquivalentTo(new []
                    {
                        string.Format("USE [{0}]", SqlSmoObject.EscapeString(database.Name, ']')),
                        string.Format("TRUNCATE TABLE [{0}].[{1}] WITH (PARTITIONS (1))", SqlSmoObject.EscapeString(table.Schema, ']'), SqlSmoObject.EscapeString(table.Name, ']'))
                    }).IgnoreCase, "Incorrect SQL statements");
                });
        }

        /// <summary>
        /// Regression test for 12166765 - failure to run PostProcess if ServerConnection is pooled
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express)]
        public void SmoTable_RowCount_property_is_read_correctly_for_pooled_ServerConnection()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    var tableCreated = db.CreateTable("DropMe");
                    var ci = new SqlConnectionInfo
                    {
                        ServerName = this.SqlConnectionStringBuilder.DataSource,
                        UseIntegratedSecurity = this.SqlConnectionStringBuilder.IntegratedSecurity,
                        Pooled = true
                    };
                    if (!ci.UseIntegratedSecurity)
                    {
                        ci.UserName = this.SqlConnectionStringBuilder.UserID;
                        ci.Password = this.SqlConnectionStringBuilder.Password;
                    }
                    var smoServer = new _SMO.Server(new ServerConnection(ci));
                    var tableCollectionEntry = smoServer.Databases[db.Name].Tables[tableCreated.Name];
                    Assert.That(tableCollectionEntry.RowCount, _NU.Is.EqualTo(0), "RowCount");
                });
        }

        /// <summary>
        /// Sanity test for script data, which uses a database-scoped connection
        /// We can't directly test the AAD w/ MFA case with automation for TFS 12252953
        /// but make sure the non-MFA case succeeds
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoTable_ScriptData_succeeds()
        {
            ExecuteFromDbPool(
                this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    var scripter = new Scripter(db.Parent)
                    {
                        Options = {ScriptData = true, ScriptSchema = false, IncludeHeaders = false}
                    };
                    var table = new Table(db, "table1");
                    var column = new Column(table, "col1",
                        new DataType(SqlDataType.Int));
                    table.Columns.Add(column);
                    table.Create();
                    db.ExecuteNonQuery("insert into table1 values (1)");
                    db.ExecuteNonQuery("insert into table1 values (2)");
                    var script = scripter.EnumScript(new Urn[] {table.Urn});
                    Assert.That(script, _NU.Is.EquivalentTo(new string[]
                    {
                        "INSERT [dbo].[table1] ([col1]) VALUES (1)",
                        "INSERT [dbo].[table1] ([col1]) VALUES (2)",

                    }), "Generated script");
                });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void SmoTable_mixed_Ansi_padding_columns_script_correctly()
        {
            ExecuteFromDbPool(this.TestContext.FullyQualifiedTestClassName,
                db =>
                {
                    
                    db.ExecuteNonQuery(@"SET ANSI_PADDING ON
GO
CREATE TABLE mixedansipadding (colPaddingOn varbinary(20));
GO
SET ANSI_PADDING OFF
ALTER TABLE mixedansipadding ADD colPaddingOff varbinary(20);
GO
"); 
                    db.Tables.Refresh();
                    Assert.That(db.Tables.Cast<Table>().Select(t => t.Name), Has.Member("mixedansipadding"), "Create table with mixed ansi padding");                    
                    var table = db.Tables["mixedansipadding"];
                    try
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(table.Columns["colPaddingOn"].AnsiPaddingStatus, Is.True,
                                $"colPaddingOn.{nameof(Column.AnsiPaddingStatus)}");
                            Assert.That(table.Columns["colPaddingOff"].AnsiPaddingStatus, Is.False,
                                $"colPaddingOff.{nameof(Column.AnsiPaddingStatus)}");
                        });
                        var scripter = new Scripter(db.Parent)
                        {
                            Options = {AnsiPadding = true}
                        };

                        var script = scripter.EnumScript(new Urn[] {table.Urn}).ToArray();
                        Assert.That(script, _NU.Is.EqualTo(new string[]
                        {
                            "SET ANSI_NULLS ON",
                            "SET QUOTED_IDENTIFIER ON",
                            "SET ANSI_PADDING ON",
                            $"CREATE TABLE [dbo].[mixedansipadding]({Environment.NewLine}\t[colPaddingOn] [varbinary](20) NULL{Environment.NewLine}) ON [PRIMARY]{Environment.NewLine}SET ANSI_PADDING OFF{Environment.NewLine}ALTER TABLE [dbo].[mixedansipadding] ADD [colPaddingOff] [varbinary](20) NULL{Environment.NewLine}",
                            $"SET ANSI_PADDING {(db.AnsiPaddingEnabled ? Globals.On : Globals.Off)}",
                        }));
                    }
                    finally
                    {
                        table?.Drop();
                    }
                });
        }


        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void SmoTable_enable_and_disable_all_indexes()
        {
            ExecuteFromDbPool(db =>
            {
                var table = db.CreateTable("AllIndexes", new ColumnProperties("col1"), new ColumnProperties("col2"));
                var index1 = table.CreateIndex("index1", new IndexProperties() { IsClustered = false, IndexType = IndexType.NonClusteredIndex, Columns = new[] { table.Columns[0] } });
                var index2 = table.CreateIndex("index2", new IndexProperties() { IsClustered = false, IndexType = IndexType.NonClusteredIndex, Columns = new[] { table.Columns[1] } });
                Trace.TraceInformation("Disabling all indexes");
                table.DisableAllIndexes();
                index1.Refresh();
                index2.Refresh();
                Assert.Multiple(() =>
                {
                    Assert.That(index1.IsDisabled, Is.True, $"{nameof(index1)}.{nameof(index1.IsDisabled)} after {nameof(table.DisableAllIndexes)}");
                    Assert.That(index2.IsDisabled, Is.True, $"{nameof(index2)}.{nameof(index2.IsDisabled)} after {nameof(table.DisableAllIndexes)}");
                });
                Trace.TraceInformation("Rebuilding all indexes");
                db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;
                table.EnableAllIndexes(IndexEnableAction.Rebuild);
                var commands = db.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                index1.Refresh();
                index2.Refresh();
                Assert.Multiple(() =>
                {
                    Assert.That(commands, Has.Exactly(1).Contains($"ALTER INDEX ALL ON [dbo].{table.Name.SqlBracketQuoteString()} REBUILD"), $"{nameof(table.EnableAllIndexes)}({nameof(IndexEnableAction.Rebuild)})");
                    Assert.That(index1.IsDisabled, Is.False, $"{nameof(index1)}.{nameof(index1.IsDisabled)} after {nameof(IndexEnableAction.Rebuild)}");
                    Assert.That(index2.IsDisabled, Is.False, $"{nameof(index2)}.{nameof(index2.IsDisabled)} after {nameof(IndexEnableAction.Rebuild)}");
                });
                table.DisableAllIndexes();
                db.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                Trace.TraceInformation("Recreating all indexes");
                table.EnableAllIndexes(IndexEnableAction.Recreate);
                commands = db.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                index1.Refresh();
                index2.Refresh();
                Assert.Multiple(() =>
                {
                    Assert.That(commands, Has.Exactly(2).Contains($"CREATE NONCLUSTERED INDEX"), $"{nameof(table.EnableAllIndexes)}({nameof(IndexEnableAction.Recreate)})");
                    Assert.That(index1.IsDisabled, Is.False, $"{nameof(index1)}.{nameof(index1.IsDisabled)} after {nameof(IndexEnableAction.Recreate)}");
                    Assert.That(index2.IsDisabled, Is.False, $"{nameof(index2)}.{nameof(index2.IsDisabled)} after {nameof(IndexEnableAction.Recreate)}");
                });
            });
        }

        /// <summary>
        /// Creates tables with a clustered index, optionally including data in the new tables
        /// </summary>
        /// <param name="dataFileGroup"></param>
        /// <param name="indexFileGroup"></param>
        /// <param name="tableCount"></param>
        /// <param name="withData"></param>
        /// <returns></returns>
        public static string CreateLotsOfTables(string dataFileGroup, string indexFileGroup, int tableCount, bool withData) => $@" declare @i int;
declare @tsql nvarchar(max);
declare @tblname sysname;
set nocount on;
set @i = 0
while @i < {tableCount}
begin
  set @tblname = N'lotsoftables_' + format(@i, 'D6')
  set @tsql = 'create table ' + @tblname + '( 
    [t_cpac] [varchar](2) NOT NULL,
	[t_cmod] [varchar](3) NOT NULL,
	[t_cses] [varchar](8) NOT NULL,
	[t_srno] [smallint] NOT NULL,
	[t_ffnm] [varchar](30) NOT NULL,
	[t_tfnm] [varchar](30) NOT NULL,
	[t_outp] [tinyint] NOT NULL,
	[t_boin] [varchar](60) NOT NULL,
	[t_ffds] [varchar](30) NOT NULL,
	[t_tbfl] [tinyint] NOT NULL,
	[t_crit] [tinyint] NOT NULL,
	[t_ftyp] [tinyint] NOT NULL,
	[t_ddnm] [varchar](30) NOT NULL,
	[t_Refcntd] [int] NOT NULL,
	[t_Refcntu] [int] NOT NULL,
 CONSTRAINT [' + @tblname + '_1a] PRIMARY KEY CLUSTERED 
(
	[t_cpac] ASC,
	[t_cmod] ASC,
	[t_cses] ASC,
	[t_srno] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [{dataFileGroup}]
) ON [{dataFileGroup}]'
  execute (@tsql)
"
            + (withData ? @"
  set @tsql = 'insert into ' + @tblname + ' values (''a'', ''b'', ''c'', 1, ''d'', ''e'', 1, ''f'', ''g'', 1, 0, 1, ''h'', 100, 101)'
  execute (@tsql)" : string.Empty) + $@"
  set @tsql = 'CREATE UNIQUE NONCLUSTERED INDEX [' + @tblname + '_2a] ON [dbo].[' + @tblname + ']
    (
	    [t_cpac] ASC,
	    [t_cmod] ASC,
	    [t_srno] ASC,
	    [t_ftyp] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [{indexFileGroup}]'
  execute (@tsql)
  set @i += 1
end
set nocount off;
";
        /// <summary>
        /// Creates a new database with a given number of tables, optionally with data in each table.
        /// </summary>
        /// <param name="serverContext"></param>
        /// <param name="tableCount"></param>
        /// <param name="withData"></param>
        /// <returns></returns>
        public static Database CreateDbWithLotsOfTables(_SMO.Server serverContext, int tableCount = 20000, bool withData = false)
        {
            var timeout = serverContext.ConnectionContext.StatementTimeout;
            serverContext.ConnectionContext.StatementTimeout = 900;
            var db = serverContext.CreateDatabaseDefinition("lotsoftables");
            var fileGroupName = "PRIMARY";
            var indexFileGroupName = "PRIMARY";
            if (serverContext.DatabaseEngineType == DatabaseEngineType.Standalone)
            {
                var filePath = serverContext.DefaultFile;
                // shorten the name a bit
                var fileName = db.Name.Replace("'", "").Replace("]", "");
                var filegroup = new FileGroup(db, "AE_DATA");
                var dataFile = new DataFile(filegroup, "Data_1", Path.Combine(filePath, $"{fileName}_data1.ndf")) { IsPrimaryFile = false, GrowthType = FileGrowthType.KB, Growth = 100 };
                filegroup.Files.Add(dataFile);
                db.FileGroups.Add(filegroup);
                filegroup = new FileGroup(db, "AE_INDEX");
                dataFile = new DataFile(filegroup, "Index_1", Path.Combine(filePath, $"{fileName}_index1.ndf")) { IsPrimaryFile = false, GrowthType = FileGrowthType.KB, Growth = 100 };
                filegroup.Files.Add(dataFile);
                db.FileGroups.Add(filegroup);
                filegroup = new FileGroup(db, "PRIMARY");
                dataFile = new DataFile(filegroup, "primary_1", Path.Combine(filePath, $"{fileName}_data.mdf")) { IsPrimaryFile = true, GrowthType = FileGrowthType.KB, Growth = 1000 };
                filegroup.Files.Add(dataFile);
                db.FileGroups.Add(filegroup);
                fileGroupName = "AE_DATA";
                indexFileGroupName = "AE_INDEX";
            }
            db.Create();
            db.ExecuteNonQuery(CreateLotsOfTables(fileGroupName, indexFileGroupName, tableCount, withData));
            serverContext.ConnectionContext.StatementTimeout = timeout;
            return db;
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, HostPlatform = HostPlatformNames.Windows, Edition = DatabaseEngineEdition.Enterprise, MinMajor = 11)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void SmoTable_enumerating_twenty_thousand_tables_with_scripting_properties_runs_quickly()
        {
            ExecuteTest(() =>
            {
                Database db = null;
                try
                {
                    db = CreateDbWithLotsOfTables(ServerContext);

                    var rowTimeout = db.DatabaseEngineType == DatabaseEngineType.Standalone ? 10 : 60;
                    var queryGoalSeconds = db.DatabaseEngineType == DatabaseEngineType.Standalone ? 12 : 80;

                    var fields = Table.GetScriptFields(typeof(Database), db.Parent.ServerVersion, db.Parent.DatabaseEngineType, db.Parent.DatabaseEngineEdition, true);
                    db.ExecutionManager.ConnectionContext.StatementTimeout = rowTimeout;
                    var stopwatch = new Stopwatch();
                    Trace.TraceInformation("Fetching Tables collection");
                    db.ExecutionManager.ConnectionContext.SqlConnectionObject.StatisticsEnabled = true;
                    stopwatch.Start();
                    Assert.DoesNotThrow(() => db.Tables.ClearAndInitialize("", fields));
                    stopwatch.Stop();
                    var statistics = db.ExecutionManager.ConnectionContext.SqlConnectionObject.RetrieveStatistics();
                    Trace.TraceInformation($"Tables population took {stopwatch.ElapsedMilliseconds} ms");
                    try
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(queryGoalSeconds * 1000), $"20k tables should query in {queryGoalSeconds} seconds");
                            Assert.That(db.Tables.Count, Is.AtLeast(20000), "Should have at least 20000 tables");
                        });
                    }
                    finally
                    {
                        foreach (var statisticKey in statistics.Keys)
                        {
                            Trace.TraceInformation($"{statisticKey,-20}: {statistics[statisticKey],10}");
                        }
                    }
                }
                finally
                {
                    db?.Parent.DropKillDatabaseNoThrow(db.Name);
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionCreateTableOption()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table1 = new Table(db, TestContext.TestName) { DataRetentionEnabled = true, DataRetentionFilterColumnName = colName, DataRetentionPeriod = 5, DataRetentionPeriodUnit = DataRetentionPeriodUnit.Month };
                    table1.Columns.Add(new Column(table1, colName, DataType.DateTime2(7)));
                    table1.Create();
                    table1.Refresh();

                    Assert.That(table1.DataRetentionPeriod, Is.EqualTo(5), "Data retention should be set to 5");
                    Assert.That(table1.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Month), "Data retention period should be enabled");
                    Assert.That(table1.DataRetentionFilterColumnName, Is.EqualTo(colName), "Data retention filter column should be set");
                    Assert.That(table1.DataRetentionEnabled, Is.True, "Data retention enabled shoould remain set");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionCreateTableOptionScript()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table1 = new Table(db, TestContext.TestName) { DataRetentionEnabled = true, DataRetentionFilterColumnName = colName, DataRetentionPeriod = 5, DataRetentionPeriodUnit = DataRetentionPeriodUnit.Month };
                    table1.Columns.Add(new Column(table1, colName, DataType.DateTime2(7)));
                    table1.Create();
                    table1.Refresh();

                    var scriptMaker = new ScriptMaker(db.GetServerObject());
                    scriptMaker.Preferences.ScriptForCreateDrop = true;

                    StringCollection createScript = scriptMaker.Script(new SqlSmoObject[] { table1 });

                    var retentionUnit = new DataRetentionPeriodUnitTypeConverter().ConvertToInvariantString(DataRetentionPeriodUnit.Month);
                    var verifyScript = $"DATA_DELETION = ON ( FILTER_COLUMN = {SqlSmoObject.MakeSqlBraket(table1.DataRetentionFilterColumnName)}, RETENTION_PERIOD = {table1.DataRetentionPeriod} {retentionUnit} )";
                    Assert.That(createScript, Has.One.Contains(verifyScript), $"The script generated must match {verifyScript}");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionAlterTableOptionScript()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table1 = new Table(db, TestContext.TestName);
                    table1.Columns.Add(new Column(table1, colName, DataType.DateTime2(7)));
                    table1.Create();
                    table1.Refresh();

                    table1.DataRetentionEnabled = true;
                    table1.DataRetentionFilterColumnName = colName;
                    table1.DataRetentionPeriod = 5;
                    table1.DataRetentionPeriodUnit = DataRetentionPeriodUnit.Month;

                    var scriptMaker = new ScriptMaker(db.GetServerObject());
                    scriptMaker.Preferences.ScriptForAlter = true;

                    StringCollection alterScript = scriptMaker.Script(new SqlSmoObject[] { table1 });

                    var retentionUnit = new DataRetentionPeriodUnitTypeConverter().ConvertToInvariantString(DataRetentionPeriodUnit.Month);
                    var verifyScript = $"DATA_DELETION = ON ( FILTER_COLUMN = {SqlSmoObject.MakeSqlBraket(table1.DataRetentionFilterColumnName)}, RETENTION_PERIOD = {table1.DataRetentionPeriod} {retentionUnit} )";
                    Assert.That(alterScript, Has.One.Contains(verifyScript), $"The script generated must match {verifyScript}");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionCreateTableOptionFailNoFilterColumn()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table1 = new Table(db, TestContext.TestName) { DataRetentionEnabled = true, DataRetentionPeriod = 5, DataRetentionPeriodUnit = DataRetentionPeriodUnit.Month };
                    table1.Columns.Add(new Column(table1, colName, DataType.DateTime2(7)));
                    Assert.Throws<FailedOperationException>(table1.Create,"If a table does not have a filter column. Data Retention cannot be turned on");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionCreateTableOptionFailNoDataRetentionPeriod()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table1 = new Table(db, TestContext.TestName) { DataRetentionEnabled = true, DataRetentionFilterColumnName = colName, DataRetentionPeriodUnit = DataRetentionPeriodUnit.Month };
                    table1.Columns.Add(new Column(table1, colName, DataType.DateTime2(7)));
                    Assert.Throws<FailedOperationException>(table1.Create, "A data retention period should be set if the DataRetentionPeriodUnit is not infinite.");
                });
        }


        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionCreateTableOptionFailNoDataRetentionUnit()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table1 = new Table(db, TestContext.TestName) { DataRetentionEnabled = true, DataRetentionFilterColumnName = colName, DataRetentionPeriod = 1 };
                    table1.Columns.Add(new Column(table1, colName, DataType.DateTime2(7)));
                    Assert.Throws<FailedOperationException>(table1.Create,"A data retention period unit should be set always.");
                });
        }


        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionCreateTableOptionDataRetentionPeriodUnitInfinite()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table = new Table(db, TestContext.TestName) { DataRetentionEnabled = true, DataRetentionFilterColumnName = colName, DataRetentionPeriodUnit = DataRetentionPeriodUnit.Infinite };
                    table.Columns.Add(new Column(table, colName, DataType.DateTime2(7)));
                    table.Create();
                    table.Refresh();

                    Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Infinite), "Data retention period should be enabled");
                    Assert.That(table.DataRetentionFilterColumnName, Is.EqualTo(colName), "Data retention filter column should be set");
                    Assert.That(table.DataRetentionEnabled, Is.True, "Data retention enabled shoould remain set");

                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionTableOptionFromAlreadyCreatedTableWithDifferentRetentionPeriodUnit()
        {
            ExecuteWithDbDrop("DatabaseRetentionAlreadyCreated",
                db =>
                {
                    var scriptHelpers = new ScriptHelpers(Manageability.Utils.ConnectionHelpers.GetAzureKeyVaultHelper());
                    scriptHelpers.LoadAndRunScriptResource("DbSetup_SqlDatabaseEdge.sql", db, Assembly.GetExecutingAssembly());
                    db.Tables.Refresh();

                    foreach (Table table in db.Tables)
                    {
                        
                        Assert.That(table.DataRetentionEnabled, Is.True, "Data retention enabled should remain set");
                        Assert.That(table.DataRetentionFilterColumnName, Is.EqualTo("dbdatetime2"), "Data retention filter column should be set");
                        if (table.Name.Contains("Day"))
                        {
                            Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Day), "Data retention period unit should have a valid unit of week");
                            Assert.That(table.DataRetentionPeriod, Is.EqualTo(1), "Data Retention period should have a value of 1");
                        }
                        else if (table.Name.Contains("Week"))
                        {
                            Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Week), "Data retention period unit should have a valid unit of week");
                            Assert.That(table.DataRetentionPeriod, Is.EqualTo(1), "Data Retention period should have a value of 1");
                        }
                        else if (table.Name.Contains("Month"))
                        {
                            Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Month), "Data retention period unit should have a valid unit of Month");
                            Assert.That(table.DataRetentionPeriod, Is.EqualTo(1), "Data Retention period should have a value of 1");
                        }
                        else if (table.Name.Contains("Year"))
                        {
                            Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Year), "Data retention period unit should have a valid unit of week");
                            Assert.That(table.DataRetentionPeriod, Is.EqualTo(1), "Data Retention period should have a value of 1");
                        }
                        else if (table.Name.Contains("Infinite"))
                        {
                            Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Infinite), "Data retention period unit should be infinte");
                        }
                    }
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionAlterTableOption()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table = db.CreateTable(TestContext.TestName, new ColumnProperties(colName, DataType.DateTime2(7)));

                    // Turn on Data Retention
                    //
                    table.DataRetentionFilterColumnName = colName;
                    table.DataRetentionPeriod = 2;
                    table.DataRetentionPeriodUnit = DataRetentionPeriodUnit.Day;
                    table.DataRetentionEnabled = true;
                    table.Alter();
                    table.Refresh();

                    // Verify Changes
                    //
                    Assert.That(table.DataRetentionPeriod, Is.EqualTo(2), "Data retention should be set to 2");
                    Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Day), "Data retention period should be enabled");
                    Assert.That(table.DataRetentionFilterColumnName, Is.EqualTo(colName), "Data retention filter column should be set");
                    Assert.That(table.DataRetentionEnabled, Is.True, "Data retention enabled shoould remain set");
                    
                    string expected = (string)db.ExecutionManager.ExecuteScalar($"select name from sys.tables where data_retention_period = 2 and data_retention_period_unit = 3 and name = '{table.Name.SqlEscapeSingleQuote()}'");
                    Assert.That(table.Name, Is.EqualTo(expected), "Data Retention values should be updated on the server");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionTableOptionNotScriptedWhenDataRetentionPropertiesAreNotDirty()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table = db.CreateTable(TestContext.TestName, new ColumnProperties(colName, DataType.DateTime2(7)));
                    table.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                    table.Create();
                    table.Refresh();

                    var createScript = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(createScript, Has.None.Contain("DATA_DELETION"), "Data Retention parameters should not be present if none of its properties has been set by user");
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();

                    table.Alter();
                    table.Refresh();
                    var alterScript = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(alterScript, Has.None.Contain("DATA_DELETION = OFF"), "Data Retention parameters should not be present if none of its properties has been set by user");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabaseEdge)]
        public void DataRetentionAlterTableOptionOffCheckRefreshValues()
        {
            ExecuteFromDbPool(
                db =>
                {
                    string colName = SmoObjectHelpers.GenerateUniqueObjectName(TestContext.TestName);
                    var table = db.CreateTable(TestContext.TestName, new ColumnProperties(colName, DataType.DateTime2(7)));

                    // Turn on Data Retention
                    //
                    table.DataRetentionFilterColumnName = colName;
                    table.DataRetentionPeriod = 2;
                    table.DataRetentionPeriodUnit = DataRetentionPeriodUnit.Day;
                    table.DataRetentionEnabled = true;
                    table.Alter();
                    table.Refresh();

                    // Verify Changes
                    //
                    Assert.That(table.DataRetentionPeriod, Is.EqualTo(2), "Data retention should be set to 2");
                    Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Day), "Data retention period should be enabled");
                    Assert.That(table.DataRetentionFilterColumnName, Is.EqualTo(colName), "Data retention filter column should be set");
                    Assert.That(table.DataRetentionEnabled, Is.True, "Data retention enabled shoould remain set");

                    
                    string expected = (string)db.ExecutionManager.ExecuteScalar($"select name from sys.tables where data_retention_period = 2 and data_retention_period_unit = 3 and name = '{table.Name.SqlEscapeSingleQuote()}'");
                    Assert.That(table.Name, Is.EqualTo(expected), "Data Retention values should be updated on the server");

                    // Turn off Data Retention
                    //
                    table.DataRetentionEnabled = false;
                    table.Alter();
                    table.Refresh();

                    Assert.That(table.DataRetentionPeriod, Is.EqualTo(-1), "Data retention period should be reset to its default value of -1");
                    Assert.That(table.DataRetentionPeriodUnit, Is.EqualTo(DataRetentionPeriodUnit.Infinite), "Data retention period unit should be reset to its default unit value of infinite");
                    Assert.That(table.DataRetentionFilterColumnName, Is.EqualTo(string.Empty), "Data retention filter column remains unchanged");
                    Assert.That(table.DataRetentionEnabled, Is.False, "This property was set to false and should remain false");
                });
        }

        /// <summary>
        /// Regression test for https://github.com/microsoft/sqlmanagementobjects/issues/52
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void SmoTable_IsSystemObject_is_true_when_extendedproperty_is_set()
        {
            ExecuteFromDbPool(db =>
            {
                var table = db.CreateTable("ToolsSupport");
                Assert.That(table.IsSystemObject, Is.False, "Table.IsSystemObject without microsoft_database_tools_support property");
                var ep = new ExtendedProperty(table, "microsoft_database_tools_support", 1);
                ep.Create();
                table.Refresh();
                Assert.That(table.IsSystemObject, Is.True, "Table.IsSystemObject with microsoft_database_tools_support property");
            });
        }

        /// <summary>
        /// Switching partitions  has 3 modes
        /// Reassigns all data of a table as a partition to an already-existing partitioned table.
        /// Switches a partition from one partitioned table to another.
        /// Reassigns all data in one partition of a partitioned table to an existing non-partitioned table.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void SmoTable_SwitchPartition_supports_all_variations()
        {
            ExecuteFromDbPool(db =>
            {
                var prefix = $"SwitchPartition{new Random().Next()}";
                var partitionScheme = db.CreatePartitionSchemeWithFileGroups(prefix,
                    new object[] { "20200201", "20200301", "20200401" }, DataType.DateTime);
                var partitionedTable1 = new Table(db, "partitionedTable1")
                {
                    PartitionScheme = partitionScheme.Name
                };
                partitionedTable1.Columns.Add(new Column(partitionedTable1, "ItemDate", DataType.DateTime));
                partitionedTable1.Columns.Add(new Column(partitionedTable1, "ItemName", DataType.SysName));
                partitionedTable1.PartitionSchemeParameters.Add(new PartitionSchemeParameter() { Name = "ItemDate" });
                partitionedTable1.Create();

                var partitionedTable2 = new Table(db, "partitionedTable2")
                {
                    PartitionScheme = partitionScheme.Name
                };
                partitionedTable2.Columns.Add(new Column(partitionedTable2, "ItemDate", DataType.DateTime));
                partitionedTable2.Columns.Add(new Column(partitionedTable2, "ItemName", DataType.SysName));
                partitionedTable2.PartitionSchemeParameters.Add(new PartitionSchemeParameter() { Name = "ItemDate" });                
                partitionedTable2.Create();
                var table = new Table(db, "UnpartitionedTable");
                table.Columns.Add(new Column(table, "ItemDate", DataType.DateTime));
                table.Columns.Add(new Column(table, "ItemName", DataType.SysName));
                table.Checks.Add(new Check(table, "checkit") { Text = "ItemDate <= '20200201'" });
                if (db.DatabaseEngineEdition != DatabaseEngineEdition.SqlDatabase)
                {
                    table.FileGroup = $"{prefix}_FG0";
                }
                table.Create();

                var table2 = new Table(db, "UnpartitionedTable2");
                table2.Columns.Add(new Column(table2, "ItemDate", DataType.DateTime));
                table2.Columns.Add(new Column(table2, "ItemName", DataType.SysName));
                table2.Checks.Add(new Check(table2, "checkit2") { Text = "ItemDate <= '20200201'" });
                if (db.DatabaseEngineEdition != DatabaseEngineEdition.SqlDatabase)
                {
                    table2.FileGroup = $"{prefix}_FG0";
                }
                table2.Create();

                db.ExecuteNonQuery(@"INSERT INTO partitionedTable1(ItemDate, ItemName) VALUES
                                      ('20200110', 'Partition1January'),
                                      ('20200215', 'Partition1February'),
                                      ('20200320', 'Partition1March'),
                                      ('20200402', 'Partition1April')");
                db.ExecuteNonQuery(@"INSERT INTO partitionedTable2(ItemDate, ItemName) VALUES
                                      ('20200110', 'Partition2January'),
                                      ('20200215', 'Partition2February'),
                                      ('20200320', 'Partition2March'),
                                      ('20200402', 'Partition2April')");

                Assert.That(() => table.SwitchPartition(null), Throws.InstanceOf<ArgumentNullException>(), "SwitchPartition(null)");
                partitionedTable1.SwitchPartition(1, table);
                var itemName = db.ExecutionManager.ExecuteScalar("select top 1 ItemName from UnpartitionedTable");
                Assert.That(itemName, Is.EqualTo("Partition1January"), "SwitchPartition of partitioned table into unpartitioned table");
                partitionedTable2.SwitchPartition(1, partitionedTable1, 1);
                itemName = db.ExecutionManager.ExecuteScalar("select top 1 ItemName from partitionedTable1 where ItemDate <= '20200201'");
                Assert.That(itemName, Is.EqualTo("Partition2January"), "SwitchPartition of partitioned table into partitioned table");
                if (db.ServerVersion.Major >= 12)
                {
                    table.LowPriorityMaxDuration = 1;
                }
                table.SwitchPartition(partitionedTable2, 1);
                itemName = db.ExecutionManager.ExecuteScalar("select top 1 ItemName from partitionedTable2 where ItemDate <= '20200201'");
                Assert.That(itemName, Is.EqualTo("Partition1January"), "SwitchPartition of unpartitioned table to partitioned table");
                db.ExecuteNonQuery(@"INSERT INTO UnpartitionedTable(ItemDate, ItemName) VALUES
                                      ('20200110', 'January')");
                table.SwitchPartition(table2);
                itemName = db.ExecutionManager.ExecuteScalar("select top 1 ItemName from UnpartitionedTable2 where ItemDate <= '20200201'");
                Assert.That(itemName, Is.EqualTo("January"), "Switch unpartitioned table to unpartitioned table");
            });
        }


        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void SmoTable_DBCC_methods()
        {
            ExecuteFromDbPool(db =>
            {
                var columns = new[]
                    {
                        new ColumnProperties("c1") {Nullable = false, Identity = true},
                        new ColumnProperties("c2") {Nullable = false}
                    };
                var table = db.CreateTable("DBCC", columns);

                db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;
                table.RebuildIndexes(10);
                Assert.Multiple(() =>
                {
                    var script = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(script, Has.One.EqualTo($"DBCC DBREINDEX({SqlSmoObject.MakeSqlString(table.FullQualifiedName)}, N'', 10)"), nameof(table.RebuildIndexes));
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    table.RecalculateSpaceUsage();
                    script = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(script, Has.One.EqualTo($"DBCC UPDATEUSAGE(0, {SqlSmoObject.MakeSqlString(table.FullQualifiedName)}) WITH NO_INFOMSGS"), nameof(table.RecalculateSpaceUsage));
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    table.CheckIdentityValue();
                    script = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(script, Has.One.EqualTo($"DBCC CHECKIDENT({SqlSmoObject.MakeSqlString(table.FullQualifiedName)})"), nameof(table.CheckIdentityValue));
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    table.CheckTable();
                    script = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(script, Has.One.EqualTo($"DBCC CHECKTABLE ({SqlSmoObject.MakeSqlString(table.FullQualifiedName)}) WITH NO_INFOMSGS"), nameof(table.CheckTable));
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                    table.CheckTableDataOnly();
                    script = table.ExecutionManager.ConnectionContext.CapturedSql.Text;
                    Assert.That(script, Has.One.EqualTo($"DBCC CHECKTABLE ({SqlSmoObject.MakeSqlString(table.FullQualifiedName)}, NOINDEX)"), nameof(table.CheckTableDataOnly));
                    table.ExecutionManager.ConnectionContext.CapturedSql.Clear();
                });
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void SmoTable_DataTable_methods()
        {
            ExecuteFromDbPool(db =>
            {
                var columns = new[]
                    {
                        new ColumnProperties("c1") {Nullable = false},
                        new ColumnProperties("c2") {Nullable = false}
                    };
                var table = db.CreateTable("datatable", columns);
                table.CreateIndex(this.TestContext.TestName, new IndexProperties() { KeyType = _SMO.IndexKeyType.DriPrimaryKey });
                var fk = new ForeignKey(table, "fk");
                var fkc = new ForeignKeyColumn(fk, table.Columns[0].Name, table.Columns[0].Name);
                fk.Columns.Add(fkc);
                fk.ReferencedTable = table.Name;
                fk.ReferencedTableSchema = table.Schema;
                fk.Create();
                table.InsertDataToTable(100);
                var stat = new Statistic(table, "mystat1");
                stat.StatisticColumns.Add(new StatisticColumn(stat, "c1"));
                table.Statistics.Add(stat);
                stat.Create();
                stat = new Statistic(table, "mystat2");
                stat.StatisticColumns.Add(new StatisticColumn(stat, "c2"));
                table.Statistics.Add(stat);
                stat.Create();
                var dt = table.EnumForeignKeys();
                Assert.Multiple(() =>
                {
                    Assert.That(dt.Rows.Cast<System.Data.DataRow>().Select(r => r["Name"]), Is.EquivalentTo(new string[] { "fk" }), nameof(table.EnumForeignKeys));
                    dt = table.EnumLastStatisticsUpdates();
                    Assert.That(dt.Rows.Cast<System.Data.DataRow>().Select(r => r["Name"]), Is.SupersetOf(new string[] { "mystat1", "mystat2" }), nameof(table.EnumLastStatisticsUpdates) + "()");
                    dt = table.EnumLastStatisticsUpdates("mystat1");
                    Assert.That(dt.Rows.Cast<System.Data.DataRow>().Select(r => r["Name"]), Has.Member( "mystat1" ), nameof(table.EnumLastStatisticsUpdates) + "(mystat1)");
                    Assert.That(dt.Rows.Cast<System.Data.DataRow>().Select(r => r["Name"]), Has.No.Member("mystat2"), nameof(table.EnumLastStatisticsUpdates) + "(mystat1)");
                });
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void SmoTable_Rename()
        {
            ExecuteFromDbPool(db =>
            {
                var table = db.CreateTable("renameme");
                var oldname = table.Name;
                var newname = table.Name.Replace("renameme", "newname");
                table.Rename(newname);
                db.Tables.Refresh();
                var tables = db.Tables.Cast<Table>().Select(t => t.Name);
                Assert.Multiple(() =>
                {
                    Assert.That(tables, Has.No.Member(oldname), "Should have removed old name");
                    Assert.That(tables, Has.Member(newname), "Should have new name");
                    Assert.That(table.Name, Is.EqualTo(newname), "table.Name after Rename");

                });
            });
        }
    }
}
