// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

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
            : base(Resource.FormatPrimaryCannotWriteToLocation(primaryServerName, backupLocation))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="primaryServerName">Name of the server</param>
        /// <param name="backupLocation">The backup location</param>
        /// <param name="innerException">The inner exception</param>
        public PrimaryCannotWriteToLocationException(string primaryServerName, string backupLocation, Exception innerException)
            : base(Resource.FormatPrimaryCannotWriteToLocation(primaryServerName, backupLocation), innerException)
        {
        }
    }
}
