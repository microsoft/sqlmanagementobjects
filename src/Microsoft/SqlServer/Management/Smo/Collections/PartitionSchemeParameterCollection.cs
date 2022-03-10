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
    public sealed  class PartitionSchemeParameterCollection : ParameterCollectionBase 
	{
		internal PartitionSchemeParameterCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}


		public PartitionSchemeParameter this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as PartitionSchemeParameter;
			}
		}

		public void CopyTo(PartitionSchemeParameter[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public PartitionSchemeParameter this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as PartitionSchemeParameter;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(PartitionSchemeParameter);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new PartitionSchemeParameter(this, key, state);
		}


		public void Add(PartitionSchemeParameter partitionSchemeParameter)
		{
			AddImpl(partitionSchemeParameter);
		}

		public void Add(PartitionSchemeParameter partitionSchemeParameter, string insertAtColumnName)
		{
			AddImpl(partitionSchemeParameter, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(PartitionSchemeParameter partitionSchemeParameter, int insertAtPosition)
		{
			AddImpl(partitionSchemeParameter, insertAtPosition);
		}


		public void Remove(PartitionSchemeParameter partitionSchemeParameter)
		{
			if( null == partitionSchemeParameter )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("partitionSchemeParameter"));
			
			RemoveObj(partitionSchemeParameter, partitionSchemeParameter.key);
		}


























		public PartitionSchemeParameter ItemById(int id)
		{
			return (PartitionSchemeParameter)GetItemById(id);
		}

		
	}
}
