// Copyright (c) Microsoft.
// Licensed under the MIT license.
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NUnit.Framework;
using Assert=NUnit.Framework.Assert;

namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    /// <summary>
    /// Tests that verify proper use of the AccessToken property by a ServerConnection object
    /// </summary>
    [TestClass]
    public class ServerConnectionTests
    {
        [TestMethod]
        [TestCategory("Unit")]
        public void When_AccessToken_is_null_SqlConnectionObject_has_null_AccessToken()
        {
            var connectionString = "Data Source=foo";
            var serverConnection = new ServerConnection(new SqlConnection(connectionString));
            Assert.That(serverConnection.SqlConnectionObject.AccessToken, Is.Null, "Non-null AccessToken");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_AccessToken_is_not_null_SqlConnectionObject_has_valid_AccessToken()
        {
            var renewableToken = new Mock<IRenewableToken>();
            renewableToken.Setup(t => t.GetAccessToken()).Returns("mytoken");
            var serverConnection = new ServerConnection(renewableToken.Object);
            Assert.That(serverConnection.SqlConnectionObject.AccessToken, Is.EqualTo("mytoken"),
                "Unexpected AccessToken");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_AccessToken_is_in_config_SqlConnectionObject_has_valid_AccessToken()
        {
            var renewableToken = new Mock<IRenewableToken>();
            renewableToken.Setup(t => t.GetAccessToken()).Returns("mytoken");
            var sci = new SqlConnectionInfo("myserver") {AccessToken = renewableToken.Object, UseIntegratedSecurity = false};
            var serverConnection = new ServerConnection(sci);
            Assert.That(serverConnection.SqlConnectionObject.AccessToken, Is.EqualTo("mytoken"), "Unexpected AccessToken");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerConnection_copies_AccessToken_from_source_ServerConnection()
        {
            var renewableToken = new Mock<IRenewableToken>();
            renewableToken.Setup(t => t.GetAccessToken()).Returns("mytoken");
            var serverConnectionSrc = new ServerConnection(renewableToken.Object);
            var sci = new SqlConnectionInfo(serverConnectionSrc, ConnectionType.Sql) {UseIntegratedSecurity = false};
            var serverConnection = new ServerConnection(sci);
            Assert.That(serverConnection.SqlConnectionObject.AccessToken, Is.EqualTo("mytoken"), "Unexpected AccessToken");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerConnection_constructor_throws_if_AccessToken_is_null_and_UseIntegratedSecurity_is_false()
        {
            var sci = new SqlConnectionInfo("serverName") {UseIntegratedSecurity = false};
            Assert.Throws<PropertyNotSetException>(() => new ServerConnection(sci),
                "ServerConnection(sci) didn't throw exception");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_AccessToken_changes_SqlConnectionObject_has_new_AccessToken_value()
        {
            var renewableToken = new Mock<IRenewableToken>();
            renewableToken.Setup(t => t.GetAccessToken()).Returns("token1");
            var serverConnection = new ServerConnection(renewableToken.Object);
            Assert.That(serverConnection.SqlConnectionObject.AccessToken, Is.EqualTo("token1"),
                "Unexpected AccessToken on first call");
            renewableToken.Setup(t => t.GetAccessToken()).Returns("token2");
            Assert.That(serverConnection.SqlConnectionObject.AccessToken, Is.EqualTo("token2"),
                "Unexpected AccessToken on second call");

        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_constructed_from_SqlConnection_with_sql_auth_LoginSecure_is_false()
        {
            var connBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "foobar",
                Password = "pwd",
                UserID = "userid",
                IntegratedSecurity = false
            };
            var serverConnection = new ServerConnection(new SqlConnection(connBuilder.ConnectionString));
            Assert.That(serverConnection.LoginSecure, Is.False, "LoginSecure should reflect connection string");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_SqlConnection_uses_SqlCredential_ServerConnection_copies_it()
        {
            var secureString = EncryptionUtility.EncryptString("badPassword");
            secureString.MakeReadOnly();
            var sqlCredential = new SqlCredential("userName", secureString);
            var sqlConnection = new SqlConnection("Data Source=fakeServer", sqlCredential);
            var connection1 = new ServerConnection(sqlConnection);
            Assert.That(connection1.Login, Is.EqualTo("userName"), "original ServerConnection.Login");
            Assert.That(connection1.Password, Is.EqualTo("badPassword"), "original ServerConnection.Password");

            // make sure the Credential isn't preserved
            Assert.That(connection1.SqlConnectionObject.Credential, Is.Null,
                "original ServerConnection.SqlConnectionObject.Credential");
            var connection2 = connection1.Copy();
            Assert.That(connection2.SqlConnectionObject.Credential, Is.Null,
                "cloned ServerConnection.SqlConnectionObject.Credential");
            Assert.That(connection2.Login, Is.EqualTo("userName"),
                "cloned ServerConnection.Login");
            Assert.That(connection2.Password,
                Is.EqualTo("badPassword"), "cloned ServerConnection.Password");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void When_assigned_ConnectionString_after_construction_Clone_is_correct()
        {
            var connBuilder = new SqlConnectionStringBuilder
            {
                DataSource = "foobar",
                Password = "pwd",
                UserID = "userid",
                IntegratedSecurity = false
            };
            var original = new ServerConnection() { ConnectionString = connBuilder.ConnectionString };
            var copy = original.Copy();
            var copyString = new SqlConnectionStringBuilder(copy.ConnectionString);
            Assert.That(copyString.DataSource, Is.EqualTo("foobar"), "DataSource");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void ServerConnection_copy_preserves_encryption_settings()
        {
            var connInfo = new SqlConnectionInfo("someserver") { EncryptConnection = true, TrustServerCertificate = true };
            var connInfoCopy = new SqlConnectionInfo(connInfo);
            Assert.That(connInfoCopy.TrustServerCertificate, Is.True, "TrustServerCertificate copied to SqlConnectionInfo");
            Assert.That(connInfoCopy.EncryptConnection, Is.True, "EncryptConnection copied to SqlConnectionInfo");
            var serverConnection = new ServerConnection(connInfo);
            (serverConnection as ISfcConnection).ForceDisconnected();
            Assert.That(serverConnection.TrustServerCertificate, Is.True, "ServerConnection copies TrustServerCertificate from SqlConnectionInfo");
            Assert.That(serverConnection.EncryptConnection, Is.True, "ServerConnection copies EncryptConnection from SqlConnectionInfo");
            var databaseConnection = serverConnection.GetDatabaseConnection("somedb", poolConnection: false);
            Assert.That(databaseConnection.TrustServerCertificate, Is.True, "GetDatabaseConnection copies TrustServerCertificate");
            Assert.That(databaseConnection.EncryptConnection, Is.True, "GetDatabaseConnection copies EncryptConnection");
            var actualConnectionString = new SqlConnectionStringBuilder(databaseConnection.SqlConnectionObject.ConnectionString);
            // using == true to force use of implicit boolean operator if needed
            Assert.That(actualConnectionString.Encrypt == true, Is.True, "GetDatabaseConnection connection Encrypt=true");
            Assert.That(actualConnectionString.TrustServerCertificate, Is.True, "GetDatabaseConnection connection TrustServerCertificate=true");
        }
    }
}
