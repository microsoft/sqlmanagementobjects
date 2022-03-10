// Copyright (c) Microsoft.
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
    /// Test suite for testing JobSchedule properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class JobSchedule_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            JobSchedule jobSched = (JobSchedule)obj;
            Job job = (Job)objVerify;

            job.Refresh();
            Assert.IsNull(job.JobSchedules[jobSched.Name],
                          "Current job schedule not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a job schedule with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_JobSchedule_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    JobServer jobSvr = database.Parent.JobServer;
                    Job job = new Job(jobSvr,
                        GenerateUniqueSmoObjectName("job"));
                    JobSchedule jobSched = new JobSchedule(job,
                        GenerateSmoObjectName("jbschd"));

                    jobSched.FrequencyTypes = FrequencyTypes.Daily;
                    jobSched.FrequencySubDayTypes = FrequencySubDayTypes.Hour;
                    jobSched.FrequencySubDayInterval = 1;
                    TimeSpan tStart = new TimeSpan(12, 0, 0);
                    jobSched.ActiveStartTimeOfDay = tStart;
                    TimeSpan tEnd = new TimeSpan(18, 0, 0);
                    jobSched.ActiveEndTimeOfDay = tEnd;
                    jobSched.FrequencyInterval = 1;
                    DateTime date = new DateTime(2016, 1, 1);
                    jobSched.ActiveStartDate = date;

                    try
                    {
                        job.Create();

                        VerifySmoObjectDropIfExists(jobSched, job);
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

