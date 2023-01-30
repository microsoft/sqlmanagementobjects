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
    public sealed  class TargetServerGroupCollection : SimpleObjectCollectionBase
	{


















		internal TargetServerGroupCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public TargetServerGroup this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as TargetServerGroup;
			}
		}


		// returns wrapper class
		public TargetServerGroup this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as TargetServerGroup;
                    
                



















			}
		}


		public void CopyTo(TargetServerGroup[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public TargetServerGroup ItemById(int id)
		{
			return (TargetServerGroup)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(TargetServerGroup);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new TargetServerGroup(this, key, state);
		}



















		public void Add(TargetServerGroup targetServerGroup) 
		{
			AddImpl(targetServerGroup);
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
