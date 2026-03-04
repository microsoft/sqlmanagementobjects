// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo.Notebook;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.IO;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoNotebookUnitTests
{
    [TestClass]
    public class NotebookFileWriterTests : UnitTestBase
    {
        const char bl = '{';
        const char br = '}';
        const char qt = '"';
        // newlines in json data are escaped
        readonly string nl = Environment.NewLine == "\r\n" ? @"\r\n" : @"\n";

        string filePath;

        // AssemblyInitialize attribute is used too late in the loading process
        static NotebookFileWriterTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve += SmoNotebookTests.CurrentDomain_AssemblyResolve;
        }

        [TestInitialize]
        public void Initialize()
        {
            filePath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            try
            {
                File.Delete(filePath);
            }
            catch { }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookFileWriter_constructor()
        {
            using (var writer = new NotebookFileWriter(filePath))
            {
                Assert.That(writer.ScriptBatchTerminator, Is.EqualTo(true), $"Default {nameof(writer.ScriptBatchTerminator)}");
                Assert.That(writer.BatchTerminator, Is.EqualTo("GO"), $"Default {nameof(writer.BatchTerminator)}");
                Assert.That(writer.InsertBatchSize, Is.EqualTo(100), $"Default {nameof(writer.InsertBatchSize)}");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookFileWriter_saves_only_on_close()
        {
            File.Delete(filePath);
            using (var writer = new NotebookFileWriter(filePath))
            {
                Assert.That(File.Exists(filePath), Is.False, "file should not exist after constructor");
                writer.ScriptContext("context", "Server");
                Assert.That(File.Exists(filePath), Is.False, "file should not exist after script");
                writer.Close();
                Assert.That(File.Exists(filePath), Is.True, "file should exist after close");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookFileWriter_empty_notebook()
        {
            using (var writer = new NotebookFileWriter(filePath))
            {
            }
            var text = File.ReadAllText(filePath);
            var expected = $"{bl}{SmoNotebookTests.METADATA},{qt}nbformat{qt}:4,{qt}nbformat_minor{qt}:2,{qt}cells{qt}:[]{br}";
            Assert.That(text, Is.EqualTo(expected), "empty notebook expected");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookFileWriter_IScriptWriter_no_batch_terminator()
        {
            var context = "this is context";
            var scriptData = new[] { "data1", "data2" };
            var scriptObject = new[] { "object1", "object2" };
            var urn = new Urn("Server/Database[@Name='db1']");
            using (var writer = new NotebookFileWriter(filePath) { ScriptBatchTerminator = false })
            {
                writer.ScriptContext(context, urn);
                writer.ScriptObject(scriptObject, urn);
                writer.ScriptData(scriptData, urn);
            }
            var text = File.ReadAllText(filePath);
            var expected = $"{bl}{SmoNotebookTests.METADATA},{qt}nbformat{qt}:4,{qt}nbformat_minor{qt}:2,{qt}cells{qt}:[{bl}{qt}cell_type{qt}:{qt}markdown{qt},{qt}source{qt}:[{qt}# [db1]{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}this is context{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}object1{nl}{qt},{qt}object2{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}data1{nl}{qt},{qt}data2{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br}]{br}";
            Assert.That(text, Is.EqualTo(expected), "one object no batch terminator");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookFileWriter_IScriptWriter_batching()
        {
            var context = $"this is context";
            var scriptData = new[] { "data1", "data2", "data3" };
            var scriptObject = new[] { $"object1{Environment.NewLine}", Environment.NewLine, "object2" };
            var urn = new Urn("Server/Database[@Name='db1']");
            using (var writer = new NotebookFileWriter(filePath) { ScriptBatchTerminator = true, InsertBatchSize = 2 })
            {
                // SingleFileWriter ignores the urn parameter to all methods
                writer.ScriptContext(context, urn);
                writer.ScriptObject(scriptObject, urn);
                writer.ScriptData(scriptData, urn);
            }
            var text = File.ReadAllText(filePath);
            var ex = $"{nl}";
            var expected = $"{bl}{SmoNotebookTests.METADATA},{qt}nbformat{qt}:4,{qt}nbformat_minor{qt}:2,{qt}cells{qt}:[{bl}{qt}cell_type{qt}:{qt}markdown{qt},{qt}source{qt}:[{qt}# [db1]{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}this is context{nl}{qt},{qt}GO{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}object1{nl}GO{nl}{qt},{qt}{nl}GO{nl}{qt},{qt}object2{nl}{qt},{qt}GO{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}data1{nl}{qt},{qt}data2{nl}{qt},{qt}GO{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br},{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}data3{nl}{qt},{qt}GO{nl}{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server/Database[@Name='db1']{qt},{qt}object_type{qt}:{qt}Database{qt}{br}{br}]{br}";
            
            Assert.That(text, Is.EqualTo(expected), "one object with batch terminator");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void NotebookFileWriter_adds_markdown_cell_per_urn()
        {
            var urn1 = new Urn("Server/Database[@Schema='schema' and @Name='name']/Table[@Name='table1']");
            var urn2 = new Urn("Server/Database[@Schema='schema' and @Name='name']");
            var writer = new NotebookFileWriter(filePath) { ScriptBatchTerminator = false };
            using (writer)
            {
                writer.ScriptObject(new[] { "obj1" }, urn1);
                writer.ScriptObject(new[] { "obj2" }, urn2);
            }
            var text = File.ReadAllText(filePath);
            Assert.That(text, Does.Contain($"{bl}{qt}cell_type{qt}:{qt}markdown{qt},{qt}source{qt}:[{qt}# [table1]{qt}]"), "markdown with urn1 no schema");
            Assert.That(text, Does.Contain($"{bl}{qt}cell_type{qt}:{qt}markdown{qt},{qt}source{qt}:[{qt}# [schema].[name]{qt}]"), "markdown with urn2 including schema");
        }
    }
}
