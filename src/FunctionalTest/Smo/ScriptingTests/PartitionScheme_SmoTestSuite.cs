// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing PartitionScheme properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class PartitionScheme_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.PartitionScheme ps = (_SMO.PartitionScheme)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.PartitionSchemes.Refresh();
            Assert.IsNull(database.PartitionSchemes[ps.Name],
                            "Current partition scheme not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a partition scheme with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_PartitionScheme_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.PartitionScheme ps = new _SMO.PartitionScheme(database,
                        GenerateSmoObjectName("ps"));
                    _SMO.PartitionFunction pf = new _SMO.PartitionFunction(database,
                        GenerateSmoObjectName("pf"));
                    _SMO.PartitionFunctionParameter pfp =
                        new _SMO.PartitionFunctionParameter(pf, _SMO.DataType.DateTime);

                    pf.PartitionFunctionParameters.Add(pfp);

                    object[] val;
                    val = new object[] { "1/1/2015" };
                    pf.RangeValues = val;

                    pf.Create();

                    _SMO.FileGroup fg = new _SMO.FileGroup(database,
                        GenerateSmoObjectName("fg"));
                    fg.Create();

                    ps.PartitionFunction = pf.Name;
                    ps.FileGroups.Add("PRIMARY");
                    ps.FileGroups.Add(fg.Name);

                    VerifySmoObjectDropIfExists(ps, database);
                });
        }

        #endregion
    }
}
