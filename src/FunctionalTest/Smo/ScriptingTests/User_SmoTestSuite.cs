// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Microsoft.SqlServer.Test.Manageability.Utils.Helpers;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing User properties and scripting
    /// </summary>
    [TestClass]
    public class User_SmoTestSuite : SmoObjectTestBase
    {
        #region Scripting Tests

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(_SMO.SqlSmoObject obj, _SMO.SqlSmoObject objVerify)
        {
            _SMO.User user = (_SMO.User)obj;
            _SMO.Database database = (_SMO.Database)objVerify;

            database.Users.Refresh();
            Assert.IsNull(database.Users[user.Name],
                          "Current user not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping an user with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_User_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    _SMO.User user = new _SMO.User(database, GenerateSmoObjectName("user"));
                    _SMO.Login login = new _SMO.Login(server, GenerateUniqueSmoObjectName("login"));

                    login.LoginType = _SMO.LoginType.SqlLogin;

                    user.Login = login.Name;

                    string userScriptDropIfExistsTemplate = "DROP USER IF EXISTS [{0}]";
                    string userScriptDropIfExists = string.Format(userScriptDropIfExistsTemplate, user.Name);
                    try
                    {
                        login.Create(Guid.NewGuid().ToString());

                        VerifySmoObjectDropIfExists(user, database, userScriptDropIfExists);
                    }
                    finally
                    {
                        login.DropIfExists();
                    }
                });
        }

        /// <summary>
        /// Verify Alter User generates the correct script for updating Login or Login and Default Schema, based on the multipleOptions parameter.
        /// </summary>
        /// <param name="multipleOptions">Should the test run alter for more than one option, ie Login and Default Schema vs only Login</param>
        private void SmoAlter_UserWithLogin_Alter (bool multipleOptions = false)
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    var server = database.Parent;
                    var login = new _SMO.Login(server, "login" + (this.TestContext.TestName ?? "")) { LoginType = _SMO.LoginType.SqlLogin };
                    var user = new _SMO.User(database, "user" + (this.TestContext.TestName ?? "")) { Login = login.Name };
                    var newLogin = "newLogin";

                    try
                    {
                        login.Create(Guid.NewGuid().ToString());
                        user.Create();

                        user.Login = newLogin;
                        string expectedUserScriptAlterUserWithLogin;
                        if (multipleOptions)
                        {
                            var newSchema = "newSchema";
                            user.DefaultSchema = newSchema;
                            expectedUserScriptAlterUserWithLogin = $"ALTER USER [{user.Name}] WITH DEFAULT_SCHEMA=[{newSchema}], LOGIN=[{newLogin}]";
                        }
                        else
                        {
                            expectedUserScriptAlterUserWithLogin = $"ALTER USER [{user.Name}] WITH LOGIN=[{newLogin}]";
                        }

                        var script = database.ExecutionManager.RecordQueryText(user.Alter);

                        // the constraints don't like carriage returns/line feeds. SMO seems to use both \r\n and \n
                        string actualScript = script.ToSingleString().FixNewLines().TrimEnd();

                        // modify the script to remove the 'USE' statement from the first portion and retain only the text following the 'ALTER' command to verify it matches the expected string
                        actualScript = actualScript.Contains("ALTER") ? actualScript.Substring(actualScript.IndexOf("ALTER")) : string.Empty;

                        // the header has the current timestamp, so just skip most of it
                        Assert.That(actualScript,
                                    Does.EndWith(expectedUserScriptAlterUserWithLogin),
                                    string.Format("Wrong ALTER script for {0}, actual script is not ending with expected script.", user.GetType().Name));
                    }
                    finally
                    {
                        // DropIfExists supported in SQL 2016 and later versions
                        if (ServerContext.VersionMajor < 13)
                        {
                            user.Drop();
                            login.Drop();
                        }
                        else
                        {
                            user.DropIfExists();
                            login.DropIfExists();
                        }
                    }
                });
        }

        /// <summary>
        /// Tests scripting an alter to existing user's login information through SMO.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 12)]
        public void SmoAlter_UserWithLogin_LoginOptionAlter()
        {
            SmoAlter_UserWithLogin_Alter();
        }

        /// <summary>
        /// Tests scripting an alter to existing user's login information and default schema through SMO.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise, MinMajor = 12)]
        public void SmoAlter_UserWithLogin_MultipleOptionsAlter()
        {
            SmoAlter_UserWithLogin_Alter(true);
        }

        #endregion

        /// <summary>
        ///
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDatabase)]
        [UnsupportedFeature(SqlFeature.Fabric)]
        public void User_creation_on_Azure_requires_password()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    var pwd = SqlTestRandom.GeneratePassword();
                    var user = database.CreateUser("containeduser" + Guid.NewGuid(), string.Empty, pwd);
                    var userExists = database.ExecutionManager.ExecuteScalar(string.Format("select top 1 1 from sys.sysusers where name='{0}'", user.Name));
                    Assert.That(userExists, Is.EqualTo(1), "user with password should be created in the database");
                    Assert.Throws<_SMO.FailedOperationException>(
                        () => database.CreateUser("containeduser" + Guid.NewGuid(), string.Empty), "user without password should not create");
                });
        }

        /// <summary>
        /// Test for scripting external users using ObjectId which checks if scripting is returning appropriate string.
        /// CreateSmoObject(user) won't work since the syntax call does the search of user in Azure Active Directory,
        /// so we only check the script string.
        /// </summary>
        [TestMethod]
        [SqlRequiredFeature(SqlFeature.AADLoginsSqlDB)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void SmoCreateFromExternalProviderUsingEntraObjectIdSQLDB_User()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.User user = new _SMO.User(database, GenerateUniqueSmoObjectName("user"));
                    user.UserType = _SMO.UserType.External;
                    user.ObjectId = Guid.NewGuid();

                    _SMO.ScriptingOptions so = new _SMO.ScriptingOptions();
                    string scriptUser = ScriptSmoObject((_SMO.IScriptable)user, so);
                    string expectedOutput = $"CREATE USER {user.FullQualifiedName} FROM  EXTERNAL PROVIDER  WITH OBJECT_ID = N'{user.ObjectId}'\r\n";
                    Assert.That(scriptUser, Is.EqualTo(expectedOutput), "CREATE USER syntax is not scripted correctly. This user type should include keywords 'FROM EXTERNAL PROVIDER USING OBJECT_ID'.");

                    user.DefaultSchema = "MySchema";
                    so = new _SMO.ScriptingOptions();
                    scriptUser = ScriptSmoObject((_SMO.IScriptable)user, so);
                    expectedOutput = $"CREATE USER {user.FullQualifiedName} FROM  EXTERNAL PROVIDER  WITH DEFAULT_SCHEMA=[{user.DefaultSchema}], OBJECT_ID = N'{user.ObjectId}'\r\n";
                    Assert.That(scriptUser, Is.EqualTo(expectedOutput), "CREATE USER syntax is not scripted correctly. This user type should include keywords 'FROM EXTERNAL PROVIDER USING OBJECT_ID'.");

                    so.ScriptDrops = true;
                    scriptUser = ScriptSmoObject((_SMO.IScriptable)user, so);
                    expectedOutput = $"DROP USER {user.FullQualifiedName}\r\n";
                    Assert.That(scriptUser, Is.EqualTo(expectedOutput), "DROP USER syntax is not scripted correctly.");
                });
        }

    }
}
