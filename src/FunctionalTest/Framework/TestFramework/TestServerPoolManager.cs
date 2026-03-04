// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils.TestFramework;

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

        private  static ConcurrentBag<(Database, IDatabaseHandler)> allDatabases = new ConcurrentBag<(Database, IDatabaseHandler)>();
        static TestServerPoolManager()
        {
            // Note we can't use the AssemblyCleanup attribute because that only runs on classes marked as
            // [TestClass] that have actual tests in them. Since this doesn't contain actual tests we
            // instead use this event so that the cleanup will still happen without requiring hooking an
            // AssemblyCleanup into the tests themselves.
            AppDomain.CurrentDomain.DomainUnload += (_,__) => Cleanup();
        }

        /// <summary>
        /// Gets a database for the specified test descriptor from the specified pool
        /// </summary>
        /// <param name="poolName">The name of the pool</param>
        /// <param name="databaseHandler">Handler to create database</param>
        /// <returns></returns>
        public static Database GetDbFromPool(string poolName, IDatabaseHandler databaseHandler)
        {
            var serverName = databaseHandler.TestDescriptor.Name;
            return GetOrCreateDatabase(poolName, serverName, databaseHandler);
        }

        private static Database GetOrCreateDatabase(string poolName, string serverName, IDatabaseHandler handler)
        {
            if (!DatabasePools.ContainsKey(poolName))
            {
                DatabasePools[poolName] = new Dictionary<string, Database>();
            }

            var pool = DatabasePools[poolName];
            if (!pool.ContainsKey(serverName))
            {
                pool[serverName] = handler.HandleDatabaseCreation();
                allDatabases.Add((pool[serverName], handler));
            }

            return pool[serverName];
        }

        /// <summary>
        /// Cleans up the manager by deleting all the databases in the pools.
        /// </summary>
        private static void Cleanup()
        {
            // Clean up all the DBs in the pools
            foreach(var (database, handler) in allDatabases)
            {   
                try
                {
                    handler.HandleDatabaseDrop();
                }
                catch(Exception ex)
                {
                    // Log this but don't re-throw since we won't consider this a test failure
                    Trace.TraceWarning($"Failed to drop database '{database.Name}' using handler '{handler.GetType().Name}': {ex.Message}");
                }
            }
        }
    }
}


