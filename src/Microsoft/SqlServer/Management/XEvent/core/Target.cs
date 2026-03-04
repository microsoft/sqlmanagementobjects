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
    /// Runtime class for Target. Each instance of this class represents a row in sys.server_event_session_targets.
    /// </summary>
    public sealed class Target : SfcInstance, ISfcDiscoverObject, ISessionObject
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "Target";

        private ITargetProvider targetProvider = null;

        private ITargetProvider ProviderImpl
        {
            get
            {
                if (this.targetProvider == null)
                {
                    this.targetProvider = this.Parent.Parent.GetTargetProviderInternal(this);
                }

                return this.targetProvider;
            }
        }

        #region constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Target"/> class.
        /// Empty constructor is an convention is a convention in SFC.
        /// </summary>
        public Target()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Target"/> class with given parent and name.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The full qulified name of the Target.</param>
        /// <exception cref="XEventException">The target name is malformed or wrong.</exception>
        /// <exception cref="ArgumentNullException">Parameter name is null</exception>
        /// <exception cref="NullReferenceException">The parent of Session is not set yet.</exception>
        public Target(Session parent, string name) :
            this(parent, parent.Parent.ObjectInfoSet.Get<TargetInfo>(name))
        {
        }

        internal string eventModuleID = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Event"/> class from an TargetInfo object.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="targetInfo">The target info.</param>
        public Target(Session parent, TargetInfo targetInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Constructor"))
            {
                tm.TraceParameterIn("parent", parent);
                tm.TraceParameterIn("targetInfo", targetInfo);
                this.Parent = parent;
                //use the setTargetInfo
                SetTargetInfo(targetInfo);
            }
        }

        /// <summary>
        /// Set the TargetInfo for a pending Target.
        /// </summary>
        /// <param name="targetInfo"></param>
        /// <exception cref="XEventException">if the Target object is not in pending state.</exception>
        /// <exception cref="ArgumentNullException">if the input targetInfo is null.</exception>
        public void SetTargetInfo(TargetInfo targetInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("SetTargetInfo"))
            {
                tm.TraceParameterIn("targetInfo", targetInfo);
                if (SfcObjectState.Pending != this.State)
                {
                    tm.TraceError("set targetInfo for an target not in pending state");
                    throw new XEventException(ExceptionTemplates.CannotSetTargetInfoForExistingTarget);
                }

                if (null == targetInfo)
                {
                    tm.TraceError("targetInfo is null.");
                    throw new ArgumentNullException("targetInfo");
                }

                this.Name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", targetInfo.Parent.Name, targetInfo.Name);
                SetPackage(targetInfo.Parent.Name);
                this.ModuleID = targetInfo.Parent.ModuleID;
                this.Description = targetInfo.Description;
            }
        }
        #endregion

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
        /// Return child collection based on element type. Event is the parent of EventColumn.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns></returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case TargetField.TypeTypeName:
                    return this.TargetFields;
                default:
                    TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
                    throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
            }
        }

        /// <summary>
        /// Gets the target data.
        /// </summary>
        /// <returns>Target data xml string.</returns>
        public string GetTargetData()
        {
            return this.ProviderImpl.GetTargetData();
        }

        TargetFieldCollection targetFields;

        /// <summary>
        /// Gets the target column info set.
        /// </summary>
        /// <value>The target column info set.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(TargetField))]
        public TargetFieldCollection TargetFields
        {
            get
            {
                if (targetFields == null)
                {
                    targetFields = new TargetFieldCollection(this);
                    //add all of the customizable columns when constructing the event fileds collection.
                    if (SfcObjectState.Pending == this.State)
                    {
                        //split the event name from PackageName.Eventname
                        string targetInfoName = this.Name.Substring(this.Name.LastIndexOf('.') + 1);
                        TargetColumnInfoCollection targetColumns = this.Parent.Parent.ObjectInfoSet.Get<TargetInfo>(this.ModuleID, this.Name).TargetColumnInfoSet;

                        foreach (TargetColumnInfo columnInfo in targetColumns)
                        {
                            //only the customizable columns appears in the event fields. Duplication is not allowed.
                            if (!targetFields.Contains(columnInfo.Name))
                            {
                                TargetField targetField = new TargetField(this, columnInfo);
                                //targetField.Value can not be set here or its Dirty state will be affected.
                                targetFields.Add(targetField);
                            }

                        }
                    }

                }
                return targetFields;
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
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", Target.TypeTypeName, SfcSecureString.EscapeSquote(Name));
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
                return new Target();
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
        /// The name of the Target
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
        /// Gets or sets target description. Set accessor is for internal use only.
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

        #endregion



        #region script methods

        /// <summary>
        /// Gets Name formatted for scripting.
        /// </summary>
        public string ScriptName
        {
            get
            {
                // if pkgName.objName is not unique
                if (this.Parent.Parent.ObjectInfoSet.GetAll<TargetInfo>(this.Name).Count > 1)
                {
                    return string.Format(CultureInfo.InvariantCulture, "[{0}].{1}", this.ModuleID, this.Name);
                }
                return this.Name;
            }
        }


        /// <summary>
        /// Generate the script for add an target. Used in Create Session.
        /// </summary>
        /// <returns></returns>
        string ISessionObject.GetCreateScript()
        {
            return this.ProviderImpl.GetCreateScript();
        }

        /// <summary>
        /// Determines whether the target has at least one field needs to be set.
        /// </summary>
        /// <returns>
        /// True if at least customizable fields exist. False otherwise.
        /// </returns>
        public bool HasCustomizableField()
        {
            foreach (TargetField field in this.TargetFields)
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
            return string.Format(CultureInfo.InvariantCulture, "DROP TARGET {0}", this.ScriptName);
        }

        bool ISessionObject.IsDirty()
        {
            foreach (TargetField field in this.TargetFields)
            {
                if (field.Properties["Value"].Dirty)
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
                    if (null != this.TargetFields)
                    {
                        foreach (TargetField field in this.TargetFields)
                        {
                            // Shouldn't happen but doesn't hurt to check
                            if (field.State != SfcObjectState.ToBeDropped)
                            {
                                sink.Add(SfcDependencyDirection.Inbound, field, SfcTypeRelation.RequiredChild, false);
                            }
                        }
                    }
                    break;

                case SfcDependencyAction.Drop:
                case SfcDependencyAction.Alter:
                    if (null != this.TargetFields)
                    {
                        sink.Add(SfcDependencyDirection.Inbound, this.TargetFields.GetEnumerator(), SfcTypeRelation.RequiredChild, false);
                    }
                    break;

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
