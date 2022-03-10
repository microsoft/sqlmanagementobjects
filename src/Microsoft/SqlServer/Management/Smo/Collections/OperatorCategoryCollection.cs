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
    public sealed  class OperatorCategoryCollection : SimpleObjectCollectionBase
	{


















		internal OperatorCategoryCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public OperatorCategory this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as OperatorCategory;
			}
		}


		// returns wrapper class
		public OperatorCategory this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as OperatorCategory;
                    
                



















			}
		}


		public void CopyTo(OperatorCategory[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public OperatorCategory ItemById(int id)
		{
			return (OperatorCategory)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(OperatorCategory);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new OperatorCategory(this, key, state);
		}




		public void Remove(OperatorCategory operatorCategory)
		{
			if( null == operatorCategory )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("operatorCategory"));
			
			RemoveObj(operatorCategory, new SimpleObjectKey(operatorCategory.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(OperatorCategory operatorCategory) 
		{
			AddImpl(operatorCategory);
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
