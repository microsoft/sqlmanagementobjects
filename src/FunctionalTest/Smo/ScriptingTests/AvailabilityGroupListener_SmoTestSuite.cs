// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if false //Commented out temporarily for moving to SSMS_Main as this will take significant rework to be usable in the new branch
namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    
    /// <summary>
    /// Test suite for testing AvailabilityGroupListener properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Full, FeatureCoverage.Manageability)]
    [TestRequirementNumberOfMachines(2, 3)]
    [TestRequirementBuildDynamicCluster(true)]
    public class AvailabilityGroupListener_SmoTestSuite : SmoObjectTestBase
    {
#region Scripting Tests

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            _SMO.AvailabilityGroupListener agl = (_SMO.AvailabilityGroupListener)obj;
            _SMO.AvailabilityGroup ag = (_SMO.AvailabilityGroup)agl.Parent;

            ag.Create();
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.AvailabilityGroupListener agl = (_SMO.AvailabilityGroupListener)obj;
            _SMO.AvailabilityGroup ag = (_SMO.AvailabilityGroup)objVerify;

            ag.AvailabilityGroupListeners.Refresh();
            Assert.IsNull(ag.AvailabilityGroupListeners[agl.Name],
                            "Current availability group listener not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an availability group listener with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod,Ignore]
        public void SmoDropIfExists_AvailabilityGroupListener_Sql16AndAfterOnPrem()
        {
            string serverPrimName = Context.TestEnvironment.SqlProcessEnvironments[0].ServerName;
            string epPrimName = GenerateUniqueSmoObjectName("epPrim");
            const int epPrimPort = 5022;
            const int aglPort = 1433;
            // Availability group listener IP must be valid
            const string aglIpAddress = "10.218.157.180";
            const string aglSubnetMask = "255.255.254.0";
            _SMO.Server serverPrim = new _SMO.Server(serverPrimName);
            _SMO.AvailabilityGroup ag = new _SMO.AvailabilityGroup(serverPrim, GenerateUniqueSmoObjectName("ag"));
            _SMO.AvailabilityGroupListener agl = new _SMO.AvailabilityGroupListener(ag, "aglTest");
            _SMO.AvailabilityGroupListenerIPAddress aglIp = new _SMO.AvailabilityGroupListenerIPAddress(agl);
            _SMO.AvailabilityReplica arPrim = new _SMO.AvailabilityReplica(ag,
                serverPrim.ConnectionContext.TrueName);
            _SMO.Endpoint[] epPrimList = serverPrim.Endpoints.EnumEndpoints(_SMO.EndpointType.DatabaseMirroring);
            _SMO.Endpoint epPrim = new _SMO.Endpoint(serverPrim, epPrimName);

            if (serverPrim.VersionMajor < 13)
                throw new RequirementsNotMetException("DropIfExists method is available for SQL2016+ (v13+)");

            try
            {
                // Server can have only one database mirroring endpoint. If endpoint doesn't exists,
                // it will be created.
                //
                if (epPrimList.Length != 0)
                {
                    epPrim = epPrimList[0];
                }
                else
                {
                    epPrim.ProtocolType = _SMO.ProtocolType.Tcp;
                    epPrim.EndpointType = _SMO.EndpointType.DatabaseMirroring;
                    epPrim.Protocol.Tcp.ListenerPort = epPrimPort;
                    epPrim.Payload.DatabaseMirroring.ServerMirroringRole = _SMO.ServerMirroringRole.All;
                    epPrim.Payload.DatabaseMirroring.EndpointEncryption = _SMO.EndpointEncryption.Required;
                    epPrim.Payload.DatabaseMirroring.EndpointEncryptionAlgorithm = _SMO.EndpointEncryptionAlgorithm.Aes;

                    epPrim.Create();
                }

                arPrim.EndpointUrl = string.Format("TCP://{0}:{1}", serverPrim.NetName, epPrim.Protocol.Tcp.ListenerPort);
                arPrim.FailoverMode = _SMO.AvailabilityReplicaFailoverMode.Automatic;
                arPrim.AvailabilityMode = _SMO.AvailabilityReplicaAvailabilityMode.SynchronousCommit;
                ag.AvailabilityReplicas.Add(arPrim);

                aglIp.IsDHCP = false;
                aglIp.IPAddress = aglIpAddress;
                aglIp.SubnetMask = aglSubnetMask;
                agl.PortNumber = aglPort;
                agl.AvailabilityGroupListenerIPAddresses.Add(aglIp);
                ag.AvailabilityGroupListeners.Add(agl);

                serverPrim.AvailabilityGroups.Add(ag);

                VerifySmoObjectDropIfExists(agl, ag);
            }
            catch (Exception)
            {
                if (ag.AvailabilityGroupListeners[agl.Name] != null &&
                    agl.State == _SMO.SqlSmoState.Existing)
                {
                    agl.Drop();
                }
                throw;
            }
            finally
            {
                ag.DropIfExists();
                if (serverPrim.Endpoints[epPrimName] != null)
                {
                    epPrim.DropIfExists();
                }
            }
        }

#endregion // Scripting Tests
    }
}
#endif

