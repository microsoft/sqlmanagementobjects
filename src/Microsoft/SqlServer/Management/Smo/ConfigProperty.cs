// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Reflection;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Instance class encapsulating SQL Server database table trigger
    /// </summary>

    public sealed class ConfigProperty
{
	private ConfigurationBase m_configbase;
	private int m_iNumber;
	internal ConfigProperty(ConfigurationBase configbase, int number)
	{
		m_configbase = configbase;
		m_iNumber= number;
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.String DisplayName
	{
		get
		{
			return (System.String)m_configbase.GetConfigProperty( m_iNumber, "Name" );
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Int32 Number
	{
		get
		{
			return GetIntProperty("Number");
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Int32 Minimum
	{
		get
		{
			return GetIntProperty("Minimum");
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Int32 Maximum
	{
		get
		{
			return GetIntProperty("Maximum");
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Boolean IsDynamic
	{
		get
		{
			return GetBoolProperty("Dynamic");
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Boolean IsAdvanced
	{
		get
		{
			return GetBoolProperty("Advanced");
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public string Description
	{
		get
		{
			return (System.String)m_configbase.GetConfigProperty( m_iNumber, "Description" );
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Int32 RunValue
	{
		get
		{
			return GetIntProperty("RunValue");
		}
	}

	/// <summary>
	/// TODO
    /// </summary>
	public System.Int32 ConfigValue
	{
		get
		{
			return GetIntProperty("Value");
		}

		set
		{
			m_configbase.SetConfigProperty( m_iNumber, value );
		}
	}

	private int GetIntProperty(string propertyName)
	{
		Object o = m_configbase.GetConfigProperty(m_iNumber, propertyName);
		if (null == o || typeof(System.DBNull) == o.GetType())
		{
			return 0;
		}
		return (System.Int32)o;
	}

	private bool GetBoolProperty(string propertyName)
	{
		Object o = m_configbase.GetConfigProperty(m_iNumber, propertyName);
		if (null == o || typeof(System.DBNull) == o.GetType())
		{
			return false;
		}
		return (System.Boolean)o;
	}
}

	public sealed class ConfigPropertyCollection : ICollection
	{
		private ConfigurationBase m_parent;
		internal ConfigPropertyCollection(ConfigurationBase parent) 
		{ 
			m_parent = parent;
		}

		
		void ICollection.CopyTo(Array array,Int32 index)
		{
			int idx = 0;
			foreach(ConfigProperty p in this)
			{
				array.SetValue(p, idx++);
			}
		}

		public void CopyTo(ConfigProperty[] array, Int32 index)
		{
			int idx = 0;
			foreach(ConfigProperty p in this)
			{
				array.SetValue(p, idx++);
			}
		}
		
		public IEnumerator GetEnumerator()
		{
			if( null == m_parent.ConfigDataTable )
            {
                m_parent.PopulateDataTable();
            }

            return new ConfigPropertyEnumerator(this);
		}

		public Int32 Count 
		{ 
			get 
			{
				if( null == m_parent.ConfigDataTable )
                {
                    m_parent.PopulateDataTable();
                }

                return m_parent.ConfigDataTable.Rows.Count;
			} 
		}

		public bool IsSynchronized
		{
			get
			{
				if( null == m_parent.ConfigDataTable )
                {
                    m_parent.PopulateDataTable();
                }

                return m_parent.ConfigDataTable.Rows.IsSynchronized;
			}
				
		}

		public object SyncRoot
		{
			get
			{
				if( null == m_parent.ConfigDataTable )
                {
                    m_parent.PopulateDataTable();
                }

                return m_parent.ConfigDataTable.Rows.SyncRoot;
			}
		}
		
		public ConfigProperty this[Int32 index] 
		{ 
			get
			{
				if( null == m_parent.ConfigDataTable )
                {
                    m_parent.PopulateDataTable();
                }

                return new ConfigProperty(m_parent, (int)m_parent.ConfigDataTable.Rows[index]["Number"]);
			}
		}

		public ConfigProperty this[string name] 
		{ 
			get
			{
				if( null == m_parent.ConfigDataTable )
                {
                    m_parent.PopulateDataTable();
                }

                try
				{
					return (ConfigProperty)m_parent.GetType().InvokeMember(name, 
						BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Instance,
						null, m_parent, null, SmoApplication.DefaultCulture ) ;
				}
				catch(MissingMethodException)
				{
					return null;
				}
			}
		}

		///<summary>
		/// nested enumerator class. It basically uses SortedList enumerations.
		///</summary>
		internal class ConfigPropertyEnumerator : IEnumerator 
		{
			private int m_idx;
			private ConfigPropertyCollection m_col;
			
			public ConfigPropertyEnumerator(ConfigPropertyCollection col) 
			{
				m_idx = -1;
				m_col = col;
			}

			object IEnumerator.Current 
			{
				get
				{
					return m_col[m_idx];
				}
			}

			public bool MoveNext() 
			{
				return ++m_idx < m_col.Count;
			}
			
			public void Reset() 
			{
				m_idx = -1;
			}
		}
	}
}

