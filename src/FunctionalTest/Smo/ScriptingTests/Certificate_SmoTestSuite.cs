// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for creating and scripting SMO Certificate objects
    /// Note that certificates are not properly covered in SMO baseline tests
    /// as we can't correctly script them
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
    public class Certificate_SmoTestSuite : SmoObjectTestBase
    {
        /// <summary>
        /// Valid binary data for a test certificate. This was generated from a self-signed cert as the content doesn't actually matter for the purposes of this test.
        /// </summary>
        /// <remarks>Do NOT use a valid Microsoft cert for this if updating in the future, or the security team gets unhappy about hardcoded certs in code (even test code)</remarks>
        private const string certificateBinaryStream = "0x308202FC308201E4A0030201020210386647026609F186460FC38993A4BCB4300D06092A864886F70D01010B05003011310F300D06035504030C064D7943657274301E170D3235303832383230353932395A170D3236303832383231313932395A3011310F300D06035504030C064D794365727430820122300D06092A864886F70D01010105000382010F003082010A0282010100D0E818D323B886D921342F189E026F0D014869507788BC2AB61126F4CABDA275427E02B60E5C856BE9BAA06D9D67D84950040198F87663ADD6E3EB58D29E41C473B9887E58A133F3804073E4C4AFAB1E28600A1C33BAD4041BD597232DFC343CDDDA007816172F139B4BDC395177C49C609E3BE55EA8BFB07EEC8D1F400DEC6CBCA175C2ACA3246615FBB5F9676B89B435ADC2A31F46A245B923E9FDE688779CB1489112D7E1F3B08A05611AF6D0AC1B108C177C56F4198CF500C4FB7A61FE75DB87842E9DF09EDB23B3EC09B49BF88C9CB4573EBBC342861D9919187D0CAFE894112567001442D2F444D2314A08E70088943DBBD6BBC0F1C05DE69E1E3CE72D0203010001A350304E300E0603551D0F0101FF0404030205A0301D0603551D250416301406082B0601050507030206082B06010505070301301D0603551D0E041604148F0B8A94BB8FC4688035707D3A517D73675A4B86300D06092A864886F70D01010B05000382010100BFDA24C14E2E629F56CEC159667C0A19F553C23C59DBAFA3AE060AD142D21289FAADFCBC3F180B22142A1B050C8F999AC5E2D70A2C13D448C81B51AFBF1DF475CA71E312D637DBD0DE4463490078250C29E04B14E54A603973ECA5644E1E440D7C80FC5A863E6605046CAD8B35986074C5361433420DE45EAA3CA6CB85D5A9AB7D8B6F795DCECEC795A32AAA62CB4D457D97F3AAADB6565C75CB484F8F67735027321432FF09172F2A8E1F1285B8DF719E0C42CD9E40D45361008414275FB89C853B0C2AB7353D8B75E49820A75753FF2BA95E4B41F14D7C934A2E0E8D214298004572E0C7804DEB22BB43642833E4AAA978BDB05ED3D425D53108BB9CEBC2F6";

        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Certificate cert = (_SMO.Certificate)obj;
            _SMO.Database db = (_SMO.Database)objVerify;

            db.Certificates.Refresh();
            Assert.IsNull(db.Certificates[cert.Name],
                          "Given certificate is not dropped.");
        }

        /// <summary>
        /// Tests creating a certificate using ... FROM BINARY = 0x.... syntax
        /// </summary>
        [TestMethod]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void CreateCertificateFromBinary()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string certName = GenerateUniqueSmoObjectName("cert_A.B.C.D");

                    _SMO.Certificate cert = new _SMO.Certificate(database, certName);

                    cert.Create(certificateBinaryStream, _SMO.CertificateSourceType.Binary);

                    // Sanity checks
                    //
                    Assert.That(cert.State, Is.EqualTo(_SMO.SqlSmoState.Existing));
                    Assert.That(cert.ID, Is.GreaterThan(0));
                    Assert.That(cert.PrivateKeyEncryptionType, Is.EqualTo(_SMO.PrivateKeyEncryptionType.NoKey));
                    Assert.That(cert.Name, Is.EqualTo(certName));

                    // Drop the certificate
                    //
                    cert.Drop();
                    VerifyIsSmoObjectDropped(cert, database);
                });
        }

        /// <summary>
        /// Tests if we're correctly a certificate using FROM BINARY = 0x.... syntax
        /// </summary>
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabaseEdge)]
        [TestMethod]
        public void ScriptCertificateFromBinary()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string certName = GenerateUniqueSmoObjectName("cert_1.2.3.4-test");
                    string expectedContents = $"CREATE CERTIFICATE [{certName}]   FROM BINARY = {certificateBinaryStream}";

                    _SMO.ExecutionManager mgr = database.ExecutionManager;

                    _SMO.Certificate cert = new _SMO.Certificate(database, certName);

                    // Capture T-SQL that would be used to create a certificate
                    //
                    StringCollection capturedSql = mgr.RecordQueryText(() => cert.Create(certificateBinaryStream, _SMO.CertificateSourceType.Binary));

                    // Reformat captured T-SQL a bit to ensure our CREATE statement is in a single line
                    // without extra linebreaks for easier comparison
                    // 
                    string actualContents = capturedSql.ToSingleString().Replace(System.Environment.NewLine, " ");

                    Assert.That(actualContents, Does.Contain(expectedContents));
                });
        }

        #endregion // Scripting Tests
    }
}

