// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ColumnMasterKeySqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("MSSQL_CERTIFICATE_STORE", "CurrentUser/My/Key", "MSSQL_CERTIFICATE_STORE", "CurrentUser/My/Key")]
        [DataRow("Provider's", "CurrentUser/My/O'Brien", "Provider''s", "CurrentUser/My/O''Brien")]
        [DataRow("提供者'名", "路径'名", "提供者''名", "路径''名")]
        [DataRow("Provider''Name", "Key''Path", "Provider''''Name", "Key''''Path")]
        public void ColumnMasterKey_ScriptCreate_EscapesProviderAndPathWithoutChangingProperties(
            string provider, string path, string escapedProvider, string escapedPath)
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var key = new ColumnMasterKey(database, "TestKey", provider, path);
            var preferences = database.GetScriptingPreferencesForCreate();
            var scripts = new StringCollection();

            key.ScriptCreate(scripts, preferences);

            var expected = $"CREATE COLUMN MASTER KEY [TestKey]{preferences.NewLine}WITH{preferences.NewLine}" +
                $"({preferences.NewLine}\tKEY_STORE_PROVIDER_NAME = N'{escapedProvider}',{preferences.NewLine}" +
                $"\tKEY_PATH = N'{escapedPath}'{preferences.NewLine})";
            Assert.That(scripts.Cast<string>(), Is.EqualTo(new[] { expected }),
                "The builder must receive escaped contents, not complete literals or raw values.");
            Assert.That(key.KeyStoreProviderName, Is.EqualTo(provider), "Scripting must preserve the provider property.");
            Assert.That(key.KeyPath, Is.EqualTo(path), "Scripting must preserve the key path property.");
        }
    }
}
