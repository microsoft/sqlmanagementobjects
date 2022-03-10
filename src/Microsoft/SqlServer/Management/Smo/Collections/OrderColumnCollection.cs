// Copyright (c) Microsoft.
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
    public sealed  class OrderColumnCollection : ParameterCollectionBase 
	{
		internal OrderColumnCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public UserDefinedFunction Parent
		{
			get
			{
				return this.ParentInstance as UserDefinedFunction;
			}
		}


		public OrderColumn this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as OrderColumn;
			}
		}

		public void CopyTo(OrderColumn[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public OrderColumn this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as OrderColumn;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(OrderColumn);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new OrderColumn(this, key, state);
		}


		public void Add(OrderColumn orderColumn)
		{
			AddImpl(orderColumn);
		}

		public void Add(OrderColumn orderColumn, string insertAtColumnName)
		{
			AddImpl(orderColumn, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(OrderColumn orderColumn, int insertAtPosition)
		{
			AddImpl(orderColumn, insertAtPosition);
		}


		public void Remove(OrderColumn orderColumn)
		{
			if( null == orderColumn )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("orderColumn"));
			
			RemoveObj(orderColumn, orderColumn.key);
		}


























		public OrderColumn ItemById(int id)
		{
			return (OrderColumn)GetItemById(id);
		}

		
	}
}
