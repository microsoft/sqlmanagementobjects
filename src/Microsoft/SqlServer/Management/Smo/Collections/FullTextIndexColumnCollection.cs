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
    public sealed  class FullTextIndexColumnCollection : SimpleObjectCollectionBase
	{


















		internal FullTextIndexColumnCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public FullTextIndex Parent
		{
			get
			{
				return this.ParentInstance as FullTextIndex;
			}
		}

		
		public FullTextIndexColumn this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as FullTextIndexColumn;
			}
		}


		// returns wrapper class
		public FullTextIndexColumn this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as FullTextIndexColumn;
                    
                



















			}
		}


		public void CopyTo(FullTextIndexColumn[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		







































		protected override Type GetCollectionElementType()
		{
			return typeof(FullTextIndexColumn);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new FullTextIndexColumn(this, key, state);
		}




		public void Remove(FullTextIndexColumn fullTextIndexColumn)
		{
			if( null == fullTextIndexColumn )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("fullTextIndexColumn"));
			
			RemoveObj(fullTextIndexColumn, new SimpleObjectKey(fullTextIndexColumn.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(FullTextIndexColumn fullTextIndexColumn) 
		{
			AddImpl(fullTextIndexColumn);
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
