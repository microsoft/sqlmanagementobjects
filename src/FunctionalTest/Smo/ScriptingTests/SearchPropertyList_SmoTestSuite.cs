// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing SearchPropertyList properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    // This test suite is unsupported on edge because it uses full text which is not supported on edge.
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
    public class SearchPropertyList_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.SearchPropertyList spl = (_SMO.SearchPropertyList)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.SearchPropertyLists.Refresh();
            Assert.IsNull(database.SearchPropertyLists[spl.Name],
                            "Current search property list not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a search property list with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase)]
        public void SmoDropIfExists_SearchPropertyList_Sql16AndAfterOnPrem()
        {
            ExecuteWithDbDrop(
                database =>
                {
                    _SMO.SearchPropertyList spl = new _SMO.SearchPropertyList(database,
                        GenerateSmoObjectName("spl"));

                    VerifySmoObjectDropIfExists(spl, database);
                });
        }

        #endregion
    }
}
