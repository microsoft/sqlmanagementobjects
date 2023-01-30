// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SmoMetadataProvider;
using Microsoft.SqlServer.Test.Manageability.Utils;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Test.SMO.MetadataProvider
{
    /// <summary>
    /// Tests for SMO's intellisense component SmoMetadataProvider
    /// </summary>
    [TestClass]
    public class MetadataProviderTests : SqlTestBase
    {
        /// <summary>
        /// Azure DB users commonly lack access to master, and intellisense was failing for them.
        /// The create user with password used by this test requires the db to be partially contained
        /// and the contained database authentication set. SqlDatabaseEdge does not support the contained database authentication option
        /// </summary>
        [TestMethod]
        [UnsupportedDatabaseEngineType(DatabaseEngineType.Standalone)]
        [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlDataWarehouse, DatabaseEngineEdition.SqlOnDemand, DatabaseEngineEdition.SqlDatabaseEdge)]
        public void When_user_has_no_access_to_master_CreateConnectedProvider_succeeds()
        {
            ExecuteWithDbDrop((db) =>
            {
                var pwd = SqlTestRandom.GeneratePassword();
                var user = db.CreateUser("containeduser" + Guid.NewGuid(), string.Empty, pwd);
                var connStr = new SqlConnectionStringBuilder(this.ServerContext.ConnectionContext.ConnectionString)
                {
                    InitialCatalog = db.Name,
                    UserID = user.Name,
                    Password = pwd
                };
                var sqlConn = new SqlConnection(connStr.ToString());
                var dbScopedConn = new ServerConnection(sqlConn);
                Assert.DoesNotThrow(() => SmoMetadataProvider.CreateConnectedProvider(dbScopedConn),
                    "CreateConnectedProvider should succeed");
            });
        }
    }
}
