// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{


    [Facets.StateChangeEvent("AUDIT_SERVER_OPERATION_EVENT", "SERVER")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    public interface IServerConfigurationFacet : Sfc.IDmfFacet
    {
        #region Interface Properties
        bool ContainmentEnabled
        {
            get;
            set;
        }

        bool AdHocRemoteQueriesEnabled
        {
            get;
            set;
        }

        int AffinityMask
        {
            get;
            set;
        }

        int Affinity64Mask
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int AffinityIOMask
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int Affinity64IOMask
        {
            get;
            set;
        }

        bool AgentXPsEnabled
        {
            get;
            set;
        }

        bool AllowUpdates
        {
            get;
            set;
        }


        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool AweEnabled
        {
            get;
            set;
        }

        int BlockedProcessThreshold
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool C2AuditTracingEnabled
        {
            get;
            set;
        }

        bool ClrIntegrationEnabled
        {
            get;
            set;
        }

        bool CommonCriteriaComplianceEnabled
        {
            get;
        }

        int CostThresholdForParallelism
        {
            get;
            set;
        }

        bool CrossDBOwnershipChainingEnabled
        {
            get;
            set;
        }

        int CursorThreshold
        {
            get;
            set;
        }

        bool DatabaseMailEnabled
        {
            get;
            set;
        }

        bool DefaultTraceEnabled
        {
            get;
            set;
        }

        int DefaultFullTextLanguage
        {
            get;
            set;
        }

        int DefaultLanguage
        {
            get;
            set;
        }

        bool DisallowResultsFromTriggers
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int FillFactor
        {
            get;
            set;
        }

        int FullTextCrawlBandwidthMin
        {
            get;
            set;
        }

        int FullTextCrawlBandwidthMax
        {
            get;
            set;
        }

        int FullTextNotifyBandwidthMin
        {
            get;
            set;
        }

        int FullTextNotifyBandwidthMax
        {
            get;
            set;
        }

        int FullTextCrawlRangeMax
        {
            get;
            set;
        }

        InDoubtTransactionResolutionType InDoubtTransactionResolution
        {
            get;
            set;
        }

        int IndexCreateMemory
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool LightweightPoolingEnabled
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int DynamicLocks
        {
            get;
            set;
        }


        int MaxDegreeOfParallelism
        {
            get;
            set;
        }

        int MaxServerMemory
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int MaxWorkerThreads
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int MediaRetention
        {
            get;
            set;
        }

        int MinMemoryPerQuery
        {
            get;
            set;
        }

        int MinServerMemory
        {
            get;
            set;
        }

        bool NestedTriggersEnabled
        {
            get;
            set;
        }

        int NetworkPacketSize
        {
            get;
            set;
        }

        bool OleAutomationEnabled
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int OpenObjects
        {
            get;
            set;
        }

        bool PrecomputeRank
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool PriorityBoost
        {
            get;
            set;
        }

        int ProtocolHandlerTimeout
        {
            get;
            set;
        }

        int QueryGovernorCostLimit
        {
            get;
            set;
        }

        int QueryWait
        {
            get;
            set;
        }

        int RecoveryInterval
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool RemoteAccessEnabled
        {
            get;
            set;
        }


        bool RemoteDacEnabled
        {
            get;
            set;
        }

        int RemoteLoginTimeout
        {
            get;
            set;
        }

        bool RemoteProcTransEnabled
        {
            get;
            set;
        }

        int RemoteQueryTimeout
        {
            get;
            set;
        }

        int ReplicationMaxTextSize
        {
            get;
            set;
        }

        bool ReplicationXPsEnabled
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool ScanForStartupProcedures
        {
            get;
            set;
        }

        bool ServerTriggerRecursionEnabled
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        bool SetWorkingSetSize
        {
            get;
            set;
        }

        bool ShowAdvancedOptions
        {
            get;
            set;
        }

        bool SmoAndDmoXPsEnabled
        {
            get;
            set;
        }

        bool SqlMailEnabled
        {
            get;
            set;
        }

        bool TransformNoiseWords
        {
            get;
            set;
        }

        int TwoDigitYearCutoff
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        int UserConnections
        {
            get;
            set;
        }

        int UserInstanceTimeout
        {
            get;
            set;
        }

        bool UserInstancesEnabled
        {
            get;
            set;
        }

        int UserOptions
        {
            get;
            set;
        }

        bool WebAssistantEnabled
        {
            get;
            set;
        }

        bool XPCmdShellEnabled
        {
            get;
            set;
        }


        bool DefaultBackupCompressionEnabled
        {
            get;
            set;
        }

        bool ExtensibleKeyManagementEnabled
        {
            get;
            set;
        }

        [PostConfigurationAction(PostConfigurationAction.RestartService)]
        FilestreamAccessLevelType FilestreamAccessLevel
        {
            get;
            set;
        }

        bool OptimizeAdhocWorkloads
        {
            get;
            set;
        }

        /// <summary>
        /// Flag indicating whether Stretch feature is enabled for the Server
        /// </summary>
        bool RemoteDataArchiveEnabled
        {
            get;
            set;
        } 

        #endregion
    }

    /// <summary>
    /// The Server Configuration properties need to only Alter and Refresh the Server.Configuration properties.
    /// This class inherits from ServerAdapterBase and overrides those functions.
    /// </summary>
    public partial class ServerConfigurationAdapter : ServerAdapterBase, IDmfAdapter, IServerConfigurationFacet
    {
        #region Constructors
        public ServerConfigurationAdapter(Microsoft.SqlServer.Management.Smo.Server obj) 
            : base (obj)
        {
        }
        #endregion

        #region Refresh and Alter
        /// <summary>
        /// The Server Configuration facet only affects Configuration options.
        /// This facet will override the ServerAdapter Refresh.
        /// </summary>
        public override void Refresh()
        {
            this.Server.Configuration.Refresh();
        }

        /// <summary>
        /// The Server Configuration facet only affects Configuration options.
        /// This facet will override the ServerAdapter Alter.
        /// </summary>
        public override void Alter()
        {
            this.Server.Configuration.Alter(true);
        }
        #endregion
    }
}
