// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System.Collections;
using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    public abstract class SecurityPredicateCollectionBase : SortedListCollectionBase<SecurityPredicate, SecurityPolicy>
    {

        internal SecurityPredicateCollectionBase(SqlSmoObject parent)
            : base((SecurityPolicy)parent)
        {
        }

        /// <summary>
        /// Internal Storage
        /// </summary>
        protected override void InitInnerCollection() => InternalStorage = new SmoSortedList<SecurityPredicate>(new SecurityPredicateObjectComparer());

        /// <summary>
        ///  Contains Method
        /// </summary>
        /// <param name="securityPredicateID">The security predicate id</param>
        /// <returns>The security predicate if it exists.</returns>
        public bool Contains(int securityPredicateID) => Contains(new SecurityPredicateObjectKey(securityPredicateID));

        /// <summary>
        /// Gets the filter predicate that applies to the specified object id if it exists.
        /// </summary>
        /// <param name="targetObjectID">The target object id</param>
        /// <returns>The security predicate if found, null otherwise</returns>
        public SecurityPredicate GetItemByTargetObjectID(int targetObjectID) => GetItemByTargetObjectID(targetObjectID, SecurityPredicateType.Filter, SecurityPredicateOperation.All);

        /// <summary>
        /// Gets the security predicate for a given target object ID, type, and operation.
        /// </summary>
        /// <param name="targetObjectID">The target object id</param>
        /// <param name="predicateType">The type of the security predicate</param>
        /// <param name="predicateOperation">The operation type of the security predicate</param>
        /// <returns>The security predicate if found, null otherwise</returns>
        public SecurityPredicate GetItemByTargetObjectID(int targetObjectID, SecurityPredicateType predicateType, SecurityPredicateOperation predicateOperation)
        {
            foreach (var secpred in InternalStorage)
            {
                if (secpred.TargetObjectID == targetObjectID
                    && secpred.PredicateType == predicateType
                    && secpred.PredicateOperation == predicateOperation)
                {
                    return secpred;
                }
            }

            return null;
        }

        internal override ObjectKeyBase CreateKeyFromUrn(Urn urn)
        {
            var securityPredicateID = int.Parse(urn.GetAttribute("SecurityPredicateID"), SmoApplication.DefaultCulture);

            return new SecurityPredicateObjectKey(securityPredicateID);
        }
    }

    internal class SecurityPredicateObjectComparer : ObjectComparerBase
    {
        internal SecurityPredicateObjectComparer()
            : base(null)
        {
        }

        public override int Compare(object obj1, object obj2) => ((SecurityPredicateObjectKey)obj1).SecurityPredicateID - ((SecurityPredicateObjectKey)obj2).SecurityPredicateID;
    }

    internal class SecurityPredicateObjectKey : ObjectKeyBase
    {
        protected int securityPredicateID;

        public SecurityPredicateObjectKey(int securityPredicateID)
            : base()
        {
            this.securityPredicateID = securityPredicateID;
        }

        static SecurityPredicateObjectKey()
        {
            _ = fields.Add(nameof(SecurityPredicate.SecurityPredicateID));
        }

        internal static readonly StringCollection fields = new StringCollection();

        public int SecurityPredicateID
        {
            get { return securityPredicateID; }
            set { securityPredicateID = value; }
        }

        public override string ToString() => string.Format(SmoApplication.DefaultCulture, "{0}", SecurityPredicateID);

        public override string UrnFilter => string.Format(SmoApplication.DefaultCulture, "@SecurityPredicateID={0}", securityPredicateID);

        public override StringCollection GetFieldNames() => fields;

        public override ObjectKeyBase Clone() => new SecurityPredicateObjectKey(SecurityPredicateID);

        public override bool IsNull => false;

        public override ObjectComparerBase GetComparer(IComparer stringComparer) => new SecurityPredicateObjectComparer();
    }
}