// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ProxyAccount properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class ProxyAccount_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            ProxyAccount proxyAcc = (ProxyAccount)obj;
            JobServer jobSvr = (JobServer)objVerify;

            jobSvr.ProxyAccounts.Refresh();
            Assert.IsNull(jobSvr.ProxyAccounts[proxyAcc.Name],
                          "Current proxy account not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a proxy account with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ProxyAccount_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    JobServer jobSvr = server.JobServer;
                    _SMO.Login login = new _SMO.Login(server,
                        GenerateUniqueSmoObjectName("login"));
                    _SMO.Credential crd = new _SMO.Credential(server,
                        GenerateUniqueSmoObjectName("crd"));
                    ProxyAccount proxyAcc = new ProxyAccount(jobSvr,
                        GenerateUniqueSmoObjectName("proxyAcc"),
                        crd.Name);

                    login.LoginType = _SMO.LoginType.SqlLogin;
                    crd.Identity = login.Name;

                    try
                    {
                        login.Create(Guid.NewGuid().ToString());
                        crd.Create();

                        VerifySmoObjectDropIfExists(proxyAcc, jobSvr);
                    }
                    catch (Exception e)
                    {
                        if (jobSvr.ProxyAccounts[proxyAcc.Name] != null)
                        {
                            proxyAcc.Drop();
                        }
                        Assert.Fail("Exception :\n" + e.ToString());
                    }
                    finally
                    {
                        crd.DropIfExists();
                        login.DropIfExists();
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

