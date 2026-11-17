// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Failover Utilites For FailoverClusterData
    /// </summary>
    public static class FailoverUtilities
    {
        private static readonly string[] ServerFields =
        {
            "ClusterQuorumState"
        };

        private static readonly string[] AvailabilityGroupFields =
        {
           "Name",
           "PrimaryReplicaServerName"
        };

        private static readonly string[] AvailabilityReplicaFields =
        {
           "UniqueId",
           "Name",
           "AvailabilityMode",
           "FailoverMode",
           "OperationalState",
           "RollupSynchronizationState",
           "Role",
           "QuorumVoteCount"
        };

        private static readonly string[] DatabaseReplicaStateFields =
        {
            "AvailabilityReplicaId",
            "IsFailoverReady",
            "IsJoined",
            "SynchronizationState",
        };

        /// <summary>
        /// Return Server With InitFieldsSetting given ServerConnection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static Smo.Server GetServerWithInitFieldsSetting(ServerConnection connection)
        {
            Smo.Server server = new Smo.Server(connection);
            server.SetDefaultInitFields(typeof(Smo.Server), ServerFields);
            server.SetDefaultInitFields(typeof(AvailabilityGroup), AvailabilityGroupFields);
            server.SetDefaultInitFields(typeof(AvailabilityReplica), AvailabilityReplicaFields);
            server.SetDefaultInitFields(typeof(DatabaseReplicaState), DatabaseReplicaStateFields);
            return server;
        }

        /// <summary>
        /// validate the password
        /// </summary>
        /// <param name="database"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public static bool TryDecrypt(Database database, string pwd)
        {
            if (database == null)
            {
                throw new ArgumentNullException("database");
            }

            if (pwd == null)
            {
                throw new ArgumentNullException("pwd");
            }

            try
            {
                database.MasterKey.Open(pwd);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// add the credential to the secondary node
        /// </summary>
        /// <param name="server"></param>
        /// <param name="dbName"></param>
        /// <param name="pwd"></param>
        public static void ProvisionCredential(Smo.Server server, string dbName, string pwd)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            if (pwd == null)
            {
                throw new ArgumentNullException("pwd");
            }

            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new ArgumentNullException("dbName");
            }

            const int SEC_ADD_DBMASTERKEYPWD_EXISTS = 15594;
            bool connectionWasOpen = server.ConnectionContext.IsOpen;

            try
            {
                string addDbmk = @"DECLARE @pwd nvarchar(4000) = REPLACE(@password, N'''', N'''''');" +
                           @"EXEC sp_control_dbmasterkey_password @db_name = @dbName, @password = @pwd, @action = N'add'";

                if (!connectionWasOpen)
                {
                    server.ConnectionContext.SqlConnectionObject.Open();
                }

                using (SqlCommand command = new SqlCommand(addDbmk, server.ConnectionContext.SqlConnectionObject))
                {
                    SqlParameter[] parameters = new SqlParameter[2];
                    parameters[0] = new SqlParameter("@dbName", dbName);
                    parameters[1] = new SqlParameter("@password", pwd);

                    command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number != SEC_ADD_DBMASTERKEYPWD_EXISTS)
                    throw;
            }
            finally
            {
                if (!connectionWasOpen)
                {
                    server.ConnectionContext.SqlConnectionObject.Close();
                }
            }
        }

        /// <summary>
        /// is there any database in the list contains master key
        /// </summary>
        /// <param name="server"></param>
        /// <param name="databases"></param>
        /// <returns></returns>
        public static bool HasDbNeedToBeDecrypted(Smo.Server server, List<PrimaryDatabaseData> databases)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            if (databases == null)
            {
                throw new ArgumentNullException("databases");
            }

            foreach (var database in databases)
            {
                if (server.Databases[database.Name].MasterKey != null)
                    return true;
            }
            return false;
        }
    }
}