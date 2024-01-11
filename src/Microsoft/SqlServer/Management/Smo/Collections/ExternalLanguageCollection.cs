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
    public sealed  class ExternalLanguageCollection : SimpleObjectCollectionBase
	{


















		internal ExternalLanguageCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}

		
		public ExternalLanguage this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ExternalLanguage;
			}
		}


		// returns wrapper class
		public ExternalLanguage this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ExternalLanguage;
                    
                



















			}
		}


		public void CopyTo(ExternalLanguage[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ExternalLanguage ItemById(int id)
		{
			return (ExternalLanguage)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ExternalLanguage);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ExternalLanguage(this, key, state);
		}




		public void Remove(ExternalLanguage externalLanguage)
		{
			if( null == externalLanguage )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("externalLanguage"));
			
			RemoveObj(externalLanguage, new SimpleObjectKey(externalLanguage.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(ExternalLanguage externalLanguage) 
		{
			AddImpl(externalLanguage);
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
