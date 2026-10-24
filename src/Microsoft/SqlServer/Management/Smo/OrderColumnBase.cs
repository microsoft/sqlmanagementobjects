// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class OrderColumn  : NamedSmoObject
    {
        // Constructors
        internal OrderColumn(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        public OrderColumn(UserDefinedFunction udf, string name, bool descending) : base()
        {
            ValidateName(name);
            this.key = new SimpleObjectKey(name);
            this.Parent = udf;
            this.Descending = descending;
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
               return "OrderColumn";
            }
        }

    }
}

