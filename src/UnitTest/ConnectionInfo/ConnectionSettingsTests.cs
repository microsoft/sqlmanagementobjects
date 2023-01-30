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
            var userName = "test@test.com";
            //switch validation based on if .Net 4.7.2+ is installed by looking for ActiveDirectoryInteractive in SqlClient
            var isInteractiveSupported =  Enum.IsDefined(typeof(SqlAuthenticationMethod), SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive.ToString());
           
            var settings = new ConnectionSettings()
            {
                Authentication = SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive,
                LoginSecure = false,
                ServerInstance = "SqlServerName"
            };

            if (isInteractiveSupported)
            {

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
            else
            {
                settings.Login = userName;
                Assert.Throws<Microsoft.SqlServer.Management.Common.InvalidPropertyValueException>(
                    delegate { connectionString = settings.ConnectionString; },
                    "Expect the use of ActiveDirectoryInteractive without support in SqlClient throws an exception");
            }
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
