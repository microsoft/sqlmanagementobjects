// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;

using System.Collections.Specialized;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Dmf = Microsoft.SqlServer.Management.Dmf;
using Facets = Microsoft.SqlServer.Management.Facets;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class IndexedXmlPathNamespace : NamedSmoObject
    {
        internal IndexedXmlPathNamespace(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Returns the value of urn expresion.
        /// </summary>
        public static string UrnSuffix
        {
            get 
            {
                return "IndexedXmlPathNamespace";
            }
        }

        internal static string[] GetScriptFields(Type parentType,
                                  Microsoft.SqlServer.Management.Common.ServerVersion version,
                                  DatabaseEngineType databaseEngineType,
                                  DatabaseEngineEdition databaseEngineEdition,
                                  bool defaultTextMode)
        {
            string[] fields =
            {
                "IsDefaultUri",
            };

            List<string> list = GetSupportedScriptFields(typeof(IndexedXmlPathNamespace.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }  
}
