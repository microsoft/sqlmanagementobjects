// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when a database-file that is necessary
    /// to create a database already exists on the secondary.
    /// </summary>
    public class DatabaseFileAlreadyExistsOnReplicaException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replicaName">replicaName</param>
        /// <param name="existingFiles">conflicting files</param>
        public DatabaseFileAlreadyExistsOnReplicaException(string replicaName, IEnumerable<string> existingFiles)
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.ValidatingDatabaseFileExistingError, replicaName, string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, existingFiles)))
        {
        }
    }
}
