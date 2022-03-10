// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing JobStep properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class JobStep_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            JobStep jobStep = (JobStep)obj;
            Job job = (Job)objVerify;

            job.Refresh();
            Assert.IsNull(job.JobSteps[jobStep.Name],
                          "Current job step not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a job step with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_JobStep_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    JobServer jobSvr = database.Parent.JobServer;
                    Job job = new Job(jobSvr,
                        GenerateUniqueSmoObjectName("job"));
                    JobStep jobStep = new JobStep(job,
                        GenerateSmoObjectName("jbstp"));

                    jobStep.Command = "SELECT 1";
                    jobStep.OnSuccessAction = StepCompletionAction.QuitWithSuccess;
                    jobStep.OnFailAction = StepCompletionAction.QuitWithFailure;
                    jobStep.SubSystem = AgentSubSystem.TransactSql;

                    try
                    {
                        job.Create();

                        VerifySmoObjectDropIfExists(jobStep, job);
                    }
                    finally
                    {
                        job.DropIfExists();
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

