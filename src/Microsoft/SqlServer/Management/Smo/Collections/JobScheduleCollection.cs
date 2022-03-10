// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590, 1591, 1592, 1573, 1571, 1570, 1572, 1587

























namespace Microsoft.SqlServer.Management.Smo.Agent
{

    ///<summary>
    /// Strongly typed collection of MAPPED_TYPE objects
    /// Supports indexing objects by their Name and Schema properties
    ///</summary>
    public sealed class JobScheduleCollection : JobScheduleCollectionBase
	{

		internal JobScheduleCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}


		public JobSchedule this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as JobSchedule;
			}
		}

		public JobSchedule this[string name]
		{
			get
			{
				return GetObjectByKey(new ScheduleObjectKey(name, GetDefaultID())) as JobSchedule;
			}
		}

		public void CopyTo(JobSchedule[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}

		public JobSchedule ItemById(int id)
		{
			IEnumerator ie = ((IEnumerable)this).GetEnumerator();
			while (ie.MoveNext())
			{
				JobSchedule c = (JobSchedule)ie.Current;

				if (c.ID == id) // found object with the right id
					return c;
			}
			return null;

		}

		protected override Type GetCollectionElementType()
		{
			return typeof(JobSchedule);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return new JobSchedule(this, key, state);
		}























		
		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new ScheduleObjectKey(name, GetDefaultID()));
		}

	}
}
