// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing DatabaseAuditSpecification properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseAuditSpecification_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.DatabaseAuditSpecification auditSpec = (_SMO.DatabaseAuditSpecification)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.DatabaseAuditSpecifications.Refresh();
            Assert.IsNull(database.DatabaseAuditSpecifications[auditSpec.Name],
                            "Current database audit specification not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a database audit specification with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_DatabaseAuditSpecification_Sql16AndAfterOnPrem()
        {

            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.Audit audit = new _SMO.Audit(server, GenerateUniqueSmoObjectName("audit"));
                    _SMO.DatabaseAuditSpecification auditSpec =
                        new _SMO.DatabaseAuditSpecification(database, GenerateUniqueSmoObjectName("auditSpec"));

                    try
                    {
                        audit.DestinationType = _SMO.AuditDestinationType.ApplicationLog;
                        audit.Create();

                        auditSpec.AuditName = audit.Name;

                        VerifySmoObjectDropIfExists(auditSpec, database);
                    }
                    catch (Exception)
                    {
                        audit.DropIfExists();
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

