// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587



























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class IndexedColumnCollection : ParameterCollectionBase 
	{
		internal IndexedColumnCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Index Parent
		{
			get
			{
				return this.ParentInstance as Index;
			}
		}


		public IndexedColumn this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as IndexedColumn;
			}
		}

		public void CopyTo(IndexedColumn[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public IndexedColumn this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as IndexedColumn;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(IndexedColumn);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new IndexedColumn(this, key, state);
		}


		public void Add(IndexedColumn indexedColumn)
		{
			AddImpl(indexedColumn);
		}

		public void Add(IndexedColumn indexedColumn, string insertAtColumnName)
		{
			AddImpl(indexedColumn, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(IndexedColumn indexedColumn, int insertAtPosition)
		{
			AddImpl(indexedColumn, insertAtPosition);
		}


		public void Remove(IndexedColumn indexedColumn)
		{
			if( null == indexedColumn )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("indexedColumn"));
			
			RemoveObj(indexedColumn, indexedColumn.key);
		}


























		public IndexedColumn ItemById(int id)
		{
			return (IndexedColumn)GetItemById(id);
		}

		
	}
}
