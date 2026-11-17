// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Synonym properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Synonym_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Synonym syn = (_SMO.Synonym)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Synonyms.Refresh();
            Assert.IsNull(database.Synonyms[syn.Name],
                          "Current synonym not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a synonym with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Synonym_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Synonym syn = new _SMO.Synonym(database, GenerateSmoObjectName("syn"));

                    syn.BaseDatabase = database.Name;
                    syn.BaseSchema = syn.Schema;
                    syn.BaseObject = "testObj";
                    syn.BaseServer = database.Parent.Name;

                    string synonymScriptDropIfExistsTemplate = "DROP SYNONYM IF EXISTS [{0}].[{1}]";
                    string synonymScriptDropIfExists = string.Format(synonymScriptDropIfExistsTemplate, syn.Schema, syn.Name);

                    VerifySmoObjectDropIfExists(syn, database, synonymScriptDropIfExists);
                });
        }

        #endregion
    }
}
