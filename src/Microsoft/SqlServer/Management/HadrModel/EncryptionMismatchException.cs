// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// EncryptionMismatchException is thrown if the encryption of any replica is does not match the others in the Availability Group
    /// </summary>
    public class EncryptionMismatchException : HadrTaskBaseException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replicasString">The replicas</param>
        public EncryptionMismatchException(string replicasString)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.EncryptionMismatchException, replicasString))
        {
        }
    }
}
