// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests full-text index SQL generation.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class FullTextIndexTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("English", "English")]
        [DataRow("O'Brien", "O''Brien")]
        [DataRow("Two'Quoted'Parts", "Two''Quoted''Parts")]
        [DataRow("Language]Name", "Language]Name")]
        public void FullTextIndex_ScriptCreate_EscapesLanguageAsStringNotIdentifier(
            string language, string expectedLanguage)
        {
            var server = ServerTests.GetDesignModeServer(12);
            var database = new Database(server, "TestDb");
            var table = new Table(database, "TestTable", "dbo");
            var index = new FullTextIndex(table)
            {
                UniqueIndexName = "TestKey",
                CatalogName = "TestCatalog",
                ChangeTracking = ChangeTracking.Automatic,
                StopListOption = StopListOption.Off
            };
            index.IndexedColumns.Add(new FullTextIndexColumn(index, "Document") { Language = language });
            var scripts = new StringCollection();
            var preferences = database.GetScriptingPreferencesForCreate();

            index.ScriptCreate(scripts, preferences);

            var nl = Environment.NewLine;
            var expectedScript = $"CREATE FULLTEXT INDEX ON [dbo].[TestTable]({nl}" +
                $"[Document] LANGUAGE N'{expectedLanguage}'){nl}" +
                $"KEY INDEX [TestKey]ON ([TestCatalog]){nl}" +
                $"WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF){nl}{nl}";
            Assert.That(scripts, Is.EqualTo(new[] { expectedScript }),
                "The complete full-text index script must escape language as a SQL string, not as an identifier.");
        }
    }
}
