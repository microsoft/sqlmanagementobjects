// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.SqlParser.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Environment = System.Environment;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class SqlCommentTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("")]
        [DataRow("\r")]
        [DataRow("\n")]
        [DataRow("\r\n")]
        [DataRow("\u0085")]
        [DataRow("\u2028")]
        [DataRow("\u2029")]
        [DataRow("\t")]
        public void Transfer_Diagnostics_KeepObjectNamesOnOneLine(string separator)
        {
            var server = ServerTests.GetDesignModeServer(17);
            var database = new Database(server, "TestDb");
            database.SetState(SqlSmoState.Existing);
            var transfer = new Transfer(database) { CopyAllObjects = false };
            transfer.Options.ContinueScriptingOnError = true;
            var name = "name]" + separator + "SELECT 1 --";
            transfer.IncompatibleObjects.Add(
                new Urn(database.Urn + "/Sequence[@Name='" + Urn.EscapeString(name) + "']"));

            var scripts = transfer.EnumScriptTransfer().ToArray();
            Assert.That(scripts, Has.Length.EqualTo(1), "Only the incompatible-object diagnostic should be emitted.");
            Assert.That(scripts[0], Does.StartWith("-- "), "The diagnostic must remain a line comment.");
            Assert.That(scripts[0].Any(c => char.IsControl(c) || c == '\u2028' || c == '\u2029'), Is.False,
                "The complete diagnostic must occupy one physical line without control characters.");
            if (separator.Length == 0)
            {
                Assert.That(scripts[0], Does.EndWith("Name:" + name),
                    "Ordinary names, including brackets, must retain their diagnostic representation.");
            }
            AssertCommentsOnly(scripts[0]);
        }

        [DataTestMethod]
        [DataRow("ordinary]name")]
        [DataRow("x*/ SELECT 1 --")]
        [DataRow("x/*")]
        [DataRow("x/* nested */")]
        [DataRow("x\r\nSELECT 1 --")]
        public void ScriptMaker_UnresolvedEntity_RemainsAComment(string name)
        {
            var server = ServerTests.GetDesignModeServer(17);
            var urn = new Urn(server.Urn + "/Database[@Name='TestDb']/UnresolvedEntity[@Name='" +
                Urn.EscapeString(name) + "']/UnresolvedEntity/Special");
            // Inject an unresolved dependency without requiring a live SQL connection.
            var discoverer = new Mock<ISmoDependencyDiscoverer>();
            discoverer.Setup(d => d.Discover(It.IsAny<IEnumerable<Urn>>())).Returns(new[] { urn });
            var options = new ScriptingOptions { WithDependencies = true, IncludeHeaders = false };
            var maker = new ScriptMaker(server, options) { Prefetch = false, Discoverer = discoverer.Object };

            var scripts = maker.Script(Array.Empty<Urn>()).Cast<string>().ToArray();

            Assert.That(scripts, Has.Length.EqualTo(1), "The discovered unresolved dependency must produce one diagnostic.");
            AssertCommentsOnly(scripts[0]);
            Assert.That(scripts[0], Does.StartWith("/****** Cannot script Unresolved Entities : "),
                "Retain the unresolved-dependency notice.");
            if (name.Contains("/*") || name.Contains("*/"))
            {
                Assert.That(scripts[0], Does.EndWith("? ******/"),
                    "Unsafe URN text must use the same replacement as object headers.");
            }
            else
            {
                Assert.That(scripts[0], Does.Contain(urn.Parent.Parent.ToString()),
                    "Safe URNs must be preserved in full.");
            }

            // Verify the diagnostic stays safe when routed to a separate file.
            var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                using (var writer = new FilePerObjectWriter(directory))
                {
                    writer.ScriptBatchTerminator = false;
                    maker.Script(Array.Empty<Urn>(), writer);
                }
                var file = Directory.GetFiles(directory).Single();
                Assert.That(Path.GetFileNameWithoutExtension(file), Is.EqualTo("UnresolvedEntity"),
                    "The diagnostic must also use the dedicated per-object output route.");
                AssertCommentsOnly(File.ReadAllText(file));
            }
            finally
            {
                foreach (var file in Directory.GetFiles(directory))
                {
                    File.Delete(file);
                }
                Directory.Delete(directory);
            }
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("Configuration instructions")]
        [DataRow("heading */ SELECT 1 --")]
        [DataRow("heading /*")]
        public void CommentCollection_CommentsEveryPhysicalLineAndProtectsHeading(string heading)
        {
            var scripts = new StringCollection
            {
                "SELECT 1;\r\nSELECT 2;\rSELECT 3;\nSELECT 4;",
                "SELECT 5;\u0085SELECT 6;\u2028SELECT 7;\u2029SELECT 8;"
            };

            SmoUtility.EncodeStringCollectionAsComment(scripts, heading);

            AssertCommentsOnly(string.Join(Environment.NewLine, scripts.Cast<string>()));
            var body = scripts.Cast<string>().Skip(string.IsNullOrEmpty(heading) ? 0 : 1);
            Assert.That(body.SelectMany(s => s.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)),
                Has.All.StartsWith("-- "), "Every physical script line must be commented, including the first entry.");
        }

        [DataTestMethod]
        [DataRow("ordinary]name", "ordinary]name")]
        [DataRow("x*/ SELECT 1 --", "?")]
        [DataRow("x/*", "?")]
        public void IncludeHeader_PreservesExistingDelimiterProtection(string name, string expectedName)
        {
            var header = ExceptionTemplates.IncludeHeader("Table", name, "date");

            Assert.That(header, Does.Contain("Table " + expectedName + " "),
                "Refactoring must preserve the existing header name replacement.");
            AssertCommentsOnly(header);
        }

        [TestMethod]
        public void LineComment_ReplacesAllControlCharactersAndUnicodeLineSeparators()
        {
            var text = "before" + new string(Enumerable.Range(char.MinValue, char.MaxValue + 1)
                .Select(i => (char)i).Where(char.IsControl).ToArray()) + "\u2028\u2029after";

            var comment = SmoUtility.FormatSqlLineComment(text);

            Assert.That(comment, Does.StartWith("-- before"), "Keep the diagnostic prefix and ordinary text.");
            Assert.That(comment, Does.EndWith("after"), "Do not truncate text after control characters.");
            Assert.That(comment.Any(c => char.IsControl(c) || c == '\u2028' || c == '\u2029'), Is.False,
                "No control character or line separator may remain in a single-line diagnostic.");
            AssertCommentsOnly(comment);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("ordinary]name")]
        [DataRow("name /* with */ delimiters")]
        [DataRow("name \u540d")]
        public void LineComment_PreservesOrdinaryText(string text)
        {
            Assert.That(SmoUtility.FormatSqlLineComment(text), Is.EqualTo("-- " + text),
                "Single-line text must not be changed merely because it contains identifier or block-comment syntax.");
        }

        [TestMethod]
        public void CommentCollection_EmptyCollectionRemainsEmptyWithoutHeading()
        {
            var scripts = new StringCollection();

            SmoUtility.EncodeStringCollectionAsComment(scripts);

            Assert.That(scripts, Is.Empty, "An empty collection must not acquire a script entry.");
        }

        [DataTestMethod]
        [DataRow("ordinaryServer")]
        [DataRow("server*/ SELECT 1 --")]
        [DataRow("server/*")]
        public void Policy_ConfigurationHeader_ProtectsTheCompletePath(string serverName)
        {
            var server = ServerTests.GetDesignModeServer(17);
            // Use the root name: ConvertUrnToPath already encodes descendant keys.
            server.ConnectionContext.TrueName = serverName;
            var store = new PolicyStore();
            ((ISfcHasConnection)store).ConnectionContext.Mode = SfcConnectionContextMode.Offline;
            var condition = new Condition(store, "AlwaysCompliant")
            {
                Facet = nameof(Server),
                ExpressionNode = new ExpressionNodeOperator(OperatorType.EQ,
                    new ExpressionNodeConstant(1), new ExpressionNodeConstant(1))
            };
            store.Conditions.Add(condition);
            var policy = new Policy(store, "TestPolicy") { Condition = condition.Name };

            var script = policy.ProduceConfigureScript(server);

            AssertCommentsOnly(script);
            Assert.That(script, Does.Contain(
                serverName.Contains("/*") || serverName.Contains("*/") ? "Scripting: '?'" : serverName),
                "Protect the complete policy path while retaining ordinary server names.");
        }

        private static void AssertCommentsOnly(string script)
        {
            var parsed = Parser.Parse(script);
            Assert.That(parsed.Errors, Is.Empty, "Diagnostic comments must parse without errors: " + script);
            Assert.That(parsed.Script.Batches.SelectMany(batch => batch.Statements), Is.Empty,
                "Diagnostic text must not become executable SQL: " + script);
            var followedByStatement = Parser.Parse(script + Environment.NewLine + "SELECT 42;");
            Assert.That(followedByStatement.Errors, Is.Empty, "Comments must not invalidate subsequent SQL.");
            Assert.That(followedByStatement.Script.Batches.SelectMany(batch => batch.Statements).Count(), Is.EqualTo(1),
                "Diagnostic comments must not swallow the next statement: " + script);
        }
    }
}
