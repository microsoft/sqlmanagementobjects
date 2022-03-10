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
    public sealed  class FileGroupCollection : SimpleObjectCollectionBase
	{


















		internal FileGroupCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}

		
		public FileGroup this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as FileGroup;
			}
		}


		// returns wrapper class
		public FileGroup this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as FileGroup;
                    
                



















			}
		}


		public void CopyTo(FileGroup[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public FileGroup ItemById(int id)
		{
			return (FileGroup)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(FileGroup);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new FileGroup(this, key, state);
		}




		public void Remove(FileGroup fileGroup)
		{
			if( null == fileGroup )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("fileGroup"));
			
			RemoveObj(fileGroup, new SimpleObjectKey(fileGroup.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(FileGroup fileGroup) 
		{
			AddImpl(fileGroup);
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
