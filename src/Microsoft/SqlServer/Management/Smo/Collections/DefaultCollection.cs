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
    public sealed class DefaultCollection : SchemaCollectionBase
	{

		internal DefaultCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}


		public Default this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Default;
			}
		}

		public Default this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as Default;
			}
		}

		public Default this[string name, string schema]
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

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as Default;
			}
		}

		public void CopyTo(Default[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public Default ItemById(int id)
		{
			return (Default)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(Default);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new Default(this, key, state);
		}

		


































			public void Add(Default def) 
			{
				if( null == def )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("def"));
			
				AddImpl(def);
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
