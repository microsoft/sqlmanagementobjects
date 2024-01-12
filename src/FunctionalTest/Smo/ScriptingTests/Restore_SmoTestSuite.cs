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
    /// Test suite for testing Restore properties and scripting
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Restore_SmoTestSuite : SmoTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Tests scripting an S3 restore through SMO on SQL22 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_Restore_Sql16AndAfter()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string s3Url = "s3://s3.amazonaws.com:443/test/TestBackup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(s3Url, _SMO.DeviceType.Url);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  URL = N'{s3Url}' WITH  NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting an S3 restore through SMO on SQL MI.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromS3Url_ManagedInstance()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string s3Url = "s3://s3.amazonaws.com:443/test/TestBackup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(s3Url, _SMO.DeviceType.Url);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  URL = N'{s3Url}'"));

                });
        }


        /// <summary>
        /// Tests scripting an S3 restore with region option through SMO on SQL22 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromS3WithRegion_Sql22AndAfter()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string url = "s3://s3.amazonaws.com:443/test/TestBackup.bak";
                    string options = "{\"s3\": {\"region\":\"{us-west-2}\"}}";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Options = options;
                    restore.Devices.AddDevice(url, _SMO.DeviceType.Url);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  URL = N'{url}' WITH  NOUNLOAD,  STATS = 10,  RESTORE_OPTIONS = '{options}'"));

                });
        }

        /// <summary>
        /// Tests scripting an Azure Blob restore through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromAzureBlob_Sql16AndAfter()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string url = "https://myaccount.blob.core.windows.net/mycontainer/TestBackup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(url, _SMO.DeviceType.Url);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  URL = N'{url}' WITH  NOUNLOAD,  STATS = 10"));
                });
        }

        /// <summary>
        /// Tests scripting an Azure Blob restore through SMO on SQL Managed Instance
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromAzureBlob_ManagedInstance()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string url = "https://myaccount.blob.core.windows.net/mycontainer/TestBackup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(url, _SMO.DeviceType.Url);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  URL = N'{url}'"));

                });
        }


        /// <summary>
        /// Tests scripting a file restore through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromFile_SqlStandalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string file = "C:\\Documents\\Backup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(file, _SMO.DeviceType.File);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  DISK = N'{file}' WITH  NOUNLOAD,  STATS = 10"));

                });
        }

        /// <summary>
        /// Tests scripting a tape restore through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromTape_SqlStandalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string file = "C:\\Documents\\Backup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(file, _SMO.DeviceType.Tape);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  TAPE = N'{file}' WITH  NOUNLOAD,  STATS = 10"));

                });
        }


        /// <summary>
        /// Tests scripting a logical device restore through SMO on SQL Standalone.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoScript_RestoreFromLogicalDevice_SqlStandalone()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    string file = "C:\\Documents\\Backup.bak";

                    _SMO.Restore restore = new _SMO.Restore();
                    restore.Database = database.Name;
                    restore.Devices.AddDevice(file, _SMO.DeviceType.LogicalDevice);

                    Assert.That(restore.Script(server)[0], Is.EqualTo($"RESTORE DATABASE [{_SMO.SqlSmoObject.SqlBraket(restore.Database)}] FROM  [{file}] WITH  NOUNLOAD,  STATS = 10"));

                });
        }

        #endregion // Scripting Tests
    }
}

