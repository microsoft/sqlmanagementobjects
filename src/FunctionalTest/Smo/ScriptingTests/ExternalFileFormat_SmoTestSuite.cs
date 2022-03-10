// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using VSTest = Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// SMO scripting External File Format TestSuite.
    /// </summary>
    [VSTest.TestClass]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.Express, DatabaseEngineEdition.SqlManagedInstance)]
    public partial class ExternalFileFormat_SmoTestSuite : SqlTestBase
    {
        /// <summary>
        /// Verifies that SQL DW database objects created using SMO from a SQL Azure V12 (Sterling)
        /// server have the same values for their properties/scripts as expected.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            MinMajor = 12, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        [SqlTestArea(SqlTestArea.Polybase)]
        public void VerifyPositiveExternalFileFormatCreateDrop_AzureSterlingV12_SqlDW()
        {
            VerifyPositiveExternalFileFormatCreateDrop(AzureDatabaseEdition.DataWarehouse);
        }

        /// <summary>
        /// Verifies that SQL database objects created using SMO from a SQL On Prem
        /// server have the same values for their properties/scripts as expected.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13, MaxMajor =15)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestArea(SqlTestArea.Polybase)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
        public void VerifyPositiveExternalFileFormatCreateDrop_2016AndAfterOnPrem()
        {
            VerifyPositiveExternalFileFormatCreateDrop();
        }

        /// Tests creating, dropping and scripting of the external file format objects via SMO.
        /// Positive test steps:
        /// 1. Create an external file format with the format type property.
        /// 2. Script create external file format with IncludeIfNotExists option set to true.
        /// 3. Verify the script contains expected information.
        /// 4. Script drop external file format with IncludeIfNotExists option set to true.
        /// 5. Verify the script contains expected information.
        /// 6. Drop the external file format and verify the count is 0.
        /// 7. Verify the script re-creates the external file format.
        /// 8. Test dropping external file format using the generated script.  Verify it was dropped correctly.
        private void VerifyPositiveExternalFileFormatCreateDrop(AzureDatabaseEdition azureDatabaseEdition = AzureDatabaseEdition.NotApplicable)
        {
            // const definitions
            const string ExternalFileFormatSerdeMethod = @"org.apache.hadoop.hive.serde2.columnar.ColumnarSerDe";
            const string FieldTerminator = "|";
            const string StringDelimiter = "#";
            const string DateFormat = "MM-dd-yyyy";
            const int FirstRow =  10;

            string[] dataCompression = { "org.apache.hadoop.io.compress.DefaultCodec", "org.apache.hadoop.io.compress.GzipCodec" };

            string[] externalFileFormatNames = { "eff1", "eff100", "eff[]1", "eff'1", "eff--1", "eff(1", "eff)1" };

            this.ExecuteWithDbDrop(
                database =>
                {
                    // positive unit tests for the DELIMITEDTEXT file format type
                    // supported DDL options are: (1) no optional properties; (2) any of the format properties (field terminator, string delimiter, date format and use type default; (3) data compression property
                    // NOTE: The serde method is not supported with the format type being DELIMITEDTEXT.
                    // 1. no optional properties
                    // 2. any of the format options
                    // 3. data compression
                    // 4. format options and data compression
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[0], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[1], ExternalFileFormatType.DelimitedText, string.Empty, FieldTerminator, string.Empty, string.Empty, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[2], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, StringDelimiter, string.Empty, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[3], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, string.Empty, DateFormat, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[4], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[5], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, string.Empty, string.Empty, true, string.Empty);

                    if (azureDatabaseEdition == AzureDatabaseEdition.DataWarehouse)
                    {
                        VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[5], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty, (int?)FirstRow);
                    }

                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[6], ExternalFileFormatType.DelimitedText, string.Empty, string.Empty, string.Empty, string.Empty, null, dataCompression[0]);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[0],
                        ExternalFileFormatType.DelimitedText,
                        string.Empty,
                        FieldTerminator,
                        StringDelimiter,
                        DateFormat,
                        false,
                        dataCompression[0],
                        azureDatabaseEdition == AzureDatabaseEdition.DataWarehouse ? (int?)FirstRow : null);

                    // positive unit tests for ORC file format type
                    // supported DDL options are: (1) no optional properties; (2) data compression property
                    // NOTE: The serde method and the format options are not supported with the format type being ORC.
                    // 1. no optional properties
                    // 2. data compression
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[1], ExternalFileFormatType.Orc, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[0], ExternalFileFormatType.Orc, string.Empty, string.Empty, string.Empty, string.Empty, null, dataCompression[0]);

                    // positive unit tests for PARQUET file format type
                    // supported DDL options are: (1) no optional properties; (2) data compression property
                    // NOTE: The serde method and the format options are not supported with the format type being PARQUET.
                    // 1. no optional properties
                    // 2. data compression
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[1], ExternalFileFormatType.Parquet, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[0], ExternalFileFormatType.Parquet, string.Empty, string.Empty, string.Empty, string.Empty, null, dataCompression[1]);

                    // positive unit tests for the RCFILE file format type
                    // supported DDL options are: (1) serde method - required; (2) data compression property - optional
                    // NOTE: The format options are not supported with the format type being RCFILE.
                    // 1. serde method (required)
                    // 2. serde method (required) and data compression
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[2], ExternalFileFormatType.RcFile, ExternalFileFormatSerdeMethod, string.Empty, string.Empty, string.Empty, null, string.Empty);
                    VerifyPositiveExternalFileFormatCreateDropHelper(database, externalFileFormatNames[3], ExternalFileFormatType.RcFile, ExternalFileFormatSerdeMethod, string.Empty, string.Empty, string.Empty, null, dataCompression[0]);
                },
                azureDatabaseEdition);
        }

        /// <summary>
        /// Tests dropping an external file format with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        public void SmoDropIfExists_ExternalFileFormat_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    ExternalFileFormat eff = new ExternalFileFormat(database,
                        "eff_" + (this.TestContext.TestName ?? ""));

                    eff.FormatType = ExternalFileFormatType.DelimitedText;

                    // 1. Try to drop external file format before it is created.
                    //
                    eff.DropIfExists();

                    eff.Create();

                    // 2. Verify the script contains expected statement.
                    //
                    ScriptingOptions so = new ScriptingOptions();
                    so.IncludeIfNotExists = true;
                    so.ScriptDrops = true;
                    StringCollection col = eff.Script(so);

                    StringBuilder sb = new StringBuilder();
                    StringBuilder scriptTemplate = new StringBuilder();
                    foreach (string statement in col)
                    {
                        sb.AppendLine(statement);
                    }
                    string dropIfExistsScripts = sb.ToString();
                    string scriptDropIfExistsTemplate = "IF  EXISTS";

                    Assert.IsTrue(dropIfExistsScripts.Contains(scriptDropIfExistsTemplate),
                                  "Drop with existence check is not scripted.");

                    // 3. Drop external file format with DropIfExists and check if it is dropped.
                    //
                    eff.DropIfExists();
                    database.ExternalFileFormats.Refresh();
                    Assert.IsNull(database.ExternalFileFormats[eff.Name],
                                    "Current external file format not dropped with DropIfExists.");

                    // 4. Try to drop already dropped external file format.
                    //
                    eff.DropIfExists();
                });
        }

        /// <summary>
        /// Executes positive tests for the create external file format object.
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <param name="externalFileFormatName">The external file format name.</param>
        /// <param name="externalFileFormatType">The external file format type.</param>
        /// <param name="externalFileFormatSerDeMethod">The external file format serialize/deserialize method property value.</param>
        /// <param name="externalFileFormatFieldTerminator">The external file format field terminator property value.</param>
        /// <param name="externalFileFormatStringDelimiter">The external file format string delimiter property value.</param>
        /// <param name="externalFileFormatDateFormat">The external file format date format property value.</param>
        /// <param name="externalFileFormatUseTypeDefaultOption">The external file format use type default option value.</param>
        /// <param name="externalFileFormatDataCompression">The external file format data compression value.</param>
        /// <param name="externalFileFormatFirstRow">The external file format first row property value.</param>
        private void VerifyPositiveExternalFileFormatCreateDropHelper(
            Database db,
            string externalFileFormatName,
            ExternalFileFormatType externalFileFormatType,
            string externalFileFormatSerDeMethod, 
            string externalFileFormatFieldTerminator, 
            string externalFileFormatStringDelimiter,
            string externalFileFormatDateFormat,
            bool? externalFileFormatUseTypeDefaultOption,
            string externalFileFormatDataCompression,
            int? externalFileFormatFirstRow = null
            )
        {
            // const definitions
            const string ExternalFileFormatCountQuery = @"SELECT COUNT(*) FROM sys.external_file_formats";
            const string ExternalFileFormatScriptDropTemplate = "DROP EXTERNAL FILE FORMAT {0}";
            const string ExternalFileFormatTestName = "External File Format Testing";

            //
            // Step 1. Create an external file format with the format type property.
            //
            TraceHelper.TraceInformation("Step 1: {0} - Creating an external file format {1}.", ExternalFileFormatTestName, externalFileFormatName);
            ExternalFileFormat externalFileFormat = new ExternalFileFormat(db, externalFileFormatName, externalFileFormatType);

            // check for optional properties
            if (!string.IsNullOrEmpty(externalFileFormatSerDeMethod))
            {
                externalFileFormat.SerDeMethod = externalFileFormatSerDeMethod;
            }
            if (!string.IsNullOrEmpty(externalFileFormatFieldTerminator))
            {
                externalFileFormat.FieldTerminator = externalFileFormatFieldTerminator;
            }
            if (!string.IsNullOrEmpty(externalFileFormatStringDelimiter))
            {
                externalFileFormat.StringDelimiter = externalFileFormatStringDelimiter;
            }
            if (!string.IsNullOrEmpty(externalFileFormatDateFormat))
            {
                externalFileFormat.DateFormat = externalFileFormatDateFormat;
            }
            if (externalFileFormatUseTypeDefaultOption.HasValue)
            {
                externalFileFormat.UseTypeDefault = externalFileFormatUseTypeDefaultOption.Value;
            }
            if (!string.IsNullOrEmpty(externalFileFormatDataCompression))
            {
                externalFileFormat.DataCompression = externalFileFormatDataCompression;
            }
            if (externalFileFormatFirstRow.HasValue)
            {
                externalFileFormat.FirstRow = externalFileFormatFirstRow.Value;
            }

            externalFileFormat.Create();
            // verify the external file format was created by querying the external file format system view
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalFileFormatCountQuery), "External file format was not created.");

            //
            // Step 2. Script external file format with IncludeIfNotExists option set to true.
            //
            TraceHelper.TraceInformation("Step 2: {0} - Scripting file format source {1}.", ExternalFileFormatTestName, externalFileFormat.Name);
            ScriptingOptions so = new ScriptingOptions();
            so.IncludeIfNotExists = true;
            so.IncludeDatabaseContext = true;
            StringCollection col = externalFileFormat.Script(so);

            //
            // Step 3. Verify the script contains expected information.
            //
            TraceHelper.TraceInformation("Step 3: {0} - Verifying generated external file format script.", ExternalFileFormatTestName);
            StringBuilder sb = new StringBuilder();
            StringBuilder scriptTemplate = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
                TraceHelper.TraceInformation(statement);
            }

            const string ExternalFileFormatScriptCreateTemplate = "CREATE EXTERNAL FILE FORMAT {0} WITH (FORMAT_TYPE = {1}"; // not closing the parenthesis to allow for optional parameters
            string createExternalFileFormatScripts = sb.ToString();
            string fullyFormatedNameForScripting = string.Format("[{0}]", Util.EscapeString(externalFileFormat.Name, ']'));
            scriptTemplate.Append(string.Format(ExternalFileFormatScriptCreateTemplate, fullyFormatedNameForScripting, this.GetSqlKeywordForFileFormatType(externalFileFormat.FormatType)));

            // process optional parameters for each file format type and add them to the T-SQL script
            ProcessOptionalProperties(externalFileFormat, scriptTemplate, db.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse);
            TraceHelper.TraceInformation(createExternalFileFormatScripts);
            TraceHelper.TraceInformation(scriptTemplate.ToString());
            Assert.That(createExternalFileFormatScripts, Does.Contain(scriptTemplate.ToString()));

            //
            // Step 4. Script drop external file format with IncludeIfNotExists option set to true.
            //
            TraceHelper.TraceInformation("Step 4: {0} - Scripting drop external file format {1}.", ExternalFileFormatTestName, externalFileFormat.Name);
            so = new ScriptingOptions();
            so.IncludeIfNotExists = true;
            so.IncludeDatabaseContext = true;
            so.ScriptDrops = true;
            col = externalFileFormat.Script(so);

            //
            // Step 5. Verify the script contains expected information.
            //
            TraceHelper.TraceInformation("Step 5: {0} - Verifying generated external file format script.", ExternalFileFormatTestName);
            sb = new StringBuilder();
            scriptTemplate = new StringBuilder();
            foreach (string statement in col)
            {
                sb.AppendLine(statement);
                TraceHelper.TraceInformation(statement);
            }
            string dropExternalFileFormatScripts = sb.ToString();
            scriptTemplate.Append(string.Format(ExternalFileFormatScriptDropTemplate, fullyFormatedNameForScripting));

            TraceHelper.TraceInformation(dropExternalFileFormatScripts);
            TraceHelper.TraceInformation(scriptTemplate.ToString());
            Assert.That(dropExternalFileFormatScripts, Does.Contain(scriptTemplate.ToString()));

            //
            // Step 6. Drop the external file format and verify the count is 0.
            //
            TraceHelper.TraceInformation("Step 6: {0} - Dropping external file format {1}.", ExternalFileFormatTestName, externalFileFormat.Name);
            externalFileFormat.Drop();
            db.ExternalFileFormats.Refresh();
            Assert.AreEqual(0, db.ExternalFileFormats.Count, "External file format was not dropped.");
            
            //
            // Step 7. Verify the script re-creates the external file format.
            //
            TraceHelper.TraceInformation("Step 7: {0} - Creating external file format using generated script.", ExternalFileFormatTestName);
            db.ExecuteNonQuery(createExternalFileFormatScripts);
            db.ExternalFileFormats.Refresh();
            Assert.AreEqual(1, db.ExternalFileFormats.Count, "There should be an external file format present in the collection.");
            Assert.AreEqual(1, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalFileFormatCountQuery), "There should be an external file format present in the database.");
            externalFileFormat = db.ExternalFileFormats[externalFileFormatName];
            Assert.IsNotNull(externalFileFormat, "External file format was not recreated by the script.");
            Assert.AreEqual(externalFileFormatName, externalFileFormat.Name, "Recreated external file format name does not match the original file format name.");
            Assert.AreEqual(externalFileFormatType, externalFileFormat.FormatType, "Recreated external file format does not have the same value for Format Type.");

            // verify optional properties that don't have default values
            // as for properties with default values, the verification will not match
            // this is because if we don't explicitly assign values to them, they will have a default value assigned
            // the optional properties with default values are: FieldTerminator = '|' and UseTypeDefault = False
            if (!string.IsNullOrEmpty(externalFileFormatSerDeMethod))
            {
                Assert.AreEqual(externalFileFormatSerDeMethod, externalFileFormat.SerDeMethod, "Recreated external file format does not have the same value for SerDe Method.");
            }
            if (!string.IsNullOrEmpty(externalFileFormatFieldTerminator))
            {
                Assert.AreEqual(externalFileFormatFieldTerminator, externalFileFormat.FieldTerminator, "Recreated external file format does not have the same value for Field Terminator.");
            }
            if (!string.IsNullOrEmpty(externalFileFormatStringDelimiter))
            {
                Assert.AreEqual(externalFileFormatStringDelimiter, externalFileFormat.StringDelimiter, "Recreated external file format does not have the same value for String Delimiter.");
            }
            if (!string.IsNullOrEmpty(externalFileFormatDateFormat))
            {
                Assert.AreEqual(externalFileFormatDateFormat, externalFileFormat.DateFormat, "Recreated external file format does not have the same value for Date Format.");
            }
            if (externalFileFormatUseTypeDefaultOption != null)
            {
                Assert.AreEqual(externalFileFormatUseTypeDefaultOption, externalFileFormat.UseTypeDefault, "Recreated external file format does not have the same value for Use Type Default.");
            }
            if (!string.IsNullOrEmpty(externalFileFormatDataCompression))
            {
                Assert.AreEqual(externalFileFormatDataCompression, externalFileFormat.DataCompression, "Recreated external file format does not have the same value for Data Compression.");
            }
            if (externalFileFormatFirstRow.HasValue)
            {
                Assert.That(externalFileFormat.FirstRow, Is.EqualTo(externalFileFormatFirstRow), "Recreated external file format does not have the same value for First Row.");
            }

            //
            // Step 8.  Test dropping external file format using the generated script.  Verify it was dropped correctly.
            //
            TraceHelper.TraceInformation("Step 8: {0} - Dropping external file format {1} using the generated script.", ExternalFileFormatTestName, externalFileFormatName);
            db.ExecuteNonQuery(dropExternalFileFormatScripts);
            db.ExternalFileFormats.Refresh();
            Assert.AreEqual(0, db.ExternalFileFormats.Count, "There should be no external file formats present in the collection.");
            Assert.AreEqual(0, (int)db.ExecutionManager.ConnectionContext.ExecuteScalar(ExternalFileFormatCountQuery), "There should be no external file formats present in the database.");
        }

        /// <summary>
        /// Tests negative scenarios for SQL DW database objects created using SMO from a SQL Azure V12 (Sterling)
        /// server.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            MinMajor = 12, MaxMajor = 12, Edition = DatabaseEngineEdition.SqlDataWarehouse)]
        [SqlTestArea(SqlTestArea.Polybase)]
        public void VerifyNegativeExternalFileFormatCreateDrop_AzureSterlingV12_SqlDW()
        {
            VerifyNegativeExternalFileFormatCreateDrop(AzureDatabaseEdition.DataWarehouse);
        }

        /// <summary>
        /// Tests negative scebarios for SQL DW database objects created using SMO from a SQL On Prem
        /// server.
        /// </summary>
        [VSTest.TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        [SqlTestArea(SqlTestArea.Polybase)]
        public void VerifyNegativeExternalFileFormatCreateDrop_2016AndAfterOnPrem()
        {
            VerifyNegativeExternalFileFormatCreateDrop();
        }

        /// Tests creating, dropping and scripting of the external file format objects via SMO.
        /// Negative test steps:
        /// 1. Create external file format with no required properties.
        /// 2. Create external file format with conflicting properties - format type is DelimitedText and SerDeMethod.
        /// 3. Create external file format with conflicting properties - format type is RcFile and FieldTerminator.
        /// 4. Create external file format with conflicting properties - format type is RcFile and StringDelimiter.
        /// 5. Create external file format with conflicting properties - format type is RcFile and DateFormat.
        /// 6. Create external file format with conflicting properties - format type is RcFile and UseTypeDefault.
        /// 7. Create external file format with conflicting properties - format type is Orc and SerDeMethod.
        /// 8. Create external file format with conflicting properties - format type is Orc and FieldTerminator.
        /// 9. Create external file format with conflicting properties - format type is Orc and StringDelimiter.
        /// 10. Create external file format with conflicting properties - format type is Orc and DateFormat.
        /// 11. Create external file format with conflicting properties - format type is Orc and UseTypeDefault.
        /// 12. Create external file format with conflicting properties - format type is Parquet and SerDeMethod.
        /// 13. Create external file format with conflicting properties - format type is Parquet and FieldTerminator.
        /// 14. Create external file format with conflicting properties - format type is Parquet and StringDelimiter.
        /// 15. Create external file format with conflicting properties - format type Parquet and DateFormat.
        /// 16. Create external file format with conflicting properties - format type is Parquet and UseTypeDefault.
        private void VerifyNegativeExternalFileFormatCreateDrop(AzureDatabaseEdition azureDatabaseEdition = AzureDatabaseEdition.NotApplicable)
        {
            // const definitions
            const string ExternalFileFormatSerdeMethod = @"org.apache.hadoop.hive.serde2.columnar.ColumnarSerDe";
            const string FieldTerminator = "|";
            const string StringDelimiter = "#";
            const string DateFormat = "MM-dd-yyyy";
            
            string[] externalFileFormatNames = { "eff1", "eff100", "eff[]1", "eff'1", "eff--1", "eff(1", "eff)1" };
            
            this.ExecuteWithDbDrop(
                database =>
                {
                    // negative unit tests for the DELIMITEDTEXT file format type
                    // unsupported DDL options are: serde method
                    VerifyNegativeExternalFileFormatCreateDropHelper(database, externalFileFormatNames[0], ExternalFileFormatType.DelimitedText, ExternalFileFormatSerdeMethod, FieldTerminator, StringDelimiter, DateFormat);
                },
                azureDatabaseEdition);
        }

        /// <summary>
        /// Executes negative tests for the create external file format object.
        /// </summary>
        /// <param name="db">The database name.</param>
        /// <param name="externalFileFormatName">The external file format name.</param>
        /// <param name="externalFileFormatType">The external file format type.</param>
        /// <param name="externalFileFormatSerDeMethod">The external file format serialize/deserialize method property value.</param>
        /// <param name="externalFileFormatFieldTerminator">The external file format field terminator property value.</param>
        /// <param name="externalFileFormatStringDelimiter">The external file format string delimiter property value.</param>
        /// <param name="externalFileFormatDateFormat">The external file format date format property value.</param>
        private void VerifyNegativeExternalFileFormatCreateDropHelper(Database db, 
            string externalFileFormatName,
            ExternalFileFormatType externalFileFormatType,
            string externalFileFormatSerDeMethod, 
            string externalFileFormatFieldTerminator, 
            string externalFileFormatStringDelimiter,
            string externalFileFormatDateFormat)
        {
            // const definitions
            const string ExternalFileFormatTestName = "External File Format Testing";

            //
            // Step 1. Create external file format with no required properties.
            //
            TraceHelper.TraceInformation("Step 1: {0} - Creating external file format {1} with no required properties.", ExternalFileFormatTestName, externalFileFormatName);
            ExternalFileFormat externalFileFormat = new ExternalFileFormat(db, externalFileFormatName);

            string errorMessage = string.Empty;

            var ex = Assert.Throws<FailedOperationException>(externalFileFormat.Create, "verify the external file format was not created due to unset FormatType");
            var innerEx = ex.GetBaseException();
            Assert.Multiple(() =>
            {
                Assert.That(innerEx, Is.InstanceOf<ArgumentNullException>(), "innermost exception");
                Assert.That(innerEx.Message, Does.Contain(nameof(externalFileFormat.FormatType)), "innermost exception message");
            });

            //
            // Step 2. Create external file format with conflicting properties - format type is DelimitedText and SerDeMethod.
            //
            externalFileFormat.FormatType = externalFileFormatType;
            externalFileFormat.SerDeMethod = externalFileFormatSerDeMethod;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "SerDeMethod", externalFileFormat.SerDeMethod, externalFileFormat.FormatType.ToString());
                
                externalFileFormat.Create(); 

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 3. Create external file format with conflicting properties - format type is RcFile and FieldTerminator.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.RcFile;
            externalFileFormat.FieldTerminator = externalFileFormatFieldTerminator;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "FieldTerminator", externalFileFormat.FieldTerminator, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 4. Create external file format with conflicting properties - format type is RcFile and StringDelimiter.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.RcFile;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = externalFileFormatStringDelimiter;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "StringDelimiter", externalFileFormat.StringDelimiter, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 5. Create external file format with conflicting properties - format type is RcFile and DateFormat.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.RcFile;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = string.Empty;
            externalFileFormat.DateFormat = externalFileFormatDateFormat;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "DateFormat", externalFileFormat.DateFormat, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 6. Create external file format with conflicting properties - format type is RcFile and UseTypeDefault.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.RcFile;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = string.Empty;
            externalFileFormat.DateFormat = string.Empty;
            externalFileFormat.UseTypeDefault = true;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "UseTypeDefault", externalFileFormat.UseTypeDefault.ToString(), externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            if (db.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                //
                // Step 7. Create external file format with conflicting properties - format type is RcFile and FirstRow.
                //
                externalFileFormat.FormatType = ExternalFileFormatType.RcFile;
                externalFileFormat.FieldTerminator = string.Empty;
                externalFileFormat.StringDelimiter = string.Empty;
                externalFileFormat.DateFormat = string.Empty;
                externalFileFormat.UseTypeDefault = false;
                externalFileFormat.FirstRow = 10;

                // verify the external file format was not created
                try
                {
                    // attempt to create an external file format
                    errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "FirstRow", externalFileFormat.FirstRow, externalFileFormat.FormatType.ToString());

                    externalFileFormat.Create();

                    // validate expected exception and error message
                    Assert.Fail(errorMessage, externalFileFormat.Name);
                }
                catch (SmoException e)
                {
                    if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                    {
                        throw;
                    }
                }
            }

            //
            // Step 8. Create external file format with conflicting properties - format type is Orc and SerDeMethod.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Orc;
            externalFileFormat.SerDeMethod = externalFileFormatSerDeMethod;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "SerDeMethod", externalFileFormat.SerDeMethod, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 9. Create external file format with conflicting properties - format type is Orc and FieldTerminator.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Orc;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = externalFileFormatFieldTerminator;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "FieldTerminator", externalFileFormat.FieldTerminator, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 10. Create external file format with conflicting properties - format type is Orc and StringDelimiter.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Orc;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = externalFileFormatStringDelimiter;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "StringDelimiter", externalFileFormat.StringDelimiter, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 11. Create external file format with conflicting properties - format type is Orc and DateFormat.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Orc;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = string.Empty;
            externalFileFormat.DateFormat = externalFileFormatDateFormat;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "DateFormat", externalFileFormat.DateFormat, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 12. Create external file format with conflicting properties - format type is Orc and UseTypeDefault.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Orc;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = string.Empty;
            externalFileFormat.DateFormat = string.Empty;
            externalFileFormat.UseTypeDefault = true;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "UseTypeDefault", externalFileFormat.UseTypeDefault, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            if (db.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                //
                // Step 13. Create external file format with conflicting properties - format type is Orc and FirstRow.
                //
                externalFileFormat.FormatType = ExternalFileFormatType.Orc;
                externalFileFormat.SerDeMethod = string.Empty;
                externalFileFormat.FieldTerminator = string.Empty;
                externalFileFormat.StringDelimiter = string.Empty;
                externalFileFormat.DateFormat = string.Empty;
                externalFileFormat.UseTypeDefault = false;
                externalFileFormat.FirstRow = 10;

                // verify the external file format was not created
                try
                {
                    // attempt to create an external file format
                    errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "FirstRow", externalFileFormat.FirstRow, externalFileFormat.FormatType.ToString());

                    externalFileFormat.Create();

                    // validate expected exception and error message
                    Assert.Fail(errorMessage, externalFileFormat.Name);
                }
                catch (SmoException e)
                {
                    if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                    {
                        throw;
                    }
                }
            }

            //
            // Step 14. Create external file format with conflicting properties - format type is Parquet and SerDeMethod.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Parquet;
            externalFileFormat.SerDeMethod = externalFileFormatSerDeMethod;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "SerDeMethod", externalFileFormat.SerDeMethod, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 15. Create external file format with conflicting properties - format type is Parquet and FieldTerminator.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Parquet;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = externalFileFormatFieldTerminator;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "FieldTerminator", externalFileFormat.FieldTerminator, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 16. Create external file format with conflicting properties - format type is Parquet and StringDelimiter.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Parquet;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = externalFileFormatStringDelimiter;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "StringDelimiter", externalFileFormat.StringDelimiter, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 17. Create external file format with conflicting properties - format type Parquet and DateFormat.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Parquet;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = string.Empty;
            externalFileFormat.DateFormat = externalFileFormatDateFormat;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "DateFormat", externalFileFormat.DateFormat, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            //
            // Step 18. Create external file format with conflicting properties - format type is Parquet and UseTypeDefault.
            //
            externalFileFormat.FormatType = ExternalFileFormatType.Parquet;
            externalFileFormat.SerDeMethod = string.Empty;
            externalFileFormat.FieldTerminator = string.Empty;
            externalFileFormat.StringDelimiter = string.Empty;
            externalFileFormat.DateFormat = string.Empty;
            externalFileFormat.UseTypeDefault = true;

            // verify the external file format was not created
            try
            {
                // attempt to create an external file format
                errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "UseTypeDefault", externalFileFormat.UseTypeDefault, externalFileFormat.FormatType.ToString());

                externalFileFormat.Create();

                // validate expected exception and error message
                Assert.Fail(errorMessage, externalFileFormat.Name);
            }
            catch (SmoException e)
            {
                if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                {
                    throw;
                }
            }

            if (db.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse)
            {
                //
                // Step 19. Create external file format with conflicting properties - format type is Parquet and FirstRow.
                //
                externalFileFormat.FormatType = ExternalFileFormatType.Parquet;
                externalFileFormat.SerDeMethod = string.Empty;
                externalFileFormat.FieldTerminator = string.Empty;
                externalFileFormat.StringDelimiter = string.Empty;
                externalFileFormat.DateFormat = string.Empty;
                externalFileFormat.UseTypeDefault = false;
                externalFileFormat.FirstRow = 10;

                // verify the external file format was not created
                try
                {
                    // attempt to create an external file format
                    errorMessage = string.Format("Cannot set the property '{0}' to '{1}' because the property '{0}' is not supported for external file format type '{2}'.", "FirstRow", externalFileFormat.FirstRow, externalFileFormat.FormatType.ToString());

                    externalFileFormat.Create();

                    // validate expected exception and error message
                    Assert.Fail(errorMessage, externalFileFormat.Name);
                }
                catch (SmoException e)
                {
                    if (!ExceptionHelpers.IsExpectedException(e, errorMessage))
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Processes optional properties for each supported file format types
        /// and adds them to the to the T-SQL script.
        /// </summary>
        /// <param name="externalFileFormat"></param>
        /// <param name="script"></param>
        /// /// <param name="isSqlDw">True if SQL DW DB.</param>
        private void ProcessOptionalProperties(ExternalFileFormat externalFileFormat, StringBuilder script, bool isSqlDw)
        {
            switch (externalFileFormat.FormatType)
            {
                case ExternalFileFormatType.DelimitedText:
                    ValidateDelimitedTextProperties(externalFileFormat, script, isSqlDw);
                    break;
                case ExternalFileFormatType.Orc:
                case ExternalFileFormatType.Parquet:
                    ValidateOrcOrParquetProperties(externalFileFormat, script);
                    break;
                case ExternalFileFormatType.RcFile:
                    ValidateRcFileProperties(externalFileFormat, script);
                    break;
                default:
                    // if the format type set to any other value throw an exception
                    throw new WrongPropertyValueException(string.Format("Unknown enumeration for external file format type {0}", externalFileFormat.FormatType.ToString()));
            }
        }

        /// <summary>
        /// Validates optional properties for the DelimitedText or Orc file format
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="externalFileFormat">External file format.</param>
        /// <param name="script">The external file format T-SQL script.</param>
        /// <param name="isSqlDw">True if SQL DW DB.</param>
        private void ValidateDelimitedTextProperties(ExternalFileFormat externalFileFormat, StringBuilder script, bool isSqlDw)
        {
            // check for optional properties
            StringBuilder formatOptions = new StringBuilder();

            // if format options optional properties are specified, they need to be enclosed in FORMAT_OPTIONS()
            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.FieldTerminator, "FIELD_TERMINATOR = N'{0}'", formatOptions);
            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.StringDelimiter, "STRING_DELIMITER = N'{0}'", formatOptions);
            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.DateFormat, "DATE_FORMAT = N'{0}'", formatOptions);

            if (isSqlDw && externalFileFormat.FirstRow > 1)
            {
                if (formatOptions.Length > 0)
                {
                    formatOptions.Append(", ");
                }

                formatOptions.Append(string.Format("FIRST_ROW = {0}", externalFileFormat.FirstRow));
            }

            if (formatOptions.Length > 0)
            {
                formatOptions.Append(", ");
            }
            formatOptions.Append(string.Format("USE_TYPE_DEFAULT = {0}", externalFileFormat.UseTypeDefault));

            // if there were file format options specified, format them and add them to the script template
            string fileFormatOptions = formatOptions.ToString();
            if (!string.IsNullOrEmpty(fileFormatOptions))
            {
                script.Append(string.Format(", FORMAT_OPTIONS ({0})", fileFormatOptions));
            }

            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.DataCompression, "DATA_COMPRESSION = N'{0}'", script);
        }

        /// <summary>
        /// Validates optional properties for the Orc and Parquet file format
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="externalFileFormat">External file format.</param>
        /// <param name="script">The external file format T-SQL script.</param>
        private void ValidateOrcOrParquetProperties(ExternalFileFormat externalFileFormat, StringBuilder script)
        {
            // check for optional properties
            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.DataCompression, "DATA_COMPRESSION = N'{0}'", script);
        }

        /// <summary>
        /// Validates optional properties for the RcFile file format
        /// and adds them to the T-SQL script.
        /// </summary>
        /// <param name="externalFileFormat">External file format.</param>
        /// <param name="script">The external file format T-SQL script.</param>
        private void ValidateRcFileProperties(ExternalFileFormat externalFileFormat, StringBuilder script)
        {
            // check for optional properties
            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.SerDeMethod, "SERDE_METHOD = N'{0}'", script);

            VerifyOptionalFormatParameters(externalFileFormat, externalFileFormat.DataCompression, "DATA_COMPRESSION = N'{0}'", script);
        }

        /// <summary>
        /// Verifies format options optional properties and adds a comma to the generated script
        /// when more than one optional format properties exist.
        /// </summary>
        /// <param name="externalFileFormat">External file format.</param>
        /// <param name="propertyValue">The format options property value.</param>
        /// <param name="sqlScript">The T-SQL script to add to the format options script.</param>
        /// <param name="formatOptions">The T-SQL format options script.</param>
        private void VerifyOptionalFormatParameters(ExternalFileFormat externalFileFormat, string propertyValue, string sqlScript, StringBuilder formatOptions)
        {
            if (!string.IsNullOrEmpty(propertyValue))
            {
                // check if this is the first optional property by comparing the formatOptions length to 0
                // if is not 0, add a comma and a space to it
                if (formatOptions.Length > 0)
                {
                    formatOptions.Append(", ");
                }
                formatOptions.Append(string.Format(sqlScript, SmoObjectHelpers.SqlEscapeSingleQuote(propertyValue)));
            }
        }

        /// <summary>
        /// Converts an external file format type to the corresponding SQL Server keyword.
        /// </summary>
        /// <param name="fileFormatType">The external file format type.</param>
        /// <returns>SQL keyword for the given type.</returns>
        private string GetSqlKeywordForFileFormatType(ExternalFileFormatType fileFormatType)
        {
            switch (fileFormatType)
            {
                case ExternalFileFormatType.Orc:
                    return "ORC";
                case ExternalFileFormatType.Parquet:
                    return "PARQUET";
                case ExternalFileFormatType.RcFile:
                    return "RCFILE";
                default:
                    Assert.AreEqual(ExternalFileFormatType.DelimitedText, fileFormatType);
                    return "DELIMITEDTEXT";
            }
        }
    }
}
