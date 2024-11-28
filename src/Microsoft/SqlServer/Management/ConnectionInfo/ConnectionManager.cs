// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Reflection;
using System.Security.Principal;

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.Data;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
    using System.Globalization;
    using System.Diagnostics;
    using Microsoft.SqlServer.Server;

    ///<summary>
    ///connection settings and creation
    ///</summary>
    public abstract class ConnectionManager : ConnectionSettings
    {
        private SqlConnection m_SqlConnectionObject;
        private object connectionLock = new object();
        private bool m_InUse;
        private int m_LoginFailedClients;
        ServerMessageEventHandler m_RemoteLoginFailedHandler;
        SqlInfoMessageEventHandler m_SqlInfoMessageHandler;
        private CapturedSql m_CapturedSQL;
        private AutoDisconnectMode m_AutoDisconnectMode;
        private bool bIsUserConnected;
        private bool isContainedAuthentication = false;
        // Indicates whether connections can be made
        private bool m_forceDisconnected;
        private ServerInformation m_serverInformation = null;
        private ServerVersion m_serverVersionOverride = null;

        /// <summary>
        /// TBD
        /// </summary>
        internal ConnectionManager() : this(null, false)
        {
        }

        internal ConnectionManager(IRenewableToken token) : this(token, true)
        {
        }

        private ConnectionManager(IRenewableToken token, bool removeIntegratedSecurity)
        {
            if (!CallerHavePermissionToUseSQLCLR())
            {
                throw new InvalidOperationException(StringConnectionInfo.SmoSQLCLRUnAvailable);
            }

            InitDefaults();
            this.AccessToken = token;
            m_SqlConnectionObject = new SqlConnection();
            this.InitSqlConnectionObject(true, removeIntegratedSecurity); //ConnectionString is not set.
        }

        /// <summary>
        /// Constructor. Initialize properties by using information from parameter sqlConnectionObject.
        /// If the status of sqlConnectionObject is Open, also query server for @@LOCK_TIMEOUT
        /// to set the LockTimeout property.
        /// </summary>
        /// <param name="sqlConnectionObject">SqlConnection</param>
        /// <param name="accessToken"></param>
        internal ConnectionManager(SqlConnection sqlConnectionObject, IRenewableToken accessToken)
        {
            if (!CallerHavePermissionToUseSQLCLR())
                throw new Exception(StringConnectionInfo.SmoSQLCLRUnAvailable);
   
            InitDefaults();
            this.AccessToken = accessToken;
            m_SqlConnectionObject = sqlConnectionObject;
            this.InitFromSqlConnection(sqlConnectionObject);
            this.InitSqlConnectionObject(false); //ConnectionString is already set.
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sci"></param>
        internal ConnectionManager(SqlConnectionInfo sci) : base(sci)
        {
            if (!CallerHavePermissionToUseSQLCLR())
                throw new Exception(StringConnectionInfo.SmoSQLCLRUnAvailable);

            InitDefaults();
            m_SqlConnectionObject = new SqlConnection();
            this.InitSqlConnectionObject(true); //ConnectionString is not set.
        }

        /// <summary>
        /// Initialize the SqlConnection object.
        /// </summary>
        /// <param name="setConnectionString">if ConnectionString needs to be set.</param>
        /// <param name="removeIntegratedSecurity"></param>
        private void InitSqlConnectionObject(bool setConnectionString, bool removeIntegratedSecurity = false)
        {            
            if (setConnectionString)
            {
                lock (this.connectionLock)
                {
                    if (removeIntegratedSecurity)
                    {
                        SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(this.ConnectionString);
                        builder.IntegratedSecurity = false;
                        this.ConnectionString = builder.ConnectionString;
                    }

                    m_SqlConnectionObject.ConnectionString = this.ConnectionString;
                }
            }
            this.ResetConnectionString = false;
        }


        private bool CallerHavePermissionToUseSQLCLR()
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            if (!SqlContext.IsAvailable)
                return true;

            return Microsoft.SqlServer.Smo.UnSafeInternals.ManagementUtil.CallerIsMicrosoftAssembly(Assembly.GetExecutingAssembly());
#else
            return true;
#endif
        }

        private void InitDefaults()
        {
            bIsUserConnected = false;
            m_AutoDisconnectMode = AutoDisconnectMode.DisconnectIfPooled;
            m_InUse = false;
            m_LoginFailedClients = 0;
            m_RemoteLoginFailedHandler = null;
            m_CapturedSQL = new CapturedSql();
        }

        internal void CopyConnectionManager(ConnectionManager cm)
        {
            CopyConnectionSettings(cm);
        }

        ///<summary>
        /// Returns true if the object has been connected with the server at least once.
        /// If true, property changes are not allowed any more and
        /// ConnectionCannotBeChangedException will be thrown when a property has
        /// been changed.
        /// </summary>
        public bool InUse
        {
            get
            {
                return m_InUse;
            }
            set
            {
                m_InUse = value;
            }
        }

        private int lockTimeout = -1;
        /// <summary>
        /// LockTimeout is the Lock timeout in seconds. Default -1 is for indefinite. InvalidPropertyValueException
        /// is thrown for all other negative numbers. Positive LockTimeout will be multiplied by 1000 and then
        /// used for "SET LOCK_TIMEOUT" setting.
        /// </summary>
        public int LockTimeout
        {
            get
            {
                return lockTimeout;
            }

            set
            {
                if (value < -1)
                {
                    throw new InvalidPropertyValueException(StringConnectionInfo.InvalidLockTimeout(value));
                }

                if (lockTimeout != value)
                {
                    lockTimeout = value;

                    if (this.IsOpen)
                    {

                        SqlConnection sqlConnection = null;

                        try
                        {
                            sqlConnection = this.SqlConnectionObject;

                            if (sqlConnection != null)
                            {
                                System.Threading.Monitor.Enter(this.SqlConnectionObject);
                            }

                            SqlCommand sqlCommand = sqlConnection.CreateCommand();
                            sqlCommand.CommandText = "SET LOCK_TIMEOUT " + (this.LockTimeout * 1000);
                            sqlCommand.CommandType = CommandType.Text;
                            ExecuteTSql(ExecuteTSqlAction.ExecuteNonQuery, sqlCommand, null, true);
                        }
                        finally
                        {
                            if (sqlConnection != null)
                            {
                                System.Threading.Monitor.Exit(sqlConnection);
                            }
                        }
                    }
                }
            }
        }        

        ///<summary>
        /// Returns the major version number of SQL Server. I.e. 8 for SQL Server 2000.
        /// Exceptions:
        /// ConnectionFailureException
        /// </summary>
        public ServerVersion ServerVersion
        {
            get
            {
                //if know that if we connect once the parameters of
                // the connection will not change
                return m_serverVersionOverride ?? GetServerInformation().ServerVersion;
            }

            set
            {
                if (!IsForceDisconnected && IsOpen)
                {
                    throw new ConnectionException(StringConnectionInfo.CannotBeSetWhileConnected);
                }
                if (m_serverVersionOverride != value)
                {
                    m_serverVersionOverride = value;
                    m_serverInformation = null;
                }
            }
        }

        
        /// <summary>
        /// Gets the ProductVersion server property of this connection
        /// </summary>
        public Version ProductVersion
        {
            get { return m_productVersionOverride ?? GetServerInformation().ProductVersion; }
            set
            {
                if (!IsForceDisconnected && IsOpen)
                {
                    throw new ConnectionException(StringConnectionInfo.CannotBeSetWhileConnected);
                }
                if (m_productVersionOverride != value)
                {
                    m_productVersionOverride = value;
                    m_serverInformation = null;
                }
            }
        }
        ///<summary>
        /// Returns the database engine type of SQL Server.
        /// Exceptions:
        /// ConnectionFailureException
        /// </summary>
        public DatabaseEngineType DatabaseEngineType
        {
            get { return m_databaseEngineTypeOverride ?? GetServerInformation().DatabaseEngineType; }
            set
            { 
                if (!IsForceDisconnected && IsOpen)
                {
                    throw new ConnectionException(StringConnectionInfo.CannotBeSetWhileConnected);
                }
                if (m_databaseEngineTypeOverride != value)
                {
                    m_databaseEngineTypeOverride = value;
                    m_serverInformation = null;
                }
            }
        }
        
        /// <summary>
        /// The edition of the Database Engine
        /// </summary>
        public DatabaseEngineEdition DatabaseEngineEdition
        {
            get { return m_databaseEngineEditionOverride ?? GetServerInformation().DatabaseEngineEdition; }
            set
            {
                if (!IsForceDisconnected && IsOpen)
                {
                    throw new ConnectionException(StringConnectionInfo.CannotBeSetWhileConnected);
                }
                if (m_databaseEngineEditionOverride != value)
                {
                    m_databaseEngineEditionOverride = value;
                    m_serverInformation = null;
                }
            }
        }

        /// <summary>
        /// The host platform of the server (Linux/Windows/etc)
        /// </summary>
        public string HostPlatform
        {
            get { return GetServerInformation().HostPlatform; }
        }

        /// <summary>
        /// Connection protocol.
        /// </summary>
        public NetworkProtocol ConnectionProtocol
        {
            get { return GetServerInformation().ConnectionProtocol; }
        }

        private bool containedAuthenticationUpdated = false;
        ///<summary>
        /// Returns true if the database engine authenticates using contained authentication.
        /// Exceptions:
        /// ConnectionFailureException
        /// </summary>
        public bool IsContainedAuthentication
        {
            get
            {
                //once the connection has been made, parameters of
                // the connection will not change
                if (!this.containedAuthenticationUpdated)
                {
                    if (!IsForceDisconnected)
                    {
                        PoolConnect();
                        try
                        {
                            CheckIfContainedAuthenticationIsUsed();
                        }
                        finally
                        {
                            PoolDisconnect();
                        }
                    }
                    else //Not Connected because of Offline/Design Mode.
                    {
                        //Contained Authentication is not supported in designmode.
                        return false;
                    }

                    this.containedAuthenticationUpdated = true;
                }

                return isContainedAuthentication;
            }
        }

        private ServerInformation GetServerInformation()
        {
            if (m_serverInformation == null)
            {
                if (!IsForceDisconnected)
                {
                    PoolConnect();
                    var sqlConnection = SqlConnectionObject;
                    try
                    {
                        Debug.WriteLine($"ConnectionManager.GetServerInformation for <{sqlConnection.ConnectionString}>");
                        System.Threading.Monitor.Enter(sqlConnection);
                        var dataAdapter = new SqlDataAdapter();
                        try
                        {
                            m_serverInformation = ServerInformation.GetServerInformation(sqlConnection,
                                dataAdapter,
                                sqlConnection.ServerVersion);
                        }
                        finally
                        {
                            // .Net core SqlDataAdapter isn't disposable
                            var dispose = (object)dataAdapter as IDisposable;
                            if (dispose != null)
                            {
                                dispose.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        System.Threading.Monitor.Exit(sqlConnection);
                        PoolDisconnect();
                    }
                }
                else //Not Connected because of Offline/Design Mode.
                {
                    //Design mode only understands Singleton engine type.
                    //Cloud is not supported in designmode.
                    m_serverInformation = new ServerInformation(m_serverVersionOverride,new Version(m_serverVersionOverride.Major, m_serverVersionOverride.Minor, m_serverVersionOverride.BuildNumber),  DatabaseEngineType.Standalone, DatabaseEngineEdition.Unknown, HostPlatformNames.Windows, NetworkProtocol.NotSpecified);
                }            
            }
            return m_serverInformation;
        }

        private bool IsConnectionOpen(SqlConnection sqlConnection)
        {
            return ConnectionState.Open == (ConnectionState.Open & sqlConnection.State);
        }

        private void CheckIfContainedAuthenticationIsUsed()
        {
            if (this.IsOpen
                && !this.containedAuthenticationUpdated) //once the connection has been made, parameters of
                                                        // the connection will not change
            {
                string initialCatalog = this.InitialCatalog;

                if (DatabaseEngineType.Standalone == this.DatabaseEngineType           //Standalone
                    && ServerVersion.Major > 10  //Denali
                    && !string.IsNullOrEmpty(initialCatalog)//Contained Authentication can't be present if no initial catalog is provided
                                                            //because one can't create contained user in master database
                                                            //and contained user can't have default database defined.
                    )
                {
                    SqlConnection sqlConnection = null;

                    try
                    {
                        sqlConnection = this.SqlConnectionObject;

                        System.Threading.Monitor.Enter(sqlConnection);

                        string contextToSet = string.Format(CultureInfo.InvariantCulture, "use {0};", CommonUtils.MakeSqlBraket(initialCatalog));
                        string resetContext = string.Empty;

                        if (!string.IsNullOrEmpty(this.SqlConnectionObject.Database))
                        {
                            resetContext = string.Format(CultureInfo.InvariantCulture, "use {0}; --resetting the context", CommonUtils.MakeSqlBraket(this.SqlConnectionObject.Database));
                        }

                        //If the initial catalog is not master then we look for authenticating database id
                        //in sys.dm_exec_sessions in order to check if this is a contained authentication or not.
                        string containedConnectionQuery = @"
if (db_id() = 1)
begin
-- contained auth is 0 when connected to master
select 0
end
else
begin
-- need dynamic sql so that we compile this query only when we know resource db is available
exec('select case when authenticating_database_id = 1 then 0 else 1 end from
sys.dm_exec_sessions where session_id = @@SPID')
end;";

                        SqlCommand sqlCommand = sqlConnection.CreateCommand();
                        sqlCommand.CommandText = contextToSet + containedConnectionQuery + resetContext;
                        sqlCommand.CommandType = CommandType.Text;

                        this.isContainedAuthentication = (Int32)ExecuteTSql(ExecuteTSqlAction.ExecuteScalar, sqlCommand, null, true) == 1;
                    }
                    finally
                    {
                       System.Threading.Monitor.Exit(sqlConnection);
                    }
                }
                else
                {
                    this.isContainedAuthentication = false;
                }

                this.containedAuthenticationUpdated = true;
            }
        }

        ///<summary>
        /// Returns a reference to the SqlConnection object used by the
        /// ConnectionContext object.
        /// This should always return the valid sqlConnection object with the latest valid ConnectionString set.
        /// Exceptions:
        /// PropertyNotAvailableException
        /// </summary>
        public SqlConnection SqlConnectionObject
        {
            get
            {
                if (null == m_SqlConnectionObject)
                {
                    m_SqlConnectionObject = new SqlConnection();
                }

                lock (this.connectionLock)
                {
                    if (!this.IsOpen)
                    {
                        if (AccessToken != null)
                        {
                            ConnectionInfoHelper.SetTokenOnConnection(m_SqlConnectionObject,
                                AccessToken.GetAccessToken());
                        }

                        if (this.ResetConnectionString //If User wants to Reset the connection string
                            || this.ConnectionString != m_SqlConnectionObject.ConnectionString)
                        /* ConnectionString of SqlConnection object will be updated
                         * if it is not similar to ServerConnection's ConnectionString
                         * in order to avoid any direct update to SqlConnection object.
                         */
                        {

                            // we grabbed the user id and password from the input SqlConnection and
                            // merged them with the connection string so make sure we don't
                            // try to use both in the object, which won't be allowed
                            m_SqlConnectionObject.Credential = null;
                            m_SqlConnectionObject.ConnectionString = this.ConnectionString;
                        }
                    }
                }
                return m_SqlConnectionObject;
            }
        }

        /// <summary>
        /// Returns the current database context.
        /// </summary>

        public string CurrentDatabase
        {
            get
            {
                string currentDatabase = string.Empty;
                if (!string.IsNullOrEmpty(this.SqlConnectionObject.Database))
                {
                    currentDatabase = this.SqlConnectionObject.Database;
                }
                return currentDatabase;
            }
        }

        /// <summary>
        /// connects to the server impersonating if necessary
        /// </summary>
        private void InternalConnect()
        {
            if (ConnectAsUser && !(IsReadAccessBlocked || SqlContext.IsAvailable))
            {
                try
                {
                    var userName = ConnectAsUserName;
                    string domain = null;
                    if (ConnectAsUserName.Contains(@"\"))
                    {
                        var nameParts = ConnectAsUserName.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                        if (nameParts.Length == 2)
                        {
                            domain = nameParts[0];
                            userName = nameParts[1];
                        }
                    }
                    using (var handle = SafeNativeMethods.GetUserToken(userName, domain, ConnectAsUserPassword))
                    {
                        WindowsIdentity.RunImpersonated(handle, InternalConnectImpl);
                    }
                }
                // this catch block  is necessary because we want to prevent client exception
                // handler code to execute before we undo the impersonation
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                InternalConnectImpl();
            }
        }

        private void InternalConnectImpl()
        {
            SqlConnection sqlConnection = this.SqlConnectionObject;
            lock (this.connectionLock)
            {
                if (!this.IsConnectionOpen(sqlConnection))
                {
                    var retryCount = 2;
                    while (true)
                    {
                        //proceed with connection
                        try
                        {
                            sqlConnection.Open();
                            return;
                        }
                        catch (SqlException e) when (e.Number == 42109 && retryCount > 0)
                        {   
                            // Handle Serverless wakeup with a retry
                            retryCount--;
                        }
                    }
                    
                }
            }

            // We don't use this.ServerVersion to avoid recursion
            CheckServerVersion(ServerInformation.ParseStringServerVersion(sqlConnection.ServerVersion));
        }

        ///<summary>
        /// Creates the actual connection to SQL Server. Ignored if already connected.
        /// It is optional to call this method, as the connection will be opened when
        /// required.
        /// Exceptions:
        /// ConnectionFailureException
        /// </summary>
        public void Connect()
        {
            if (IsForceDisconnected)
            {
                return;
            }

            if (this.IsOpen)
            {                
                bIsUserConnected = true;

                return;
            }
            SqlConnection sqlConnection = null;

            try
            {
                InternalConnect();
                if (this.LockTimeout != -1)
                {
                    sqlConnection = this.SqlConnectionObject;

                    if (sqlConnection != null)
                    {
                        System.Threading.Monitor.Enter(sqlConnection);
                    }

                    SqlCommand sqlCommand = this.SqlConnectionObject.CreateCommand();
                    sqlCommand.CommandText = "SET LOCK_TIMEOUT " + (this.LockTimeout * 1000);
                    sqlCommand.CommandType = CommandType.Text;
                    ExecuteTSql(ExecuteTSqlAction.ExecuteNonQuery, sqlCommand, null, true);
                }

                //no updates of the connection properties are allowed from now on
                this.BlockUpdates = true;
                m_InUse = true;

            }
            catch (Exception e)
            {
                // Note: We cannot use 'this.ServerInstance' because it may not be be initialized
                // (i.e. it would have the default value of "(local)" which may be totally unrelated
                // to the ConnectionString value and thus provide an error message that would be
                // quite confusing.

                var builder = new SqlConnectionStringBuilder(this.ConnectionString);

                throw new ConnectionFailureException(StringConnectionInfo.ConnectionFailure(builder.DataSource), e);
            }
            finally
            {
                if (sqlConnection != null)
                {
                    System.Threading.Monitor.Exit(sqlConnection);
                }
            }

            //if we got here it means we succesfully connected
            bIsUserConnected = true;
        }

        /// <summary>
        /// Defines all the Methods used to Execute T-SQL on a server in the ServerConnection object.
        /// </summary>
        protected enum ExecuteTSqlAction
        {
            Unknown,
            ExecuteNonQuery,
            ExecuteReader,
            ExecuteScalar,
            FillDataSet
        }

        /// <summary>
        /// Executes T-SQL using the appropriate methods depending on the action information passed as the parameter.
        /// </summary>
        /// <param name="action">Defines method to be used for executing T-SQL</param>
        /// <param name="execObject">Object on which that method needs to be called</param>
        /// <param name="fillDataSet">DataSet in which data need to filled in case of SqlDataAdapter execObject</param>
        /// <param name="catchException">If the exception to be caught.</param>
        /// <returns></returns>

        protected object ExecuteTSql(ExecuteTSqlAction action,
                                object execObject,
                                DataSet fillDataSet,
                                bool catchException)
        {
            //Connection should already be open.

            try
            {
                switch (action)
                {
                    case ExecuteTSqlAction.FillDataSet:
                        return (execObject as SqlDataAdapter).Fill(fillDataSet);
                    case ExecuteTSqlAction.ExecuteNonQuery:
                        return (execObject as SqlCommand).ExecuteNonQuery();
                    case ExecuteTSqlAction.ExecuteReader:
                        return (execObject as SqlCommand).ExecuteReader();
                    case ExecuteTSqlAction.ExecuteScalar:
                        return (execObject as SqlCommand).ExecuteScalar();
                    default:
                        return null;
                }
            }
            catch (SqlException exc) when (HandleExecuteException(exc, action,
                                execObject,
                                catchException,
                                fillDataSet, out object result))
            {
                return result;
            }
        }

        private bool HandleExecuteException(SqlException exc, ExecuteTSqlAction action,
                                object execObject, bool catchException,
                                DataSet fillDataSet, out object result)
        {
            var currentDatabaseContext = SqlConnectionObject.Database;
            var sqlConnection = SqlConnectionObject;
            var retry = false;
            lock (connectionLock)
            {
                if (catchException)
                {
                    // For exceptions related to expired tokens, we need to close the connection and force a reopen
                    if (AccessToken != null && exc.Number == 0 && exc.Class == 0xb && sqlConnection.State == ConnectionState.Open)
                    {
                        sqlConnection.Close();
                    }
                    //All SqlExceptions doesn't close SqlConnection object, hence I am catching all the SqlExceptions
                    //and re-executing only in those cases where connection has been closed with the execption.
                    if (!IsConnectionOpen(sqlConnection)) //Connection is not open.
                    {
                        // this is a duplicate check for non-null AccessToken,
                        // but at some point the initial retry assignment could change to have
                        // other scenarios that force a closing of the connection
                        if (AccessToken != null)
                        {
                            ConnectionInfoHelper.SetTokenOnConnection(sqlConnection, AccessToken.GetAccessToken());
                        }
                        sqlConnection.Open();
                        // This logic might have an issue if dbname changes. To avoid this issue and avoid running extra queries
                        // SqlConnection should provide the database id as well
                        if (sqlConnection.Database != currentDatabaseContext)
                        {
                            // we have to make sure database still exists before switching
                            if (IsDatabaseValid(sqlConnection, currentDatabaseContext))
                            {
                                sqlConnection.ChangeDatabase(currentDatabaseContext);
                                //resetting original database context.
                            }
                        }

                        retry = true;
                    }
                }
            }

            if (retry)
            {
                try
                {
                    result = ExecuteTSql(action, execObject, fillDataSet, false); //Sending false and ensuring that only 1 reattempt is made.
                    return true;
                }
                catch (SqlException caughtException) //Catch exceptions occuring in retries.
                {
                    Trace.TraceError(string.Format(
                                            CultureInfo.CurrentCulture,
                                            "Exception caught and not thrown while connection retry: {0}",
                                            caughtException.Message)
                                            );
                }
            }
            result = null;
            return false;
        }
        /// <summary>
        /// Verifies that a given database exists
        /// </summary>
        private bool IsDatabaseValid(SqlConnection sqlConnection, string dbName)
        {
            try
            {
                if (sqlConnection.State != ConnectionState.Open)
                {
                    sqlConnection.Open();
                }
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = sqlConnection;
                cmd.Parameters.Add("@db_name", SqlDbType.NVarChar).Value = dbName;
                cmd.CommandText = "SELECT CASE WHEN db_id(@db_name) IS NULL THEN 0 ELSE 1 END";
                return Convert.ToBoolean(cmd.ExecuteScalar());
            }
            catch (SqlException exp)
            {
                Trace.TraceError(string.Format(
                                        System.Globalization.CultureInfo.CurrentCulture,
                                        "Exception caught and not thrown while connection retry: {0}",
                                        exp.Message)
                                        );
                return false;
            }
        }

        /// <summary>
        /// Check if we are connecting to a supported server version.
        /// block off &lt;= 7.0 in ServerConnection.
        /// </summary>
        /// <param name="version"></param>
        protected void CheckServerVersion(ServerVersion version)
        {
            if (version.Major <= 7)
            {
                throw new ConnectionFailureException(StringConnectionInfo.ConnectToInvalidVersion(version.ToString()));
            }
        }

        ///<summary>
        /// Closes the connection with SQL Server. Ignored if already disconnected.
        /// Exceptions:
        /// ConnectionFailureException
        /// </summary>
        public void Disconnect()
        {
            if (!this.IsOpen)
            {
                bIsUserConnected = false;
                return;
            }

            SqlConnection sqlConnection = this.SqlConnectionObject;
            lock (this.connectionLock)
            {
                sqlConnection.Close();
            }
            bIsUserConnected = false;
        }

        /// <summary>
        /// connects only if not already connected
        /// </summary>
        internal void PoolConnect()
        {
            if (!bIsUserConnected && !IsForceDisconnected)
            {
                this.Connect();
                bIsUserConnected = false;
            }
        }

        /// <summary>
        /// disconnects only if it is pooled
        /// </summary>
        internal void PoolDisconnect()
        {
            if (!bIsUserConnected)
            {
                if (AutoDisconnectMode.DisconnectIfPooled == this.AutoDisconnectMode &&
                    !IsReadAccessBlocked &&
                    false == this.NonPooledConnection &&
                    false == this.BlockPoolDisconnect)
                {
                    this.Disconnect();
                }
            }
        }

        internal abstract bool BlockPoolDisconnect
        {
            get;
        }

        internal abstract void InitAfterConnect();

        ///<summary>
        /// Returns true if the SqlConnection object is connected with the server.
        /// This can only return true for non pooled connections as pooled connections
        /// are always closed directly after an operation.
        /// </summary>
        public bool IsOpen
        {
            get
            {
                return this.IsConnectionOpen(m_SqlConnectionObject);
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void GenerateStatementExecutedEvent(string query)
        {
            if (null != statementEventHandler)
            {
                statementEventHandler(this, new StatementEventArgs(query, DateTime.Now));
            }
        }

        /// <summary>
        /// Occurs when the state of the connection changes.
        /// Uses System.Data.StateChangeEventHandler.
        /// </summary>
        public event StateChangeEventHandler StateChange
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                {
                    return;
                }
                this.SqlConnectionObject.StateChange += value;
            }
            remove
            {
                this.SqlConnectionObject.StateChange -= value;
            }
        }

        /// <summary>
        /// Occurs when SQL Server returns a warning or informational message.
        /// Uses SqlClient.SqlInfoMessageEventHandler.
        /// </summary>
        ///
        public event SqlInfoMessageEventHandler InfoMessage
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                    return;
                this.SqlConnectionObject.InfoMessage += value;
            }
            remove
            {
                this.SqlConnectionObject.InfoMessage -= value;
            }
        }

        private event ServerMessageEventHandler ServerMessageInternal;
        /// <summary>
        /// Occurs when SQL Server returns a warning or informational message.
        /// Uses SqlClient.SqlInfoMessageEventHandler.
        /// </summary>
        ///
        public event ServerMessageEventHandler ServerMessage
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                    return;
                if (null == ServerMessageInternal)
                {
                    m_SqlInfoMessageHandler = new SqlInfoMessageEventHandler(SerializeInfoMessage);
                    this.InfoMessage += m_SqlInfoMessageHandler;
                }
                this.ServerMessageInternal += value;
            }
            remove
            {
                this.ServerMessageInternal -= value;
                if (null == ServerMessageInternal)
                {
                    this.InfoMessage -= m_SqlInfoMessageHandler;
                    m_SqlInfoMessageHandler = null;
                }
            }
        }

        /// <summary>
        /// Event that is called each time a T-SQL statement has been executed and capture is set
        /// This allows users to add a event hook to trace T-SQL statements.
        /// </summary>
        private StatementEventHandler statementEventHandler;
        private Version m_productVersionOverride;
        private DatabaseEngineType? m_databaseEngineTypeOverride;
        private DatabaseEngineEdition? m_databaseEngineEditionOverride;

        public event StatementEventHandler StatementExecuted
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                    return;
                statementEventHandler += value;
            }
            remove
            {
                statementEventHandler -= value;
            }
        }


        private event ServerMessageEventHandler RemoteLoginFailedInternal;
        /// <summary>
        /// Called when the server needs to connect to remote servers
        /// and the login fails
        /// </summary>
        public event ServerMessageEventHandler RemoteLoginFailed
        {
            add
            {
                //Ignore event subscription
                if (SqlContext.IsAvailable)
                    return;
                if (0 == m_LoginFailedClients++)
                {
                    m_RemoteLoginFailedHandler = new ServerMessageEventHandler(
                                                OnRemoteLoginFailedMessage);
                    this.ServerMessage += m_RemoteLoginFailedHandler;
                }
                RemoteLoginFailedInternal += value;
            }
            remove
            {
                if (0 <= --m_LoginFailedClients)
                {
                    this.ServerMessage -= m_RemoteLoginFailedHandler;
                    m_RemoteLoginFailedHandler = null;
                }
                RemoteLoginFailedInternal -= value;
            }
        }

        private void OnRemoteLoginFailedMessage(object sender, ServerMessageEventArgs e)
        {
            // check for messages for remote login failed
            if (e.Error.Number <= 18480 && e.Error.Number >= 18489)
            {
                RemoteLoginFailedInternal(this, e);
            }
        }

        private void SerializeInfoMessage(object sender, SqlInfoMessageEventArgs e)
        {
            foreach (SqlError err in e.Errors)
            {
                ServerMessageEventArgs smArgs = new ServerMessageEventArgs(err);
                ServerMessageInternal(this, smArgs);
            }
        }

        /// <summary>
        ///
        /// </summary>
        public CapturedSql CapturedSql
        {
            get
            {
                return m_CapturedSQL;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public AutoDisconnectMode AutoDisconnectMode
        {
            get
            {
                return m_AutoDisconnectMode;
            }
            set
            {
                m_AutoDisconnectMode = value;
            }
        }

        /// <summary>
        /// Enforces a disconnect and ensures that connection cannot be re-opened again
        /// </summary>
        public void ForceDisconnected()
        {
            m_forceDisconnected = true;
            Disconnect();
        }

        /// <summary>
        /// Indicates that the connection has been forcefully disconnected
        /// </summary>
        public bool IsForceDisconnected
        {
            get
            {
                return m_forceDisconnected;
            }
        }
    }
}