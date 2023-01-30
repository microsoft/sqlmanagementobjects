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
    public sealed class UserDefinedDataTypeCollection : SchemaCollectionBase
	{

		internal UserDefinedDataTypeCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}


		public UserDefinedDataType this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserDefinedDataType;
			}
		}

		public UserDefinedDataType this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as UserDefinedDataType;
			}
		}

		public UserDefinedDataType this[string name, string schema]
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

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as UserDefinedDataType;
			}
		}

		public void CopyTo(UserDefinedDataType[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public UserDefinedDataType ItemById(int id)
		{
			return (UserDefinedDataType)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(UserDefinedDataType);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new UserDefinedDataType(this, key, state);
		}

		


































			public void Add(UserDefinedDataType userDefinedDataType) 
			{
				if( null == userDefinedDataType )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("userDefinedDataType"));
			
				AddImpl(userDefinedDataType);
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
