// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests SQL string handling in managed backup queries and scripts.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class SmartAdminTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("TestDb", "N'TestDb'")]
        [DataRow("O'Brien", "N'O''Brien'")]
        [DataRow("Two'Quoted'Parts", "N'Two''Quoted''Parts'")]
        [DataRow("Database with spaces", "N'Database with spaces'")]
        [DataRow("Database[brackets]", "N'Database[brackets]'")]
        [DataRow("\u6570\u636e\u5e93'\u540d", "N'\u6570\u636e\u5e93''\u540d'")]
        public void SmartAdmin_Refresh_QuotesDatabaseNameAsUnicodeLiteral(string databaseName, string expectedLiteral)
        {
            var smartAdmin = CreateSmartAdmin(databaseName, captureSql: true);
            var connection = smartAdmin.Parent.ConnectionContext;
            // Capture-only execution returns no result set and never opens a SQL connection.
            connection.SqlExecutionModes = SqlExecutionModes.CaptureSql;

            smartAdmin.Refresh();

            var expectedQuery = $@"SELECT is_availability_database,
                        is_dropped,
                        is_managed_backup_enabled,
                        credential_name,
                        retention_days,
                        storage_url,
                        encryption_algorithm,
                        encryptor_type,
                        encryptor_name
                    FROM  [msdb].[smart_admin].[fn_backup_db_config]({expectedLiteral})";

            Assert.That(connection.CapturedSql.Text.Count, Is.EqualTo(1),
                "Database-level refresh should issue exactly one query.");
            Assert.That(connection.CapturedSql.Text[0].Replace("\r\n", "\n"),
                Is.EqualTo(expectedQuery.Replace("\r\n", "\n")),
                "The complete refresh query must contain an escaped Unicode database-name literal.");
        }

        [DataTestMethod]
        [DataRow("TestDb", "N'TestDb'", false)]
        [DataRow("TestDb", "N'TestDb'", true)]
        [DataRow("O'Brien", "N'O''Brien'", false)]
        [DataRow("O'Brien", "N'O''Brien'", true)]
        [DataRow("Two'Quoted'Parts", "N'Two''Quoted''Parts'", false)]
        [DataRow("Two'Quoted'Parts", "N'Two''Quoted''Parts'", true)]
        [DataRow("Database with spaces", "N'Database with spaces'", false)]
        [DataRow("Database with spaces", "N'Database with spaces'", true)]
        [DataRow("Database[brackets]", "N'Database[brackets]'", false)]
        [DataRow("Database[brackets]", "N'Database[brackets]'", true)]
        [DataRow("\u6570\u636e\u5e93'\u540d", "N'\u6570\u636e\u5e93''\u540d'", false)]
        [DataRow("\u6570\u636e\u5e93'\u540d", "N'\u6570\u636e\u5e93''\u540d'", true)]
        public void SmartAdmin_Script_QuotesDatabaseNameAsUnicodeLiteral(
            string databaseName, string expectedLiteral, bool scriptForAlter)
        {
            var smartAdmin = CreateSmartAdmin(databaseName);
            var options = new ScriptingOptions
            {
                TargetServerVersion = SqlServerVersion.Version120,
                ScriptForAlter = scriptForAlter
            };

            var scripts = smartAdmin.Script(options);

            var expectedScript = "EXEC [msdb].[smart_admin].[sp_set_db_backup] " +
                $"@enable_backup = 1, @database_name = {expectedLiteral}, " +
                "@retention_days = 7, @credential_name = N'TestCredential', " +
                "@storage_url = N'https://example.invalid/backups', " +
                "@encryption_algorithm = N'AES_256', @encryptor_type = N'CERTIFICATE', " +
                "@encryptor_name = N'TestCertificate';";

            Assert.That(scripts, Is.EqualTo(new[] { expectedScript }),
                "The complete database backup script must contain an escaped Unicode database-name literal.");
        }

        [DataTestMethod]
        [DataRow(null, false)]
        [DataRow(null, true)]
        [DataRow("", false)]
        [DataRow("", true)]
        public void SmartAdmin_Script_WithoutDatabaseName_PreservesInstanceConfiguration(
            string databaseName, bool scriptForAlter)
        {
            var smartAdmin = CreateSmartAdmin(databaseName);
            var options = new ScriptingOptions
            {
                TargetServerVersion = SqlServerVersion.Version120,
                ScriptForAlter = scriptForAlter
            };

            var scripts = smartAdmin.Script(options);

            var expectedScript = "EXEC [msdb].[smart_admin].[sp_set_instance_backup] " +
                "@enable_backup = 1, @retention_days = 7, @credential_name = N'TestCredential', " +
                "@storage_url = N'https://example.invalid/backups', " +
                "@encryption_algorithm = N'AES_256', @encryptor_type = N'CERTIFICATE', " +
                "@encryptor_name = N'TestCertificate';";

            Assert.That(scripts, Is.EqualTo(new[]
                {
                    "EXEC [msdb].[smart_admin].[sp_backup_master_switch] @new_state  = 1;",
                    expectedScript
                }),
                "Null and empty names must retain instance-level scripting without a database argument.");
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void SmartAdmin_Refresh_WithoutDatabaseName_PreservesInstanceRefresh(string databaseName)
        {
            var smartAdmin = CreateSmartAdmin(databaseName);
            var connection = smartAdmin.Parent.ConnectionContext;
            connection.SqlExecutionModes = SqlExecutionModes.CaptureSql;

            smartAdmin.Refresh();

            Assert.That(connection.CapturedSql.Text, Is.Empty,
                "Instance-level refresh should clear cached singleton properties without a database-level query.");
            Assert.That(smartAdmin.Properties.Get(nameof(SmartAdmin.BackupEnabled)).Dirty, Is.False,
                "Instance-level refresh should discard the previously modified backup setting.");
        }

        [DataTestMethod]
        [DataRow(nameof(SmartAdmin.CredentialName), false, false)]
        [DataRow(nameof(SmartAdmin.CredentialName), false, true)]
        [DataRow(nameof(SmartAdmin.CredentialName), true, false)]
        [DataRow(nameof(SmartAdmin.CredentialName), true, true)]
        [DataRow(nameof(SmartAdmin.StorageUrl), false, false)]
        [DataRow(nameof(SmartAdmin.StorageUrl), false, true)]
        [DataRow(nameof(SmartAdmin.StorageUrl), true, false)]
        [DataRow(nameof(SmartAdmin.StorageUrl), true, true)]
        [DataRow(nameof(SmartAdmin.EncryptionAlgorithm), false, false)]
        [DataRow(nameof(SmartAdmin.EncryptionAlgorithm), false, true)]
        [DataRow(nameof(SmartAdmin.EncryptionAlgorithm), true, false)]
        [DataRow(nameof(SmartAdmin.EncryptionAlgorithm), true, true)]
        [DataRow(nameof(SmartAdmin.EncryptorType), false, false)]
        [DataRow(nameof(SmartAdmin.EncryptorType), false, true)]
        [DataRow(nameof(SmartAdmin.EncryptorType), true, false)]
        [DataRow(nameof(SmartAdmin.EncryptorType), true, true)]
        [DataRow(nameof(SmartAdmin.EncryptorName), false, false)]
        [DataRow(nameof(SmartAdmin.EncryptorName), false, true)]
        [DataRow(nameof(SmartAdmin.EncryptorName), true, false)]
        [DataRow(nameof(SmartAdmin.EncryptorName), true, true)]
        public void SmartAdmin_Script_EscapesEachStringParameter(
            string propertyName, bool databaseLevel, bool scriptForAlter)
        {
            var smartAdmin = CreateSmartAdmin(databaseLevel ? "TestDb" : null);
            var options = new ScriptingOptions
            {
                TargetServerVersion = SqlServerVersion.Version120,
                ScriptForAlter = scriptForAlter
            };
            smartAdmin.Properties.SetValueWithConsistencyCheck(propertyName, "Value'\u540d]'End");

            var scripts = smartAdmin.Script(options);

            const string escapedValue = "Value''\u540d]''End";
            var credential = propertyName == nameof(SmartAdmin.CredentialName) ? escapedValue : "TestCredential";
            var storageUrl = propertyName == nameof(SmartAdmin.StorageUrl) ? escapedValue : "https://example.invalid/backups";
            var algorithm = propertyName == nameof(SmartAdmin.EncryptionAlgorithm) ? escapedValue : "AES_256";
            var encryptorType = propertyName == nameof(SmartAdmin.EncryptorType) ? escapedValue : "CERTIFICATE";
            var encryptorName = propertyName == nameof(SmartAdmin.EncryptorName) ? escapedValue : "TestCertificate";
            var procedure = databaseLevel ? "sp_set_db_backup" : "sp_set_instance_backup";
            var databaseParameter = databaseLevel ? "@database_name = N'TestDb', " : string.Empty;
            var expectedScript = $"EXEC [msdb].[smart_admin].[{procedure}] @enable_backup = 1, " +
                $"{databaseParameter}@retention_days = 7, @credential_name = N'{credential}', " +
                $"@storage_url = N'{storageUrl}', @encryption_algorithm = N'{algorithm}', " +
                $"@encryptor_type = N'{encryptorType}', @encryptor_name = N'{encryptorName}';";
            var expectedScripts = databaseLevel
                ? new[] { expectedScript }
                : new[] { "EXEC [msdb].[smart_admin].[sp_backup_master_switch] @new_state  = 1;", expectedScript };

            Assert.That(scripts, Is.EqualTo(expectedScripts),
                $"The complete backup configuration script must escape {propertyName} independently.");
        }

        private static SmartAdmin CreateSmartAdmin(string databaseName, bool captureSql = false)
        {
            var server = ServerTests.GetDesignModeServer(12);
            if (captureSql)
            {
                // Refresh checks for forced disconnection before capturing SQL, so use a capture-only connection.
                server = new Management.Smo.Server(new ServerConnection
                {
                    ServerVersion = new ServerVersion(12, 0),
                    TrueName = "captureOnly",
                    DatabaseEngineType = DatabaseEngineType.Standalone,
                    DatabaseEngineEdition = DatabaseEngineEdition.Enterprise,
                    SqlExecutionModes = SqlExecutionModes.CaptureSql
                });
                server.m_comparer = new Management.Smo.StringComparer(string.Empty, 1033);
            }
            var smartAdmin = new SmartAdmin(server, new ObjectKeyBase(), SqlSmoState.Existing);
            smartAdmin.MasterSwitch = true;
            smartAdmin.BackupEnabled = true;
            smartAdmin.BackupRetentionPeriodInDays = 7;
            smartAdmin.CredentialName = "TestCredential";
            smartAdmin.StorageUrl = "https://example.invalid/backups";
            smartAdmin.EncryptionAlgorithm = "AES_256";
            smartAdmin.EncryptorType = "CERTIFICATE";
            smartAdmin.EncryptorName = "TestCertificate";
            smartAdmin.DatabaseName = databaseName;
            return smartAdmin;
        }
    }
}
