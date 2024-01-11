// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Data;
using System.Xml;

namespace Microsoft.SqlServer.Test.SMO.DMF
{
    /// <summary>
    /// Tests for DMF Policies. Since we're unlikely to hotfix PBM/SMO for SqlClr on old versions
    /// of SQL we're only testing against v12+
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public partial class PolicyTests : SqlTestBase
    {
        /// <summary>
        /// Tests that we can successfully create a Policy for a table, execute it and
        /// have it pass successfully. 
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        public void Policy_CanCreateAndExecuteTablePolicy()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    Condition policyCondition = null, tableNameCondition = null, databaseNameCondition = null;
                    ObjectSet objectSet = null;
                    Policy policy = null;
                    try
                    {
                        var table = database.CreateTable(this.TestMethod.Name);

                        var policyStore = new PolicyStore(
                            new SqlStoreConnection(
                                new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString)));

                        //Create the condition that the Policy is going to use
                        policyCondition = policyStore.CreateCondition(
                            facet: "Table",
                            expressionNodeExpression: "@IsSystemObject = False()",
                            conditionNamePrefix: "Table_IsSystemObject_Condition");

                        //Create the condition we'll use to filter out the table name for the object set
                        tableNameCondition = policyStore.CreateCondition(
                            facet: "Table",
                            expressionNodeExpression: string.Format("@Name = '{0}'", Urn.EscapeString(table.Name)),
                            conditionNamePrefix: "Table_Name_Condition");

                        //Create the condition we'll use to filter out the database name for the object set
                        databaseNameCondition = policyStore.CreateCondition(
                            facet: "Database",
                            expressionNodeExpression: string.Format("@Name = '{0}'", Urn.EscapeString(database.Name)),
                            conditionNamePrefix: "Database_Name_Condition");

                        //Create the object set so that we only run this policy against the specific table 
                        //created for this test (evaluating objects outside the control of the test makes it
                        //difficult to determine a consistent outcome)
                        objectSet = policyStore.CreateObjectSet(
                            facet: "Table",
                            targetSetsAndLevelConditions: new Dictionary<string, IList<Tuple<string, string>>>()
                            {
                                {
                                "Server/Database/Table",
                                new [] {
                                    new Tuple<string,string>("Server/Database", databaseNameCondition.Name),
                                    //Only evaluate on Tables with the specific name of the one we created
                                    new Tuple<string,string>("Server/Database/Table", tableNameCondition.Name),
                                }
                            } },
                            objectSetNamePrefix: this.TestMethod.Name);

                        policy = policyStore.CreatePolicy(
                            condition: policyCondition.Name,
                            policyEvaluationMode: AutomatedPolicyEvaluationMode.None,
                            objectSet: objectSet.Name,
                            policyNamePrefix: this.TestMethod.Name);

                        TraceHelper.TraceInformation("Evaluating Policy {0} in PolicyStore {1}", policy.Name, policyStore.Name);
                        var result = policy.Evaluate(AdHocPolicyEvaluationMode.Check, policyStore.SqlStoreConnection);
                        var policies = policyStore.EnumApplicablePolicies(new SfcQueryExpression(table.Urn.Value));
                        var facetPolicies = policyStore.EnumPoliciesOnFacet(nameof(Table), PolicyStore.EnumerationMode.All);
                        var facetConditions = policyStore.EnumConditionsOnFacet(nameof(Table), PolicyStore.EnumerationMode.All);
                        var targetConditions = policyStore.EnumTargetSetConditions(typeof(Database), PolicyStore.EnumerationMode.All);
                        Assert.Multiple(() =>
                        {
                            Assert.That(policy.EvaluationHistories.Count, Is.EqualTo(1), "Should have 1 evaluation history");
                            Assert.That(result, Is.True, "The Policy should have evaluated to true");
                            Assert.That(policies.Rows.Cast<DataRow>().Select(r => r[nameof(Policy.Name)]), Has.Member(policy.Name), "EnumApplicablePolicies should include the policy");
                            Assert.That(facetPolicies.Cast<string>(), Has.Member(policy.Name), "EnumFacetPolicies should include the policy");
                            Assert.That(facetConditions.Cast<string>(), Has.Member(tableNameCondition.Name), "EnumFacetConditions should include the condition");
                            Assert.That(targetConditions.Cast<string>(), Has.Member(databaseNameCondition.Name), "EnumFacetConditions should include the condition");
                            Assert.That(policy.IsSystemObject, Is.False, "policy.IsSystemObject");
                        });
                        policyStore.MarkSystemObject(policy, marker: true);
                        policyStore.MarkSystemObject(databaseNameCondition, marker: true);
                        policyStore.MarkSystemObject(objectSet, marker: true);
                        databaseNameCondition.Refresh();
                        objectSet.Refresh();
                        policyStore.Policies.Refresh(refreshChildObjects: true);
                        var nonSystemConditions = policyStore.EnumTargetSetConditions(typeof(Database), PolicyStore.EnumerationMode.NonSystemOnly);
                        var systemConditions = policyStore.EnumTargetSetConditions(typeof(Database), PolicyStore.EnumerationMode.SystemOnly);
                        facetPolicies = policyStore.EnumPoliciesOnFacet(nameof(Table), PolicyStore.EnumerationMode.SystemOnly);
                        facetConditions = policyStore.EnumConditionsOnFacet(nameof(Table), PolicyStore.EnumerationMode.NonSystemOnly);
                        Assert.Multiple(() =>
                        {
                            Assert.That(policy.IsSystemObject, Is.True, "policy.IsSystemObject after MarkSystemObject");
                            Assert.That(databaseNameCondition.IsSystemObject, Is.True, "condition.IsSystemObject after MarkSystemObject");
                            Assert.That(objectSet.IsSystemObject, Is.True, "objectSet.IsSystemObject after MarkSystemObject");
                            Assert.That(systemConditions.Cast<string>(), Has.Member(databaseNameCondition.Name), "system Conditions should include the condition");
                            Assert.That(nonSystemConditions.Cast<string>(), Has.No.Member(databaseNameCondition.Name), "nonsystem Conditions should exclude the condition");
                            Assert.That(facetPolicies.Cast<string>(), Has.Member(policy.Name), "EnumFacetPolicies SystemOnly should include the policy");
                            Assert.That(facetConditions.Cast<string>(), Has.Member(tableNameCondition.Name), "EnumFacetConditions NonSystemOnly should include the condition");
                        });
                    }
                    finally
                    {
                        //Clean up test objects, note the order is important since the
                        //policy uses the object set and a condition and the object set
                        //uses the conditions
                        SmoObjectHelpers.SafeDrop(policy, objectSet, policyCondition, tableNameCondition, databaseNameCondition);
                    }

                });

        }

        /// <summary>
        /// SMO versions on server 2016 and prior have a defect handling database names with quotes
        /// in this scenario, so just testing on 2017 and newer (see bug 9731281)
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14, MaxMajor = 14, HostPlatform = "Windows")]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express)]
        public void Server_blocks_sproc_creation_based_on_policy_140()
        {
            Test_creation_policy_impl();
        }

        /// <summary>
        /// Regression test for 12143605
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15, HostPlatform = "Windows")]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlManagedInstance)]
        public void Server_blocks_sproc_creation_based_on_policy_150_plus()
        {
            Test_creation_policy_impl();
        }

        private void Test_creation_policy_impl()
        {
            Condition policyCondition = null, databaseNameCondition = null;
            ObjectSet objectSet = null;
            Policy policy = null;
            ExecuteWithDbDrop((db) =>
            {

                var policyStore = new PolicyStore(
                    new SqlStoreConnection(
                        new SqlConnection(this.SqlConnectionStringBuilder.ConnectionString)));
                try
                {
                    // Condition for sprocs with name not starting with sp
                    policyCondition = policyStore.CreateCondition(
                    facet: "StoredProcedure",
                    expressionNodeExpression: "@Name != 'spBlock'",
                    conditionNamePrefix: "spNameCondition");

                    //Create the condition we'll use to filter out the database name for the object set
                    databaseNameCondition = policyStore.CreateCondition(
                        facet: "Database",
                        expressionNodeExpression: string.Format("@Name = '{0}'", Urn.EscapeString(db.Name)),
                        conditionNamePrefix: "Database_Name_Condition");

                    // scope evaluation to just our database
                    objectSet = policyStore.CreateObjectSet(
                        facet: "StoredProcedure",
                        targetSetsAndLevelConditions: new Dictionary<string, IList<Tuple<string, string>>>()
                        {
                            {
                                "Server/Database/StoredProcedure",
                                new[] {new Tuple<string, string>("Server/Database", databaseNameCondition.Name),}
                            }
                        });

                    policy = policyStore.CreatePolicy(policyCondition.Name, AutomatedPolicyEvaluationMode.Enforce,
                        objectSet.Name, policyNamePrefix: "sproc_block_policy");
                    policy.Enabled = true;
                    policy.Alter();
                    var sproc = new StoredProcedure(db, "nameAllowed") { TextBody = "print 12" };
                    sproc.TextHeader = String.Format("CREATE PROCEDURE {0} AS", SmoObjectHelpers.SqlBracketQuoteString(sproc.Name));
                    Assert.DoesNotThrow(sproc.Create, "nameAllowed should be created");
                    var policyString = new System.Text.StringBuilder();
                    policyStore.CreatePolicyFromFacet(sproc, nameof(StoredProcedure), "policy", "condition", XmlWriter.Create(policyString));
                    Assert.That(policyString.ToString(), Is.Not.Empty, "policy xml");
                    var objectPolicy = policyStore.DeserializePolicy(XmlReader.Create(new System.IO.StringReader(policyString.ToString())), overwriteExistingPolicy: true, overwriteExistingCondition: true);
                    Assert.Multiple(() =>
                    {
                        Assert.That(objectPolicy.UsesFacet(nameof(StoredProcedure)), Is.True, "UsesFacet");
                        Assert.That(objectPolicy.Name, Is.EqualTo("policy"), "deserialized policy name");
                        Assert.That(objectPolicy.Condition, Is.EqualTo("condition"), "deserialized policy condition");
                    });
                    var policySet = policyStore.EnumApplicablePolicies(new SfcQueryExpression(sproc.Urn));
                    Assert.That(policySet.Rows.Cast<DataRow>().Select(r => r["Name"]), Is.EqualTo(new[] { policy.Name }), "EnumApplicablePolicies");
                    sproc = new StoredProcedure(db, "spBlock") { TextBody = "print 1" };
                    sproc.TextHeader = String.Format("CREATE PROCEDURE {0} AS", SmoObjectHelpers.SqlBracketQuoteString(sproc.Name));
                    var e = Assert.Throws<Management.Smo.FailedOperationException>(sproc.Create, "spBlock should fail");
                    Exception innermostException = e;
                    while (innermostException.InnerException != null)
                    {
                        innermostException = innermostException.InnerException;
                    }
                    Assert.That(innermostException, Is.InstanceOf(typeof(SqlException)), "InnerException:{0}", innermostException);
                    var sqlException = (SqlException)innermostException;
                    Assert.That(sqlException.Number, Is.EqualTo(3609), "SqlException.Number");
                    Assert.That(policy.EvaluationHistories.Count, Is.EqualTo(2), "EvaluationHistories.Count");

                }
                finally
                {
                    //Clean up test objects, note the order is important since the
                    //policy uses the object set and a condition and the object set
                    //uses the conditions
                    SmoObjectHelpers.SafeDrop(policy, objectSet, policyCondition, databaseNameCondition);
                }

            });
        }

        /// <summary>
        /// Make sure we can create a Condition for each Facet. 
        /// We handle failures related to new facets that don't exist on old versions
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 12)]
        public void Facets_are_all_assignable_to_a_condition()
        {
            ExecuteTest(() =>
            {
                Assert.Multiple(() =>
                {
                    var ps = new PolicyStore
                    {
                        SqlStoreConnection = new SqlStoreConnection(ServerContext.ConnectionContext.SqlConnectionObject)
                    };

                    foreach (Type facet in FacetRepository.RegisteredFacets)
                    {
                        var drop = false;
                        var f = facet; // avoid loop variable capture
                        var pi = FacetRepository.GetFacetProperties(f)[0];
                        var value = GenerateValue(pi.PropertyType);
                        Trace.TraceInformation("Property: '{0}'; type: '{1}'; value: '{2}'", pi.Name, pi.PropertyType, value);
                        ExpressionNode node = new ExpressionNodeOperator(OperatorType.EQ,
                            new ExpressionNodeAttribute(pi.Name, f),
                            ExpressionNodeConstant.ConstructNode(value));
                        var name = "con" + pi.Name + Guid.NewGuid();
                        var c = new Condition(ps, name)
                        {
                            ExpressionNode = node,
                            Facet = f.Name
                        };
                        try
                        {
                            c.Create();
                            drop = true;
                            Assert.That(ps.Conditions.Select(cond => cond.Name), Has.Member(c.Name),
                                "PolicyStore.Conditions should have new Condition after Create");
                        }
                        catch (SfcCRUDOperationFailedException ef)
                        {
                            Trace.TraceInformation("Facet: {0} Server:{1} Error: {2}", f.Name, ps.Name, ef);
                            Assert.That(ef.InnerException, Is.InstanceOf(typeof(ExecutionFailureException)), "ef.InnerException");

                            if  (ef.InnerException is ExecutionFailureException ex)
                            {
                                Assert.That(ex.InnerException, Is.InstanceOf(typeof(SqlException)), "Only SqlException should be thrown");
                                if (ex.InnerException is SqlException sqlEx)
                                {
                                    Assert.That(sqlEx.Number, Is.EqualTo(34014), "Only Facet doesn't exist should be thrown");
                                }                
                            }
                        }
                        finally
                        {
                            if (drop)
                            {
                                c.Drop();
                            }
                        }
                    }
                });
            });
        }    

        /// <summary>
        /// Create a test value of the given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object GenerateValue(Type type)
        {
            object value = null;

            if (type.IsEnum)
            {
                value = 1;
            }
            else if (type == typeof(Boolean))
            {
                value = true;
            }
            else if (type == typeof(int))
            {
                value = 1;
            }
            else if (type == typeof(byte))
            {
                value = (byte)2;
            }
            else if (type == typeof(short))
            {
                value = (short)3;
            }
            else if (type == typeof(long))
            {
                value = (long)4;
            }
            else if (type == typeof(double))
            {
                value = (double)5.0;
            }
            else if (type == typeof(float))
            {
                value = (float)6.0;
            }
            else if (type == typeof(decimal))
            {
                value = (decimal)7.0;
            }
            else if (type == typeof(string))
            {
                value = "string";
            }
            else if (type == typeof(char))
            {
                value = 'c';
            }
            else if (type == typeof(DateTime))
            {
                value = DateTime.Parse("11/6/207 12:04");
            }
            else if (type == typeof(System.Guid))
            {
                value = System.Guid.Empty;
            }
            else if (type == typeof(int[]))
            {
                value = new int[] { 1, 2, 3 };
            }
            else if (type == typeof(Byte[]))
            {
                value = new Byte[] { 4, 5, 6 };
            }
            else if (type == typeof(short[]))
            {
                value = new short[] { 7, 8, 9 };
            }
            else if (type == typeof(long[]))
            {
                value = new long[] { 10, 11, 12 };
            }
            else if (type == typeof(double[]))
            {
                value = new double[] { 13.0, 14.0, 15.0 };
            }
            else if (type == typeof(float[]))
            {
                value = new float[] { 16.0f, 17.0f, 18.0f };
            }
            else if (type == typeof(decimal[]))
            {
                value = new decimal[] { 19.0m, 20.0m, 21.0m };
            }
            else if (type == typeof(string[]))
            {
                value = new string[] { "str1", "str2", "str3" };
            }
            else if (type == typeof(char[]))
            {
                value = new char[] { 'a', 'b', 'c' };
            }

            return value;
        }

    }
}
