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
    public sealed  class AvailabilityDatabaseCollection : SimpleObjectCollectionBase
	{


















		internal AvailabilityDatabaseCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public AvailabilityGroup Parent
		{
			get
			{
				return this.ParentInstance as AvailabilityGroup;
			}
		}

		
		public AvailabilityDatabase this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as AvailabilityDatabase;
			}
		}


		// returns wrapper class
		public AvailabilityDatabase this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as AvailabilityDatabase;
                    
                



















			}
		}


		public void CopyTo(AvailabilityDatabase[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public AvailabilityDatabase ItemById(int id)
		{
			return (AvailabilityDatabase)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(AvailabilityDatabase);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new AvailabilityDatabase(this, key, state);
		}




		public void Remove(AvailabilityDatabase AvailabilityDatabase)
		{
			if( null == AvailabilityDatabase )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("AvailabilityDatabase"));
			
			RemoveObj(AvailabilityDatabase, new SimpleObjectKey(AvailabilityDatabase.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(AvailabilityDatabase AvailabilityDatabase) 
		{
			AddImpl(AvailabilityDatabase);
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
