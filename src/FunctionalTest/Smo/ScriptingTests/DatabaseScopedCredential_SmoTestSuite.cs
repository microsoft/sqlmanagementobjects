// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing DatabaseScopedCredential properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseScopedCredential_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.DatabaseScopedCredential crd = (_SMO.DatabaseScopedCredential)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.DatabaseScopedCredentials.Refresh();
            Assert.IsNull(database.DatabaseScopedCredentials[crd.Name],
                          "Current database scoped credential not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a database scoped credential with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_DatabaseScopedCredential_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.DatabaseScopedCredential crd = new _SMO.DatabaseScopedCredential(database,
                        GenerateSmoObjectName("crd"));

                    database.Parent.ConnectionContext.ExecuteNonQuery("dbcc traceon(4631, -1)");

                    crd.Identity = "testID";

                    VerifySmoObjectDropIfExists(crd, database);
                });
        }

        #endregion // Scripting Tests
    }
}

