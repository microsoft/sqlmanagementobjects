// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.Smo
{
    using System.Globalization;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Contains assorted helper functions
    /// </summary>
    [ComVisible(false)]
    public class SqlSupport
    {
        ///	<summary>
        ///default constructor</summary>
        private SqlSupport()
        {
        }

        
       
        /// <summary>
        /// Translates the collation characteristics into CompareOptions
        /// </summary>
        /// <param name="collation">A SQL Server collation identifier string</param>
        /// <returns></returns>
        public static CompareOptions GetCompareOptionsFromCollation(string collation)
        {
            // the servers collections will be built with an empty string for collation name
            // and it is case insensitive
            CompareOptions options = CompareOptions.None;
            if( collation.Length == 0)
            {
                options |= CompareOptions.IgnoreCase;
            }
            else
            {
                // split collation name into tokens, so that we can parse the 
                // different sorting options
                string[] tokens = collation.Split(new char[] {'_'});
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
                        options = CompareOptions.Ordinal;
                    }
                    else
                    {
                        // get all other sorting options
                        if( !CS )
                        {
                            options |= CompareOptions.IgnoreCase;
                        }

                        if ( !AS )
                        {
                            options |= CompareOptions.IgnoreNonSpace;
                        }

                        if ( !KS )
                        {
                            options |= CompareOptions.IgnoreKanaType;
                        }

                        if ( !WS )
                        {
                            options |= CompareOptions.IgnoreWidth;
                        }
                    }
                }
            }
            return options;
        }
    }
}
