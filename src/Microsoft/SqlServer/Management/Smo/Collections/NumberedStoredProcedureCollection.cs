// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// Collection of NumberedStoredProcedure objects associated with a StoredProcedure
    /// </summary>
    public sealed partial class NumberedStoredProcedureCollection : NumberedObjectCollectionBase
    {

        internal NumberedStoredProcedureCollection(SqlSmoObject parentInstance) : base(parentInstance)
        {
        }

        /// <summary>
        /// Returns the parent StoredProcedure
        /// </summary>
        public StoredProcedure Parent => ParentInstance as StoredProcedure;

        protected override string UrnSuffix => NumberedStoredProcedure.UrnSuffix;

        internal override NumberedStoredProcedure GetCollectionElementInstance(ObjectKeyBase key, SqlSmoState state)
        {
            return new NumberedStoredProcedure(this, key, state);
        }


        public NumberedStoredProcedure GetProcedureByNumber(short number)
        {
            return GetObjectByKey(new NumberedObjectKey(number)) as NumberedStoredProcedure;
        }
    }
}
