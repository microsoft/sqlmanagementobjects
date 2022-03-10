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
    public sealed  class StatisticCollection : SimpleObjectCollectionBase
	{


















		internal StatisticCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		
		public Statistic this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Statistic;
			}
		}


		// returns wrapper class
		public Statistic this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as Statistic;
                    
                



















			}
		}


		public void CopyTo(Statistic[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Statistic ItemById(int id)
		{
			return (Statistic)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(Statistic);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Statistic(this, key, state);
		}




		public void Remove(Statistic statistic)
		{
			if( null == statistic )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("statistic"));
			
			RemoveObj(statistic, new SimpleObjectKey(statistic.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(Statistic statistic) 
		{
			AddImpl(statistic);
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
