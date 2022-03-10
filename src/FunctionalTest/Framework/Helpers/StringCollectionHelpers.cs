// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helpful extensions methods on <see cref="T:System.Collections.SpecializedStringCollection" />.
    /// </summary>
    public static class StringCollectionHelpers
    {
        /// <summary>
        /// Merges the contents of a StringCollection into a single string, with each string being on a new line. 
        /// </summary>
        /// <param name="sc">The string collection to convert.</param>
        /// <returns>A single string with the contents of the StringCollection</returns>
        public static string ToSingleString(this StringCollection sc)
        {
            if (sc == null)
            {
                return string.Empty;
            }

            return sc.Cast<string>().Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine(s)).ToString();
        }
    }
}
