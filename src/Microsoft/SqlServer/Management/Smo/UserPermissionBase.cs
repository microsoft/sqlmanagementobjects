// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Collections.Generic;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;

using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    [SfcElementType("Permission")]
    internal partial class UserPermission : NamedSmoObject
    {
        internal UserPermission(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            :
            base(parentColl, key, state)
        {
        }

        internal UserPermission()
            :
            base()
        {
        }

        [SfcObject(SfcObjectRelationship.ParentObject)]
        internal SqlSmoObject Parent
        {
            get
            {
                CheckObjectState();
                return base.ParentColl.ParentInstance as SqlSmoObject;
            }
            set { SetParentImpl(value); }
        }
        // returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get
            {
                return "Permission";
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
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {   
                        "Code",
                        "Grantee",
                        "GranteeType",
                        "Grantor",
                        "GrantorType",
                        "ObjectClass",
                        "PermissionState"};
            List<string> list = GetSupportedScriptFields(typeof(UserPermission.PropertyMetadataProvider),fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }


        /// <summary>
        /// Customizes the initialization mechanism to avoid requesting ColumnName.
        /// The enumerator query would have brought this in otherwise.
        /// </summary>
        /// <returns></returns>
        internal override string[] GetRejectFields()
        {
            return new string[] { "Urn", "ColumnName" };
        }
    }
}
