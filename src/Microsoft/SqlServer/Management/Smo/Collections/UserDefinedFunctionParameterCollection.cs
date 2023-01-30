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
    public sealed  class UserDefinedFunctionParameterCollection : ParameterCollectionBase 
	{
		internal UserDefinedFunctionParameterCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public UserDefinedFunction Parent
		{
			get
			{
				return this.ParentInstance as UserDefinedFunction;
			}
		}


		public UserDefinedFunctionParameter this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as UserDefinedFunctionParameter;
			}
		}

		public void CopyTo(UserDefinedFunctionParameter[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public UserDefinedFunctionParameter this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as UserDefinedFunctionParameter;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(UserDefinedFunctionParameter);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new UserDefinedFunctionParameter(this, key, state);
		}


		public void Add(UserDefinedFunctionParameter userDefinedFunctionParameter)
		{
			AddImpl(userDefinedFunctionParameter);
		}

		public void Add(UserDefinedFunctionParameter userDefinedFunctionParameter, string insertAtColumnName)
		{
			AddImpl(userDefinedFunctionParameter, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(UserDefinedFunctionParameter userDefinedFunctionParameter, int insertAtPosition)
		{
			AddImpl(userDefinedFunctionParameter, insertAtPosition);
		}


		public void Remove(UserDefinedFunctionParameter userDefinedFunctionParameter)
		{
			if( null == userDefinedFunctionParameter )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("userDefinedFunctionParameter"));
			
			RemoveObj(userDefinedFunctionParameter, userDefinedFunctionParameter.key);
		}


























		public UserDefinedFunctionParameter ItemById(int id)
		{
			return (UserDefinedFunctionParameter)GetItemById(id);
		}

		
	}
}
