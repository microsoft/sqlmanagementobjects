// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing ServerAuditSpecification properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ServerAuditSpecification_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ServerAuditSpecification auditSpec = (_SMO.ServerAuditSpecification)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.ServerAuditSpecifications.Refresh();
            Assert.IsNull(server.ServerAuditSpecifications[auditSpec.Name],
                            "Current server audit specification not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a server audit specification with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoDropIfExists_ServerAuditSpecification_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.Audit audit = new _SMO.Audit(server,
                        GenerateUniqueSmoObjectName("audit"));
                    _SMO.ServerAuditSpecification auditSpec = new _SMO.ServerAuditSpecification(server,
                        GenerateUniqueSmoObjectName("auditSpec"));

                    try
                    {
                        audit.DestinationType = _SMO.AuditDestinationType.ApplicationLog;
                        audit.Create();

                        auditSpec.AuditName = audit.Name;

                        VerifySmoObjectDropIfExists(auditSpec, server);
                    }
                    catch (Exception e)
                    {
                        if (server.ServerAuditSpecifications[auditSpec.Name] != null)
                        {
                            auditSpec.Drop();
                        }
                        Assert.Fail("Exception :\n" + e.ToString());
                    }
                    finally
                    {
                        audit.DropIfExists();
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

