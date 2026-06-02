// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// ShareValidationException is thrown when the backup location cannot
    /// be accessed by the user.
    /// </summary>
    public class ShareValidationException : HadrTaskBaseException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="backupLocation">BackupLocation</param>
        public ShareValidationException(string backupLocation)
            : base(Resource.FormatInvalidShare(backupLocation))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="backupLocation">Backup Location</param>
        /// <param name="innerException">Inner Exception</param>
        public ShareValidationException(string backupLocation, Exception innerException)
            : base(Resource.FormatInvalidShare(backupLocation), innerException)
        {
        }
    }
}
