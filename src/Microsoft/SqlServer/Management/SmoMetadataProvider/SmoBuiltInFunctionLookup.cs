// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class SmoBuiltInFunctionLookup : BuiltInFunctionLookupBase
    {
        /// <summary>
        /// Gets singleton instance of the <see cref="SmoBuiltInFunctionLookup"/> class.
        /// </summary>
        public static SmoBuiltInFunctionLookup Instance
        {
            get { return Singleton.Instance; }
        }

        static private class Singleton
        {
            public static SmoBuiltInFunctionLookup Instance = new SmoBuiltInFunctionLookup();

            // static constructor to suppress beforefieldinit attribute
            static Singleton()
            {
            }
        }

        // Singleton Instance
        private SmoBuiltInFunctionLookup()
        {
        }
    }
}
