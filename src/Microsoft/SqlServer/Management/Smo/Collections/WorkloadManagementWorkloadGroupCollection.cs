// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class WorkloadManagementWorkloadGroupCollection : SimpleObjectCollectionBase
	{


















		internal WorkloadManagementWorkloadGroupCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}

		
		public WorkloadManagementWorkloadGroup this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as WorkloadManagementWorkloadGroup;
			}
		}


		// returns wrapper class
		public WorkloadManagementWorkloadGroup this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as WorkloadManagementWorkloadGroup;
                    
                



















			}
		}


		public void CopyTo(WorkloadManagementWorkloadGroup[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public WorkloadManagementWorkloadGroup ItemById(int id)
		{
			return (WorkloadManagementWorkloadGroup)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(WorkloadManagementWorkloadGroup);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new WorkloadManagementWorkloadGroup(this, key, state);
		}



















		public void Add(WorkloadManagementWorkloadGroup WorkloadManagementWorkloadGroup) 
		{
			AddImpl(WorkloadManagementWorkloadGroup);
		}


		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new SimpleObjectKey(name));
		}


		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");



            if( null == name || name.Length == 0)

				throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            return new SimpleObjectKey(name);        
        }


















	}
}
