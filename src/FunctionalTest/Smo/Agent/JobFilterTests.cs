// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
namespace Microsoft.SqlServer.Test.SMO.Agent
{
    [TestClass]
    [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
    public class JobFilterTests : SqlTestBase
    {
        [TestMethod]
        public void JobFilter_constrains_EnumJobs_output()
        {
            ExecuteTest(() =>
            {
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.EnumJobs(null), "EnumJobs(null)");
                ServerContext.SetDefaultInitFields(typeof(JobCategory), allFields: true);
                ServerContext.SetDefaultInitFields(typeof(Job), allFields: true);
                // yes it's a race condition to compare two collections but the likelihood of jobs being added or remove are low
                var allJobsData = ServerContext.JobServer.EnumJobs();
                var allJobs = ServerContext.JobServer.Jobs.Cast<Job>().ToArray();
                Assert.That(allJobsData.Rows.Cast<DataRow>().Select(r => r["Name"]),
                    Is.EquivalentTo(allJobs.Select(j => j.Name)), "EnumJobs should match Jobs collection");
                if (allJobs.Length == 0)
                {
                    return;
                }
                var jobCategory = ServerContext.JobServer.JobCategories[allJobs[0].Category].ID;
                // there's a race condition but the set of jobs on our test servers change slowly
                var jobFilter = new JobFilter
                {
                    Category = allJobs[0].Category,
                    Owner = allJobs[0].OwnerLoginName,
                    JobType = allJobs[0].JobType,
                    StepSubsystem = allJobs[0].JobSteps[0].SubSystem,
                    Enabled = allJobs[0].IsEnabled                   
                };
                var data = ServerContext.JobServer.EnumJobs(jobFilter);
                var filteredJobs = data.Rows.Cast<DataRow>().ToArray();
                Assert.That(filteredJobs.Select(r => (string)r["Name"]), Has.Member(allJobs[0].Name), "Filter should have found the job");
                Assert.That(filteredJobs.Select(r => (int)r["CategoryID"]), Has.All.EqualTo(jobCategory), "Category filter");
                Assert.That(filteredJobs.Select(r => (string)r["OwnerLoginName"]), Has.All.EqualTo(jobFilter.Owner), "Owner filter");
                Assert.That(filteredJobs.Select(r => (bool)r["IsEnabled"]), Has.All.EqualTo(jobFilter.Enabled), "Enabled filter");
                var midJob = allJobs.OrderBy(a => a.DateCreated).ToArray()[allJobs.Length / 2];
                Trace.TraceInformation($"Total jobs: {allJobs.Length}. Max date:{midJob.DateCreated}");
                jobFilter = new JobFilter()
                {
                    DateJobCreated = midJob.DateCreated,
                    DateFindOperand = FindOperand.LessThan,
                    CurrentExecutionStatus = JobExecutionStatus.Idle,
                };
                data = ServerContext.JobServer.EnumJobs(jobFilter);
                var jobNames = data.Rows.Cast<DataRow>().OrderBy(r => (DateTime)r["DateCreated"]).Select(r => (string)r["Name"]).ToArray();
                // DateTime resolution isn't sufficient to uniquely identify jobs, so jobs created during the same 1 second period get excluded.
                Assert.That(jobNames, Is.SubsetOf(allJobs.OrderBy(a => a.DateCreated).Take(allJobs.Length/2).Select(j => j.Name).ToArray()), "Filter on date");
            });
        }

        [TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void JobHistoryFilter_constrains_EnumJobHIstory_output()
        {
            ExecuteTest(() =>
            {
                var allJobHistory = ServerContext.JobServer.EnumJobHistory();
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.EnumJobHistory(null), "EnumJobHistory(null)");
                var allJobs = ServerContext.JobServer.Jobs.Cast<Job>().ToArray();
                var testJob = allJobs.FirstOrDefault(a => a.LastRunDate > DateTime.MinValue);
                Assert.That(testJob, Is.Not.Null, "No jobs have last run date");
                var jobHistoryFilter = new JobHistoryFilter()
                {
                    JobName = testJob.Name,
                    EndRunDate = testJob.LastRunDate
                };
                var data = ServerContext.JobServer.EnumJobHistory(jobHistoryFilter).Rows.Cast<DataRow>().ToArray();
                Assert.That(data, Is.Not.Empty, "EnumJobHistory should have at least one row");
                Assert.That(data.Length, Is.LessThanOrEqualTo(allJobHistory.Rows.Count), "Filter should be a subset of all history");
                Assert.Multiple(() =>
                {
                   Assert.That(data.Select(r => (string)r["JobName"]), Has.All.EqualTo(testJob.Name), "Filter on JobName");
                   Assert.That(data.Select(r => (DateTime)r["RunDate"]), Has.All.LessThanOrEqualTo(testJob.LastRunDate), "Filter on EndRunDate");
                });
                // Take a shortcut on the rest of the filter properties by using internal method to get the Request and inspect it
                jobHistoryFilter = new JobHistoryFilter()
                {
                    MinimumRetries = 1,
                    OutcomeTypes = CompletionResult.Failed,
                    MinimumRunDuration = 10,
                    StartRunDate = testJob.DateCreated,
                    SqlSeverity = 16,
                    SqlMessageID = 100
                };
                Assert.Multiple(() =>
                {
                    var baseUrn = ServerContext.JobServer.Urn.ToString();
                    var expectedFilter = $"/History[@RetriesAttempted > 1 and @RunDuration > 10 and @RunStatus = 0 and @SqlMessageID = 100 and @SqlSeverity= 16 and @RunDate >= '{SqlSmoObject.SqlDateString(testJob.DateCreated)}']";
                    var request = jobHistoryFilter.GetEnumRequest(testJob);
                    Assert.That(request.Urn.ToString(), Is.EqualTo($"{baseUrn}/Job[@JobID= '{testJob.JobIDInternal}']{expectedFilter}"), "JobHistoryFilter Urn for specific Job");
                    request = jobHistoryFilter.GetEnumRequest(ServerContext.JobServer);
                    Assert.That(request.Urn.ToString(), Is.EqualTo($"{baseUrn}/Job{expectedFilter}"), "JobHistoryFilter Urn for JobServer");
                });
            });
        }
    }
}
