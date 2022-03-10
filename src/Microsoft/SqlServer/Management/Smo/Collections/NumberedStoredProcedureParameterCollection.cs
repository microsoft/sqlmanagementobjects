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
    public sealed  class NumberedStoredProcedureParameterCollection : ParameterCollectionBase 
	{
		internal NumberedStoredProcedureParameterCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public NumberedStoredProcedure Parent
		{
			get
			{
				return this.ParentInstance as NumberedStoredProcedure;
			}
		}


		public NumberedStoredProcedureParameter this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as NumberedStoredProcedureParameter;
			}
		}

		public void CopyTo(NumberedStoredProcedureParameter[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public NumberedStoredProcedureParameter this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as NumberedStoredProcedureParameter;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(NumberedStoredProcedureParameter);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new NumberedStoredProcedureParameter(this, key, state);
		}


		public void Add(NumberedStoredProcedureParameter numberedStoredProcedureParameter)
		{
			AddImpl(numberedStoredProcedureParameter);
		}

		public void Add(NumberedStoredProcedureParameter numberedStoredProcedureParameter, string insertAtColumnName)
		{
			AddImpl(numberedStoredProcedureParameter, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(NumberedStoredProcedureParameter numberedStoredProcedureParameter, int insertAtPosition)
		{
			AddImpl(numberedStoredProcedureParameter, insertAtPosition);
		}


		public void Remove(NumberedStoredProcedureParameter numberedStoredProcedureParameter)
		{
			if( null == numberedStoredProcedureParameter )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("numberedStoredProcedureParameter"));
			
			RemoveObj(numberedStoredProcedureParameter, numberedStoredProcedureParameter.key);
		}


























		public NumberedStoredProcedureParameter ItemById(int id)
		{
			return (NumberedStoredProcedureParameter)GetItemById(id);
		}

		
	}
}
