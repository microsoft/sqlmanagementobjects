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
    public sealed  class ColumnCollection : ParameterCollectionBase 
	{
		internal ColumnCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}


		public Column this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Column;
			}
		}

		public void CopyTo(Column[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public Column this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as Column;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(Column);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new Column(this, key, state);
		}


		public void Add(Column column)
		{
			AddImpl(column);
		}

		public void Add(Column column, string insertAtColumnName)
		{
			AddImpl(column, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(Column column, int insertAtPosition)
		{
			AddImpl(column, insertAtPosition);
		}


		public void Remove(Column column)
		{
			if( null == column )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("column"));
			
			RemoveObj(column, column.key);
		}


























		public Column ItemById(int id)
		{
			return (Column)GetItemById(id);
		}

		
	}
}
