// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// SecondaryCannotReadLocationException is thrown when the backup location cannot
    /// be ready by the Secondary Server.
    /// </summary>
    public class SecondaryCannotReadLocationException : HadrTaskBaseException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="secondaryServerName">The name of the secondaryServer</param>
        /// <param name="backupLocation">The backup location</param>
        public SecondaryCannotReadLocationException(string secondaryServerName, string backupLocation)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.SecondaryCannotReadLocation, secondaryServerName, backupLocation))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="secondaryServerName">The name of the secondary server</param>
        /// <param name="backupLocation">The backup location</param>
        /// <param name="innerException">The inner exception</param>
        public SecondaryCannotReadLocationException(string secondaryServerName, string backupLocation, Exception innerException)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.SecondaryCannotReadLocation, secondaryServerName, backupLocation), innerException)
        {
        }
    }
}
