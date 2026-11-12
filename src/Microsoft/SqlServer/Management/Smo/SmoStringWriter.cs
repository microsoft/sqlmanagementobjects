// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Represents the class used to write scripts of database objects.
    /// </summary>
    internal class SmoStringWriter : ISmoScriptWriter
    {
        /// <summary>
        /// Gets or sets the output collection of an
        /// instance of T:Microsoft.SqlServer.Management.Smo.SmoStringWriter.
        /// </summary>
        public StringCollection FinalStringCollection { get; set; }

        /// <summary>
        /// The current database context as string.
        /// </summary>
        string currentContext;

        /// <summary>
        /// Default Constructor that initializes a new instance of the
        /// T:Microsoft.SqlServer.Management.Smo.SmoStringWriter class.
        /// </summary>
        public SmoStringWriter()
        {
            this.FinalStringCollection = new StringCollection();
            this.currentContext = string.Empty;
        }

        /// <summary>
        /// Writes script for a database object.
        /// </summary>
        /// <param name="script">The collection of scripts to
        /// which the current script will be added.</param>
        /// <param name="obj">The T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn
        /// that will be scripted.</param>
        /// <returns></returns>
        public void ScriptObject(IEnumerable<string> script, Urn obj)
        {
            this.FinalStringCollection.AddCollection(script);
            PrependHeaderIfNeeded();
        }


        /// <summary>
        /// Writes script for a data table.
        /// </summary>
        /// <param name="dataScript">The collection of scripts to
        /// which the current script will be added.</param>
        /// <param name="table">The table to be scripted.</param>
        /// <returns></returns>
        public void ScriptData(IEnumerable<string> dataScript, Urn table)
        {
            this.FinalStringCollection.AddCollection(dataScript);
            PrependHeaderIfNeeded();
        }

        /// <summary>
        /// Writes the USE [Database] segment of the script.
        /// </summary>
        /// <param name="databaseContext">The database context as string.</param>
        /// <param name="obj">The T:Microsoft.SqlServer.Management.Sdk.Sfc.Urn
        /// that will be scripted.</param>
        /// <returns></returns>
        public void ScriptContext(String databaseContext, Urn obj)
        {
            if (!databaseContext.Equals(this.currentContext,StringComparison.Ordinal))
            {
                this.FinalStringCollection.Add(databaseContext);
                this.currentContext = databaseContext;
            }
            PrependHeaderIfNeeded();
        }

        private string _header;
        /// <summary>
        /// The header string to insert at the beginning of the script
        /// </summary>
        public string Header
        {
            private get
            {
                return _header;
            }
            set
            {
                //Only allow setting once if it's a valid value
                if (!string.IsNullOrEmpty(value))
                {
                    //See append header if needed below for context on why we do this
                    _header = value;
                }
            }
        }

        private bool _wroteHeader = false;
        /// <summary>
        /// Prepends the header to the script string if it's the first in our collection.
        /// </summary>
        /// <returns></returns>
        /// <remarks>This is a workaround for the scripting since the SmoStringWriter
        /// doesn't append batch terminators itself (which all the other writer
        /// implementations do). Instead it's up to the caller to do that - for example
        /// in ObjectExplorer\ObjectExplorer\src\SqlScriptMenu.cs::WriteStringCollection
        /// But we don't want to add a batch terminator to the header so instead we'll just wait
        /// for the first script string to come in and then prefix it with the header
        /// at that point. This isn't ideal but it's the safest way without doing a
        /// significant refactoring of this class and how it deals with batch terminators</remarks>
        private void PrependHeaderIfNeeded()
        {
            if (!this._wroteHeader && !string.IsNullOrEmpty(this.Header) && this.FinalStringCollection.Count > 0)
            {
                this._wroteHeader = true;
                this.FinalStringCollection[0] = this.Header + System.Environment.NewLine + System.Environment.NewLine + this.FinalStringCollection[0];
            }
        }
    }
}
