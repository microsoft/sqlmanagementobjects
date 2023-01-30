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
    public sealed  class AvailabilityGroupListenerCollection : SimpleObjectCollectionBase
	{


















		internal AvailabilityGroupListenerCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public AvailabilityGroup Parent
		{
			get
			{
				return this.ParentInstance as AvailabilityGroup;
			}
		}

		
		public AvailabilityGroupListener this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as AvailabilityGroupListener;
			}
		}


		// returns wrapper class
		public AvailabilityGroupListener this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as AvailabilityGroupListener;
                    
                



















			}
		}


		public void CopyTo(AvailabilityGroupListener[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public AvailabilityGroupListener ItemById(int id)
		{
			return (AvailabilityGroupListener)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(AvailabilityGroupListener);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new AvailabilityGroupListener(this, key, state);
		}



















		public void Add(AvailabilityGroupListener AvailabilityGroupListener) 
		{
			AddImpl(AvailabilityGroupListener);
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
