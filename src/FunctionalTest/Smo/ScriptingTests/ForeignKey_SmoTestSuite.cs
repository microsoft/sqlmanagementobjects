// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ForeignKey properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ForeignKey_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ForeignKey fk = (_SMO.ForeignKey)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.ForeignKeys.Refresh();
            Assert.IsNull(table.ForeignKeys[fk.Name],
                          "Current foreign key constraint not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a foreign key constraint with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ForeignKey_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    var columns = new[]
                    {
                        new ColumnProperties("c1") {Nullable = false}
                    };

                    _SMO.Table table = database.CreateTable(this.TestContext.TestName, columns);
                    _SMO.ForeignKey fk = new _SMO.ForeignKey(table, GenerateSmoObjectName("fk"));

                    table.CreateIndex(this.TestContext.TestName, new IndexProperties() { KeyType = _SMO.IndexKeyType.DriPrimaryKey});

                    _SMO.ForeignKeyColumn fkc = new _SMO.ForeignKeyColumn(fk, table.Columns[0].Name, table.Columns[0].Name);
                    fk.Columns.Add(fkc);
                    fk.ReferencedTable = table.Name;
                    fk.ReferencedTableSchema = table.Schema;

                    const string foreignKeyScriptDropIfExistsTemplate = "ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}";
                    string foreignKeyScriptDropIfExists = string.Format(foreignKeyScriptDropIfExistsTemplate,
                        table.FormatFullNameForScripting(new _SMO.ScriptingPreferences()),
                        fk.FormatFullNameForScripting(new _SMO.ScriptingPreferences() {IncludeScripts = {SchemaQualify = false}}));

                    VerifySmoObjectDropIfExists(fk, table, foreignKeyScriptDropIfExists);
                });
        }

        #endregion // Scripting Tests
    }
}
