// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using Microsoft.SqlServer.Management.Common;
    using Microsoft.SqlServer.Management.Sdk.Sfc;
    using Microsoft.SqlServer.Management.Smo.SqlEnum;


    /// <summary>
    /// Contains common utility functions
    /// </summary>
#if DEBUG || EXPOSE_MANAGED_INTERNALS
    public
#else
    internal
#endif
    class Util
    {
        /// <summary>
        /// Convert a database type name to the equivalent CLS type
        /// </summary>
        /// <param name="strDBType"></param>
        /// <returns></returns>
        static public String DbTypeToClrType(String strDBType)
        {
            // SMO_NEW_DATATYPE
            // Add the data type -> corresponding CLR type here
            String strType;
            switch(strDBType)
            {
                case "xml":
                case "json":
                case "vector":
                case "nvarchar":
                case "varchar":
                case "sysname":
                case "nchar":
                case "char":
                case "ntext":
                case "text":
                    strType = "System.String";
                    break;
                case "int":
                    strType = "System.Int32";
                    break;
                case "bigint":
                    strType = "System.Int64";
                    break;
                case "bit":
                    strType = "System.Boolean";
                    break;
                case "long":
                    strType = "System.Int32";
                    break;
                case "real":
                case "float":
                    strType = "System.Double";
                    break;
                case "datetime":
                case "datetime2":
                case "date":
                    // For "date" in particular, it is assumed the time portion is zeroed, so in effect merging it with a "time" (as a client-side TimeSpan) would merely
                    // add its ticks to the date to make a complete date+time for a DateTime object.
                    strType = "System.DateTime";
                    break;
                case "datetimeoffset":
                    strType = "System.DateTimeOffset";
                    break;
                case "time":
                case "timespan":
                    // T-SQL still doesn't have a real timespan type (time is just a 24-hour clock rep, but is the closest thing).
                    // We allow "timespan" as well here to be matched since we want the effect that it is a legal logical type in our enumerator mappings.
                    // It would really just be the number of ticks presented as a T-SQL int/bigint.
                    // Alternatively, we could go with the convention of also accounting for fractional seconds instead of ticks presented as a T-SQL decimal value.
                    strType = "System.TimeSpan";
                    break;
                case "tinyint":
                    strType = "System.Byte";
                    break;
                case "smallint":
                    strType = "System.Int16";
                    break;
                case "uniqueidentifier":
                    strType = "System.Guid";
                    break;
                case "numeric":
                case "decimal":
                    strType = "System.Decimal";
                    break;
                case "binary":
                case "image":
                case "varbinary":
                    strType = "System.Byte[]";
                    break;
                case "sql_variant":
                    strType = "System.Object";
                    break;
                default:
                    throw new InvalidConfigurationFileEnumeratorException(StringSqlEnumerator.UnknownType(strDBType));
            }
            return strType;
        }

        /// <summary>
        /// Transform a dataset into the expected enumeration result type
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="res"></param>
        /// <returns></returns>
        protected EnumResult TransformToRequest(DataSet ds, ResultType res)
        {
            if( ResultType.Default == res )
            {
                res = ResultType.DataSet;
            }

            if ( ResultType.DataSet == res )
            {
                return new EnumResult(ds, res);
            }
            else if( ResultType.DataTable == res )
            {
                return new EnumResult(ds.Tables[0], res);
            }

            throw new ResultTypeNotSupportedEnumeratorException(res);
        }

        /// <summary>
        /// Escape a particular character in a string
        /// </summary>
        /// <param name="value">The string</param>
        /// <param name="escapeCharacter">The character to escape</param>
        /// <returns>The equivalent string with the character escaped</returns>
        public static String EscapeString(String value, char escapeCharacter)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in value)
            {
                sb.Append(c);
                if( escapeCharacter == c )
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        static internal String MakeSqlString(String value)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("N'");
            sb.Append(EscapeString(value, '\''));
            sb.Append("'");
            return sb.ToString();
        }

        static internal Assembly LoadAssembly(string assemblyName)
        {
            Assembly a = null;
            try
            {
                String fullName = SqlEnumNetCoreExtension.GetAssembly(typeof(Util)).FullName;
                fullName = fullName.Replace(SqlEnumNetCoreExtension.GetAssembly(typeof(Util)).GetName().Name, assemblyName);
                a = Assembly.Load(new AssemblyName(fullName));
            }
            catch(Exception e)
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.FailedToLoadAssembly(assemblyName) + "\n\n" + e.ToString());
            }
            if( null == a )
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.FailedToLoadAssembly(assemblyName));
            }
            return a;
        }

        static internal Object CreateObjectInstance(Assembly assembly, string objectType)
        {
            Object o = SqlEnumNetCoreExtension.CreateInstance(assembly, objectType,
                false, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
                null, null, CultureInfo.InvariantCulture, null);
            if( null == o )
            {
                throw new InternalEnumeratorException(StringSqlEnumerator.CouldNotInstantiateObj(objectType));
            }
            return o;
        }

        //stops when a name is completed
        internal static String UnEscapeString(String escapedValue, char startEscapeChar, char escapeChar, ref int index)
        {
            return UnEscapeString(escapedValue, startEscapeChar, escapeChar, '\0', ref index);
        }

        //stops when a name is completed
        internal static String UnEscapeString(string escapedValue, char startEscapeChar, char escapeChar, char partSeperator, ref int index)
        {
            StringBuilder sb = new StringBuilder();
            bool delete = false;
            bool needTerminator = false;

            char c;
            for (; index < escapedValue.Length; index++)
            {
                c = escapedValue[index];
                if (false == needTerminator && startEscapeChar == c)
                {
                    needTerminator = true;
                    continue;
                }
                else if (escapeChar == c)
                {
                    if (false == delete)
                    {
                        delete = true;
                        continue;
                    }
                    delete = false;
                }
                else if(( true == delete && true == needTerminator ) || (c == partSeperator && false == needTerminator))
                {
                    break;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }


        internal static StringCollection SplitNames(string name)
        {
            return SplitNames(name, '\0');
        }

        internal static StringCollection SplitNames(string name, char partSeperator)
        {
            if (name == null)
            {
                return null;
            }

            StringCollection listNames = new StringCollection();

            string s;
            int pos = -1;
            while(pos < name.Length)
            {
                ++pos;
                s = Util.UnEscapeString(name, '[', ']', partSeperator, ref pos);
                listNames.Insert(0, s);
            }

            return listNames;
        }


        internal static string EscapeLikePattern(string pattern)
        {
            // The characters: %[]_ are special characters to the sql LIKE operator. To escape them,
            // enclose them in brackets so "ab[_]c" matches "ab_c" and "ab[[]c" matches ab[c
            // more information can be found at:
            // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_la-lz_115x.asp

            // defer stringbuilder creation until we know we need to escape
            StringBuilder sb = null;

            for (int i = 0; i < pattern.Length; i++)
            {
                bool escape = false;

                switch(pattern[i])
                {
                    case '%':
                    case '[':
                    case '_':
                        escape = true;
                        break;

                    default:
                        break;
                }

                if (escape && null == sb)
                {
                    // Havent had to escape yet,
                    // put the leading portion of the string into the stringbuilder
                    // from now on, every char will go into the
                    // the StringBuilder
                    sb = new StringBuilder(pattern.Length * 3);
                    sb.Append(pattern.Substring(0, i));
                }

                // if we are going to escape this character, then sb should not be null
                Debug.Assert(!escape || sb != null);


                if (escape)
                {
                    sb.Append("[");
                }

                if (sb != null)
                {
                    sb.Append(pattern[i]);
                }


                if (escape)
                {
                    sb.Append("]");
                }


            }

            // if we didnt do any escaping, just return the pattern, else return the StringBuilder sb
            return sb == null ? pattern : sb.ToString();
        }

        /// <summary>
        /// To be removed with transition to .NET 4.0 since it is implemented in Framework
        /// </summary>
        /// <returns></returns>
        internal static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null)
            {
                return true;
            }
            for (int i = 0; i < value.Length; i++)
            {
                if (!Char.IsWhiteSpace(value[i]))
                {
                    return false;
                }
            }
            return true;
        }

    }

    /// <summary>
    /// Denotes the type of file path for PathWrapper methods
    /// </summary>
    public enum PathType
    {
        Windows,
        Linux,
        Unspecified
    }

    /// <summary>
    /// Helper class to deal with Path manipulation and handles XI paths as well.
    /// We could take ServerConnection as a parameter and get the PathSeparator from it but these might be used in offline
    /// situations too
    /// </summary>
    static public class PathWrapper
    {

        /// <summary>
        /// Returns the appropriate path separator string for the given server connection
        /// </summary>
        /// <param name="serverConnection"></param>
        /// <returns></returns>
        static public string PathSeparatorFromServerConnection(ServerConnection serverConnection)
        {
            // Technically we could fetch it from serverproperty('PathSeparator') with SMO
            // but there's really no need to add a server call for this. HostPlatform is usually
            // already populated by the time this function is called
            return serverConnection.HostPlatform == HostPlatformNames.Linux ? "/" : @"\";
        }

        /// <summary>
        /// Combine 2 path strings. Needed to handle XI path as well.
        /// </summary>
        /// <param name="path1">First string.</param>
        /// <param name="path2">Second string</param>
        /// <returns>combined path string</returns>
        static public string Combine(string path1, string path2)
        {
            return Combine(path1, path2, PathType.Unspecified);
        }

        /// <summary>
        /// Combine 2 path strings. Needed to handle XI path as well.
        /// </summary>
        /// <param name="path1">First string.</param>
        /// <param name="path2">Second string</param>
        /// <param name="pathType">type of path. If Unspecified, the function will use Linux if s1 starts with /, Windows otherwise </param>
        /// <returns>combined path string</returns>
        static public string Combine(string path1, string path2, PathType pathType)
        {
            if (path1 == null)
            {
                throw new ArgumentNullException("path1");
            }

            if (path2 == null)
            {
                throw new ArgumentNullException("path2");
            }

            if (pathType == PathType.Unspecified)
            {
                pathType = path1.StartsWith("/") ? PathType.Linux : PathType.Windows;
            }

            Uri pathUri;
            bool fUriCreated = Uri.TryCreate(path1, UriKind.Absolute, out pathUri);
            if (fUriCreated && pathUri.IsUriSchemeHttps())
            {
                // SQL-XI path, need to use '/' instead of '\'
                //
                return string.Concat(path1, "/", path2);
            }
            else if (pathType == PathType.Windows)
            {
                return Path.Combine(path1, path2);
            }
            else // mimic Path.Combine
            {

                if (path1.Trim().Length == 0 || path2.StartsWith("/"))
                {
                    return path2;
                }
                return String.Format("{0}/{1}", path1.TrimEnd('/'), path2);
            }
        }

        /// <summary>
        /// Returns the directory string given a path. Handles XI path. It is a simple
        /// wrapper so it will be Garbage-In|Garbage-out.
        /// </summary>
        /// <param name="s1">Original path.</param>
        /// <returns>directory string</returns>
        static public string GetDirectoryName(string s1)
        {
            return GetDirectoryName(s1, PathType.Unspecified);
        }

        /// <summary>
        /// Returns the directory string given a path. Handles XI path. It is a simple
        /// wrapper so it will be Garbage-In|Garbage-out.
        /// </summary>
        /// <param name="s1">Original path.</param>
        /// <param name="pathType">type of path. If Unspecified, the function will use Linux if s1 starts with /, Windows otherwise </param>
        /// <returns>directory string</returns>
        static public string GetDirectoryName(string s1, PathType pathType)
        {
            if (pathType == PathType.Unspecified)
            {
                pathType = s1.StartsWith("/") ? PathType.Linux : PathType.Windows;
            }
            Uri pathUri;
            bool fUriCreated = Uri.TryCreate(s1, UriKind.Absolute, out pathUri);
            if (fUriCreated && pathUri.IsUriSchemeHttps())
            {
                int slashInd = s1.LastIndexOf("/", StringComparison.Ordinal);
                return s1.Substring(0, slashInd);
            }
            else if (pathType == PathType.Windows)
            {
                return Path.GetDirectoryName(s1);
            }
            else // mimic Path.GetDirectoryName
            {
                if (s1 == null || s1.Trim().Length == 0)
                {
                    throw new ArgumentNullException("s1");
                }
                if (s1 == "/")
                {
                    return null;
                }
                var lastSeparatorIndex = s1.LastIndexOf("/", StringComparison.Ordinal);
                // No directory
                if (lastSeparatorIndex < 0)
                {
                    return String.Empty;
                }
                if (lastSeparatorIndex == s1.Length - 1)
                {
                    return s1.TrimEnd('/');
                }
                if (lastSeparatorIndex == 0)
                {
                    return "/";
                }
                return s1.Substring(0, lastSeparatorIndex);
            }
        }

        /// <summary>
        /// Given a path returns true if the path is a XI path
        /// </summary>
        /// <param name="s1">Original path.</param>
        static public bool IsXIPath(string s1)
        {
            Uri pathUri;
            bool fUriCreated = Uri.TryCreate(s1, UriKind.Absolute, out pathUri);
            if (fUriCreated && pathUri.IsUriSchemeHttps())
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if the path includes a root, such as a UNC path or a drive
        /// letter on Windows, or begins with / on Linux
        /// If the path starts with / the path is treated as a Linux path, otherwise as a Windows path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public bool IsRooted(string path)
        {
            return IsRooted(path, PathType.Unspecified);
        }

        /// <summary>
        /// Determines if the path includes a root, such as a UNC path or a drive
        /// letter on Windows, or begins with / on Linux
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pathType">The type of path. If set to Unspecified - if the path starts with / the path is treated as a Linux path, otherwise as a Windows path
        /// </param>
        /// <returns></returns>
        static public bool IsRooted(string path, PathType pathType)
        {
            if (pathType == PathType.Unspecified)
            {
                pathType = path.StartsWith("/") ? PathType.Linux : PathType.Windows;
            }
            return pathType == PathType.Windows ? Path.IsPathRooted(path) : path.StartsWith("/");
        }
    }
}
