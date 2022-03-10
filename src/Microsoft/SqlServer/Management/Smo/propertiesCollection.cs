// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// This is specialized bit storage for storing 4 bits per item
    /// Bits are packed into Int32 values
    /// 
    /// </summary>
    class BitStorage
    {
        // This storage is optimized to have exactly 4 bits per item
        // If the number of bits per item changes the BitPattern below
        // will stop working
        const int BitsPerItem = 4;
        // This pattern initializes all bits in any given UInt32
        // as following:
        // Retrieved = 0, Dirty = 0, Enabled = 0, Uninitialized = 1
        const System.UInt32 BitPattern = 0x88888888;
        internal enum BitIndex
        {
            Retrieved = 0,
            Dirty = 1,
            Enabled = 2,
            Uninitialized = 3
        };

        // Number of items (4-bit sets)
        private int count;
        // Array that stores actual bits
        // It makes sense to store in UInt32 rather than bytes because of the memory alignment
        private UInt32[] bitArray;

        internal BitStorage(int itemCount)
        {
            this.count = itemCount;

            this.bitArray = new UInt32[(itemCount * BitsPerItem + 31) / 32];
            // Initialize all array elements with the pattern
            for (int i = 0; i < this.bitArray.Length; i++)
            {
                this.bitArray[i] = BitPattern;
            }
        }

        protected void SetBit(int itemIndex, BitStorage.BitIndex bitIndex, bool value)
        {
            Diagnostics.TraceHelper.Assert(itemIndex >= 0 && itemIndex < this.count);
            
            int index = itemIndex * BitsPerItem + (int)bitIndex;

            if (value)
            {
                this.bitArray[index / 32] |= ((UInt32)1 << (index % 32));
            }
            else
            {
                this.bitArray[index / 32] &= ~((UInt32)1 << (index % 32));
            }            
        }

        protected bool GetBit(int itemIndex, BitStorage.BitIndex bitIndex)
        {
            Diagnostics.TraceHelper.Assert(itemIndex >= 0 && itemIndex < this.count);

            int index = itemIndex * BitsPerItem + (int)bitIndex;
            return (this.bitArray[index / 32] & (1 << (index % 32))) != 0;
        }

        internal int Count
        {
            get { return this.count; }
        }
    }

    abstract class PropertyStorageBase : BitStorage
    {
        internal PropertyStorageBase(int count) : base(count)
        {
        }

        internal bool IsNull(int index)
        {
            return GetValue(index) == null;
        }

        internal bool IsDirty(int index)
        {
            return GetBit(index, BitIndex.Dirty);
        }

        internal void SetDirty(int index, bool val)
        {
            SetBit(index, BitIndex.Dirty, val);
        }

        internal bool IsRetrieved(int index)
        {
            return GetBit(index, BitIndex.Retrieved);
        }

        internal void SetRetrieved(int index, bool val)
        {
            SetBit(index, BitIndex.Retrieved, val);
        }

        internal bool IsEnabled(int index)
        {
            return GetBit(index, BitIndex.Enabled);
        }

        internal void SetEnabled(int index, bool val)
        {
            SetBit(index, BitIndex.Enabled, val);
        }

        internal abstract object GetValue(int index);
        internal abstract void SetValue(int index, object value);
    }

    class PropertyBag : PropertyStorageBase
    {
        object[] m_propertyValues;

        internal PropertyBag( int count ) : base(count)
        {
            m_propertyValues = new object[count];
        }

        internal override object GetValue(int index)
        {
            return m_propertyValues[index];
        }

        internal override void SetValue(int index, object value)
        {
            m_propertyValues[index] = value;
        }
    }

    class PropertyDispatcher : PropertyStorageBase
    {
        IPropertyDataDispatch m_dispatch;

        internal PropertyDispatcher(int count, IPropertyDataDispatch dispatch)
            : base(count)
        {
            m_dispatch = dispatch;
        }

        internal override object GetValue(int index)
        {
            if (GetBit(index, BitIndex.Uninitialized))
            {
                return null;
            }
            return m_dispatch.GetPropertyValue(index);
        }

        internal override void SetValue(int index, object value)
        {
            if( value == null )
            {
                SetBit(index, BitIndex.Uninitialized, true);
            }
            else
            {
                m_dispatch.SetPropertyValue( index, value );
                SetBit(index, BitIndex.Uninitialized, false);
            }
        }
    }

    class PropertyStorageFactory
    {
        internal static PropertyStorageBase Create(SmoObjectBase parent, int propCount)
        {
            IPropertyDataDispatch dispatch = parent as IPropertyDataDispatch;
            if( dispatch == null )
            {
                return new PropertyBag(propCount);
            }
            else
            {
                return new PropertyDispatcher(propCount, dispatch);
            }
        }
    }

    public class PropertyCollection : ICollection
                                    , Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet
    {
        #region private
        enum InitializationState { Empty, Partial, Full };
        internal SmoObjectBase m_parent;
        PropertyStorageBase m_PropertyStorage;
        PropertyMetadataProvider m_pmp;

        internal PropertyMetadataProvider PropertiesMetadata
        {
            get { return m_pmp; }
        }

        internal int LookupID(string propertyName, PropertyAccessPurpose pap)
        {
            return this.PropertiesMetadata.PropertyNameToIDLookupWithException(propertyName, pap);
        }

        protected int LookupID(string propertyName)
        {
            return this.PropertiesMetadata.PropertyNameToIDLookupWithException(propertyName);
        }

        int LookupIDNoBoundCheck(string propertyName)
        {
            return this.PropertiesMetadata.PropertyNameToIDLookup(propertyName);
        }

        void RetrieveProperty(int index, bool useDefaultOnMissingValue)
        {
            if (!IsAvailable(index))
            {
                SetValue(index, m_parent.OnPropertyMissing(GetName(index), useDefaultOnMissingValue));
                if (IsDesignMode)
                {
                    // We have retrieved the default value into the property bag
                    // To fully simulate the "online" mode, we set the Retrieved flag on the property
                    // as if the value came from the server
                    SetRetrieved(index, true);
                }
            }
        }

        void HandleNullValue(int index)
        {
            if (IsDesignMode && IsNull(index))
            {
                Property prop = GetProperty(index);

                if (!prop.Writable)
                {
                    throw new PropertyNotAvailableException(ExceptionTemplates.PropertyNotAvailableInDesignMode(GetName(index)));
                }
                throw new PropertyNotAvailableException(ExceptionTemplates.PropertyNotSetInDesignMode(GetName(index)));
            }
            if (IsNull(index) && IsRetrieved(index))
            {
                throw new PropertyCannotBeRetrievedException(GetName(index), m_parent);
            }
        }

        Property GetProperty(string name)
        {
            return GetProperty(LookupID(name));
        }

        Property GetProperty(int index)
        {
            return new Property(this, index);
        }

        StaticMetadata GetStaticMetadata(int index)
        {
            return this.PropertiesMetadata.GetStaticMetadata(index);
        }

#endregion private	

#region internal
        internal Property Get(string name)
        {
            return GetProperty(name);
        }

        internal Property Get(int index)
        {
            return GetProperty(index);
        }

        internal string GetName(int index)
        {
            return GetStaticMetadata(index).Name;
        }

        internal object GetValueWithNullReplacement(string propertyName)
        {
            return GetValueWithNullReplacement(propertyName, true, IsDesignMode? true : false);
        }

        /// <summary>
        /// Returns the property value. It does not allow null values unless this is a
        /// value that makes sense in the OO model, otherwise an exception is thrown.
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="throwOnNullValue">Indicates whether we should throw an exception
        /// in case of a null value in the property bag</param>
        /// <returns></returns>
        internal object GetValueWithNullReplacement(string propertyName, bool throwOnNullValue, bool useDefaultOnMissingValue)
        {
            int index = LookupID(propertyName, PropertyAccessPurpose.Read);
            RetrieveProperty(index, useDefaultOnMissingValue);
            if( throwOnNullValue)
            {
                HandleNullValue(index);
            }
            return this.GetValue(index);
        }

        internal object GetValue(int index)
        {
            object value = m_PropertyStorage.GetValue(index);
            return value;
        }

        internal void SetValueWithConsistencyCheck(string propertyName, object value)
        {
            SetValueWithConsistencyCheck(propertyName, value, false);
        }

        internal void SetValueWithConsistencyCheck(string propertyName, object value, bool allowNull)
        {
            if (false == allowNull && null == value)
            {
                throw new ArgumentNullException();
            }

            SetValueFromUser(LookupID(propertyName, PropertyAccessPurpose.Write), value);
        }


        internal void SetValueFromUser(int index, object value)
        {
            if( IsReadOnly(index) )
            {
                throw new PropertyReadOnlyException(GetName(index));
            }

            if (null != value)
            {
                Type type = PropertyType(index);
                if (type != value.GetType() && type != typeof(System.Object))
                {
                    throw new PropertyTypeMismatchException(GetName(index), value.GetType().ToString(), type.ToString());
                }
            }

            // check the input for validity
            m_parent.ValidateProperty(GetProperty(index), value);

            if (m_parent.ShouldNotifyPropertyChange)
            {
                if (this.GetValue(index) != value)
                {
                    SetValue(index, value);
                    SetDirty(index, true);
                    m_parent.OnPropertyChanged(GetName(index));
                }
            }
            else
            {
                SetValue(index, value);
                SetDirty(index, true);
            }
        }


        internal void SetValue(int index, object value)
        {
            m_PropertyStorage.SetValue(index,value);
        }

        internal bool IsDirty(int index)
        {
            return m_PropertyStorage.IsDirty(index);
        }

        internal void SetDirty(int index, bool val)
        {
            m_PropertyStorage.SetDirty(index,val);
        }

        internal bool IsRetrieved(int index)
        {
            return m_PropertyStorage.IsRetrieved(index);
        }

        internal void SetAllRetrieved(bool val)
        {
            for (int i = 0; i < m_PropertyStorage.Count; ++i)
            {
                m_PropertyStorage.SetRetrieved(i, val);
            }
        }

        internal void SetRetrieved(int index, bool val)
        {
            m_PropertyStorage.SetRetrieved(index,val);
        }

        internal bool IsEnabled(int index)
        {
            return m_PropertyStorage.IsEnabled(index);
        }

        internal void SetEnabled(int index, bool enabled)
        {
            if (m_parent.ShouldNotifyPropertyMetadataChange)
            {
                if (this.m_PropertyStorage.IsEnabled(index) != enabled)
                {
                    m_PropertyStorage.SetEnabled(index, enabled);
                    m_parent.OnPropertyMetadataChanged(GetName(index));
                }
            }
            else
            {
                m_PropertyStorage.SetEnabled(index, enabled);
            }
        }

        internal Type PropertyType(int index)
        { 
            return GetStaticMetadata(index).PropertyType;
        }

        internal bool IsReadOnly(int index)
        { 
            return GetStaticMetadata(index).ReadOnly;
        }

        internal bool IsExpensive(int index)
        { 
            return GetStaticMetadata(index).Expensive;
        }

        internal bool IsAvailable(int index)
        {
            return IsDirty(index) || IsRetrieved(index);
        }

        internal bool IsNull(int index)
        {
            return m_PropertyStorage.IsNull(index);
        }

        internal bool IsEnumeration(int index)
        {
            return GetStaticMetadata(index).IsEnumeration;
        }

        internal bool IsDesignMode
        {
            get { return ( m_parent is SqlSmoObject && ((SqlSmoObject)m_parent).IsDesignMode); }
        }

        internal PropertyCollection(SmoObjectBase parent, PropertyMetadataProvider pmp)
        { 
            m_pmp = pmp;
            m_parent = parent;
            m_PropertyStorage = PropertyStorageFactory.Create(parent,this.PropertiesMetadata.Count);
        }

        internal bool Dirty
        {
            get
            {
                for(int i = 0; i < m_PropertyStorage.Count; i++)
                {
                    if( IsDirty(i) )
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        internal void SetAllDirty(bool val)
        {
            for(int i = 0; i < m_PropertyStorage.Count; i++)
            {
                SetDirty(i, val);
            }
        }

        internal void SetAllDirtyAsRetrieved(bool val)
        {
            for (int i = 0; i < m_PropertyStorage.Count; i++)
            {
                if (IsDirty(i))
                {
                    SetRetrieved(i, val);
                }
            }
        }

        internal bool ArePropertiesDirty(StringCollection propsList)
        {
            foreach(string propertyName in propsList )
            {
                int index = LookupID(propertyName);
                if( IsDirty(index) )
                {
                    return true;
                }
            }
            return false;
        }

        public Property GetPropertyObject(int index)
        {
            RetrieveProperty(index, IsDesignMode? true : false);
            HandleNullValue(index);

            return Get( index);
        }

        internal Property GetPropertyObjectAllowNull(int index)
        {
            RetrieveProperty(index, IsDesignMode? true : false);
            return Get(index);
        }

        public Property GetPropertyObject(Int32 index, bool doNotLoadPropertyValues)
        {
            if( doNotLoadPropertyValues )
            {
                return Get(index);
            }
            else
            {
                return GetPropertyObject(index);
            }
        }

        public Property GetPropertyObject(string name)
        {
            return GetPropertyObject(LookupID(name));
        }

        internal Property GetPropertyObjectAllowNull(string name)
        {
            return GetPropertyObjectAllowNull(LookupID(name));
        }
        
        public Property GetPropertyObject(string name, bool doNotLoadPropertyValues)
        {
            return Get(LookupID(name));
        }

        /// <summary>
        /// Sets a value indicating whether the named property should be considered changed
        /// </summary>
        /// <param name="propertyName">Name of the property</param>
        /// <param name="isDirty">if true, value is considered changed locally</param>
        public void SetDirty(string propertyName, bool isDirty)
        {
            SetDirty(LookupID(propertyName, PropertyAccessPurpose.Read), isDirty);
        }
#endregion internal

#region public
        public Int32 Count 
        { 
            get 
            {
                return this.PropertiesMetadata.Count;
            } 
        }

        public void CopyTo(Property[] array,Int32 index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        void ICollection.CopyTo(Array array,Int32 index)
        {
            for(int i = 0; i < this.Count; i++)
            {
                array.SetValue(GetProperty(i), index+i);
            }
        }

        public Property this[string name]
        {
            get
            {
                return GetPropertyObject(name);
            }
        }

        public Property this[Int32 index]
        {
            get
            {
                return GetPropertyObject(index);
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new PropertyEnumerator(this);
        }

        public bool Contains(string propertyName) 
        { 
            int index = LookupIDNoBoundCheck(propertyName);
            return index >= 0 && index < m_PropertyStorage.Count;
        }

        internal class PropertyEnumerator : IEnumerator 
        {
            protected PropertyCollection m_propertyCollection;
            int m_currentPos;
            
            public PropertyEnumerator(PropertyCollection propertyCollection) 
            {
                m_propertyCollection = propertyCollection;
                m_currentPos = -1;
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
                    // before returning the Property object to the user
                    if (!m_propertyCollection.IsRetrieved(m_currentPos))
                    {
                        RetrieveProperty(m_currentPos);
                    }

                    return m_propertyCollection.GetProperty(m_currentPos);
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

            protected virtual void RetrieveProperty(int m_currentPos)
            {
                m_propertyCollection.RetrieveProperty(m_currentPos, m_propertyCollection.IsDesignMode ? true : false);
            }
        }

        class SfcPropertyEnumerator : PropertyEnumerator
        {
            public SfcPropertyEnumerator(PropertyCollection propertyCollection)
                : base(propertyCollection)
            {

            }

            protected override void RetrieveProperty(int m_currentPos)
            {
                m_propertyCollection.RetrieveProperty(m_currentPos, true);
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
#endregion public

        #region ISfcPropertySet Members

        Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty GetISfcProperty(int index)
        {
            RetrieveProperty(index, true);
            return Get(index);
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.Contains<T>(string name)
        {
            int index;
            if (this.PropertiesMetadata.TryPropertyNameToIDLookup(name, out index))
            {
                Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty property = this.GetISfcProperty(index);
                if ((property != null) && (property.Type !=null))
                {
                    return typeof(T).GetIsAssignableFrom(property.Type);
                }
            }
            return false;
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.Contains(Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty property)
        {
            if (property!=null)
            {
                int index;
                if (this.PropertiesMetadata.TryPropertyNameToIDLookup(property.Name, out index))
                {
                    Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty prop = this.GetISfcProperty(index);
                    return property.Equals(prop);
                }
            }
            return false;
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.Contains(string propertyName)
        {
            int index;
            if (this.PropertiesMetadata.TryPropertyNameToIDLookup(propertyName, out index))
            {
                Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty prop = this.GetISfcProperty(index);
                return prop != null;
            }
            return false;
        }

        System.Collections.Generic.IEnumerable<Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty> Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.EnumProperties()
        {
            SfcPropertyEnumerator enumerator = new SfcPropertyEnumerator(this);

            while (enumerator.MoveNext())
            {
                yield return (Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty)enumerator.Current;
            }
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.TryGetProperty(string name, out Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty property)
        {
            int index;
            if (this.PropertiesMetadata.TryPropertyNameToIDLookup(name, out index))
            {
                property = this.GetISfcProperty(index);
                return property != null;
            }
            property = null;
            return false;
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.TryGetPropertyValue(string name, out object value)
        {
            return ((Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet)this).TryGetPropertyValue<object>(name, out value);
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcPropertySet.TryGetPropertyValue<T>(string name, out T value)
        {
            value = default(T);
            try
            {
                int index;
                if (this.PropertiesMetadata.TryPropertyNameToIDLookup(name, out index))
                {
                    Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty property = this.GetISfcProperty(index);

                    if (property != null)
                    {
                        value = (T)property.Value;
                        return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        #endregion
    }

    public class SqlPropertyCollection : PropertyCollection
    {
        internal SqlPropertyCollection(SqlSmoObject parent, SqlPropertyMetadataProvider pmp) 
            : base(parent, pmp)
        {
        }

        public SqlPropertyInfo GetPropertyInfo(string name)
        {
            return ((SqlPropertyMetadataProvider)this.PropertiesMetadata).GetPropertyInfo(this.LookupID(name));
        }

        public SqlPropertyInfo[] EnumPropertyInfo(SqlServerVersions versions)
        {
            return ((SqlPropertyMetadataProvider)this.PropertiesMetadata).EnumPropertyInfo(versions);
        }

        public SqlPropertyInfo[] EnumPropertyInfo()
        {
            return EnumPropertyInfo(SqlServerVersions.Version90);
        }
    }
}
