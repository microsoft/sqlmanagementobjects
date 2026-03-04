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
    /// Test suite for testing ColumnMasterKey properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedFeature(SqlFeature.Fabric)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ColumnMasterKey_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ColumnMasterKey cmk = (_SMO.ColumnMasterKey)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.ColumnMasterKeys.Refresh();
            Assert.IsNull(database.ColumnMasterKeys[cmk.Name],
                          "Current column master key not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an column master key with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void SmoDropIfExists_ColumnMasterKey_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string cmkName = GenerateSmoObjectName("cmk");
                    string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
                    string cmkPath = "CurrentUser/My/f2260f28d909d21c642a3d8e0b45a830e79a1420";
                    _SMO.ColumnMasterKey cmk = new _SMO.ColumnMasterKey(database, cmkName, cmkStoreProviderName, cmkPath);

                    VerifySmoObjectDropIfExists(cmk, database);
                });
        }

        /// <summary>
        /// Tests dropping an column master key with IF EXISTS option through SMO on SQL19 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        public void SmoDropIfExists_ColumnMasterKey_AEV2()
        {
            this.ExecuteWithDbDrop(
               database =>
               {
                   string cmkName = GenerateSmoObjectName("cmk");
                   string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
                   string cmkPath = "CurrentUser/My/f2260f28d909d21c642a3d8e0b45a830e79a1420";
                   bool enclaveComputations = true;
                   byte[] signature = SqlTestRandom.GenerateRandomBytes(32);
                   _SMO.ColumnMasterKey cmk = new _SMO.ColumnMasterKey(database, cmkName, cmkStoreProviderName, cmkPath, enclaveComputations, signature);

                   VerifySmoObjectDropIfExists(cmk, database);
               });
        }

        #endregion
    }
}