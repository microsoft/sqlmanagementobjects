// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests literal encoding when design-mode stop words are replayed.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class FullTextStopListSqlLiteralTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("word", "English", "word", "English")]
        [DataRow("O'Brien", "English", "O''Brien", "English")]
        [DataRow("word", "O'Brien", "word", "O''Brien")]
        [DataRow("\u540d'Word]", "\u540d'Language]", "\u540d''Word]", "\u540d''Language]")]
        public void FullTextStopList_ScriptCreate_ReplaysEscapedDesignModeWords(
            string word, string language, string expectedWord, string expectedLanguage)
        {
            var database = new Database(ServerTests.GetDesignModeServer(17), "TestDb");
            var stopList = new FullTextStopList(database, "Test'StopList]");
            stopList.AddStopWord(word, language);
            stopList.SetState(SqlSmoState.Existing);
            var preferences = database.GetScriptingPreferencesForCreate();
            preferences.IncludeScripts.Owner = false;
            var scripts = new StringCollection();

            stopList.ScriptCreate(scripts, preferences);

            var nl = System.Environment.NewLine;
            Assert.That(scripts, Is.EqualTo(new[]
                {
                    $"CREATE FULLTEXT STOPLIST [Test'StopList]]]{nl};{nl}" +
                    $"ALTER FULLTEXT STOPLIST [Test'StopList]]] ADD N'{expectedWord}' LANGUAGE N'{expectedLanguage}';{nl}"
                }), "Existing design-mode stop lists must replay independently escaped words and languages.");
        }

        [TestMethod]
        public void FullTextStopList_AddStopWord_PreservesEscapingOutsideDesignMode()
        {
            var connection = new ServerConnection
            {
                ServerInstance = "unit-test-server.invalid",
                ConnectTimeout = 1,
                ServerVersion = new ServerVersion(17, 0),
                TrueName = "captureOnly",
                DatabaseEngineType = DatabaseEngineType.Standalone,
                DatabaseEngineEdition = DatabaseEngineEdition.Enterprise,
                IsFabricServer = false,
                SqlExecutionModes = SqlExecutionModes.CaptureSql
            };
            var server = new Management.Smo.Server(connection);
            server.m_comparer = new Management.Smo.StringComparer(string.Empty, 1033);
            var stopList = new FullTextStopList(new Database(server, "TestDb"), "Test'StopList]");

            stopList.AddStopWord("O'Brien", "Test'Language");

            Assert.That(connection.CapturedSql.Text, Is.EqualTo(new[]
                {
                    "USE [TestDb]",
                    "ALTER FULLTEXT STOPLIST [Test'StopList]]] ADD 'O''Brien' LANGUAGE 'Test''Language';"
                }), "The already-escaped non-design-mode path must retain its existing SQL.");
        }
    }
}
