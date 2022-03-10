// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;
using _SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Test the functionality of the DdlParser class in SqlEnum
    /// </summary>
    [TestClass]
    public class DdlParserTests : UnitTestBase
    {
        /// <summary>
        /// Tests query starting with CREATE OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) before sqlVersion130, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Valid_BeforeSqlVersion130()
        {
            const string objectText = @"CREATE PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult == true, "Parsing DDL Header failed.");
        }

        /// <summary>
        /// Tests query starting with ALTER OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) before sqlVersion130, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Alter_Valid_BeforeSqlVersion130()
        {
            const string objectText = @"ALTER PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult == true, "Parsing DDL Header failed.");
        }

        /// <summary>
        /// Tests query starting with unrecognized symbol before sqlVersion130, returns invalid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Unrecognized_Symbol_Invalid_BeforeSqlVersion130()
        {
            const string objectText = @"INVALID dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult == false, "Parsing DDL Header succeed while it should fail.");
        }

        /// <summary>
        /// Tests query starting with CREATE OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) on sqlVersion130 or later, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Valid_SqlVersion130orLater()
        {
            const string objectText = @"CREATE PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult == true, "Parsing DDL Header failed.");
        }

        /// <summary>
        /// Tests query starting with CREATE MATERIALIZED VIEW on sqlVersion130 or later, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Materialized_View_Valid_SqlVersion130orLater()
        {
            const string objectText = @"CREATE MATERIALIZED VIEW dbo.sp AS SELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult, NUnit.Framework.Is.True, "CheckDdlHeader should succeed for MATERIALIZED VIEW");
            Assert.That(headerInfo.objectType, NUnit.Framework.Is.EqualTo("VIEW"), "ObjectType for MATERIALIZED VIEW");
        }

        /// <summary>
        /// Tests query starting with ALTER OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) on sqlVersion130 or later, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Alter_Valid_SqlVersion130orLater()
        {
            const string objectText = @"ALTER PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult == true, "Parsing DDL Header failed.");
        }

        /// <summary>
        /// Tests query starting with CREATE OR ALTER OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) on sqlVersion130 or later, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Or_Alter_Valid_SqlVersion130orLater()
        {
            const string objectText = @"CREATE OR ALTER PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, isOrAlterSupported: true, headerInfo: out headerInfo);

            Assert.That(actualResult == true, "Parsing DDL Header failed.");
        }

        /// <summary>
        /// Tests query starting with CREATE OR OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) on sqlVersion130 or later, returns invalid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Or_Invalid_SqlVersion130orLater()
        {
            const string objectText = @"CREATE OR PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, isOrAlterSupported: true, headerInfo: out headerInfo);

            Assert.That(actualResult == false, "Parsing DDL Header succeed while it should fail.");
        }

        /// <summary>
        /// Tests query starting with CREATE ALTER OBJ(i.e. PROC\FUNCTION\TRIGGER\VIEW) on sqlVersion130 or later, returns invalid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Alter_Invalid_SqlVersion130orLater()
        {
            const string objectText = @"CREATE ALTER PROCEDURE dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, isOrAlterSupported: true, headerInfo: out headerInfo);

            Assert.That(actualResult == false, "Parsing DDL Header succeed while it should fail.");
        }

        /// <summary>
        /// Tests query starting with unrecognized symbol on sqlVersion130 or later, returns invalid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Unrecognized_Symbol_Invalid_SqlVersion130orLater()
        {
            const string objectText = @"INVALID dbo.sp AS BEGIN\nSELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, isOrAlterSupported: true, headerInfo: out headerInfo);

            Assert.That(actualResult == false, "Parsing DDL Header succeed while it should fail.");
        }

        /// <summary>
        /// Tests query starting with CREATE MATERIALIZED FFUNCTION on sqlVersion130 or later, returns valid.
        /// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void CheckDdlHeader_Starting_With_Create_Materialized_Function_Invalid_SqlVersion130orLater()
        {
            const string objectText = @"CREATE MATERIALIZED FUNCTION dbo.sp AS SELECT * FROM sys.procedures WHERE type_desc LIKE 'SQL_STORED_PROCEDURE'\nEND";
            _SMO.DdlTextParserHeaderInfo headerInfo;
            bool actualResult = _SMO.DdlTextParser.CheckDdlHeader(objectText, useQuotedIdentifier: true, headerInfo: out headerInfo);

            Assert.That(actualResult, NUnit.Framework.Is.False, string.Format("Parsing DDL Header succeed while it should fail with syntax : {0}.", objectText));
        }
    }
}
