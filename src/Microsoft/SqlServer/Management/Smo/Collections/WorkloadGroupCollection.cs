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
    public sealed  class WorkloadGroupCollection : SimpleObjectCollectionBase
	{


















		internal WorkloadGroupCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public ResourcePool Parent
		{
			get
			{
				return this.ParentInstance as ResourcePool;
			}
		}

		
		public WorkloadGroup this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as WorkloadGroup;
			}
		}


		// returns wrapper class
		public WorkloadGroup this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as WorkloadGroup;
                    
                



















			}
		}


		public void CopyTo(WorkloadGroup[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public WorkloadGroup ItemById(int id)
		{
			return (WorkloadGroup)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(WorkloadGroup);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new WorkloadGroup(this, key, state);
		}



















		public void Add(WorkloadGroup workloadGroup) 
		{
			AddImpl(workloadGroup);
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
