// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing WorkloadGroup properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class WorkloadGroup_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Tests dropping a workload group with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabaseEdge)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_WorkloadGroup_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.ResourcePool rp = new _SMO.ResourcePool(server.ResourceGovernor,
                        GenerateUniqueSmoObjectName("rp"));
                    _SMO.WorkloadGroup wg = new _SMO.WorkloadGroup(rp,
                        GenerateUniqueSmoObjectName("wg"));

                    try
                    {
                        rp.Create();

                        VerifySmoObjectDropIfExists(wg, rp);
                    }
                    finally
                    {
                        rp.DropIfExists();
                    }
                });
        }

        /// <summary>
        /// Tests create a workload group inside a non-default external resource pool
        /// and moving the group to another external resource pool
        /// 
        /// Test steps:
        /// 1. Create 2 external resource pools
        /// 2. Create a workload group inside ep1
        /// 3. Verify the group was created in ep1
        /// 4. Move the workload group to ep1
        /// 5. Verify the group was moved to ep2
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlDatabaseEdge)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void TestCreateMoveGroupToExternalResourcePool()
        {
            this.ExecuteTest(
                server =>
                {
                    string epName1 = GenerateUniqueSmoObjectName("rp");
                    string epName2 = GenerateUniqueSmoObjectName("rp");
                    string groupName = GenerateUniqueSmoObjectName("wg");

                    _SMO.ResourcePool defaultPool = server.ResourceGovernor.ResourcePools["default"];
                    Assert.IsNotNull(defaultPool, "default resource pool should exist");

                    try
                    {
                        TraceHelper.TraceInformation("Creating external resource pool {0}", epName1);
                        _SMO.ExternalResourcePool ep1 = new _SMO.ExternalResourcePool(server.ResourceGovernor, epName1);
                        ep1.Create();

                        TraceHelper.TraceInformation("Creating external resource pool {0}", epName2);
                        _SMO.ExternalResourcePool ep2 = new _SMO.ExternalResourcePool(server.ResourceGovernor, epName2);
                        ep2.Create();

                        TraceHelper.TraceInformation("Creating workload group {0} associated with external resource pool {1}", groupName, epName1);
                        _SMO.WorkloadGroup group = new _SMO.WorkloadGroup(defaultPool, groupName);
                        group.ExternalResourcePoolName = epName1;
                        group.Create();

                        defaultPool.Refresh();
                        Assert.IsTrue(defaultPool.WorkloadGroups.Contains(groupName));
                        group = defaultPool.WorkloadGroups[groupName];
                        Assert.AreEqual(epName1, group.ExternalResourcePoolName);

                        TraceHelper.TraceInformation("Moving workload group to external resource pool {0}", epName2);
                        group.ExternalResourcePoolName = epName2;
                        group.Alter();
                        group.Refresh();
                        Assert.AreEqual(epName2, group.ExternalResourcePoolName);
                    }
                    finally
                    {
                        if (defaultPool.WorkloadGroups.Contains(groupName))
                        {
                            defaultPool.WorkloadGroups[groupName].Drop();
                        }

                        if (server.ResourceGovernor.ExternalResourcePools.Contains(epName1))
                        {
                            server.ResourceGovernor.ExternalResourcePools[epName1].Drop();
                        }

                        if (server.ResourceGovernor.ExternalResourcePools.Contains(epName2))
                        {
                            server.ResourceGovernor.ExternalResourcePools[epName2].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests create a workload group inside the default external resource pool
        /// 
        /// Test steps:
        /// 1. Create a workload group
        /// 2. Verify the group was created inside the default external resource pool
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDatabaseEdge)]
        public void TestCreateGroupInDefaultExternalPool()
        {
            this.ExecuteTest(
                server =>
                {
                    string defaultName = "default";
                    string groupName = GenerateUniqueSmoObjectName("wg");

                    _SMO.ResourcePool defaultPool = server.ResourceGovernor.ResourcePools[defaultName];
                    Assert.IsNotNull(defaultPool, "default resource pool should exist");

                    try
                    {
                        TraceHelper.TraceInformation("Creating workload group {0}", groupName);
                        _SMO.WorkloadGroup group = new _SMO.WorkloadGroup(defaultPool, groupName);
                        group.Create();

                        defaultPool.Refresh();
                        Assert.IsTrue(defaultPool.WorkloadGroups.Contains(groupName));
                        group = defaultPool.WorkloadGroups[groupName];
                        Assert.AreEqual(defaultName, group.ExternalResourcePoolName);
                    }
                    finally
                    {
                        if (defaultPool.WorkloadGroups.Contains(groupName))
                        {
                            defaultPool.WorkloadGroups[groupName].Drop();
                        }
                    }
                });
        }

        #endregion //Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.WorkloadGroup wg = (_SMO.WorkloadGroup)obj;
            _SMO.ResourcePool rp = (_SMO.ResourcePool)objVerify;

            rp.WorkloadGroups.Refresh();
            Assert.IsNull(rp.WorkloadGroups[wg.Name],
                            "Current resource pool not dropped with DropIfExists.");
        }
    }
}

