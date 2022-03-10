// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Diagnostics.STrace;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.SqlParser.Metadata;
using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class Server : DatabaseObjectBase, IServer
    {
        private const string MasterDatabaseName = "master";

        private readonly Smo.Server m_smoMetadataObject;
        private readonly bool m_isConnected;

        private IMetadataCollection<IDatabase> m_databases;

        private readonly CredentialCollectionHelper m_credentials;
        private readonly LoginCollectionHelper m_logins;
        private readonly TriggerCollectionHelper m_triggers;

        private CollationInfo m_collationInfo;
        private IDatabase m_masterDatabase;

        private readonly object m_syncRoot = new object();

        public Server(Smo.Server smoMetadataObject, bool isConnected)
        {
            TraceHelper.TraceContext.Assert(smoMetadataObject != null, "SmoMetadataProvider Assert", "smoMetadataObject != null");

            this.m_smoMetadataObject = smoMetadataObject;
            this.m_isConnected = isConnected;

            using (var activity = TraceHelper.TraceContext.GetActivityContext("Refresh Server."))
            {

#if NYI_TIMERS
            // -- TIMER - START -- [Refresh_Server]
            Timers.Start(TimerId.Refresh_Server);
#endif

                // We here load the optimized init field configuration for all SMO types.
                // In general we need to load the optimized configuration right before
                // refreshing a SMO collection. If this threw an exception then we load
                // the safe configuration then we try again.
                // See RefreshSmoCollection method below.

                // set init fields of SMO types
                if (this.IsConnected)
                {
                    foreach (Config.SmoInitFields initFields in Config.SmoInitFields.GetAllInitFields())
                    {
                        this.m_smoMetadataObject.SetDefaultInitFields(initFields.Type, this.m_smoMetadataObject.ConnectionContext.DatabaseEngineEdition, initFields.Optimized);
                    }

                    // refresh metadata object - only if in connected mode
                    this.m_smoMetadataObject.Refresh();
                }

                // refresh SMO database collection 
                // This only uses the "Safe" properties list but that should speed up the initial
                // list population, as the check for IsAccessible can be expensive. We can delay the fetch 
                // for any given database until it is actually accessed.
                PopulateDatabasesCollection(m_smoMetadataObject.ConnectionContext, m_smoMetadataObject);

                m_collationInfo = CollationInfo.Default;
                // retrieve SMO master database
                Smo.Database smoMasterDb = null;
                try
                {
                    smoMasterDb = m_smoMetadataObject.ServerType != DatabaseEngineType.SqlAzureDatabase ? this.m_smoMetadataObject.Databases[MasterDatabaseName] : null;
                    // retrieve server collation (i.e. master database collation)
                    if (smoMasterDb != null)
                    {
                        string serverCollation = smoMasterDb.Collation;
                        this.m_collationInfo = Utils.GetCollationInfo(serverCollation);
                    }
                }
                catch (ConnectionFailureException)
                {
                    // user doesn't have access to master
                }

                //--- create collection of databases ---
                // If we don't have access to master we only add the current database to it
                DatabaseCollection databases = new DatabaseCollection(
                    smoMasterDb == null ? 1 : this.m_smoMetadataObject.Databases.Count, this.m_collationInfo);

                if (smoMasterDb != null)
                {
                    // iterate over databases and add each one of them
                    foreach (Smo.Database database in this.m_smoMetadataObject.Databases)
                    {
                        databases.Add(new Database(database, this));
                    }
                }
                else
                {
                    var smoDb =
                        this.m_smoMetadataObject.Databases[this.m_smoMetadataObject.ConnectionContext.DatabaseName];
                    databases.Add(new Database(smoDb, this));
                }
                this.SetDatabases(databases);

                // create and set collection helpers
                this.m_credentials = new CredentialCollectionHelper(this);
                this.m_logins = new LoginCollectionHelper(this);
                this.m_triggers = new TriggerCollectionHelper(this);
#if NYI_TIMERS
            // -- TIMER - STOP -- [Refresh_Server]
            Timers.Stop(TimerId.Refresh_Server);
#endif
            }
        }

        public bool IsConnected
        {
            get { return this.m_isConnected; }
        }

        #region IDatabaseObject Members
        public IDatabaseObject Parent
        {
            get { return null; }
        }

        public bool IsSystemObject
        {
            get { return false; }
        }

        public bool IsVolatile
        {
            get { return false; }
        }

        public T Accept<T>(IDatabaseObjectVisitor<T> visitor)
        {
            if (visitor == null) throw new ArgumentNullException("visitor");

            return visitor.Visit(this);
        }
        #endregion

        #region IMetadataObject Members
        public string Name
        {
            get { return this.m_smoMetadataObject.Name; }
        }
        public T Accept<T>(IMetadataObjectVisitor<T> visitor)
        {
            return this.Accept((IDatabaseObjectVisitor<T>)visitor);
        }

        #endregion


        #region IServer Members
        public CollationInfo CollationInfo
        {
            get { return this.m_collationInfo; }
        }

        public IMetadataCollection<ICredential> Credentials
        {
            get { return this.m_credentials.MetadataCollection; }
        }

        public IMetadataCollection<IDatabase> Databases
        {
            get { return this.m_databases; }
        }

        public IMetadataCollection<ILogin> Logins 
        {
            get { return this.m_logins.MetadataCollection; } 
        }

        public IMetadataCollection<IServerDdlTrigger> Triggers
        {
            get { return this.m_triggers.MetadataCollection; }
        }

        #endregion

        #region Public Members
        public IDatabase MasterDatabase
        {
            get
            {
                TraceHelper.TraceContext.Assert(this.m_masterDatabase != null, "SmoMetadataProvider Assert", "master database cannot be null!");

                return this.m_masterDatabase;
            }
        }

        /// <summary>
        /// Retrieves a snapshot of the server database list and updates its database
        /// collection. This method checks for create, dropped and accessibility changed
        /// databases.
        /// </summary>
        public void RefreshDatabaseList()
        {
            // We here are going to create a temp SMO server object that we will use to
            // capture a snapshot of the list of databases in the server. We could have
            // called Refresh on the database collection of our SMO server object, but
            // none of the two overloads behaves the way we want it. For instance, if
            // we called DatabaseCollection.Refresh(), it won't update fields of 
            // already fetched databases. While if we called Refresh(true), SMO will
            // iterate over each database in the collection and will call Refresh on it
            // causing a big number of queries to be executed. SMO will retrieve the
            // full database list in one query if the database collection was not yet
            // created.
            // In addition to this, refreshing the database list in parallel to the
            // existing list simplifies the logic of having to copy some fields to our
            // database object before calling Refresh().

            TraceHelper.TraceContext.Assert(this.IsConnected, "SmoMetadataProvider", "Must be in connected mode.");
            using (var methodTrace = TraceHelper.TraceContext.GetMethodContext("RefreshDatabaseList"))
            {
                ServerConnection serverConnection = this.m_smoMetadataObject.ConnectionContext;
                Smo.Server newServer = new Smo.Server(serverConnection);
                newServer.SetDefaultInitFields(
                    Config.SmoInitFields.Database.Type, this.m_smoMetadataObject.ConnectionContext.DatabaseEngineEdition, Config.SmoInitFields.Database.Safe);

                PopulateDatabasesCollection(serverConnection, newServer);

                methodTrace.TraceVerbose("Found {0} databases on server {1}", newServer.Databases.Count, newServer.Name);
                Smo.DatabaseCollection curDatabaseCollection = this.m_smoMetadataObject.Databases;
                Smo.DatabaseCollection newDatabaseCollection = newServer.Databases;

                bool hasChanged = false;

                if (curDatabaseCollection.Count == newDatabaseCollection.Count)
                {
                    foreach (Smo.Database newDatabase in newDatabaseCollection)
                    {
                        Smo.Database curDatabase = curDatabaseCollection[newDatabase.Name];
                        methodTrace.TraceVerbose("Checking IsAccessible for {0} and {1}", newDatabase.Name, curDatabase == null? "<none>" : curDatabase.Name);
                        if ((curDatabase == null) || (Utils.GetPropertyValue<bool>(newDatabase, "IsAccessible", true) != Utils.GetPropertyValue<bool>(curDatabase, "IsAccessible", true)))
                        {
                            hasChanged = true;
                            break;
                        }
                    }
                }
                else
                {
                    hasChanged = true;
                }

                if (hasChanged)
                {
                    methodTrace.TraceVerbose("Database collection has changed");
                    DatabaseCollection databases = new DatabaseCollection(
                        newDatabaseCollection.Count, this.m_collationInfo);

                    // load database safe init field list
                    this.m_smoMetadataObject.SetDefaultInitFields(
                        Config.SmoInitFields.Database.Type, serverConnection.DatabaseEngineEdition, Config.SmoInitFields.Database.Safe);

                    foreach (Database database in this.Databases)
                    {
                        string dbName = database.Name;
                        Smo.Database curSmoDatabase = curDatabaseCollection[dbName];
                        Smo.Database newSmoDatabase = newDatabaseCollection[dbName];

                        TraceHelper.TraceContext.Assert(curSmoDatabase != null, "Bind Assert", "curSmoDatabase != null");

                        // if there is no newSmoDatabase object then the database must
                        // have been dropped
                        if (newSmoDatabase != null)
                        {
                            methodTrace.TraceVerbose("Checking IsAccessible for {0} and {1}", newSmoDatabase.Name,  curSmoDatabase.Name);
                            if (Utils.GetPropertyValue<bool>(curSmoDatabase, "IsAccessible", true) == Utils.GetPropertyValue<bool>(newSmoDatabase, "IsAccessible", true))
                            {
                                databases.Add(database);
                            }
                            else
                            {
                                curSmoDatabase.Refresh();
                                databases.Add(new Database(curSmoDatabase, this));
                            }
                        }
                    }

                    methodTrace.TraceVerbose("Refreshing Smo database collection");
                    // refresh current SMO collection
                    this.TryRefreshSmoCollection(curDatabaseCollection, Config.SmoInitFields.Database);

                    TraceHelper.TraceContext.Assert(newDatabaseCollection.Count == curDatabaseCollection.Count,
                        "Bind Assert", "Number of databases must match that of the latest snapshot!");

                    // Check if there is any database in the new snapshot that we don't have
                    // in our new database collection.
                    if (curDatabaseCollection.Count > databases.Count)
                    {
                        // we iterate over all SMO databases and add the ones that we don't have
                        foreach (Smo.Database smoDatabase in curDatabaseCollection)
                        {
                            if (!databases.Contains(smoDatabase.Name))
                            {
                                databases.Add(new Database(smoDatabase, this));
                            }
                        }
                    }

                    this.SetDatabases(databases);
                }
            }
        }

        private static void PopulateDatabasesCollection(ServerConnection serverConnection, Smo.Server newServer)
        {
            // For Azure SQL database, we only want to fetch the current database because there's no point showing objects
            // from other databases. Also, these fetches of properties from other databases are likely to fail due to lack
            // of access and these queries may create undesired activity
            if (serverConnection.DatabaseEngineType == DatabaseEngineType.SqlAzureDatabase)
            {
                var databaseName = !string.IsNullOrEmpty(serverConnection.DatabaseName)
                    ? serverConnection.DatabaseName
                    : !string.IsNullOrEmpty(serverConnection.CurrentDatabase)
                        ? serverConnection.CurrentDatabase
                        : "master";
                newServer.Databases.ClearAndInitialize(string.Format("[@Name='{0}']", Urn.EscapeString(databaseName)),
                    null);
            }
            else
            {
                newServer.Databases.Refresh();
            }
        }

        public void TryRefreshSmoCollection(Smo.SmoCollectionBase collection, Config.SmoInitFields initFields)
        {
            TraceHelper.TraceContext.Assert(collection != null, "MetadataProvider Assert", "collection != null");
            using (var methodTrace = TraceHelper.TraceContext.GetMethodContext("TryRefreshSmoCollection"))
            {
                //In disconnected mode, all SMO objects should be up to date all the time
                if (this.IsConnected)
                {
                    try
                    {
                        if (initFields != null)
                        {
                            // We here load the optimized init field configuration first, if the query
                            // issued by SMO caused an ExecutionFailureException then we load the safe
                            // field configuration and try again. The most common reason for this
                            // exception is if the user doesn't have permission to any of the data
                            // retrieved by the query. For instance if the a SVF has a UDDT return
                            // and the user has permission to the SVF itself but not the type.

                            try
                            {
                                // we first load optimized fields and attempt to refresh
                                this.m_smoMetadataObject.SetDefaultInitFields(initFields.Type,
                                    this.m_smoMetadataObject.ConnectionContext.DatabaseEngineEdition,
                                    initFields.Optimized);
                                collection.Refresh();
                            }
                            catch (Exception)
                            {
                                // if we failed then we load safe fields and re-refresh
                                this.m_smoMetadataObject.SetDefaultInitFields(initFields.Type,
                                    this.m_smoMetadataObject.ConnectionContext.DatabaseEngineEdition, initFields.Safe);
                                collection.Refresh();
                            }
                        }
                        else
                        {
                            // load default fields.
                            collection.Refresh();
                        }
                    }
                    catch (InvalidVersionEnumeratorException ivee)
                    {
                        methodTrace.TraceCatch(ivee);
                        // Suppress this exception, thrown when the property is generally not supported on Azure. 
                    }
                    catch (Smo.UnsupportedVersionException)
                    {
                        // server version is < 10 (pre Katmai)
                    }
                    catch (Exception e)
                    {
                        methodTrace.TraceCatch(e);
                        // Suppress all exceptions when working with in a connected mode. 
                    }
                }
            }
        }

        private void SetDatabases(IMetadataCollection<IDatabase> collection)
        {
            TraceHelper.TraceContext.Assert(collection != null, "MetadataProvider Assert {0}", "collection != null");

            IDatabase masterDb = collection[MasterDatabaseName];

            // if we don't have a master database, we add an empty place-holder one
            if (masterDb == null)
            {
                masterDb = SmoMetadataFactory.Instance.Database.CreateEmptyDatabase(
                    this, MasterDatabaseName, this.m_collationInfo, true);

                // add it to the collection of databases
                IMetadataCollection<IDatabase> newCollection = Collection<IDatabase>.CreateOrderedCollection(
                    this.m_collationInfo, masterDb);
                collection = Collection<IDatabase>.Merge(newCollection, collection);
            }

            TraceHelper.TraceContext.Assert(masterDb != null, "MetadataProvider Assert {0}", "masterDb != null");
            TraceHelper.TraceContext.Assert(collection[MasterDatabaseName] == masterDb, "MetadataProvider Assert {0}", "collection[MasterDatabaseName] == masterDb");
            
            this.m_databases = collection;
            this.m_masterDatabase = masterDb;
        }

        #endregion

        #region CollectionHelper Class
        abstract private class CollectionHelper<T, S> : UnorderedCollectionHelperBase<T, S>
            where T : class, IServerOwnedObject
            where S : Smo.NamedSmoObject
        {
            protected readonly Server m_server;

            public CollectionHelper(Server server)
            {
                TraceHelper.TraceContext.Assert(server != null, "SmoMetadataProvider Assert", "server != null");

                this.m_server = server;
            }

            protected override Server Server
            {
                get { return this.m_server; }
            }
         

            protected override CollationInfo GetCollationInfo()
            {
                return this.m_server.m_collationInfo;
            }
        }

        /// <summary>
        /// Credentials
        /// </summary>
        private class CredentialCollectionHelper : CollectionHelper<ICredential, Smo.Credential>
        {
            public CredentialCollectionHelper(Server server)
                : base(server)
            {
            }

            protected override IMetadataList<Smo.Credential> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.Credential>(
                    this.m_server,
                    this.m_server.m_smoMetadataObject.Credentials);
            }

            protected override IMutableMetadataCollection<ICredential> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new CredentialCollection(initialCapacity, collationInfo);
            }

            protected override ICredential CreateMetadataObject(Smo.Credential smoObject)
            {
                return new Credential(smoObject, this.m_server);
            }
        }

        /// <summary>
        /// Logins
        /// </summary>
        private class LoginCollectionHelper : CollectionHelper<ILogin, Smo.Login>
        {
            public LoginCollectionHelper(Server server)
                : base(server)
            {
            }

            protected override IMetadataList<Smo.Login> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.Login>(
                    this.m_server,
                    this.m_server.m_smoMetadataObject.Logins);
            }

            protected override IMutableMetadataCollection<ILogin> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new LoginCollection(initialCapacity, collationInfo);
            }

            protected override ILogin CreateMetadataObject(Smo.Login smoObject)
            {
                return Login.CreateLogin(smoObject, this.m_server);
            }
        }

        /// <summary>
        /// Triggers
        /// </summary>
        private class TriggerCollectionHelper : CollectionHelper<IServerDdlTrigger, Smo.ServerDdlTrigger>
        {
            public TriggerCollectionHelper(Server server)
                : base(server)
            {
            }

            protected override IMetadataList<Smo.ServerDdlTrigger> RetrieveSmoMetadataList()
            {
                return new SmoCollectionMetadataList<Smo.ServerDdlTrigger>(
                    this.m_server,
                    this.m_server.m_smoMetadataObject.Triggers);
            }

            protected override IMutableMetadataCollection<IServerDdlTrigger> CreateMutableCollection(int initialCapacity, CollationInfo collationInfo)
            {
                return new ServerDdlTriggerCollection(initialCapacity, collationInfo);
            }

            protected override IServerDdlTrigger CreateMetadataObject(Smo.ServerDdlTrigger smoObject)
            {
                return new ServerDdlTrigger(smoObject, this.m_server);
            }
        }

        #endregion
    }
}
