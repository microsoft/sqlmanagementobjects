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
    /// Test suite for testing LinkedServer properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
    public class LinkedServer_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.LinkedServer lnkServer = (_SMO.LinkedServer)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.LinkedServers.Refresh();
            Assert.IsNull(server.LinkedServers[lnkServer.Name],
                          "Current linked server not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a linked server with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_LinkedServer_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.LinkedServer lnkServer = new _SMO.LinkedServer(server,
                        GenerateUniqueSmoObjectName("lnkServer"));

                    lnkServer.ProductName = "SQL Server";

                    try
                    {
                        VerifySmoObjectDropIfExists(lnkServer, server);
                    }
                    catch (Exception)
                    {
                        if (server.LinkedServers[lnkServer.Name] != null)
                        {
                            lnkServer.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

