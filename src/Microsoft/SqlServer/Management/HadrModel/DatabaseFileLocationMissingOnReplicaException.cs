// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when a folder that is necessary
    /// to create a database on the secondary is missing.
    /// </summary>
    public class DatabaseFileLocationMissingOnReplicaException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replicaName">Name of the replica</param>
        /// <param name="missingFolders">Missing folders</param>
        public DatabaseFileLocationMissingOnReplicaException(string replicaName, IEnumerable<string> missingFolders)
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.DatabaseFileLocationMissingOnReplicaException, replicaName, string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, missingFolders)))
        {
        }
    }
}
