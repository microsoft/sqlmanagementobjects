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
    public sealed  class StoredProcedureParameterCollection : ParameterCollectionBase 
	{
		internal StoredProcedureParameterCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public StoredProcedure Parent
		{
			get
			{
				return this.ParentInstance as StoredProcedure;
			}
		}


		public StoredProcedureParameter this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as StoredProcedureParameter;
			}
		}

		public void CopyTo(StoredProcedureParameter[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public StoredProcedureParameter this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as StoredProcedureParameter;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(StoredProcedureParameter);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new StoredProcedureParameter(this, key, state);
		}


		public void Add(StoredProcedureParameter storedProcedureParameter)
		{
			AddImpl(storedProcedureParameter);
		}

		public void Add(StoredProcedureParameter storedProcedureParameter, string insertAtColumnName)
		{
			AddImpl(storedProcedureParameter, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(StoredProcedureParameter storedProcedureParameter, int insertAtPosition)
		{
			AddImpl(storedProcedureParameter, insertAtPosition);
		}


		public void Remove(StoredProcedureParameter storedProcedureParameter)
		{
			if( null == storedProcedureParameter )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("storedProcedureParameter"));
			
			RemoveObj(storedProcedureParameter, storedProcedureParameter.key);
		}


























		public StoredProcedureParameter ItemById(int id)
		{
			return (StoredProcedureParameter)GetItemById(id);
		}

		
	}
}
