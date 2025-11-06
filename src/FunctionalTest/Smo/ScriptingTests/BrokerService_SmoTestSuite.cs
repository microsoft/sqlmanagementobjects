//*********************************************************************
//  Copyright (c) Microsoft Corporation.
//
//
//  Purpose:
//      SMO BrokerService object tests
//
//
//*********************************************************************

using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.TestShellTests.Manageability.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing BrokerService properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class BrokerService_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            BrokerService brokerSvc = (BrokerService)obj;
            ServiceBroker svcBroker = (ServiceBroker)objVerify;

            svcBroker.Services.Refresh();
            Assert.IsNull(svcBroker.Services[brokerSvc.Name],
                            "Current broker service not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a broker service with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_BrokerService_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ServiceBroker svcBroker = database.ServiceBroker;
                    ServiceQueue svcQueue = new ServiceQueue(svcBroker,
                        GenerateSmoObjectName("sq"));
                    BrokerService brokerSvc = new BrokerService(svcBroker,
                        GenerateSmoObjectName("bs"));

                    svcQueue.Create();
                    brokerSvc.QueueName = svcQueue.Name;

                    VerifySmoObjectDropIfExists(brokerSvc, svcBroker);
                });
        }

        #endregion // Scripting Tests
    }
}

