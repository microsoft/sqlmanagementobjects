// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    [TestClass]
    public class ServerSmoTests : SqlTestBase
    {
        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Express)]
        public void Server_DetachDatabase_and_AttachDatabase_round_trip()
        {
            ExecuteTest(() =>
            {
                Database db = ServerContext.CreateDatabaseWithRetry("DetachAttach");
                var dbName = db.Name;
                try
                {
                    ServerContext.SetDefaultInitFields(typeof(LogFile), nameof(LogFile.FileName));
                    ServerContext.SetDefaultInitFields(typeof(DataFile), nameof(DataFile.IsPrimaryFile), nameof(DataFile.FileName));
                    var dbFiles = db.FileGroups.OfType<FileGroup>().SelectMany(fg => fg.Files.OfType<DataFile>()).ToList();
                    var logFileNames = db.LogFiles.OfType<LogFile>().Select(logFile => logFile.FileName).ToList();
                    var primaryFileName = dbFiles.Single(f => f.IsPrimaryFile).FileName;
                    var dbFileNames = dbFiles.Select(dbFile => dbFile.FileName);
                    var filesToAttach = new StringCollection();
                    filesToAttach.AddRange(dbFileNames.Union(logFileNames).ToArray());
                    Assert.Throws<FailedOperationException>(() => ServerContext.AttachDatabase(dbName, filesToAttach), "AttachDatabase with existing name");
                    Assert.DoesNotThrow(() => db.Parent.DetachDatabase(dbName, false));
                    var detachedDataFiles = ServerContext.EnumDetachedDatabaseFiles(primaryFileName).OfType<string>();
                    var detachedLogFiles = ServerContext.EnumDetachedLogFiles(primaryFileName).OfType<string>();
                    Assert.Multiple(() =>
                    {
                        // We can't call methods on the original db object after this point because it is informed of being dropped
                        Assert.Throws<FailedOperationException>(db.Refresh, "Refresh for detached db");
                        var dataTable = new DataTable();
                        Assert.DoesNotThrow(() => dataTable = ServerContext.DetachedDatabaseInfo(primaryFileName), "Server.DetachedDatabaseInfo(primaryFileName)");
                        var rows = dataTable.Rows.Cast<DataRow>().ToList();
                        Assert.That(rows, Is.Not.Empty, "DetachedDatabaseInfo should return non-empty DataTable");
                        Trace.TraceInformation("--- Dumping DetachDatabaseInfo table");
                        var columns = dataTable.Columns.Cast<DataColumn>().Select(col => $"{col.ColumnName,-30}");
                        Assert.That(dataTable.Columns.Cast<DataColumn>().Select(c => c.ColumnName), Is.EqualTo(new[] { "Property", "Value" }), "DetachDatabaseInfo Column names");
                        Trace.TraceInformation($"\t{string.Join("", columns)}");
                        foreach (var row in rows)
                        {
                            var fields = columns.Select(col => $"{row[col.TrimEnd(' ')],-30}");
                            Trace.TraceInformation($"\t{string.Join("", fields)}");
                        }
                        Assert.That(dbFileNames, Is.EquivalentTo(detachedDataFiles), "EnumDetachedDatabaseFiles returns all files from the detached database");
                        Assert.That(logFileNames, Is.EquivalentTo(detachedLogFiles), "EnumDetachedLogFiles returns all log files from the detached database");
                        Assert.DoesNotThrow(() => ServerContext.AttachDatabase(dbName, filesToAttach), $"AttachDatabase({dbName},{string.Join(",", filesToAttach)}");
                        ServerContext.Databases.Refresh();
                        Assert.DoesNotThrow(() => db = ServerContext.Databases[dbName]);
                        Assert.DoesNotThrow(() => db.EnumDatabasePermissions(), "Attached database EnumDatabasePermissions");
                    });
                }
                finally
                {
                    ServerContext.DropKillDatabaseNoThrow(dbName);
                }
            });
        }

        [TestMethod]
        public void Server_DetachDatabase_and_AttachDatabase_error_handling()
        {
            ExecuteTest(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.Throws<ArgumentNullException>(() => ServerContext.DetachDatabase(null, true, true), "DetachDatabase(null, true, true)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.DetachDatabase(null, true), "DetachDatabase(null, true)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase(null, new StringCollection()), "AttachDatabase(null, stringCollection)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase(null, new StringCollection(), AttachOptions.NewBroker), "AttachDatabase(null, stringCollection, attachOptions)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase("name", null, AttachOptions.None), "AttachDatabase(name, null, stringCollection)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase("name", new StringCollection(), null), "AttachDatabase(name, stringCollection, null)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase("name", null, "owner"), "AttachDatabase(name, null, owner)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase("name", new StringCollection(), null, AttachOptions.RebuildLog), "AttachDatabase(name, stringCollection, null, attachOptions)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.AttachDatabase("name", null, "owner", AttachOptions.RebuildLog), "AttachDatabase(name, null, owner, attachOptions)");
                    var foe = Assert.Throws<FailedOperationException>(() => ServerContext.AttachDatabase("", new StringCollection() { "filename" }), "AttachDatabase with empty db name");
                    Assert.That(foe.InnerException, Is.InstanceOf<ArgumentException>(), "InnerException for empty db name");
                    foe = Assert.Throws<FailedOperationException>(() => ServerContext.AttachDatabase("db" + Guid.NewGuid().ToString(), new StringCollection()), "AttachDatabase with empty file list");
                    Assert.That(foe.InnerException, Is.InstanceOf<ArgumentException>().Or.InstanceOf<Management.Common.ConnectionFailureException>(), "InnerException for empty file list");
                    foe = Assert.Throws<FailedOperationException>(() => ServerContext.AttachDatabase("db" + Guid.NewGuid().ToString(), new StringCollection() { "filename" }, owner: ""), "AttachDatabase with empty owner");
                    Assert.That(foe.InnerException, Is.InstanceOf<ArgumentException>().Or.InstanceOf<Management.Common.ConnectionFailureException>(), "InnerException for empty owner");
                });
            });
        }

        [TestMethod]
        public void Server_EnumMembers_returns_valid_list()
        {
            ExecuteFromDbPool((db) =>
            {
                var roleNames = Enumerable.Empty<string>();
                var smoServer = new Management.Smo.Server(ServerContext.ConnectionContext.GetDatabaseConnection(db.Name, poolConnection: false));
                Assert.DoesNotThrow(() => roleNames = smoServer.EnumMembers(RoleTypes.All).Cast<string>(), "EnumMembers(All)");
                Assert.That(roleNames, Is.Not.Empty, "EnumMembers returns non-empty list");
            });
        }

        /// <summary>
        /// This test deletes backup history and relies on a scavenger job on the server to remove the old bak files.
        /// It also gets in coverage of Database methods to enumerate backup sets and files
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise, HostPlatform = Management.Common.HostPlatformNames.Windows)]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Express)]
        public void Server_DeleteBackupHistory_succeeds()
        {
            ExecuteFromDbPool((db) =>
            {
                db.TakeFullBackup();
                var backupSetsDataTable = db.EnumBackupSets();
                Assert.That(backupSetsDataTable.Rows.Cast<DataRow>(), Is.Not.Empty, "Backing up the database didn't create a backup set");
                var backupSetFiles = db.EnumBackupSetFiles();
                Assert.That(backupSetFiles.Rows.Cast<DataRow>(), Is.Not.Empty, "EnumBackupSetFiles()");
                var backupSetId = (int)backupSetsDataTable.Rows[0]["ID"];
                backupSetFiles = db.EnumBackupSetFiles(backupSetId);
                Assert.That(backupSetFiles.Rows.Cast<DataRow>(), Is.Not.Empty, "EnumBackupSetFiles(backupSetId)");
                ServerContext.DeleteBackupHistory(db.Name);
                backupSetsDataTable = db.EnumBackupSets();
                Assert.That(backupSetsDataTable.Rows.Cast<DataRow>(), Is.Empty, "EnumBackupSets after DeleteBackupHistory(db.Name)");
                db.TakeFullBackup();
                backupSetsDataTable = db.EnumBackupSets();
                var mediaSetId = (int)backupSetsDataTable.Rows[0]["MediaSetId"];
                ServerContext.DeleteBackupHistory(mediaSetId);
                backupSetsDataTable = db.EnumBackupSets();
                Assert.That(backupSetsDataTable.Rows.Cast<DataRow>(), Is.Empty, "EnumBackupSets after DeleteBackupHistory(mediaSetId)");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Express)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedFeature(SqlFeature.NoDropCreate)] // Requires GetDatabaseConnection to return a new connection
        public void Server_KillAllProcesses_succeeds()
        {
            ExecuteFromDbPool((db) =>
            {
                var sqlConnectionDb = ServerContext.ConnectionContext.GetDatabaseConnection(db.Name, poolConnection: false);
                var spid = sqlConnectionDb.ProcessID;
                Assert.That(spid, Is.Not.Zero, "database connection isn't open");
                var executingSpid = db.ExecutionManager.ConnectionContext.ProcessID;
                Assert.That(executingSpid, Is.Not.Zero, "connection isn't open");
                db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteAndCaptureSql;
                try
                {
                    // For Azure the call has to be on a connection directly to the user database
                    new Management.Smo.Server(db.ExecutionManager.ConnectionContext).KillAllProcesses(db.Name);
                    var scriptLines = db.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>().ToList();
                    Assert.Multiple(() =>
                    {
                        Assert.That(scriptLines, Has.Member($"BEGIN TRY KILL {spid} END TRY BEGIN CATCH PRINT '{spid} is not active or could not be killed' END CATCH"), "KillAllProcesses should kill open connection");
                        Assert.That(scriptLines, Has.No.Member($"BEGIN TRY KILL {executingSpid} END TRY BEGIN CATCH PRINT '{executingSpid} is not active or could not be killed' END CATCH"), "KillAllProcesses should not kill connection making the call");
                    });
                }
                finally
                {
                    db.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    sqlConnectionDb.Disconnect();
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        public void Server_EnumPerformanceCounters_retrieves_counter_data()
        {
            ExecuteTest(() =>
            {
                Assert.Multiple(() =>
                {
                    Assert.Throws<ArgumentNullException>(() => ServerContext.EnumPerformanceCounters(null), "EnumPerformanceCounters(null)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.EnumPerformanceCounters(null, "counterName"), "EnumPerformanceCounters(null, counterName)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.EnumPerformanceCounters("objectName", null), "EnumPerformanceCounters(objectName, null)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.EnumPerformanceCounters("objectName", "counterName", null), "EnumPerformanceCounters(objectName, counterName, null)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.EnumPerformanceCounters("objectName", null, "instanceName"), "EnumPerformanceCounters(objectName, null, instanceName)");
                    Assert.Throws<ArgumentNullException>(() => ServerContext.EnumPerformanceCounters(null, "counterName", "instanceName"), "EnumPerformanceCounters(null, counterName, instanceName)");
                });
                DataTable dataTableCounters = new DataTable();
                Assert.That(() => dataTableCounters = ServerContext.EnumPerformanceCounters("bogusObjectName"), Throws.Nothing, "EnumPerformanceCounters(bogusObjectName)");
                Assert.That(dataTableCounters.Rows.Cast<DataRow>(), Is.Empty, "EnumPerformanceCounters(bogusName) should return no data");
                Assert.That(() => dataTableCounters = ServerContext.EnumPerformanceCounters(), Throws.Nothing, "EnumPerformanceCounters()");
                var columns = dataTableCounters.Columns.Cast<DataColumn>().Select(c => c.ColumnName);
                Assert.That(columns, Is.EqualTo(new[] { "ObjectName", "CounterName", "InstanceName" }), "Unexpected columns");
                var allRows = dataTableCounters.Rows.Cast<DataRow>().ToList();
                Assert.That(allRows, Is.Not.Empty, "EnumPerformanceCounters should return data");
                var objectName = (string)allRows[0]["ObjectName"];
                Assert.That(() => dataTableCounters = ServerContext.EnumPerformanceCounters(objectName), Throws.Nothing, "EnumPerformanceCounters(objectName)");
                var expectedCounters = allRows.Where(r => ((string)r["ObjectName"]).Equals(objectName)).Select(r => (string)r["CounterName"]);
                var actualCounters = dataTableCounters.Rows.Cast<DataRow>().Select(r => (string)r["CounterName"]);
                Assert.That(actualCounters, Is.EquivalentTo(expectedCounters), $"Counters for {objectName} server-side filter should match a client-side filter");
                var rowWithInstance = allRows.FirstOrDefault(r => ((string)r["InstanceName"]) != null);
                if (rowWithInstance != null)
                {
                    Assert.That(() => dataTableCounters = ServerContext.EnumPerformanceCounters((string)rowWithInstance["ObjectName"], (string)rowWithInstance["CounterName"], (string)rowWithInstance["InstanceName"]), Throws.Nothing, "EnumPerformanceCounters(objectName, counterName, instanceName");
                    // the instance may have disappeared since we queried the first time
                    Assert.That(dataTableCounters.Rows.Cast<DataRow>().ToList(), Is.Empty.Or.Count.EqualTo(1), "At most 1 row should be returned for an object/counter/instance combo");
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 15, Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_MaximumMemoryGrantPercentageAsDouble_Is_Scripted()
        {
            this.ExecuteTest(
                server =>
                {
                    try
                    {
                        var resourcegovernor = server.ResourceGovernor;
                        var resourcePool = new ResourcePool(resourcegovernor, "ResourceOffline_");
                        var workloadGroup = new WorkloadGroup(resourcePool, "WorkloadOffline_") { RequestMaximumMemoryGrantPercentageAsDouble = 0.5 };
                        // We don't actually want to create a Workload on the server, we only want to make sure double (or floating point) values are accepted.
                        server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                        resourcePool.Create();
                        workloadGroup.Create();
                        var commands = server.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                        string expected = $"CREATE WORKLOAD GROUP [WorkloadOffline_] WITH(request_max_memory_grant_percent=0.5) USING [ResourceOffline_]";
                        Assert.That(commands, Has.Member(expected), "Invalid Query to Create Workload Group");
                    }
                    finally
                    {
                        server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    }
                });
        }

        //Method added to check legacy version of SQL Server (2017 and below), where "request_max_memory_grant_percent" is Int in WorkloadGroup.
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 14, Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_MaximumMemoryGrantPercentage_Is_Scripted()
        {
            this.ExecuteTest(
                server =>
                {
                    try
                    {
                        var resourcegovernor = server.ResourceGovernor;
                        var resourcePool = new ResourcePool(resourcegovernor, "ResourceOffline_");
                        var workloadGroup = new WorkloadGroup(resourcePool, "WorkloadOffline_") { RequestMaximumMemoryGrantPercentage = 42 };
                        server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                        resourcePool.Create();
                        workloadGroup.Create();
                        var commands = server.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                        string expected = $"CREATE WORKLOAD GROUP [WorkloadOffline_] WITH(request_max_memory_grant_percent=42) USING [ResourceOffline_]";
                        Assert.That(commands, Has.Member(expected), "Invalid Query to Create Workload Group");
                    }
                    finally
                    {
                        server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    }
                });
        }

        //Method added to test Alter method in WorkloadGroup for decimal property "request_max_memory_grant_percent_numeric".
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_Alter()
        {
            this.ExecuteTest(
                server =>
                {
                    try
                    {
                        server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.CaptureSql;
                        //Test alter for RequestMaximumMemoryGrantPercentage
                        ResourcePool defaultPool = server.ResourceGovernor.ResourcePools["default"];
                        WorkloadGroup group = defaultPool.WorkloadGroups["default"];
                        group.RequestMaximumMemoryGrantPercentage = 2;
                        string expected = $"ALTER WORKLOAD GROUP [default] WITH(request_max_memory_grant_percent=2)";
                        group.Alter();
                        var commands = server.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                        Assert.That(commands, Has.Member(expected), "Invalid Query to Alter Workload Group for RequestMaximumMemoryGrantPercentage");
                        group.Refresh();
                        //Test alter for RequestMaximumMemoryGrantPercentageAsDouble
                        if (group.IsSupportedProperty("RequestMaximumMemoryGrantPercentageAsDouble"))
                        {
                            group.RequestMaximumMemoryGrantPercentageAsDouble = 0.5;
                            expected = $"ALTER WORKLOAD GROUP [default] WITH(request_max_memory_grant_percent=0.5)";
                            group.Alter();
                            commands = server.ExecutionManager.ConnectionContext.CapturedSql.Text.Cast<string>();
                            Assert.That(commands, Has.Member(expected), "Invalid Query to Alter Workload Group for RequestMaximumMemoryGrantPercentageAsDouble");
                        }
                    }
                    finally
                    {
                        server.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    }
                });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse)]
        [TestCategory("Legacy")]
        public void Server_EnumLocks()
        {
            ExecuteTest(() =>
            {
                var allLocks = ServerContext.EnumLocks();
                var colNames = allLocks.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                Assert.That(colNames, Is.EquivalentTo(new[] { "RequestorSpid", "LockType", "Database", "Schema", "Table", "Index", "Status", }), "Columns from EnumLocks");
                var allRows = allLocks.Rows.Cast<DataRow>().ToList();
                if (allRows.Any())
                {
                    Trace.TraceInformation("Calling EnumLocks(processId)");
                    var spid = (int)allRows[0]["RequestorSpid"];
                    var spidLocks = ServerContext.EnumLocks(spid);
                    var spidRows = spidLocks.Rows.Cast<DataRow>().ToList();
                    Assert.That(spidRows.Select(r => r["RequestorSpid"]), Has.All.EqualTo(spid), "EnumLocks(requestorSpid) should filter to the matching spid");
                }
                else
                {
                    Trace.TraceInformation("No locks returned by EnumLocks()");
                }

            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = HostPlatformNames.Windows)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Express, HostPlatform = HostPlatformNames.Windows)]
        public void Server_EnumGroups()
        {
            ExecuteTest(() =>
            {
                var groups = ServerContext.EnumWindowsDomainGroups();
                var colNames = groups.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                Assert.That(colNames, Is.EquivalentTo(new[] { "Name", "Description" }), "Columns from EnumWindowsDomainGroups");
                var allRows = groups.Rows.Cast<DataRow>().ToList();
                Assert.That(allRows.Select(r => r.Field<string>("Name")), Has.Member("Administrators"), "EnumWindowsDomainGroups should have Administrators");
                var userDomain = Environment.UserDomainName;
                Assert.That(() => ServerContext.EnumWindowsDomainGroups(userDomain), Throws.Nothing, $"EnumWindowsDomainGroups({userDomain}");
                groups = ServerContext.EnumWindowsGroupInfo();
                colNames = groups.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                Assert.That(colNames, Is.EquivalentTo(new[] { "account name", "type", "privilege", "mapped login name", "permission path" }), "Columns from EnumWindowsGroupInfo");
                allRows = groups.Rows.Cast<DataRow>().ToList();
                Assert.That(allRows.Count, Is.AtLeast(1), "EnumWindowsGroupInfo should return at least 1 row");
                Assert.That(allRows.Select(r => r.Field<string>("type")), Has.All.EqualTo("group"), "EnumWindowsGroupInfo should return only group type");
                // We can call the other variants of EnumWindowsGroupInfo but we can't rely on them returning any data, and they could throw if SQL can't talk to active directory
                // if we specify one of the accounts in the list. So we use BUILTIN\Administrators and just make sure it doesn't throw.
                Assert.That(() => ServerContext.EnumWindowsGroupInfo(@"BUILTIN\Administrators"), Throws.Nothing, "EnumWindowsGroupInfo(account)");
                Assert.That(() => ServerContext.EnumWindowsGroupInfo(@"BUILTIN\Administrators", listMembers: true), Throws.Nothing, "EnumWindowsGroupInfo(account, true)");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = HostPlatformNames.Windows)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Express, HostPlatform = HostPlatformNames.Windows)]
        public void Server_EnumUserInfo()
        {
            ExecuteTest(() =>
            {
                var users = ServerContext.EnumWindowsUserInfo();
                var colNames = users.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                Assert.That(colNames, Is.EquivalentTo(new[] { "account name", "type", "privilege", "mapped login name", "permission path" }), "Columns from EnumWindowsUserInfo");
                var allRows = users.Rows.Cast<DataRow>().ToList();
                Assert.That(allRows.Count, Is.AtLeast(1), "EnumWindowsUserInfo should return at least 1 row");
                Assert.That(allRows.Select(r => r.Field<string>("type")), Has.All.EqualTo("user"), "EnumWindowsUserInfo should return only user type");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, HostPlatform = HostPlatformNames.Windows)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Express, HostPlatform = HostPlatformNames.Windows)]
        public void Server_EnumAvailableMedia()
        {
            ExecuteTest(() =>
            {
                var media = ServerContext.EnumAvailableMedia();
                var colNames = media.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
                Assert.That(colNames, Is.EquivalentTo(new[] { "Name", "LowFree", "HighFree", "MediaTypes" }), "Columns from EnumAvailableMedia");
                var allRows = media.Rows.Cast<DataRow>().ToList();
                Assert.That(allRows.Count, Is.AtLeast(1), "EnumAvailableMedia should return at least 1 row");
                var mediaType = allRows[0].Field<MediaTypes>("MediaTypes");
                allRows = ServerContext.EnumAvailableMedia(mediaType).Rows.Cast<DataRow>().ToList();
                Assert.That(allRows.Count, Is.AtLeast(1), $"EnumAvailableMedia({mediaType} should return at least 1 row");
                Assert.That(allRows.Select(r => r.Field<MediaTypes>("MediaTypes")), Has.All.EqualTo(mediaType), $"EnumAvailableMedia({mediaType}");
                allRows = ServerContext.EnumAvailableMedia(MediaTypes.All & ~mediaType).Rows.Cast<DataRow>().ToList();
                Assert.That(allRows.Select(r => r.Field<MediaTypes>("MediaTypes")), Has.None.EqualTo(mediaType), $"EnumAvailableMedia({MediaTypes.All & ~mediaType}");
                Assert.That(() => ServerContext.EnumAvailableMedia(MediaTypes.SharedFixedDisk), Throws.Nothing, "EnumAvailableMedia(MediaTypes.SharedFixedDisk)");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse)]
        public void Server_EnumServerAttributes()
        {
            ExecuteTest(() =>
            {
                DataTable data = null;
                Assert.That(() => data = ServerContext.EnumServerAttributes(), Throws.Nothing, "EnumServerAttributes");
                Assert.That(data, Is.Not.Null, "EnumServerAttributes returned null");
                Assert.That(data.Rows.Count, Is.AtLeast(1), "EnumServerAttributes should return data");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse)]
        public void Server_TraceFlags()
        {
            ExecuteTest(() =>
            {
                DataTable data = null;
                Assert.That(() => data = ServerContext.EnumActiveGlobalTraceFlags(), Throws.Nothing, "EnumActiveGlobalTraceFlags");
                Assert.That(data, Is.Not.Null, "EnumActiveGlobalTraceFlags returned null");
                if (data.Rows.Count > 0)
                {
                    var tf = data.Rows[0].Field<int>("TraceFlag");
                    Assert.That(ServerContext.IsTraceFlagOn(tf, isGlobalTraceFlag: true), Is.True, $"TraceFlag {tf} is on");
                    Assert.That(ServerContext.IsTraceFlagOn(tf, isGlobalTraceFlag: false), Is.False, $"TraceFlag {tf} is on globally");
                }
                // Azure doesn't allow trace flags, and MI requires global trace flags per https://docs.microsoft.com/en-us/sql/t-sql/database-console-commands/dbcc-traceon-transact-sql?view=sql-server-ver15
                if (ServerContext.DatabaseEngineType != DatabaseEngineType.SqlAzureDatabase && ServerContext.DatabaseEngineEdition != DatabaseEngineEdition.SqlManagedInstance)
                {
                    ServerContext.SetTraceFlag(1800, isOn: true);
                    Assert.That(ServerContext.IsTraceFlagOn(1800, isGlobalTraceFlag: false), Is.True, $"TraceFlag 1800 is on");
                    // I opened https://github.com/MicrosoftDocs/sql-docs/issues/4920 because there doesn't seem to be 
                    // a difference between dbcc tracestatus() and dbcc tracestatus (-1)
                    // Should we change the IsTraceFlagOn implementation so it returns false for a session-only flag when isGlobalTraceFlag param is true?
                    Assert.That(ServerContext.IsTraceFlagOn(1800, isGlobalTraceFlag: true), Is.True, $"TraceFlag 1800 is on the session");
                    ServerContext.SetTraceFlag(1800, isOn: false);
                    Assert.That(ServerContext.IsTraceFlagOn(1800, isGlobalTraceFlag: false), Is.False, $"TraceFlag 1800 is off");
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 11)]
        public void Server_EnumDirectories_returns_valid_content()
        {
            ExecuteTest(() =>
            {
                var inputFolder = ServerContext.DefaultFile.TrimEnd('\\', '/');
                var parentFolder = PathWrapper.GetDirectoryName(inputFolder);
                var folder = System.IO.Path.GetFileName(inputFolder);
                var data = ServerContext.EnumDirectories(parentFolder);

                var folders = data.Rows.Cast<DataRow>().Select(r => r["Name"]);
                Assert.That(folders, Has.Member(folder), $"EnumDirectories {parentFolder} contents");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDataWarehouse)]
        public void Server_GetLCIDCollation_returns_correct_LCID()
        {
            ExecuteTest(() =>
            {
                var japaneseUnicode = ServerContext.GetLCIDCollation("Japanese_Unicode_CI_AI");
                var french = ServerContext.GetLCIDCollation("French_100_CS_AS");
                var fake = ServerContext.GetLCIDCollation("fake");
                Assert.Multiple(() =>
                {
                    Assert.That(japaneseUnicode, Is.EqualTo(1041), "Japanese_Unicode_CI_AI");
                    Assert.That(french, Is.EqualTo(1036), "French_100_CS_AS");
                    Assert.That(fake, Is.EqualTo(1033), "fake");
                });
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 14)]
        public void Server_Drives_are_enumerable()
        {
            ExecuteTest(() =>
            {
                var req = new Request()
                {
                    Urn = "Server/Drive",
                    Fields = new[] { "Name", "Size" }
                };
                var actual = ((DataSet)(new Enumerator().Process(ServerContext.ConnectionContext, req)))
                   .Tables[0]
                   .Rows.OfType<DataRow>()
                   .Select(row => new { Name = (string)row["Name"], Size = (Int64)row["Size"] });
                var expected = ServerContext.ConnectionContext.ExecuteWithResults("select fixed_drive_path Name, free_space_in_bytes/(1024*1024) Size from sys.dm_os_enumerate_fixed_drives")
                    .Tables[0]
                   .Rows.OfType<DataRow>()
                   .Select(row => new { Name = (string)row["Name"], Size = (Int64)row["Size"] });
                Assert.That(actual.Select(a => a.Name), Is.EqualTo(expected.Select(a => a.Name)), "Drive names");
                Assert.That(actual.Select(a => a.Size), Is.EqualTo(expected.Select(a => a.Size)), "Drive sizes");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Server_SetDefaultInitFields_allows_unsupported_properties()
        {
            ExecuteWithDbDrop(db =>
            {
                db.CreateTable("t1");
                var properties = new string[] { nameof(Table.Name), nameof(Table.LedgerType), nameof(Table.DwTableDistribution), nameof(Table.DataRetentionEnabled), "SomeFakeProperty" };
                ServerContext.SetDefaultInitFields(typeof(Table), properties);
                db.Tables.ClearAndInitialize("", Enumerable.Empty<string>());
                var missingProperties = new MissingProperties();
                SqlSmoObject.PropertyMissing += missingProperties.OnPropertyMissing;
                try
                {
                    Trace.TraceInformation($"Number of tables: {db.Tables.Count}");
                    foreach (Table table in db.Tables) 
                    {
                        Trace.TraceInformation(table.Name);
                        if (table.IsSupportedProperty(nameof(Table.LedgerType)))
                        {
                            Trace.TraceInformation($"LedgerType: {table.LedgerType}");
                        }
                        if (table.IsSupportedProperty(nameof(Table.DwTableDistribution)))
                        {
                            Trace.TraceInformation($"DwTableDistribution : {table.DwTableDistribution}");
                        }
                        if (table.IsSupportedProperty(nameof(Table.DataRetentionEnabled)))
                        {
                            Trace.TraceInformation($"DataRetentionEnabled: {table.DataRetentionEnabled}");
                        }
                    }
                }
                finally
                {
                    SqlSmoObject.PropertyMissing -= missingProperties.OnPropertyMissing;
                    ServerContext.SetDefaultInitFields(allFields:false);
                }
                Assert.That(missingProperties.Properties, Is.Empty, "Properties should have been fetched in the initial query");
            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Server_SetDefaultInitFields_allfields_avoids_missing_property_events()
        {
            ExecuteWithDbDrop(db =>
            {
                db.Parent.SetDefaultInitFields(true);
                var t = db.CreateTable("t1");
                var missingProperties = new MissingProperties();
                SqlSmoObject.PropertyMissing += missingProperties.OnPropertyMissing;
                db.Tables.ClearAndInitialize(null, Enumerable.Empty<string>());
                try
                {
                    foreach (var table in db.Tables.Cast<Table>())
                    {
                        if (table.IsSupportedProperty(nameof(table.DwTableDistribution)))
                        {
                            Trace.TraceInformation($"DwTableDistribution {table.DwTableDistribution}");
                        }
                        if (table.IsSupportedProperty(nameof(table.LedgerType)))
                        {
                            Trace.TraceInformation($"LedgerType {table.LedgerType}");
                        }
                        if (table.IsSupportedProperty(nameof(table.DataRetentionPeriod)))
                        {
                            Trace.TraceInformation($"DataRetentionPeriod {table.DataRetentionPeriod}");
                        }
                        Trace.TraceInformation($"TextFileGroup {table.TextFileGroup}");
                        if (t.Name == table.Name)
                        {
                            t = table;
                        }
                    }
                    foreach (var col in t.Columns.Cast<Column>())
                    {
                        Trace.TraceInformation($"Default {col.Default}");
                        if (col.IsSupportedProperty(nameof(col.DistributionColumnName)))
                        {
                            Trace.TraceInformation($"DistributionColumnName {col.DistributionColumnName}");
                        }
                        if (col.IsSupportedProperty(nameof(col.IsFullTextIndexed)))
                        {
                            Trace.TraceInformation($"IsFullTextIndexed {col.IsFullTextIndexed}");
                        }
                    }
                    Assert.That(missingProperties.Properties, Is.Empty, "All fields should have been fetched when populating collections");
                }
                finally
                {
                    SqlSmoObject.PropertyMissing -= missingProperties.OnPropertyMissing;
                    ServerContext.SetDefaultInitFields(allFields: false);
                }

            });
        }

        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void Server_GetSmoObject_succeeds_if_Databases_collection_is_empty()
        {
            ExecuteWithDbDrop(db => 
            {
                var table = db.CreateTable("table");
                using (var sqlConnection = new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString))
                {
                    var server = new Management.Smo.Server(new ServerConnection(sqlConnection));
                    var foundTable = (Table)server.GetSmoObject(table.Urn);
                    Assert.That(foundTable.Name, Is.EqualTo(table.Name), $"Didn't find the table with urn {table.Urn}");
                }
            });
        }
        class MissingProperties
        {
            public readonly IList<string> Properties = new List<string>();
            private readonly int threadId = Thread.CurrentThread.ManagedThreadId;
            public void OnPropertyMissing(object sender, PropertyMissingEventArgs args)
            {
                if (Thread.CurrentThread.ManagedThreadId == threadId)
                {
                    Properties.Add($"{args.TypeName}.{args.PropertyName}");
                }
            }
        }
    }
}
