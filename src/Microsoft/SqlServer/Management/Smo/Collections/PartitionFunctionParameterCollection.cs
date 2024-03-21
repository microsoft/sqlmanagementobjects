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
    public sealed  class PartitionFunctionParameterCollection : ParameterCollectionBase 
	{
		internal PartitionFunctionParameterCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public PartitionFunction Parent
		{
			get
			{
				return this.ParentInstance as PartitionFunction;
			}
		}


		public PartitionFunctionParameter this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as PartitionFunctionParameter;
			}
		}

		public void CopyTo(PartitionFunctionParameter[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}












		protected override Type GetCollectionElementType()
		{
			return typeof(PartitionFunctionParameter);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new PartitionFunctionParameter(this, key, state);
		}


		public void Add(PartitionFunctionParameter partitionFunctionParameter)
		{
			AddImpl(partitionFunctionParameter);
		}

		public void Add(PartitionFunctionParameter partitionFunctionParameter, string insertAtColumnName)
		{
			AddImpl(partitionFunctionParameter, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(PartitionFunctionParameter partitionFunctionParameter, int insertAtPosition)
		{
			AddImpl(partitionFunctionParameter, insertAtPosition);
		}


		public void Remove(PartitionFunctionParameter partitionFunctionParameter)
		{
			if( null == partitionFunctionParameter )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("partitionFunctionParameter"));
			
			RemoveObj(partitionFunctionParameter, partitionFunctionParameter.key);
		}


























		public PartitionFunctionParameter ItemById(int id)
		{
			return (PartitionFunctionParameter)GetItemById(id);
		}

		
	}
}
