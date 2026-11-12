// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Backup properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Backup_SmoTestSuite : SmoTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Tests scripting an S3 backup through SMO on SQL22 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        public void SmoScript_Backup_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string s3Url = "s3://s3.amazonaws.com:443/test/TestBackup.bak";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.CopyOnly = true;
                    backup.Devices.AddDevice(s3Url, _SMO.DeviceType.Url);
                    
                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  URL = N'{s3Url}' WITH  COPY_ONLY, NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting an S3 backup with Region option through SMO on SQL22 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToS3UrlWithRegion_ManagedInstance()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string s3Url = "s3://s3.amazonaws.com:443/test/TestBackup.bak";
                    string options = "{\"s3\": {\"region\":\"us-west-2\"}}";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.CopyOnly = true;
                    backup.Devices.AddDevice(s3Url, _SMO.DeviceType.Url);
                    backup.Options = options;

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  URL = N'{s3Url}' WITH  COPY_ONLY, NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10, BACKUP_OPTIONS = '{options}'"));

                });
        }

        /// <summary>
        /// Tests scripting an azure blob backup through SMO on SQL 16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToAzureBlobStorage_Sql16AndAfter()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string url = "https://myaccount.blob.core.windows.net/mycontainer/TestBackup.bak";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.CopyOnly = true;
                    backup.Devices.AddDevice(url, _SMO.DeviceType.Url);

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  URL = N'{url}' WITH  COPY_ONLY, NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting a file backup through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToFile_Standalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string filePath = "C:\\Documents\\filePath.bak";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.Devices.AddDevice(filePath, _SMO.DeviceType.File);

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  DISK = N'{filePath}' WITH NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting a tape backup through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToTape_Standalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string tapeName = "\\\\.\\tape0";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.Devices.AddDevice(tapeName, _SMO.DeviceType.Tape);

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  TAPE = N'{tapeName}' WITH NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting a logical device backup through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToLogicalDevice_Standalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string filePath = "C:\\Documents\\filePath.bak";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.Devices.AddDevice(filePath, _SMO.DeviceType.LogicalDevice);

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  [{filePath}] WITH NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting a virtual device backup through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToVirtualDevice_Standalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string filePath = "C:\\Documents\\filePath.bak";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.Devices.AddDevice(filePath, _SMO.DeviceType.VirtualDevice);

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  VIRTUAL_DEVICE = N'{filePath}' WITH NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting an azure blob backup through SMO on SQL MI.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_BackupToAzureBlobStorage_ManagedInstance()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string url = "https://myaccount.blob.core.windows.net/mycontainer/TestBackup.bak";

                    _SMO.Backup backup = new _SMO.Backup();
                    backup.Database = database.Name;
                    backup.CopyOnly = true;
                    backup.Devices.AddDevice(url, _SMO.DeviceType.Url);

                    Assert.That(backup.Script(server), Is.EqualTo($"BACKUP DATABASE [{_SMO.SqlSmoObject.SqlBraket(backup.Database)}] TO  URL = N'{url}' WITH  BLOCKSIZE = 65536,  MAXTRANSFERSIZE = 4194304,  COPY_ONLY, NOFORMAT, NOINIT, NOSKIP, REWIND, NOUNLOAD,  STATS = 10"));

                });
        }

        #endregion // Scripting Tests
    }
}

