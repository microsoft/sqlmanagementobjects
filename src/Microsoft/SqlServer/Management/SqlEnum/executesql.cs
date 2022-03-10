// Copyright (c) Microsoft.
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
    using System.Collections;
    using Microsoft.SqlServer.Management.Common;
    using System.Runtime.InteropServices;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;


    /// <summary>
    ///this class is used to execute tsql 
    ///it is the only way tsql gets executed by the enumerator</summary>
    [ComVisible(false)]
    internal class ExecuteSql
    {
        ServerConnection m_conctx;
        bool bHasConnected;
        ArrayList m_Messages;
        SqlExecutionModes? m_semInitial;
        SqlInfoMessageEventHandler m_ServerInfoMessage;
        string databaseName;

        /// <summary>
        ///init connection trying to cast con
        ///to one of the supported connection types</summary>
        public ExecuteSql(Object con)
        {
            bHasConnected = false;
            InitConnection(con);
        }
        /// <summary>
        ///init connection trying to cast con
        ///to one of the supported connection types</summary>
        public ExecuteSql(Object con,string databaseName, bool poolConnection = true)
        {
            bHasConnected = false;
            InitConnection(con);
            this.databaseName = databaseName;
            InitServerConnectionWithDatabaseName(poolConnection);
        }

        private void InitServerConnectionWithDatabaseName(bool poolConnection = true)
        {
            if (m_conctx != null)
            {
                if (!String.IsNullOrEmpty(this.databaseName))
                {
                    m_conctx = m_conctx.GetDatabaseConnection(this.databaseName, poolConnection);

                }
            }
         }

        /// <summary>
        ///establish connection if not already connected</summary>
        internal void Connect()
        {
            // Allow nested Connect calls. Only remember the first mode on Connect
            if (!m_semInitial.HasValue)
            {
                m_semInitial = m_conctx.SqlExecutionModes;
            }
            m_conctx.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
            if( false == m_conctx.IsOpen )
            {
                try
                {
                    m_conctx.Connect();
                }
                catch
                {
                    m_conctx.SqlExecutionModes = m_semInitial.Value;
                    m_semInitial = null;
                    throw;
                }
                bHasConnected = true;
            }
        }

        /// <summary>
        ///disconnect if the connection was initially disconnected</summary>
        internal void Disconnect()
        {
            if (m_semInitial.HasValue)
            {
                m_conctx.SqlExecutionModes = m_semInitial.Value;
                m_semInitial = null;
            }
            
            if( bHasConnected )
            {
                m_conctx.Disconnect();
            }
        }

        /// <summary>
        ///init connection trying to cast con
        ///to one of the supported connection types</summary>
        void InitConnection(Object con)
        {
            m_conctx = con as ServerConnection;
            if( null != m_conctx )
            {
                return;
            }
            
                 SqlConnectionInfoWithConnection sciwc = con as SqlConnectionInfoWithConnection;
                 if( null != sciwc )
                 {
                    m_conctx = sciwc.ServerConnection;
                    return;
                 }
                 
            SqlConnectionInfo sci = con as SqlConnectionInfo;
            if( null != sci )
            {
                m_conctx = new ServerConnection(sci);
                return;
            }
            SqlConnection sc = con as SqlConnection;
            if( null != sc )
            {
                m_conctx = new ServerConnection(sc);
                return;
            }
            SqlDirectConnection sdc = con as SqlDirectConnection;
            if( null != sdc )
            {
                m_conctx = new ServerConnection(sdc.SqlConnection);
                return;
            }
            throw new InternalEnumeratorException(StringSqlEnumerator.InvalidConnectionType);
        }

        /// <summary>
        ///start capturing messages</summary>
        void StartCapture()
        {
            m_Messages = new ArrayList();
            m_ServerInfoMessage = new SqlInfoMessageEventHandler(RecordMessage);
            m_conctx.InfoMessage += m_ServerInfoMessage;
        }

        /// <summary>
        ///record a message</summary>
        private void RecordMessage(object sender, SqlInfoMessageEventArgs e)
        {
            m_Messages.Add(e);
        }

        /// <summary>
        ///stop capturing messages, return what was captured</summary>
        ArrayList ClearCapture()
        {
            if( null != m_ServerInfoMessage )
            {
                m_conctx.InfoMessage -= m_ServerInfoMessage;
                m_ServerInfoMessage = null;
            }

            ArrayList listMessages = m_Messages;
            m_Messages = null;
            return listMessages;
        }

        /// <summary>
        /// if execution failed with connection error try to reconnect
        /// try only once as MDAC resets the connection pool after a connection error
        ///so we are garanteed to make a genuine attempt to reconnect instead af taking an already 
        ///invalid connection from the pool
        ///return true if success</summary>
        bool TryToReconnect(ExecutionFailureException e)
        {
            //check for valid exception. 
            if( null == e )
            {
                return false;
            }
            //check that is a connection related problem
            if (((SqlException)e.InnerException).Class >= 20)
            {
                //make shure we are closed
                if (false == m_conctx.IsOpen)
                {
                    //attempt reopen with current settings
                    //if fails report 
                    try
                    {
                        m_conctx.SqlConnectionObject.Open();
                    }
                    catch(SqlException)
                    {
                        return false;
                    }
                    return true;
                }
            }

            // ErrorNumber: 41383
            // ErrorSeverity: EX_USER
            // ErrorFormat: An internal error occurred while running the DMV query. This was likely caused by concurrent DDL operations. Please retry the query.
            // ErrorInserts: none
            // ErrorCorrectiveAction: Re-run the DMV query.
            // ErrorFirstProduct: SQL12
            // ErrorInformationDisclosure: SystemMetadata
            if (((SqlException)e.InnerException).Number == 41383)
            {
                return true;
            }

            //not a connection problem , the query was bad
            return false;
        }

        /// <summary>
        ///execute a query without results</summary>
        public void ExecuteImmediate(String query)
        {
            Enumerator.TraceInfo("query:\n{0}\n", query);

            try
            {
                m_conctx.ExecuteNonQuery(query, ExecutionTypes.NoCommands);
            }
            catch(ExecutionFailureException e)
            {
                if( TryToReconnect(e) )
                {
                    m_conctx.ExecuteNonQuery(query, ExecutionTypes.NoCommands);
                }
                else
                {
                    throw; //go with the original exception
                }
            }
        }

        /// <summary>
        ///excute a query and return a DataTable with the results</summary>
        public DataTable ExecuteWithResults(String query)
        {
            Enumerator.TraceInfo("query:\n{0}\n", query);

            DataSet ds = null;
            try
            {
                ds = m_conctx.ExecuteWithResults(query);
            }
            catch(ExecutionFailureException e)
            {
                if( TryToReconnect(e) )
                {
                    ds = m_conctx.ExecuteWithResults(query);
                }
                else
                {
                    throw; //go with the original exception
                }
            }
            if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
            {
                return ds.Tables[0];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Execute a query and get a DataReader for the results.
        /// </summary>
        /// <param name="query">The query text before any parameterization or caching checks.</param>
        /// <param name="command">The SqlCommand to use in case cancelling remaining reader processing is needed.</param>
        /// <returns>The data reader.</returns>
        public SqlDataReader GetDataReader(String query, out SqlCommand command)
        {
            Enumerator.TraceInfo("query:\n{0}\n", query);

            SqlDataReader dr = null;
            try
            {
                dr = m_conctx.ExecuteReader(query, out command);
            }
            catch (ExecutionFailureException e)
            {
                if (TryToReconnect(e))
                {
                    dr = m_conctx.ExecuteReader(query, out command);
                }
                else
                {
                    throw; //go with the original exception
                }
            }
            return dr;
        }

        /// <summary>
        /// Execute a query and get a DataReader for the results.
        /// </summary>
        /// <param name="query">The query text before any parameterization or caching checks.</param>
        /// <returns>The data reader.</returns>
        public SqlDataReader GetDataReader(String query)
        {
            SqlCommand command;
            return this.GetDataReader(query, out command);
        }

        /// <summary>
        ///return the ServerVersion</summary>
        public ServerVersion GetServerVersion()
        {
            //don't need to retry SqlConnection stores the version
            //after it first connects even if the connection goes bad
            return m_conctx.ServerVersion;
        }

        /// <summary>
        ///return the DatabaseEngineType</summary>
        public DatabaseEngineType GetDatabaseEngineType()
        {
            //don't need to retry SqlConnection stores the configuration
            //after it first connects even if the connection goes bad
            return m_conctx.DatabaseEngineType;
        }
        
        /// <summary>
        ///return the DatabaseEngineEdition for the connection</summary>
        public DatabaseEngineEdition GetDatabaseEngineEdition()
        {
            //don't need to retry SqlConnection stores the configuration
            //after it first connects even if the connection goes bad
            return m_conctx.DatabaseEngineEdition;
        }

        /// <summary>
        ///returns if authentication is contained</summary>
        internal bool IsContainedAuthentication()
        {
            //don't need to retry SqlConnection stores the configuration
            //after it first connects even if the connection goes bad
            return m_conctx.IsContainedAuthentication;
        }


        /// <summary>
        ///execute the sql for ther given connection without returning results
        ///but capturing the messages</summary>
        static public ArrayList ExecuteImmediateGetMessage(String query, Object con)
        {
            ExecuteSql e = new ExecuteSql(con);
            e.Connect();
            ArrayList listMessages;
            try
            {
                e.StartCapture();
                e.ExecuteImmediate(query);
            }
            finally
            {
                listMessages = e.ClearCapture();
                e.Disconnect();
            }
            return listMessages;
        }

        /// <summary>
        ///execute the sql for ther given connection without returning results</summary>
        static public void ExecuteImmediate(String query, Object con)
        {
            ExecuteSql e = new ExecuteSql(con);
            e.Connect();
            try
            {
                e.ExecuteImmediate(query);
            }
            finally
            {
                e.Disconnect();
            }
        }

        /// <summary>
        /// Executes the specified query using the specified connection object
        /// </summary>
        /// <param name="query"></param>
        /// <param name="con"></param>
        /// <returns></returns>
        static public DataTable ExecuteWithResults(String query, Object con)
        {
            StringCollection sc = new StringCollection();
            sc.Add(query);
            return ExecuteSql.ExecuteWithResults(sc, con);
        }


       /// <summary>
       /// Executes the specified query against a target database using the specified connection. Optionally
       /// allows specifying whether connection pooling is used. 
       /// </summary>
       /// <param name="query"></param>
       /// <param name="con"></param>
       /// <param name="database"></param>
       /// <param name="poolConnection"></param>
       /// <returns></returns>
        static public DataTable ExecuteWithResults(String query, Object con, String database, bool poolConnection = true)
        {
            StringCollection sc = new StringCollection();
            sc.Add(query);
            return ExecuteSql.ExecuteWithResults(sc, con, database, poolConnection);
        }

        /// <summary>
        ///execute the sql for ther given connection returning results in a DataTable
        ///this is a tsql for final results 
        ///StatementBuilder holds info for additional processing needs and formating information
        ///the first tsqls in the list are executed without resulst, results are taken only for the last tsql</summary>
        static public DataTable ExecuteWithResults(StringCollection query, Object con, StatementBuilder sb)
        {
            DataProvider dp = null;
            dp = GetDataProvider(query, con, sb, DataProvider.RetriveMode.RetriveDataTable);
            return dp.GetTable();
        }

        /// <summary>
        ///execute the sql for ther given connection returning results in a DataProvider
        ///this is a tsql for final results 
        ///StatementBuilder holds info for additional processing needs and formating information
        ///the first tsqls in the list are executed without resulst, 
        /// results are taken only for the last tsql		
        /// </summary>
        static internal DataProvider GetDataProvider(StringCollection query, Object con, StatementBuilder sb)
        {
            return GetDataProvider(query, con, sb, DataProvider.RetriveMode.RetriveDataReader);
        }

        /// <summary>
        ///execute the sql for ther given connection returning results in a DataProvider
        ///this is a tsql for final results 
        ///StatementBuilder holds info for additional processing needs and formating information
        ///DataProvider.RetriveMode tells if the DataProvider must bring all rows 
        /// in a DataTable or be prepared to be used as a DataReader 
        /// the first tsqls in the list are executed without resulst, results are 
        /// taken only for the last tsql
        /// </summary>
        static internal DataProvider GetDataProvider(StringCollection query, Object con, StatementBuilder sb, DataProvider.RetriveMode rm)
        {
            ExecuteSql e = new ExecuteSql(con);
            e.Connect();

            bool bDataProvInitialized = false;
            DataProvider dp = null;
            try
            {
                try
                {
                    dp = new DataProvider(sb, rm);
                    int i = 0;
                    for(; i < query.Count - 1; i++)
                    {
                        e.ExecuteImmediate(query[i]);
                    }
                    dp.SetConnectionAndQuery(e, query[i]);
                    bDataProvInitialized = true;
                }
                //if we fail we will attempt to run the cleanup sql
                catch (ExecutionFailureException efe)
                {
                    //check that is not a connection related problem
                    if (((SqlException)efe.InnerException).Class < 20 && sb.SqlPostfix.Length > 0)
                    {
                        try
                        {
                            e.ExecuteImmediate(sb.SqlPostfix);
                        }
                        //ignore eventual exception on cleanup
                        catch (ExecutionFailureException)
                        {
                        }
                    }
                    throw; //rethrow the original exception
                }
            }
            finally
            {
                //clean up in case of exception
                if( null != dp && false == bDataProvInitialized )
                {
                    dp.Close();
                }
            }
            return dp;
        }

        /// <summary>
        ///execute tsql for the given connection and return the results in the DataTable</summary>
        static public DataTable ExecuteWithResults(StringCollection query, Object con)
        {
            ExecuteSql e = new ExecuteSql(con);
            return e.Execute(query);
        }

        static public DataTable ExecuteWithResults(StringCollection query, Object con, string dbName, bool poolConnection = true)
        {
            ExecuteSql executeSql;
            // We want to avoid creating a new connection
            // so we force the current connection to switch to the right database if possible
            // if the connection is already set to the current database, just use it
            var serverConnection = con as ServerConnection;
            if (serverConnection != null)
            {
                // we could use the server's comparer but it might be expensive to fetch it 
                // so optimize the connection reuse only for exact match
                var currentDb = serverConnection.CurrentDatabase;
                if (!string.IsNullOrEmpty(currentDb) && string.Compare(currentDb, dbName, StringComparison.Ordinal) == 0)
                {
                    executeSql = new ExecuteSql(con);
                    return executeSql.Execute(query);
                }
                // We can only optimize for non-pooled connections for a couple reasons:
                // 1. Only non-pooled ServerConnection uses the same SqlConnection object for every query
                // 2. We can't reliably prepend and append "use" statements to the query collection because ExecuteSql.Query 
                // assumes the last query in the collection returns the result. We'd have to somehow append the "use master" 
                // to the very last query in a safe way that doesn't break the query.
                if (serverConnection.DatabaseEngineType == DatabaseEngineType.Standalone && serverConnection.NonPooledConnection)
                {
                    executeSql = new ExecuteSql(con);
                    executeSql.Connect();
                    executeSql.ExecuteImmediate(string.Format(CultureInfo.InvariantCulture, "use [{0}];",
                        Util.EscapeString(dbName, ']')));
                    try
                    {
                        return executeSql.Execute(query);
                    }
                    finally
                    {
                        try
                        {
                            if (serverConnection.IsOpen)
                            {
                                executeSql.ExecuteImmediate(string.Format(CultureInfo.InvariantCulture, "use [{0}];",
                                    Util.EscapeString(currentDb, ']')));
                            }
                        }
                        catch (Exception e)
                        {
                            Enumerator.TraceInfo("ExecuteWithResults: Unable to set context back to {0}: {1}", currentDb, e);
                        }
                        executeSql.Disconnect();
                    }
                }
            }
            // use the old behavior
            executeSql = new ExecuteSql(con, dbName, poolConnection);
            return executeSql.Execute(query);
        }

        private DataTable Execute(StringCollection query)
        {
            this.Connect();
            try
            {
                int i = 0;
                for (; i < query.Count - 1; i++)
                {
                    this.ExecuteImmediate(query[i]);
                }
                return this.ExecuteWithResults(query[i]);
            }
            finally
            {
                this.Disconnect();
            }
        }

        /// <summary>
        ///return the server version for the server with the given connection</summary>
        static public ServerVersion GetServerVersion(Object con)
        {
            ServerVersion sv = con as ServerVersion;
            if (sv != null)
            {
                return sv;
            }

            ServerInformation si = con as ServerInformation;
            if (si != null)
            {
                return si.ServerVersion;
            }

            return new ExecuteSql(con).GetServerVersion();
        }

        /// <summary>
        ///return the database engine type for the server with the given connection</summary>
        static public DatabaseEngineType GetDatabaseEngineType(Object con)
        {
            if (con is System.Int32 //Underlying type of DatabaseEngineType is System.Int32. Otherwise IsDefined method will throw System.ArgumentException.
                && Enum.IsDefined(typeof(DatabaseEngineType), con))
            {
                return (DatabaseEngineType)con;
            }

            ServerInformation si = con as ServerInformation;
            if (si != null)
            {
                return si.DatabaseEngineType;
            }

            return (new ExecuteSql(con)).GetDatabaseEngineType();
        }

        /// <summary>
        ///return the database engine edition for the given connection</summary>
        static public DatabaseEngineEdition GetDatabaseEngineEdition(Object con)
        {
            if (con is System.Int32 //Underlying type of DatabaseEngineEdition is System.Int32. Otherwise IsDefined method will throw System.ArgumentException.
                && Enum.IsDefined(typeof(DatabaseEngineEdition), con))
            {
                return (DatabaseEngineEdition)con;
            }

            ServerInformation si = con as ServerInformation;
            if (si != null)
            {
                return si.DatabaseEngineEdition;
            }

            return (new ExecuteSql(con)).GetDatabaseEngineEdition();
        }

        /// <summary>
        /// returns if the connection's authentication is contained or not.
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        static internal bool IsContainedAuthentication(Object con)
        {
            return (new ExecuteSql(con)).IsContainedAuthentication();
        }

        /// <summary>
        /// Checks whether the specified database is accessible to the user. If an error occurs
        /// it will not throw - returning FALSE instead
        /// </summary>
        /// <param name="con">The connection object</param>
        /// <param name="databaseName">The name of the database to check</param>
        static internal bool GetIsDatabaseAccessibleNoThrow(Object con, string databaseName)
        {
            bool isAccessible = false;
            try
            {
                //has_dbaccess only returns a valid value on Azure if you're already connected to the DB you're querying for, which implies you have
                //access. So in order to save ourselves a call (since DatabaseEngineType is cached) on Azure we'll just let the caller assume the DB
                //is accessible
                isAccessible = ExecuteSql.GetDatabaseEngineType(con) == DatabaseEngineType.SqlAzureDatabase ?
                    true :
                    bool.Parse(ExecuteSql.ExecuteWithResults(@"SELECT CASE WHEN has_dbaccess(N'" + Util.EscapeString(databaseName, '\'') + "') = 1 THEN 'true' ELSE 'false' END", con).Rows[0][0].ToString());
            }
            catch
            {
                //Error occurred so we'll assume we can't connect to the DB
            }

            return isAccessible;
        }
    }
}
