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
    public sealed  class LinkedServerLoginCollection : SimpleObjectCollectionBase
	{


















		internal LinkedServerLoginCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public LinkedServer Parent
		{
			get
			{
				return this.ParentInstance as LinkedServer;
			}
		}

		
		public LinkedServerLogin this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as LinkedServerLogin;
			}
		}


		// returns wrapper class
		public LinkedServerLogin this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as LinkedServerLogin;
                    
                



















			}
		}


		public void CopyTo(LinkedServerLogin[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		







































		protected override Type GetCollectionElementType()
		{
			return typeof(LinkedServerLogin);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new LinkedServerLogin(this, key, state);
		}



















		public void Add(LinkedServerLogin linkedServerLogin) 
		{
			AddImpl(linkedServerLogin);
		}


		internal SqlSmoObject GetObjectByName(string name)
		{
			return GetObjectByKey(new SimpleObjectKey(name));
		}


		internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
		{ 
			string name = urn.GetAttribute("Name");

			if( null == name)



				throw new SmoException(ExceptionTemplates.PropertyMustBeSpecifiedInUrn("Name", urn.Type));
            return new SimpleObjectKey(name);        
        }


















	}
}
