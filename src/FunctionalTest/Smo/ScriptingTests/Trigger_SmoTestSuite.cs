// Copyright (c) Microsoft.
// Licensed under the MIT license.

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
        #endregion
    }
}
