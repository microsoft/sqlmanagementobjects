// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Linq;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Facets;
using System;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.DmfUnitTests
{
    [TestClass]
    public class FacetTests : UnitTestBase
    {
        static Dictionary<Type, string[]> propertyExceptions = new Dictionary<Type, string[]>
        {
            {typeof(Database), new string[] { nameof(Database.AcceleratedRecoveryEnabled),nameof(Database.ActiveConnections),nameof(Database.AvailabilityDatabaseSynchronizationState),nameof(Database.AvailabilityGroupName),nameof(Database.CaseSensitive),nameof(Database.CatalogCollation),nameof(Database.ContainmentType),nameof(Database.DatabaseGuid),nameof(Database.DataRetentionEnabled),nameof(Database.DataSpaceUsage),nameof(Database.DboLogin),nameof(Database.DefaultFullTextCatalog),nameof(Database.DefaultSchema),nameof(Database.FilestreamDirectoryName),nameof(Database.FilestreamNonTransactedAccess),nameof(Database.HasDatabaseEncryptionKey),nameof(Database.HasFileInCloud),nameof(Database.HasMemoryOptimizedObjects),nameof(Database.IndexSpaceUsage),nameof(Database.IsAccessible),nameof(Database.IsDatabaseSnapshot),nameof(Database.IsDatabaseSnapshotBase),nameof(Database.IsDbAccessAdmin),nameof(Database.IsDbBackupOperator),nameof(Database.IsDbDatareader),nameof(Database.IsDbDatawriter),nameof(Database.IsDbDdlAdmin),nameof(Database.IsDbDenyDatareader),nameof(Database.IsDbDenyDatawriter),nameof(Database.IsDbOwner),nameof(Database.IsDbSecurityAdmin),nameof(Database.IsFullTextEnabled),nameof(Database.IsMailHost),nameof(Database.IsManagementDataWarehouse),nameof(Database.IsMirroringEnabled),nameof(Database.IsSqlDw),nameof(Database.IsVarDecimalStorageFormatEnabled),nameof(Database.LastBackupDate),nameof(Database.LastDifferentialBackupDate),nameof(Database.LastGoodCheckDbTime),nameof(Database.LastLogBackupDate),nameof(Database.LegacyCardinalityEstimation),nameof(Database.LegacyCardinalityEstimationForSecondary),nameof(Database.LogReuseWaitStatus),nameof(Database.MaxDop),nameof(Database.MemoryAllocatedToMemoryOptimizedObjectsInKB),nameof(Database.MemoryUsedByMemoryOptimizedObjectsInKB),nameof(Database.MirroringFailoverLogSequenceNumber),nameof(Database.MirroringID),nameof(Database.MirroringPartner),nameof(Database.MirroringPartnerInstance),nameof(Database.MirroringRedoQueueMaxSize),nameof(Database.MirroringRoleSequence),nameof(Database.MirroringSafetyLevel),nameof(Database.MirroringSafetySequence),nameof(Database.MirroringStatus),nameof(Database.MirroringWitness),nameof(Database.MirroringWitnessStatus),nameof(Database.NestedTriggersEnabled),nameof(Database.ParameterSniffing),nameof(Database.ParameterSniffingForSecondary),nameof(Database.PersistentVersionStoreFileGroup),nameof(Database.PersistentVersionStoreSizeKB),nameof(Database.QueryOptimizerHotfixes),nameof(Database.QueryOptimizerHotfixesForSecondary),nameof(Database.RecoveryForkGuid),nameof(Database.ReplicationOptions),nameof(Database.ServiceBrokerGuid),nameof(Database.Size),nameof(Database.SnapshotIsolationState),nameof(Database.SpaceAvailable),nameof(Database.Status),nameof(Database.TransformNoiseWords),nameof(Database.TwoDigitYearCutoff),nameof(Database.UserName),nameof(Database.Version) } },
            {typeof(Login), new string[] { nameof(Login.DateLastModified),nameof(Login.DenyWindowsLogin),nameof(Login.HasAccess),nameof(Login.IsPasswordExpired),nameof(Login.PasswordHashAlgorithm),nameof(Login.Sid),nameof(Login.WindowsLoginAccessType) } },
            {typeof(Table), new string[] { nameof(Table.DataRetentionEnabled),nameof(Table.DataRetentionFilterColumnName),nameof(Table.DataRetentionPeriod),nameof(Table.DataRetentionPeriodUnit),nameof(Table.DataSourceName),nameof(Table.DataSpaceUsed),nameof(Table.DateLastModified),nameof(Table.Durability),nameof(Table.ExternalTableDistribution),nameof(Table.FileFormatName),nameof(Table.FileGroup),nameof(Table.FileStreamFileGroup),nameof(Table.FileStreamPartitionScheme),nameof(Table.FileTableDirectoryName),nameof(Table.FileTableNameColumnCollation),nameof(Table.FileTableNamespaceEnabled),nameof(Table.HasAfterTrigger),nameof(Table.HasClassifiedColumn),nameof(Table.HasClusteredColumnStoreIndex),nameof(Table.HasClusteredIndex),nameof(Table.HasCompressedPartitions),nameof(Table.HasDeleteTrigger),nameof(Table.HasHeapIndex),nameof(Table.HasIndex),nameof(Table.HasInsertTrigger),nameof(Table.HasInsteadOfTrigger),nameof(Table.HasNonClusteredColumnStoreIndex),nameof(Table.HasNonClusteredIndex),nameof(Table.HasPrimaryClusteredIndex),nameof(Table.HasSparseColumn),nameof(Table.HasSpatialData),nameof(Table.HasSystemTimePeriod),nameof(Table.HasUpdateTrigger),nameof(Table.HasXmlCompressedPartitions),nameof(Table.HasXmlData),nameof(Table.HasXmlIndex),nameof(Table.HistoryTableID),nameof(Table.HistoryTableName),nameof(Table.HistoryTableSchema),nameof(Table.IndexSpaceUsed),nameof(Table.IsEdge),nameof(Table.IsExternal),nameof(Table.IsFileTable),nameof(Table.IsIndexable),nameof(Table.IsLedger),nameof(Table.IsMemoryOptimized),nameof(Table.IsNode),nameof(Table.IsPartitioned),nameof(Table.IsSystemVersioned),nameof(Table.IsVarDecimalStorageFormatEnabled),nameof(Table.LedgerType),nameof(Table.LedgerViewName),nameof(Table.LedgerViewOperationTypeColumnName),nameof(Table.LedgerViewOperationTypeDescColumnName),nameof(Table.LedgerViewSequenceNumberColumnName),nameof(Table.LedgerViewTransactionIdColumnName),nameof(Table.Location),nameof(Table.PartitionScheme),nameof(Table.RejectSampleValue),nameof(Table.RejectType),nameof(Table.RejectValue),nameof(Table.RemoteDataArchiveFilterPredicate),nameof(Table.RemoteObjectName),nameof(Table.RemoteSchemaName),nameof(Table.RowCount),nameof(Table.ShardingColumnName),nameof(Table.SystemTimePeriodEndColumn),nameof(Table.SystemTimePeriodStartColumn),nameof(Table.TemporalType),nameof(Table.TextFileGroup) } },
            {typeof(View), new string[] { nameof(View.DateLastModified),nameof(View.HasAfterTrigger),nameof(View.HasClusteredIndex),nameof(View.HasColumnSpecification),nameof(View.HasDeleteTrigger),nameof(View.HasIndex),nameof(View.HasInsertTrigger),nameof(View.HasInsteadOfTrigger),nameof(View.HasNonClusteredIndex),nameof(View.HasPrimaryClusteredIndex),nameof(View.HasUpdateTrigger),nameof(View.IsIndexable),nameof(View.LedgerViewType) } },
            {typeof(User), new string[] { nameof(User.AuthenticationType),nameof(User.DateLastModified),nameof(User.HasDBAccess) } },
        };

/// <summary>
/// Physical facets like Table, Login, View etc implement an interface named IxxxOptions where xxx is the object name.
/// IxxxOptions implements IDmfFacet and is the only version of the facet that can be used for policy enforcement during DDL
/// operations like CREATE and ALTER. For example, you can't use a condition involving a Table facet in a policy that is 
/// evaluated during DDL processing, but you can use ITableOptions.
/// This test enumerates which public properties of each physical facet are exposed through its IxxxOptions interface and 
/// fails if a public property is found that is not in the interface and is not listed in the exception list.
/// </summary>
        [TestMethod]
        [TestCategory("Unit")]
        public void  Options_facets_have_correct_properties_of_their_implementation_facets()
        {
            Assert.Multiple(() =>
           {
               foreach (Type facet in FacetRepository.RegisteredFacets.Cast<Type>())
               {
                   var optionsInterface = facet.GetInterfaces().FirstOrDefault(t => t.Name.EndsWith("Options") && typeof(IDmfFacet).IsAssignableFrom(t));
                   if (optionsInterface == null)
                   {
                       Trace.TraceInformation($"Facet {facet} has no IxxxOptions interface");
                       continue;
                   }
                   var physicalFacetProperties = FacetRepository.GetFacetProperties(facet);
                   var optionsProperties = FacetRepository.GetFacetProperties(optionsInterface);
                   if (physicalFacetProperties.Length == optionsProperties.Length)
                   {
                       Trace.TraceInformation($"Facet {optionsInterface} has the same properties as {facet}");
                       continue;
                   }
                   var exceptions = propertyExceptions.ContainsKey(facet) ? propertyExceptions[facet] : new string[] { };
                   var missingProperties = physicalFacetProperties.Select(p => p.Name).Except(optionsProperties.Select(p => p.Name)).OrderBy(v => v).ToArray();
                   Assert.That(missingProperties, Is.EquivalentTo(exceptions) , $"Facet {facet} has new public properties not in {optionsInterface} or {optionsInterface} now has members added to it. Consider adding those properties to {optionsInterface} so they can be used in policy based management during DDL. Otherwise update {nameof(propertyExceptions)} dictionary with the following:{Environment.NewLine}"
                       + BuildExceptionMessage(facet, missingProperties));
               }
           });
        }

        private static string BuildExceptionMessage(Type facet, IEnumerable<string> exceptions)
        {
            return $"{{typeof({facet.Name}), new string[] {{ {String.Join(",", exceptions.Select(e => $"nameof({facet.Name}.{e})"))} }} }},";
        }
    }
}
