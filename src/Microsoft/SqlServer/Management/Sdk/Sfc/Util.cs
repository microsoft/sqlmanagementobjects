// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Microsoft.Win32;

// TODO: either provide a fully functional safe string formatting, or move this thing to Acme and let each domain roll their own implementation
namespace Microsoft.SqlServer.Management.Sdk.Sfc
{

    /// <summary>
    /// This enumeration determines the use of query iterators on object query results. It only applies to queries returning an iterator.
    /// Caching avoids these issues entirely at the expense of first caching all of the query results in memory.
    /// Single or multiple active queries both use a non-cached iterator, but affect how the domain instance responds the GetConnection method.
    /// 
    /// All iterators of query results must always be disposed as soon as they are no longer in use regardless of mode.
    /// </summary>
    public enum SfcObjectQueryMode
    {
        /// <summary>
        /// CachedQueries avoids any issues with nested or overlapping active queries by internally caching the query results and iterating on that.
        /// </summary>
        CachedQuery = 0,
        /// A non-cached iterator is desired since no other query will be issued on the same domain instance connection while this iterator is open and in use.
        SingleActiveQuery,
        /// A non-cached iterator is desired, but since other queries may be issued on the domain instance's connection while this iterator is still open and in use,
        /// a more suitable cloned or alternate connection may be used. Assume the domain instance connection is busy with another query even if it currently is not.
        MultipleActiveQueries
    }

    /// <summary>
    /// Utility class for miscellaneous functions
    /// </summary>
    public static class SfcUtility
    {
        //sqlce tools path    
        private static String sqlceToolsPath = null;
        private static MethodInfo getChildTypeInfo = null;
        internal static Dictionary<string, Type> typeCache = new Dictionary<string,Type>();

        /// <summary>
        /// returns equivalent SML-URI for a given uri
        /// </summary>
        /// <param name="urn"></param>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        public static String GetSmlUri(Urn urn, Type instanceType)
        {
            //i don't use the cache by default in the public api because we
            //clean up the cache internally in sfc
            return GetSmlUri(urn, instanceType, false);
        }

        /// <summary>
        /// Calls the SqlSmoObject.GetChildType for the string type passed as a parameter
        /// </summary>
        /// <param name="type"></param>
        /// <param name="parentName"></param>
        /// <param name="instanceType"></param>
        /// <returns></returns>
        internal static Type GetSmoChildType(string type, string parentName, Type instanceType)
        {
            // Ideally, SMO should be a registered domain that implements this call via an interface call
            // We don't have that, so use a reflection temporary solution

            // Used to be:
            //levelType = SqlSmoObject.GetChildType(type, null);

            // Now we can't access SMO, so we have to use reflection:
            if (getChildTypeInfo == null)
            {
                Assembly assembly = instanceType.Assembly();
                Type sqlSmoObjectType = assembly.GetType("Microsoft.SqlServer.Management.Smo.SqlSmoObject");
                getChildTypeInfo = sqlSmoObjectType.GetMethod("GetChildType", BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);
            }

            return (Type)getChildTypeInfo.Invoke(null, new object[] { type, parentName });

        }

        /// <summary>
        /// returns equivalent SML-URI for a given uri
        /// </summary>
        /// <param name="urn"></param>
        /// <param name="instanceType"></param>
        /// <param name="useCache">if true then a urn cache is used to improve performance</param>
        /// <returns></returns>
        internal static String GetSmlUri(Urn urn, Type instanceType, bool useCache)
        {
            if ((urn == null) || (String.IsNullOrEmpty(urn.ToString())))
            {
                return null;
            }

            StringBuilder smlUri = new StringBuilder();

            String typeFullName = instanceType.FullName;
            String rootTypeFullName = SfcRegistration.GetRegisteredDomainForType(instanceType).RootTypeFullName; 
            String domainInfo = rootTypeFullName.Substring(0, rootTypeFullName.LastIndexOf('.') + 1);
            String parentName = null;
            int length = urn.XPathExpression.Length;
            bool isSmoType = instanceType.FullName.StartsWith("Microsoft.SqlServer.Management.Smo", StringComparison.Ordinal);
            for (int i = 0; i < length; i++)
            {
                XPathExpressionBlock b = urn.XPathExpression[i];
                string type = b.Name;
                smlUri.Append("/" + SfcSecureString.XmlEscape(type));

                Type levelType = null;
                if (isSmoType)
                {
                    if (useCache)
                    {
                        // to improve performance we keep a cache of name, Type so we don't have to call GetSMOChildType every time
                        if (!typeCache.ContainsKey(parentName + type))
                        {
                            typeCache[parentName + type] = GetSmoChildType(type, parentName, instanceType);
                        }

                        levelType = typeCache[parentName + type];
                    }
                    else
                    {
                        levelType = GetSmoChildType(type, parentName, instanceType);
                    }
 

                    parentName = levelType.Name;
                }
                else
                {
                    levelType = SfcRegistration.GetObjectTypeFromFullName(domainInfo + type);
                }

                SfcMetadataDiscovery metaData = new SfcMetadataDiscovery(levelType);

                bool hasMultiPartKey = false;
                StringBuilder keyString = new StringBuilder();

                foreach (SfcMetadataRelation property in metaData.ReadOnlyKeys)
                {
                    if (hasMultiPartKey)
                    {
                        keyString.Append(".");
                    }
                    else
                    {
                        hasMultiPartKey = true;
                    }

                    String escapedString = SfcSecureString.SmlEscape(b.GetAttributeFromFilter(property.PropertyName));

                    if (escapedString == null)
                    {
                        return null; //An SML URI is invalid if it has null keys
                    }

                    keyString.Append(escapedString);
                }

                if (!string.IsNullOrEmpty(keyString.ToString()))
                {
                    smlUri.Append("/" + SfcSecureString.XmlEscape(keyString.ToString()));
                }
            }

            return smlUri.ToString();
        }

        public static String GetUrn(object obj)
        {
            if (obj is SfcInstance)
            {
                return ((SfcInstance)obj).KeyChain.Urn;
            }
            else if(obj is IAlienObject)
            {
                IAlienObject alien = obj as IAlienObject;
                return alien.GetUrn();
            }
            else
            {
                return null;
            }
        }

        internal static object GetParent(object obj)
        {
            if (obj is SfcInstance)
            {
                return ((SfcInstance)obj).Parent;
            }
            else if (obj is IAlienObject)
            {
                IAlienObject alien = obj as IAlienObject;
                return alien.GetParent();
            }
            else
            {
                return null;
            }
        }

        internal static String GetXmlContent(XmlReader reader, String typeTag, bool isEmptyNode)
        {
            StringBuilder sb = new StringBuilder();
            XmlWriter writer = XmlWriter.Create(sb);
            writer.WriteStartDocument();
            writer.WriteStartElement(typeTag);

            if (!isEmptyNode)
            {
                do
                {
                    writer.WriteNode(reader, false);
                } while (reader.IsStartElement());
            }

            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Close();

            return sb.ToString();
        }


        internal static object GetXmlValue(String xmlContent, Type valueType)
        {
            StringReader sr = new StringReader(xmlContent);
            XmlReader valueReader = XmlReader.Create(sr);

            XmlSerializer serializer = new XmlSerializer(valueType);
            return serializer.Deserialize(valueReader);
        }
    }

    // TODO: find a better name
    public static class SfcSecureString
    {
        internal const char SmlEscaper = '_';

        //stringRegex - helps in getting all the characters apart from XML Process instructions 
        //numberRegex - helps in getting all the numeric equivalents of restricted XML characters
        //The pattern being matched is <?char *?> where * is ascii value of restricted chars
        static Regex stringRegex = new Regex(@"<\?char" + @"\s\d+" + @"\?>");
        static Regex numberRegex = new Regex(@"<\?" + "char" + @"(\s)(?<number>(\d+))" + @"\?>");

        private static String EscapeImpl(String s, char cEsc)
        {
            StringBuilder sb = new StringBuilder();
            foreach(char c in s)
            {
                sb.Append(c);
                if( cEsc == c )
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static String EscapeSquote(String value)
        {
            return EscapeImpl(value, '\'');
        }

        public static String EscapeBracket(String value)
        {
            return EscapeImpl(value, ']');
        }

        public static String SmlEscape(String originalString)
        {
            if (String.IsNullOrEmpty(originalString))
            {
                return null;
            }

            StringBuilder newString = new StringBuilder();

            // Escape any existing smlEscape chars along with 
            //'/'        - to separate these from URI sense /
            //'.'        - to give a separation of keys
            //#,:,?,@    - to protect from SML IF standards
            //&,<,>,','' - to escape XML keywords
            StringBuilder escapedString = new StringBuilder();
            foreach (char c in originalString)
            {
                switch (c)
                {
                    case SmlEscaper: 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append(SmlEscaper);
                        break;
                    case '.': 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append('.');
                        break;                        
                    case '/': 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append('/');
                        break;
                    case '#': 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append('a');
                        break;
                    case ':': 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append('b');
                        break;
                    case '?': 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append('c');
                        break;
                    case '@': 
                        escapedString.Append(SmlEscaper);
                        escapedString.Append('d');
                        break;
                    case '&':
                        escapedString.Append("&amp;");
                        break;
                    case '>':
                        escapedString.Append("&gt;");
                        break;
                    case '<':
                        escapedString.Append("&lt;");
                        break;
                    case '\'':
                        escapedString.Append("&apos;");
                        break;
                    case '"':
                        escapedString.Append("&quot;");
                        break;
                    default:
                        escapedString.Append(c);
                        break;
                }
            }

            return escapedString.ToString();
        }

        public static String SmlUnEscape(String escapedString)
        {
            if (String.IsNullOrEmpty(escapedString))
            {
                return null;
            }

            StringBuilder originalString = new StringBuilder();
            String tempString = null;
            for (int i = 0; i < escapedString.Length; i++)
            {
                if ((escapedString[i] == SmlEscaper) && ((i + 1) < escapedString.Length))
                {
                    //in case of an escape character, next character could be one of special chars
                    switch (escapedString[++i])
                    {
                        case 'a':
                            originalString.Append('#');
                            break;
                        case 'b':
                            originalString.Append(':');
                            break;
                        case 'c':
                            originalString.Append('?');
                            break;
                        case 'd':
                            originalString.Append('@');
                            break;
                        default:
                            originalString.Append(escapedString[i]);
                            break;
                    }
                }
                else
                {
                    if (escapedString[i] == '&')
                    {
                        tempString = escapedString.Substring(i + 1);
                        if (tempString.StartsWith("amp;", StringComparison.Ordinal))
                        {
                            originalString.Append('&');
                            i += "amp;".Length;
                        }
                        else if (tempString.StartsWith("gt;", StringComparison.Ordinal))
                        {
                            originalString.Append('>');
                            i += "gt;".Length;
                        }
                        else if (tempString.StartsWith("lt;", StringComparison.Ordinal))
                        {
                            originalString.Append('<');
                            i += "lt;".Length;
                        }
                        else if (tempString.StartsWith("apos;", StringComparison.Ordinal))
                        {
                            originalString.Append('\'');
                            i += "apos;".Length;
                        }
                        else if (tempString.StartsWith("amp;", StringComparison.Ordinal))
                        {
                            originalString.Append('"');
                            i += "amp;".Length;
                        }
                        else
                        {
                            //Depending on the caller of this function, & could have already been escaped
                            originalString.Append('&');
                        }
                    }
                    else
                    {
                        originalString.Append(escapedString[i]);
                    }
                }
            }

            return originalString.ToString();
        }


        public static String XmlEscape(String originalString)
        {
            StringBuilder escapedString = new StringBuilder();

            foreach (char c in originalString)
            {
                int numericalValue = Convert.ToInt32(c);

                //Below characters are specified as RESTRICTED characters by W3C XML comittee. 
                //So, we need to escape them as Processing Instructions - <?char **?> where ** is ascii value
                //Refer to http://www.w3.org/TR/2006/REC-xml11-20060816/#NT-Char for further details
                
                //NOTE: \r is added explicitly for storing CRLF
                if (((numericalValue >=   1) && (numericalValue <=   8)) ||
                    ((numericalValue >=  11) && (numericalValue <=  31)) ||
                    ((numericalValue >= 127) && (numericalValue <= 132)) ||
                    ((numericalValue >= 134) && (numericalValue <= 159))
                    )
                {
                    escapedString.Append("<?char " + numericalValue + "?>");
                }
                else
                {
                    escapedString.Append(c);
                }
            }

            return escapedString.ToString();
        }

        public static String XmlUnEscape(String escapedString)
        {
            StringBuilder originalString = new StringBuilder();

            //\r occurs more often than others. Special case to optimize the performance
            escapedString = escapedString.Replace("<?char 13?>", "\r");

            //Get all digits of the restricted character set
            MatchCollection theMatches = numberRegex.Matches(escapedString);
            List<int> numbers = new List<int>();
            foreach (Match theMatch in theMatches)
            {
                numbers.Add(Convert.ToInt32(
                    theMatch.Groups["number"].ToString()));
            }

            int i = 0;
            foreach (String str in stringRegex.Split(escapedString))
            {
                originalString.Append(str);
                if (i < numbers.Count)
                {
                    originalString.Append(Convert.ToChar(numbers[i++]));
                }
            }

            return originalString.ToString();
        }
    }
}
    
