// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.ComponentModel;
using System.Linq;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This facet aggregates various availability group state information. It is used to support 
    /// SQL Server manageability tools.
    /// </summary>
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("AvailabilityGroupStateName")]
    [Sfc.DisplayDescriptionKey("AvailabilityGroupStateDesc")]
    public interface IAvailabilityGroupState : Sfc.IDmfFacet, IRefreshable 
    {
        #region Properties

        /// <summary>
        /// Gets a value indicating whether the Availability Group is up. This is true if a functioning primary replica
        /// exists for the Availability Group.
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_IsOnlineName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_IsOnlineDesc")]
        bool IsOnline
        {
            get;
        }

        /// <summary>
        /// Gets a value indicating whether the failover mode of the availability group
        /// is Automatic Failover.
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_IsAutoFailoverName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_IsAutoFailoverDesc")]
        bool IsAutoFailover
        {
            get;
        }

        /// <summary>
        /// Gets the number of synchronous secondary replicas with Automatic Failover mode and Synchronized state.
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_NumberOfSynchronizedSecondaryReplicasName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_NumberOfSynchronizedSecondaryReplicasDesc")]
        int NumberOfSynchronizedSecondaryReplicas
        {
            get;
        }

        /// <summary>
        /// Gets the nubmer of replicas in a "Not Synchronizing" state.
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_NumberOfNotSynchronizingReplicasName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_NumberOfNotSynchronizingReplicasDesc")]
        int NumberOfNotSynchronizingReplicas
        {
            get;
        }

        /// <summary>
        /// Gets the number of replicas that are not in a "Synchronized" state. Since only synchronous replicas
        /// can be in a "synchronized" state, this does not apply to asynchronous replicas
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_NumberOfNotSynchronizedReplicasName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_NumberOfNotSynchronizedReplicasDesc")]
        int NumberOfNotSynchronizedReplicas
        {
            get;
        }

        /// <summary>
        /// Gets the number of replicas that are neither a primary or a secondary in the Availability Group.
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_NumberOfReplicasWithUnhealthyRoleName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_NumberOfReplicasWithUnhealthyRoleDesc")]
        int NumberOfReplicasWithUnhealthyRole
        {
            get;
        }

        /// <summary>
        /// Gets the nubmer of replicas that are not in a "Connected" state
        /// </summary>
        [Sfc.DisplayNameKey("AvailabilityGroupState_NumberOfDisconnectedReplicasName")]
        [Sfc.DisplayDescriptionKey("AvailabilityGroupState_NumberOfDisconnectedReplicasDesc")]
        int NumberOfDisconnectedReplicas
        {
            get;
        }

        #endregion
    }

    /// <summary>
    /// This is an adapter class that implements the <see cref="IAvailabilityGroupState"/> logical facet for 
    /// an Availability Group.
    /// </summary>
    public partial class AvailabilityGroupState : IAvailabilityGroupState, IDmfAdapter, IRefreshable
    {
        /// <summary>
        /// Initializes a new instance of the AvailabilityGroupState class.
        /// </summary>
        /// <param name="ag">The Availability Group this object will expose.</param>
        public AvailabilityGroupState(AvailabilityGroup ag)
        {
            this.ag = ag;
            this.isInitialized = false;
        }

        #region IAvailabilityGroupState Members

        /// <summary>
        /// Gets a value indicating whether the Availability Group is up. This is true if a functioning primary replica
        /// exists for the Availability Group.
        /// </summary>
        public bool IsOnline
        {
            get 
            {
                this.CheckInitialized();

                return this.isOnline;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the failover mode of the availability group is 
        /// is Automatic Failover. Accessing this property may result in an exception, so we 
        /// initialize it seperately.  Specifically, if the primary replica is not currently visible, 
        /// we throw an exception indicating this. This only occurs when accessing this property 
        /// from a secondary replica that cannot communicate with the cluster store. 
        /// </summary>
        public bool IsAutoFailover
        {
            get 
            {
                if (this.isAutoFailover == null)
                {
                    this.isAutoFailover = false;
                    string primaryServerName = this.ag.PrimaryReplicaServerName;
                    bool primaryOnline = !string.IsNullOrEmpty(primaryServerName);
                    AvailabilityReplica primaryReplica = primaryOnline ? this.ag.AvailabilityReplicas[primaryServerName] : null;
                    if (primaryReplica != null)
                    {
                        this.isAutoFailover =
                            primaryReplica.FailoverMode == AvailabilityReplicaFailoverMode.Automatic &&
                            primaryReplica.AvailabilityMode == AvailabilityReplicaAvailabilityMode.SynchronousCommit;
                    }
                    else
                    {
                        throw new PropertyCannotBeRetrievedException(ExceptionTemplates.PropertyCannotBeRetrievedFromSecondary("IsAutoFailover"));
                    }
                }

                return this.isAutoFailover.Value;
            }
        }

        /// <summary>
        /// Gets the number of synchronous secondary replicas with Automatic Failover mode and Synchronized state.
        /// </summary>
        public int NumberOfSynchronizedSecondaryReplicas
        {
            get 
            { 
                this.CheckInitialized();

                return this.numberOfSynchronizedSecondaryReplicas;
            }
        }

        /// <summary>
        /// Gets the nubmer of replicas in a "Not Synchronizing" state.
        /// </summary>
        public int NumberOfNotSynchronizingReplicas
        {
            get 
            {
                this.CheckInitialized();

                return this.numberOfNotSynchronizingReplicas;
            }
        }

        /// <summary>
        /// Gets the number of replicas that are not in a "Synchronized" state. Since only synchronous replicas
        /// can be in a "synchronized" state, this does not apply to asynchronous replicas
        /// </summary>
        public int NumberOfNotSynchronizedReplicas
        {
            get 
            {
                this.CheckInitialized();

                return this.numberOfNotSynchronizedReplicas;
            }
        }

        /// <summary>
        /// Gets the number of replicas that are neither a primary or a secondary in the Availability Group.
        /// </summary>
        public int NumberOfReplicasWithUnhealthyRole
        {
            get 
            {
                this.CheckInitialized();

                return this.numberOfReplicasWithUnhealthyRole;
            }
        }

        /// <summary>
        /// Gets the nubmer of replicas that are not in a "Connected" state
        /// </summary>
        public int NumberOfDisconnectedReplicas
        {
            get 
            {
                this.CheckInitialized();

                return this.numberOfDisconnectedReplicas;
            }
        }

        #endregion

        #region IRefreshable Members

        /// <summary>
        /// Refresh the availability group state data.
        /// </summary>
        public void Refresh()
        {
            this.ag.Refresh();
            this.isInitialized = false;
            this.isAutoFailover = null;
        }

        #endregion

        #region Private memebers

        private AvailabilityGroup ag;
        private bool isInitialized;
        private bool isOnline;
        private bool? isAutoFailover;
        private int numberOfSynchronizedSecondaryReplicas;
        private int numberOfNotSynchronizingReplicas;
        private int numberOfNotSynchronizedReplicas;
        private int numberOfReplicasWithUnhealthyRole;
        private int numberOfDisconnectedReplicas;

        private void Initialize()
        {
            this.isOnline = !string.IsNullOrEmpty(this.ag.PrimaryReplicaServerName);
                        
            this.numberOfSynchronizedSecondaryReplicas = (
                     from AvailabilityReplica ar in this.ag.AvailabilityReplicas
                     where (ar.Role == AvailabilityReplicaRole.Secondary)
                        && (ar.FailoverMode == AvailabilityReplicaFailoverMode.Automatic)
                        && (ar.AvailabilityMode == AvailabilityReplicaAvailabilityMode.SynchronousCommit)
                        && (ar.RollupSynchronizationState == AvailabilityReplicaRollupSynchronizationState.Synchronized)
                     select ar).Count();

            this.numberOfNotSynchronizingReplicas = (
                    from AvailabilityReplica ar in this.ag.AvailabilityReplicas
                    where (ar.RollupSynchronizationState == AvailabilityReplicaRollupSynchronizationState.NotSynchronizing)
                    select ar).Count();

            this.numberOfNotSynchronizedReplicas = (
                    from AvailabilityReplica ar in this.ag.AvailabilityReplicas
                    where (ar.AvailabilityMode == AvailabilityReplicaAvailabilityMode.SynchronousCommit)
                          && (ar.RollupSynchronizationState != AvailabilityReplicaRollupSynchronizationState.Synchronized)
                    select ar).Count();

            this.numberOfReplicasWithUnhealthyRole = (
                    from AvailabilityReplica ar in this.ag.AvailabilityReplicas
                    where (ar.Role != AvailabilityReplicaRole.Secondary) && (ar.Role != AvailabilityReplicaRole.Primary)
                    select ar).Count();

            this.numberOfDisconnectedReplicas = (
                    from AvailabilityReplica ar in this.ag.AvailabilityReplicas
                    where (ar.ConnectionState != AvailabilityReplicaConnectionState.Connected)
                    select ar).Count();

            this.isInitialized = true;
        }

        private void CheckInitialized()
        {
            if (!this.isInitialized)
            {
                this.Initialize();
            }
        }

        #endregion
    }
}
