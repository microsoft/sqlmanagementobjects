// Copyright (c) Microsoft.
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

        #endregion

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase,
            Edition = DatabaseEngineEdition.SqlDatabase)]
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

    }
}
