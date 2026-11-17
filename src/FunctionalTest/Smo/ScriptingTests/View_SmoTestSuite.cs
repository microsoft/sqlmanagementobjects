// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;



namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing View properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class View_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.View view = (_SMO.View)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Views.Refresh();
            Assert.IsNull(database.Views[view.Name],
                          "Current view not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a view with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_View_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.View view = new _SMO.View(database, GenerateSmoObjectName("view"));

                    view.TextHeader = String.Format("CREATE VIEW [{0}] AS", view.Name);
                    view.TextBody = "SELECT * FROM sys.tables";

                    string viewScriptDropIfExistsTemplate = "DROP VIEW IF EXISTS [{0}].[{1}]";
                    string viewScriptDropIfExists = string.Format(viewScriptDropIfExistsTemplate, view.Schema, view.Name);

                    VerifySmoObjectDropIfExists(view, database, viewScriptDropIfExists);
                });
        }

        /// <summary>
        /// Tests create or alter a view through SMO on SQL16 and later.
        /// 1. Create the view, verify ScriptCreateOrAlter text and verify the object was created correctly
        /// 2. Alter the view, verify ScriptCreateOrAlter text and verify the object was updated correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoCreateOrAlter_View_Sql16AndAfterOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    // 1. Create the view, verify ScriptCreateOrAlter and check existence

                    _SMO.View view = DatabaseObjectHelpers.CreateViewDefinition(database,
                                                                      "view",
                                                                      schema: "dbo",
                                                                      textBody: "SELECT * FROM sys.tables");
                    view.TextHeader = string.Format("CREATE OR ALTER VIEW {0}.{1} AS",
                                                    SmoObjectHelpers.SqlBracketQuoteString(view.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(view.Name));

                    VerifySmoObjectCreateOrAlterForCreate(
                        database,
                        view,
                        string.Format(@"CREATE OR ALTER VIEW {0} AS SELECT * FROM sys.tables", view.FullQualifiedName));

                    // 2. Alter the view, verify ScriptCreateOrAlter and check existence

                    view.TextHeader = string.Format("CREATE OR ALTER VIEW {0}.{1} AS",
                                                    SmoObjectHelpers.SqlBracketQuoteString(view.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(view.Name));
                    view.TextBody = "SELECT * FROM sys.all_columns";

                    VerifySmoObjectCreateOrAlterForAlter(
                        database,
                        view,
                        string.Format(@"CREATE OR ALTER   VIEW {0} AS SELECT * FROM sys.all_columns", view.FullQualifiedName));
                });
        }

        /// <summary>
        /// Tests CreateOrAlter() is not supported for view through SMO on SQL14 and before.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MaxMajor = 12)]
        public void SmoCreateOrAlter_View_Sql14AndBeforeOnPrem()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    _SMO.View view = DatabaseObjectHelpers.CreateViewDefinition(database,
                                                                      "view",
                                                                      schema: "dbo",
                                                                      textBody: "SELECT * FROM sys.tables");
                    view.TextHeader = string.Format("CREATE OR ALTER VIEW {0}.{1} AS",
                                                    SmoObjectHelpers.SqlBracketQuoteString(view.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(view.Name));

                    _SMO.FailedOperationException e = Assert.Throws<_SMO.FailedOperationException>(
                        () => view.CreateOrAlter(),
                        string.Format(
                            "Expected FailedOperationException with message containing \"CreateOrAlter failed for View '{0}.{1}'.\" when calling CreateOrAlter against unsupported downlevel servers, but no such exception was thrown",
                            view.Schema,
                            view.Name));
                    Assert.That(e.Message, Does.Contain(string.Format("CreateOrAlter failed for View '{0}.{1}'.", view.Schema, view.Name)), "Unexpected error message");
                });
        }

        #endregion
    }
}
