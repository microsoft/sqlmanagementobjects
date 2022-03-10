// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587



using Microsoft.SqlServer.Management.Sdk.Sfc;



































namespace Microsoft.SqlServer.Management.Smo.Mail
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class ConfigurationValueCollection : SimpleObjectCollectionBase
	{


















		internal ConfigurationValueCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlMail Parent
		{
			get
			{
				return this.ParentInstance as SqlMail;
			}
		}

		
		public ConfigurationValue this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ConfigurationValue;
			}
		}


		// returns wrapper class
		public ConfigurationValue this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ConfigurationValue;
                    
                



















			}
		}


		public void CopyTo(ConfigurationValue[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		







































		protected override Type GetCollectionElementType()
		{
			return typeof(ConfigurationValue);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ConfigurationValue(this, key, state);
		}



















		public void Add(ConfigurationValue configurationValue) 
		{
			AddImpl(configurationValue);
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
