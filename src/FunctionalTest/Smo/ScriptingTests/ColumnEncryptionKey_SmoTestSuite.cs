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
    /// Test suite for testing ColumnEncryptionKey properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ColumnEncryptionKey_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ColumnEncryptionKey cek = (_SMO.ColumnEncryptionKey)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.ColumnEncryptionKeys.Refresh();
            Assert.IsNull(database.ColumnEncryptionKeys[cek.Name],
                "Current column encryption key not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an column encryption key with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ColumnEncryptionKey_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string cekName = GenerateSmoObjectName("cek");
                    string cmkName = GenerateSmoObjectName("cmk");
                    string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
                    string cmkPath = "CurrentUser/My/f2260f28d909d21c642a3d8e0b45a830e79a1420";
                    const string encryptionAlgorithm = "rsa_oaep";
                    byte[] cekEncryptedVal = SqlTestRandom.GenerateRandomBytes(32);
                    _SMO.ColumnMasterKey cmk = new _SMO.ColumnMasterKey(database, cmkName, cmkStoreProviderName, cmkPath);
                    _SMO.ColumnEncryptionKey cek = new _SMO.ColumnEncryptionKey(database, cekName);

                    cmk.Create();

                    _SMO.ColumnEncryptionKeyValue cekVal = new _SMO.ColumnEncryptionKeyValue(cek,
                        cmk, encryptionAlgorithm, cekEncryptedVal);
                    cek.ColumnEncryptionKeyValues.Add(cekVal);

                    VerifySmoObjectDropIfExists(cek, database);
                });
        }

        #endregion
    }
}
