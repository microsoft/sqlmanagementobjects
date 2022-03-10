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
    /// Test suite for testing Server Trigger properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class ServerDdlTrigger_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ServerDdlTrigger triggerSrv = (_SMO.ServerDdlTrigger)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Triggers.Refresh();
            Assert.IsNull(server.Triggers[triggerSrv.Name],
                          "Current server trigger not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a server trigger with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_DatabaseDdlTrigger_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.ServerDdlTrigger triggerSrv = new _SMO.ServerDdlTrigger(server,
                        GenerateUniqueSmoObjectName("trg"));

                    triggerSrv.TextHeader = string.Format("CREATE TRIGGER [{0}] ON ALL SERVER FOR DROP_TABLE AS", triggerSrv.Name);
                    triggerSrv.TextBody = "PRINT 'Table is deleted!'";
                    triggerSrv.ImplementationType = _SMO.ImplementationType.TransactSql;

                    string triggerSrvScriptDropIfExistsTemplate = "DROP TRIGGER IF EXISTS [{0}]";
                    string triggerSrvScriptDropIfExists = string.Format(triggerSrvScriptDropIfExistsTemplate, triggerSrv.Name);

                    try
                    {
                        VerifySmoObjectDropIfExists(triggerSrv, server, triggerSrvScriptDropIfExists);
                    }
                    catch (Exception)
                    {
                        if (server.Triggers[triggerSrv.Name] != null)
                        {
                            triggerSrv.Drop();
                        }
                        throw;
                    }
                });
        }

        #endregion
    }
}
