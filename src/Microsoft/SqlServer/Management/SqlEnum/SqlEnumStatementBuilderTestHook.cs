// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Test hook for the statement builder
    /// </summary>
    internal static class SqlEnumStatementBuilderTestHook
    {
        [ThreadStatic]
        private static Dictionary<string, string> sqlStatementFragmentDictionary;

        /// <summary>
        /// Add the sql statement fragment and its replacement
        /// </summary>
        /// <param name="originalString">The original string to look for</param>
        /// <param name="replacementString">The replacement</param>
        public static void AddSqlStatementFragmentReplacement(string originalString, string replacementString)
        {
            FragmentDictionary[originalString] = replacementString;
        }

        /// <summary>
        /// Clear the fragments to be replaced
        /// </summary>
        public static void Clear()
        {
            FragmentDictionary.Clear();
        }

        /// <summary>
        /// Fragments dictionary
        /// </summary>
        private static Dictionary<string, string> FragmentDictionary
        {
            get
            {
                if (sqlStatementFragmentDictionary == null)
                {
                    sqlStatementFragmentDictionary = new Dictionary<string, string>();
                }

                return sqlStatementFragmentDictionary;
            }
        }

        /// <summary>
        /// Gets the Fragments to be replaced
        /// </summary>
        public static IReadOnlyDictionary<string, string> SqlStatementFragmentsToBeReplaced
        {
            get
            {
                return FragmentDictionary;
            }
        }
    }
}
