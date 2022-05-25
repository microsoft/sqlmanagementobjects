// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Column properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Column_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Column column = (_SMO.Column)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.Columns.Refresh();
            Assert.That(table.Columns[column.Name], Is.Null,
                          "Current column not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a column with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Column_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                    _SMO.Column column = new _SMO.Column(table, "drop_col", new _SMO.DataType(_SMO.SqlDataType.Int));

                    VerifyIsSmoObjectDropped(column, table);
                });
        }

        /// <summary>
        /// This property is supported on DW but it takes forever to execute the test and doesn't add
        /// value.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void Column_with_decimal_identity_seed_can_be_scripted()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var tableName = $"table{Guid.NewGuid()}";
                    var table = new _SMO.Table(database, tableName);
                    table.Columns.Add(new _SMO.Column(table, "id") { Identity = true, IdentitySeedAsDecimal = -9223372036854775809m, IdentityIncrementAsDecimal = 1m, DataType = _SMO.DataType.Decimal(0, 21) });
                    table.Columns.Add(new _SMO.Column(table, "c", _SMO.DataType.Int));
                    table.Create();
                    database.Refresh();
                    database.Parent.SetDefaultInitFields(typeof(_SMO.Column), "IdentitySeedAsDecimal");
                    table = database.Tables[tableName];
                    Assert.That(table.Columns["id"].IdentitySeedAsDecimal, Is.EqualTo(-9223372036854775809m), "Incorrect IdentitySeedAsDecimal");
                    var scripter = new _SMO.Scripter(database.Parent);
                    var script = scripter.Script(table);
                    Assert.That(script, Has.Member($@"CREATE TABLE [dbo].[{tableName}](
	[id] [decimal](21, 0) IDENTITY(-9223372036854775809,1) NOT NULL,
	[c] [int] NULL
) ON [PRIMARY]
".FixNewLines()), "Incorrect generated script");
                    Assert.Throws<SqlException>(() => { Int64 x = table.Columns["id"].IdentitySeed; }, "IdentitySeed that overflows Int64 should throw");
                });
        }

        private static IEnumerable<ColumnProperties> GetAllDataTypeColumns()
        {
            var i = 0;
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.BigInt);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.HierarchyId);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Binary(1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Bit);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Char(1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.DateTime);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Decimal(1, 1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Numeric(1, 1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Float);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Geography);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Geometry);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Image);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Int);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Money);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.NChar(1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.NText);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.NVarChar(1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.NVarCharMax);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Real);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.SmallDateTime);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.SmallInt);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.SmallMoney);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Text);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Timestamp);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.TinyInt);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.UniqueIdentifier);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.VarBinary(8));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.VarBinaryMax);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.VarChar(2));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.VarCharMax);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Variant);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.SysName);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Date);
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.Time(1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.DateTimeOffset(1));
            yield return new ColumnProperties($"col{i++}", _SMO.DataType.DateTime2(1));
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse)]
        public void Column_script_all_data_types()
        {

            ExecuteFromDbPool(
                database =>
                {
                    var columns = GetAllDataTypeColumns().OrderBy(c => c.Name).ToArray();
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName, columns);
                    table.Refresh();
                    var createdColumns = table.Columns.Cast<_SMO.Column>()
                        .Select(c => new ColumnProperties(c.Name, c.DataType)).OrderBy(c => c.Name);
                    Assert.That(createdColumns,
                       Is.EquivalentTo(columns)
                                .Using<ColumnProperties, ColumnProperties>((a, b) => a.Name.Equals(b.Name) && a.SmoDataType.Equals(b.SmoDataType)),
                        "Created columns");
                });
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Column_Create_Masked_Column_And_Alter()
        {
            ExecuteFromDbPool(
                database =>
                {
                    var tableName = $"table{Guid.NewGuid()}";
                    var table = new _SMO.Table(database, tableName);
                    var columnA = "FirstName";
                    var columnB = "LastName";
                    var maskingFunction = @"partial(1, ""XXXXXXX"", 0)";
                    table.Columns.Add(new _SMO.Column(table, columnA) { DataType = _SMO.DataType.VarChar(100), IsMasked = true, MaskingFunction = maskingFunction });
                    table.Columns.Add(new _SMO.Column(table, columnB) { DataType = _SMO.DataType.VarChar(100) });
                    table.Create();
                    database.Refresh();
                    table = database.Tables[tableName];
                    // verify masked column properties
                    Assert.That(table.Columns[columnA].IsMasked, Is.True, "Create column; IsMasked should be set");
                    Assert.That(table.Columns[columnA].MaskingFunction, Is.EqualTo(maskingFunction), "Create column; Incorrect MaskingFunction");
                    Assert.That(table.Columns[columnB].IsMasked, Is.False, "Create column; IsMasked should be unset");
                    Assert.That(table.Columns[columnB].MaskingFunction, Is.Empty, "Create column; MaskingFunction should be empty");

                    // alter mask
                    table.Columns[columnA].IsMasked = false;
                    table.Columns[columnB].IsMasked = true;
                    table.Columns[columnB].MaskingFunction = maskingFunction;
                    table.Columns[columnA].Alter();
                    table.Columns[columnB].Alter();
                    table.Refresh();
                    Assert.That(table.Columns[columnA].IsMasked, Is.False, "Alter column; IsMasked should be unset");
                    Assert.That(table.Columns[columnB].IsMasked, Is.True, "Alter column; IsMasked should be set");
                    Assert.That(table.Columns[columnB].MaskingFunction, Is.EqualTo(maskingFunction), "Alter column; Incorrect MaskingFunction");

                });
        }

        /// <summary>
        /// Verifies that dropped columns on ledger tables are not scripted.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlManagedInstance)]
        public void DroppedLedgerColumn_is_not_scripted()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // create the table
                    var tableName = $"table{Guid.NewGuid()}";
                    var table = new Table(database, tableName);
                    table.Columns.Add(new Column(table, "kcol1") { DataType = DataType.Int });
                    table.Columns.Add(new Column(table, "kcol2") { DataType = DataType.Int });
                    table.IsSystemVersioned = false;
                    table.IsLedger = true;
                    table.LedgerType = LedgerTableType.AppendOnlyLedgerTable;
                    table.Create();
                    database.Refresh();
                    table = database.Tables[tableName];

                    // drop a ledger column
                    table.Columns["kcol2"].Drop();

                    table.Refresh();
                    table.Columns.Refresh();
                    Column droppedColumn = null;
                    
                    foreach (Column col in table.Columns)
                    {
                        if (col.Name.Contains("MSSQL_Dropped"))
                        {
                            droppedColumn = col;
                            continue;
                        }
                    }

                    // Verify dropped column property
                    Assert.That(droppedColumn.DroppedLedgerColumn(), Is.True, "Column is not marked as a Dropped Ledger Column");

                    // Verify create script does not include the dropped column
                    var scripter = new Scripter(database.Parent);
                    var script = scripter.Script(table);
                    Assert.That(script, !Has.Member($@"kcol2".FixNewLines()), "Incorrect generated script");
                });
        }

        #endregion // Scripting Tests

        #region bind tests
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void Column_BindDefault_binds_default_value()
        {
            ExecuteFromDbPool(db =>
            {
                var table = db.CreateTable("bindDefault", new ColumnProperties("col1", DataType.Int));
                var dbDefault = new Default(db, "default" + Guid.NewGuid().ToString())
                {
                    TextBody = "1",
                };
                dbDefault.TextHeader = $"CREATE DEFAULT {dbDefault.Name.SqlBracketQuoteString()} AS";
                dbDefault.Create();
                table.Columns["col1"].BindDefault(dbDefault.Schema, dbDefault.Name);
                table.Columns["col1"].Refresh();
                Assert.That(table.Columns["col1"].Default, Is.EqualTo(dbDefault.Name), "Column.Default after BindDefault");
                table.Columns["col1"].UnbindDefault();
                table.Columns["col1"].Refresh();
                Assert.That(table.Columns["col1"].Default, Is.Null.Or.Empty, "Column.Default after UnbindDefault");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand)]
        public void Column_BindRule_binds_rule()
        {
            ExecuteFromDbPool(db =>
            {
                var table = db.CreateTable("bindDefault", new ColumnProperties("col1", DataType.Int));
                var view = db.CreateView("view", "dbo", "select 'view' as col1");
                var udtt = new UserDefinedTableType(db, GenerateSmoObjectName("udtt"));
                udtt.Columns.Add(new Column(udtt, "testCol", _SMO.DataType.Int));
                udtt.Create();
                var rule = new Rule(db, "myRule")
                {
                    TextHeader = "CREATE RULE [myRule] AS",
                    TextBody = "@value BETWEEN GETDATE() AND DATEADD(year, 4, GETDATE())"
                };
                rule.Create();
                var column = view.Columns[0];
                Assert.Multiple(() =>
                {
                    
                    Assert.That(() => column.BindRule(rule.Schema, rule.Name), Throws.InstanceOf<FailedOperationException>(), "BindRule on a View column");
                    Assert.That(column.UnbindRule, Throws.InstanceOf<FailedOperationException>(), "UnbindRule on a View column");
                    column = udtt.Columns[0];
                    Assert.That(() => column.BindRule(rule.Schema, rule.Name), Throws.InstanceOf<FailedOperationException>(), "BindRule on a UDTT column");
                    Assert.That(column.UnbindRule, Throws.InstanceOf<FailedOperationException>(), "UnbindRule on a UDTT column");
                    column = table.Columns[0];
                    Assert.That(() => column.BindRule(null, "myRule"), Throws.ArgumentNullException, "null rule schema");
                    Assert.That(() => column.BindRule("", null), Throws.ArgumentNullException, "null rule name");
                    Assert.That(() => column.BindRule("dbo", ""), Throws.ArgumentException, "empty rule name");
                });
                
                column.BindRule(rule.Schema, rule.Name);
                column.Refresh();
                Assert.That(column.Rule, Is.EqualTo(rule.Name), "BindRule should take effect");
                column.UnbindRule();
                column.Refresh();
                Assert.That(column.Rule, Is.Null.Or.Empty, "UnbindRule should remove the rule");
            });
        }
        #endregion
    }
}
