// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ServiceContract properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ServiceContract_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            ServiceContract svcContract = (ServiceContract)obj;
            ServiceBroker svcBroker = (ServiceBroker)objVerify;

            svcBroker.ServiceContracts.Refresh();
            Assert.IsNull(svcBroker.ServiceContracts[svcContract.Name],
                            "Current service contract not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a service contract with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ServiceContract_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ServiceBroker svcBroker = database.ServiceBroker;
                    ServiceContract svcContract = new ServiceContract(svcBroker,
                        GenerateSmoObjectName("sc"));
                    MessageType testMsg = new MessageType(svcBroker,
                        GenerateSmoObjectName("testMsg"));
                    MessageTypeMapping testMsgMap = new MessageTypeMapping(
                        svcContract, testMsg.Name, MessageSource.InitiatorAndTarget);

                    testMsg.Create();
                    svcContract.MessageTypeMappings.Add(testMsgMap);

                    VerifySmoObjectDropIfExists(svcContract, svcBroker);
                });
        }

        #endregion // Scripting Tests
    }
}

