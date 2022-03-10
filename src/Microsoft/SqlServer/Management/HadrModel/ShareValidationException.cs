// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

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
            : base(string.Format(CultureInfo.InvariantCulture, Resource.InvalidShare, backupLocation))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="backupLocation">Backup Location</param>
        /// <param name="innerException">Inner Exception</param>
        public ShareValidationException(string backupLocation, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.InvalidShare, backupLocation), innerException)
        {
        }
    }
}
