// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Collection of SecurityPredicate objects associated with a SecurityPolicy
    /// </summary>
    public sealed partial class SecurityPredicateCollection : SecurityPredicateCollectionBase
    {
        internal SecurityPredicateCollection(SqlSmoObject parentInstance)  : base(parentInstance)
        {
        }

        /// <summary>
        /// Returns the parent object
        /// </summary>
        public SecurityPolicy Parent => ParentInstance as SecurityPolicy;

        protected override string UrnSuffix => SecurityPredicate.UrnSuffix;

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
                securityPredicate.SecurityPredicateID = this[Count - 1].SecurityPredicateID + 1;
                securityPredicate.key = new SecurityPredicateObjectKey(securityPredicate.SecurityPredicateID);
            }

            InternalStorage.Add(securityPredicate.key, securityPredicate);
        }

        internal override SecurityPredicate GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return  new SecurityPredicate(this, key, state);
        }
    }
}
