// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.GeneralFunctionality
{
    public class DependencyDiscoveryTestsBase : SqlTestBase
    {
        protected void TestDependencyScriptingForDrop(string backupFile, AzureDatabaseEdition edition = AzureDatabaseEdition.NotApplicable)
        {
            ExecuteWithDbDrop("Deps", edition, backupFile,
                database =>
                {
                    database.Parent.SetDefaultInitFields(typeof(UserDefinedFunction), true);
                    database.Parent.SetDefaultInitFields(typeof(StoredProcedure), true);
                    var tableFields = Table.GetScriptFields(typeof(Database), database.Parent.ServerVersion, database.DatabaseEngineType, database.DatabaseEngineEdition, false);
                    database.Parent.SetDefaultInitFields(typeof(Table), nameof(Table.IsSystemObject));
                    database.Parent.SetDefaultInitFields(typeof(Table), tableFields);
                    database.Parent.SetDefaultInitFields(typeof(View), nameof(View.LedgerViewType), nameof(View.IsDroppedLedgerView), nameof(View.IsSystemObject));
                    database.Parent.SetDefaultInitFields(typeof(BrokerService), nameof(BrokerService.IsSystemObject));
                    database.Parent.SetDefaultInitFields(typeof(ServiceQueue), nameof(ServiceQueue.IsSystemObject));

                    if (backupFile == null)
                    {
                        var startupObjects = GetDroppableObjects(database);
                        Assert.That(startupObjects, Is.Empty, "There should be zero droppable objects on start");
                    }
                    TestSetup.SetupDb(database, ConnectionHelpers.GetAzureKeyVaultHelper(), TargetServerFriendlyName, "AzureSterlingV12");
                    var initialObjects = GetDroppableObjects(database);
                    Assert.That(initialObjects.Count, Is.GreaterThan(10), "The database should have droppable objects");
                    var objects = GetDatabaseObjects(database).SelectMany(o => o).Where(o => !o.IsSystemObjectInternal()).ToArray();
                    var scriptMaker = new ScriptMaker(database.Parent, new ScriptingOptions(database) { ScriptDrops = true, AllowSystemObjects = false, WithDependencies = true });
                    var statements = scriptMaker.Script(objects);
                    // Some statements in the script like DISABLE TRIGGER have to come after a semicolon delimited statement
                    var script = statements.ToDelimitedSingleString(";");
                    TraceHelper.TraceInformation($"Final combined drop script:{Environment.NewLine}{script}");
                    database.ExecuteNonQuery(statements);
                    if (database.IsSupportedObject<ServiceBroker>())
                    {
                        foreach (var p in database.ServiceBroker.Priorities.Cast<BrokerPriority>().ToList())
                        {
                            p.Drop();
                        }
                        foreach (var s in database.ServiceBroker.Services.Cast<BrokerService>().Where(b => !b.IsSystemObject).ToList())
                        {
                            s.Drop();
                        }
                        foreach (var q in database.ServiceBroker.Queues.Cast<ServiceQueue>().Where(b => !b.IsSystemObject).ToList())
                        {
                            q.Drop();
                        }
                    }
                    var finalObjects = GetDroppableObjects(database);
                    var extras = "";
                    if (finalObjects.Count > 0)
                    {
                        var sb = new StringBuilder();
                        foreach (var o in finalObjects)
                        {
                            _ = sb.AppendLine(o);
                        }
                        extras = sb.ToString();
                    }
                    Assert.That(finalObjects.Count, Is.Zero, "Droppable objects exist in the database after create/drop" + Environment.NewLine + extras);
                });
        }

        private IList<string> GetDroppableObjects(Database database)
        {
            var query = database.Parent.IsSupportedProperty(typeof(Table), nameof(Table.LedgerType)) ? @"select o.name, o.type_desc 
from sys.all_objects o 
left outer join sys.tables AS t 
	ON case when o.parent_object_id <> 0 then o.parent_object_id else o.object_id end = t.object_id 
		AND (t.is_dropped_ledger_table = 1 OR t.ledger_type = 1)
left outer join sys.views AS v ON v.object_id = o.object_id AND v.is_dropped_ledger_view = 1
where o.is_ms_shipped = 0 and t.object_id is null and v.object_id is null
and cast(
case 
    when o.is_ms_shipped = 1 then 1
    when (
        select 
            major_id 
        from 
            sys.extended_properties 
        where 
            major_id = o.object_id and 
            minor_id = 0 and 
            class = 1 and 
            name = N'microsoft_database_tools_support') 
        is not null then 1
    else 0
end as bit) = 0 and o.type_desc not in ('EVENT_NOTIFICATION')
"
:
// a sproc in the on-prem setup has microsoft_database_tools_support extended property
@"select name, type_desc from sys.all_objects 
where cast(
case 
    when is_ms_shipped = 1 then 1
    when (
        select 
            major_id 
        from 
            sys.extended_properties 
        where 
            major_id = object_id and 
            minor_id = 0 and 
            class = 1 and 
            name = N'microsoft_database_tools_support') 
        is not null then 1
    else 0
end as bit) = 0 and type_desc not in ('EVENT_NOTIFICATION')";

            var queries = new System.Collections.Specialized.StringCollection() { query };
            var initialObjects = database.ExecutionManager.ExecuteWithResults(queries).Tables[0];
            return initialObjects.Rows.Cast<System.Data.DataRow>().Select(r => $"{r["name"]}:{r["type_desc"]}").ToList();
        }

        // These are the top level objects in Azure DB that ScriptMaker/SmoDependencyDiscoverer can find dependencies for
        private IEnumerable<IList<SqlSmoObject>> GetDatabaseObjects(Database database)
        {
            Func<Table, bool> tablePredicate = (table) => true;
            Func<View, bool> viewPredicate = (view) => true;
            if (database.Parent.IsSupportedProperty(typeof(Table), nameof(Table.LedgerType)))
            {
                tablePredicate = t => !t.IsDroppedLedgerTable && t.LedgerType != LedgerTableType.HistoryTable;
                viewPredicate = v => !v.IsDroppedLedgerView && v.LedgerViewType != LedgerViewType.LedgerView;
            }
            yield return database.Tables.Cast<Table>().Where(tablePredicate).Cast<SqlSmoObject>().ToList();
            yield return database.UserDefinedFunctions.Cast<SqlSmoObject>().ToList();
            yield return database.Views.Cast<View>().Where(viewPredicate).Cast<SqlSmoObject>().ToList();
            yield return database.StoredProcedures.Cast<SqlSmoObject>().ToList();
            yield return database.Defaults.Cast<SqlSmoObject>().ToList();
            yield return database.Rules.Cast<SqlSmoObject>().ToList();
            yield return database.UserDefinedAggregates.Cast<SqlSmoObject>().ToList();
            yield return database.Synonyms.Cast<SqlSmoObject>().ToList();
            yield return database.Sequences.Cast<SqlSmoObject>().ToList();
            yield return database.UserDefinedDataTypes.Cast<SqlSmoObject>().ToList();
            yield return database.XmlSchemaCollections.Cast<SqlSmoObject>().ToList();
            yield return database.UserDefinedTypes.Cast<SqlSmoObject>().ToList();
            yield return database.Assemblies.Cast<SqlSmoObject>().ToList();
            yield return database.PartitionSchemes.Cast<SqlSmoObject>().ToList();
            yield return database.PartitionFunctions.Cast<SqlSmoObject>().ToList();
            yield return database.UserDefinedTableTypes.Cast<SqlSmoObject>().ToList();
            yield return database.Triggers.Cast<SqlSmoObject>().ToList();
            yield return database.PlanGuides.Cast<SqlSmoObject>().ToList();
        }
    }

    [TestClass]
    public class DependencyDiscoverTestsAzure : DependencyDiscoveryTestsBase
    {
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        public void SmoDependencyDiscoverer_AzureDb_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(null, AzureDatabaseEdition.BusinessCritical);
        }

    }

    [TestClass]
    public class DependencyDiscoveryTestsStandalone120 : DependencyDiscoveryTestsBase
    {
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 11, MaxMajor = 11)]
        public void SmoDependencyDiscoverer_v110_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(@"\\sqltoolsfs\utbackups\SmoBaselineVerification_SQL2012.bak", AzureDatabaseEdition.NotApplicable);
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 12, MaxMajor = 12)]
        public void SmoDependencyDiscoverer_v120_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(@"\\sqltoolsfs\utbackups\SmoBaselineVerification_SQL2014.bak", AzureDatabaseEdition.NotApplicable);
        }
    }

    [TestClass]
    public class DependencyDiscoveryTestsStandalone140 : DependencyDiscoveryTestsBase
    {

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 13, MaxMajor = 13)]
        public void SmoDependencyDiscoverer_v130_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(@"\\sqltoolsfs\utbackups\SmoBaselineVerification_SQL2016.bak", AzureDatabaseEdition.NotApplicable);
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 14, MaxMajor = 14, HostPlatform = "Windows")]
        public void SmoDependencyDiscoverer_v140_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(@"\\sqltoolsfs\utbackups\SmoBaselineVerification_SQL2017.bak", AzureDatabaseEdition.NotApplicable);
        }
    }

    [TestClass]
    public class DependencyDiscoveryTestsStandalone160 : DependencyDiscoveryTestsBase
    {
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15, MaxMajor = 15, HostPlatform = "Windows")]
        public void SmoDependencyDiscoverer_v150_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(@"\\sqltoolsfs\utbackups\SmoBaselineVerification_SQLv150.bak", AzureDatabaseEdition.NotApplicable);
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 16, MaxMajor = 16, HostPlatform = "Windows")]
        public void SmoDependencyDiscoverer_v160_can_enumerate_objects_to_drop_to_clean_database()
        {
            TestDependencyScriptingForDrop(@"\\sqltoolsfs\utbackups\SmoBaselineVerification_SQLv160_CS.bak", AzureDatabaseEdition.NotApplicable);
        }
    }
}