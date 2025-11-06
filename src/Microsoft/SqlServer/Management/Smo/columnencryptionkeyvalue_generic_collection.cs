 // Copyright (c) Microsoft.
 // Licensed under the MIT license.
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Text;

#include "coll_macros.h"

namespace NAMESPACE_NAME
{
	///<summary>
	/// Strongly typed list of MAPPED_TYPE objects
	/// Has strongly typed support for all of the methods of the sorted list class
	///</summary>
	public SEALED_IMP PARTIAL_KEYWORD class TOKEN_PASTE( MAPPED_TYPE, COLLECTION_SUFFIX) : ColumnEncryptionKeyValueCollectionBase
	{
		internal TOKEN_PASTE( MAPPED_TYPE, COLLECTION_SUFFIX)(SqlSmoObject parentInstance)  : base(parentInstance)
		{
		}

#ifdef PARENT
		/// <summary>
		/// Returns the parent object
		/// </summary>
		public PARENT Parent
		{
			get
			{
				return this.ParentInstance as PARENT;
			}
		}
#endif
		
		/// <summary>
		/// Returns the column encryption key value for a given index
		/// </summary>
		/// <param name="index">The index in the collection</param>
		/// <returns>The column encryption key at the given index</returns>
		public MAPPED_TYPE this[Int32 index]
		{
			get
			{
				return GetObjectByIndex(index) as MAPPED_TYPE;
			}
		}

		/// <summary>
		/// Gets the column encryption key value for a given column master key id
		/// </summary>
		/// <param name="ColumnMasterKeyID">The Column Master Key ID</param>
		/// <returns>The CEK value if found, null otherwise</returns>
		public MAPPED_TYPE GetItemByColumnMasterKeyID(int ColumnMasterKeyID)
		{
			return InternalStorage[new ColumnEncryptionKeyValueObjectKey(ColumnMasterKeyID)] as MAPPED_TYPE;
		}

		/// <summary>
		/// Adds the CEK value to the collection.
		/// </summary>
		public void Add(MAPPED_TYPE MAPPED_TYPE_VAR)
		{
			InternalStorage.Add(new ColumnEncryptionKeyValueObjectKey(MAPPED_TYPE_VAR.ColumnMasterKeyID), MAPPED_TYPE_VAR);
		}

		/// <summary>
		/// Copies the collection to an arryay
		/// </summary>
		/// <param name="array">The array to copy to</param>
		/// <param name="index">The zero-based index in array at which copying begins</param>
		public void CopyTo(MAPPED_TYPE[] array, int index)
		{
			((ICollection)this).CopyTo(array, index);
		}

		/// <summary>
		/// Returns the collection element type
		/// </summary>
		/// <returns>The collection element type</returns>
		protected override Type GetCollectionElementType()
		{
			return typeof(MAPPED_TYPE);
		}

		internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
		{
			return  new MAPPED_TYPE(this, key, state);
		}
	}
}