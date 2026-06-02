// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Trigger properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Trigger_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Trigger trigger = (_SMO.Trigger)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.Triggers.Refresh();
            Assert.IsNull(table.Triggers[trigger.Name],
                          "Current trigger not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a trigger with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Trigger_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Table table = database.CreateTable(this.TestContext.TestName);
                    _SMO.Trigger trigger = new _SMO.Trigger(table, GenerateSmoObjectName("trg"));

                    trigger.TextMode = false;
                    trigger.Insert = true;
                    trigger.Update = true;
                    trigger.InsertOrder = ActivationOrder.First;
                    trigger.TextBody = "SELECT 'Trigger testing.'";
                    trigger.ImplementationType = _SMO.ImplementationType.TransactSql;

                    string triggerScriptDropIfExistsTemplate = "DROP TRIGGER IF EXISTS [{0}].[{1}]";
                    string triggerScriptDropIfExists = string.Format(triggerScriptDropIfExistsTemplate, table.Schema, trigger.Name);

                    VerifySmoObjectDropIfExists(trigger, table, triggerScriptDropIfExists);
                });
        }

        /// <summary>
        /// Tests create or alter a trigger through SMO on SQL16 and later.
        /// 1. Create the trigger, verify ScriptCreateOrAlter text and verify the object was created correctly
        /// 2. Alter the trigger, verify ScriptCreateOrAlter text and verify the object was updated correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoCreateOrAlter_Trigger_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    // 1. Create the trigger, verify ScriptCreateOrAlter and check existence

                    _SMO.Table table = DatabaseObjectHelpers.CreateTable(database, "trgTbl");
                    _SMO.Trigger trigger = TableObjectHelpers.CreateTriggerDefinition(
                                                table,
                                                "trigger",
                                                textBody: "SELECT 'Create trigger testing.'");
                    trigger.TextHeader = string.Format("CREATE OR ALTER TRIGGER {0} ON {1}.{2} FOR INSERT AS",
                                                       SmoObjectHelpers.SqlBracketQuoteString(trigger.Name),
                                                       SmoObjectHelpers.SqlBracketQuoteString(table.Schema),
                                                       SmoObjectHelpers.SqlBracketQuoteString(table.Name));

                    VerifySmoObjectCreateOrAlterForCreate(
                        database,
                        trigger,
                        string.Format(@"CREATE OR ALTER TRIGGER {0}.{1} ON {2} FOR INSERT AS SELECT 'Create trigger testing.'", 
                            SmoObjectHelpers.SqlBracketQuoteString(table.Schema),
                            SmoObjectHelpers.SqlBracketQuoteString(trigger.Name),
                            table.FullQualifiedName));

                    // 2. Alter the trigger, verify ScriptCreateOrAlter and check existence

                    trigger.TextHeader = string.Format("CREATE OR ALTER TRIGGER {0} ON {1}.{2} FOR INSERT AS",
                                                       SmoObjectHelpers.SqlBracketQuoteString(trigger.Name),
                                                       SmoObjectHelpers.SqlBracketQuoteString(table.Schema),
                                                       SmoObjectHelpers.SqlBracketQuoteString(table.Name));
                    trigger.TextBody = "SELECT 'Alter trigger testing.'";

                    VerifySmoObjectCreateOrAlterForAlter(
                        database,
                        trigger,
                        string.Format(@"SET ANSI_NULLS ON SET QUOTED_IDENTIFIER ON CREATE OR ALTER   TRIGGER {0}.{1} ON {2} FOR INSERT AS SELECT 'Alter trigger testing.' ALTER TABLE {2} ENABLE TRIGGER {1}",
                            SmoObjectHelpers.SqlBracketQuoteString(table.Schema),
                            SmoObjectHelpers.SqlBracketQuoteString(trigger.Name),
                            table.FullQualifiedName));
                });
        }

        /// <summary>
        /// Tests CreateOrAlter() is not supported for trigger through SMO on SQL14 and before.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MaxMajor = 12)]
        public void SmoCreateOrAlter_Trigger_Sql14AndBeforeOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Table table = DatabaseObjectHelpers.CreateTable(database, "trgTbl");
                    _SMO.Trigger trigger = TableObjectHelpers.CreateTriggerDefinition(
                                                table,
                                                "trigger",
                                                textBody: "SELECT 'Create trigger testing.'");
                    trigger.TextHeader = string.Format("CREATE OR ALTER TRIGGER {0} ON {1}.{2} FOR INSERT AS",
                                                       SmoObjectHelpers.SqlBracketQuoteString(trigger.Name),
                                                       SmoObjectHelpers.SqlBracketQuoteString(table.Schema),
                                                       SmoObjectHelpers.SqlBracketQuoteString(table.Name));

                    _SMO.FailedOperationException e = Assert.Throws<_SMO.FailedOperationException>(
                        () => trigger.CreateOrAlter(),
                        string.Format(
                            "Expected FailedOperationException with message containing \"CreateOrAlter failed for Trigger '{0}'.\" when calling CreateOrAlter against unsupported downlevel servers, but no such exception was thrown",
                            trigger.Name));
                    Assert.That(e.Message, Does.Contain(string.Format("CreateOrAlter failed for Trigger '{0}'.", trigger.Name)), "Unexpected error message.");
                });
        }

        /// <summary>
        /// Tests that creating a DML trigger via SMO results in an enabled trigger.
        /// Verifies the IsEnabled property reflects the correct state.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoTrigger_CreateDmlTrigger_IsEnabled()
        {
            ExecuteFromDbPool(this.TestContext.FullyQualifiedTestClassName, database =>
                {
                    // Create a table
                    var table = database.CreateTable(this.TestContext.TestName, new ColumnProperties("c1"));

                    // Create a trigger using SMO
                    var trigger = new _SMO.Trigger(table, GenerateSmoObjectName("trg"));
                    trigger.TextMode = false;
                    trigger.Insert = true;
                    trigger.Update = true;
                    trigger.TextBody = "RAISERROR('Trigger fired', 1, 1)";
                    trigger.ImplementationType = _SMO.ImplementationType.TransactSql;
                    trigger.Create();

                    // Refresh and verify
                    table.Triggers.Refresh();
                    var createdTrigger = table.Triggers[trigger.Name];
                    Assert.IsNotNull(createdTrigger, "Trigger should exist");
                    Assert.That(createdTrigger.IsEnabled, Is.True, "Trigger should be enabled after creation");
                });
        }

        /// <summary>
        /// Tests that scripting a disabled DML trigger at the trigger level produces a script with DISABLE TRIGGER.
        /// Verifies that dropping and re-creating the trigger from the script preserves the disabled state.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoTrigger_ScriptDisabledTrigger_RoundTrip()
        {
            ExecuteFromDbPool(this.TestContext.FullyQualifiedTestClassName, database =>
                {
                    // Create a table with a column
                    var tableName = GenerateSmoObjectName("tbl");
                    var table = database.CreateTable(tableName, new ColumnProperties("c1"));

                    // Create a DML trigger via T-SQL
                    var triggerName = GenerateSmoObjectName("trg");
                    database.ExecuteNonQuery(
                        $"CREATE TRIGGER {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(triggerName)} ON {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(table.Name)} AFTER INSERT AS BEGIN SET NOCOUNT ON; END");

                    // Disable the trigger via T-SQL
                    database.ExecuteNonQuery($"DISABLE TRIGGER {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(triggerName)} ON {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(table.Name)}");

                    // Retrieve via SMO and script at the trigger level
                    table.Triggers.Refresh();
                    var smoTrigger = table.Triggers[triggerName];
                    Assert.IsNotNull(smoTrigger, "Trigger should exist");
                    Assert.That(smoTrigger.IsEnabled, Is.False, "Trigger should be disabled");

                    var scripts = smoTrigger.Script(new _SMO.ScriptingOptions { IncludeDatabaseContext = false });

                    // Verify script contains DISABLE TRIGGER
                    var scriptText = string.Join("\n", scripts.Cast<string>());
                    Assert.That(scriptText, Does.Contain("DISABLE TRIGGER"),
                        "Script should contain DISABLE TRIGGER statement");

                    // Drop the trigger
                    smoTrigger.Drop();
                    table.Triggers.Refresh();
                    Assert.IsNull(table.Triggers[triggerName], "Trigger should be dropped");

                    // Re-create from scripts
                    foreach (string script in scripts)
                    {
                        database.ExecuteNonQuery(script);
                    }

                    // Verify the re-created trigger is disabled
                    table.Triggers.Refresh();
                    var recreated = table.Triggers[triggerName];
                    Assert.IsNotNull(recreated, "Trigger should be re-created from script");
                    Assert.That(recreated.IsEnabled, Is.False,
                        "Trigger should be disabled after round-trip from script");
                });
        }

        /// <summary>
        /// Tests that scripting a table with a disabled DML trigger (at the table level with Triggers=true)
        /// produces a script with DISABLE TRIGGER. Verifies that dropping and re-creating the table from
        /// the script preserves the disabled trigger state.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoTrigger_ScriptTableWithDisabledTrigger_RoundTrip()
        {
            ExecuteFromDbPool(this.TestContext.FullyQualifiedTestClassName, database =>
                {
                    // Create a table with a column
                    var tableName = GenerateSmoObjectName("tbl");
                    var table = database.CreateTable(tableName, new ColumnProperties("c1"));

                    // Create a DML trigger via T-SQL
                    var triggerName = GenerateSmoObjectName("trg");
                    database.ExecuteNonQuery(
                        $"CREATE TRIGGER {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(triggerName)} ON {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(table.Name)} AFTER INSERT AS BEGIN SET NOCOUNT ON; END");

                    // Disable the trigger via T-SQL
                    database.ExecuteNonQuery($"DISABLE TRIGGER {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(triggerName)} ON {_SMO.SqlSmoObject.MakeSqlBraket("dbo")}.{_SMO.SqlSmoObject.MakeSqlBraket(table.Name)}");

                    // Retrieve via SMO and script at the table level with Triggers=true
                    database.Tables.Refresh();
                    var smoTable = database.Tables[table.Name];
                    Assert.IsNotNull(smoTable, "Table should exist");

                    var scripts = smoTable.Script(new _SMO.ScriptingOptions { Triggers = true, IncludeDatabaseContext = false });

                    // Verify script contains DISABLE TRIGGER
                    var scriptText = string.Join("\n", scripts.Cast<string>());
                    Assert.That(scriptText, Does.Contain("DISABLE TRIGGER"),
                        "Script should contain DISABLE TRIGGER statement when scripting table with disabled trigger");

                    // Drop the entire table
                    smoTable.Drop();
                    database.Tables.Refresh();
                    Assert.IsNull(database.Tables[table.Name], "Table should be dropped");

                    // Re-execute all scripts
                    foreach (string script in scripts)
                    {
                        database.ExecuteNonQuery(script);
                    }

                    // Verify the re-created table and trigger
                    database.Tables.Refresh();
                    var recreatedTable = database.Tables[table.Name];
                    Assert.IsNotNull(recreatedTable, "Table should be re-created");

                    recreatedTable.Triggers.Refresh();
                    var recreatedTrigger = recreatedTable.Triggers[triggerName];
                    Assert.IsNotNull(recreatedTrigger, "Trigger should be re-created");
                    Assert.That(recreatedTrigger.IsEnabled, Is.False,
                        "Trigger should be disabled when scripted at table level");
                });
        }

        #endregion
    }
}
