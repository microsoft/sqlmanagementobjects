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
    public sealed class UserDefinedFunctionCollection : SchemaCollectionBase
	{

		internal UserDefinedFunctionCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}


		public UserDefinedFunction this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserDefinedFunction;
			}
		}

		public UserDefinedFunction this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as UserDefinedFunction;
			}
		}

		public UserDefinedFunction this[string name, string schema]
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

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as UserDefinedFunction;
			}
		}

		public void CopyTo(UserDefinedFunction[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public UserDefinedFunction ItemById(int id)
		{
			return (UserDefinedFunction)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(UserDefinedFunction);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new UserDefinedFunction(this, key, state);
		}

		


































			public void Add(UserDefinedFunction userDefinedFunction) 
			{
				if( null == userDefinedFunction )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("userDefinedFunction"));
			
				AddImpl(userDefinedFunction);
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
