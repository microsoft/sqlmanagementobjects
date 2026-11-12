// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Microsoft.SqlServer.Management.Smo;
using Assert = NUnit.Framework.Assert;
using System.Threading.Tasks;


namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Database Role properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public class DatabaseRole_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(SqlSmoObject obj, SqlSmoObject objVerify)
        {
            var roleDb = (DatabaseRole)obj;
            var database = (Database)objVerify;

            database.Roles.Refresh();
            Assert.IsNull(database.Roles[roleDb.Name],
                          "Current role not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a database role with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_DatabaseRole_Sql16AndAfterOnPrem()
        {
            ExecuteFromDbPool(
                database =>
                {
                    var roleDb = new DatabaseRole(database, GenerateSmoObjectName("role"));

                    string roleScriptDropIfExistsTemplate = "DROP ROLE IF EXISTS [{0}]";
                    string roleScriptDropIfExists = string.Format(roleScriptDropIfExistsTemplate, roleDb.Name);

                    VerifySmoObjectDropIfExists(roleDb, database, roleScriptDropIfExists);
                });
        }

        #endregion

        /// <summary>
        /// Creates a two-level database role membership hierarchy for tests:
        /// outerRole  -- has direct member -->  nestedRole  -- has direct member -->  nestedUser
        /// </summary>
        private (DatabaseRole outerRole, DatabaseRole nestedRole, User nestedUser) SetupNestedRoleHierarchy(Database database)
        {
            var outerRole = new DatabaseRole(database, GenerateSmoObjectName("outerRole"));
            var nestedRole = new DatabaseRole(database, GenerateSmoObjectName("nestedRole"));
            var nestedUser = new User(database, GenerateSmoObjectName("nestedUser"))
            {
                UserType = UserType.NoLogin
            };

            outerRole.Create();
            nestedRole.Create();
            nestedUser.Create();

            // Make nestedRole a DIRECT member of outerRole.
            outerRole.AddMember(nestedRole.Name);
            // Make nestedUser a member of nestedRole only -> transitive to outerRole.
            nestedRole.AddMember(nestedUser.Name);

            return (outerRole, nestedRole, nestedUser);
        }

        /// <summary>
        /// Tests that EnumMembers returns both direct user members and nested role members.
        /// </summary>
        [TestMethod]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void DatabaseRole_EnumMembers_ReturnsDirectAndNestedRoleMembers()
        {
            ExecuteFromDbPool(
                database =>
                {
                    var (outerRole, nestedRole, nestedUser) = SetupNestedRoleHierarchy(database);

                    // Act
                    var members = outerRole.EnumMembers();

                    // Assert: both the directly-added nestedRole and the transitive nestedUser
                    // should be in the member list, because EnumMembers expands recursively.
                    Assert.That(members, Is.Not.Null, "EnumMembers should not return null");
                    Assert.That(members, Is.EquivalentTo(new[] { nestedRole.Name, nestedUser.Name }),
                        "EnumMembers should return the direct and transitive members");
                });
        }

        /// <summary>
        /// Tests that EnumMembersAsync returns both direct user members and nested role members.
        /// </summary>
        [TestMethod]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public async Task DatabaseRole_EnumMembersAsync_ReturnsDirectAndNestedRoleMembers()
        {
            await ExecuteFromDbPoolAsync(
                async database =>
                {
                    var (outerRole, nestedRole, nestedUser) = SetupNestedRoleHierarchy(database);

                    // Act
                    var members = await outerRole.EnumMembersAsync();

                    // Assert: both the directly-added nestedRole and the transitive nestedUser
                    // should be in the member list, because EnumMembersAsync expands recursively.
                    Assert.That(members, Is.Not.Null, "EnumMembersAsync should not return null");
                    Assert.That(members, Is.EquivalentTo(new[] { nestedRole.Name, nestedUser.Name }),
                        "EnumMembersAsync should return the direct and transitive members");
                });
        }

        /// <summary>
        /// Tests that EnumDirectMembersAsync returns only the direct members of the role, and does NOT recursively include nested members.
        /// </summary>
        [TestMethod]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public async Task DatabaseRole_EnumDirectMembersAsync_ReturnsOnlyDirectMembersNotNestedMembers()
        {
            await ExecuteFromDbPoolAsync(
                async database =>
                {
                    var (outerRole, nestedRole, nestedUser) = SetupNestedRoleHierarchy(database);

                    // Act
                    var directMembers = await outerRole.EnumDirectMembersAsync();

                    // Assert: only nestedRole should appear; nestedUser must NOT appear,
                    // because it is only a transitive (nested) member of outerRole.
                    Assert.That(directMembers, Is.Not.Null, "EnumDirectMembersAsync should not return null");
                    Assert.That(directMembers, Is.EquivalentTo(new[] { nestedRole.Name }),
                        "EnumDirectMembersAsync should return only the direct member, not transitive members");
                });
        }
    }
}
