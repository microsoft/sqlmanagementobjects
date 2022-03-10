// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Common;
using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Shared Data for AG Replicas
    /// </summary>
    public class AvailabilityGroupReplicaData
    {
        #region Constructor
        /// <summary>
        /// Default Constructor
        /// </summary>
        public AvailabilityGroupReplicaData()
        {
            this.BackupPriority = AvailabilityGroupReplica.DefaultBackupPriority;
            this.EndpointCanBeConfigured = true;
            this.EndpointPortNumber = AvailabilityGroupReplica.DefaultEndpointPortNumber;
            this.EndpointServiceAccount = null;
            this.EndpointEncryption = SMO.EndpointEncryption.Required;
            this.EndpointServiceAccountSid = null;
            this.Endpoint = null;
            this.EndpointEncryptionAlgorithm = SMO.EndpointEncryptionAlgorithm.Aes;
            this.ReadableSecondaryRole = SMO.AvailabilityReplicaConnectionModeInSecondaryRole.AllowNoConnections;
        }
        #endregion

        #region Properties
        /// <summary>
        /// The connection to the replica server
        /// </summary>
        [Bindable(false)]
        public ServerConnection Connection
        {
            get;
            set;
        }

        #region Replicas Properties
        /// <summary>
        /// ReplicaName
        /// </summary>
        public String ReplicaName
        {
            get;
            set;
        }

        /// <summary>
        /// Initial role of the replica
        /// </summary>
        public ReplicaRole InitialRole
        {
            get;
            set;
        }

        /// <summary>
        /// flag to determine if the replica is set to automatic failover
        /// Returns true if the replica is set to automatic failover, otherwise false
        /// </summary>
        public bool AutomaticFailover
        {
            get;
            set;
        }

        /// <summary>
        /// Availability mode of the availability replica
        /// </summary>
        public SMO.AvailabilityReplicaAvailabilityMode AvailabilityMode
        {
            get;
            set;
        }

        /// <summary>
        /// Readable secondary role of the availability replica
        /// </summary>
        public SMO.AvailabilityReplicaConnectionModeInSecondaryRole ReadableSecondaryRole
        {
            get;
            set;
        }

        /// <summary>
        /// The read-only routing list
        /// </summary>
        public IList<IList<string>> ReadOnlyRoutingList { get; set; }

        /// <summary>
        /// The read-only routing URL
        /// </summary>
        public string ReadOnlyRoutingUrl { get; set; }

        /// <summary>
        /// Gets the actual failover mode by cluster type
        /// </summary>
        /// <param name="clusterType">cluster type</param>
        /// <returns>Failover mode</returns>
        public SMO.AvailabilityReplicaFailoverMode GetFailoverMode(SMO.AvailabilityGroupClusterType clusterType)
        {
            return (clusterType == SMO.AvailabilityGroupClusterType.External) ? SMO.AvailabilityReplicaFailoverMode.External :
                (AutomaticFailover ? SMO.AvailabilityReplicaFailoverMode.Automatic : SMO.AvailabilityReplicaFailoverMode.Manual);
        }

        #endregion

        #region Endpoints Properties
        /// <summary>
        /// Property for Url for the endpoint
        /// </summary>
        public String EndpointUrl
        {
            get;
            set;
        }

        /// <summary>
        /// Endpoint port number
        /// </summary>
        public int EndpointPortNumber
        {
            get;
            set;
        }

        /// <summary>
        /// Endpoint name
        /// </summary>
        public string EndpointName
        {
            get;
            set;
        }

        /// <summary>
        /// EndpointEncryption flag based on EndpointEncryption enum
        /// </summary>
        public SMO.EndpointEncryption EndpointEncryption
        {
            get;
            set;
        }

        /// <summary>
        /// The endpoint encryption algorithm
        /// </summary>
        public SMO.EndpointEncryptionAlgorithm EndpointEncryptionAlgorithm
        {
            get;
            set;
        }

        /// <summary>
        /// Endpoint service account
        /// </summary>
        public string EndpointServiceAccount
        {
            get;
            set;
        }

        /// <summary>
        /// The Service Account Sid
        /// </summary>
        public byte[] EndpointServiceAccountSid
        {
            get;
            set;
        }

        /// <summary>
        /// True if the endpoint is already present on the replica
        /// </summary>
        public bool EndpointPresent
        {
            get;
            set;
        }

        /// <summary>
        /// flag to determine if the endpoint can be configured
        /// </summary>
        public bool EndpointCanBeConfigured
        {
            get;
            set;
        }

        /// <summary>
        /// The underlying SMO endpoint object
        /// </summary>
        public SMO.Endpoint Endpoint
        {
            get;
            set;
        }
        #endregion

        #region BackupPreferences Properties
        /// <summary>
        /// The back Up Priority for this AGReplica
        /// </summary>
        public int BackupPriority
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Gets the Replica state
        /// </summary>
        public AvailabilityObjectState State
        {
            get;
            set;
        }

        /// <summary>
        /// Determines whether the replica is a Failover Clustered Instance(FCI) or not
        /// </summary>
        public bool IsClustered
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the seeding mode
        /// </summary>
        public SMO.AvailabilityReplicaSeedingMode SeedingMode
        {
            get;
            set;
        }

        #endregion
    }
}
