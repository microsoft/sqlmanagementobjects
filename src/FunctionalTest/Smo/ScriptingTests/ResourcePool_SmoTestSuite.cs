// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ResourcePool properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class ResourcePool_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ResourcePool rp = (_SMO.ResourcePool)obj;
            _SMO.ResourceGovernor rg = (_SMO.ResourceGovernor)objVerify;

            rg.ResourcePools.Refresh();
            Assert.IsNull(rg.ResourcePools[rp.Name],
                            "Current resource pool not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a resource pool with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlDatabaseEdge)]
        public void SmoDropIfExists_ResourcePool_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string rpName = GenerateUniqueSmoObjectName("rp");
                    _SMO.ResourceGovernor rg = database.Parent.ResourceGovernor;
                    _SMO.ResourcePool rp = new _SMO.ResourcePool(rg, rpName);

                    try
                    {
                        VerifySmoObjectDropIfExists(rp, rg);
                    }
                    catch (Exception)
                    {
                        if ( rg.ResourcePools[rp.Name] != null)
                        {
                            rp.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

