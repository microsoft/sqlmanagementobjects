// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This class maps database file paths (i.e. DataFiles and LogFiles paths) from 
    /// the source server to the corresponding target server location specified by the user. 
    /// </summary>
    [ComVisible(false)]
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public sealed class DatabaseFileMappingsDictionary : Dictionary<string, string>
    {
        // Dictionary definition.
        private Dictionary<string, string> databaseFileMappingsDictionary = null;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public DatabaseFileMappingsDictionary()
        {
            databaseFileMappingsDictionary = new Dictionary<string, string>();
        }

        /// <summary>
        /// Determines whether this dictionary contains the specified database source file path.
        /// </summary>
        /// <param name="sourceFilePath">Full path to the source database file, including the file name as well.</param>
        /// <returns>true if the dictionary contains this file path; false otherwise.</returns>
        new public bool ContainsKey(string sourceFilePath)
        {
            return databaseFileMappingsDictionary.ContainsKey(sourceFilePath);
        }

        /// <summary>
        /// Gets or sets the value associated with the specified database source file path.
        /// </summary>
        /// <param name="sourceFilePath">Full path to the source database file, including the file name as well.</param>
        /// <returns>The target database file path associated with this original database source file path. If the specified source file path is not found, a get operation throws a System.Collections.Generic.KeyNotFoundException, and a set operation creates a new element with the specified source file path.</returns>
        new public string this[string sourceFilePath] 
        {
            get { return databaseFileMappingsDictionary[sourceFilePath];}
            set { databaseFileMappingsDictionary[sourceFilePath] = value; } 
        }

        /// <summary>
        /// Adds the specified database source file path and its associated database target file path 
        /// to the dictionary.
        /// </summary>
        /// <param name="sourceFilePath">Full path to the source database file, including the file name as well.</param>
        /// <param name="targetFilePath">Full path to the target database file, including the file name as well.</param>
        new public void Add(string sourceFilePath, string targetFilePath)
        {
            databaseFileMappingsDictionary.Add(sourceFilePath, targetFilePath);
        }
    }
}
