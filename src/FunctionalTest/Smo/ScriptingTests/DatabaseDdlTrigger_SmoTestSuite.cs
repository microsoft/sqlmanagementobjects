// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Database Trigger properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseDdlTrigger_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.DatabaseDdlTrigger triggerDb = (_SMO.DatabaseDdlTrigger)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Triggers.Refresh();
            Assert.IsNull(database.Triggers[triggerDb.Name],
                          "Current database trigger not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a database trigger with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_DatabaseDdlTrigger_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.DatabaseDdlTrigger triggerDb = new _SMO.DatabaseDdlTrigger(database, GenerateSmoObjectName("trg"));

                    triggerDb.TextHeader = string.Format("CREATE TRIGGER [{0}] ON DATABASE FOR DROP_TABLE AS", triggerDb.Name);
                    triggerDb.TextBody = "PRINT 'Table is deleted!'";
                    triggerDb.ImplementationType = _SMO.ImplementationType.TransactSql;
                    triggerDb.ExecutionContext = _SMO.DatabaseDdlTriggerExecutionContext.Caller;

                    string triggerDbScriptDropIfExistsTemplate = "DROP TRIGGER IF EXISTS [{0}]";
                    string triggerDbScriptDropIfExists = string.Format(triggerDbScriptDropIfExistsTemplate, triggerDb.Name);

                    VerifySmoObjectDropIfExists(triggerDb, database, triggerDbScriptDropIfExists);
                });
        }

        #endregion
    }
}
