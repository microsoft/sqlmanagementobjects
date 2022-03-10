// Copyright (c) Microsoft.
// Licensed under the MIT license.


using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Diagnostics;
using TraceHelper = Microsoft.SqlServer.Test.Manageability.Utils.Helpers.TraceHelper;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helper methods and values for general SMO objects
    /// </summary>
    public static class SmoObjectHelpers
    {
        /// <summary>
        /// Generates a unique object name, with optional prefix. This name also optionally will
        /// add the following characters to test that object names are escaped correctly :
        ///
        ///     Single-Quote            '
        ///     Double Single-Quote     ''
        ///     Closing Bracket         ]
        ///     Double Closing Bracket  ]]
        ///     {enclosed guid}
        /// </summary>
        /// <param name="dbNamePrefix"></param>
        /// <param name="includeClosingBracket"></param>
        /// <param name="includeDoubleClosingBracket"></param>
        /// <param name="includeSingleQuote"></param>
        /// <param name="includeDoubleSingleQuote"></param>
        public static string GenerateUniqueObjectName(string dbNamePrefix = "",
            bool includeClosingBracket = true,
            bool includeDoubleClosingBracket = true,
            bool includeSingleQuote = true,
            bool includeDoubleSingleQuote = true)
        {
            return string.Format("{0}{1}{2}{3}{4}{5}",
                dbNamePrefix ?? string.Empty,
                includeDoubleSingleQuote ? "''" : string.Empty,
                includeDoubleClosingBracket ? "]]" : string.Empty,
                includeClosingBracket ? "]" : string.Empty,
                includeSingleQuote ? "'" : string.Empty,
                "{" + Guid.NewGuid() + "}");
        }

        /// <summary>
        /// Escapes a character in a string using the normal SQL method of replacing all
        /// instances of that character with that character repeated two times (so ' becomes '')
        /// </summary>
        /// <param name="str"></param>
        /// <param name="escapeChar"></param>
        /// <returns></returns>
        public static string SqlEscapeString(string str, char escapeChar)
        {
            string escapeString = escapeChar.ToString();
            return str.Replace(escapeString, escapeString + escapeString);
        }

        /// <summary>
        /// Escapes a string by replacing all instances of ] with ]]
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SqlEscapeClosingBracket(this string str)
        {
            return SqlEscapeString(str, ']');
        }

        /// <summary>
        /// Escapes a string by replacing all instances of ' with ''
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SqlEscapeSingleQuote(this string str)
        {
            return SqlEscapeString(str, '\'');
        }

        /// <summary>
        /// Quotes a string in square brackets [], escaping the closing brackets
        /// in the string as necessary
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SqlBracketQuoteString(this string str)
        {
            return "[" + SqlEscapeClosingBracket(str) + "]";
        }

        /// <summary>
        /// Quotes a string in single quotes '', escaping the single quotes
        /// in the string as necessary
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string SqlSingleQuoteString(this string str)
        {
            return "'" + SqlEscapeSingleQuote(str) + "'";
        }

        /// <summary>
        /// Creates an index on the optionally specified columns (defaulting to the first one if none are specified).
        /// </summary>
        /// <param name="tableView"></param>
        /// <param name="namePrefix"></param>
        /// <param name="indexProperties"></param>
        /// <returns></returns>
        public static Microsoft.SqlServer.Management.Smo.Index CreateIndex(this TableViewTableTypeBase tableView,
            string namePrefix,
            IndexProperties indexProperties = null)
        {
            if (indexProperties == null)
            {
                //If caller doesn't specify index properties use defaults
                indexProperties = new IndexProperties();
            }

            var index = new Microsoft.SqlServer.Management.Smo.Index(tableView, SmoObjectHelpers.GenerateUniqueObjectName(namePrefix));
            if (indexProperties.Columns == null)
            {
                //Default to using first column if none were specified
                indexProperties.Columns = new[] { tableView.Columns[0] };
            }
            foreach (Column column in indexProperties.Columns)
            {
                index.IndexedColumns.Add(new IndexedColumn(index, column.Name));

            }
            index.IndexType = indexProperties.IndexType;
            index.IndexKeyType = indexProperties.KeyType;
            index.IsClustered = indexProperties.IsClustered;
            index.IsUnique = indexProperties.IsUnique;
            index.OnlineIndexOperation = indexProperties.OnlineIndexOperation;

            // Only set the resumable property if specified as true, since this can be run
            // against server versions that don't support the resumable option.
            if (indexProperties.Resumable)
            {
                index.ResumableIndexOperation = indexProperties.Resumable;
            }

           TraceHelper.TraceInformation("Creating new index \"{0}\" with IndexType {1} KeyType {2}, IsClustered {3}, IsUnique {4}, IsOnline {5}, IsResumable {6} and {7} columns",
                index.Name,
                indexProperties.IndexType,
                indexProperties.KeyType,
                indexProperties.IsClustered,
                indexProperties.IsUnique,
                indexProperties.OnlineIndexOperation,
                indexProperties.Resumable,
                indexProperties.Columns.Length);
            index.Create();
            return index;
        }

        /// <summary>
        /// Safely calls Drop() on a set of <see cref="IDroppable"/> objects. This will
        /// catch any exceptions thrown when calling Drop(), log them and then move
        /// on to the next object.
        /// </summary>
        /// <param name="objs"></param>
        public static void SafeDrop(params IDroppable[] objs)
        {
            foreach (IDroppable obj in objs)
            {
                if (obj == null)
                {
                    continue;
                }

                string name = "<unknown>";
                try
                {
                    name = obj.ToString();
                    NamedSmoObject namedObj = obj as NamedSmoObject;
                    if (namedObj != null)
                    {
                        name = namedObj.Name;
                    }

                   TraceHelper.TraceInformation("Safely dropping object {0}", name);
                    obj.Drop();
                }
                catch (Exception e)
                {
                    //Don't want to throw even if an error occurs, just log it for
                    //debugging purposes
                    Trace.TraceWarning("Exception trying to drop object {0} - {1}. Ignoring as this object is being safely dropped.", name, e.Message);
                }
            }

        }
    }
}
