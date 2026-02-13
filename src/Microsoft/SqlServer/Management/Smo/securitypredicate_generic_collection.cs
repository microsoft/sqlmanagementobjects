 // Copyright (c) Microsoft Corporation.
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
	public SEALED_IMP PARTIAL_KEYWORD class TOKEN_PASTE( MAPPED_TYPE, COLLECTION_SUFFIX) : SecurityPredicateCollectionBase
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
		/// Returns the security predicate for a given index
		/// </summary>
		/// <param name="index">The index in the collection</param>
		/// <returns>The security predicate at the given index</returns>
		public MAPPED_TYPE this[Int32 index]
		{
			get
			{
				return GetObjectByIndex(index) as MAPPED_TYPE;
			}
		}
		/// <summary>
		/// Adds the security predicate to the collection.
		/// </summary>
		public void Add(MAPPED_TYPE MAPPED_TYPE_VAR)
		{
			// Since we don't necessarily know what predicate ID the server will assign this security predicate,
			// generate a unique estimated ID based on the last security predicate in the collection.
			// The actual resultant ID may differ, in which case the next refresh on the policy will pick up the change.
			//
			if (InternalStorage.Contains(MAPPED_TYPE_VAR.key))
			{
				MAPPED_TYPE_VAR.SecurityPredicateID = this[this.Count - 1].SecurityPredicateID + 1;
				MAPPED_TYPE_VAR.key = new SecurityPredicateObjectKey(MAPPED_TYPE_VAR.SecurityPredicateID);
			}

			InternalStorage.Add(MAPPED_TYPE_VAR.key, MAPPED_TYPE_VAR);
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
