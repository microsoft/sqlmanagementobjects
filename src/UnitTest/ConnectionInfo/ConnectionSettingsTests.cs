// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Linq;
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using System.Diagnostics;

namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    [TestClass]
    public class ConnectionSettingsTests : Test.UnitTestBase
    {
        
        [TestMethod]
        [TestCategory("Unit")]
        public void VerifyInteractiveModeConnectionSettings()
        {
            string connectionString;
            string userName = "test@test.com";
           
            var settings = new ConnectionSettings()
            {
                Authentication = SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive,
                LoginSecure = false,
                ServerInstance = "SqlServerName"
            };


#if !MICROSOFTDATA
            Assert.Throws<Microsoft.SqlServer.Management.Common.PropertyNotSetException>(
            delegate { connectionString = settings.ConnectionString; },
            "Expect Property Not Set Exception");
            // Setup user name

#endif
            settings.Login = userName;
            connectionString = settings.ConnectionString;
            Assert.That(connectionString, Does.Contain(userName).IgnoreCase, "Connection string should have user name set");
                                
                //test that you can create a sqlconnection object with the connection string
                var conn = new SqlConnection(connectionString);
                Assert.That(conn.ConnectionString == connectionString, "Connection string should be set in SqlConnection");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SqlConnectionInfo_supports_ApplicationIntent()
        {
            Assert.That(SqlConnectionInfo.IsApplicationIntentKeywordSupported(), Is.True,
                "IsApplicationIntentKeywordSupported");
            var connectionInfo = new SqlConnectionInfo("myserver")
            {
                ApplicationIntent = "Readonly"
            };
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.ApplicationIntent, Is.EqualTo(ApplicationIntent.ReadOnly),
                "Readonly string should map to ReadOnly ApplicationIntent");
            connectionInfo.ApplicationIntent = "invalidValue";
            connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.ApplicationIntent, Is.EqualTo(ApplicationIntent.ReadWrite),
                "Unknown value should map to the default ReadWrite");
        }

#if MICROSOFTDATA
        [TestMethod]
        [TestCategory("Unit")]
        public void SqlConnectionInfo_supports_HostNameInCertificate()
        {
            string testdomain = "exmaple.net";
            var connectionInfo = new SqlConnectionInfo("myserver")
            {
                EncryptConnection = true,
                HostNameInCertificate = testdomain
            };
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.HostNameInCertificate, Is.EqualTo(testdomain),
                "HostNameInCertificate should be set to provided value.");

            connectionInfo.HostNameInCertificate = null;
            connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.HostNameInCertificate, Is.EqualTo(String.Empty),
                "Setting HostNameInCertificate to null should be handled.");

            connectionInfo.HostNameInCertificate = String.Empty;
            connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.HostNameInCertificate, Is.EqualTo(String.Empty),
                "Setting HostNameInCertificate to empty string should be handled.");
        }

        [TestMethod]
        [TestCategory("Unit")]
        public void SqlConnectionInfo_supports_ServerCertificate()
        {
            string testPath = "C:\\Path\\";
            var connectionInfo = new SqlConnectionInfo("myserver")
            {
                EncryptConnection = true,
                ServerCertificate = testPath
            };
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.ServerCertificate, Is.EqualTo(testPath),
                "ServerCertificate should be set to provided value.");

            connectionInfo.ServerCertificate = null;
            connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.ServerCertificate, Is.EqualTo(String.Empty),
                "Setting ServerCertificate to null should be handled.");

            connectionInfo.ServerCertificate = String.Empty;
            connectionStringBuilder = new SqlConnectionStringBuilder(connectionInfo.ConnectionString);
            Assert.That(connectionStringBuilder.ServerCertificate, Is.EqualTo(String.Empty),
                "Setting ServerCertificate to empty string should be handled.");
        }
#endif

        [TestMethod]
        [TestCategory("Unit")]
        public void ConnectionSettings_handles_strict_encryption()
        {
            var conn = new ConnectionSettings()
            {
                StrictEncryption = true
            };
            var connStr = new SqlConnectionStringBuilder(conn.ConnectionString);
            // Using == true forces use of the implicit bool operator
            Assert.That(connStr.Encrypt == true, Is.True, "Setting StrictEncryption sets SqlConnectionStringBuilder.Encrypt to true");
#if MICROSOFTDATA
            Assert.That(connStr.Encrypt, Is.EqualTo(SqlConnectionEncryptOption.Strict), "Setting StrictEncryption sets Encrypt=Strict");
#endif
            conn = new ConnectionSettings()
            {
                EncryptConnection = true
            };
            connStr = new SqlConnectionStringBuilder(conn.ConnectionString);
            Assert.That(connStr.Encrypt == true, Is.True, "Setting EncryptConnection=true sets SqlConnectionStringBuilder.Encrypt to true");
#if MICROSOFTDATA
            Assert.That(connStr.Encrypt, Is.EqualTo(SqlConnectionEncryptOption.Mandatory), "Setting EncryptConnection=true sets Encrypt=Mandatory");
#endif
            var sqlConnectionInfo = new SqlConnectionInfo("someserver") { StrictEncryption = true };
            var connFromInfo = new ConnectionSettings(sqlConnectionInfo);
            Assert.That(connFromInfo.StrictEncryption, Is.True, "ConnectionSettings should copy StrictEncryption value from SqlConnectionInfo");
            var sqlConnectionInfoCopy = new SqlConnectionInfo(sqlConnectionInfo);
            Assert.That(sqlConnectionInfoCopy.StrictEncryption, Is.True, "SqlConnectionInfo copy constructor should copy StrictEncryption value");

            var sqlConnection = new SqlConnection(new SqlConnectionStringBuilder()
            {
#if MICROSOFTDATA
                Encrypt = SqlConnectionEncryptOption.Strict
#else
                Encrypt = true
#endif
            }.ConnectionString);
            var serverConnection = new ServerConnection(sqlConnection);
            Assert.That(serverConnection.EncryptConnection, Is.True, "ServerConnection.EncryptConnection from SqlConnection");
#if MICROSOFTDATA
            Assert.That(serverConnection.StrictEncryption, Is.True, "ServerConnection.StrictEncryption from SqlConnection");
#else
            Assert.That(serverConnection.StrictEncryption, Is.False, "ServerConnection.StrictEncryption from SqlConnection");
#endif
            Assert.Throws<PropertyNotAvailableException>(() => serverConnection.StrictEncryption = false, 
                "Setting StrictEncryption after assigning ConnectionString should throw");
        }
    

        private const string username = "someuser";
        private const string pwd = "placeholderpwd";
        [TestMethod]
        [TestCategory("Unit")]
        [DataTestMethod]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.NotSpecified, username, pwd, null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.NotSpecified, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.NotSpecified, "", "", typeof(PropertyNotSetException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.SqlPassword, username, pwd, null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.SqlPassword, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.SqlPassword, "", "", typeof(PropertyNotSetException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated, username, pwd, null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryIntegrated, "", "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword, username, pwd, null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryPassword, "", "", typeof(PropertyNotSetException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive, username, pwd, typeof(System.ArgumentException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive, username, "", null)]
#if MICROSOFTDATA
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive, "", "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDefault, username, pwd, typeof(System.ArgumentException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDefault, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDefault, "", "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDeviceCodeFlow, username, pwd, null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDeviceCodeFlow, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryDeviceCodeFlow, "", "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryServicePrincipal, username, pwd, null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryServicePrincipal, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryServicePrincipal, "", "", typeof(PropertyNotSetException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryManagedIdentity, username, pwd,typeof(ArgumentException))]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryManagedIdentity, username, "", null)]
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryManagedIdentity, "", "", null)]
#else
        [DataRow(SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive, "", "", typeof(PropertyNotSetException))]
#endif

        public void ConnectionSettings_create_connections_with_all_AAD_options(SqlConnectionInfo.AuthenticationMethod auth, string userName, string password, Type exceptionType)
        {
            var settings = new ConnectionSettings
            {
                Authentication = auth,
                LoginSecure = false,
            };
            if (!string.IsNullOrEmpty(userName))
            {
                settings.Login = userName;
            }
            if (!string.IsNullOrEmpty(password))
            {
                settings.Password = password;
            }
            try
            {
                var connectionString = settings.ConnectionString;
                // For newer AAD auth types, ConnectionSettings doesn't validate all parameter combinations to allow 
                // evolution in SqlClient without having to change SMO. As SqlClient changes valid combinations we just have
                // to update this test
                var conn = new SqlConnection(settings.ConnectionString);
                Assert.That(exceptionType, Is.Null, $"ConnectionString or SqlConnection constructor should have failed:{settings.Authentication}:{settings.Login}:{settings.Password}.{Environment.NewLine}Generated string:{conn.ConnectionString}");
                Assert.That(conn.ConnectionString, Is.EqualTo(connectionString), $"{settings.Authentication}:{settings.Login}:{settings.Password}");
                var actualAuthentication = new SqlConnectionStringBuilder(connectionString).Authentication;
                var smoAuthentication = (SqlConnectionInfo.AuthenticationMethod)Enum.Parse(typeof(SqlConnectionInfo.AuthenticationMethod), actualAuthentication.ToString());
                Assert.That(smoAuthentication, Is.EqualTo(auth), $"Actual connection used incorrect authentication. {actualAuthentication}");
            } catch (Exception e)
            {
                Assert.That(e.GetType(), Is.EqualTo(exceptionType), 
                    $"ConnectionString or SqlConnection constructor threw unexpected exception:{Environment.NewLine}{settings.Authentication}:{settings.Login}:{settings.Password}{Environment.NewLine} {e}");                
            }
        }
    }
}
