// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
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

    /// <summary>
    ///this class is used to execute tsql 
    ///it is the only way tsql gets executed by the enumerator</summary>
    [ComVisible(false)]
    public class ExecuteSql
    {
        ServerConnection m_conctx;
        bool bHasConnected;
        ArrayList m_Messages;
        SqlExecutionModes? m_semInitial;
        SqlInfoMessageEventHandler m_ServerInfoMessage;

        /// <summary>
        ///init connection trying to cast con
        ///to one of the supported connection types</summary>
        public ExecuteSql(Object con)
        {
            bHasConnected = false;
            InitConnection(con);
        }

        /// <summary>
        ///establish connection if not already connected</summary>
        internal void Connect()
        {
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
            throw new InternalEnumeratorException(SfcStrings.InvalidConnectionType);
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
            return ds.Tables[0];
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
        /// Execute a query and get a DataReader for the results.
        /// </summary>
        /// <param name="query">The query text before any parameterization or caching checks.</param>
        /// <param name="command">The SqlCommand to use in case cancelling remaining reader processing is needed.</param>
        public SqlDataReader GetDataReader(String query, out SqlCommand command)
        {
            Enumerator.TraceInfo("query:\n{0}\n", query);

            SqlDataReader dr = null;
            try
            {
                dr = m_conctx.ExecuteReader(query, out command);
            }
            catch(ExecutionFailureException e)
            {
                if( TryToReconnect(e) )
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
        ///return the ServerVersion for the connection</summary>
        public ServerVersion GetServerVersion()
        {
            //don't need to retry SqlConnection stores the version
            //after it first connects even if the connection goes bad
            return m_conctx.ServerVersion;
        }

        /// <summary>
        ///return the DatabaseEngineType for the connection</summary>
        public DatabaseEngineType GetDatabaseEngineType()
        {
            //don't need to retry SqlConnection stores the server configuration
            //after it first connects even if the connection goes bad
            return m_conctx.DatabaseEngineType;
        }

        /// <summary>
        /// Returns the DatabaseEngineEdition for the connection
        /// </summary>
        public DatabaseEngineEdition GetDatabaseEngineEdition()
        {
            //don't need to retry SqlConnection stores the server configuration
            //after it first connects even if the connection goes bad
            return m_conctx.DatabaseEngineEdition;
        }

        /// <summary>
        ///returns if authentication is contained</summary>
        internal bool IsContainedAuthentication()
        {
            //don't need to retry SqlConnection stores the server configuration
            //after it first connects even if the connection goes bad
            return m_conctx.IsContainedAuthentication;
        }

        /// <summary>
        /// Returns true if the connection is to a Fabric Database or Fabric Warehouse
        /// </summary>
        /// <returns></returns>
        public static bool IsFabricConnection(object con) => new ExecuteSql(con).IsFabricConnection();

        /// <summary>
        /// Returns true if the current connection is to a Fabric Database or Fabric Warehouse
        /// </summary>
        /// <returns></returns>
        public bool IsFabricConnection() => m_conctx.IsFabricServer;

        internal string GetHostPlatform()
        { 
            return m_conctx.HostPlatform;
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
        ///execute the sql for ther given connection returning results in a DataTable</summary>
        static public DataTable ExecuteWithResults(String query, Object con)
        {
            StringCollection sc = new StringCollection();
            sc.Add(query);
            return ExecuteSql.ExecuteWithResults(sc, con);
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
            e.Connect();
            try
            {
                int i = 0;
                for(; i < query.Count - 1; i++)
                {
                    e.ExecuteImmediate(query[i]);
                }
                return e.ExecuteWithResults(query[i]);
            }
            finally
            {
                e.Disconnect();
            }
        }

        /// <summary>
        ///return the server version for the server with the given connection</summary>
        static public ServerVersion GetServerVersion(Object con)
        {
            return new ExecuteSql(con).GetServerVersion();
        }

        /// <summary>
        ///return the database engine type for the server with the given connection</summary>
        static public DatabaseEngineType GetDatabaseEngineType(Object con)
        {
            return new ExecuteSql(con).GetDatabaseEngineType();
        }

        /// <summary>
        ///return the DatabaseEngineEdition for the server with the given connection</summary>
        static public DatabaseEngineEdition GetDatabaseEngineEdition(Object con)
        {
            return new ExecuteSql(con).GetDatabaseEngineEdition();
        }

        /// <summary>
        ///returns if authentication is contained
        /// </summary>
        static public bool IsContainedAuthentication(Object con)
        {
            return new ExecuteSql(con).IsContainedAuthentication();
        }

        /// <summary>
        /// Returns the HostPlatform property of the connection
        /// </summary>
        /// <param name="con"></param>
        /// <returns>A value from the HostPlatformNames class</returns>
        static public string GetHostPlatform(Object con)
        {
            return new ExecuteSql(con).GetHostPlatform();
        }

    }
}