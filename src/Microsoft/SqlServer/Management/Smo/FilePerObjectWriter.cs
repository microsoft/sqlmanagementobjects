// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Represents the class used to write scripts of database objects.
    /// </summary>
    internal class FilePerObjectWriter : ISmoScriptWriter, IDisposable
    {
        Dictionary<Urn, SingleFileWriter> SingleFileWriters;
        HashSet<string> fileNames;

        /// <summary>
        /// Specifies the encoding for the files used by T:Microsoft.SqlServer.Management.Smo.FilePerObjectWriter.
        /// </summary>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Specifies whether files are appended or overwritten.
        /// </summary>
        public bool AppendToFile { get; set; }

        string folderPath;
        const string FILE_EXTENSION = ".sql";
        const Char INVALID_CHARACTER_REPLACEMENT = ' ';

        /// <summary>
        /// String to be written at the end of every batch when complete.
        /// </summary>
        public string BatchTerminator { get; set; }

        /// <summary>
        /// Specifies whether or not to script batch terminator after every batch.
        /// </summary>
        public bool ScriptBatchTerminator { get; set; }

        /// <summary>
        /// Specifies the number of insert statements to write before batch terminator is set.
        /// </summary>
        public int InsertBatchSize { get; set; }

        /// <summary>
        /// Initializes the variables and properties to initialize a new instance of T:Microsoft.SqlServer.Management.Smo.FilePerObjectWriter.
        /// </summary>
        private void Init()
        {
            this.BatchTerminator = Globals.Go;
            this.InsertBatchSize = 100;
        }

        /// <summary>
        /// Creates a new instance of FilePerObjectWriter.
        /// </summary>
        /// <param name="folderPath">Folder path for the output files of T:Microsoft.SqlServer.Management.Smo.FilePerObjectWriter.</param>
        public FilePerObjectWriter(string folderPath)
        {
            VerfiyFolderPath(folderPath);
            this.SingleFileWriters = new Dictionary<Urn, SingleFileWriter>();
            this.folderPath = folderPath;
            Init();
            this.fileNames = new HashSet<string>();
        }

        /// <summary>
        /// Verifies that the FolderPath exists.
        /// </summary>
        /// <param name="folderPath">Folder path for the output files of T:Microsoft.SqlServer.Management.Smo.FilePerObjectWriter.</param>
        private void VerfiyFolderPath(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ScriptWriterException(ExceptionTemplates.InnerException, new ArgumentNullException("folderPath"));
            }
            if (!Directory.Exists(folderPath))
            {
                throw new ScriptWriterException(ExceptionTemplates.FolderPathNotFound);
            }
        }

        #region IScriptWriter implementation

        /// <summary>
        /// Writes object's script specified by the script parameter.
        /// </summary>
        /// <param name="script">The collection of script to be written.</param>
        /// <param name="obj">The database object that will be scripted.</param>
        /// <returns></returns>
        public void ScriptObject(IEnumerable<string> script, Urn obj)
        {
            SingleFileWriter filewriter = this.GetFileWriter(obj);
            filewriter.ScriptObject(script, obj);
        }

        /// <summary>
        /// Writes table's data script
        /// </summary>
        /// <param name="dataScript">The collection of script to be written.</param>
        /// <param name="table">The T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn to script.</param>
        /// <returns></returns>
        public void ScriptData(IEnumerable<string> dataScript, Urn table)
        {
            SingleFileWriter filewriter = this.GetFileWriter(table);
            filewriter.ScriptData(dataScript, table);
        }

        /// <summary>
        /// Writes USE [Database] segment of the script.
        /// </summary>
        /// <param name="databaseContext">A string that identifies the database context of objects to be scripted.</param>
        /// <param name="obj">The database object T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn to be scripted.</param>
        /// <returns></returns>
        public void ScriptContext(String databaseContext, Urn obj)
        {
            SingleFileWriter filewriter = this.GetFileWriter(obj);
            filewriter.ScriptContext(databaseContext, obj);
        }

        private string _header;
        /// <summary>
        /// The header string to insert at the beginning of the script
        /// </summary>
        public string Header
        {
            set
            {
                if(!string.IsNullOrEmpty(value))
                {
                    //We don't have a full list of all the writers we're going to be using so just save this off
                    //and append the header to each new writer we create.
                    //Note that we don't write anything to existing writers - they might already have content written
                    //and so writing a header now might end up with it in a weird place.
                    _header = value;
                }
            }
        }

        #endregion

        #region IDisposable implementation
        /// <summary>
        /// Disposes associated filestreams.
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        /// <summary>
        /// Disposes associated filestreams.
        /// </summary>
        public void Dispose()
        {
            foreach (KeyValuePair<Urn, SingleFileWriter> item in this.SingleFileWriters)
            {
                if (item.Value != null)
                {
                    item.Value.Dispose();
                }
            }
        }

        #endregion

        #region Filename
        /// <summary>
        /// Combine path, file name and extension
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected virtual String CompleteFileName(String fileName)
        {
            return Path.Combine(this.folderPath, fileName + FilePerObjectWriter.FILE_EXTENSION);
        }

        /// <summary>
        /// Initialize SingleFileWriter with Options
        /// </summary>
        /// <param name="uniqueFileName"></param>
        /// <returns></returns>
        private SingleFileWriter GetSingleFileWriter(string uniqueFileName)
        {
            SingleFileWriter fileWriter;
            if (this.Encoding != null)
            {
                fileWriter = new SingleFileWriter(CompleteFileName(uniqueFileName), this.AppendToFile, this.Encoding);
            }
            else
            {
                fileWriter = new SingleFileWriter(CompleteFileName(uniqueFileName), this.AppendToFile);
            }
            fileWriter.ScriptBatchTerminator = this.ScriptBatchTerminator;
            fileWriter.InsertBatchSize = this.InsertBatchSize;
            fileWriter.BatchTerminator = this.BatchTerminator;

            if (!string.IsNullOrEmpty(this._header))
            {
                fileWriter.Header = _header;
            }

            return fileWriter;
        }

        /// <summary>
        /// Gets SingleFilewriter for object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        protected virtual SingleFileWriter GetFileWriter(Urn obj)
        {
            if (obj.Type  == "UnresolvedEntity")
            {
                //It is unresolved entity
                return this.GetUnresolveEntityWriter();
            }
            // see if we have already written an object with this key
            if (this.SingleFileWriters.ContainsKey(obj))
            {
                return this.SingleFileWriters[obj];
            }
            else
            {
                SingleFileWriter filewriter;
                Urn key;

                key = ObjectKey(obj);

                if (this.SingleFileWriters.ContainsKey(key))
                {
                    return this.SingleFileWriters[key];
                }
                else
                {
                    String validFileName = GetValidFileName(key);
                    String uniqueFileName = validFileName;

                    // find a unique name by appending numbers to filenames
                    int count = 0;
                    while (this.fileNames.Contains(uniqueFileName))
                    {
                        count++;

                        uniqueFileName = String.Format(CultureInfo.InvariantCulture, "{0}[{1}]", validFileName, count);
                    }

                    filewriter = GetSingleFileWriter(uniqueFileName);
                    this.fileNames.Add(uniqueFileName);
                    this.SingleFileWriters.Add(key, filewriter);
                    return filewriter;
                }
            }
        }

        private static Urn ObjectKey(Urn obj)
        {
            if (obj.Type.Equals(DefaultConstraint.UrnSuffix))
            {
                if (obj.Parent.Type.Equals(Column.UrnSuffix))
                {
                    return obj.Parent.Parent;
                }
                else
                {
                    return obj;
                }
            }
            else
            {
                return ObjectKeyRec(obj);
            }
        }

        private static Urn ObjectKeyRec(Urn key)
        {
            //Try to add object related scripts like ALTER TABLE for table  to Objects's script
            switch (key.Type)
            {
                case "ForeignKey":
                case "Check":
                case "FullTextIndex":
                case "Index":
                case "Trigger":
                case "Column":
                case "Param":
                case "Statistic":
                    key = key.Parent;
                    break;
                case "ExtendedProperty":
                    key = ObjectKey(key.Parent);
                    break;
                default:
                    break;
            }
            return key;
        }

        private SingleFileWriter GetUnresolveEntityWriter()
        {
            string unresolvedEntity = "UnresolvedEntity";
            Urn urn = new Urn(unresolvedEntity);
            if (this.SingleFileWriters.ContainsKey(urn))
            {
                return this.SingleFileWriters[urn];
            }
            else
            {
                SingleFileWriter filewriter;
                string uniqueFileName = unresolvedEntity;
                filewriter = GetSingleFileWriter(uniqueFileName);
                this.fileNames.Add(uniqueFileName);
                this.SingleFileWriters.Add(urn, filewriter);
                return filewriter;
            }
        }

        /// <summary>
        /// Get a valid file name.
        /// Replaces characters that are not valid for files with InvalidCharacterReplacement
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        protected virtual String GetValidFileName(Urn urn)
        {
            String fileName = GetFileName(urn);

            char[] InvalidChars = Path.GetInvalidFileNameChars();

            foreach(char invalidCharecter in InvalidChars)
            {
                fileName = fileName.Replace(invalidCharecter, FilePerObjectWriter.INVALID_CHARACTER_REPLACEMENT);
            }

            return fileName;
        }

        /// <summary>
        /// Get filename from a Urn using Name and Schema
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        protected virtual String GetFileName(Urn urn)
        {
            StringBuilder fileName = new StringBuilder();

            String schema = urn.GetAttribute("Schema");

            if (schema != null && schema.Length > 0)
            {
                fileName.AppendFormat("{0}.", schema);
            }

            String name = urn.GetAttribute("Name");


            if (name == null)
            {
                throw new ScriptWriterException(string.Format(SmoApplication.DefaultCulture,ExceptionTemplates.FilePerObjectUrnMissingName, urn));
            }

            fileName.AppendFormat("{0}.{1}", name, urn.Type);

            return fileName.ToString();
        }

        #endregion
    }
}
