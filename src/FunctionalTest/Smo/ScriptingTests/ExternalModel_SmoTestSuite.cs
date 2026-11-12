// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using _VSUT = Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.SqlServer.Management.SqlParser.MetadataProvider.MetadataProviderUtils.Names;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing External Model properties and scripting.
    /// </summary>
    [_VSUT.TestClass]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 17)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlManagedInstance, DatabaseEngineEdition.SqlOnDemand)]
    public class ExternalModel_SmoTestSuite : SmoObjectTestBase
    {
        #region Static Vars

        private static readonly string SetupScript =
     "IF NOT EXISTS (SELECT * FROM sys.external_models WHERE name = N'TestModel') " +
     "BEGIN " +
     "EXEC sp_executesql N'" +
     "CREATE EXTERNAL MODEL [TestModel] " +
     "WITH ( " +
     "LOCATION = ''https://models.example.com/testmodel.onnx'', " +
     "API_FORMAT = ''OpenAI'', " +
     "MODEL_TYPE = Embeddings, " +
     "MODEL = ''all-minilm'', " +
     "PARAMETERS = ''{\"valid\" : \"json\"}'', " +
     "CREDENTIAL = OpenAiMSCred " +
     ")' " +
     "END";

        #endregion

        #region Database Test Helpers

        /// <summary>
        /// Runs the setup scripts for the specified targetServer.
        /// </summary>
        /// <param name="db">Database.</param>
        internal static void SetupDb(_SMO.Database db)
        {
            TraceHelper.TraceInformation($"Setting up database {db.Name}");

            // Create the credential required for external model
            db.ExecuteNonQuery(
                "IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = N'OpenAiMSCred') " +
                "BEGIN " +
                "CREATE DATABASE SCOPED CREDENTIAL OpenAiMSCred " +
                "WITH IDENTITY = 'System Managed Identity'; " +
                "END");

            db.ExecuteNonQuery(SetupScript);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Test Create, Alter, and Drop of ExternalModel.
        /// </summary>
        /// <param name="db"></param>
        private void TestCreateAlterDrop(_SMO.Database db)
        {
            // Ensure login and user exist
            db.ExecuteNonQuery($@"
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = N'NewUserLogin')
BEGIN
    CREATE LOGIN [NewUserLogin] WITH PASSWORD = '{SqlTestRandom.GeneratePassword()}';
END;

IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = N'NewUser')
BEGIN
    CREATE USER [NewUser] FOR LOGIN [NewUserLogin];
END;
");

            // Ensure credential exists
            db.ExecuteNonQuery(@"
IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = N'OpenAiMSCred')
BEGIN
    CREATE DATABASE SCOPED CREDENTIAL OpenAiMSCred
    WITH IDENTITY = 'System Managed Identity';
END;
");

            // Grant permission on credential to the new user
            db.ExecuteNonQuery(@"
GRANT REFERENCES ON DATABASE SCOPED CREDENTIAL::OpenAiMSCred TO [NewUser];
");
            // Create a new external model
            var modelName = GenerateUniqueSmoObjectName("ExternalModel");
            var model = new _SMO.ExternalModel(db, modelName)
            {
                Location = "https://models.example.com/testmodel.onnx",
                ApiFormat = "OpenAI",
                ModelType = ExternalModelType.Embeddings,
                Model = "all-minilm",
                Parameters = "{\"valid\" : \"json\"}",
                Credential = "OpenAiMSCred",
                Owner = "NewUser"
            };

            model.Create();
            db.ExternalModels.Refresh();

            // Verify model exists in SMO collection
            Assert.That(db.ExternalModels.Cast<_SMO.ExternalModel>().Select(m => m.Name), Has.Member(model.Name),
                $"Model {model.Name} is not created in setup.");

            // Verify model exists in system catalog
            var dataSet = db.ExecuteWithResults($"SELECT * FROM sys.external_models WHERE name = '{SmoObjectHelpers.SqlEscapeSingleQuote(model.Name)}'");
            var table = dataSet.Tables[0];
            Assert.That(table.Rows.Count, Is.EqualTo(1), $"The model {model.Name} is not created.");

            // Get principal_id from the table
            var principalId = Convert.ToInt32(table.Rows[0]["principal_id"]);

            // Resolve OwnerName using USER_NAME(principal_id)
            var ds = db.ExecuteWithResults($"SELECT USER_NAME({principalId}) AS OwnerName");
            var ownerName = ds.Tables[0].Rows[0]["OwnerName"].ToString();

            // Assert OwnerName matches expected
            Assert.That(ownerName, Is.EqualTo(model.Owner),
                $"Expected Owner in sys.external_models to be '{model.Owner}', but got '{ownerName}'.");

            // Alter all properties
            var newLocation = "https://models.example.com/updatedmodel.onnx";
            var newApiFormat = "Ollama";
            var newModelType = ExternalModelType.Embeddings;
            var newModelName = "updated-minilm";
            var newParameters = "{\"updated\":\"json\"}";
            var newCredential = "OpenAiMSCred"; // Keep same or change if needed

            model.Location = newLocation;
            model.ApiFormat = newApiFormat;
            model.ModelType = newModelType;
            model.Model = newModelName;
            model.Parameters = newParameters;
            model.Credential = newCredential;

            model.Alter();
            model.Refresh();

            // Verify all updated properties
            var updatedModel = db.ExternalModels[model.Name];
            Assert.That(updatedModel.Location, Is.EqualTo(newLocation), $"Expected Location to be updated to {newLocation}, but got {updatedModel.Location}");
            Assert.That(updatedModel.ApiFormat, Is.EqualTo(newApiFormat), $"Expected ApiFormat to be updated to {newApiFormat}, but got {updatedModel.ApiFormat}");
            Assert.That(updatedModel.ModelType, Is.EqualTo(newModelType), $"Expected ModelType to be updated to {newModelType}, but got {updatedModel.ModelType}");
            Assert.That(updatedModel.Model, Is.EqualTo(newModelName), $"Expected Model to be updated to {newModelName}, but got {updatedModel.Model}");
            Assert.That(updatedModel.Parameters, Is.EqualTo(newParameters), $"Expected Parameters to be updated to {newParameters}, but got {updatedModel.Parameters}");
            Assert.That(updatedModel.Credential, Is.EqualTo(newCredential), $"Expected Credential to be updated to {newCredential}, but got {updatedModel.Credential}");

            // Drop the model
            model.Drop();
            VerifyIsSmoObjectDropped(model, db);
        }

        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.ExternalModel model = (_SMO.ExternalModel)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.ExternalModels.Refresh();
            Assert.That(database.ExternalModels[model.Name], Is.Null,
                $"Model {model.Name} is not dropped with DropIfExists.");

            var dataSet = database.ExecuteWithResults($"SELECT * FROM sys.external_models WHERE name = '{SmoObjectHelpers.SqlEscapeSingleQuote(model.Name)}'");
            var table = dataSet.Tables[0];
            Assert.That(table.Rows.Count, Is.EqualTo(0), $"The model {model.Name} is not dropped.");
        }

        #endregion

        #region Scripting Tests

        /// <summary>
        /// Validate that the Properties for ExternalModel can be enumerated.
        /// Also validates that the state of the object is kept in sync after creation.
        /// </summary>
        [_VSUT.TestMethod]
        public void ExternalModel_Can_Enumerate_Properties()
        {
            this.ExecuteWithDbDrop(
                "ExternalModelProperties",
                db =>
                {
                    // Ensure credential exists
                    db.ExecuteNonQuery(
                        "IF NOT EXISTS (SELECT * FROM sys.database_scoped_credentials WHERE name = N'OpenAiMSCred') " +
                        "BEGIN " +
                        "CREATE DATABASE SCOPED CREDENTIAL OpenAiMSCred " +
                        "WITH IDENTITY = 'System Managed Identity'; " +
                        "END");

                    var model = new _SMO.ExternalModel(db, "MyModel")
                    {
                        Location = "https://models.example.com/model.onn677yx",
                        ApiFormat = "OpenAI",
                        ModelType = ExternalModelType.Embeddings,
                        Model = "all-minilm",
                        Parameters = "{\"valid\":\"json\"}",
                        Credential = "OpenAiMSCred"
                    };

                    // Validate initial state
                    Assert.That(model.State, Is.EqualTo(_SMO.SqlSmoState.Creating), "Unexpected state before creation");

                    // Create the model
                    model.Create();
                    Assert.That(model.State, Is.EqualTo(_SMO.SqlSmoState.Existing), "Unexpected state after creation");

                    // Enumerate properties
                    Assert.DoesNotThrow(() => model.Properties.Cast<_SMO.Property>().ToArray(),
                        "It should be possible to enumerate the properties of an ExternalModel object");

                    // Validate property values
                    Assert.That(model.Location, Is.EqualTo("https://models.example.com/model.onn677yx"), "Location mismatch");
                    Assert.That(model.ApiFormat, Is.EqualTo("OpenAI"), "ApiFormat mismatch");
                    Assert.That(model.ModelType, Is.EqualTo(ExternalModelType.Embeddings), "ModelType mismatch");
                    Assert.That(model.Model, Is.EqualTo("all-minilm"), "Model mismatch");
                    Assert.That(model.Parameters, Is.EqualTo("{\"valid\":\"json\"}"), "Parameters mismatch");
                    Assert.That(model.Credential, Is.EqualTo("OpenAiMSCred"), "Credential mismatch");
                });
        }

        #endregion

        #region Create/Alter/Drop Tests

        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void ExternalModel_TestCreateAlterDrop()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);
                    TestCreateAlterDrop(database);
                });
        }

        [_VSUT.TestMethod]
        [UnsupportedHostPlatform(SqlHostPlatforms.Linux)]
        [SqlTestCategory(SqlTestCategory.Staging)]
        public void SmoDropIfExists_ExternalModel()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    SetupDb(database);
                    _SMO.ExternalModel model = new _SMO.ExternalModel(database, "MyModel")
                    {
                        Location = "https://models.example.com/model.onn677yx",
                        ApiFormat = "OpenAI",
                        ModelType = ExternalModelType.Embeddings,
                        Model = "all-minilm",
                        Parameters = "{\"valid\":\"json\"}",
                        Credential = "OpenAiMSCred"
                    };
                    VerifySmoObjectDropIfExists(model, database);
                });
        }

        #endregion
    }
}
