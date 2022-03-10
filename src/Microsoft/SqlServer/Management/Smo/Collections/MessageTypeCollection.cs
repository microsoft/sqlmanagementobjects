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
    public sealed  class MessageTypeCollection : SimpleObjectCollectionBase
	{

		//has custom string comparer
		StringComparer m_comparer;

		//must initialize in constructor
		internal MessageTypeCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base(parentInstance)
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

		
		public MessageType this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as MessageType;
			}
		}


		// returns wrapper class
		public MessageType this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as MessageType;
                    
                



















			}
		}


		public void CopyTo(MessageType[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public MessageType ItemById(int id)
		{
			return (MessageType)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(MessageType);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new MessageType(this, key, state);
		}



















		public void Add(MessageType messageType) 
		{
			AddImpl(messageType);
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
