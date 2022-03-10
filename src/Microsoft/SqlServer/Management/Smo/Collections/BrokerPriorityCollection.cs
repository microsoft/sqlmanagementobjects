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
    public sealed  class BrokerPriorityCollection : SimpleObjectCollectionBase
	{

		//has custom string comparer
		StringComparer m_comparer;

		//must initialize in constructor
		internal BrokerPriorityCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base(parentInstance)
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

		
		public BrokerPriority this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as BrokerPriority;
			}
		}


		// returns wrapper class
		public BrokerPriority this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as BrokerPriority;
                    
                



















			}
		}


		public void CopyTo(BrokerPriority[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public BrokerPriority ItemById(int id)
		{
			return (BrokerPriority)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(BrokerPriority);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new BrokerPriority(this, key, state);
		}



















		public void Add(BrokerPriority brokerPriority) 
		{
			AddImpl(brokerPriority);
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
