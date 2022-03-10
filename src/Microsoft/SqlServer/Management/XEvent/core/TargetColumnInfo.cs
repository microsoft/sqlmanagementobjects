// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.XEvent
{
    /// <summary>
    /// TargetInfo currently has only one kind of column: customizable column.
    /// A TargetColumnInfo object reprensents a customizable column of a TargetInfo object.
    /// </summary>
    public sealed class TargetColumnInfo : SfcInstance
    {
        /// <summary>
        /// Type name
        /// </summary>
        public const string TypeTypeName = "TargetColumnInfo";

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetColumnInfo"/> class.
        /// </summary>
        internal TargetColumnInfo()
        {
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [SfcObject(SfcObjectRelationship.ParentObject, SfcObjectCardinality.One)]
        public new TargetInfo Parent
        {
            get { return (TargetInfo)base.Parent; }
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
        /// <returns>A target column has no child, so this function always throw the exception</returns>
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
            /// <param name="other">The other.</param>
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
                return String.Format(CultureInfo.InvariantCulture, "{0}[@Name='{1}']", TargetColumnInfo.TypeTypeName, SfcSecureString.EscapeSquote(Name));
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
            /// Creates the impl.
            /// </summary>
            /// <returns></returns>
            protected override SfcInstance CreateImpl()
            {
                return new TargetColumnInfo();
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
        /// The name of the TarColumnInfo.
        /// </summary>
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
        /// Gets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
        public int ID
        {
            get
            {
                SfcProperty p = this.Properties["ID"];

                return (int)p.Value;
            }
        }


        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>The value.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
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
        /// <value>The description for the column. Null is possible.</value>
        [SfcProperty(SfcPropertyFlags.Required)]
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
        /// Gets the name of the type.
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
        /// <value>The capabilities description.Null is possible.</value>
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
    }
}
