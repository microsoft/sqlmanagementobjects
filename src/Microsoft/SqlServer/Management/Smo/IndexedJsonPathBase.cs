// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc;

using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class IndexedJsonPath : SqlSmoObject
    {
        internal IndexedJsonPath(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Constructor for creating a new IndexedJsonPath with the specified parent index and path.
        /// </summary>
        /// <param name="parent">The parent Index object</param>
        /// <param name="path">The JSON path (e.g., "$.customer.name")</param>
        public IndexedJsonPath(Index parent, string path)
            : base()
        {
            this.key = new IndexedJsonPathObjectKey(path);
            this.SetParentImpl(parent);
            this.Path = path;
        }

        /// <summary>
        /// Returns the value of urn expresion.
        /// </summary>
        public static string UrnSuffix
        {
            get 
            {
                return nameof(IndexedJsonPath);
            }
        }

        internal static string[] GetScriptFields(Type parentType,
                                  Microsoft.SqlServer.Management.Common.ServerVersion version,
                                  Cmn.DatabaseEngineType databaseEngineType,
                                  Cmn.DatabaseEngineEdition databaseEngineEdition,
                                  bool defaultTextMode)
        {
            string[] fields =
            {
                nameof(Path),
            };

            List<string> list = GetSupportedScriptFields(typeof(IndexedJsonPath.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }  
}
