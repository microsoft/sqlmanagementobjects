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
    /// Test suite for testing ColumnEncryptionKeyValue properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ColumnEncryptionKeyValue_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            _SMO.ColumnEncryptionKeyValue cekVal = (_SMO.ColumnEncryptionKeyValue)obj;
            _SMO.ColumnEncryptionKey cek = cekVal.Parent;

            cek.Create();
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ColumnEncryptionKey cek = (_SMO.ColumnEncryptionKey)objVerify;

            cek.ColumnEncryptionKeyValues.Refresh();
            Assert.IsTrue(1 == cek.ColumnEncryptionKeyValues.Count,
                          "Current column encryption key value not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an column encryptyon key value with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ColumnEncryptionKeyValue_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string cekName = GenerateSmoObjectName("cek");
                    string cmkName = GenerateSmoObjectName("cmk");
                    string dropCmkName = GenerateSmoObjectName("dropCmk");
                    string cmkStoreProviderName = "MSSQL_CERTIFICATE_STORE";
                    string cmkPath = "CurrentUser/My/f2260f28d909d21c642a3d8e0b45a830e79a1420";
                    const string encryptionAlgorithm = "rsa_oaep";
                    byte[] cekEncryptedVal = SqlTestRandom.GenerateRandomBytes(32);
                    byte[] dropCekEncryptedVal = SqlTestRandom.GenerateRandomBytes(32);
                    _SMO.ColumnMasterKey cmk = new _SMO.ColumnMasterKey(database, cmkName, cmkStoreProviderName, cmkPath);
                    _SMO.ColumnMasterKey dropCmk = new _SMO.ColumnMasterKey(database, dropCmkName, cmkStoreProviderName, cmkPath);
                    _SMO.ColumnEncryptionKey cek = new _SMO.ColumnEncryptionKey(database, cekName);

                    cmk.Create();
                    dropCmk.Create();

                    _SMO.ColumnEncryptionKeyValue cekVal = new _SMO.ColumnEncryptionKeyValue(cek,
                        cmk, encryptionAlgorithm, cekEncryptedVal);
                    _SMO.ColumnEncryptionKeyValue dropCekVal = new _SMO.ColumnEncryptionKeyValue(cek,
                        dropCmk, encryptionAlgorithm, dropCekEncryptedVal);
                    cek.ColumnEncryptionKeyValues.Add(cekVal);
                    cek.ColumnEncryptionKeyValues.Add(dropCekVal);

                    VerifySmoObjectDropIfExists(dropCekVal, cek);
                });
        }

        #endregion
    }
}
