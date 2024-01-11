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
    public sealed  class ExternalLanguageFileCollection : SimpleObjectCollectionBase
	{


















		internal ExternalLanguageFileCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public ExternalLanguage Parent
		{
			get
			{
				return this.ParentInstance as ExternalLanguage;
			}
		}

		
		public ExternalLanguageFile this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ExternalLanguageFile;
			}
		}


		// returns wrapper class
		public ExternalLanguageFile this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ExternalLanguageFile;
                    
                



















			}
		}


		public void CopyTo(ExternalLanguageFile[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ExternalLanguageFile ItemById(int id)
		{
			return (ExternalLanguageFile)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ExternalLanguageFile);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ExternalLanguageFile(this, key, state);
		}




		public void Remove(ExternalLanguageFile externalLanguageFile)
		{
			if( null == externalLanguageFile )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("externalLanguageFile"));
			
			RemoveObj(externalLanguageFile, new SimpleObjectKey(externalLanguageFile.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(ExternalLanguageFile externalLanguageFile) 
		{
			AddImpl(externalLanguageFile);
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
