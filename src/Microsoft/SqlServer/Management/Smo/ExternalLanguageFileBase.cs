// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExternalLanguageFile : ScriptNameObjectBase
    {
        internal ExternalLanguageFile(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Returns the name of the type in the urn expression.
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return nameof(ExternalLanguageFile);
            }
        }

        /// <summary>
        /// Returns bytes of languages's file.
        /// </summary>
        public System.Byte[] GetFileBytes()
        {
            Request req = new Request(this.Urn, new string[] { nameof(ContentFromBinary) });
            DataTable results = this.ExecutionManager.GetEnumeratorData(req);

            if (results.Rows.Count > 0)
            {
                return (byte[])results.Rows[0][0];
            }
            else
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// Returns name of the language file.
        /// </summary>
        public string GetFileName()
        {
            Request req = new Request(this.Urn, new string[] { nameof(FileName) });
            DataTable results = this.ExecutionManager.GetEnumeratorData(req);

            if (results.Rows.Count > 0)
            {
                return results.Rows[0][0].ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns bytes of language's file in hex ('0x...') representation.
        /// </summary>
        public string GetFileText()
        {
            byte[] fileBytes = GetFileBytes();
            StringBuilder results = new StringBuilder(fileBytes.Length + 2);
            results.Append("0x");
            foreach (byte b in fileBytes)
            {
                // Format the string as two uppercase hexadecimal characters.
                results.Append(b.ToString("X2", CultureInfo.InvariantCulture));
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
        /// <returns>The fields that are needed to script this object</returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Microsoft.SqlServer.Management.Common.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            return new string[] {
                nameof(ContentFromFile),
                nameof(ContentFromBinary),
                nameof(FileName),
                nameof(Parameters),
                nameof(Platform),
                nameof(EnvironmentVariables)
            };
        }
    }
}
