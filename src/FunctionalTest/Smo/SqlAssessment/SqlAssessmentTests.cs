// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.IO;
using System.Linq;
using Microsoft.SqlServer.Management.Assessment;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.SqlAssessment
{
    /// <summary>
    /// SQL Assessment extensions for SMO tests
    /// </summary>
    [TestClass]
    public class AssessmentTests : SqlTestBase
    {

        /// <summary>
        /// Definition for a check which always fails for any target SQL server instance or database.
        /// </summary>
        private const string FailingCheckJson = @"{
            'name': 'Failing Check',
            'version': '1.0',
            'schemaVersion': '1.0',
            'rules':[
                {
                    'id': 'IntentionallyFails',
                    'itemType': 'definition',
                    'displayName': 'Intentionally failing',
                    'description': 'This check intentionally fails.',
                    'message': 'Failed as expected.',
                    'target': { 'type': 'Server, Database' },
                    'condition': false
                }
            ]
        }";

        /// <summary>
        /// Adds special checks for testing
        /// </summary>
        [TestInitialize]
        public void Init()
        {
            using (var strReader = new StringReader(FailingCheckJson))
            {
                SqlAssessmentExtensions.Engine.PushRuleFactoryJson(strReader);
            }
        }

        /// <summary>
        /// Tests that we can successfully get assessment items for an object
        /// </summary>
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [TestMethod]
        public void Server_TestGetSqlAssessmentItems()
        {
            ExecuteTest(testMethod: (Microsoft.SqlServer.Management.Smo.Server targetServer) =>
            {
                var actualItems = targetServer.GetAssessmentItems();
                Assert.That(actualItems, Is.Not.Null, "GetAssessmentItems returned null for the server.");
                Assert.That(actualItems, Is.Not.Empty, "GetAssessmentItems did not return any item for the server.");

                Assert.That(
                    actualItems.Select(c => c.Id),
                    Has.Member("IntentionallyFails"),
                    "GetAssessmentItems did not return the check that always fails.");
            });
        }

        /// <summary>
        /// Tests that we can successfully invoke assessment for an object
        /// </summary>
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [TestMethod]
        public void Server_TestGetAssessmentResultsList()
        {
            ExecuteTest(testMethod: (Microsoft.SqlServer.Management.Smo.Server targetServer) =>
            {
                var actualResultTask = targetServer.GetAssessmentResultsList();
                Assert.That(actualResultTask, Is.Not.Null, "GetAssessmentResultsList returned null.");

                var actualResults = actualResultTask.Result;
                Assert.That(actualResults, Is.Not.Null, "GetAssessmentResultsList task returned null.");
                Assert.That(actualResults, Is.Not.Empty, "GetAssessmentResultsList did not return any note for the server.");

                var resultsArray = actualResults.ToArray();
                if (IsUnsupportedTarget(resultsArray))
                {
                    return;
                }

                Assert.That(resultsArray.Select(r => r.Message), Has.Member("Failed as expected."),
                    "GetAssessmentResultsList did not return the result for the always failing check (server).");

                foreach (var item in resultsArray)
                {
                    Assert.That(item.TargetType, Is.EqualTo(SqlObjectType.Server), 
                        "GetAssessmentResultsList returned an object targeted at {0} for a server.", 
                        item.TargetType.ToString());
                }
            });
        }

        /// <summary>
        /// Tests that we can successfully invoke assessment for an object
        /// </summary>
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [TestMethod]
        public void Server_TestGetAssessmentResults()
        {
            ExecuteTest(testMethod: (Microsoft.SqlServer.Management.Smo.Server targetServer) =>
            {
                var actualResults = targetServer.GetAssessmentResults().ToArray();
                Assert.That(actualResults, Is.Not.Empty, "GetAssessmentResults did not return any note for the server.");

                if (IsUnsupportedTarget(actualResults))
                {
                    return;
                }

                Assert.That(actualResults.Select(r => r.Message), Has.Member("Failed as expected."),
                    "GetAssessmentResults did not return the result for the always failing check (server).");
                foreach (var item in actualResults)
                {
                    Assert.That(item.TargetType, Is.EqualTo(SqlObjectType.Server),
                        "GetAssessmentResults returned an object targeted at {0} for a server.",
                        item.TargetType.ToString());
                }
            });
        }

        /// <summary>
        /// Tests that we can successfully invoke assessment for a database
        /// </summary>
        [SqlTestCategory(SqlTestCategory.NoRegression)]
        [TestMethod]
        public void Database_TestGetAssessmentResults()
        {
            ExecuteTest(testMethod: (Microsoft.SqlServer.Management.Smo.Server targetServer) =>
            {
                var targetDatabase = targetServer.Databases["master"];
                var actualResults = targetDatabase.GetAssessmentResults().ToArray();
                Assert.That(actualResults, Is.Not.Empty, "GetAssessmentResults did not return any note for the database.");

                if (IsUnsupportedTarget(actualResults))
                {
                    return;
                }

                Assert.That(actualResults.Select(r => r.Message), Has.Member("Failed as expected."),
                    "GetAssessmentResults did not return the result for the always failing check (database).");
                foreach (var item in actualResults)
                {
                    Assert.That(item.TargetType, Is.EqualTo(SqlObjectType.Database),
                        "GetAssessmentResults returned an object targeted at {0} for the database.",
                        item.TargetType.ToString());
                }
            });
        }

        /// <summary>
        /// This method checks whether the assessment engine
        /// detected an unsupported target.
        /// </summary>
        /// <param name="actualItems">Assessment results returned by the engine.</param>
        /// <returns>Returns true</returns>
        private bool IsUnsupportedTarget(IAssessmentResult[] actualResults)
        {
            return (actualResults[0] is IAssessmentError err)
                   && err.Message.Contains("VIEW SERVER STATE");
        }
    }
}
