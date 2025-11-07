// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
            typeof(SensitivityClassificationCollection),
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
                .Where(p => typeof(ISmoCollection).IsAssignableFrom(p.PropertyType))
                .Select(pi => pi.PropertyType)
                .Except(validatedCollections)
                .OrderBy(t => t.Name);

            Assert.That(unknownCollections.Any(), Is.False,
                @"Found unexpected collections in Database. If you're adding a new collection to the Database type follow the steps below to update this test:
1. Add the new Collection type to the validatedCollections set above
2. Follow all the instructions listed on the ""Add new property to an existing object"" section of the OneNote
3. Submit a Code Review with your changes
3. Get confirmation (preferably have them sign off in the CR) from a SMO Committer that your changes are correct

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


        [TestCategory("Unit")]
        [TestMethod]
        public void Database_QueryStoreOptions_supports_DesignMode()
        {
            var server = ServerTests.GetDesignModeServer(16);
            var db = new Database(server, "test");
            var expectedReadonlyScript = new StringCollection() { "ALTER DATABASE [test] SET QUERY_STORE = ON", "ALTER DATABASE [test] SET QUERY_STORE (OPERATION_MODE = READ_ONLY)",};
            var expectedCustomScript = new StringCollection() { "ALTER DATABASE [test] SET QUERY_STORE = ON", "ALTER DATABASE [test] SET QUERY_STORE (OPERATION_MODE = READ_ONLY, QUERY_CAPTURE_MODE = CUSTOM, QUERY_CAPTURE_POLICY = (STALE_CAPTURE_POLICY_THRESHOLD = 4 HOURS), MAX_PLANS_PER_QUERY = 210)" };
            db.Create();
            Assert.That(db.QueryStoreOptions, Is.Not.Null, "QueryStoreOptions should be supported on 16");
            db.QueryStoreOptions.DesiredState = QueryStoreOperationMode.ReadOnly;
            Assert.That(db.QueryStoreOptions.ActualState, Is.EqualTo(QueryStoreOperationMode.Off), "ActualState should have default value");
            var sp = db.GetScriptingPreferencesForCreate();
            var script = new StringCollection();
            db.QueryStoreOptions.ScriptAlter(script, sp);
            Assert.That(script, Is.EqualTo(expectedReadonlyScript), "Setting ReadOnly");
            db.QueryStoreOptions.MaxPlansPerQuery = 210;
            db.QueryStoreOptions.QueryCaptureMode = QueryStoreCaptureMode.Custom;
            db.QueryStoreOptions.CapturePolicyStaleThresholdInHrs = 4;
            
            script = new StringCollection();
            db.QueryStoreOptions.ScriptAlter(script, sp);
            Assert.That(script, Is.EqualTo(expectedCustomScript), "Setting Custom capture mode");

            Assert.That(db.QueryStoreOptions.MaxPlansPerQuery, Is.EqualTo(210), nameof(QueryStoreOptions.MaxPlansPerQuery));
            // We could change QueryStoreOptions to override Alter or AlterImpl and set the value in design mode
            Assert.That(db.QueryStoreOptions.ActualState, Is.EqualTo(QueryStoreOperationMode.Off), "ActualState should not be changed by Alter");
        }

    }
}
