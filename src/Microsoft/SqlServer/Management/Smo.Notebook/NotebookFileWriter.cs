// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// Implementation of SingleFileWriterBase that persists scripts as a Jupyter Notebook
    /// The data is flushed to the file during Dispose or Close
    /// </summary>
    internal class NotebookFileWriter : SingleFileWriterBase
    {
        private SmoNotebook notebook = new SmoNotebook();
        private readonly string filePath;
        private Urn currentUrn = new Urn();
        /// <summary>
        /// Constructs a new NotebookFileWriter which persists to the given file name.
        /// Any existing file with that name will be overwritten.
        /// </summary>
        /// <param name="filePath"></param>
        public NotebookFileWriter(string filePath)
        {
            CheckValidFileName(filePath);
            this.filePath = filePath;
        }
        
        /// <summary>
        /// Flushes data to disk.
        /// </summary>
        public void Close()
        {
            if (notebook != null)
            {
                using (var stream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    notebook.Save(stream, prettyPrint: Indented);
                    notebook = null;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        /// <summary>
        /// If the urn is different from the current urn, inject a markdown cell
        /// with the name of the object being scripted
        /// </summary>
        /// <param name="urn"></param>
        private void DemarkUrn(Urn urn)
        {
            if (currentUrn != urn)
            {
                var type = urn.Type;
                var schema = urn.GetAttribute("Schema", type);
                var name = urn.GetNameForType(type);
                
                if (string.IsNullOrEmpty(schema))
                {
                    name = $"# {SqlSmoObject.MakeSqlBraket(name)}";
                }
                else
                {
                    name = $"# {SqlSmoObject.MakeSqlBraket(schema)}.{SqlSmoObject.MakeSqlBraket(name)}";
                }
                notebook.AddMarkdownCell(new[] { name }, urn);
                currentUrn = urn;
            }
        }

        protected override void ScriptContextImpl(IEnumerable<string> context, Urn obj)
        {
            DemarkUrn(obj);
            notebook.AddCodeCell(context.WithLineFeeds(), obj);
        }

        protected override void ScriptDataImpl(IEnumerable<string> dataScript, Urn table)
        {
            DemarkUrn(table);
            notebook.AddCodeCell(dataScript.WithLineFeeds(), table);
        }

        protected override void ScriptObjectImpl(IEnumerable<string> script, Urn obj)
        {
            DemarkUrn(obj);
            notebook.AddCodeCell(script.WithLineFeeds(), obj);
        }

        protected override void WriteHeaderImpl(string header)
        {
            notebook.AddCodeCell(new[] { header }.WithLineFeeds(), null);
        }
    }

    internal static class LineFeedExtension
    {
        /// <summary>
        /// Adds Environment.NewLine to each string if it is not already there
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static IEnumerable<string> WithLineFeeds(this IEnumerable<string> lines)
        {
            return lines.Select(l => l.EndsWith(Environment.NewLine) ? l : l + Environment.NewLine);
        }
    }
}
