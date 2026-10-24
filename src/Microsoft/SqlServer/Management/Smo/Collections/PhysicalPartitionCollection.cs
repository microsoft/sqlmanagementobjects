// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Collection of PhysicalPartition objects associated with an index or table
    /// </summary>
    public sealed partial class PhysicalPartitionCollection : PartitionNumberedObjectCollectionBase
	{

		internal PhysicalPartitionCollection(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}

        /// <summary>
        /// Returns the parent of the collection
        /// </summary>
		public SqlSmoObject Parent => ParentInstance as SqlSmoObject;

        protected override string UrnSuffix => PhysicalPartition.UrnSuffix;

        public void Add(PhysicalPartition physicalPartition) => InternalStorage.Add(new PartitionNumberedObjectKey((short)physicalPartition.PartitionNumber), physicalPartition);

        public void Remove(PhysicalPartition physicalPartition) => InternalStorage.Remove(new PartitionNumberedObjectKey((short)physicalPartition.PartitionNumber));
        public void Remove(int partitionNumber) => InternalStorage.Remove(new PartitionNumberedObjectKey((short)partitionNumber));

        internal override PhysicalPartition GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new PhysicalPartition(this, key, state);
    }
}
