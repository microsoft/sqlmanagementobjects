// Copyright (c) Microsoft.
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
    /// Test suite for testing User-defined Function properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class UserDefinedFunction_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.UserDefinedFunction udf = (_SMO.UserDefinedFunction)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.UserDefinedFunctions.Refresh();
            Assert.IsNull(database.UserDefinedFunctions[udf.Name],
                          "Current stored procedure not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an user-defined function with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_UDF_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.UserDefinedFunction udf = new _SMO.UserDefinedFunction(database,
                        GenerateSmoObjectName("udf"));

                    udf.TextHeader = String.Format("CREATE FUNCTION {0}() RETURNS INT", udf.Name);
                    udf.TextBody = "BEGIN\nRETURN 0;\nEND";

                    string udfScriptDropIfExistsTemplate = "DROP FUNCTION IF EXISTS [{0}].[{1}]";
                    string udfScriptDropIfExists = string.Format(udfScriptDropIfExistsTemplate, udf.Schema, udf.Name);

                    VerifySmoObjectDropIfExists(udf, database, udfScriptDropIfExists);
                });
        }

        /// <summary>
        /// Tests create or alter an user-defined function through SMO on SQL16 and later.
        /// 1. Create the function, verify ScriptCreateOrAlter text and verify the object was created correctly
        /// 2. Alter the function, verify ScriptCreateOrAlter text and verify the object was updated correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoCreateOrAlter_UDF_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    // 1. Create the function, verify ScriptCreateOrAlter and check existence

                    _SMO.UserDefinedFunction udf = DatabaseObjectHelpers.CreateUdfDefinition(
                                                                            database,
                                                                            "udf",
                                                                            schema: "dbo",
                                                                            textBody: "BEGIN\nRETURN 0;\nEND");
                    udf.TextHeader = string.Format("CREATE OR ALTER FUNCTION {0}.{1}() RETURNS INT",
                                                    SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(udf.Name));


                    VerifySmoObjectCreateOrAlterForCreate(
                        database,
                        udf,
                        string.Format(@"CREATE OR ALTER FUNCTION {0}() RETURNS INT BEGIN RETURN 0; END", udf.FullQualifiedName));

                    // 2. Alter the function, verify ScriptCreateOrAlter and check existence

                    udf.TextHeader = String.Format("CREATE OR ALTER FUNCTION {0}.{1}() RETURNS VARCHAR(255)",
                                                    SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(udf.Name));
                    udf.TextBody = "BEGIN\nRETURN 'Hello World!';\nEND";

                    VerifySmoObjectCreateOrAlterForAlter(
                        database,
                        udf,
                        string.Format(@"SET ANSI_NULLS ON SET QUOTED_IDENTIFIER ON CREATE OR ALTER   FUNCTION {0}() RETURNS VARCHAR(255) BEGIN RETURN 'Hello World!'; END", udf.FullQualifiedName));
                });
        }

        /// <summary>
        /// Tests CreateOrAlter() is not supported for user defined function through SMO on SQL14 and before.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MaxMajor = 12)]
        public void SmoCreateOrAlter_UDF_Sql14AndBeforeOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.UserDefinedFunction udf = DatabaseObjectHelpers.CreateUdfDefinition(
                                                                            database,
                                                                            "udf",
                                                                            schema: "dbo",
                                                                            textBody: "BEGIN\nRETURN 0;\nEND");
                    udf.TextHeader = string.Format("CREATE OR ALTER FUNCTION {0}.{1}() RETURNS INT",
                                                    SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                                                    SmoObjectHelpers.SqlBracketQuoteString(udf.Name));

                    _SMO.FailedOperationException e = Assert.Throws<_SMO.FailedOperationException>(
                        () => udf.CreateOrAlter(),
                        string.Format(
                            "Expected FailedOperationException with message containing \"CreateOrAlter failed for UserDefinedFunction '{0}.{1}'.\" when calling CreateOrAlter against unsupported downlevel servers, but no such exception was thrown",
                            udf.Schema,
                            udf.Name));
                    Assert.That(e.Message, Does.Contain(string.Format("CreateOrAlter failed for UserDefinedFunction '{0}.{1}'.", udf.Schema, udf.Name)), "Unexpected error message.");
                });
        }

        /// <summary>
        /// Tests create or alter an user-defined function with inline option through SMO on Sqlv150 and later.
        /// 1. Create the function, verify ScriptCreateOrAlter text and verify the object was created correctly
        /// 2. Alter the function, verify ScriptCreateOrAlter text and verify the object was updated correctly
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
        public void SmoCreateOrAlter_UDFInline_Sqlv150AndAfterOnPrem()
        {

            ExecuteWithDbDrop(database =>
            {
                // 1. Create the function, verify ScriptCreateOrAlter and check existence

                _SMO.UserDefinedFunction udf = DatabaseObjectHelpers.CreateUdfDefinition(
                    database,
                    "udf",
                    schema: "dbo",
                    textBody: "BEGIN\nRETURN 0;\nEND");
                udf.TextHeader = string.Format("CREATE OR ALTER FUNCTION {0}.{1}() RETURNS INT WITH INLINE = ON",
                    SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                    SmoObjectHelpers.SqlBracketQuoteString(udf.Name));

                VerifySmoObjectCreateOrAlterForCreate(
                    database,
                    udf,
                    string.Format(@"CREATE OR ALTER FUNCTION {0}() RETURNS INT WITH INLINE = ON BEGIN RETURN 0; END",
                        udf.FullQualifiedName));

                // 2. Check if the properties are set as expected for the SMO udf object.
                Assert.IsTrue(udf.InlineType && udf.IsInlineable,
                    "Expected InlineType and IsInlineable properties to be true.");

                // 3. Alter the function, verify ScriptCreateOrAlter and check existence

                udf.TextHeader = String.Format(
                    "CREATE OR ALTER FUNCTION {0}.{1}() RETURNS VARCHAR(255) WITH INLINE = ON",
                    SmoObjectHelpers.SqlBracketQuoteString(udf.Schema),
                    SmoObjectHelpers.SqlBracketQuoteString(udf.Name));
                udf.TextBody = "BEGIN\nRETURN 'Hello World!';\nEND";

                VerifySmoObjectCreateOrAlterForAlter(
                    database,
                    udf,
                    string.Format(
                        @"SET ANSI_NULLS ON SET QUOTED_IDENTIFIER ON CREATE OR ALTER   FUNCTION {0}() RETURNS VARCHAR(255) WITH INLINE = ON BEGIN RETURN 'Hello World!'; END",
                        udf.FullQualifiedName));

                // 4. Create the function with INLINE = OFF, verify ScriptCreateOrAlter and check existence
                _SMO.UserDefinedFunction udf2 = DatabaseObjectHelpers.CreateUdfDefinition(
                    database,
                    "udf2",
                    schema: "dbo",
                    textBody: "BEGIN\nRETURN 0;\nEND");
                udf2.TextHeader = string.Format("CREATE OR ALTER FUNCTION {0}.{1}() RETURNS INT WITH INLINE = OFF",
                    SmoObjectHelpers.SqlBracketQuoteString(udf2.Schema),
                    SmoObjectHelpers.SqlBracketQuoteString(udf2.Name));

                VerifySmoObjectCreateOrAlterForCreate(
                    database,
                    udf2,
                    string.Format(@"CREATE OR ALTER FUNCTION {0}() RETURNS INT WITH INLINE = OFF BEGIN RETURN 0; END",
                        udf2.FullQualifiedName));

                // 5. Check if the properties are set as expected for the SMO udf object.
                Assert.IsFalse(udf2.InlineType, "Expected InlineType property to be FALSE");
                Assert.IsTrue(udf2.IsInlineable, "Expected IsInlineable property to be TRUE");

            });
        }
        #endregion
    }
}
