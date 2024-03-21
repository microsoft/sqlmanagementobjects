// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// ColumnEncryptionKeyValueCollectionBase
    /// </summary>
    public abstract class ColumnEncryptionKeyValueCollectionBase : SortedListCollectionBase
    {

        internal ColumnEncryptionKeyValueCollectionBase(SqlSmoObject parent)
            : base(parent)
        {
        }

        /// <summary>
        /// Internal Storage
        /// </summary>
        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList(new ColumnEncryptionKeyValueObjectComparer());
        }

        /// <summary>
        ///  Contains Method
        /// </summary>
        /// <param name="ColumnMasterKeyID">Column Master Key Defintion ID</param>
        /// <returns>Returns if there is a CEK value encrypted by the given CMK ID.</returns>
        public bool Contains(int ColumnMasterKeyID)
        {
            return this.Contains(new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID));
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            int ColumnMasterKeyID = int.Parse(urn.GetAttribute("ColumnMasterKeyID"), SmoApplication.DefaultCulture);

            return new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID);
        }
    }

    internal class ColumnEncryptionKeyValueObjectComparer : ObjectComparerBase
    {
        internal ColumnEncryptionKeyValueObjectComparer()
            : base(null)
        {
        }

        public override int Compare(object obj1, object obj2)
        {
            return ((ColumnEncryptionKeyValueObjectKey)obj1).ColumnMasterKeyID - ((ColumnEncryptionKeyValueObjectKey)obj2).ColumnMasterKeyID;
        }
    }

    internal class ColumnEncryptionKeyValueObjectKey : ObjectKeyBase
    {
        public int ColumnMasterKeyID;

        public ColumnEncryptionKeyValueObjectKey(int columnMasterKeyID)
            : base()
        {
            this.ColumnMasterKeyID = columnMasterKeyID;
        }

        static ColumnEncryptionKeyValueObjectKey()
        {
            fields.Add("ColumnMasterKeyID");
        }

        internal static readonly StringCollection fields = new StringCollection();

        public override string ToString()
        {
            return string.Format(SmoApplication.DefaultCulture, "{0}",
                                            ColumnMasterKeyID);
        }

        /// <summary>
        /// This is the one used for constructing the Urn
        /// </summary>
        public override string UrnFilter
        {
            get { return string.Format(SmoApplication.DefaultCulture, "@ColumnMasterKeyID={0}", ColumnMasterKeyID); }
        }

        public override StringCollection GetFieldNames()
        {
            return fields;
        }

        public override ObjectKeyBase Clone()
        {
            return new ColumnEncryptionKeyValueObjectKey(this.ColumnMasterKeyID);
        }

        public override bool IsNull
        {
            get { return false; }
        }

        public override ObjectComparerBase GetComparer(IComparer stringComparer)
        {
            return new ColumnEncryptionKeyValueObjectComparer();
        }
    }
}