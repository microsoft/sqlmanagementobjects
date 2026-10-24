// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Schema properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class Schema_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Schema schema = (_SMO.Schema)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Schemas.Refresh();
            Assert.IsNull(database.Schemas[schema.Name],
                          "Current schema not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a schema with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Schema_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Schema schema = new _SMO.Schema(database, GenerateSmoObjectName("sch"));

                    string schemaScriptDropIfExistsTemplate = "DROP SCHEMA IF EXISTS [{0}]";
                    string schemaScriptDropIfExists = string.Format(schemaScriptDropIfExistsTemplate, schema.Name);

                    VerifySmoObjectDropIfExists(schema, database, schemaScriptDropIfExists);
                });
        }

        #endregion
    }
}
