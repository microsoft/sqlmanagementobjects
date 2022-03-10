// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// PrimaryCannotWriteToLocationException is thrown when the backup location cannot
    /// be written to by the Primary Server.
    /// </summary>
    public class PrimaryCannotWriteToLocationException : HadrTaskBaseException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="primaryServerName">Name of the server</param>
        /// <param name="backupLocation">The backup location</param>
        public PrimaryCannotWriteToLocationException(string primaryServerName, string backupLocation)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.PrimaryCannotWriteToLocation, primaryServerName, backupLocation))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="primaryServerName">Name of the server</param>
        /// <param name="backupLocation">The backup location</param>
        /// <param name="innerException">The inner exception</param>
        public PrimaryCannotWriteToLocationException(string primaryServerName, string backupLocation, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.PrimaryCannotWriteToLocation, primaryServerName, backupLocation), innerException)
        {
        }
    }
}
