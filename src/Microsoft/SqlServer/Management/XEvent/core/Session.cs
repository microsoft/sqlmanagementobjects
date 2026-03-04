// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;


namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Session is the main object user code interacts with.
    /// A Session object represents a row in sys.server_event_sessions and also includes some data from sys.dm_xe_sessions if it's started.
    /// </summary>
    public sealed class Session : SfcInstance, ISfcCreatable, ISfcAlterable, ISfcDroppable, ISfcValidate, ISfcDiscoverObject
    {
        /// <summary>
        ///  Enumeration of the list of all possible event capabilities
        /// </summary>
        [Flags]
        private enum EventCapabilities
        {
            None = 0x00,
            Private = 0x01,
            NoBlock = 0x400
        }

        // Define some constants for our property names for access by the session provider
        /// <summary>
        /// MaxMemory
        /// </summary>
        public const string MaxMemoryProperty = "MaxMemory";
        /// <summary>
        /// EventRetentionMode
        /// </summary>
        public const string EventRetentionModeProperty = "EventRetentionMode";
        /// <summary>
        /// MaxDispatchLatency
        /// </summary>
        public const string MaxDispatchLatencyProperty = "MaxDispatchLatency";
        /// <summary>
        /// MaxEventSize
        /// </summary>
        public const string MaxEventSizeProperty = "MaxEventSize";
        /// <summary>
        /// MemoryPartitionMode
        /// </summary>
        public const string MemoryPartitionModeProperty = "MemoryPartitionMode";
        /// <summary>
        /// TrackCausality
        /// </summary>
        public const string TrackCausalityProperty = "TrackCausality";
        /// <summary>
        /// AutoStart
        /// </summary>
        public const string AutoStartProperty = "AutoStart";
        /// <summary>
        /// MaxDuration
        /// </summary>
        public const string MaxDurationProperty = nameof(MaxDuration);
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "Session";

        private ISessionProvider sessionProvider = null;

        private ISessionProvider ProviderImpl
        {
            get
            {
                if (this.sessionProvider == null)
                {
                    this.sessionProvider = this.Parent.GetSessionProviderInternal(this);
                }

                return this.sessionProvider;
            }
        }        

        /// <summary>
        /// Default constructor
        /// </summary>
        public Session()
        {
        }

        /// <summary>
        /// Mostly used constructor
        /// </summary> 
        public Session(BaseXEStore parent, string name)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("constructor"))
            {
                tm.TraceParameterIn("parent", parent);
                tm.TraceParameterIn("name", name);

                base.Parent = parent;
                this.Name = name;
            }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new BaseXEStore Parent
        {
            get { return (BaseXEStore)base.Parent; }
            set { base.Parent = value; }
        }


        #region UIPropertyState

        private bool statesInitialized = false;

        /// <summary>
        /// Initializes the state of the UI property.
        /// </summary>
        protected override void InitializeUIPropertyState()
        {
            //Prevent the cyclic calling between this function and the SetEnabled function in case this one is called directly
            if (this.statesInitialized)
                return;
            this.statesInitialized = true;
            UpdateUIPropertyState();
        }



        /// <summary>
        /// Updates the state of the UI property.
        /// </summary>
        protected override void UpdateUIPropertyState()
        {
            /*
             * If the user is not interested in the states changes, then the InitializeUIPropertyState was not called before, and so
             * we are not interested in updating the states, so we won't update anything until the user is actually interested in doing this
             * which is done either by trying to get the enabled value, or setting it, or registering to the metadata changes event.
             * 
             */
            if (!this.statesInitialized)
                return;

            SfcProperty name = this.Properties["Name"];

            //States rules
            if (this.State == SfcObjectState.Pending)
                name.Enabled = true;
            else
                name.Enabled = false;
        }

        #endregion

        private void ValidateName(bool throwOnFirst, ValidationState validationState)
        {
            if (String.IsNullOrEmpty(this.Name))
            {
                TraceHelper.TraceContext.TraceError("Name can not be null or empty.");
                Exception exception = new XEventException(ExceptionTemplates.NameNullEmpty);
                if (throwOnFirst)
                {
                    throw exception;
                }
                else
                {
                    validationState.AddError(exception, "Name");
                }
            }
        }

        private void Validate(string methodName, bool throwOnFirst, ValidationState validationState)
        {
            ValidateName(throwOnFirst, validationState);
            Exception exception = null;

            // if the "event retention mode" of the session is "no event loss"
            if (this.EventRetentionMode == EventRetentionModeEnum.NoEventLoss)
            {
                foreach (Event evt in this.Events)
                {
                    if (!XEUtils.ToBeCreated(evt.State))
                        continue;
                    // if the event has "no_blocking"  capability 
                    if (((EventCapabilities)this.Parent.ObjectInfoSet.Get<EventInfo>(evt.ModuleID, evt.Name).Capabilities & EventCapabilities.NoBlock) != 0)
                    {
                        exception = new XEventException(ExceptionTemplates.NoBlockingEventNotAllowedInNoEventLossSession(this.Name, evt.Name));
                        if (throwOnFirst)
                        {
                            throw exception;
                        }
                        validationState.AddError(exception, "Events");
                    }
                }
            }


            if (methodName == ValidationMethod.Create)
            {
                // Technically we can create a session without any event or target. But current SQL parser
                // doesn't accept create event session statement without add event clause. So here we follow
                // the same rule. We'll remove this checking after the parser accepts create event session
                // statement without add event clause.
                if (this.Events.Count == 0)
                {
                    TraceHelper.TraceContext.TraceError("Cannot create a session without any event.");
                    exception = new XEventException(ExceptionTemplates.NewSessionMustContainEvent);
                    if (throwOnFirst)
                    {
                        throw exception;
                    }
                    validationState.AddError(exception, "EventCount");
                }
            }

            if (methodName == ValidationMethod.Alter)
            {
                if (this.State != SfcObjectState.Existing)
                {
                    exception = new XEventException(ExceptionTemplates.InvalidState(this.State, SfcObjectState.Existing));
                    if (throwOnFirst)
                    {
                        throw exception;
                    }
                    validationState.AddError(exception, "State");
                }
            }

        }




        /// <summary>
        /// Validates the specified method name.
        /// </summary>
        /// <param name="methodName">Name of the method, ValidationMethod.Create or ValidationMethod.Alter</param>
        /// <exception cref="XEventException">Validation failed.</exception>
        public void Validate(string methodName)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Validate(string)"))
            {
                tm.TraceParameterIn("methodName", methodName);
                if (methodName != ValidationMethod.Create && methodName != ValidationMethod.Alter)
                {
                    tm.TraceError("Invalid parameter.");
                    throw new XEventException(ExceptionTemplates.InvalidParameter(methodName));
                }

                // This cannot possibly happen in the UI
                // If it does happen we will throw there anyway the first time we access Parent
                if (null == this.Parent)
                {
                    tm.TraceError("Parent can't be null.");
                    throw new XEventException(ExceptionTemplates.ParentNull);
                }

                Validate(methodName, true, null);
            }
        }

        #region ISfcValidate members

        /// <summary>
        /// Validates the specified method name.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns></returns>
        public ValidationState Validate(string methodName, params object[] arguments)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Validate"))
            {
                tm.TraceParameterIn("methodName", methodName);
                tm.TraceParameterIn("arguments", arguments);

                ValidationState validationState = new ValidationState();
                Validate(methodName, false, validationState);

                return validationState;
            }
        }

        #endregion

        #region CRUD support
       
        /// <summary>
        /// Create the session in the back-end server.
        /// </summary>
        public void Create()
        {
            TraceHelper.TraceContext.TraceMethodEnter("Create");

            Validate(ValidationMethod.Create);
            base.CreateImpl();

            TraceHelper.TraceContext.TraceMethodExit("Create");
        }


        /// <summary>
        /// Script create for this session.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptCreate()
        {
            Validate(ValidationMethod.Create);
            return this.ProviderImpl.GetCreateScript();            
        }

        /// <summary>
        /// Alter the session in the back-end server.
        /// </summary>
        public void Alter()
        {
            TraceHelper.TraceContext.TraceMethodEnter("Alter");
            Validate(ValidationMethod.Alter);

            if (!this.IsDirty())
            {
                return;
            }

            this.ProviderImpl.ValidateAlter();
            base.AlterImpl();
            TraceHelper.TraceContext.TraceMethodExit("Alter");
        }

    
        /// <summary>
        /// Script alter for this session.
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptAlter()
        {
            Validate(ValidationMethod.Alter);
            return this.ProviderImpl.GetAlterScript();
        }      

        /// <summary>
        /// Drop the session in the back-end server.
        /// </summary>
        public void Drop()
        {
            TraceHelper.TraceContext.TraceMethodEnter("Drop");

            ValidateName(true, null);
            base.DropImpl();

            TraceHelper.TraceContext.TraceMethodExit("Drop");
        }

        /// <summary>
        /// Scripts drop for this session
        /// </summary>
        /// <returns></returns>
        public ISfcScript ScriptDrop()
        {
            ValidateName(true, null);
            return this.ProviderImpl.GetDropScript();
        }


        #endregion

        #region Start/stop Operations
        
        /// <summary>
        /// Starts this session.
        /// </summary>
        public void Start()
        {
            this.ValidateName(true, null);
            this.ProviderImpl.Start();
            this.IsRunning = true;
            SfcApplication.Events.OnObjectAltered(this, new SfcObjectAlteredEventArgs(this.Urn, this)); //UI needs to be notified object is changed to refresh the node
        }

        /// <summary>
        /// Stops this session.
        /// </summary>
        public void Stop()
        {
            ValidateName(true, null);
            this.ProviderImpl.Stop();
            this.IsRunning = false;
            SfcApplication.Events.OnObjectAltered(this, new SfcObjectAlteredEventArgs(this.Urn, this)); //UI needs to be notified object is changed to refresh the node
        }

        #endregion

        /// <summary>
        /// Creates the identity key.
        /// </summary>
        /// <returns></returns>
        protected override SfcKey CreateIdentityKey()
        {
            Key key = null;
            //Can't create a key without a key value
            if (this.Name != null)
            {
                key = new Key(this.Name);
            }
            return key;
        }

        /// <summary>
        /// Gets the identity key.
        /// </summary>
        /// <value>The identity key.</value>
        /// <returns></returns>
        [SfcIgnore]
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// Gets the child collection.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns></returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case Event.TypeTypeName:
                    return this.Events;
                case Target.TypeTypeName:
                    return this.Targets;
                default:
                    TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
                    throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
            }
        }




        /// <summary>
        /// Internal used Key class
        /// </summary>
        public sealed class Key : SfcKey
        {
            /// <summary>
            /// Properties
            /// </summary>
            private string keyName;

            /// <summary>
            /// Default constructor for generic Key generation
            /// </summary>
            public Key()
            {
            }

            /// <summary>
            /// Constructors
            /// </summary>
            /// <param name="other"></param>
            public Key(Key other)
            {
                if (other == null)
                {
                    throw new ArgumentNullException("other");
                }
                keyName = other.Name;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="name"></param>
            public Key(string name)
            {
                keyName = name;
            }

            /// <summary>
            /// 
            /// </summary>
            public string Name
            {
                get
                {
                    return keyName;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Key"/> class.
            /// </summary>
            /// <param name="filedDict">A set of name-value pairs that represent Urn fragment.</param>
            public Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                keyName = (string)filedDict["Name"];
            }

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                return this == obj;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj1"></param>
            /// <param name="obj2"></param>
            /// <returns></returns>
            public new static bool Equals(object obj1, object obj2)
            {
                return (obj1 as Key) == (obj2 as Key);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <returns></returns>
            public override bool Equals(SfcKey key)
            {
                return this == key;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(object obj, Key rightOperand)
            {
                if (obj == null || obj is Key)
                    return (Key)obj == rightOperand;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, object obj)
            {
                if (obj == null || obj is Key)
                    return leftOperand == (Key)obj;
                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator ==(Key leftOperand, Key rightOperand)
            {
                // If both are null, or both are same instance, return true.
                if (System.Object.ReferenceEquals(leftOperand, rightOperand))
                    return true;

                // If one is null, but not both, return false.
                if (((object)leftOperand == null) || ((object)rightOperand == null))
                    return false;

                return leftOperand.IsEqual(rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="obj"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(object obj, Key rightOperand)
            {
                return !(obj == rightOperand);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="obj"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, object obj)
            {
                return !(leftOperand == obj);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="leftOperand"></param>
            /// <param name="rightOperand"></param>
            /// <returns></returns>
            public static bool operator !=(Key leftOperand, Key rightOperand)
            {
                return !(leftOperand == rightOperand);
            }

            /// <summary>
            /// Equality and Hashing
            /// </summary>
            /// <returns></returns>
            public override int GetHashCode()
            {
                return this.Name.GetHashCode();
            }

            private bool IsEqual(Key key)
            {
                return string.CompareOrdinal(this.Name, key.Name) == 0;
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", Session.TypeTypeName, SfcSecureString.EscapeSquote(Name));
            }

        } // end of Key class


        #region object factory

        /// <summary>
        /// Singleton class used by collection class
        /// </summary>
        internal sealed class ObjectFactory : SfcObjectFactory
        {
            static readonly ObjectFactory instance = new ObjectFactory();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static ObjectFactory() { }

            ObjectFactory() { }

            public static ObjectFactory Instance
            {
                get { return instance; }
            }

            protected override SfcInstance CreateImpl()
            {
                return new Session();
            }
        }

        /// <summary>
        /// Gets the object factory.
        /// </summary>
        /// <returns></returns>
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }
        #endregion

        #region Public properties

        /// <summary>
        /// Gets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [SfcProperty(SfcPropertyFlags.Data)]
        public int ID
        {
            get
            {
                object value = this.Properties["ID"].Value;
                if (value == null)
                {
                    return -1;
                }
                return (int)value;
            }
        }


        /// <summary>
        /// The name of the Session
        /// </summary>
        /// <exception cref="XEventException">Set name for existing session or set name to null/empty string.</exception>
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
        [SfcKey(0)]
        public string Name
        {
            get
            {
                return (string)this.Properties["Name"].Value;
            }
            set
            {
                if (SfcObjectState.Pending == this.State)
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        throw new XEventException(ExceptionTemplates.NameNullEmpty);
                    }
                    Properties[nameof(Name)].Value = value;
                    AbstractIdentityKey = null;
                }
                else
                {
                    throw new XEventException(ExceptionTemplates.CannotSetNameForExistingObject + ExceptionTemplates.ChangeNameForExistingSession);
                }
            }
        }


        EventCollection events;
        /// <summary>
        /// Gets the event collection.
        /// </summary>
        /// <value>The event collection.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Event))]
        public EventCollection Events
        {
            get
            {
                if (this.events == null)
                {
                    this.events = new EventCollection(this);
                }
                ((ISfcCollection)this.events).EnsureInitialized(); //Ensure collection is initialized
                return this.events;
            }
        }


        TargetCollection targets;
        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Target))]
        public TargetCollection Targets
        {
            get
            {
                if (this.targets == null)
                {
                    this.targets = new TargetCollection(this);
                }
                ((ISfcCollection)this.targets).EnsureInitialized(); //Ensure collection is initialized
                return this.targets;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this session is running.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this session is running; otherwise, <c>false</c>.
        /// </value>
        [SfcProperty(Data = true)]
        public bool IsRunning
        {
            get
            {
                object v = this.Properties["IsRunning"].Value;
                if (v == null)
                {
                    return false;
                }
                return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            }
            private set
            {
                this.Properties["IsRunning"].Value = value;
            }
        }

        /// <summary>
        /// Event retention mode describes how event loss is handled.        
        /// </summary>
        public enum EventRetentionModeEnum
        {
            // Xsd Enumeration Values are defined in Sql/Common/xsd/sqlserver/2008/07/extendedeventsconfig/xeconfig.xsd
            // The enumerations in the Xsd continue to start with lowercase only, for backward compatibility reasons

            /// <summary>
            /// Events can be lost from the session.
            /// </summary>
            AllowSingleEventLoss,

            /// <summary>
            /// Full event buffers can be lost from the session.
            /// </summary>
            AllowMultipleEventLoss,

            /// <summary>
            /// No event loss is allowed.
            /// </summary>
            NoEventLoss
        }

        /// <summary>
        /// Gets or sets the event retention mode.
        /// </summary>
        /// <value>The event retention mode.</value>
        /// <exception cref="XEventException">Set EventRetentionMode to an unknown value.</exception>
        [SfcProperty(Data = true)]
        public EventRetentionModeEnum EventRetentionMode
        {
            get
            {
                object v = this.Properties[Session.EventRetentionModeProperty].Value;
                if (v == null)
                {
                    return EventRetentionModeEnum.AllowSingleEventLoss;
                }
                string str = v as string;
                switch (str)
                {
                    case "S":
                        return EventRetentionModeEnum.AllowSingleEventLoss;
                    case "M":
                        return EventRetentionModeEnum.AllowMultipleEventLoss;
                    case "N":
                        return EventRetentionModeEnum.NoEventLoss;
                }
                TraceHelper.TraceContext.TraceCriticalError("Unknown EventRetentionMode");
                throw new XEventException(ExceptionTemplates.InvalidParameter(Session.EventRetentionModeProperty));
            }
            set
            {
                string str;
                switch (value)
                {
                    case EventRetentionModeEnum.AllowSingleEventLoss:
                        str = "S";
                        break;
                    case EventRetentionModeEnum.AllowMultipleEventLoss:
                        str = "M";
                        break;
                    case EventRetentionModeEnum.NoEventLoss:
                        str = "N";
                        break;
                    default:
                        throw new XEventException(ExceptionTemplates.InvalidParameter(Session.EventRetentionModeProperty));
                }
                this.Properties[Session.EventRetentionModeProperty].Value = str;
            }
        }

        /// <summary>
        /// 0 indicates that dispatch latency is infinite.
        /// </summary>
        public const int InfiniteDispatchLatency = 0;

        /// <summary>
        /// Default dispatch latency is 30 seconds.
        /// </summary>
        public const int DefaultDispatchLatency = 30;

        /// <summary>
        /// Gets or sets the max dispatch latency (in seconds).
        /// </summary>
        /// <value>The max dispatch latency.</value>
        [SfcProperty(Data = true)]
        public int MaxDispatchLatency
        {
            get
            {
                object v = this.Properties[Session.MaxDispatchLatencyProperty].Value;
                if (v == null)
                {
                    return Session.DefaultDispatchLatency;
                }
                return (int)v / 1000;
            }
            set
            {
                this.Properties[Session.MaxDispatchLatencyProperty].Value = value * 1000;
            }
        }

        /// <summary>
        /// The maximum amount of memeory by default is 4 MB.
        /// </summary>
        public const int DefaultMaxMemory = 4096;

        /// <summary>
        /// Gets or sets the max memory (in KB).
        /// </summary>
        /// <value>The max memory.</value>
        [SfcProperty(Data = true)]
        public int MaxMemory
        {
            get
            {
                object v = this.Properties[Session.MaxMemoryProperty].Value;
                if (v == null)
                {
                    return Session.DefaultMaxMemory;
                }
                return (int)v;
            }
            set
            {
                this.Properties[Session.MaxMemoryProperty].Value = value;
            }
        }

        /// <summary>
        /// Gets or sets the size (in KB) of the max event.
        /// </summary>
        /// <value>The size of the max event.</value>
        [SfcProperty(Data = true)]
        public int MaxEventSize
        {
            get
            {
                object v = this.Properties[Session.MaxEventSizeProperty].Value;
                if (v == null)
                {
                    return 0;
                }
                return (int)v;
            }
            set
            {
                this.Properties[Session.MaxEventSizeProperty].Value = value;
            }
        }

        /// <summary>
        /// Memory partition mode describes the location in memory where event buffers are created.
        /// </summary>
        public enum MemoryPartitionModeEnum
        {            
            /// <summary>
            /// A single set of buffers are created within a SQL Server instance.
            /// </summary>
            None,

            /// <summary>
            /// A set of buffers is created for each non-uniform memory access (NUMA) node.
            /// </summary>
            PerNode,

            /// <summary>
            /// A set of buffers is created for each CPU.
            /// </summary>
            PerCpu
        }

        /// <summary>
        /// Gets or sets the memory partition mode.
        /// </summary>
        /// <value>The memory partition mode.</value>
        /// <exception cref="XEventException">Set MemoryPartitionMode to an unknown value.</exception>
        [SfcProperty(Data = true)]
        public MemoryPartitionModeEnum MemoryPartitionMode
        {
            get
            {
                object v = this.Properties["MemoryPartitionMode"].Value;
                if (v == null)
                {
                    return MemoryPartitionModeEnum.None;
                }
                string str = v as string;
                switch (str)
                {
                    case "G":
                        return MemoryPartitionModeEnum.None;
                    case "N":
                        return MemoryPartitionModeEnum.PerNode;
                    case "C":
                        return MemoryPartitionModeEnum.PerCpu;
                }
                TraceHelper.TraceContext.TraceCriticalError("Unrecognized MemoryPartitionMode");
                throw new XEventException(ExceptionTemplates.InvalidParameter("MemoryPartitionMode"));
            }
            set
            {
                string str;
                switch (value)
                {
                    case MemoryPartitionModeEnum.None:
                        str = "G";
                        break;
                    case MemoryPartitionModeEnum.PerNode:
                        str = "N";
                        break;
                    case MemoryPartitionModeEnum.PerCpu:
                        str = "C";
                        break;
                    default:
                        throw new XEventException(ExceptionTemplates.InvalidParameter("MemoryPartitionMode"));
                }
                this.Properties["MemoryPartitionMode"].Value = str;
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether [track causality].
        /// </summary>
        /// <value><c>true</c> if [track causality]; otherwise, <c>false</c>.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public bool TrackCausality
        {
            get
            {
                object v = this.Properties["TrackCausality"].Value;
                if (v == null)
                {
                    return false;
                }
                return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            }
            set
            {
                this.Properties["TrackCausality"].Value = value ? 1 : 0;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [auto start].
        /// </summary>
        /// <value><c>true</c> if [auto start]; otherwise, <c>false</c>.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public bool AutoStart
        {
            get
            {
                object v = this.Properties[Session.AutoStartProperty].Value;
                if (v == null)
                {
                    return false;
                }
                return Convert.ToBoolean(v, CultureInfo.InvariantCulture);
            }
            set
            {
                this.Properties[Session.AutoStartProperty].Value = value ? 1 : 0;
            }
        }

        /// <summary>
        /// Session is currently not running.
        /// </summary>
        public static readonly DateTime NotStarted = DateTime.MinValue;

        /// <summary>
        /// Gets the start time.
        /// </summary>
        /// <value>The start time.</value>
        [SfcProperty(Data = true)]
        public DateTime StartTime
        {
            get
            {
                object v = this.Properties["StartTime"].Value;
                if (v == null)
                {
                    return Session.NotStarted;
                }
                return (DateTime)v;
            }
        }

        /// <summary>
        /// 0 indicates that duration is unlimited.
        /// </summary>
        public const long UnlimitedDuration = 0;

        /// <summary>
        /// Default duration is unlimited (0).
        /// </summary>
        public const long DefaultMaxDuration = 0;

        /// <summary>
        /// Gets or sets the max duration (in seconds).
        /// MaxDuration is only supported on SQL Server 2025 (version 17) and later.
        /// A value of 0 indicates unlimited duration.
        /// </summary>
        /// <value>The max duration in seconds.</value>
        [SfcProperty(Data = true)]
        public long MaxDuration
        {
            get
            {
                object v = this.Properties[Session.MaxDurationProperty].Value;
                if (v == null)
                {
                    return Session.DefaultMaxDuration;
                }
                return Convert.ToInt64(v, CultureInfo.InvariantCulture);
            }
            set
            {
                this.Properties[Session.MaxDurationProperty].Value = value;
            }
        }

        #endregion Public properties

        /// <summary>
        /// State of the object, used in Alter function in session.
        /// </summary>
        /// <value>The state.</value>
        [SfcIgnore]
        public new SfcObjectState State
        {
            get
            {
                return base.State;
            }
        }

        /// <summary>
        /// New an event and add it to this session.
        /// </summary>
        /// <param name="eventInfo">The event info.</param>
        /// <returns>The newly created event object.</returns>
        public Event AddEvent(EventInfo eventInfo)
        {
            Event evt = new Event(this, eventInfo);
            this.Events.Add(evt);
            return evt;
        }

        /// <summary>
        /// New an event from fully qualified event name and add it to this session.
        /// </summary>
        /// <param name="eventName">Name of the event.</param>
        /// <exception cref="XEventException">Event name is malformed or wrong.</exception>
        /// <exception cref="ArgumentNullException">Parameter eventName is null</exception>
        /// <returns>The newly created event object.</returns>
        public Event AddEvent(string eventName)
        {
            Event evt = new Event(this, eventName);
            this.Events.Add(evt);
            return evt;
        }

        /// <summary>
        /// Removes the event object from the session.
        /// </summary>
        /// <param name="evt">The event object.</param>
        /// <exception cref="ArgumentNullException">Parameter evt is null.</exception>
        /// <returns>Returns whether the event is successfully removed.</returns>
        public bool RemoveEvent(Event evt)
        {
            if (evt == null)
            {
                throw new ArgumentNullException("evt");
            }
            return this.Events.Remove(evt);
        }

        /// <summary>
        /// New a target and add it to this session.
        /// </summary>
        /// <param name="targetInfo">The target info.</param>
        /// <returns>The newly created target object.</returns>
        public Target AddTarget(TargetInfo targetInfo)
        {
            Target target = new Target(this, targetInfo);
            this.Targets.Add(target);
            return target;
        }

        /// <summary>
        /// New a target from fully qualified target name and add it to this session .
        /// </summary>
        /// <param name="targetName">Name of the target.</param>
        /// <exception cref="XEventException">Target name is malformed or wrong.</exception>
        /// <exception cref="ArgumentNullException">Parameter targetName is null</exception>
        /// <returns>The newly created target object.</returns>
        public Target AddTarget(string targetName)
        {
            Target target = new Target(this, targetName);
            this.Targets.Add(target);
            return target;
        }

        /// <summary>
        /// Removes the target object from this session.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <exception cref="ArgumentNullException">Parameter target is null.</exception>
        /// <returns>Returns whether the target is successfully removed.</returns>
        public bool RemoveTarget(Target target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }
            return this.Targets.Remove(target);
        }


        /// <summary>
        ///    Checks if the session's state is dirty
        /// </summary>
        /// <returns></returns>
        private bool IsDirty()
        {
            // check if any of the events is dirty
            foreach (Event evt in this.Events)
            {
                if (evt.State == SfcObjectState.Pending || evt.State == SfcObjectState.ToBeDropped
                    || evt.State == SfcObjectState.Existing && (evt as ISessionObject).IsDirty() || evt.State == SfcObjectState.Recreate)
                {
                    return true;
                }
            }

            // check if any of the targets is dirty
            foreach (Target tgt in this.Targets)
            {
                if (tgt.State == SfcObjectState.Pending || tgt.State == SfcObjectState.ToBeDropped
                      || tgt.State == SfcObjectState.Existing && (tgt as ISessionObject).IsDirty() || tgt.State == SfcObjectState.Recreate)
                {
                    return true;
                }
            }

            // check if the session options are dirty
            foreach (SfcProperty prop in this.Properties)
            {
                if (prop.Dirty)
                {
                    return true;
                }
            }

            //default
            return false;
        }

        #region ISfcDiscoverObject methods
        void ISfcDiscoverObject.Discover(ISfcDependencyDiscoveryObjectSink sink)
        {
            if (sink == null)
            {
                throw new ArgumentNullException("sink");
            }
            switch (sink.Action)
            {
                case SfcDependencyAction.Create:
                    if (null != this.Events)
                    {
                        foreach (Event evt in this.Events)
                        {
                            // Shouldn't happen but doesn't hurt to check
                            if (evt.State != SfcObjectState.ToBeDropped)
                            {
                                sink.Add(SfcDependencyDirection.Inbound, evt, SfcTypeRelation.RequiredChild, false);
                            }
                        }
                    }
                    if (null != this.Targets)
                    {
                        foreach (Target target in this.Targets)
                        {
                            // Shouldn't happen but doesn't hurt to check
                            if (target.State != SfcObjectState.ToBeDropped)
                            {
                                sink.Add(SfcDependencyDirection.Inbound, target, SfcTypeRelation.RequiredChild, false);
                            }
                        }
                    }
                    break;

                case SfcDependencyAction.Alter:
                    if (null != this.Events)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, this.Events.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                    }
                    if (null != this.Targets)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, this.Targets.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                    }
                    break;

                case SfcDependencyAction.Drop:
                case SfcDependencyAction.Rename:
                case SfcDependencyAction.Move:
                    return;

                default:
                    TraceHelper.TraceContext.TraceError("Unknown sink.Action.");
                    throw new XEventException(ExceptionTemplates.InvalidParameter("sink.Action"));
            }
        }
        #endregion


    }

    /// <summary>
    ///   common interface part of Events and Targets
    /// </summary>
    public interface ISessionObject
    {
        /// <summary>
        /// Gets Session state.
        /// </summary>
        SfcObjectState State { get; }

        /// <summary>
        /// Gets Create script for the Session.
        /// </summary>
        /// <returns>A string containting the script.</returns>
        string GetCreateScript();

        /// <summary>
        /// Gets Drop script for the Session.
        /// </summary>
        /// <returns>A string containting the script.</returns>
        string GetDropScript();

        /// <summary>
        /// Indicates whether the Session is Dirty.
        /// </summary>
        /// <returns></returns>
        bool IsDirty();
    }

}
