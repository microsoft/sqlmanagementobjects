// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Agent
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class ProxyAccountCollection : SimpleObjectCollectionBase
	{


















		internal ProxyAccountCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public ProxyAccount this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ProxyAccount;
			}
		}


		// returns wrapper class
		public ProxyAccount this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ProxyAccount;
                    
                



















			}
		}


		public void CopyTo(ProxyAccount[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ProxyAccount ItemById(int id)
		{
			return (ProxyAccount)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ProxyAccount);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ProxyAccount(this, key, state);
		}



















		public void Add(ProxyAccount proxyAccount) 
		{
			AddImpl(proxyAccount);
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
