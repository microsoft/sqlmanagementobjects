// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This exception is thrown when a drive in which
    /// one of the database files are stored on primary is not
    /// found in the secondary replica
    /// </summary>
    public class DriveNotFoundOnReplicaException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="driveLetter">Drive Letter</param>
        /// <param name="replicaName">Replica Name</param>
        public DriveNotFoundOnReplicaException(char driveLetter, string replicaName) 
            : base(string.Format(CultureInfo.InvariantCulture,
                Resource.DataBaseDiskSizeValidationException,
                replicaName, driveLetter))
        {
        }

    }
}
