// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Collection of ColumnEncryptionKeyValue objects associated with a ColumnEncryptionKey
    /// </summary>
    public sealed partial class ColumnEncryptionKeyValueCollection : ColumnEncryptionKeyValueCollectionBase
    {
        internal ColumnEncryptionKeyValueCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }


        /// <summary>
        /// Returns the parent object
        /// </summary>
        public ColumnEncryptionKey Parent => ParentInstance as ColumnEncryptionKey;

        protected override string UrnSuffix => ColumnEncryptionKeyValue.UrnSuffix;


        /// <summary>
        /// Gets the column encryption key value for a given column master key id
        /// </summary>
        /// <param name="ColumnMasterKeyID">The Column Master Key ID</param>
        /// <returns>The CEK value if found, null otherwise</returns>
        public ColumnEncryptionKeyValue GetItemByColumnMasterKeyID(int ColumnMasterKeyID) => InternalStorage[new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID)];

        /// <summary>
        /// Adds the CEK value to the collection.
        /// </summary>
        public void Add(ColumnEncryptionKeyValue columnEncryptionKeyValue) => InternalStorage.Add(new ColumnEncryptionKeyValueObjectKey(columnEncryptionKeyValue.ColumnMasterKeyID), columnEncryptionKeyValue);


        internal override ColumnEncryptionKeyValue GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state) => new ColumnEncryptionKeyValue(this, key, state);
    }
}
