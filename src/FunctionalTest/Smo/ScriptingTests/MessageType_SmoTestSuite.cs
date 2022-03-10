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
    /// Test suite for testing MessageType properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class MessageType_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            MessageType msgType = (MessageType)obj;
            ServiceBroker svcBroker = (ServiceBroker)objVerify;

            svcBroker.MessageTypes.Refresh();
            Assert.IsNull(svcBroker.MessageTypes[msgType.Name],
                            "Current message type not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a message type with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_ServiceContract_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ServiceBroker svcBroker = database.ServiceBroker;
                    MessageType msgType = new MessageType(svcBroker,
                        GenerateSmoObjectName("msgType"));

                    VerifySmoObjectDropIfExists(msgType, svcBroker);
                });
        }

        #endregion // Scripting Tests
    }
}

