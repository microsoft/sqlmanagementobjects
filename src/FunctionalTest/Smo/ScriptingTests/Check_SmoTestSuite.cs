// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Check properties and scripting
    /// </summary>
    [TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Check_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Check chk = (_SMO.Check)obj;
            _SMO.Table table = (_SMO.Table)objVerify;

            table.Checks.Refresh();
            Assert.IsNull(table.Checks[chk.Name],
                          "Current check constraint not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a check constraint with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        public void SmoDropIfExists_Check()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Table table = database.CreateTable( this.TestContext.TestName);
                    _SMO.Check chk = new _SMO.Check(table, GenerateSmoObjectName("chk"));

                    chk.Text = string.Format("{0} >= 0", table.Columns[0].Name);

                    const string checkScriptDropIfExistsTemplate = "ALTER TABLE {0} DROP CONSTRAINT IF EXISTS {1}";
                    string checkScriptDropIfExists = string.Format(checkScriptDropIfExistsTemplate, 
                        table.FormatFullNameForScripting(new _SMO.ScriptingPreferences()), 
                        chk.FormatFullNameForScripting(new _SMO.ScriptingPreferences()));

                    VerifySmoObjectDropIfExists(chk, table, checkScriptDropIfExists);
                });
        }

        #endregion // Scripting Tests
    }
}
