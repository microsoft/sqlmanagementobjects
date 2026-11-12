// Copyright (c) Microsoft.
// Licensed under the MIT license.
using Microsoft.SqlServer.Management.Smo.Notebook;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoNotebookUnitTests
{
    [TestClass]
    public class SmoNotebookTests
    {
        const char bl = '{';
        const char br = '}';
        const char qt = '"';
        static readonly string nl = Environment.NewLine;
        internal static string METADATAPRETTY = $"{qt}metadata{qt}: {bl}{nl}    {qt}kernel_spec{qt}: {bl}{nl}      {qt}name{qt}: {qt}SQL{qt},{nl}      {qt}language{qt}: {qt}sql{qt},{nl}      {qt}display_name{qt}: {qt}SQL{qt}{nl}    {br},{nl}    {qt}language_info{qt}: {bl}{nl}      {qt}name{qt}: {qt}sql{qt},{nl}      {qt}version{qt}: {qt}{qt}{nl}    {br}{nl}  {br}";
        internal static string METADATA = METADATAPRETTY.Replace(nl, "").Replace(" ", "");
 
        static SmoNotebookTests()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        internal static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var redirections = new HashSet<string>(new[] {
                "Newtonsoft.Json" }, System.StringComparer.OrdinalIgnoreCase);

            var an = new AssemblyName(args.Name);
            var dll = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), an.Name + ".dll");

            return redirections.Contains(an.Name) && File.Exists(dll) ? Assembly.LoadFrom(dll) : null;
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SmoNotebook_serialize_with_no_cells_has_empty_list()
        {
            var notebook = new SmoNotebook();
            using (var stream = new MemoryStream())
            {
                notebook.Save(stream, prettyPrint: true);
                var text = stream.ReadString();
                Assert.That(text, Is.EqualTo($"{bl}{nl}  {METADATAPRETTY},{nl}  {qt}nbformat{qt}: 4,{nl}  {qt}nbformat_minor{qt}: 2,{nl}  {qt}cells{qt}: []{nl}{br}"), "serialized Notebook with no cells");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SmoNotebook_AddCodeCell()
        {
            var notebook = new SmoNotebook();
            notebook.AddCodeCell(new[] { "line1", "line2" }, new Management.Sdk.Sfc.Urn("Server"));
            using (var stream = new MemoryStream())
            {
                notebook.Save(stream, prettyPrint: false);
                var text = stream.ReadString();
                Assert.That(text,
                    Does.Contain($"{qt}cells{qt}:[{bl}{qt}outputs{qt}:[],{qt}execution_count{qt}:0,{qt}cell_type{qt}:{qt}code{qt},{qt}source{qt}:[{qt}line1{qt},{qt}line2{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server{qt},{qt}object_type{qt}:{qt}Server{qt}{br}{br}]"),
                    "code cell with 2 lines");
            }
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SmoNotebook_AddMarkdownCell()
        {
            var notebook = new SmoNotebook();
            notebook.AddMarkdownCell(new[] { "line1", "line2" }, new Management.Sdk.Sfc.Urn("Server"));
            using (var stream = new MemoryStream())
            {
                notebook.Save(stream, prettyPrint: false);
                var text = stream.ReadString();
                Assert.That(text,
                    Does.Contain($"{qt}cells{qt}:[{bl}{qt}cell_type{qt}:{qt}markdown{qt},{qt}source{qt}:[{qt}line1{qt},{qt}line2{qt}],{qt}metadata{qt}:{bl}{qt}urn{qt}:{qt}Server{qt},{qt}object_type{qt}:{qt}Server{qt}{br}{br}"),
                    "markdown cell with 2 lines");
            }
        }
    }

    internal static class StreamExtensions
    {
        /// <summary>
        /// Reads the contents of stream as a string using the given Encoding.
        /// If no Encoding is provided, Encoding.Utf8 is used.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ReadString(this Stream stream, Encoding encoding = null)
        {
            stream.Seek(0, SeekOrigin.Begin);
            var text = new StreamReader(stream, encoding ?? Encoding.UTF8).ReadToEnd();
            return text;
        }
    }
}
