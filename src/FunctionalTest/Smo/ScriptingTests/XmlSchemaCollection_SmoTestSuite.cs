// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing XmlSchemaCollection properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class XmlSchemaCollection_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.XmlSchemaCollection xsc = (_SMO.XmlSchemaCollection)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.XmlSchemaCollections.Refresh();
            Assert.IsNull(database.XmlSchemaCollections[xsc.Name],
                            "Current xml schema collection not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a xml schema collection with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_XmlSchemaCollection_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.XmlSchemaCollection xsc = new _SMO.XmlSchemaCollection(database,
                        GenerateSmoObjectName("xmlsc"));

                    xsc.Text = "<schema xmlns=\"http://www.w3.org/2001/XMLSchema\"><element name=\"e\"/></schema>";

                    VerifySmoObjectDropIfExists(xsc, database);
                });
        }

        #endregion // Scripting Tests
    }
}

