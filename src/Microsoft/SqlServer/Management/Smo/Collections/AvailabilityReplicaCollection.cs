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
    public sealed  class AvailabilityReplicaCollection : SimpleObjectCollectionBase
	{

		//has custom string comparer
		StringComparer m_comparer;

		//must initialize in constructor
		internal AvailabilityReplicaCollection(SqlSmoObject parentInstance, StringComparer comparer)  : base(parentInstance)
		{
			m_comparer = comparer;
		}

		override internal StringComparer StringComparer
		{
			get 
			{
				return m_comparer;
			}
		}







		public AvailabilityGroup Parent
		{
			get
			{
				return this.ParentInstance as AvailabilityGroup;
			}
		}

		
		public AvailabilityReplica this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as AvailabilityReplica;
			}
		}


		// returns wrapper class
		public AvailabilityReplica this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as AvailabilityReplica;
                    
                



















			}
		}


		public void CopyTo(AvailabilityReplica[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public AvailabilityReplica ItemById(int id)
		{
			return (AvailabilityReplica)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(AvailabilityReplica);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new AvailabilityReplica(this, key, state);
		}




		public void Remove(AvailabilityReplica availabilityReplica)
		{
			if( null == availabilityReplica )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("availabilityReplica"));
			
			RemoveObj(availabilityReplica, new SimpleObjectKey(availabilityReplica.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(AvailabilityReplica availabilityReplica) 
		{
			AddImpl(availabilityReplica);
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
