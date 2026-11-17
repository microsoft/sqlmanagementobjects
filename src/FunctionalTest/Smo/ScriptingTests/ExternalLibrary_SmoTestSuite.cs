// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using _VSUT = Microsoft.VisualStudio.TestTools.UnitTesting;
using ExternalLibraryContentType = Microsoft.SqlServer.Management.Smo.ExternalLibraryContentType;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing External Library properties and scripting.
    /// </summary>
    [_VSUT.TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 14)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand)]
    public class ExternalLibrary_SmoTestSuite : SmoObjectTestBase
    {
        #region Static Vars

        /// <summary>
        /// Setup script to run.
        /// </summary>
        private static readonly string setupScript = "ExternalLibrary_SmoTestSuite_Setup.sql";

        /// <summary>
        /// Lazy Eval version 1 - R library. 
        /// TODO: 10721215. Now we have some permission issues when trying to read the library file from remote fileshare.
        /// Once the issue is fixed, we should update the below paths with 
        /// \\sqlcl\team\Engine\Extensibility\LibraryManagement\lazyeval_0.1.10.zip
        /// </summary>
        private static readonly string lazyEval_1 = @"C:\TestArtifacts\lazyeval_0.1.10.zip";

        /// <summary>
        /// Lazy Eval version 2 - R library. 
        /// TODO: 10721215. Now we have some permission issues when trying to read the library file from remote fileshare.
        /// Once the issue is fixed, we should update the path to 
        /// \\sqlcl\team\Engine\Extensibility\LibraryManagement\lazyeval_0.2.0.zip
        /// </summary>
        private static readonly string lazyEval_2 = @"C:\TestArtifacts\lazyeval_0.2.0.zip";

        /// <summary>
        /// Dummy library bits. 
        /// </summary>
        private static readonly string dummyContent_1 = "123";

        /// <summary>
        /// Dummy library bits. 
        /// </summary>
        private static readonly string dummyContent_2 = "456";

        /// <summary>
        /// External library name prefix. 
        /// </summary>
        private static readonly string externalLibraryPrefix = "ExternalLibrary";

        #endregion // Static Vars

        #region Database Test Helpers

        /// <summary>
        /// Runs the setup scripts for the specified targetServer.
        /// </summary>
        /// <param name="db">Database.</param>
        internal static void SetupDb(_SMO.Database db)
        {
            TraceHelper.TraceInformation(String.Format("Setting up database {0}", db.Name));
            new ScriptHelpers().LoadAndRunScriptResource(setupScript, db, Assembly.GetExecutingAssembly());
        }

        #endregion // Database Test Helpers

        #region Helpers

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(_SMO.SqlSmoObject obj)
        {
            _SMO.ExternalLibrary lib = (_SMO.ExternalLibrary)obj;
            _SMO.Database database = lib.Parent;            
        }

        /// <summary>
        /// 1. Create a library (from a path or actual bits) and verify it appears in the database collections and in SQL Server system tables.
        /// 2. Alter the library and verify the library content is modified.
        /// 3. Drop the library and verify it is removed from the database collections and SQL Server system tables.
        /// <param name="db">Database.</param>
        /// <param name="libraryContentSource">The source to get the library content from.</param>
        /// </summary>
        private void TestCreateAlterDrop(_SMO.Database db, ExternalLibraryContentType libraryContentSource)
        {
            // Create a library.
            _SMO.ExternalLibrary lib;

            if (libraryContentSource == ExternalLibraryContentType.Path)
            {
                lib = db.CreateExternalLibrary(GenerateSmoObjectName(externalLibraryPrefix), lazyEval_1, ExternalLibraryContentType.Path);
            }
            else
            {
                lib = db.CreateExternalLibrary(GenerateSmoObjectName(externalLibraryPrefix), dummyContent_1, ExternalLibraryContentType.Binary);
            }

            db.ExternalLibraries.Refresh();

            // Verify the library is created.
            Assert.That(db.ExternalLibraries.Cast<_SMO.ExternalLibrary>().Select(l => l.Name), Has.Member(lib.Name),
                String.Format("Library {0} is not created in setup.", lib.Name));

            // Verify the query result has one row.
            var dataSet = db.ExecuteWithResults(String.Format("SELECT * FROM sys.external_libraries WHERE name = '{0}'", 
                SmoObjectHelpers.SqlEscapeSingleQuote(lib.Name)));
            var table = dataSet.Tables[0];
            Assert.That(table.Rows.Count, Is.EqualTo(1), String.Format("The library {0} is not created.", lib.Name));

            string libraryOriginalContent = lib.ExternalLibraryFile.GetFileText();

            // Alter the library.
            if (libraryContentSource == ExternalLibraryContentType.Path)
            {
                lib.Alter(lazyEval_2, ExternalLibraryContentType.Path);
            }
            else
            {
                lib.Alter(dummyContent_2, ExternalLibraryContentType.Binary);
            }
            lib.Refresh();

            // Verify the content has changed.
            Assert.That(lib.ExternalLibraryFile.GetFileText(), Is.Not.EqualTo(libraryOriginalContent), 
                String.Format("Library {0} content is the same after alter operation.", lib.Name));

            // Drop the library.
            lib.Drop();
            VerifyIsSmoObjectDropped(lib, db);
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ExternalLibrary lib = (_SMO.ExternalLibrary)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.ExternalLibraries.Refresh();
            Assert.That(database.ExternalLibraries[lib.Name], Is.Null,
                          String.Format("Library {0} is not dropped with DropIfExists.", lib.Name));

            // Verify query result is empty.
            var dataSet = database.ExecuteWithResults(String.Format("SELECT * FROM sys.external_libraries WHERE name = '{0}'", 
                SmoObjectHelpers.SqlEscapeSingleQuote(lib.Name)));
            var table = dataSet.Tables[0];
            Assert.That(table.Rows.Count, Is.EqualTo(0), String.Format("The library {0} is not dropped.", lib.Name));
        }

        #endregion // Helpers

        #region Scripting Tests

        /// <summary>
        /// Validate that the Properties for both ExternalLibrary and ExternalLibraryFile objects
        /// can be enumerated. Also, validates that the state of the objects are kept in sync.
        ///
        /// This is a regression test to make sure that the state of the 2 objects are kept in sync,
        /// particularly that the state of the ExternalLibraryFile object is changed from Creating
        /// to Existing once the ExternalLibrary object is created.
        /// </summary>
        [_VSUT.TestMethod]
        public void ExternalLibrary_Can_Enumerate_ExternalLibrary_and_ExternalLibraryFile_Properties()
        {
            this.ExecuteWithDbDrop(
                "ExternalLibraryProperties",
                db =>
                {
                    var el = new _SMO.ExternalLibrary(db, "MyLibrary") { ExternalLibraryLanguage = "R" };

                    Assert.That(el.State, Is.EqualTo(_SMO.SqlSmoState.Creating), "Unexpected state for ExternalLibrary");
                    Assert.That(el.ExternalLibraryFile.State, Is.EqualTo(_SMO.SqlSmoState.Creating), "Unexpected state for ExternalLibraryFile");

                    el.Create("010203", ExternalLibraryContentType.Binary);

                    Assert.That(el.State, Is.EqualTo(_SMO.SqlSmoState.Existing), "Unexpected state for ExternalLibrary after creation");
                    Assert.That(el.ExternalLibraryFile.State, Is.EqualTo(_SMO.SqlSmoState.Existing), "Unexpected state for ExternalLibraryFile after creation");

                    Assert.DoesNotThrow(() => el.Properties.Cast<_SMO.Property>().ToArray(), "It should be possible to enumerate the properties of an ExternalLibrary object");
                    Assert.DoesNotThrow(() => el.ExternalLibraryFile.Properties.Cast<_SMO.Property>().ToArray(), "It should be possible to enumerate the properties of an ExternalLibraryFile object");
                });
        }

        /// <summary>
        /// This is a regression tests to make sure that scripting of an ExternalLibrary
        /// works when the ContentType is Binary. It validates the scripting both after
        /// a Create() and an Alter(), so in a way it augments the scripting baseline test
        /// we already have (VerifyBaseline_*).
        /// </summary>
        [_VSUT.TestMethod]
        public void ExternalLibrary_Can_Script_When_ContentType_Is_Binary()
        {
            this.ExecuteWithDbDrop(
                "ExternalLibraryScriptTypeBin",
                db =>
                {
                    // Create a dummy library with some fake data (3 bytes)
                    var el = new _SMO.ExternalLibrary(db, "MyLibrary") { ExternalLibraryLanguage = "R" };
                    el.Create("010203", _SMO.ExternalLibraryContentType.Binary);

                    // Script the object and make sure it has the expected script fragment
                    Assert.That(el.Script()[0], Contains.Substring("FROM (content = 0x010203)"), "Unexpected scripting after Create()");

                    // Now we alter the same object: under the hood, the object is scripted for Alter,
                    el.Alter("030201", ExternalLibraryContentType.Binary);
                    Assert.That(el.Script()[0], Contains.Substring("FROM (content = 0x030201)"), "Unexpected scripting after Alter()");
                });
        }


        #endregion
        #region Create/Alter/Drop Tests

        /// <summary>
        /// Tests creating, altering, and dropping an external library through SMO from binary content.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void ExternalLibrary_TestCreateAlterDropFromBinary()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);
                    TestCreateAlterDrop(database, ExternalLibraryContentType.Binary);
                });
        }

        /// <summary>
        /// Tests creating, altering, and dropping an external library through SMO from binary content.
        /// TODO: 10721215: to create a smo test using an external library path.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void ExternalLibrary_TestCreateAlterDropFromPath()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);
                    TestCreateAlterDrop(database, ExternalLibraryContentType.Path);
                });
        }

        /// <summary>
        /// Tests dropping an external library with IF EXISTS option through SMO.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void SmoDropIfExits_ExternalLibrary()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);
                    _SMO.ExternalLibrary lib = new _SMO.ExternalLibrary(database, GenerateSmoObjectName("lazyeval"));
                    VerifySmoObjectDropIfExists(lib, database);
                });
        }

        #endregion
    }
}