// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when BackupDatabaseTask fail
    /// </summary>
    public class BackupDatabaseTaskException : HadrTaskBaseException
    {
        /// <summary>
        /// Exception with Database Name and inner Exception
        /// </summary>
        public BackupDatabaseTaskException(string DatabaseName, Exception inner)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BackupDatabaseTaskException, DatabaseName), inner)
        {
        }

        /// <summary>
        /// Exception with Database Name 
        /// </summary>
        public BackupDatabaseTaskException(string DatabaseName)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BackupDatabaseTaskException, DatabaseName))
        {
        }
    }
}
