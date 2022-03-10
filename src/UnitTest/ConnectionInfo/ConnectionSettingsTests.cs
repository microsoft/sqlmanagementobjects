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

namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    [TestClass]
    public class ConnectionSettingsTests
    {
        
        [TestMethod]
        [TestCategory("Unit")]
        public void VerifyInteractiveModeConnectionSettings()
        {
            string connectionString;
            string userName = "test@test.com";
            //switch validation based on if .Net 4.7.2+ is installed by looking for ActiveDirectoryInteractive in SqlClient
            bool isInteractiveSupported =  Enum.IsDefined(typeof(SqlAuthenticationMethod), SqlConnectionInfo.AuthenticationMethod.ActiveDirectoryInteractive.ToString());
           
            ConnectionSettings settings = new ConnectionSettings()
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
                SqlConnection conn = new SqlConnection(connectionString);
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
    }
}
