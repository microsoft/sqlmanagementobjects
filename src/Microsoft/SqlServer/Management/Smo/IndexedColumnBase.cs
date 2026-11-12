// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class IndexedColumn : NamedSmoObject
	{
        internal IndexedColumn(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
			base(parentColl, key, state)
		{
		}

		public IndexedColumn(Index index, string name, bool descending) : base()
		{
			ValidateName(name);
			this.key = new SimpleObjectKey(name);
			this.Parent = index;
			this.Descending = descending;
		}

		// returns the name of the type in the urn expression
		public static string UrnSuffix
		{
			get 
			{
				return "IndexedColumn";
			}
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

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">The database engine type of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType, 
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    DatabaseEngineType databaseEngineType, 
                                    DatabaseEngineEdition databaseEngineEdition,
                                bool defaultTextMode)
        {
           string[] fields = {   
                         "IsIncluded",
                        "Descending",
                        "IsComputed" ,
                        "ColumnStoreOrderOrdinal"};
            List<string> list = GetSupportedScriptFields(typeof(IndexedColumn.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}