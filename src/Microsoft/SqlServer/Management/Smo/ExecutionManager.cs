// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Globalization;
    using System.Data;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
    using System.Collections.Specialized;
    using System.Threading;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Server;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Diagnostics = Microsoft.SqlServer.Management.Diagnostics;

#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

    /// <summary>
    /// encapsulates ConnectionContext and isolates it from the rest of SMO
    /// </summary>
    public class ExecutionManager
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="name"></param>
        internal ExecutionManager(string name)
        {
            m_conctx = new ServerConnection();
            m_conctx.ServerInstance = name;
            m_conctx.ApplicationName = ExceptionTemplates.SqlManagement;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="cc"></param>
        internal ExecutionManager(ServerConnection cc)
        {
            m_conctx = cc;
        }

        private ServerConnection m_conctx;
        /// <summary>
        /// Connection context
        /// </summary>
        public ServerConnection ConnectionContext
        {
            get
            {
                return m_conctx;
            }
        }

        private SqlSmoObject m_parent;
        /// <summary>
        /// Reference back to Parent object
        /// </summary>
        internal SqlSmoObject Parent
        {
            get
            {
                return m_parent;
            }
            set
            {
                m_parent = value;
            }
        }


        /// <summary>
        /// make enumerator data request
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        internal DataTable GetEnumeratorData(Request req)
        {
            ConnectionContext.CheckDisconnected();

#if INCLUDE_PERF_COUNT
            StartTime();
#endif
            try
            {
#if DEBUG
                Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, 
                "get data for urn: {0}", req.Urn);
#endif
                return new Enumerator().Process(this.ConnectionContext, req);
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                PerfLogUrn(req);
                StringBuilder framepath = new StringBuilder(1024);
                StackFrame[] frames = new StackTrace().GetFrames();
                if( null != frames )
                {
                    foreach (StackFrame f in frames)
                    {
                        if( null == f )
                            continue;
                        framepath.Append("->");
                        MethodBase mi = f.GetMethod();
                        if( null != mi )
                        {
                            if( null != mi.DeclaringType )
                                framepath.Append(mi.DeclaringType.Name + "." + mi.Name);
                            else
                                framepath.Append(mi.Name);
                        }
                    }
                    Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                        framepath.ToString());
                }
#endif
        }
        }

        /// <summary>
        /// Returns an IDataReader containing the results of the request
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        internal System.Data.IDataReader GetEnumeratorDataReader(Request req)
        {
            ConnectionContext.CheckDisconnected();

            req.ResultType = ResultType.IDataReader;
#if INCLUDE_PERF_COUNT
            StartTime();
#endif
            try
            {
                Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                    "get data for urn: {0}", req.Urn);
                return EnumResult.ConvertToDataReader(Enumerator.GetData(this.ConnectionContext, req));
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                PerfLogUrn(req);
                StringBuilder framepath = new StringBuilder(1024);
                StackFrame[] frames = new StackTrace().GetFrames();
                if( null != frames )
                {
                    foreach (StackFrame f in frames)
                    {
                        if( null == f )
                            continue;
                        framepath.Append("->");
                        MethodBase mi = f.GetMethod();
                        if( null != mi )
                        {
                            if( null != mi.DeclaringType )
                                framepath.Append(mi.DeclaringType.Name + "." + mi.Name);
                            else
                                framepath.Append(mi.Name);
                        }
                    }
                    Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                        framepath.ToString());
                }
#endif
            }
        }

        /// <summary>
        /// make enumerator self discover request
        /// </summary>
        /// <param name="roi"></param>
        /// <returns></returns>
        internal ObjectInfo GetEnumeratorInfo(RequestObjectInfo roi)
        {
            ConnectionContext.CheckDisconnected();

#if INCLUDE_PERF_COUNT
            if( PerformanceCounters.DoCount )
                PerformanceCounters.ObjectInfoRequestCount++;
#endif
            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, 
                "get object info for urn: {0}", roi.Urn);
            return new Enumerator().Process(this.ConnectionContext, roi);
        }

        /// <summary>
        /// make enumerator dependecies request
        /// </summary>
        /// <param name="roi"></param>
        /// <returns></returns>
        internal DependencyChainCollection GetDependencies(DependencyRequest dependencyRequest)
        {
            ConnectionContext.CheckDisconnected();

#if INCLUDE_PERF_COUNT
            StartTime();
#endif
            try
            {
                Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "get dependencies ");
                return new Enumerator().EnumDependencies(this.ConnectionContext, dependencyRequest);
            }
            catch(ConnectionException e)
            {
                throw new SmoException(ExceptionTemplates.SqlInnerException, e);
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                if( PerformanceCounters.DoCount )
                    PerformanceCounters.DependencyDiscoveryDuration += EndTime();
#endif
        }
        }

        private StringCollection m_ServerMessages;	// placeholder for DBCC messages

        private void OnServerMessage(object sender, ServerMessageEventArgs e)
        {
            // 5701 is database context change message, we don't care about it
            if( e.Error.Number != 5701 )
            {
                m_ServerMessages.Add(e.Error.Message);
            }
        }

        /// <summary>
        /// An event raised just before T-SQL queries are executed.
        /// It is raised on a thread that executes the queries.
        /// </summary>
        private EventHandler beforeExecuteSql;
        internal event EventHandler BeforeExecuteSql
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                beforeExecuteSql += value;
            }
            remove
            {
                beforeExecuteSql -= value;
            }
        }

        /// <summary>
        /// An event raised after ExecuteNonQuery method is completed on
        /// a background thread.
        /// This event is raised after AsyncWaitHandle is signaled.
        /// </summary>
        private ExecuteNonQueryCompletedEventHandler executeNonQueryCompleted;
        internal event ExecuteNonQueryCompletedEventHandler ExecuteNonQueryCompleted
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }

                executeNonQueryCompleted += value;
            }
            remove
            {
                executeNonQueryCompleted -= value;
            }
        }

        internal void OnExecuteNonQueryCompleted(ExecuteNonQueryCompletedEventArgs args)
        {
            if (null != this.executeNonQueryCompleted)
            {
                executeNonQueryCompleted(this, args);
            }
        }

        /// <summary>
        /// An wait handle that gets signaled when an asynchronous operation
        /// is completed.
        /// </summary>
        internal AutoResetEvent AsyncWaitHandle
        {
            get { return this.asyncWaitHandle; }
        }
        private AutoResetEvent asyncWaitHandle;

        /// <summary>
        /// wrapper for ConnectionContext ExecuteNonQuery
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="executionType"></param>
        /// <returns></returns>
        internal void ExecuteNonQuery(StringCollection queries, ExecutionTypes executionType)
        {
#if INCLUDE_PERF_COUNT
                BeforeSql();
                foreach(string q in queries)
                {
                    Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                        "execute sql: " + q);
                }
#endif
            foreach(string q in queries)
            {
                DumpTraceString("execute sql: " + q);
            }

            try
            {
                this.ConnectionContext.ExecuteNonQuery(queries, executionType);
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        /// <summary>
        /// wrapper for ConnectionContext ExecuteNonQuery
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="executionType"></param>
        /// <returns></returns>
        internal void ExecuteNonQuery(string sqlCommand, ExecutionTypes executionType)
        {
#if INCLUDE_PERF_COUNT
                BeforeSql();
                Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "execute sql: " + sqlCommand);
#endif
            DumpTraceString("execute sql: " + sqlCommand);

            try
            {
                this.ConnectionContext.ExecuteNonQuery(sqlCommand, executionType);
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        /// <summary>
        /// Executes a DBCC statement, intercepting the server informatiomn messages
        /// and returning them
        /// </summary>
        /// <param name="queries"></param>
        /// <returns></returns>
        internal StringCollection ExecuteNonQueryWithMessage(StringCollection queries)
        {
            m_ServerMessages = new StringCollection();

            ServerMessageEventHandler dbccMessageHandler = new ServerMessageEventHandler(OnServerMessage);

            ExecuteNonQueryWithMessage(queries, dbccMessageHandler);

            // make a copy of m_ServerMessages to return to the user
            StringCollection retDBCCs = m_ServerMessages;
            m_ServerMessages = null;
            return retDBCCs;
        }

        internal void ExecuteNonQueryWithMessage(StringCollection queries, ServerMessageEventHandler dbccMessageHandler)
        {
            ExecuteNonQueryWithMessage(queries, dbccMessageHandler, false);
        }

        internal class ExecResult
        {
            private SqlError sqlError = null;
            private ServerMessageEventHandler serverMessageEventHandler;

            internal ExecResult()
            {
                serverMessageEventHandler = new ServerMessageEventHandler(OnInfoMessage);
            }

            void OnInfoMessage(object sender, ServerMessageEventArgs e)
            {
                //if and error has occured we have to handle it outselves
                //as we reported that we want errors treated as messages
                if (null == sqlError && e.Error.Class > 10)
                {
                    sqlError = e.Error;
                }
            }

            internal ServerMessageEventHandler GetEventHandler()
            {
                return serverMessageEventHandler;
            }

            internal SqlError GetError()
            {
                return sqlError;
            }
        }

        /// <summary>
        /// Executes a DBCC statement, using the specified <see cref="ServerMessageEventHandler"/> to pass Server Information messages received
        /// and returning them
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="dbccMessageHandler"></param>
        /// <param name="errorsAsMessages"></param>
        /// <returns></returns>
        internal void ExecuteNonQueryWithMessage(StringCollection queries,
            ServerMessageEventHandler dbccMessageHandler,
            bool errorsAsMessages)
        {
            ExecuteNonQueryWithMessage(queries, dbccMessageHandler, errorsAsMessages, /*retry*/true);
        }

        /// <summary>
        /// Executes a DBCC statement, using the specified <see cref="ServerMessageEventHandler"/> to pass Server Information messages received
        /// and returning them
        /// </summary>
        /// <param name="queries"></param>
        /// <param name="dbccMessageHandler"></param>
        /// <param name="errorsAsMessages"></param>
        /// <param name="retry">Whether we should retry if an exception is thrown during execution</param>
        /// <returns></returns>
        internal void ExecuteNonQueryWithMessage(StringCollection queries,
            ServerMessageEventHandler dbccMessageHandler,
            bool errorsAsMessages,
            bool retry)
        {
            ExecResult er = null;
            bool fireInfoMessageEventOnUserErrors = false;

            // execute the statements, but we hook for ServerMessage events
            this.ConnectionContext.ServerMessage += dbccMessageHandler;

            if (errorsAsMessages)
            {
                fireInfoMessageEventOnUserErrors = this.ConnectionContext.SqlConnectionObject.FireInfoMessageEventOnUserErrors;
                this.ConnectionContext.SqlConnectionObject.FireInfoMessageEventOnUserErrors = true;

                er = new ExecResult();
                this.ConnectionContext.ServerMessage += er.GetEventHandler();
            }
            try
            {
                ExecuteNonQuery(queries, retry);
            }
            finally
            {

                // we need to clean the event handler from the list no matter
                // what happens in DBCC execution
                this.ConnectionContext.ServerMessage -= dbccMessageHandler;
                if (errorsAsMessages)
                {
                    this.ConnectionContext.SqlConnectionObject.FireInfoMessageEventOnUserErrors = fireInfoMessageEventOnUserErrors;
                    if (null != er)
                    {
                        this.ConnectionContext.ServerMessage -= er.GetEventHandler();
                    }
                }

            }
            //if we had an error report it
            if (true == errorsAsMessages && null != er && null != er.GetError())
            {
                throw new SmoException(er.GetError().ToString());
            }
        }

        internal DataSet ExecuteWithResultsAndMessages(string cmd,
            ServerMessageEventHandler dbccMessageHandler,
            bool errorsAsMessages)
        {
            return ExecuteWithResultsAndMessages(cmd, dbccMessageHandler, errorsAsMessages, /*retry*/true);
        }

        internal DataSet ExecuteWithResultsAndMessages(string cmd,
            ServerMessageEventHandler dbccMessageHandler,
            bool errorsAsMessages,
            bool retry)
        {
            ExecResult er = null;
            bool fireInfoMessageEventOnUserErrors = false;
            DataSet d = new DataSet();
            d.Locale = CultureInfo.InvariantCulture;

            // execute the statements, but we hook for ServerMessage events
            this.ConnectionContext.ServerMessage += dbccMessageHandler;

            if (errorsAsMessages)
            {
                fireInfoMessageEventOnUserErrors = this.ConnectionContext.SqlConnectionObject.FireInfoMessageEventOnUserErrors;
                this.ConnectionContext.SqlConnectionObject.FireInfoMessageEventOnUserErrors = true;

                er = new ExecResult();
                this.ConnectionContext.ServerMessage += er.GetEventHandler();
            }
            try
            {
                DumpTraceString("execute sql: " + cmd);
                d = this.ConnectionContext.ExecuteWithResults(cmd, retry);
            }
            finally
            {

                // we need to clean the event handler from the list no matter
                // what happens in DBCC execution
                this.ConnectionContext.ServerMessage -= dbccMessageHandler;
                if (errorsAsMessages)
                {
                    this.ConnectionContext.SqlConnectionObject.FireInfoMessageEventOnUserErrors = fireInfoMessageEventOnUserErrors;
                    if (null != er)
                    {
                        this.ConnectionContext.ServerMessage -= er.GetEventHandler();
                    }
                }

            }
            //if we had an error report it
            if (true == errorsAsMessages && null != er && null != er.GetError())
            {
                throw new SmoException(er.GetError().ToString());
            }

            return d;
        }

        internal void ExecuteNonQueryWithMessageAsync(StringCollection queries,
            ServerMessageEventHandler dbccMessageHandler, bool errorsAsMessages)
        {
            ExecuteNonQueryWithMessageAsync(queries, dbccMessageHandler, errorsAsMessages, /*retry*/true);
        }

        internal void ExecuteNonQueryWithMessageAsync(StringCollection queries,
            ServerMessageEventHandler dbccMessageHandler,
            bool errorsAsMessages,
            bool retry)
        {
            //
            // There is no locking in this method because we assume that it cannot be
            // called from different threads. SMO is still a single-threaded library
            // even if we are creating other threads under the cover.
            // Client should never call SMO methods from multiple threads.
            //

            // Create a wait handle
            this.asyncWaitHandle = new AutoResetEvent(false);

            // Just start the thread and exit
            ExecuteNonQueryThread workerThread = new ExecuteNonQueryThread(this, queries, dbccMessageHandler, errorsAsMessages, retry);
            workerThread.Start();
        }

        internal void ExecuteNonQuery(StringCollection queries)
        {
            ExecuteNonQuery(queries, /*retry*/true);
        }

        internal void ExecuteNonQuery(StringCollection queries, bool retry)
        {
            //go to master so that we dont lock the database
            try
            {
#if INCLUDE_PERF_COUNT
                BeforeSql();
#endif
                if (null != beforeExecuteSql)
                {
                    beforeExecuteSql(this, EventArgs.Empty);
                }

                foreach(string q in queries)
                {
                    DumpTraceString("execute sql: " + q);
                }

                // Run queries
                this.ConnectionContext.ExecuteNonQuery(queries, ExecutionTypes.NoCommands, retry);
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        internal void ExecuteNonQueryAsync(StringCollection queries)
        {
            ExecuteNonQueryAsync(queries, /*retry*/true);
        }

        internal void ExecuteNonQueryAsync(StringCollection queries, bool retry)
        {
            //
            // There is no locking in this method because we assume that it cannot be
            // called from different threads. SMO is still a single-threaded library
            // even if we are creating other threads under the cover.
            // Client should never call SMO methods from multiple threads.
            //

            // Create a wait handle
            this.asyncWaitHandle = new AutoResetEvent(false);

            // Just start the thread and exit
            ExecuteNonQueryThread workerThread = new ExecuteNonQueryThread(this, queries, null, false, retry);
            workerThread.Start();
        }

        internal void ExecuteNonQuery(string cmd)
        {
            ExecuteNonQuery(cmd, /*retry*/true);
        }

        internal void ExecuteNonQuery(string cmd, bool retry)
        {

            try
            {
#if INCLUDE_PERF_COUNT
                BeforeSql();
#endif
                DumpTraceString("execute sql: " + cmd);

                if (null != beforeExecuteSql)
                {
                    beforeExecuteSql(this, EventArgs.Empty);
                }

                this.ConnectionContext.ExecuteNonQuery(cmd, ExecutionTypes.NoCommands, retry);

            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        internal DataSet ExecuteWithResults(StringCollection query)
        {
            try
            {
#if INCLUDE_PERF_COUNT
                BeforeSql();
#endif
                foreach(string q in query)
                {
                    DumpTraceString("execute sql: " + q);
                }
                DataSet[] list = this.ConnectionContext.ExecuteWithResults(query);
                DataSet ds = new DataSet();
                ds.Locale = CultureInfo.InvariantCulture;
                foreach(DataSet d in list)
                {
                    DataTable[] arrtbls = new DataTable[d.Tables.Count];
                    d.Tables.CopyTo(arrtbls, 0);

                    foreach(DataTable t in arrtbls)
                    {
                        d.Tables.Remove(t);
                        ds.Tables.Add(t);
                    }
                }

                return ds;
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }

        }

        internal DataSet ExecuteWithResults(string query)
        {
            return ExecuteWithResults(query, /*retry*/true);
        }

        internal DataSet ExecuteWithResults(string query, bool retry)
        {
            try
            {
#if INCLUDE_PERF_COUNT
                BeforeSql();
#endif
                DumpTraceString("execute sql: " + query);
                DataSet ds = this.ConnectionContext.ExecuteWithResults(query, retry);

                return ds;
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        internal object[] ExecuteScalar(StringCollection query)
        {
            try
            {
#if INCLUDE_PERF_COUNT
                BeforeSql();
#endif
                foreach( String s in query)
                {
                    DumpTraceString("execute sql: " + s);
                }

                object [] o = this.ConnectionContext.ExecuteScalar(query);

                return o;
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        internal object ExecuteScalar(string query)
        {
            try
            {
#if INCLUDE_PERF_COUNT
                BeforeSql();
#endif
                DumpTraceString("execute sql: " + query);
                object o = this.ConnectionContext.ExecuteScalar(query);

                return o;
            }
            finally
            {
#if INCLUDE_PERF_COUNT
                AfterSql();
#endif
            }
        }

        internal bool Recording
        {
            get
            {
#if DEBUGTRACE
                Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                    "recording: " + (SqlExecutionModes.ExecuteSql != ( SqlExecutionModes.ExecuteSql & this.ConnectionContext.SqlExecutionModes )).ToString(SmoApplication.DefaultCulture));
#endif
                return SqlExecutionModes.ExecuteSql != ( SqlExecutionModes.ExecuteSql & this.ConnectionContext.SqlExecutionModes );
            }
        }

        internal Version GetProductVersion()
        {
            return this.ConnectionContext.ProductVersion;
        }

        internal ServerVersion GetServerVersion()
        {
            ServerVersion sv = this.ConnectionContext.ServerVersion;
            if( sv == null )
            {
                if (this.Parent.IsDesignMode)
                {
                    throw new SfcDesignModeException(ExceptionTemplates.ServerVersionNotSpecified);
                }
                this.ConnectionContext.Connect();
                sv = this.ConnectionContext.ServerVersion;
                this.ConnectionContext.Disconnect();
            }
            return sv;
        }

        internal DatabaseEngineType GetDatabaseEngineType()
        {
            return this.ConnectionContext.DatabaseEngineType;
        }

        internal DatabaseEngineEdition GetDatabaseEngineEdition()
        {
            return this.ConnectionContext.DatabaseEngineEdition;
        }

        internal NetworkProtocol GetConnectionProtocol()
        {
            return this.ConnectionContext.ConnectionProtocol;
        }

        internal bool IsFabricConnection => ConnectionContext.IsFabricServer;

        internal bool IsCurrentConnectionStandardLogin(string name)
        {
            if( !this.ConnectionContext.LoginSecure &&
                this.ConnectionContext.Login == name )
            {
                return true;
            }
            return false;
        }

        internal string TrueServerName
        {
            get
            {
                return this.ConnectionContext.TrueName;
            }
        }

        internal void Abort()
        {
            this.ConnectionContext.Cancel();
        }

        private void DumpTraceString(string s)
        {
            if (s.ToLower(SmoApplication.DefaultCulture).Contains("password"))
            {
                s = "This statement contains sensitive information and has been replaced for security reasons.";
            }

            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "{0}", s);
        }

#if INCLUDE_PERF_COUNT
        DateTime m_dt;

        void StartTime()
        {
            m_dt = DateTime.Now;
        }

        TimeSpan EndTime()
        {
            return DateTime.Now - m_dt;
        }

        String GetUrnSkeleton(Request req)
        {
            StringBuilder urnSkeletonHashKey = new StringBuilder();
            // Get the Urn skeleton
            XPathExpression xpe = req.Urn.XPathExpression;
            for( int i = 0; i < xpe.Length; i++)
            {
                if( i != 0 )
                {
                    urnSkeletonHashKey.Append("/");
                }
                urnSkeletonHashKey.Append(xpe[i].Name);
            }

            if(req.Fields != null)
            {
                foreach(string s in req.Fields)
                {
                    urnSkeletonHashKey.Append(" ");
                    urnSkeletonHashKey.Append(s);
                }
            }
            return urnSkeletonHashKey.ToString();
        }

        void PerfLogUrn(Request req)
        {
            if( PerformanceCounters.DoCount )
            {
                // Log the performance counters for this call
                TimeSpan ts = EndTime();
                PerformanceCounters.EnumQueriesDuration = PerformanceCounters.EnumQueriesDuration + ts;
                PerformanceCounters.EnumQueriesCount++;

                string skeleton = GetUrnSkeleton(req);
                if( !PerformanceCounters.UrnSkeletonsPerf.Contains(skeleton) )
                    PerformanceCounters.UrnSkeletonsPerf[skeleton] = new FrequencyPair();

                FrequencyPair fp = PerformanceCounters.UrnSkeletonsPerf[skeleton] as FrequencyPair;

                fp.Count++;
                fp.Duration +=  ts;
            }
        }

        void BeforeSql()
        {
            StartTime();
        }

        void AfterSql()
        {
            if( PerformanceCounters.DoCount )
                PerformanceCounters.SqlExecutionDuration = PerformanceCounters.SqlExecutionDuration + EndTime();
        }
#endif
    }

    /// <summary>
    /// An enum describing a state of asynchronous query
    /// </summary>
    public enum ExecutionStatus
    {
        Inactive,
        InProgress,
        Succeeded,
        Failed
    }

    /// <summary>
    /// Event handler prototype for ExecuteNonQueryCompleted event.
    /// </summary>
    internal delegate void ExecuteNonQueryCompletedEventHandler(object sender, ExecuteNonQueryCompletedEventArgs args);

    /// <summary>
    /// Argument class for ExecuteNonQueryCompleted event.
    /// </summary>
    internal sealed class ExecuteNonQueryCompletedEventArgs : EventArgs
    {
        internal ExecuteNonQueryCompletedEventArgs(ExecutionStatus status, Exception lastException)
        {
            this.executionStatus = status;
            this.lastException   = lastException;
        }

        public ExecutionStatus ExecutionStatus
        {
            get { return this.executionStatus; }
        }

        public Exception LastException
        {
            get { return this.lastException; }
        }

        private ExecutionStatus executionStatus;
        private Exception       lastException;
    }

    /// <summary>
    /// A helper class that wraps a thread for ExecuteNonQuery method.
    /// </summary>
    internal sealed class ExecuteNonQueryThread
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="em">ExecutionManager object that owns this thread.</param>
        /// <param name="queries">List of queries to execute</param>
        /// <param name="dbccMessageHandler">Message handler for server events</param>
        public ExecuteNonQueryThread(ExecutionManager em, StringCollection queries, ServerMessageEventHandler dbccMessageHandler, bool errorsAsMessages)
            : this(em, queries, dbccMessageHandler, errorsAsMessages, /*retry*/true)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="em">ExecutionManager object that owns this thread.</param>
        /// <param name="queries">List of queries to execute</param>
        /// <param name="dbccMessageHandler">Message handler for server events</param>
        /// <param name="retry">Whether we should retry if an exception is thrown during execution</param>
        public ExecuteNonQueryThread(ExecutionManager em, StringCollection queries, ServerMessageEventHandler dbccMessageHandler, bool errorsAsMessages, bool retry)
        {
            this.em = em;
            this.queries = queries;
            this.dbccMessageHandler = dbccMessageHandler;
            this.errorsAsMessages = errorsAsMessages;
            this.retry = retry;
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start()
        {
            ///This function is called only from outside SMO SQLCLR, but we add this
            ///Check in case someone called it in the future from within SMO SQLCLR
            if (SqlContext.IsAvailable)
            {
                throw new SmoException(ExceptionTemplatesImpl.SmoSQLCLRUnAvailable);
            }

            Thread thread = new Thread(new ThreadStart(this.ThreadProc));
            thread.Start();
        }

        private void ThreadProc()
        {
            ExecuteNonQueryCompletedEventArgs args = null;

            try
            {
                if (null != this.dbccMessageHandler)
                {
                    em.ExecuteNonQueryWithMessage(queries, this.dbccMessageHandler, this.errorsAsMessages, this.retry);
                }
                else
                {
                    em.ExecuteNonQuery(queries, this.retry);
                }

                args = new ExecuteNonQueryCompletedEventArgs(ExecutionStatus.Succeeded, null);
            }
            catch (SmoException exception)
            {
                args = new ExecuteNonQueryCompletedEventArgs(ExecutionStatus.Failed, exception);
            }
            catch (ExecutionFailureException exception)
            {
                args = new ExecuteNonQueryCompletedEventArgs(ExecutionStatus.Failed, exception);
            }
            finally
            {
                // Raise 'completed' event
                this.em.OnExecuteNonQueryCompleted(args);

                // Let know whoever is waiting that we are done
                if (null != this.em.AsyncWaitHandle)
                {
                    this.em.AsyncWaitHandle.Set();
                }

            }
        }

        private ExecutionManager em;
        private StringCollection queries;
        private ServerMessageEventHandler dbccMessageHandler;
        private bool errorsAsMessages;
        private bool retry;
    }

}
