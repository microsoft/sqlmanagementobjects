// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Globalization;
using Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Provides methods to provide database-scoped connections in place of a server-scoped connection
    /// </summary>
    public static class ConnectionHelpers
    {
        private static string GetSqlDatabaseName(this Urn urn)
        {
            do
            {
                // Grigoryp 11/16/11 adding DatabaseXEStore here
                // This is really bad, but I have no other choice at the moment.
                // This urn-parsing mechanism should be replaced with generic solution not tied to any particular domain.
                // Enumerator has to be domain-agnostic.
                if (urn.Type == "Database" || urn.Type == "DatabaseXEStore")
                {
                    return urn.GetAttribute("Name");
                }

                urn = urn.Parent;

            } while (urn != null);

            return null;
        }

        /// <summary>
        ///     takes care of updating connectionInfo to take care of Cloud DB
        ///      as Database Scope Operations in Cloud DB need a direct connection to the Database in question
        ///      it checks if the connectionInfo represents a CloudDB connection
        ///      &amp; if so, transparently switches the connection based on the database name in the request.Urn
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="urn"></param>
        internal static bool UpdateConnectionInfoIfCloud(ref Object connectionInfo, Urn urn)
        {
            /*   
                 *  Note: This will handle only the Requests that have an explicit Database Name in the request.Urn
                 *     and that should be sufficient to handle enumerator requests from the Object Explorer.
                 *     
                 *        A more generic solutions to handle Urns like 
                 *              Server[@Name='foo']/Database/Table - all tables in all databases
                 *              Server[@Name='foo']/Database[@IsSystemObject=true()]/View - all views in system databases.
                 *        is being tracked in VSTS: 341169
                 * 
                 */

            ServerConnection serverConnection = GetServerConnection(connectionInfo);
            bool isUpdated = false;

            // DW Gen3 does not need a new connection when changing databases.
            // DW Gen2 is SqlDataWarehouse edition with major version of 10.
            // DW Gen3 is SqlDataWarehouse edition with major version of 12.
            if (serverConnection != null &&
                serverConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase &&
                !(serverConnection.DatabaseEngineEdition == DatabaseEngineEdition.SqlDataWarehouse &&
                serverConnection.ProductVersion.Major >= 12))
            {
                string dbName = urn.GetSqlDatabaseName();
                if (dbName != null)
                {
                    IComparer<string> dbNamesComparer = ServerConnection.ConnectionFactory.GetInstance(serverConnection).ServerComparer as IComparer<string>;
                    if (dbNamesComparer.Compare(dbName, serverConnection.DatabaseName) != 0)
                    {
                        connectionInfo = serverConnection.GetDatabaseConnection(dbName);
                        isUpdated = true;
                    }
                }
            }
            return isUpdated;
        }

        /// <summary>
        /// If using contained authentication, adds a database name to the connection string and validates the caller has access using the credential
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <param name="urn"></param>
        internal static void UpdateConnectionInfoIfContainedAuthentication(ref Object connectionInfo, Urn urn)
        {
            ServerConnection serverConnection = GetServerConnection(connectionInfo);

            if (serverConnection != null && serverConnection.IsContainedAuthentication)
            {
                string dbName = urn.GetSqlDatabaseName();

                if (dbName == null) //It is not a database specific Urn, i.e. a server scope Urn.
                {
                    if (!String.IsNullOrEmpty(serverConnection.InitialCatalog))
                    {
                        dbName = serverConnection.InitialCatalog;
                    }
                    else
                    {
                        dbName = "master";
                    }
                }

                IComparer<string> comparer = new ServerComparer(serverConnection);
                
                if (comparer.Compare(dbName, serverConnection.CurrentDatabase) != 0) //No need to change the context if it is already correct.
                {
                    //storing original sql execution mode of the serverconnection
                    SqlExecutionModes originalMode = serverConnection.SqlExecutionModes;
                    //queries sent from Enumerator are not part of CaptureSql
                    serverConnection.SqlExecutionModes = SqlExecutionModes.ExecuteSql;

                    try
                    {
                        serverConnection.ExecuteNonQuery(String.Format(CultureInfo.InvariantCulture,
                            "use [{0}]", Util.EscapeString(dbName, ']')));
                    }
                    catch (Exception exc)
                    {
                        SqlException sqlExc = exc.InnerException as SqlException;
                        if (sqlExc != null
                            && (sqlExc.Number == 916 //The server principal is not able to access the database under the current security context.
                                || sqlExc.Number == 911) //Database does not exist. Make sure that the name is entered correctly.
                            )
                        {
                            TraceHelper.LogExCatch(exc);
                        }
                        else
                        {
                            throw exc;
                        }
                    }
                    finally
                    {
                        //resetting execution mode to the original.
                        if (serverConnection.SqlExecutionModes != originalMode)
                        {
                            serverConnection.SqlExecutionModes = originalMode;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns a ServerConnection object if the input is either a SqlConnectionInfoWithConnection or a ServerConnection, null otherwise
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <returns></returns>
        private static ServerConnection GetServerConnection(object connectionInfo)
        {
            ServerConnection serverConnection = null;

            //Case 1: If connectionInfo is SqlConnectionInfoWithConnection
            SqlConnectionInfoWithConnection sqlConnectionInfoWithConnection = connectionInfo as SqlConnectionInfoWithConnection;
            if (sqlConnectionInfoWithConnection != null)
            {
                serverConnection = sqlConnectionInfoWithConnection.ServerConnection;
            }

            if (serverConnection == null)
            {
                //Case 2: If connectionInfo is ServerConnection
                serverConnection = connectionInfo as ServerConnection;
            }

            return serverConnection;
        }

        /// <summary>
        /// Given a SqlConnection and Urn, returns the ServerConnection scoped to the database specified by the Urn.        
        /// If the Urn does not require a database scope, the ServerConnection will be scoped to whichever database the input SqlConnection
        /// is using.
        /// </summary>
        /// <param name="sqlConnection"></param>
        /// <param name="urn"></param>
        /// <returns></returns>
        public static ServerConnection ToScopedServerConnection(this SqlConnection sqlConnection, Urn urn)
        {
            object serverConnection = new ServerConnection(sqlConnection);
            UpdateConnectionInfoIfCloud(ref serverConnection, urn);
            return (ServerConnection)serverConnection;
        }
    }
}
