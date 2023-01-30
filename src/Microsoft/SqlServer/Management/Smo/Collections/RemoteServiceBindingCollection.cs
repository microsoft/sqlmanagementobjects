// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Broker
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class RemoteServiceBindingCollection : SimpleObjectCollectionBase
	{


















		internal RemoteServiceBindingCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public ServiceBroker Parent
		{
			get
			{
				return this.ParentInstance as ServiceBroker;
			}
		}

		
		public RemoteServiceBinding this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as RemoteServiceBinding;
			}
		}


		// returns wrapper class
		public RemoteServiceBinding this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as RemoteServiceBinding;
                    
                



















			}
		}


		public void CopyTo(RemoteServiceBinding[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public RemoteServiceBinding ItemById(int id)
		{
			return (RemoteServiceBinding)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(RemoteServiceBinding);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new RemoteServiceBinding(this, key, state);
		}



















		public void Add(RemoteServiceBinding remoteServiceBinding) 
		{
			AddImpl(remoteServiceBinding);
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
