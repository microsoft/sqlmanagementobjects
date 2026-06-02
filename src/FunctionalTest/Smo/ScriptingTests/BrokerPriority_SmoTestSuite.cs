//*********************************************************************
//  Copyright (c) Microsoft Corporation.
//
//
//  Purpose:
//      SMO BrokerPriority object tests
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
    /// Test suite for testing BrokerPriority properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class BrokerPriority_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            BrokerPriority priority = (BrokerPriority)obj;
            ServiceBroker svcBroker = (ServiceBroker)objVerify;

            svcBroker.Priorities.Refresh();
            Assert.IsNull(svcBroker.Priorities[priority.Name],
                            "Current broker priority not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a broker priority with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_BrokerPriority_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ServiceBroker svcBroker = database.ServiceBroker;
                    BrokerPriority priority = new BrokerPriority(svcBroker,
                        GenerateSmoObjectName("priority"));

                    VerifySmoObjectDropIfExists(priority, svcBroker);
                });
        }

        #endregion // Scripting Tests
    }
}

