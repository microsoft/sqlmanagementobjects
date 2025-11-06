// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    [TestClass]
    [UnsupportedFeature(SqlFeature.Fabric)]
    public class WorkloadSmoTests : SqlTestBase
    {
        //Method added to test WorkloadGroup GroupMaximumTempdbDataMB property scripting
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 17, Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_GroupMaximumTempdbDataMB_Is_Scripted()
        {
            this.ExecuteTest(
                server =>
                {
                    var resourcegovernor = server.ResourceGovernor;
                    ResourcePool defaultPool = resourcegovernor.ResourcePools["default"];
                    var workloadGroup = new WorkloadGroup(defaultPool, "WorkloadTempdbMB1_");

                    // Set the GroupMaximumTempdbDataMB property if supported
                    if (workloadGroup.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataMB)))
                    {
                        workloadGroup.GroupMaximumTempdbDataMB = 2048;
                    }

                    var commands = server.ExecutionManager.RecordQueryText(workloadGroup.Create, false).Cast<string>();
                    string expected = $"CREATE WORKLOAD GROUP [WorkloadTempdbMB1_] WITH(group_max_tempdb_data_mb=2048) USING [default]";
                    Assert.That(commands, Has.Member(expected), "Invalid Query to Create Workload Group with GroupMaximumTempdbDataMB");
                });
        }

        //Method added to test WorkloadGroup GroupMaximumTempdbDataPercent property scripting
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 17, Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_GroupMaximumTempdbDataPercent_Is_Scripted()
        {
            this.ExecuteTest(
                server =>
                {
                    var resourcegovernor = server.ResourceGovernor;
                    ResourcePool defaultPool = resourcegovernor.ResourcePools["default"];
                    var workloadGroup = new WorkloadGroup(defaultPool, "WorkloadTempdbPercent_");

                    // Set the GroupMaximumTempdbDataPercent property if supported
                    if (workloadGroup.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataPercent)))
                    {
                        workloadGroup.GroupMaximumTempdbDataPercent = 15.5;
                    }

                    var commands = server.ExecutionManager.RecordQueryText(workloadGroup.Create, false).Cast<string>();
                    string expected = $"CREATE WORKLOAD GROUP [WorkloadTempdbPercent_] WITH(group_max_tempdb_data_percent=15.5) USING [default]";
                    Assert.That(commands, Has.Member(expected), "Invalid Query to Create Workload Group with GroupMaximumTempdbDataPercent");
                });
        }

        //Method added to test Alter method in WorkloadGroup for TempDB properties
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 17, Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_TempdbProperties_Alter()
        {
            this.ExecuteTest(
                server =>
                {
                    ResourcePool defaultPool = server.ResourceGovernor.ResourcePools["default"];
                    WorkloadGroup group = defaultPool.WorkloadGroups["default"];

                    //Test alter for GroupMaximumTempdbDataMB
                    if (group.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataMB)))
                    {
                        group.GroupMaximumTempdbDataMB = 4096.5;
                        string expected = $"ALTER WORKLOAD GROUP [default] WITH(group_max_tempdb_data_mb=4096.5)";
                        var commands = server.ExecutionManager.RecordQueryText(group.Alter,false).Cast<string>();
                        Assert.That(commands, Has.Member(expected), "Invalid Query to Alter Workload Group for GroupMaximumTempdbDataMB");
                        group.Refresh();
                    }

                    //Test alter for GroupMaximumTempdbDataPercent
                    if (group.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataPercent)))
                    {
                        group.GroupMaximumTempdbDataPercent = 30.75;
                        string expected = $"ALTER WORKLOAD GROUP [default] WITH(group_max_tempdb_data_percent=30.75)";
                        var commands = server.ExecutionManager.RecordQueryText(group.Alter, false).Cast<string>();
                        Assert.That(commands, Has.Member(expected), "Invalid Query to Alter Workload Group for GroupMaximumTempdbDataPercent");
                        group.Refresh();
                    }

                    //Test alter for TempDB properties set to -1 (should appear as null in script)
                    if (group.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataMB)))
                    {
                        group.GroupMaximumTempdbDataMB = -1;
                        var commands = server.ExecutionManager.RecordQueryText(group.Alter, false).Cast<string>();

                        // When property is set to -1 in ALTER, it should appear as null in the script to reset the value
                        string expectedAlter = $"ALTER WORKLOAD GROUP [default] WITH(group_max_tempdb_data_mb=null)";
                        Assert.That(commands, Has.Member(expectedAlter), "ALTER script should contain group_max_tempdb_data_mb=null when set to -1");
                        group.Refresh();
                    }

                    if (group.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataPercent)))
                    {
                        group.GroupMaximumTempdbDataPercent = -1;
                        var commands = server.ExecutionManager.RecordQueryText(group.Alter,false).Cast<string>();

                        // When property is set to -1 in ALTER, it should appear as null in the script to reset the value
                        string expectedAlter = $"ALTER WORKLOAD GROUP [default] WITH(group_max_tempdb_data_percent=null)";
                        Assert.That(commands, Has.Member(expectedAlter), "ALTER script should contain group_max_tempdb_data_percent=null when set to -1");
                    }
                });
        }

        //Method added to test WorkloadGroup -1 to null translation for TempDB properties
        [TestMethod]
        [SupportedServerVersionRange(MinMajor = 17, Edition = DatabaseEngineEdition.Enterprise)]
        public void Workload_Verify_TempdbProperties_MinusOne_Excluded_From_Script()
        {
            this.ExecuteTest(
                server =>
                {
                    var resourcegovernor = server.ResourceGovernor;
                    ResourcePool defaultPool = server.ResourceGovernor.ResourcePools["default"];
                    var workloadGroup = new WorkloadGroup(defaultPool, "WorkloadTempdbMinusOne_");

                    // Set the TempDB properties to -1 if supported
                    if (workloadGroup.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataMB)))
                    {
                        workloadGroup.GroupMaximumTempdbDataMB = -1;
                    }
                    if (workloadGroup.IsSupportedProperty(nameof(WorkloadGroup.GroupMaximumTempdbDataPercent)))
                    {
                        workloadGroup.GroupMaximumTempdbDataPercent = -1;
                    }

                    var commands = server.ExecutionManager.RecordQueryText(workloadGroup.Create, false).Cast<string>();
                        
                    // When properties are set to -1, they should be excluded from the script (treated as null)
                    string expected = $"CREATE WORKLOAD GROUP [WorkloadTempdbMinusOne_] USING [default]";
                    Assert.That(commands, Has.Member(expected), "Invalid Query to Create Workload Group - TempDB properties with -1 should be excluded from script");
                        
                    // Verify that the script does NOT contain the TempDB properties when set to -1
                    foreach (string command in commands)
                    {
                        Assert.That(command, Does.Not.Contain("group_max_tempdb_data_mb"), "Script should not contain group_max_tempdb_data_mb when set to -1");
                        Assert.That(command, Does.Not.Contain("group_max_tempdb_data_percent"), "Script should not contain group_max_tempdb_data_percent when set to -1");
                    }
                });
        }
    }
}
