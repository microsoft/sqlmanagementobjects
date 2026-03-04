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
    /// RunTime class for Action. Each instance of this class represents a row in sys.server_event_session_actions.
    /// </summary>
    public sealed class Action : SfcInstance
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "Action";

        /// <summary>
        /// Initializes a new instance of the <see cref="Action"/> class.
        /// </summary>
        public Action()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Action"/> class with the given parent and name.
        /// The name here should be in form of eventModuleId.packagename.eventname or packagename.eventname.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="name">The name.</param>
        /// <exception cref="XEventException">The event name is malformed or wrong.</exception>
        /// <exception cref="ArgumentNullException">Parameter name is null.</exception>
        /// <exception cref="NullReferenceException">The parent(or grandparent) of Event is not set yet.</exception>
        public Action(Event parent, string name) :
            this(parent, parent.Parent.Parent.ObjectInfoSet.Get<ActionInfo>(name))
        {
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="Action"/> class from an instance of ActionInfo.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="actionInfo">The ActionInfo object.</param>
        public Action(Event parent, ActionInfo actionInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Constructor"))
            {
                tm.TraceParameterIn("parent", parent);
                tm.TraceParameterIn("actionInfo", actionInfo);
                this.Parent = parent;
                SetActionInfo(actionInfo);
            } 
        }

        /// <summary>
        /// Set the ActionInfo for a pending Action.
        /// </summary>
        /// <param name="actionInfo">an instance of ActionInfo</param>
        /// <exception cref="XEventException">if the Action object is not in pending state.</exception>
        /// <exception cref="ArgumentNullException">if the input actionInfo is null.</exception>
        public void SetActionInfo(ActionInfo actionInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("SetActionInfo"))
            {
                tm.TraceParameterIn("actionInfo", actionInfo);

                //Set ActionInfo can only happened when it's in pending state.
                if (SfcObjectState.Pending != this.State)
                {
                    tm.TraceError("set actionInfo for an action not in pending state");
                    throw new XEventException(ExceptionTemplates.CannotSetActionInfoForExistingAction);
                }

                if (null == actionInfo)
                {
                    tm.TraceError("actionInfo is null.");
                    throw new ArgumentNullException("actionInfo");
                }

                this.Name = string.Format(CultureInfo.InvariantCulture, "{0}.{1}", actionInfo.Parent.Name, actionInfo.Name);
                SetPackage(actionInfo.Parent.Name);
                this.ModuleID = actionInfo.Parent.ModuleID;
                this.Description = actionInfo.Description;
            }
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new Event Parent
        {
            get { return (Event)base.Parent; }
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
        /// Gets the child collection.
        /// </summary>
        /// <param name="elementType">Type of the element.</param>
        /// <returns>By now, action has no child, so this function always throw exception.</returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                default:
                    TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
                    throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
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
            /// <param name="filedDict">The filed dict.</param>
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

            /// <summary>
            /// Determines whether the specified key is equal.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>
            /// 	<c>true</c> if the specified key is equal; otherwise, <c>false</c>.
            /// </returns>
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
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", Action.TypeTypeName, SfcSecureString.EscapeSquote(Name));
            }
        }

        #region object factory
        // Singleton factory class
        sealed class ObjectFactory : SfcObjectFactory
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
                return new Action();
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


        /// <summary>
        /// The name of the Action
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
        /// Gets the name of the package this action belongs to.
        /// </summary>
        /// <value>The name of the package.</value>
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
        /// Gets or sets action description. Set accessor is for internal use only.
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

        #region script functions
        /// <summary>
        /// Gets Create script for the Action.
        /// </summary>
        /// <returns>A string containting the script.</returns>
        public string GetScriptCreate()
        {
            // if pkgName.objName is not unique
            if (this.Parent.Parent.Parent.ObjectInfoSet.GetAll<ActionInfo>(this.Name).Count > 1) 
            {
                return string.Format(CultureInfo.InvariantCulture, "[{0}].{1}",
                    this.ModuleID, this.Name);
            }

            return this.Name;
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
