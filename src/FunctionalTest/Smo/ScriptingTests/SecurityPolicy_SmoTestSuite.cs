// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// SMO scripting Security Policy TestSuite.
    /// </summary>
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public partial class SecurityPolicy_SmoTestSuite : SqlTestBase
    {
        /// <summary>
        /// Tests that security policies containing only filter predicates can be created altered and dropped successfully.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void VerifySecPolicyFilterPredCreateAlterDrop()
        {
            string schemaName = "rls";
            string[] tableNames = { "t1", "t2", "t3", "t4" };
            Tuple<string, SqlDataType>[] columns = { new Tuple<string, SqlDataType>("testColumn1", SqlDataType.Int), new Tuple<string, SqlDataType>("testColumn2", SqlDataType.BigInt) };
            string functionName = "f1";
            
            this.ExecuteWithDbDrop(
                database =>
            {
                SetupDatabaseForSecurityPolicyTests(database, schemaName, tableNames, columns, functionName);

                VerifySecurityPolicyCreateAlterDrop(database, schemaName, tableNames, columns, functionName, SecurityPredicateType.Filter, SecurityPredicateOperation.All);
            });
        }

        /// <summary>
        /// Tests security policies with both block and filter predicates can be created altered and dropped successfully.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void VerifySecPolicyBlockPredCreateAlterDrop()
        {
            string schemaName = "rls";
            string[] tableNames = { "t1", "t2", "t3", "t4" };
            Tuple<string, SqlDataType>[] columns = { new Tuple<string, SqlDataType>("testColumn1", SqlDataType.Int), new Tuple<string, SqlDataType>("testColumn2", SqlDataType.BigInt) };
            string functionName = "f1";

            this.ExecuteWithDbDrop(database =>
            {
                database.Parent.SetDefaultInitFields(typeof(ExtendedProperty), true);
                database.Parent.SetDefaultInitFields(typeof(SecurityPredicate), true);
                database.Parent.SetDefaultInitFields(typeof(SecurityPolicy), true);
                SetupDatabaseForSecurityPolicyTests(database, schemaName, tableNames, columns, functionName);

                VerifySecurityPolicyCreateAlterDrop(database, schemaName, tableNames, columns, functionName, SecurityPredicateType.Block, SecurityPredicateOperation.All);
                VerifySecurityPolicyCreateAlterDrop(database, schemaName, tableNames, columns, functionName, SecurityPredicateType.Block, SecurityPredicateOperation.AfterInsert);
                VerifySecurityPolicyCreateAlterDrop(database, schemaName, tableNames, columns, functionName, SecurityPredicateType.Block, SecurityPredicateOperation.AfterUpdate);
                VerifySecurityPolicyCreateAlterDrop(database, schemaName, tableNames, columns, functionName, SecurityPredicateType.Block, SecurityPredicateOperation.BeforeDelete);
                VerifySecurityPolicyCreateAlterDrop(database, schemaName, tableNames, columns, functionName, SecurityPredicateType.Block, SecurityPredicateOperation.BeforeUpdate);
            });
        }

        /// <summary>
        /// Tests dropping a security policy with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_SecPolicy_Sql16AndAfterOnPrem()
        {
            string schemaName = "testSchema";
            string[] tableNames = { "testTable" };
            Tuple<string, SqlDataType>[] columns = { new Tuple<string, SqlDataType>("testColumn", SqlDataType.Int) };
            string functionName = "testFunc";
            string secPolName = "secPol";
            string predicateDefinition = String.Format("[{0}].[{1}]([{2}])", schemaName, functionName, columns[0].Item1);

            this.ExecuteWithDbDrop(
                database =>
                {
                    Database testDb = SetupDatabaseForSecurityPolicyTests(database, schemaName, tableNames, columns, functionName);
                    Table testTbl = testDb.Tables[tableNames[0], schemaName];

                    SecurityPolicy secPol = new SecurityPolicy(testDb, secPolName, schemaName, notForReplication: true, isEnabled: true);
                    SecurityPredicate secPredicate = new SecurityPredicate(secPol, schemaName, tableNames[0], testTbl.ID, predicateDefinition);
                    secPredicate.PredicateType = SecurityPredicateType.Block;
                    secPredicate.PredicateOperation = SecurityPredicateOperation.All;
                    secPol.SecurityPredicates.Add(secPredicate);

                    // 1. Try to drop security policy before it is created.
                    //
                    secPol.DropIfExists();

                    secPol.Create();

                    // 2. Verify the script contains expected statement.
                    //
                    ScriptingOptions so = new ScriptingOptions();
                    so.IncludeIfNotExists = true;
                    so.ScriptDrops = true;
                    StringCollection col = secPol.Script(so);

                    StringBuilder sb = new StringBuilder();
                    StringBuilder scriptTemplate = new StringBuilder();
                    foreach (string statement in col)
                    {
                        sb.AppendLine(statement);
                    }
                    string dropSecPolicyIfExistsScripts = sb.ToString();
                    string secPolicyScriptDropIfExistsTemplate = "DROP SECURITY POLICY IF EXISTS [{0}].[{1}]";
                    scriptTemplate.Append(string.Format(secPolicyScriptDropIfExistsTemplate, schemaName, secPol.Name));

                    Assert.IsTrue(dropSecPolicyIfExistsScripts.Contains(scriptTemplate.ToString()),
                                  "Drop with existence check is not scripted.");

                    // 3. Drop security policy with DropIfExists and check if it is dropped.
                    //
                    secPol.DropIfExists();
                    database.SecurityPolicies.ClearAndInitialize(null, null);;
                    Assert.IsNull(database.SecurityPolicies[secPol.Name, schemaName],
                                  "Current index not dropped with DropIfExists.");

                    // 4. Try to drop already dropped security policy.
                    //
                    secPol.DropIfExists();
                });
        }

        /// <summary>
        /// Tests that we can add granular block predicates and drop a global predicate from the security policy in a single alter, and vice versa.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void VerifyAddDropBlockPredicateGranularity()
        {
            string schemaName = "rls";
            string[] tableNames = { "t1" };
            Tuple<string, SqlDataType>[] columns = { new Tuple<string, SqlDataType>("testColumn1", SqlDataType.Int), new Tuple<string, SqlDataType>("testColumn2", SqlDataType.BigInt) };
            string functionName = "f1";
            string secPolName = "secPol";
            string predicateDefinition = String.Format("[{0}].[{1}]([{2}])", schemaName, functionName, columns[0].Item1);
            SecurityPredicateOperation[] granularOps = { SecurityPredicateOperation.AfterInsert, SecurityPredicateOperation.AfterUpdate, SecurityPredicateOperation.BeforeDelete, SecurityPredicateOperation.BeforeUpdate };
            TypeConverter securityPredicateOperationConverter = TypeDescriptor.GetConverter(typeof(SecurityPredicateOperation));

            this.ExecuteWithDbDrop(database =>
            {
                Database testDb = SetupDatabaseForSecurityPolicyTests(database, schemaName, tableNames, columns, functionName);
                Table testTbl = testDb.Tables[tableNames[0], schemaName];

                // Create a security policy with a global block predicate on t1.
                //
                SecurityPolicy secPol = new SecurityPolicy(testDb, secPolName, schemaName, notForReplication: true, isEnabled: true);
                SecurityPredicate globalPredicate = new SecurityPredicate(secPol, schemaName, tableNames[0], testTbl.ID, predicateDefinition);
                globalPredicate.PredicateType = SecurityPredicateType.Block;
                globalPredicate.PredicateOperation = SecurityPredicateOperation.All;
                secPol.SecurityPredicates.Add(globalPredicate);
                secPol.Create();

                // Verify the predicate was created properly.
                //
                secPol.SecurityPredicates.ClearAndInitialize(null, null);;
                Assert.AreEqual(1, secPol.SecurityPredicates.Count, "Incorrect number of security predicates after creation");
                globalPredicate = secPol.SecurityPredicates[0];
                Assert.AreEqual(SecurityPredicateType.Block, globalPredicate.PredicateType, "Security predicate created was not a block predicate.");
                Assert.AreEqual(SecurityPredicateOperation.All, globalPredicate.PredicateOperation, "Security predicate created was not a global block predicate.");

                // Now mark the globl predicate for drop, and add granular predicates for each predicate operation.
                //
                globalPredicate.MarkForDrop(true);
                foreach (SecurityPredicateOperation opType in granularOps)
                {
                    SecurityPredicate granularPredicate = new SecurityPredicate(secPol, schemaName, tableNames[0], testTbl.ID, predicateDefinition);
                    granularPredicate.PredicateType = SecurityPredicateType.Block;
                    granularPredicate.PredicateOperation = (SecurityPredicateOperation)opType;
                    secPol.SecurityPredicates.Add(granularPredicate);
                }

                // Perform the alter, and verify the results.
                //
                secPol.Alter();
                secPol.SecurityPredicates.ClearAndInitialize(null, null);;
                Assert.AreEqual(granularOps.Length, secPol.SecurityPredicates.Count, "Incorrect number of security predicates after creation.");

                // Verify a granular block predicate for each operation exists, and mark each for drop.
                //
                foreach (SecurityPredicateOperation opType in granularOps)
                {
                    SecurityPredicate granularPredicate = secPol.SecurityPredicates.GetItemByTargetObjectID(testTbl.ID, SecurityPredicateType.Block, opType);
                    Assert.IsNotNull(granularPredicate, "Did not create a security predicate for operation {0}", securityPredicateOperationConverter.ConvertToInvariantString(opType));
                    granularPredicate.MarkForDrop(true);
                }

                // Add the global block predicate back.
                //
                globalPredicate = new SecurityPredicate(secPol, schemaName, tableNames[0], testTbl.ID, predicateDefinition);
                globalPredicate.PredicateType = SecurityPredicateType.Block;
                globalPredicate.PredicateOperation = SecurityPredicateOperation.All;
                secPol.SecurityPredicates.Add(globalPredicate);

                // Verify the global block predicate once more.
                //
                secPol.Alter();
                secPol.SecurityPredicates.ClearAndInitialize(null, null);;
                Assert.AreEqual(1, secPol.SecurityPredicates.Count, "Incorrect number of security predicates after creation");
                globalPredicate = secPol.SecurityPredicates.GetItemByTargetObjectID(testTbl.ID, SecurityPredicateType.Block, SecurityPredicateOperation.All);
                Assert.AreEqual(SecurityPredicateType.Block, globalPredicate.PredicateType, "Security predicate created was not a block predicate.");
                Assert.AreEqual(SecurityPredicateOperation.All, globalPredicate.PredicateOperation, "Security predicate created was not a global block predicate.");
            });
        }

        /// <summary>
        /// Sets up the test database for testing security policy scripting.
        /// </summary>
        /// <param name="database">The test database</param>
        /// <param name="schemaName">The test schema name</param>
        /// <param name="tableNames">The array of test table names</param>
        /// <param name="columns">The array of test columns</param>
        /// <param name="functionName">The test function name.</param>
        /// <returns>The generated test database.</returns>
        private Database SetupDatabaseForSecurityPolicyTests(
            Database database,
            string schemaName,
            string[] tableNames,
            Tuple<string, SqlDataType>[] columns,
            string functionName)
        {
            Schema sch = new Schema(database, schemaName);
            sch.Create();
            UserDefinedFunction function = new UserDefinedFunction(database, functionName, schemaName);
            function.TextHeader = String.Format("CREATE FUNCTION {0}.{1} (@x BIGINT) RETURNS TABLE WITH SCHEMABINDING AS", schemaName, functionName);
            function.TextBody = "return select 1 as is_visible";
            function.Create();

            foreach (string tableName in tableNames)
            {
                var tab = new Table(database, tableName, schemaName);

                foreach (Tuple <string, SqlDataType> column in columns)
                {
                    Column col = new Column(tab, column.Item1, new DataType(column.Item2));
                    tab.Columns.Add(col);
                }

                tab.Create();
            }

            database.Tables.Refresh();
            Assert.IsNotNull(database.Tables[tableNames[0], schemaName]);
            return database;
        }

        /// <summary>
        /// Tests scripting, creating, altering, and dropping of security policies and predicates via SMO.
        /// Test steps:
        /// 1. Create a Security Policy with a simple predicate of the specified type and two extended properties.
        /// 2. Script the Security policy with IncludeIfNotExists to true.
        /// 3. Drop the Security policy, and verify that the policy has been dropped.
        /// 4. Verify that the script contains the expected information.
        /// 5. Run the script and verify the Security Policy is re-created.
        /// 6. Validate that altering a policy, and creating an additional predicate of the specified type behaves as expected.
        /// 7. Validate that altering a security predicate behaves as expected.
        /// 8. Validate that a multi-operation alter on a security policy behaves as expected.
        /// 9. Test that dropping a security predicate, and then a policy behaves as epected.
        /// 10. Repeat steps 1-9 for all variants of notForReplication isEnabled.
        /// </summary>
        /// <param name="database">The test database</param>
        /// <param name="schemaName">The test schema name</param>
        /// <param name="tableNames">The array of test table names</param>
        /// <param name="columns">The array of test columns</param>
        /// <param name="functionName">The test function name.</param>
        /// <param name="predicateOperation">The security predicate operation</param>
        /// <param name="spType">The security predicate type</param>
        private void VerifySecurityPolicyCreateAlterDrop(
            Database database,
            string schemaName,
            string[] tableNames,
            Tuple<string, SqlDataType>[] columns,
            string functionName,
            SecurityPredicateType spType,
            SecurityPredicateOperation predicateOperation)
        {
            for (int notForReplication = 0; notForReplication <= 1; notForReplication++)
            {
                for (int isEnabled = 0; isEnabled <= 1; isEnabled++)
                {
                    for(int isSchemaBound = 0; isSchemaBound <=1; isSchemaBound++)
                    {
                        VerifySecurityPolicyCreateAlterDropHelper(
                            database,
                            schemaName,
                            tableNames,
                            columns,
                            functionName,
                            notForReplication != 0,
                            isEnabled != 0,
                            isSchemaBound != 0,
                            spType,
                            predicateOperation);
                    }
                }
            }
        }

        /// <summary>
        /// Helper Method that implements the logic for VerifySecurityPolicyCreateAlterDrop, to easily test different variants
        /// </summary>
        /// <param name="database">The test database</param>
        /// <param name="schemaName">The test schema name</param>
        /// <param name="tableNames">The array of test table names</param>
        /// <param name="columns">The array of test columns</param>
        /// <param name="functionName">The test function name.</param>
        /// <param name="notForReplication">Whether the unaltered policy should be marked for replication</param>
        /// <param name="isEnabled">Whether the unaltered policy should be marked as enabled</param>
        /// <param name="isSchemaBound">Whether the policy should be marked as schema bound</param>
        /// <param name="spType">The security predicate type</param>
        /// <param name="predicateOperation">The security predicate operation type</param>
        private void VerifySecurityPolicyCreateAlterDropHelper(
            Database database,
            string schemaName,
            string[] tableNames,
            Tuple<string, SqlDataType>[] columns,
            string functionName,
            bool notForReplication,
            bool isEnabled,
            bool isSchemaBound,
            SecurityPredicateType spType,
            SecurityPredicateOperation predicateOperation)
        {
            string secPolName = "secPol1";
            string unalteredPredicateDefinition = isSchemaBound ?
                String.Format("[{0}].[{1}]([{2}])", schemaName, functionName, columns[0].Item1) 
                : String.Format("[{0}]([{1}])", functionName, columns[0].Item1); 
            string alteredPredicateDefinition = String.Format("[{0}].[{1}]([{2}])", schemaName, functionName, columns[1].Item1);
            string securityPolicyCountQuery = @"SELECT COUNT(*) FROM sys.security_policies";
            string securityPredicateCountQuery = @"SELECT COUNT(*) FROM sys.security_predicates";
            string alteredSecurityPredicateCountQuery =
                String.Format(@"{0} WHERE predicate_definition = '({1})'", securityPredicateCountQuery, alteredPredicateDefinition);
            string unalteredSecurityPolicyCountQuery =
                String.Format(@"SELECT COUNT(*) from sys.security_policies WHERE is_enabled = '{0}' AND is_not_for_replication = '{1}'", isEnabled, notForReplication);
            string alteredSecurityPolicyCountQuery =
                String.Format(@"SELECT COUNT(*) from sys.security_policies WHERE is_enabled = '{0}' AND is_not_for_replication = '{1}'", !isEnabled, !notForReplication);
            TypeConverter securityPredicateTypeConverter = TypeDescriptor.GetConverter(typeof(SecurityPredicateType));
            TypeConverter securityPredicateOperationConverter = TypeDescriptor.GetConverter(typeof(SecurityPredicateOperation));

            try
            {
                // Step 1. Create a security policy with a simple predicate of the specified type and two extended properties.
                //
                SecurityPolicy secPol = new SecurityPolicy(database, secPolName, schemaName, notForReplication,
                    isEnabled) {IsSchemaBound = isSchemaBound};
                SecurityPredicate firstPredicate = new SecurityPredicate(secPol, database.Tables[tableNames[0], schemaName], unalteredPredicateDefinition);
                firstPredicate.PredicateOperation = predicateOperation;
                firstPredicate.PredicateType = spType;
                secPol.SecurityPredicates.Add(firstPredicate);
                secPol.ExtendedProperties.Add(new ExtendedProperty(secPol, "Ext prop1", "Ext prop value1"));
                secPol.ExtendedProperties.Add(new ExtendedProperty(secPol, "Ext prop2", "Ext prop value2"));
                secPol.Create();
                database.SecurityPolicies.ClearAndInitialize(null, null);
                secPol = database.SecurityPolicies[secPolName, schemaName];
                Assert.AreEqual(1, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(securityPolicyCountQuery), "Security policy was not created.");
                Assert.AreEqual(2, secPol.ExtendedProperties.Count, "No extended properties created");
                Assert.AreEqual(secPol.ExtendedProperties["Ext prop1"].Value, "Ext prop value1", "Created security policy does not have the correct extended properties");
                Assert.AreEqual(secPol.ExtendedProperties["Ext prop2"].Value, "Ext prop value2", "Created security policy does not have the correct extended properties");
                Assert.AreEqual(isSchemaBound, secPol.IsSchemaBound, "is_schema_bound property not set correctly.");
                secPol.SecurityPredicates.ClearAndInitialize(null, null);;
                firstPredicate = secPol.SecurityPredicates.GetItemByTargetObjectID(database.Tables[tableNames[0], schemaName].ID, spType, predicateOperation);
                Assert.AreEqual(spType, firstPredicate.PredicateType, "Security predicate was created with the wrong predicate type.");
                Assert.AreEqual(predicateOperation, firstPredicate.PredicateOperation, "Security predicate was created with the wrong operation type.");

                //Step 2. Script the Security policy with IncludeIfNotExists to true
                //
                ScriptingOptions so = new ScriptingOptions();
                so.IncludeIfNotExists = true;
                so.IncludeDatabaseContext = true;
                so.ExtendedProperties = true;
                StringCollection sc = secPol.Script(so);

                //Step 3. Drop the security policy, and verify that the count is 0.
                //
                secPol.Drop();
                database.SecurityPolicies.ClearAndInitialize(null, null);
                Assert.AreEqual(0, database.SecurityPolicies.Count);

                StringBuilder sb = new StringBuilder();
                foreach (string statement in sc)
                {
                    sb.AppendLine(statement);
                    TraceHelper.TraceInformation(statement);
                }
                string scripts = sb.ToString();

                //Step 4. Validate the script contains the expected information.
                //
                Assert.IsTrue(scripts.Contains(String.Format("CREATE SECURITY POLICY [{0}].[{1}]", schemaName, secPolName)));
                Assert.IsTrue(scripts.Contains(String.Format("ADD {0} PREDICATE {1} ON [{2}].[{3}]{4}",
                    securityPredicateTypeConverter.ConvertToInvariantString(spType),
                    unalteredPredicateDefinition,
                    schemaName,
                    tableNames[0],
                    (predicateOperation != SecurityPredicateOperation.All ? " " + securityPredicateOperationConverter.ConvertToInvariantString(predicateOperation) : ""))),
                    "Script did not recreate the predicate as expected.");

                if (notForReplication)
                {
                    Assert.IsTrue(scripts.Contains("NOT FOR REPLICATION"), "Script did not contain the proper NOT FOR REPLICATION clause");
                }

                Assert.IsTrue(scripts.Contains(String.Format("WITH (STATE = {0},", isEnabled ? "ON" : "OFF")), "Script did not contain the proper state option.");
                Assert.IsTrue(scripts.Contains(String.Format(" SCHEMABINDING = {0})", isSchemaBound ? "ON" : "OFF")), "Script did not contain the proper schema binding option");

                // Step 5. Verify the script recreates the security policy.
                //
                database.ExecuteNonQuery(scripts);
                database.SecurityPolicies.ClearAndInitialize(null, null);;
                secPol = database.SecurityPolicies[secPolName, schemaName];
                Assert.IsNotNull(secPol, "Security policy was not recreated by the script.");
                Assert.AreEqual(secPolName, secPol.Name, "Recreated security policy name does not match the original policy name.");
                Assert.AreEqual(notForReplication, secPol.NotForReplication, "Recreated security policy does not have the same value for not_for_replication.");
                Assert.AreEqual(isEnabled, secPol.Enabled, "Recreated security policy does not have the same value for is_enabled.");
                Assert.AreEqual(isSchemaBound, secPol.IsSchemaBound, "is_schema_bound property not set correctly.");
                Assert.AreEqual(2, secPol.ExtendedProperties.Count, "Recreated security policy does not have the correct number of extended properties");
                Assert.AreEqual(secPol.ExtendedProperties["Ext prop1"].Value, "Ext prop value1", "Recreated security policy does not have the correct extended properties");
                Assert.AreEqual(secPol.ExtendedProperties["Ext prop2"].Value, "Ext prop value2", "Recreated security policy does not have the correct extended properties");
                secPol.SecurityPredicates.ClearAndInitialize(null, null);
                firstPredicate = secPol.SecurityPredicates.GetItemByTargetObjectID(database.Tables[tableNames[0], schemaName].ID, spType, predicateOperation);
                Assert.AreEqual(spType, firstPredicate.PredicateType, "Security predicate was recreated with the wrong predicate type.");
                Assert.AreEqual(predicateOperation, firstPredicate.PredicateOperation, "Security predicate was recreated with the wrong operation type.");

                // Step 6. Add a new predicate to the policy via both SecurityPolicy.Alter and SecurityPredicate.Create
                //
                Assert.AreEqual(1, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar( securityPredicateCountQuery));
                SecurityPredicate secondPredicate = new SecurityPredicate(secPol, database.Tables[tableNames[1], schemaName], unalteredPredicateDefinition);
                secondPredicate.PredicateOperation = predicateOperation;
                secondPredicate.PredicateType = spType;
                secPol.SecurityPredicates.Add(secondPredicate);
                secPol.Alter();
                Assert.AreEqual(2, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(securityPredicateCountQuery));
                Assert.AreEqual(SqlSmoState.Existing, secondPredicate.State, "Security predicate state was not updated via SecPol.Alter().");

                SecurityPredicate thirdPredicate = new SecurityPredicate(secPol, database.Tables[tableNames[2], schemaName], unalteredPredicateDefinition);
                thirdPredicate.PredicateOperation = predicateOperation;
                thirdPredicate.PredicateType = spType;
                thirdPredicate.Create();
                secPol.SecurityPredicates.Refresh();
                Assert.AreEqual(3, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(securityPredicateCountQuery));
                Assert.AreEqual(3, secPol.SecurityPredicates.Count, "Security policy collection should contain 3 security predicates.");
                Assert.AreEqual(SqlSmoState.Existing, thirdPredicate.State, "Security predicate state was not updated via SecurityPredicate.Create().");

                // Step 7. Alter the predicate definitions for one of the predicates, and verify that alter works via SecurityPredicate.Alter
                //
                Assert.AreEqual(0, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(alteredSecurityPredicateCountQuery));
                firstPredicate = secPol.SecurityPredicates.GetItemByTargetObjectID(database.Tables[tableNames[0], schemaName].ID, spType, predicateOperation);
                firstPredicate.PredicateDefinition = alteredPredicateDefinition;
                firstPredicate.Alter();
                Assert.AreEqual(1, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(alteredSecurityPredicateCountQuery), "Unexpected number of predicates have been altered.");

                // Step 8. Perform a multi-operation SecurityPolicy.Alter including: Adding, dropping, and altering a predicate in a single security policy alter statement.
                // Note: the fourth predicate is always a filter predicate to test SMO supports multiple predicate types in a single policy.
                //
                Assert.AreEqual(1, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(unalteredSecurityPolicyCountQuery), "There should be exactly one unaltered security policy.");
                Assert.AreEqual(0, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(alteredSecurityPolicyCountQuery), "There should be no altered security policies.");

                SecurityPredicate fourthPredicate = new SecurityPredicate(secPol, database.Tables[tableNames[3], schemaName], unalteredPredicateDefinition);
                secPol.SecurityPredicates.Add(fourthPredicate);
                secondPredicate.PredicateDefinition = alteredPredicateDefinition;
                thirdPredicate.MarkForDrop(true);
                secPol.NotForReplication = !notForReplication;
                secPol.Enabled = !isEnabled;
                secPol.Alter();
                secPol.SecurityPredicates.Refresh();
                secPol.Refresh();
                Assert.Multiple(() =>
                {
                   Assert.That((int)database.ExecutionManager.ConnectionContext.ExecuteScalar(alteredSecurityPredicateCountQuery), Is.EqualTo(2), "Unexpected number of predicates have been altered.");
                   Assert.IsNull(secPol.SecurityPredicates.GetItemByTargetObjectID(database.Tables[tableNames[2], schemaName].ID, spType, predicateOperation), "Third predicate should have been dropped, but is still present in the collection");
                   Assert.IsNotNull(secPol.SecurityPredicates.GetItemByTargetObjectID(database.Tables[tableNames[3], schemaName].ID), "Fourth predicate should have been added, but is not present in the colletion");
                   Assert.AreEqual(SqlSmoState.Existing, fourthPredicate.State, "Fourth predicate should have a state of existing after creation.");
                   Assert.AreEqual(3, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(securityPredicateCountQuery), "The third predicate should have been dropped, and the fourth should have been added.");
                   Assert.AreEqual(0, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(unalteredSecurityPolicyCountQuery), "There should be no unaltered security policies.");
                   Assert.AreEqual(1, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(alteredSecurityPolicyCountQuery), "There should be exactly one altered security policy.");
                });
                // Step 9. Finally test that SecurityPredicate.Drop() and SecurityPolicy.Drop() behave as expected.
                //
                firstPredicate.Drop();
                secPol.SecurityPredicates.ClearAndInitialize(null, null);;
                Assert.AreEqual(2, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(securityPredicateCountQuery), "There should be 2 predicates present in the database after the explicit drop.");
                Assert.AreEqual(2, secPol.SecurityPredicates.Count, "Security policy collection should contain 2 security predicates after the explicit drop.");
                secPol.Drop();
                database.SecurityPolicies.ClearAndInitialize(null, null);;
                Assert.AreEqual(0, database.SecurityPolicies.Count, "There should be no security policies present in the database.");
                Assert.AreEqual(0, (int)database.ExecutionManager.ConnectionContext.ExecuteScalar(@"SELECT COUNT(*) FROM sys.security_policies"), "There should be no security policies present in the database.");
            }
            finally
            {
                database.SecurityPolicies.ClearAndInitialize(null, null);;
                SecurityPolicy secpol = database.SecurityPolicies[secPolName];

                if (secpol != null)
                {
                    secpol.Drop();
                }
            }
        }
    }
}
