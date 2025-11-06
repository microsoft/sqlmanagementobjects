//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    ///
    /// </summary>
    [TestClass]
    public class SqlSmoObjectTests : UnitTestBase
    {
        /// <summary>
        /// List of types that we expect not to have a PropertyMetadataProvider defined
        /// </summary>
        private readonly Type[] expectedTypesWithoutPropertyMetadataProvider =
        {
            typeof(SqlSmoObject),
            typeof(NamedSmoObject),
            typeof(AgentObjectBase),
            typeof(CategoryBase),
            typeof(ScriptNameObjectBase),
            typeof(BrokerObjectBase),
            typeof(AuditSpecification),
            typeof(DdlTriggerBase),
            typeof(EndpointPayload),
            typeof(DatabaseFile),
            typeof(ScriptSchemaObjectBase),
            typeof(DefaultRuleBase),
            typeof(EndpointProtocol),
            typeof(ScheduleBase),
            typeof(ParameterBase),
            typeof(Parameter),
            typeof(SoapMethodObject),
            typeof(MessageObjectBase),
            typeof(TableViewTableTypeBase),
            typeof(TableViewBase)
        };

        /// <summary>
        /// This is a list of the properties for each SMO type that we expect to not have a value for some versions. This is not a normal case - most Boolean properties
        /// should have a value for all server versions (an appropriate default should be given for versions that property isn't directly applicable for).
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> expectedBooleanPropertiesWithoutValuesForSomeVersions = new Dictionary<string, HashSet<string>>()
        {
            {
                "Audit",
                new HashSet<string>()
                {
                    nameof(Audit.IsOperator)
                }
            },
            {
                "AvailabilityGroup",
                new HashSet<string>()
                {
                    "BasicAvailabilityGroup",
                    "DatabaseHealthTrigger",
                    "DtcSupportEnabled",
                    nameof(AvailabilityGroup.IsContained), // New in SQL 2022; decided not to expose it for downlevel versions of SQL.
                    "IsDistributedAvailabilityGroup",
                }
            },
            {
                "Check",
                new HashSet<string>()
                {
                    "IsFileTableDefined",
                }
            },
            {
                "Column",
                new HashSet<string>()
                {
                    "IsColumnSet",
                    "IsDeterministic",
                    "IsDistributedColumn",
                    "IsFileStream",
                    "IsFullTextIndexed",
                    "IsHidden",
                    "IsMasked",
                    "IsPersisted",
                    "IsPrecise",
                    "IsSparse",
                    "IsClassified"
                }
            },
            {
                "ColumnMasterKey",
                new HashSet<string>()
                {
                    "AllowEnclaveComputations"
                }
            },
            {
                "Database",
                new HashSet<string>()
                {  
                    // ADR is read/write but only settable on 2019 on premises
                    nameof(Database.AcceleratedRecoveryEnabled),
                    "AnsiNullDefault",
                    "AnsiNullsEnabled",
                    "AnsiPaddingEnabled",
                    "AnsiWarningsEnabled",
                    "ArithmeticAbortEnabled",
                    "AutoClose",
                    "AutoCreateIncrementalStatisticsEnabled",
                    "AutoCreateStatisticsEnabled",
                    "AutoUpdateStatisticsAsync",
                    "AutoUpdateStatisticsEnabled",
                    "BrokerEnabled",
                    "CaseSensitive",
                    "ChangeTrackingAutoCleanUp",
                    "ChangeTrackingEnabled",
                    "CloseCursorsOnCommitEnabled",
                    "ConcatenateNullYieldsNull",
                    "DatabaseOwnershipChaining",
                    "DateCorrelationOptimization",
                    nameof(Database.DataRetentionEnabled),
                    "EncryptionEnabled",
                    "HasDatabaseEncryptionKey",
                    "HasFileInCloud",
                    "HasFullBackup",
                    "HasMemoryOptimizedObjects",
                    "HonorBrokerPriority",
                    "IsAccessible",
                    "IsDatabaseSnapshot",
                    "IsDatabaseSnapshotBase",
                    "IsDbManager",
                    nameof(Database.IsLedger),
                    "IsLoginManager",
                    "IsMailHost",
                    "IsManagementDataWarehouse",
                    // There's no reason to expose below property for non-azure. 
                    // On premises database sizes are managed by filegroup and datafile classes.
                    // MaxSizeInBytes itself isn't supported on non-Azure.
                    "IsMaxSizeApplicable",
                    "IsMirroringEnabled",
                    "IsParameterizationForced",
                    "IsReadCommittedSnapshotOn",
                    "IsSqlDwEdition",
                    "IsUpdateable",
                    "IsVarDecimalStorageFormatEnabled",
                    "LocalCursorsDefault",
                    "NestedTriggersEnabled",
                    "NumericRoundAbortEnabled",
                    nameof(Database.OptimizedLockingOn),
                    "QuotedIdentifiersEnabled",
                    "ReadOnly",
                    "RecursiveTriggersEnabled",
                    "RemoteDataArchiveEnabled",
                    "RemoteDataArchiveUseFederatedServiceAccount",
                    "TemporalHistoryRetentionEnabled",
                    "TransformNoiseWords",
                    "Trustworthy",
                }
            },
            {
                "DatabaseOptions",
                new HashSet<string>()
                {
                    "AnsiPaddingEnabled",
                    "ArithmeticAbortEnabled",
                    "AutoCreateStatisticsIncremental",
                    "AutoUpdateStatisticsAsync",
                    "BrokerEnabled",
                    "ConcatenateNullYieldsNull",
                    "DatabaseOwnershipChaining",
                    nameof(DatabaseOptions.DataRetentionEnabled),
                    "DateCorrelationOptimization",
                    "IsParameterizationForced",
                    "NumericRoundAbortEnabled",
                    "ReadOnly",
                    "Trustworthy",
                }
            },
            {
                "DatabaseReplicaState",
                new HashSet<string>()
                {
                    "IsFailoverReady",
                    "IsJoined",
                    "IsLocal",
                    "IsSuspended",
                }
            },
            {
              "DatabaseScopedConfiguration",
              new HashSet<string>
              {
                  "IsValueDefault" // the lack of support for this property is meaningful in scripting
              }
            },
            {
                "DataFile",
                new HashSet<string>()
                {
                    "IsOffline",
                    "IsReadOnly",
                    "IsReadOnlyMedia",
                    "IsSparse",
                }
            },
            {
                "DefaultConstraint",
                new HashSet<string>()
                {
                    "IsFileTableDefined",
                }
            },
            {
                "ExtendedStoredProcedure",
                new HashSet<string>()
                {
                    "IsSchemaOwned",
                }
            },
            {
                "FileGroup",
                new HashSet<string>()
                {
                    "IsFileStream",
                    "AutogrowAllFiles",
                }
            },
            {
                "ForeignKey",
                new HashSet<string>()
                {
                    "IsFileTableDefined",
                    "IsMemoryOptimized",
                }
            },
            {
                "FullTextService",
                new HashSet<string>()
                {
                    "AllowUnsignedBinaries",
                    "LoadOSResourcesEnabled",
                }
            },
            {
                "HttpProtocol",
                new HashSet<string>()
                {
                    "IsCompressionEnabled",
                    "IsSystemObject",
                }
            },
            {
                "Index",
                new HashSet<string>()
                {
                    "HasCompressedPartitions",
                    "HasFilter",
                    "HasSparseColumn",
                    nameof(Microsoft.SqlServer.Management.Smo.Index.HasXmlCompressedPartitions),
                    "IgnoreDuplicateKeys",
                    "IsClustered",
                    "IsDisabled",
                    "IsFileTableDefined",
                    "IsFullTextKey",
                    "IsMemoryOptimized",
                    "IsOptimizedForSequentialKey",
                    "IsPartitioned",
                    "IsSpatialIndex",
                    "IsSystemNamed",
                    "IsSystemObject",
                    "IsUnique",
                    "IsXmlIndex",
                    "NoAutomaticRecomputation",
                    "PadIndex",
                }
            },
            {
                "IndexedColumn",
                new HashSet<string>()
                {
                    "Descending",
                    "IsComputed",
                    "IsIncluded",
                }
            },
            {
                "Information",
                new HashSet<string>()
                {
                    "IsClustered",
                    "IsFullTextInstalled",
                    "IsHadrEnabled",
                    "IsPolyBaseInstalled",
                    "IsSingleUser",
                    "IsXTPSupported",
                }
            },
            {
                "JobServer",
                new HashSet<string>()
                {
                    "ReplaceAlertTokensEnabled",
                    "SaveInSentFolder",
                    "SqlAgentAutoStart",
                    "SqlAgentRestart",
                    "SqlServerRestart",
                    "SysAdminOnly",
                    "WriteOemErrorLog",
                }
            },
            {
                "LinkedServer",
                new HashSet<string>()
                {
                    "IsPromotionofDistributedTransactionsForRPCEnabled",
                    "LazySchemaValidation",
                    "Publisher",
                    "Rpc",
                    "RpcOut",
                    "Subscriber",
                    "UseRemoteCollation",
                }
            },
            {
                "LogFile",
                new HashSet<string>()
                {
                    "IsOffline",
                    "IsReadOnly",
                    "IsReadOnlyMedia",
                    "IsSparse",
                }
            },
            {
                "Login",
                new HashSet<string>()
                {
                    "DenyWindowsLogin",
                    "HasAccess",
                    "IsDisabled",
                    "IsLocked",
                    "IsPasswordExpired",
                    "IsSystemObject",
                    "MustChangePassword",
                    "PasswordExpirationEnabled",
                    "PasswordPolicyEnforced",
                }
            },
            {
                "MasterKey",
                new HashSet<string>()
                {
                    "IsEncryptedByServer",
                    "IsOpen",
                }
            },
            {
                "ProxyAccount",
                new HashSet<string>()
                {
                    "IsEnabled",
                }
            },
            {
                "Server",
                new HashSet<string>()
                {
                    "HasNullSaPassword",
                    "IsCaseSensitive",
                    "IsClustered",
                    "IsContainedAuthentication",
                    "IsFullTextInstalled",
                    "IsHadrEnabled",
                    "IsPolyBaseInstalled",
                    "IsSingleUser",
                    "IsXTPSupported",
                    "NamedPipesEnabled",
                    "TcpEnabled",
                }
            },
            {
                "ServerProxyAccount",
                new HashSet<string>()
                {
                    "IsEnabled",
                }
            },
            {
                "ServerRole",
                new HashSet<string>()
                {
                    "IsFixedRole",
                }
            },
            {
                "ServiceBrokerPayload",
                new HashSet<string>()
                {
                    "IsMessageForwardingEnabled",
                    "IsSystemObject",
                }
            },
            {
                "ServiceQueue",
                new HashSet<string>()
                {
                    "IsPoisonMessageHandlingEnabled",
                    "IsRetentionEnabled",
                    "IsSystemObject",
                }
            },
            {
                "SmartAdmin",
                new HashSet<string>()
                {
                    "BackupEnabled",
                    "MasterSwitch",
                }
            },
            {
                "SoapPayload",
                new HashSet<string>()
                {
                    "IsSessionEnabled",
                    "IsSqlBatchesEnabled",
                    "IsSystemObject",
                    "SessionNeverTimesOut",
                }
            },
            {
                "SoapPayloadMethod",
                new HashSet<string>()
                {
                    "IsSystemObject",
                }
            },
            {
                "Statistic",
                new HashSet<string>()
                {
                    "HasFilter",
                    "IsAutoCreated",
                    nameof(Statistic.IsAutoDropped),
                    "IsFromIndexCreation",
                    "IsTemporary",
                    "NoAutomaticRecomputation",
                }
            },
            {
                "StoredProcedure",
                new HashSet<string>()
                {
                    "IsNativelyCompiled",
                    "IsSchemaBound",
                    "IsSchemaOwned",
                    "IsSystemObject",
                    "QuotedIdentifierStatus",
                    "Recompile",
                    "Startup",
                }
            },
            {
                "StoredProcedureParameter",
                new HashSet<string>()
                {
                    "HasDefaultValue",
                    "IsCursorParameter",
                    "IsOutputParameter",
                    "IsReadOnly",
                }
            },
            {
                "Table",
                new HashSet<string>()
                {
                    "AnsiNullsStatus",
                    "ChangeTrackingEnabled",
                    nameof(Table.DataRetentionEnabled),
                    "FakeSystemTable",
                    "FileTableNamespaceEnabled",
                    "HasAfterTrigger",
                    "HasClassifiedColumn",
                    "HasClusteredColumnStoreIndex",
                    "HasClusteredIndex",
                    "HasCompressedPartitions",
                    "HasDeleteTrigger",
                    "HasHeapIndex",
                    "HasIndex",
                    "HasInsertTrigger",
                    "HasInsteadOfTrigger",
                    "HasNonClusteredColumnStoreIndex",
                    "HasNonClusteredIndex",
                    "HasPrimaryClusteredIndex",
                    "HasSparseColumn",
                    "HasSpatialData",
                    "HasSystemTimePeriod",
                    "HasUpdateTrigger",
                    nameof(Table.HasXmlCompressedPartitions),
                    "HasXmlData",
                    "HasXmlIndex",
                    "IsEdge",
                    "IsExternal",
                    "IsFileTable",
                    "IsIndexable",
                    "IsMemoryOptimized",
                    "IsNode",
                    "IsPartitioned",
                    "IsSchemaOwned",
                    "IsSystemObject",
                    "IsSystemVersioned",
                    "IsVarDecimalStorageFormatEnabled",
                    "QuotedIdentifierStatus",
                    "RemoteDataArchiveEnabled",
                    "RemoteTableProvisioned",
                    "Replicated",
                    "TrackColumnsUpdatedEnabled",
                }
            },
            {
                "TcpProtocol",
                new HashSet<string>()
                {
                    "IsDynamicPort",
                    "IsSystemObject",
                }
            },
            {
                "Trigger",
                new HashSet<string>()
                {
                    "IsNativelyCompiled",
                    "IsSchemaBound",
                    "IsSystemObject",
                    "NotForReplication",
                    "QuotedIdentifierStatus",
                    "Update",
                }
            },
            {
                "UserDefinedAggregateParameter",
                new HashSet<string>()
                {
                    "IsReadOnly",
                }
            },
            {
                "UserDefinedDataType",
                new HashSet<string>()
                {
                    "IsSchemaOwned",
                    "Nullable",
                    "VariableLength",
                }
            },
            {
                "UserDefinedFunction",
                new HashSet<string>()
                {
                    "AnsiNullsStatus",
                    "IsDeterministic",
                    "IsEncrypted",
                    "IsNativelyCompiled",
                    "IsSchemaBound",
                    "IsSchemaOwned",
                    "IsSystemObject",
                    "QuotedIdentifierStatus",
                    "ReturnsNullOnNullInput",
                    "InlineType",
                }
            },
            {
                "UserDefinedFunctionParameter",
                new HashSet<string>()
                {
                    "HasDefaultValue",
                    "IsReadOnly",
                }
            },
            {
                "UserDefinedTableType",
                new HashSet<string>()
                {
                    "IsMemoryOptimized",
                    "IsSchemaOwned",
                    "IsUserDefined",
                    "Nullable",
                }
            },
            {
                "UserOptions",
                new HashSet<string>()
                {
                    "AbortOnArithmeticErrors",
                    "AbortTransactionOnError",
                    "AnsiNullDefaultOff",
                    "AnsiNullDefaultOn",
                    "AnsiNulls",
                    "AnsiPadding",
                    "AnsiWarnings",
                    "ConcatenateNullYieldsNull",
                    "CursorCloseOnCommit",
                    "DisableDefaultConstraintCheck",
                    "IgnoreArithmeticErrors",
                    "ImplicitTransactions",
                    "NoCount",
                    "NumericRoundAbort",
                    "QuotedIdentifier",
                }
            },
            {
                "View",
                new HashSet<string>()
                {
                    "HasAfterTrigger",
                    "HasClusteredIndex",
                    "HasColumnSpecification",
                    "HasDeleteTrigger",
                    "HasIndex",
                    "HasInsertTrigger",
                    "HasInsteadOfTrigger",
                    "HasNonClusteredIndex",
                    "HasPrimaryClusteredIndex",
                    "HasUpdateTrigger",
                    "IsEncrypted",
                    "IsIndexable",
                    "IsSchemaBound",
                    "IsSchemaOwned",
                    "IsSystemObject",
                    "QuotedIdentifierStatus",
                    "ReturnsViewMetadata",
                }
            },
            {
                "WorkloadGroup",
                new HashSet<string>()
                {
                    "IsSystemObject",
                }
            },
            {
                nameof(WorkloadManagementWorkloadClassifier),
                new HashSet<string>()
                {
                   "IsSystemObject",
                }
            },
            {
                nameof(WorkloadManagementWorkloadGroup),
                new HashSet<string>()
                {
                    nameof(WorkloadManagementWorkloadGroup.IsSystemObject),
                    nameof(WorkloadManagementWorkloadGroup.HasClassifier),
                }
            },
        };

        /// <summary>
        /// Check that all SMO Boolean properties have a value for all server versions (combination of version, engine type and engine edition). If they don't and
        /// that's expected they can be added to the filter list 'expectedBooleanPropertiesWithoutValuesForSomeVersions' above, but that's a special case and should
        /// not normally be done.
        ///
        /// For properties which are for features that are unsupported on certain versions an appropriate default value should be given (such as 'false' for an
        /// IsEnabled type property for a feature)
        /// </summary>
        [TestCategory("Unit")]
        [TestMethod]
        public void AllSmoBooleanProperties_HaveValueForAllServerVersions()
        {
            StringBuilder propertiesWithoutValues = new StringBuilder();

            foreach (var type in typeof(Management.Smo.Server).Assembly.GetTypes().Where(t => typeof(SqlSmoObject).IsAssignableFrom(t)))
            {
                //Most types should have a PropertyMetadataProvider defined - there's a few we expect that don't so filter those out.
                Type propertyMetadataProviderType = type.GetNestedType("PropertyMetadataProvider", BindingFlags.NonPublic);

                if (propertyMetadataProviderType == null)
                {
                    if (!expectedTypesWithoutPropertyMetadataProvider.Contains(type))
                    {
                        throw new InternalTestFailureException(string.Format("Type {0} did not have a PropertyMetadataProvider defined", type.Name));
                    }

                    continue;
                }

                //CheckPropertyValid method is used to validate that the property is accessible for a given version, engine type and engine edition
                MethodInfo miCheckPropertyValid = propertyMetadataProviderType.GetMethod("CheckPropertyValid", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);

                if (miCheckPropertyValid == null)
                {
                    throw new InternalTestFailureException(string.Format("Type {0}.PropertyMetadataProvider did not have the CheckPropertyValid method defined", type.Name));
                }

                //We build up a list of all the properties from the static metadata array. We do have to combine all the arrays though since it's possible a property might only
                //be defined for one type/edition combo.
                MethodInfo miStaticMetadataArray = propertyMetadataProviderType.GetMethod("GetStaticMetadataArray",
                 BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);

                var booleanProperties = Enum.GetValues(typeof(DatabaseEngineType))
                                .Cast<DatabaseEngineType>()
                                .SelectMany(
                                    t =>
                                        Enum.GetValues(typeof(DatabaseEngineEdition))
                                            .Cast<DatabaseEngineEdition>()
                                            .SelectMany(e => miStaticMetadataArray.Invoke(null, new object[] { t, e }) as StaticMetadata[]))
                                            .Where(s => s.PropertyType == typeof(bool)).Select(s => s.Name).Distinct().OrderBy(s => s);
                foreach (string propName in booleanProperties)
                {
                    foreach (DatabaseEngineType engineType in Enum.GetValues(typeof(DatabaseEngineType)).Cast<DatabaseEngineType>().Where(t => t != DatabaseEngineType.Unknown))
                    {
                        foreach (DatabaseEngineEdition engineEdition in Enum.GetValues(typeof(DatabaseEngineEdition)).Cast<DatabaseEngineEdition>().Where(t => t != DatabaseEngineEdition.Unknown))
                        {
                            foreach (ServerVersion version in SmoUtility.GetSupportedVersions(engineType, engineEdition))
                            {
                                //If the whole object itself isn't supported then we shouldn't expect the properties to have any value, so just skip
                                if (!SmoUtility.IsSupportedObject(type, version, engineType, engineEdition))
                                {
                                    continue;
                                }

                                var result = (bool)miCheckPropertyValid.Invoke(null, new object[] { propertyMetadataProviderType, propName, version, engineType, engineEdition });

                                if (!result)
                                {
                                    //Property isn't valid - ignore if we have a filter set for this one though
                                    if (expectedBooleanPropertiesWithoutValuesForSomeVersions.ContainsKey(type.Name) &&
                                        expectedBooleanPropertiesWithoutValuesForSomeVersions[type.Name].Contains(propName))
                                    {
                                        continue;
                                    }

                                    propertiesWithoutValues.AppendLine(String.Format("{0}.{1} ServerVersion={2} EngineType={3} EngineEdition={4}",
                                                                                     type.Name, propName, version, engineType, engineEdition));
                                }
                            }
                        }
                    }
                }
            }

            Assert.That(propertiesWithoutValues.Length, Is.EqualTo(0),
                @"The following Boolean properties were found to not have a value defined for the listed server version/engine type/engine edition. 
Typically all Boolean values should have a value for all version combinations - if this is a special case that has been approved by the SMO committers
then add the property to the expectedBooleanPropertiesWithoutValuesForSomeVersions dictionary in the test.
{0}",
                propertiesWithoutValues);
        }

        /// <summary>
        /// These are types that we expect not to have a UrnSuffix property defined - usually because
        /// they are a form of base type that is not expected to be created itself.
        /// </summary>
        private readonly ISet<Type> urnSuffixIgnoreTypes = new HashSet<Type>()
        {
            typeof(AgentObjectBase),
            typeof(AuditSpecification),
            typeof(BrokerObjectBase),
            typeof(CategoryBase),
            typeof(DatabaseFile),
            typeof(MessageObjectBase),
            typeof(DefaultRuleBase),
            typeof(NamedSmoObject),
            typeof(ScheduleBase),
            typeof(ScriptNameObjectBase),
            typeof(ScriptSchemaObjectBase),
            typeof(SoapMethodObject),
            typeof(TableViewTableTypeBase),
            typeof(TableViewBase)
        };


        /// <summary>
        /// The types that we expect not to have an ordering defined. These are mostly types that are children
        /// of a non-Server type, since the ordering is currently primarily used for scripting root level objects
        /// such as Database (these root level objects will then usually script their children themselves)
        /// </summary>
        private readonly ISet<Type> nonUrnTypeKeyTypes = new HashSet<Type>()
        {
            typeof(Column),
            typeof(DatabaseDdlTrigger),
            typeof(DatabaseMirroringPayload),
            typeof(DatabaseOptions),
            typeof(DatabaseReplicaState),
            typeof(DatabaseRole),
            typeof(DataFile),
            typeof(Default),
            typeof(DefaultConstraint),
            typeof(EdgeConstraintClause),
            typeof(ExtendedStoredProcedure),
            typeof(ExternalLanguageFile),
            typeof(ExternalLibraryFile),
            typeof(FileGroup),
            typeof(ForeignKeyColumn),
            typeof(FullTextIndexColumn),
            typeof(HttpProtocol),
            typeof(IndexedColumn),
            typeof(IndexedXmlPath),
            typeof(IndexedXmlPathNamespace),
            typeof(Information),
            typeof(Language),
            typeof(LogFile),
            typeof(MessageTypeMapping),
            typeof(NumberedStoredProcedure),
            typeof(ParameterBase),
            typeof(Parameter),
            typeof(NumberedStoredProcedureParameter),
            typeof(OleDbProviderSettings),
            typeof(OrderColumn),
            typeof(PartitionFunctionParameter),
            typeof(PartitionSchemeParameter),
            typeof(PhysicalPartition),
            typeof(ServerDdlTrigger),
            typeof(ServerProxyAccount),
            typeof(ServerRole),
            typeof(ServiceContractMapping),
            typeof(Settings),
            typeof(SmartAdmin),
            typeof(SoapPayload),
            typeof(SoapPayloadMethod),
            typeof(SqlAssemblyFile),
            typeof(StatisticColumn),
            typeof(StoredProcedureParameter),
            typeof(SymmetricKey),
            typeof(SystemDataType),
            typeof(SystemMessage),
            typeof(TargetServer),
            typeof(TcpProtocol),
            typeof(UserDefinedAggregateParameter),
            typeof(UserDefinedFunctionParameter),
            typeof(UserOptions),
            typeof(UserPermission)
        };


        /// <summary>
        /// Verifies that all SqlSmoObject types (except the ones we purposely ignore) have :
        ///     -A UrnSuffix property
        ///     -A non-default ordering specified in UrnTypeKey
        /// The UrnSuffix is not strictly required but is something most objects should have, barring special circumstances such as "base" objects
        /// that aren't expected to be created themselves.
        ///
        /// The ordering is used for scripting and also while not strictly necessary is useful to have to ensure that when objects are scripted they
        /// are always output in the same ordering (otherwise different runs of the scripting may product different scripts). 
        /// </summary>
        [TestCategory("Unit")]
        [TestMethod]
        public void AllSmoObjectsHaveUrnSuffix_And_UrnKeyOrder()
        {
            var typesWithoutUrnSuffix = new List<string>();
            var typesWithoutOrdering = new List<string>();
            var ignoredTypesWithOrdering = new List<string>();

            foreach (var type in typeof(Management.Smo.Server).Assembly.GetTypes()
                .Where(t => !t.IsAbstract && !urnSuffixIgnoreTypes.Contains(t) && typeof(SqlSmoObject).IsAssignableFrom(t)))
            {
                var urnSuffix = SqlSmoObject.GetUrnSuffix(type);
                if (string.IsNullOrEmpty(urnSuffix))
                {
                    typesWithoutUrnSuffix.Add(type.Name);
                    continue;
                }

                // Make sure every type has an ordering defined - if not it's possibly an error
                var urnTypeKey = new UrnTypeKey(urnSuffix);

                if (urnTypeKey.CreateOrder == ObjectOrder.uninitialized ||
                   urnTypeKey.CreateOrder == ObjectOrder.@default)
                {
                    if (nonUrnTypeKeyTypes.Contains(type))
                    {
                        continue;
                    }

                    typesWithoutOrdering.Add(type.Name);
                }
                else
                {
                    // We have this type marked as ignored but it has an order - this is likely due to the type being added later
                    // and this test not being updated.
                    if (nonUrnTypeKeyTypes.Contains(type))
                    {
                        ignoredTypesWithOrdering.Add(type.Name);
                    }
                }
            }

            Assert.That(typesWithoutUrnSuffix, Is.Empty,
                "The following types do not have a UrnSuffix defined on it - all non-base SqlSmoObject types should define that property.");

            Assert.That(typesWithoutOrdering, Is.Empty,
                "The following types do not have an ordering specified in UrnTypeKey - all root SqlSmoObjects types should specify an ordering.");

            Assert.That(ignoredTypesWithOrdering, Is.Empty,
                "The following types have an ordering specified but are marked as expecting to not have one - they should likely be removed from the nonUrnTypeKeyTypes list.");
        }

        [TestCategory("Unit")]
        [TestMethod]
        public void SqlSmoObject_FormatSqlVariant_supports_all_variant_types()
        {
            var now = DateTimeOffset.UtcNow;
            var guid = Guid.NewGuid();
            var tuples = new (object variant, string text)[] {
                ((Int32)(-11), "-11"),
                ((byte)2, "0x02"),
                ((decimal)10.1, "10.1"),
                ("a 'string", "N'a ''string'"),
                ((Int16)128, "128"),
                ((Int64)999999999, "999999999"),
                ((double)100.2, "100.2"),
                ((float)200.1, "200.1"),
                (new DateTime(2000, 1,2,1,2,30), "N'2000-01-02T01:02:30.000'"),
                (now, $"N'{now.ToString(CultureInfo.InvariantCulture)}'"),
                (new SqlDateTime(), "NULL"),
                (new SqlDateTime(2001, 1,3,4,5,6), "N'2001-01-03T04:05:06.000'"),
                (new byte[]{1,0,2}, "0x010002"),
                (new SqlBinary(), "NULL"),
                (new SqlBinary(new byte[] {2,0,1}), "0x020001"),
                (true, "1"),
                (false, "0"),
                (guid, $"N'{guid.ToString()}'"),
                ((uint)101, "101")
            };
            Assert.Multiple(() =>
            {
                foreach (var tuple in tuples)
                {
                    Assert.That(SqlSmoObject.FormatSqlVariant(tuple.variant), Is.EqualTo(tuple.text), $"Incorrect SqlVariant string for type {tuple.variant.GetType()}");
                }
            });
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SqlSmoObject_metadataprovider_found_by_MetadataProviderLookup()
        {
            foreach (var objectType in typeof(SqlSmoObject).Assembly.GetTypes().Where(t => typeof(SqlSmoObject).IsAssignableFrom(t)).Except(expectedTypesWithoutPropertyMetadataProvider))
            {
                var metadataProviderType = MetadataProviderLookup.GetPropertyMetadataProviderType(objectType);
                var propertyMetadataProviderType = objectType.GetNestedType("PropertyMetadataProvider", BindingFlags.NonPublic);
                Assert.That(metadataProviderType, Is.EqualTo(propertyMetadataProviderType), $"Incorrect metadata provider from lookup for {objectType}");
            }
        }
    }

}
