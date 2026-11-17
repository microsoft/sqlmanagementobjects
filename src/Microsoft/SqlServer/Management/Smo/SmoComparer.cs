// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;


namespace Microsoft.SqlServer.Management.Smo
{
    internal class StringComparer : IComparer, IComparer<string>, IEqualityComparer<string>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="collation">SQL Server collation name</param>
        /// <param name="lcid"></param>
        internal StringComparer( string collation, int lcid)
        {
            ChangeCollation(collation, lcid);
        }

        /// <summary>
        /// Change the comparer for the new collation
        /// </summary>
        /// <param name="newCollation"></param>
        /// <param name="lcid"></param>
        internal void ChangeCollation(string newCollation, int lcid)
        {
            // the servers collections will be built with an empty string for collation name
            // and it is case insensitive
            m_options = CompareOptions.None;
            if( newCollation.Length == 0)
            {
                m_options |= CompareOptions.IgnoreCase;
            }
            else
            {
                // split collation name into tokens, so that we can parse the 
                // different sorting options
                string[] tokens = newCollation.Split(new char[] {'_'});
                if( null != tokens )
                {
                    bool CS = true;
                    bool AS = true;
                    bool KS = false;
                    bool WS = false;
                    bool BIN = false;
                    foreach(string s in tokens)
                    {
                        switch(s)
                        {
                            case "CI" : 
                                CS = false;
                                break;
                            case "AI" : 
                                AS = false;
                                break;
                            case "KS" : 
                                KS = true;
                                break;
                            case "WS" : 
                                WS = true;
                                break;
                            case "BIN" : 
                            case "BIN2":
                                BIN = true;
                                break;
                        }
                    }

                    if( BIN )
                    {
                        // this is binary sorting
                        m_options = CompareOptions.Ordinal;
                    }
                    else
                    {
                        // get all other sorting options
                        if( !CS )
                        {
                            m_options |= CompareOptions.IgnoreCase;
                        }

                        if ( !AS )
                        {
                            m_options |= CompareOptions.IgnoreNonSpace;
                        }

                        if ( !KS )
                        {
                            m_options |= CompareOptions.IgnoreKanaType;
                        }

                        if ( !WS )
                        {
                            m_options |= CompareOptions.IgnoreWidth;
                        }
                    }
                }
            }

            // go with invariant culture for the moment
            // which is English language
            m_cultureInfo = new CultureInfo(lcid);
        }

        /// <summary>
        /// The IComparer implementation
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(  object x,  object y)
        {
            if( null == x && null == y )
            {
                return 0;
            }
            else if ( null != x && null == y ) 
            {
                return 1;
            }
            else if( null == x && null != y ) 
            {
                return -1;
            }
            else
            {
                return m_cultureInfo.CompareInfo.Compare((string)x, (string)y, m_options);
            }
        }

        // private members
        CultureInfo m_cultureInfo;
        CompareOptions m_options;

        internal CultureInfo CultureInfo
        {
            get
            {
                return m_cultureInfo;
            }
        }

        internal CompareOptions CompareOptions 
        {
            get
            {
                return m_options;
            }
            
        }


        public int Compare(string x, string y)
        {
            return m_cultureInfo.CompareInfo.Compare(x, y, m_options);
        }

        public bool Equals(string x, string y)
        {
            return (Compare(x, y) == 0);
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}

