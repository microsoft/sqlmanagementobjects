// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587



























namespace Microsoft.SqlServer.Management.Smo.Agent
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class JobStepCollection : ParameterCollectionBase 
	{
		internal JobStepCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public Job Parent
		{
			get
			{
				return this.ParentInstance as Job;
			}
		}


		public JobStep this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as JobStep;
			}
		}

		public void CopyTo(JobStep[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}


		// returns wrapper class
		public JobStep this[string name]
		{
			get
			{
				return GetObjectByKey( new SimpleObjectKey(name)) as JobStep;
			}
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(JobStep);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new JobStep(this, key, state);
		}


		public void Add(JobStep jobStep)
		{
			AddImpl(jobStep);
		}

		public void Add(JobStep jobStep, string insertAtColumnName)
		{
			AddImpl(jobStep, new SimpleObjectKey(insertAtColumnName));
		}
		
		public void Add(JobStep jobStep, int insertAtPosition)
		{
			AddImpl(jobStep, insertAtPosition);
		}


		public void Remove(JobStep jobStep)
		{
			if( null == jobStep )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("jobStep"));
			
			RemoveObj(jobStep, jobStep.key);
		}


























		public JobStep ItemById(int id)
		{
			return (JobStep)GetItemById(id);
		}

		
	}
}
