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
    /// An EventInfo object may have three kinds of columns: customizable, data, readonly.
    /// A ReadOnlyEventColumnInfo object reprensents a readonly column of an EventInfo object.
    /// </summary>
    public sealed class ReadOnlyEventColumnInfo : SfcInstance
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "ReadOnlyEventColumnInfo";

        /// <summary>
        /// Default constructor
        /// </summary>
        internal ReadOnlyEventColumnInfo()
        {
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new EventInfo Parent
        {
            get { return (EventInfo)base.Parent; }
            set { base.Parent = value; }
        }

        /// <summary>
        /// Sets the name.
        /// </summary>
        /// <param name="name">The name.</param>
        private void SetName(string name)
        {
            this.Properties["Name"].Value = name;
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
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", ReadOnlyEventColumnInfo.TypeTypeName, SfcSecureString.EscapeSquote(Name));
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
                return new ReadOnlyEventColumnInfo();
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
                SfcProperty p = this.Properties["ID"];
                return (int)p.Value;
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
        }


        /// <summary>
        /// Gets or sets the name of the type.
        /// </summary>
        /// <value>The name of the type.</value>
        [SfcProperty(Data = true)]
        public string TypeName
        {
            get
            {
                SfcProperty p = this.Properties["TypeName"];
                return (string)p.Value;
            }
        }

        /// <summary>
        /// Gets package ID of the type.
        /// </summary>
        /// <value>The package ID.</value>
        [SfcProperty(Data = true)]
        public Guid TypePackageID
        {
            get
            {
                SfcProperty p = this.Properties["TypePackageID"];
                return (Guid)p.Value;
            }
        }

        /// <summary>
        /// Gets the name of the type package.
        /// </summary>
        /// <value>The name of the type package.</value>
        [SfcProperty(Data = true)]
        public string TypePackageName
        {
            get
            {
                SfcProperty p = this.Properties["TypePackageName"];
                return (string)p.Value;
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        [SfcProperty(Data = true)]
        public string Value
        {
            get
            {
                object value = this.Properties["Value"].Value;
                if (DBNull.Value == value)
                {
                    return null;
                }
                return value as string;
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
        }


        /// <summary>
        /// Gets the capabilities.
        /// </summary>
        /// <value>The capabilities.</value>
        [SfcProperty(Data = true)]
        public int Capabilities
        {
            get
            {
                SfcProperty p = this.Properties["Capabilities"];
                return (int)p.Value;
            }
        }

        /// <summary>
        /// Gets the capabilities description.
        /// </summary>
        /// <value>The capabilities description.</value>
        [SfcProperty(Data = true)]
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

        #endregion Public properties

    }

}
