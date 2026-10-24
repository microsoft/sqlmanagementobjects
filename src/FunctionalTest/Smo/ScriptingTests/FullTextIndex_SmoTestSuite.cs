// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing FullTextIndex properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class FullTextIndex_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Table table = (_SMO.Table)objVerify;

            table.Refresh();
            Assert.IsNull(table.FullTextIndex,
                          "Current full text index not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a full text index with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void SmoDropIfExists_FullTextIndex_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ColumnProperties[] columns =
                        {new ColumnProperties("test_col", _SMO.DataType.Char(1))};

                    _SMO.Table table = DatabaseObjectHelpers.CreateTable(
                        database: database, 
                        tableNamePrefix: TestContext.TestName,
                        schemaName: "dbo",
                        columnProperties: columns);

                    _SMO.Column column = table.Columns[0];
                    
                    column.Nullable = false;
                    column.Alter();

                    _SMO.Index ui = new _SMO.Index(table, GenerateSmoObjectName("ui"));
                    _SMO.IndexedColumn uic = new _SMO.IndexedColumn(ui, table.Columns[0].Name);

                    ui.IndexedColumns.Add(uic);
                    ui.IndexKeyType = _SMO.IndexKeyType.DriUniqueKey;
                    ui.Create();

                    _SMO.FullTextCatalog ftc = new _SMO.FullTextCatalog(database,
                        GenerateSmoObjectName("ftc"));
                    _SMO.FullTextIndex fti = new _SMO.FullTextIndex(table);
                    _SMO.FullTextIndexColumn ftic = new _SMO.FullTextIndexColumn(fti,
                        table.Columns[0].Name);

                    ftc.IsDefault = true;
                    ftc.Create();

                    fti.IndexedColumns.Add(ftic);
                    fti.ChangeTracking = _SMO.ChangeTracking.Automatic;
                    fti.UniqueIndexName = ui.Name;
                    fti.CatalogName = ftc.Name;

                    VerifySmoObjectDropIfExists(fti, table);
                });
        }

        #endregion
    }
}
