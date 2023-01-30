// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Agent
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class JobCategoryCollection : SimpleObjectCollectionBase
	{


















		internal JobCategoryCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public JobCategory this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as JobCategory;
			}
		}


		// returns wrapper class
		public JobCategory this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as JobCategory;
                    
                



















			}
		}


		public void CopyTo(JobCategory[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public JobCategory ItemById(int id)
		{
			return (JobCategory)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(JobCategory);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new JobCategory(this, key, state);
		}




		public void Remove(JobCategory jobCategory)
		{
			if( null == jobCategory )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("jobCategory"));
			
			RemoveObj(jobCategory, new SimpleObjectKey(jobCategory.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(JobCategory jobCategory) 
		{
			AddImpl(jobCategory);
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
