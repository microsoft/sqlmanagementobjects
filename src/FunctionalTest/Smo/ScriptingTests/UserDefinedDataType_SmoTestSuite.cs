// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing UserDefinedDataType properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class UserDefinedDataType_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.UserDefinedDataType uddt = (_SMO.UserDefinedDataType)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.UserDefinedDataTypes.Refresh();
            Assert.IsNull(database.UserDefinedDataTypes[uddt.Name],
                          "Current user-defined data type not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an user-defined data type with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_UDDT_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.UserDefinedDataType uddt = new _SMO.UserDefinedDataType(database,
                        GenerateSmoObjectName("uddt"));

                    uddt.SystemType = "int";

                    string uddtScriptDropIfExistsTemplate = "DROP TYPE IF EXISTS [{0}].[{1}]";
                    string uddtScriptDropIfExists = string.Format(uddtScriptDropIfExistsTemplate,
                        uddt.Schema, uddt.Name);

                    VerifySmoObjectDropIfExists(uddt, database, uddtScriptDropIfExists);
                });
        }

        #endregion
    }
}
