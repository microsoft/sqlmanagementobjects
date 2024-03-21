// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// EncryptionAlgorithmMismatchException is thrown if the encryption algorithm of any replica is does not match the others in the Availability Group
    /// </summary>
    public class EncryptionAlgorithmMismatchException : HadrValidationErrorException
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="replicasString">The replicas</param>
        public EncryptionAlgorithmMismatchException(string replicasString)
            : base(string.Format(CultureInfo.InvariantCulture, Resource.EncryptionMismatchException, replicasString))
        {
        }
    }
}
