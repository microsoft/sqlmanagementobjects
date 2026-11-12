// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AzureDatabaseSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("GeneralPurpose", "GP_Gen5_2", "GeneralPurpose", "GP_Gen5_2")]
        [DataRow("Edition's", "Objective's", "Edition''s", "Objective''s")]
        [DataRow("层'级", "目'标", "层''级", "目''标")]
        public void AzureDatabase_ScriptCreate_EscapesServiceTierWithoutChangingProperties(
            string edition, string objective, string escapedEdition, string escapedObjective)
        {
            var database = CreateDatabase(edition, objective);
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.ForDirectExecution = true;
            var scripts = new StringCollection();

            database.ScriptCreate(scripts, preferences);

            Assert.That(scripts.Cast<string>(), Is.EqualTo(new[]
            {
                $"CREATE DATABASE [TestDatabase]  (EDITION = '{escapedEdition}', SERVICE_OBJECTIVE = '{escapedObjective}');{Environment.NewLine}"
            }), "Azure CREATE must escape both quoted builder values without changing their literal modes.");
            Assert.That(database.AzureEdition, Is.EqualTo(edition), "Scripting must preserve the edition property.");
            Assert.That(database.AzureServiceObjective, Is.EqualTo(objective), "Scripting must preserve the objective property.");
        }

        [DataTestMethod]
        [DataRow(false, false)]
        [DataRow(true, false)]
        [DataRow(false, true)]
        [DataRow(true, true)]
        public void AzureDatabase_ScriptAlter_EscapesOnlyPresentDirtyServiceTierForDirectExecution(
            bool dirtyEdition, bool dirtyObjective)
        {
            var database = CreateDatabase("Edition's", "Objective's");
            database.SetState(SqlSmoState.Existing);
            database.Properties.SetAllDirtyAsRetrieved(true);
            database.Properties.SetAllDirty(false);
            if (dirtyEdition)
            {
                database.AzureEdition = "Edition's changed";
            }
            if (dirtyObjective)
            {
                database.AzureServiceObjective = "Objective's changed";
            }
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.ScriptForAlter = true;
            preferences.ForDirectExecution = true;
            var scripts = new StringCollection();

            database.ScriptAlter(scripts, preferences);

            var options = new[]
            {
                dirtyEdition ? "EDITION = 'Edition''s changed'" : null,
                dirtyObjective ? "SERVICE_OBJECTIVE = 'Objective''s changed'" : null
            }.Where(option => option != null);
            var expected = dirtyEdition || dirtyObjective
                ? new[] { $"ALTER DATABASE [TestDatabase] MODIFY ({string.Join(", ", options)});" }
                : new string[0];
            Assert.That(scripts.Cast<string>().Where(script => script.Contains(" MODIFY ")), Is.EqualTo(expected),
                $"Direct ALTER must preserve dirty filtering: edition={dirtyEdition}, objective={dirtyObjective}.");
            Assert.That(database.AzureEdition, Is.EqualTo(dirtyEdition ? "Edition's changed" : "Edition's"),
                "ALTER scripting must not replace the edition with escaped text.");
            Assert.That(database.AzureServiceObjective, Is.EqualTo(dirtyObjective ? "Objective's changed" : "Objective's"),
                "ALTER scripting must not replace the service objective with escaped text.");
        }

        [TestMethod]
        public void AzureDatabase_ScriptAlter_NonDirectScriptingIncludesCleanValuesAndOmitsNulls()
        {
            var database = CreateDatabase("Edition's", null);
            database.SetState(SqlSmoState.Existing);
            database.Properties.SetAllDirtyAsRetrieved(true);
            database.Properties.SetAllDirty(false);
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.ScriptForAlter = true;
            preferences.ForDirectExecution = false;
            var scripts = new StringCollection();

            database.ScriptAlter(scripts, preferences);

            Assert.That(scripts.Cast<string>().Where(script => script.Contains(" MODIFY ")),
                Is.EqualTo(new[] { "ALTER DATABASE [TestDatabase] MODIFY (EDITION = 'Edition''s');" }),
                "Non-direct scripting must include clean present values but not null values.");
        }

        [DataTestMethod]
        [DataRow(DatabaseEngineEdition.SqlDataWarehouse)]
        [DataRow(DatabaseEngineEdition.SqlOnDemand)]
        public void AzureDatabase_ScriptCreate_DoesNotCrossScriptSourceServiceTier(DatabaseEngineEdition targetEdition)
        {
            var database = CreateDatabase("Edition's", "Objective's");
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.TargetDatabaseEngineEdition = targetEdition;
            var scripts = new StringCollection();

            database.ScriptCreate(scripts, preferences);

            var script = string.Join(Environment.NewLine, scripts.Cast<string>());
            Assert.That(script, Does.Not.Contain("Edition").And.Not.Contain("Objective"),
                "A different target edition must not receive source provisioning strings.");
            if (targetEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                Assert.That(script, Does.Contain("EDITION = 'DataWarehouse', SERVICE_OBJECTIVE = 'DW100c'"),
                    "A warehouse target still requires its fixed provisioning defaults.");
            }
        }

        private static Database CreateDatabase(string edition, string objective)
        {
            var server = ServerTests.GetDesignModeServer(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDatabase);
            var database = new Database(server, "TestDatabase", DatabaseEngineEdition.SqlDatabase)
            {
                AzureEdition = edition
            };
            if (objective != null)
            {
                database.AzureServiceObjective = objective;
            }
            return database;
        }
    }
}
