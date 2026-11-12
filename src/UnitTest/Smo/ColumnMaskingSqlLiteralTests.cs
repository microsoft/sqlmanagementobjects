// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using System.Linq;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ColumnMaskingSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("default()", "default()")]
        [DataRow("email()", "email()")]
        [DataRow("partial(1,\"O'Brien\",0)", "partial(1,\"O''Brien\",0)")]
        [DataRow("partial(1,\"O''Brien\",0)", "partial(1,\"O''''Brien\",0)")]
        [DataRow("partial(1,\"名'前\",0)", "partial(1,\"名''前\",0)")]
        public void ColumnMasking_ScriptCreate_EscapesFunctionWithoutChangingProperty(string function, string escapedFunction)
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var table = new Table(database, "MaskedTable", "dbo");
            var column = new Column(table, "MaskedColumn", DataType.NVarChar(100))
            {
                IsMasked = true,
                MaskingFunction = function
            };
            table.Columns.Add(column);
            var scripts = new StringCollection();

            table.ScriptCreate(scripts, database.GetScriptingPreferencesForCreate());

            Assert.That(string.Join("\n", scripts.Cast<string>()),
                Does.Contain($"MASKED WITH (FUNCTION = '{escapedFunction}')"),
                "The complete masking-function literal must encode its text rather than validate function syntax.");
            Assert.That(column.MaskingFunction, Is.EqualTo(function), "CREATE scripting must preserve masking-function text.");
        }

        [DataTestMethod]
        [DataRow("default()", "default()", false)]
        [DataRow("partial(1,\"O'Brien\",0)", "partial(1,\"O''Brien\",0)", false)]
        [DataRow("partial(1,\"O''Brien\",0)", "partial(1,\"O''''Brien\",0)", false)]
        [DataRow("partial(1,\"名'前\",0)", "partial(1,\"名''前\",0)", false)]
        [DataRow("partial(1,\"O'Brien\",0)", "partial(1,\"O''Brien\",0)", true)]
        public void ColumnMasking_ScriptAlter_EscapesFunctionWithAndWithoutTypeChange(
            string function, string escapedFunction, bool changeType)
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var table = new Table(database, "MaskedTable", "dbo");
            var column = new Column(table, "MaskedColumn", DataType.NVarChar(100)) { Nullable = true };
            table.Columns.Add(column);
            table.SetState(SqlSmoState.Existing);
            column.SetState(SqlSmoState.Existing);
            column.Properties.SetAllDirtyAsRetrieved(true);
            column.Properties.SetAllDirty(false);
            column.IsMasked = true;
            column.MaskingFunction = function;
            if (changeType)
            {
                column.DataType = DataType.NVarChar(200);
            }
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.ScriptForAlter = true;
            var scripts = new StringCollection();

            column.ScriptAlter(scripts, preferences);

            Assert.That(scripts.Cast<string>().Where(script => script.Contains(" ADD MASKED ")),
                Is.EqualTo(new[] { $"ALTER TABLE [dbo].[MaskedTable] ALTER COLUMN [MaskedColumn] ADD MASKED WITH (FUNCTION = '{escapedFunction}')" }),
                $"ALTER must escape masking-function text when a main type ALTER is emitted: {changeType}.");
            Assert.That(column.MaskingFunction, Is.EqualTo(function), "ALTER scripting must preserve masking-function text.");
        }

        [DataTestMethod]
        [DataRow("default()")]
        [DataRow("partial(1, \"O''Brien\", 0)")]
        [DataRow("partial(1, \"Two''''Quotes\", 0)")]
        [DataRow("partial(1, \"\u540d''\u524d\", 0)")]
        public void ColumnMasking_ScriptCreate_PreservesCatalogEscaping(string catalogFunction)
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var table = new Table(database, "MaskedTable", "dbo");
            var column = new Column(table, "MaskedColumn", DataType.NVarChar(100)) { IsMasked = true };
            table.Columns.Add(column);
            table.SetState(SqlSmoState.Existing);
            column.SetState(SqlSmoState.Existing);
            var property = column.Properties.Get(nameof(Column.MaskingFunction));
            property.SetValue(catalogFunction);
            property.SetRetrieved(true);
            property.SetDirty(false);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                var scripts = new StringCollection();
                table.ScriptCreate(scripts, database.GetScriptingPreferencesForCreate());

                Assert.That(string.Join("\n", scripts.Cast<string>()),
                    Does.Contain($"MASKED WITH (FUNCTION = '{catalogFunction}')"),
                    "Repeated CREATE scripting must preserve the already-escaped catalog definition.");
                Assert.That(column.MaskingFunction, Is.EqualTo(catalogFunction),
                    "Scripting must not change the public catalog value.");
                Assert.That(property.Dirty, Is.False, "Scripting must not mark the catalog value dirty.");
            }
        }

        [DataTestMethod]
        [DataRow("partial(1, \"O''Brien\", 0)", false)]
        [DataRow("partial(1, \"O''Brien\", 0)", true)]
        [DataRow("partial(1, \"Two''''Quotes\", 0)", true)]
        [DataRow("partial(1, \"\u540d''\u524d\", 0)", false)]
        public void ColumnMasking_ScriptAlter_PreservesUnchangedCatalogFunction(
            string catalogFunction, bool changeType)
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var table = new Table(database, "MaskedTable", "dbo");
            var column = new Column(table, "MaskedColumn", DataType.NVarChar(100))
            {
                Nullable = true,
                IsMasked = true
            };
            table.Columns.Add(column);
            table.SetState(SqlSmoState.Existing);
            column.SetState(SqlSmoState.Existing);
            column.Properties.Get(nameof(Column.MaskingFunction)).SetValue(catalogFunction);
            column.Properties.SetAllRetrieved(true);
            column.Properties.SetAllDirty(false);
            if (changeType)
            {
                column.DataType = DataType.NVarChar(200);
            }
            else
            {
                column.Nullable = false;
            }
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.ScriptForAlter = true;
            var scripts = new StringCollection();

            column.ScriptAlter(scripts, preferences);

            Assert.That(scripts.Cast<string>().Where(script => script.Contains(" ADD MASKED ")),
                Is.EqualTo(new[]
                {
                    $"ALTER TABLE [dbo].[MaskedTable] ALTER COLUMN [MaskedColumn] ADD MASKED WITH (FUNCTION = '{catalogFunction}')"
                }),
                "Changing the type or nullability must reapply the catalog mask without escaping it again.");
            Assert.That(column.MaskingFunction, Is.EqualTo(catalogFunction),
                "ALTER scripting must preserve the public catalog definition.");
        }

        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void ColumnMasking_DesignModeCreateAndAlter_KeepCatalogRepresentation(bool alter)
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var table = new Table(database, "MaskedTable", "dbo");
            var column = new Column(table, "MaskedColumn", DataType.NVarChar(100))
            {
                Nullable = true,
                IsMasked = true,
                MaskingFunction = "partial(1, \"O'Brien\", 0)"
            };
            table.Columns.Add(column);
            table.SetState(SqlSmoState.Existing);
            column.Create();
            if (alter)
            {
                column.MaskingFunction = "partial(1, \"O'Brien\", 0)";
                column.Alter();
            }
            Assert.That(column.MaskingFunction, Is.EqualTo("partial(1, \"O''Brien\", 0)"),
                "Successful offline operations must cache catalog-shaped values after clearing dirty flags.");

            var scripts = new StringCollection();
            table.ScriptCreate(scripts, database.GetScriptingPreferencesForCreate());
            Assert.That(string.Join("\n", scripts.Cast<string>()),
                Does.Contain("MASKED WITH (FUNCTION = 'partial(1, \"O''Brien\", 0)')"),
                "Scripting after an offline operation must not lose or duplicate padding apostrophes.");
        }

        [TestMethod]
        public void ColumnMasking_ScriptCreate_EncodesUnpairedQuoteInCleanProperty()
        {
            var database = new Database(ServerTests.GetDesignModeServer(13), "TestDatabase");
            var table = new Table(database, "MaskedTable", "dbo");
            var column = new Column(table, "MaskedColumn", DataType.NVarChar(100)) { IsMasked = true };
            table.Columns.Add(column);
            column.Properties.Get(nameof(Column.MaskingFunction)).SetValue("partial(1,\"O'Brien\",0)");
            column.Properties.Get(nameof(Column.MaskingFunction)).SetRetrieved(true);
            var scripts = new StringCollection();

            table.ScriptCreate(scripts, database.GetScriptingPreferencesForCreate());

            Assert.That(string.Join("\n", scripts.Cast<string>()),
                Does.Contain("MASKED WITH (FUNCTION = 'partial(1,\"O''Brien\",0)')"),
                "Even clean property values must pass through the SQL literal encoder.");
        }
    }
}
