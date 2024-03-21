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
    /// Test suite for testing Audit properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class Audit_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.Audit audit = (_SMO.Audit)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Audits.Refresh();
            Assert.IsNull(server.Audits[audit.Name],
                "Current audit not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an audit with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Audit_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    string auditName = GenerateUniqueSmoObjectName("audit");
                    _SMO.Server server = database.Parent;
                    _SMO.Audit audit = new _SMO.Audit(server, auditName);

                    if (server.DatabaseEngineEdition == DatabaseEngineEdition.SqlManagedInstance)
                    {
                        audit.DestinationType = _SMO.AuditDestinationType.Url;
                        audit.FilePath = "https://mydata.blob.core.windows.net/mycontainer";
                        audit.RetentionDays = 14;
                    }
                    else
                    {
                        audit.DestinationType = _SMO.AuditDestinationType.ApplicationLog;
                    }

                    try
                    {
                        VerifySmoObjectDropIfExists(audit, server);
                    }
                    catch (Exception)
                    {
                        if (server.Audits[audit.Name] != null)
                        {
                            audit.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion // Scripting Tests
    }
}

