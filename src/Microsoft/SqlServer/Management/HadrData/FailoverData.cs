// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;


namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// AlwaysOn Failover Cluster Data
    /// Three kinds of servers are proposed in the class:
    /// EntryPoint Server : the replica from which the wizard starts. It could be a primary or secondary server.
    /// Primary Server : the primary replica of an availability group.
    /// Target (or new primary) Server : the new primary server after failover. It must be a secondary replica.
    /// </summary>
    public class FailoverData
    {

        #region EntryPointServer
        /// <summary>
        /// Gets or sets the EntryPoint server connection.
        /// The connection for the server instance from which the wizard starts.
        /// If the wizard starts from the primary replica of an Availability Group in the primary server, 
        /// then the connection is for the primary replica; otherwise, it is for the secondary replica. 
        /// </summary>
        public ServerConnection EntryPointServerConnection
        {
            get;
            set;
        }

        /// EntryPoint Server : the replica from which the wizard starts. It could be a primary or secondary server.
        private Smo.Server entryPointServer;

        //Properties for entryPointServer
        public Smo.Server EntryPointServer
        {
            get
            {
                if (this.entryPointServer == null)
                {
                    this.entryPointServer = FailoverUtilities.GetServerWithInitFieldsSetting(this.EntryPointServerConnection);
                }

                return this.entryPointServer;
            }
        }

        // Retrieve the true name of the Entry Point Server because the name could be a listener name if
        // users connect the AG with its listener.
        public string EntryPointServerTrueName
        {
            get
            {
                return this.EntryPointServer.ConnectionContext.TrueName;
            }
        }

        /// <summary>
        /// Method to clean EntryPointServer
        /// </summary>
        protected void SetEntryPointServerToNull()
        {
            this.entryPointServer = null;
            this.replicas = null;
        }

        #endregion

        #region AGData/PrimaryServer

        /// <summary>
        /// Primary Server Instance Name
        /// </summary>
        public string PrimaryServerInstanceName
        {
            get
            {
                return this.AvailabilityGroup.PrimaryReplicaServerName;
            }
        }      

        /// <summary>
        /// AG Data From the EntryPointServer
        /// </summary>
        public AvailabilityGroup AvailabilityGroup
        {
            get
            {
                AvailabilityGroup ag = this.EntryPointServer.AvailabilityGroups[this.AvailabilityGroupName];

                if (ag == null)
                {
                    throw new ArgumentNullException();
                }

                return ag;
            }
        }

        /// <summary>
        /// Replicas of the AG
        /// </summary>
        private AvailabilityReplicaCollection replicas;

        /// <summary>
        /// Properties of Replicas of the AG
        /// </summary>
        public AvailabilityReplicaCollection AvailabilityReplicas
        {
            get
            {
                if (this.replicas == null)
                {
                    this.replicas = this.AvailabilityGroup.AvailabilityReplicas;

                    // Retrieve the data of all the replicas
                    this.replicas.Refresh();
                }

                return this.replicas;
            }
        }

        /// <summary>
        /// Primary Replica
        /// </summary>
        public AvailabilityReplica PrimaryReplica
        {
            get
            {
                return this.AvailabilityReplicas[this.PrimaryServerInstanceName];
            }
        }

        /// <summary>
        /// Current AG Name
        /// </summary>
        public string AvailabilityGroupName
        {
            get;
            set;
        }
        #endregion

        #region TargetServer

        /// <summary>
        /// Failover Category
        /// This property should be determined by viewData
        /// </summary>
        public FailoverCategory TargetReplicaFailoverCategory
        {
            set;
            get;
        }

        /// <summary>
        /// Target Server
        /// If failover from a Secondary Replica, TargetServer should be set to this.EntryPointServer
        /// If failover from a Primary Replica, TargetServer should be new Smo.Server(this.TargetServerConnection)
        /// </summary>
        public Smo.Server TargetServer
        {
            get;
            set;
        }

        /// <summary>
        /// Primary Server
        /// </summary>
        public Smo.Server PrimaryServer
        {
            get; 
            set;
        }

        /// <summary>
        /// Target Servcer Connection
        /// If failover from a Secondary Replica, TargetServerConnection should be set to this.EntryPointServerConnection
        /// If failover from a Primary Replica, TargetServerConnection should be set to selected SecondaryReplica.Connection
        /// </summary>
        public ServerConnection TargetServerConnection
        {
            get;
            set;
        }

        /// <summary>
        /// Method to reset Target Server
        /// </summary>
        public void ResetTargetServer()
        {
            this.TargetServer = null;
        }

        /// <summary>
        /// Target AG Data
        /// </summary>
        public AvailabilityGroup TargetAvailabilityGroup
        {
            get
            {
                AvailabilityGroup ag = this.TargetServer.AvailabilityGroups[this.AvailabilityGroupName];

                if (ag == null)
                {
                    throw new InvalidOperationException(string.Format(Resource.AvailabilityGroupNotExistError, this.AvailabilityGroupName, this.TargetServer.Name));
                }

                return ag;
            }
        }

        /// <summary>
        /// Target Replica Name
        /// </summary>
        public string TargetReplicaName
        {
            get;
            set;
        }

        /// <summary>
        /// Target Replica 
        /// </summary>
        public AvailabilityReplica TargetAvailabilityReplica
        {
            get
            {
                AvailabilityReplica targetReplica = this.TargetAvailabilityGroup.AvailabilityReplicas[this.TargetReplicaName];

                if (targetReplica == null)
                {
                    throw new InvalidOperationException(Resource.FailoverTargetReplicaNotExistError);
                }

                return targetReplica;
            }
        }
        #endregion

    }
}
