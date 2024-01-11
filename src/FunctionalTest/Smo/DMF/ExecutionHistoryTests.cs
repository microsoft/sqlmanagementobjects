// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.DMF
{
    /// <summary>
    /// Porting DMV Execution History tests from ds_main
    /// Merged this class with ExecutionHistoryTests to avoid race conditions during parallel test execution
    /// </summary>
    public partial class PolicyTests : SqlTestBase
    {
        /// <summary>
        /// Verifies that when the LogOnSuccess config option is set to true a successful
        /// policy evaluation results in a single entry in the EvaluationHistories table.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 10)]

        public void When_LogOnSuccess_is_true_Policy_EvaluationHistories_match_server_data()
        {
            ExecuteWithDbDrop(db =>
            {
                Policy policy = null;
                Condition condition = null;
                ObjectSet objectSet = null;
                TraceHelper.TraceInformation("Setting LogOnSuccess to true");
                var configValue =
                    (int)
                        ServerContext.ConnectionContext.ExecuteScalar(
                            "select current_value from msdb.dbo.syspolicy_configuration where name='LogOnSuccess'");
                ServerContext.ConnectionContext.ExecuteNonQuery(
                    "exec msdb.dbo.sp_syspolicy_configure  @name=N'LogOnSuccess', @value=1");
                try
                {

                    var policyStore = new PolicyStore(new SqlStoreConnection(
                        new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString)));
                    var expr = new ExpressionNodeOperator(OperatorType.EQ, new ExpressionNodeAttribute("Name"),
                        ExpressionNode.ConstructNode(db.Name));
                    condition = policyStore.CreateCondition(typeof (Database).Name, expr);
                    objectSet = policyStore.CreateObjectSet(
                            facet: "Database",
                            targetSetsAndLevelConditions: new Dictionary<string, IList<Tuple<string, string>>>()
                            {
                                {
                                    // No filter because we want a mix of success and failure
                                "Server/Database",
                                new [] {
                                    new Tuple<string,string>("Server/Database", string.Empty),
                                }
                            }},
                            objectSetNamePrefix: this.TestMethod.Name);

                    policy = policyStore.CreatePolicy(condition.Name, AutomatedPolicyEvaluationMode.None, objectSet.Name);

                    TraceHelper.TraceInformation("Evaluating Policy {0} in Check mode", policy.Name);
                    policy.Evaluate(AdHocPolicyEvaluationMode.Check, policyStore.SqlStoreConnection);
                    TraceHelper.TraceInformation("Evaluating Policy {0} in Configure mode", policy.Name);
                    policy.Evaluate(AdHocPolicyEvaluationMode.Configure, policyStore.SqlStoreConnection);
                    VerifyExecutionHistory(policy);
                    Assert.That(
                        policy.EvaluationHistories.SelectMany(eh => eh.ConnectionEvaluationHistories)
                            .SelectMany(c => c.EvaluationDetails)
                            .Select(ed => ed.Result), Has.Exactly(1).True, "Expected 1 evaluation successful match");
                    foreach (var evaluationHistory in policy.EvaluationHistories)
                    {
                        VerifyHistoryDetail(policy, evaluationHistory.ID);
                    }
                }
                finally
                {
                    try
                    {
                        TraceHelper.TraceInformation("Setting LogOnSuccess to " + configValue);
                        ServerContext.ConnectionContext.ExecuteNonQuery(
                            "exec msdb.dbo.sp_syspolicy_configure  @name=N'LogOnSuccess', @value=" + configValue);
                    }
                    catch (Exception e)
                    {
                        TraceHelper.TraceInformation("Unable to restore LogOnSuccess config value: " + e);
                    }
                    SmoObjectHelpers.SafeDrop(policy, condition, objectSet);
                }
            });
        }

        /// <summary>
        /// Verifies that when a policy fails a well-formed XML is returned by the
        /// PolicyEvaluationWriter and contains the correct content.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 10)]

        public void When_Evaluate_fails_PolicyEvaluationResultsWriter_generates_correct_xml()
        {
            ExecuteTest(() =>
            {
                Policy policy = null;
                Condition condition = null;
                ObjectSet objectSet = null;
                try
                {
                    var xpCmdShellEnabled = (1 == ServerContext.Configuration.XPCmdShellEnabled.RunValue);
                    var policyStore = new PolicyStore(new SqlStoreConnection(
                        new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString)));
                    var expr = new ExpressionNodeOperator(OperatorType.NE, new ExpressionNodeAttribute("XPCmdShellEnabled"),
                        ExpressionNode.ConstructNode(xpCmdShellEnabled));
                    condition = policyStore.CreateCondition(typeof (ISurfaceAreaFacet).Name, expr);
                    objectSet = policyStore.CreateObjectSet(
                        facet: "ISurfaceAreaFacet",
                        targetSetsAndLevelConditions:null,
                        // object set names have 128 char max
                        objectSetNamePrefix: this.TestMethod.Name.Substring(20));

                    policy = policyStore.CreatePolicy(condition.Name, AutomatedPolicyEvaluationMode.None, objectSet.Name);

                    TraceHelper.TraceInformation("Evaluating Policy {0} in Check mode", policy.Name);
                    var result = policy.Evaluate(AdHocPolicyEvaluationMode.Check, policyStore.SqlStoreConnection);
                    Assert.That(result, Is.False, "Evaluate XPCmdShellEnabled != {0} should return false", xpCmdShellEnabled);
                    Assert.That(policy.EvaluationHistories.Count, Is.EqualTo(1), "One only evaluation expected");
                    var stringBuilder = new StringBuilder();
                    var xmlWriter = XmlTextWriter.Create(stringBuilder,
                        PolicyEvaluationResultsWriter.GetXmlWriterSettings());
                    using (var resultsWriter = new PolicyEvaluationResultsWriter(xmlWriter))
                    {
                        foreach (EvaluationHistory evaluationHistory in policy.EvaluationHistories)
                        {
                            resultsWriter.WriteEvaluationHistory(evaluationHistory);
                        }
                    }
                    xmlWriter.Flush();
                    TraceHelper.TraceInformation("Serialized history: " + stringBuilder);
                    XmlDocument document = new XmlDocument();
                    document.LoadXml(stringBuilder.ToString());
                    XmlNamespaceManager nsManager = SfcXmlHelper.GetXmlNsManager(document);

                    XmlHelper.SelectFirstAndOnlyNode("/PolicyEvaluationResults", document, nsManager);

                    XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationHistory", document, nsManager);
                    XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationHistory/dmf:StartDate", document, nsManager);
                    XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationHistory/dmf:EndDate", document, nsManager);

                    XPathNavigator selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationHistory/dmf:Exception", document, nsManager);
                    Assert.That(selection.Value, Is.Null.Or.Empty, "The history reports an exception, but there should not be one.");

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationHistory/dmf:Result", document, nsManager);
                    Assert.That(selection.ValueAs(typeof(bool)), Is.EqualTo(result), "The history reports a different result than the result from the evaluation.");

                    XmlHelper.SelectFirstAndOnlyNode("//dmf:ConnectionEvaluationHistory", document, nsManager);

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:ConnectionEvaluationHistory/dmf:Exception", document, nsManager);
                    Assert.That(selection.Value, Is.Null.Or.Empty, "The connection history reports an exception, but there should not be one.");

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:ConnectionEvaluationHistory/dmf:Result", document, nsManager);
                    Assert.That(selection.ValueAs(typeof(bool)), Is.EqualTo(result), "The connection history reports a different result than the result from the evaluation.");

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:ConnectionEvaluationHistory/dmf:ServerInstance", document, nsManager);
                    Assert.That(selection.Value, Does.Contain(ServerContext.ConnectionContext.TrueName).IgnoreCase, "The connection history reports an incorrect server name");

                    XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationDetail", document, nsManager);
                    XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationDetail/dmf:EvaluationDate", document, nsManager);

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationDetail/dmf:Exception", document, nsManager);
                    Assert.That(selection.Value, Is.Null.Or.Empty, "The detail reports an exception, but there should not be one.");

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationDetail/dmf:ResultDetail", document, nsManager);
                    Assert.That(selection.Value, Is.Not.Null.And.Not.Empty, "ResultDetail empty");

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationDetail/dmf:TargetQueryExpression", document, nsManager);
                    Assert.That(selection.Value, Is.Not.Null.And.Not.Empty, "TargetQueryExpression empty");

                    selection = XmlHelper.SelectFirstAndOnlyNode("//dmf:EvaluationDetail/dmf:Result", document, nsManager);
                    Assert.That(selection.ValueAs(typeof(bool)), Is.EqualTo(result), "The detail reports a different result than the result from the evaluation.");
                }
                finally
                {
                    SmoObjectHelpers.SafeDrop(policy, condition, objectSet);
                }
            });
        }

        /// <summary>
        /// Verifies that when a condition has an OR operator the EvaluationHistories list
        /// contains 2 entries
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 10)]

        public void When_Condition_has_OR_operator_ConnectionEvaluationHistories_contains_multiple_details()
        {
            ExecuteWithDbDrop((db) =>
            {
                Policy policy = null;
                Condition condition = null;
                ObjectSet objectSet = null;
                Condition dbNameCondition = null;
                try
                {
                    var policyStore = new PolicyStore(new SqlStoreConnection(
                        new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString)));
                    ExpressionNode expr1 = new ExpressionNodeOperator(OperatorType.LIKE, new ExpressionNodeAttribute("Name"), new ExpressionNodeConstant("Temp%"));
                    ExpressionNode expr2 = new ExpressionNodeOperator(OperatorType.EQ, new ExpressionNodeAttribute("IsFixedRole"), new ExpressionNodeFunction(ExpressionNodeFunction.Function.False));
                    ExpressionNode expr = new ExpressionNodeOperator(OperatorType.OR, expr1, expr2);
                    //Create the condition we'll use to filter out the database name for the object set
                    dbNameCondition = policyStore.CreateCondition(
                        facet: "Database",
                        expressionNodeExpression: string.Format("@Name = '{0}'", Urn.EscapeString(db.Name)),
                        conditionNamePrefix: "Database_Name_Condition");
                    condition = policyStore.CreateCondition("DatabaseRole", expr);
                    objectSet = policyStore.CreateObjectSet(
                        facet: "DatabaseRole",
                        targetSetsAndLevelConditions: new Dictionary<string, IList<Tuple<string, string>>>()
                        {
                            {
                                "Server/Database/Role",
                                new[]
                                {
                                    new Tuple<string, string>("Server/Database", dbNameCondition.Name),
                                    new Tuple<string, string>("Server/Database/Role", string.Empty),
                                }
                            }
                        },
                        // object set names have 128 char max
                        objectSetNamePrefix: this.TestMethod.Name.Substring(20));

                    policy = policyStore.CreatePolicy(condition.Name, AutomatedPolicyEvaluationMode.None, objectSet.Name);

                    TraceHelper.TraceInformation("Evaluating Policy {0} in Check mode", policy.Name);
                    var result = policy.Evaluate(AdHocPolicyEvaluationMode.Check, policyStore.SqlStoreConnection);
                    Assert.That(result, Is.False, "Policy should evaluate False");
                    Assert.That(policy.EvaluationHistories.Count, Is.EqualTo(1), "One evaluation history expected");
                    var detailsCount =
                        policy.EvaluationHistories.SelectMany(eh => eh.ConnectionEvaluationHistories)
                        .Sum(ceh => ceh.EvaluationDetails.Count);
                    TraceHelper.TraceInformation("Number of detail records:" + detailsCount);
                    Assert.That(detailsCount, Is.AtLeast(2), "History detail records expected");
                }
                finally
                {
                    SmoObjectHelpers.SafeDrop(policy, condition, objectSet, dbNameCondition);
                }
            });
        }

        /// <summary>
        /// Verifies that when a database is offline evaluating a policy that checks
        /// DB names completes successfully and does not record any exceptions.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 10)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance)]
        public void When_Database_is_offline_evaluate_does_not_record_an_exception()
        {
            ExecuteWithDbDrop(db =>
            {
                Policy policy = null;
                Condition condition = null;
                ObjectSet objectSet = null;
                Condition dbNameCondition = null;
                try
                {
                    db.SetOffline();
                    var policyStore = new PolicyStore(new SqlStoreConnection(
                        new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString)));
                    var expr = new ExpressionNodeOperator(OperatorType.EQ, new ExpressionNodeAttribute("Schema"),
                        ExpressionNode.ConstructNode("dbo"));
                    //Create the condition we'll use to filter out the database name for the object set
                    dbNameCondition = policyStore.CreateCondition(
                        facet: "Database",
                        expressionNodeExpression: string.Format("@Name = '{0}'", Urn.EscapeString(db.Name)),
                        conditionNamePrefix: "Database_Name_Condition");
                    condition = policyStore.CreateCondition("Table", expr);
                    objectSet = policyStore.CreateObjectSet(
                        facet: "Table",
                        targetSetsAndLevelConditions: new Dictionary<string, IList<Tuple<string, string>>>()
                        {
                            {
                                "Server/Database/Table",
                                new[]
                                {
                                    new Tuple<string, string>("Server/Database", dbNameCondition.Name),
                                    new Tuple<string, string>("Server/Database/Table", string.Empty),
                                }
                            }
                        },
                        objectSetNamePrefix: this.TestMethod.Name);

                    policy = policyStore.CreatePolicy(condition.Name, AutomatedPolicyEvaluationMode.None, objectSet.Name);

                    TraceHelper.TraceInformation("Evaluating Policy {0} in Check mode", policy.Name);
                    // return value doesn't matter
                    policy.Evaluate(AdHocPolicyEvaluationMode.Check, policyStore.SqlStoreConnection);
                    Assert.That(policy.EvaluationHistories.Count, Is.EqualTo(1), "One evaluation history expected");
                    Assert.That(
                        policy.EvaluationHistories.SelectMany(eh => eh.ConnectionEvaluationHistories)
                            .SelectMany(ceh => ceh.EvaluationDetails)
                            .Select(d => d.Exception), Has.All.Null.Or.Empty, "No exception expected");
                }
                finally
                {
                    SmoObjectHelpers.SafeDrop(policy, condition, objectSet, dbNameCondition);
                }
            });
        }

        private void VerifyExecutionHistory(Policy policy, int expectedCount = 2)
        {
            TraceHelper.TraceInformation("Comparing SMO Policy.EvaluationHistories with query output");
            var dataSet = ServerContext.ExecutionManager.ConnectionContext.ExecuteWithResults(
                "SELECT history_id AS ID, start_date AS StartDate, end_date AS EndDate, result AS Result, exception AS Exception FROM msdb.dbo.syspolicy_policy_execution_history WHERE policy_id=" +
                policy.ID);
            var table = dataSet.Tables[0];
            var rows = table.Rows.OfType<DataRow>().ToList();
            Assert.That(rows.Count, Is.EqualTo(expectedCount), "Unexpected number of history entries");
            var histories = policy.EvaluationHistories;
            Assert.That(histories.Select(h => h.ID), Is.EquivalentTo(rows.Select(r => (long) r.ItemArray[0])),
                "IDs don't match");
            Assert.That(histories.Select(h => h.StartDate), Is.EquivalentTo(rows.Select(r => r.ItemArray[1])),
                "Start dates don't match");
            Assert.That(histories.Select(h => h.EndDate), Is.EquivalentTo(rows.Select(r => r.ItemArray[2])),
                "End dates don't match");
            Assert.That(histories.Select(h => h.Result), Is.EquivalentTo(rows.Select(r => r.ItemArray[3])),
                "Results don't match");
            Assert.That(histories.Select(h => h.Exception), Is.EquivalentTo(rows.Select(r => r.ItemArray[4])),
                "Exceptions don't match");
        }

        private void VerifyHistoryDetail(Policy policy, long historyId)
        {
            TraceHelper.TraceInformation("Comparing SMO ConnectionEvaluationHistory.EvaluationDetails with query output for history id {0}", historyId);
            var dataSet = ServerContext.ExecutionManager.ConnectionContext.ExecuteWithResults(
                "select detail_id as ID, target_query_expression as TargetQueryExpression, execution_date as ExecutionDate, result as Result, result_detail from msdb.dbo.syspolicy_policy_execution_history_details where history_id=" +
                historyId);
            var table = dataSet.Tables[0];
            var rows = table.Rows.OfType<DataRow>().ToList();
            TraceHelper.TraceInformation("Expecting {0} rows", rows.Count);
            var details =
                policy.EvaluationHistories[historyId].ConnectionEvaluationHistories.SelectMany(c => c.EvaluationDetails)
                    .ToList();
            Assert.That(details.Select(d => d.ID), Is.EquivalentTo(rows.Select(r => (long) r.ItemArray[0])),
                "IDs don't match");
            Assert.That(details.Select(d => d.TargetQueryExpression), Is.EquivalentTo(rows.Select(r => r.ItemArray[1])),
                "TargetQueryExpression doesn't match");
            Assert.That(details.Select(d => d.EvaluationDate), Is.EquivalentTo(rows.Select(r => r.ItemArray[2])),
                "EvaluationDate doesn't match");
            Assert.That(details.Select(d => d.Result), Is.EquivalentTo(rows.Select(r => r.ItemArray[3])),
                "Result doesn't match");
            Assert.That(details.Select(d => d.ResultDetail), Is.EquivalentTo(rows.Select(r => r.ItemArray[4])),
                "ResultDetail doesn't match");
        }
    }
}
