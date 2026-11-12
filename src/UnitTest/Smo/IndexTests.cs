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
    public class IndexTests : UnitTestBase
    {
        [DataTestMethod]
        [DataRow("urn:example:plain", "urn:example:plain", false, false)]
        [DataRow("urn:example:O'Brien", "urn:example:O''Brien", false, false)]
        [DataRow("urn:example:O'Brien", "urn:example:O''Brien", true, false)]
        [DataRow("urn:example:O'Brien", "urn:example:O''Brien", false, true)]
        [DataRow("urn:example:Two'Quoted'Parts", "urn:example:Two''Quoted''Parts", false, true)]
        [DataRow("urn:example:\u540d'\u524d", "urn:example:\u540d''\u524d", false, false)]
        [DataRow("urn:example:\u540d'\u524d", "urn:example:\u540d''\u524d", false, true)]
        public void Index_SelectiveXml_Script_EscapesNamespaceUri(
            string uri, string expectedUri, bool defaultNamespace, bool scriptForAlter)
        {
            var index = CreateSelectiveXmlIndex();
            index.IndexedXmlPathNamespaces.Add(new IndexedXmlPathNamespace(index, "ns")
            {
                Uri = uri,
                IsDefaultUri = defaultNamespace
            });

            var scripts = ScriptSelectiveXmlIndex(index, scriptForAlter);

            var nl = System.Environment.NewLine;
            var namespaceItem = defaultNamespace ? $"DEFAULT N'{expectedUri}'" : $"N'{expectedUri}' as ns";
            Assert.That(scripts.Length, Is.EqualTo(1), "Selective XML index scripting should produce one index statement.");
            Assert.That(scripts[0], Does.Contain($"WITH XMLNAMESPACES{nl}({nl}{namespaceItem}{nl})"),
                "The complete namespace clause must escape apostrophes without changing default/prefix handling.");
        }

        [DataTestMethod]
        [DataRow("/root/item[@name='OBrien']", "/root/item[@name=''OBrien'']", "xs:string", "xs:string", false)]
        [DataRow("/root/item[@name='OBrien']", "/root/item[@name=''OBrien'']", "xs:string", "xs:string", true)]
        [DataRow("/root/item", "/root/item", "type'name", "type''name", false)]
        [DataRow("/root/item", "/root/item", "type'name", "type''name", true)]
        [DataRow("/root/item[@name='Two' and @other='Parts']", "/root/item[@name=''Two'' and @other=''Parts'']", "xs:string", "xs:string", false)]
        [DataRow("/root/item[@name='Two' and @other='Parts']", "/root/item[@name=''Two'' and @other=''Parts'']", "xs:string", "xs:string", true)]
        [DataRow("/root/\u540d[@name='value']", "/root/\u540d[@name=''value'']", "xs:string", "xs:string", false)]
        [DataRow("/root/\u540d[@name='value']", "/root/\u540d[@name=''value'']", "xs:string", "xs:string", true)]
        public void Index_SelectiveXml_Script_EscapesPathAndXQueryType(
            string path, string expectedPath, string typeDescription, string expectedType, bool scriptForAlter)
        {
            var index = CreateSelectiveXmlIndex();
            index.IndexedXmlPaths[0].Path = path;
            index.IndexedXmlPaths[0].XQueryTypeDescription = typeDescription;

            var scripts = ScriptSelectiveXmlIndex(index, scriptForAlter);

            Assert.That(scripts.Length, Is.EqualTo(1), "Selective XML index scripting should produce one index statement.");
            Assert.That(scripts[0],
                Does.Contain($"{(scriptForAlter ? "ADD " : string.Empty)}[TestPath] = N'{expectedPath}' as XQUERY N'{expectedType}'"),
                "The complete promoted-path clause must escape path and XQuery type literals independently.");
        }

        private static Index CreateSelectiveXmlIndex()
        {
            var server = ServerTests.GetDesignModeServer(12);
            var database = new Database(server, "TestDatabase");
            var table = new Table(database, "TestTable", "dbo");
            table.Columns.Add(new Column(table, "Document", new DataType(SqlDataType.Xml)));
            table.SetState(SqlSmoState.Existing);
            var index = new Index(table, "TestSelectiveIndex") { IndexType = IndexType.SelectiveXmlIndex };
            index.IndexedColumns.Add(new IndexedColumn(index, "Document"));
            index.IndexedXmlPaths.Add(new IndexedXmlPath(index, "TestPath")
            {
                Path = "/root/item",
                PathType = IndexedXmlPathType.XQuery,
                XQueryTypeDescription = "xs:string"
            });
            return index;
        }

        private static string[] ScriptSelectiveXmlIndex(Index index, bool scriptForAlter)
        {
            var scripts = new StringCollection();
            var preferences = index.Parent.GetScriptingPreferencesForCreate();
            preferences.ScriptForAlter = scriptForAlter;
            if (scriptForAlter)
            {
                index.SetState(SqlSmoState.Existing);
                index.ScriptAlter(scripts, preferences);
            }
            else
            {
                index.ScriptCreate(scripts, preferences);
            }
            return scripts.Cast<string>().Where(script =>
                script.StartsWith("CREATE SELECTIVE XML INDEX ") || script.StartsWith("ALTER INDEX ")).ToArray();
        }

    }
}
