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
    public sealed  class SecurityPredicateCollection : SecurityPredicateCollectionBase
    {
        internal SecurityPredicateCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }


        /// <summary>
        /// Returns the parent object
        /// </summary>
        public SecurityPolicy Parent
        {
            get
            {
                return this.ParentInstance as SecurityPolicy;
            }
        }

        
        /// <summary>
        /// Returns the security predicate for a given index
        /// </summary>
        /// <param name="index">The index in the collection</param>
        /// <returns>The security predicate at the given index</returns>
        public SecurityPredicate this[Int32 index]
        {
            get
            {
                return GetObjectByIndex(index) as SecurityPredicate;
            }
        }
        /// <summary>
        /// Adds the security predicate to the collection.
        /// </summary>
        public void Add(SecurityPredicate securityPredicate)
        {
            // Since we don't necessarily know what predicate ID the server will assign this security predicate,
            // generate a unique estimated ID based on the last security predicate in the collection.
            // The actual resultant ID may differ, in which case the next refresh on the policy will pick up the change.
            //
            if (InternalStorage.Contains(securityPredicate.key))
            {
                securityPredicate.SecurityPredicateID = this[this.Count - 1].SecurityPredicateID + 1;
                securityPredicate.key = new SecurityPredicateObjectKey(securityPredicate.SecurityPredicateID);
            }

            InternalStorage.Add(securityPredicate.key, securityPredicate);
        }

        /// <summary>
        /// Copies the collection to an arryay
        /// </summary>
        /// <param name="array">The array to copy to</param>
        /// <param name="index">The zero-based index in array at which copying begins</param>
        public void CopyTo(SecurityPredicate[] array, int index)
        {
            ((ICollection)this).CopyTo(array, index);
        }

        /// <summary>
        /// Returns the collection element type
        /// </summary>
        /// <returns>The collection element type</returns>
        protected override Type GetCollectionElementType()
        {
            return typeof(SecurityPredicate);
        }

        internal override SqlSmoObject GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return  new SecurityPredicate(this, key, state);
        }
    }
}
