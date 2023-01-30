// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    public partial class SqlAssemblyFile : ScriptNameObjectBase
    {
        internal SqlAssemblyFile(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        // Returns the name of the type in the urn expression
        public static string UrnSuffix
        {
            get 
            {
                return "SqlAssemblyFile";
            }
        }

        // Returns bytes of assembly's file.
        public System.Byte[] GetFileBytes()
        {
            Request req = new Request( this.Urn, new string[] {"FileBytes"});
            DataTable results = this.ExecutionManager.GetEnumeratorData(req);
            
            if( results.Rows.Count > 0 )
            {
                return (byte[])results.Rows[0][0];
            }
            else
            {
                return new byte[0];
            }
        }
        
        // Returns bytes of assembly's file in hex ('0x...') representation.
        public string GetFileText()
        {    
            byte[] fileBytes = GetFileBytes();
            StringBuilder results = new StringBuilder( fileBytes.Length   + 2);
            results.Append("0x");
            foreach( byte b in fileBytes )
            {
                results.Append(b.ToString("X2", SmoApplication.DefaultCulture));
            }

            return results.ToString();
        }

        /// <summary>
        /// Returns the fields that will be needed to script this object.
        /// </summary>
        /// <param name="parentType">The type of the parent object</param>
        /// <param name="version">The version of the server</param>
        /// <param name="databaseEngineType">DatabaseEngineType of the server</param>
        /// <param name="databaseEngineEdition">The database engine edition of the server</param>
        /// <param name="defaultTextMode">indicates the text mode of the server. 
        /// If true this means only header and body are needed, otherwise all properties</param>
        /// <returns></returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Microsoft.SqlServer.Management.Common.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[] {
                                    "ID"
                                };
        }
    }
}
