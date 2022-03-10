// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo.Broker
{

    ///<summary>
    /// Strongly typed collection of MAPPED_TYPE objects
    /// Supports indexing objects by their Name and Schema properties
    ///</summary>
    public sealed class ServiceQueueCollection : SchemaCollectionBase
	{

		internal ServiceQueueCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public ServiceBroker Parent
		{
			get
			{
				return this.ParentInstance as ServiceBroker;
			}
		}


		public ServiceQueue this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ServiceQueue;
			}
		}

		public ServiceQueue this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as ServiceQueue;
			}
		}

		public ServiceQueue this[string name, string schema]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }
                else if (schema == null)
                {
                    throw new ArgumentNullException("schema cannot be null");
                }

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as ServiceQueue;
			}
		}

		public void CopyTo(ServiceQueue[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public ServiceQueue ItemById(int id)
		{
			return (ServiceQueue)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(ServiceQueue);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new ServiceQueue(this, key, state);
		}

		


































			public void Add(ServiceQueue serviceQueue) 
			{
				if( null == serviceQueue )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("serviceQueue"));
			
				AddImpl(serviceQueue);
			}

		internal SqlSmoObject GetObjectByName(string name)
		{
            if (name == null)
            {
                throw new ArgumentNullException("schema cannot be null");
            }

			return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema()));
		}

	}
}
