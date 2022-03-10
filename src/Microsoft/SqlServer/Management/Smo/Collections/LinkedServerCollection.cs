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
    public sealed  class LinkedServerCollection : SimpleObjectCollectionBase
	{


















		internal LinkedServerCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Server Parent
		{
			get
			{
				return this.ParentInstance as Server;
			}
		}

		
		public LinkedServer this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as LinkedServer;
			}
		}


		// returns wrapper class
		public LinkedServer this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as LinkedServer;
                    
                



















			}
		}


		public void CopyTo(LinkedServer[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public LinkedServer ItemById(int id)
		{
			return (LinkedServer)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(LinkedServer);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new LinkedServer(this, key, state);
		}



















		public void Add(LinkedServer linkedServer) 
		{
			AddImpl(linkedServer);
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
