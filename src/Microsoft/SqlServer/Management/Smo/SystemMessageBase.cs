// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class SystemMessage : MessageObjectBase
    {
        /// <summary>
        /// internal .ctor
        /// </summary>
        /// <param name="parentColl"></param>
        /// <param name="key"></param>
        /// <param name="state"></param>
        internal SystemMessage(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "SystemMessage";
            }
        }

        /// <summary>
        /// Message ID
        /// </summary>
        [SfcKey(1)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public Int32 ID
        {
            get
            {
                return ((MessageObjectKey)key).ID;
            }
        }

        /// <summary>
        /// Language
        /// </summary>
        [SfcKey(0)]
        [SfcProperty(SfcPropertyFlags.Standalone)]
        public string Language
        {
            get
            {
                return ((MessageObjectKey)key).Language;
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
                                            "ID",
                                        "Language"};
            List<string> list = GetSupportedScriptFields(typeof(SystemMessage.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}


