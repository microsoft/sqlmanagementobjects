// Copyright (c) Microsoft Corporation.
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
            return sc.ToDelimitedSingleString("");
        }

        /// <summary>
        /// Returns the contents of the StringCollection as a single string, with each component string separated by new line + delimiter.
        /// The delimiter character is appended to every line, so it's not meant to be used the same way as String.Join
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string ToDelimitedSingleString(this StringCollection sc, string delimiter)
        {
            return sc.Cast<string>().Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine($"{s}{delimiter}")).ToString();
        }
    }
}
