// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Credential properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Credential_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Credential crd = (_SMO.Credential)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Credentials.Refresh();
            Assert.IsNull(server.Credentials[crd.Name],
                            "Current credential not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a credential with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Credential_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.Credential crd = new _SMO.Credential(server, GenerateUniqueSmoObjectName("crd"));

                    crd.Identity = "testID";

                    try
                    {
                        VerifySmoObjectDropIfExists(crd, server);
                    }
                    catch (Exception)
                    {
                        if (server.Credentials[crd.Name] != null)
                        {
                            crd.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

