// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
namespace Microsoft.SqlServer.Test.SMO.Agent
{
    [TestClass]
    public class JobServerTests : SqlTestBase
    {
        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void JobServer_PurgeJobHistory_generates_correct_query()
        {
            ExecuteTest(() =>
            {
                var capturedSql = ServerContext.ExecutionManager.RecordQueryText(ServerContext.JobServer.PurgeJobHistory).ToSingleString();
                Assert.That(capturedSql, Is.EqualTo($"EXEC msdb.dbo.sp_purge_jobhistory{Environment.NewLine}"), "PurgeJobHistory with no filter");
                var jobHistoryFilter = new JobHistoryFilter()
                {
                    EndRunDate = new DateTime(2022, 03, 05, 11, 11, 11),
                    JobName = "my'job"
                };
                capturedSql = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.PurgeJobHistory(jobHistoryFilter)).ToSingleString();
                Assert.That(capturedSql, Is.EqualTo($"EXEC msdb.dbo.sp_purge_jobhistory  @job_name='my''job', @oldest_date='2022-03-05T11:11:11'{Environment.NewLine}"), "PurgeJobHistory with JobName and EndRunDate filter");
                jobHistoryFilter = new JobHistoryFilter()
                {
                    JobID = Guid.NewGuid(),
                    // Setting extra properties to demonstrate they are ignored by PurgeJobHistory
                    SqlSeverity = 16,
                    MinimumRetries = 20
                };
                capturedSql = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.PurgeJobHistory(jobHistoryFilter)).ToSingleString();
                Assert.That(capturedSql, Is.EqualTo($"EXEC msdb.dbo.sp_purge_jobhistory  @job_id=N'{jobHistoryFilter.JobID}'{Environment.NewLine}"), "PurgeJobHistory with JobName and EndRunDate filter");
            });
        }

        [TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SupportedServerVersionRange(MinMajor = 16, Edition = DatabaseEngineEdition.Enterprise)]
        public void JobServer_EnumPerformanceCounters_returns_agent_counters()
        {
            ExecuteTest(() =>
            {
                var data = ServerContext.JobServer.EnumPerformanceCounters();
                Assert.That(data.Columns.Cast<DataColumn>().Select(c => c.ColumnName), Is.EqualTo(new[] { "ObjectName", "CounterName", "InstanceName" }), "Column names");
                // This test will need an update if AgentPerfInfo.xml is updated
                Assert.That(data.Rows.Cast<DataRow>().ToArray(), Has.Length.EqualTo(63), "Count of perf counters");
                data = ServerContext.JobServer.EnumPerformanceCounters(objectName: "SQLAgent:Jobs");
                Assert.That(data.Rows.Cast<DataRow>().Select(r => r["ObjectName"]), Has.All.EqualTo("SQLAgent:Jobs"), "Filter on ObjectName");
                Assert.That(data.Rows.Cast<DataRow>().ToArray(), Has.Length.EqualTo(24), "Count of SQLAgent:Jobs perf counters");
                data = ServerContext.JobServer.EnumPerformanceCounters(objectName: "SQLAgent:Jobs", counterName: "Active Jobs");
                Assert.That(data.Rows.Cast<DataRow>().Select(r => r["CounterName"]), Has.All.EqualTo("Active Jobs"), "Filter on CounterName");
                Assert.That(data.Rows.Cast<DataRow>().ToArray(), Has.Length.EqualTo(4), "Count of Active Jobs perf counters");
                data = ServerContext.JobServer.EnumPerformanceCounters(objectName: "SQLAgent:Jobs", counterName: "Active Jobs", instanceName: "_Total");
                Assert.That(data.Rows.Cast<DataRow>().ToArray(), Has.Length.EqualTo(1), "Count of _Total instance for SQLAgent:Jobs Active Jobs");
                Assert.That(data.Rows[0]["InstanceName"], Is.EqualTo("_Total"), "Filter on InstanceName");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void JobServer_Msx_methods_generate_correct_queries()
        {
            ExecuteTest(() =>
            {
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.SetMsxAccount(null), "SetMsxAccount(null)");
                var pwd = Guid.NewGuid().ToString();
                var queries = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.SetMsxAccount("somecredential")).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { $"EXEC msdb.dbo.sp_msx_set_account @credential_name = N'somecredential'" }));
                queries = ServerContext.ExecutionManager.RecordQueryText(ServerContext.JobServer.ClearMsxAccount).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC msdb.dbo.sp_msx_set_account" }), "ClearMsxAccount");
                queries = ServerContext.ExecutionManager.RecordQueryText(ServerContext.JobServer.MsxDefect).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC msdb.dbo.sp_msx_defect" }), "MsxDefect");
                queries = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.MsxDefect(forceDefection:true)).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC msdb.dbo.sp_msx_defect @forced_defection = 1" }), "MsxDefect");
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.MsxEnlist(null, "somelocation"), "MsxEnlist(null, somelocation)");
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.MsxEnlist("someserver", null), "MsxEnlist(someserver, null)");
                queries = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.MsxEnlist("someserver", "somelocation")).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC msdb.dbo.sp_msx_enlist @msx_server_name = N'someserver', @location = N'somelocation'" }), "MsxEnlist");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]

        public void JobServer_ErrorLog_methods_generate_correct_queries()
        {
            ExecuteTest(() =>
            {
                var queries = ServerContext.ExecutionManager.RecordQueryText(ServerContext.JobServer.CycleErrorLog).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC msdb.dbo.sp_cycle_agent_errorlog" }), "CycleErrorLog");
                var data = ServerContext.JobServer.EnumErrorLogs();
                Assert.That(data.Columns.Cast<DataColumn>().Select(c => c.ColumnName), Is.EquivalentTo(new string[] { "Urn", "Name", "ArchiveNo", "CreateDate", "Size" }), "EnumErrorLogs table columns");
                foreach (var dataRow in data.Rows.Cast<DataRow>())
                {
                    var urn = new Urn((string)dataRow["Urn"]);
                    var logNumber = (int)dataRow["ArchiveNo"];
                    Assert.That(urn.Type, Is.EqualTo("ErrorLog"), $"Urn of agent error log {urn.Value}");
                    Assert.That(urn.Value, Contains.Substring($"JobServer/ErrorLog[@ArchiveNo='{logNumber}']"), $"Urn filters on ArchiveNo {urn.Value}");
                }
                data = ServerContext.JobServer.ReadErrorLog();
                Assert.That(data.Columns.Cast<DataColumn>().Select(c => c.ColumnName), Is.EquivalentTo(new string[] { "LogDate", "ErrorLevel", "Text" }), "ReadErrorLog table columns");
            });
        }

        private Job CreateJob(string loginName = null)
        {

            var jobName = SmoObjectHelpers.GenerateUniqueObjectName("agentjob");
            var job = new Job(ServerContext.JobServer, jobName);
            if (loginName != null)
            {
                ServerContext.CreateLogin(loginName, LoginType.SqlLogin, Guid.NewGuid().ToString());
                job.OwnerLoginName = loginName;
            }
            var jobStep = new JobStep(job,
                    SmoObjectHelpers.GenerateUniqueObjectName("jbstep"))
            {
                Command = "SELECT 1",
                OnSuccessAction = StepCompletionAction.QuitWithSuccess,
                OnFailAction = StepCompletionAction.QuitWithFailure,
                SubSystem = AgentSubSystem.TransactSql
            };
            job.JobSteps.Add(jobStep);
            job.Create();
            job.Refresh();
            return job;
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void JobServer_Job_methods()
        {
            ExecuteTest(() =>
            {
                var job = CreateJob();
                var jobName = job.Name;
                var jobId = job.JobID;
                ServerContext.JobServer.Jobs.ClearAndInitialize("", Enumerable.Empty<string>());
                job = ServerContext.JobServer.GetJobByID(jobId);
                Assert.That(job.Name, Is.EqualTo(jobName), "Job returned by GetJobIdID");
                ServerContext.JobServer.RemoveJobByID(jobId);
                Assert.That(job.State, Is.EqualTo(SqlSmoState.Dropped), "Job should be marked Dropped by RemoveJobByID");
                Assert.That(ServerContext.JobServer.GetJobByID(jobId), Is.Null, "Dropped Job should be removed from collection");
                ServerContext.JobServer.Jobs.ClearAndInitialize($"[(@Name = '{Urn.EscapeString(jobName)}')]", Enumerable.Empty<string>());
                Assert.That(ServerContext.JobServer.Jobs.Cast<Job>().Select(j => j.Name), Is.Empty, "Job should have been dropped by RemoveJobByID");
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.RemoveJobsByLogin(null), "RemoveJobsByLogin(null)");
                var queries = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.RemoveJobsByLogin("somelogin")).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { $"EXEC msdb.dbo.sp_manage_jobs_by_login @action = N'DELETE', @current_owner_login_name = N'somelogin'" }));
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.ReassignJobsByLogin(null, "newLogin"), "ReassignJobsByLogin(null, newlogin)");
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.ReassignJobsByLogin("oldLogin", null), "ReassignJobsByLogin(oldLogin, null)");
                queries = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.ReassignJobsByLogin("oldLogin", "newLogin")).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC msdb.dbo.sp_manage_jobs_by_login @action = N'REASSIGN', @current_owner_login_name = N'oldLogin', @new_owner_login_name = N'newLogin'" }),
                    "ReassignJobsByLogin");
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.DropJobsByLogin(null), "DropJobsByLogin(null)");
                var loginName = SmoObjectHelpers.GenerateUniqueObjectName("agentlogin");
                job = CreateJob(loginName);
                ServerContext.JobServer.Jobs.Refresh();
                try
                {
                    Assert.That(ServerContext.JobServer.Jobs.Cast<Job>().Select(j => j.Name), Has.Member(job.Name), "Job exists before Drop");
                    ServerContext.JobServer.DropJobsByLogin(job.OwnerLoginName);
                    Assert.That(ServerContext.JobServer.Jobs.Cast<Job>().Select(j => j.Name), Has.None.EqualTo(job.Name), "Job exists before Drop");

                }
                finally
                {
                    ServerContext.Logins.Refresh();
                    ServerContext.Logins[loginName].Drop();
                }
                Assert.Throws<ArgumentNullException>(() => ServerContext.JobServer.DropJobsByServer(null), "DropJobsByServer(null)");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void JobServer_miscellaneous_methods_generate_correct_queries()
        {
            ExecuteTest(() =>
            {
                var data = ServerContext.JobServer.EnumSubSystems();
                var columns = data.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                Assert.That(columns, 
                    Is.EquivalentTo(new string[] { "subsystem", "description", "subsystem_dll", "agent_exe", "start_entry_point", "event_entry_point", "stop_entry_point", "max_worker_threads", "subsystem_id" }), 
                    "Subsystem column names");
                Assert.That(data.Rows.Cast<DataRow>().Select(r => (string)r["subsystem"]), Is.SupersetOf(new string[] { "TSQL", "Snapshot", "LogReader", "Distribution", "Merge", "QueueReader" }),
                    "Expected set of agent subsystems");
                var queries = ServerContext.ExecutionManager.RecordQueryText(() => ServerContext.JobServer.StartMonitor(string.Empty, 1)).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC master.dbo.xp_sqlagent_monitor N'START', N'', 1" }), "StartMonitor(empty, 1)");
                queries = ServerContext.ExecutionManager.RecordQueryText(ServerContext.JobServer.StopMonitor).Cast<string>();
                Assert.That(queries, Is.EqualTo(new string[] { "EXEC master.dbo.xp_sqlagent_monitor N'STOP'" }), "StopMonitor");

            });
        }
    }
}
