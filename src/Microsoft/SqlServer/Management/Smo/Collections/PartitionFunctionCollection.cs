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
    public sealed  class PartitionFunctionCollection : SimpleObjectCollectionBase
	{


















		internal PartitionFunctionCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}



		public Database Parent
		{
			get
			{
				return this.ParentInstance as Database;
			}
		}

		
		public PartitionFunction this[Int32 index]
		{
			get
			{ 
				return GetObjectByIndex(index) as PartitionFunction;
			}
		}


		// returns wrapper class
		public PartitionFunction this[string name]
		{
			get
			{
                





    
	    			    return  GetObjectByName(name) as PartitionFunction;
                    
                



















			}
		}


		public void CopyTo(PartitionFunction[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}
		

		public PartitionFunction ItemById(int id)
		{
			return (PartitionFunction)GetItemById(id);
		}


































		protected override Type GetCollectionElementType()
		{
			return typeof(PartitionFunction);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new PartitionFunction(this, key, state);
		}



















		public void Add(PartitionFunction partitionFunction) 
		{
			AddImpl(partitionFunction);
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
