// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

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
            : base(Resource.FormatBackupDatabaseTaskException(DatabaseName), inner)
        {
        }

        /// <summary>
        /// Exception with Database Name 
        /// </summary>
        public BackupDatabaseTaskException(string DatabaseName)
            : base(Resource.FormatBackupDatabaseTaskException(DatabaseName))
        {
        }
    }
}
