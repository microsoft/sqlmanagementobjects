// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management;
using System.Text;
using Microsoft.SqlServer.Management.Smo.Broker;
using ManagementPropertyData = System.Management.PropertyData;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{


    /// <summary>
    /// Handler prototype for all server events.
    /// </summary>
    public delegate void ServerEventHandler(object sender, ServerEventArgs e);

    /// <summary>
    /// Argument class used by ServerEventHandler.
    /// </summary>
    public sealed class ServerEventArgs : EventArgs
    {
    #region Constructor
        internal ServerEventArgs(EventType eventType, PropertyDataCollection properties)
        {
            this.eventType = eventType;
            this.properties = new EventPropertyCollection(properties);
            this.properties.Add("EventClass", this.eventType);
        }
    #endregion

    #region Public interface
        public EventType EventType
        {
            get { return eventType; }
        }

        public DateTime PostTime
        {
            get { return (DateTime)this.properties["PostTime"].Value; }
        }

        public int Spid
        {
            get { return (int)this.properties["SPID"].Value; }
        }

        public string SqlInstance
        {
            get { return (string)this.properties["SQLInstance"].Value; }
        }

        public EventPropertyCollection Properties
        {
            get { return this.properties; }
        }
    #endregion

    #region Implementation
        private EventPropertyCollection properties;
        private EventType eventType;
    #endregion
    }

    /// <summary>
    /// Class that handles subscribing and routing
    /// of all server events. Used by numer of public
    /// SMO classes.
    /// </summary>
    internal abstract class EventsWorkerBase : IDisposable
    {
        #region Construction
        internal EventsWorkerBase(SqlSmoObject target, Type eventSetType, Type eventEnumType)
        {
            Debug.Assert(target != null, "Must have a target");
            Debug.Assert(target.GetServerObject() != null, "Target object must belong to a server");

            Debug.Assert(eventSetType != null);
            Debug.Assert(eventEnumType != null);
            Debug.Assert(eventSetType.IsSubclassOf(typeof(EventSetBase)), "eventSetType type must inherit from EventSetBase");
            Debug.Assert(eventEnumType.IsSubclassOf(typeof(Enum)), "eventEnumType type must inherit from Enum");

            // This may throw an exception if target is in Pending state
            Server server = target.GetServerObject();

            // Check for correct server version
            if (server.ServerVersion.Major < 9)
            {
                throw new InvalidVersionSmoOperationException(server.ServerVersion);
            }

            this.eventEnumType = eventEnumType;
            this.events = (EventSetBase)Activator.CreateInstance(eventSetType);

            // Create managed scope but do not connect yet
            this.managementScope = server.Events.ManagementScope;
        }
        #endregion

        #region Public interface
        /// <summary>
        /// Returns currrent selection of events.
        /// </summary>
        public EventSetBase GetEventSelection()
        {
            return this.events.Copy();
        }

        public void AddDefaultEventHandler(ServerEventHandler eventHandler)
        {
            this.eventHandlers.AddHandler(EventsWorkerBase.defaultEventHandlerKey, eventHandler);
        }

        public void RemoveDefaultEventHandler(ServerEventHandler eventHandler)
        {
            this.eventHandlers.RemoveHandler(EventsWorkerBase.defaultEventHandlerKey, eventHandler);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Target">SMO object that is a target of the subscription.</param>
        /// <param name="events"></param>
        /// <param name="handler"></param>
        public void SubscribeToEvents(EventSetBase addEvents, ServerEventHandler eventHandler)
        {
            Debug.Assert(addEvents.GetType() == this.events.GetType(), "Must subscribe to correct type of events");

            try
            {
                // Find object key for the eventHandler
                object eventHandlerKey = GetEventHandlerKey(eventHandler);

                Debug.Assert(null != eventHandlerKey);

                // For each event in event set create or update our subscription
                for (int eventID = 0; eventID < addEvents.NumberOfElements; ++eventID)
                {
                    if (addEvents.GetBitAt(eventID))
                    {
                        // User requested subsciption for that event.
                        // Check if we have one already
                        string eventClass = GetEventClass(eventID);

                        EventSubscription subscription = (EventSubscription)eventSubscriptions[eventClass];
                        if (null != subscription)
                        {
                            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                                "Updating event handler for event " + eventClass + " on class " + this.Target.GetType().Name);

                            // We do not subscribe the second time,
                            // just update the handler
                            subscription.EventHandlerKey = eventHandlerKey;
                        }
                        else
                        {
                            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                                "Adding subscription for event " + eventClass + " on class " + this.Target.GetType().Name);

                            // Create a new subscription
                            CreateSubscription(eventID, eventClass, eventHandlerKey);
                            // Select this event in our events selection list
                            this.events.SetBitAt(eventID, true);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw new FailedOperationException(ExceptionTemplates.CannotSubscribe, this, exception);
            }
        }

        public void UnsubscribeFromEvents(EventSetBase removeEvents)
        {
            // For each event in event set remove a subscription
            for (int eventID = 0; eventID < removeEvents.NumberOfElements; ++eventID)
            {
                if (removeEvents.GetBitAt(eventID))
                {
                    string eventClass = GetEventClass(eventID);

                    EventSubscription subscription = (EventSubscription)eventSubscriptions[eventClass];
                    if (null != subscription)
                    {
                        Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                            "Removing subscription for event " + eventClass + " on class " + this.Target.GetType().Name);

                        // Remove subscription
                        this.eventSubscriptions.Remove(eventClass);
                        subscription.Dispose();

                        // Deselect this event in our events selection list
                        this.events.SetBitAt(eventID, false);
                    }
                    // else ... ignore the event, nothing to remove
                }
            }
        }

        public void StartEvents()
        {
            if (!this.eventsStarted)
            {
                try
                {
                    foreach (EventSubscription subscription in this.eventSubscriptions.Values)
                    {
                        Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                            "Starting event subscription on: {0}, query: {1}", subscription.EventWatcher.Scope.Path, subscription.EventWatcher.Query.QueryString);

                        subscription.EventWatcher.Start();
                    }
                    this.eventsStarted = true;
                }
                catch (Exception exception)
                {
                    throw new FailedOperationException(ExceptionTemplates.CannotStartSubscription, this, exception);
                }
            }
        }

        public void StopEvents()
        {
            if (this.eventsStarted)
            {
                foreach (EventSubscription subscription in this.eventSubscriptions.Values)
                {
                    subscription.EventWatcher.Stop();
                }
                this.eventsStarted = false;
            }
        }

        #region IDisposable Members
        public void Dispose()
        {

            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways, "Removing all subscriptions");

            // Stop all subscriptions and release all resources
            foreach (EventSubscription subscription in this.eventSubscriptions.Values)
            {
                subscription.Dispose();
            }

            this.eventSubscriptions.Clear();
            this.eventHandlers.Dispose();

            // No need for finalizer, since it would be empty
            // All objects to release are external objects
        }
        #endregion
        #endregion

        #region Implementation

        protected abstract SqlSmoObject Target { get; }

        protected abstract EventQuery CreateWqlQuery(string eventClass);

        /// <summary>
        /// Search through all event handlers and check if 
        /// we do not have that one already. If so return its
        /// key object, otherwise add new handler to the collection.
        /// </summary>
        /// <param name="eventHandler">A handler to find</param>
        /// <returns>An object that is used as a key to identify the handler.</returns>
        private object GetEventHandlerKey(ServerEventHandler eventHandler)
        {
            if (eventHandler != null)
            {
                object eventHandlerKey = eventHandlers.FindHandler(eventHandler);
                if (eventHandlerKey == null)
                {
                    // Add the handler
                    eventHandlerKey = new object();
                    eventHandlers.AddHandler(eventHandlerKey, eventHandler);
                }
                return eventHandlerKey;
            }
            else
            {
                return EventsWorkerBase.defaultEventHandlerKey;
            }
        }

        protected virtual string GetEventClass(int eventID)
        {
            StringBuilder eventClass = new StringBuilder(Enum.GetName(this.eventEnumType, eventID));
            return ConvertToEventClass(eventClass);
        }

        protected static string ConvertToEventClass(StringBuilder eventName)
        {
            // Convert enum name to the event class name using the following algorithm:
            // 1) Replace DB with Db so it stays as one word
            // 2) Insert _ before each capital letter
            // 3) Replace "Trace" with "Trc"
            // 4) Uppercase all letters

            eventName.Replace("DB", "Db");

            int position = 1; // Skip the first character
            while (position < eventName.Length)
            {
                if (char.IsUpper(eventName[position]) || char.IsDigit(eventName[position]))
                {
                    eventName.Insert(position++, '_');
                }
                ++position;
            }

            eventName.Replace("Trace", "Trc");
            return eventName.ToString().ToUpper(CultureInfo.InvariantCulture);
        }

        protected static EventType ConvertFromEventClass(string eventClass)
        {
            Debug.Assert(eventClass != null && eventClass.Length > 0);

            // Convert event class name to corresponding value of EventType enum
            // 1) Remove all _
            // 2) Replace "Trc" with "Trace"
            // 3) Match a resulting string with enum value, using case-insensitive comparison

            StringBuilder eventName = new StringBuilder(eventClass);

            int position = 1; // Skip the first character
            while (position < eventName.Length)
            {
                if (eventName[position] == '_')
                {
                    eventName.Remove(position, 1);
                }
                ++position;
            }

            eventName.Replace("Trc", "Trace");
            return (EventType)Enum.Parse(typeof(EventType), eventName.ToString(), true);
        }

        private void CreateSubscription(int eventID, string eventClass, object eventHandlerKey)
        {
            //
            // Create a WQL query 
            //
            EventQuery query = CreateWqlQuery(eventClass);

            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                "Subscription query {0}", query.QueryString);

            // 
            // Connect to WMI service and create a watcher object for 
            // the query we have
            //
            TryConnect();
            Debug.Assert(this.managementScope.IsConnected, "Must be connected at this time");

            ManagementEventWatcher watcher = new ManagementEventWatcher(this.managementScope, query);
            watcher.EventArrived += new EventArrivedEventHandler(OnEventArrived);

            EventSubscription subscription = new EventSubscription(watcher, eventHandlerKey);

            // Start listening if needed
            if (this.eventsStarted)
            {
                watcher.Start();
            }

            // Once everything succeeded, add subscription to our collection
            this.eventSubscriptions.Add(eventClass, subscription);
        }

        /// <summary>
        /// This is the main event handler that recieves all WMI events
        /// and dispatches then to other event handlers based on 
        /// the event class name.
        /// Note that this method is usually called on a background thread,
        /// different from the one that creates all the subscriptions.
        /// User's event handler will be also called on that thread.
        /// eventSubscriptions and eventHandler collections are protected
        /// agains multi-thread use like in this scenario (one writer, multiple
        /// readers).
        /// </summary>
        /// <param name="sender">Sender object. This is ManagementEventWatcher object.</param>
        /// <param name="args">Arument class that contains the event.</param>
        private void OnEventArrived(object sender, EventArrivedEventArgs args)
        {
            // Use event class name to find the scubscritpion
            string subscriptionKey = args.NewEvent.ClassPath.ClassName;
            EventType eventType = ConvertFromEventClass(args.NewEvent.ClassPath.ClassName);
            EventSubscription subscription = null;

            Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                "Received event {0} on class {1}", args.NewEvent.ClassPath.ClassName, this.Target.GetType().Name);

            while (subscriptionKey != null && subscriptionKey.Length > 0)
            {
                subscription = (EventSubscription)this.eventSubscriptions[subscriptionKey];
                if (null != subscription)
                {
                    // Find the correct event handler to use for this event
                    ServerEventHandler eventHandler = (ServerEventHandler)this.eventHandlers[subscription.EventHandlerKey];
                    if (null != eventHandler)
                    {
                        Diagnostics.TraceHelper.Trace(SmoApplication.ModuleName, SmoApplication.trAlways,
                            "Raising event {0} on subscription: {1}", args.NewEvent.ClassPath.ClassName, subscriptionKey);

                        eventHandler(this.Target, new ServerEventArgs(eventType, args.NewEvent.Properties));
                    }
                }

                /// REVIEW-2003/10/28-macies Commented out server event groups
                /// Commented out until we figured out what to do with server event groups
                // Go up in hierarchy and see if there are event groups we are subscribed to
                // subscriptionKey = ServerEventsGroupMapping.GetParent(subscriptionKey);
                subscriptionKey = null;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1806:DoNotIgnoreMethodResults", MessageId = "System.Management.ManagementObject")]
        private void TryConnect()
        {
            if (!this.managementScope.IsConnected)
            {
                try
                {
                    this.managementScope.Connect();

                    // Double-check that provider exists.
                    ManagementPath path = new ManagementPath("Win32Provider.Name=\"MSSQL_ManagementProvider\"");
                    new ManagementObject(this.managementScope, path, new ObjectGetOptions());
                }
                catch (ManagementException managementException)
                {
                    if (managementException.ErrorCode == ManagementStatus.InvalidNamespace ||
                        managementException.ErrorCode == ManagementStatus.ProviderLoadFailure)
                    {
                        // This error means that WMI provider for SQL Server is not installed on
                        // the server.
                        // Provide a more user-friendly message in that case. 
                        throw new SmoException(ExceptionTemplates.WMIProviderNotInstalled(this.Target.GetServerObject().Name), managementException);
                    }
                    else
                    {
                        // It will be wrapped in the caller method
                        throw;
                    }
                }
                // Ignore other exceptions, they will be catched in caller method
            }
        }

        private sealed class EventSubscription : IDisposable
        {
            public EventSubscription(ManagementEventWatcher eventWatcher, object eventHandlerKey)
            {
                this.EventWatcher = eventWatcher;
                this.EventHandlerKey = eventHandlerKey;
            }

        #region IDisposable Members
            public void Dispose()
            {
                if (null != this.EventWatcher)
                {
                    this.EventWatcher.Stop();
                    this.EventWatcher.Dispose();
                }

                // No finalizer requiored for this class
                // since it would do nothing.
            }
        #endregion

            public readonly ManagementEventWatcher EventWatcher;
            public object EventHandlerKey;
        }

        internal static EventQuery CreateWqlQueryForServer(string eventClass)
        {
            return new EventQuery(string.Format(SmoApplication.DefaultCulture, "SELECT * FROM {0}", eventClass));
        }

        internal static EventQuery CreateWqlQueryForDatabase(string eventClass, string databaseName)
        {
            return new EventQuery(string.Format(SmoApplication.DefaultCulture, "SELECT * FROM {0} WHERE DatabaseName = '{1}'",
                eventClass, EscapeWqlParameter(databaseName)));
        }

        internal static EventQuery CreateWqlQueryForDatabaseObject(string eventClass, string databaseName, string objectName, string objectType)
        {
            return new EventQuery(
                string.Format(SmoApplication.DefaultCulture, "SELECT * FROM {0} WHERE DatabaseName = '{1}' AND ObjectName = '{2}' AND ObjectType = '{3}'",
                eventClass, EscapeWqlParameter(databaseName), EscapeWqlParameter(objectName), objectType));
        }

        internal static EventQuery CreateWqlQueryForTargetObject(string eventClass, string databaseName, string schemaName, string objectType, string targetObjectName, string targetObjectType)
        {
            return new EventQuery(
                string.Format(SmoApplication.DefaultCulture, "SELECT * FROM {0} WHERE DatabaseName = '{1}' AND SchemaName = '{2}' AND ObjectType = '{3}' AND TargetObjectName = '{4}' AND TargetObjectType='{5}'",
                eventClass, EscapeWqlParameter(databaseName), EscapeWqlParameter(schemaName), objectType, EscapeWqlParameter(targetObjectName), targetObjectType));

        }

        internal static EventQuery CreateWqlQueryForSourceObject(string eventClass, string databaseName, string schemaName, string objectName, string objectType)
        {
            return new EventQuery(
                string.Format(SmoApplication.DefaultCulture, "SELECT * FROM {0} WHERE DatabaseName = '{1}' AND SchemaName = '{2}' AND ObjectName = '{3}' AND ObjectType='{4}'",
                eventClass, EscapeWqlParameter(databaseName), EscapeWqlParameter(schemaName), EscapeWqlParameter(objectName), objectType));

        }

        private static string EscapeWqlParameter(string parameter)
        {
            return SqlSmoObject.EscapeString(SqlSmoObject.EscapeString(parameter, '\\'), '\'');
        }

        private EventHandlerList eventHandlers = new EventHandlerList();
        private Hashtable eventSubscriptions = new Hashtable();

        private Type eventEnumType;      // An enum type used by this ServerEventWorker
        private EventSetBase events;             // Current set of events we are subscribed to
        private bool eventsStarted;      // Indicates if events should be started
        private ManagementScope managementScope;    // Our WMI connection

        private static readonly object defaultEventHandlerKey = new object();
        #endregion
    }

    ///
    /// Specialized EventsWorker classes
    /// 


    /// <summary>
    /// Specialized class for Server object events.
    /// </summary>
    internal sealed class ServerEventsWorker : EventsWorkerBase
    {
        #region Construction
        public ServerEventsWorker(Server target, Type eventSetType, Type eventEnumType)
            : base(target, eventSetType, eventEnumType)
        {
            this.target = target;
        }
        #endregion

        #region Overriden methods
        protected override SqlSmoObject Target
        {
            get { return this.target; }
        }

        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            return CreateWqlQueryForServer(eventClass);
        }
        #endregion

        #region Implementation
        private Server target;
        #endregion
    }

    /// <summary>
    /// Specialized class for Database object events.
    /// </summary>
    internal sealed class DatabaseEventsWorker : EventsWorkerBase
    {
        #region Constructors
        public DatabaseEventsWorker(Database target)
            : base(target, typeof(DatabaseEventSet), typeof(DatabaseEventValues))
        {
            this.target = target;
        }
        #endregion

        #region Override methods
        protected override SqlSmoObject Target
        {
            get { return this.target; }
        }

        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            return CreateWqlQueryForDatabase(eventClass, this.target.Name);
        }
        #endregion

        #region Implementation
        private Database target;
        #endregion
    }

    internal abstract class ObjectInSchemaEventsWorker : EventsWorkerBase
    {
        #region Constructor
        internal ObjectInSchemaEventsWorker(ScriptSchemaObjectBase target, Type eventSetType, Type eventEnumType)
            : base(target, eventSetType, eventEnumType)
        {
            this.target = target;
        }
        #endregion

        #region Override methods
        protected override SqlSmoObject Target
        {
            get { return this.target; }
        }

        protected virtual string ObjectType
        {
            get { return this.target.GetType().Name; }
        }

        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            return CreateWqlQueryForSourceObject(eventClass,
                this.target.ParentColl.ParentInstance.InternalName, this.target.Schema, this.target.Name, this.ObjectType);
        }
        #endregion

        #region Implementation
        private ScriptSchemaObjectBase target;
        #endregion
    }

    internal sealed class TableEventsWorker : ObjectInSchemaEventsWorker
    {
        #region Constructor
        public TableEventsWorker(Table target)
            : base(target, typeof(TableEventSet), typeof(TableEventValues))
        {
        }
        #endregion

        #region Overriden methods
        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            bool targetObjectQuery = false;
            string objectType = string.Empty;

            if (eventClass.IndexOf("_INDEX", StringComparison.Ordinal) > 0)
            {
                targetObjectQuery = true;
                objectType = "Index";
            }
            else if (eventClass.IndexOf("_STATISTICS", StringComparison.Ordinal) > 0)
            {
                targetObjectQuery = true;
                objectType = "Statistics";
            }

            if (targetObjectQuery)
            {
                Table target = (Table)this.Target;

                return CreateWqlQueryForTargetObject(eventClass,
                    target.ParentColl.ParentInstance.InternalName, // databaseName
                    target.Schema,
                    objectType,
                    target.Name,
                    "TABLE");
            }
            else
            {
                return base.CreateWqlQuery(eventClass);
            }
        }
        #endregion
    }

    internal sealed class ViewEventsWorker : ObjectInSchemaEventsWorker
    {
        #region Constructor
        public ViewEventsWorker(View target)
            : base(target, typeof(ViewEventSet), typeof(ViewEventValues))
        {
        }
        #endregion

        #region Overriden methods
        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            bool targetObjectQuery = false;
            string objectType = string.Empty;

            if (eventClass.IndexOf("_INDEX", StringComparison.Ordinal) > 0)
            {
                targetObjectQuery = true;
                objectType = "Index";
            }
            else if (eventClass.IndexOf("_STATISTICS", StringComparison.Ordinal) > 0)
            {
                targetObjectQuery = true;
                objectType = "Statistics";
            }

            if (targetObjectQuery)
            {
                View target = (View)this.Target;

                return CreateWqlQueryForTargetObject(eventClass,
                    target.ParentColl.ParentInstance.InternalName, // databaseName
                    target.Schema,
                    objectType,
                    target.Name,
                    "VIEW");
            }
            else
            {
                return base.CreateWqlQuery(eventClass);
            }
        }
        #endregion
    }

    internal sealed class UserDefinedFunctionEventsWorker : ObjectInSchemaEventsWorker
    {
        #region Constructor
        public UserDefinedFunctionEventsWorker(UserDefinedFunction target)
            : base(target, typeof(UserDefinedFunctionEventSet), typeof(UserDefinedFunctionEventValues))
        {
        }
        #endregion

        #region Overriden methods
        protected override string ObjectType
        {
            get { return "Function"; }
        }

        #endregion
    }

    internal sealed class StoredProcedureEventsWorker : ObjectInSchemaEventsWorker
    {
        #region Constructor
        public StoredProcedureEventsWorker(StoredProcedure target)
            : base(target, typeof(StoredProcedureEventSet), typeof(StoredProcedureEventValues))
        {
        }
        #endregion

        #region Overriden methods
        protected override string ObjectType
        {
            get { return "Procedure"; }
        }
        #endregion
    }

    internal sealed class ServiceQueueEventsWorker : ObjectInSchemaEventsWorker
    {
        #region Constructor
        public ServiceQueueEventsWorker(ServiceQueue target)
            : base(target, typeof(ServiceQueueEventSet), typeof(ServiceQueueEventValues))
        {
        }
        #endregion

        #region Overriden methods
        protected override string ObjectType
        {
            get { return "Queue"; }
        }

        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            ServiceQueue queue = (ServiceQueue)this.Target;

            return CreateWqlQueryForSourceObject(eventClass,
                queue.Parent.Parent.Name, queue.Schema, queue.Name, this.ObjectType);
        }
        #endregion
    }

    internal sealed class SqlAssemblyEventsWorker : EventsWorkerBase
    {
        #region Constructor
        public SqlAssemblyEventsWorker(SqlAssembly target)
            : base(target, typeof(SqlAssemblyEventSet), typeof(SqlAssemblyEventValues))
        {
            this.target = target;
        }
        #endregion

        #region Override methods
        protected override SqlSmoObject Target
        {
            get { return this.target; }
        }

        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            return CreateWqlQueryForDatabaseObject(eventClass,
                this.target.ParentColl.ParentInstance.InternalName, // database name
                this.target.Name,
                "Assembly");
        }
        #endregion

        #region Implementation
        private SqlAssembly target;
        #endregion
    }

    internal sealed class ObjectEventsWorker : EventsWorkerBase
    {
        #region Constructor
        public ObjectEventsWorker(SqlSmoObject target)
            : base(target, typeof(ObjectEventSet), typeof(ObjectEventValues))
        {
            this.target = target;
        }
        #endregion

        #region Override methods
        protected override SqlSmoObject Target
        {
            get { return this.target; }
        }

        /// <summary>
        /// This override provides a special handling of
        /// event class name for 'Object' enum values.
        /// In this case event class name is concatenated
        /// from target class name and DROP_ or ALTER_
        /// prefix.
        /// </summary>
        protected override string GetEventClass(int eventID)
        {
            StringBuilder eventName = new StringBuilder();

            // 
            // Get the prefix
            //
            switch (eventID)
            {
                case (int)ObjectEventValues.Alter:
                    eventName.Append("Alter");
                    break;

                case (int)ObjectEventValues.Drop:
                    eventName.Append("Drop");
                    break;

                default:
                    Debug.Fail("Unknown event ID");
                    break;
            }

            //
            // Get the name of the object.
            // In some special cases 'Alter' event is 
            // not provided. We throw exception then.
            //
            string typeName = this.target.GetType().Name;
            switch (typeName)
            {
                case "UserDefinedType":
                    if (eventID == (int)ObjectEventValues.Alter)
                    {
                        throw new SmoException(ExceptionTemplates.NotSupportedNotification(typeName, "Alter"));
                    }

                    typeName = "Type";
                    break;

                case "ServerRole":
                    typeName = "Role";
                    break;

                case "Statistic":
                    if (eventID == (int)ObjectEventValues.Alter)
                    {
                        return "UPDATE_STATISTICS"; // this is very special case, so we just return it
                    }
                    // hopefuly it will be changed on the server side
                    typeName = "Statistics";
                    break;

                case "Synonym":
                    if (eventID == (int)ObjectEventValues.Alter)
                    {
                        throw new SmoException(ExceptionTemplates.NotSupportedNotification(typeName, "Alter"));
                    }

                    break;               

                case "BrokerService":
                    typeName = "Service";
                    break;

                case "ServiceContract":
                    typeName = "Contract";
                    break;

                case "ServiceRoute":
                    typeName = "Route";
                    break;

                // default:
                // for all other classes keep the typeName as it is
            }
            eventName.Append(typeName);

            return ConvertToEventClass(eventName);
        }

        protected override EventQuery CreateWqlQuery(string eventClass)
        {
            EventQuery query = null;

            string typeName = this.target.GetType().Name;
            switch (typeName)
            {
                // Server level events
                case "Certificate":
                case "Login":
                case "ServerRole":

                case "Endpoint":
                    query = CreateWqlQueryForServer(eventClass);
                    break;

                // Database level events
                case "ApplicationRole":
                case "UserDefinedType":
                case "PartitionFunction":
                case "PartitionScheme":
                case "Schema":
                case "Synonym":
                case "Sequence":
                case "Trigger":
                case "User":

                // Service Broker objects
                case "BrokerService":
                case "BrokerPriority":
                case "MessageType":
                case "RemoteServiceBinding":
                case "ServiceContract":
                case "ServiceRoute":
                    query = CreateWqlQueryForDatabase(eventClass, this.target.GetDBName());
                    break;

                // View or Table level events
                case "Index":
                    {
                        TableViewBase tableView = (TableViewBase)this.target.ParentColl.ParentInstance;

                        query = CreateWqlQueryForTargetObject(eventClass,
                            tableView.ParentColl.ParentInstance.InternalName,
                            tableView.Schema,
                            "Index",
                            tableView.Name,
                            (tableView is Table) ? "TABLE" : "VIEW");

                    }
                    break;
                case "Statistic":
                    {
                        TableViewBase tableView = (TableViewBase)this.target.ParentColl.ParentInstance;

                        query = CreateWqlQueryForTargetObject(eventClass,
                            tableView.ParentColl.ParentInstance.InternalName,
                            tableView.Schema,
                            "Statistics",
                            tableView.Name,
                            (tableView is Table) ? "TABLE" : "VIEW");
                    }
                    break;

                default:
                    {
                        Debug.Fail(string.Format(SmoApplication.DefaultCulture, "Unknown object type: {0}", typeName));
                    }
                    break;

            }
            return query;
        }

        #endregion

        #region Implementation
        SqlSmoObject target;
        #endregion
    }

    ///
    /// Collection classes
    ///

    /// <summary>
    /// A name-value type of class that holds a single event property.
    /// </summary>
    public sealed class EventProperty
    {
        #region Construction
        /// <summary>
        /// Default constructor
        /// </summary>
        internal EventProperty(string name, object value)
        {
            this.name = name;
            this.value = value;
        }
        #endregion

        #region Public interface
        public string Name
        {
            get { return this.name; }
        }

        public object Value
        {
            get { return this.value; }
        }
        #endregion

        #region Implementation
        private string name;
        private object value;
        #endregion
    }

    /// <summary>
    /// A specialized name-object collection (dictionary)
    /// used for event properties.
    /// </summary>
    public sealed class EventPropertyCollection : ICollection, IEnumerable, IEnumerable<EventProperty>
    {
        #region Constructors
        /// <summary>
        /// Internal constructor that fills in the collection from the
        /// WMI property data collection.
        /// </summary>
        /// <param name="properties"></param>
        internal EventPropertyCollection(PropertyDataCollection properties)
        {
            this.collection = new Collection();

            foreach (ManagementPropertyData property in properties)
            {
                switch (property.Name)
                {
                    case "PostTime":
                        this.collection.Add(new EventProperty(property.Name, ConvertToDateTime((string)property.Value)));
                        break;
                    default:
                        this.collection.Add(new EventProperty(property.Name, property.Value));
                        break;
                }
            }
        }
        #endregion

        #region Public interface
        public EventProperty this[int index]
        {
            get { return this.collection[index]; }
        }

        public EventProperty this[string name]
        {
            get { return this.collection[name]; }
        }

        public int Count
        {
            get { return this.collection.Count; }
        }

        public void CopyTo(EventProperty[] array, int index)
        {
            this.collection.CopyTo(array, index);
        }

        [CLSCompliant(false)]
        public IEnumerator<EventProperty> GetEnumerator()
        {
            return this.collection.GetEnumerator();
        }

        bool ICollection.IsSynchronized
        {
            get { return ((ICollection)(this.collection)).IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return ((ICollection)(this.collection)).SyncRoot; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)(this.collection)).CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)(this.collection)).GetEnumerator();
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Converts a date and time encoded in a string into
        /// a DateTime object.
        /// The string representation is as follows:
        ///     yyyymmddhhmmss.mls
        /// </summary>
        private static DateTime ConvertToDateTime(string dateTime)
        {
            char[] dtArray = dateTime.ToCharArray();
            IFormatProvider provider = CultureInfo.InvariantCulture;

            return new DateTime(Int32.Parse(new string(dtArray, 0, 4), NumberStyles.None, provider),   // year
                       Int32.Parse(new string(dtArray, 4, 2), NumberStyles.None, provider),   // month
                       Int32.Parse(new string(dtArray, 6, 2), NumberStyles.None, provider),   // day
                       Int32.Parse(new string(dtArray, 8, 2), NumberStyles.None, provider),   // hour
                       Int32.Parse(new string(dtArray, 10, 2), NumberStyles.None, provider),  // minute
                       Int32.Parse(new string(dtArray, 12, 2), NumberStyles.None, provider)); // second
        }

        internal void Add(string name, object value)
        {
            this.collection.Add(new EventProperty(name, value));
        }

        private class Collection : KeyedCollection<string, EventProperty>
        {
            protected override string GetKeyForItem(EventProperty item)
            {
                return item.Name;
            }
        }

        private Collection collection;
        #endregion
    }

    /// <summary>
    /// Provides a simple list of delegates.
    /// </summary>
    internal sealed class EventHandlerList : IDisposable
    {
        #region Public interface
        /// <summary>
        /// Gets or sets the delegate for the specified key.
        /// </summary>
        public Delegate this[object key]
        {
            get
            {
                ListEntry e = Find(key);
                if (e != null)
                {
                    return e.handler;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                ListEntry e = Find(key);
                if (e != null)
                {
                    e.handler = value;
                }
                else
                {
                    lock (this.syncRoot)
                    {
                        head = new ListEntry(key, value, head);
                    }
                }
            }
        }

        /// <summary>
        /// Combines a delegate with existing one for hte same key
        /// or adds a new one.
        /// </summary>
        /// <param name="key">A key that identifies a delegate</param>
        /// <param name="value">A new delegate value</param>
        public void AddHandler(object key, Delegate value)
        {
            ListEntry e = Find(key);
            if (e != null)
            {
                e.handler = Delegate.Combine(e.handler, value);
            }
            else
            {
                lock (this.syncRoot)
                {
                    head = new ListEntry(key, value, head);
                }
            }
        }

        /// <summary>
        /// Removes a delegate from the collection.
        /// </summary>
        /// <param name="key">A key that identifies a delegate</param>
        /// <param name="value">A delegate value to remove</param>
        public void RemoveHandler(object key, Delegate value)
        {
            ListEntry e = Find(key);
            if (e != null)
            {
                e.handler = Delegate.Remove(e.handler, value);
            }
        }

        /// <summary>
        /// Does reverse lookup, i.e. finds a key for the 
        /// delegate.
        /// </summary>
        /// <param name="value">A delegate to look for.</param>
        /// <returns>Returns a key that represents this delegate.</returns>
        public object FindHandler(Delegate value)
        {
            ListEntry found = null;

            lock (this.syncRoot)
            {
                found = head;
            }

            while (found != null)
            {
                if (found.handler.Equals(value))
                {
                    return found.key;
                }
                found = found.next;
            }
            return null;
        }

        /// <summary>
        /// Implements IDisposable interface
        /// </summary>
        public void Dispose()
        {
            lock (this.syncRoot)
            {
                head = null;
            }
            // Do not need to call SupressFinalize because we do not have a finalizer.
            // Finalizer is not required because it would be empty.
        }
        #endregion

        #region Implementation

        private ListEntry Find(object key)
        {
            ListEntry found = null;
            lock (this.syncRoot)
            {
                found = head;
            }

            while (found != null)
            {
                if (found.key == key)
                {
                    break;
                }
                found = found.next;
            }
            return found;
        }

        private object syncRoot = new object();
        private ListEntry head;

        private sealed class ListEntry
        {
            internal ListEntry next;
            internal object key;
            internal Delegate handler;

            public ListEntry(object key, Delegate handler, ListEntry next)
            {
                this.next = next;
                this.key = key;
                this.handler = handler;
            }
        }
        #endregion
    }
}
