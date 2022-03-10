// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Test.Manageability.Utils;

namespace Microsoft.SqlServer.Test.SMO.ScriptingTests
{
    /// <summary>
    /// Provides methods to manage Availability Groups for tests
    /// </summary>
    public class AlwaysOnTestHelper
    {
        /// <summary>
        /// Creates and returns a default AG object for testing.
        /// </summary>
        /// <returns>An AG with a single primary replica</returns>
        public static AvailabilityGroup CreateDefaultAGObject(Management.Smo.Server server, int secondaryReplicaCount = 2)
        {
            var ag = new AvailabilityGroup(server, "ag1");

            // Add the replica itself. This will be the primary.
            ag.AvailabilityReplicas.Add(new AvailabilityReplica(ag, server.NetNameWithInstance())
            {
                EndpointUrl = "tcp:" + server.NetName + ":5022",
                FailoverMode = AvailabilityReplicaFailoverMode.Automatic,
                AvailabilityMode = AvailabilityReplicaAvailabilityMode.SynchronousCommit
            });

            for (int i = 0; i < secondaryReplicaCount; i++)
            {
                string serverName = string.Format("secondary{0}", i + 1);

                // Add fake secondary. Since we only have scripting tests it doesn't matter that they're fake.
                ag.AvailabilityReplicas.Add(new AvailabilityReplica(ag, serverName)
                {
                    EndpointUrl = string.Format("tcp:{0}:5022", serverName),
                    FailoverMode = AvailabilityReplicaFailoverMode.Automatic,
                    AvailabilityMode = AvailabilityReplicaAvailabilityMode.SynchronousCommit
                });
            }

            return ag;
        }

        public static void CreateDatabaseWithBackup(Management.Smo.Server server, Database db)
        {
            db.Create();
            db.TakeFullBackup();
        }
        
        public static void CreateAvailabilityGroupForDatabase(Management.Smo.Server server, AvailabilityGroup ag, string dbName)
        {
            var aDb = new AvailabilityDatabase(ag, dbName);
            ag.AvailabilityDatabases.Add(aDb);

            ag.AvailabilityReplicas.Add(new AvailabilityReplica(ag, server.NetNameWithInstance())
            {
                EndpointUrl = "tcp://localhost:8022",
                FailoverMode = AvailabilityReplicaFailoverMode.Manual,
                AvailabilityMode = AvailabilityReplicaAvailabilityMode.SynchronousCommit,
                SeedingMode = AvailabilityReplicaSeedingMode.Automatic,
                ConnectionModeInSecondaryRole = AvailabilityReplicaConnectionModeInSecondaryRole.AllowNoConnections
            });
            ag.Create();
        }

        public static void CreateDistributedAvailabilityGroup(Management.Smo.Server server, AvailabilityGroup dag, string firstReplicaName, string secondReplicaEndpointUrl)
        {
            dag.AvailabilityReplicas.Add(new AvailabilityReplica(dag, firstReplicaName)
            {
                EndpointUrl = "TCP://localhost:5022",
                FailoverMode = AvailabilityReplicaFailoverMode.Manual,
                AvailabilityMode = AvailabilityReplicaAvailabilityMode.AsynchronousCommit,
                SeedingMode = AvailabilityReplicaSeedingMode.Automatic,
                Name = firstReplicaName
            });

            var secondReplicaName = "replica" + Guid.NewGuid();
            dag.AvailabilityReplicas.Add(new AvailabilityReplica(dag, secondReplicaName)
            {
                EndpointUrl = secondReplicaEndpointUrl,
                FailoverMode = AvailabilityReplicaFailoverMode.Manual,
                AvailabilityMode = AvailabilityReplicaAvailabilityMode.AsynchronousCommit,
                SeedingMode = AvailabilityReplicaSeedingMode.Automatic,
                Name = secondReplicaName
            });
            dag.Create();
        }
    }
}
