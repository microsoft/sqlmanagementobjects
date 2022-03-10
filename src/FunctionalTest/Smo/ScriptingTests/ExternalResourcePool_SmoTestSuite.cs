// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using smo = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing external resource pool properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlDatabaseEdge, DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand)]
    public class ExternalResourcePool_SmoTestSuite : SmoObjectTestBase
    {
        private const string DefaultName = "default";
        private const int DefaultMaxCpuPercent = 100;
        private const int DefaultMaxMemoryPercent = 20;
        private const int DefaultMaxProcesses = 0;
        private const smo.AffinityType DefaultAffinityType = smo.AffinityType.Auto;

        #region Test methods

        /// <summary>
        /// Tests create with default properties/alter/drop of external resource pools via SMO
        /// 
        /// Test steps:
        /// 1. Create an external resource pool with default properties.
        /// 2. Verify the pool was created with default properties
        /// 3. Alter the pool's properties to non-default
        /// 4. Verify altered properties
        /// 5. Alter the pool's properties back to default.
        /// 6. Verify altered properties
        /// 7. Drop the pool
        /// 8. Verify the pool was dropped
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ExternalResourcePool_TestCreateAlterDrop_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string poolName = GenerateUniqueSmoObjectName("rp");

                    try
                    {
                        smo.ExternalResourcePool pool = new smo.ExternalResourcePool(server.ResourceGovernor, poolName);
                        CreatePoolAndVerify(server, pool);

                        AlterPoolAndVerify(server, pool, 21, 22, 23, smo.AffinityType.Manual, new int[] { 0 });

                        // Revert back to default properties (tests the ability to change from manual to auto affinity type)
                        AlterPoolAndVerify(server, pool);

                        TraceHelper.TraceInformation("Dropping external resource pool {0}", poolName);
                        pool.Drop();
                        server.ResourceGovernor.ExternalResourcePools.Refresh();
                        Assert.IsFalse(server.ResourceGovernor.ExternalResourcePools.Contains(poolName), "External resource pool was not dropped");
                    }
                    finally
                    {
                        if (server.ResourceGovernor.ExternalResourcePools.Contains(poolName))
                        {
                            server.ResourceGovernor.ExternalResourcePools[poolName].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests create with non-deault properties/drop of external resource pools via SMO
        /// 
        /// Test steps:
        /// 1. Create an external resource pool with non-default properties.
        /// 2. Verify the pool was created with specified properties
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ExternalResourcePool_TestCreateWithProperties_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string poolName = GenerateUniqueSmoObjectName("rp");

                    try
                    {
                        smo.ExternalResourcePool pool = new smo.ExternalResourcePool(server.ResourceGovernor, poolName);
                        CreatePoolAndVerify(server, pool, 21, 22, 23, smo.AffinityType.Manual, new int[] { 0 });
                    }
                    finally
                    {
                        if (server.ResourceGovernor.ExternalResourcePools.Contains(poolName))
                        {
                            server.ResourceGovernor.ExternalResourcePools[poolName].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests create an external pool with default properties when the default external
        /// pool has non-default properties via SMO
        /// 
        /// Test steps:
        /// 1. Alter the default external resource pool's affinity to non-default
        /// 2. Create an external resource pool with default properties
        /// 3. Verify the pool was created with default properties
        /// </summary>
        /// <remarks>
        /// This is a special test case because the ExternalResourcePoolAffinityInfo property of
        /// every new external resource pool uses the default external pool as a reference to
        /// construct the memory layout and populate the affinity information.
        /// So we want to verify that every new pool will have the default affinity, even if
        /// the default pool's affinity is not default.
        /// </remarks>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void TestCreatePoolFromNonDefaultAffinityTemplate_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string poolName = GenerateUniqueSmoObjectName("rp");

                    smo.ExternalResourcePool defaultPool = server.ResourceGovernor.ExternalResourcePools[DefaultName];
                    Assert.IsNotNull(defaultPool);

                    try
                    {
                        AlterPoolAndVerify(server, defaultPool, 21, 22, 23, smo.AffinityType.Manual, new int[] { 0 });

                        smo.ExternalResourcePool pool = new smo.ExternalResourcePool(server.ResourceGovernor, poolName);
                        CreatePoolAndVerify(server, pool);
                    }
                    finally
                    {
                        // Revert the default pool back to default values
                        AlterPoolAndVerify(server, defaultPool);

                        if (server.ResourceGovernor.ExternalResourcePools.Contains(poolName))
                        {
                            server.ResourceGovernor.ExternalResourcePools[poolName].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests scripting of external resource pools via SMO.
        /// 
        /// Test steps:
        /// 1. Script create external resource pool with default properties
        /// 2. Script create external resource pool with IncludeIfNotExists = true
        /// 3. Execute create script
        /// 4. Verify the pool was created with default properties
        /// 5. Execute create if not exists script
        /// 6. Script drop external resource pool
        /// 7. Script drop external resource pool with IncludeIfNotExists = true
        /// 8. Execute drop script
        /// 9. Verify the pool was dropped
        /// 10. Execute drop if exists script
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ExternalResourcePool_TestScriptCreateDrop_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string poolName = GenerateUniqueSmoObjectName("rp");

                    try
                    {
                        smo.ExternalResourcePool pool = new smo.ExternalResourcePool(server.ResourceGovernor, poolName)
                        {
                            MaximumCpuPercentage = 21,
                            MaximumMemoryPercentage = 22,
                            MaximumProcesses = 23,
                        };

                        pool.ExternalResourcePoolAffinityInfo.AffinityType = smo.AffinityType.Manual;
                        pool.ExternalResourcePoolAffinityInfo.Cpus.SetAffinityToRange(0, 0, true);

                        // Step 1:
                        // Create scripts for create, create if not exists
                        TraceHelper.TraceInformation("Scripting create external resource pool {0}", poolName);
                        smo.ScriptingOptions so = new smo.ScriptingOptions();
                        string createExternalPoolScript = GetScript(pool, so);

                        TraceHelper.TraceInformation("Scripting create if not exists external pool {0}", poolName);
                        so.IncludeIfNotExists = true;
                        string createIfNotExistsExternalPoolScript = GetScript(pool, so);

                        // Step 2:
                        // Create the pool
                        TraceHelper.TraceInformation("Executing create script");
                        server.ConnectionContext.ExecuteNonQuery(createExternalPoolScript);

                        // Verify the pool information can be read back correctly
                        server.ResourceGovernor.ExternalResourcePools.Refresh();
                        Assert.IsTrue(server.ResourceGovernor.ExternalResourcePools.Contains(poolName), "Can't read external resource pool information back");
                        pool = server.ResourceGovernor.ExternalResourcePools[poolName];
                        VerifyPoolValues(server, pool, 21, 22, 23, smo.AffinityType.Manual, new int[] { 0 });

                        // Step 3:
                        // Create the pool with IncludeIfNotExists option
                        //
                        // DEVNOTE:
                        // Since the pool already exists, this should execute successfully and be a no-op. If
                        // there's something wrong with the implementation of the IncludeIfNotExists option we'd
                        // get an exception, since we're trying to create a pool that already exists. So the
                        // main objective is to test a successful execution of the script
                        TraceHelper.TraceInformation("Executing create if not exists script");
                        server.ConnectionContext.ExecuteNonQuery(createIfNotExistsExternalPoolScript);

                        // Step 4:
                        // Create scripts for drop, drop if exists
                        TraceHelper.TraceInformation("Scripting drop external resource pool {0}", poolName);
                        so = new smo.ScriptingOptions();
                        so.ScriptDrops = true;
                        string dropExternalPoolScript = GetScript(pool, so);

                        TraceHelper.TraceInformation("Scripting drop if exists external resource pool {0}", poolName);
                        so.IncludeIfNotExists = true;
                        string dropIfExistsExternalPoolScript = GetScript(pool, so);

                        // Step 5:
                        // Drop the pool
                        TraceHelper.TraceInformation("Executing drop script");
                        server.ConnectionContext.ExecuteNonQuery(dropExternalPoolScript);

                        server.ResourceGovernor.ExternalResourcePools.Refresh();
                        Assert.IsFalse(server.ResourceGovernor.ExternalResourcePools.Contains(poolName), "External resource pool was not dropped");

                        // Step 6:
                        // Drop the pool with IncludeIfNotExists option
                        //
                        // DEVNOTE:
                        // Since the pool was already dropped, this should execute successfully and be a no-op. If
                        // there's something wrong with the implementation of the IncludeIfNotExists option we'd
                        // get an exception, since we're trying to drop a pool that doesn't exist. So the
                        // main objective is to test a successful execution of the script
                        TraceHelper.TraceInformation("Executing drop if exists script");
                        server.ConnectionContext.ExecuteNonQuery(dropIfExistsExternalPoolScript);

                        server.ResourceGovernor.ExternalResourcePools.Refresh();
                        Assert.IsFalse(server.ResourceGovernor.ExternalResourcePools.Contains(poolName), "External resource pool was not dropped");
                    }
                    finally
                    {
                        if (server.ResourceGovernor.ExternalResourcePools.Contains(poolName))
                        {
                            server.ResourceGovernor.ExternalResourcePools[poolName].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests alter of external resource pool affinity via SMO
        /// 
        /// Test steps:
        /// 1. Create an external resource pool with default properties
        /// 2. Verify the pool was created with default properties
        /// 3. Alter the pool's affinity to non-default
        /// 4. Verify altered properties
        /// 5. Alter the pool's affinity back to default
        /// 6. Verify altered properties
        /// </summary>
        /// <remarks>
        /// ExternalResourcePoolAffinityInfo is an entity that can be altered on its own, not
        /// just via altering the external resource pool
        /// </remarks>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ExternalResourcePool_TestAlterAffinity_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string poolName = GenerateUniqueSmoObjectName("rp");

                    try
                    {
                        smo.ExternalResourcePool pool = new smo.ExternalResourcePool(server.ResourceGovernor, poolName);
                        CreatePoolAndVerify(server, pool);

                        smo.ExternalResourcePoolAffinityInfo affinityInfo = pool.ExternalResourcePoolAffinityInfo;
                        Assert.IsNotNull(affinityInfo);

                        affinityInfo.AffinityType = smo.AffinityType.Manual;
                        affinityInfo.Cpus.SetAffinityToRange(0, 0, true);

                        TraceHelper.TraceInformation("Altering external pool affinity with affinityType = {0}", affinityInfo.AffinityType);
                        affinityInfo.Alter();
                        affinityInfo.Refresh();

                        VerifyPoolAffinity(affinityInfo, smo.AffinityType.Manual, new int[] { 0 });

                        pool.Refresh();
                        VerifyPoolAffinity(pool.ExternalResourcePoolAffinityInfo, smo.AffinityType.Manual, new int[] { 0 });

                        // Revert back to default values
                        affinityInfo.AffinityType = smo.AffinityType.Auto;
                        TraceHelper.TraceInformation("Altering external pool affinity with affinityType = {0}", affinityInfo.AffinityType);
                        affinityInfo.Alter();
                        server.ResourceGovernor.ExternalResourcePools.Refresh();
                        VerifyPoolValues(server, server.ResourceGovernor.ExternalResourcePools[poolName]);
                    }
                    finally
                    {
                        if (server.ResourceGovernor.ExternalResourcePools.Contains(poolName))
                        {
                            server.ResourceGovernor.ExternalResourcePools[poolName].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests scripting alter of external resource pool affinity via SMO
        /// 
        /// Test steps:
        /// 1. Create an external resource pool with default properties
        /// 2. Verify the pool was created with default properties
        /// 3. Script alter the pool's affinity to non-default values
        /// 4. Execute alter script
        /// 5. Verify altered affinity
        /// 6. Script alter the pool's affinity back to default values
        /// 7. Execute alter script
        /// 8. Verify altered affinity
        /// </summary>
        /// <remarks>
        /// ExternalResourcePoolAffinityInfo is an entity that can be altered on its own, not
        /// just via scripting the external resource pool
        /// </remarks>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ExternalResourcePool_TestScriptAlterAffinity_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string poolName = GenerateUniqueSmoObjectName("rp");

                    try
                    {
                        smo.ExternalResourcePool pool = new smo.ExternalResourcePool(server.ResourceGovernor, poolName);
                        CreatePoolAndVerify(server, pool);

                        smo.ExternalResourcePoolAffinityInfo affinityInfo = pool.ExternalResourcePoolAffinityInfo;
                        Assert.IsNotNull(affinityInfo);

                        affinityInfo.AffinityType = smo.AffinityType.Manual;
                        affinityInfo.Cpus.SetAffinityToRange(0, 0, true);

                        TraceHelper.TraceInformation("Scripting alter external pool affinity to non-default values");
                        smo.ScriptingOptions so = new smo.ScriptingOptions();
                        string alterAffinityScript = GetScript(affinityInfo, so);

                        TraceHelper.TraceInformation("Executing alter script");
                        server.ConnectionContext.ExecuteNonQuery(alterAffinityScript);

                        affinityInfo.Refresh();
                        VerifyPoolAffinity(affinityInfo, smo.AffinityType.Manual, new int[] { 0 });

                        // Revert back to default values
                        TraceHelper.TraceInformation("Scripting alter external pool affinity back to default values");
                        affinityInfo.AffinityType = smo.AffinityType.Auto;
                        alterAffinityScript = GetScript(affinityInfo, so);

                        TraceHelper.TraceInformation("Executing alter script");
                        server.ConnectionContext.ExecuteNonQuery(alterAffinityScript);

                        affinityInfo.Refresh();
                        VerifyPoolAffinity(affinityInfo);
                    }
                    finally
                    {
                        if (server.ResourceGovernor.ExternalResourcePools.Contains(poolName))
                        {
                            server.ResourceGovernor.ExternalResourcePools[poolName].Drop();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests refreshing an external resource pool via SMO
        /// 
        /// Test steps:
        /// 1. Verify the default external pool properties have default values
        /// 2. Change the properties' values to non-default without committing the changes
        /// 3. Verify the change locally
        /// 4. Refresh the default pool instance
        /// 5. Verify the default external pool properties have default values
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void ExternalResourcePool_TestRefresh_2016AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    smo.ExternalResourcePool pool = server.ResourceGovernor.ExternalResourcePools[DefaultName];
                    Assert.IsNotNull(pool);

                    TraceHelper.TraceInformation("Verify the default external resource pool has default values");
                    VerifyPoolValues(server, pool);

                    pool.MaximumCpuPercentage = 21;
                    pool.MaximumMemoryPercentage = 22;
                    pool.MaximumProcesses = 23;
                    pool.ExternalResourcePoolAffinityInfo.AffinityType = smo.AffinityType.Manual;
                    pool.ExternalResourcePoolAffinityInfo.Cpus.GetByID(0).AffinityMask = true;

                    TraceHelper.TraceInformation("Change the default pool's values to non-default");
                    VerifyPoolValues(server, pool, 21, 22, 23, smo.AffinityType.Manual, new int[] { 0 });

                    TraceHelper.TraceInformation("Refresh the pool");
                    pool.Refresh();

                    TraceHelper.TraceInformation("Verify the default external resource pool still has default values");
                    VerifyPoolValues(server, pool);
                });
        }

        /// <summary>
        /// Tests dropping a resource pool with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ExternalResourcePool_Sql16AndAfterOnPrem()
        {
            this.ExecuteTest(
                server =>
                {
                    string rpName = GenerateUniqueSmoObjectName("rp");
                    smo.ResourceGovernor rg = server.ResourceGovernor;
                    smo.ExternalResourcePool rp = new smo.ExternalResourcePool(rg, rpName);

                    try
                    {
                        VerifySmoObjectDropIfExists(rp, rg);
                    }
                    catch (Exception)
                    {
                        if (rg.ExternalResourcePools[rp.Name] != null)
                        {
                            rp.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion Test methods

        #region Private helper methods

        /// <summary>
        /// Verify the SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(smo.SqlSmoObject obj, smo.SqlSmoObject objVerify)
        {
            smo.ExternalResourcePool rp = (smo.ExternalResourcePool)obj;
            smo.ResourceGovernor rg = (smo.ResourceGovernor)objVerify;

            rg.ExternalResourcePools.Refresh();
            Assert.IsNull(rg.ExternalResourcePools[rp.Name],
                            "Current external resource pool not dropped with DropIfExists.");
        }

        /// <summary>
        /// Create the given external resource pool and verifies it was created with the given values
        /// </summary>
        /// <param name="server">The server on which to creaete the pool</param>
        /// <param name="pool">The external resource pool to create</param>
        /// <param name="maxCpuPercent">Requested max cpu percent</param>
        /// <param name="maxMemoryPercent">Requested max memory percent</param>
        /// <param name="maxProcesses">Requested max processes</param>
        /// <param name="affinityType">Requested affinity type</param>
        /// <param name="affinitizedCpuIds">Reqested CPU ids to affinitize</param>
        private void CreatePoolAndVerify(smo.Server server,
                                        smo.ExternalResourcePool pool,
                                        int? maxCpuPercent = null,
                                        int? maxMemoryPercent = null,
                                        long? maxProcesses = null,
                                        smo.AffinityType? affinityType = null,
                                        int[] affinitizedCpuIds = null)
        {
            if ((!affinityType.HasValue || affinityType == smo.AffinityType.Auto) != (affinitizedCpuIds == null))
            {
                throw new ArgumentException("affinityType == auto iff affinitizedCpuIds == null");
            }

            TraceHelper.TraceInformation("Creating external resource pool {0} with maxCpuPercent = {1}, maxMemoryPercent = {2}, maxProcesses = {3}, affinityType = {4}",
                pool.Name, maxCpuPercent, maxMemoryPercent, maxProcesses, affinityType);

            if (maxCpuPercent.HasValue)
            {
                pool.MaximumCpuPercentage = maxCpuPercent.Value;
            }
            if (maxMemoryPercent.HasValue)
            {
                pool.MaximumMemoryPercentage = maxMemoryPercent.Value;
            }

            if (maxMemoryPercent.HasValue)
            {
                pool.MaximumProcesses = maxProcesses.Value;
            }

            if (affinityType.HasValue)
            {
                pool.ExternalResourcePoolAffinityInfo.AffinityType = affinityType.Value;
            }

            if (affinitizedCpuIds != null)
            {
                foreach (int cpuId in affinitizedCpuIds)
                {
                    pool.ExternalResourcePoolAffinityInfo.Cpus.GetByID(cpuId).AffinityMask = true;
                }
            }

            pool.Create();
            VerifyPoolValues(server, pool, maxCpuPercent, maxMemoryPercent, maxProcesses, affinityType, affinitizedCpuIds);

            // Verify the pool information can be read back correctly, and the assigned values are correct
            server.ResourceGovernor.ExternalResourcePools.Refresh();
            Assert.IsTrue(server.ResourceGovernor.ExternalResourcePools.Contains(pool.Name), "Can't read external pool information back from ResourceGovernor");
            VerifyPoolValues(server, server.ResourceGovernor.ExternalResourcePools[pool.Name], maxCpuPercent, maxMemoryPercent, maxProcesses, affinityType, affinitizedCpuIds);
        }

        /// <summary>
        /// Alter the given external resource pool and verifies it was altered to the given values
        /// </summary>
        /// <param name="server">The server on which to creaete the pool</param>
        /// <param name="pool">The external resource pool to create</param>
        /// <param name="maxCpuPercent">Requested max cpu percent</param>
        /// <param name="maxMemoryPercent">Requested max memory percent</param>
        /// <param name="maxProcesses">Requested max processes</param>
        /// <param name="affinityType">Requested affinity type</param>
        /// <param name="affinitizedCpuIds">Reqested CPU ids to affinitize</param>
        private void AlterPoolAndVerify(smo.Server server,
                                        smo.ExternalResourcePool pool,
                                        int maxCpuPercent = DefaultMaxCpuPercent,
                                        int maxMemoryPercent = DefaultMaxMemoryPercent,
                                        long maxProcesses = DefaultMaxProcesses,
                                        smo.AffinityType affinityType = DefaultAffinityType,
                                        int[] affinitizedCpuIds = null)
        {
            if ((affinityType == smo.AffinityType.Auto) != (affinitizedCpuIds == null))
            {
                throw new ArgumentException("affinityType == auto iff affinitizedCpuIds == null");
            }

            TraceHelper.TraceInformation("Altering external pool {4} with maxCpuPercent = {0}, maxMemoryPercent = {1}, maxProcesses = {2}, affinityType = {3}",
                maxCpuPercent, maxMemoryPercent, maxProcesses, affinityType, pool.Name);

            pool.MaximumCpuPercentage = maxCpuPercent;
            pool.MaximumMemoryPercentage = maxMemoryPercent;
            pool.MaximumProcesses = maxProcesses;
            pool.ExternalResourcePoolAffinityInfo.AffinityType = affinityType;
            if (affinitizedCpuIds != null)
            {
                foreach (int cpuId in affinitizedCpuIds)
                {
                    pool.ExternalResourcePoolAffinityInfo.Cpus.GetByID(cpuId).AffinityMask = true;
                }
            }

            pool.Alter();
            pool.Refresh();

            VerifyPoolValues(server, pool, maxCpuPercent, maxMemoryPercent, maxProcesses, affinityType, affinitizedCpuIds);
        }

        /// <summary>
        /// Verify the given external resource pool has the extected property values.
        /// </summary>
        /// <param name="server">The server on which the pool to validate is located</param>
        /// <param name="pool">The pool to validate</param>
        /// <param name="maxCpuPercent">Expected max cpu percent value. If null, the default value is validated</param>
        /// <param name="maxMemoryPercent">Expected max memory percent value. If null, the default value is validated</param>
        /// <param name="maxProcesses">Expected max memory value. If null, the default value is validated</param>
        /// <param name="affinityType">Expected affinity type value. If null, the default value is validated</param>
        /// <param name="affinitizedCpuIds">Expected affinitized cpu ids. If null or empty, no cpu ids are expected to be affinitized</param>
        private void VerifyPoolValues(smo.Server server,
                                      smo.ExternalResourcePool pool,
                                      int? maxCpuPercent = null,
                                      int? maxMemoryPercent = null,
                                      long? maxProcesses = null,
                                      smo.AffinityType? affinityType = null,
                                      int[] affinitizedCpuIds = null)
        {
            maxCpuPercent = maxCpuPercent ?? DefaultMaxCpuPercent;
            maxMemoryPercent = maxMemoryPercent ?? DefaultMaxMemoryPercent;
            maxProcesses = maxProcesses ?? DefaultMaxProcesses;

            TraceHelper.TraceInformation("Verifying external pool {0} with values ID = {1}, maxCpuPercent = {2}, maxMemoryPercent = {3}, maxProcesses = {4}",
                pool.Name, pool.ID, maxCpuPercent, maxMemoryPercent, maxProcesses);

            if (pool.Name == DefaultName)
            {
                Assert.AreEqual(2, pool.ID, "ID value is not correct");
                Assert.IsTrue(pool.IsSystemObject, "IsSystemObject value is not correct");
            }
            else
            {
                Assert.IsTrue(pool.ID > 2, "ID value is not correct");
                Assert.IsFalse(pool.IsSystemObject, "IsSystemObject value is not correct");
            }

            Assert.AreEqual(smo.SqlSmoState.Existing, pool.State, "State value is not correct");
            Assert.AreEqual(maxCpuPercent, pool.MaximumCpuPercentage, "MaximumCpuPercentage value is not correct.");
            Assert.AreEqual(maxMemoryPercent, pool.MaximumMemoryPercentage, "MaximumMemoryPercentage value is not correct.");
            Assert.AreEqual(maxProcesses, pool.MaximumProcesses, "MaximumProcesses value is not correct.");

            Assert.AreEqual(server.AffinityInfo.NumaNodes.Count, pool.ExternalResourcePoolAffinityInfo.NumaNodes.Count, "Incorrect number of Numa Nodes in external resource pool affinity.");
            Assert.AreEqual(server.AffinityInfo.Cpus.Count, pool.ExternalResourcePoolAffinityInfo.Cpus.Count, "Incorrect number of cpus, should match the number of cpus installed.");

            VerifyPoolAffinity(pool.ExternalResourcePoolAffinityInfo, affinityType, affinitizedCpuIds);
        }

        /// <summary>
        /// Verify the given external resource pool affinity has the extected property values.
        /// </summary>
        /// <param name="affinityInfo">The pool affinity to validate</param>
        /// <param name="affinityType">Expected affinity type value. If null, the default value is validated</param>
        /// <param name="affinitizedCpuIds">Expected affinitized cpu ids. If null or empty, no cpu ids are expected to be affinitized</param>
        private void VerifyPoolAffinity(smo.ExternalResourcePoolAffinityInfo affinityInfo, smo.AffinityType? affinityType = null, int[] affinitizedCpuIds = null)
        {
            if ((!affinityType.HasValue || affinityType == smo.AffinityType.Auto) != (affinitizedCpuIds == null))
            {
                throw new ArgumentException("affinityType == auto iff affinitizedCpuIds == null");
            }

            affinityType = affinityType ?? DefaultAffinityType;
            affinitizedCpuIds = affinitizedCpuIds ?? new int[] { };

            TraceHelper.TraceInformation("Verifying external pool {0} with values affinityType = {1}", affinityInfo.Parent.Name, affinityType);

            Assert.AreEqual<smo.AffinityType>(affinityType.Value, affinityInfo.AffinityType, "External resource pool affinity type is not correct.");
            VerifyCpuAffinity(affinityInfo, affinitizedCpuIds);
            VerifyCpuAffinityInNumaNode(affinityInfo, affinitizedCpuIds);
        }

        /// <summary>
        /// Verify the cpu collection correctness of the given ExternalResourcePoolAffinityInfo
        /// </summary>
        /// <param name="affinityInfo">The pool affinity to validate</param>
        /// <param name="affinitizedCpuIds">CPU ids that are expected to be affinitized</param>
        private void VerifyCpuAffinity(smo.ExternalResourcePoolAffinityInfo affinityInfo, int[] affinitizedCpuIds)
        {
            // Check that all the cpu ids in the CPU collection have the expected affinity mask
            foreach (smo.Cpu cpu in affinityInfo.Cpus)
            {
                bool expectedAffinity = affinitizedCpuIds.Contains(cpu.ID);
                Assert.AreEqual(expectedAffinity, cpu.AffinityMask, "Affinity mask of cpu id {0} is incorrect", cpu.ID);
            }

            // Check that all the ids in AffitinizedCPUs are expected to be affinitized
            foreach (smo.Cpu cpu in affinityInfo.Cpus.AffitinizedCPUs)
            {
                Assert.IsTrue(affinitizedCpuIds.Contains(cpu.ID), "Cpu id {0} is not expected to be affinitized", cpu.ID);
            }

            // Check that all the cpu ids that are expected to be affinitized exist and have the correct affinity mask
            foreach (int cpuId in affinitizedCpuIds)
            {
                Assert.IsTrue(affinityInfo.Cpus.GetByID(cpuId).AffinityMask, "Cpu id {0} is expected to be affinitized but isn't", cpuId);
            }
        }

        /// <summary>
        /// Verify the numa node collection correctness of the given ExternalResourcePoolAffinityInfo
        /// </summary>
        /// <param name="affinityInfo">The pool affinity to validate</param>
        /// <param name="affinitizedCpuIds">CPU ids that are expected to be affinitized</param>
        private void VerifyCpuAffinityInNumaNode(smo.ExternalResourcePoolAffinityInfo affinityInfo, int[] affinitizedCpuIds)
        {
            List<int> affinitizedNumaNodes =  new List<int>();

            // Create a list of numa nodes that are expected to be affinitized
            foreach (int cpuId in affinitizedCpuIds)
            {
                affinitizedNumaNodes.Add(affinityInfo.Cpus.GetByID(cpuId).NumaNodeID);
            }
            
            if (affinitizedCpuIds.Length > 0)
            {
                Assert.IsTrue(affinitizedNumaNodes.Count > 0, "there should be at least 1 affinitized numa node");
            }

            // Check that all the numa nodes in the NumaNodes collection have the expected affinify
            foreach (smo.NumaNode numaNode in affinityInfo.NumaNodes)
            {
                smo.NumaNodeAffinity expectedNumaNodeAffinity;
                if (affinitizedNumaNodes.Contains(numaNode.ID))
                {
                    if (affinityInfo.NumaNodes[numaNode.ID].Cpus.Count == 1)
                    {
                        expectedNumaNodeAffinity = smo.NumaNodeAffinity.Full;
                    }
                    else
                    {
                        expectedNumaNodeAffinity = smo.NumaNodeAffinity.Partial;
                    }
                }
                else
                {
                    expectedNumaNodeAffinity = smo.NumaNodeAffinity.None;
                }

                Assert.AreEqual<smo.NumaNodeAffinity>(expectedNumaNodeAffinity, numaNode.AffinityMask, "affinity of numa node {0} is incorrect", numaNode.ID);

                foreach (smo.Cpu cpu in numaNode.Cpus)
                {
                    bool expectedAffinity = affinitizedCpuIds.Contains(cpu.ID);
                    Assert.AreEqual(expectedAffinity, cpu.AffinityMask, "Affinity mask of cpu id {0} is incorrect", cpu.ID);
                }
            }

            foreach (smo.Cpu cpu in affinityInfo.Cpus)
            {
                if (cpu.AffinityMask)
                {
                    Assert.IsTrue(affinitizedNumaNodes.Contains(cpu.NumaNodeID), "numa node {0} is expected to be affinitized but isn't", cpu.NumaNodeID);
                }
            }
        }

        /// <summary>
        /// Returns a T-SQL query for performing the requested operation
        /// </summary>
        /// <param name="scriptable">The object to script</param>
        /// <param name="so">Scripting Options</param>
        /// <returns>A T-SQL query for performing the requested operation</returns>
        private string GetScript(smo.IScriptable scriptable, smo.ScriptingOptions so)
        {
            StringCollection col = scriptable.Script(so);
            StringBuilder sb = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
            }

            string script = sb.ToString();
            TraceHelper.TraceInformation(script);

            return script;
        }

        #endregion Private helper methods
    }
}