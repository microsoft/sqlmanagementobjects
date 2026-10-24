// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Collection Extension Method Class
    /// </summary>
    internal static class CollectionsExtensionMethods
    {
        public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> items)
        {

            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            foreach (T item in items)
            {
                target.Add(item);
            }
        }
    }
}
