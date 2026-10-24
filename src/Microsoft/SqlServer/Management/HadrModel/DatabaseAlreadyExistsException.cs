// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when a database being 
    /// added to the AG already exists on one of the 
    /// replicas.
    /// </summary>
    public class DatabaseAlreadyExistsException : HadrValidationErrorException
    {
        /// <summary>
        /// Standard Exception with replica Name, and databases that 
        /// already exist on the replica.
        /// </summary>
        /// <param name="replicaName">The replica name</param>
        /// <param name="existingDatabases">The names of databases that already exist</param>
        public DatabaseAlreadyExistsException(string replicaName, IEnumerable<string> existingDatabases)
            : base(Resource.FormatDatabasesExistingOnReplica(replicaName, string.Join(",", existingDatabases))){}
    }
}
