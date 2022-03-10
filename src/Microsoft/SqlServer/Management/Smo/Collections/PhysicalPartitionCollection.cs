// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587

























namespace Microsoft.SqlServer.Management.Smo
{

    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed partial class PhysicalPartitionCollection : 

        PartitionNumberedObjectCollectionBase



	{

		internal PhysicalPartitionCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}


		public SqlSmoObject Parent
		{
			get
			{
				return this.ParentInstance as SqlSmoObject;
			}
		}

		

        public void Add(PhysicalPartition physicalPartition)
        {
            InternalStorage.Add(new PartitionNumberedObjectKey((short)physicalPartition.PartitionNumber),physicalPartition);
        }

        public void Remove(PhysicalPartition physicalPartition)
        {
            InternalStorage.Remove(new PartitionNumberedObjectKey((short)physicalPartition.PartitionNumber));
        }
        public void Remove(Int32 partitionNumber)
        {
            InternalStorage.Remove(new PartitionNumberedObjectKey((short)partitionNumber));
        }



       

		public PhysicalPartition this[Int32 index]
		{
			get
			{ 
			    return GetObjectByIndex(index) as PhysicalPartition;
			}
		}

		protected override Type GetCollectionElementType()
		{
			return typeof(PhysicalPartition);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new PhysicalPartition(this, key, state);
		}













	}
}
