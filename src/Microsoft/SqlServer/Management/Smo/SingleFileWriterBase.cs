// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Base class for writing multiple scripts to a single file 
    /// </summary>
    internal abstract class SingleFileWriterBase : ISmoScriptWriter, IDisposable
    {
        /// <summary>
        /// Current database context as string.
        /// </summary>
        string currentContext;

        /// <summary>
        /// Gets a string that will be written after every batch.
        /// </summary>
        public string BatchTerminator { get; set; }

        /// <summary>
        /// Gets or sets a Boolean value that determines whether to
        /// script batch terminator after every batch.
        /// </summary>
        public bool ScriptBatchTerminator { get; set; }

        /// <summary>
        /// Gets or sets the number of insert statements to wait before batch terminator.
        /// </summary>
        public int InsertBatchSize { get; set; }

        /// <summary>
        /// Whether the output should be formatted for human readability
        /// </summary>
        public bool Indented { get; set; }

        private bool _wroteHeader = false;

        protected SingleFileWriterBase()
        {
            BatchTerminator = Globals.Go;
            currentContext = string.Empty;
            InsertBatchSize = 100;
            ScriptBatchTerminator = true;
        }

        /// <summary>
        /// The header string to insert at the beginning of the script
        /// </summary>
        public string Header
        {
            set
            {
                //We only want to write the header once
                if (!_wroteHeader && !string.IsNullOrEmpty(value))
                {
                    //Immediately write out to our output stream without the batch terminator
                    //since the header shouldn't contain any content that requires being in
                    //a separate batch
                    WriteHeaderImpl(value);
                    _wroteHeader = true;
                }
            }
        }

        protected abstract void WriteHeaderImpl(string header);

        public void Dispose()
        {
            Dispose(true);
        }

        ~SingleFileWriterBase()
        {
            Dispose(false);
        }

        protected abstract void Dispose(bool disposing);
        public void ScriptContext(string databaseContext, Urn obj)
        {
            if (!databaseContext.Equals(this.currentContext, StringComparison.Ordinal))
            {
                if (this.ScriptBatchTerminator)
                {
                    ScriptContextImpl(new[] { databaseContext, BatchTerminator }, obj);
                }
                else
                {
                    ScriptContextImpl(new[] { databaseContext }, obj);
                }

                this.currentContext = databaseContext;
            }
        }

        protected abstract void ScriptContextImpl(IEnumerable<string> context, Urn obj);
        

        public void ScriptData(IEnumerable<string> dataScript, Urn table)
        {
            if (ScriptBatchTerminator && InsertBatchSize > 0)
            {
                foreach (var batch in dataScript.Batch(InsertBatchSize).Where(b => b.Any()))
                {
                    ScriptDataImpl(batch.Union(new[] { BatchTerminator }), table);
                }
            }
            else
            {
                ScriptDataImpl(dataScript, table);
            }
        }

        protected abstract void ScriptDataImpl(IEnumerable<string> dataScript, Urn table);

        public void ScriptObject(IEnumerable<string> script, Urn obj)
        {
            if (this.ScriptBatchTerminator)
            {
                ScriptObjectImpl(script.WithBatches(BatchTerminator), obj);
            }
            else
            {
                ScriptObjectImpl(script, obj);
            }
        }

        protected abstract void ScriptObjectImpl(IEnumerable<string> script, Urn obj);


        /// <summary>
        /// Checks the filename to verify that it does not contain
        /// invalid characters and that the path exists.
        /// </summary>
        /// <param name="path">A string value that contains the path to the file.</param>
        /// <returns></returns>
        protected void CheckValidFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ScriptWriterException(ExceptionTemplates.InnerException, new ArgumentNullException("path"));
            }


            string filename;
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(Path.GetFullPath(path))))
                {
                    throw new ScriptWriterException(ExceptionTemplates.FolderPathNotFound);
                }
                filename = Path.GetFileName(path);
            }
            catch (ArgumentException ex)
            {
                throw new ScriptWriterException(ExceptionTemplates.FileWritingException, ex);
            }

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new ScriptWriterException(ExceptionTemplates.InvalideFileName);
            }
        }
    }

    internal static class BatchExtension
    {
        internal static IEnumerable<string> WithBatches(this IEnumerable<string> script, string batchTerminator)
        {
            // ScriptObjectImpl treats each string as a line, so lines ending in NewLine can just have "GO\r\n" appended
            // The goal is to avoid introducing extra line feeds between a script and the batch separator, as they 
            // are meaningful in text objects like sprocs
            foreach (var line in script)
            {
                if (line.EndsWith(System.Environment.NewLine))
                {
                    yield return line + batchTerminator;
                }
                else
                {
                    yield return line;
                    yield return batchTerminator;
                }
            }
        }

        internal static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> collection, int batchSize)
        {
            var nextbatch = new T[batchSize];
            int i = 0;
            foreach (T item in collection)
            {
                nextbatch[i++] = item;
                if (i == batchSize)
                {
                    yield return nextbatch;
                    nextbatch = new T[batchSize];
                    i = 0;
                }
            }
            Array.Resize(ref nextbatch, i);
            yield return nextbatch;
        }
    }
}

