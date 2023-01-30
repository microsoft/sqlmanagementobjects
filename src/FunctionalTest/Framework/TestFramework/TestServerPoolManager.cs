// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Smo;

using SMO = Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Manager for pools of databases that can be reused by multiple tests so they don't have to recreate a database for each test.
    ///
    /// Note that tests using this need to be able to handle the database not being in a perfectly clean state - other tests may leave
    /// the database in an unclean state.
    ///
    /// Because of this tests that use a database from a pool should strive to either clean up their resources created/modified during the test
    /// or to only make changes that are unlikely to affect other tests.
    /// </summary>
    public static class TestServerPoolManager
    {
        public const string DEFAULT_POOL_NAME = "DEFAULT";

        /// <summary>
        /// DB pools for tests to share and reuse. Note this is shared across all tests that inherit from this test base.
        /// Key - Pool Name
        ///     Key - ServerName
        ///     Value - List of DBs for that server
        /// </summary>
        [ThreadStatic]
        private static IDictionary<string, IDictionary<string, Database>> databasePools;

        private static IDictionary<string, IDictionary<string, Database>> DatabasePools =>
            databasePools ?? (databasePools = new Dictionary<string, IDictionary<string, Database>>());

        private  static ConcurrentBag<Database> allDatabases = new ConcurrentBag<Database>();
        static TestServerPoolManager()
        {
            // Note we can't use the AssemblyCleanup attribute because that only runs on classes marked as
            // [TestClass] that have actual tests in them. Since this doesn't contain actual tests we
            // instead use this event so that the cleanup will still happen without requiring hooking an
            // AssemblyCleanup into the tests themselves.
            AppDomain.CurrentDomain.DomainUnload += (_,__) => Cleanup();
        }

        /// <summary>
        /// Gets a database for the specified server from the specified pool
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        /// <param name="server">The server to get the database from</param>
        /// <returns></returns>
        public static Database GetDbFromPool(string poolName, SMO.Server server)
        {
            if (!DatabasePools.ContainsKey(poolName))
            {
                DatabasePools[poolName] = new Dictionary<string, Database>();
            }

            var pool = DatabasePools[poolName];
            if (!pool.ContainsKey((server.Name)))
            {
                pool[server.Name] = server.CreateDatabaseWithRetry();
                allDatabases.Add(pool[server.Name]);
            }

            return pool[server.Name];
        }

        /// <summary>
        /// Gets a database for the specified server from the default pool
        /// </summary>
        /// <param name="server">The server to get the database for</param>
        /// <returns></returns>
        public static Database GetDbFromPool(SMO.Server server)
        {
            return GetDbFromPool(DEFAULT_POOL_NAME, server);
        }

        /// <summary>
        /// Cleans up the manager by deleting all the databases in the pools.
        /// </summary>
        private static void Cleanup()
        {
            // Clean up all the DBs in the pools
            foreach(var database in allDatabases)
            {
                database.Parent.DropKillDatabaseNoThrow(database.Name);
            }
        }
    }
}


