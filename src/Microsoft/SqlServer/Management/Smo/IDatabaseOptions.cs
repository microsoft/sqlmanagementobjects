// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [Facets.StateChangeEvent("CREATE_DATABASE", "DATABASE")]
    [Facets.StateChangeEvent("ALTER_DATABASE", "DATABASE")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "DATABASE")] // For Owner
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [DisplayNameKey("IDatabaseOptions_Name")]
    [DisplayDescriptionKey("IDatabaseOptions_Desc")]
    public interface IDatabaseOptions : IDmfFacet
    {
        [DisplayNameKey("Database_AnsiNullDefaultName")]
        [DisplayDescriptionKey("Database_AnsiNullDefaultDesc")]
        Boolean AnsiNullDefault { get; set; }

        [DisplayNameKey("Database_AnsiNullsEnabledName")]
        [DisplayDescriptionKey("Database_AnsiNullsEnabledDesc")]
        Boolean AnsiNullsEnabled { get; set; }

        [DisplayNameKey("Database_AnsiPaddingEnabledName")]
        [DisplayDescriptionKey("Database_AnsiPaddingEnabledDesc")]
        Boolean AnsiPaddingEnabled { get; set; }

        [DisplayNameKey("Database_AnsiWarningsEnabledName")]
        [DisplayDescriptionKey("Database_AnsiWarningsEnabledDesc")]
        Boolean AnsiWarningsEnabled { get; set; }

        [DisplayNameKey("Database_ArithmeticAbortEnabledName")]
        [DisplayDescriptionKey("Database_ArithmeticAbortEnabledDesc")]
        Boolean ArithmeticAbortEnabled { get; set; }

        [DisplayNameKey("Database_AutoCloseName")]
        [DisplayDescriptionKey("Database_AutoCloseDesc")]
        Boolean AutoClose { get; set; }

        [DisplayNameKey("Database_AutoCreateStatisticsEnabledName")]
        [DisplayDescriptionKey("Database_AutoCreateStatisticsEnabledDesc")]
        Boolean AutoCreateStatisticsEnabled { get; set; }

        [DisplayNameKey("Database_AutoCreateIncrementalStatisticsEnabledName")]
        [DisplayDescriptionKey("Database_AutoCreateIncrementalStatisticsEnabledDesc")]
        Boolean AutoCreateIncrementalStatisticsEnabled { get; set; }

        [DisplayNameKey("Database_AutoShrinkName")]
        [DisplayDescriptionKey("Database_AutoShrinkDesc")]
        Boolean AutoShrink { get; set; }

        [DisplayNameKey("Database_AutoUpdateStatisticsAsyncName")]
        [DisplayDescriptionKey("Database_AutoUpdateStatisticsAsyncDesc")]
        Boolean AutoUpdateStatisticsAsync { get; set; }

        [DisplayNameKey("Database_AutoUpdateStatisticsEnabledName")]
        [DisplayDescriptionKey("Database_AutoUpdateStatisticsEnabledDesc")]
        Boolean AutoUpdateStatisticsEnabled { get; set; }

        [DisplayNameKey("Database_BrokerEnabledName")]
        [DisplayDescriptionKey("Database_BrokerEnabledDesc")]
        Boolean BrokerEnabled { get; set; }

        [DisplayNameKey("Database_ChangeTrackingAutoCleanUpName")]
        [DisplayDescriptionKey("Database_ChangeTrackingAutoCleanUpDesc")]
        Boolean ChangeTrackingAutoCleanUp { get; set; }

        [DisplayNameKey("Database_ChangeTrackingEnabledName")]
        [DisplayDescriptionKey("Database_ChangeTrackingEnabledDesc")]
        Boolean ChangeTrackingEnabled { get; set; }

        [DisplayNameKey("Database_ChangeTrackingRetentionPeriodName")]
        [DisplayDescriptionKey("Database_ChangeTrackingRetentionPeriodDesc")]
        Int32 ChangeTrackingRetentionPeriod { get; set; }

        [DisplayNameKey("Database_ChangeTrackingRetentionPeriodUnitsName")]
        [DisplayDescriptionKey("Database_ChangeTrackingRetentionPeriodUnitsDesc")]
        Microsoft.SqlServer.Management.Smo.RetentionPeriodUnits ChangeTrackingRetentionPeriodUnits { get; set; }

        [DisplayNameKey("Database_CloseCursorsOnCommitEnabledName")]
        [DisplayDescriptionKey("Database_CloseCursorsOnCommitEnabledDesc")]
        Boolean CloseCursorsOnCommitEnabled { get; set; }

        [DisplayNameKey("Database_CollationName")]
        [DisplayDescriptionKey("Database_CollationDesc")]
        String Collation { get; set; }

        [DisplayNameKey("Database_CompatibilityLevelName")]
        [DisplayDescriptionKey("Database_CompatibilityLevelDesc")]
        Microsoft.SqlServer.Management.Smo.CompatibilityLevel CompatibilityLevel { get; set; }

        [DisplayNameKey("Database_ConcatenateNullYieldsNullName")]
        [DisplayDescriptionKey("Database_ConcatenateNullYieldsNullDesc")]
        Boolean ConcatenateNullYieldsNull { get; set; }

        [DisplayNameKey("Database_CreateDateName")]
        [DisplayDescriptionKey("Database_CreateDateDesc")]
        DateTime CreateDate { get; }

        [DisplayNameKey("Database_DatabaseOwnershipChainingName")]
        [DisplayDescriptionKey("Database_DatabaseOwnershipChainingDesc")]
        Boolean DatabaseOwnershipChaining { get; set; }

        [DisplayNameKey("Database_DatabaseSnapshotBaseNameName")]
        [DisplayDescriptionKey("Database_DatabaseSnapshotBaseNameDesc")]
        String DatabaseSnapshotBaseName { get; }

        [DisplayNameKey("Database_DateCorrelationOptimizationName")]
        [DisplayDescriptionKey("Database_DateCorrelationOptimizationDesc")]
        Boolean DateCorrelationOptimization { get; set; }

        [DisplayNameKey("Database_DefaultFileGroupName")]
        [DisplayDescriptionKey("Database_DefaultFileGroupDesc")]
        String DefaultFileGroup { get; }

        [DisplayNameKey("Database_DefaultFileStreamFileGroupName")]
        [DisplayDescriptionKey("Database_DefaultFileStreamFileGroupDesc")]
        String DefaultFileStreamFileGroup { get; }

        [DisplayNameKey("Database_EncryptionEnabledName")]
        [DisplayDescriptionKey("Database_EncryptionEnabledDesc")]
        Boolean EncryptionEnabled { get; set; }

        [DisplayNameKey("Database_HonorBrokerPriorityName")]
        [DisplayDescriptionKey("Database_HonorBrokerPriorityDesc")]
        Boolean HonorBrokerPriority { get; set; }

        [DisplayNameKey("Database_IDName")]
        [DisplayDescriptionKey("Database_IDDesc")]
        Int32 ID { get; }

        [DisplayNameKey("Database_IsLedgerName")]
        [DisplayDescriptionKey("Database_IsLedgerDesc")]
        Boolean IsLedger { get; set; }

        [DisplayNameKey("Database_IsParameterizationForcedName")]
        [DisplayDescriptionKey("Database_IsParameterizationForcedDesc")]
        Boolean IsParameterizationForced { get; set; }

        [DisplayNameKey("Database_IsReadCommittedSnapshotOnName")]
        [DisplayDescriptionKey("Database_IsReadCommittedSnapshotOnDesc")]
        Boolean IsReadCommittedSnapshotOn { get; set; }

        [DisplayNameKey("Database_IsSystemObjectName")]
        [DisplayDescriptionKey("Database_IsSystemObjectDesc")]
        Boolean IsSystemObject { get; }

        [DisplayNameKey("Database_IsUpdateableName")]
        [DisplayDescriptionKey("Database_IsUpdateableDesc")]
        Boolean IsUpdateable { get; }

        [DisplayNameKey("Database_LocalCursorsDefaultName")]
        [DisplayDescriptionKey("Database_LocalCursorsDefaultDesc")]
        Boolean LocalCursorsDefault { get; set; }

        [DisplayNameKey("NamedSmoObject_NameName")]
        [DisplayDescriptionKey("NamedSmoObject_NameDesc")]
        String Name { get; }   // sp_rename results in ALTER DATABASE statement

        [DisplayNameKey("Database_OwnerName")]
        [DisplayDescriptionKey("Database_OwnerDesc")]
        String Owner { get; }   

        [DisplayNameKey("Database_NumericRoundAbortEnabledName")]
        [DisplayDescriptionKey("Database_NumericRoundAbortEnabledDesc")]
        Boolean NumericRoundAbortEnabled { get; set; }
        
        [DisplayNameKey("Database_MirroringTimeoutName")]
        [DisplayDescriptionKey("Database_MirroringTimeoutDesc")]
        Int32 MirroringTimeout { get; set;}

        [DisplayNameKey("Database_OptimizedLockingOnName")]
        [DisplayDescriptionKey("Database_OptimizedLockingOnDesc")]
        Boolean OptimizedLockingOn { get; set; }

        [DisplayNameKey("Database_PageVerifyName")]
        [DisplayDescriptionKey("Database_PageVerifyDesc")]
        Microsoft.SqlServer.Management.Smo.PageVerify PageVerify { get; set; }

        [DisplayNameKey("Database_PrimaryFilePathName")]
        [DisplayDescriptionKey("Database_PrimaryFilePathDesc")]
        String PrimaryFilePath { get; }

        [DisplayNameKey("Database_QuotedIdentifiersEnabledName")]
        [DisplayDescriptionKey("Database_QuotedIdentifiersEnabledDesc")]
        Boolean QuotedIdentifiersEnabled { get; set; }

        [DisplayNameKey("Database_ReadOnlyName")]
        [DisplayDescriptionKey("Database_ReadOnlyDesc")]
        Boolean ReadOnly { get; set; }

        [DisplayNameKey("Database_RecoveryModelName")]
        [DisplayDescriptionKey("Database_RecoveryModelDesc")]
        Microsoft.SqlServer.Management.Smo.RecoveryModel RecoveryModel { get; set; }

        [DisplayNameKey("Database_RecursiveTriggersEnabledName")]
        [DisplayDescriptionKey("Database_RecursiveTriggersEnabledDesc")]
        Boolean RecursiveTriggersEnabled { get; set; }

        [DisplayNameKey("Database_RemoteDataArchiveEnabledName")]
        [DisplayDescriptionKey("Database_RemoteDataArchiveEnabledDesc")]
        Boolean RemoteDataArchiveEnabled { get; set; }

        [DisplayNameKey("Database_RemoteDataArchiveEndpointName")]
        [DisplayDescriptionKey("Database_RemoteDataArchiveEndpointDesc")]
        String RemoteDataArchiveEndpoint { get; set; }

        [DisplayNameKey("Database_RemoteDataArchiveLinkedServerName")]
        [DisplayDescriptionKey("Database_RemoteDataArchiveLinkedServerDesc")]
        String RemoteDataArchiveLinkedServer { get; }

        [DisplayNameKey("Database_RemoteDatabaseNameName")]
        [DisplayDescriptionKey("Database_RemoteDatabaseNameDesc")]
        String RemoteDatabaseName { get; }

        [DisplayNameKey("Database_RemoteDataArchiveUseFederatedServiceAccount")]
        [DisplayDescriptionKey("Database_RemoteDataArchiveUseFederatedServiceAccountDesc")]
        Boolean RemoteDataArchiveUseFederatedServiceAccount { get; }

        [DisplayNameKey("Database_RemoteDataArchiveCredentialName")]
        [DisplayDescriptionKey("Database_RemoteDataArchiveCredentialDesc")]
        String RemoteDataArchiveCredential { get; }

        [DisplayNameKey("Database_TrustworthyName")]
        [DisplayDescriptionKey("Database_TrustworthyDesc")]
        Boolean Trustworthy { get; set; }

        [DisplayNameKey("Database_UserAccessName")]
        [DisplayDescriptionKey("Database_UserAccessDesc")]
        Microsoft.SqlServer.Management.Smo.DatabaseUserAccess UserAccess { get; set; }

        [DisplayNameKey("Database_TargetRecoveryTimeName")]
        [DisplayDescriptionKey("Database_TargetRecoveryTimeDesc")]
        Int32 TargetRecoveryTime { get; set; }

        [DisplayNameKey("Database_DelayedDurabilityName")]
        [DisplayDescriptionKey("Database_DelayedDurabilityDesc")]
        DelayedDurability DelayedDurability { get; set; }

    }
}
