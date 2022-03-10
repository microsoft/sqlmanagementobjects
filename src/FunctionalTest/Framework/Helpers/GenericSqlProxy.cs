// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
#if MICROSOFTDATA
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Provides an in-memory proxy with callbacks that allow tests to run code before transmission and after receipt of
    /// data on the wire
    /// </summary>
    [DebuggerDisplay("{connectionString}:[{Port}]")]
    public class GenericSqlProxy : IDisposable
    {
        // We pick a buffer size that's large enough to hold most single replies so we don't over-inject latency
        private const int BufferSizeBytes = 128 * 1024;
        readonly string connectionString;
        volatile bool disposed;
        private TcpListener listener = null;
        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        /// <summary>
        /// Constructs a GenericSqlProxy for the local default sql instance
        /// </summary>
        public GenericSqlProxy() : this(".")
        {

        }

        /// <summary>
        /// Construct a new GenericSqlProxy for the given connection string
        /// </summary>
        /// <param name="connectionString"></param>
        public GenericSqlProxy(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// The port through which the proxy redirects the sql connection
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Initializes the proxy by opening the TCP listener and copying data between client and server
        /// </summary>
        /// <param name="localPort">local port number to use. 0 will use a random port</param>
        /// <returns>The connection string to use for the SqlConnection</returns>
        public string Initialize(int localPort = 0)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            GetTcpInfoFromDataSource(builder.DataSource, out string hostName, out int port);
            listener = new TcpListener(IPAddress.Loopback, localPort);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
            Port = ((IPEndPoint) listener.LocalEndpoint).Port;
            Trace.TraceInformation($"Starting TcpListener on port {Port}");
            var waitEvent = new ManualResetEvent(false);
            Task.Factory.StartNew(() => { AsyncInit(listener, hostName, port, waitEvent); });
            waitEvent.WaitOne();

            return new SqlConnectionStringBuilder(builder.ConnectionString)
            {
                DataSource = $"tcp:127.0.0.1,{Port}"
            }.ConnectionString;
        }

        private void AsyncInit(TcpListener tcpListener, string hostName, int port, ManualResetEvent waitEvent)
        {

            while (!disposed)
            {
                var accept = tcpListener.AcceptTcpClientAsync();
                waitEvent.Set();
                if (accept.Wait(1000, tokenSource.Token) && !tokenSource.IsCancellationRequested)
                {
                    Trace.TraceInformation($"{DateTime.Now} Accepted!");
                    var localClient = accept.GetAwaiter().GetResult();
                    Trace.TraceInformation($"{DateTime.Now} Connecting!");
                    OnConnect?.Invoke(this, new ProxyConnectionEventArgs(localClient));
                    var remoteClient = new TcpClient() {NoDelay = true};
                    tokenSource.Token.Register(() =>
                    {
                        localClient.Dispose();
                        remoteClient.Dispose();
                    });
                    remoteClient.ConnectAsync(hostName, port).Wait(tokenSource.Token);
                    if (!tokenSource.IsCancellationRequested)
                    {
                        

                        Task.Factory.StartNew(() => { ForwardToSql(localClient, remoteClient); });
                        Task.Factory.StartNew(() => { ForwardToClient(localClient, remoteClient); });
                    }
                    else
                    {
                        Trace.TraceInformation("AsyncInit aborted due to cancellation token set");
                    }
                }
            }
        }

        /// <summary>
        /// Fires before the proxy writes a buffer to the host 
        /// </summary>
        public event EventHandler<StreamWriteEventArgs> OnWriteHost;

        /// <summary>
        /// Fires before the proxy writes a buffer to the client
        /// </summary>
        public event EventHandler<StreamWriteEventArgs> OnWriteClient;

        /// <summary>
        /// Fires when a new connection to the proxy's port is accepted
        /// </summary>
        public event EventHandler<ProxyConnectionEventArgs> OnConnect;

        private void ForwardToSql(TcpClient ourClient, TcpClient sqlClient)
        {
            long index = 0;
            try
            {
                while (!disposed)
                {
                    byte[] buffer = new byte[BufferSizeBytes];
                    int bytesRead = ourClient.GetStream().ReadAsync(buffer, 0, buffer.Length, tokenSource.Token).Result;
                    if (!tokenSource.Token.IsCancellationRequested)
                    {
                        OnWriteHost?.Invoke(this, new StreamWriteEventArgs(index++, buffer, bytesRead));
                        sqlClient.GetStream().Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception)
            {
                if (!disposed)
                {
                    throw;
                }
            }
            finally
            {
                Trace.TraceInformation("ForwardToSql exiting");
            }
        }

        private void ForwardToClient(TcpClient ourClient, TcpClient sqlClient)
        {
            long index = 0;
            try
            {
                while (!disposed)
                {
                    byte[] buffer = new byte[BufferSizeBytes];
                    int bytesRead = sqlClient.GetStream().ReadAsync(buffer, 0, buffer.Length, tokenSource.Token).Result;
                    if (!tokenSource.Token.IsCancellationRequested)
                    {
                        OnWriteClient?.Invoke(this, new StreamWriteEventArgs(index++, buffer, bytesRead));
                        ourClient.GetStream().Write(buffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception)
            {
                if (!disposed)
                {
                    throw;
                }
            }
            finally
            {
                Trace.TraceInformation("ForwardToClient exiting");
            }
        }

        private static void GetTcpInfoFromDataSource(string dataSource, out string hostName, out int port)
        {
            string[] dataSourceParts = dataSource.Split(',');
            if (dataSourceParts.Length == 1)
            {
                hostName = dataSourceParts[0].Replace("tcp:", "");
                port = 1433;
            }
            else if (dataSourceParts.Length == 2)
            {
                hostName = dataSourceParts[0].Replace("tcp:", "");
                port = int.Parse(dataSourceParts[1]);
            }
            else
            {
                throw new InvalidOperationException("TCP Connection String not in correct format!");
            }
        }

        public void Dispose()
        {
            disposed = true;
            tokenSource.Cancel();
            Trace.TraceInformation("Disposing TcpListener on port {0}", Port);
            listener?.Stop();
        }
    }

    /// <summary>
    /// Arguments passed to the OnWriteHost and OnWriteClient event handlers
    /// </summary>
    public class StreamWriteEventArgs : EventArgs
    {
        internal StreamWriteEventArgs(long index, byte[]buffer, int bytesWritten)
        {
            Index = index;
            Buffer = buffer;
            BytesWritten = bytesWritten;
        }
        /// <summary>
        /// The 0-based index of this event showing the order it was received
        /// </summary>
        public long Index;
        /// <summary>
        /// The data sent or received. This array is a fixed size.
        /// </summary>
        public byte[] Buffer;
        /// <summary>
        /// How many bytes are in the Buffer. 
        /// </summary>
        public int BytesWritten;
    }

    /// <summary>
    /// Arguments passed to the OnConnect event handler
    /// </summary>
    public class ProxyConnectionEventArgs : EventArgs
    {
        internal ProxyConnectionEventArgs(TcpClient client)
        {
            Client = client;
        }

        /// <summary>
        /// The new TcpClient instance associated with the connection
        /// </summary>
        public TcpClient Client;
    }
}
