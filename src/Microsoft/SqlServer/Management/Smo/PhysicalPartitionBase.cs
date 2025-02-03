// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Instance class encapsulating : Server[@Name='']/Database/PartitionFunction
    /// </summary>
    public partial class PhysicalPartition : SqlSmoObject
    {

        internal PhysicalPartition(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
           
        }
       
        private void Init()
        {
            DataCompression = DataCompressionType.None;
            PartitionNumber = 1;
            RightBoundaryValue = null;
            FileGroupName = string.Empty;
            RangeType = RangeType.None;
        }

        /// <summary>
        /// PhysicalPartition constructor which take initialization parameter, parent reference, 
        /// partition number, and compression type.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="partitionNumber"></param>
        /// <param name="dataCompressionType"></param>
        public PhysicalPartition(SqlSmoObject parent, Int32 partitionNumber, DataCompressionType dataCompressionType)
        {
            SetParentImpl(parent);
            Init();
            PartitionNumber = partitionNumber;
            if (this.ServerVersion.Major >= 10)
            {
                DataCompression = dataCompressionType;
            }
            this.key = new PartitionNumberedObjectKey((short)PartitionNumber);
        }

        /// <summary>
        /// PhysicalPartition constructor which take initialization parameter, parent reference, 
        /// partition number, and xml compression type.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="partitionNumber"></param>
        /// <param name="dataCompressionType"></param>
        /// <param name="xmlCompressionType"></param>
        public PhysicalPartition(SqlSmoObject parent, Int32 partitionNumber, DataCompressionType dataCompressionType, XmlCompressionType xmlCompressionType)
        {
            SetParentImpl(parent);
            Init();
            PartitionNumber = partitionNumber;
            if (this.ServerVersion.Major >= 10)
            {
                DataCompression = dataCompressionType;
            }

            if (this.IsSupportedProperty(nameof(XmlCompression)))
            {
                XmlCompression = xmlCompressionType;
            }
            this.key = new PartitionNumberedObjectKey((short)PartitionNumber);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public PhysicalPartition()
        {
            Init();
            this.key = new PartitionNumberedObjectKey((short)PartitionNumber);
        }

        /// <summary>
        /// Constructor for PhysicalPartition class which take parent and partitionNumber
        /// as initialization variable
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="partitionNumber"></param>
        public PhysicalPartition(SqlSmoObject parent, Int32 partitionNumber)
        {
            SetParentImpl(parent);
            Init();   
            PartitionNumber = partitionNumber;
            this.key = new PartitionNumberedObjectKey((short)PartitionNumber);
        }

        internal PhysicalPartition(PhysicalPartition physicalPartition)
        {
            SetParentImpl(physicalPartition.Parent);
            this.DataCompression = physicalPartition.DataCompression;

            if (this.IsSupportedProperty(nameof(XmlCompression)))
            {
                this.XmlCompression = physicalPartition.XmlCompression;
            }
            this.PartitionNumber = physicalPartition.PartitionNumber;
            this.RightBoundaryValue = physicalPartition.RightBoundaryValue;
            this.FileGroupName = physicalPartition.FileGroupName;
            this.RangeType = physicalPartition.RangeType;
        }

        internal bool Compare(PhysicalPartition physicalPartition)
        {
            if (this.DataCompression != physicalPartition.DataCompression)
            {
                return false;
            }

            if ((this.IsSupportedProperty(nameof(XmlCompression)))
                && this.XmlCompression != physicalPartition.XmlCompression)
            {
                return false;
            }

            if (this.PartitionNumber != physicalPartition.PartitionNumber)
            {
                return false;
            }

            if (this.RangeType != physicalPartition.RangeType)
            {
                return false;
            }

            return (0 == string.Compare(this.FileGroupName, physicalPartition.FileGroupName, StringComparison.Ordinal));
        }

        /// <summary>
        /// Method to fetch and set the Property RightBoundaryValue value from database 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public System.Object RightBoundaryValue
        {
            get
            {
                    return this.Properties.GetValueWithNullReplacement("RightBoundaryValue", false, false);
            }

            set
            {
                //allows null to be set
                this.Properties.SetValueWithConsistencyCheck("RightBoundaryValue", value, true);
            }
        }

        /// <summary>
        /// Method to fetch and set the Property RangeType value from database 
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone | SfcPropertyFlags.SqlAzureDatabase)]
        public Microsoft.SqlServer.Management.Smo.RangeType RangeType
        {
            get
            {
                //RangeType may not exist on server if associated table/index is not partitioned
                try
                {
                    //casting required as RangeType field on server side is short
                    return (Microsoft.SqlServer.Management.Smo.RangeType)this.Properties.GetValueWithNullReplacement("RangeType");
                }
                catch (PropertyCannotBeRetrievedException)
                {
                    //if it get failed that means associated table or index is not partitioned
                    //so, let return none, which means no boundary
                    return RangeType.None;
                }
            }
            set
            {
                Properties.SetValueWithConsistencyCheck("RangeType", value);
            }
        }

        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Microsoft.SqlServer.Management.Smo.DataCompressionType DataCompression
        {
            get
            {
                this.ThrowIfNotSupported(typeof(PhysicalPartition));
                //Even if somebody checking the data compression state for the partition on
                //yukon server then returing compression state none is OK
                if (this.ServerVersion.Major < 10)
                {
                    return DataCompressionType.None;
                }
                return (Microsoft.SqlServer.Management.Smo.DataCompressionType)this.Properties.GetValueWithNullReplacement("DataCompression");
            }
            set
            {
                ThrowIfBelowVersion100();
				
                if (value == DataCompressionType.ColumnStore || value == DataCompressionType.ColumnStoreArchive)
                {
                    ThrowIfBelowVersion120();
                }
                Properties.SetValueWithConsistencyCheck("DataCompression", value);
            }
        }

        public static string UrnSuffix
        {
            get
            {
                return "PhysicalPartition";
            }
        }

        internal bool IsDirty(string property)
        {
            return this.Properties.IsDirty(this.Properties.LookupID(property, PropertyAccessPurpose.Read));
        }
        
    }
}
