// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Diagnostics;

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
                if (SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.NotSpecified && SqlConnectionStringBuilder.Authentication != SqlAuthenticationMethod.SqlPassword)
                {
                    Trace.TraceWarning($"Skipping connected provider test on {SqlConnectionStringBuilder.DataSource} because SQL logins are not available");
                    return;
                }
                var pwd = SqlTestRandom.GeneratePassword();
                var user = db.CreateUser("containeduser" + Guid.NewGuid(), string.Empty, pwd);
                var userName = user.Name;
                try
                {
                    var connStr = new SqlConnectionStringBuilder(ServerContext.ConnectionContext.ConnectionString)
                    {
                        InitialCatalog = db.Name,
                        UserID = user.Name,
                        Password = pwd,
                        Authentication = SqlAuthenticationMethod.NotSpecified
                    };
                    using (var sqlConn = new SqlConnection(connStr.ToString()))
                    {
                        var dbScopedConn = new ServerConnection(sqlConn);
                        Assert.DoesNotThrow(() => SmoMetadataProvider.CreateConnectedProvider(dbScopedConn),
                            "CreateConnectedProvider should succeed");
                    }
                }
                finally
                {
                    try
                    {
                        user.Drop();
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Unable to drop user {0}. {1}", userName, ex);
                    }
                }
            });
        }
    }
}
