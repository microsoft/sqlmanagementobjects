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
    public abstract class ColumnEncryptionKeyValueCollectionBase : SortedListCollectionBase<ColumnEncryptionKeyValue, ColumnEncryptionKey>
    {

        internal ColumnEncryptionKeyValueCollectionBase(SqlSmoObject parent)
            : base((ColumnEncryptionKey)parent)
        {
        }

        /// <summary>
        /// Internal Storage
        /// </summary>
        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<ColumnEncryptionKeyValue>(new ColumnEncryptionKeyValueObjectComparer());

        /// <summary>
        ///  Contains Method
        /// </summary>
        /// <param name="ColumnMasterKeyID">Column Master Key Defintion ID</param>
        /// <returns>Returns if there is a CEK value encrypted by the given CMK ID.</returns>
        public bool Contains(int ColumnMasterKeyID) => Contains(new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID));

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var ColumnMasterKeyID = int.Parse(urn.GetAttribute("ColumnMasterKeyID"), SmoApplication.DefaultCulture);

            return new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID);
        }
    }

    internal class ColumnEncryptionKeyValueObjectComparer : ObjectComparerBase
    {
        internal ColumnEncryptionKeyValueObjectComparer()
            : base(null)
        {
        }

        public override int Compare(object obj1, object obj2) => ((ColumnEncryptionKeyValueObjectKey)obj1).ColumnMasterKeyID - ((ColumnEncryptionKeyValueObjectKey)obj2).ColumnMasterKeyID;
    }

    internal class ColumnEncryptionKeyValueObjectKey : ObjectKeyBase
    {
        public int ColumnMasterKeyID;

        public ColumnEncryptionKeyValueObjectKey(int columnMasterKeyID)
            : base()
        {
            ColumnMasterKeyID = columnMasterKeyID;
        }

        static ColumnEncryptionKeyValueObjectKey()
        {
            _ = fields.Add(nameof(ColumnMasterKeyID));
        }

        internal static readonly StringCollection fields = new StringCollection();

        public override string ToString() => string.Format(SmoApplication.DefaultCulture, "{0}",
                                            ColumnMasterKeyID);

        /// <summary>
        /// This is the one used for constructing the Urn
        /// </summary>
        public override string UrnFilter => string.Format(SmoApplication.DefaultCulture, "@ColumnMasterKeyID={0}", ColumnMasterKeyID);

        public override StringCollection GetFieldNames() => fields;

        public override ObjectKeyBase Clone() => new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID);

        public override bool IsNull => false;

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new ColumnEncryptionKeyValueObjectComparer();
    }
}