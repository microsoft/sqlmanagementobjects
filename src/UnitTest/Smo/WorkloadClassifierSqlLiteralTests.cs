// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

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
    public class WorkloadClassifierSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow(nameof(WorkloadManagementWorkloadClassifier.GroupName), "WORKLOAD_GROUP", "Group")]
        [DataRow(nameof(WorkloadManagementWorkloadClassifier.MemberName), "MEMBERNAME", "Member")]
        [DataRow(nameof(WorkloadManagementWorkloadClassifier.WlmLabel), "WLM_LABEL", "Label")]
        [DataRow(nameof(WorkloadManagementWorkloadClassifier.WlmContext), "WLM_CONTEXT", "Context")]
        [DataRow(nameof(WorkloadManagementWorkloadClassifier.StartTime), "START_TIME", "08:00")]
        [DataRow(nameof(WorkloadManagementWorkloadClassifier.EndTime), "END_TIME", "17:00")]
        public void WorkloadClassifier_ScriptCreate_EscapesEachStringAndPreservesPresenceAndDirtyFiltering(
            string propertyName, string optionName, string ordinaryValue)
        {
            foreach (var value in new[] { null, string.Empty, ordinaryValue, "O'Brien", "名'前", "Two''Quotes" })
            {
                foreach (bool scriptForAlter in new[] { false, true })
                {
                    foreach (bool dirty in new[] { false, true })
                    {
                        var server = ServerTests.GetDesignModeServer(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse);
                        var database = new Database(server, "TestDatabase", DatabaseEngineEdition.SqlDataWarehouse);
                        var classifier = new WorkloadManagementWorkloadClassifier(database, "TestClassifier");
                        if (value != null)
                        {
                            SetOption(classifier, propertyName, value);
                        }
                        if (!dirty)
                        {
                            classifier.Properties.SetAllDirtyAsRetrieved(true);
                            classifier.Properties.SetAllDirty(false);
                        }
                        var preferences = database.GetScriptingPreferencesForCreate();
                        preferences.ScriptForAlter = scriptForAlter;
                        var scripts = new StringCollection();

                        classifier.ScriptCreate(scripts, preferences);

                        var expected = "CREATE WORKLOAD CLASSIFIER [TestClassifier]";
                        if (value != null && (!scriptForAlter || dirty))
                        {
                            expected += $" WITH({optionName}=N'{value.Replace("'", "''")}')";
                        }
                        Assert.That(scripts.Cast<string>(), Is.EqualTo(new[] { expected }),
                            $"{propertyName}: value={value ?? "<null>"}, ScriptForAlter={scriptForAlter}, dirty={dirty}. Empty CREATE options must not be suppressed.");
                        if (value != null)
                        {
                            Assert.That(classifier.Properties.Get(propertyName).Value, Is.EqualTo(value),
                                $"{propertyName} must remain unchanged after scripting.");
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void WorkloadClassifier_ScriptCreate_KeepsAllOptionsAndImportanceSeparated()
        {
            var server = ServerTests.GetDesignModeServer(12, DatabaseEngineType.SqlAzureDatabase, DatabaseEngineEdition.SqlDataWarehouse);
            var database = new Database(server, "TestDatabase", DatabaseEngineEdition.SqlDataWarehouse);
            var classifier = new WorkloadManagementWorkloadClassifier(database, "TestClassifier")
            {
                GroupName = "Group's",
                MemberName = "Member's",
                WlmLabel = string.Empty,
                WlmContext = "Context's",
                StartTime = "08:00",
                EndTime = "17:00",
                Importance = WorkloadManagementImportance.Normal
            };
            var scripts = new StringCollection();

            classifier.ScriptCreate(scripts, database.GetScriptingPreferencesForCreate());

            var separator = ", " + System.Environment.NewLine + "\t\t";
            var expected = "CREATE WORKLOAD CLASSIFIER [TestClassifier] WITH(" + string.Join(separator, new[]
            {
                "WORKLOAD_GROUP=N'Group''s'", "MEMBERNAME=N'Member''s'", "WLM_LABEL=N''",
                "WLM_CONTEXT=N'Context''s'", "START_TIME=N'08:00'", "END_TIME=N'17:00'", "IMPORTANCE=NORMAL"
            }) + ")";
            Assert.That(scripts.Cast<string>(), Is.EqualTo(new[] { expected }),
                "Escaping must preserve ordering, separators, empty options, and unquoted importance.");
        }

        private static void SetOption(WorkloadManagementWorkloadClassifier classifier, string propertyName, string value)
        {
            switch (propertyName)
            {
                case nameof(WorkloadManagementWorkloadClassifier.GroupName):
                    classifier.GroupName = value;
                    break;
                case nameof(WorkloadManagementWorkloadClassifier.MemberName):
                    classifier.MemberName = value;
                    break;
                case nameof(WorkloadManagementWorkloadClassifier.WlmLabel):
                    classifier.WlmLabel = value;
                    break;
                case nameof(WorkloadManagementWorkloadClassifier.WlmContext):
                    classifier.WlmContext = value;
                    break;
                case nameof(WorkloadManagementWorkloadClassifier.StartTime):
                    classifier.StartTime = value;
                    break;
                case nameof(WorkloadManagementWorkloadClassifier.EndTime):
                    classifier.EndTime = value;
                    break;
                default:
                    Assert.Fail($"Unexpected classifier string property: {propertyName}");
                    break;
            }
        }
    }
}
