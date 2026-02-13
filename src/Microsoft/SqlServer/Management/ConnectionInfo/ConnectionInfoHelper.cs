// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif

namespace Microsoft.SqlServer.Management.Common
{
    public static class ConnectionInfoHelper
    {
        /// <summary>
        /// Sets the AccessToken property on SqlConnection object
        /// </summary>
        public static void SetTokenOnConnection(SqlConnection conn, string accessToken)
        {
            conn.AccessToken = accessToken;

        }

        /// <summary>
        /// Get AccessToken from SqlConnection
        /// </summary>
        /// <remarks>Will be an empty string if AccessToken is not supported in the current .NET Framework (&lt;4.6)</remarks>
        public static string GetTokenFromSqlConnection(SqlConnection conn)
        {
            CheckForNull(conn);
            return conn.AccessToken;
        }

        /// <summary>
        /// Creates SqlConnection from the SqlConnectionInfo class
        /// </summary>
        public static SqlConnection CreateSqlConnection(SqlConnectionInfo connectionInfo)
        {
            SqlConnection conn = new SqlConnection(connectionInfo.ConnectionString);
            if (connectionInfo.AccessToken != null)
            {
                SetTokenOnConnection(conn, connectionInfo.AccessToken.GetAccessToken());
            }

            return conn;
        }

        private static void CheckForNull(SqlConnection conn)
        {
            if (conn == null)
            {
                throw new ArgumentNullException("conn");
            }
        }
    }
}
