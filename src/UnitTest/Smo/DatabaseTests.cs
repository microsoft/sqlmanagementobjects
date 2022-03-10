// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SmoUnitTests
{
    /// <summary>
    /// Tests for the Database object
    /// </summary>
    [TestClass]
    public class DatabaseTests : UnitTestBase
    {
        /// <summary>
        /// Set of collection types that have been validated by a SMO
        /// comitter as having updated the necessary parts of SMO to
        /// handle the collection correctly (mostly around scripting).
        /// See the AllDatabaseCollectionProperties_AreVerifiedAsHavingUpdateTransferCode
        /// test below for more details on how to update this.
        /// </summary>
        private readonly HashSet<Type> validatedCollections = new HashSet<Type>()
        {
            typeof(ApplicationRoleCollection),
            typeof(AsymmetricKeyCollection),
            typeof(CertificateCollection),
            typeof(ColumnEncryptionKeyCollection),
            typeof(ColumnMasterKeyCollection),
            typeof(DatabaseAuditSpecificationCollection),
            typeof(DatabaseDdlTriggerCollection),
            typeof(DatabaseRoleCollection),
            typeof(DatabaseScopedConfigurationCollection),
            typeof(DatabaseScopedCredentialCollection),
            typeof(DefaultCollection),
            typeof(ExtendedPropertyCollection),
            typeof(ExtendedStoredProcedureCollection),
            typeof(ExternalDataSourceCollection),
            typeof(ExternalFileFormatCollection),
            typeof(ExternalLanguageCollection),
            typeof(ExternalLibraryCollection),
            typeof(ExternalStreamCollection),
            typeof(ExternalStreamingJobCollection),
            typeof(FileGroupCollection),
            typeof(FullTextCatalogCollection),
            typeof(FullTextStopListCollection),
            typeof(LogFileCollection),
            typeof(PartitionFunctionCollection),
            typeof(PartitionSchemeCollection),
            typeof(PlanGuideCollection),
            typeof(RuleCollection),
            typeof(SchemaCollection),
            typeof(SearchPropertyListCollection),
            typeof(SecurityPolicyCollection),
            typeof(SequenceCollection),
            typeof(SqlAssemblyCollection),
            typeof(StoredProcedureCollection),
            typeof(SymmetricKeyCollection),
            typeof(SynonymCollection),
            typeof(TableCollection),
            typeof(UserCollection),
            typeof(UserDefinedAggregateCollection),
            typeof(UserDefinedDataTypeCollection),
            typeof(UserDefinedFunctionCollection),
            typeof(UserDefinedTableTypeCollection),
            typeof(UserDefinedTypeCollection),
            typeof(ViewCollection),
            typeof(WorkloadManagementWorkloadClassifierCollection),
            typeof(WorkloadManagementWorkloadGroupCollection),
            typeof(XmlSchemaCollectionCollection),
        };

        /// <summary>
        /// Validate that all collection objects in Database have been signed off as correctly updating
        /// the necessary code (specifically transfer, which has some special logic). This tests just scans for
        /// properties on Database that extend SmoCollectionBase and if they aren't in the validated list
        /// fails - asking the user to get sign off that they updated everything correctly.
        /// </summary>
        [TestCategory("Unit")]
        [TestMethod]
        public void AllDatabaseCollectionProperties_AreVerifiedAsHavingUpdateTransferCode()
        {
            //See if any properties that extend SmoCollectionBase aren't in our validated list,
            //fail the test if there are
            var unknownCollections = typeof(Database).GetProperties()
                .Where(p => typeof(SmoCollectionBase).IsAssignableFrom(p.PropertyType))
                .Select(pi => pi.PropertyType)
                .Except(validatedCollections)
                .OrderBy(t => t.Name);

            Assert.That(unknownCollections.Any(), Is.False,
                @"Found unexpected collections in Database. If you're adding a new collection to the Database type follow the steps below to update this test:
1. Add the new Collection type to the validatedCollections set above
2. Follow all the instructions listed on the ""Add new property to an existing object"" section of the OneNote
3. Submit a Code Review with your changes
3. Get confirmation (preferebly have them sign off in the CR) from a SMO Comitter that your changes are correct

Unexpected Collection Types :
{0}", string.Join(System.Environment.NewLine, unknownCollections));

        }

        [TestCategory("Unit")]
        [TestMethod]
        public void Database_default_constructor_sets_designmode()
        {
            // setting ServerVersion allows setting Database.Parent without a server query to get the version
            var serverConnection = new ServerConnection() { ServerVersion = new ServerVersion(15, 0), TrueName = "designMode" };
            var server = new Management.Smo.Server(serverConnection);
            // designmode requires an offline connection
            (server as ISfcHasConnection).ConnectionContext.Mode = SfcConnectionContextMode.Offline;
            var database = new Database()
            {
                Parent = server,
                Name = "dbname"
            };
            Assert.That(database.IsDesignMode, Is.True, "IsDesignMode");
            var script = database.Script();
            Assert.That(script.Cast<string>(), Is.EqualTo(new[] { "CREATE DATABASE [dbname]" }), "database.Script() in design mode");
        }
    }
}
