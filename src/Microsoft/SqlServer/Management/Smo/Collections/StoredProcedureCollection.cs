// Copyright (c) Microsoft.
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
    public sealed class StoredProcedureCollection : SchemaCollectionBase
	{

		internal StoredProcedureCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}


		public StoredProcedure this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as StoredProcedure;
			}
		}

		public StoredProcedure this[string name]
		{
			get
			{
                if (name == null)
                {
                    throw new ArgumentNullException("name cannot be null");
                }

		        return GetObjectByKey(new SchemaObjectKey(name, GetDefaultSchema())) as StoredProcedure;
			}
		}

		public StoredProcedure this[string name, string schema]
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

                return GetObjectByKey(new SchemaObjectKey(name, schema)) as StoredProcedure;
			}
		}

		public void CopyTo(StoredProcedure[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		public StoredProcedure ItemById(int id)
		{
			return (StoredProcedure)GetItemById(id);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(StoredProcedure);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new StoredProcedure(this, key, state);
		}

		


































			public void Add(StoredProcedure storedProcedure) 
			{
				if( null == storedProcedure )
					throw new FailedOperationException(ExceptionTemplates.AddCollection, this, new ArgumentNullException("storedProcedure"));
			
				AddImpl(storedProcedure);
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
