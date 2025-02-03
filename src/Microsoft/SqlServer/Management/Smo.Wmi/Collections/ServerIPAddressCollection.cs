
 ///////////////////////////////////////////////////////////////
 //
 // WARNING : DO NOT ATTEMPT TO MODIFY THIS FILE MANUALLY
 // 
 // This class is autogenerated from WmiCollectionTemplate.cs
 //
 ///////////////////////////////////////////////////////////////

using System;
using System.Collections;





















	



namespace Microsoft.SqlServer.Management.Smo.Wmi
{

	///<summary>
	/// Strongly typed list of MAPPED_TYPE objects
	/// Has strongly typed support for all of the methods of the sorted list class
	///</summary>
	public sealed class ServerIPAddressCollection : WmiCollectionBase, ICollection 
	{

		internal ServerIPAddressCollection(WmiSmoObject parentInstance)  : base(parentInstance)
		{
		}










		
		// checks if the collection contains the specified key
		// if the key is not found, we try to look for the specified object on the server
		public bool Contains(string key) 
		{ 
			if(innerColl.ContainsKey(key))
			{
				return true;
			}
			else
			{
				if(!initialized )
				{
					// try to get the child object, if any
					if(null!=InitializeChildObject(key))
						return true;
				}
			}

			// in all other cases we don't have this object
			return false;
		}

		
		public ServerIPAddress this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index);
			}
		}

		// returns wrapped object
		internal ServerIPAddress GetObjectByIndex(Int32 index)
		{
			if(!initialized)
				InitializeChildCollection();
			
			return innerColl.GetByIndex(index) as ServerIPAddress;
		}
		
		// returns wrapper class
		public ServerIPAddress this[string name]
		{
			get
			{
				return  GetObjectByName(name);
			}
		}

		// returns wrapped object
		internal ServerIPAddress GetObjectByName(string name)
		{
			object instanceObject = innerColl[name];
			
			if( null==instanceObject && !initialized  )
			{
				instanceObject = InitializeChildObject(name);
			}
			
			return instanceObject as ServerIPAddress;
		}












	
		// this function tries to instantiate a missing object
		// and if it exists we add it to the innerColl
		internal object InitializeChildObject(string name)
		{
			// we create a new object
			ServerIPAddress childobj = new ServerIPAddress(this, name);
			
			if(childobj.Initialize())
			{
				// update object's state and add it to the collection
				this.Add(childobj);
				return childobj;
			}
			else
			{
				return null;
			}
		}

		//Initializes the child collection, keeping all the old objects
		// TODO: Make sure we add thread safety stuff, at least here 
		// since we are changing the underlying collection
		internal void InitializeChildCollection()
		{
			// keep the old collection, because we'll append all the objects to the new one
			SortedList oldColl = innerColl;
			innerColl = new SortedList();

			// populate the new collection, calling into parent's function
			ParentInstance.EnumChildren(typeof(ServerIPAddress).ToString(), this);

			// now merge the old collection into the new one
			foreach(DictionaryEntry de in oldColl)
			{
				ServerIPAddress objMAPPED_TYPE = de.Value as ServerIPAddress;
				
				// this asssignment has the effect of adding new members to the collection
				// and of replacing the existing ones with the old values
				innerColl[objMAPPED_TYPE.Name] = objMAPPED_TYPE;
			}

			// update the state flag
			this.initialized = true;
		}

		public Int32 Count 
		{ 
			get 
			{
				if(!initialized )
					InitializeChildCollection();
				return innerColl.Count;
			} 
		}

		public void CopyTo(ServerIPAddress[] array, Int32 index)
		{
			((ICollection)this).CopyTo(array, index);
		}

		void ICollection.CopyTo(Array array, Int32 index)
		{
			if(!initialized)
				InitializeChildCollection();

			int idx = index;
			foreach(DictionaryEntry de in innerColl)
			{
				array.SetValue( (ServerIPAddress)de.Value, idx++);
			}
		}

		public IEnumerator  GetEnumerator() 
		{
			if(!initialized)
				InitializeChildCollection();
			return new ServerIPAddressCollectionEnumerator(this);
		}


		// nested enumerator class
		// we need that to override the behaviour of SortedList
		// that exposes an IDictionaryEnumerator interface
		internal sealed class ServerIPAddressCollectionEnumerator : IEnumerator 
		{
			internal IDictionaryEnumerator baseEnumerator;
			
			internal ServerIPAddressCollectionEnumerator(WmiCollectionBase col) 
			{
				this.baseEnumerator = col.innerColl.GetEnumerator();
			}

			object IEnumerator.Current 
			{ 
				get 
				{	
					return baseEnumerator.Value as ServerIPAddress;
				} 
			}
			
			public ServerIPAddress Current 
			{ 
				get 
				{	
					return baseEnumerator.Value as ServerIPAddress;
				} 
			}
			
			public bool MoveNext() 
			{
				return baseEnumerator.MoveNext();
			}
			
			public void Reset() 
			{
				baseEnumerator.Reset();
			}

		}

	}
}

