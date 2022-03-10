// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
namespace Microsoft.SqlServer.Management.Smo
{
    public class Property : Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty
	{
#region private
		PropertyCollection m_propertyCollection;
		int m_propertyIndex;
#endregion private

#region internal
		internal Property(PropertyCollection propertyCollection, int propertyIndex)
		{
			m_propertyCollection = propertyCollection;
			m_propertyIndex = propertyIndex;
		}

		internal Property(Property p)
		{
			m_propertyCollection = p.m_propertyCollection;
			m_propertyIndex = p.m_propertyIndex;
		}

#endregion internal

        #region public
		public string Name
		{
			get { return m_propertyCollection.GetName(m_propertyIndex); }
		}

		public object Value 
		{ 
			get 
			{ 
				return m_propertyCollection.GetValue(m_propertyIndex); 
			}
			set 
			{
				if (null == value)
				{
					throw new ArgumentNullException();
				}

				m_propertyCollection.SetValueFromUser(m_propertyIndex, value); 
			}
		}

		internal void SetValue(object value)
		{
			m_propertyCollection.SetValue(m_propertyIndex, value);
		}

		internal PropertyCollection Parent
		{
			get { return m_propertyCollection; }
		}

		internal void SetRetrieved(bool retrieved)
		{
			m_propertyCollection.SetRetrieved(m_propertyIndex, retrieved);
		}

		internal void SetDirty(bool dirty)
		{
			m_propertyCollection.SetDirty(m_propertyIndex, dirty);
		}

        internal void SetEnabled(bool enabled)
        {
            m_propertyCollection.SetEnabled(m_propertyIndex, enabled);
        }

		internal bool Enumeration
		{
			get
			{
				return m_propertyCollection.IsEnumeration(m_propertyIndex);
			}
		}

		internal Property(object o1, object o2, object o3)
		{
		}

		public Type Type
		{
			get { return m_propertyCollection.PropertyType(m_propertyIndex); }
		}

		public bool Writable 
		{ 
			get { return !m_propertyCollection.IsReadOnly(m_propertyIndex); } 
		}

		public bool Readable 
		{ 
			get { return true; } 
		}

		public bool Expensive
		{ 
			get { return m_propertyCollection.IsExpensive(m_propertyIndex); } 
		}

		public bool Dirty
		{ 
			get { return m_propertyCollection.IsDirty(m_propertyIndex); } 
		}

		public bool Retrieved
		{ 
			get { return m_propertyCollection.IsAvailable(m_propertyIndex); } 
		}

		public bool IsNull
		{
			get { return m_propertyCollection.IsNull(m_propertyIndex); } 
		}

		public override string ToString()
		{
			return string.Format(SmoApplication.DefaultCulture, "Name={0}/Type={1}/Writable={2}/Value={3}", 
				this.Name, this.Type.ToString(), this.Writable, !this.IsNull ? this.Value : "null");
		}

		public int CompareTo(object obj)
		{
			if(null == obj)
            {
                return -1;
            }
            else
            {
                return string.Compare(Name, ((Property)obj).Name, StringComparison.Ordinal );
            }
        }

		public override bool Equals(object o) 
		{
			return (0 == CompareTo(o));
		}

		public static bool operator==(Property prop1, Property prop2)
		{
			if( null == (object)prop1 && null == (object)prop2 )
            {
                return true;
            }

            if ( null == (object)prop1 )
            {
                return false;
            }

            return prop1.Equals(prop2);
		}

		public static bool operator !=(Property prop1, Property prop2) 
		{
			return !(prop1 ==  prop2);
		}
		
		public static bool operator >(Property prop1, Property prop2) 
		{
			return (0 < prop1.CompareTo(prop2));
		}
		
		public static bool operator <(Property prop1, Property prop2) 
		{
			return (0 > prop1.CompareTo(prop2));
		}
		
		// Override the Object.GetHashCode() method:
		public override int GetHashCode() 
		{
			return this.Name.GetHashCode();
		}
#endregion public

        #region ISfcProperty Members

        System.ComponentModel.AttributeCollection Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Attributes
        {
            get { return new System.ComponentModel.AttributeCollection(); }
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Dirty
        {
            get { return this.Dirty; }
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Enabled
        {
            get { return m_propertyCollection.IsEnabled(m_propertyIndex); }
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.IsNull
        {
            get { return this.IsNull; }
        }

        string Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Name
        {
            get { return this.Name; }
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Required
        {
            get { return false; }
        }

        Type Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Type
        {
            get { return this.Type; }
        }

        object Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Value
        {
            get
            {
                return this.Value;
            }
            set
            {
                this.Value = value;
            }
        }

        bool Microsoft.SqlServer.Management.Sdk.Sfc.ISfcProperty.Writable
        {
            get { return this.Writable; }
        }

        #endregion
    }
}
