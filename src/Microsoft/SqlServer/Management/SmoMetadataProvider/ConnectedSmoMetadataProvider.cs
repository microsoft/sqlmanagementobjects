// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;
using Microsoft.Win32;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    partial class SmoMetadataProvider
    {
        private sealed class ConnectedSmoMetadataProvider : SmoMetadataProvider
        {
            private static int DefaultRefreshDbListMillisecond;
            private readonly ServerConnection m_serverConnection;
            private readonly int m_refreshDbListMillisecond;
            private int m_lastRefreshTimestamp;
            private const string RegPath = @"Software\Microsoft\Microsoft SQL Server\SMO\ConnectedMetadataProvider";

            static ConnectedSmoMetadataProvider()
            {
                DefaultRefreshDbListMillisecond = 120000;

#if !NETSTANDARD2_0 && !NETCOREAPP
                try
                {
                    using (var key = Registry.CurrentUser.OpenSubKey(RegPath))
                    {
                        if (key != null)
                        {
                            DefaultRefreshDbListMillisecond =
                                Convert.ToInt32(key.GetValue("DbRefreshDelayMs", DefaultRefreshDbListMillisecond));
                        }
                    }
                }
                catch (Exception e)
                {
                    TraceHelper.TraceContext.TraceCatch(e);
                }
#endif
            }
            public static ConnectedSmoMetadataProvider Create(ServerConnection connection)
            {
                if (connection == null) throw new ArgumentNullException("connection");

                // For Azure SQL Database connections we only build the database list once
                return Create(connection, connection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase ? Int32.MaxValue :
                    DefaultRefreshDbListMillisecond);
            }

            public static ConnectedSmoMetadataProvider Create(ServerConnection connection, int refreshDbListMillisecond)
            {
                if (connection == null) throw new ArgumentNullException("connection");
                if (refreshDbListMillisecond < 0)
                    throw new ArgumentOutOfRangeException("refreshDbListMillisecond", "Value must be >= 0!");

                Smo.Server server = new Smo.Server(connection);
                return new ConnectedSmoMetadataProvider(connection, server, refreshDbListMillisecond);
            }

            private ConnectedSmoMetadataProvider(ServerConnection connection, Smo.Server server, int refreshDbListMillisecond)
                : base(server, true)
            {
                TraceHelper.TraceContext.Assert(connection != null, "SmoMetadataProvider Assert", "connection != null");
                TraceHelper.TraceContext.Assert(refreshDbListMillisecond >= 0, "SmoMetadataProvider Assert", "refreshDbListMillisecond >= 0");

                this.m_serverConnection = connection;
                this.m_refreshDbListMillisecond = refreshDbListMillisecond;
            }

            /// <summary>
            /// Gets the method that will handle BeforeBind event.
            /// </summary>
            public override MetadataProviderEventHandler BeforeBindHandler
            {
                get { return this.OnBeforeBind; }
            }

            /// <summary>
            /// Gets the method that will handle AfterBind event.
            /// </summary>
            public override MetadataProviderEventHandler AfterBindHandler
            {
                get { return this.OnAfterBind; }
            }

            private void OnBeforeBind(object sender, MetadataProviderEventArgs e)
            {
                using (var method = TraceHelper.TraceContext.GetMethodContext("OnBeforeBind"))
                {
                    int currentTimestamp = Environment.TickCount;
                    int timeElapsed = currentTimestamp - this.m_lastRefreshTimestamp;

                    if (timeElapsed > this.m_refreshDbListMillisecond)
                    {
                        lock (this.m_server)
                        {
                            // check if another thread beat us to refreshing the DB list
                            if (currentTimestamp > this.m_lastRefreshTimestamp)
                            {
                                using (var activity = method.GetActivityContext("Refresh database list"))
                                {
                                    try
                                    {
                                        this.m_server.RefreshDatabaseList();
                                    }
                                    catch (Exception exp)
                                    {
                                        activity.TraceError("Failed to refresh database list due to a an exception.");
                                        activity.TraceCatch(exp);
                                    }
                                    finally
                                    {
                                        // Regardless whether or not we succeeded in refreshing 
                                        // the list of databases, we should update the timestamp
                                        // so that we do not reattempt until a full interval elapses.
                                        this.m_lastRefreshTimestamp = currentTimestamp;

                                    }
                                }
                            }
                        }
                    }
                }
            }

            private void OnAfterBind(object sender, MetadataProviderEventArgs e)
            {
                // need to disconnect at the end of each bind request
                this.m_serverConnection.Disconnect();
            }
        }
    }
}
