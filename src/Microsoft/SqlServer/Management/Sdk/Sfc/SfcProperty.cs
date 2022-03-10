// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// The element type of PropertyCollections which specifies the value, info and state of a particular property of an object instance.
    /// It uses the SfcPropertyCollection and its access to the Sfc type-specific IPropertyDataDispatch data interface for value access,
    /// and its access to the metadata for property info.
    /// 
    /// All management of the value and state of a property must pass through here or it may not be tracked correctly such as dirty state
    /// and retrieved state. If a strongly-typed property in the derived class simply wants to talk directly to its PropertyBag member, it would have to updat
    /// </summary>
    public sealed class SfcProperty : ISfcProperty
    {
        SfcPropertyCollection m_propertyCollection;
        string m_propertyName;

        internal SfcProperty(SfcPropertyCollection propertyCollection, string propertyName)
        {
            m_propertyCollection = propertyCollection;
            m_propertyName = propertyName;
        }


        public AttributeCollection Attributes
        {
            get
            {
                return this.m_propertyCollection.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[this.m_propertyCollection.LookupID(this.m_propertyName)].RelationshipAttributes;
            }
        }

        public string Name
        {
            get
            {
                return this.m_propertyName;
            }
        }

        public object Value
        {
            get
            {
                return m_propertyCollection.GetValue(m_propertyName);
            }
            set
            {
                m_propertyCollection.SetValue(m_propertyName, value);
            }
        }

        public bool IsAvailable
        {
            get
            {
                return m_propertyCollection.IsAvailable(m_propertyName);
            }
        }

        public bool Enabled
        {
            get
            {
                return m_propertyCollection.GetEnabled(m_propertyName);
            }
            set
            {
                m_propertyCollection.SetEnabled(m_propertyName, value);
            }
        }

        public Type Type
        {
            get { return m_propertyCollection.Type(m_propertyName); }
        }

        public bool Writable
        {
            get { return !m_propertyCollection.IsReadOnly(m_propertyName); }
        }

        public bool Readable
        {
            get { return true; }
        }

        public bool Expensive
        {
            get { return m_propertyCollection.IsExpensive(m_propertyName); }
        }

        public bool Computed
        {
            get { return m_propertyCollection.IsComputed(m_propertyName); }
        }

        public bool Encrypted
        {
            get { return m_propertyCollection.IsEncrypted(m_propertyName); }
        }

	    public bool Standalone
        {
            get { return m_propertyCollection.IsStandalone(m_propertyName); }
        }

        public bool SqlAzureDatabase
        {
            get { return m_propertyCollection.IsSqlAzureDatabase(m_propertyName); }
        }

        public bool IdentityKey
        {
            get { return m_propertyCollection.IsIdentityKey(m_propertyName); }
        }

        public bool Required
        {
            get { return m_propertyCollection.IsRequired(m_propertyName); }
        }

        public bool Dirty
        {
            get { return m_propertyCollection.IsDirty(m_propertyName); }
            internal set { m_propertyCollection.SetDirty(m_propertyName, value); }
        }

        public bool Retrieved
        {
            get { return m_propertyCollection.IsRetrieved(m_propertyName); }
            internal set { m_propertyCollection.SetRetrieved(m_propertyName, value); }
        }

        public bool IsNull
        {
            get { return m_propertyCollection.IsNull(m_propertyName); }
        }

        public override string ToString()
        {
            // TODO: use culture here
            return string.Format("Name={0}/Type={1}/Writable={2}/Value={3}",
                this.Name, this.Type.ToString(), this.Writable, !this.IsNull ? this.Value : "null");
        }

        public int CompareTo(object obj)
        {
            if (null == obj)
            {
                return -1;
            }
            else
            {
                try
                {
                    ISfcProperty property = obj as ISfcProperty;
                    if (property == null)
                    {
                        return -1;
                    }

                    bool areEqual = (string.Compare(Name, property.Name, StringComparison.Ordinal) == 0);
                    areEqual = areEqual && property.Dirty == this.Dirty;
                    areEqual = areEqual && property.IsNull == this.IsNull;
                    areEqual = areEqual && property.Required == this.Required;
                    areEqual = areEqual && property.Type == this.Type;
                    areEqual = areEqual && property.Value == this.Value;
                    areEqual = areEqual && property.Writable == this.Writable;
                    areEqual = areEqual && property.Attributes.Equals(this.Attributes);
                    if (areEqual)
                    {
                        return 0;
                    }
                    else
                    {
                        return -1;
                    }
                }
                catch
                {
                    //If any of the properties above throw an exception, return false.
                    return -1;
                }
            }
        }

        public override bool Equals(object o)
        {
            return (0 == CompareTo(o));
        }

        public static bool operator ==(SfcProperty prop1, SfcProperty prop2)
        {
            if (null == (object)prop1 && null == (object)prop2)
            {
                return true;
            }

            if (null == (object)prop1)
            {
                return false;
            }

            return prop1.Equals(prop2);
        }

        public static bool operator !=(SfcProperty prop1, SfcProperty prop2)
        {
            return !(prop1 == prop2);
        }

        public static bool operator >(SfcProperty prop1, SfcProperty prop2)
        {
            return (0 < prop1.CompareTo(prop2));
        }

        public static bool operator <(SfcProperty prop1, SfcProperty prop2)
        {
            return (0 > prop1.CompareTo(prop2));
        }

        // Override the Object.GetHashCode() method:
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }

    /// <summary>
    /// Each instance has a SfcPropertyCollection which is used for general access to the property names, values and info.
    /// It uses the metatdata each type provides for info about the properties. It also uses the PropertyDataDispatch proxy class
    ///  to map property indexes to strongly-typed data members in the PropertyBag of each Sfc instance
    /// </summary>
    public sealed class SfcPropertyCollection : ICollection, ISfcPropertySet
    {
        internal PropertyDataDispatcher m_propertyDispatcher;

        BitArray m_retrieved;
        BitArray m_dirty;
        BitArray m_enabled = null;

        internal SfcPropertyCollection(PropertyDataDispatcher dispatcher)
        {
            m_propertyDispatcher = dispatcher;
            int propertiesCount = m_propertyDispatcher.GetParent().Metadata.InternalStorageSupportedCount;
            m_retrieved = new BitArray(propertiesCount);
            m_dirty = new BitArray(propertiesCount);
        }

        #region ISfcPropertySet Functions

        public bool Contains(string propertyName)
        {
            int index = LookupID(propertyName);
            return index >= 0 && index < this.Count;
        }



        public bool Contains(ISfcProperty property)
        {
            SfcProperty sfcProperty = this[property.Name];
            if (sfcProperty == null)
            {
                return false;
            }

            return sfcProperty.Equals(property);
        }

        public bool Contains<T>(string propertyName)
        {
            if (Contains(propertyName))
            {
                return this[propertyName].Type == typeof(T);
            }
            return false;
        }

        public bool TryGetPropertyValue<T>(string propertyName, out T value)
        {
            value = default(T);
            try
            {
                value = (T)this[propertyName].Value;
                return true;
            }
            catch
            {
                return false;
            }
        }
        public bool TryGetPropertyValue(string propertyName, out object value)
        {
            return TryGetPropertyValue<object>(propertyName, out value);
        }


        public bool TryGetProperty(string propertyName, out ISfcProperty property)
        {
            property = null;
            try
            {
                property = this[propertyName];
                return true;
            }
            catch
            {
                return false;
            }
        }


        public IEnumerable<ISfcProperty> EnumProperties()
        {
            return new SfcEnumerable(this);
        }

        #endregion

        internal bool IsDirty(string propertyName)
        {
            int index = LookupID(propertyName);
            return m_dirty[index];
        }

        internal void SetDirty(string propertyName, bool val)
        {
            int index = LookupID(propertyName);
            m_dirty[index] = val;
        }

        public bool IsAvailable(string propertyName)
        {
            int index = LookupID(propertyName);
            return m_retrieved[index] || m_dirty[index];
        }

        //Using index to support enumerator
        internal bool IsRetrieved(int index)
        {
            return m_retrieved[index];
        }

        internal bool IsRetrieved(string propertyName)
        {
            int index = LookupID(propertyName);
            return this.IsRetrieved(index);
        }

        internal bool DynamicMetaDataEnabled
        {
            get
            {
                return m_enabled != null;
            }
        }

        internal void SetRetrieved(string propertyName, bool val)
        {
            int index = LookupID(propertyName);
            m_retrieved[index] = val;
        }

        public void CopyTo(SfcProperty[] array, Int32 index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array, Int32 index)
        {
            for (int i = 0; i < this.Count; i++)
            {
                array.SetValue(GetPropertyObject(i), index + i);
            }
        }

        public Int32 Count
        {
            get
            {
                return this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupportedCount;
            }
        }

        internal object GetValue(string propertyName)
        {
            // TODO: decide what to do when property is not initialized (not retrieved and not dirty)
            // We probably want to throw, but maybe we want domains to have control over this behavior.
            return m_propertyDispatcher.GetPropertyValue(propertyName);
        }

        internal void SetValue(string propertyName, object value)
        {
            object oldValue = GetValue(propertyName);
            m_propertyDispatcher.SetPropertyValue(propertyName, value);
            SetDirty(propertyName, true);
            if (oldValue != null && oldValue.Equals(value))
            {
                return;
            }

            PropertyChangedEventArgs args = new PropertyChangedEventArgs(propertyName);
            //Notify the base class with the value changes
            this.m_propertyDispatcher.GetParent().InternalOnPropertyValueChanges(args);
        }

        internal bool GetEnabled(string propertyName)
        {
            int index = LookupID(propertyName);
            if (m_enabled == null)
            {
                //Call the initialization to initialize the variables with the initial values
                this.m_propertyDispatcher.InitializeState();
            }

            if (m_enabled == null)
            {
                return true;
            }
            else
            {
                return m_enabled[index];
            }
        }

        internal void SetEnabled(string propertyName, bool value)
        {
            int index = LookupID(propertyName);
            bool oldEnabled = GetEnabled(propertyName);
            if (m_enabled == null)
            {
                m_enabled = new BitArray(this.Count, true);
                //Call the initialization to initialize the variables with the initial values
                this.m_propertyDispatcher.InitializeState();
            }
            m_enabled[index] = value;
            if (oldEnabled == value)
            {
                return;
            }

            SfcPropertyMetadataChangedEventArgs args = new SfcPropertyMetadataChangedEventArgs(propertyName);
            //Notify the base class with the metadata "Enabled" changes
            this.m_propertyDispatcher.GetParent().InternalOnPropertyMetadataChanges(args);
        }

        internal int LookupID(string propertyName)
        {
            List<SfcMetadataRelation> propertiesInformaton = this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported;
            for (int i = 0; i < propertiesInformaton.Count; i++)
            {
                if (string.Compare(propertyName, propertiesInformaton[i].PropertyName, StringComparison.Ordinal) == 0)
                {
                    return i;
                }
            }
            return -1;
        }

        public SfcProperty this[string propertyName]
        {
            get
            {
                return GetPropertyObject(propertyName);
            }
        }

        //Using index to support enumerator
        SfcProperty GetPropertyObject(int index)
        {
            return GetPropertyObject(this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyName);
        }

        SfcProperty GetPropertyObject(string propertyName)
        {
            RetrieveProperty(propertyName);
            return new SfcProperty(this, propertyName);
        }

        //Using index to support enumerator
        void RetrieveProperty(int index)
        {
            // TODO: hook up with OQ
            RetrieveProperty(this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyName);
        }

        void RetrieveProperty(string propertyName)
        {
            SetRetrieved(propertyName, true);
        }

        public IEnumerator GetEnumerator()
        {
            return new PropertyEnumerator(this);
        }

        internal Type Type(string propertyName)
        {
            int index = LookupID(propertyName);
            return this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].Type;
        }

        internal bool IsNull(string propertyName)
        {
            return GetValue(propertyName) == null;
        }

        internal bool IsComputed(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.Computed);
        }

        internal bool IsEncrypted(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.Encrypted);
        }

        internal bool IsExpensive(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.Expensive);
        }

	internal bool IsStandalone(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.Standalone);
        }

        internal bool IsSqlAzureDatabase(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.SqlAzureDatabase);
        }

        internal bool IsIdentityKey(string propertyName)
        {
            int index = LookupID(propertyName);
            foreach (Attribute attribute in this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].RelationshipAttributes)
            {
                if (attribute is SfcKeyAttribute)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool IsReadOnly(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.Data);
        }

        internal bool IsRequired(string propertyName)
        {
            int index = LookupID(propertyName);
            return 0 != (this.m_propertyDispatcher.GetParent().Metadata.InternalStorageSupported[index].PropertyFlags & SfcPropertyFlags.Required);
        }

        internal class SfcEnumerable : IEnumerable<ISfcProperty>
        {
            private SfcPropertyCollection collection;
            internal SfcEnumerable(SfcPropertyCollection collection)
            {
                this.collection = collection;
            }

            public IEnumerator<ISfcProperty> GetEnumerator()
            {
                return new PropertyEnumerator(collection);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return new PropertyEnumerator(collection);
            }
        }

        internal class PropertyEnumerator : IEnumerator, IEnumerator<ISfcProperty>
        {
            private SfcPropertyCollection m_propertyCollection;
            int m_currentPos;

            public PropertyEnumerator(SfcPropertyCollection propertyCollection)
            {
                m_propertyCollection = propertyCollection;
                m_currentPos = -1;
            }

            ISfcProperty IEnumerator<ISfcProperty>.Current
            {
                get
                {
                    return (ISfcProperty)this.Current;
                }
            }

            public object Current
            {
                get
                {
                    if (m_currentPos >= m_propertyCollection.Count)
                    {
                        // if we are past the end of the collection we want 
                        // to throw an exception here, rather than let 
                        // operations on m_propertyCollection throw
                        throw new InvalidOperationException();
                    }

                    // if the property is not retrieved we will retrieve it
                    // before returning the SfcProperty object to the user
                    if (!m_propertyCollection.IsRetrieved(m_currentPos))
                    {
                        m_propertyCollection.RetrieveProperty(m_currentPos);
                    }

                    return m_propertyCollection.GetPropertyObject(m_currentPos);
                }
            }

            public bool MoveNext()
            {
                return ++m_currentPos < m_propertyCollection.Count;
            }

            public void Reset()
            {
                m_currentPos = -1;
            }

            public void Dispose()
            {
            }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return this; }
        }
    }
}
