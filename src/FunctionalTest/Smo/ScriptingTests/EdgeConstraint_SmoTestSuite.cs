// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing EdgeConstraint properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class EdgeConstraint_SmoTestSuite : SmoObjectTestBase
    {
        // TODO: The tests are incomplete. All CRUD ops have not been covered, this is because of issues
        // encountered while creating Graph Node tables via the test suite. This will be revisited.

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.EdgeConstraint edgeConstraint = (_SMO.EdgeConstraint)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.EdgeConstraints.Refresh();
            Assert.IsNull(table.EdgeConstraints[edgeConstraint.Name],
                          "Current table not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a check constraint with IF EXISTS option through SMO on SQL19 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoDropIfExists_Edge_Sql19AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table edgeTable = GetTestGraphTable(database, "edgeTable", false);
                    _SMO.Table fromTable = GetTestGraphTable(database, "fromTable", true);
                    _SMO.Table toTable = GetTestGraphTable(database, "toTable", true);

                    _SMO.EdgeConstraint edgeConstraint = new _SMO.EdgeConstraint(edgeTable, GenerateSmoObjectName("EC_TEST"));

                    edgeConstraint.EdgeConstraintClauses.Add(GetEdgeConstraintTestClause(edgeConstraint, fromTable, toTable));

                    const string checkScriptDropIfExistsTemplate = "ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}";
                    string checkScriptDropIfExists = string.Format(checkScriptDropIfExistsTemplate,
                        edgeTable.FormatFullNameForScripting(new _SMO.ScriptingPreferences()),
                        edgeConstraint.FormatFullNameForScripting(new _SMO.ScriptingPreferences()));

                    VerifySmoObjectDropIfExists(edgeConstraint, edgeTable, checkScriptDropIfExists);
                });
        }

        /// <summary>
        /// Tests that EdgeConstraints are scripted correctly.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoScripting_EdgeConstraint_Sql19AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table edgeTable = GetTestGraphTable(database, "edgeTable", false);
                    _SMO.Table fromNode = GetTestGraphTable(database, "fromTable", true);
                    _SMO.Table toNode = GetTestGraphTable(database, "toTable", true);

                    _SMO.EdgeConstraint edgeConstraint = new _SMO.EdgeConstraint(edgeTable, GenerateSmoObjectName("EC_TEST"));

                    edgeConstraint.EdgeConstraintClauses.Add(GetEdgeConstraintTestClause(edgeConstraint, fromNode, toNode));

                    string scriptData = edgeConstraint.Script().ToSingleString();

                    Assert.IsTrue(scriptData.Contains("CONNECTION"));
                    Assert.IsTrue(scriptData.Contains("To"));
                    Assert.IsTrue(scriptData.Contains("fromTable"));
                    Assert.IsTrue(scriptData.Contains("toTable"));

                });
        }

        /// <summary>
        /// Tests that EdgeConstraints without clauses are not scripted.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoValidate_EdgeConstraintWithNoClause_Sql19AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table edgeTable = GetTestGraphTable(database, "edgeTable", false);
                    _SMO.Table fromNode = GetTestGraphTable(database, "fromTable", true);
                    _SMO.Table toNode = GetTestGraphTable(database, "toTable", true);

                    _SMO.EdgeConstraint edgeConstraint = new _SMO.EdgeConstraint(edgeTable, GenerateSmoObjectName("EC_TEST"));

                    // Edge constraints without any clauses are not supported.
                    //
                    Assert.Throws<FailedOperationException>(() => edgeConstraint.Create());
                });
        }

        /// <summary>
        /// Test to ensures that you cannot rename an edge constraint with null or empty names.
        /// Also ensures the rename functionality with valid new names.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoRename_EdgeConstraint_Sql19AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Table edgeTable = GetTestGraphTable(database, "edgeTable", false);
                    _SMO.Table fromTable = GetTestGraphTable(database, "fromTable", true);
                    _SMO.Table toTable = GetTestGraphTable(database, "toTable", true);

                    _SMO.EdgeConstraint edgeConstraint = new _SMO.EdgeConstraint(edgeTable, GenerateSmoObjectName("EC_TEST"));
                    edgeConstraint.EdgeConstraintClauses.Add(GetEdgeConstraintTestClause(edgeConstraint, fromTable, toTable));
                    edgeConstraint.Create();

                    Assert.Throws<InvalidSmoOperationException>(() => edgeConstraint.Rename(""));
                    Assert.Throws<InvalidSmoOperationException>(() => edgeConstraint.Rename(null));

                    edgeConstraint.Rename("EC_RENAMED");

                    Assert.IsTrue(edgeConstraint.Name.Equals("EC_RENAMED"));
                    
                });
        }

        /// <summary>
        /// Returns a dummy EdgeConstraintClause object used for testing.
        /// </summary>
        /// <param name="edgeConstraint">Reference of a parent EdgeConstraint object</param>
        /// <param name="fromTable">Table that forms origin of the connection</param>
        /// <param name="toTable">Table that forms the sink of the connection</param>
        /// <returns>Test instance of EdgeConstraintClause</returns>
        private EdgeConstraintClause GetEdgeConstraintTestClause(EdgeConstraint edgeConstraint, Table fromTable, Table toTable)
        {
            // EdgeConstraint clauses have a non-functional name. Therefore a numeric value is being used
            // as a name below.
            //
            _SMO.EdgeConstraintClause edgeConstraintClause = new _SMO.EdgeConstraintClause(edgeConstraint, "1567");

            edgeConstraintClause.From = fromTable.Name;
            edgeConstraintClause.To = toTable.Name;

            return edgeConstraintClause;
        }

        /// <summary>
        /// Instantiates a graph edge table based upon the passed arguments and
        /// calls the Create implementation of it.
        /// </summary>
        /// <param name="database">Reference of the database object which is the parent of the table to be created</param>
        /// <param name="name">name identifier of the new table</param>
        /// <param name="isNode">flag identifying whether the graph table represents a node or an edge</param>
        /// <returns>Test instance of a Graph Table</returns>
        private Table GetTestGraphTable(Database database, string name, bool isNode)
        {
            TableProperties tableProps = new TableProperties();
            tableProps.IsEdge = !isNode;
            tableProps.IsNode = isNode;

            _SMO.Table graphTable = DatabaseObjectHelpers.CreateTable(database, name, "dbo", tableProps);
            return graphTable;
        }
    }
}