﻿// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Endpoint properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlOnDemand)]
    public class Endpoint_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Endpoint ep = (_SMO.Endpoint)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Endpoints.Refresh();
            Assert.IsNull(server.Endpoints[ep.Name],
                          "Current endpoint not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an endpoint with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_Endpoint_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.Endpoint ep = new _SMO.Endpoint(server,
                        GenerateUniqueSmoObjectName("ep"));

                    ep.ProtocolType = _SMO.ProtocolType.Tcp;
                    ep.EndpointType = _SMO.EndpointType.TSql;
                    ep.Protocol.Tcp.ListenerPort = 3333;

                    try
                    {
                        VerifySmoObjectDropIfExists(ep, server);
                    }
                    catch (Exception)
                    {
                        if (server.Endpoints[ep.Name] != null)
                        {
                            ep.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

