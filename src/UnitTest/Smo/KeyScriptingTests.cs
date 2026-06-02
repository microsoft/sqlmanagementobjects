// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for AsymmetricKey and SymmetricKey scripting to verify
    /// PROVIDER_KEY_NAME is properly quoted using MakeSqlString.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class KeyScriptingTests : UnitTestBase
    {
        /// <summary>
        /// Verifies that AsymmetricKey ScriptCreate produces the correct full script
        /// for the cryptographic provider code path, with PROVIDER_KEY_NAME properly
        /// quoted using MakeSqlString to prevent SQL injection.
        /// </summary>
        [TestMethod]
        [DataRow("MyProviderKey", "N'MyProviderKey'", DisplayName = "Simple key name")]
        [DataRow("Key'With'Quotes", "N'Key''With''Quotes'", DisplayName = "Key name with single quotes")]
        public void AsymmetricKey_ScriptCreate_Provider_QuotesProviderKeyNameWithMakeSqlString(string providerKeyName, string expectedQuotedKeyName)
        {
            var server = ServerTests.GetDesignModeServer(10);
            var database = new Database(server, "TestDb");
            var asymmetricKey = new AsymmetricKey(database, "TestAsymKey",
                "RSA_2048", providerKeyName, CreateDispositionType.CreateNew, AsymmetricKeySourceType.Provider);

            // Set the ProviderName property required by the Provider code path
            ((IAlienObject)asymmetricKey).SetPropertyValue(
                nameof(AsymmetricKey.ProviderName),
                typeof(string),
                "TestProvider");

            var query = new StringCollection();
            var sp = database.GetScriptingPreferencesForCreate();
            asymmetricKey.ScriptCreate(query, sp);

            var nl = System.Environment.NewLine;
            var expectedScript =
                $"CREATE ASYMMETRIC KEY [TestAsymKey]{nl}" +
                $"FROM PROVIDER [TestProvider]{nl}" +
                $"WITH {nl}" +
                $"\tPROVIDER_KEY_NAME = {expectedQuotedKeyName}, {nl}" +
                $"\tALGORITHM = RSA_2048, {nl}" +
                $"\tCREATION_DISPOSITION = CREATE_NEW";

            Assert.That(query.Count, Is.EqualTo(1), "Provider path should produce exactly one script statement");
            Assert.That(query[0], Is.EqualTo(expectedScript), "Full CREATE ASYMMETRIC KEY script mismatch");
        }

        /// <summary>
        /// Verifies that SymmetricKey ScriptCreate produces the correct full script
        /// for the cryptographic provider code path, with PROVIDER_KEY_NAME properly
        /// quoted using MakeSqlString to prevent SQL injection.
        /// </summary>
        [TestMethod]
        [DataRow("MyProviderKey", "N'MyProviderKey'", DisplayName = "Simple key name")]
        [DataRow("Key'With'Quotes", "N'Key''With''Quotes'", DisplayName = "Key name with single quotes")]
        public void SymmetricKey_ScriptCreate_Provider_QuotesProviderKeyNameWithMakeSqlString(string providerKeyName, string expectedQuotedKeyName)
        {
            var server = ServerTests.GetDesignModeServer(10);
            var database = new Database(server, "TestDb");
            var keyEncryption = new SymmetricKeyEncryption(KeyEncryptionType.Provider, "TestProvider");
            var symmetricKey = new SymmetricKey(database, "TestSymKey",
                keyEncryption, "AES_256", providerKeyName, CreateDispositionType.CreateNew);

            var query = new StringCollection();
            var sp = database.GetScriptingPreferencesForCreate();
            symmetricKey.ScriptCreate(query, sp);

            var nl = System.Environment.NewLine;
            var expectedScript =
                $"CREATE SYMMETRIC KEY [TestSymKey]{nl}" +
                $"FROM PROVIDER [TestProvider]{nl}" +
                $"WITH {nl}" +
                $"\tPROVIDER_KEY_NAME = {expectedQuotedKeyName}, {nl}" +
                $"\tALGORITHM = AES_256, {nl}" +
                $"\tCREATION_DISPOSITION = CREATE_NEW";

            Assert.That(query.Count, Is.EqualTo(1), "Provider path should produce exactly one script statement");
            Assert.That(query[0], Is.EqualTo(expectedScript), "Full CREATE SYMMETRIC KEY script mismatch");
        }
    }
}
