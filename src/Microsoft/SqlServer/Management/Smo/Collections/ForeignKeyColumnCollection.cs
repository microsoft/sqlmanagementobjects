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
    public sealed  class ForeignKeyColumnCollection : ParameterCollectionBase 
	{
		internal ForeignKeyColumnCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public ForeignKey Parent
		{
			get
			{
				return this.ParentInstance as ForeignKey;
			}
		}


		public ForeignKeyColumn this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ForeignKeyColumn;
			}
		}

		public void CopyTo(ForeignKeyColumn[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public ForeignKeyColumn this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as ForeignKeyColumn;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(ForeignKeyColumn);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new ForeignKeyColumn(this, key, state);
		}


		public void Add(ForeignKeyColumn foreignKeyColumn)
		{
			AddImpl(foreignKeyColumn);
		}

		public void Add(ForeignKeyColumn foreignKeyColumn, string insertAtColumnName)
		{
			AddImpl(foreignKeyColumn, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(ForeignKeyColumn foreignKeyColumn, int insertAtPosition)
		{
			AddImpl(foreignKeyColumn, insertAtPosition);
		}


		public void Remove(ForeignKeyColumn foreignKeyColumn)
		{
			if( null == foreignKeyColumn )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("foreignKeyColumn"));
			
			RemoveObj(foreignKeyColumn, foreignKeyColumn.key);
		}


























		public ForeignKeyColumn ItemById(int id)
		{
			return (ForeignKeyColumn)GetItemById(id);
		}

		
	}
}
