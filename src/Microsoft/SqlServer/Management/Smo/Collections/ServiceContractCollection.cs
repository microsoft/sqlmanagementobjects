// Copyright (c) Microsoft.
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
    public sealed  class ServiceContractCollection : SimpleObjectCollectionBase
	{

		//has custom string comparer
		StringComparer m_comparer;

		//must initialize in constructor
		internal ServiceContractCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base(parentInstance)
		{
			m_comparer = comparer;
		}

		override internal StringComparer StringComparer
		{
			get 
			{
				return m_comparer;
			}
		}







		public ServiceBroker Parent
		{
			get
			{
				return this.ParentInstance as ServiceBroker;
			}
		}

		
		public ServiceContract this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ServiceContract;
			}
		}


		// returns wrapper class
		public ServiceContract this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ServiceContract;
                    
                



















			}
		}


		public void CopyTo(ServiceContract[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ServiceContract ItemById(int id)
		{
			return (ServiceContract)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ServiceContract);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ServiceContract(this, key, state);
		}



















		public void Add(ServiceContract serviceContract) 
		{
			AddImpl(serviceContract);
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
