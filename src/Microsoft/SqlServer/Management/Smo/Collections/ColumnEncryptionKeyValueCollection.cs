// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections;

namespace Microsoft.SqlServer.Management.Smo
{
    ///<summary>
    /// Strongly typed list of MAPPED_TYPE objects
    /// Has strongly typed support for all of the methods of the sorted list class
    ///</summary>
    public sealed  class ColumnEncryptionKeyValueCollection : ColumnEncryptionKeyValueCollectionBase
    {
        internal ColumnEncryptionKeyValueCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }


        /// <summary>
        /// Returns the parent object
        /// </summary>
        public ColumnEncryptionKey Parent
        {
            get
            {
                return this.ParentInstance as ColumnEncryptionKey;
            }
        }

        
        /// <summary>
        /// Returns the column encryption key value for a given index
        /// </summary>
        /// <param name="index">The index in the collection</param>
        /// <returns>The column encryption key at the given index</returns>
        public ColumnEncryptionKeyValue this[Int32 index]
        {
            get
            {
                return GetObjectByIndex(index) as ColumnEncryptionKeyValue;
            }
        }

        /// <summary>
        /// Gets the column encryption key value for a given column master key id
        /// </summary>
        /// <param name="ColumnMasterKeyID">The Column Master Key ID</param>
        /// <returns>The CEK value if found, null otherwise</returns>
        public ColumnEncryptionKeyValue GetItemByColumnMasterKeyID(int ColumnMasterKeyID)
        {
            return InternalStorage[new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID)] as ColumnEncryptionKeyValue;
        }

        /// <summary>
        /// Adds the CEK value to the collection.
        /// </summary>
        public void Add(ColumnEncryptionKeyValue columnEncryptionKeyValue)
        {
            InternalStorage.Add(new ColumnEncryptionKeyValueObjectKey(columnEncryptionKeyValue.ColumnMasterKeyID), columnEncryptionKeyValue);
        }

        /// <summary>
        /// Copies the collection to an arryay
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="index">The zero-based index in array at which copying begins</param>
        public void CopyTo(ColumnEncryptionKeyValue[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        /// <summary>
        /// Returns the collection element type
        /// </summary>
        /// <returns>The collection element type</returns>
        protected override Type GetCollectionElementType()
        {
            return typeof(ColumnEncryptionKeyValue);
        }

        internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return  new ColumnEncryptionKeyValue(this, key, state);
        }
    }
}
