// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Helper class for extension methods
    /// </summary>
    internal static class ExtensionClass
    {
        /// <summary>
        /// Add StringCollection to StringCollection
        /// </summary>
        /// <param name="strcol1"></param>
        /// <param name="strcol2"></param>
        internal static void AddCollection(this StringCollection strcol1,StringCollection strcol2)
        {
            foreach (string s in strcol2)
            {
                strcol1.Add(s);   
            }
        }

        /// <summary>
        /// Add Ienumerable to StringCollection
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="enumerableString"></param>
        internal static void AddCollection(this StringCollection collection, IEnumerable<string> enumerableString)
        {
            foreach (string s in enumerableString)
            {
                collection.Add(s);
            }
        }
    }
}
