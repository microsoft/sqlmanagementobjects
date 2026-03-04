// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;

namespace Microsoft.SqlServer.Management.Smo
{
    public sealed partial class SearchPropertyCollection : SimpleObjectCollectionBase<SearchProperty, SearchPropertyList>
    {
        protected override void InitInnerCollection()
        {
            InternalStorage = new SmoSortedList<SearchProperty>(new SimpleObjectCaseSensitiveComparer());
        }

        internal class SimpleObjectCaseSensitiveComparer : IComparer
        {
            int IComparer.Compare(object obj1, object obj2)
            {
                return string.Compare((obj1 as SimpleObjectKey).Name, (obj2 as SimpleObjectKey).Name, false, SmoApplication.DefaultCulture);
            }
        }
    }
}
