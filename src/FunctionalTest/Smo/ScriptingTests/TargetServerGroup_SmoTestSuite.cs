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
    /// Test suite for testing TargetServerGroup properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class TargetServerGroup_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            TargetServerGroup tsg = (TargetServerGroup)obj;
            JobServer jobSvr = (JobServer)objVerify;

            jobSvr.TargetServerGroups.Refresh();
            Assert.IsNull(jobSvr.TargetServerGroups[tsg.Name],
                          "Current job not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a target server group with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_TargetServerGroup_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    JobServer jobSvr = database.Parent.JobServer;
                    TargetServerGroup tsg = new TargetServerGroup(jobSvr,
                        GenerateUniqueSmoObjectName("tsg"));

                    try
                    {
                        VerifySmoObjectDropIfExists(tsg, jobSvr);
                    }
                    catch (Exception)
                    {
                        if (jobSvr.TargetServerGroups[tsg.Name] != null)
                        {
                            tsg.Drop();
                        }
                        throw;
                    }
                });
        }

            #endregion // Scripting Tests
    }
}

