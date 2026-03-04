// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when BackupLogTask fail
    /// </summary>
    public class BackupLogTaskException : HadrTaskBaseException
    {
        /// <summary>
        /// Exception with Database Name and inner Exception
        /// </summary>
        public BackupLogTaskException(string databaseName, Exception inner)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BackupLogTaskException, databaseName), inner)
        {
        }

        /// <summary>
        /// Exception with Database Name 
        /// </summary>
        public BackupLogTaskException(string databaseName)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.BackupLogTaskException, databaseName))
        {
        }
    }
}
