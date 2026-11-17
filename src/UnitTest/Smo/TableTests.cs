// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the Table object.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class TableTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("dbo", "TestTable", "[dbo].[TestTable]")]
        [DataRow("dbo", "O'Brien", "[dbo].[O''Brien]")]
        [DataRow("Owner's", "TestTable", "[Owner''s].[TestTable]")]
        [DataRow("Owner's]", "Table'Name]", "[Owner''s]]].[Table''Name]]]")]
        [DataRow("\u540d'\u524d", "\u8868'\u540d", "[\u540d''\u524d].[\u8868''\u540d]")]
        public void Table_ScriptVardecimalCompression_EscapesQualifiedNameAsSqlLiteral(
            string schemaName, string tableName, string expectedQualifiedName)
        {
            foreach (bool scriptForAlter in new[] { false, true })
            {
                foreach (bool enabled in new[] { false, true })
                {
                    var server = ServerTests.GetDesignModeServer(10);
                    var database = new Database(server, "TestDatabase");
                    var table = new Table(database, tableName, schemaName);
                    table.Columns.Add(new Column(table, "Id", DataType.Int));
                    table.SetState(SqlSmoState.Existing);
                    table.IsVarDecimalStorageFormatEnabled = enabled;
                    var scripts = new StringCollection();
                    var preferences = database.GetScriptingPreferencesForCreate();
                    preferences.ScriptForAlter = scriptForAlter;
                    preferences.OldOptions.NoVardecimal = false;

                    if (scriptForAlter)
                    {
                        table.ScriptAlter(scripts, preferences);
                    }
                    else
                    {
                        table.ScriptCreate(scripts, preferences);
                    }

                    var optionScripts = scripts.Cast<string>()
                        .Where(script => script.StartsWith("EXEC sys.sp_tableoption ")).ToArray();
                    var expectedScript = $"EXEC sys.sp_tableoption N'{expectedQualifiedName}', " +
                        $"N'vardecimal storage format', N'{(enabled ? "ON" : "OFF")}'";
                    Assert.That(optionScripts, Is.EqualTo(new[] { expectedScript }),
                        $"The complete vardecimal statement must escape schema and table names for alter={scriptForAlter}, enabled={enabled}.");
                }
            }
        }

        [TestMethod]
        public void Table_ScriptCreate_escapes_closing_brackets_in_filetable_constraint_names()
        {
            var server = ServerTests.GetDesignModeServer(13);
            var database = new Database(server, "TestDatabase");
            var table = new Table(database, "EscapedFileTable");
            ((IAlienObject)table).SetPropertyValue(nameof(Table.IsFileTable), typeof(bool), true);
            table.Columns.Add(new Column(table, "path_locator", DataType.HierarchyId));
            table.Columns.Add(new Column(table, "stream_id", DataType.UniqueIdentifier));
            table.Columns.Add(new Column(table, "parent_path_locator", DataType.HierarchyId));
            table.SetState(SqlSmoState.Existing);

            var primaryKey = new Index(table, "FileTablePk]Constraint");
            primaryKey.IndexedColumns.Add(new IndexedColumn(primaryKey, "path_locator"));
            table.Indexes.Add(primaryKey);

            var streamIdIndex = new Index(table, "FileTableStreamId]Constraint");
            streamIdIndex.IndexedColumns.Add(new IndexedColumn(streamIdIndex, "stream_id"));
            table.Indexes.Add(streamIdIndex);

            var fullPathIndex = new Index(table, "FileTableFullPath]Constraint");
            fullPathIndex.IndexedColumns.Add(new IndexedColumn(fullPathIndex, "parent_path_locator"));
            table.Indexes.Add(fullPathIndex);

            var script = new StringCollection();
            var scriptingPreferences = database.GetScriptingPreferencesForCreate();
            scriptingPreferences.Table.SystemNamesForConstraints = true;
            table.ScriptCreate(script, scriptingPreferences);
            var scriptText = string.Join("\n", script.Cast<string>());

            Assert.Multiple(() =>
            {
                Assert.That(
                    scriptText,
                    Does.Contain("FILETABLE_PRIMARY_KEY_CONSTRAINT_NAME=[FileTablePk]]Constraint]"),
                    "FileTable primary key constraint name should escape closing brackets.");
                Assert.That(
                    scriptText,
                    Does.Contain("FILETABLE_STREAMID_UNIQUE_CONSTRAINT_NAME=[FileTableStreamId]]Constraint]"),
                    "FileTable stream ID constraint name should escape closing brackets.");
                Assert.That(
                    scriptText,
                    Does.Contain("FILETABLE_FULLPATH_UNIQUE_CONSTRAINT_NAME=[FileTableFullPath]]Constraint]"),
                    "FileTable full path constraint name should escape closing brackets.");
            });
        }
    }
}
