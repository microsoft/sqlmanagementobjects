// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using _SMO = Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Linq;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Management.Smo;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Test suite for testing Login properties and scripting
    /// </summary>
    //##[TestSuite(LabRunCategory.Gql, FeatureCoverage.Manageability)]
    [TestClass]
    public class Login_SmoTestSuite : SmoObjectTestBase
    {
#region Scripting Tests

        /// <summary>
        /// Create Smo object.
        /// <param name="obj">Smo object.</param>
        /// </summary>
        protected override void CreateSmoObject(SqlSmoObject obj)
        {
            Login login = (Login)obj;

            login.Create(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Verify that SMO object is dropped.
        /// <param name="obj">Smo object.</param>
        /// <param name="objVerify">Smo object used for verification of drop.</param>
        /// </summary>
        protected override void VerifyIsSmoObjectDropped(SqlSmoObject obj, SqlSmoObject objVerify)
        {
            Login login = (Login)obj;
            _SMO.Server server = (_SMO.Server)objVerify;

            server.Logins.Refresh();
            Assert.IsNull(server.Logins[login.Name],
                          "Current login not dropped with DropIfExists.");
        }

        /// <summary>
        /// Tests dropping a login with IF EXISTS option through SMO on SQL16 and later.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 13)]
        public void SmoDropIfExists_Login_Sql16AndAfterOnPrem()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    Login login = new Login(server,
                        GenerateUniqueSmoObjectName("login"));

                    login.LoginType = LoginType.SqlLogin;

                    try
                    {
                        VerifySmoObjectDropIfExists(login, server);
                    }
                    catch (Exception)
                    {
                        if (server.Logins[login.Name] != null)
                        {
                            login.Drop();
                        }
                        throw;
                    }
                });
        }

        /// <summary>
        /// Test for scripting external logins which checks if scripting is returning appropriate string.
        /// CreateSmoObject(login) won't work since the syntax call does the search of login in Azure Active Directory,
        /// so we only check the script string.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlManagedInstance)]
        public void SmoCreateFromExternalProvider_Login()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    Login login = new Login(server,
                        GenerateUniqueSmoObjectName("login"));
                    login.LoginType = LoginType.ExternalUser;

                    ScriptingOptions so = new ScriptingOptions();
                    string scriptLogin = ScriptSmoObject((IScriptable)login, so);
                    string expectedOutput = string.Format("CREATE LOGIN {0} FROM EXTERNAL PROVIDER\r\n", login.FullQualifiedName);
                    Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "CREATE LOGIN syntax is not scripted correctly. This login type should include keywords 'FROM EXTERNAL PROVIDER'.");

                    so.ScriptDrops = true;
                    scriptLogin = ScriptSmoObject((IScriptable)login, so);
                    expectedOutput = string.Format("DROP LOGIN {0}\r\n", login.FullQualifiedName);
                    Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "DROP LOGIN syntax is not scripted correctly.");
                });
        }

        /// <summary>
        /// Test for scripting external logins which checks if scripting is returning appropriate string on SQL22 and later.
        /// CreateSmoObject(login) won't work since the syntax call does the search of login in Azure Active Directory,
        /// so we only check the script string.
        /// </summary>
        [TestMethod]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.Standalone, MinMajor = 16)]
        public void SmoCreateFromExternalProviderOnPrem_Login()
        {
            this.ExecuteWithDbDrop(
                database =>
                {
                    _SMO.Server server = database.Parent;
                    Login login = new Login(server,
                        GenerateUniqueSmoObjectName("login"));
                    login.LoginType = LoginType.ExternalUser;

                    ScriptingOptions so = new ScriptingOptions();
                    string scriptLogin = ScriptSmoObject((IScriptable)login, so);
                    string expectedOutput = string.Format("CREATE LOGIN {0} FROM EXTERNAL PROVIDER\r\n", login.FullQualifiedName);
                    Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "CREATE LOGIN syntax is not scripted correctly. This login type should include keywords 'FROM EXTERNAL PROVIDER'.");

                    so.ScriptDrops = true;
                    scriptLogin = ScriptSmoObject((IScriptable)login, so);
                    expectedOutput = string.Format("DROP LOGIN {0}\r\n", login.FullQualifiedName);
                    Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "DROP LOGIN syntax is not scripted correctly.");
                });
        }

        /// <summary>
        /// Test for scripting external logins which checks if scripting is returning appropriate string.
        /// CreateSmoObject(login) won't work since the syntax call does the search of login in Azure Active Directory,
        /// so we only check the script string.
        /// </summary>
        [TestMethod]
        [SqlRequiredFeature(SqlFeature.AADLoginsSqlDB)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void SmoCreateFromExternalProviderSQLDB_Login()
        {
            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);
                Login login = new Login(server,
                    GenerateUniqueSmoObjectName("login"));
                login.LoginType = LoginType.ExternalUser;

                ScriptingOptions so = new ScriptingOptions();
                string scriptLogin = ScriptSmoObject((IScriptable)login, so);
                string expectedOutput = $"CREATE LOGIN {login.FullQualifiedName} FROM EXTERNAL PROVIDER\r\n";
                Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "CREATE LOGIN syntax is not scripted correctly. This login type should include keywords 'FROM EXTERNAL PROVIDER'.");

                so.ScriptDrops = true;
                scriptLogin = ScriptSmoObject((IScriptable)login, so);
                expectedOutput = $"DROP LOGIN {login.FullQualifiedName}\r\n";
                Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "DROP LOGIN syntax is not scripted correctly.");
            });
        }

        /// <summary>
        /// Test to verify the collection for AzureSterling is fetching the External Logins
        /// </summary>
        [TestMethod]
        [SqlRequiredFeature(SqlFeature.AADLoginsSqlDB)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void Verify_external_logins_in_Server_AzureSqlDB()
        {
            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);
                server.Logins.Refresh();
                Assert.That(server.Logins.Cast<Login>().Select(l => l.LoginType), Has.Member(LoginType.ExternalUser));
            });
        }

        /// <summary>
        /// marked as Legacy because the EnumDatabaseMappings query doesn't seem to be deterministic. 
        /// https://github.com/microsoft/sqlmanagementobjects/issues/37
        /// </summary>
        [TestMethod]
        [TestCategory("Legacy")]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        public void Login_apis_work_correctly()
        {
            ExecuteFromDbPool(db =>
            {
                var login = new Login(db.Parent, GenerateUniqueSmoObjectName("login"))
                {
                    LoginType = LoginType.SqlLogin,

                };
                var pwd = Guid.NewGuid().ToString();
                login.Create(pwd);
                var credential = new Credential(db.Parent, GenerateUniqueSmoObjectName("login")) { Identity = "someidentity" };
                var dbUser = db.CreateUser("loginUser", login.Name);
                try
                {
                    Assert.That(login.EnumDatabaseMappings()?.Select(m => m.DBName), Has.Member(db.Name), "login.EnumDatabaseMappings");
                    credential.Create();
                    var role = db.Parent.Roles.Cast<ServerRole>().First(r => !r.EnumMemberNames().Contains(login.Name) && r.Name != "bulkadmin");
                    login.AddToRole(role.Name);
                    Assert.Multiple(() =>
                    {
                        Assert.That(role.EnumMemberNames().Cast<string>(), Has.Member(login.Name), $"AddToRole({role.Name})");
                        Assert.That(login.IsMember(role.Name), Is.True, $"IsMember({role.Name}) after AddToRole");
                    });
                    role.DropMember(login.Name);
                    Assert.Multiple(() =>
                    {
                        Assert.That(role.EnumMemberNames().Cast<string>(), Has.No.Member(login.Name), $"role.DropMember({login.Name})");
                        Assert.That(login.IsMember(role.Name), Is.False, $"IsMember({role.Name}) after role.DropMember");
                        Assert.That(login.GetDatabaseUser(db.Name), Is.EqualTo(dbUser.Name), $"GetDatabaseUser({db.Name})");
                    });
                    var credentials = login.EnumCredentials();
                    Assert.That(credentials.Cast<string>(), Is.Empty, "EnumCredentials before AddCredential");
                    login.AddCredential(credential.Name);
                    credentials = login.EnumCredentials();
                    Assert.That(credentials.Cast<string>(), Is.EqualTo(new[] { credential.Name }), $"EnumCredentials after AddCredential({credential.Name})");
                    login.DropCredential(credential.Name);
                    credentials = login.EnumCredentials();
                    Assert.That(credentials.Cast<string>(), Is.Empty, $"EnumCredentials after DropCredential({credential.Name})");

                    var isDisabled = login.IsDisabled;
                    if (isDisabled)
                    {
                        login.Enable();
                        login.Refresh();
                        Assert.That(login.IsDisabled, Is.False, "IsDisabled after Enable");
                        login.Disable();
                        login.Refresh();
                        Assert.That(login.IsDisabled, Is.True, "IsDisabled after Disable");
                    }
                    else
                    {
                        login.Disable();
                        login.Refresh();
                        Assert.That(login.IsDisabled, Is.True, "IsDisabled after Disable");
                        login.Enable();
                        login.Refresh();
                        Assert.That(login.IsDisabled, Is.False, "IsDisabled after Enable");
                    }

                }
                finally
                {
                    try
                    {
                        if (dbUser?.State == SqlSmoState.Existing)
                        {
                            dbUser.Drop();
                        }
                        if (credential?.State == SqlSmoState.Existing)
                        {
                            credential.Drop();
                        }
                        if (login?.State == SqlSmoState.Existing)
                        {
                            login.Drop();
                        }
                    }
                    catch
                    {
                    }
                }
            });
        }

        [TestMethod]
        [SupportedServerVersionRange(Edition = DatabaseEngineEdition.Enterprise)]
        public void Login_ChangePassword_succeeds_for_admin_and_currentuser()
        {
            ExecuteTest(() =>
            {
                var login = new Login(ServerContext, GenerateUniqueSmoObjectName("login"))
                {
                    LoginType = LoginType.SqlLogin,
                    PasswordPolicyEnforced = false

                };
                var pwd = Guid.NewGuid().ToString();
                login.Create(pwd);
                try
                {
                    if (login.IsDisabled)
                    {
                        login.Enable();
                    }
                    var newConnectionString = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ConnectionString)
                    {
                        UserID = login.Name,
                        Password = pwd,
                        IntegratedSecurity = false,
                        Pooling = false
                    };
                    using (var lowPrivConnection = new SqlConnection(newConnectionString.ConnectionString))
                    {
                        var newServer = new _SMO.Server(new ServerConnection(lowPrivConnection));
                        try
                        {
                            var lowPrivLogin = newServer.Logins[login.Name];
                            Assert.That(() => lowPrivLogin.ChangePassword(Guid.NewGuid().ToString()), Throws.InstanceOf<SmoException>(), "ChangePassword(pwd) as low privilege user requires old password");
                            var newPwd = Guid.NewGuid().ToString();
                            lowPrivLogin.ChangePassword(oldPassword: pwd, newPassword: newPwd);
                        }
                        finally
                        {
                            newServer.ConnectionContext.ForceDisconnected();
                        }
                    }
                    using (var newConnection = new SqlConnection(newConnectionString.ConnectionString))
                    {
                        var sqlException = Assert.Throws<SqlException>(newConnection.Open, "SqlConnection.Open with the old password after low privilege user changed password");
                        Assert.That(sqlException.Number, Is.EqualTo(18456), "SqlException.Number for login failure");
                    }
                }
                finally
                {
                    login.Drop();
                }
            });
        }

        /// <summary>
        /// Test for scripting external logins using ObjectId which checks if scripting is returning appropriate string.
        /// CreateSmoObject(login) won't work since the syntax call does the search of login in Azure Active Directory,
        /// so we only check the script string.
        /// </summary>
        [TestMethod]
        [SqlRequiredFeature(SqlFeature.AADLoginsSqlDB)]
        [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, Edition = DatabaseEngineEdition.SqlDatabase)]
        public void SmoCreateFromExternalProviderUsingEntraObjectIdSQLDB_Login()
        {
            ExecuteTest(() =>
            {
                var server = new _SMO.Server(this.ServerContext.ConnectionContext);
                Login login = new Login(server,
                    GenerateUniqueSmoObjectName("login"));
                login.LoginType = LoginType.ExternalUser;
                login.ObjectId = Guid.NewGuid();

                ScriptingOptions so = new ScriptingOptions();
                string scriptLogin = ScriptSmoObject((IScriptable)login, so);
                string expectedOutput = $"CREATE LOGIN {login.FullQualifiedName} FROM EXTERNAL PROVIDER WITH OBJECT_ID = N'{login.ObjectId}'\r\n";
                Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "CREATE LOGIN syntax is not scripted correctly. This login type should include keywords 'FROM EXTERNAL PROVIDER USING OBJECT_ID'.");

                so.ScriptDrops = true;
                scriptLogin = ScriptSmoObject((IScriptable)login, so);
                expectedOutput = $"DROP LOGIN {login.FullQualifiedName}\r\n";
                Assert.That(scriptLogin, Is.EqualTo(expectedOutput), "DROP LOGIN syntax is not scripted correctly.");
            });
        }
#endregion // Scripting Tests
    }
}

