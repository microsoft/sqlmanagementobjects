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
    public   class TriggerCollection : SimpleObjectCollectionBase
	{


















		internal TriggerCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		
		public Trigger this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Trigger;
			}
		}


		// returns wrapper class
		public Trigger this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as Trigger;
                    
                



















			}
		}


		public void CopyTo(Trigger[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Trigger ItemById(int id)
		{
			return (Trigger)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(Trigger);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Trigger(this, key, state);
		}




		public void Remove(Trigger trigger)
		{
			if( null == trigger )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("trigger"));
			
			RemoveObj(trigger, new SimpleObjectKey(trigger.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(Trigger trigger) 
		{
			AddImpl(trigger);
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
