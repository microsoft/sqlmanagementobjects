// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class SmoCollationLookup : CollationLookupBase
    {
        /// <summary>
        /// Gets singleton instance of the <see cref="SmoCollationLookup"/> class.
        /// </summary>
        public static SmoCollationLookup Instance
        {
            get { return Singleton.Instance; }
        }

        static private class Singleton
        {
            public static SmoCollationLookup Instance = new SmoCollationLookup();

            // static constructor to suppress beforefieldinit attribute
            static Singleton()
            {
            }
        }

        // Singleton Instance
        private SmoCollationLookup()
        {
        }
    }
}
