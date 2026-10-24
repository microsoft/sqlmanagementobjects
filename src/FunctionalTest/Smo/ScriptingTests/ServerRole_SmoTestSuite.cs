// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ServerRole properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ServerRole_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ServerRole roleSvr = (_SMO.ServerRole)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Roles.Refresh();
            Assert.IsNull(server.Roles[roleSvr.Name],
                          "Current server role not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a server role with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ServerRole_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.ServerRole roleSvr = new _SMO.ServerRole(server,
                        GenerateUniqueSmoObjectName("roleSvr"));

                    try
                    {
                        VerifySmoObjectDropIfExists(roleSvr, server);
                    }
                    catch (Exception)
                    {
                        if (server.Roles[roleSvr.Name] != null)
                        {
                            roleSvr.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

