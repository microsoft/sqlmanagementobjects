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
    public sealed  class AuditCollection : SimpleObjectCollectionBase
	{


















		internal AuditCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public Audit this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Audit;
			}
		}


		// returns wrapper class
		public Audit this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as Audit;
                    
                



















			}
		}


		public void CopyTo(Audit[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Audit ItemById(int id)
		{
			return (Audit)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(Audit);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Audit(this, key, state);
		}



















		public void Add(Audit audit) 
		{
			AddImpl(audit);
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
