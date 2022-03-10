// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing UserDefinedTableType properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class UserDefinedTableType_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.UserDefinedTableType udtt = (_SMO.UserDefinedTableType)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.UserDefinedTableTypes.Refresh();
            Assert.IsNull(database.UserDefinedTableTypes[udtt.Name],
                          "Current user-defined table type not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an user-defined table type with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_UDTT_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.UserDefinedTableType udtt = new _SMO.UserDefinedTableType(database,
                        GenerateSmoObjectName("udtt"));

                    _SMO.Column column = new _SMO.Column(udtt, "testCol", _SMO.DataType.Int);
                    udtt.Columns.Add(column);

                    string udttScriptDropIfExistsTemplate = "DROP TYPE IF EXISTS [{0}].[{1}]";
                    string udttScriptDropIfExists = string.Format(udttScriptDropIfExistsTemplate,
                        udtt.Schema, udtt.Name);

                    VerifySmoObjectDropIfExists(udtt, database, udttScriptDropIfExists);
                });
        }

        #endregion
    }
}
