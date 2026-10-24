// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing RemoteServiceBinding properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class RemoteServiceBinding_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            RemoteServiceBinding rsb = (RemoteServiceBinding)obj;
            ServiceBroker svcBroker = (ServiceBroker)objVerify;

            svcBroker.RemoteServiceBindings.Refresh();
            Assert.IsNull(svcBroker.RemoteServiceBindings[rsb.Name],
                            "Current remote service binding not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a remote service binding with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_RemoteServiceBinding_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    ServiceBroker svcBroker = database.ServiceBroker;
                    RemoteServiceBinding rsb = new RemoteServiceBinding(svcBroker,
                        GenerateSmoObjectName("rsb"));
                    _SMO.User user = new _SMO.User(database, GenerateSmoObjectName("user"));
                    _SMO.Login login = new _SMO.Login(server, GenerateUniqueSmoObjectName("login"));

                    login.LoginType = _SMO.LoginType.SqlLogin;
                    user.Login = login.Name;

                    rsb.RemoteService = GenerateSmoObjectName("rs");
                    rsb.CertificateUser = user.Name;

                    try
                    {
                        login.Create(Guid.NewGuid().ToString());
                        user.Create();

                        VerifySmoObjectDropIfExists(rsb, svcBroker);
                    }
                    finally
                    {
                        login.DropIfExists();
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

