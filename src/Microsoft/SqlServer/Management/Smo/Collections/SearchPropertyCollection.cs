// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class SearchPropertyCollection : SimpleObjectCollectionBase
	{


















		internal SearchPropertyCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SearchPropertyList Parent
		{
			get
			{
				return this.ParentInstance as SearchPropertyList;
			}
		}

		
		public SearchProperty this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as SearchProperty;
			}
		}


		// returns wrapper class
		public SearchProperty this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as SearchProperty;
                    
                



















			}
		}


		public void CopyTo(SearchProperty[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public SearchProperty ItemById(int id)
		{
			return (SearchProperty)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(SearchProperty);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new SearchProperty(this, key, state);
		}



















		public void Add(SearchProperty searchProperty) 
		{
			AddImpl(searchProperty);
		}


		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new SimpleObjectKey(name));
		}


		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");



            if( null == name || name.Length == 0)

				throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            return new SimpleObjectKey(name);        
        }

        /// <summary>
        /// Initializes the storage
        /// </summary>
        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new SimpleObjectCaseSensitiveComparer());
        }
		
        internal class SimpleObjectCaseSensitiveComparer : IComparer
        {
            int IComparer.Compare(object obj1, object obj2)
            {
                return string.Compare((obj1 as SimpleObjectKey).Name, (obj2 as SimpleObjectKey).Name, false, SmoApplication.DefaultCulture);
            }
        }


	}
}
