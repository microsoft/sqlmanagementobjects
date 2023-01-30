// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

using Microsoft.SqlServer.Management.XEvent;

namespace Microsoft.SqlServer.Management.XEventDbScoped
{
    /// <summary>
    /// XEStore is the root for all metadata classes and runtime classes.
    /// </summary>
    public sealed class DatabaseXEStore : BaseXEStore, ISfcDomain
    {
        /// <summary>
        /// Type name.
        /// </summary>
        public const string TypeTypeName = "DatabaseXEStore";

        private const string NameSpace = "http://schemas.microsoft.com/sqlserver/2008/07/extendedeventsconfig";
        private const string DomainName = "DatabaseXEvent";

        private string name;
        private SfcConnectionContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseXEStore"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="name">Store name.</param>
        public DatabaseXEStore(SqlStoreConnection connection, string name)
            : this()
        {
            if (connection == null)
            {
                throw new ArgumentNullException("connection");
            }

            if (String.IsNullOrEmpty(name))
            {
                throw new XEventException(ExceptionTemplates.NameNullEmpty);
            }

            this.name = name;
            this.OriginalConnection = connection;
            this.InitConnection();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseXEStore"/> class.
        /// This constructor is only used to support serialization. 
        /// </summary>
        public DatabaseXEStore()
        {
        }

        /// <summary>
        /// Constructs a new DatabaseXEStore instance whose name is the same as the current active database
        /// </summary>
        /// <param name="connection"></param>
        public DatabaseXEStore(SqlStoreConnection connection) :this(connection, connection.ServerConnection.CurrentDatabase)
        {
            
        }

        /// <summary>
        /// Produces a string representing the store.
        /// </summary>
        /// <returns>A string representing the store.</returns>
        public override string ToString()
        {
            // ToString is overriden here since the default from SfcInstance is to use whatever the Key.ToString() does,
            // and since XEStore doesn't have any Key fields per se, we override it on the class itself and make our own string.
            return String.Format(
                CultureInfo.InvariantCulture, 
                "{0} (Server='{1}' and Name='{2}')", 
                DatabaseXEStore.TypeTypeName,
                this.ServerName, 
                this.Name,
                this.SfcConnection != null ? SfcSecureString.EscapeSquote(this.SfcConnection.ServerInstance) : String.Empty);
        }

        /// <summary>
        /// Returns the key associated with the store.
        /// </summary>
        /// <returns>The key associated with the store.</returns>
        [SfcIgnore]
        public override SfcKey IdentityKey
        {
            get { return this.AbstractIdentityKey; }
        }

        #region ISfcHasConnection Members

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting a single serial query, so the query must end before another one may begin.
        /// </summary>
        /// <returns>The connection to use.</returns>
        ISfcConnection ISfcHasConnection.GetConnection()
        {
            return this.SfcConnection;
        }

        /// <summary>
        /// Sets the active connection.
        /// </summary>
        /// <param name="connection">Connection to use.</param>
        void ISfcHasConnection.SetConnection(ISfcConnection connection)
        {
            this.SetConnection(connection);
        }

        /// <summary>
        /// Get the current connection to query on.
        /// Return a connection supporting either a single serial query or multiple simultaneously open queries as requested.
        /// </summary>
        /// <param name="mode">Query mode.</param>
        /// <returns>The connection to use, or null to use Cache mode. Cache mode avoids connection and open data reader issues.</returns>
        ISfcConnection ISfcHasConnection.GetConnection(SfcObjectQueryMode mode)
        {
            return this.StoreProvider.GetConnection(mode);
        }

        /// <summary>
        /// Gets connection context.
        /// </summary>
        SfcConnectionContext ISfcHasConnection.ConnectionContext
        {
            get
            {
                if (this.context == null)
                {
                    // If our SqlStoreConnection is still null when this is called, we are forced into Offline mode.
                    this.context = new SfcConnectionContext(this);
                }

                return this.context;
            }
        }

        #endregion

        #region ISfcDomain Members

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>Type correspoding to the given name.</returns>
        Type ISfcDomain.GetType(string typeName)
        {
            switch (typeName)
            {
                case DatabaseXEStore.TypeTypeName: return typeof(DatabaseXEStore);
                case Package.TypeTypeName: return typeof(Package);
                case EventInfo.TypeTypeName: return typeof(EventInfo);
                case EventColumnInfo.TypeTypeName: return typeof(EventColumnInfo);
                case DataEventColumnInfo.TypeTypeName: return typeof(DataEventColumnInfo);
                case ReadOnlyEventColumnInfo.TypeTypeName: return typeof(ReadOnlyEventColumnInfo);
                case ActionInfo.TypeTypeName: return typeof(ActionInfo);
                case TargetInfo.TypeTypeName: return typeof(TargetInfo);
                case TargetColumnInfo.TypeTypeName: return typeof(TargetColumnInfo);
                case PredSourceInfo.TypeTypeName: return typeof(PredSourceInfo);
                case PredCompareInfo.TypeTypeName: return typeof(PredCompareInfo);
                case TypeInfo.TypeTypeName: return typeof(TypeInfo);
                case MapInfo.TypeTypeName: return typeof(MapInfo);
                case MapValueInfo.TypeTypeName: return typeof(MapValueInfo);
                case Session.TypeTypeName: return typeof(Session);
                case Event.TypeTypeName: return typeof(Event);
                case XEvent.Action.TypeTypeName: return typeof(XEvent.Action);
                case EventField.TypeTypeName: return typeof(EventField);
                case Target.TypeTypeName: return typeof(Target);
                case TargetField.TypeTypeName: return typeof(TargetField);
            }

            return null;
        }

        /// <summary>
        /// Returns a Key object given a Urn fragment.
        /// </summary>
        /// <param name="urnFragment">A urn fragment.</param>
        /// <returns>An <see cref="SfcKey"/> for given Urn fragment.</returns>
        SfcKey ISfcDomain.GetKey(IUrnFragment urnFragment)
        {
            switch (urnFragment.Name)
            {
                case DatabaseXEStore.TypeTypeName: return new DatabaseXEStore.DatabaseKey(this);
                case Package.TypeTypeName: return new Package.Key(urnFragment.FieldDictionary);
                case EventInfo.TypeTypeName: return new EventInfo.Key(urnFragment.FieldDictionary);
                case EventColumnInfo.TypeTypeName: return new EventColumnInfo.Key(urnFragment.FieldDictionary);
                case DataEventColumnInfo.TypeTypeName: return new DataEventColumnInfo.Key(urnFragment.FieldDictionary);
                case ReadOnlyEventColumnInfo.TypeTypeName: return new ReadOnlyEventColumnInfo.Key(urnFragment.FieldDictionary);
                case ActionInfo.TypeTypeName: return new ActionInfo.Key(urnFragment.FieldDictionary);
                case TargetInfo.TypeTypeName: return new TargetInfo.Key(urnFragment.FieldDictionary);
                case TargetColumnInfo.TypeTypeName: return new TargetColumnInfo.Key(urnFragment.FieldDictionary);
                case PredSourceInfo.TypeTypeName: return new PredSourceInfo.Key(urnFragment.FieldDictionary);
                case PredCompareInfo.TypeTypeName: return new PredCompareInfo.Key(urnFragment.FieldDictionary);
                case TypeInfo.TypeTypeName: return new TypeInfo.Key(urnFragment.FieldDictionary);
                case MapInfo.TypeTypeName: return new MapInfo.Key(urnFragment.FieldDictionary);
                case MapValueInfo.TypeTypeName: return new MapValueInfo.Key(urnFragment.FieldDictionary);
                case Session.TypeTypeName: return new Session.Key(urnFragment.FieldDictionary);
                case Event.TypeTypeName: return new Event.Key(urnFragment.FieldDictionary);
                case XEvent.Action.TypeTypeName: return new XEvent.Action.Key(urnFragment.FieldDictionary);
                case EventField.TypeTypeName: return new EventField.Key(urnFragment.FieldDictionary);
                case Target.TypeTypeName: return new Target.Key(urnFragment.FieldDictionary);
                case TargetField.TypeTypeName: return new TargetField.Key(urnFragment.FieldDictionary);
            }

            throw new XEventException(ExceptionTemplates.UnsupportedKey(urnFragment.Name));
        }

        /// <summary>
        /// Gets the execution engine.
        /// </summary>
        /// <returns>The execution engine to use.</returns>
        ISfcExecutionEngine ISfcDomain.GetExecutionEngine()
        {
            return this.ExecutionEngine;
        }

        SfcTypeMetadata ISfcDomain.GetTypeMetadata(string typeName)
        {
            switch (typeName)
            {
                case Event.TypeTypeName:
                    return Event.GetTypeMetadata();
                case Target.TypeTypeName:
                    return Target.GetTypeMetadata();
                case XEvent.Action.TypeTypeName:
                    return XEvent.Action.GetTypeMetadata();
                case EventField.TypeTypeName:
                    return EventField.GetTypeMetadata();
                case TargetField.TypeTypeName:
                    return TargetField.GetTypeMetadata();
                case Session.TypeTypeName:
                case DatabaseXEStore.TypeTypeName:
                    return null;
                default:
                    throw new XEventException(ExceptionTemplates.InvalidParameter("typeName"));
            }
        }

        bool ISfcDomain.UseSfcStateManagement()
        {
            return true;    // XEvent uses SFC-provided state management
        }

        /// <summary>
        /// Returns the logical version of the domain.
        /// </summary>
        /// <returns>The logical version of the domain.</returns>
        int ISfcDomainLite.GetLogicalVersion()
        {
            // 1 = Katmai CTP5
            // 2 = Katmai CTP6
            return 3;      // logical version changes only when the schema of domain changes
        }

        /// <summary>
        /// Gets the name of the domain.
        /// </summary>
        /// <value>The name of the domain.</value>
        [SfcIgnore]
        string ISfcDomainLite.DomainName
        {
            get { return DatabaseXEStore.DomainName; }
        }

        /// <summary>
        /// Gets the name of the domain instance.
        /// </summary>
        /// <value>The name of the domain instance.</value>
        [SfcIgnore]
        string ISfcDomainLite.DomainInstanceName
        {
            get
            {
                if ((this as ISfcHasConnection).ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return this.Name;
                }
                else
                {
                    return this.Name;
                }
            }
        }

        #endregion

        private sealed class DatabaseKey : NamedDomainKey<DatabaseXEStore>
        {
            private string serverName;

            public DatabaseKey()
                : base(null) // Caller has to remember to set Root!
            {
            }

            public DatabaseKey(ISfcDomain domain)
                : base(domain)
            {
            }

            public DatabaseKey(ISfcDomain domain, string name)
                : base(domain, name)
            {
            }

            public DatabaseKey(ISfcDomain domain, string name, string serverName)
                : base(domain, name)
            {
                this.serverName = serverName;
            }

            public override string GetUrnFragment()
            {
                // SessionStore has valid name only in connected mode, so we add the Name attribute only in that case
                if (this.Domain.ConnectionContext.Mode == SfcConnectionContextMode.Offline)
                {
                    return DatabaseXEStore.TypeTypeName;
                }

                string urn = DatabaseXEStore.TypeTypeName + "[@Name='" + SfcTsqlProcFormatter.SqlString(this.Name) + "' and @ServerName='" + SfcTsqlProcFormatter.SqlString(this.serverName) + "']";
                return urn;
            }

            protected override string UrnName
            {
                get
                {
                    return DatabaseXEStore.TypeTypeName;
                }
            }
        }

        #region Public properties

        /// <summary>
        /// Gets the name of XEStore.
        /// </summary>
        public override string Name
        {
            get
            {
                return this.name;
            }

            protected set
            {
                if (this.name != null)
                {
                    throw new XEventException(ExceptionTemplates.CannotSetNameForExistingObject);
                }
                else
                {
                    if (String.IsNullOrEmpty(value))
                    {
                        throw new XEventException(ExceptionTemplates.NameNullEmpty);
                    }

                    this.name = value;
                }
            }
        }

        /// <summary>
        /// Gets ServerName for the store.
        /// </summary>
        [SfcProperty(Data = true)]
        public override string ServerName
        {
            get { return this.StoreProvider.DomainInstanceName; }
        }

        #endregion
        

        /// <summary>
        /// Creates a key identifying the store.
        /// </summary>
        /// <returns>A key identifying the store.</returns>
        protected override SfcKey CreateIdentityKey()
        {
            return new DatabaseKey(this, this.name, this.ServerName);
        }

        /// <summary>
        /// Gets provider to perform operations on the Store.
        /// </summary>
        /// <returns>The provider to use.</returns>
        protected override IXEStoreProvider GetStoreProvider()
        {
            return new DatabaseXEStoreProvider(this);
        }

        /// <summary>
        /// Gets provider to perform Session operations.
        /// </summary>
        /// <param name="session">A session to use.</param>
        /// <returns>The provider to use.</returns>
        protected override ISessionProvider GetSessionProivder(Session session)
        {
            return new DatabaseSessionProvider(session);
        }

        /// <summary>
        /// Gets provider to perform Target operations.
        /// </summary>
        /// <param name="target">A target to use.</param>
        /// <returns>The provider to use.</returns>
        protected override ITargetProvider GetTargetProvider(Target target)
        {
            return new DatabaseTargetProvider(target);
        }

        /// <summary>
        /// Gets provider to perform Event operations.
        /// </summary>
        /// <param name="xevent">An event to use.</param>
        /// <returns>The provider to use.</returns>
        protected override IEventProvider GetEventProvider(Event xevent)
        {
            return new DatabaseEventProvider(xevent);
        }
    }
}
