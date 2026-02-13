// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Data;
#if MICROSOFTDATA
#else
using System.Data.SqlClient;
#endif
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Management;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.SqlServer.Management.Smo.Broker;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo.Mail;
using Microsoft.SqlServer.Server;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;
using System.Linq.Expressions;

namespace Microsoft.SqlServer.Management.Smo
{
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [Microsoft.SqlServer.Management.Sdk.Sfc.PhysicalFacet]
    [Sfc.RootFacetAttribute(typeof(Server))]
    public partial class Server : SqlSmoObject, Cmn.IAlterable, IScriptable, IServerSettings, IServerInformation, IAlienRoot, ISfcDomainLite
    {
        private ExecutionManager m_ExecutionManager;
        //Server is root for SMO domain. It should provide domain name, which is used in serialization, among others.
        private const string DomainName = "SMO";

        public Server(string name)
            : base()
        {
            m_ExecutionManager = new ExecutionManager(name);
            m_ExecutionManager.Parent = this;
            Init();
        }

        public Server()
            : base()
        {
            m_ExecutionManager = new ExecutionManager(".");
            m_ExecutionManager.Parent = this;
            Init();
        }


        private ServerConnection serverConnection;
        /// <summary>
        /// Constructs a new Server object that relies on the given ServerConnection for connectivity.
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <remarks>If serverConnection.ConnectAsUser is true, its NonPooledConnection property must also be true.
        /// Otherwise, Server may attempt to duplicate the ServerConnection without preserving the ConnectAsUser parameters, 
        /// leading to either failed connections or connections as the incorrect user when using integrated security.
        /// </remarks>
        public Server(ServerConnection serverConnection)
            : base()
        {
            this.serverConnection = serverConnection;
            //Note: Execution Manager for this case will be initialized in the GetExecutionManager() method
            //   to take care of the transparent conenction switching for the Cloud DB
            //   (as the tranparent switching within this constructor breaks some usercases of the on-premises server)
            Init();
        }

        private bool IsAzureDbScopedConnection(ServerConnection sc)
        {
            // we delay making a connection to fetch the engine type since we can eliminate common cases without it.
            return ((!string.IsNullOrEmpty(sc.DatabaseName) && sc.DatabaseName.ToUpperInvariant() != "MASTER") ||
                    (!string.IsNullOrEmpty(sc.InitialCatalog) && sc.InitialCatalog.ToUpperInvariant() != "MASTER"))
                   && sc.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase;
        }

        public override ExecutionManager ExecutionManager
        {
            get
            {
                return this.GetExecutionManager();
            }
        }
        void Init()
        {
            if (this.serverConnection == null)
            {
                Debug.Assert(m_ExecutionManager != null, "m_ExecutionManager == null");
                this.serverConnection = m_ExecutionManager.ConnectionContext;
            }

            SetState(SqlSmoState.Existing);
            objectInSpace = false;
            SetObjectKey(new SimpleObjectKey(this.Name));
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public string Name
        {
            get
            {
                try
                {
                    if (!SqlContext.IsAvailable)
                    {
                        //try to get it from server connection property
                        return NormalizeServerName(ConnectionContext.ServerInstance);
                    }
                    else
                    {
                        return ConnectionContext.ServerInstance;
                    }
                }
                catch (PropertyNotAvailableException)
                {
                    //failed to syncronize with connection context because it has a connection string set
                    //try to get it from server
                    try
                    {
                        return ConnectionContext.TrueName;
                    }
                    catch (ExecutionFailureException)
                    {

                    }
                }
                //could not determine the name
                return string.Empty;
            }
        }

        //serialization adapter is needed since system.version cannot be serialized by xmlserializer (because state is not settable).
        [SfcSerializationAdapter(typeof(VersionSerializationAdapter))]
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Version Version
        {
            get
            {
                ServerVersion sv = this.ServerVersion;
                return new Version(sv.Major, sv.Minor, sv.BuildNumber);
            }
            //internal so that public API doesn't change. Fine for now since serializer uses reflection.
            //In the near future, SFC will be made friend of this assembly, so this setter becomes
            //available at compile time as well.
            internal set
            {

                //make server version settable in design mode, so that serializer can set the value.
                if (this.IsDesignMode)
                {
                    if (value != null)
                    {
                        this.ConnectionContext.ServerVersion = new ServerVersion(value.Major, value.Minor, value.Build);
                    }
                    else
                    {
                        this.ConnectionContext.ServerVersion = null;
                    }
                }
                else
                {
                    Debug.Assert(false, "Version property of Server can only be set in design mode");
                }
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Edition EngineEdition
        {
            get
            {
                // from Books Online (SQL Server 2005):
                // SERVERPROPERTY('EngineEdition') returns the following
                //   1 = Personal or Desktop Engine
                //   2 = Standard
                //   3 = Enterprise, Enterprise Evaluation, or Developer
                //   4 = Express
                int result = (Int32)this.Properties.GetValueWithNullReplacement("EngineEdition");
                return (Edition)result;
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Version ResourceVersion
        {
            get
            {
                return new Version(this.ResourceVersionString);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Version BuildClrVersion
        {
            get
            {
                //BuildClrVersionString is of format 'v2.0.50727', hence getting substring leaving first character
                return new Version(this.BuildClrVersionString.Substring(1));
            }
        }


        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public string Collation => ExecutionManager.ConnectionContext.Collation;

        /// <summary>
        /// Overrides the standard behavior of scripting object permissions.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sp"></param>
        internal override void AddScriptPermission(StringCollection query, ScriptingPreferences sp)
        {
            // script server permissions.
            AddScriptPermissions(query, PermissionWorker.PermissionEnumKind.Server, sp);
        }

        internal void SetServerNameFromConnectionInfo()
        {
            SetObjectKey(new SimpleObjectKey(NormalizeServerName(ConnectionContext.ServerInstance)));
        }

        private string NormalizeServerName(string name)
        {
            // treat special server names : ".", "", "(locall)"
            if (name == "." || name == "(local)" || name == "localhost" || name.Length == 0)
            {
                return System.Environment.MachineName;
            }

            // named instances on the above exceptions
            if (name.StartsWith(".\\", StringComparison.Ordinal))
            {
                return name.Replace(".", System.Environment.MachineName);
            }

            if (name.StartsWith("(local)\\", StringComparison.Ordinal))
            {
                return name.Replace("(local)", System.Environment.MachineName);
            }

            return name;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Server";
            }
        }

        /// <summary>
        /// This object supports permissions.
        /// </summary>
        internal override UserPermissionCollection Permissions
        {
            get
            {
                // call the base class
                return GetUserPermissions();
            }
        }

        /// <summary>
        /// Returns the comparer object corresponding to the collation string passed.
        /// </summary>
        /// <param name="collationName"></param>
        /// <returns></returns>
        public IComparer GetStringComparer(string collationName)
        {
            int lcid = GetLCIDCollation(collationName);
            StringComparer comparer = new StringComparer(collationName, lcid);
            return comparer;
        }

        //given a collation name, reads the collation atributes from server
        //and constructs a StringComparer
        SortedList collationCache;
        internal int GetLCIDCollation(string collationName)
        {
            //Latin1 this is a collation hardcoded for some of the metadata
            //columns make it a special case
            //excluding SQL_Latin1_General_CP1254 as its lcid is 1055 instead of 1033
            if (collationName.Contains("Latin1") && !collationName.Contains("SQL_Latin1_General_CP1254"))
            {
                return 1033;
            }

            //VSTS 763652: "Culture isn't supported failure" is hit using SSMS to connect SQL instance when SQLCOLLATION="Japanese_Unicode_CS_AS_KS_WS".
            //According to msdn http://support.microsoft.com/?id=302747, Japanese_Unicode
            //has been deprecated from Windows Server 2000 onwards. The link suggests to use LCID 1041 in the clients.
            if (collationName.StartsWith("Japanese_Unicode", StringComparison.Ordinal))
            {
                return 1041;
            }

            // Try to get the LCID without trying to get to the server, which can be unnecessary expensive 
            // If the collation does not happen to be known, then we'll pay the price of a trip to the server.
            var colInfo = CollationInfo.GetCollationInfo(collationName);
            if (colInfo != null)
            {
                return colInfo.LCID;
            }

            //initialize the collation cache if needed
            if (null == collationCache)
            {
                collationCache = new SortedList(System.StringComparer.Ordinal);
            }

            //if we don't already store the collation lcid
            //we have to get it from the server
            if (!collationCache.Contains(collationName))
            {
                //build enumerator rquest
                Request req = new Request(
                    "Server/Collation[@Name = '" + Urn.EscapeString(collationName) + "']",
                    new String[] { "LocaleID" });

                DataTable collationTable = this.ExecutionManager.GetEnumeratorData(req);
                //if we have something valid use it
                if (collationTable.Rows.Count == 1 && !(collationTable.Rows[0][0] is DBNull))
                {
                    collationCache[collationName] = (int)collationTable.Rows[0][0];
                }
                //else fall back to 1033
                else
                {
                    collationCache[collationName] = 1033;
                }
            }
            return (int)collationCache[collationName];
        }


        /// <summary>
        /// Returns the object with the corresponding Urn in string form
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        object IAlienRoot.SfcHelper_GetSmoObject(string urn)
        {
            return GetSmoObject(new Urn(urn));
        }

        /// <summary>
        ///
        /// Helper for SFC. Ask Enumerator for a DataTable of results given a Urn.
        /// </summary>
        /// <returns>DataTable</returns>
        DataTable IAlienRoot.SfcHelper_GetDataTable(object connection, string urn, string[] fields,
            Microsoft.SqlServer.Management.Sdk.Sfc.OrderBy[] orderByFields)
        {
            OrderBy[] smoOrderByFields = null;
            if (null != orderByFields)
            {
                smoOrderByFields = new OrderBy[orderByFields.Length];
                orderByFields.CopyTo(smoOrderByFields, 0);
            }
            return Enumerator.GetData(connection, new Urn(urn), fields, smoOrderByFields);
        }

        void IAlienRoot.DesignModeInitialize()
        {
            //ensure the server is disconnected.

            //For design mode, server version is required. we set the server version to be latest known version.
            FileVersionInfo latestVersion = FileVersionInfo.GetVersionInfo(this.GetType().GetAssembly().Location);
            this.ConnectionContext.ServerVersion = new ServerVersion(latestVersion.FileMajorPart, latestVersion.FileMinorPart);
            this.ConnectionContext.TrueName = "DesignMode";
            //sever connection.
            ((ISfcHasConnection)this).ConnectionContext.Mode = SfcConnectionContextMode.Offline;
        }
        /// <summary>
        /// Helper for SFC. Query and iterator/enumerator interfaces should be the level we abstract at, but for now
        /// we make sure caching via InitChildLevel is done while we still give back the list of Urns.
        /// </summary>
        /// <returns>The list of SMO Urns.</returns>
        List<string> IAlienRoot.SfcHelper_GetSmoObjectQuery(string urn, string[] fields, OrderBy[] orderByFields)
        {
            return GetSmoObjectQuery(new Urn(urn), fields, orderByFields);
        }

        /// <summary>
        /// Execute the given query and return the list of Urns matching it.
        /// The objects representing the Urns are all cached for subsequent access via the usual collections and GetSmoObject.
        /// </summary>
        /// <param name="queryString">The XPath query string to satisfy.</param>
        /// <param name="fields">The fields to ensure are present in all leaf-type objects matched, or null for default fields.</param>
        /// <param name="orderByFields">The fields to order the resulting list of Urns, or null for default ordering.</param>
        /// <returns>The list of SMO Urns satisfying the query.</returns>
        private List<string> GetSmoObjectQuery(string queryString, string[] fields, OrderBy[] orderByFields)
        {
            // Prime a few things that otherwise would cause a DataReader in use exception deeper in
            string dummyEdition = this.Information.Edition;

            Urn fullQueryUrn = new Urn(queryString);
            XPathExpression parsedUrn = fullQueryUrn.XPathExpression;
            int nodeCount = parsedUrn.Length;
            if (nodeCount < 1)
            {
                throw new InternalSmoErrorException(ExceptionTemplates.CallingInitChildLevelWithWrongUrn(fullQueryUrn));
            }

            // Do a top-down query though each level of the Urn, to cause ancestors to be cached and present at each level.
            GetSmoObjectQueryRec(new Urn(queryString));

            List<string> urnList = null;
            try
            {
                // See if any additional fields needed for the true leaf type query
                string[] infrastructureFields = null;
                switch (parsedUrn[parsedUrn.Length - 1].Name)
                {
                    default:
                        // No extra fields at this time to add, apart from the ones on the early passes in the recursive part
                        break;
                }
                urnList = InitQueryUrns(fullQueryUrn, fields, orderByFields, infrastructureFields);
            }
            catch (ArgumentException e)
            {
                throw new ArgumentException(ExceptionTemplates.UnsupportedObjectQueryUrn(queryString), e);
            }

            return urnList;
        }

        private void GetSmoObjectQueryRec(Urn urn)
        {
            urn = urn.Parent;
            if (urn == null || urn.Parent == null)
            {
                // Stop recursing if we are on the "Server" part, since it doesn't really mean anythign to query for just a server.
                return;
            }

            // Since these are intermediate top-down queries for caching purposes, don't pass any request info yet.
            GetSmoObjectQueryRec(urn);

            // From the top-down (left to right in the Urn), we need to perform a query to cache the ancestor infrastructure
            XPathExpression parsedUrn = urn.XPathExpression;

            // Add special fields we know get asked for for certain types when they are the leaf,
            // otherwise the queries are way too inefficient
            int nodeCount = parsedUrn.Length;
            Type t = GetChildType(parsedUrn[nodeCount - 1].Name,
                (nodeCount > 1) ? parsedUrn[nodeCount - 2].Name : this.GetType().Name);
            string[] fields = GetQueryTypeInfrastructureFields(t);

            InitQueryUrns(urn, fields, null, null);
        }

        /// <summary>
        /// Returns the object with the corresponding Urn
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public SqlSmoObject GetSmoObject(Urn urn)
        {
#if DEBUGTRACE
            Trace("Entering: Server.GetSmoObject( " + urn + ")");
#endif

            try
            {
                if (null == urn)
                {
                    throw new ArgumentNullException("urn");
                }

                if ("Default" == urn.Type && "Column" == urn.Parent.Type)
                {
                    Column c = GetSmoObjectRec(urn.Parent) as Column;
                    return c.GetDefaultConstraintBaseByName(urn.GetAttribute("Name"));
                }
                if ("DatabaseOption" == urn.Type)
                {
                    Database d = GetSmoObjectRec(urn.Parent) as Database;
                    return d.DatabaseOptions;
                }

                return GetSmoObjectRec(urn);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetSmoObject, this, e);
            }
        }


        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.SqlServer.Management.Smo.SmoException.#ctor(System.String)")]
        private SqlSmoObject GetSmoObjectRec(Urn urn)
        {
            // stop condition goes first
            // TODO: add code to handle urn's that do not contain server
            if (null == urn.Parent)
            {
                if (urn.Type == "Server")
                {
                    CheckValidUrnServerLevel(urn.XPathExpression[0]);
                    return this;
                }
                else
                {
                    throw new SmoException("For the moment we don't support Urn's that do not start with Server");
                }
            }

            // we are going down one level to get the parent object
            SqlSmoObject parentNode = GetSmoObjectRec(urn.Parent);

            // we'll try to get the child object here. This can fail if parent
            // does not have this node in the child collection
            string nodeType = urn.Type;

            // take care of the special case of objects that are not in a collection
            nodeType = String.Intern(nodeType);   // faster comparison in switch
            /*
            if( nodeType == ServiceBroker.UrnSuffix)
                return ( ((Database)parentNode).ServiceBroker );
            */
            if (nodeType == JobServer.UrnSuffix)
            {
                return ((Server)parentNode).JobServer;
            }

            if (nodeType == AlertSystem.UrnSuffix)
            {
                return ((JobServer)parentNode).AlertSystem;
            }

            if (nodeType == UserOptions.UrnSuffix)
            {
                return ((Server)parentNode).UserOptions;
            }

            if (nodeType == Information.UrnSuffix)
            {
                return ((Server)parentNode).Information;
            }

            if (nodeType == Settings.UrnSuffix)
            {
                return ((Server)parentNode).Settings;
            }

            if (nodeType == FullTextIndex.UrnSuffix)
            {
                return ((TableViewBase)parentNode).FullTextIndex;
            }

            if (nodeType == DefaultConstraint.UrnSuffix &&
                    parentNode is Microsoft.SqlServer.Management.Smo.Column)
            {
                return ((Column)parentNode).DefaultConstraint;
            }

            if (nodeType == DatabaseOptions.UrnSuffix)
            {
                return ((Database)parentNode).DatabaseOptions;
            }

            if (nodeType == SqlMail.UrnSuffix)
            {
                return ((Server)parentNode).Mail;
            }

            if (nodeType == SoapPayload.UrnSuffix)
            {
                return ((Endpoint)parentNode).Payload.Soap;
            }

            if (nodeType == DatabaseMirroringPayload.UrnSuffix)
            {
                return ((Endpoint)parentNode).Payload.DatabaseMirroring;
            }

            if (nodeType == ServiceBrokerPayload.UrnSuffix &&
                    parentNode is Microsoft.SqlServer.Management.Smo.Endpoint)
            {
                return ((Endpoint)parentNode).Payload.ServiceBroker;
            }

            if (nodeType == HttpProtocol.UrnSuffix)
            {
                return ((Endpoint)parentNode).Protocol.Http;
            }

            if (nodeType == TcpProtocol.UrnSuffix)
            {
                return ((Endpoint)parentNode).Protocol.Tcp;
            }

            if (nodeType == ServiceBroker.UrnSuffix)
            {
                return ((Database)parentNode).ServiceBroker;
            }

            if (nodeType == DatabaseEncryptionKey.UrnSuffix)
            {
                return ((Database)parentNode).DatabaseEncryptionKey;
            }

            if (nodeType == ServiceMasterKey.UrnSuffix &&
                    parentNode is Microsoft.SqlServer.Management.Smo.Server)
            {
                return ((Server)parentNode).ServiceMasterKey;
            }

            if (nodeType == MasterKey.UrnSuffix &&
                    parentNode is Microsoft.SqlServer.Management.Smo.Database)
            {
                return ((Database)parentNode).MasterKey;
            }

            if (nodeType == ResourceGovernor.UrnSuffix)
            {
                return ((Server)parentNode).ResourceGovernor;
            }

            if (nodeType == ServerProxyAccount.UrnSuffix)
            {
                return ((Server)parentNode).ProxyAccount;
            }

            if (nodeType == SmartAdmin.UrnSuffix)
            {
                return ((Server)parentNode).SmartAdmin;
            }

            if (nodeType == FullTextService.UrnSuffix)
            {
                return ((Server)parentNode).FullTextService;
            }

            if (nodeType == QueryStoreOptions.UrnSuffix)
            {
                return ((Database) parentNode).QueryStoreOptions;
            }

            // retrieve the child collection
            object childCollection = SqlSmoObject.GetChildCollection(parentNode,
                urn.XPathExpression,
                urn.XPathExpression.Length - 1);

            // transform the Urn filter into a key
            ObjectKeyBase childKey = ((AbstractCollectionBase)childCollection).CreateKeyFromUrn(urn);
            // get the child object from child collection
            var thisNode = ((ISmoInternalCollection)childCollection).GetObjectByKey(childKey);
            return null == thisNode
                ? throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist(GetTypeName(nodeType), childKey.ToString()))
                : thisNode;
        }

        /// <summary>
        /// Alter the metadata for the server, including dependent child objects such as Configuration, Information, and Settings.
        /// The Configuration class will not override value checking with this call.
        /// </summary>
        public void Alter()
        {
            this.overrideValueChecking = false;
            base.AlterImpl();
        }

        // the base class doesn't allow passing values from alter to ScriptAlter()
        // thus we have to use a shared variable between the two methods
        bool overrideValueChecking = false;
        /// <summary>
        /// Alter the metadata for the server, including dependent child objects such as Configuration, Information, and Settings.
        /// </summary>
        /// <param name="overrideValueChecking">Boolean property value that specifies whether the Configuration changes should be installed with "RECONFIGURE WITH OVERRRIDE"</param>
        public void Alter(bool overrideValueChecking)
        {
            this.overrideValueChecking = overrideValueChecking;
            base.AlterImpl();
        }

        internal override void ScriptAlter(StringCollection query, ScriptingPreferences sp)
        {
            //script configurations
            if (null != m_config)
            {
                m_config.ScriptAlter(query, sp, this.overrideValueChecking);
            }
            if (null != this.affinityInfo)
            {
                this.affinityInfo.Alter();
            }
            ScriptProperties(query, sp);
        }

        // the initial TextMode property value for TextObjects
        bool defaultTextMode = true;
        public bool DefaultTextMode
        {
            get { return defaultTextMode; }
            set { defaultTextMode = value; }
        }


        /// <summary>
        /// Detach a database
        /// </summary>
        public void DetachDatabase(string databaseName, bool updateStatistics)
        {
            if (null == databaseName)
            {
                throw new ArgumentNullException("databaseName");
            }

            try
            {
                DetachDatabaseWorker(databaseName, updateStatistics, false, false);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DetachDatabase, this, e);
            }
        }

        /// <summary>
        /// Detach a database
        /// </summary>
        public void DetachDatabase(string databaseName, bool updateStatistics, bool removeFulltextIndexFile)
        {
            ThrowIfBelowVersion90();

            if (null == databaseName)
            {
                throw new ArgumentNullException("databaseName");
            }

            try
            {
                

                DetachDatabaseWorker(databaseName, updateStatistics, true, removeFulltextIndexFile);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DetachDatabase, this, e);
            }
        }


        private void DetachDatabaseWorker(string name, bool updateStatistics, bool emitFT, bool dropFulltextIndexFile)
        {
            if (!Databases.Contains(name))
            {
                throw new MissingObjectException(ExceptionTemplates.ObjectDoesNotExist(ExceptionTemplates.Database, name));
            }

            StringCollection query = new StringCollection();

            query.Add("USE [master]"); //release this possible lock on the detaching db
            StringBuilder sbStatement = new StringBuilder();
            sbStatement.AppendFormat(SmoApplication.DefaultCulture, "EXEC master.dbo.sp_detach_db @dbname = N'{0}'", SqlString(name));
            if (updateStatistics)
            {
                sbStatement.Append(", @skipchecks = 'false'");
            }

            if (emitFT)
            {
                sbStatement.AppendFormat(SmoApplication.DefaultCulture,
                    ", @keepfulltextindexfile=N'{0}'", dropFulltextIndexFile ? "false" : "true");
            }
            query.Add(sbStatement.ToString());

            // detach database from the server
            this.ExecutionManager.ExecuteNonQuery(query);

            // remove the object from the collection
            Databases[name].MarkDroppedInternal();
            Databases.RemoveObject(new SimpleObjectKey(name));

            if (!this.ExecutionManager.Recording)
            {
                if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                {
                    // give back a 'fake' Urn - the database does not exist anymore
                    // we are consistent with Drop(), where we also return the Urn of the dropped object
                    // this is needed so clients can identify the object just from the
                    // context of the event args
                    Urn detachedUrn = new Urn(this.Urn + string.Format(SmoApplication.DefaultCulture, "/Database[@Name='{0}']", Urn.EscapeString(name)));
                    SmoApplication.eventsSingleton.CallDatabaseEvent(this, new DatabaseEventArgs(
                                                            detachedUrn, null, name, DatabaseEventType.Detach));
                }
            }
        }

        /// <summary>
        /// Worker function for various attach overloads
        /// </summary>
        /// <param name="name"></param>
        /// <param name="files"></param>
        /// <param name="owner"></param>
        /// <param name="attachOptions"></param>
        private void AttachDatabaseWorker(string name,
                                          StringCollection files,
                                          string owner,
                                          AttachOptions attachOptions)
        {
            try
            {
                StringCollection query = new StringCollection();

                if (files.Count < 1)
                {
                    throw new ArgumentException(ExceptionTemplates.TooFewFiles);
                }

                if (this.Databases.Contains(name))
                {
                    throw new SmoException(ExceptionTemplates.DatabaseAlreadyExists);
                }

                if (name.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.EmptyInputParam("name", "string"));
                }

                if (owner != null && owner.Length == 0)
                {
                    throw new ArgumentException(ExceptionTemplates.EmptyInputParam("owner", "string"));
                }


                StringBuilder statement = new StringBuilder();
                statement.AppendFormat(SmoApplication.DefaultCulture, "CREATE DATABASE [{0}] ON ", SqlBraket(name));

                for (int i = 0; i < files.Count; i++)
                {
                    string filename = files[i];
                    if (i != 0)
                    {
                        statement.Append(Globals.comma);
                    }

                    statement.Append(Globals.newline);
                    statement.Append(Globals.LParen);
                    statement.AppendFormat(SmoApplication.DefaultCulture, " FILENAME = N'{0}' ", SqlString(filename));
                    statement.Append(Globals.RParen);
                }

                statement.Append(Globals.newline);

                if (attachOptions == AttachOptions.RebuildLog)
                {
                    statement.Append(" FOR ATTACH_REBUILD_LOG");
                }
                else
                {
                    statement.Append(" FOR ATTACH");

                    switch (attachOptions)
                    {
                        case AttachOptions.EnableBroker:
                            statement.Append(" WITH ENABLE_BROKER");
                            break;

                        case AttachOptions.NewBroker:
                            statement.Append(" WITH NEW_BROKER");
                            break;

                        case AttachOptions.ErrorBrokerConversations:
                            statement.Append(" WITH ERROR_BROKER_CONVERSATIONS");
                            break;

                        case AttachOptions.None:
                            //
                            // do nothing.
                            //
                            break;

                        default:
                            throw new ArgumentException(ExceptionTemplates.UnknownEnumeration("AttachOptions"));
                    }
                }


                query.Add(Scripts.USEMASTER);
                query.Add(statement.ToString());

                if (null != owner)
                {
                    // try to change the owner of the database
                    string sysname = string.Empty;
                    string dbsidfieldname = string.Empty;

                    if (this.ServerVersion.Major <= 8)
                    {
                        sysname = "master.dbo.sysdatabases";
                        dbsidfieldname = "sid";
                    }
                    else
                    {
                        sysname = "master.sys.databases";
                        dbsidfieldname = "owner_sid";
                    }

                    query.Add(string.Format(SmoApplication.DefaultCulture, "if exists (select name from {0} sd where name = N'{1}' and SUSER_SNAME(sd.{2}) = SUSER_SNAME() ) " +
                        "EXEC [{3}].dbo.sp_changedbowner @loginame=N'{4}', @map=false",
                        sysname, SqlString(name), dbsidfieldname,
                        SqlBraket(name), SqlString(owner)));
                }

                this.ExecutionManager.ExecuteNonQuery(query);

                // add the new database to the collection
                Databases.InitializeChildObject(new SimpleObjectKey(name));

                if (!this.ExecutionManager.Recording)
                {
                    if (!SmoApplication.eventsSingleton.IsNullDatabaseEvent())
                    {
                        SmoApplication.eventsSingleton.CallDatabaseEvent(this, new DatabaseEventArgs(
                                                                Databases[name].Urn, Databases[name], name, DatabaseEventType.Attach));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.AttachDatabase, this, e);
            }
        }

        /// <summary>
        /// Attach Database
        /// </summary>
        /// <param name="name">database name</param>
        /// <param name="files">list of files to attach</param>
        /// <param name="owner">new owner name</param>
        public void AttachDatabase(string name, StringCollection files, string owner)
        {

            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == files)
            {
                throw new ArgumentNullException("files");
            }

            if (null == owner)
            {
                throw new ArgumentNullException("owner");
            }

            AttachDatabaseWorker(name, files, owner, AttachOptions.None);
        }

        /// <summary>
        /// Attach Database
        /// </summary>
        /// <param name="name">database name</param>
        /// <param name="files">file list</param>
        public void AttachDatabase(string name, StringCollection files)
        {
            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == files)
            {
                throw new ArgumentNullException("files");
            }

            AttachDatabaseWorker(name, files, null, AttachOptions.None);
        }

        /// <summary>
        /// Attach Database
        /// </summary>
        /// <param name="name">database name</param>
        /// <param name="files">file list</param>
        /// <param name="attachOptions">options used when attaching database</param>
        public void AttachDatabase(string name,
                                   StringCollection files,
                                   AttachOptions attachOptions)
        {
            ThrowIfBelowVersion90();

            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == files)
            {
                throw new ArgumentNullException("files");
            }

            AttachDatabaseWorker(name, files, null, attachOptions);
        }

        /// <summary>
        /// Attach Database
        /// </summary>
        /// <param name="name">database name</param>
        /// <param name="files">file list</param>
        /// <param name="owner">new owner name</param>
        /// <param name="attachOptions">options used when attaching database</param>
        public void AttachDatabase(string name,
                                   StringCollection files,
                                   string owner,
                                   AttachOptions attachOptions)
        {
            ThrowIfBelowVersion90();

            if (null == name)
            {
                throw new ArgumentNullException("name");
            }

            if (null == files)
            {
                throw new ArgumentNullException("files");
            }

            if (null == owner)
            {
                throw new ArgumentNullException("owner");
            }

            AttachDatabaseWorker(name, files, owner, attachOptions);
        }

        private void CheckValidUrnServerLevel(XPathExpressionBlock xb)
        {
            if (xb.Name != "Server")
            {
                throw new SmoException(ExceptionTemplates.ServerLevelMustBePresent);
            }
            if (null == xb.Filter)
            {
                return;
            }
            string name = xb.GetAttributeFromFilter("Name");

            //null if in capture mode and we didn't yet take the name
            String serverName = this.ExecutionManager.TrueServerName;

            if (null != name && null != serverName && 0 == string.Compare(name, Urn.EscapeString(serverName), StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            throw new SmoException(ExceptionTemplates.InvalidUrnServerLevel);
        }

        /// <summary>
        /// Compares two Urn's, taking into account the collations on the server
        /// </summary>
        /// <param name="urn1"></param>
        /// <param name="urn2"></param>
        /// <returns></returns>
        public int CompareUrn(Urn urn1, Urn urn2)
        {
            try
            {
                return CompareUrnWorker(urn1, urn2);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.CompareUrn, this, e);
            }
        }


        private int CompareUrnWorker(Urn urn1, Urn urn2)
        {
            if (null == urn1)
            {
                throw new ArgumentNullException("urn1");
            }

            if (null == urn2)
            {
                throw new ArgumentNullException("urn2");
            }

            // break the urn's into XPathExpression
            XPathExpression xp1 = urn1.XPathExpression;
            XPathExpression xp2 = urn2.XPathExpression;

            if (xp1.Length != xp2.Length)
            {
                return xp1.Length - xp2.Length;
            }

            // if the urn is actually an empty string
            if (xp1.Length == 0)
            {
                return -1;
            }

            // Dont check the validity of URN at server level while comparing
            // We should be able to compare urns of different servers too.

            // check if the urns are different at the first level itself
            if (xp1[0].Name != "Server" || xp2[0].Name != "Server")
            {
                throw new SmoException(ExceptionTemplates.ServerLevelMustBePresent);
            }
            string lvl1name1 = string.Empty;
            if (null == xp1[0].Filter)
            {
                lvl1name1 = Urn.EscapeString(this.ExecutionManager.TrueServerName);
            }
            else
            {
                lvl1name1 = xp1[0].GetAttributeFromFilter("Name");
            }
            string lvl1name2 = string.Empty;
            if (null == xp2[0].Filter)
            {
                lvl1name2 = Urn.EscapeString(this.ExecutionManager.TrueServerName);
            }
            else
            {
                lvl1name2 = xp2[0].GetAttributeFromFilter("Name");
            }

            int svrcomparision = string.Compare(lvl1name1, lvl1name2, StringComparison.OrdinalIgnoreCase);

            if (svrcomparision != 0)
            {
                return svrcomparision;
            }

            // if there is only one level and we got here, the urn's are equal
            if (xp1.Length == 1)
            {
                return 0;
            }

            // at this point we have to set the connection context for the
            // server

            // check the second level
            string lvl2name;
            if (xp1[1].Name == xp2[1].Name)
            {
                string lvl2name1 = xp1[1].GetAttributeFromFilter("Name");
                string lvl2name2 = xp2[1].GetAttributeFromFilter("Name");
                int res1 = -1;

                res1 = this.StringComparer.Compare(lvl2name1, lvl2name2);

                if (res1 != 0)
                {
                    return res1;
                }

                // keep the name of the object, we might need it if it's database
                lvl2name = lvl2name1;
            }
            else
            {
                return string.Compare(xp1[1].Name, xp2[1].Name);
            }

            // if there are only two levels and we got here, the urn's are equal
            if (xp1.Length == 2)
            {
                return 0;
            }

            // now choose the comparer for the next levels
            // if the second level is database, the comparer comes from the
            // database, otherwise it comes from the server
            // note that if the database does not exist, it still has a valid
            // comparer, which is taken form the server
            StringComparer comparer = this.StringComparer;
            if (xp1[1].Name == "Database" && (null != lvl2name) && (this.Databases[lvl2name] != null))
            {
                comparer = this.Databases[lvl2name].StringComparer;
            }

            // from now on we can treat all remaining levels in the same fashion
            string lvlname1;
            string lvlname2;
            for (int i = 2; i < xp1.Length; ++i)
            {
                // if type is the same
                if (xp1[i].Name == xp2[i].Name)
                {
                    // get object names
                    lvlname1 = xp1[i].GetAttributeFromFilter("Name");
                    lvlname2 = xp2[i].GetAttributeFromFilter("Name");

                    // check for the names to be identical
                    int res = comparer.Compare(lvlname1, lvlname2);
                    if (res != 0)
                    {
                        return res;
                    }
                    else
                    {
                        string schema1 = xp1[i].GetAttributeFromFilter("Schema");
                        string schema2 = xp2[i].GetAttributeFromFilter("Schema");

                        if (null != schema1 && null == schema2)
                        {
                            return comparer.Compare(schema1, string.Empty);
                        }

                        if (null == schema1 && null != schema2)
                        {
                            return comparer.Compare(string.Empty, schema2);
                        }

                        if (null != schema1 && null != schema2)
                        {
                            res = comparer.Compare(schema1, schema2);
                        }

                        if (res != 0)
                        {
                            return res;
                        }
                    }
                }
                else
                {
                    return string.Compare(xp1[i].Name, xp2[i].Name);
                }
            }

            // finally, if we get here it means the urns are equals
            return 0;
        }

        #region Child objects and collections
        Configuration m_config = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public Configuration Configuration
        {
            get
            {
                if (null == m_config)
                {
                    m_config = new Configuration(this);
                }
                return m_config;
            }
        }

        AffinityInfo affinityInfo = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public AffinityInfo AffinityInfo
        {
            get
            {
                if (null == affinityInfo)
                {
                    affinityInfo = new AffinityInfo(this);
                }
                return affinityInfo;
            }
        }
        ServerProxyAccount proxyAccount = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public ServerProxyAccount ProxyAccount
        {
            get
            {
                this.ThrowIfNotSupported(typeof(ServerProxyAccount));
                if (null == proxyAccount)
                {
                    proxyAccount = new ServerProxyAccount(this, new SimpleObjectKey(this.Name), SqlSmoState.Existing);
                }

                return proxyAccount;
            }
        }

        SqlMail mail = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public SqlMail Mail
        {
            get
            {
                ThrowIfExpressSku(ExceptionTemplates.UnsupportedFeatureSqlMail);
                this.ThrowIfNotSupported(typeof (SqlMail));
                if (null == mail)
                {
                    mail = new SqlMail(this, new SimpleObjectKey(this.Name), SqlSmoState.Existing);
                }

                return mail;
            }
        }

        DatabaseCollection m_Databases = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Database), SfcObjectFlags.Design)]
        public DatabaseCollection Databases
        {
            get
            {
                if (m_Databases == null)
                {
                    m_Databases = new DatabaseCollection(this);
                }

                return m_Databases;
            }
        }

        EndpointCollection m_Endpoints = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Endpoint))]
        public EndpointCollection Endpoints
        {
            get
            {
                if (m_Endpoints == null)
                {
                    m_Endpoints = new EndpointCollection(this);
                }

                return m_Endpoints;
            }
        }

        LanguageCollection m_Languages = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Language))]
        public LanguageCollection Languages
        {
            get
            {
                if (m_Languages == null)
                {
                    m_Languages = new LanguageCollection(this);
                }
                return m_Languages;
            }
        }

        SystemMessageCollection systemMessages = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SystemMessage))]
        public SystemMessageCollection SystemMessages
        {
            get
            {
                if (systemMessages == null)
                {
                    systemMessages = new SystemMessageCollection(this);
                }
                return systemMessages;
            }
        }

        UserDefinedMessageCollection userDefinedMessages = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(UserDefinedMessage))]
        public UserDefinedMessageCollection UserDefinedMessages
        {
            get
            {
                if (userDefinedMessages == null)
                {
                    userDefinedMessages = new UserDefinedMessageCollection(this);
                }
                return userDefinedMessages;
            }
        }

        CredentialCollection credentials;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Credential))]
        public CredentialCollection Credentials
        {
            get
            {
                if (credentials == null)
                {
                    credentials = new CredentialCollection(this);
                }
                return credentials;
            }
        }

        CryptographicProviderCollection cryptographicProviders;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(CryptographicProvider))]
        public CryptographicProviderCollection CryptographicProviders
        {
            get
            {
                this.ThrowIfNotSupported(typeof(CryptographicProvider));
                if (cryptographicProviders == null)
                {
                    cryptographicProviders = new CryptographicProviderCollection(this);
                }
                return cryptographicProviders;
            }
        }

        LoginCollection m_Logins = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Login))]
        public LoginCollection Logins
        {
            get
            {
                if (m_Logins == null)
                {
                    m_Logins = new LoginCollection(this);
                }
                return m_Logins;
            }
        }

        ServerRoleCollection m_Roles = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ServerRole))]
        public ServerRoleCollection Roles
        {
            get
            {
                if (m_Roles == null)
                {
                    m_Roles = new ServerRoleCollection(this);
                }
                return m_Roles;
            }
        }

        LinkedServerCollection m_LinkedServers = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(LinkedServer))]
        public LinkedServerCollection LinkedServers
        {
            get
            {
                this.ThrowIfNotSupported(typeof(LinkedServer));
                if (m_LinkedServers == null)
                {
                    m_LinkedServers = new LinkedServerCollection(this);
                }
                return m_LinkedServers;
            }
        }

        SystemDataTypeCollection systemDataTypes = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(SystemDataType))]
        public SystemDataTypeCollection SystemDataTypes
        {
            get
            {
                if (systemDataTypes == null)
                {
                    systemDataTypes = new SystemDataTypeCollection(this);
                }
                return systemDataTypes;
            }
        }

        JobServer jobServer = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public JobServer JobServer
        {
            get
            {
                ThrowIfExpressSku(ExceptionTemplates.UnsupportedFeatureSqlAgent);
                this.ThrowIfNotSupported(typeof(JobServer));
                if (null == jobServer)
                {
                    jobServer = new JobServer(this, new SimpleObjectKey(this.Name), SqlSmoState.Existing);
                }

                return jobServer;
            }
        }

        ResourceGovernor resourceGovernor = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public ResourceGovernor ResourceGovernor
        {
            get
            {
                ThrowIfExpressSku(ExceptionTemplates.UnsupportedFeatureResourceGovernor);
                this.ThrowIfNotSupported(typeof(ResourceGovernor));
                if (null == resourceGovernor)
                {
                    resourceGovernor = new ResourceGovernor(this, new ObjectKeyBase(), SqlSmoState.Existing);
                }

                return resourceGovernor;
            }
        }

        ServiceMasterKey masterKey = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public ServiceMasterKey ServiceMasterKey
        {
            get
            {
                this.ThrowIfNotSupported(typeof(ServiceMasterKey));
                if (null == masterKey)
                {
                    masterKey = new ServiceMasterKey(this, new ObjectKeyBase(), SqlSmoState.Existing);
                }

                return masterKey;
            }
        }

        SmartAdmin smartAdmin = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public SmartAdmin SmartAdmin
        {
            get
            {
                ThrowIfExpressSku(ExceptionTemplates.UnsupportedFeatureSmartAdmin);

                this.ThrowIfNotSupported(typeof(SmartAdmin));
                if (null == smartAdmin)
                {
                    smartAdmin = new SmartAdmin(this, new ObjectKeyBase(), SqlSmoState.Existing);
                }

                return smartAdmin;
            }
        }

        private Settings m_Settings = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public Settings Settings
        {
            get
            {
                if (null == m_Settings)
                {
                    m_Settings = new Settings(this, new ObjectKeyBase(), SqlSmoState.Existing);
                }

                return m_Settings;
            }
        }

        private Information m_Information = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public Information Information
        {
            get
            {
                if (null == m_Information)
                {
                    m_Information = new Information(this, new ObjectKeyBase(), SqlSmoState.Existing);
                }

                return m_Information;
            }
        }

        private UserOptions m_UserOption = null;
        [SfcObject(SfcObjectRelationship.ChildObject, SfcObjectCardinality.One)]
        public UserOptions UserOptions
        {
            get
            {
                this.ThrowIfNotSupported(typeof(UserOptions));

                if (null == m_UserOption)
                {
                    m_UserOption = new UserOptions(this, new ObjectKeyBase(), SqlSmoState.Existing);
                }

                return m_UserOption;
            }
        }

        BackupDeviceCollection m_BackupDevices = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(BackupDevice))]
        public BackupDeviceCollection BackupDevices
        {
            get
            {
                if (m_BackupDevices == null)
                {
                    m_BackupDevices = new BackupDeviceCollection(this);
                }
                return m_BackupDevices;
            }
        }


        FullTextService fullTextService = null;
        [SfcObject(SfcObjectRelationship.Object, SfcObjectCardinality.One)]
        public FullTextService FullTextService
        {
            get
            {
                this.ThrowIfNotSupported(typeof(FullTextService));
                // FullText feature is supported for Express editions also
                if (fullTextService == null)
                {
                    fullTextService = new FullTextService(this, new SimpleObjectKey(this.Name), SqlSmoState.Existing);
                }
                return fullTextService;
            }
        }

        ServerDdlTriggerCollection serverDdlTriggerCollection = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ServerDdlTrigger))]
        public ServerDdlTriggerCollection Triggers
        {
            get
            {
                this.ThrowIfNotSupported(typeof(ServerDdlTrigger));
                if (serverDdlTriggerCollection == null)
                {
                    serverDdlTriggerCollection = new ServerDdlTriggerCollection(this);
                }
                return serverDdlTriggerCollection;
            }
        }

        private AuditCollection auditCollection = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(Audit))]
        public AuditCollection Audits
        {
            get
            {
                this.ThrowIfNotSupported(typeof(Audit));
                if (auditCollection == null)
                {
                    auditCollection = new AuditCollection(this);
                }
                return auditCollection;
            }
        }

        private ServerAuditSpecificationCollection serverAuditSpecificationCollection = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(ServerAuditSpecification))]
        public ServerAuditSpecificationCollection ServerAuditSpecifications
        {
            get
            {
                this.ThrowIfNotSupported(typeof(ServerAuditSpecification));
                if (serverAuditSpecificationCollection == null)
                {
                    serverAuditSpecificationCollection = new ServerAuditSpecificationCollection(this);
                }
                return serverAuditSpecificationCollection;
            }
        }

        //HADR objects

        AvailabilityGroupCollection m_AvailabilityGroups = null;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(AvailabilityGroup))]
        public AvailabilityGroupCollection AvailabilityGroups
        {
            get
            {
                if (m_AvailabilityGroups == null)
                {
                    m_AvailabilityGroups = new AvailabilityGroupCollection(this);
                }

                return m_AvailabilityGroups;
            }
        }

        #endregion


        private DataTable collations = null;
        public DataTable EnumCollations()
        {
            try
            {
                // ordering by Name is essential in SqlSmoObject.CheckCollation()
                // so do not change
                if (null == collations)
                {
                    Request req = new Request(this.Urn.Value + "/Collation");
                    req.OrderByList = new OrderBy[] { new OrderBy("Name", OrderBy.Direction.Asc) };
                    collations = this.ExecutionManager.GetEnumeratorData(req);
                }
                return collations;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumCollations, this, e);
            }
        }

        Dictionary<string, CollationVersion> collationVersionDictionary;

        internal CollationVersion GetCollationVersion(string collationName)
        {
            if (this.collationVersionDictionary == null)
            {
                InitializeCollationVersionDictionary();
            }

            if (this.collationVersionDictionary.ContainsKey(collationName))
            {
                return this.collationVersionDictionary[collationName];
            }
            else
            {
                CollationVersion collationVersion = this.FindCollationVersion(collationName);
                this.collationVersionDictionary.Add(collationName, collationVersion);
                return collationVersion;
            }
        }

        private void InitializeCollationVersionDictionary()
        {
            this.collationVersionDictionary = new Dictionary<string, CollationVersion>(System.StringComparer.OrdinalIgnoreCase);
        }

        private CollationVersion FindCollationVersion(string collationName)
        {
            SqlExecutionModes originalvalue = this.ExecutionManager.ConnectionContext.SqlExecutionModes;
            DataTable collations;
            if (this.ExecutionManager.ConnectionContext.SqlExecutionModes == SqlExecutionModes.CaptureSql)
            {
                this.ExecutionManager.ConnectionContext.SqlExecutionModes = SqlExecutionModes.ExecuteSql;
            }
            try
            {
                /*  COLLTIONPROPERTY Function return version as null for shiloh so used lcid as a workaround,
                    version value  0 means shiloh supported collation
                    version value  1 means yukon  supported collation
                    version value  2 means katmai supported collation   */
                collations = this.ExecutionManager.ExecuteWithResults("SELECT COLLATIONPROPERTY('" + collationName + "', 'Version') as CollationVersion").Tables[0];
            }
            finally
            {
                this.ExecutionManager.ConnectionContext.SqlExecutionModes = originalvalue;
            }


            DataRow dr = collations.Rows[0];
            if (!string.IsNullOrEmpty(dr["CollationVersion"].ToString()))
            {
                int collationNum = int.Parse(dr["CollationVersion"].ToString(), SmoApplication.DefaultCulture);
                return (CollationVersion)Enum.ToObject(typeof(CollationVersion), collationNum);
            }
            else
            {
                // if we did not find the collation then we need to throw
                throw new WrongPropertyValueException(ExceptionTemplates.InvalidCollation(collationName));
            }
        }

        internal DataTable EnumPerfInfoInternal(string objectName, string counterName, string instanceName)
        {
            ThrowIfCloud();
            try
            {
                StringBuilder sb = new StringBuilder();
                bool bFilterAdded = false;
                if (null != objectName)
                {
                    sb.AppendFormat(CultureInfo.InvariantCulture, "@ObjectName = '{0}'", Urn.EscapeString(objectName));
                    bFilterAdded = true;
                }
                if (null != counterName)
                {
                    if (bFilterAdded)
                    {
                        sb.Append(" and ");
                        sb.AppendFormat(CultureInfo.InvariantCulture, "@CounterName = '{0}'", Urn.EscapeString(counterName));
                        bFilterAdded = true;
                    }
                }
                if (null != instanceName)
                {
                    if (bFilterAdded)
                    {
                        sb.Append(" and ");
                        sb.AppendFormat(CultureInfo.InvariantCulture, "@InstanceName = '{0}'", Urn.EscapeString(instanceName));
                        bFilterAdded = true;
                    }
                }
                StringBuilder sbUrn = new StringBuilder();
                sbUrn.Append(this.Urn.Value);
                sbUrn.Append("/PerfInfo");
                if (sb.Length > 0)
                {
                    sbUrn.Append("[");
                    sbUrn.Append(sb.ToString());
                    sbUrn.Append("]");
                }
                Request req = new Request(sbUrn.ToString());
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPerformanceCounters, this, e);
            }
        }

        /// <summary>
        /// Retrieves performance counter data from the server.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumPerformanceCounters()
        {
            try
            {
                return EnumPerfInfoInternal(null, null, null);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumPerformanceCounters, this, e);
            }

        }

        /// <summary>
        /// Retrieves performance counter data from the server.
        /// </summary>
        /// <param name="objectName">The name of the object for which to fetch counter data.</param>
        /// <returns></returns>
        /// <exception cref="FailedOperationException">If the remote operation fails</exception>
        /// <exception cref="ArgumentNullException">If objectName is null</exception>
        public DataTable EnumPerformanceCounters(string objectName)
        {
            if (null == objectName)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            return EnumPerfInfoInternal(objectName, null, null);
        }

        /// <summary>
        /// Retrieves performance counter data from the server.
        /// </summary>
        /// <param name="objectName">The name of the object for which to fetch counter data.</param>
        /// <param name="counterName">The name of the counter for which to fetch instance data.</param>
        /// <returns></returns>
        /// <exception cref="FailedOperationException">If the remote operation fails</exception>
        /// <exception cref="ArgumentNullException">If any parameter is null</exception>
        public DataTable EnumPerformanceCounters(string objectName, string counterName)
        {
            if (null == objectName)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (null == counterName)
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            return EnumPerfInfoInternal(objectName, counterName, null);            
        }

        /// <summary>
        /// Retrieves performance counter data from the server.
        /// </summary>
        /// <param name="objectName">The name of the object for which to fetch counter data.</param>
        /// <param name="counterName">The name of the counter for which to fetch instance data.</param>
        /// <param name="instanceName">The name of the counter instance to find.</param>
        /// <returns></returns>
        /// <exception cref="FailedOperationException">If the remote operation fails</exception>
        /// <exception cref="ArgumentNullException">If any parameter is null</exception>
        public DataTable EnumPerformanceCounters(string objectName, string counterName, string instanceName)
        {
            if (null == objectName)
            {
                throw new ArgumentNullException(nameof(objectName));
            }

            if (null == counterName)
            {
                throw new ArgumentNullException(nameof(counterName));
            }

            if (null == instanceName)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }

            return EnumPerfInfoInternal(objectName, counterName, instanceName);
        }

        /// <summary>
        /// Enumerates the witness roles the server plays in a database mirroring partnership
        /// </summary>
        /// <returns></returns>
        public DataTable EnumDatabaseMirrorWitnessRoles()
        {
            ThrowIfBelowVersion90();
            ThrowIfCloud();

            try
            {
                Request req = new Request(this.Urn.Value + "/DatabaseMirroringWitnessRole");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumDatabaseMirrorWitnessRoles, this, e);
            }
        }

        public DataTable EnumDatabaseMirrorWitnessRoles(string database)
        {
            ThrowIfBelowVersion90();
            ThrowIfCloud();

            if (null == database)
            {
                throw new ArgumentNullException("database");
            }

            try
            {
                Request req = new Request(this.Urn.Value + "/DatabaseMirroringWitnessRole[@Database='" +
                    Urn.EscapeString(database) + "']");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumDatabaseMirrorWitnessRoles, this, e);
            }
        }

        public DataTable EnumErrorLogs()
        {
            ThrowIfCloud();

            try
            {
                Request req = new Request(this.Urn.Value + "/ErrorLog");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumErrorLogs, this, e);
            }
        }

        public DataTable ReadErrorLog()
        {
            // 0 is always the current archive
            return ReadErrorLog(0);
        }

        public DataTable ReadErrorLog(int logNumber)
        {
            ThrowIfCloud();

            try
            {
                StringCollection log = new StringCollection();

                //build request for error log text
                Request req = new Request(string.Format(SmoApplication.DefaultCulture, "{0}/ErrorLog[@ArchiveNo='{1}']/LogEntry", this.Urn, logNumber));
                //specify fields
                req.Fields = new String[] { "LogDate", "ProcessInfo", "Text" };
                //must be ordered to have meaning
                req.OrderByList = new OrderBy[] { new OrderBy("LogDate", OrderBy.Direction.Asc) };

                //return info to client
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ReadErrorLog, this, e);
            }
        }

        /// <summary>
        /// Drops a database. If users are connected to it their connections will be dropped.
        /// </summary>
        /// <param name="database"></param>
        public void KillDatabase(string database)
        {
            ThrowIfCloud();

            try
            {
                if (null == database)
                {
                    throw new ArgumentNullException("database");
                }

                StringCollection query = new StringCollection();
                query.Add(Scripts.USEMASTER);

                query.Add(string.Format(SmoApplication.DefaultCulture, "ALTER DATABASE {0} SET SINGLE_USER WITH ROLLBACK IMMEDIATE", MakeSqlBraket(database)));

                try
                {
                    this.ExecutionManager.ExecuteNonQuery(query);
                }
                catch
                {
                    // If a database is not in Online state (e.g. Restoring) alter operation will be invalid, ignore the exception and try dropping it directly
                }

                this.Databases[database].Drop();
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.KillDatabase, this, e);
            }

        }

        public void KillProcess(int processId)
        {
            try
            {
                StringCollection query = new StringCollection();
                if (DatabaseEngineType != Cmn.DatabaseEngineType.SqlAzureDatabase)
                {
                    query.Add(Scripts.USEMASTER);
                }

                query.Add(string.Format(SmoApplication.DefaultCulture, "KILL {0}", processId));
                this.ExecutionManager.ExecuteNonQuery(query);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.KillProcess, this, e);
            }

        }

        public Int32 GetActiveDBConnectionCount(string dbName)
        {
            try
            {
                if (null == dbName)
                {
                    throw new ArgumentNullException("dbName");
                }

                string cmd = string.Format(SmoApplication.DefaultCulture, "select count(*) from {0}dbo.sysprocesses where dbid=db_id(N'{1}')", 
                    DatabaseEngineType == Cmn.DatabaseEngineType.SqlAzureDatabase ? string.Empty : "master.",
                    SqlString(dbName));
                return (Int32)this.ExecutionManager.ExecuteScalar(cmd);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.GetActiveDBConnectionCount, this, e);
            }
        }

        /// <summary>
        /// Kills all user processes that are using the given database.
        /// </summary>
        /// <param name="databaseName"></param>
        public void KillAllProcesses(string databaseName)
        {

            if (null == databaseName)
            {
                throw new ArgumentNullException("databaseName");
            }
            try
            {
                if (ConnectionContext.SqlExecutionModes == SqlExecutionModes.CaptureSql)
                {
                    return;
                }

                // enumerate all processes in the database
                DataTable spids = null;
                string spidColumn = null;
                if (ServerVersion.Major == 8)
                {
                    spids = ExecutionManager.ExecuteWithResults(string.Format(SmoApplication.DefaultCulture,
                        "SELECT DISTINCT req_spid FROM master.dbo.syslockinfo WHERE rsc_type = 2 AND rsc_dbid = db_id('{0}') AND req_spid > 50",
                        SqlString(databaseName))).Tables[0];
                    spidColumn = "req_spid";
                }
                else
                {
                    spids = ExecutionManager.ExecuteWithResults(string.Format(SmoApplication.DefaultCulture,
                    "SELECT DISTINCT dtl.request_session_id FROM sys.dm_tran_locks dtl left join sys.dm_exec_sessions des on dtl.request_session_id = des.session_id WHERE dtl.resource_type = 'DATABASE' AND dtl.resource_database_id = db_id(N'{0}') and dtl.request_session_id != @@spid",
                    SqlString(databaseName))).Tables[0];
                    spidColumn = "request_session_id";
                }

                Debug.Assert(null != spids, "null == spids");
                Debug.Assert(null != spidColumn, "null == spidColumn");

                var col = new StringCollection();
                if (DatabaseEngineType == DatabaseEngineType.Standalone)
                {
                    _ = col.Add(Scripts.USEMASTER);
                }

                // iterate through the process list, build the statement to kill
                // all user processes
                foreach (DataRow row in spids.Rows)
                {
                    var spid = Convert.ToInt32(row[spidColumn], SmoApplication.DefaultCulture);
                    _ = col.Add($"BEGIN TRY KILL {spid} END TRY BEGIN CATCH PRINT '{spid} is not active or could not be killed' END CATCH");
                }

                ExecutionManager.ExecuteNonQuery(col);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DropAllActiveDBConnections, this, e);
            }
        }

        public DataTable EnumDirectories(String path)
        {
            ThrowIfCloud();

            try
            {
                if (null == path)
                {
                    throw new ArgumentNullException("path");
                }

                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/File[@Path='{0}']", Urn.EscapeString(path)));
                req.Fields = new String[] { "Name", "IsFile" };

                DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

                ArrayList rowsToRemove = new ArrayList();
                foreach (DataRow row in dt.Rows)
                {
                    if (true == (bool)row[1])
                    {
                        rowsToRemove.Add(row);
                    }
                }
                foreach (DataRow row in rowsToRemove)
                {
                    row.Delete();
                }
                dt.Columns.Remove(dt.Columns[1]);
                dt.AcceptChanges();

                return dt;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumDirectories, this, e);
            }
        }

        /// <summary>
        /// Returns a table of active locks
        /// </summary>
        /// <returns></returns>
        public DataTable EnumLocks()
        {
            try
            {
                Request req = new Request(this.Urn.Value + "/Lock");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumLocks, this, e);
            }
        }

        /// <summary>
        /// Returns a table of active locks
        /// </summary>
        /// <param name="processId">The processId for which the table will be filtered</param>
        /// <returns></returns>
        public DataTable EnumLocks(int processId)
        {
            try
            {
                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/Lock[@RequestorSpid={0}]", processId));
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumLocks, this, e);
            }
        }

        public DataTable EnumWindowsDomainGroups()
        {
            ThrowIfCloud();
            try
            {
                Request req = new Request(this.Urn.Value + "/NTGroup");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsDomainGroups, this, e);
            }
        }

        public DataTable EnumWindowsDomainGroups(string domain)
        {
            ThrowIfCloud();
            try
            {
                if (null == domain)
                {
                    throw new ArgumentNullException("domain");
                }

                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/NTGroup[@Domain='{0}']", Urn.EscapeString(domain)));
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsDomainGroups, this, e);
            }

        }

        public DataTable EnumProcesses()
        {
            ThrowIfCloud();
            try
            {
                Request req = new Request(this.Urn.Value + "/Process");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumProcesses, this, e);
            }
        }

        public DataTable EnumProcesses(int processId)
        {
            ThrowIfCloud();
            try
            {
                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/Process[@Spid={0}]", processId));
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumProcesses, this, e);
            }
        }

        /// <summary>
        /// enumerate processes optionaly excluting the system processes
        /// </summary>
        /// <param name="excludeSystemProcesses"></param>
        /// <returns></returns>
        public DataTable EnumProcesses(bool excludeSystemProcesses)
        {
            ThrowIfCloud();
            try
            {
                string filter = string.Empty;
                if (true == excludeSystemProcesses)
                {
                    filter = "[@IsSystem = false()]";
                }

                Request req = new Request(this.Urn.Value + "/Process" + filter);
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumProcesses, this, e);
            }
        }


        public DataTable EnumProcesses(string loginName)
        {
            ThrowIfCloud();
            try
            {
                if (null == loginName)
                {
                    throw new ArgumentNullException("loginName");
                }

                Request req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/Process[@Login='{0}']", Urn.EscapeString(loginName)));
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumProcesses, this, e);
            }
        }

        public DataTable EnumStartupProcedures()
        {
            try
            {
                Request req = new Request(this.Urn.Value + "/Database[@Name='master']/StoredProcedure[@Startup = true()]");

                req.Fields = new String[] { "Name", "Schema" };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumStartupProcedures, this, e);
            }
        }

        /// <summary>
        /// Sets a session trace flag
        /// </summary>
        /// <param name="number"></param>
        /// <param name="isOn"></param>
        public void SetTraceFlag(int number, bool isOn)
        {
            if (isOn)
            {
                ExecutionManager.ExecuteNonQuery(string.Format("DBCC TRACEON ({0})", number));
            }
            else
            {
                ExecutionManager.ExecuteNonQuery(string.Format("DBCC TRACEOFF ({0})", number));
            }
        }

        /// <summary>
        /// Checks whether a trace flag is enabled on the server
        /// </summary>
        /// <param name="traceFlag">Trace flag number</param>
        /// <param name="isGlobalTraceFlag">A boolean value indicating whether the trace flag is global</param>
        /// <returns></returns>
        public bool IsTraceFlagOn(int traceFlag, bool isGlobalTraceFlag)
        {
            DataTable result = isGlobalTraceFlag ? EnumActiveGlobalTraceFlags() : EnumActiveCurrentSessionTraceFlags();

            return result.Rows.Cast<DataRow>().Any(row =>
            {
                var vTraceFlag = Convert.ToInt32(row[0]);
                var vIsEnabled = Convert.ToBoolean(row[1]);
                var vIsGlobalTraceFlag = Convert.ToBoolean(row[2]);
                var vIsSessionTraceFlag = Convert.ToBoolean(row[3]);

                return traceFlag == vTraceFlag && vIsEnabled && ((isGlobalTraceFlag && vIsGlobalTraceFlag) || vIsSessionTraceFlag);
            });
        }

        /// <summary>
        /// Enumerate all active global trace flags set for SQL Instance and return them as DataTable.
        /// Returns the output of DBCC TRACESTATUS (-1)
        /// </summary>
        /// <returns></returns>
        public DataTable EnumActiveGlobalTraceFlags()
        {
            DataSet ds = ExecutionManager.ExecuteWithResults("CREATE TABLE #tracestatus (TraceFlag INT, Status INT, Global INT, Session INT)\n" +
                "INSERT INTO #tracestatus EXEC ('DBCC TRACESTATUS (-1) WITH NO_INFOMSGS')\n" +
                "SELECT * FROM #tracestatus\n" +
                "DROP TABLE #tracestatus");

            return ds.Tables[0];
        }

        /// <summary>
        /// Returns output of DBCC TRACESTATUS()
        /// </summary>
        /// <returns></returns>
        public DataTable EnumActiveCurrentSessionTraceFlags()
        {
            ThrowIfBelowVersion90();
            DataSet ds = ExecutionManager.ExecuteWithResults("CREATE TABLE #tracestatus (TraceFlag INT, Status INT, Global INT, Session INT)\n" +
                "INSERT INTO #tracestatus EXEC ('DBCC TRACESTATUS () WITH NO_INFOMSGS')\n" +
                "SELECT * FROM #tracestatus\n" +
                "DROP TABLE #tracestatus");

            return ds.Tables[0];
        }

        internal DataTable EnumAccountInfo(string arguments, string filter)
        {
            ThrowIfCloud();
            if ("" != filter)
            {
                filter = "where " + filter;

            }
            return this.ExecutionManager.ExecuteWithResults(
                "create table #t1 ([account name] sysname, type nvarchar(10), privilege nvarchar(10), [mapped login name] sysname, [permission path] nvarchar(512))\n" +
                String.Format(SmoApplication.DefaultCulture, "insert into #t1 exec xp_logininfo {0}\n", arguments) +
                String.Format(SmoApplication.DefaultCulture, "select * from #t1 {0}\n", filter) +
                "drop table #t1").Tables[0];
        }

        /// <summary>
        /// Returns output of xp_logininfo filtered to users
        /// </summary>
        /// <returns></returns>
        public DataTable EnumWindowsUserInfo()
        {
            ThrowIfCloud();
            try
            {
                return EnumAccountInfo("", "type = 'user'");
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsUserInfo, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_logininfo filtered to users
        /// </summary>
        /// <param name="account">The name of the user for which to return data</param>
        /// <returns></returns>
        public DataTable EnumWindowsUserInfo(System.String account)
        {
            ThrowIfCloud();
            try
            {
                return EnumWindowsUserInfo(account, false);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsUserInfo, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_logininfo filtered to users
        /// </summary>
        /// <param name="account">The name of the user for which to return data</param>
        /// <param name="listPermissionPaths">Whether to list all the groups through which the account has permissions</param>
        /// <returns></returns>
        public DataTable EnumWindowsUserInfo(System.String account, System.Boolean listPermissionPaths)
        {
            ThrowIfCloud();
            if (null == account)
            {
                throw new ArgumentNullException("account");
            }
            try
            {
                if (listPermissionPaths)
                {
                    return EnumAccountInfo(String.Format(SmoApplication.DefaultCulture,
                                "N'{0}', N'all'", SqlString(account)), "");
                }
                return EnumAccountInfo(String.Format(SmoApplication.DefaultCulture,
                                "N'{0}'", SqlString(account)), "");
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsUserInfo, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_logininfo filtered to groups
        /// </summary>
        /// <returns></returns>
        public DataTable EnumWindowsGroupInfo()
        {
            ThrowIfCloud();
            try
            {
                return EnumAccountInfo("", "type = 'group'");
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsGroupInfo, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_logininfo filtered to groups
        /// </summary>
        /// <param name="group">The name of the group for which to return data</param>
        /// <returns></returns>
        public DataTable EnumWindowsGroupInfo(System.String group)
        {
            ThrowIfCloud();
            if (null == group)
            {
                throw new ArgumentNullException(nameof(group));
            }
            try
            {
                return EnumWindowsGroupInfo(group, false);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsGroupInfo, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_logininfo filtered to groups.
        /// </summary>
        /// <param name="group">The name of the group for which to return data</param>
        /// <param name="listMembers">Whether to return the list of members of the group. Default is false.</param>
        /// <returns></returns>
        public DataTable EnumWindowsGroupInfo(System.String group, System.Boolean listMembers)
        {
            ThrowIfCloud();
            if (null == group)
            {
                throw new ArgumentNullException(nameof(group));
            }
            try
            {
                if (listMembers)
                {
                    return EnumAccountInfo(String.Format(SmoApplication.DefaultCulture,
                                    "N'{0}', N'members'", SqlString(group)), "");
                }
                return EnumAccountInfo(String.Format(SmoApplication.DefaultCulture,
                                    "N'{0}'", SqlString(group)), "");
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumWindowsGroupInfo, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_availablemedia
        /// </summary>
        /// <returns></returns>
        public DataTable EnumAvailableMedia()
        {
            ThrowIfCloud();
            try
            {
                return EnumAvailableMedia(MediaTypes.All);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumAvailableMedia, this, e);
            }
        }

        /// <summary>
        /// Returns output of xp_availablemedia
        /// </summary>
        /// <param name="media">Specifies media types to which the data will be filtered. If media includes MediaTypes.SharedFixedDisk, only shared drives will be returned.</param>
        /// <returns></returns>
        public DataTable EnumAvailableMedia(MediaTypes media)
        {
            ThrowIfCloud();
            try
            {
                String strFilterMain = String.Empty;

                int nMed = (int)media;
                if (MediaTypes.SharedFixedDisk == (MediaTypes.SharedFixedDisk & media))
                {
                    strFilterMain = "@SharedDrive = true()";
                    nMed -= (int)MediaTypes.SharedFixedDisk;
                }
                if (0 != nMed)
                {
                    if (strFilterMain.Length > 0)
                    {
                        strFilterMain += " and ";
                    }
                    strFilterMain += string.Format(SmoApplication.DefaultCulture, "0 != BitWiseAnd(@MediaTypes, {0})", nMed);
                }

                Request req;
                if (strFilterMain.Length > 0)
                {
                    req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/AvailableMedia[{0}]", strFilterMain));
                }
                else
                {
                    req = new Request(this.Urn.Value + string.Format(SmoApplication.DefaultCulture, "/AvailableMedia"));
                }

                req.Fields = new String[] { "Name", "LowFree", "HighFree", "MediaTypes" };
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumAvailableMedia, this, e);
            }
        }

        /// <summary>
        /// Returns the content of sp_server_info
        /// </summary>
        /// <returns></returns>
        public DataTable EnumServerAttributes()
        {
            try
            {
                return this.ExecutionManager.ExecuteWithResults("EXEC sp_server_info").Tables[0];
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumServerAttributes, this, e);
            }
        }

        protected override void CleanObject()
        {
            base.CleanObject();
            if (null != m_config)
            {
                m_config.CleanObject();
            }
        }

        internal override PropagateInfo[] GetPropagateInfo(PropagateAction action)
        {
            var propagateInfo = new List<PropagateInfo>()
            {
                new PropagateInfo(Settings)
            };

            if (this.IsSupportedObject<UserOptions>())
            {
                propagateInfo.Add(new PropagateInfo(UserOptions));
            }

            return propagateInfo.ToArray();
        }

        /// <summary>
        ///  Contains the list of initialization fields for each SqlSmoObject type
        ///  The order of fields in the list must be maintained, as required fields are always first.
        /// </summary>
        private IDictionary<Type, IList<string>> TypeInitFields
        {
            get;
        } = new Dictionary<Type, IList<string>>();

        /// <summary>
        /// returns a clone of the default init fields. This function will be deprecated. Please use the overload function.
        /// </summary>
        /// <param name="typeObject"></param>
        /// <returns></returns>
        public StringCollection GetDefaultInitFields(Type typeObject)
        {
            return GetDefaultInitFields(typeObject, this.DatabaseEngineEdition);
        }

        /// <summary>
        /// returns a clone of the default init fields
        /// </summary>
        /// <param name="typeObject"></param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns></returns>
        public StringCollection GetDefaultInitFields(Type typeObject, DatabaseEngineEdition databaseEngineEdition)
        {
            // validate the type
            if (!typeObject.IsSubclassOf(typeof(SqlSmoObject)) || !typeObject.GetIsSealed())
            {
                throw new FailedOperationException(ExceptionTemplates.CannotSetDefInitFlds(typeObject.Name)).SetHelpContext("CannotSetDefInitFlds");
            }

            StringCollection fields = new StringCollection();
            string[] existingFields = GetDefaultInitFieldsInternal(typeObject, databaseEngineEdition);
            if (null != existingFields)
            {
                int start = 1;
                if (existingFields.Length > 1)
                {
                    start = (existingFields[0] == "Schema") ? 2 : 1;
                }

                for (int i = start; i < existingFields.Length; i++)
                {
                    fields.Add(existingFields[i]);
                }
            }

            return fields;
        }

        /// <summary>
        /// Set the default for the fields of the given object type. This function will be deprecated. Please use the overload function.
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="fields">List of the fields</param>
        public void SetDefaultInitFields(Type typeObject, StringCollection fields)
        {
            SetDefaultInitFields(typeObject, fields, this.DatabaseEngineEdition);
        }

        /// <summary>
        /// Set the default fields of the given object type
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="fields">List of the fields</param>
        /// <param name="databaseEngineEdition">This value is only used when the type of the object is Database. For child objects of Database, properties not supported for the edition can be passed here and will be ignored.</param>
        public void SetDefaultInitFields(Type typeObject, StringCollection fields, DatabaseEngineEdition databaseEngineEdition)
        {
            // validate input parameters
            if (null == typeObject)
            {
                throw new FailedOperationException(ExceptionTemplates.SetDefaultInitFields, this, new ArgumentNullException("typeObject"));
            }

            if (null == fields)
            {
                throw new FailedOperationException(ExceptionTemplates.SetDefaultInitFields, this, new ArgumentNullException("fields"));
            }

            // make sure the type can have default init fields
            if (!typeObject.IsSubclassOf(typeof(SqlSmoObject)) || !typeObject.GetIsSealed())
            {
                throw new FailedOperationException(ExceptionTemplates.CannotSetDefInitFlds(typeObject.Name)).SetHelpContext("CannotSetDefInitFlds");
            }

            var initFields = CreateInitFieldsColl(typeObject, databaseEngineEdition).ToList();
            initFields.AddRange(fields.Cast<string>().Where(f => !initFields.Contains(f)));
            TypeInitFields[typeObject] = initFields;
        }

        /// <summary>
        /// Set the default fields of the given object type. This function will be deprecated. Please use the overload function.
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="fields">List of the fields</param>
        public void SetDefaultInitFields(Type typeObject, params string[] fields)
        {
            SetDefaultInitFields(typeObject, this.DatabaseEngineEdition, fields);
        }

        /// <summary>
        /// Set the default fields of the given object type
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="fields">List of the fields</param>
        /// <param name="databaseEngineEdition">Database edition of the object. This is required a Database and it children can have different properties based on the databaseEngineEdition</param>
        public void SetDefaultInitFields(Type typeObject, DatabaseEngineEdition databaseEngineEdition, params string[] fields)
        {
            if (null == fields)
            {
                throw new FailedOperationException(ExceptionTemplates.SetDefaultInitFields, this, new ArgumentNullException("fields"));
            }

            var fieldColl = new StringCollection();
            fieldColl.AddRange(fields);
            SetDefaultInitFields(typeObject, fieldColl, databaseEngineEdition);
        }


        /// <summary>
        /// Get the Property field supported by given object type
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="databaseEngineEdition">Database edition of the object. This is required a Database and it children can have different properties based on the databaseEngineEdition</param>
        public StringCollection GetPropertyNames(Type typeObject, DatabaseEngineEdition databaseEngineEdition)
        {
            // validate input parameters
            if (null == typeObject)
            {
                throw new FailedOperationException(ExceptionTemplates.GetPropertyNames, this, new ArgumentNullException("typeObject"));
            }

            // make sure the type can have default init fields
            if (!typeObject.IsSubclassOf(typeof(SqlSmoObject)) || !typeObject.GetIsSealed())
            {
                throw new FailedOperationException(ExceptionTemplates.CannotSetDefInitFlds(typeObject.Name)).SetHelpContext("CannotSetDefInitFlds");
            }

            Type typeProps = typeObject.GetNestedType("PropertyMetadataProvider", BindingFlags.NonPublic);
            if (null == typeProps)
            {
                return null;
            }

            Type[] types = new Type[] {typeof(Common.ServerVersion),typeof(Common.DatabaseEngineType), typeof(Common.DatabaseEngineEdition) };

            ConstructorInfo constructorInfoObj = typeProps.GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                CallingConventions.HasThis, types, null);

            if (null == constructorInfoObj)
            {
                return null;
            }

            SqlPropertyMetadataProvider pmp = constructorInfoObj.Invoke(new Object[] { this.ServerVersion, this.DatabaseEngineType, databaseEngineEdition }) as SqlPropertyMetadataProvider;

            if (null == pmp)
            {
                return null;
            }

            StringCollection properties = new StringCollection();
            for (int i = 0; i < pmp.Count; i++)
            {
                string property = pmp.GetStaticMetadata(i).Name;
                if (databaseEngineEdition != DatabaseEngineEdition.SqlOnDemand || IsSupportedProperty(property))
                {
                    properties.Add(property);
                }
            }
            return properties;
        }


        /// <summary>
        /// Set the default fields of the given object type. This function will be deprecated. Please use the overload function.
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="allFields">Should set all fields or not</param>
        public void SetDefaultInitFields(System.Type typeObject, bool allFields)
        {
            SetDefaultInitFields(typeObject, allFields, this.DatabaseEngineEdition);
        }


        /// <summary>
        /// Set the default fields of the given object type
        /// </summary>
        /// <param name="typeObject">Type of the object</param>
        /// <param name="allFields">Should set all fields or not</param>
        /// <param name="databaseEngineEdition">Database edition of the object. This is required a Database and it children can have different properties based on the databaseEngineEdition</param>
        public void SetDefaultInitFields(System.Type typeObject, bool allFields, DatabaseEngineEdition databaseEngineEdition)
        {
            StringCollection fields = new StringCollection();
            if (allFields)
            {
                fields = GetPropertyNames(typeObject, databaseEngineEdition);
            }

            SetDefaultInitFields(typeObject, fields, databaseEngineEdition);
        }

        bool useAllFieldsForInit = false;

        /// <summary>
        /// Sets a value indicating whether objects in collections should be initialized with all properties and clears any prior settings
        /// from other overrides of this method.
        /// Specific properties to fetch for a given type can be chosen for initialization by using another override of this method.
        /// </summary>
        /// <param name="allFields">When true, collection initialization will fetch all properties of the objects in the collection.
        /// When false, only a minimal subset of properties will be fetched. 
        /// </param>
        public void SetDefaultInitFields(bool allFields)
        {
            // make sure we clean the hashtable containing properties
            // that have been set so far, either by the user or have been accumulating
            TypeInitFields.Clear();

            this.useAllFieldsForInit = allFields;
        }

        // validate if the input is in the list of init fields
        internal bool IsInitField(Type typeObject, string fieldName)
        {
            if (fieldName == "Schema" || fieldName == "Name")
            {
                return true;
            }

            var existingFields = TypeInitFields.ContainsKey(typeObject) ? TypeInitFields[typeObject] : null;
            return existingFields?.Contains(fieldName) ?? false;
        }

        internal string[] GetDefaultInitFieldsInternal(Type typeObject, DatabaseEngineEdition databaseEngineEdition)
        {
            // Has the existing fields already been inited
            var requiredFields = CreateInitFieldsColl(typeObject, databaseEngineEdition);
            IList<string> existingFields;
            if (!(TypeInitFields.TryGetValue(typeObject, out existingFields)))
            {
                var fields = CreateInitFieldsColl(typeObject, databaseEngineEdition).ToList();
                // Should we init with all fields
                if (useAllFieldsForInit)
                {
                    // If we are possibly connected to a logical master in Azure, combine DB and DW properties for "all"
                    if (DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabase)
                    {
                        var dbProperties = GetPropertyNames(typeObject, DatabaseEngineEdition.SqlDatabase).Cast<string>();
                        var dwProperties = GetPropertyNames(typeObject, DatabaseEngineEdition.SqlDataWarehouse).Cast<string>();
                        fields.AddRange(dbProperties.Union(dwProperties).Where(f => !fields.Contains(f)));
                    }
                    else
                    {
                        fields.AddRange(GetPropertyNames(typeObject, databaseEngineEdition).Cast<string>().Where(f => !fields.Contains(f)));
                    }
                    TypeInitFields[typeObject] = fields;
                }
                existingFields = fields;
            }
            // The caller may have used SetDefaultInitFields with properties not available in the current DatabaseEngineEdition.
            // Filter those out.
            // The order of the required fields must be preserved.
            // Note that some required fields like Name and Schema are not included in IsSupportedProperty
            return existingFields.Where(f => requiredFields.Contains(f) || IsSupportedProperty(typeObject, f, databaseEngineEdition)).ToArray();
        }

        private IEnumerable<string> CreateInitFieldsColl(Type typeObject, DatabaseEngineEdition databaseEngineEdition)
        {
            // add fields that are mandatory
            // we will strip those fields when the user is requesting them
            // but we need them at runtime when we initialize the object, and we'd
            // rather have them in the collection for performance consideration
            // Note that some common property names like ID are not defined in the base class
            // as they have to be codegen'd for the concrete classes.
            // Some fields don't have C# wrapper properties.
            
            if (typeObject.IsSubclassOf(typeof(ScriptSchemaObjectBase)))
            {
                yield return nameof(ScriptSchemaObjectBase.Schema);
                yield return nameof(ScriptSchemaObjectBase.Name);

                if (typeObject.IsSubclassOf(typeof(TableViewBase)))
                {
                    yield return nameof(Table.ID);
                }
            }
            else if (typeObject == typeof(NumberedStoredProcedure))
            {
                yield return nameof(NumberedStoredProcedure.Number);
            }
            else if (SqlSmoObject.IsOrderedByID(typeObject))
            {
                yield return "ID";
                yield return "Name";
            }
            else if (typeObject.IsSubclassOf(typeof(MessageObjectBase)))
            {
                yield return "ID";
                yield return "Language";
            }
            else if (typeObject.IsSubclassOf(typeof(SoapMethodObject)))
            {
                yield return "Namespace";
                yield return "Name";
            }
            else if (typeObject.IsSubclassOf(typeof(ScheduleBase)))
            {
                yield return "Name";
                yield return "ID";
            }
            else if (typeObject == typeof(Job))
            {
                yield return nameof(Job.Name);
                yield return nameof(Job.CategoryID);
                yield return nameof(Job.JobID);
            }
            else if (typeObject == typeof(Database))
            {
                yield return nameof(Database.Name);
                // The DatabaseEngineEdition value of a Fabric database will still be SqlDatabase but
                // apps frequently need to distinguish Fabric instances from non-Fabric instances so make it relatively
                // cheap to determine.
                yield return nameof(Database.IsFabricDatabase);
                // If we are connected to an OnDemand or DataWarehouse instance through the ServerConnection
                // the Database can only be the same edition as the ServerConnection.
                // If we are connected to logical master then Database can have a different edition, so
                // add RealEngineEdition to the query so we get the edition from sys.database_service_objectives
                if (DatabaseEngineEdition == DatabaseEngineEdition.SqlDatabase) {
                    yield return "RealEngineEdition";
                }
                // Fetch collation properties because they are used frequently to compare collection members
                if (IsSupportedProperty(typeof(Database), nameof(Database.CatalogCollation), databaseEngineEdition))
                {
                    yield return nameof(Database.CatalogCollation);
                }
                yield return nameof(Database.Collation);
            }
            else if (typeObject.IsSubclassOf(typeof(NamedSmoObject)))
            {
                yield return nameof(NamedSmoObject.Name);
            }
            else if (typeObject == typeof(PhysicalPartition))
            {
                yield return nameof(PhysicalPartition.PartitionNumber);
            }
            else if (typeObject == typeof(DatabaseReplicaState))
            {
                yield return nameof(DatabaseReplicaState.AvailabilityReplicaServerName);
                yield return nameof(DatabaseReplicaState.AvailabilityDatabaseName);
            }
            else if (typeObject == typeof(AvailabilityGroupListenerIPAddress))
            {
                yield return nameof(AvailabilityGroupListenerIPAddress.IPAddress);
                yield return nameof(AvailabilityGroupListenerIPAddress.SubnetMask);
                yield return nameof(AvailabilityGroupListenerIPAddress.SubnetIP);
            }
            else if (typeObject == typeof(SecurityPredicate))
            {
                yield return nameof(SecurityPredicate.SecurityPredicateID);
            }
            else if (typeObject == typeof(ColumnEncryptionKeyValue))
            {
                yield return nameof(ColumnEncryptionKeyValue.ColumnMasterKeyID);
            }
            if (typeObject == typeof(Column))
            {
                yield return nameof(Column.DefaultConstraintName);
            }
            if (typeObject == typeof (DefaultConstraint))
            {
                yield return nameof(DefaultConstraint.IsFileTableDefined);
            }
            if (typeObject == typeof(Information))
            {
                yield return nameof(Information.Edition);
            }
            if (typeObject == typeof(DataType))
            {
                yield return "DataType";
                yield return "SystemType";
                yield return "Length";
                yield return nameof(DataType.NumericPrecision);
                yield return nameof(DataType.NumericScale);
                yield return "XmlSchemaNamespace";
                yield return "XmlSchemaNamespaceSchema";
                yield return nameof(DataType.XmlDocumentConstraint);
                yield return "DataTypeSchema";
            }
            if (typeObject == typeof(IndexedJsonPath))
            {
                yield return nameof(IndexedJsonPath.Path);
            }
        }

        private Hashtable objectMetadataHash = new Hashtable();


        // add the mandatory fields to the list produced by GetScriptInitFieldsInternal2
        internal string[] GetScriptInitFieldsInternal(Type childType, Type parentType, ScriptingPreferences sp, DatabaseEngineEdition databaseEngineEdition)
        {
            string[] res2 = GetScriptInitFieldsInternal2(childType, parentType, sp, databaseEngineEdition);
            return AddNecessaryFields(childType, res2);
        }

        /// <summary>
        /// Put types in this list if they don't have Name/Schema/ID properties
        /// </summary>
        private static readonly HashSet<string> typesToIgnore = new HashSet<string>
        {
            nameof(NumberedStoredProcedure),
            nameof(QueryStoreOptions),
            nameof(ColumnEncryptionKeyValue),
        };
        private static string[] AddNecessaryFields(Type childType, string[] res2)
        {
            if (typesToIgnore.Contains(childType.Name))
            {
                return res2;
            }
            string[] results;
            bool isObjectWithSchema = childType.IsSubclassOf(typeof(ScriptSchemaObjectBase));
            bool isObjectWithID = IsOrderedByID(childType);
            int iOffset = (isObjectWithSchema || isObjectWithID) ? 2 : 1;
            results = new string[res2.Length + iOffset];
            if (iOffset == 2)
            {
                results[0] = isObjectWithSchema ? "Schema" : "ID";
            }
            results[iOffset - 1] = "Name";
            res2.CopyTo(results, iOffset);

            return results;
        }

        // this is a list of fields that are necessary for scripting for every object type
        internal string[] GetScriptInitFieldsInternal2(Type childType, Type parentType, ScriptingPreferences sp, DatabaseEngineEdition databaseEngineEdition)
        {
            MethodInfo mi = childType.GetMethod("GetScriptFields",
                BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            if (mi != null)
            {
                return mi.Invoke(null, new object[] { parentType, this.ServerVersion, this.DatabaseEngineType, databaseEngineEdition, this.DefaultTextMode && (!sp.OldOptions.EnforceScriptingPreferences) }) as string[];
            }
            Debug.Assert(null != mi, childType.Name + " is missing GetScriptFields method!");

            return new string[] { };
        }

        // add the mandatory fields to the list produced by GetScriptInitFieldsInternal2
        internal bool GetScriptInitExpensiveFieldsInternal(Type childType, Type parentType, ScriptingPreferences sp,out string[] fields, DatabaseEngineEdition databaseEngineEdition)
        {
            string []res2 = GetScriptInitExpensiveFieldsInternal2(childType, parentType, sp, databaseEngineEdition);
            if (res2.Length == 0)
            {
                fields = res2;
                return false;
            }
            else
            {
                fields = AddNecessaryFields(childType, res2);
                return true;
            }
        }

        private string[] GetScriptInitExpensiveFieldsInternal2(Type childType, Type parentType, ScriptingPreferences sp, DatabaseEngineEdition databaseEngineEdition)
        {
            MethodInfo mi = childType.GetMethod("GetScriptFields2",
                BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.NonPublic);
            if (mi != null)
            {
                return mi.Invoke(null, new object[] { parentType, this.ServerVersion, this.DatabaseEngineType, databaseEngineEdition, this.DefaultTextMode && (!sp.OldOptions.EnforceScriptingPreferences), sp }) as string[];
            }
            return new string[] { };
        }

        /// <summary>
        /// connection context
        /// </summary>
        public ServerConnection ConnectionContext
        {
            get
            {
                return this.serverConnection;
            }
        }

        private object syncRoot = new Object();
        internal ExecutionManager GetExecutionManager()
        {
            if (m_ExecutionManager == null)
            {
                lock (this.syncRoot)
                {
                    if (m_ExecutionManager == null)
                    {
                        m_ExecutionManager = new ExecutionManager(this.serverConnection) {Parent = this};
                    }
                }
            }

            return m_ExecutionManager;
        }

        /// <summary>
        /// Deletes the entries in the backup and restore history tables for backup sets
        /// older than oldestDate
        /// </summary>
        /// <param name="oldestDate"></param>
        public void DeleteBackupHistory(System.DateTime oldestDate)
        {
            ThrowIfCloud();
            try
            {
                this.ExecutionManager.ExecuteNonQuery(string.Format(SmoApplication.DefaultCulture, "declare @dt datetime select @dt = cast(N'{0}' as datetime) exec msdb.dbo.sp_delete_backuphistory @dt",
                                                                     SqlSmoObject.SqlDateString(oldestDate)));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DeleteBackupHistory, this, e);
            }
        }

        /// <summary>
        /// The DetachedDBInfo method returns information about a detached database.
        /// </summary>
        /// <returns></returns>
        public DataTable DetachedDatabaseInfo(string mdfName)
        {
            ThrowIfCloud();

            if (null == mdfName)
            {
                throw new ArgumentNullException("mdfName");
            }

            try
            {
                CheckObjectState();
                return this.ExecutionManager.GetEnumeratorData(new Request("Server/PrimaryFile[@Name='" + Urn.EscapeString(mdfName) + "']", new String[] { "Property", "Value" }));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DetachedDatabaseInfo, this, e);
            }
        }

        /// <summary>
        /// Lists all database files referenced by a primary database file.
        /// </summary>
        /// <returns></returns>
        public StringCollection EnumDetachedDatabaseFiles(string mdfName)
        {
            ThrowIfCloud();

            if (null == mdfName)
            {
                throw new ArgumentNullException("mdfName");
            }

            try
            {
                CheckObjectState();

                DataTable dt = this.ExecutionManager.GetEnumeratorData(new Request("Server/PrimaryFile[@Name='" + Urn.EscapeString(mdfName) + "']/File[@IsFile=true()]", new String[] { "FileName" }));

                StringCollection sc = new StringCollection();

                foreach (DataRow row in dt.Rows)
                {
                    sc.Add((string)row["FileName"]);
                }

                return sc;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumDetachedDatabaseFiles, this, e);
            }
        }

        /// <summary>
        /// The ListDetachedLogFiles method lists all log files referenced by primary log file.
        /// </summary>
        /// <returns></returns>
        public StringCollection EnumDetachedLogFiles(string mdfName)
        {
            ThrowIfCloud();

            if (null == mdfName)
            {
                throw new ArgumentNullException("mdfName");
            }

            try
            {
                CheckObjectState();

                DataTable dt = this.ExecutionManager.GetEnumeratorData(new Request("Server/PrimaryFile[@Name='" + Urn.EscapeString(mdfName) + "']/File[@IsFile=false()]", new String[] { "FileName" }));
                StringCollection sc = new StringCollection();

                foreach (DataRow row in dt.Rows)
                {
                    sc.Add((string)row["FileName"]);
                }

                return sc;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumDetachedLogFiles, this, e);
            }
        }

        /// <summary>
        /// The IsDetachedPrimaryFile method specifies whether a file is a detached primary database file.
        /// </summary>
        /// <param name="mdfName"></param>
        /// <returns></returns>
        public bool IsDetachedPrimaryFile(string mdfName)
        {
            ThrowIfCloud();

            if (null == mdfName)
            {
                throw new ArgumentNullException("mdfName");
            }

            try
            {
                CheckObjectState();
                return 1 == ((int)this.ExecutionManager.ExecuteScalar("dbcc checkprimaryfile (" + SqlSmoObject.MakeSqlString(mdfName) + ", 0)"));
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.IsDetachedPrimaryFile, this, e);
            }
        }

        /// <summary>
        /// The IsNTGroupMember method exposes an instance of Microsoft� SQL Server� 2000 access rights for Windows NT� 4.0 or Microsoft Windows 2000 user accounts.
        /// Cloud for now doesn't support windows logins. And Sql logins can't be member of any windows group. Hence this method is not supported for Cloud.
        /// </summary>
        /// <returns></returns>
        public bool IsWindowsGroupMember(string windowsGroup, string windowsUser)
        {
            ThrowIfCloud();

            if (null == windowsGroup)
            {
                throw new ArgumentNullException("windowsGroup");
            }

            if (null == windowsUser)
            {
                throw new ArgumentNullException("windowsUser");
            }

            try
            {
                CheckObjectState();

                DataTable dt = EnumWindowsGroupInfo(windowsGroup, true);

                foreach (DataRow row in dt.Rows)
                {
                    // Account name is in the first column
                    // Need to do case-insensitive compare, since NT security is not case sensitive
                    if (0 == String.Compare(windowsUser, row[0].ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Found a match, so the windowsUser is a member of the specified NTGroup
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.IsWindowsGroupMember, this, e);
            }
        }

        /// <summary>
        /// Returns a list of server or database roles in which the currently connected login is a member.
        /// </summary>
        /// <param name="roleType">The type of role being queried. </param>
        /// <returns></returns>
        /// <remarks>RoleTypes.Server has no meaning for Azure SQL Database connections and will be ignored in that case.</remarks>
        public StringCollection EnumMembers(RoleTypes roleType)
        {
            try
            {
                CheckObjectState();
                StringCollection sc = new StringCollection();

                if( (RoleTypes.Server & roleType) == RoleTypes.Server
                    && (DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
                    )
                {
                    Request req = new Request("Server/Role/Member[@Name='" + Urn.EscapeString(this.ConnectionContext.TrueLogin) + "']",
                        new string[] { });
                    req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest(new string[] { "Name" }) };
                    DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

                    foreach (DataRow row in dt.Rows)
                    {
                        if (!sc.Contains(row[0].ToString()))
                        {
                            sc.Add(row[0].ToString());
                        }
                    }
                }
                if ((RoleTypes.Database & roleType) == RoleTypes.Database)
                {
                    DataTable dtInfo = this.ExecutionManager.ExecuteWithResults("select user_name(), db_name()").Tables[0];
                    if (0 < dtInfo.Rows.Count)
                    {
                        string userName = dtInfo.Rows[0][0].ToString();
                        string dbName = dtInfo.Rows[0][1].ToString();
                        Request req = new Request("Server/Database[@Name='" + Urn.EscapeString(dbName) + "']/Role/Member[@Name='" + Urn.EscapeString(userName) + "']",
                            new string[] { });
                        req.ParentPropertiesRequests = new PropertiesRequest[] { new PropertiesRequest(new string[] { "Name" }) };
                        DataTable dt = this.ExecutionManager.GetEnumeratorData(req);

                        foreach (DataRow row in dt.Rows)
                        {
                            if (!sc.Contains(row[0].ToString()))
                            {
                                sc.Add(row[0].ToString());
                            }
                        }
                    }
                }
                return sc;
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.ServerEnumMembers, this, e);
            }
        }

        /// <summary>
        /// will connect to the specified serve, retrieve the version string, and disconnect before return to the caller
        /// This allows caller to ping the SQLServer version
        /// Expensive because we need to make a connection to get the info, call only when necessary
        /// </summary>
        /// <returns></returns>
        public ServerVersion PingSqlServerVersion(string serverName, string login, string password)
        {
            if (null == serverName)
            {
                throw new ArgumentNullException("serverName");
            }

            if (null == login)
            {
                throw new ArgumentNullException("login");
            }

            if (null == password)
            {
                throw new ArgumentNullException("password");
            }

            try
            {
                CheckObjectState();
                return new ServerConnection(serverName, login, password).ServerVersion;
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.PingSqlServerVersion, this, e);
            }
        }

        /// <summary>
        /// will connect to the specified serve, retrieve the version string, and disconnect before return to the caller
        /// This allows caller to ping the SQLServer version
        /// Expensive because we need to make a connection to get the info, call only when necessary
        /// </summary>
        /// <returns></returns>
        public ServerVersion PingSqlServerVersion(string serverName)
        {
            if (null == serverName)
            {
                throw new ArgumentNullException("serverName");
            }

            try
            {
                CheckObjectState();
                return new ServerConnection(serverName).ServerVersion;
            }
            catch (Exception e)
            {
                FilterException(e);
                throw new FailedOperationException(ExceptionTemplates.PingSqlServerVersion, this, e);
            }
        }

        /// <summary>
        /// Deletes the entries in the backup and restore history tables for backup sets
        /// on mediaSetID
        /// </summary>
        public void DeleteBackupHistory(System.Int32 mediaSetId)
        {
            //Not supported for Cloud
            ThrowIfCloud();
            try
            {
                StringBuilder mediaSetStmt = new StringBuilder(Globals.INIT_BUFFER_SIZE);
                mediaSetStmt.Append("begin transaction");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("declare @id as int");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.AppendFormat(SmoApplication.DefaultCulture, "select @id = {0}", mediaSetId);
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.restorefilegroup");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("where restore_history_id in");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("(select restore_history_id from msdb.dbo.restorehistory");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("where backup_set_id in (select backup_set_id from msdb.dbo.backupset where media_set_id = @id))");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("if @@error <> 0 goto error");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.restorefile");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("where restore_history_id in");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("(select restore_history_id from msdb.dbo.restorehistory");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("where backup_set_id in (select backup_set_id from msdb.dbo.backupset where media_set_id = @id))");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("if @@error <> 0 goto error");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.restorehistory where backup_set_id in (select backup_set_id from msdb.dbo.backupset where media_set_id = @id)");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("if @@error <> 0 goto error");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.backupmediafamily where media_set_id = @id");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("if @@error <> 0 goto error");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.backupfilegroup where backup_set_id in (select backup_set_id from msdb.dbo.backupset where media_set_id = @id)");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.backupfile where backup_set_id in (select backup_set_id from msdb.dbo.backupset where media_set_id = @id)");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("if @@error <> 0 goto error");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("delete from msdb.dbo.backupset where media_set_id = @id");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("if @@error <> 0 goto error");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("commit transaction");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("goto end_of_batch");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("error:");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("rollback transaction");
                mediaSetStmt.Append(Globals.newline);
                mediaSetStmt.Append("end_of_batch:");
                mediaSetStmt.Append(Globals.newline);

                this.ExecutionManager.ExecuteNonQuery(mediaSetStmt.ToString());
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DeleteBackupHistory, this, e);
            }
        }

        /// <summary>
        /// Deletes the entries in the backup and restore history tables for database
        /// </summary>
        /// <param name="database"></param>
        public void DeleteBackupHistory(System.String database)
        {
            //Version information:
            //70: action not possible
            //80: msdb.dbo.sp_delete_database_backuphistory @db_nm nvarchar(256)
            //90: msdb.dbo.sp_delete_database_backuphistory @database_name sysname

            //Not supported for Cloud
            ThrowIfCloud();

            try
            {
                //check param
                if (null == database)
                {
                    throw new ArgumentNullException("database");
                }

                //supported starting with version 8.0
                ThrowIfBelowVersion80();

                //
                // build the script
                //

                StringBuilder script = new StringBuilder("EXEC msdb.dbo.sp_delete_database_backuphistory ");

                //add parameter name based on version
                if (8 == this.ServerVersion.Major)
                {
                    script.Append("@db_nm = ");
                }
                else //Yukon or bigger
                {
                    script.Append("@database_name = ");
                }

                //add the database name
                script.Append(MakeSqlString(database));

                //execute the script
                this.ExecutionManager.ExecuteNonQuery(script.ToString());
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.DeleteBackupHistory, this, e);
            }
        }

        public override void Refresh()
        {
            base.Refresh();
            this.Settings.Refresh();
            if(this.IsSupportedObject<UserOptions>())
            {
                this.UserOptions.Refresh();
            }
            this.Information.Refresh();
            this.Configuration.Refresh();
            if (affinityInfo != null)
            {
                this.AffinityInfo.Refresh();
            }

            //Specifically not supported on express sku (IsSupportedObject only checks supportability of server version and type)
            if (this.IsSupportedObject<ResourceGovernor>() && IsExpressSku() == false)
            {
                this.ResourceGovernor.Refresh();
            }

            //Specifically not supported on express sku (IsSupportedObject only checks supportability of server version and type)
            if (this.IsSupportedObject<SmartAdmin>() && IsExpressSku() == false)
            {
                this.SmartAdmin.Refresh();
            }

            //clean up the cached collation LCIDs
            collationCache = null;
        }

        /// <summary>
        /// ManagementScope used to manage events. We can't declare the type
        /// here since we can't take a dependency on System.Management
        /// </summary>
        //internal object managementScope = null;
        private ServerEvents events;
        public ServerEvents Events
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            get
            {
                if (SqlContext.IsAvailable)
                {
                    throw new SmoException(ExceptionTemplates.SmoSQLCLRUnAvailable);
                }

                if (null == this.events)
                {
                    this.events = new ServerEvents((Server)this);
                }
                return this.events;
            }
        }

        /// <summary>
        /// Override of the Urn calculation that does not add a filter
        /// if we ask to use ID as a key for the object
        /// </summary>
        /// <param name="urnbuilder"></param>
        /// <param name="idOption"></param>
        protected override void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            // Server class is irregular in that its Urn never has an ID. So when asked to produce
            // Urn with ID, we put Name into the Urn; when asked for Urn with *only* ID, put nothing.
            switch (idOption)
            {
                case UrnIdOption.NoId:
                case UrnIdOption.WithId:
                    base.GetUrnRecursive(urnbuilder, UrnIdOption.NoId);
                    break;
                default:
                    urnbuilder.Append(UrnSuffix);
                    break;
            }
        }

        public StringCollection Script()
        {
            return ScriptImpl();
        }

        // Script object with specific scripting optiions
        public StringCollection Script(ScriptingOptions scriptingOptions)
        {
            return ScriptImpl(scriptingOptions);
        }

        internal override void ScriptCreate(StringCollection query, ScriptingPreferences sp)
        {
            //We don't need to script registry entries for cloud.
            //Presently ScriptProperties only script Registry entries.
            if (DatabaseEngineType.SqlAzureDatabase != this.DatabaseEngineType)
            {
                ScriptProperties(query, sp);
            }
        }

        // keep these sorted
        internal static IEnumerable RegistryProperties(DatabaseEngineType engineType)
        {
            yield return new string[] { "AuditLevel", "AuditLevel", "REG_DWORD" };
            yield return new string[] { "BackupDirectory", "BackupDirectory", "REG_SZ" };
            yield return new string[] { "DefaultFile", "DefaultData", "REG_SZ" };
            yield return new string[] { "DefaultLog", "DefaultLog", "REG_SZ" };
            yield return new string[] { "ErrorLogSizeKb", "ErrorLogSizeInKb", "REG_DWORD" };
            yield return new string[] { "LoginMode", "LoginMode", "REG_DWORD" };
            yield return new string[] { "MailProfile", "MailAccountName", "REG_SZ" };
            yield return new string[] { "NumberOfLogFiles", "NumErrorLogs", "REG_DWORD" };
            yield return new string[] { "PerfMonMode", "Performance", "REG_DWORD" };
            yield return new string[] { "TapeLoadWaitTime", "Tapeloadwaittime", "REG_DWORD" };
            yield return new string[] { "", "", "" };
        }

        private void ScriptProperties(StringCollection query, ScriptingPreferences sp)
        {
            Initialize(true);

            StringBuilder sbStatement = new StringBuilder();
            Object o = null;

            foreach (string[] REG_PROPS in RegistryProperties(this.DatabaseEngineType))
            {
                if(REG_PROPS[0].Length == 0)
                {
                    break;
                }
                if (!IsSupportedProperty(REG_PROPS[0], sp))
                {
                    continue;
                }
                Property prop = Properties.Get(REG_PROPS[0]);
                if ((null != prop.Value) && (!sp.ScriptForAlter || prop.Dirty))
                {
                    o = prop.Value;

                    if ((REG_PROPS[0] == "NumberOfLogFiles" && (int)o < 6) ||
                        ((o is string) && ((String)o).Length == 0))
                    {
                        ScriptDeleteRegSetting(query, REG_PROPS);
                        continue;
                    }

                    if (REG_PROPS[0] == "LoginMode")
                    {
                        ServerLoginMode loginMode = (ServerLoginMode)o;
                        if (loginMode != ServerLoginMode.Integrated &&
                            loginMode != ServerLoginMode.Normal &&
                            loginMode != ServerLoginMode.Mixed)
                        {
                            throw new SmoException(ExceptionTemplates.UnsupportedLoginMode(loginMode.ToString()));
                        }

                        // LoginMode is enumeration, must be converted to its integer value
                        ScriptRegSetting(query, REG_PROPS, Enum.Format(typeof(ServerLoginMode), (ServerLoginMode)o, "d"));
                        continue;
                    }

                    if (REG_PROPS[0] == "AuditLevel")
                    {
                        AuditLevel auditLevel = (AuditLevel)o;
                        if (0 > auditLevel || AuditLevel.All < auditLevel)
                        {
                            throw new SmoException(ExceptionTemplates.UnknownEnumeration(typeof(AuditLevel).Name));
                        }

                        // auditLevel is enumeration, must be converted to its integer value
                        ScriptRegSetting(query, REG_PROPS, Enum.Format(typeof(AuditLevel), auditLevel, "d"));
                        continue;
                    }

                    if (REG_PROPS[0] == "PerfMonMode")
                    {
                        // Verify that the PerfMonMode set is one of the valid values to set
                        // PerfMonMode.None is not a valid value to set
                        PerfMonMode perfMonMode = (PerfMonMode)o;

                        switch (perfMonMode)
                        {
                            case PerfMonMode.Continuous:
                            case PerfMonMode.OnDemand:
                                // PerfMonMode is enumeration, must be converted to its integer value
                                ScriptRegSetting(query, REG_PROPS, Enum.Format(typeof(PerfMonMode), (PerfMonMode)o, "d"));
                                break;
                            case PerfMonMode.None:
                                // if we get this value we will not script anything, but we need to
                                // throw an error if this comes from the user
                                if (sp.ForDirectExecution)
                                {
                                    goto default;
                                }
                                else
                                {
                                    break;
                                }

                            default:
                                throw new SmoException(ExceptionTemplates.UnknownEnumeration(perfMonMode.GetType().Name));
                        }

                        continue;
                    }

                    // This check is for default data/log directory and backup directory.
                    if (REG_PROPS[0] == "DefaultFile" || REG_PROPS[0] == "DefaultLog" || REG_PROPS[0] == "BackupDirectory")
                    {
                        string regpath = (string)o;
                        if (0 == regpath.Length)
                        {
                            // remove the registry key if the string is empty.
                            ScriptDeleteRegSetting(query, REG_PROPS);
                        }
                        else
                        {
                            // strip the final '\' off
                            if (regpath[regpath.Length - 1] == '\\')
                            {
                                regpath = regpath.Remove(regpath.Length - 1, 1);
                            }
                            ScriptRegSetting(query, REG_PROPS, regpath);
                        }

                        continue;
                    }

                    ScriptRegSetting(query, REG_PROPS, o);
                }
            }
        }

        void ScriptRegSetting(StringCollection query, string[] prop, Object oValue)
        {
            String sRegWrite = Scripts.REG_WRITE_WRITE_PROP;

            if ("REG_SZ" == prop[2])
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], "N'" + SqlString(oValue.ToString())) + "'");
            }
            else if (oValue is System.Boolean)
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], ((bool)oValue) ? 1 : 0));
            }
            else
            {
                query.Add(string.Format(SmoApplication.DefaultCulture, sRegWrite, prop[1], prop[2], oValue.ToString()));
            }
        }

        void ScriptDeleteRegSetting(StringCollection query, string[] prop)
        {
            String sRegDelete = Scripts.REG_DELETE;
            query.Add(string.Format(SmoApplication.DefaultCulture, sRegDelete, prop[1]));
        }

        private OleDbProviderSettingsCollection m_OleDbProviderSettings;
        [SfcObject(SfcContainerRelationship.ObjectContainer, SfcContainerCardinality.ZeroToAny, typeof(OleDbProviderSettings))]
        public OleDbProviderSettingsCollection OleDbProviderSettings
        {
            get
            {
                CheckObjectState();
                if (null == m_OleDbProviderSettings)
                {
                    m_OleDbProviderSettings = new OleDbProviderSettingsCollection(this);
                }
                return m_OleDbProviderSettings;
            }
        }

        /// <summary>
        /// Returns true if the given property name is valid for the object of Type type for the Server's version and edition.
        /// </summary>
        /// <param name="type">A type that is assignable to SqlSmoObject</param>
        /// <param name="propertyName"></param>
        /// <param name="databaseEngineEdition">Specific database edition to check for property supported. If Unknown or not specified, the check will use the edition of the master database.</param>
        /// <returns>true if the property can be requested, false otherwise</returns>
        /// <exception cref="ArgumentException">When type is not a SqlSmoObject or if the SqlSmoObject does not have property visibility based on the Server version</exception>
        public bool IsSupportedProperty(Type type, string propertyName, DatabaseEngineEdition databaseEngineEdition = DatabaseEngineEdition.Unknown)
        {
            databaseEngineEdition = databaseEngineEdition != DatabaseEngineEdition.Unknown ? databaseEngineEdition : DatabaseEngineEdition;
            return !SqlSmoObject.GetDisabledProperties(type, databaseEngineEdition).Contains(propertyName) 
                && SqlPropertyMetadataProvider.CheckPropertyValid(MetadataProviderLookup.GetPropertyMetadataProviderType(type), propertyName, ServerVersion, DatabaseEngineType, databaseEngineEdition);
        }


        /// <summary>
        /// Returns true if the given property name is valid for the object of Type T for the Server's version and edition.
        /// </summary>
        /// <typeparam name="T">The derived type of SqlSmoObject for which to check</typeparam>
        /// <param name="propertyName"></param>
        /// <param name="databaseEngineEdition">Specific database edition to check for property supported. If Unknown or not specified, the check will use the edition of the master database.</param>
        /// <returns>true if the property can be requested, false otherwise</returns>
        /// <exception cref="ArgumentException">When the SqlSmoObject does not have property visibility based on the Server version</exception>
        public bool IsSupportedProperty<T>(string propertyName, DatabaseEngineEdition databaseEngineEdition = DatabaseEngineEdition.Unknown) where T : SqlSmoObject
        {
            return IsSupportedProperty(typeof(T), propertyName, databaseEngineEdition);
        }

        /// <summary>
        /// Returns true if the expression identifies a valid SqlSmoObject property for the current Server version and edition
        /// </summary>
        /// <typeparam name="T">The derived type of SqlSmoObject for which to check</typeparam>
        /// <param name="expression">A LINQ expression that references a property of the object of type T. The name of the referenced property in the expression is used for the lookup.</param>
        /// <param name="databaseEngineEdition"></param>
        /// <returns>true if the property can be requested, false otherwise</returns>
        /// <exception cref="ArgumentException">When the SqlSmoObject does not have property visibility based on the Server version</exception>
        /// <example>server.IsSupportedProperty&lt;Table&gt;(t =&gt; t.LedgerType)</example>
        public bool IsSupportedProperty<T>(Expression<Func<T, object>> expression, DatabaseEngineEdition databaseEngineEdition = DatabaseEngineEdition.Unknown)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }
            if (expression.Body is MemberExpression memberExpression)
            {
                return IsSupportedProperty(memberExpression.Member.DeclaringType, memberExpression.Member.Name, databaseEngineEdition);
            }
            // when the return type of the expression is a value type, it contains a call to Convert, resulting in boxing, so we get a UnaryExpression instead
            if (expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
                if (memberExpression != null)
                {
                    return IsSupportedProperty(memberExpression.Member.DeclaringType, memberExpression.Member.Name);
                }
            }
            throw new ArgumentException(ExceptionTemplates.InvalidPropertyExpression(expression.ToString()));
        }

#region ISfcHasConnection Members

        private SfcConnectionContext sfcConnectionContext;

        SfcConnectionContext ISfcHasConnection.ConnectionContext
        {
            get
            {
                if (null == sfcConnectionContext)
                {
                    sfcConnectionContext = new SfcConnectionContext(this);
                }
                return sfcConnectionContext;
            }
        }

        ISfcConnection ISfcHasConnection.GetConnection(SfcObjectQueryMode activeQueriesMode)
        {
            return this.ConnectionContext;
        }

        ISfcConnection ISfcHasConnection.GetConnection()
        {
            return this.ConnectionContext;
        }

        void ISfcHasConnection.SetConnection(ISfcConnection connection)
        {
            throw new NotSupportedException();
        }

#endregion

#region ISfcDomainLite Members

        /// <summary>
        /// Returns logical version of SMO object model schema
        /// </summary>
        /// <returns></returns>
        int ISfcDomainLite.GetLogicalVersion()
        {
            //currently what we have is the default version. Later on, if logical SMO schema
            //changes (i.e the serialized SMO differs from before), this version
            //value can be incremented.
            return 1;
        }

        /// <summary>
        /// Returns the SMO domain name
        /// </summary>
        string ISfcDomainLite.DomainName
        {
            get
            {
                return Server.DomainName;
            }
        }

        /// <summary>
        /// Returns the instance name of this domain
        /// </summary>
        string ISfcDomainLite.DomainInstanceName
        {
            get
            {
                //both connected and design modes require true name, so it should have been set.
                return m_ExecutionManager.TrueServerName;
            }
        }
#endregion

#region HADR Functionality

        /// <summary>
        /// Gets the supported availability group cluster types for the server
        /// </summary>
        public AvailabilityGroupClusterType[] SupportedAvailabilityGroupClusterTypes
        {
            get
            {
                List<AvailabilityGroupClusterType> supportedClusterTypes = new List<AvailabilityGroupClusterType>();

                if (IsMemberOfWsfcCluster)
                {
                    supportedClusterTypes.Add(AvailabilityGroupClusterType.Wsfc);
                }

                if (VersionMajor >= 14)
                {
                    supportedClusterTypes.Add(AvailabilityGroupClusterType.External);
                    supportedClusterTypes.Add(AvailabilityGroupClusterType.None);
                }

                return supportedClusterTypes.ToArray();
            }
        }

        /// <summary>
        /// Default availability group cluster type for the server
        /// </summary>
        public AvailabilityGroupClusterType DefaultAvailabilityGroupClusterType
        {
            get { return IsMemberOfWsfcCluster ? AvailabilityGroupClusterType.Wsfc : AvailabilityGroupClusterType.External; }
        }

        /// <summary>
        /// Checks whether the server is a member of WSFC cluster
        /// </summary>
        public bool IsMemberOfWsfcCluster
        {
            // cluster name is only available when the instance is a member of WSFC cluster
            // Due to a bug in the sys.dm_hadr_cluster dmv a sever that is not part of a
            //cluster will return a 2 character name - the first character being a '\0' and
            //the second being some other value (sometimes null, sometimes something else)
            //See TFS#10680276 for more information
            //As a workaround we'll check here if the first character is null - and if so
            //assume that we're not actually part of a cluster
            get { return HostPlatform == HostPlatformNames.Windows && !string.IsNullOrEmpty(ClusterName) && !(ClusterName[0] == '\0'); }
        }

        /// <summary>
        /// This method is called on a secondary replica that has been added to an Availability Group
        /// from a primary to complete the handshake.
        /// Upon successful execution the instance will become a functioning replica in the availability
        /// group.
        /// </summary>
        public void JoinAvailabilityGroup(string availabilityGroupName)
        {
            JoinAvailabilityGroup(availabilityGroupName, AvailabilityGroupClusterType.Wsfc);
        }

        /// <summary>
        /// This method is called on a secondary replica that has been added to an Availability Group
        /// from a primary to complete the handshake.
        /// Upon successful execution the instance will become a functioning replica in the availability
        /// group.
        /// </summary>
        public void JoinAvailabilityGroup(string availabilityGroupName, AvailabilityGroupClusterType availabilityGroupClusterType)
        {
            try
            {
                //ALTER AVAILABILTY GROUP [group_name] JOIN
                string script = Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space +
                                    SqlSmoObject.MakeSqlBraket(availabilityGroupName) + Globals.space + Scripts.JOIN;

                if (availabilityGroupClusterType != AvailabilityGroupClusterType.Wsfc)
                {
                    this.ThrowIfBelowVersion140();
                    // WITH (CLUSTER_TYPE = {CLUSTERTYPE})
                    script += Globals.space + Globals.With + Globals.space+ Globals.LParen +
                        AvailabilityGroup.ClusterTypeScript + Globals.space + Globals.EqualSign + Globals.space +
                        AvailabilityGroup.GetAvailabilityGroupClusterType(availabilityGroupClusterType) +
                        Globals.RParen;
                }

                script += Globals.statementTerminator;

                this.ExecutionManager.ExecuteNonQuery(script);

                //generate a create event for the newly added availability group
                AvailabilityGroup agJoined = this.AvailabilityGroups[availabilityGroupName];  //get the object for the just joined availabilty group
                if (!this.ExecutionManager.Recording)
                {
                    // generate internal events
                    if (!SmoApplication.eventsSingleton.IsNullObjectCreated())
                    {
                        SmoApplication.eventsSingleton.CallObjectCreated(GetServerObject(),
                            new ObjectCreatedEventArgs(agJoined.Urn, agJoined));
                    }
                }
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.JoinAvailabilityGroupFailed(this.Name, availabilityGroupName), e);
            }
        }

        /// <summary>
        /// This method is called on a secondary replica that has joined the availability group to grant availability group create database privilege
        /// for automatic seeding to work
        /// </summary>
        /// <param name="availabilityGroupName">Availability group name</param>
        public void GrantAvailabilityGroupCreateDatabasePrivilege(string availabilityGroupName)
        {
            SetAvailabilityGroupCreateDatabasePrivilege(availabilityGroupName, grantPrivilege: true);
        }

        /// <summary>
        /// Revoke the create database privilege of the specified availability group
        /// </summary>
        /// <param name="availabilityGroupName">Availability group name</param>
        public void RevokeAvailabilityGroupCreateDatabasePrivilege(string availabilityGroupName)
        {
            SetAvailabilityGroupCreateDatabasePrivilege(availabilityGroupName, grantPrivilege: false);
        }

        /// <summary>
        /// Set the availability group's create database privilege
        /// </summary>
        /// <param name="availabilityGroupName">Availability group name</param>
        /// <param name="grantPrivilege">boolean value indicating whether to grant or revoke the privilege</param>
        private void SetAvailabilityGroupCreateDatabasePrivilege(string availabilityGroupName, bool grantPrivilege)
        {
            if (string.IsNullOrEmpty(availabilityGroupName))
            {
                throw new ArgumentNullException("availabilityGroupName");
            }

            try
            {
                string actionScript = grantPrivilege ? Scripts.GRANT : Scripts.DENY;
                //ALTER AVAILABILITY GROUP [group_name] DENY/GRANT CREATE ANY DATABASE
                string script = Scripts.ALTER + Globals.space + AvailabilityGroup.AvailabilityGroupScript + Globals.space
                                + SqlSmoObject.MakeSqlBraket(availabilityGroupName) + Globals.space
                                + actionScript
                                + Scripts.CREATE + Globals.space + Scripts.ANY + Globals.space
                                + AvailabilityGroup.DatabaseScript;

                script += Globals.statementTerminator;

                this.ExecutionManager.ExecuteNonQuery(script);
            }
            catch (Exception e)
            {
                FilterException(e);

                string exceptionMessage = grantPrivilege ? ExceptionTemplates.GrantAGCreateDatabasePrivilegeFailed(this.Name, availabilityGroupName)
                    : ExceptionTemplates.RevokeAGCreateDatabasePrivilegeFailed(this.Name, availabilityGroupName);
                throw new FailedOperationException(exceptionMessage, e);
            }
        }

        /// <summary>
        /// Enumerate the current state of the Windows cluster members of which the instance is a part of.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumClusterSubnets()
        {
            this.ThrowIfBelowVersion110();
            ThrowIfCloud();

            try
            {
                Request req = new Request(this.Urn.Value + "/ClusterSubnet");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumClusterSubnets(this.Name), this, e);
            }
        }

        /// <summary>
        /// Enumerate the current state of the Windows cluster members of which the instance is a part of.
        /// </summary>
        /// <returns></returns>
        public DataTable EnumClusterMembersState()
        {
            this.ThrowIfBelowVersion110();
            ThrowIfCloud();

            try
            {
                Request req = new Request(this.Urn.Value + "/ClusterMemberState");
                return this.ExecutionManager.GetEnumeratorData(req);
            }
            catch (Exception e)
            {
                FilterException(e);

                throw new FailedOperationException(ExceptionTemplates.EnumClusterMemberState(this.Name), this, e);
            }
        }

        #region Feature availability check section, Use these properties when the desired SMO object is not available.

        public bool IsConfigurationOnlyAvailabilityReplicaSupported
        {
            get
            {
                // This feature is enabled starting from SQL Server 2017 RTM CU1
                //
                return this.VersionMajor > 14 || (this.VersionMajor == 14 && this.BuildNumber >= 3000);
            }
        }

        public bool IsAvailabilityReplicaSeedingModeSupported
        {
            get { return this.VersionMajor >= 13; }
        }

        public bool IsCrossPlatformAvailabilityGroupSupported
        {
            get { return this.VersionMajor >= 14; }
        }

        public bool IsReadOnlyListWithLoadBalancingSupported
        {
            get { return this.VersionMajor >= 13; }
        }

        #endregion

#endregion

        #region File System Functionality

        /// <summary>
        /// Checks whether the specified file exists
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>A boolean value indicating whether the specified file exists</returns>
        public bool FileExists(string filePath)
        {
            var result = QueryFileInformation(filePath);

            // 0 - file_exists
            return Convert.ToBoolean(result[0]);
        }

        /// <summary>
        /// Checks whether the parent directory of the specified file exists
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>A boolean value indicating whether the parent directory of specified file exists</returns>
        public bool ParentDirectoryExists(string filePath)
        {
            var result = QueryFileInformation(filePath);

            // 2 - parent_directory_exists
            return Convert.ToBoolean(result[2]);
        }

        /// <summary>
        /// Gets the information of the specified file.
        ///  Columns in the row:
        ///  0 - file_exists
        ///  1 - file_is_a_directory
        ///  2 - parent_directory_exists
        /// </summary>
        /// <param name="filePath">The file path on server</param>
        /// <returns>A datarow object with the information of the specified file </returns>
        private DataRow QueryFileInformation(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            string commandText;

            // Use SQL parameter to avoid the need of handling of strings as well as SQL injection
            string filePathParameter = string.Format("<msparam>{0}</msparam>", filePath);

            if (HostPlatform == HostPlatformNames.Windows)
            {
                commandText = string.Format("exec master..xp_fileexist {0}", filePathParameter);
            }
            else
            {
                commandText = string.Format("select file_exists, file_is_a_directory, parent_directory_exists from sys.dm_os_file_exists({0})", filePathParameter);
            }

            DataSet dataSet = this.ConnectionContext.ExecuteWithResults(commandText);

            if (dataSet == null || dataSet.Tables.Count != 1 || dataSet.Tables[0].Rows.Count != 1 || dataSet.Tables[0].Columns.Count != 3)
            {
                throw new InternalSmoErrorException(ExceptionTemplates.InvalidFileInformationData);
            }

            return dataSet.Tables[0].Rows[0];
        }

#endregion
    }
    /// <summary>
    /// Class that encapsulates all functionality
    /// related to Server Events.
    /// </summary>
    public sealed class ServerEvents
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        internal ServerEvents(Server parent)
        {
            this.parent = parent;
        }

        /// <summary>
        /// Internal WMI connection object. It is used
        /// by all objects on this server to recieve
        /// Server Events.
        /// </summary>
        private ManagementScope managementScope;
        internal ManagementScope ManagementScope
        {
            get
            {
                if (null == this.managementScope)
                {
                    // Get the machine name
                    string machineName = this.parent.Information.NetName;
                    string instanceName = this.parent.InstanceName;
                    if (instanceName.Length == 0)
                    {
                        instanceName = "MSSQLSERVER";
                    }

                    this.managementScope = new ManagementScope();
                    this.managementScope.Path = new ManagementPath(
                        string.Format(SmoApplication.DefaultCulture, @"\\{0}\root\Microsoft\SqlServer\ServerEvents\{1}", machineName, instanceName));

                    if (null != this.connectionOptions)
                    {
                        this.managementScope.Options = this.connectionOptions;
                    }
                }
                return this.managementScope;
            }
        }

        /// <summary>
        /// User-provided credentials to connect to WMI
        /// provider on the server.
        /// If no credentials are provided we will connect
        /// with current user context.
        /// </summary>
        private ConnectionOptions connectionOptions;
        public void SetCredentials(string username, string password)
        {
            if (null == this.connectionOptions)
            {
                this.connectionOptions = new ConnectionOptions();
            }
            this.connectionOptions.Username = username;
            this.connectionOptions.Password = password;
        }

        /// <summary>
        /// Returns current selection of server events.
        /// This method returns a copy of event selection
        /// which can be modified and submitted back through
        /// SubscribeToEvents method.
        /// </summary>
        public ServerEventSet GetEventSelection()
        {
            InitializeServerEvent();
            return (ServerEventSet)this.serverEventWorker.GetEventSelection();
        }

        /// <summary>
        /// Returns current selection of server trace events.
        /// This method returns a copy of event selection
        /// which can be modified and submitted back through
        /// SubscribeToEvents method.
        /// </summary>
        public ServerTraceEventSet GetTraceEventSelection()
        {
            InitializeServerTraceEvent();
            return (ServerTraceEventSet)this.serverTraceEventWorker.GetEventSelection();
        }

        /// <summary>
        /// Subscribes to a set of events on a server.
        /// Notifications for those events will be raised
        /// using a default handler on this class.
        /// Each time this method is called a new set of
        /// events is added to already subscribed events.
        /// </summary>
        public void SubscribeToEvents(ServerEventSet events)
        {
            InitializeServerEvent();
            this.serverEventWorker.SubscribeToEvents(events, null);
        }

        /// <summary>
        /// Subscribes to a set of events on a server.
        /// Notifications for those events will be raised
        /// using an event handler provided to this method.
        /// Each time this method is called a new set of
        /// events is added to already subscribed events.
        /// If an event is already subscribed its exisitng
        /// handler will be replaced by the new handler passed
        /// to this method.
        /// </summary>
        public void SubscribeToEvents(ServerEventSet events, ServerEventHandler eventHandler)
        {
            InitializeServerEvent();
            this.serverEventWorker.SubscribeToEvents(events, eventHandler);
        }

        /// <summary>
        /// Subscribes to a set of trace events on a server.
        /// Notifications for those events will be raised
        /// using a default handler on this class.
        /// Each time this method is called a new set of
        /// events is added to already subscribed events.
        /// </summary>
        public void SubscribeToEvents(ServerTraceEventSet events)
        {
            InitializeServerTraceEvent();
            this.serverTraceEventWorker.SubscribeToEvents(events, null);
        }

        /// <summary>
        /// Subscribes to a set of trace events on a server.
        /// Notifications for those events will be raised
        /// using an event handler provided to this method.
        /// Each time this method is called a new set of
        /// events is added to already subscribed events.
        /// If an event is already subscribed its exisitng
        /// handler will be replaced by the new handler passed
        /// to this method.
        /// </summary>
        public void SubscribeToEvents(ServerTraceEventSet events, ServerEventHandler eventHandler)
        {
            InitializeServerTraceEvent();
            this.serverTraceEventWorker.SubscribeToEvents(events, eventHandler);
        }

        /// <summary>
        /// Starts event notifications. This method must be called to
        /// start receiving notifications.
        /// </summary>
        public void StartEvents()
        {
            InitializeServerEvent();
            InitializeServerTraceEvent();

            this.serverEventWorker.StartEvents();
            this.serverTraceEventWorker.StartEvents();
        }

        /// <summary>
        /// Stops event notifications.
        /// </summary>
        public void StopEvents()
        {
            if (null != this.serverEventWorker)
            {
                this.serverEventWorker.StopEvents();
            }
            if (null != this.serverTraceEventWorker)
            {
                this.serverTraceEventWorker.StopEvents();
            }
        }

        /// <summary>
        /// Unsubscribes from a set of events.
        /// </summary>
        public void UnsubscribeFromEvents(ServerEventSet events)
        {
            if (null != this.serverEventWorker)
            {
                this.serverEventWorker.UnsubscribeFromEvents(events);
            }
        }

        /// <summary>
        /// Unsubscribes from a set of trace events.
        /// </summary>
        public void UnsubscribeFromEvents(ServerTraceEventSet events)
        {
            if (null != this.serverTraceEventWorker)
            {
                this.serverTraceEventWorker.UnsubscribeFromEvents(events);
            }
        }

        /// <summary>
        /// Unsubscribes from all events and releases all
        /// memory allocated for events subscriptions.
        /// </summary>
        public void UnsubscribeAllEvents()
        {
            if (null != this.serverEventWorker)
            {
                this.serverEventWorker.Dispose();
                this.serverEventWorker = null;
            }
            if (null != this.serverTraceEventWorker)
            {
                this.serverTraceEventWorker.Dispose();
                this.serverTraceEventWorker = null;
            }
        }

        /// <summary>
        /// A default event handler. Add your handlers
        /// here in order to recieve notifiactions.
        /// </summary>
        public event ServerEventHandler ServerEvent
        {
            add
            {
                InitializeServerEvent();
                InitializeServerTraceEvent();

                this.serverEventWorker.AddDefaultEventHandler(value);
                this.serverTraceEventWorker.AddDefaultEventHandler(value);
            }

            remove
            {
                if (null != this.serverEventWorker)
                {
                    this.serverEventWorker.RemoveDefaultEventHandler(value);
                }
                if (null != this.serverTraceEventWorker)
                {
                    this.serverTraceEventWorker.RemoveDefaultEventHandler(value);
                }
            }
        }

        /// <summary>
        /// Allocated event helper class for server events.
        /// </summary>
        private void InitializeServerEvent()
        {
            if (null == serverEventWorker)
            {
                serverEventWorker = new ServerEventsWorker(this.parent, typeof(ServerEventSet), typeof(ServerEventValues));
            }
        }

        /// <summary>
        /// Allocated event helper class for server trace events.
        /// </summary>
        private void InitializeServerTraceEvent()
        {
            if (null == serverTraceEventWorker)
            {
                serverTraceEventWorker = new ServerEventsWorker(this.parent, typeof(ServerTraceEventSet), typeof(ServerTraceEventValues));
            }
        }


        private ServerEventsWorker serverEventWorker;
        private ServerEventsWorker serverTraceEventWorker;
        private Server parent;
    }
}

