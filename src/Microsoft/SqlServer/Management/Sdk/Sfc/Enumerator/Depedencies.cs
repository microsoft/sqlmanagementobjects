// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// describes a dependency relationship: for a Urn a list of dependencies
    /// </summary>
    [ComVisible(false)]
	public class Dependency 
	{
		Urn m_urn;
        bool schemaBound;
		DependencyChainCollection m_links;

		/// <summary>
		/// default constructor
		/// </summary>
		public Dependency()
		{
			m_links = new DependencyChainCollection();
		}

		/// <summary>
		/// copy constructor
		/// </summary>
		/// <param name="dep"></param>
		public Dependency( Dependency dep )
		{
			m_urn   = new Urn( dep.Urn );
            schemaBound = dep.IsSchemaBound;
			m_links = new DependencyChainCollection( dep.Links );
		}

		/// <summary>
		/// Deep copy
		/// </summary>
		/// <returns></returns>
		public Dependency Copy()
		{
			return new Dependency(this);
		}

		/// <summary>
		/// Urn for wich we have dependendencies
		/// </summary>
		/// <value></value>
		public Urn Urn
		{
			get { return m_urn; }
			set { m_urn = value; }
		}

        /// <summary>
        /// If the dependency with the parent is schema bound
        /// </summary>
        public bool IsSchemaBound
        {
            get { return schemaBound; }
            set { schemaBound = value; }
        }

		/// <summary>
		/// list of dependencies
		/// </summary>
		/// <value></value>
		public DependencyChainCollection Links
		{
			get
			{ 
				return m_links; 
			}
		}
	}

	/// <summary>
	/// models a generalized tree of dependencies
	/// </summary>
	[ComVisible(false)]
	public class DependencyChainCollection : ArrayList
	{
		/// <summary>
		/// default constructor
		/// </summary>
		public DependencyChainCollection(){}

		/// <summary>
		/// copy constructor
		/// </summary>
		/// <param name="deps"></param>

        [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DependencyChainCollection(DependencyChainCollection deps)
		{
			for( int i=0; i<deps.Count; i++ )
			{
				Add( deps[i].Copy() );
			}
		}

		/// <summary>
		/// get dependency node by index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public new Dependency this[int index]
		{
			get { return (Dependency)base[index]; }
		}

		/// <summary>
		/// Strongly typed Copy implementation
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyTo(Dependency[] array, Int32 index)
		{
			int idx = 0;
			foreach(DictionaryEntry de in this)
			{
				array.SetValue( (Dependency)de.Value, idx++);
			}
		}

		/// <summary>
		/// Strongly typed Add implementation
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public int Add(Dependency value)
		{
			return this.Add((object)value);
		}

		/// <summary>
		/// Strongly typed Insert implementation
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public void Insert(int index, Dependency value)
		{
			this.Insert(index, (object)value);
		}

		/// <summary>
		/// Strongly typed IndexOf implementation
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public int IndexOf(Dependency value)
		{
			return this.IndexOf((object)value);
		}

		/// <summary>
		/// Strongly typed Contains implementation
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool Contains(Dependency value)
		{
			return this.Contains((object)value);
		}

		/// <summary>
		/// Strongly typed Remove implementation
		/// </summary>
		/// <param name="value"></param>
		public void Remove(Dependency value)
		{
			this.Remove((object)value);
		}
	}
}
			
