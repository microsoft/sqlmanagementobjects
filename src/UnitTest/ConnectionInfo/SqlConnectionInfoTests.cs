// Copyright (c) Microsoft.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.ComponentModel;
using Assert = NUnit.Framework.Assert;


namespace Microsoft.SqlServer.ConnectionInfoUnitTests
{
    [TestClass]
    public class SqlConnectionInfoTests
    {
        [TestMethod]
        public void SqlConnectionInfoWithConnection_copy_returns_editable_ServerConnection()
        {
            var sqlConnection = new SqlConnection("data source=.;integrated security=true");
            var serverConnection = new ServerConnection(sqlConnection);
            var sourceConnection = new SqlConnectionInfoWithConnection(sqlConnection);
            var copy = sourceConnection.Copy();
            Assert.DoesNotThrow(() => copy.ServerConnection.ServerInstance = "somename", "Setting properties on copied connection should succeed");
        }

        [TestMethod]
        public void SqlConnectionInfo_impersonation_failure_throws_ConnectionFailureException()
        {
            var conn = new ServerConnection("someservername") { ConnectAsUser = true, ConnectAsUserName = "someuser", ConnectAsUserPassword = Guid.NewGuid().ToString() };
            var ex = Assert.Throws<ConnectionFailureException>(conn.Connect, "Connect() should throw ConnectionFailureException when impersonation fails");
            Assert.That(ex.InnerException, Is.InstanceOf<Win32Exception>(), "InnerException");
            Assert.That(ex.InnerException.Message, Does.Contain("The user name or password is incorrect"), "InnerException.Message");
        }
    }
}
