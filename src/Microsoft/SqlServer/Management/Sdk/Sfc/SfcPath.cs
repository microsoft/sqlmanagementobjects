// Copyright (c) Microsoft.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;


namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    public class SfcSqlPathUtilities
    {
        /// <summary>
        /// Converts a URN to a path
        /// </summary>
        /// <param name="urn"></param>
        /// <returns></returns>
        public static string ConvertUrnToPath(Urn urn)
        {
            if (urn == null)
            {
                throw new ArgumentNullException("urn");
            }

            if (urn.ToString().Length == 0)
            {
                throw new ArgumentException(SfcStrings.InvalidUrn, "urn");
            }

            string[] levels = urn.XPathExpression.ExpressionSkeleton.Split(new char[] { '/' });

            StringBuilder sb = new StringBuilder(255);
            SfcDomainInfo foundDomain = null;

            const string PathSeparator = "\\";

            // See which domain implements this root
            foreach (SfcDomainInfo domain in SfcRegistration.Domains)
            {
                if (domain.Name.Equals(levels[0], StringComparison.Ordinal))
                {
                    foundDomain = domain;
                    break;
                }
            }
            if (foundDomain == null)
            {
                throw new SfcPathConversionException(SfcStrings.UnknownDomain(levels[0]));
            }

            sb.Append(foundDomain.PSDriveName);
            sb.Append(@"\");
            string name = urn.GetAttribute("Name", levels[0]);
            sb.Append(name);
            if (!name.Contains(PathSeparator))
            {
                sb.Append(PathSeparator);
                sb.Append("DEFAULT");
            }

            // Get the metadata for the root
            SfcMetadataDiscovery root = new SfcMetadataDiscovery(foundDomain.RootType);

            List<SfcMetadataRelation> relations = root.Objects;
            SfcMetadataRelation currentRelation;

            // Process all levels
            for (int level = 1; level < levels.Length; level++)
            {
                currentRelation = null;

                // Search for the level in metadata
                foreach (SfcMetadataRelation rel in relations)
                {
                    if (levels[level].Equals(rel.ElementTypeName, StringComparison.InvariantCulture))
                    {
                        currentRelation = rel;
                        break;
                    }
                }

                if (currentRelation == null)
                {
                    throw new SfcPathConversionException(SfcStrings.LevelNotFound(levels[level], urn.ToString()));
                }

                // If it is a container, we need to print the container as well
                if (currentRelation.Relationship == SfcRelationship.ChildContainer ||
                    currentRelation.Relationship == SfcRelationship.ObjectContainer ||
                    currentRelation.Relationship == SfcRelationship.ChildObject ||
                    currentRelation.Relationship == SfcRelationship.Object)
                {
                    sb.Append(PathSeparator);
                    sb.Append(currentRelation.PropertyName);
                }


                // Look in metadata, and see if there are keys defined. Then read all the key values
                // from the URN and construct a PowerShell key element.
                if (currentRelation.ReadOnlyKeys.Count > 0)
                {
                    // Now get the keys from the attributes
                    bool first = true;
                    int keysFound = 0;
                    StringBuilder keyBuilder = new StringBuilder();
                    keyBuilder.Append(PathSeparator);
                    foreach (SfcMetadataRelation key in currentRelation.ReadOnlyKeys)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            keyBuilder.Append("."); // Change back to SfcPath.KeySeparator
                        }
                        string keyValue = urn.GetAttribute(key.PropertyName, levels[level]);

                        if (String.IsNullOrEmpty(keyValue))
                        {
                            if (level < levels.Length - 1)
                            {
                                // The key can only be be undefined at the last level
                                throw new SfcPathConversionException(SfcStrings.InvalidKeyValue(key.PropertyName, urn.ToString()));
                            }
                        }
                        else
                        {
                            // Key found, add to key string
                            keyBuilder.Append(SfcSqlPathUtilities.EncodeSqlName(Urn.UnEscapeString(keyValue)));
                            keysFound++;
                        }
                    }

                    // Check if we have all keys (or none) for the last level
                    if (level == levels.Length - 1)
                    {
                        if (keysFound != 0)
                        {
                            if (keysFound != currentRelation.ReadOnlyKeys.Count)
                            {
                                // Keys are missing
                                throw new SfcPathConversionException(SfcStrings.MissingKeys(urn.ToString(), levels[level]));
                            }
                            sb.Append(keyBuilder.ToString());
                        }
                    }
                    else
                    {
                        sb.Append(keyBuilder.ToString());
                    }
                }

                relations = currentRelation.Relations;
            }
            return sb.ToString();
        }


        /// <summary>
        /// Encodes the string into a URL encoded string.
        /// Note that we encode ':'. This is a known bug in PowerShell 1.0 that you cannot cd into
        /// a location that has a ':'.
        /// We need to receive the '\' escaped as we do not know whether it is a path separator or a part
        /// of a name (which is a legal id character in SQL).
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string EncodeSqlName(string name)
        {
            StringBuilder result = new StringBuilder(255);

            const string m_needEncoding = @"\/:%<>*?[]|";

            for (int index = 0; index < name.Length; ++index)
            {
                // We need to escape '\' and '/' as we do not see the difference with a path separator
                // The ':' -- see the text above
                // The % needs to be escaped as UrlDecode uses this character to encode characters into hex code
                // < and > cannot be escaped in PowerShell.
                // '*?[]|' have special meaning as path characters.
                // The '.' is used to separate schema and name, so we need to escape any '.' that
                // is part of the name.
                // Control characters can appear in names. Escape these as well.
                if (Char.IsControl(name[index]) || name[index] == '.' || m_needEncoding.Contains(name[index].ToString()))
                {
                    result.Append('%');
                    result.Append(String.Format("{0:X2}", Convert.ToByte(name[index])));
                }
                else
                {
                    result.Append(name[index]);
                }
            }
            return result.ToString();
        } // SfcEncodeSqlName

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string DecodeSqlName(string name)
        {
            StringBuilder result = new StringBuilder();

            for (int index = 0; index < name.Length; ++index)
            {
                if (name[index] == '%')
                {
                    if (index + 2 < name.Length)
                    {
                        // string hex = name[index + 1].ToString() + name[index + 2].ToString();
                        string hex = name.Substring(index + 1, 2);
                        result.Append(Convert.ToChar(Convert.ToInt32(hex, 16)));
                        index += 2;
                        continue;
                    }
                }
                result.Append(name[index]);
            }
            return result.ToString();
        } // DecodeSqlName

        /// <summary>
        /// Decodes an array of names.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public static string[] DecodeSqlName(string[] names)
        {
            string[] ret = new string[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                ret[i] = DecodeSqlName(names[i]);
            }
            return ret;
        }
    }
}
