// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Job properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class Job_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            Job job = (Job)obj;
            JobServer jobSvr = (JobServer)objVerify;

            jobSvr.Jobs.Refresh();
            Assert.IsNull(jobSvr.Jobs[job.Name],
                          "Current job not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a job with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Job_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    JobServer jobSvr = database.Parent.JobServer;
                    Job job = new Job(jobSvr,
                        GenerateUniqueSmoObjectName("job"));

                    try
                    {
                        VerifySmoObjectDropIfExists(job, jobSvr);
                    }
                    catch (Exception)
                    {
                        if (jobSvr.Jobs[job.Name] != null)
                        {
                            job.Drop();
                        }
                        throw;
                    }
                });
        }

        /// <summary>
        /// Tests that scripting multiple jobs with AgentJobId=false uses @job_name in sp_delete_job
        /// and @job_id in sp_add_jobstep.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
        public void SmoScripter_AgentJobIdFalse_EmitsJobNameInDeleteJob()
        {
            ExecuteFromDbPool(this.TestContext.FullyQualifiedTestClassName, database =>
                {
                    var server = database.Parent;
                    var jobs = new System.Collections.Generic.List<_SMO.Agent.Job>();

                    try
                    {
                        // Create two test jobs, each with one step
                        for (int i = 0; i < 2; i++)
                        {
                            var job = new _SMO.Agent.Job(server.JobServer,
                                GenerateUniqueSmoObjectName("job"));
                            job.Create();

                            var step = new _SMO.Agent.JobStep(job, "Step1");
                            step.Command = "SELECT 1";
                            step.SubSystem = _SMO.Agent.AgentSubSystem.TransactSql;
                            step.Create();

                            jobs.Add(job);
                        }

                        // Configure the scripter
                        var scripter = new _SMO.Scripter(server);
                        scripter.Options.ScriptDrops = false;
                        scripter.Options.WithDependencies = false;
                        scripter.Options.IncludeHeaders = true;
                        scripter.Options.AppendToFile = true;
                        scripter.Options.AgentJobId = false;

                        // Test each job
                        foreach (var job in jobs)
                        {
                            // Script with ScriptDrops = true and verify sp_delete_job uses @job_name
                            scripter.Options.ScriptDrops = true;
                            var deleteScripts = scripter.Script(new Urn[] { job.Urn });

                            Assert.That(deleteScripts, Has.Some.Matches<string>(line =>
                                line.Contains("msdb.dbo.sp_delete_job") && line.Contains($"@job_name=N'{job.Name}'")),
                                $"Expected sp_delete_job with @job_name=N'{job.Name}' in delete scripts");

                            // Script with ScriptDrops = false and verify sp_add_jobstep uses @job_id
                            scripter.Options.ScriptDrops = false;
                            var createScripts = scripter.Script(new Urn[] { job.Urn });

                            Assert.That(createScripts, Has.Some.Matches<string>(line =>
                                line.Contains("msdb.dbo.sp_add_jobstep") && line.Contains("@job_id=@jobId")),
                                "Expected sp_add_jobstep with @job_id=@jobId in create scripts");
                        }
                    }
                    finally
                    {
                        // Cleanup: drop all test jobs
                        foreach (var job in jobs)
                        {
                            if (job.State == _SMO.SqlSmoState.Existing)
                            {
                                job.Drop();
                            }
                        }
                    }
                });
        }

        #endregion // Scripting Tests

        // TODO: Fix collection construction to ensure StringComparer initialization at the Parent and Server levels 
        // Then we can remove the m_comparer assignments in object constructors.
        // https://github.com/microsoft/sqlmanagementobjects/issues/31
        //[TestMethod]
        //[SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        //public void JobServer_constructor_succeeds_without_msdb_database()
        //{
        //    ExecuteTest(() =>
        //    {
        //        var server = new Management.Smo.Server(new ServerConnection(new SqlConnection(SqlConnectionStringBuilder.ConnectionString)));
        //        Trace.TraceInformation("Removing msdb from server.Databases");
        //        server.Databases.ClearAndInitialize("[@Name = 'master']", Enumerable.Empty<string>());
        //        var jobServer = server.JobServer;
        //        Assert.That(jobServer.Name, Is.EqualTo(server.Name), "jobServer.Name");
        //        var jobServerComparer = jobServer.StringComparer;
        //        server.Databases.ClearAndInitialize("[@Name = 'msdb']", Enumerable.Empty<string>());
        //        var msdbComparer = server.Databases["msdb"].StringComparer;
        //        Assert.Multiple(() =>
        //        {
        //            Assert.That(jobServerComparer.CultureInfo, Is.EqualTo(msdbComparer.CultureInfo), "JobServer StringComparer.CultureInfo should match msdb StringComparer");
        //            Assert.That(jobServerComparer.CompareOptions, Is.EqualTo(msdbComparer.CompareOptions), "JobServer StringComparer.CompareOptions should match msdb StringComparer");
        //        });
        //    });
        //}
    }
}

