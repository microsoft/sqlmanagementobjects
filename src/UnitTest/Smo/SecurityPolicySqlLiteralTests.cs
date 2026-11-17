// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests literal encoding independently of security policy identifier quoting.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class SecurityPolicySqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("TestPolicy", "TestPolicy", "TestPolicy", "dbo", "dbo", "dbo")]
        [DataRow("TestPolicy", "TestPolicy", "TestPolicy", "OtherSchema", "OtherSchema", "OtherSchema")]
        [DataRow("O'Brien", "O''Brien", "O'Brien", "O'Schema", "O''Schema", "O'Schema")]
        [DataRow("\u540d'Policy]", "\u540d''Policy]", "\u540d'Policy]]",
            "\u540d'Schema]", "\u540d''Schema]", "\u540d'Schema]]")]
        public void SecurityPolicy_ScriptCreate_EscapesExistenceNameAndSchema(
            string name, string expectedLiteralName, string expectedIdentifierName,
            string schema, string expectedLiteralSchema, string expectedIdentifierSchema)
        {
            var database = new Database(ServerTests.GetDesignModeServer(17), "TestDb");
            var policy = new SecurityPolicy(database, name, schema, notForReplication: false, isEnabled: true);
            policy.SecurityPredicates.Add(new SecurityPredicate(policy, "dbo", "TestTable", 1, "[dbo].[TestPredicate]([Id])"));
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.IncludeScripts.ExistenceCheck = true;
            var scripts = new StringCollection();

            policy.ScriptCreate(scripts, preferences);

            Assert.That(scripts.Count, Is.EqualTo(1), "A single CREATE batch is expected.");
            Assert.That(scripts[0], Does.StartWith(
                $"IF NOT EXISTS (SELECT * FROM sys.security_policies WHERE name = N'{expectedLiteralName}' AND schema_id = SCHEMA_ID(N'{expectedLiteralSchema}'))"),
                "The existence check must compare the unqualified name and schema as escaped Unicode literals.");
            Assert.That(scripts[0], Does.Contain($"CREATE SECURITY POLICY [{expectedIdentifierSchema}].[{expectedIdentifierName}] "),
                "The CREATE statement must independently quote the policy identifier.");
        }

        [DataTestMethod]
        [DataRow(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase, true)]
        [DataRow(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase, false)]
        [DataRow(13, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise, true)]
        [DataRow(17, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise, true)]
        [DataRow(17, DatabaseEngineType.Standalone, DatabaseEngineEdition.Enterprise, false)]
        public void SecurityPolicy_ScriptDrop_UsesVersionAppropriateExistenceCheck(
            int majorVersion, DatabaseEngineType engineType, DatabaseEngineEdition edition, bool includeExistenceCheck)
        {
            var server = ServerTests.GetDesignModeServer(majorVersion, engineType, edition);
            server.ConnectionContext.ServerInstance = "unit-test-server.invalid";
            server.ConnectionContext.ConnectTimeout = 1;
            var database = new Database(server, "TestDb", edition);
            var policy = new SecurityPolicy(database, "\u540d'Policy]", "\u540d'Schema]", notForReplication: false, isEnabled: true);
            policy.SetState(SqlSmoState.Existing);
            var preferences = new ScriptingPreferences(policy);
            preferences.IncludeScripts.ExistenceCheck = includeExistenceCheck;
            var scripts = new StringCollection();

            policy.ScriptDrop(scripts, preferences);

            var expected = "DROP SECURITY POLICY " +
                (includeExistenceCheck && majorVersion >= 13 ? "IF EXISTS " : string.Empty) +
                "[\u540d'Schema]]].[\u540d'Policy]]]";
            if (includeExistenceCheck && majorVersion < 13)
            {
                var nl = preferences.NewLine;
                expected = "IF  EXISTS (SELECT * FROM sys.security_policies WHERE name = N'\u540d''Policy]' " +
                    $"AND schema_id = SCHEMA_ID(N'\u540d''Schema]')){nl}BEGIN{nl}{expected}{nl}END{nl}";
            }
            Assert.That(scripts, Is.EqualTo(new[] { expected }),
                "Azure v12 must use an escaped name-and-schema guard; modern targets must retain native IF EXISTS.");
        }

        [TestMethod]
        public void SecurityPolicy_ScriptDrop_RejectsUnsupportedStandaloneTarget()
        {
            var database = new Database(ServerTests.GetDesignModeServer(17), "TestDb");
            var policy = new SecurityPolicy(database, "TestPolicy", "dbo", notForReplication: false, isEnabled: true);
            var preferences = new ScriptingPreferences(policy);
            preferences.TargetServerVersion = SqlServerVersion.Version120;
            preferences.IncludeScripts.ExistenceCheck = true;
            var scripts = new StringCollection();

            Assert.That(() => policy.ScriptDrop(scripts, preferences), Throws.TypeOf<UnsupportedVersionException>(),
                "Adding an Azure v12 guard must not enable security policies on standalone SQL Server v12.");
            Assert.That(scripts, Is.Empty, "Unsupported targets must not produce a DROP batch.");
        }
    }
}
