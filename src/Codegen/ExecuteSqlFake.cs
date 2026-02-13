// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
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
    internal class ExecuteSql
    {
        public ExecuteSql(Object con)
        {
        }

        public ExecuteSql(Object con, string dbName)
        {
        }

        /// <summary>
        ///establish connection if not already connected</summary>
        internal void Connect()
        {
            throw new Exception();
        }

        /// <summary>
        ///disconnect if the connection was initially disconnected</summary>
        internal void Disconnect()
        {
            throw new Exception();
        }

        /// <summary>
        ///init connection trying to cast con
        ///to one of the supported connection types</summary>
        void InitConnection(Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///start capturing messages</summary>
        void StartCapture()
        {
            throw new Exception();
        }

        /// <summary>
        ///record a message</summary>
        private void RecordMessage(object sender, SqlInfoMessageEventArgs e)
        {
            throw new Exception();
        }

        /// <summary>
        ///stop capturing messages, return what was captured</summary>
        ArrayList ClearCapture()
        {
            throw new Exception();
        }

        /// <summary>
        /// if execution failed with connection error try to reconnect
        /// try only once as MDAC resets the connection pool after a connection error
        ///so we are garanteed to make a genuine attempt to reconnect instead af taking an already 
        ///invalid connection from the pool
        ///return true if success</summary>
        bool TryToReconnect(ExecutionFailureException e)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute a query without results</summary>
        public void ExecuteImmediate(String query)
        {
            throw new Exception();
        }

        /// <summary>
        ///excute a query and return a DataTable with the results</summary>
        public DataTable ExecuteWithResults(String query)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute a query and get a DataReader for the results</summary>
        public SqlDataReader GetDataReader(String query)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute a query and get a DataReader for the results</summary>
        public SqlDataReader GetDataReader(String query, out SqlCommand command)
        {
            throw new Exception();
        }

        /// <summary>
        ///return the ServerVersion</summary>
        public ServerVersion GetServerVersion()
        {
            throw new Exception();
        }

        /// <summary>
        ///return the database engine type for the server with the given connection</summary>
        static public DatabaseEngineType GetDatabaseEngineType()
        {
            throw new Exception();
        }

        /// <summary>
        ///execute the sql for ther given connection without returning results
        ///but capturing the messages</summary>
        static public ArrayList ExecuteImmediateGetMessage(String query, Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute the sql for ther given connection without returning results</summary>
        static public void ExecuteImmediate(String query, Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute the sql for ther given connection returning results in a DataTable</summary>
        static public DataTable ExecuteWithResults(String query, Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute the sql for ther given connection returning results in a DataTable
        ///this is a tsql for final results 
        ///StatementBuilder holds info for additional processing needs and formating information
        ///the first tsqls in the list are executed without resulst, results are taken only for the last tsql</summary>
        static public DataTable ExecuteWithResults(StringCollection query, Object con, StatementBuilder sb)
        {
            throw new Exception();
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
            throw new Exception();
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
            throw new Exception();
        }

        /// <summary>
        ///execute tsql for the given connection and return the results in the DataTable</summary>
        static public DataTable ExecuteWithResults(StringCollection query, Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///execute tsql for the given connection and return the results in the DataTable</summary>
        static public DataTable ExecuteWithResults(StringCollection query, Object con,string dbName)
        {
            throw new Exception();
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
            throw new Exception();
        }
        

        static public DataTable ExecuteWithResults(StringCollection query, Object con,string dbName, bool poolConnection = true)
        {
            throw new Exception();
        }


        /// <summary>
        ///return the server version for the server with the given connection</summary>
        static public ServerVersion GetServerVersion(Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///return the database engine type for the server with the given connection</summary>
        static public DatabaseEngineType GetDatabaseEngineType(Object con)
        {
            throw new Exception();
        }

        /// <summary>
        ///return the database engine edition for the given connection</summary>
        static internal DatabaseEngineEdition GetDatabaseEngineEdition(Object con)
        {
            throw new Exception();
        }

        /// <summary>
        /// returns if the connection's authentication is contained or not.
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        static internal bool IsContainedAuthentication(Object con)
        {
            throw new Exception();
        }

        /// <summary>
        /// Returns true if the connection is a fabric connection
        /// </summary>
        /// <param name="con"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static bool IsFabricConnection(object con) => throw new Exception();

        /// <summary>
        /// Checks whether the specified database is accessible to the user. If an error occurs
        /// it will not throw - returning FALSE instead
        /// </summary>
        /// <param name="con">The connection object</param>
        /// <param name="databaseName">The name of the database to check</param>
        static internal bool GetIsDatabaseAccessibleNoThrow(Object con, string databaseName)
        {
            throw new Exception();
        }
    }
}
