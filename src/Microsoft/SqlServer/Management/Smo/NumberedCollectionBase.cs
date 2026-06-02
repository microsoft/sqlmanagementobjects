// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class NumberedObjectCollectionBase : SortedListCollectionBase<NumberedStoredProcedure, StoredProcedure>
    {

        internal NumberedObjectCollectionBase(SqlSmoObject parent) : base((StoredProcedure)parent)
        {
        }

        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<NumberedStoredProcedure>(new NumberedObjectComparer());

        public bool Contains(short number) => Contains(new NumberedObjectKey(number));

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var number = short.Parse(urn.GetAttribute(nameof(NumberedStoredProcedure.Number)), SmoApplication.DefaultCulture);

            return new NumberedObjectKey(number);
        }
    }

    internal class NumberedObjectComparer : ObjectComparerBase
    {
        internal NumberedObjectComparer() : base(null)
        {
        }

        public override int Compare(object obj1, object obj2) => ((NumberedObjectKey)obj1).Number - ((NumberedObjectKey)obj2).Number;
    }

    internal class NumberedObjectKey : ObjectKeyBase
    {
        protected short number;


        public NumberedObjectKey(short number) : base()
        {
            this.number = number;
        }

        static NumberedObjectKey()
        {
            _ = fields.Add(nameof(NumberedStoredProcedure.Number));
        }

        internal static readonly StringCollection fields = new StringCollection();

        public short Number
        {
            get { return number; }
            set { number = value; }
        }

        public override string ToString() => string.Format(SmoApplication.DefaultCulture, "{0}",
                                            number);

        public override string UrnFilter => string.Format(SmoApplication.DefaultCulture, "@Number={0}", number);

        public override StringCollection GetFieldNames() => fields;

        public override ObjectKeyBase Clone() => new NumberedObjectKey(Number);

        public override bool IsNull => false;

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new NumberedObjectComparer();

    }

    /// <summary>
    /// Collection of PhysicalPartition objects associated with an index or table
    /// </summary>
    public abstract class PartitionNumberedObjectCollectionBase : SortedListCollectionBase<PhysicalPartition, SqlSmoObject>
    {

        internal PartitionNumberedObjectCollectionBase(SqlSmoObject parent)
            : base(parent)
        {
        }

        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<PhysicalPartition>(new PartitionNumberedObjectComparer());

        /// <summary>
        /// Returns whether the collection contains a partition identified by the given number
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public bool Contains(int number) => Contains(new PartitionNumberedObjectKey(number));


        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            int number = short.Parse(urn.GetAttribute("PartitionNumber"), SmoApplication.DefaultCulture);

            return new PartitionNumberedObjectKey(number);
        }
    }

    internal class PartitionNumberedObjectComparer : ObjectComparerBase
    {
        internal PartitionNumberedObjectComparer()
            : base(null)
        {
        }

        public override int Compare(object obj1, object obj2) => ((PartitionNumberedObjectKey)obj1).Number - ((PartitionNumberedObjectKey)obj2).Number;
    }

    internal class PartitionNumberedObjectKey : ObjectKeyBase
    {
        protected int number;


        public PartitionNumberedObjectKey(int number)
            : base()
        {
            this.number = number;
        }

        static PartitionNumberedObjectKey()
        {
            fields = new StringCollection
            {
                nameof(PhysicalPartition.PartitionNumber)
            };
        }

        internal static readonly StringCollection fields;

        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        public override string ToString() => string.Format(SmoApplication.DefaultCulture, "{0}",
                                            number);

        public override string UrnFilter => string.Format(SmoApplication.DefaultCulture, "@PartitionNumber={0}", number);

        public override StringCollection GetFieldNames() => fields;

        public override ObjectKeyBase Clone() => new PartitionNumberedObjectKey(Number);

        public override bool IsNull => false;

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new PartitionNumberedObjectComparer();

    }
}
