// Copyright (c) Microsoft.
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
    public sealed  class AlertCategoryCollection : SimpleObjectCollectionBase
	{


















		internal AlertCategoryCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public AlertCategory this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as AlertCategory;
			}
		}


		// returns wrapper class
		public AlertCategory this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as AlertCategory;
                    
                



















			}
		}


		public void CopyTo(AlertCategory[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public AlertCategory ItemById(int id)
		{
			return (AlertCategory)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(AlertCategory);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new AlertCategory(this, key, state);
		}




		public void Remove(AlertCategory alertCategory)
		{
			if( null == alertCategory )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("alertCategory"));
			
			RemoveObj(alertCategory, new SimpleObjectKey(alertCategory.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(AlertCategory alertCategory) 
		{
			AddImpl(alertCategory);
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
