// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Diagnostics;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Microsoft.SqlServer.Management.XEventDbScoped.UnitTests
{
    /// <summary>
    /// Base class for all XE tests that need to create a SqlConnection
    /// </summary>
    [SqlTestArea(SqlTestArea.ExtendedEvents)]
    [SupportedServerVersionRange(DatabaseEngineType = DatabaseEngineType.SqlAzureDatabase, MinMajor = 12)]
    [UnsupportedDatabaseEngineEdition(DatabaseEngineEdition.SqlOnDemand)]
    public abstract class DbScopedXEventTestBase : SqlTestBase
    {
        protected DatabaseXEStore store = null;
        protected ServerConnection connection = null;
        /// <summary>
        /// Name of the db pool to share among derived classes
        /// </summary>
        protected const string PoolName = "DbScopedXEPool";

        /// <summary>
        /// Enforces creation of a new SqlConnection to access the database so
        /// we can use ExecuteFromDbPool safely during test runs that parallelize test
        /// execution at the class level.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="test"></param>
        protected void ExecuteTest(Database db, System.Action test)
        {
            var stringBuilder = new SqlConnectionStringBuilder(this.SqlConnectionStringBuilder.ToString())
            {
                InitialCatalog = db.Name,
                Pooling = false
            };
            var sqlConn = new SqlConnection(stringBuilder.ToString());
            var sqlStoreConnection = new SqlStoreConnection(sqlConn);
            this.connection = new ServerConnection(sqlConn);
            this.store = new DatabaseXEStore(sqlStoreConnection, db.Name);
            test();
        }

        public override void PostExecuteTest()
        {
            base.PostExecuteTest();
            if (connection != null)
            {
                connection.Disconnect();
                connection = null;
            }
            store = null;
        }

        /*      
         * Hiding this since it shows up in every derived class. Uncomment for debugging if needed
         *      [TestMethod]
                [VisualStudio.TestTools.UnitTesting.Ignore]
                public void Connection_and_store_can_initialize_from_Smo_Server()
                {
                    ExecuteWithDbDrop((db) =>
                    {
                        ExecuteTest(db, ValidateConnection);
                    });
                }

                private void ValidateConnection()
                {
                    Assert.That(this.connection, Is.Not.Null, "this.connection is null");
                    Assert.That(this.store, Is.Not.Null, "this.store is null");
                }
                */
        internal bool VerifyObjectCount(string packageName, string objectType, int expectedCount)
        {
            return VerifyCount(string.Format(CultureInfo.InvariantCulture, "SELECT COUNT(*) FROM sys.dm_xe_objects objects JOIN sys.dm_xe_packages packages ON objects.package_guid = packages.guid WHERE objects.object_type = N'{0}' AND packages.name = N'{1}' AND (objects.capabilities & 1 = 0 OR objects.capabilities IS NULL)", objectType, packageName), expectedCount);
        }

        internal bool VerifyPackageCount(int expectedCount)
        {
            return VerifyCount(@"SELECT COUNT(*) FROM sys.dm_xe_packages WHERE capabilities & 1 = 0 OR capabilities IS NULL", expectedCount);
        }

        internal bool VerifyColumnCount(string packageName, string objectName, int expectedCount)
        {
            return VerifyCount(string.Format(CultureInfo.InvariantCulture, "SELECT COUNT(*) FROM sys.dm_xe_object_columns columns, sys.dm_xe_objects objects, sys.dm_xe_packages packages WHERE object_name = N'{0}' and columns.object_name = objects.name and objects.package_guid = packages.guid and packages.name = N'{1}' and columns.column_type='customizable'", objectName, packageName), expectedCount);
        }

        internal bool VerifyDataColumnCount(string packageName, string objectName, int expectedCount)
        {
            return VerifyCount(string.Format(CultureInfo.InvariantCulture, "SELECT COUNT(*) FROM sys.dm_xe_object_columns columns, sys.dm_xe_objects objects, sys.dm_xe_packages packages WHERE object_name = N'{0}' and columns.object_name = objects.name and objects.package_guid = packages.guid and packages.name = N'{1}' and columns.column_type='data'", objectName, packageName), expectedCount);
        }

        internal bool VerifyReadOnlyColumnCount(string packageName, string objectName, int expectedCount)
        {
            return VerifyCount(string.Format(CultureInfo.InvariantCulture, "SELECT COUNT(*) FROM sys.dm_xe_object_columns columns, sys.dm_xe_objects objects, sys.dm_xe_packages packages WHERE object_name = N'{0}' and columns.object_name = objects.name and objects.package_guid = packages.guid and packages.name = N'{1}' and columns.column_type='readonly'", objectName, packageName), expectedCount);
        }


        internal bool VerifyMapValueCount(string packageName, string mapName, int expectedCount)
        {
            return VerifyCount(string.Format(CultureInfo.InvariantCulture, "SELECT COUNT(*) FROM sys.dm_xe_map_values mvalues, sys.dm_xe_objects objects , sys.dm_xe_packages packages WHERE mvalues.name = N'{0}' and mvalues.name = objects.name and mvalues.object_package_guid = objects.package_guid and objects.package_guid = packages.guid and packages.name = N'{1}'", mapName, packageName), expectedCount);
        }

        internal bool VerifyCount(string cmd, int expectedCount)
        {
            bool flag = false;
            try
            {
                var count = connection.ExecuteScalar(cmd);
                Assert.That(count, Is.EqualTo(expectedCount), "Unexpected count for {0}", cmd);
                flag = true;
            }
            catch (SqlException e)
            {
                Trace.TraceWarning("Unable to execute command: {0}\r\n{1}", cmd, e);
            }
            return flag;
        }
    }
}
