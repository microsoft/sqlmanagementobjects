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
    public sealed  class ExternalResourcePoolCollection : SimpleObjectCollectionBase
	{


















		internal ExternalResourcePoolCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public ResourceGovernor Parent
		{
			get
			{
				return this.ParentInstance as ResourceGovernor;
			}
		}

		
		public ExternalResourcePool this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ExternalResourcePool;
			}
		}


		// returns wrapper class
		public ExternalResourcePool this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ExternalResourcePool;
                    
                



















			}
		}


		public void CopyTo(ExternalResourcePool[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ExternalResourcePool ItemById(int id)
		{
			return (ExternalResourcePool)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ExternalResourcePool);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ExternalResourcePool(this, key, state);
		}



















		public void Add(ExternalResourcePool externalResourcePool) 
		{
			AddImpl(externalResourcePool);
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
