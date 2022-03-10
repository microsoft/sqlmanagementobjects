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
    public sealed  class DatabaseAuditSpecificationCollection : SimpleObjectCollectionBase
	{


















		internal DatabaseAuditSpecificationCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}

		
		public DatabaseAuditSpecification this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as DatabaseAuditSpecification;
			}
		}


		// returns wrapper class
		public DatabaseAuditSpecification this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as DatabaseAuditSpecification;
                    
                



















			}
		}


		public void CopyTo(DatabaseAuditSpecification[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public DatabaseAuditSpecification ItemById(int id)
		{
			return (DatabaseAuditSpecification)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(DatabaseAuditSpecification);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new DatabaseAuditSpecification(this, key, state);
		}



















		public void Add(DatabaseAuditSpecification databaseAuditSpecification) 
		{
			AddImpl(databaseAuditSpecification);
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
