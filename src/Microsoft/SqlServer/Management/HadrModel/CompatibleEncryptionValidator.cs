// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Text;
using Microsoft.SqlServer.Management.HadrData;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// This class implements a validator for ensuring that the 
    /// encryption (includes encryption algorithms) for all the replicas are compatible.
    /// </summary>
    public class CompatibleEncryptionValidator : Validator
    {
        #region Properties
        /// <summary>
        /// The availability group data with which the class was
        /// initialized
        /// </summary>
        private AvailabilityGroupData AvailabilityGroupData
        {
            get;
            set;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="data">The availability group data</param>
        public CompatibleEncryptionValidator(AvailabilityGroupData data)
            : base(Resource.ValidatingEndpointEncryption)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            this.AvailabilityGroupData = data;
        }
        #endregion

        #region Validator
        /// <summary>
        /// The validator validates that the encryption algorithm for
        /// all replicas are compatible.
        /// 
        /// This task is tried once
        /// </summary>
        /// <param name="policy"></param>
        protected override void Validate(IExecutionPolicy policy)
        {
            // This task is only tried once. So expiration is set to true during the first execution.
            policy.Expired = true;

            SMO.EndpointEncryption encryption =
                this.AvailabilityGroupData.AvailabilityGroupReplicas[0].EndpointEncryption;

            SMO.EndpointEncryptionAlgorithm encryptionAlgorithm =
                this.AvailabilityGroupData.AvailabilityGroupReplicas[0].EndpointEncryptionAlgorithm;

            foreach (AvailabilityGroupReplica replica in this.AvailabilityGroupData.AvailabilityGroupReplicas)
            {
                // first check the encryption
                encryption = ValidateReplicaEndpointEncryption(replica, encryption);

                // second check the algorithm
                ValidateReplicaEndpointEncryptionAlgorithm(replica, encryptionAlgorithm);
            }
        }

        /// <summary>
        /// Check if the encryption of the replica is compatible with the one that is passed in
        /// </summary>
        /// <param name="replica">replica data</param>
        /// <param name="encryptionAlgorithm">the encryption algorithm to match</param>
        private void ValidateReplicaEndpointEncryptionAlgorithm(
            AvailabilityGroupReplica replica,
            SMO.EndpointEncryptionAlgorithm encryptionAlgorithm)
        {
            if (replica.EndpointEncryptionAlgorithm == SMO.EndpointEncryptionAlgorithm.AesRC4 ||
                replica.EndpointEncryptionAlgorithm == SMO.EndpointEncryptionAlgorithm.RC4Aes)
            {
                // If a replica supports both algorithms, then its compatible
                return;
            }

            if ((replica.EndpointEncryptionAlgorithm == SMO.EndpointEncryptionAlgorithm.Aes) && (encryptionAlgorithm == SMO.EndpointEncryptionAlgorithm.RC4))
            {
                throw new EncryptionAlgorithmMismatchException(this.GetReplicasString());
            }
            
            if ((replica.EndpointEncryptionAlgorithm == SMO.EndpointEncryptionAlgorithm.RC4) && (encryptionAlgorithm == SMO.EndpointEncryptionAlgorithm.Aes)) 
            {
                throw new EncryptionAlgorithmMismatchException(this.GetReplicasString());
            }
        }

        /// <summary>
        /// Check if the required/disabled property of the endpoint encryption matches
        /// </summary>
        /// <param name="replica">replica data</param>
        /// <param name="encryption">encryption</param>
        /// <returns>The replica encryption in the case where encryption passed in is supported, whereas the replica encryption is not supported</returns>
        private SMO.EndpointEncryption ValidateReplicaEndpointEncryption(
            AvailabilityGroupReplica replica,
            SMO.EndpointEncryption encryption)
        {
            if ((encryption == SMO.EndpointEncryption.Required) && (replica.EndpointEncryption == SMO.EndpointEncryption.Disabled))
            {
                throw new EncryptionMismatchException(this.GetReplicasString());
            }

            if ((encryption == SMO.EndpointEncryption.Disabled) && (replica.EndpointEncryption == SMO.EndpointEncryption.Required))
            {
                throw new EncryptionMismatchException(this.GetReplicasString());
            }

            if ((encryption == SMO.EndpointEncryption.Supported) && (replica.EndpointEncryption != SMO.EndpointEncryption.Supported))
            {
                encryption = replica.EndpointEncryption;
            }
            return encryption;
        }

        /// <summary>
        /// Returns a string that lists endpoints, endpointEncryption and EndpointEncryptionAlgorithm
        /// for replicas in the availabilityGroupData
        /// </summary>
        /// <returns>A string that lists endpoints, endpointEncryption and EndpointEncryptionAlgorithm</returns>
        private string GetReplicasString()
        {
            StringBuilder replicas = new StringBuilder();
            foreach (AvailabilityGroupReplica replica in this.AvailabilityGroupData.AvailabilityGroupReplicas)
            {
                LocalizableEnumConverter endpointEncryptionAlgorithmEnumConverter = new LocalizableEnumConverter(typeof(SMO.EndpointEncryptionAlgorithm));
                LocalizableEnumConverter endpointEncryptionEnumConverter = new LocalizableEnumConverter(typeof(SMO.EndpointEncryption));
                replicas.Append(string.Format(Resource.ReplicaEndpointStringOutputFormat,
                                              replica.EndpointName,
                                              replica.InitialRoleString,
                                              endpointEncryptionEnumConverter.ConvertToString(replica.EndpointEncryption),
                                              endpointEncryptionAlgorithmEnumConverter.ConvertToString(replica.EndpointEncryptionAlgorithm)));
            }
            return replicas.ToString();
        }

        #endregion
    }
}
