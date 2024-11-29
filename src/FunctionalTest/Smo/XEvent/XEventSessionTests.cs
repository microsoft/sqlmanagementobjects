// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlServer.Management.XEventDbScoped;
using NUnit.Framework;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Management.Smo;
using System.Linq;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Assert = NUnit.Framework.Assert;
using System.Reflection.Emit;

namespace Microsoft.SqlServer.Test.SMO.XEvent
{
    [TestClass]
    public class XEventSessionTests : SqlTestBase
    {
        private BaseXEStore store = null;
        private ServerConnection connection = null;

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptCreateAlter()
        {
            ExecuteFromDbPool((db) =>
            {
                ScriptCreateAlter(isServerScoped: false, db);
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptCreateTSQL()
        {
            ExecuteFromDbPool((db) =>
            {
                ScriptCreateTSQL(isServerScoped: false, db);
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptAlterPredicates()
        {
            ExecuteFromDbPool((db) =>
            {
                ScriptAlterPredicates(isServerScoped: false, db);
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void ScriptAlterEventWithNewLine()
        {
            ExecuteFromDbPool((db) =>
            {
                ScriptAlterEventWithNewLine(isServerScoped: false, db);
            });
        }

        // On prem test cases.

        // This test fails on Linux because the event collection order is different than on Windows.
        // Marked as Legacy until we can fix the test.
        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        [TestCategory("Legacy")]
        public void OnpremScriptCreateAlter()
        {
            ExecuteTest(() =>
            {
                ScriptCreateAlter(isServerScoped: true);
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        public void OnPremScriptCreateTSQL()
        {
            ExecuteTest(() =>
            {
                ScriptCreateTSQL(isServerScoped: true);
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        public void OnPremScriptAlterPredicates()
        {
            ExecuteTest(() =>
            {
                ScriptAlterPredicates(isServerScoped: true);
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, Edition = DatabaseEngineEdition.Enterprise, MinMajor = 15)]
        public void OnpremScriptAlterEventWithNewLine()
        {
            ExecuteTest(() =>
            {
                ScriptAlterEventWithNewLine(isServerScoped: true);
            });
        }

        [TestMethod]
        [UnsupportedFeature(SqlFeature.NoDropCreate)]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlDatabase)]
        [DataRow("Latin1_General_100_CI_AS", CatalogCollationType.SQLLatin1GeneralCP1CIAS)]
        [DataRow("Latin1_General_100_CI_AS", CatalogCollationType.DatabaseDefault)]
        [DataRow("Latin1_General_100_CI_AS", null)]
        [DataRow("SQL_Latin1_General_CP1_CS_AS", CatalogCollationType.DatabaseDefault)]
        [DataRow("SQL_Latin1_General_CP1_CS_AS", CatalogCollationType.SQLLatin1GeneralCP1CIAS)]
        [DataRow("SQL_Latin1_General_CP1_CS_AS", null)]
        [DataRow("", null)]
        [DataRow("", CatalogCollationType.SQLLatin1GeneralCP1CIAS)]
        [DataRow("", CatalogCollationType.DatabaseDefault)]
        public void EnumerateSessionsWithCaseSensitiveCatalogCollation(string collation, CatalogCollationType? catalogCollationType)
        {
            ExecuteTest(() =>
            {
                var db = new Database(ServerContext, "CSCollation" + Guid.NewGuid().ToString());
                if (!string.IsNullOrEmpty(collation))
                {
                    db.Collation = collation;
                }
                if (catalogCollationType.HasValue)
                {
                    db.CatalogCollation = catalogCollationType.Value;
                }
                db.Create();
                try
                {
                    db.ExecuteNonQuery(@"CREATE EVENT SESSION [newsession] ON DATABASE 
ADD EVENT sqlserver.sql_batch_completed(
    SET collect_batch_text=(1)
    ACTION(sqlserver.client_app_name,sqlserver.database_id,sqlserver.query_hash,sqlserver.session_id,sqlserver.sql_text)
    WHERE ([package0].[greater_than_uint64]([sqlserver].[database_id],(4)) AND [package0].[equal_boolean]([sqlserver].[is_system],(0))))
ADD TARGET package0.ring_buffer(SET max_events_limit=(0),max_memory=(8192))
WITH (MAX_MEMORY=4096 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=30 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=ON,STARTUP_STATE=OFF)
");
                    var sqlStoreConnection = new SqlStoreConnection(db.ExecutionManager.ConnectionContext.SqlConnectionObject);
                    var xeStore = new DatabaseXEStore(sqlStoreConnection);
                    Assert.That(xeStore.Sessions.Select(s => s.Name).ToList(), Has.Member("newsession"), "Session not found");
                    var session = xeStore.Sessions["newsession"];
                    var target = session.Targets.Single();
                    Assert.That(target.Description, Contains.Substring("ring buffer"), "Wrong target description");
                    var targetField = target.TargetFields.First();
                    Assert.That(targetField.Name, Contains.Substring("max_events"), "Wrong target field name");
                    var ev = session.Events.Single();
                    Assert.That(ev.Name, Is.EqualTo("sqlserver.sql_batch_completed"), "Wrong event name");
                    var action = ev.Actions["sqlserver.sql_text"];
                    Assert.That(action.Description, Contains.Substring("SQL text"), "Wrong action description");
                    var eventField = ev.EventFields.First();
                    Assert.That(eventField.Name, Is.EqualTo("collect_batch_text"), "Wrong event field name");
                    db.ExecutionManager.ConnectionContext.Disconnect();
                }
                finally
                {
                    _ = ServerContext.DropKillDatabaseNoThrow(db.Name);
                }
            });
        }

        [TestMethod]
        [SqlTestArea(SqlTestArea.ExtendedEvents)]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 12)]
        public void EnumerateSessionsAndObjectDataOnPrem()
        {
            ExecuteTest(() =>
            {
                var sessionName = "newSession" + Guid.NewGuid().ToString();
                _ = ServerContext.ConnectionContext.ExecuteNonQuery($@"CREATE EVENT SESSION [{sessionName}] ON SERVER 
ADD EVENT sqlserver.sql_batch_completed(
    SET collect_batch_text=(1)
    ACTION(sqlserver.client_app_name,sqlserver.database_id,sqlserver.query_hash,sqlserver.session_id,sqlserver.sql_text)
    WHERE ([package0].[greater_than_uint64]([sqlserver].[database_id],(4)) AND [package0].[equal_boolean]([sqlserver].[is_system],(0))))
ADD TARGET package0.ring_buffer(SET max_events_limit=(0),max_memory=(8192))
WITH (MAX_MEMORY=4096 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=30 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=ON,STARTUP_STATE=OFF)
");
                try
                {
                    var sqlStoreConnection = new SqlStoreConnection(ServerContext.ConnectionContext.SqlConnectionObject);
                    var xeStore = new XEStore(sqlStoreConnection);
                    Assert.That(xeStore.Sessions.Select(s => s.Name).ToList(), Has.Member(sessionName), "Session not found");
                    var session = xeStore.Sessions[sessionName];
                    var target = session.Targets.Single();
                    Assert.That(target.Description, Contains.Substring("ring buffer"), "Wrong target description");
                    var targetField = target.TargetFields.First();
                    Assert.That(targetField.Name, Contains.Substring("max_events"), "Wrong target field name");
                    var ev = session.Events.Single();
                    Assert.That(ev.Name, Is.EqualTo("sqlserver.sql_batch_completed"), "Wrong event name");
                    var action = ev.Actions["sqlserver.sql_text"];
                    Assert.That(action.Description, Contains.Substring("SQL text"), "Wrong action description");
                    var eventField = ev.EventFields.First();
                    Assert.That(eventField.Name, Is.EqualTo("collect_batch_text"), "Wrong event field name");
                }
                finally
                {
                    _ = ServerContext.ConnectionContext.ExecuteNonQuery($"DROP EVENT SESSION [{sessionName}] ON SERVER");
                }
            });
        }

        private void CreateSession(BaseXEStore store, out string name, out Session session)
        {
            name = Guid.NewGuid().ToString();
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }
            session = store.CreateSession(name);
            Assert.That(session.ScriptCreate, Throws.InstanceOf<XEventException>());
        }

        private BaseXEStore CreateStore(bool isServerScoped, Database db = null)
        {
            var sqlStoreConnection = new SqlStoreConnection(new SqlConnection(SqlConnectionStringBuilder.ToString()));
            if (isServerScoped)
            {
                return new XEStore(sqlStoreConnection);
            }
            else
            {
                Assert.That(db, Is.Not.Null, "db parameter cannot be null for Database scoped tests");
                return new DatabaseXEStore(sqlStoreConnection, db.Name);
            }
        }

        private void InitializeServerConnection(bool isServerScoped, Database db = null)
        {
            var connectionString = SqlConnectionStringBuilder.ToString();
            if (!isServerScoped)
            {
                Assert.That(db, Is.Not.Null, "db parameter cannot be null for Database scoped tests");
                connectionString = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = db.Name, Pooling = false }.ToString();
            }
            this.connection = new ServerConnection(new SqlConnection(connectionString));
        }

        private void ScriptCreateAlter(bool isServerScoped, Database db = null)
        {
            store = CreateStore(isServerScoped, db);
            CreateSession(store, out var name, out var session);

            // List of Predicate
            var waitTypeDic = new Dictionary<object, string>()
                {
                    { "ABR", "'ABR'" },
                    { 388, "388" }
                };

            var createSessionOnString = isServerScoped ? "SERVER" : "DATABASE";

            var expectedCreateString = $@"CREATE EVENT SESSION [{name}] ON {createSessionOnString} 
ADD EVENT sqlos.wait_completed(
    WHERE ([wait_type]=({waitTypeDic["ABR"]}))),
ADD EVENT sqlos.wait_info(
    WHERE ([wait_type]=({waitTypeDic[388]})))
WITH (MAX_MEMORY=4096 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=30 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=OFF,STARTUP_STATE=OFF)
".FixNewLines();
            var expectedAlterString = $@"ALTER EVENT SESSION [{name}] ON {createSessionOnString} 
DROP EVENT sqlos.wait_completed, 
DROP EVENT sqlos.wait_info, 
DROP EVENT sqlos.wait_info_external
ALTER EVENT SESSION [{name}] ON {createSessionOnString} 
ADD EVENT sqlos.wait_completed(
    WHERE ([wait_type]<>({waitTypeDic["ABR"]}))), 
ADD EVENT sqlos.wait_info(
    WHERE ([wait_type]<>({waitTypeDic[388]}))), 
ADD EVENT sqlos.wait_info_external(
    WHERE ([wait_type]<>(388)))
".FixNewLines();

            // Dictionary adding new events in collection in different sequences
            var expectedAlterString2 = $@"ALTER EVENT SESSION [{name}] ON {createSessionOnString} 
DROP EVENT sqlos.wait_completed, 
DROP EVENT sqlos.wait_info_external, 
DROP EVENT sqlos.wait_info
ALTER EVENT SESSION [{name}] ON {createSessionOnString} 
ADD EVENT sqlos.wait_completed(
    WHERE ([wait_type]<>({waitTypeDic["ABR"]}))), 
ADD EVENT sqlos.wait_info_external(
    WHERE ([wait_type]<>(388))), 
ADD EVENT sqlos.wait_info(
    WHERE ([wait_type]<>({waitTypeDic[388]})))
".FixNewLines();

            // Creating Event
            var eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_completed");
            var eventInfo1 = store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_info");
            var operand = new PredOperand(eventInfo.DataEventColumnInfoSet["wait_type"]);

            Assert.Multiple(() =>
            {
                // Adding event into session
                var evt = session.AddEvent(eventInfo);
                evt.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, new PredValue(waitTypeDic.First(x => x.Value == "'ABR'").Key));
                var evt1 = session.AddEvent(eventInfo1);
                evt1.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, new PredValue(waitTypeDic.First(x => x.Value == "388").Key));
                // Creating session environment 
                session.Create();
                Assert.That(session.ScriptCreate().ToString(), Is.EqualTo(expectedCreateString), "Generated Create script is not same as expected string!");

                //Alter section
                evt.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.NE, operand, new PredValue(waitTypeDic.FirstOrDefault(x => x.Value == "'ABR'").Key));
                evt1.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.NE, operand, new PredValue(waitTypeDic.FirstOrDefault(x => x.Value == "388").Key));
                var eventInfo2 = store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_info_external");
                var evt2 = session.AddEvent(eventInfo2);
                evt2.Predicate = new PredCompareExpr(PredCompareExpr.ComparatorType.NE, operand, new PredValue(388));
                session.Alter();
                Assert.That(session.ScriptAlter().ToString(), Is.EqualTo(expectedAlterString).Or.EqualTo(expectedAlterString2),
                    "Generated Alter script is not same as expected string!");
                session.Drop();
            });
        }

        private void ScriptCreateTSQL(bool isServerScoped, Database db = null)
        {
            store = CreateStore(isServerScoped, db);
            var name = Guid.NewGuid().ToString();
            if (store.Sessions[name] != null)
            {
                store.Sessions[name].Drop();
            }
            // Establish sql connection for creating session using TSQL.
            InitializeServerConnection(isServerScoped, db);

            var createSessionOnString = isServerScoped ? "SERVER" : "DATABASE";

            var expectedCreateString = $@"CREATE EVENT SESSION [{name}] ON {createSessionOnString} 
ADD EVENT sqlos.wait_completed(
    WHERE ([wait_type]='BROKER_START' AND [wait_type]=(388))),
ADD EVENT sqlos.wait_info(
    WHERE ([wait_type]='ASSEMBLY_LOAD'))
WITH (MAX_MEMORY=4096 KB,EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,MAX_DISPATCH_LATENCY=30 SECONDS,MAX_EVENT_SIZE=0 KB,MEMORY_PARTITION_MODE=NONE,TRACK_CAUSALITY=OFF,STARTUP_STATE=OFF)
".FixNewLines();

            connection.ExecuteScalar(expectedCreateString);

            store.Refresh();

            var session = store.Sessions[name];
            Assert.Multiple(() =>
            {
                Assert.That(store.Sessions[name].Events["sqlos.wait_completed"].PredicateExpression, Is.EqualTo("([wait_type]='BROKER_START' AND [wait_type]=(388))"), "Generated PredExpression is not same as expected PredExpression!");
                Assert.That(store.Sessions[name].Events["sqlos.wait_info"].PredicateExpression, Is.EqualTo("([wait_type]='ASSEMBLY_LOAD')"), "Generated PredExpression is not same as expected PredExpression!");
                Assert.That(session.ScriptCreate().ToString(), Is.EqualTo(expectedCreateString), "Generated Create script is not same as expected string!");
            });
            session.Drop();
        }

        private void ScriptAlterPredicates(bool isServerScoped, Database db = null)
        {
            store = CreateStore(isServerScoped, db);
            CreateSession(store, out var name, out var session);

            var predlst = new List<object>() { 450, 1385, "ACTIVE_RG_LIST" };

            // Creating Event
            var eventInfo = store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_completed");
            var operand = new PredOperand(eventInfo.DataEventColumnInfoSet["wait_type"]);

            // Adding event into session
            var evt = session.AddEvent(eventInfo);

            // Creating Predicate expression
            var pred1 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, new PredValue(predlst[0]));
            var pred2 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, new PredValue(predlst[1]));
            var pred3 = new PredCompareExpr(PredCompareExpr.ComparatorType.EQ, operand, new PredValue(predlst[2]));

            var predIntermediate = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Or, pred1, pred2);
            evt.Predicate = new PredLogicalExpr(PredLogicalExpr.LogicalOperatorType.Or, predIntermediate, pred3);

            // Creating session environment 
            session.Create();

            var createSessionOnString = isServerScoped ? "SERVER" : "DATABASE";

            var expectedCreateString = $@"ALTER EVENT SESSION [{session.Name}] ON {createSessionOnString} 
DROP EVENT sqlos.wait_completed
ALTER EVENT SESSION [{session.Name}] ON {createSessionOnString} 
ADD EVENT sqlos.wait_completed(
    WHERE ((([wait_type]=(450)) OR ([wait_type]=(1385))) OR ([wait_type]=('ACTIVE_RG_LIST'))))
".FixNewLines();
            Assert.That(session.ScriptAlter().ToString(), Is.EqualTo(expectedCreateString), "Generated Alter script is not same as expected string!");

            session.Drop();
        }

        private void ScriptAlterEventWithNewLine(bool isServerScoped, Database db = null)
        {
            store = CreateStore(isServerScoped, db);
            CreateSession(store, out var name, out var session);

            session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.rpc_starting"));

            var createSessionOnString = isServerScoped ? "SERVER" : "DATABASE";

            var expectedAlterString = $@"ALTER EVENT SESSION [{name}] ON {createSessionOnString} 
ADD EVENT sqlos.wait_completed, 
ADD EVENT sqlserver.sp_statement_starting
".FixNewLines();

            // Creating session environment 
            session.Create();

            // Adding events for Alter
            session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlserver.sp_statement_starting"));
            session.AddEvent(store.ObjectInfoSet.Get<EventInfo>("sqlos.wait_completed"));

            Assert.That(session.ScriptAlter().ToString(), Is.EqualTo(expectedAlterString), "Generated Alter script is not same as expected string!");

            session.Drop();
        }
    }
}
