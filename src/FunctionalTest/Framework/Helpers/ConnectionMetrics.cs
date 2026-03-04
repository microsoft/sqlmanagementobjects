// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Threading;
using Microsoft.SqlServer.Management.Common;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Provides a ServerConnection instrumented to collect the number of connections and volume of data sent to and from the server.
    /// </summary>
    public class ConnectionMetrics : IDisposable
    {
        /// <summary>
        /// How many distinct connections have been opened on the proxy
        /// </summary>
        public int ConnectionCount;
        /// <summary>
        /// Total bytes of data read from the proxy by the client
        /// </summary>
        public long BytesRead;
        /// <summary>
        /// Total bytes of data sent to the proxy by the client
        /// </summary>
        public long BytesSent;
        /// <summary>
        /// How many queries have been executed on the SqlConnection
        /// </summary>
        public int QueryCount;
        /// <summary>
        /// The ServerConnection to use for instrumented tests
        /// </summary>
        public readonly ServerConnection ServerConnection;
        /// <summary>
        /// The proxy used for instrumentation. Tests can add their own event handlers to the proxy if desired.
        /// Those event handlers should perform minimal processing.
        /// </summary>
        public readonly GenericSqlProxy Proxy;
        /// <summary>
        /// A list of (timestamp, bytesread) tuples corresponding to each BytesRead event on the proxy
        /// </summary>
        public readonly List<(DateTimeOffset Timestamp, long BytesRead)> BytesReadHistogram = new List<(DateTimeOffset Timestamp, long BytesRead)>();
        private ConnectionMetrics(ServerConnection serverConnection, GenericSqlProxy proxy)
        {
            Proxy = proxy;
            ServerConnection = serverConnection;
            proxy.OnConnect += Proxy_OnConnect;
            proxy.OnWriteHost += Proxy_OnWriteHost;
            proxy.OnWriteClient += Proxy_OnWriteClient;
            serverConnection.StatementExecuted += ServerConnection_StatementExecuted;
        }

        /// <summary>
        /// Resets all the metrics to 0 and clears the histogram
        /// </summary>
        public void Reset()
        {
            ConnectionCount = 0;
            BytesRead = BytesSent = 0;
            QueryCount = 0;
            BytesReadHistogram.Clear();
        }

        private void ServerConnection_StatementExecuted(object sender, StatementEventArgs e)
        {
            QueryCount++;
        }

        private void Proxy_OnWriteClient(object sender, StreamWriteEventArgs e)
        {
            BytesRead += e.BytesWritten;
            BytesReadHistogram.Add((DateTimeOffset.UtcNow, e.BytesWritten));
        }

        private void Proxy_OnWriteHost(object sender, StreamWriteEventArgs e)
        {
            BytesSent += e.BytesWritten;
        }

        private void Proxy_OnConnect(object sender, ProxyConnectionEventArgs e)
        {
            ConnectionCount++;
        }

        public void Dispose()
        {
            Proxy.OnConnect -= Proxy_OnConnect;
            Proxy.OnWriteHost -= Proxy_OnWriteHost;
            Proxy.OnWriteClient -= Proxy_OnWriteClient;
            ServerConnection.StatementExecuted -= ServerConnection_StatementExecuted;
            ServerConnection.SqlConnectionObject.Dispose();
            Proxy.Dispose();
        }

        /// <summary>
        /// Creates a ConnectionMetrics object for measure statements, data movement, and connections for the given connection string
        /// </summary>
        /// <param name="connectionString">The connection string for the connection to measure</param>
        /// <param name="latencyPaddingMs">How many milliseconds of latency to inject in each client write. Default is 0</param>
        /// <param name="proxyPort">The specific port the proxy should use. If 0, a port is allocated dynamically.</param>
        /// <returns></returns>
        public static ConnectionMetrics SetupMeasuredConnection(string connectionString, int latencyPaddingMs = 0, int proxyPort = 0)
        {
            var proxy = new GenericSqlProxy(connectionString);
            if (latencyPaddingMs > 0)
            {
                proxy.OnWriteClient += (o,e) => DelayWrite(latencyPaddingMs, e);
            }
            // If running these tests in a container you may need to set a specific port
            // and expose that port in the dockerfile
            var sqlConnection = new SqlConnection(proxy.Initialize(proxyPort));
            var serverConnection = new ServerConnection(sqlConnection);
            return new ConnectionMetrics(serverConnection, proxy);
        }

        static void DelayWrite(long delay, StreamWriteEventArgs args)
        {
            Thread.Sleep(Convert.ToInt32(delay));
        }
    }


}
