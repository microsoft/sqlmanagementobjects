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
    /// An EventField object reprensents a row in sys.server_event_session_fields.
    /// </summary>
    public sealed class EventField : SfcInstance
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "EventField";

        /// <summary>
        /// Default constructor
        /// </summary>
        public EventField()
        {
        }

        /// <summary>
        /// Constructor takes an Event and an EventColumnInfo as parameters
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="eventColumnInfo">The event column info.</param>
        /// <exception cref="ArgumentNullException">Parameter parent or eventColumnInfo is null.</exception>
        /// <exception cref="NullReferenceException">eventColumnInfo's parent(or grandparent) is null. Mostly because it's not enumerated correctly.</exception>
        /// <exception cref="XEventException">Parameter eventColumnInfo is invalid.</exception>
        public EventField(Event parent, EventColumnInfo eventColumnInfo)
        {
            using (MethodTraceContext tm = TraceHelper.TraceContext.GetMethodContext("Constructor"))
            {
                if (parent == null)
                {
                    tm.TraceError("Parameter parent is null.");
                    throw new ArgumentNullException("parent");
                }
                if (eventColumnInfo == null)
                {
                    tm.TraceError("Parameter eventColumnInfo is null.");
                    throw new ArgumentNullException("eventColumnInfo");
                }
                string columnFullName = eventColumnInfo.Parent.Parent.Name + "." + eventColumnInfo.Parent.Name;
                if (parent.Name != columnFullName)
                {
                    tm.TraceError("Parameter eventColumnInfo can't be used with parameter parent.");
                    throw new XEventException(ExceptionTemplates.InvalidParameter("eventColumnInfo"));
                }
                this.Parent = parent;
                this.Name = eventColumnInfo.Name;
                this.Description = eventColumnInfo.Description;
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
            TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
            throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
        }




        /// <summary>
        /// 
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
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", EventField.TypeTypeName, SfcSecureString.EscapeSquote(Name));
            }
        }

        #region Object factory

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
                return new EventField();
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

        #endregion Object factory


        #region Public properties

        /// <summary>
        /// Gets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
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
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
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
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        [SfcProperty(Data = true)]
        public object Value
        {
            get
            {
                object value = this.Properties["Value"].Value;
                if (DBNull.Value == value)
                {
                    return null;
                }
                return value;
            }
            set
            {
                this.Properties["Value"].Value = value;
            }
        }

        /// <summary>
        /// Gets the description.
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
