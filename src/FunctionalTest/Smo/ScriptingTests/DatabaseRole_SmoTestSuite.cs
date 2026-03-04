// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Database Role properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseRole_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.DatabaseRole roleDb = (_SMO.DatabaseRole)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Roles.Refresh();
            Assert.IsNull(database.Roles[roleDb.Name],
                          "Current role not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a database role with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_DatabaseRole_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.DatabaseRole roleDb = new _SMO.DatabaseRole(database, GenerateSmoObjectName("role"));

                    string roleScriptDropIfExistsTemplate = "DROP ROLE IF EXISTS [{0}]";
                    string roleScriptDropIfExists = string.Format(roleScriptDropIfExistsTemplate, roleDb.Name);

                    VerifySmoObjectDropIfExists(roleDb, database, roleScriptDropIfExists);
                });
        }

        #endregion
    }
}
