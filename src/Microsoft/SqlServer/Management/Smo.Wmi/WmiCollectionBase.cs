using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SqlServer.Management.Smo.Wmi
{
	/// <summary>
	/// Base collection for children of ManagedComputer
	/// </summary>
	public abstract class WmiCollectionBase 
	{
		// the object that holds this collection
		private WmiSmoObject parentInstance;

		internal WmiCollectionBase(WmiSmoObject parentInstance)
		{
			this.parentInstance = parentInstance;
			initialized = false;
			// all WMI object collections are need case insensitive comparison
			innerColl = new SortedList(new CaseInsensitiveComparer(SmoApplication.DefaultCulture));
 		}

        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        internal protected SortedList innerColl;
		
		public bool IsSynchronized
		{
			get
			{
				return innerColl.IsSynchronized;
			}
				
		}

		public object SyncRoot
		{
			get
			{
				return innerColl.SyncRoot;
			}
		}
		
		// we have this so that contained objects can ask for their parent object
		internal WmiSmoObject ParentInstance
		{
			get
			{
				return parentInstance;
			}
		}

		// flag is true if the innerColl is initialized from the enumerator
        [SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        internal protected bool initialized;

		protected virtual void Add(WmiSmoObject wmiObj) 
		{
			innerColl.Add(wmiObj.Name, wmiObj);
		}
		
		internal void AddInternal(WmiSmoObject wmiObj)
		{
			Add(wmiObj);
		}

		protected virtual void Remove(string objname)
		{
			innerColl.Remove(objname);
		}

		internal void RemoveInternal(string key)
		{
			Remove(key);
		}
	}
}


