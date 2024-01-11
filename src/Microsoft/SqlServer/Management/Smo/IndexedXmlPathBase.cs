// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;

using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;
using Dmf = Microsoft.SqlServer.Management.Dmf;
using Facets = Microsoft.SqlServer.Management.Facets;

namespace Microsoft.SqlServer.Management.Smo
{
    public partial class IndexedXmlPath : NamedSmoObject, Cmn.IMarkForDrop
    {
        internal IndexedXmlPath(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.ReadOnlyAfterCreation | SfcPropertyFlags.Design | SfcPropertyFlags.Deploy | SfcPropertyFlags.SqlAzureDatabase | SfcPropertyFlags.Standalone)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        private DataType dataType = null;
        /// <summary>
        /// Datatype of SQL values in path item.
        /// </summary>
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public DataType DataType
        {
            get
            {
                return GetDataType(ref dataType);
            }
            set
            {
                SetDataType(ref dataType, value);
            }
        }

        public void MarkForDrop(bool dropOnAlter)
        {
            base.MarkForDropImpl(dropOnAlter);
        }

        /// <summary>
        /// Returns the value of urn expresion.
        /// </summary>
        public static string UrnSuffix
        {
            get 
            {
                return "IndexedXmlPath";
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
                "Path",
            };

            List<string> list = GetSupportedScriptFields(typeof(IndexedXmlPath.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }  
}
