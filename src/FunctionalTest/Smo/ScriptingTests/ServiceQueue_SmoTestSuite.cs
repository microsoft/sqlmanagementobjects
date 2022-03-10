// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ServiceQueue properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ServiceQueue_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            ServiceQueue svcQueue = (ServiceQueue)obj;
            ServiceBroker svcBroker = (ServiceBroker)objVerify;

            svcBroker.Queues.Refresh();
            Assert.IsNull(svcBroker.Queues[svcQueue.Name],
                            "Current service queue not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a service queue with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ServiceQueue_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ServiceBroker svcBroker = database.ServiceBroker;
                    ServiceQueue svcQueue = new ServiceQueue(svcBroker,
                        GenerateSmoObjectName("sq"));

                    VerifySmoObjectDropIfExists(svcQueue, svcBroker);
                });
        }

        #endregion // Scripting Tests
    }
}

