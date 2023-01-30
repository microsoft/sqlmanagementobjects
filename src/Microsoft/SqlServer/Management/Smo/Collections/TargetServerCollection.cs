// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Agent
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class TargetServerCollection : SimpleObjectCollectionBase
	{


















		internal TargetServerCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public TargetServer this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as TargetServer;
			}
		}


		// returns wrapper class
		public TargetServer this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as TargetServer;
                    
                



















			}
		}


		public void CopyTo(TargetServer[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public TargetServer ItemById(int id)
		{
			return (TargetServer)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(TargetServer);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new TargetServer(this, key, state);
		}



















		public void Add(TargetServer server) 
		{
			AddImpl(server);
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
