// Stub: returns empty strings for all resources to remove dependency on embedded .resx resources.

#nullable enable

namespace Microsoft.SqlServer.Management.Smo.SqlEnum
{
    internal static partial class StringSqlEnumerator
    {
        public static global::System.Globalization.CultureInfo? Culture { get; set; }

        internal static string? GetResourceString(string resourceKey, string? defaultValue = null) =>
            defaultValue ?? string.Empty;

        private static string GetResourceString(string resourceKey, string[]? formatterNames) =>
            string.Empty;

        /// <summary>Incorrect version tag. You must specify either a min_major, cloud_min_major or datawarehouse_enabled attribute. 
        /// 
        /// {elementContent}</summary>
        public static string @IncorrectVersionTag => GetResourceString("IncorrectVersionTag")!;
        /// <summary>Incorrect version tag. You must specify either a min_major, cloud_min_major or datawarehouse_enabled attribute. 
        /// 
        /// {elementContent}</summary>
        internal static string FormatIncorrectVersionTag(object? elementContent)
           => string.Format(Culture, GetResourceString("IncorrectVersionTag", new[] { "elementContent" }), elementContent);

        /// <summary>This object is not supported on Azure Synapse Analytics databases.</summary>
        public static string @ObjectNotSupportedOnSqlDw => GetResourceString("ObjectNotSupportedOnSqlDw")!;
        /// <summary>This object is only supported on Azure SQL Edge.</summary>
        public static string @ObjectSupportedOnlyOnSqlEdge => GetResourceString("ObjectSupportedOnlyOnSqlEdge")!;
        /// <summary>Value '{value}' on attribute '{attributeName}' was unable to be parsed correctly.</summary>
        public static string @InvalidAttributeValue => GetResourceString("InvalidAttributeValue")!;
        /// <summary>Value '{value}' on attribute '{attributeName}' was unable to be parsed correctly.</summary>
        internal static string FormatInvalidAttributeValue(object? value, object? attributeName)
           => string.Format(Culture, GetResourceString("InvalidAttributeValue", new[] { "value", "attributeName" }) ?? "", value, attributeName);

        /// <summary>Version was not specified on loading configuration file.</summary>
        public static string @NullVersionOnLoadingCfgFile => GetResourceString("NullVersionOnLoadingCfgFile")!;
        /// <summary>Enumeration object tag was not found.</summary>
        public static string @EnumObjectTagNotFound => GetResourceString("EnumObjectTagNotFound")!;
        /// <summary>Connection type is not valid.</summary>
        public static string @InvalidConnectionType => GetResourceString("InvalidConnectionType")!;
        /// <summary>Only Path or only FullName must be specified.</summary>
        public static string @OnlyPathOrFullName => GetResourceString("OnlyPathOrFullName")!;
        /// <summary>A Path must be specified with a FileName.</summary>
        public static string @FileNameMustHavePath => GetResourceString("FileNameMustHavePath")!;
        /// <summary>Database name must be specified.</summary>
        public static string @DatabaseNameMustBeSpecified => GetResourceString("DatabaseNameMustBeSpecified")!;
        /// <summary>File '{fileName}' was not found. Search paths were:
        /// {paths}
        /// The file might be missing in sources or misspelled in config.xml, or case sensitivity may not be preserved.</summary>
        public static string @FailedToLoadResFileFromPaths => GetResourceString("FailedToLoadResFileFromPaths")!;
        /// <summary>File '{fileName}' was not found. Search paths were:
        /// {paths}
        /// The file might be missing in sources or misspelled in config.xml, or case sensitivity may not be preserved.</summary>
        internal static string FormatFailedToLoadResFileFromPaths(object? fileName, object? paths)
           => string.Format(Culture, GetResourceString("FailedToLoadResFileFromPaths", new[] { "fileName", "paths" }), fileName, paths);

        /// <summary>File '{fileName}' was not found in Assembly {assembly}.
        /// The file might be missing in sources or misspelled in config.xml, or case sensitivity may not be preserved.</summary>
        public static string @FailedToLoadResFileFromAssembly => GetResourceString("FailedToLoadResFileFromAssembly")!;
        /// <summary>File '{fileName}' was not found in Assembly {assembly}.
        /// The file might be missing in sources or misspelled in config.xml, or case sensitivity may not be preserved.</summary>
        internal static string FormatFailedToLoadResFileFromAssembly(object? fileName, object? assembly)
           => string.Format(Culture, GetResourceString("FailedToLoadResFileFromAssembly", new[] { "fileName", "assembly" }), fileName, assembly);

        /// <summary>{objType} is not supported in dependency discovery. Only objects of the following types are supported: {suppList}.</summary>
        public static string @UnsupportedTypeDepDiscovery => GetResourceString("UnsupportedTypeDepDiscovery")!;
        /// <summary>{objType} is not supported in dependency discovery. Only objects of the following types are supported: {suppList}.</summary>
        internal static string FormatUnsupportedTypeDepDiscovery(object? objType, object? suppList)
           => string.Format(Culture, GetResourceString("UnsupportedTypeDepDiscovery", new[] { "objType", "suppList" }), objType, suppList);

        /// <summary>Cannot provide DataReader because of the properties: {propList}</summary>
        public static string @QueryNotSupportedPostProcess => GetResourceString("QueryNotSupportedPostProcess")!;
        /// <summary>Cannot provide DataReader because of the properties: {propList}</summary>
        internal static string FormatQueryNotSupportedPostProcess(object? propList)
           => string.Format(Culture, GetResourceString("QueryNotSupportedPostProcess", new[] { "propList" }), propList);

        /// <summary>failed to load assembly {assembly}.</summary>
        public static string @FailedToLoadAssembly => GetResourceString("FailedToLoadAssembly")!;
        /// <summary>failed to load assembly {assembly}.</summary>
        internal static string FormatFailedToLoadAssembly(object? assembly)
           => string.Format(Culture, GetResourceString("FailedToLoadAssembly", new[] { "assembly" }), assembly);

        /// <summary>failed to create Urn for object code: {objCode}</summary>
        public static string @FailedToCreateUrn => GetResourceString("FailedToCreateUrn")!;
        /// <summary>failed to create Urn for object code: {objCode}</summary>
        internal static string FormatFailedToCreateUrn(object? objCode)
           => string.Format(Culture, GetResourceString("FailedToCreateUrn", new[] { "objCode" }), objCode);

        /// <summary>Unknown operator.</summary>
        public static string @UnknownOperator => GetResourceString("UnknownOperator")!;
        /// <summary>{prop} must be specified on {obj}.</summary>
        public static string @PropMustBeSpecified => GetResourceString("PropMustBeSpecified")!;
        /// <summary>{prop} must be specified on {obj}.</summary>
        internal static string FormatPropMustBeSpecified(object? prop, object? obj)
           => string.Format(Culture, GetResourceString("PropMustBeSpecified", new[] { "prop", "obj" }), prop, obj);

        /// <summary>Urn is not valid for dependency discovery: {urn}</summary>
        public static string @InvalidUrnForDepends => GetResourceString("InvalidUrnForDepends")!;
        /// <summary>Urn is not valid for dependency discovery: {urn}</summary>
        internal static string FormatInvalidUrnForDepends(object? urn)
           => string.Format(Culture, GetResourceString("InvalidUrnForDepends", new[] { "urn" }), urn);

        /// <summary>too many database levels.</summary>
        public static string @TooManyDbLevels => GetResourceString("TooManyDbLevels")!;
        /// <summary>could not instantiate object {objType}.</summary>
        public static string @CouldNotInstantiateObj => GetResourceString("CouldNotInstantiateObj")!;
        /// <summary>could not instantiate object {objType}.</summary>
        internal static string FormatCouldNotInstantiateObj(object? objType)
           => string.Format(Culture, GetResourceString("CouldNotInstantiateObj", new[] { "objType" }), objType);

        /// <summary>{objType} is not derived from {objName}.</summary>
        public static string @NotDerivedFrom => GetResourceString("NotDerivedFrom")!;
        /// <summary>{objType} is not derived from {objName}.</summary>
        internal static string FormatNotDerivedFrom(object? objType, object? objName)
           => string.Format(Culture, GetResourceString("NotDerivedFrom", new[] { "objType", "objName" }), objType, objName);

        /// <summary>Unknown type: {type}.</summary>
        public static string @UnknownType => GetResourceString("UnknownType")!;
        /// <summary>Unknown type: {type}.</summary>
        internal static string FormatUnknownType(object? type)
           => string.Format(Culture, GetResourceString("UnknownType", new[] { "type" }), type);

        /// <summary>Configuration file is not valid.</summary>
        public static string @InvalidConfigurationFile => GetResourceString("InvalidConfigurationFile")!;
        /// <summary>invalid configuration file: missing section {section}.</summary>
        public static string @MissingSection => GetResourceString("MissingSection")!;
        /// <summary>invalid configuration file: missing section {section}.</summary>
        internal static string FormatMissingSection(object? section)
           => string.Format(Culture, GetResourceString("MissingSection", new[] { "section" }), section);

        /// <summary>object is not under database.</summary>
        public static string @NotDbObject => GetResourceString("NotDbObject")!;
        /// <summary>objects are not in a single database.</summary>
        public static string @NotSingleDb => GetResourceString("NotSingleDb")!;
        /// <summary>&lt;post_process&gt; tag missing class_name.</summary>
        public static string @NoClassNamePostProcess => GetResourceString("NoClassNamePostProcess")!;
        /// <summary>Operation not supported on version {version}.</summary>
        public static string @InvalidVersion => GetResourceString("InvalidVersion")!;
        /// <summary>Operation not supported on version {version}.</summary>
        internal static string FormatInvalidVersion(object? version)
           => string.Format(Culture, GetResourceString("InvalidVersion", new[] { "version" }), version);

        /// <summary>Operation not supported on {productName}.</summary>
        public static string @InvalidSqlServer => GetResourceString("InvalidSqlServer")!;
        /// <summary>Operation not supported on {productName}.</summary>
        internal static string FormatInvalidSqlServer(object? productName)
           => string.Format(Culture, GetResourceString("InvalidSqlServer", new[] { "productName" }), productName);

        /// <summary>Failed to retrieve dependency information ({rowInformation}).</summary>
        public static string @CouldNotGetInfoFromDependencyRow => GetResourceString("CouldNotGetInfoFromDependencyRow")!;
        /// <summary>Failed to retrieve dependency information ({rowInformation}).</summary>
        internal static string FormatCouldNotGetInfoFromDependencyRow(object? rowInformation)
           => string.Format(Culture, GetResourceString("CouldNotGetInfoFromDependencyRow", new[] { "rowInformation" }), rowInformation);

        /// <summary>SQL Server 2005</summary>
        public static string @SqlServer90Name => GetResourceString("SqlServer90Name")!;
        /// <summary>SQL Server 2000</summary>
        public static string @SqlServer80Name => GetResourceString("SqlServer80Name")!;
        /// <summary>The functionality you're trying to execute is disabled inside SQLCLR, if you still want to execute this functionality you can try executing it inside your client application.</summary>
        public static string @SmoSQLCLRUnAvailable => GetResourceString("SmoSQLCLRUnAvailable")!;
        /// <summary>Unknown Permission type {permissionType}</summary>
        public static string @UnknownPermissionType => GetResourceString("UnknownPermissionType")!;
        /// <summary>Unknown Permission type {permissionType}</summary>
        internal static string FormatUnknownPermissionType(object? permissionType)
           => string.Format(Culture, GetResourceString("UnknownPermissionType", new[] { "permissionType" }), permissionType);

        /// <summary>Undefined permission code {code}</summary>
        public static string @UnknownPermissionCode => GetResourceString("UnknownPermissionCode")!;
        /// <summary>Undefined permission code {code}</summary>
        internal static string FormatUnknownPermissionCode(object? code)
           => string.Format(Culture, GetResourceString("UnknownPermissionCode", new[] { "code" }), code);

        /// <summary>Executing</summary>
        public static string @Executing => GetResourceString("Executing")!;
        /// <summary>Waiting for worker thread</summary>
        public static string @WaitingForWorkerThread => GetResourceString("WaitingForWorkerThread")!;
        /// <summary>Between retries</summary>
        public static string @BetweenRetries => GetResourceString("BetweenRetries")!;
        /// <summary>Not running</summary>
        public static string @Idle => GetResourceString("Idle")!;
        /// <summary>Suspended</summary>
        public static string @Suspended => GetResourceString("Suspended")!;
        /// <summary>Waiting for step to finish</summary>
        public static string @WaitingForStepToFinish => GetResourceString("WaitingForStepToFinish")!;
        /// <summary>Performing completion action</summary>
        public static string @PerformingCompletionAction => GetResourceString("PerformingCompletionAction")!;
        /// <summary>Unknown</summary>
        public static string @Unknown => GetResourceString("Unknown")!;
        /// <summary>Queued</summary>
        public static string @Queued => GetResourceString("Queued")!;
        /// <summary>None</summary>
        public static string @ctNone => GetResourceString("ctNone")!;
        /// <summary>Partial</summary>
        public static string @ctPartial => GetResourceString("ctPartial")!;
        /// <summary>Full</summary>
        public static string @rmFull => GetResourceString("rmFull")!;
        /// <summary>Bulk Logged</summary>
        public static string @rmBulkLogged => GetResourceString("rmBulkLogged")!;
        /// <summary>Simple</summary>
        public static string @rmSimple => GetResourceString("rmSimple")!;
        /// <summary>None</summary>
        public static string @msNone => GetResourceString("msNone")!;
        /// <summary>Suspended</summary>
        public static string @msSuspended => GetResourceString("msSuspended")!;
        /// <summary>Disconnected</summary>
        public static string @msDisconnected => GetResourceString("msDisconnected")!;
        /// <summary>Synchronizing</summary>
        public static string @msSynchronizing => GetResourceString("msSynchronizing")!;
        /// <summary>Pending Failover</summary>
        public static string @msPendingFailover => GetResourceString("msPendingFailover")!;
        /// <summary>Synchronized</summary>
        public static string @msSynchronized => GetResourceString("msSynchronized")!;
        /// <summary>None Synchronizing</summary>
        public static string @agshNoneSynchronizing => GetResourceString("agshNoneSynchronizing")!;
        /// <summary>Partially Synchronizing</summary>
        public static string @agshPartiallySynchronizing => GetResourceString("agshPartiallySynchronizing")!;
        /// <summary>All Synchronizing</summary>
        public static string @agshAllSynchronizing => GetResourceString("agshAllSynchronizing")!;
        /// <summary>All Synchronized</summary>
        public static string @agshAllSynchronized => GetResourceString("agshAllSynchronized")!;
        /// <summary>Pending Failover</summary>
        public static string @arosPendingFailover => GetResourceString("arosPendingFailover")!;
        /// <summary>Pending</summary>
        public static string @arosPending => GetResourceString("arosPending")!;
        /// <summary>Online</summary>
        public static string @arosOnline => GetResourceString("arosOnline")!;
        /// <summary>Offline</summary>
        public static string @arosOffline => GetResourceString("arosOffline")!;
        /// <summary>Failed</summary>
        public static string @arosFailed => GetResourceString("arosFailed")!;
        /// <summary>Failed No Quorum</summary>
        public static string @arosFailedNoQuorum => GetResourceString("arosFailedNoQuorum")!;
        /// <summary>In Progress</summary>
        public static string @arrhInProgress => GetResourceString("arrhInProgress")!;
        /// <summary>Online</summary>
        public static string @arrhOnline => GetResourceString("arrhOnline")!;
        /// <summary>Not Synchronizing</summary>
        public static string @arshNotSynchronizing => GetResourceString("arshNotSynchronizing")!;
        /// <summary>Synchronizing</summary>
        public static string @arshSynchronizing => GetResourceString("arshSynchronizing")!;
        /// <summary>Synchronized</summary>
        public static string @arshSynchronized => GetResourceString("arshSynchronized")!;
        /// <summary>Uninitialized</summary>
        public static string @arrUninitialized => GetResourceString("arrUninitialized")!;
        /// <summary>Resolving</summary>
        public static string @arrResolving => GetResourceString("arrResolving")!;
        /// <summary>Secondary</summary>
        public static string @arrSecondary => GetResourceString("arrSecondary")!;
        /// <summary>Primary</summary>
        public static string @arrPrimary => GetResourceString("arrPrimary")!;
        /// <summary>Disconnected</summary>
        public static string @arcsDisconnected => GetResourceString("arcsDisconnected")!;
        /// <summary>Connected</summary>
        public static string @arcsConnected => GetResourceString("arcsConnected")!;
        /// <summary>Pending communication</summary>
        public static string @hmsPendingCommunication => GetResourceString("hmsPendingCommunication")!;
        /// <summary>Running</summary>
        public static string @hmsRunning => GetResourceString("hmsRunning")!;
        /// <summary>Failed</summary>
        public static string @hmsFailed => GetResourceString("hmsFailed")!;
        /// <summary>Node Majority</summary>
        public static string @cqtNodeMajority => GetResourceString("cqtNodeMajority")!;
        /// <summary>Node and Disk Majority</summary>
        public static string @cqtNodeAndDiskMajority => GetResourceString("cqtNodeAndDiskMajority")!;
        /// <summary>Node and Fileshare Majority</summary>
        public static string @cqtNodeAndFileshareMajority => GetResourceString("cqtNodeAndFileshareMajority")!;
        /// <summary>Disk Only</summary>
        public static string @cqtDiskOnly => GetResourceString("cqtDiskOnly")!;
        /// <summary>Not Applicable</summary>
        public static string @cqtNotApplicable => GetResourceString("cqtNotApplicable")!;
        /// <summary>Cloud Witness</summary>
        public static string @cqtCloudWitness => GetResourceString("cqtCloudWitness")!;
        /// <summary>Unknown Quorum State</summary>
        public static string @cqsUnknownQuorumState => GetResourceString("cqsUnknownQuorumState")!;
        /// <summary>Normal Quorum</summary>
        public static string @cqsNormalQuorum => GetResourceString("cqsNormalQuorum")!;
        /// <summary>Forced Quorum</summary>
        public static string @cqsForcedQuorum => GetResourceString("cqsForcedQuorum")!;
        /// <summary>Not Applicable</summary>
        public static string @cqsNotApplicable => GetResourceString("cqsNotApplicable")!;
        /// <summary>Node</summary>
        public static string @cmtNode => GetResourceString("cmtNode")!;
        /// <summary>Disk Witness</summary>
        public static string @cmtDiskWitness => GetResourceString("cmtDiskWitness")!;
        /// <summary>Fileshare Witness</summary>
        public static string @cmtFileshareWitness => GetResourceString("cmtFileshareWitness")!;
        /// <summary>Cloud Witness</summary>
        public static string @cmtCloudWitness => GetResourceString("cmtCloudWitness")!;
        /// <summary>Offline</summary>
        public static string @cmsOffline => GetResourceString("cmsOffline")!;
        /// <summary>Partially Online</summary>
        public static string @cmsPartiallyOnline => GetResourceString("cmsPartiallyOnline")!;
        /// <summary>Online</summary>
        public static string @cmsOnline => GetResourceString("cmsOnline")!;
        /// <summary>Unknown</summary>
        public static string @cmsUnknown => GetResourceString("cmsUnknown")!;
        /// <summary>Disallow connections</summary>
        public static string @replicaReadModeNoConnections => GetResourceString("replicaReadModeNoConnections")!;
        /// <summary>Allow only read-intent connections</summary>
        public static string @replicaReadModeReadIntentConnectionsOnly => GetResourceString("replicaReadModeReadIntentConnectionsOnly")!;
        /// <summary>Allow all connections</summary>
        public static string @replicaReadModeAllConnections => GetResourceString("replicaReadModeAllConnections")!;
        /// <summary>Allow read/write connections</summary>
        public static string @cmprReadWriteConnections => GetResourceString("cmprReadWriteConnections")!;
        /// <summary>Allow all connections</summary>
        public static string @cmprAllConnections => GetResourceString("cmprAllConnections")!;
        /// <summary>No</summary>
        public static string @cmsrNoConnections => GetResourceString("cmsrNoConnections")!;
        /// <summary>Read-intent only</summary>
        public static string @cmsrReadIntentConnectionsOnly => GetResourceString("cmsrReadIntentConnectionsOnly")!;
        /// <summary>Yes</summary>
        public static string @cmsrAllConnections => GetResourceString("cmsrAllConnections")!;
        /// <summary>Automatic</summary>
        public static string @seedingModeAutomatic => GetResourceString("seedingModeAutomatic")!;
        /// <summary>Manual</summary>
        public static string @seedingModeManual => GetResourceString("seedingModeManual")!;
        /// <summary>Synchronous commit</summary>
        public static string @aramSynchronousCommit => GetResourceString("aramSynchronousCommit")!;
        /// <summary>Asynchronous commit</summary>
        public static string @aramAsynchronousCommit => GetResourceString("aramAsynchronousCommit")!;
        /// <summary>Configuration only</summary>
        public static string @aramConfigurationOnly => GetResourceString("aramConfigurationOnly")!;
        /// <summary>Automatic</summary>
        public static string @arfmAutomatic => GetResourceString("arfmAutomatic")!;
        /// <summary>Manual</summary>
        public static string @arfmManual => GetResourceString("arfmManual")!;
        /// <summary>External</summary>
        public static string @arfmExternal => GetResourceString("arfmExternal")!;
        /// <summary>Not Joined</summary>
        public static string @arjsNotJoined => GetResourceString("arjsNotJoined")!;
        /// <summary>Joined Standalone Instance</summary>
        public static string @arjsJoinedStandaloneInstance => GetResourceString("arjsJoinedStandaloneInstance")!;
        /// <summary>Joined SQL Server Failover Cluster Instance</summary>
        public static string @arjsJoinedFailoverClusterInstance => GetResourceString("arjsJoinedFailoverClusterInstance")!;
        /// <summary>Not Synchronizing</summary>
        public static string @adssNotSynchronizing => GetResourceString("adssNotSynchronizing")!;
        /// <summary>Synchronizing</summary>
        public static string @adssSynchronizing => GetResourceString("adssSynchronizing")!;
        /// <summary>Synchronized</summary>
        public static string @adssSynchronized => GetResourceString("adssSynchronized")!;
        /// <summary>Reverting</summary>
        public static string @adssReverting => GetResourceString("adssReverting")!;
        /// <summary>Initializing</summary>
        public static string @adssInitializing => GetResourceString("adssInitializing")!;
        /// <summary>Suspend From User</summary>
        public static string @drsrSuspendFromUser => GetResourceString("drsrSuspendFromUser")!;
        /// <summary>Suspend From Partner</summary>
        public static string @drsrSuspendFromPartner => GetResourceString("drsrSuspendFromPartner")!;
        /// <summary>Suspend From Redo</summary>
        public static string @drsrSuspendFromRedo => GetResourceString("drsrSuspendFromRedo")!;
        /// <summary>Suspend From Apply</summary>
        public static string @drsrSuspendFromApply => GetResourceString("drsrSuspendFromApply")!;
        /// <summary>Suspend From Capture</summary>
        public static string @drsrSuspendFromCapture => GetResourceString("drsrSuspendFromCapture")!;
        /// <summary>Suspend From Restart</summary>
        public static string @drsrSuspendFromRestart => GetResourceString("drsrSuspendFromRestart")!;
        /// <summary>Suspend From Undo</summary>
        public static string @drsrSuspendFromUndo => GetResourceString("drsrSuspendFromUndo")!;
        /// <summary>Not Applicable</summary>
        public static string @drsrNotApplicable => GetResourceString("drsrNotApplicable")!;
        /// <summary>Primary</summary>
        public static string @agabpPrimary => GetResourceString("agabpPrimary")!;
        /// <summary>Secondary Only</summary>
        public static string @agabpSecondaryOnly => GetResourceString("agabpSecondaryOnly")!;
        /// <summary>Secondary</summary>
        public static string @agabpSecondary => GetResourceString("agabpSecondary")!;
        /// <summary>None</summary>
        public static string @agabpNone => GetResourceString("agabpNone")!;
        /// <summary>On Server Down</summary>
        public static string @agfcOnServerDown => GetResourceString("agfcOnServerDown")!;
        /// <summary>On Server Unresponsive</summary>
        public static string @agfcOnServerUnresponsive => GetResourceString("agfcOnServerUnresponsive")!;
        /// <summary>On Critical Server Errors</summary>
        public static string @agfcOnCriticalServerErrors => GetResourceString("agfcOnCriticalServerErrors")!;
        /// <summary>On Moderate Server Errors</summary>
        public static string @agfcOnModerateServerErrors => GetResourceString("agfcOnModerateServerErrors")!;
        /// <summary>On Any Qualified Failure Condition</summary>
        public static string @agfcOnAnyQualifiedFailureCondition => GetResourceString("agfcOnAnyQualifiedFailureCondition")!;
        /// <summary>Offline</summary>
        public static string @aglipOffline => GetResourceString("aglipOffline")!;
        /// <summary>Online</summary>
        public static string @aglipOnline => GetResourceString("aglipOnline")!;
        /// <summary>Online Pending</summary>
        public static string @aglipOnlinePending => GetResourceString("aglipOnlinePending")!;
        /// <summary>Failure</summary>
        public static string @agliFailure => GetResourceString("agliFailure")!;
        /// <summary>Unknown</summary>
        public static string @agliUnknown => GetResourceString("agliUnknown")!;
        /// <summary>EXTERNAL</summary>
        public static string @agctExternal => GetResourceString("agctExternal")!;
        /// <summary>NONE</summary>
        public static string @agctNone => GetResourceString("agctNone")!;
        /// <summary>Windows Server Failover Cluster</summary>
        public static string @agctWsfc => GetResourceString("agctWsfc")!;
        /// <summary>A filegroup used for row data</summary>
        public static string @fgtRowsFileGroup => GetResourceString("fgtRowsFileGroup")!;
        /// <summary>A filegroup used for filestream data</summary>
        public static string @fgtFileStreamDataFileGroup => GetResourceString("fgtFileStreamDataFileGroup")!;
        /// <summary>A filegroup used for memory optimized data</summary>
        public static string @fgtMemoryOptimizedDataFileGroup => GetResourceString("fgtMemoryOptimizedDataFileGroup")!;
        /// <summary>Filter</summary>
        public static string @securityPredicateTypeFilter => GetResourceString("securityPredicateTypeFilter")!;
        /// <summary>Block</summary>
        public static string @securityPredicateTypeBlock => GetResourceString("securityPredicateTypeBlock")!;
        /// <summary>All</summary>
        public static string @securityPredicateOperationAll => GetResourceString("securityPredicateOperationAll")!;
        /// <summary>After Insert</summary>
        public static string @securityPredicateOperationAfterInsert => GetResourceString("securityPredicateOperationAfterInsert")!;
        /// <summary>After Update</summary>
        public static string @securityPredicateOperationAfterUpdate => GetResourceString("securityPredicateOperationAfterUpdate")!;
        /// <summary>Before Update</summary>
        public static string @securityPredicateOperationBeforeUpdate => GetResourceString("securityPredicateOperationBeforeUpdate")!;
        /// <summary>Before Delete</summary>
        public static string @securityPredicateOperationBeforeDelete => GetResourceString("securityPredicateOperationBeforeDelete")!;
        /// <summary>Clustered</summary>
        public static string @Clustered => GetResourceString("Clustered")!;
        /// <summary>Non-Clustered</summary>
        public static string @NonClustered => GetResourceString("NonClustered")!;
        /// <summary>Primary XML</summary>
        public static string @PrimaryXml => GetResourceString("PrimaryXml")!;
        /// <summary>Secondary XML</summary>
        public static string @SecondaryXml => GetResourceString("SecondaryXml")!;
        /// <summary>Spatial</summary>
        public static string @Spatial => GetResourceString("Spatial")!;
        /// <summary>Non-Clustered Columnstore</summary>
        public static string @NonClusteredColumnStore => GetResourceString("NonClusteredColumnStore")!;
        /// <summary>Non-Clustered Hash</summary>
        public static string @NonClusteredHash => GetResourceString("NonClusteredHash")!;
        /// <summary>Selective XML</summary>
        public static string @SelectiveXml => GetResourceString("SelectiveXml")!;
        /// <summary>Secondary Selective XML</summary>
        public static string @SecondarySelectiveXml => GetResourceString("SecondarySelectiveXml")!;
        /// <summary>Clustered Columnstore</summary>
        public static string @ClusteredColumnStore => GetResourceString("ClusteredColumnStore")!;
        /// <summary>Heap</summary>
        public static string @Heap => GetResourceString("Heap")!;
        /// <summary>Vector</summary>
        public static string @Vector => GetResourceString("Vector")!;
        /// <summary>JSON</summary>
        public static string @Json => GetResourceString("Json")!;
        /// <summary>Transact-SQL script (T-SQL)</summary>
        public static string @TransactSql => GetResourceString("TransactSql")!;
        /// <summary>ActiveX Script</summary>
        public static string @ActiveScripting => GetResourceString("ActiveScripting")!;
        /// <summary>Operating system (CmdExec)</summary>
        public static string @CmdExec => GetResourceString("CmdExec")!;
        /// <summary>SQL Server Analysis Services Command</summary>
        public static string @AnalysisCommand => GetResourceString("AnalysisCommand")!;
        /// <summary>SQL Server Analysis Services Query</summary>
        public static string @AnalysisQuery => GetResourceString("AnalysisQuery")!;
        /// <summary>Replication Distributor</summary>
        public static string @ReplDistribution => GetResourceString("ReplDistribution")!;
        /// <summary>Replication Merge</summary>
        public static string @ReplMerge => GetResourceString("ReplMerge")!;
        /// <summary>Replication Queue Reader</summary>
        public static string @ReplQueueReader => GetResourceString("ReplQueueReader")!;
        /// <summary>Replication Snapshot</summary>
        public static string @ReplSnapshot => GetResourceString("ReplSnapshot")!;
        /// <summary>Replication Transaction-Log Reader</summary>
        public static string @ReplLogReader => GetResourceString("ReplLogReader")!;
        /// <summary>SQL Server Integration Services Package</summary>
        public static string @SSIS => GetResourceString("SSIS")!;
        /// <summary>PowerShell</summary>
        public static string @PowerShell => GetResourceString("PowerShell")!;
        /// <summary>Database Default</summary>
        public static string @dbCatalogCollationDatabaseDefault => GetResourceString("dbCatalogCollationDatabaseDefault")!;
        /// <summary>Latin1_General_100_CI_AS_KS_WS_SC</summary>
        public static string @dbCatalogCollationContained => GetResourceString("dbCatalogCollationContained")!;
        /// <summary>SQL_Latin1_General_CP1_CI_AS</summary>
        public static string @dbCatalogCollationSQL_Latin1_General_CP1_CI_AS => GetResourceString("dbCatalogCollationSQL_Latin1_General_CP1_CI_AS")!;
        /// <summary>Unknown</summary>
        public static string @UnknownDest => GetResourceString("UnknownDest")!;
        /// <summary>File</summary>
        public static string @FileDest => GetResourceString("FileDest")!;
        /// <summary>Security Log</summary>
        public static string @SecurityLogDest => GetResourceString("SecurityLogDest")!;
        /// <summary>Application Log</summary>
        public static string @ApplicationLogDest => GetResourceString("ApplicationLogDest")!;
        /// <summary>URL</summary>
        public static string @UrlDest => GetResourceString("UrlDest")!;
        /// <summary>External Monitor</summary>
        public static string @ExternalMonitorDest => GetResourceString("ExternalMonitorDest")!;
        /// <summary>Continue</summary>
        public static string @OnFailureActionContinue => GetResourceString("OnFailureActionContinue")!;
        /// <summary>Shutdown SQL Server instance</summary>
        public static string @OnFailureActionShutdown => GetResourceString("OnFailureActionShutdown")!;
        /// <summary>Fail operation</summary>
        public static string @OnFailureActionFail => GetResourceString("OnFailureActionFail")!;
        /// <summary>Off</summary>
        public static string @Off => GetResourceString("Off")!;
        /// <summary>On</summary>
        public static string @On => GetResourceString("On")!;
        /// <summary>Read only</summary>
        public static string @ReadOnly => GetResourceString("ReadOnly")!;
        /// <summary>Read write</summary>
        public static string @ReadWrite => GetResourceString("ReadWrite")!;
        /// <summary>Error</summary>
        public static string @Error => GetResourceString("Error")!;
        /// <summary>All</summary>
        public static string @All => GetResourceString("All")!;
        /// <summary>Auto</summary>
        public static string @Auto => GetResourceString("Auto")!;
        /// <summary>None</summary>
        public static string @None => GetResourceString("None")!;
        /// <summary>Custom</summary>
        public static string @Custom => GetResourceString("Custom")!;
        /// <summary>The database name must be specified in the urn: {urn}</summary>
        public static string @DatabaseNameMustBeSpecifiedinTheUrn => GetResourceString("DatabaseNameMustBeSpecifiedinTheUrn")!;
        /// <summary>The database name must be specified in the urn: {urn}</summary>
        internal static string FormatDatabaseNameMustBeSpecifiedinTheUrn(object? urn)
           => string.Format(Culture, GetResourceString("DatabaseNameMustBeSpecifiedinTheUrn", new[] { "urn" }), urn);


    }
}
