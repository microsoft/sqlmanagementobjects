// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing DefaultRule properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DefaultRule_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Database database = (_SMO.Database)objVerify;

            if (obj is _SMO.Default)
            {
                _SMO.Default def = (_SMO.Default)obj;

                database.Defaults.Refresh();
                Assert.IsNull(database.Defaults[def.Name],
                              "Current default not dropped with DropIfExists.");
            }
            else
            {
                _SMO.Rule rule = (_SMO.Rule)obj;

                database.Rules.Refresh();
                Assert.IsNull(database.Rules[rule.Name],
                              "Current rule not dropped with DropIfExists.");
            }
        }

        /// <summary>
        /// Tests dropping a default with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Default_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Default def = new _SMO.Default(database, GenerateSmoObjectName("def"));

                    def.TextHeader = string.Format("CREATE DEFAULT [{0}] AS", def.Name);
                    def.TextBody = "GetDate()";

                    string defaultScriptDropIfExistsTemplate = "DROP DEFAULT IF EXISTS [{0}].[{1}]";
                    string defaultScriptDropIfExists = string.Format(defaultScriptDropIfExistsTemplate, def.Schema, def.Name);

                    VerifySmoObjectDropIfExists(def, database, defaultScriptDropIfExists);
                });
        }

        /// <summary>
        /// Tests dropping a rule with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Rule_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.Rule rule = new _SMO.Rule(database, GenerateSmoObjectName("rule"));

                    rule.TextHeader = string.Format("CREATE RULE [{0}] AS", rule.Name);
                    rule.TextBody = "@value BETWEEN GETDATE() AND DATEADD(year, 4, GETDATE())";

                    string ruleScriptDropIfExistsTemplate = "DROP RULE IF EXISTS [{0}].[{1}]";
                    string ruleScriptDropIfExists = string.Format(ruleScriptDropIfExistsTemplate, rule.Schema, rule.Name);

                    VerifySmoObjectDropIfExists(rule, database, ruleScriptDropIfExists);
                });
        }

        #endregion
    }
}
