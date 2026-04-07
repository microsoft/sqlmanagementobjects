// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when <see cref="FreeDiskSpaceValidator"/> determines
    /// that there is not sufficient space to accomodate the database data files
    /// in the data-drive of the replica
    /// </summary>
    public class InSufficientFreeSpaceForDatabaseFilesException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replicaName">The replica name</param>
        public InSufficientFreeSpaceForDatabaseFilesException(string replicaName)
            : base(Resource.FormatInSufficientFreeSpaceForDataFiles(replicaName))
        {
        }
    }
}
