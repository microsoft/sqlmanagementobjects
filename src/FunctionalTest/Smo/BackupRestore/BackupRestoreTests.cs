// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.BackupRestore
{
    [TestClass]
    public class BackupRestoreTests : SqlTestBase
    {
        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void PageRestorePlanner_restores_database_page()
        {
            ExecuteWithDbDrop(db =>
            {
                if (db.RecoveryModel != RecoveryModel.Full)
                {
                    db.RecoveryModel = RecoveryModel.Full;
                    db.Alter();
                }
                var table = db.CreateTable("tbl");
                table.InsertDataToTable(1000);
                db.TakeFullBackup();
                // we have to fake some suspect pages
                var fileId = db.FileGroups["PRIMARY"].Files[0].ID;
                ServerContext.ConnectionContext.ExecuteNonQuery($"insert into msdb.dbo.suspect_pages(database_id, file_id, page_id, event_type, error_count) values({db.ID}, {fileId}, 10, 1, 1 )");
                try
                {
                    var planner = new PageRestorePlanner(db)
                    {
                        TailLogBackupFile = Path.Combine(db.PrimaryFilePath, $"PageRestore{Guid.NewGuid()}.log")
                    };
                    Assert.That(planner.SuspectPages.Select(p => (p.FileID, p.PageID)), Is.EquivalentTo(new[] { (fileId, 10) }), "planner.SuspectPages");
                    var plan = planner.CreateRestorePlan();
                    plan.Execute();
                    var suspectPages = PageRestorePlanner.GetSuspectPages(db);
                    Assert.That(suspectPages, Is.Empty, "GetSuspectPages after plan.Execute");
                }
                finally
                {
                    this.ServerContext.ConnectionContext.ExecuteNonQuery($"delete from msdb.dbo.suspect_pages where database_id = {db.ID}");
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise)]
        public void DatabaseRestorePlanner_restores_database()
        {
            ExecuteWithDbDrop(db =>
            {
                var table = db.CreateTable("tbl");
                table.InsertDataToTable(1000);
                db.TakeFullBackup();
                var table2 = db.CreateTable("gone");
                table.InsertDataToTable(1000);
                table.Drop();
                var planner = new DatabaseRestorePlanner(db.Parent, db.Name)
                {
                    TailLogBackupFile = Path.Combine(db.PrimaryFilePath, $"DbRestore{Guid.NewGuid()}.log"),
                    BackupTailLog = true,
                    TailLogWithNoRecovery = true
                };
                var plan = planner.CreateRestorePlan();
                plan.Execute();
                db.Tables.Refresh();
                Assert.That(db.Tables.Cast<Table>().Select(t => t.Name), Has.Member(table.Name), $"table {table.Name} should be recovered");
                Assert.That(db.Tables.Cast<Table>().Select(t => t.Name), Has.No.Member(table2.Name), $"table {table2.Name} should be gone");
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        // Verifies that during a point in time restore a newer backup with a last LSN that is in the middle of the intended restore range wont break the restore plan
        public void DatabaseRestorePlanner_restore_database_plan_NewerOutOfSequencePointInTime()
        {
            DateTime pointInTime = new DateTime(2021, 03, 26, 21, 30, 0);
            DatabaseRestorePlanner_GeneratePlan("NewerOutOfSequenceSetup.sql", "NewerOutOfSequencePointInTimePlan.sql", "NewerOutOfSequenceCleanup.sql", pointInTime);
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        // Verifies that during a point in time restore an older backup with a last LSN in the middle of the intended restore range wont break the restore plan
        public void DatabaseRestorePlanner_restore_database_plan_OlderOutofSequencePointInTime()
        {
            DateTime pointInTime = new DateTime(2021, 04, 19, 00, 15, 0);
            DatabaseRestorePlanner_GeneratePlan("OlderOutOfSequenceSetup.sql", "OlderOutOfSequencePointInTimePlan.sql", "OlderOutOfSequenceCleanup.sql", pointInTime);
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = Management.Common.DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        // Verifies that a full backup that started before a Log backup will still be correctly selected for the backup chain
        public void DatabaseRestorePlanner_restore_database_plan_LongFullBackup()
        {
            DatabaseRestorePlanner_GeneratePlan("LongFullBackupSetup.sql", "LongFullBackupPlan.sql", "LongFullBackupCleanup.sql");
        }

        private void DatabaseRestorePlanner_GeneratePlan(string setupFilename, string expectedPlanFilename, string cleanupFilename, DateTime? pointInTime = null)
        {
            ExecuteTest(() =>
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string cleanupScript;
                using (StreamReader reader = new StreamReader(asm.GetManifestResourceStream(cleanupFilename)))
                {
                    cleanupScript = reader.ReadToEnd();
                }
                try
                {
                    // Run the clean up script before setup to make sure nothing will interfere. in a clean environment this effects 0 rows
                    ServerContext.ConnectionContext.ExecuteNonQuery(cleanupScript);
                    string setupScript;
                    using (StreamReader reader = new StreamReader(asm.GetManifestResourceStream(setupFilename)))
                    {
                        setupScript = reader.ReadToEnd();
                    }
                    ServerContext.ConnectionContext.ExecuteNonQuery(setupScript);
                    DatabaseRestorePlanner planner = pointInTime.HasValue ? 
                        new DatabaseRestorePlanner(ServerContext, "Data", pointInTime.Value, tailLogBackupFile:null):
                        new DatabaseRestorePlanner(ServerContext, "Data");
                    RestorePlan plan = planner.CreateRestorePlan();
                    string actualScript = plan.Script().ToSingleString();
                    string expectedScript;
                    using (StreamReader reader = new StreamReader(asm.GetManifestResourceStream(expectedPlanFilename)))
                    {
                        expectedScript = reader.ReadToEnd();
                    }
                    Assert.That(actualScript, Is.EqualTo(expectedScript), $"Restore plan should match expected plan");
                }
                finally
                {
                    ServerContext.ConnectionContext.ExecuteNonQuery(cleanupScript);
                }
            });
        }
    }
}
