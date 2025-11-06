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
        /// Valid binary data for a test certificate
        /// </summary>
        private const string certificateBinaryStream = "0x308205A830820390A00302010202101ED397095FD8B4B347701EAABE7F45B3300D06092A864886F70D01010C05003065310B3009060355040613025553311E301C060355040A13154D6963726F736F667420436F72706F726174696F6E313630340603550403132D4D6963726F736F66742052534120526F6F7420436572746966696361746520417574686F726974792032303137301E170D3139313231383232353132325A170D3432303731383233303032335A3065310B3009060355040613025553311E301C060355040A13154D6963726F736F667420436F72706F726174696F6E313630340603550403132D4D6963726F736F66742052534120526F6F7420436572746966696361746520417574686F72697479203230313730820222300D06092A864886F70D01010105000382020F003082020A0282020100CA5BBE94338C299591160A95BD4762C189F39936DF4690C9A5ED786A6F479168F8276750331DA1A6FBE0E543A3840257015D9C4840825310BCBFC73B6890B6822DE5F465D0CC6D19CC95F97BAC4A94AD0EDE4B431D8707921390808364353904FCE5E96CB3B61F50943865505C1746B9B685B51CB517E8D6459DD8B226B0CAC4704AAE60A4DDB3D9ECFC3BD55772BC3FC8C9B2DE4B6BF8236C03C005BD95C7CD733B668064E31AAC2EF94705F206B69B73F578335BC7A1FB272AA1B49A918C91D33A823E7640B4CD52615170283FC5C55AF2C98C49BB145B4DC8FF674D4C1296ADF5FE78A89787D7FD5E2080DCA14B22FBD489ADBACE479747557B8F45C8672884951C6830EFEF49E0357B64E798B094DA4D853B3E55C428AF57F39E13DB46279F1EA25E4483A4A5CAD513B34B3FC4E3C2E68661A45230B97A204F6F0F3853CB330C132B8FD69ABD2AC82DB11C7D4B51CA47D14827725D87EBD545E648659DAF5290BA5BA2186557129F68B9D4156B94C4692298F433E0EDF9518E4150C9344F7690ACFC38C1D8E17BB9E3E394E14669CB0E0A506B13BAAC0F375AB712B590811E56AE572286D9C9D2D1D751E3AB3BC655FD1E0ED3740AD1DAAAEA69B897288F48C407F852433AF4CA55352CB0A66AC09CF9F281E1126AC045D967B3CEFF23A2890A54D414B92AA8D7ECF9ABCD255832798F905B9839C40806C1AC7F0E3D00A50203010001A3543052300E0603551D0F0101FF040403020186300F0603551D130101FF040530030101FF301D0603551D0E0416041409CB597F86B2708F1AC339E3C0D9E9BFBB4DB223301006092B06010401823715010403020100300D06092A864886F70D01010C05000382020100ACAF3E5DC21196898EA3E792D69715B813A2A6422E02CD16055927CA20E8BAB8E81AEC4DA89756AE6543B18F009B52CD55CD53396D624C8B0D5B7C2E44BF83108FF3538280C34F3AC76E113FE6E3169184FB6D847F3474AD89A7CEB9D7D79F846492BE95A1AD095333DDEE0AEA4A518E6F55ABBAB59446AE8C7FD8A2502565608046DB3304AE6CB598745425DC93E4F8E355153DB86DC30AA412C169856EDF64F15399E14A75209D950FE4D6DC03F15918E84789B2575A94B6A9D8172B1749E576CBC156993A37B1FF692C919193E1DF4CA337764DA19FF86D1E1DD3FAECFBF4451D136DCFF759E52227722B86F357BB30ED244DDC7D56BBA3B3F8347989C1E0F20261F7A6FC0FBB1C170BAE41D97CBD27A3FD2E3AD19394B1731D248BAF5B2089ADB7676679F53AC6A69633FE5392C846B11191C6997F8FC9D66631204110872D0CD6C1AF3498CA6483FB1357D1C1F03C7A8CA5C1FD9521A071C193677112EA8F880A691964992356FBAC2A2E70BE66C40C84EFE58BF39301F86A9093674BB268A3B5628FE93F8C7A3B5E0FE78CB8C67CEF37FD74E2C84F3372E194396DBD12AFBE0C4E707C1B6F8DB332937344166DE8F4F7E095808F965D38A4F4ABDE0A308793D84D00716245274B3A42845B7F65B76734522D9C166BAAA8D87BA3424C71C70CCA3E83E4A6EFB701305E51A379F57069A641440F86B02C91C63DEAAE0F84";

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

