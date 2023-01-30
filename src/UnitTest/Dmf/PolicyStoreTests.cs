// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Linq;
using Assert = NUnit.Framework.Assert;
using NUnit.Framework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Dmf;
using System.Diagnostics;

namespace Microsoft.SqlServer.Test.DmfUnitTests
{
    [TestClass]

    public class PolicyStoreTests : UnitTestBase
    {
        // Update this list whenever a new Facet is added to support policy based management
        const string AllFacets = "ApplicationRole,AsymmetricKey,Audit,AvailabilityDatabase,AvailabilityGroup,AvailabilityReplica,BackupDevice,BrokerPriority,BrokerService,Certificate,ColumnEncryptionKey,ColumnEncryptionKeyValue,ColumnMasterKey,Credential,CryptographicProvider,Database,DatabaseAuditSpecification,DatabaseDdlTrigger,DatabaseReplicaState,DatabaseRole,DataFile,Default,Endpoint,FileGroup,FullTextCatalog,FullTextIndex,FullTextStopList,IAvailabilityGroupState,IDatabaseMaintenanceFacet,IDatabaseOptions,IDatabasePerformanceFacet,IDatabaseSecurityFacet,ILoginOptions,IMultipartNameFacet,INameFacet,Index,IServerAuditFacet,IServerConfigurationFacet,IServerInformation,IServerPerformanceFacet,IServerSecurityFacet,IServerSelectionFacet,IServerSettings,IServerSetupFacet,ISmartAdminState,ISurfaceAreaFacet,ITableOptions,IUserOptions,IViewOptions,LinkedServer,LogFile,Login,MessageType,PartitionFunction,PartitionScheme,PlanGuide,RemoteServiceBinding,ResourceGovernor,ResourcePool,Rule,Schema,SearchPropertyList,Sequence,Server,ServerAuditSpecification,ServerDdlTrigger,ServerRole,ServiceContract,ServiceQueue,ServiceRoute,SmartAdmin,Statistic,StoredProcedure,SymmetricKey,Synonym,Table,Trigger,User,UserDefinedAggregate,UserDefinedDataType,UserDefinedFunction,UserDefinedTableType,UserDefinedType,View,WorkloadGroup,XmlSchemaCollection";
        [TestMethod]
        public void PolicyStore_EnumDomainFacets_returns_correct_set()
        {
            var allFacets = PolicyStore.EnumDomainFacets("SMO", null).Cast<FacetInfo>();
            var facetNames = string.Join(",", allFacets.Select(f => f.FacetType.Name).OrderBy(n => n).ToArray());
            Trace.TraceInformation($"EnumDomainFacets:{facetNames}");
            Assert.That(facetNames, Is.EqualTo(AllFacets), "EnumDomainFacets returns all facets");
            allFacets = PolicyStore.Facets.Cast<FacetInfo>();
            facetNames = string.Join(",", allFacets.Select(f => f.FacetType.Name).OrderBy(n => n).ToArray());
            Trace.TraceInformation($"Facets:{facetNames}");
            Assert.That(facetNames, Is.EqualTo(AllFacets), "Facets returns all facets");
        }

        [TestMethod]
        public void PolicyStore_EnumRootFacets()
        {
            var rootFacets = PolicyStore.EnumRootFacets(typeof(Management.Smo.Server));
            var facetNames = string.Join(",", rootFacets.Select(f => f.FacetType.Name).OrderBy(n => n).ToArray());
            Trace.TraceInformation($"EnumRootFacets server:{facetNames}");
            Assert.That(facetNames, Is.EqualTo("IServerInformation,IServerSettings,Server"), "EnumRootFacets Server");
            rootFacets = PolicyStore.EnumRootFacets(typeof(Management.Smo.Database));
            facetNames = string.Join(",", rootFacets.Select(f => f.FacetType.Name).OrderBy(n => n).ToArray());
            Trace.TraceInformation($"EnumRootFacets database:{facetNames}");
            Assert.That(facetNames, Is.Empty, "EnumRootFacets Database");
        }
    }
}
