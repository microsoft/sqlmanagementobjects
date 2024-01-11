// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using _VSUT = Microsoft.VisualStudio.TestTools.UnitTesting;
using ExternalLanguageFilePlatform = Microsoft.SqlServer.Management.Smo.ExternalLanguageFilePlatform;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing External Language properties and scripting.
    /// </summary>
    [_VSUT.TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 15)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
    public class ExternalLanguage_SmoTestSuite : SmoObjectTestBase
    {
        #region Enums

        /// <summary>
        /// Specifies the content type for an external language installation or alteration.
        /// </summary>
        private enum ExternalLanguageContentType
        {
            Binary = 0,
            Path
        }

        #endregion

        #region Static Vars

        /// <summary>
        /// Setup script to run.
        /// </summary>
        private static readonly string SetupScript = "IF NOT EXISTS (SELECT * from sys.external_languages langs WHERE langs.language = N'TestLanguage') " +
            "EXEC sys.sp_executesql N'CREATE EXTERNAL LANGUAGE [TestLanguage] FROM (content = 0xA0000000, file_name = ''libTestExtension.dll'')';";

        /// <summary>
        /// RExtension v1.0. 
        /// </summary>
        private static readonly string LangRExtension = @"C:\TestArtifacts\R-lang-extension-windows-release.zip";

        /// <summary>
        /// RExtension file name. 
        /// </summary>
        private static readonly string FileNameForRExtension = @"libRExtension.dll";

        /// <summary>
        /// PythonExtension v1.0. 
        /// </summary>
        private static readonly string LangPythonExtension = @"C:\TestArtifacts\python-lang-extension-windows.zip";

        /// <summary>
        /// PythonExtension file name. 
        /// </summary>
        private static readonly string FileNameForPythonExtension = @"pythonextension.dll";

        /// <summary>
        /// Dummy language bits. 
        /// </summary>
        private static readonly byte[] DummyContent_1 = new byte[] { 0x31, 0x32 };

        /// <summary>
        /// Dummy language file name. 
        /// </summary>
        private static readonly string FileNameForDummyLang1 = "dummyFile1.dll";

        /// <summary>
        /// Dummy language bits. 
        /// </summary>
        private static readonly byte[] DummyContent_2 = new byte[] { 0x33, 0x34 };

        /// <summary>
        /// Dummy language file name. 
        /// </summary>
        private static readonly string FileNameForDummyLang2 = "dummyFile2.dll";

        /// <summary>
        /// Dummy language bits. 
        /// </summary>
        private static readonly byte[] DummyContent_3 = new byte[] { 0x35, 0x36 };

        /// <summary>
        /// Dummy language file name. 
        /// </summary>
        private static readonly string FileNameForDummyLang3 = "dummyFile3.dll";

        /// <summary>
        /// External language name prefix. 
        /// </summary>
        private static readonly string ExternalLanguagePrefix = "ExternalLanguage";

        #endregion // Static Vars

        #region Database Test Helpers

        /// <summary>
        /// Runs the setup scripts for the specified targetServer.
        /// </summary>
        /// <param name="db">Database.</param>
        internal static void SetupDb(_SMO.Database db)
        {
            TraceHelper.TraceInformation($"Setting up database {db.Name}");
            db.ExecuteNonQuery(SetupScript);
        }

        #endregion // Database Test Helpers

        #region Scripting Tests

        /// <summary>
        /// Validate that the Properties for both ExternalLanguage and ExternalLanguageFile objects
        /// can be enumerated. Also, validates that the state of the objects are kept in sync.
        ///
        /// This is a regression test to make sure that the state of the 2 objects are kept in sync,
        /// particularly that the state of the ExternalLanguageFile object is changed from Creating
        /// to Existing once the ExternalLanguage object is created.
        /// </summary>
        [_VSUT.TestMethod]
        public void ExternalLanguage_Can_Enumerate_ExternalLanguage_and_ExternalLanguageFile_Properties()
        {
            this.ExecuteFromDbPool(
                "ExternalLanguageProperties",
                db =>
                {
                    var languageName = $"MyLanguage{Guid.NewGuid()}";
                    var el = new _SMO.ExternalLanguage(db, languageName);
                    Assert.That(el.State, Is.EqualTo(_SMO.SqlSmoState.Creating), "Unexpected state for ExternalLanguage");

                    el.AddFile(FileNameForDummyLang1, contentFromBinary: DummyContent_1);
                    Assert.That(el.ExternalLanguageFiles[0].State, Is.EqualTo(_SMO.SqlSmoState.Creating), "Unexpected state for ExternalLanguageFile");

                    el.Create();
                    Assert.That(el.State, Is.EqualTo(_SMO.SqlSmoState.Existing), "Unexpected state for ExternalLanguage after creation");
                    Assert.That(el.ExternalLanguageFiles[0].State, Is.EqualTo(_SMO.SqlSmoState.Existing), "Unexpected state for ExternalLanguageFile after creation");
                });
        }

        /// <summary>
        /// This is a regression tests to make sure that scripting of an ExternalLanguage
        /// works when the ContentType is Binary. It validates the scripting both after
        /// a Create() and an Alter(), so in a way it augments the scripting baseline test
        /// we already have (VerifyBaseline_*).
        /// </summary>
        [_VSUT.TestMethod]
        public void ExternalLanguage_Can_Script_When_ContentType_Is_Binary()
        {
            this.ExecuteFromDbPool(
                "ExternalLanguageScriptTypeBin",
                db =>
                {
                    var languageName = $"MyLanguage{Guid.NewGuid()}";

                    // Create a dummy language with some fake data (2 bytes)
                    //
                    var el = new _SMO.ExternalLanguage(db, languageName);
                    el.AddFile(FileNameForDummyLang1, DummyContent_1);

                    // Adding a dummy extended property
                    //
                    el.ExtendedProperties.Add(new ExtendedProperty(el, "Ext prop1", "Ext prop value1"));

                    el.Create();

                    // Making sure the Extended Properties were added.
                    //
                    Assert.AreEqual(1, el.ExtendedProperties.Count, "No extended properties created");
                    Assert.AreEqual(el.ExtendedProperties["Ext prop1"].Value, "Ext prop value1", "Created external language does not have the correct extended property");

                    // Script the object and make sure it has the expected script fragment
                    string elScript = el.Script()[0];
                    Assert.That(elScript, Contains.Substring($"FROM (content = 0x3132, file_name = '{FileNameForDummyLang1}'"), "Unexpected scripting after Create().");

                    // Now we alter the same object: under the hood, the object is scripted for Alter,
                    el.ExternalLanguageFiles[0].FileName = FileNameForDummyLang2;
                    el.ExternalLanguageFiles[0].ContentFromBinary = DummyContent_2;
                    el.Alter();
                    elScript = el.Script()[0];
                    Assert.That(elScript, Contains.Substring($"FROM (content = 0x3334, file_name = '{FileNameForDummyLang2}'"), "Unexpected scripting after Alter().");
                });
        }


        #endregion

        #region Create/Alter/Drop Tests

        /// <summary>
        /// Tests creating, altering, and dropping an external language through SMO from binary content.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void ExternalLanguage_TestCreateAlterDropFromBinary()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    SetupDb(database);
                    TestCreateAlterDrop(database, ExternalLanguageContentType.Binary);
                });
        }

        /// <summary>
        /// Tests creating, altering, and dropping an external language through SMO from file path.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void ExternalLanguage_TestCreateAlterDropFromPath()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    SetupDb(database);
                    TestCreateAlterDrop(database, ExternalLanguageContentType.Path);
                });
        }

        /// <summary>
        /// Tests dropping an external language with IF EXISTS option through SMO.
        /// </summary>
        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void SmoDropIfExists_ExternalLanguage()
        {
            this.ExecuteFromDbPool(
                database =>
                {
                    SetupDb(database);
                    _SMO.ExternalLanguage lang = new _SMO.ExternalLanguage(database, GenerateSmoObjectName("TestLanguage"));
                    lang.AddFile(FileNameForRExtension, contentFromFile: LangRExtension);
                    VerifySmoObjectDropIfExists(lang, database);
                });
        }

        #endregion

        #region Helpers

        /// <summary>
        /// 1. Create a language (from a path or actual bits) and verify it appears in the database collections and in SQL Server system tables.
        /// 2. Alter the language and verify the language content is modified.
        /// 3. Drop the language and verify it is removed from the database collections and SQL Server system tables.
        /// </summary>
        /// <param name="db">Database.</param>
        /// <param name="languageContentSource">The source to get the language content from.</param>
        private void TestCreateAlterDrop(_SMO.Database db, ExternalLanguageContentType languageContentSource)
        {
            // Create a language.
            //
            _SMO.ExternalLanguage lang;

            if (languageContentSource == ExternalLanguageContentType.Path)
            {
                lang = db.CreateExternalLanguage(
                    GenerateUniqueSmoObjectName(ExternalLanguagePrefix),
                    FileNameForRExtension,
                    externalLangFilePath: LangRExtension);
            }
            else
            {
                lang = db.CreateExternalLanguage(
                    GenerateUniqueSmoObjectName(ExternalLanguagePrefix),
                    FileNameForDummyLang1,
                    externalLangContentBits: DummyContent_1);
            }

            db.ExternalLanguages.Refresh();

            // Verify the language is created.
            //
            Assert.That(db.ExternalLanguages.Cast<_SMO.ExternalLanguage>().Select(l => l.Name), Has.Member(lang.Name),
                $"Language {lang.Name} is not created in setup.");

            // Verify the query result has one row.
            //
            var dataSet = db.ExecuteWithResults($"SELECT * FROM sys.external_languages WHERE language = '{SmoObjectHelpers.SqlEscapeSingleQuote(lang.Name)}'");
            var table = dataSet.Tables[0];
            Assert.That(table.Rows.Count, Is.EqualTo(1), $"The language {lang.Name} is not created.");

            string languageOriginalContent = lang.ExternalLanguageFiles[0].GetFileText();

            // Alter the language.
            //
            if (languageContentSource == ExternalLanguageContentType.Path)
            {
                lang.ExternalLanguageFiles[0].ContentFromFile = LangPythonExtension;
                lang.ExternalLanguageFiles[0].FileName = FileNameForPythonExtension;
            }
            else
            {
                lang.ExternalLanguageFiles[0].ContentFromBinary = DummyContent_2;
                lang.ExternalLanguageFiles[0].FileName = FileNameForDummyLang2;
            }
            lang.ExternalLanguageFiles[0].Platform = ExternalLanguageFilePlatform.Windows;
            lang.Alter();
            lang.Refresh();

            // Verify the content has changed.
            //
            var langContent = lang.ExternalLanguageFiles[0].GetFileText();
            Assert.That(langContent, Is.Not.EqualTo(languageOriginalContent),
                $"Language {lang.Name} content is the same after alter operation.");

            //Verify number of files hasn't been changed
            //
            Assert.That(lang.ExternalLanguageFiles.Count, Is.EqualTo(1), $"The number of files for this external language has unexpectedly changed.");


            if (languageContentSource == ExternalLanguageContentType.Binary)
            {
                Assert.That(langContent, Is.EqualTo($"0x3334"),
                    $"After alter operation, language {lang.Name} content is not as expected.");
            }

            lang.AddFile(FileNameForDummyLang3, DummyContent_3, ExternalLanguageFilePlatform.Linux);
            lang.Alter();
            lang.Refresh();

            // Verify number of files has changed
            //
            Assert.That(lang.ExternalLanguageFiles.Count, Is.EqualTo(2), $"The number of files for this external language has not changed or changed unexpectedly.");

            // Marking an external lang file to be dropped
            //
            lang.RemoveFile(ExternalLanguageFilePlatform.Linux);
            lang.Alter();
            lang.Refresh();

            // Verify number of files has changed
            //
            Assert.That(lang.ExternalLanguageFiles.Count, Is.EqualTo(1), $"The number of files for this external language has not changed or changed unexpectedly.");

            // Drop the language.
            //
            lang.Drop();
            VerifyIsSmoObjectDropped(lang, db);
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// </summary>
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ExternalLanguage lang = (_SMO.ExternalLanguage)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.ExternalLanguages.Refresh();
            Assert.That(database.ExternalLanguages[lang.Name], Is.Null,
                          $"Language {lang.Name} is not dropped with DropIfExists.");

            // Verify query result is empty.
            //
            var dataSet = database.ExecuteWithResults($"SELECT * FROM sys.external_languages WHERE language = '{SmoObjectHelpers.SqlEscapeSingleQuote(lang.Name)}'");
            var table = dataSet.Tables[0];
            Assert.That(table.Rows.Count, Is.EqualTo(0), $"The language {lang.Name} is not dropped.");
        }



        #endregion // Helpers
    }
}