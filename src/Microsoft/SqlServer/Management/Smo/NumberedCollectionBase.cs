// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// base class for all generic collections
    /// </summary>
    public abstract class NumberedObjectCollectionBase : SortedListCollectionBase
    {

        internal NumberedObjectCollectionBase(SqlSmoObject parent) : base(parent)
        {
        }

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new NumberedObjectComparer());
        }
        
        public bool Contains(short number) 
        {
            return this.Contains(new NumberedObjectKey(number));
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            short number = short.Parse(urn.GetAttribute("Number"), SmoApplication.DefaultCulture);

            return new NumberedObjectKey(number);
        }
    }

    internal class NumberedObjectComparer : ObjectComparerBase
    {
        internal NumberedObjectComparer() : base(null)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            return ((NumberedObjectKey)obj1).Number - ((NumberedObjectKey)obj2).Number;
        }
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
            fields.Add("Number");
        }

        internal static readonly StringCollection fields = new StringCollection();

        public short Number
        {
            get { return number; }
            set { number = value; }
        }

        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}", 
                                            number);
        }

        public override string UrnFilter
        {
            get { return string.Format(SmoApplication.DefaultCulture, "@Number={0}", number); }
        }

        public override StringCollection GetFieldNames()
        {
            return fields;
        }

        public override ObjectKeyBase Clone()
        {
            return new NumberedObjectKey(this.Number);
        }

        public override bool IsNull
        {
            get { return false;}
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new NumberedObjectComparer();
        }

    }

    public abstract class PartitionNumberedObjectCollectionBase : SortedListCollectionBase
    {

        internal PartitionNumberedObjectCollectionBase(SqlSmoObject parent)
            : base(parent)
        {
        }

        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new PartitionNumberedObjectComparer());
        }

        public bool Contains(int number)
        {
            return this.Contains(new PartitionNumberedObjectKey(number));
        }


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

        public override int Compare(object obj1, object obj2)
        {
            return ((PartitionNumberedObjectKey)obj1).Number - ((PartitionNumberedObjectKey)obj2).Number;
        }
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
            fields = new StringCollection();
            fields.Add("PartitionNumber");
        }

        internal static StringCollection fields;

        public int Number
        {
            get { return number; }
            set { number = value; }
        }

        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}",
                                            number);
        }

        public override string UrnFilter
        {
            get { return string.Format(SmoApplication.DefaultCulture, "@PartitionNumber={0}", number); }
        }

        public override StringCollection GetFieldNames()
        {
            return fields;
        }

        public override ObjectKeyBase Clone()
        {
            return new PartitionNumberedObjectKey(this.Number);
        }

        public override bool IsNull
        {
            get { return false; }
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new PartitionNumberedObjectComparer();
        }

    }

   
}    
