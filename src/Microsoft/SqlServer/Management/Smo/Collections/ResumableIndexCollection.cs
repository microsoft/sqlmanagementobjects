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
    public sealed  class ResumableIndexCollection : SimpleObjectCollectionBase
	{


















		internal ResumableIndexCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		
		public ResumableIndex this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as ResumableIndex;
			}
		}


		// returns wrapper class
		public ResumableIndex this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as ResumableIndex;
                    
                



















			}
		}


		public void CopyTo(ResumableIndex[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public ResumableIndex ItemById(int id)
		{
			return (ResumableIndex)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(ResumableIndex);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new ResumableIndex(this, key, state);
		}



















		public void Add(ResumableIndex resumableIndex) 
		{
			AddImpl(resumableIndex);
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
