// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
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
    /// Test suite for testing DefaultConstraint properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DefaultConstraint_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            _SMO.DefaultConstraint def = (_SMO.DefaultConstraint)obj;
            _SMO.Column column = def.Parent;

            column.Create();
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Column column = (_SMO.Column)objVerify;

            column.Refresh();
            Assert.IsNull(column.DefaultConstraint,
                          "Current default constraint not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a default constraint with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_DefaultConstraint_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                    _SMO.Column column = new _SMO.Column(table, GenerateSmoObjectName("col"),
                                                         new _SMO.DataType(_SMO.SqlDataType.Int));

                    _SMO.DefaultConstraint def = column.AddDefaultConstraint("def_" + (this.TestContext.TestName ?? ""));
                    def.Text = "0";

                    const string defaultConstraintScriptDropIfExistsTemplate = "ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}";
                    string defaultConstraintScriptDropIfExists = string.Format(defaultConstraintScriptDropIfExistsTemplate,
                        table.FormatFullNameForScripting(new _SMO.ScriptingPreferences()),
                        //The object is schema-qualified but the script doesn't generate the schema so we have to specifically exclude the schema
                        def.FormatFullNameForScripting(new _SMO.ScriptingPreferences() {IncludeScripts = {SchemaQualify = false}}));

                    VerifySmoObjectDropIfExists(def, column, defaultConstraintScriptDropIfExists);
                });
        }

        /// <summary>
        /// Regression test for Defect 10023665:sql server smo generating inline defaultconstraint when adding a column to a table containing data
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 10)]
        public void When_Table_has_rows_add_column_with_default_constraint_succeeds()
        {
            ExecuteFromDbPool(database =>
            {
                // First create a table with a default constraint column to make sure the fix didn't break that
                var tbl = new _SMO.Table(database, GenerateSmoObjectName("DefCon"));
                var column = new _SMO.Column(tbl, "col1", _SMO.DataType.Int) { Nullable = false };
                var constraint = column.AddDefaultConstraint();
                constraint.Text = "5";
                tbl.Columns.Add(column);
                Assert.DoesNotThrow(() => tbl.Create(), "Failed to create table with default constraint column");
                database.ExecuteNonQuery(string.Format("USE [{0}] insert into [{1}] ([col1]) values (2)", _SMO.SqlSmoObject.EscapeString(database.Name, ']'), _SMO.SqlSmoObject.EscapeString(tbl.Name, ']')) );
                tbl.Refresh();
                column = new _SMO.Column(tbl, "col2", _SMO.DataType.Int) {Nullable = false};
                constraint = column.AddDefaultConstraint();
                constraint.Text = "1";
                tbl.Columns.Add(column);
                Assert.DoesNotThrow(() =>
                {
                    try
                    {
                        tbl.Alter();
                    }
                    catch (Exception e)
                    {
                        while (e.InnerException != null)
                        {
                            e = e.InnerException;
                        }
                        Trace.TraceError("Innermost exception: {0}", e);
                        throw;
                    }
                }, "Alter table should succeed if new column has default constraint");
                tbl.Refresh();
                Assert.That(tbl.Columns.OfType<_SMO.Column>().Select(col => col.Name), Has.Member("col2"), "New column not added by Alter");
                Assert.That(tbl.Columns["col1"].DefaultConstraint.Text, Is.EqualTo("((5))"), "col1 has wrong DefaultConstraint");
                Assert.That(tbl.Columns["col2"].DefaultConstraint.Text, Is.EqualTo("((1))"), "New column col2 has wrong DefaultConstraint");
            });
        }

        #endregion // Scripting Tests
    }
}
