// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _NU = NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing WorkloadManagement WorkloadGroup properties and scripting
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
    public class WorkloadManagementWorkloadGroup_SmoTestSuite: SmoTestBase
    {
        #region helpers

        internal static _SMO.WorkloadManagementWorkloadGroup CreateWorkloadGroup(_SMO.Database db, string name, int capPercentageResource = 75,
            int minPercentageResource = 50, double requestMinResourceGrantPercent = 25.0, double requestMaxResourceGrantPercent = 25.0,
            WorkloadManagementImportance importance = WorkloadManagementImportance.Normal, int queryExecutionTimeoutSec=0)
        {
            var workloadGroup = new _SMO.WorkloadManagementWorkloadGroup(db, name)
            {
                CapPercentageResource = capPercentageResource,
                MinPercentageResource = minPercentageResource,
                RequestMinResourceGrantPercent = requestMinResourceGrantPercent,
                RequestMaxResourceGrantPercent = requestMaxResourceGrantPercent,
                Importance = importance,
                QueryExecutionTimeoutSec = queryExecutionTimeoutSec
            };
            workloadGroup.Create();
            return workloadGroup;
        }

        #endregion
        #region Scripting Tests

        /// <summary>
        /// Test altering a workload group 
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        public void SmoAlter_WorkloadManagementWorkloadGroup()
        {
            this.ExecuteWithDbDrop(
                db =>
                {
                    // create
                    var workloadGroup = CreateWorkloadGroup(db, "AlterWG", requestMinResourceGrantPercent: 25);
                    // alter and validate
                    workloadGroup.RequestMinResourceGrantPercent = 12.5;
                    workloadGroup.Alter();
                    workloadGroup.Refresh();
                    Assert.That(db.WorkloadManagementWorkloadGroups[workloadGroup.Name].RequestMinResourceGrantPercent, _NU.Is.EqualTo(12.5),
                                "WorkloadManagemengWorkloadGroup Alter failed");
                }
            );
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabase)]
        public void SmoDropIfExists_WorkloadManagementWorkloadGroup()
        {
            this.ExecuteWithDbDrop(
                db =>
                {
                    string wname = "DropIfExistsWG";
                    // validate does not exist
                    Assert.That(db.WorkloadManagementWorkloadGroups[wname], _NU.Is.Null);

                    // create
                    var workloadGroup = CreateWorkloadGroup(db, wname);
                    db.WorkloadManagementWorkloadGroups.Refresh();
                    Assert.That(db.WorkloadManagementWorkloadGroups[wname], _NU.Is.Not.Null);

                    // drop and validate
                    workloadGroup.DropIfExists();
                    db.WorkloadManagementWorkloadGroups.Refresh();
                    Assert.That(db.WorkloadManagementWorkloadGroups[wname], _NU.Is.Null,
                        "WorkloadManagemengWorkloadGroup DropIfExists failed");

                    // ensure drop on non-existent workload group throws an exception
                    Assert.Throws<_SMO.FailedOperationException>(() => workloadGroup.Drop(),
                        "WorkloadManagemengWorkloadGroup Drop should have thrown on non-existent object");
                }
            );
        }

        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabase)]
        public void SmoDropAndCreate_with_ExistenceCheck_AvoidsDynamicSQL_WorkloadManagementWorkloadGroup()
        {
            this.ExecuteWithDbDrop(
                db =>
                {
                    var workloadGroup = CreateWorkloadGroup(db, "DropAndCreateWG");
                    var scriptingPreferences = new ScriptingPreferences(db)
                    {
                        Behavior = _SMO.ScriptBehavior.DropAndCreate,
                        IncludeScripts =
                        {
                            ExistenceCheck = true
                        }
                    };

                    var nameBracketed = SmoObjectHelpers.SqlBracketQuoteString(workloadGroup.Name);
                    var nameEscaped = SmoObjectHelpers.SqlEscapeSingleQuote(workloadGroup.Name);
                    var indent = "\t\t";
                    // validate create and drop
                    var expectedScript = new[]
                    {
                        string.Format("IF EXISTS (SELECT name FROM sys.workload_management_workload_groups WHERE name = N'{0}'){2}BEGIN{2}DROP WORKLOAD GROUP {1}{2}END", nameEscaped, nameBracketed, Environment.NewLine),
                        string.Format("CREATE WORKLOAD GROUP {0}{1}WITH(min_percentage_resource=50, {1}{2}cap_percentage_resource=75, {1}{2}request_min_resource_grant_percent=25, {1}{2}request_max_resource_grant_percent=25, {1}{2}importance=NORMAL, {1}{2}query_execution_timeout_sec=0)", nameBracketed, Environment.NewLine, indent)
                    };
                    SmoTestBase.ValidateUrnScripting(db, new[] { workloadGroup.Urn }, expectedScript, scriptingPreferences);

                    // validate create
                    scriptingPreferences.Behavior = ScriptBehavior.Create;
                    SmoTestBase.ValidateUrnScripting(db, new[] { workloadGroup.Urn },
                        new[]
                        {
                          string.Format("IF NOT EXISTS (SELECT name FROM sys.workload_management_workload_groups WHERE name = N'{0}'){2}BEGIN{2}CREATE WORKLOAD GROUP {1}{2}WITH(min_percentage_resource=50, {2}{3}cap_percentage_resource=75, {2}{3}request_min_resource_grant_percent=25, {2}{3}request_max_resource_grant_percent=25, {2}{3}importance=NORMAL, {2}{3}query_execution_timeout_sec=0){2}END", nameEscaped, nameBracketed, Environment.NewLine, indent),
                        }, scriptingPreferences);
                }
                );
        }

        #endregion
    }
}
