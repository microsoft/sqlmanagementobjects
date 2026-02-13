// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{

    /// <summary>
    /// Tests for the BackupRestoreBase class
    /// </summary>
    [TestClass]
    public class BackupRestoreTests
    {
        BackupDeviceList azureBlobDevices = new BackupDeviceList();
        BackupDeviceList s3Devices = new BackupDeviceList();

        private void InitDevices()
        {
            azureBlobDevices.AddDevice("https://myaccount.blob.core.windows.net/mycontainer/my_backup_1.bak", DeviceType.Url);
            azureBlobDevices.AddDevice("https://myaccount.blob.core.windows.net/mycontainer/my_backup_2.bak", DeviceType.Url);
            s3Devices.AddDevice("s3://s3.region-code.amazonaws.com/bucket-name/my_backup_1.bak", DeviceType.Url);
            s3Devices.AddDevice("s3://s3.region-code.amazonaws.com/bucket-name/my_backup_2.bak", DeviceType.Url);
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void BackupDeviceLists_AreAzureBlobBackupRestore()
        {
            InitDevices();
            Assert.That(BackupRestoreBase.IsAzureBlobBackupRestore(azureBlobDevices), Is.True, "Azure blob backup devices weren't recognized as such by the BackupRestoreBase class.");
            Assert.That(BackupRestoreBase.IsAzureBlobBackupRestore(s3Devices), Is.False, "S3 backup devices were recognized as azure blob devices by the BackupRestoreBase class.");
        }
    }
}
