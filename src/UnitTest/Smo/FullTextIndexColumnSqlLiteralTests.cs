// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests literal encoding in legacy full-text column scripts.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class FullTextIndexColumnSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("dbo", "TestTable", "[dbo].[TestTable]")]
        [DataRow("O'Brien", "Table'Name", "[O''Brien].[Table''Name]")]
        [DataRow("\u540d'Schema]", "Table'Name]", "[\u540d''Schema]]].[Table''Name]]]")]
        public void FullTextIndexColumn_ScriptDrop_EscapesLegacyTableLiteral(
            string schema, string tableName, string expectedTable)
        {
            var database = new Database(ServerTests.GetDesignModeServer(17), "TestDb");
            var table = new Table(database, tableName, schema);
            var index = new FullTextIndex(table);
            var column = new FullTextIndexColumn(index, "Document'Name]");
            var preferences = new ScriptingPreferences(column) { TargetServerVersion = SqlServerVersion.Version80 };
            var scripts = new StringCollection();

            column.ScriptDrop(scripts, preferences);

            Assert.That(scripts, Is.EqualTo(new[]
                {
                    $"EXEC dbo.sp_fulltext_column @tabname=N'{expectedTable}', @colname=N'Document''Name]', @action=N'drop'"
                }), "Legacy DROP must escape the table literal without double-escaping the column literal.");
        }

        [TestMethod]
        public void FullTextIndexColumn_ScriptDrop_PreservesModernIdentifierSyntax()
        {
            var database = new Database(ServerTests.GetDesignModeServer(17), "TestDb");
            var table = new Table(database, "Table'Name]", "Schema'Name]");
            var column = new FullTextIndexColumn(new FullTextIndex(table), "Document'Name]");
            var scripts = new StringCollection();

            column.ScriptDrop(scripts, new ScriptingPreferences(column));

            Assert.That(scripts, Is.EqualTo(new[]
                {
                    "ALTER FULLTEXT INDEX ON [Schema'Name]]].[Table'Name]]] DROP ([Document'Name]]]) "
                }), "Modern targets must continue to use identifiers rather than SQL string literals.");
        }
    }
}
