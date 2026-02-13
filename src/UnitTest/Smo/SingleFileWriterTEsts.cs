using System;
using System.IO;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// SingleFileWriter is used by multiplefilewriter and is the base class for
    /// the notebook file writer. These tests assure the basic functionality doesn't 
    /// break as we refactor to support notebooks.
    /// </summary>
    [TestClass]
    public class SingleFileWriterTests
    {
        string filePath;
        readonly string nl = Environment.NewLine;

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
        public void SingleFileWriter_constructor()
        {
            using (var writer = new SingleFileWriter(filePath))
            {
                Assert.That(writer.ScriptBatchTerminator, Is.EqualTo(true), $"Default {nameof(writer.ScriptBatchTerminator)}");
                Assert.That(writer.BatchTerminator, Is.EqualTo("GO"), $"Default {nameof(writer.BatchTerminator)}");
                Assert.That(writer.InsertBatchSize, Is.EqualTo(100), $"Default {nameof(writer.InsertBatchSize)}");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SingleFileWriter_appendToFile()
        {
            var canary = "Line 1" + Environment.NewLine;
            File.WriteAllText(filePath, canary);
            using (var writer = new SingleFileWriter(filePath, appendToFile: true))
            {
                writer.Header = "header";
            }
            var text = File.ReadAllText(filePath);
            Assert.That(text, Is.EqualTo($"{canary}header{nl}"), "AppendToFile should preserve existing text");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SingleFileWriter_IScriptWriter_no_batch_terminator()
        {
            var context = "this is context";
            var scriptData = new[] { "data1", "data2" };
            var scriptObject = new[] { "object1", "object2" };
            using (var writer = new SingleFileWriter(filePath) { ScriptBatchTerminator = false })
            {
                // SingleFileWriter ignores the urn parameter to all methods
                writer.ScriptContext(context, null);
                writer.ScriptObject(scriptObject, null);
                writer.ScriptData(scriptData, null);
            }
            var text = File.ReadAllText(filePath);
            Assert.That(text, Is.EqualTo($"{context}{nl}{scriptObject[0]}{nl}{scriptObject[1]}{nl}{scriptData[0]}{nl}{scriptData[1]}{nl}"));
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SingleFileWriter_IScriptWriter_batching()
        {
            var context = "this is context";
            var scriptData = new[] { "data1", "data2", "data3" };
            var scriptObject = new[] { $"object1{nl}", nl, "object2" };
            using (var writer = new SingleFileWriter(filePath) { ScriptBatchTerminator = true, InsertBatchSize = 2 })
            {
                // SingleFileWriter ignores the urn parameter to all methods
                writer.ScriptContext(context, null);
                writer.ScriptObject(scriptObject, null);
                writer.ScriptData(scriptData, null);
            }
            var text = File.ReadAllText(filePath);
            Assert.That(text, Is.EqualTo($"{context}{nl}GO{nl}{scriptObject[0]}GO{nl}{nl}GO{nl}{scriptObject[2]}{nl}GO{nl}{scriptData[0]}{nl}{scriptData[1]}{nl}GO{nl}{scriptData[2]}{nl}GO{nl}"));
        }
    }
}