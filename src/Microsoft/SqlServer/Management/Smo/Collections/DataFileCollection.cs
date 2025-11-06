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
    public sealed  class DataFileCollection : SimpleObjectCollectionBase
	{


















		internal DataFileCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public FileGroup Parent
		{
			get
			{
				return this.ParentInstance as FileGroup;
			}
		}

		
		public DataFile this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as DataFile;
			}
		}


		// returns wrapper class
		public DataFile this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as DataFile;
                    
                



















			}
		}


		public void CopyTo(DataFile[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public DataFile ItemById(int id)
		{
			return (DataFile)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(DataFile);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new DataFile(this, key, state);
		}




		public void Remove(DataFile dataFile)
		{
			if( null == dataFile )
				throw new FailedOperationException(ExceptionTemplates.RemoveCollection, this, new ArgumentNullException("dataFile"));
			
			RemoveObj(dataFile, new SimpleObjectKey(dataFile.Name));
		}

		public void Remove(string name)
		{
			this.Remove(new SimpleObjectKey(name));
		}



		public void Add(DataFile dataFile) 
		{
			AddImpl(dataFile);
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
