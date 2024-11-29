// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Test.SMO.ScriptingTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.RegressionTests
{


    /// <summary>
    /// Tests that cover regressed scenarios (which don't fit under other test categories)
    /// </summary>
    //##[TestSuite(LabRunCategory.Full, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class RegressionTests : SmoTestBase
    {

        /// <summary>
        /// Verifies we can traverse the dependencies of a table with an applied security policy without error.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void TFS4531584_UnableToViewDependenciesOnFilteredTable()
        {
            const String SecPolName = "secPol1";
            const String FunctionName = "f1";
            const String SchemaName = "rls";
            const String tableName = "t1";
            const String columnName = "testColumn1";
            const String PredicateDefinitionFormat = "[{0}].[{1}]([{2}])";
            const String FunctionTextHeaderFormat = "CREATE FUNCTION {0}.{1} (@x INT) RETURNS TABLE WITH SCHEMABINDING AS";
            const String FunctionTextBody = "return select 1 as is_visible";

            this.ExecuteWithDbDrop(
                database =>
                {
                    // Step 1. Create the test schema, a UDF to be used as a predicate, and a table.
                    //

                    var sch = new _SMO.Schema(database, SchemaName);
                    sch.Create();
                    var function = new _SMO.UserDefinedFunction(database, FunctionName, SchemaName);
                    function.TextHeader = String.Format(FunctionTextHeaderFormat, SchemaName, FunctionName);
                    function.TextBody = FunctionTextBody;
                    function.Create();

                    var tab = new _SMO.Table(database, tableName, SchemaName);
                    var col = new _SMO.Column(tab, columnName, new _SMO.DataType(_SMO.SqlDataType.Int));
                    tab.Columns.Add(col);
                    tab.Create();

                    database.Tables.Refresh();
                    Assert.IsNotNull(database.Tables[tableName, SchemaName]);

                    // Create a security policy with a simple predicate.
                    //
                    var secPol = new _SMO.SecurityPolicy(database, SecPolName, SchemaName, true
                        /* not for replication */, true /* is enabled */);
                    var predicate = new _SMO.SecurityPredicate(secPol,
                        database.Tables[tableName, SchemaName],
                        String.Format(PredicateDefinitionFormat, SchemaName, FunctionName, columnName));
                    secPol.SecurityPredicates.Add(predicate);
                    secPol.Create();
                    database.SecurityPolicies.Refresh();
                    secPol = database.SecurityPolicies[SecPolName, SchemaName];
                    Assert.IsNotNull(secPol, "No security policy object was created.");

                    // Find the dependencies of the table.
                    //
                    var depWalker = new _SMO.DependencyWalker(database.Parent);
                    depWalker.DiscoveryProgress +=
                        new _SMO.ProgressReportEventHandler((object sender, _SMO.ProgressReportEventArgs args) =>
                        {
                            if (args.Current.Type.Equals("Table"))
                            {
                                tab.Refresh();
                                var depTable = (_SMO.Table)database.Parent.GetSmoObject(args.Current);
                                Assert.AreEqual(tab, depTable, "Table is not equal to the table created above.");
                            }
                            else if (args.Current.Type.Equals("SecurityPolicy"))
                            {
                                secPol.Refresh();
                                var depSecPol = (_SMO.SecurityPolicy)database.Parent.GetSmoObject(args.Current);
                                Assert.AreEqual(secPol, depSecPol,
                                    "Dependent security policy is not equal to the security policy created above.");
                            }
                            else
                            {
                                Assert.Fail(String.Format("Unexpected dependency with urn: {0} on the table.",
                                    args.Current));
                            }
                        });

                    var urns = new Urn[1];
                    urns[0] = tab.Urn;
                    var tree = depWalker.DiscoverDependencies(urns, false /* don't show parents */);
                    Assert.That(tree.Count, Is.EqualTo(2),
                        "There should only be two dependencies in the tree, the policy and the table.");

                    // Walk the dependencies and validate using the above handler.
                    //
                    _ = depWalker.WalkDependencies(tree);
                });
        }

        [TestMethod]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        /// <summary>
        /// Regression test for https://github.com/microsoft/sqlmanagementobjects/issues/123
        /// </summary>
        public void Table_Alter_when_Column_marked_for_drop_succeeds_on_all_platforms()
        {
            ExecuteWithDbDrop(db =>
            {
                var table = db.CreateTable("markfordrop", new[] {
                    new ColumnProperties("col1") { Nullable  = false }, new ColumnProperties("col2") { Nullable  = false } });
                table.Columns["col1"].MarkForDrop(true);
                table.Alter();
                table.Columns.ClearAndInitialize(string.Empty, Enumerable.Empty<string>());
                Assert.That(table.Columns.Cast<_SMO.Column>().Select(c => c.Name), Is.EqualTo(new[] { "col2" }), "Column not dropped");
            });
        }
    }
}
