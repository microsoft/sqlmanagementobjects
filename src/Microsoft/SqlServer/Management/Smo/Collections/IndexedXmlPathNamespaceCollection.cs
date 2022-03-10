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
    public sealed  class IndexedXmlPathNamespaceCollection : SimpleObjectCollectionBase
	{


















		internal IndexedXmlPathNamespaceCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Index Parent
		{
			get
			{
				return this.ParentInstance as Index;
			}
		}

		
		public IndexedXmlPathNamespace this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as IndexedXmlPathNamespace;
			}
		}


		// returns wrapper class
		public IndexedXmlPathNamespace this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as IndexedXmlPathNamespace;
                    
                



















			}
		}


		public void CopyTo(IndexedXmlPathNamespace[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		







































		protected override Type GetCollectionElementType()
		{
			return typeof(IndexedXmlPathNamespace);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new IndexedXmlPathNamespace(this, key, state);
		}




		public void Remove(IndexedXmlPathNamespace indexedXmlPathNamespace)
		{
			if( null == indexedXmlPathNamespace )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("indexedXmlPathNamespace"));
			
			RemoveObj(indexedXmlPathNamespace, new SimpleObjectKey(indexedXmlPathNamespace.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(IndexedXmlPathNamespace indexedXmlPathNamespace) 
		{
			AddImpl(indexedXmlPathNamespace);
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
