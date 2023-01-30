// Copyright (c) Microsoft Corporation.
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
    public sealed  class IndexCollection : SimpleObjectCollectionBase
	{


















		internal IndexCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		
		public Index this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Index;
			}
		}


		// returns wrapper class
		public Index this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as Index;
                    
                



















			}
		}


		public void CopyTo(Index[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Index ItemById(int id)
		{
			return (Index)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(Index);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Index(this, key, state);
		}




		public void Remove(Index index)
		{
			if( null == index )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("index"));
			
			RemoveObj(index, new SimpleObjectKey(index.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(Index index) 
		{
			AddImpl(index);
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


















	}
}
