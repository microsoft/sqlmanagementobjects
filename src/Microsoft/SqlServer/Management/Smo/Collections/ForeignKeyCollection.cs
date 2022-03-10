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
    public sealed  class ForeignKeyCollection : SimpleObjectCollectionBase
	{


















		internal ForeignKeyCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Table Parent
		{
			get
			{
				return this.ParentInstance as Table;
			}
		}

		
		public ForeignKey this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ForeignKey;
			}
		}


		// returns wrapper class
		public ForeignKey this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ForeignKey;
                    
                



















			}
		}


		public void CopyTo(ForeignKey[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ForeignKey ItemById(int id)
		{
			return (ForeignKey)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ForeignKey);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ForeignKey(this, key, state);
		}




		public void Remove(ForeignKey foreignKey)
		{
			if( null == foreignKey )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("foreignKey"));
			
			RemoveObj(foreignKey, new SimpleObjectKey(foreignKey.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(ForeignKey foreignKey) 
		{
			AddImpl(foreignKey);
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
