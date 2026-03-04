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
    /// Metadata class for Package. 
    /// </summary>
    public sealed class Package : SfcInstance
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "Package";
        /// <summary>
        /// Initializes a new instance of the <see cref="Package"/> class.
        /// Empty constructor is an convention is a convention in SFC.
        /// </summary>
        internal Package()
        { 
        
        }

        /// <summary>
        /// Set the name of package.
        /// </summary>
        private void SetName(string name)
        {
            this.Properties["Name"].Value = name;
        }

        /// <summary>
        /// Create a key from package name. The key is used by SFC framework. 
        /// </summary>
        /// <returns>a instance of Package.Key</returns>
        protected override SfcKey CreateIdentityKey()
        {
            Key key = null;
            //Can't create a key without a key value
            if (this.Name != null)
            {
                key = new Key(this.ModuleID.ToString(), this.Name);
            }
            return key;
        }

        /// <summary>
        /// Key Property.
        /// </summary>
        /// <value>The identity key.</value>
        [SfcIgnore]
        public Key IdentityKey
        {
            get { return (Key)this.AbstractIdentityKey; }
        }

        /// <summary>
        /// Return the child collection based on the element type.
        /// </summary>
        /// <param name="elementType">type of the collection element</param>
        /// <returns>child collection of the specify type</returns>
        protected override ISfcCollection GetChildCollection(string elementType)
        {
            switch (elementType)
            {
                case EventInfo.TypeTypeName:
                    return this.EventInfoSet;
                case ActionInfo.TypeTypeName:
                    return this.ActionInfoSet;
                case TargetInfo.TypeTypeName:
                    return this.TargetInfoSet;
                case TypeInfo.TypeTypeName:
                    return this.TypeInfoSet;
                case PredSourceInfo.TypeTypeName:
                    return this.PredSourceInfoSet;
                case PredCompareInfo.TypeTypeName:
                    return this.PredCompareInfoSet;
                case MapInfo.TypeTypeName:
                    return this.MapInfoSet;
                default:
                    TraceHelper.TraceContext.TraceError("No such collection for type {0}", elementType);
                    throw new XEventException(ExceptionTemplates.NoSuchCollection(elementType));
            }
        }

        /// <summary>
        ///  An internal wrapper over the corresponding protected method.
        /// </summary>
        /// <returns>child collection of the specified type</returns>
        internal IXEObjectInfoCollection<T> GetChildCollection<T>() where T : SfcInstance, IXEObjectInfo
        {
            return this.GetChildCollection(typeof(T).Name) as IXEObjectInfoCollection<T>;
        }

        


        #region children collections
        EventInfoCollection eventInfoSet;
        /// <summary>
        /// Collection of EventInfo.
        /// </summary> 
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(EventInfo))]
        public EventInfoCollection EventInfoSet
        {
            get
            {
                if (eventInfoSet == null)
                {
                    eventInfoSet = new EventInfoCollection(this);
                }
                return eventInfoSet;
            }
        }

        ActionInfoCollection actionInfoSet;
        /// <summary>
        /// Collection of ActionInfo.
        /// </summary>
        /// <value>The ActionInfo set.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(ActionInfo))]
        public ActionInfoCollection ActionInfoSet
        {
            get
            {
                if (actionInfoSet == null)
                {
                    actionInfoSet = new ActionInfoCollection(this);
                }
                return actionInfoSet;
            }
        }
        TargetInfoCollection targetInfoSet;
        /// <summary>
        /// Collection of TargetInfoSet.
        /// </summary>
        /// <value>The TargetInfo set.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(TargetInfo))]
        public TargetInfoCollection TargetInfoSet
        {
            get
            {
                if (targetInfoSet == null)
                {
                    targetInfoSet = new TargetInfoCollection(this);
                }
                return targetInfoSet;
            }
        }

        TypeInfoCollection typeInfoSet;
        /// <summary>
        /// 
        /// </summary> 
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(TypeInfo))]
        public TypeInfoCollection TypeInfoSet
        {
            get
            {
                if (typeInfoSet == null)
                {
                    typeInfoSet = new TypeInfoCollection(this);
                }
                return typeInfoSet;
            }
        }

        MapInfoCollection mapInfoSet;
        /// <summary>
        /// Collection of MapInfo.
        /// </summary>
        /// <value>The MapInfo set.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(MapInfo))]
        public MapInfoCollection MapInfoSet
        {
            get
            {
                if (mapInfoSet == null)
                {
                    mapInfoSet = new MapInfoCollection(this);
                }
                return mapInfoSet;
            }
        }

        PredSourceInfoCollection predSourceInfoSet;
        /// <summary>
        /// 
        /// </summary>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(PredSourceInfo))]
        public PredSourceInfoCollection PredSourceInfoSet
        {
            get
            {
                if (predSourceInfoSet == null)
                {
                    predSourceInfoSet = new PredSourceInfoCollection(this);
                }
                return predSourceInfoSet;
            }
        }

        PredCompareInfoCollection predCompareInfoSet;
        /// <summary>
        /// Collection of PredCompareInfo
        /// </summary>
        /// <value>The PredCompareInfo set.</value>
        [SfcObject(SfcContainerRelationship.ChildContainer, SfcContainerCardinality.ZeroToAny, typeof(PredCompareInfo))]
        public PredCompareInfoCollection PredCompareInfoSet
        {
            get
            {
                if (predCompareInfoSet == null)
                {
                    predCompareInfoSet = new PredCompareInfoCollection(this);
                }
                return predCompareInfoSet;
            }
        }
        #endregion



        /// <summary>
        /// A key class for identification.
        /// </summary>
        public sealed class Key : SfcKey
        {
            private string Name {get;set;}
            private string ModuleID {get; set;}

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
                this.Name = other.Name;
                this.ModuleID = other.ModuleID;
            }


            /// <summary>
            /// Initializes a new instance of the <see cref="Key"/> class.
            /// </summary>
            /// <param name="guid">The module GUID.</param>
            /// <param name="name">The non-fully qualified package name.</param>
            public Key(string guid, string name)
            {
                this.Name = name;
                this.ModuleID = guid;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Key"/> class.
            /// </summary>
            /// <param name="filedDict">A set of name-value pairs that represent Urn fragment.</param>
            public Key(Dictionary<string, object> filedDict)
            {
                // this will throw if the field is not found.
                this.Name = (string) filedDict["Name"];
                this.ModuleID = (string) filedDict["ModuleID"];                
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
                return this.Name.GetHashCode() ^ this.ModuleID.GetHashCode();
            }

            /// <summary>
            /// Determines whether the specified key is equal.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <returns>
            ///     <c>true</c> if the specified key is equal; otherwise, <c>false</c>.
            /// </returns>
            private bool IsEqual(Key key)
            {
                return string.CompareOrdinal(this.Name, key.Name) == 0 && string.CompareOrdinal(this.ModuleID.ToUpperInvariant(), key.ModuleID.ToUpperInvariant()) == 0;
            }

            /// <summary>
            /// Conversions
            /// </summary>
            /// <returns></returns>
            public override string GetUrnFragment()
            {             
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}' and @ModuleID='{2}']",
                    Package.TypeTypeName, SfcSecureString.EscapeSquote(this.Name), this.ModuleID.ToUpperInvariant());
            }
        }

        #region object factory
        // Singleton factory class
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
            /// Creates the impl for the object factory.
            /// </summary>
            /// <returns></returns>
            protected override SfcInstance CreateImpl()
            {
                return new Package();
            }
        }


        /// <summary>
        /// Gets the object factory instance for Package.
        /// </summary>
        /// <returns></returns>
        public static SfcObjectFactory GetObjectFactory()
        {
            return ObjectFactory.Instance;
        }
        #endregion

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
        /// <summary>
        /// Gets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public Guid ID
        {
            get 
            {
                return (Guid)this.Properties["ID"].Value;
            }
        }

        /// <summary>
        /// Gets the name of the package.
        /// </summary>
        /// <value>The name of the package.</value>
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
        [SfcKey(1)]
        public string Name
        {
            get
            {
                return (string)this.Properties["Name"].Value;
            }
        }


        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string Description
        {
            get
            {
                return this.Properties["Description"].Value as string;
            }
        }

        /// <summary>
        /// Gets the capabilities.
        /// </summary>
        /// <value>The capabilities.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public int Capabilities
        {
            get
            {
                object value = this.Properties["Capabilities"].Value;

                return (int)value;
            }
        }

        /// <summary>
        /// Gets the capabilities desc.
        /// </summary>
        /// <value>The capabilities desc.Null is possible.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public string CapabilitiesDesc
        {
            get 
            {
                object value = this.Properties["CapabilitiesDesc"].Value;
                if (DBNull.Value == value)
                {
                    return null;
                }
                return value as string;
            }
        }

        /// <summary>
        /// Gets the module ID.
        /// </summary>
        /// <value>The module ID.</value>
        [SfcProperty(SfcPropertyFlags.Required | SfcPropertyFlags.ReadOnlyAfterCreation)]
        [SfcKey(0)]
        public Guid ModuleID
        {
            get
            {
                return (Guid)this.Properties["ModuleID"].Value;
            }
        }

        /// <summary>
        /// Gets the module address.
        /// </summary>
        /// <value>The module address.</value>
        [SfcProperty(Data = true)]
        public Byte[] ModuleAddress
        {
            get
            {
                SfcProperty p = this.Properties["ModuleAddress"];

                return (Byte[])p.Value;
            }
        }
    }
}
