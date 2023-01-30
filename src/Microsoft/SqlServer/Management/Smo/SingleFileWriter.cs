// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

/*************************************************************
*                                                            *
*   Copyright (C) Microsoft Corporation. All rights reserved.*
*                                                            *
*************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Represents the class used to write scripts of database objects.
    /// </summary>
    internal class SingleFileWriter : SingleFileWriterBase
    {
        StreamWriter streamWriter;

        private void Init(string path, bool appendToFile, Encoding encoding)
        {
            CheckValidFileName(path);

            try
            {
                if (encoding != null)
                {
                    this.streamWriter = NetCoreHelpers.CreateStreamWriter(path, appendToFile, encoding);
                }
                else
                {
                    this.streamWriter = NetCoreHelpers.CreateStreamWriter(path, appendToFile);
                }
            }
            catch (IOException ex)
            {
                throw new ScriptWriterException(ExceptionTemplates.FileWritingException, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new ScriptWriterException(ExceptionTemplates.FileWritingException, ex);
            }
            catch(ArgumentException ex)
            {
                throw new ScriptWriterException(ExceptionTemplates.FileWritingException, ex);
            }
            catch(SecurityException ex)
            {
                throw new ScriptWriterException(ExceptionTemplates.FileWritingException, ex);
            }
        }


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the
        /// T:Microsoft.SqlServer.Management.Smo.SingleFileWriter class.
        /// </summary>
        /// <param name="path">A string value that contains the path to the file.</param>
        public SingleFileWriter(string path)
        {
            Init(path, false, null);
        }

        /// <summary>
        /// Initializes a new instance of the
        /// T:Microsoft.SqlServer.Management.Smo.SingleFileWriter class.
        /// </summary>
        /// <param name="path">A string value that contains the path to the file.</param>
        /// <param name="appendToFile">A Boolean value that indicates whether
        /// or not to append data to the file.</param>
        public SingleFileWriter(string path, bool appendToFile)
        {
            Init(path, appendToFile, null);
        }

        /// <summary>
        /// Initializes a new instance of the
        /// T:Microsoft.SqlServer.Management.Smo.SingleFileWriter class.
        /// </summary>
        /// <param name="path">A string value that contains the path to the file.</param>
        /// <param name="appendToFile">A Boolean value that indicates whether
        /// or not to append data to the file.</param>
        /// <param name="encoding">The encoding type to be used for writing to the file.</param>
        public SingleFileWriter(string path, bool appendToFile, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ScriptWriterException(ExceptionTemplates.InnerException, new ArgumentNullException("encoding"));
            }

            Init(path, appendToFile, encoding);
        }

        /// <summary>
        /// Initializes a new instance of the
        /// T:Microsoft.SqlServer.Management.Smo.SingleFileWriter class.
        /// </summary>
        /// <param name="path">A string value that contains the path to the file.</param>
        /// <param name="encoding">The encoding type to be used for writing to the file.</param>
        public SingleFileWriter(string path, Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ScriptWriterException(ExceptionTemplates.InnerException, new ArgumentNullException("encoding"));
            }

            Init(path, false, encoding);
        }

        #endregion

        #region IScriptWriter implementation

        /// <summary>
        /// Writes the script for a database object.
        /// </summary>
        /// <param name="script">A collection of string values that contain
        /// the scripts</param>
        /// <param name="obj">The database object to script.</param>
        /// <returns></returns>
        protected override void ScriptObjectImpl(IEnumerable<string> script, Urn obj)
        {
            foreach (string s in script)
            {
                streamWriter.WriteLine(s);
            }
        }

        /// <summary>
        /// Writes the script for a data table.
        /// </summary>
        /// <param name="dataScript">The scripts collection to which
        /// the data table script will be written.</param>
        /// <param name="table">The table to script.</param>
        /// <returns></returns>
        protected override void ScriptDataImpl(IEnumerable<string> dataScript, Urn table)
        {
            foreach (string s in dataScript)
            {
                streamWriter.WriteLine(s);
            }
        }

        /// <summary>
        /// Writes the USE [Database] segment of the script.
        /// </summary>
        /// <param name="databaseContext">The database context as string.</param>
        /// <param name="obj">An T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn.</param>
        /// <returns></returns>
        protected override void ScriptContextImpl(IEnumerable<string> databaseContext, Urn obj)
        {
            foreach(string s in databaseContext)
            {
                streamWriter.WriteLine(s);
            }
        }

        private bool _wroteHeader = false;
        /// <summary>
        /// The header string to insert at the beginning of the script
        /// </summary>
        protected override void WriteHeaderImpl(string header)
        {
            streamWriter.WriteLine(header);
        }

        #endregion

        #region IDisposable implementation

        /// <summary>
        /// Disposes the stream writer.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Disposes the stream writer.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && streamWriter != null)
            {
                streamWriter.Dispose();
                streamWriter = null;
            }
        }

        #endregion
    }
}
