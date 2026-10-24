// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing LinkedServerLogin properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
    public class LinkedServerLogin_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.LinkedServerLogin lnkServerLogin = (_SMO.LinkedServerLogin)obj;
            _SMO.LinkedServer lnkServer = (_SMO.LinkedServer)objVerify;

            lnkServer.LinkedServerLogins.Refresh();
            Assert.IsNull(lnkServer.LinkedServerLogins[lnkServerLogin.Name],
                          "Current linked server login not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a linked server login with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_LinkedServerLogin_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.LinkedServer lnkServer = new _SMO.LinkedServer(server,
                        GenerateUniqueSmoObjectName("lnkServer"));
                    _SMO.LinkedServerLogin lnkServerLogin = new _SMO.LinkedServerLogin(lnkServer,
                        "");

                    lnkServer.ProductName = "SQL Server";

                    lnkServerLogin.Impersonate = true;

                    try
                    {
                        lnkServer.Create();

                        VerifySmoObjectDropIfExists(lnkServerLogin, lnkServer);
                    }
                    finally
                    {
                        lnkServer.DropIfExists();
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

