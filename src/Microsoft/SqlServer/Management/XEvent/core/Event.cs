// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// Runtime class for Event. Each instance of this class represents a row in sys.server_event_session_events.
    /// </summary>
    public sealed class Event : SfcInstance, ISfcDiscoverObject, ISessionObject
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "Event";

        private IEventProvider eventProvider = null;

        private IEventProvider ProviderImpl
        {
            get
            {
                if (this.eventProvider == null)
                {
                    this.eventProvider = this.Parent.Parent.GetEventProviderInternal(this);
                }

                return this.eventProvider;
            }
        }

        #region constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class.
        /// Empty constructor is a convention in SFC.
        /// </summary>
        public Event()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class with given parent and name.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The full qulified name of the event.</param>
        /// <exception cref="XEventException">The event name is malformed or wrong.</exception>
        /// <exception cref="ArgumentNullException">Parameter name is null.</exception>
        /// <exception cref="NullReferenceException">The parent of Session is not set yet.</exception>
        public Event(Session parent, string name) : 
            this(parent, parent.Parent.ObjectInfoSet.Get<EventInfo>(name))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class from an EventInfo object.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="eventInfo">The event info.</param>
        public Event(Session parent, EventInfo eventInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Constructor"))
            {
                tm.TraceParameterIn("parent", parent);
                tm.TraceParameterIn("eventInfo", eventInfo);
                this.Parent = parent;
                //use the set function
                SetEventInfo(eventInfo);

            }
        }
        #endregion

        /// <summary>
        /// Set the EventInfo for a pending Event.
        /// </summary>
        /// <param name="eventInfo">An instance of EventInfo</param>
        /// <exception cref="XEventException">if the Event object is not in pending state.</exception>
        /// <exception cref="ArgumentNullException">if the input eventInfo is null.</exception>
        public void SetEventInfo(EventInfo eventInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("SetEventInfo"))
            {
                tm.TraceParameterIn("eventInfo", eventInfo);
                if (SfcObjectState.Pending != this.State)
                {
                    tm.TraceError("set eventinfo for an event not in pending state");
                    throw new XEventException(ExceptionTemplates.CannotSetEventInfoForExistingEvent);
                }

                if (null == eventInfo)
                {
                    tm.TraceError("eventInfo is null.");
                    throw new ArgumentNullException("eventInfo");
                }

                this.Name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", eventInfo.Parent.Name, eventInfo.Name);
                SetPackage(eventInfo.Parent.Name);
                this.ModuleID = eventInfo.Parent.ModuleID;
                this.Description = eventInfo.Description;
            }
        }

        /// <summary>
        /// Parent Property for Event.
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new Session Parent
        {
            get { return (Session)base.Parent; }
            set { base.Parent = value; }
        }

        /// <summary>
        /// Sets the package.
        /// </summary>
        /// <param name="packageName">Name of the package.</param>
        private void SetPackage(string packageName)
        {
            this.Properties["PackageName"].Value = packageName;
        }

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
        /// Return child collection based on element type. Event is the parent of EventField and Action.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns></returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case EventField.TypeTypeName:
                    return this.EventFields;
                case Action.TypeTypeName:
                    return this.Actions;
                default:
                    TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
                    throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
            }
        }

        EventFieldCollection eventFields = null;

        /// <summary>
        /// Gets the event fileds collection.
        /// </summary>
        /// <value>The collection of event fields.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(EventField))]
        public EventFieldCollection EventFields
        {
            get
            {
                if (eventFields == null)
                {
                    eventFields = new EventFieldCollection(this);

                    if (SfcObjectState.Pending == this.State)
                    {
                        //add all of the customizable columns when constructing the event fileds collection.
                        EventColumnInfoCollection eventColumns = 
                            this.Parent.Parent.ObjectInfoSet.Get<EventInfo>(this.ModuleID, this.Name).EventColumnInfoSet;
                        foreach (EventColumnInfo columnInfo in eventColumns)
                        {
                            //only the customizable columns appears in the event fields. Duplication is not allowed.
                            if (!eventFields.Contains(columnInfo.Name))
                            {
                                EventField eventField = new EventField(this, columnInfo);
                                //eventField.Value can not be set here or its Dirty state will be affected.
                                eventFields.Add(eventField);
                            }

                        }
                    }
                }
                return eventFields;
            }
        }

        ActionCollection actions;

        /// <summary>
        /// Gets the actions for this event.
        /// </summary>
        /// <value>The event column info set.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(Action))]
        public ActionCollection Actions
        {
            get
            {
                if (this.actions == null)
                {
                    this.actions = new ActionCollection(this);
                }
                ((ISfcCollection)this.actions).EnsureInitialized(); //ensure that the collection is initialized.
                return actions;
            }
        }
        /// <summary>
        /// A key class for identification.
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
            /// Initializes a new instance of the <see cref="Key"/> class.
            /// </summary>
            /// <param name="name">The name.</param>
            public Key(string name)
            {
                keyName = name;
            }

            /// <summary>
            /// Gets the name.
            /// </summary>
            /// <value>The name.</value>
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
            /// Equalses the specified obj1.
            /// </summary>
            /// <param name="obj1">The obj1.</param>
            /// <param name="obj2">The obj2.</param>
            /// <returns></returns>
            public new static bool Equals(object obj1, object obj2)
            {
                return (obj1 as Key) == (obj2 as Key);
            }

            /// <summary>
            /// Equalses the specified key.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns></returns>
            public override bool Equals(SfcKey key)
            {
                return this == key;
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="obj">The obj.</param>
            /// <param name="rightOperand">The right operand.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator ==(object obj, Key rightOperand)
            {
                if (obj == null || obj is Key)
                    return (Key)obj == rightOperand;
                return false;
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="leftOperand">The left operand.</param>
            /// <param name="obj">The obj.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator ==(Key leftOperand, object obj)
            {
                if (obj == null || obj is Key)
                    return leftOperand == (Key)obj;
                return false;
            }

            /// <summary>
            /// Implements the operator ==.
            /// </summary>
            /// <param name="leftOperand">The left operand.</param>
            /// <param name="rightOperand">The right operand.</param>
            /// <returns>The result of the operator.</returns>
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
            /// Implements the operator !=.
            /// </summary>
            /// <param name="obj">The obj.</param>
            /// <param name="rightOperand">The right operand.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator !=(object obj, Key rightOperand)
            {
                return !(obj == rightOperand);
            }

            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            /// <param name="leftOperand">The left operand.</param>
            /// <param name="obj">The obj.</param>
            /// <returns>The result of the operator.</returns>
            public static bool operator !=(Key leftOperand, object obj)
            {
                return !(leftOperand == obj);
            }

            /// <summary>
            /// Implements the operator !=.
            /// </summary>
            /// <param name="leftOperand">The left operand.</param>
            /// <param name="rightOperand">The right operand.</param>
            /// <returns>The result of the operator.</returns>
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
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", Event.TypeTypeName, SfcSecureString.EscapeSquote(Name));
            }
        }

        #region object factory
        /// <summary>
        /// Singleton factory class for Event
        /// </summary>
        internal sealed class ObjectFactory : SfcObjectFactory
        {
            static readonly ObjectFactory instance = new ObjectFactory();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static ObjectFactory() { }

            ObjectFactory() { }

            /// <summary>
            /// Gets the instance.
            /// </summary>
            /// <value>The instance.</value>
            public static ObjectFactory Instance
            {
                get { return instance; }
            }

            /// <summary>
            /// Creates the impl.
            /// </summary>
            /// <returns></returns>
            protected override SfcInstance CreateImpl()
            {
                return new Event();
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
        #endregion object factory

        #region public Properties
        /// <summary>
        /// The name of the Event
        /// </summary>
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
                    this.Properties["Name"].Value = value;
                }
                else
                {
                    throw new XEventException(ExceptionTemplates.CannotSetNameForExistingObject);
                }
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public int ID
        {
            get
            {
                object value = this.Properties["ID"].Value;
                //if the Object is in Pending state, the ID is not set,use -1 to indicate this.
                if (value == null)
                {
                    return -1;
                }
                return (int)value;
            }
        }

        /// <summary>
        /// Gets the module ID.
        /// </summary>
        /// <value>The module ID.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public Guid ModuleID
        {
            get
            {
                object value = this.Properties["ModuleID"].Value;
                string str = value as string;
                if (str != null)
                {
                    Guid guid = new Guid(str);
                    this.Properties["ModuleID"].Value = guid;
                    return guid;
                }
                return (Guid)value;
            }
            private set
            {
                this.Properties["ModuleID"].Value = value;
            }
        }

        /// <summary>
        /// Gets the package name that the event belongs to.
        /// </summary>
        /// <value>The description.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string PackageName
        {
            get
            {
                SfcProperty p = this.Properties["PackageName"];

                return (string)p.Value;
            }
        }

        /// <summary>
        /// This is the string representation of the predicate.
        /// </summary>
        /// <value>The predicate string.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string PredicateExpression
        {
            get
            {
                //the predicate is from the backend server.
                if (!this.isPredicateDirty)
                {
                    object value = this.Properties["PredicateExpression"].Value;
                    //the predicate may be null if is not set
                    if (DBNull.Value == value)
                    {
                        return null;
                    }
                    return value as string;
                }
                else
                {
                    //the predicate is set by the user
                    return this.predicate == null ? null : this.Parent.Parent.FormatPredicateExpression(this.predicate);
                }
            }
            set
            {
                CheckNullObject("PredicateExpression", value);
                this.Properties["PredicateExpression"].Value = value;
            }
        }

        bool isPredicateXmlDirty = true; // to avoid redundant Refresh

        /// <summary>
        /// This is the xml string representation of the predicate.
        /// </summary>
        /// <value>The predicate xml string.</value>
        [SfcProperty(SfcPropertyFlags.Data)]
        internal string PredicateXml
        {
            get
            {
                if (this.State == SfcObjectState.Existing && this.isPredicateXmlDirty)
                {
                    // after creation, PredicateXml is still empty and needs to be read from DB
                    // put it here(not in PostCreate) can let Refresh() only be called when PredicateXml is referenced
                    this.Refresh();
                    this.isPredicateXmlDirty = false;
                }

                object value = this.Properties["PredicateXml"].Value;
                //the predicate may be null if is not set
                if (DBNull.Value == value)
                {
                    return null;
                }
                return value as string;
            }
        }

        /// <summary>
        /// Gets or sets event description. Set accessor is for internal use only.
        /// </summary>
        /// <value>The description.</value>
        [SfcProperty(Data = true)]
        public string Description
        {
            get
            {
                object value = this.Properties["Description"].Value;
                if (DBNull.Value == value)
                {
                    return null;
                }
                return value as string;
            }
            private set
            {
                this.Properties["Description"].Value = value;
            }
        }

        private PredExpr predicate = null;
        private bool isPredicateDirty = false;

        /// <summary>
        /// Predicate tree for PredicateExpression.
        /// </summary>
        public PredExpr Predicate
        {
            get
            {
                //the predicate is from the backend server and hasn't been parsed yet.
                if (this.predicate == null && !this.isPredicateDirty)
                {
                    this.predicate = PredExpr.ParsePredicateXml(this.Parent.Parent, this.PredicateXml);
                }
                return this.predicate;
            }
            set
            {
                this.predicate = value;
                this.isPredicateDirty = true;
            }
        }

        /// <summary>
        ///  post create operation
        /// </summary>
        /// <param name="executionResult"></param>
        protected override void PostCreate(object executionResult)
        {
            base.PostCreate(executionResult);
            this.ResetPredicate();
        }

        /// <summary>
        /// post alter operation
        /// </summary>
        /// <param name="executionResult"></param>
        protected override void PostAlter(object executionResult)
        {
            base.PostAlter(executionResult);
            this.ResetPredicate();
        }

        /// <summary>
        ///  takes care of resetting the dirty flag
        ///  post create/alter operations
        /// </summary>
        private void ResetPredicate()
        {
            this.isPredicateDirty = false;
            this.isPredicateXmlDirty = true;
        }

        private void CheckNullObject(string objectName, Object obj)
        {
            if (null == obj)
            {
                TraceHelper.TraceContext.TraceError(string.Format(CultureInfo.InvariantCulture, "{0} value is null.", objectName));
                throw new ArgumentNullException(objectName);
            }
        }

        #endregion

        #region add remove actions
        /// <summary>
        /// Create an action from the ActionInfo and add it into the action collection.
        /// </summary>
        /// <param name="actionInfo">The ActionInfo object.</param>
        /// <returns>The newly created action.</returns>
        public Action AddAction(ActionInfo actionInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("AddAction:ActionInfo"))
            {
                if (null == actionInfo)
                {
                    tm.TraceError("actionInfo is null.");
                    throw new ArgumentNullException("actionInfo");
                }
                //create the action object
                Action action = new Action(this, actionInfo);
                //add the action in this event's actions
                this.Actions.Add(action);
                return action;
            }

        }

        /// <summary>
        /// Create an action with the specify name and add it to the actions.
        /// </summary>
        /// <param name="actionName">Full qulified name of the action.</param>
        /// <returns>The new created action</returns>
        public Action AddAction(string actionName)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("AddAction:ActionName"))
            {
                if (null == actionName || actionName.Trim().Length == 0)
                {
                    tm.TraceError("actionName is null or empty string.");
                    throw new XEventException(ExceptionTemplates.NameNullEmpty);
                }
                //Action constructor in charge of the validation.
                Action action = new Action(this, actionName);
                this.Actions.Add(action);
                return action;
            }
        }

        /// <summary>
        /// Removes the action from the session.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <returns>true if the action is found and removed, false otherwise.</returns>
        public bool RemoveAction(Action action)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("RemoveAction"))
            {
                if (null == action)
                {
                    tm.TraceError("actionInfo is null.");
                    throw new ArgumentNullException("actionInfo");
                }
            }
            return this.Actions.Remove(action);
        }
        #endregion

        #region script methods

        /// <summary>
        /// Gets Event name formatted for scripting.
        /// </summary>
        public string ScriptName
        {
            get
            {
                // if pkgName.objName is not unique
                if (this.Parent.Parent.ObjectInfoSet.GetAll<EventInfo>(this.Name).Count > 1) 
                {
                    return string.Format(CultureInfo.InvariantCulture, "[{0}].{1}", this.ModuleID, this.Name);
                }
                return this.Name;
            }
        }


        /// <summary>
        /// Generate the script for add an event. Used in Create Session.
        /// </summary>
        /// <returns></returns>
        string ISessionObject.GetCreateScript()
        {
            return this.ProviderImpl.GetCreateScript();
        }

        /// <summary>
        /// Determines whether the event has at least one field needs to be set.
        /// </summary>
        /// <returns>
        /// True if at least customizable fields exist. False otherwise.
        /// </returns>
        public bool HasCustomizableField()
        {
            foreach (EventField field in this.EventFields)
            {
                if (field.Value != null)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Generating the script for drop the event. Used in Alter Session.
        /// </summary>
        /// <returns></returns>
        string ISessionObject.GetDropScript()
        {

        return this.ProviderImpl.GetDropScript();       
        }

        bool ISessionObject.IsDirty()
        {
            foreach (Action action in this.Actions)
            {
                // Pending and ToBeDropped both should return dirty
                if (action.State == SfcObjectState.Pending || action.State == SfcObjectState.ToBeDropped)
                {
                    return true;
                }
            }

            foreach (EventField field in this.EventFields)
            {
                if (field.Properties["Value"].Dirty)
                {
                    return true;
                }
            }


            if (this.isPredicateDirty)
            {
                return true;
            }

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
        #endregion

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
                    if (null != this.Actions)
                    {
                        foreach (Action action in this.Actions)
                        {
                            // Shouldn't happen but doesn't hurt to check
                            if (action.State != SfcObjectState.ToBeDropped)
                            {
                                sink.Add(SfcDependencyDirection.Inbound, action, SfcTypeRelation.RequiredChild, false);
                            }
                        }
                    }
                    if (null != this.EventFields)
                    {
                        foreach (EventField field in this.EventFields)
                        {
                            // Shouldn't happen but doesn't hurt to check
                            if (field.State != SfcObjectState.ToBeDropped)
                            {
                                sink.Add(SfcDependencyDirection.Inbound, field, SfcTypeRelation.RequiredChild, false);
                            }
                        }
                    }
                    break;

                case SfcDependencyAction.Alter:
                    if (null != this.Actions)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, this.Actions.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                    }
                    if (null != this.EventFields)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, this.EventFields.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
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

        #region TypeMetadata support

        class TypeMetadata : SfcTypeMetadata
        {
            static readonly TypeMetadata instance = new TypeMetadata();

            // Explicit static constructor to tell C# compiler not to mark type as beforefieldinit
            static TypeMetadata() { }

            TypeMetadata() { }

            public static TypeMetadata Instance
            {
                get { return instance; }
            }

            public override bool IsCrudActionHandledByParent(SfcDependencyAction depAction)
            {
                using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("TypeMetadata.IsCrudActionHandledByParent"))
                {
                    switch (depAction)
                    {
                        case SfcDependencyAction.Create:
                        case SfcDependencyAction.Drop:
                        case SfcDependencyAction.Alter:
                            return true;
                        case SfcDependencyAction.Rename:
                        case SfcDependencyAction.Move:
                            return false;
                        default:
                            tm.TraceError("Unknown depAction.");
                            throw new XEventException(ExceptionTemplates.InvalidParameter("depAction"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets Sfc Type Metadata.
        /// </summary>
        /// <returns>Type Metadata.</returns>
        public static SfcTypeMetadata GetTypeMetadata()
        {
            return TypeMetadata.Instance;
        }

        #endregion
    }

}
