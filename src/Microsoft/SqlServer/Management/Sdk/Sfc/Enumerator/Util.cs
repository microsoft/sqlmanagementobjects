// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    using System;
    using System.Collections.Specialized;
    using System.Data;
    using System.Globalization;
    using System.Reflection;
    using System.Text;


    /// <summary>
    /// Contains common utility functions
    /// </summary>
    public class Util
	{
		/// <summary>
		/// Convert a database type name to the equivalent CLS type
		/// </summary>
		/// <param name="strDBType"></param>
		/// <returns></returns>
        static public String DbTypeToClrType(String strDBType)
		{
			String strType;
			switch(strDBType)
			{
				case "xml":goto case "text";
                case "json": goto case "text";
                case "nvarchar":goto case "text";
				case "varchar":goto case "text";
				case "sysname":goto case "text";
				case "nchar":goto case "text";
				case "char":goto case "text";
				case "ntext":goto case "text";
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
				case "real":goto case "float";
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
					strType = "System.Decimal";
					break;
                case "decimal": goto case "numeric";
				case "binary":goto case "varbinary";
				case "image":goto case "varbinary";
				case "varbinary":
					strType = "System.Byte[]";
					break;
				case "sql_variant":
					strType = "System.Object";
					break;
				default:
					throw new InvalidConfigurationFileEnumeratorException(SfcStrings.UnknownType(strDBType));
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
            else 
            {
                TraceHelper.Assert( ResultType.DataTable == res );
				return new EnumResult(ds.Tables[0], res);
            }
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

        /// <summary>
        /// Load assembly replacing it's name.
        /// </summary>
		static public Assembly LoadAssembly(string assemblyName)
		{
			Assembly a = null;
			try
			{
                String fullName = SmoManagementUtil.GetExecutingAssembly().FullName;
                fullName = fullName.Replace(SmoManagementUtil.GetExecutingAssembly().GetName().Name, assemblyName);
				a = SmoManagementUtil.LoadAssembly(fullName);
			}
			catch(Exception e)
			{
				throw new InternalEnumeratorException(SfcStrings.FailedToLoadAssembly(assemblyName) + "\n\n" + e.ToString());
			}
			if( null == a )
			{
				throw new InternalEnumeratorException(SfcStrings.FailedToLoadAssembly(assemblyName));
			}
			return a;
		}

		static internal Object CreateObjectInstance(Assembly assembly, string objectType)
		{
			Object o = assembly.CreateInstance(objectType,
				false, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,
				null, null, CultureInfo.InvariantCulture, null);
			if( null == o )
			{
				throw new InternalEnumeratorException(SfcStrings.CouldNotInstantiateObj(objectType));
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
				TraceHelper.Assert(!escape || sb != null);

				
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

	}
			
}
