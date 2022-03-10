// Copyright (c) Microsoft.
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
    public sealed  class AvailabilityGroupCollection : SimpleObjectCollectionBase
	{


















		internal AvailabilityGroupCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public AvailabilityGroup this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as AvailabilityGroup;
			}
		}


		// returns wrapper class
		public AvailabilityGroup this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as AvailabilityGroup;
                    
                



















			}
		}


		public void CopyTo(AvailabilityGroup[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public AvailabilityGroup ItemById(int id)
		{
			return (AvailabilityGroup)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(AvailabilityGroup);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new AvailabilityGroup(this, key, state);
		}




		public void Remove(AvailabilityGroup availabilityGroup)
		{
			if( null == availabilityGroup )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("availabilityGroup"));
			
			RemoveObj(availabilityGroup, new SimpleObjectKey(availabilityGroup.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(AvailabilityGroup availabilityGroup) 
		{
			AddImpl(availabilityGroup);
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
