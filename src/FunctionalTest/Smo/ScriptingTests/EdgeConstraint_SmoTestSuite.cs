// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework.Internal;

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

                    string expectedScriptData = $"ALTER TABLE {edgeTable.FullQualifiedName} ADD CONSTRAINT [{edgeConstraint.Name}]" +
                        $" CONNECTION ({fromNode.FullQualifiedName} To {toNode.FullQualifiedName})\r\n";

                    Assert.That(scriptData, Is.EqualTo(expectedScriptData));
                });
        }

        /// <summary>
        /// Tests that EdgeConstraints are scripted correctly with different schemas.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoScripting_ECWithMultipleSchemas_Sql19AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    var fromSchema = new _SMO.Schema(database, SmoObjectHelpers.GenerateUniqueObjectName("fromSchema"));
                    fromSchema.Create();
                    var fromNode = GetTestGraphTable(database, "fromTable", true, fromSchema.Name);

                    var toSchema = new _SMO.Schema(database, SmoObjectHelpers.GenerateUniqueObjectName("toSchema"));
                    toSchema.Create();
                    var toNode = GetTestGraphTable(database, "toTable", true, toSchema.Name);

                    var edgeTable = new _SMO.Table(database, SmoObjectHelpers.GenerateUniqueObjectName("edgeTable"));
                    edgeTable.IsEdge = true;
                    var edgeConstraintName = GenerateSmoObjectName("EC_TEST");
                    edgeTable.EdgeConstraints.Add(new _SMO.EdgeConstraint(edgeTable, edgeConstraintName));
                    edgeTable.EdgeConstraints[edgeConstraintName].EdgeConstraintClauses.Add(GetEdgeConstraintTestClause(edgeTable.EdgeConstraints[edgeConstraintName], fromNode, toNode));
                    edgeTable.Create();

                    var expectedECName = (string)database.ExecutionManager.ExecuteScalar($@"SELECT name FROM sys.edge_constraints
                        WHERE type = 'EC' AND parent_object_id = {edgeTable.ID}");
                    Assert.That(edgeConstraintName, Is.EqualTo(expectedECName), "Edge constraint is not found in the table.");
                    
                    var scripter = new _SMO.Scripter(database.Parent);
                    var script = scripter.Script(edgeTable);
                    var expectedScriptData = $"ALTER TABLE {edgeTable.FullQualifiedName} ADD CONSTRAINT [{_SMO.SqlSmoObject.SqlBraket(edgeConstraintName)}]" +
                        $" CONNECTION ({fromNode.FullQualifiedName} To {toNode.FullQualifiedName})";
                    Assert.That(script, Has.Member(expectedScriptData), "Incorrect edge constraint script got generated.");
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
            _SMO.EdgeConstraintClause edgeConstraintClause = new _SMO.EdgeConstraintClause(edgeConstraint, fromTable, toTable);

            return edgeConstraintClause;
        }

        /// <summary>
        /// Instantiates a graph edge table based upon the passed arguments and
        /// calls the Create implementation of it.
        /// </summary>
        /// <param name="database">Reference of the database object which is the parent of the table to be created</param>
        /// <param name="name">name identifier of the new table</param>
        /// <param name="isNode">flag identifying whether the graph table represents a node or an edge</param>
        /// <param name="schemaName">schema of the new table</param>
        /// <returns>Test instance of a Graph Table</returns>
        private Table GetTestGraphTable(Database database, string name, bool isNode, string schemaName = "dbo")
        {
            TableProperties tableProps = new TableProperties()
            {
                IsNode = isNode,
                IsEdge = !isNode,
            };

            _SMO.Table graphTable = DatabaseObjectHelpers.CreateTable(database, name, schemaName, tableProps);
            return graphTable;
        }
    }
}