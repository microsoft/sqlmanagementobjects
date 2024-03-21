// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when any of the source databases' files is not in the default directory
    /// </summary>
    public class DatabaseFileNotInDefaultDirectoryException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="defaultDataFolder">Default data folder</param>
        /// <param name="defaultLogFolder">Default log folder</param>
        /// <param name="filesNotInDefaultDirectory">List of files not in default directory</param>
        public DatabaseFileNotInDefaultDirectoryException(string defaultDataFolder, string defaultLogFolder, IEnumerable<string> filesNotInDefaultDirectory)
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.DatabaseFileNotInDefaultDirectoryException, defaultDataFolder, defaultLogFolder, string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, filesNotInDefaultDirectory)))
        {
        }
    }
}
