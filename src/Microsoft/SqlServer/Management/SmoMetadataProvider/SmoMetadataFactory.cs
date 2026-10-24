// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.SqlParser.MetadataProvider;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    class SmoMetadataFactory : MetadataFactory
    {
        /// <summary>
        /// Gets singleton instance of the <see cref="SmoMetadataFactory"/> class.
        /// </summary>
        public static SmoMetadataFactory Instance
        {
            get { return Singleton.Instance; }
        }

        static private class Singleton
        {
            public static SmoMetadataFactory Instance = new SmoMetadataFactory();

            // static constructor to suppress beforefieldinit attribute
            static Singleton()
            {
            }
        }

        // Singleton Instance
        private SmoMetadataFactory()
        {
            // Individual metadata factory objects (i.e. Column, Parameter...) could be
            // overwritten here via the protected setter of those properties. See base
            // class MetadataProvider.MetadataFactoryBase for details.
        }
    }
}
