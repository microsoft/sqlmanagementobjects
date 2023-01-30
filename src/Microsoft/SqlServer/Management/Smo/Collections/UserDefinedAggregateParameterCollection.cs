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
    public sealed  class UserDefinedAggregateParameterCollection : ParameterCollectionBase 
	{
		internal UserDefinedAggregateParameterCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public UserDefinedAggregate Parent
		{
			get
			{
				return this.ParentInstance as UserDefinedAggregate;
			}
		}


		public UserDefinedAggregateParameter this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserDefinedAggregateParameter;
			}
		}

		public void CopyTo(UserDefinedAggregateParameter[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public UserDefinedAggregateParameter this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as UserDefinedAggregateParameter;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(UserDefinedAggregateParameter);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new UserDefinedAggregateParameter(this, key, state);
		}


		public void Add(UserDefinedAggregateParameter userDefinedAggregateParameter)
		{
			AddImpl(userDefinedAggregateParameter);
		}

		public void Add(UserDefinedAggregateParameter userDefinedAggregateParameter, string insertAtColumnName)
		{
			AddImpl(userDefinedAggregateParameter, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(UserDefinedAggregateParameter userDefinedAggregateParameter, int insertAtPosition)
		{
			AddImpl(userDefinedAggregateParameter, insertAtPosition);
		}


		public void Remove(UserDefinedAggregateParameter userDefinedAggregateParameter)
		{
			if( null == userDefinedAggregateParameter )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("userDefinedAggregateParameter"));
			
			RemoveObj(userDefinedAggregateParameter, userDefinedAggregateParameter.key);
		}


























		public UserDefinedAggregateParameter ItemById(int id)
		{
			return (UserDefinedAggregateParameter)GetItemById(id);
		}

		
	}
}
