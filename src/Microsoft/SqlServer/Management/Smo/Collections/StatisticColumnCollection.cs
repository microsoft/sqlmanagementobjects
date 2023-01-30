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
    public sealed  class StatisticColumnCollection : ParameterCollectionBase 
	{
		internal StatisticColumnCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Statistic Parent
		{
			get
			{
				return this.ParentInstance as Statistic;
			}
		}


		public StatisticColumn this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as StatisticColumn;
			}
		}

		public void CopyTo(StatisticColumn[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public StatisticColumn this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as StatisticColumn;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(StatisticColumn);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new StatisticColumn(this, key, state);
		}


		public void Add(StatisticColumn statisticColumn)
		{
			AddImpl(statisticColumn);
		}

		public void Add(StatisticColumn statisticColumn, string insertAtColumnName)
		{
			AddImpl(statisticColumn, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(StatisticColumn statisticColumn, int insertAtPosition)
		{
			AddImpl(statisticColumn, insertAtPosition);
		}


		public void Remove(StatisticColumn statisticColumn)
		{
			if( null == statisticColumn )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("statisticColumn"));
			
			RemoveObj(statisticColumn, statisticColumn.key);
		}


























		public StatisticColumn ItemById(int id)
		{
			return (StatisticColumn)GetItemById(id);
		}

		
	}
}
