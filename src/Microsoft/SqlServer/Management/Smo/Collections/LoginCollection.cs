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
    public sealed  class LoginCollection : SimpleObjectCollectionBase
	{


















		internal LoginCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public Login this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Login;
			}
		}


		// returns wrapper class
		public Login this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as Login;
                    
                



















			}
		}


		public void CopyTo(Login[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Login ItemById(int id)
		{
			return (Login)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(Login);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Login(this, key, state);
		}



















		public void Add(Login login) 
		{
			AddImpl(login);
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
