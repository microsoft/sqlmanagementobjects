// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing SecurityPredicate properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class SecurityPredicate_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.SecurityPolicy secPol = (_SMO.SecurityPolicy)objVerify;

            secPol.Refresh();
            Assert.IsTrue(1 == secPol.SecurityPredicates.Count,
                          "Current security policy not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a security predicate with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_SecPolicy_Sql16AndAfterOnPrem()
        {
            const string schemaName = "dbo";
            const string functionName = "testFunc";

            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Table table = DatabaseObjectHelpers.CreateTable(database, TestContext.TestName, schemaName);
                    _SMO.UserDefinedFunction function = new _SMO.UserDefinedFunction(database, functionName, schemaName);
                    function.TextHeader = String.Format("CREATE FUNCTION {0}.{1} (@x BIGINT) RETURNS TABLE WITH SCHEMABINDING AS",
                        schemaName, functionName);
                    function.TextBody = "return select 1 as is_visible";
                    function.Create();

                    _SMO.SecurityPolicy secPol = new _SMO.SecurityPolicy(database, 
                        GenerateSmoObjectName("secPol"), schemaName,
                        notForReplication: true,
                        isEnabled: true);
                    
                    string predicateDefinition = String.Format("[{0}].[{1}]([{2}])",
                        schemaName, functionName, table.Columns[0].Name);

                    _SMO.SecurityPredicate secPredicate = new _SMO.SecurityPredicate(secPol,
                        schemaName, table.Name, table.ID, predicateDefinition);
                    secPredicate.PredicateType = _SMO.SecurityPredicateType.Block;
                    secPredicate.PredicateOperation = _SMO.SecurityPredicateOperation.All;
                    secPol.SecurityPredicates.Add(secPredicate);

                    secPol.Create();

                    string dropPredicateDefinition = String.Format("[{0}].[{1}]([{2}])",
                        schemaName, functionName, table.Columns[0].Name);

                    _SMO.SecurityPredicate dropSecPredicate = new _SMO.SecurityPredicate(secPol,
                        schemaName, table.Name, table.ID, dropPredicateDefinition);
                    dropSecPredicate.PredicateType = _SMO.SecurityPredicateType.Filter;
                    dropSecPredicate.PredicateOperation = _SMO.SecurityPredicateOperation.All;

                    VerifySmoObjectDropIfExists(dropSecPredicate, secPol);
                });
        }

        #endregion // Scripting Tests
    }
}

