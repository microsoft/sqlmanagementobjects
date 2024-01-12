// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Common
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Collections;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Security;
    using System.Text.RegularExpressions;
#if MICROSOFTDATA
    using Microsoft.Data.SqlClient;
#else
    using System.Data.SqlClient;
#endif
#if !NATIVEBATCHPARSER
    using Microsoft.Data.Tools.Sql.BatchParser;
#endif

    public sealed class ServerComparer : IComparer<string>
    {
        CompareOptions compareOps = CompareOptions.None;
        CultureInfo cultureInfo;
        private const int SHILOH = 8;

        internal CultureInfo CultureInfo
        {
            get
            {
                return this.cultureInfo;
            }
        }

        internal CompareOptions CompareOptions
        {
            get
            {
                return this.compareOps;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="conn"></param>
        public ServerComparer(ServerConnection conn) : this(conn, null)
        {

        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="databaseName"></param>
        public ServerComparer(ServerConnection conn, string databaseName)
        {
            if (conn != null && !conn.IsForceDisconnected)
            {
                // SQL Server ComparisonStyle flags
                // See BOL topics COLLATIONPROPERTY and DATABASEPROPERTYEX
                // for more info
                const int cIgnoreCase = 1;
                const int cIgnoreAccent = 2;
                const int cIgnoreKana = 0x10000; // 65536
                const int cIgnoreWidth = 0x20000; // 131072

                // Parameterizing the lcidAndCompareOptionsQuery to avoid SQL injection
                const string lcidAndCompareOptionsQuery =
                    "SELECT COLLATIONPROPERTY((select collation_name from sys.databases where name = ISNULL(@dbname, db_name())), 'LCID'), COLLATIONPROPERTY((select collation_name from sys.databases where name = ISNULL(@dbname, db_name())), 'ComparisonStyle')";
                const string lcidAndCompareOptionsQueryForShiloh =
                    @"SELECT COLLATIONPROPERTY(
                (select
                CAST(DATABASEPROPERTYEX(name, 'Collation') as nvarchar(255))
                from master.dbo.sysdatabases
                where name = ISNULL(@dbname, db_name())
                ),
                'LCID'
                ),
                COLLATIONPROPERTY(
                (select
                CAST(DATABASEPROPERTYEX(name, 'Collation') as nvarchar(255))
                from master.dbo.sysdatabases
                where name = ISNULL(@dbname, db_name())
                ),
                'ComparisonStyle'
                )";
                int lcid;
                int ops;
                // We already know the master collation for Azure SQL DB, no need to run a query
                if (conn.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase &&
                    databaseName != null &&
                    databaseName.Equals("master", StringComparison.OrdinalIgnoreCase))
                {
                    lcid = 1033;
                    ops = 0x30001;
                }
                else
                {
                    string queryToExecute = lcidAndCompareOptionsQuery;
                    if (conn.ServerVersion.Major <= SHILOH)
                    {
                        queryToExecute = lcidAndCompareOptionsQueryForShiloh;
                    }

                    SqlCommand sqlCommand = new SqlCommand(queryToExecute);

                    sqlCommand.Parameters.Add(new SqlParameter("@dbname", SqlDbType.NVarChar));

                    if (!string.IsNullOrEmpty(databaseName))
                    {
                        sqlCommand.Parameters["@dbname"].Value = databaseName;
                    }
                    else
                    {
                        // lcidAndCompareOptionsQuery will use db_name() as database name in this case
                        // since it has ISNULL(@dbname, db_name()) expression in it
                        sqlCommand.Parameters["@dbname"].Value = DBNull.Value;
                    }

                    SqlDataReader dataReader = null;
                    SqlExecutionModes execMode = conn.SqlExecutionModes;
                    ServerConnection retryConnection = null;

                    try
                    {
                        conn.SqlExecutionModes = SqlExecutionModes.ExecuteSql;

                        try
                        {
                            dataReader = conn.ExecuteReader(sqlCommand);
                        }
                        catch (InvalidOperationException ex)
                        {
                            // Probably "There is already an open DataReader associated with this Command"
                            // Create a new connection and try again
                            System.Diagnostics.Debug.WriteLine(ex.Message);

                            retryConnection = conn.Copy();
                            dataReader = retryConnection.ExecuteReader(sqlCommand);
                        }

                        bool hasData = dataReader.Read();
                        Trace.Assert(hasData);

                        lcid = dataReader.GetInt32(0);

                        // Note: it is possible that SQL is handing us back an LCID that Windows
                        // (from Win2k and beyond) stopped supporting.
                        // Trying to get a System.Culture object from it would throw an exception.
                        // So, instead, we apply a workaround and "normalize" the LCID to something
                        // that hopefully we can handle.
                        switch (lcid)
                        {
                            case 0x10404: // zh-TW
                            case 0x10804: // zh-CN
                            case 0x10c04: // zh-HK
                            case 0x11004: // zh-SG
                            case 0x11404: // zh-MO
                            case 0x10411: // ja-JP
                            case 0x10412: // ko-KR
                                lcid = lcid & 0x03fff;
                                break;
                            case 0x827:   // Non-supported Lithuanian code page, map it to supported Lithuanian.
                                lcid = 0x427;
                                break;
                            default:
                                break;
                        }

                        // Get the comparison ops
                        ops = dataReader.GetInt32(1);
                    }
                    finally
                    {
                        if (dataReader != null)
                        {
                            dataReader.Dispose();
                        }
                        if (retryConnection != null)
                        {
                            retryConnection.Disconnect();
                        }
                        //ExecuteReader calls PoolConnect but not PoolDisconnect, call it now
                        //to clean up the ConnectionInfo once we're done with it
                        conn.PoolDisconnect();
                        conn.SqlExecutionModes = execMode;
                    }
                }
                // Set up the comparison culture based on the LCID
                cultureInfo = NetCoreHelpers.GetNewCultureInfo(lcid);
                // Build the CompareOptions mask
                if (ops == 0)
                {
                    // binary
                    compareOps = CompareOptions.Ordinal;
                }
                else
                {
                    if ((ops & cIgnoreCase) != 0)
                    {
                        compareOps |= CompareOptions.IgnoreCase;
                    }

                    if ((ops & cIgnoreAccent) != 0)
                    {
                        compareOps |= CompareOptions.IgnoreNonSpace;
                    }

                    if ((ops & cIgnoreKana) != 0)
                    {
                        compareOps |= CompareOptions.IgnoreKanaType;
                    }

                    if ((ops & cIgnoreWidth) != 0)
                    {
                        compareOps |= CompareOptions.IgnoreWidth;
                    }
                }
            }
            else
            {
                cultureInfo = CultureInfo.InvariantCulture;
            }
        }

        private DatabaseNameEqualityComparer databaseNameEqualityComparer = null;
        /// <summary>
        /// Returns a comparer which matches the server's master database string comparer
        /// </summary>
        public IEqualityComparer<string> DatabaseNameEqualityComparer
        {
            get
            {
                return databaseNameEqualityComparer ??
                       (databaseNameEqualityComparer = new DatabaseNameEqualityComparer(this));
            }
        }

        #region IComparer Members

        /// <summary>
        ///
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        int IComparer<string>.Compare(string x, string y)
        {
            return cultureInfo.CompareInfo.Compare(x, y, compareOps);
        }

        #endregion
    }

    /// <summary>
    /// Utility <see cref="IEqualityComparer" /> used for comparing database names using the same
    /// semantics as the target server
    /// </summary>
    internal class DatabaseNameEqualityComparer : IEqualityComparer<string>
    {
        private ServerComparer serverComparer;

        public DatabaseNameEqualityComparer(ServerComparer serverComparer)
        {
            this.serverComparer = serverComparer;
        }

        #region IEqualityComparer Members

        bool IEqualityComparer<string>.Equals(string x, string y)
        {

            return (this.serverComparer as IComparer<string>).Compare(x, y) == 0;
        }

        int IEqualityComparer<string>.GetHashCode(string s)
        {
            if ((this.serverComparer.CompareOptions & CompareOptions.IgnoreCase) == CompareOptions.IgnoreCase)
            {
                //Call NetCoreHelpers method to call the appropriate method for this framework.
                return s.StringToUpper(this.serverComparer.CultureInfo).GetHashCode();
            }

            return s.GetHashCode();
        }

        #endregion
    }

    // Encapsulates a cached query and its execution count
    class SqlBatch : CacheItem<string>
    {
        public SqlBatch(SqlCommand command)
            : base()
        {
            Command = command;
        }
        public override string Key
        {
            get
            {
                return Command.CommandText;
            }
        }
        public SqlCommand Command;
    }

    public enum QueryParameterizationMode { None, ForcedParameterization, ParameterizeLiterals };
    public enum DeferredUseMode { None, CollapseRedundant, MergeSql };

    /// <summary>
    ///
    /// </summary>
    public sealed class ServerConnection : ConnectionManager, ISfcConnection
    {
        private int m_StatementTimeout;
        private string m_BatchSeparator;
        private int m_TransactionDepth;
        private SqlExecutionModes m_ExecutionMode;
        private SqlCommand m_SqlCommand;
        private SqlCommand currentSqlCommand;
        private List<SqlParameter> m_Parameters;
        private bool isSqlConnectionUsed = false; //If SqlConnection object is used to create serverconnection object then true.

        const int CACHE_SIZE = 128; // Size of command cache

        private ExecutionCache<string, SqlBatch> m_CommandCache = new ExecutionCache<string, SqlBatch>(CACHE_SIZE);

        private static Regex reUseDb = new Regex(@"^USE\s*(?<left>\[)*((?(left)[^\]]|[^;\s])+)(?(left)\]|\s)*\s*;*\s*", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        // reQueryTags detect the msparam keyword and parse hex number or any other string as a named expression. This is because SqlDataReader is unable
        // to convert hex values into integers automatically when they are used as query parameters.
        // Note: msparam tag can enclose a multiline Expression. -anchals
        private static Regex reQueryTags = new Regex(@"<msparam\>((?<HEX>(\s)*(0[xX][0-9a-fA-F]+)(\s)*)|(?<STR>((.|\n)*?)))\</msparam\>", RegexOptions.Compiled);
        private static Regex reQueryPrepStatement = new Regex(@"exec[^_]*((sp_executesql)|(sp_cursorprepexec)|(sp_prepexec)|(sp_cursorprepare)|(sp_cursoropen)|(sp_prepare))([^']|'(?=@)|'(?=,))*'(?![@])(?<qry>([^']|''|'(?!\s*,))*)'.*", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static Regex reQueryParametersQIOn = new Regex(@"(?<left>[\s,(?=!<>])(((((?<mid>[N])?(?<term>')([^']|'')*(?<right>'))|(0x[\da-fA-F]*)|([-+]?(([\d]*\.[\d]*|[\d]+)([eE]?[\d]*)))|([~]?[-+]?([\d]+)))([\s]?[\+\-\*\/\%\&\|\^][\s]?)?)+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static Regex reQueryParametersQIOff = new Regex(@"(?<left>[\s,(?=!<>])(((((?<mid>[N])?(?<term>('|""))([^'""]|''|"""")*(?<right>('|"")))|(0x[\da-fA-F]*)|([-+]?(([\d]*\.[\d]*|[\d]+)([eE]?[\d]*)))|([~]?[-+]?([\d]+)))([\s]?[\+\-\*\/\%\&\|\^][\s]?)?)+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        private static QueryParameterizationMode parameterizationMode = QueryParameterizationMode.ForcedParameterization;

        private static DeferredUseMode deferredUseMode = DeferredUseMode.None;
        private static bool cachedQueries = false;

        /// <summary>
        /// Gets or sets a value indicating the <see cref="QueryParameterizationMode"/> QueryParameterizationMode all ServerConnection instances use
        /// </summary>
        public static QueryParameterizationMode ParameterizationMode
        {
            get
            {
                return parameterizationMode;
            }
            set
            {
                parameterizationMode = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="DeferredUseMode"/> UseMode all ServerConnection instances use
        /// </summary>
        public static DeferredUseMode UseMode
        {
            get
            {
                return deferredUseMode;
            }
            set
            {
                deferredUseMode = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether ServerConnection instances should cache query results
        /// </summary>
        public static bool CachedQueries
        {
            get
            {
                return cachedQueries;
            }
            set
            {
                cachedQueries = value;
            }
        }

        private const string BatchSeparator_Default = "GO";
        private const int StatementTimeout_Default = 30;
        // According to RPC communication docs (which SqlCommand uses for execution internally) this should be 2100
        // but we leave another 10 to be on safer side. SqlCommand seems to be sending a few extra parameters of its
        // own in RPC. i.e. why if we set it to exact 2100 it doesn't work.
        private const int MaxParams_Default = 2090;
        internal const string Database_Default = "master";

        /// <summary>
        ///
        /// </summary>
        public ServerConnection()
        {
            InitDefaults();
        }

        /// <summary>
        /// Creates a ServerConnection object taking in the token.
        /// The extra boolean is used to overload this constructor as another constructor with string is present.
        /// </summary>
        /// <param name="token">An optional access token provider</param>
        public ServerConnection(IRenewableToken token) : base(token)
        {
            InitDefaults();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sci"></param>
        public ServerConnection(SqlConnectionInfo sci) : base(sci)
        {
            InitDefaults();

            //QueryTimeout uses -1 as default value
            if (sci.QueryTimeout >= 0)
            {
                this.StatementTimeout = sci.QueryTimeout;
            }
        }

        /// <summary>
        /// Constructs a new ServerConnection object from the given SqlConnection
        /// </summary>
        /// <param name="sqlConnection"></param>
        public ServerConnection(SqlConnection sqlConnection) : this(sqlConnection, null)
        {

        }

        ///  <summary>
        /// Constructs a new ServerConnection object 
        ///  </summary>
        ///  <param name="sqlConnection"></param>
        /// <param name="accessToken"></param>
        public ServerConnection(SqlConnection sqlConnection, IRenewableToken accessToken)
            : base(sqlConnection, accessToken)
        {
            InitDefaults();
            this.isSqlConnectionUsed = true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="serverInstance"></param>
        public ServerConnection(string serverInstance)
        {
            InitDefaults();

            try
            {
                this.ServerInstance = serverInstance;
            }
            catch (InvalidPropertyValueException e)
            {
                throw new InvalidArgumentException(e.Message, e);
            }
        }

        public ServerConnection(string serverInstance, string userName, string password)
        {
            InitDefaults();
            try
            {
                this.ServerInstance = serverInstance;
                this.LoginSecure = false;
                this.Login = userName;
                this.Password = password;
            }
            catch (InvalidPropertyValueException e)
            {
                throw new InvalidArgumentException(e.Message, e);
            }
        }

        public ServerConnection(string serverInstance, string userName, SecureString password)
        {
            InitDefaults();
            try
            {
                this.ServerInstance = serverInstance;
                this.LoginSecure = false;
                this.Login = userName;
                this.SecurePassword = password;
            }
            catch (InvalidPropertyValueException e)
            {
                throw new InvalidArgumentException(e.Message, e);
            }
        }

        private void InitDefaults()
        {
            m_SqlCommand = this.SqlConnectionObject.CreateCommand();
            currentSqlCommand = m_SqlCommand;
            m_Parameters = new List<SqlParameter>();
            m_StatementTimeout = 600;
            m_TransactionDepth = 0;
            m_BatchSeparator = BatchSeparator_Default;
            m_ExecutionMode = SqlExecutionModes.ExecuteSql;
        }

        private void CopyServerConnection(ServerConnection sc)
        {
            CopyConnectionManager(sc);
            sc.StatementTimeout = m_StatementTimeout;
            sc.BatchSeparator = m_BatchSeparator;
            sc.SqlExecutionModes = m_ExecutionMode;
            sc.m_TrueLogin = m_TrueLogin;
            sc.m_TrueName = m_TrueName;
        }

        /// <summary>
        /// This function performs a deep copy
        /// </summary>
        /// <returns></returns>
        public ServerConnection Copy()
        {
            ServerConnection sc = null;

            //If the ServerConnection constructor takes SqlConnection object
            //Current password set is null. We can't extract password out from the SqlConnection object.
            if (this.isSqlConnectionUsed)
            {
                bool isConnected = ConnectionState.Open == (ConnectionState.Open & this.SqlConnectionObject.State);
                SqlConnection cloneConnection = ((ICloneable)this.SqlConnectionObject).Clone() as SqlConnection;

                if (isConnected)
                {
                    cloneConnection.Open();
                }

                sc = new ServerConnection(cloneConnection);
            }
            else
            {
                sc = new ServerConnection();
            }

            this.CopyServerConnection(sc);
            return sc;
        }

        private SqlCommand AllocSqlCommand(string query)
        {
            SqlCommand cmd = this.SqlConnectionObject.CreateCommand();

            cmd.CommandText = query;

            // Set up its parameters
            foreach (SqlParameter param in m_Parameters)
            {
                SqlParameter p = new SqlParameter(param.ParameterName, param.Value);
                p.SqlDbType = param.SqlDbType;
                p.Size = param.Size;
                cmd.Parameters.Add(p);
            }

            return cmd;
        }

        private SqlCommand CacheQuery(string query)
        {
            const int PREPARE_THRESHOLD = 3; // We start using prepared statements after the 3rd execution
            SqlCommand newCmd;

            // Only cache queries with parameters
            if (this.m_Parameters.Count > 0)
            {
                if (m_CommandCache.ContainsKey(query))
                {
                    SqlBatch item = (SqlBatch)m_CommandCache[query];
                    item.ExecutionCount++;

                    // If we've seen this batch PREPARE_THRESHOLD or more times, use prepared execution
                    if (item.ExecutionCount == PREPARE_THRESHOLD)
                    {
                        //TODO: Get a resolution for VSTS 162340 and uncomment the below
                        // batch.Command.Prepare();
                    }

                    // If we're already in the cache, copy the new values to the existing parameters
                    foreach (SqlParameter param in this.m_Parameters)
                    {
                        item.Command.Parameters[param.ParameterName].Value = param.Value;
                    }
                    return item.Command;
                }
                else
                {
                    // Not found in the cache; add it
                    newCmd = AllocSqlCommand(query);

                    // Add the new cmd to the cache
                    m_CommandCache.Add(new SqlBatch(newCmd));
                }
            }
            else
            {
                // No parameters - don't cache it
                newCmd = AllocSqlCommand(query);
            }

            return newCmd;
        }

        /// <summary>
        /// Used to add the query parameters when the query is in parameterization mode ParameterizeLiterals
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        private String AddParameterLiterals(Match match)
        {
            SqlParameter param = new SqlParameter("@_msparam_" + m_Parameters.Count.ToString(), match.Groups[1].Value);
            param.SqlDbType = SqlDbType.NVarChar;  // Auto-parameters are nvarchars
            param.Size = 4000; //Use nvarchar's max length in order to support large params
            this.m_Parameters.Add(param);
            return param.ParameterName;
        }

        /// <summary>
        /// Match delegate method for parameter replacement RegEx
        /// Adds each parameter value to parameter collection during RegEx
        /// search of query text
        /// Returns parameter marker (e.g., @P0) for insertion into query text
        /// </summary>
        /// <param name="match">Match object passed in by RegEx engine</param>
        /// <returns>Marker corresponding to value added to parameter collection</returns>
        private String AddParameterForced(Match match)
        {
            string val = string.Empty;
            SqlDbType dbType = SqlDbType.NVarChar;
            Group hexGrp = match.Groups["HEX"], strGrp = match.Groups["STR"];

            // System.Data.SqlClient.SqlDataReader cannot implicitly convert the 0x... hex values
            // into integer. The rest of the values are converted automatically.
            // The following code replaces the hex value with the corresponding decimal equivalent
            // before adding it as a parameter so that the data reader interprets it correctly. -anchals
            if (hexGrp != null && hexGrp.Length > 0)
            {
                val = Convert.ToInt64(hexGrp.ToString(), 16).ToString();
                dbType = SqlDbType.BigInt;
            }
            else if (strGrp != null)
            {
                val = strGrp.ToString();
            }
            else
            {
                Trace.Assert(false, "Unexpected:Parameterization: Regular expression mismatch!");
            }
            string replacementValue;
            // If current parameter count is more than or equal to the maximum parameters allowed then we
            // don't parameterize. This limit is imposed due to dependency on RPC parameter limit of
            // class SqlCommand.-anchals
            if (m_Parameters.Count < MaxParams_Default)
            {

                SqlParameter param = new SqlParameter("@_msparam_" + m_Parameters.Count.ToString(), val);
                param.SqlDbType = dbType;  // Auto-parameters are nvarchars
                param.Size = 4000; //Use nvarchar's max length in order to support large params
                this.m_Parameters.Add(param);
                replacementValue = param.ParameterName;
            }
            else
            {
                // if we exceeded the parameter limit then directly insert the value in the query back again.
                string escapeString = CommonUtils.EscapeString(val, "\'");
                replacementValue = @"'" + escapeString + @"'"; // even integer values in quotes is treated as integer by engine.

            }
            return replacementValue;
        }

        /// <summary>
        /// Translate a query with embedded literals to a normalized form (all literals replaced by
        /// a standard token)
        /// </summary>
        /// <remarks>
        /// Examples:
        ///
        /// SELECT * FROM table WHERE column = 123
        ///
        /// becomes:
        ///
        /// SELECT * FROM table WHERE column = ?
        ///
        /// SELECT * FROM table WHERE column = 'foo'
        ///
        /// becomes:
        ///
        /// SELECT * FROM table WHERE column = '?'
        /// </remarks>
        /// <param name="QueryText">Query text to normalize</param>
        /// <param name="QuotedIdentifiers">Switch indicating whether query text expects QUOTED_IDENTIFER to be enabled</param>
        /// <returns>Query text with inline literals translated to ? symbols</returns>
        public static string NormalizeQuery(string QueryText, bool QuotedIdentifiers)
        {
            string normText;
            normText = QueryText;
            if (reQueryPrepStatement.IsMatch(normText))
            {
                normText = reQueryPrepStatement.Replace(normText, "${qry}").Replace("''", "'");
            }
            if (QuotedIdentifiers)
            {
                normText = reQueryParametersQIOn.Replace(normText, "${left}${mid}${term}?${right}");
            }
            else
            {
                normText = reQueryParametersQIOff.Replace(normText, "${left}${mid}${term}?${right}");
            }
            return normText;
        }

        /// <summary>
        /// Overload for the above function -- see it for details
        /// </summary>
        /// <param name="QueryText"></param>
        /// <returns></returns>
        public static string NormalizeQuery(string QueryText)
        {
            return NormalizeQuery(QueryText, true);
        }

        ///<summary>Creates a SqlCommand with CommandText set to the specified query</summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private SqlCommand GetSqlCommand(string query)
        {
            SqlCommand cmd;
            if (parameterizationMode >= QueryParameterizationMode.ForcedParameterization)
            {
                m_Parameters.Clear();

                string paramQuery;
                if (reQueryTags.IsMatch(query))
                {
                    // Replace delimited parameter values with parameter markers
                    // Each parameter is wrapped in <msparam>...</msparam>
                    // RegEx match function (AddParameter) builds SqlParameter collection
                    paramQuery = reQueryTags.Replace(query, AddParameterForced);
                }
                else
                {
                    // Replace literal values with parameter markers
                    // RegEx match function (AddParameter) builds SqlParameter collection
                    if (parameterizationMode == QueryParameterizationMode.ParameterizeLiterals)
                    {
                        // Literals can't use the same AddParameter as ForcedParameterization uses as its regex is different.-anchals
                        paramQuery = reQueryParametersQIOn.Replace(query, AddParameterLiterals);
                    }
                    else
                    {
                        paramQuery = query;
                    }
                }

                // Defer USEs of databases other than master until subsequent cmd
                if (deferredUseMode >= DeferredUseMode.CollapseRedundant)
                {

                    // Special case USE statements
                    // Don't actually send singleton USE statements separately
                    Match useDb = reUseDb.Match(query);
                    if (useDb.Groups.Count > 1)
                    {
                        string dbname = useDb.Groups[1].Value;

                        // Don't send the USE if we've already sent it
                        if (
                            (dbname == SqlConnectionObject.Database) && // Attempting to reuse current database
                            (useDb.Groups[0].Value == query)            // Nothing in the query besides the USE
                            )
                        {
                            paramQuery = string.Empty;  // Signal to executor not to run query
                        }
                    }
                }

                if (!string.IsNullOrEmpty(paramQuery))
                {
                    cmd = CacheQuery(paramQuery);
                }
                else
                {
                    cmd = m_SqlCommand;
                    cmd.CommandText = "";
                }
            }
            else
            {
                cmd = m_SqlCommand;
                cmd.CommandText = query;
            }

            cmd.CommandTimeout = this.StatementTimeout;
            currentSqlCommand = cmd;
            return cmd;
        }

        private void CaptureCommand(string query)
        {
            this.GenerateStatementExecutedEvent(query);
            if (SqlExecutionModes.CaptureSql == (SqlExecutionModes.CaptureSql & this.SqlExecutionModes))
            {
                this.CapturedSql.Add(query);
            }
        }

        private bool IsDirectExecutionMode()
        {
            return SqlExecutionModes.ExecuteSql == (SqlExecutionModes.ExecuteSql & this.SqlExecutionModes);
        }

        ///<summary>
        /// This is the number of seconds that a statement is attempted to be sent to the
        /// server before it fails.
        /// Default is 600 seconds (same as Shiloh).
        /// Exceptions:
        /// InvalidPropertyValueException
        /// </summary>
        public int StatementTimeout
        {
            get
            {
                return m_StatementTimeout;
            }
            set
            {
                if (0 > value)
                {
                    throw new InvalidPropertyValueException();
                }
                m_StatementTimeout = value;
            }
        }

        #region ISqlConnection implementation

        /// <summary>
        /// </summary>
        bool ISfcConnection.Connect()
        {
            this.Connect();
            if (!this.IsOpen)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// </summary>
        bool ISfcConnection.Disconnect()
        {
            if (this.IsOpen)
            {
                this.Disconnect();
            }
            return !this.IsOpen;
        }

        ISfcConnection ISfcConnection.Copy()
        {
            return this.Copy();
        }

        Version ISfcConnection.ServerVersion
        {
            get
            {
                // Return parent class's ServerVersion as a .Net Version
                return (Version)this.ServerVersion;
            }
        }

        public ServerType ConnectionType
        {
            get
            {
                return ServerType.DatabaseEngine;
            }
        }

        /// Temporary function needed as long as the sql enumerator is
        /// unaware of the SqlStoreConnection type
        object ISfcConnection.ToEnumeratorObject()
        {
            return this;
        }

        #endregion
        // Changes the password on the server, accepts SecureString object
        // Exceptions:
        //	ChangePasswordFailureException
        public void ChangePassword(SecureString newPassword)
        {
            CheckDisconnected();

            try
            {
                SqlConnection.ChangePassword(this.ConnectionString, EncryptionUtility.DecryptSecureString(newPassword));
                this.SecurePassword = newPassword;
            }
            catch (SqlException e)
            {
                throw new ChangePasswordFailureException(StringConnectionInfo.PasswordCouldNotBeChanged, e);
            }
        }

        // Changes the password on the server.
        // Exceptions:
        //	ChangePasswordFailureException
        public void ChangePassword(string newPassword)
        {
            CheckDisconnected();

            try
            {
                SqlConnection.ChangePassword(this.ConnectionString, newPassword);
                this.ForceSetPassword(newPassword);
            }
            catch (SqlException e)
            {
                throw new ChangePasswordFailureException(StringConnectionInfo.PasswordCouldNotBeChanged, e);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sqlCommands"></param>
        /// <returns></returns>
        public int[] ExecuteNonQuery(StringCollection sqlCommands)
        {
            return ExecuteNonQuery(sqlCommands, ExecutionTypes.Default);
        }

        /// <summary>
        /// Executes the T-SQL statements in the StringCollection.
        /// The command termininator ('GO') is recognized by ExecuteNonQuery.
        /// Batches separated with the GO statement will be sent and executed individually.
        /// The statement is recorded and not executed if CaptureMode has been set to true.
        /// An array of int values is returned that contain the numbers of rows affected for
        /// each statement in the StringCollection.
        /// If multiple batches
        /// are executed, the total numbers of affected rows of all batches is returned.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommands"></param>
        /// <param name="executionType"></param>
        /// <returns></returns>
        public int[] ExecuteNonQuery(StringCollection sqlCommands, ExecutionTypes executionType)
        {
            return ExecuteNonQuery(sqlCommands, executionType, /*retry*/true);
        }

        /// <summary>
        /// Executes the T-SQL statements in the StringCollection.
        /// The command termininator ('GO') is recognized by ExecuteNonQuery.
        /// Batches separated with the GO statement will be sent and executed individually.
        /// The statement is recorded and not executed if CaptureMode has been set to true.
        /// An array of int values is returned that contain the numbers of rows affected for
        /// each statement in the StringCollection.
        /// If multiple batches
        /// are executed, the total numbers of affected rows of all batches is returned.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommands"></param>
        /// <param name="executionType"></param>
        /// <param name="retry">Whether we should retry if an exception is thrown during execution</param>
        /// <returns></returns>
        public int[] ExecuteNonQuery(StringCollection sqlCommands,
            ExecutionTypes executionType,
            bool retry)
        {
            CheckDisconnected();

            AutoDisconnectMode adm = this.AutoDisconnectMode;
            this.AutoDisconnectMode = AutoDisconnectMode.NoAutoDisconnect;
            try
            {
                ArrayList ar = new ArrayList();
                foreach (String query in sqlCommands)
                {
                    ar.Add(ExecuteNonQuery(query, executionType, retry));
                }
                int[] nlist = new int[ar.Count];
                ar.CopyTo(nlist);
                return nlist;
            }
            finally
            {
                this.AutoDisconnectMode = adm;
                this.PoolDisconnect();
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sqlCommand)
        {
            return ExecuteNonQuery(sqlCommand, ExecutionTypes.Default);
        }

        /// <summary>
        /// Executes a T-SQL statement. The command termininator ('GO') is recognized by
        /// ExecuteNonQuery. Batches will be sent and executed individually.
        /// The statement is recorded and not executed if CaptureMode has been set to true.
        /// An int value is returned that contain the numbers of rows affected. If multiple batches
        /// are executed, the total numbers of affected rows of all batches is returned.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="executionType"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sqlCommand,
            ExecutionTypes executionType)
        {
            return ExecuteNonQuery(sqlCommand, executionType, /*retry*/true);
        }

        /// <summary>
        /// Executes a T-SQL statement. The command termininator ('GO') is recognized by
        /// ExecuteNonQuery. Batches will be sent and executed individually.
        /// The statement is recorded and not executed if CaptureMode has been set to true.
        /// An int value is returned that contain the numbers of rows affected. If multiple batches
        /// are executed, the total numbers of affected rows of all batches is returned.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="executionType"></param>
        /// <param name="retry">Whether we should retry if an exception is thrown during execution</param>
        /// <returns></returns>
        public int ExecuteNonQuery(string sqlCommand,
            ExecutionTypes executionType,
            bool retry)
        {
            CheckDisconnected();

            int statementsToReverse = 0;
            int statementsExecuted = 0;
            StringCollection col = GetStatements(sqlCommand, executionType, ref statementsToReverse);
            if (!IsDirectExecutionMode())
            {
                foreach (string query in col)
                {
                    CaptureCommand(query);
                }
                return 0; //empty result
            }

            PoolConnect();
            try
            {
                int nAffectedRows = 0;
                foreach (string query in col)
                {
                    try
                    {
                        SqlCommand cmd = GetSqlCommand(query);

                        // Certain internal commands as well as redundant USE stmts
                        // have their CommandText blanked
                        // Signals to us not to really execute anything
                        if (!string.IsNullOrEmpty(cmd.CommandText))
                        {
                            CaptureCommand(query);
                            nAffectedRows += (int)ExecuteTSql(ExecuteTSqlAction.ExecuteNonQuery, cmd, null, retry);
                            statementsExecuted++;
                        }
                    }
                    catch (SqlException e)
                    {
                        RefreshTransactionDepth(e.Class);
                        if (ExecutionTypes.ContinueOnError != (ExecutionTypes.ContinueOnError & executionType))
                        {
                            if (statementsExecuted > 0 && e.Class <= 20) //not a connection problem
                            {
                                //reverse sets
                                int mustReversNo = statementsToReverse < statementsExecuted ? statementsToReverse : statementsExecuted;
                                for (int i = col.Count - mustReversNo; i < col.Count; i++)
                                {
                                    ExecuteNonQuery(col[i], ExecutionTypes.ContinueOnError, retry);
                                }
                            }
                            throw new ExecutionFailureException(StringConnectionInfo.ExecutionFailure, e);
                        }
                    }
                }
                return nAffectedRows;
            }
            finally
            {
                PoolDisconnect();
            }
        }

        private StringCollection GetStatements(String query, ExecutionTypes executionType, ref int statementsToReverse)
        {
            statementsToReverse = 0;
            StringCollection querycol = new StringCollection();

            if (ExecutionTypes.NoCommands == (ExecutionTypes.NoCommands & executionType))
            {
                querycol.Add(query);
            }
            else
            {
#if NATIVEBATCHPARSER
                //Call NetCoreHelpers method to call the appropriate method for this framework.
                string fullAsmName = typeof(ServerConnection).GetAssembly().FullName.Replace("ConnectionInfo", "BatchParserClient");
                Assembly bpClient = NetCoreHelpers.LoadAssembly(fullAsmName);

                Type batchType = bpClient.GetType("Microsoft.SqlServer.Management.Common.ExecuteBatch", true);
                object executeBatch = batchType.InvokeMember(".ctor", BindingFlags.CreateInstance, null, null, new object[] { },
                    System.Globalization.CultureInfo.InvariantCulture);

                StringCollection stms = (StringCollection)batchType.InvokeMember("GetStatements",
                                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public,
                                    null,
                                    executeBatch,
                                    new object[] { query }, System.Globalization.CultureInfo.InvariantCulture);
#else
                BatchParser batchParser = new BatchParser();
                Parser commandParser = new Parser(batchParser, batchParser, new System.IO.StringReader(query), "[script]");
                commandParser.SetRecognizeSqlCmdSyntax(false);
                commandParser.ThrowOnUnresolvedVariable = true;

                StringCollection stms = new StringCollection();
                // BatchParser is written with the general idea that the batch will actually be
                // executed during parsing.  Here, though, we simply gather the batches for subsequent processing.
                batchParser.Execute = (batchScript, num, lineNumber) =>
                {
                    num = Math.Max(num, 1);
                    for (int i = 0; i < num; ++i)
                    {
                        stms.Add(batchScript);
                    }
                    return true;
                };
                batchParser.ErrorMessage = (message, messageType) => { };
                batchParser.Message = (message) => { };
                batchParser.StartingLine = 0;
                batchParser.DisableVariableSubstitution();
                commandParser.Parse();
#endif

                foreach (String s in stms)
                {
                    querycol.Add(s);
                }
            }

            if (ExecutionTypes.QuotedIdentifierOn == (ExecutionTypes.QuotedIdentifierOn & executionType))
            {
                statementsToReverse++;
                querycol.Insert(0, "SET QUOTED_IDENTIFIER ON");
                querycol.Add("SET QUOTED_IDENTIFIER OFF");
            }

            if (ExecutionTypes.ParseOnly == (ExecutionTypes.ParseOnly & executionType))
            {
                statementsToReverse++;
                querycol.Insert(0, "SET PARSEONLY ON");
                querycol.Add("SET PARSEONLY OFF");
            }

            if (ExecutionTypes.NoExec == (ExecutionTypes.NoExec & executionType))
            {
                statementsToReverse++;
                querycol.Insert(0, "SET NOEXEC ON");
                querycol.Add("SET NOEXEC OFF");
            }
            return querycol;
        }

        /// <summary>
        /// Defines the batch separator used by the ExecuteNonQuery methods. Default is "GO".
        /// The batch separator is case-insensitive.
        /// </summary>
        public string BatchSeparator
        {
            get
            {
                return m_BatchSeparator;
            }
            set
            {
                if (0 >= m_BatchSeparator.Length)
                {
                    throw new InvalidPropertyValueException();
                }
                m_BatchSeparator = value;
            }
        }

        /// <summary>
        /// Executes the T-SQL statements in the StringCollection.
        /// An array of DataSets are returned that contain the results for each statement
        /// in the StringCollection.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommands"></param>
        /// <returns></returns>

        public DataSet[] ExecuteWithResults(StringCollection sqlCommands)
        {
            AutoDisconnectMode adm = this.AutoDisconnectMode;
            this.AutoDisconnectMode = AutoDisconnectMode.NoAutoDisconnect;
            try
            {
                ArrayList ar = new ArrayList();
                foreach (String query in sqlCommands)
                {
                    ar.Add(ExecuteWithResults(query));
                }
                DataSet[] dslist = new DataSet[ar.Count];
                ar.CopyTo(dslist);
                return dslist;
            }
            finally
            {
                this.AutoDisconnectMode = adm;
                this.PoolDisconnect();
            }
        }

        /// <summary>
        /// Executes the T-SQL statements in sqlCommand.
        /// A DataSet is returned that contains the results.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public DataSet ExecuteWithResults(string sqlCommand)
        {
            return ExecuteWithResults(sqlCommand, /*retry*/true);
        }

        /// <summary>
        /// Executes the T-SQL statements in sqlCommand.
        /// A DataSet is returned that contains the results.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <param name="retry">Whether we should retry if an exception is thrown during execution</param>
        /// <returns></returns>

        public DataSet ExecuteWithResults(string sqlCommand, bool retry)
        {
            CheckDisconnected();

            CaptureCommand(sqlCommand);
            if (!IsDirectExecutionMode())
            {
                return null;
            }
            PoolConnect();
            try
            {
                DataSet ds = new DataSet();
                ds.Locale = System.Globalization.CultureInfo.InvariantCulture;
                SqlDataAdapter dataAdapter = new SqlDataAdapter();
                dataAdapter.SelectCommand = GetSqlCommand(sqlCommand);

                // Due to deferred USEs, CommandText may be blank
                if (!string.IsNullOrEmpty(dataAdapter.SelectCommand.CommandText))
                {
                    ExecuteTSql(ExecuteTSqlAction.FillDataSet, dataAdapter, ds, retry);
                }
                return ds;
            }
            catch (SqlException e)
            {
                RefreshTransactionDepth(e.Class);
                throw new ExecutionFailureException(StringConnectionInfo.ExecutionFailure, e);
            }
            finally
            {
                PoolDisconnect();
            }
        }

        /// <summary>
        /// Executes T-SQL statements.
        /// A SqlDataReader object is returned that can be used to read the stream of
        /// rows that are returned by SQL Server.
        /// The Connect() method will be called if the connection with the server is not open.
        /// it doesn't automatically disconnect
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand">The SQL command text.</param>
        /// <returns>The data reader.</returns>
        public SqlDataReader ExecuteReader(string sqlCommand)
        {
            SqlCommand command;
            return this.ExecuteReader(sqlCommand, out command);
        }

        /// <summary>
        /// Executes T-SQL statements.
        /// A SqlDataReader object is returned that can be used to read the stream of
        /// rows that are returned by SQL Server.
        /// A SqlCommand object is also returned that can be used to cancel the data reader pipe if an abort of a long-running query is needed.
        /// it doesn't automatically disconnect
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand">The SQL command text.</param>
        /// <param name="command">The resulting SqlCommand object for data reader pipe cancellation.</param>
        /// <returns>The data reader.</returns>
        public SqlDataReader ExecuteReader(string sqlCommand, out SqlCommand command)
        {
            command = null;
            if (string.IsNullOrEmpty(sqlCommand))
            {
                return null;
            }
            // Send the SqlCommand object to the caller in case they want to remember it for subsequent reader pipe cancellation.
            command = GetSqlCommand(sqlCommand); // Due to deferred USEs, CommandText of the command may be blank

            return this.GetExecuteReader(command);
        }

        /// <summary>
        /// Executes T-SQL statements.
        /// A SqlDataReader object is returned that can be used to read the stream of
        /// rows that are returned by SQL Server.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="command">SQL command object.</param>
        /// <returns>The data reader.</returns>
        internal SqlDataReader ExecuteReader(SqlCommand command)
        {
            if (command == null)
            {
                return null;
            }

            command.Connection = this.SqlConnectionObject;
            return this.GetExecuteReader(command);
        }

        /// <summary>
        /// Returns a SqlDataReader with an active connection.
        /// </summary>
        /// <param name="command">Ready to execute sqlcommand object.</param>
        /// <returns></returns>
        private SqlDataReader GetExecuteReader(SqlCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.CommandText))
            {
                return null;
            }

            CheckDisconnected();

            CaptureCommand(command.CommandText);
            if (!IsDirectExecutionMode())
            {
                return null;
            }
            PoolConnect();
            try
            {
                return ExecuteTSql(ExecuteTSqlAction.ExecuteReader, command, null, true) as SqlDataReader;
            }
            catch (SqlException e)
            {
                RefreshTransactionDepth(e.Class);
                throw new ExecutionFailureException(StringConnectionInfo.ExecutionFailure, e);
            }
            //no auto disconnect
        }

        /// <summary>
        /// Executes the T-SQL statements in the StringCollection.
        /// An array of objects are returned that each contain the first column of the
        /// first row of the result set of each executed statement.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommands"></param>
        /// <returns></returns>
        public object[] ExecuteScalar(StringCollection sqlCommands)
        {
            CheckDisconnected();

            AutoDisconnectMode adm = this.AutoDisconnectMode;
            this.AutoDisconnectMode = AutoDisconnectMode.NoAutoDisconnect;
            try
            {
                ArrayList ar = new ArrayList();
                foreach (String query in sqlCommands)
                {
                    ar.Add(ExecuteScalar(query));
                }
                object[] olist = new object[ar.Count];
                ar.CopyTo(olist);
                return olist;
            }
            finally
            {
                this.AutoDisconnectMode = adm;
                this.PoolDisconnect();
            }
        }

        /// <summary>
        /// Executes a T-SQL statement.
        /// An objects is returned that contains the first column of the first row of
        /// the result set.
        /// The Connect() method will be called if the connection with the server is not open.
        /// Exceptions:
        /// ConnectionFailureException
        /// ExecutionFailureExeception
        /// </summary>
        /// <param name="sqlCommand"></param>
        /// <returns></returns>
        public object ExecuteScalar(string sqlCommand)
        {
            CheckDisconnected();

            CaptureCommand(sqlCommand);
            if (!IsDirectExecutionMode())
            {
                return null;
            }
            PoolConnect();
            try
            {
                object result = null;
                SqlCommand cmd = GetSqlCommand(sqlCommand);
                if (!string.IsNullOrEmpty(cmd.CommandText))
                {
                    if (cachedQueries)
                    {
                        // Make sure we create an entry for cache, if GetSqlCommand does not already do it
                        SqlBatch item;
                        if (!m_CommandCache.ContainsKey(cmd.CommandText))
                        {
                            item = new SqlBatch(cmd);
                            m_CommandCache.Add(item);
                        }
                        else
                        {
                            item = (SqlBatch)m_CommandCache[cmd.CommandText];
                        }

                        // Set result to cached value, or update the cache
                        if (!item.HasResult())
                        {
                            result = ExecuteTSql(ExecuteTSqlAction.ExecuteScalar, cmd, null, true);
                            // Add to cache
                            item.Result = result;
                        }
                        else
                        {
                            result = item.Result;
                        }
                    }
                    else // !cachedQueries
                    {
                        result = ExecuteTSql(ExecuteTSqlAction.ExecuteScalar, cmd, null, true);
                    }
                }

                return result;
            }
            catch (SqlException e)
            {
                RefreshTransactionDepth(e.Class);
                throw new ExecutionFailureException(StringConnectionInfo.ExecutionFailure, e);
            }
            finally
            {
                PoolDisconnect();
            }
        }

        /// <summary>
        /// Starts a new transaction. If CaptureMode is true, the transaction statement
        /// will be added to the capture buffer.It is possible to nest transactions.
        /// It is the user's responsibility to commit or rollback every opened transaction.
        /// It is not guaranteed that all T-SQL emitted by the object model, can be
        /// encapsulated by a transaction. Furthermore, the state of the objects that
        /// have been changed will not reflect the actual database state until a
        /// transaction has been committed or after a transaction has been rolled backed.
        /// It is the responsibility of the user to refresh affected objects.
        /// Use with care.
        /// </summary>
        public void BeginTransaction()
        {
            CheckDisconnected();

            AutoDisconnectMode adm = this.AutoDisconnectMode;
            this.AutoDisconnectMode = AutoDisconnectMode.NoAutoDisconnect;
            try
            {
                this.ExecuteNonQuery("BEGIN TRANSACTION", ExecutionTypes.Default);
                m_TransactionDepth++;
            }
            finally
            {
                this.AutoDisconnectMode = adm;
                if (0 >= m_TransactionDepth)
                {
                    PoolDisconnect();
                }
            }
        }

        /// <summary>
        /// Commits a transaction.
        /// If CaptureMode is true, the transaction command will be added to the
        /// capture buffer.
        /// Exceptions:
        /// NotInTransactionException
        /// </summary>
        public void CommitTransaction()
        {
            CheckDisconnected();

            try
            {
                this.ExecuteNonQuery("if (@@trancount > 0) COMMIT TRANSACTION", ExecutionTypes.Default);
                if (m_TransactionDepth > 0)
                {
                    m_TransactionDepth--;
                }
            }
            catch (ExecutionFailureException efe)
            {
                SqlException se = efe.InnerException as SqlException;
                //Server: Msg 3902, Level 16, State 1, Line 1
                //The COMMIT TRANSACTION request has no corresponding BEGIN TRANSACTION.
                if (null != se)
                {
                    if (16 == se.Class && 3902 == se.Number)
                    {
                        m_TransactionDepth = 0;
                        throw new NotInTransactionException(StringConnectionInfo.NotInTransaction);
                    }
                }
                throw;
            }
            finally
            {
                PoolDisconnect();
            }
        }

        /// <summary>
        /// Aborts a transaction (all changes made will not be saved to the database).
        /// If CaptureMode is true, the transaction command will be added to the
        /// capture buffer.
        /// Exceptions:
        /// NotInTransactionException
        /// </summary>
        public void RollBackTransaction()
        {
            CheckDisconnected();

            try
            {
                this.ExecuteNonQuery("if (@@trancount > 0) ROLLBACK TRANSACTION", ExecutionTypes.Default);
                if (m_TransactionDepth > 0)
                {
                    m_TransactionDepth--;
                }
            }
            catch (ExecutionFailureException efe)
            {
                SqlException se = efe.InnerException as SqlException;
                //Server: Msg 3903, Level 16, State 1, Line 1
                //The ROLLBACK TRANSACTION request has no corresponding BEGIN TRANSACTION.
                if (null != se)
                {
                    if (16 == se.Class && 3903 == se.Number)
                    {
                        m_TransactionDepth = 0;
                        throw new NotInTransactionException(StringConnectionInfo.NotInTransaction);
                    }
                }
                throw;
            }
            finally
            {
                PoolDisconnect();
            }
        }

        /// <summary>
        /// Provides the transaction depth as counted by the object model. This
        /// doesn't include any transactions that may have been started on the
        /// server, or by issuing BEGIN TRAN with the ExecuteNonQuery method.
        /// </summary>
        public int TransactionDepth
        {
            get
            {
                CheckDisconnected();
                m_TransactionDepth = (int)this.ExecuteScalar("select @@TRANCOUNT");
                return m_TransactionDepth;
            }
        }

        private void RefreshTransactionDepth(byte severity)
        {
            CheckDisconnected();

            if (m_TransactionDepth <= 0 || severity >= 20) //not in transaction or connection is dead
            {
                m_TransactionDepth = 0;
                return;
            }
            SqlExecutionModes em = this.SqlExecutionModes;

            try
            {
                this.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                m_TransactionDepth = (int)this.ExecuteScalar("select @@TRANCOUNT");
            }
            finally
            {
                this.SqlExecutionModes = em;
            }
        }

        /// <summary>
        /// Determines if SQL statements are captured or sent to the server.
        /// </summary>
        public SqlExecutionModes SqlExecutionModes
        {
            get
            {
                return m_ExecutionMode;
            }
            set
            {
                m_ExecutionMode = value;
            }
        }

        /// <summary>
        /// Returns an enum that specified the fixed server role
        /// the login is member of.
        /// </summary>
        public FixedServerRoles FixedServerRoles
        {
            get
            {
                SqlExecutionModes em = this.SqlExecutionModes;
                try
                {
                    this.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    return (FixedServerRoles)(int)this.ExecuteScalar("select is_srvrolemember('sysadmin') * 1 +" +
                        "is_srvrolemember('serveradmin') * 2 +" +
                        "is_srvrolemember('setupadmin') * 4 +" +
                        "is_srvrolemember('securityadmin') * 8 +" +
                        "is_srvrolemember('processadmin') * 16 +" +
                        "is_srvrolemember('dbcreator') * 32 +" +
                        "is_srvrolemember('diskadmin') * 64" +
                        ((7 < this.ServerVersion.Major) ? "+ is_srvrolemember('bulkadmin') * 128" : ""));
                }
                finally
                {
                    this.SqlExecutionModes = em;
                }
            }
        }

        /// <summary>
        /// Tests if login is member of any of the specified server roles.
        /// </summary>
        public bool IsInFixedServerRole(FixedServerRoles fixedServerRole)
        {
            return fixedServerRole == (fixedServerRole & this.FixedServerRoles);
        }

        /// <summary>
        /// The UserProfile property returns a high-level role description for the Microsoft SQL Server login used by the current connection.
        /// </summary>
        public ServerUserProfiles UserProfile
        {
            get
            {
                SqlExecutionModes em = this.SqlExecutionModes;
                try
                {
                    this.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    return (ServerUserProfiles)(int)this.ExecuteScalar("exec master.dbo.sp_MSdbuserpriv N'serv'");
                }
                catch (ExecutionFailureException exception)
                {
                    // Handling of an error:
                    //
                    // Msg 15517, Level 16, State 1, Procedure sp_MSdbuserpriv, Line 0
                    // Cannot execute as the database principal because the principal "guest" does not exist, this type of principal cannot be impersonated, or you do not have permission.
                    //
                    // This means that our login has only 'guest' permission in master and cannot even run sp_MSdbuserpriv.
                    // In such case we can return ServerUserProfiles.None, because indeed we have no permissions.
                    //
                    SqlException sqlError = exception.InnerException as SqlException;
                    if (null != sqlError)
                    {
                        if (sqlError.Number == 15517 && sqlError.Class == 16)
                        {
                            return ServerUserProfiles.None;
                        }
                    }

                    // re-throw all other errors
                    throw;
                }
                finally
                {
                    this.SqlExecutionModes = em;
                }
            }
        }

        /// <summary>
        /// The ProcessID property returns the SQL Server process identifier for the connection
        /// used by the ServerConnection object.
        /// </summary>
        public int ProcessID
        {
            get
            {
#if MICROSOFTDATA
                CheckDisconnected();
                PoolConnect();
                return SqlConnectionObject.ServerProcessId;
#else
                SqlExecutionModes em = this.SqlExecutionModes;
                try
                {
                    this.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                    return Convert.ToInt32(this.ExecuteScalar("select @@SPID"), ConnectionInfoBase.DefaultCulture);
                }
                finally
                {
                    this.SqlExecutionModes = em;
                }
#endif
            }
        }

        private string m_TrueLogin;
        /// <summary>
        /// The TrueLogin property returns the login record name used by the current connection.
        /// </summary>
        public string TrueLogin
        {
            get
            {
                if (null == m_TrueLogin)
                {
                    SqlExecutionModes em = this.SqlExecutionModes;
                    try
                    {
                        this.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                        m_TrueLogin = (string)this.ExecuteScalar("select suser_sname()");
                    }
                    finally
                    {
                        this.SqlExecutionModes = em;
                    }
                }
                return m_TrueLogin;
            }
        }

        private string m_TrueName;
        /// <summary>
        /// The TrueName property returns the result set of the Microsoft® SQL Server global function @@INSTANCENAME.
        /// </summary>
        public string TrueName
        {
            get
            {
                if (null == m_TrueName)
                {
                    if (this.IsForceDisconnected)
                    {
                        throw new DisconnectedConnectionException(StringConnectionInfo.TrueNameMustBeSet);
                    }

                    SqlExecutionModes em = this.SqlExecutionModes;
                    try
                    {
                        this.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
                        if (7 < this.ServerVersion.Major)
                        {
                            m_TrueName = this.ExecuteScalar("select SERVERPROPERTY(N'servername')") as string;
                        }
                        else
                        {
                            m_TrueName = (string)this.ExecuteScalar("select @@SERVERNAME");
                        }
                    }
                    finally
                    {
                        this.SqlExecutionModes = em;
                    }
                }
                return m_TrueName;
            }
            set
            {
                if (!this.IsForceDisconnected && this.IsOpen)
                {
                    throw new DisconnectedConnectionException(StringConnectionInfo.CannotSetTrueName);
                }
                m_TrueName = value;
            }
        }

        public void Cancel()
        {
            if (!String.IsNullOrEmpty(currentSqlCommand.CommandText))
            {
                currentSqlCommand.Cancel();
            }
        }

        internal override void InitAfterConnect()
        {
            m_TransactionDepth = 0;
        }

        internal override bool BlockPoolDisconnect
        {
            get
            {
                return ((m_TransactionDepth > 0) || (ServerConnection.deferredUseMode > DeferredUseMode.None));
            }
        }

        internal void CheckDisconnected()
        {
            if (this.IsForceDisconnected)
            {
                throw new DisconnectedConnectionException(StringConnectionInfo.CannotPerformOperationWhileDisconnected);
            }
        }

        /// <summary>
        /// Returns a connection that has the specified database name in the connection string.
        /// If the current connection is already referencing the given database, the current connection is returned.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="poolConnection"></param>
        /// <returns></returns>
        public ServerConnection GetDatabaseConnection(string dbName, bool poolConnection = true)
        {
            return GetDatabaseConnection(dbName, poolConnection, null);
        }

        /// <summary>
        /// Returns a connection that has the specified database name in the connection string
        /// If the current connection is already referencing the given database, the current connection is returned.
        /// </summary>
        /// <param name="dbName"></param>
        /// <param name="poolConnection"></param>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        public ServerConnection GetDatabaseConnection(string dbName, bool poolConnection, IRenewableToken accessToken)
        {
            var dbNamesComparer = ServerConnection.ConnectionFactory.GetInstance(this).ServerComparer.DatabaseNameEqualityComparer;
            return dbNamesComparer.Equals(dbName, DatabaseName) ?
                  this
                : ConnectionFactory.GetInstance(this).GetDatabaseConnection(dbName, poolConnection, accessToken ?? AccessToken);
        }

        /// <summary>
        /// The factory also takes care of caching the connections
        /// </summary>
        internal class ConnectionFactory
        {
            private string connectionString;
            private ServerComparer serverComparer;
            private Dictionary<string, ServerConnection> pooledDbConnectionsCache;
            // It's not safe to reuse ServerConnection instances on multiple threads, but we don't want
            // to use non-pooled connections much because most code tends to leak ServerConnection references.
            // We compromise by making the cache thread local.
            [ThreadStatic]
            private static Dictionary<string, ConnectionFactory> connFactoriesCache;

            private static IDictionary<string, ConnectionFactory> ConnFactoriesCache
            {
                get { return connFactoriesCache ?? (connFactoriesCache = new Dictionary<string, ConnectionFactory>()); }
            }

            /// <summary>
            /// Gets the server comparer used for this connection factory
            /// </summary>
            public ServerComparer ServerComparer
            {
                get
                {
                    return this.serverComparer;
                }
            }

            /// <summary>
            /// Gets the connection factory instance for the given server connection
            /// </summary>
            /// <param name="serverConnection">Connection for which to get a factory</param>
            /// <returns>A cached connection factory instance</returns>
            public static ConnectionFactory GetInstance(ServerConnection serverConnection)
            {
                ConnectionFactory connFactory;

                if (!ConnFactoriesCache.TryGetValue(serverConnection.ConnectionString, out connFactory))
                {
                    connFactory = new ConnectionFactory(serverConnection);
                    ConnFactoriesCache[serverConnection.ConnectionString] = connFactory;
                }

                return connFactory;
            }

            /// <summary>
            /// Creates a new <see cref="ConnectionFactory"/> object
            /// </summary>
            /// <param name="serverConnection">Connection for which to create the connection factory</param>
            private ConnectionFactory(ServerConnection serverConnection)
            {
                this.connectionString = serverConnection.ConnectionString;
                this.serverComparer = new ServerComparer(serverConnection, "master");

                // The pooled database connections cache uses a dictionary with a key comparer based on the collation
                // of the target server.
                //
                this.pooledDbConnectionsCache = new Dictionary<string, ServerConnection>(new DatabaseNameEqualityComparer(serverComparer));
            }

            /// <summary>
            /// Returns a connection that has the specified database name in the connection string
            /// </summary>
            /// <param name="dbName">Database for which to get a connection</param>
            /// <param name="poolConnection">Indicates if the connection should be pooled</param>
            /// <param name="accessToken">Access token for the connection</param>
            /// <returns>Connection to the specified database</returns>
            public ServerConnection GetDatabaseConnection(string dbName, bool poolConnection = true, IRenewableToken accessToken = null)
            {
                ServerConnection dbConnection;
                //Only pooled connections are cached since they can be reused
                if (poolConnection)
                {
                    // check if the connection is not yet cached
                    if (!pooledDbConnectionsCache.TryGetValue(dbName, out dbConnection))
                    {
                        lock (this)
                        {
                            if (!pooledDbConnectionsCache.TryGetValue(dbName, out dbConnection))
                            {
                                dbConnection = this.CreateServerConnection(this.connectionString, dbName, poolConnection, accessToken);
                                pooledDbConnectionsCache[dbName] = dbConnection;
                            }
                        }
                    }
                    // We have race conditions where the first db connection fetch doesn't pass in the token
                    // Make sure later requests are given the right token.
                    dbConnection.AccessToken = accessToken ?? dbConnection.AccessToken;
                }
                else
                {
                    dbConnection = this.CreateServerConnection(this.connectionString, dbName, poolConnection, accessToken);
                }

                return dbConnection;
            }

            /// <summary>
            /// Helper function to create a ServerConnection object with the specified parameters
            /// </summary>
            /// <param name="connString">Connection string to use</param>
            /// <param name="initialCatalog">Initial database</param>
            /// <param name="poolConn">Indicates if pooling should be used</param>
            /// <param name="accessToken">Access token for the connection</param>
            /// <returns>Server connection</returns>
            private ServerConnection CreateServerConnection(string connString, string initialCatalog, bool poolConn, IRenewableToken accessToken = null)
            {
                var builder = new SqlConnectionStringBuilder(connString)
                {
                    InitialCatalog = initialCatalog,
                    Pooling = poolConn
                };

                ServerConnection serverConnection;
                if (accessToken != null)
                {
                    serverConnection = new ServerConnection(accessToken)
                    {
                        ConnectionString = builder.ConnectionString
                    };
                }
                else
                {
// Initialize the server connection from a SQL connection to make sure we copy all properties to the new connection
                    serverConnection = new ServerConnection(new SqlConnection(builder.ConnectionString));
                }

                return serverConnection;
            }
        }
    }
}
