// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;
using System.Collections.Specialized;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Agent
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class AlertCollection : SimpleObjectCollectionBase
	{


















		internal AlertCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public JobServer Parent
		{
			get
			{
				return this.ParentInstance as JobServer;
			}
		}

		
		public Alert this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as Alert;
			}
		}


		// returns wrapper class
		public Alert this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as Alert;
                    
                



















			}
		}


		public void CopyTo(Alert[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public Alert ItemById(int id)
		{
			return (Alert)GetItemById(id);
		}










		public StringCollection Script()
		{
			return this.Script(new ScriptingOptions());
		}

		public StringCollection Script(ScriptingOptions scriptingOptions)
		{
			if( this.Count <= 0 )
			{
				return new StringCollection();
			}

			SqlSmoObject [] scriptList = new SqlSmoObject[this.Count];
			int i = 0;
			foreach(SqlSmoObject o in this)
			{
				scriptList[i++] = o;
			}
			Scripter scr = new Scripter(scriptList[0].GetServerObject());
			scr.Options = scriptingOptions;
			return scr.Script(scriptList);
		}


		protected override Type GetCollectionElementType()
		{
			return typeof(Alert);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new Alert(this, key, state);
		}



















		public void Add(Alert alert) 
		{
			AddImpl(alert);
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
