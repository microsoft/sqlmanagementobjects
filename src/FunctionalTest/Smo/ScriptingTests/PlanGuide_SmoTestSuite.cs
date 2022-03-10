﻿// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using _SMO = Microsoft.SqlServer.Management.Smo;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing PlanGuide properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class PlanGuide_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.PlanGuide pg = (_SMO.PlanGuide)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.PlanGuides.Refresh();
            Assert.IsNull(database.PlanGuides[pg.Name],
                            "Current plan guide not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a plan guide with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_PlanGuide_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.PlanGuide pg = new _SMO.PlanGuide(database,
                        GenerateSmoObjectName("pg"));

                    pg.Statement = "SELECT * FROM sys.tables";
                    pg.ScopeType = _SMO.PlanGuideType.Sql;
                    pg.ScopeBatch = pg.Statement;
                    pg.Hints = "OPTION(RECOMPILE)";

                    VerifyIsSmoObjectDropped(pg, database);
                });
        }

        #endregion
    }
}
