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
    public sealed  class ResourcePoolCollection : SimpleObjectCollectionBase
	{


















		internal ResourcePoolCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public ResourceGovernor Parent
		{
			get
			{
				return this.ParentInstance as ResourceGovernor;
			}
		}

		
		public ResourcePool this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ResourcePool;
			}
		}


		// returns wrapper class
		public ResourcePool this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ResourcePool;
                    
                



















			}
		}


		public void CopyTo(ResourcePool[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ResourcePool ItemById(int id)
		{
			return (ResourcePool)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ResourcePool);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ResourcePool(this, key, state);
		}



















		public void Add(ResourcePool resourcePool) 
		{
			AddImpl(resourcePool);
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
