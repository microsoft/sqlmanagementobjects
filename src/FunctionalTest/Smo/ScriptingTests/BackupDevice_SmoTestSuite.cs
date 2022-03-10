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
    /// Test suite for testing BackupDevice properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class BackupDevice_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.BackupDevice backupDev = (_SMO.BackupDevice)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.BackupDevices.Refresh();
            Assert.IsNull(server.BackupDevices[backupDev.Name],
                "Current login not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a backup device with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_BackupDevice_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string backupDevName = GenerateUniqueSmoObjectName("backupDev");
                    _SMO.Server server = database.Parent;
                    _SMO.BackupDevice backupDev = new _SMO.BackupDevice(server, backupDevName);

                    backupDev.BackupDeviceType = _SMO.BackupDeviceType.Disk;
                    backupDev.PhysicalLocation = String.Format("C:\\temp\\{0}.bak", backupDev.Name);

                    try
                    {
                        VerifySmoObjectDropIfExists(backupDev, server);
                    }
                    catch (Exception)
                    {
                        if (server.BackupDevices[backupDev.Name] != null)
                        {
                            backupDev.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

