// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Facets;
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// List of SQL Server repair types
    /// </summary>
    public enum InDoubtTransactionResolutionType
    {
        NoPresumption = 0,      // Recovery fails if MS DTC cannot resolve any in-doubt transactions.
        PresumeCommit = 1,      // Any MS DTC in-doubt transactions are presumed to have committed.
        PresumeAbort = 2,       // Any MS DTC in-doubt transactions are presumed to have aborted.
    }

    /// <summary>
    /// List of SQL Server filestream access level types
    /// </summary>
    public enum FilestreamAccessLevelType
    {
        Disabled = 0,      
        TSqlAccess = 1,
        FullAccess = 2,      
    }

    /// <summary>
    /// This class acts as the flattener for Server properties hidden in Server.Settings and Server.Configuration
    /// Facets will inherit from this class to add new logical properties, use this class's Alter and Refresh methods, or
    /// they override the Alter and Refresh methods.
    /// 
    /// This class should not implement IDmfAdapter or any facet directly.  Facets must be on leaf-level classes and 
    /// this class is not intended to be a leaf-level class.
    /// </summary>
    public abstract class ServerAdapterBase : IAlterable, IRefreshable
    {
        private Microsoft.SqlServer.Management.Smo.Server wrappedObject = null;

        public ServerAdapterBase(Microsoft.SqlServer.Management.Smo.Server obj)
        {
            this.wrappedObject = obj;
        }

        protected Microsoft.SqlServer.Management.Smo.Server Server
        {
            get
            {
                return this.wrappedObject;
            }
        }

        #region Server Settings
        /// <summary>
        /// Sets auditing mode for tracking Logins that have logged with success/failure.
        /// </summary>
        public AuditLevel LoginAuditLevel
        {
            get
            {
                return this.Server.AuditLevel;
            }
            set
            {
                this.Server.AuditLevel = value;
            }
        }

        /// <summary>
        /// SQL Server Authentication Mode
        /// </summary>
        public ServerLoginMode LoginMode
        {
            get
            {
                return this.Server.LoginMode;
            }
        }

        /// <summary>
        /// Gets service name that the server instance runs under
        /// </summary>
        public string ServiceName
        {
            get
            {
                return this.Server.ServiceName;
            }
        }

        /// <summary>
        /// Gets the startup mode for the service
        /// </summary>
        public ServiceStartMode ServiceStartMode
        {
            get
            {
                return this.Server.ServiceStartMode;
            }
        }

        /// <summary>
        /// The instance name of this Server
        /// </summary>
        public string InstanceName
        {
            get
            {
                return this.Server.InstanceName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public FileStreamEffectiveLevel FilestreamLevel
        {
            get
            {
                return this.Server.FilestreamLevel;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string FilestreamShareName
        {
            get
            {
                return this.Server.FilestreamShareName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string Collation
        {
            get
            {
                return this.Server.Collation;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string SqlDomainGroup
        {
            get
            {
                return this.Server.SqlDomainGroup;
            }
        }

        // Local registry setting.�Clusters should read from the active node.�

        /// <summary>
        /// 
        /// </summary>
        public string InstallDataDirectory
        {
            get
            {
                return this.Server.InstallDataDirectory;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BackupDirectory
        {
            get
            {
                return this.Server.BackupDirectory;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultFile
        {
            get
            {
                return this.Server.DefaultFile;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string DefaultLog
        {
            get
            {
                return this.Server.DefaultLog;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool NamedPipesEnabled
        {
            get 
            {
                return this.Server.NamedPipesEnabled;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool TcpEnabled
        {
            get
            {
                return this.Server.TcpEnabled;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string InstallSharedDirectory
        {
            get
            {
                return this.Server.InstallSharedDirectory;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public ServiceStartMode BrowserStartMode
        {
            get
            {
                return this.Server.BrowserStartMode;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string BrowserServiceAccount
        {
            get
            {
                return this.Server.BrowserServiceAccount;
            }
        }
        #endregion

        #region Server Configuration Options

        /// <summary>
        /// Enables or disables contained databases and
        /// authentication using Facets, across a server instance.
        /// </summary>
        public bool ContainmentEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.ContainmentEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.ContainmentEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool AdHocRemoteQueriesEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.AdHocDistributedQueriesEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.AdHocDistributedQueriesEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Binds SQL Server disk I/O to a specified subset of CPUs.
        /// </summary>
        public int AffinityMask
        {
            get
            {
                return this.Server.Configuration.AffinityMask.RunValue;
            }
            set
            {
                this.Server.Configuration.AffinityMask.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Binds SQL Server disk I/O to a specified subset of CPUs. This option is available only on the 64-bit version 
        /// of Microsoft SQL Server.
        /// </summary>
        public int Affinity64Mask
        {
            get
            {
                return this.Server.Configuration.Affinity64Mask.RunValue;
            }
            set
            {
                this.Server.Configuration.Affinity64Mask.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Binds SQL Server threads to a specified subset of CPUs.
        /// </summary>
        public int AffinityIOMask
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "AffinityIOMask",
                    this.Server.Configuration.AffinityIOMask.ConfigValue,
                    this.Server.Configuration.AffinityIOMask.RunValue);

                return this.Server.Configuration.AffinityIOMask.RunValue;
            }
            set
            {
                this.Server.Configuration.AffinityIOMask.ConfigValue = value;
            }
        }


        public int Affinity64IOMask
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "AffinityIOMask",
                    this.Server.Configuration.AffinityIOMask.ConfigValue,
                    this.Server.Configuration.AffinityIOMask.RunValue);

                return this.Server.Configuration.Affinity64IOMask.RunValue;
            }
            set
            {
                this.Server.Configuration.Affinity64IOMask.ConfigValue = value;
            }
        }

        public bool AgentXPsEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.AgentXPsEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.AgentXPsEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool AllowUpdates
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.AllowUpdates.RunValue);
            }
            set
            {
                this.Server.Configuration.AllowUpdates.ConfigValue = Convert.ToInt32(value);
            }
        }

#pragma warning disable 0612
        public bool AweEnabled
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "AweEnabled",
                    Convert.ToBoolean(this.Server.Configuration.AweEnabled.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.AweEnabled.RunValue));


                return Convert.ToBoolean(this.Server.Configuration.AweEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.AweEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Audits all attempts to access statements and objects. Writes attempts to a file in the SQL Server Data folder.
        /// </summary>
        public bool C2AuditTracingEnabled
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "C2AuditTracingEnabled",
                    Convert.ToBoolean(this.Server.Configuration.C2AuditMode.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.C2AuditMode.RunValue));

                return Convert.ToBoolean(this.Server.Configuration.C2AuditMode.RunValue);
            }
            set
            {
                this.Server.Configuration.C2AuditMode.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int BlockedProcessThreshold
        {
            get
            {
                return this.Server.Configuration.BlockedProcessThreshold.RunValue;
            }
            set
            {
                this.Server.Configuration.BlockedProcessThreshold.ConfigValue = value;
            }
        }

        public bool DefaultBackupCompressionEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.DefaultBackupCompression.RunValue);
            }
            set
            {
                this.Server.Configuration.DefaultBackupCompression.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool ClrIntegrationEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.IsSqlClrEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.IsSqlClrEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool CommonCriteriaComplianceEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.CommonCriteriaComplianceEnabled.RunValue);
            }
        }

        /// <summary>
        /// Specifies the threshold at which SQL Server creates and runs parallel plans for queries. 
        /// </summary>
        public int CostThresholdForParallelism
        {
            get
            {
                return this.Server.Configuration.CostThresholdForParallelism.RunValue;
            }
            set
            {
                this.Server.Configuration.CostThresholdForParallelism.ConfigValue = value;
            }
        }

        public bool CrossDBOwnershipChainingEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.CrossDBOwnershipChaining.RunValue);
            }
            set
            {
                this.Server.Configuration.CrossDBOwnershipChaining.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int CursorThreshold
        {
            get
            {
                return this.Server.Configuration.CursorThreshold.RunValue;
            }
            set
            {
                this.Server.Configuration.CursorThreshold.ConfigValue = value;
            }
        }

        public bool ExtensibleKeyManagementEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.ExtensibleKeyManagementEnabled.RunValue);
            }

            set
            {
                this.Server.Configuration.ExtensibleKeyManagementEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }


        public bool DatabaseMailEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.DatabaseMailEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.DatabaseMailEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool DefaultTraceEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.DefaultTraceEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.DefaultTraceEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int DefaultFullTextLanguage
        {
            get
            {
                return this.Server.Configuration.DefaultFullTextLanguage.RunValue;
            }
            set
            {
                this.Server.Configuration.DefaultFullTextLanguage.ConfigValue = value;
            }
        }

        public int DefaultLanguage
        {
            get
            {
                return this.Server.Configuration.DefaultLanguage.RunValue;
            }
            set
            {
                this.Server.Configuration.DefaultLanguage.ConfigValue = value;
            }
        }

        public bool DisallowResultsFromTriggers
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.DisallowResultsFromTriggers.RunValue);
            }
            set
            {
                this.Server.Configuration.DisallowResultsFromTriggers.ConfigValue = Convert.ToInt32(value);
            }
        }

        public FilestreamAccessLevelType FilestreamAccessLevel
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "FilestreamAccessLevel",
                    this.Server.Configuration.FillFactor.ConfigValue,
                    this.Server.Configuration.FillFactor.RunValue);

                return (FilestreamAccessLevelType)this.Server.Configuration.FilestreamAccessLevel.RunValue;
            }
            set
            {
                this.Server.Configuration.FilestreamAccessLevel.ConfigValue = (int) value;
            }
        }

        public bool OptimizeAdhocWorkloads
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.OptimizeAdhocWorkloads.RunValue);
            }
            set
            {
                this.Server.Configuration.OptimizeAdhocWorkloads.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int FillFactor
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "FillFactor",
                    this.Server.Configuration.FillFactor.ConfigValue,
                    this.Server.Configuration.FillFactor.RunValue);

                return this.Server.Configuration.FillFactor.RunValue;
            }
            set
            {
                this.Server.Configuration.FillFactor.ConfigValue = value;
            }
        }

        public int FullTextCrawlBandwidthMin
        {
            get
            {
                return this.Server.Configuration.FullTextCrawlBandwidthMin.RunValue;
            }
            set
            {
                this.Server.Configuration.FullTextCrawlBandwidthMin.ConfigValue = value;
            }
        }

        public int FullTextCrawlBandwidthMax
        {
            get
            {
                return this.Server.Configuration.FullTextCrawlBandwidthMax.RunValue;
            }
            set
            {
                this.Server.Configuration.FullTextCrawlBandwidthMax.ConfigValue = value;
            }
        }

        public int FullTextNotifyBandwidthMin
        {
            get
            {
                return this.Server.Configuration.FullTextNotifyBandwidthMin.RunValue;
            }
            set
            {
                this.Server.Configuration.FullTextNotifyBandwidthMin.ConfigValue = value;
            }
        }

        public int FullTextNotifyBandwidthMax
        {
            get
            {
                return this.Server.Configuration.FullTextNotifyBandwidthMax.RunValue;
            }
            set
            {
                this.Server.Configuration.FullTextNotifyBandwidthMax.ConfigValue = value;
            }
        }

        public int FullTextCrawlRangeMax
        {
            get
            {
                return this.Server.Configuration.FullTextCrawlRangeMax.RunValue;
            }
            set
            {
                this.Server.Configuration.FullTextCrawlRangeMax.ConfigValue = value;
            }
        }


        public InDoubtTransactionResolutionType InDoubtTransactionResolution
        {
            get
            {
                return (InDoubtTransactionResolutionType) this.Server.Configuration.InDoubtTransactionResolution.RunValue;
            }
            set
            {
                this.Server.Configuration.InDoubtTransactionResolution.ConfigValue = (int) value;
            }
        }

        public int IndexCreateMemory
        {
            get
            {
                return this.Server.Configuration.IndexCreateMemory.RunValue;
            }
            set
            {
                this.Server.Configuration.IndexCreateMemory.ConfigValue = value;
            }
        }


        /// <summary>
        /// Provides a means of reducing the system overhead that is associated with the excessive context switching 
        /// that occurs sometimes in symmetric multiprocessing (SMP) environments.
        /// </summary>
        public bool LightweightPoolingEnabled
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "LightweightPoolingEnabled",
                    Convert.ToBoolean(this.Server.Configuration.LightweightPooling.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.LightweightPooling.RunValue));

                return Convert.ToBoolean(this.Server.Configuration.LightweightPooling.RunValue);
            }
            set
            {
                this.Server.Configuration.LightweightPooling.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Sets the maximum number of available locks. This option limits the amount of memory the Database Engine uses for locks.
        /// </summary>
        public int DynamicLocks
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "DynamicLocks",
                    this.Server.Configuration.Locks.ConfigValue,
                    this.Server.Configuration.Locks.RunValue);

                return this.Server.Configuration.Locks.RunValue;
            }
            set
            {
                this.Server.Configuration.Locks.ConfigValue = value;
            }
        }


        /// <summary>
        /// Limits the number of processors that are used to run a single statement for each parallel plan execution.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get
            {
                return this.Server.Configuration.MaxDegreeOfParallelism.RunValue;
            }
            set
            {
                this.Server.Configuration.MaxDegreeOfParallelism.ConfigValue = value;
            }
        }

        public int MaxServerMemory
        {
            get
            {
                return this.Server.Configuration.MaxServerMemory.RunValue;
            }
            set
            {
                this.Server.Configuration.MaxServerMemory.ConfigValue = value;
            }
        }

        /// <summary>
        /// Configures the number of worker threads that are available to SQL Server processes.
        /// </summary>
        public int MaxWorkerThreads
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "MaxWorkerThreads",
                    this.Server.Configuration.MaxWorkerThreads.ConfigValue,
                    this.Server.Configuration.MaxWorkerThreads.RunValue);

                return this.Server.Configuration.MaxWorkerThreads.RunValue;
            }
            set
            {
                this.Server.Configuration.MaxWorkerThreads.ConfigValue = value;
            }
        }

        public int MediaRetention
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "MediaRetention",
                    this.Server.Configuration.MediaRetention.ConfigValue,
                    this.Server.Configuration.MediaRetention.RunValue);

                return this.Server.Configuration.MediaRetention.RunValue;
            }
            set
            {
                this.Server.Configuration.MediaRetention.ConfigValue = value;
            }
        }

        public int MinMemoryPerQuery
        {
            get
            {
                return this.Server.Configuration.MinMemoryPerQuery.RunValue;
            }
            set
            {
                this.Server.Configuration.MinMemoryPerQuery.ConfigValue = value;
            }
        }

        public int MinServerMemory
        {
            get
            {
                return this.Server.Configuration.MinServerMemory.RunValue;
            }
            set
            {
                this.Server.Configuration.MinServerMemory.ConfigValue = value;
            }
        }

        public bool NestedTriggersEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.NestedTriggers.RunValue);
            }
            set
            {
                this.Server.Configuration.NestedTriggers.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Sets the packet size (in bytes) that is used across the whole network.
        /// </summary>
        public int NetworkPacketSize
        {
            get
            {
                return this.Server.Configuration.NetworkPacketSize.RunValue;
            }
            set
            {
                this.Server.Configuration.NetworkPacketSize.ConfigValue = value;
            }
        }

        public bool OleAutomationEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.OleAutomationProceduresEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.OleAutomationProceduresEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Sets the maximum number of database objects that can be open at one time on an instance of SQL Server 2000 Database 
        /// objects are those objects that are defined in the sysobjects table: tables, views, rules, stored procedures, defaults, 
        /// and triggers
        /// </summary>
        public int OpenObjects
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "OpenObjects",
                    this.Server.Configuration.OpenObjects.ConfigValue,
                    this.Server.Configuration.OpenObjects.RunValue);

                return this.Server.Configuration.OpenObjects.RunValue;
            }
            set
            {
                this.Server.Configuration.OpenObjects.ConfigValue = value;
            }
        }

        public bool PrecomputeRank
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.PrecomputeRank.RunValue);
            }
            set
            {
                this.Server.Configuration.PrecomputeRank.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool PriorityBoost
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "PriorityBoost",
                    Convert.ToBoolean(this.Server.Configuration.PriorityBoost.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.PriorityBoost.RunValue));

                return Convert.ToBoolean(this.Server.Configuration.PriorityBoost.RunValue);
            }
            set
            {
                this.Server.Configuration.PriorityBoost.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int ProtocolHandlerTimeout
        {
            get
            {
                return this.Server.Configuration.ProtocolHandlerTimeout.RunValue;
            }
            set
            {
                this.Server.Configuration.ProtocolHandlerTimeout.ConfigValue = value;
            }
        }

        public int QueryGovernorCostLimit
        {
            get
            {
                return this.Server.Configuration.QueryGovernorCostLimit.RunValue;
            }
            set
            {
                this.Server.Configuration.QueryGovernorCostLimit.ConfigValue = value;
            }
        }

        public int QueryWait
        {
            get
            {
                return this.Server.Configuration.QueryWait.RunValue;
            }
            set
            {
                this.Server.Configuration.QueryWait.ConfigValue = value;
            }
        }

        public int RecoveryInterval
        {
            get
            {
                return this.Server.Configuration.RecoveryInterval.RunValue;
            }
            set
            {
                this.Server.Configuration.RecoveryInterval.ConfigValue = value;
            }
        }

        public bool RemoteAccessEnabled
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "RemoteAccessEnabled",
                    Convert.ToBoolean(this.Server.Configuration.RemoteAccess.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.RemoteAccess.RunValue));

                return Convert.ToBoolean(this.Server.Configuration.RemoteAccess.RunValue);
            }
            set
            {
                this.Server.Configuration.RemoteAccess.ConfigValue = Convert.ToInt32(value);
            }
        }


        public bool RemoteDacEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.RemoteDacConnectionsEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.RemoteDacConnectionsEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int RemoteLoginTimeout
        {
            get
            {
                return this.Server.Configuration.RemoteLoginTimeout.RunValue;
            }
            set
            {
                this.Server.Configuration.RemoteLoginTimeout.ConfigValue = value;
            }
        }

        public bool RemoteProcTransEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.RemoteProcTrans.RunValue);
            }
            set
            {
                this.Server.Configuration.RemoteProcTrans.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int RemoteQueryTimeout
        {
            get
            {
                return this.Server.Configuration.RemoteQueryTimeout.RunValue;
            }
            set
            {
                this.Server.Configuration.RemoteQueryTimeout.ConfigValue = value;
            }
        }

        public int ReplicationMaxTextSize
        {
            get
            {
                return this.Server.Configuration.ReplicationMaxTextSize.RunValue;
            }
            set
            {
                this.Server.Configuration.ReplicationMaxTextSize.ConfigValue = value;
            }
        }

        public bool ReplicationXPsEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.ReplicationXPsEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.ReplicationXPsEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool ScanForStartupProcedures
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "ScanForStartupProcedures",
                    Convert.ToBoolean(this.Server.Configuration.ScanForStartupProcedures.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.ScanForStartupProcedures.RunValue));

                return Convert.ToBoolean(this.Server.Configuration.ScanForStartupProcedures.RunValue);
            }
            set
            {
                this.Server.Configuration.ScanForStartupProcedures.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool ServerTriggerRecursionEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.ServerTriggerRecursionEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.ServerTriggerRecursionEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool SetWorkingSetSize
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "SetWorkingSetSize",
                    Convert.ToBoolean(this.Server.Configuration.SetWorkingSetSize.ConfigValue),
                    Convert.ToBoolean(this.Server.Configuration.SetWorkingSetSize.RunValue));

                return Convert.ToBoolean(this.Server.Configuration.SetWorkingSetSize.RunValue);
            }
            set
            {
                this.Server.Configuration.SetWorkingSetSize.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool ShowAdvancedOptions
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.ShowAdvancedOptions.RunValue);
            }
            set
            {
                this.Server.Configuration.ShowAdvancedOptions.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool SmoAndDmoXPsEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.SmoAndDmoXPsEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.SmoAndDmoXPsEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool SqlMailEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.SqlMailXPsEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.SqlMailXPsEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool TransformNoiseWords
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.TransformNoiseWords.RunValue);
            }
            set
            {
                this.Server.Configuration.TransformNoiseWords.ConfigValue = Convert.ToInt32(value);
            }
        }

        public int TwoDigitYearCutoff
        {
            get
            {
                return this.Server.Configuration.TwoDigitYearCutoff.RunValue;
            }
            set
            {
                this.Server.Configuration.TwoDigitYearCutoff.ConfigValue = value;
            }
        }

        public int UserConnections
        {
            get
            {
                Dmf.Common.Utils.CheckConfigurationProperty(
                    "UserConnections",
                    this.Server.Configuration.UserConnections.ConfigValue,
                    this.Server.Configuration.UserConnections.RunValue);

                return this.Server.Configuration.UserConnections.RunValue;
            }
            set
            {
                this.Server.Configuration.UserConnections.ConfigValue = value;
            }
        }

        public int UserInstanceTimeout
        {
            get
            {
                return this.Server.Configuration.UserInstanceTimeout.RunValue;
            }
            set
            {
                this.Server.Configuration.UserInstanceTimeout.ConfigValue = value;
            }
        }

        public bool UserInstancesEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.UserInstancesEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.UserInstancesEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }


        public int UserOptions
        {
            get
            {

                return this.Server.Configuration.UserOptions.RunValue;
            }
            set
            {
                this.Server.Configuration.UserOptions.ConfigValue = value;
            }
        }

        public bool WebAssistantEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.WebXPsEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.WebXPsEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        public bool XPCmdShellEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.XPCmdShellEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.XPCmdShellEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        /// <summary>
        /// Flag indicating whether Stretch feature is enabled for the Server
        /// </summary>
        public bool RemoteDataArchiveEnabled
        {
            get
            {
                return Convert.ToBoolean(this.Server.Configuration.RemoteDataArchiveEnabled.RunValue);
            }
            set
            {
                this.Server.Configuration.RemoteDataArchiveEnabled.ConfigValue = Convert.ToInt32(value);
            }
        }

        #endregion

        public virtual void Refresh()
        {
            this.Server.Refresh();
        }

        public virtual void Alter()
        {
            this.Server.Alter(true);
        }

    }


    /// <summary>
    /// This class is exactly the ServerAdapterBase class, but it implements IDmfAdapter
    /// </summary>
    public partial class ServerAdapter : ServerAdapterBase, IDmfAdapter
    {
        public ServerAdapter(Microsoft.SqlServer.Management.Smo.Server obj)
            : base(obj)
        {
        }
    }
}
