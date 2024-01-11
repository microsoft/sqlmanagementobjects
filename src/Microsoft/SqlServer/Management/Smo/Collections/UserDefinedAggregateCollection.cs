// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed collection of MAPPED_TYPE objects
    /// Supports indexing objects by their Name and Schema properties
    ///</summary>
    public sealed class UserDefinedAggregateCollection : SchemaCollectionBase
	{

		internal UserDefinedAggregateCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}


		public UserDefinedAggregate this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserDefinedAggregate;
			}
		}

		public UserDefinedAggregate this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as UserDefinedAggregate;
			}
		}

		public UserDefinedAggregate this[string name, string schema]
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

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as UserDefinedAggregate;
			}
		}

		public void CopyTo(UserDefinedAggregate[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public UserDefinedAggregate ItemById(int id)
		{
			return (UserDefinedAggregate)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(UserDefinedAggregate);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new UserDefinedAggregate(this, key, state);
		}

		


































			public void Add(UserDefinedAggregate userDefinedAggregate) 
			{
				if( null == userDefinedAggregate )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("userDefinedAggregate"));
			
				AddImpl(userDefinedAggregate);
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
