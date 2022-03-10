// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Newtonsoft.Json;

namespace Microsoft.SqlServer.Management.Smo.Notebook
{
    /// <summary>
    /// Enables creation and saving of a simple Jupyter notebook
    /// using the SQL kernel.
    /// Each code cell corresponds to one object URN. 
    /// Code for one URN can span multiple cells.
    /// Cells are saved in the order written.
    /// </summary>
    public class SmoNotebook
    {
        static readonly NotebookMetadata notebookMetadata = new NotebookMetadata();

        // version 4.2 is what Azure Data Studio outputs
        readonly NotebookModel notebookModel = new NotebookModel(4, 2)
        {
            metadata = notebookMetadata
        };

        /// <summary>
        /// Adds a code cell to the notebook
        /// </summary>
        /// <param name="content"></param>
        /// <param name="urn">Optional Urn to add as metadata to the cell</param>
        public void AddCodeCell(IEnumerable<string> content, Urn urn)
        {
            notebookModel.cells.Add(new CodeCellModel()
            {
                source = content.ToList(),
                metadata = new Dictionary<string, string>() { { "urn", urn?.Value }, { "object_type", urn?.Type } }
            });
        }

        /// <summary>
        /// Adds a markdown cell to the notebook
        /// </summary>
        /// <param name="content"></param>
        /// <param name="urn">Optional Urn to add as metadata to the cell</param>
        public void AddMarkdownCell(IEnumerable<string> content, Urn urn)
        {
            notebookModel.cells.Add(new MarkdownCellModel()
            {
                source = content.ToList(),
                metadata = new Dictionary<string, string>() { { "urn", urn?.Value }, { "object_type", urn?.Type } }
            });
        }

        /// <summary>
        /// Saves the complete Jupyter Notebook to the given stream in UTF8 encoding
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="prettyPrint"></param>
        public void Save(Stream stream, bool prettyPrint = false)
        {
            var options = new Newtonsoft.Json.JsonSerializerSettings() { Formatting = prettyPrint ? Formatting.Indented : Formatting.None };

            using (var textWriter = new StreamWriter(stream, System.Text.Encoding.UTF8, 100, leaveOpen:true))
            {
                using (var writer = new JsonTextWriter(textWriter))
                {
                    JsonSerializer.Create(options).Serialize(writer, notebookModel);
                }
            }
        }
    }

    internal class NotebookMetadata
    {
        static readonly KernelSpec kernelSpec = new KernelSpec();
        static readonly LanguageInfo languageInfo = new LanguageInfo();

        /// <summary>
        /// which kernel to run cells with
        /// </summary>
        public KernelSpec kernel_spec => kernelSpec;
        public LanguageInfo language_info => languageInfo;
    }

    internal class KernelSpec
    {
        public string name => "SQL";
        public string language => "sql";
        public string display_name => "SQL";
    }

    internal class LanguageInfo
    {
        public string name => "sql";
        public string version => "";
    }

    internal static class EncodeExtensions
    {
        const string WindowsNewLine = @"\\r\\n";
        const string OtherNewLine = @"\\n";
        /// <summary>
        /// JsonSerializer does not escape newlines in string values 
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        static public IEnumerable<string> WithJsonNewLines(this IEnumerable<string> lines)
        {
            var newLineEscape = Environment.NewLine == "\r\n" ? WindowsNewLine : OtherNewLine;
            return lines.Select(l => l.Replace(Environment.NewLine, newLineEscape));
        }
    }
}
