// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Data;
using System.Globalization;
using System.Text;
using Microsoft.SqlServer.Management.Sdk.Sfc;
using Microsoft.SqlServer.Management.Sdk.Sfc.Metadata;
using Cmn = Microsoft.SqlServer.Management.Common;


namespace Microsoft.SqlServer.Management.Smo
{
    public partial class ExternalLibraryFile : ScriptNameObjectBase
    {
        internal ExternalLibraryFile(ExternalLibrary parent, ObjectKeyBase key, SqlSmoState state) : 
        base(key, state)
        {
            singletonParent = parent as ExternalLibrary;

            // The state of this object reflects the state of its parent
            this.SetState(parent.State);

            // When the parent state changes, we want to be notified so we can
            // update the state of this object as well.
            // Note: it does not seem to be possible to assign a new parent to an existing
            //       object of this time, so we are not concerned about being notified
            //       about property changes of an stale parent.
            parent.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
                if (sender is ExternalLibrary && e.PropertyName == "State")
                {
                    this.SetState(parent.State);
                }
            };
        }

        /// <summary>
        /// The parent external library owning this file.
        /// </summary>
        [SfcObject(SfcObjectRelationship.ParentObject)]
        public ExternalLibrary Parent
        {
            get 
            {
                CheckObjectState();
                return singletonParent as ExternalLibrary;
            }
        }

        /// <summary>
        /// Returns the name of the type in the urn expression.
        /// </summary>
        public static string UrnSuffix
        {
            get 
            {
                return "ExternalLibraryFile";
            }
        }

        /// <summary>
        /// Returns bytes of library's file.
        /// </summary>
        public System.Byte[] GetFileBytes()
        {
            Request req = new Request(this.Urn, new string[] { "Content" });
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
        /// Returns bytes of library's file in hex ('0x...') representation.
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
        /// Gets the Urn recursively.
        /// </summary>
        protected override sealed void GetUrnRecursive(StringBuilder urnbuilder, UrnIdOption idOption)
        {
            Parent.GetUrnRecImpl(urnbuilder, idOption);
            urnbuilder.AppendFormat(CultureInfo.InvariantCulture, "/{0}", UrnSuffix);
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
            // DEVNOTE: [MatteoT, 6/13/2019] - this code is questionable for at least 3 reasons:
            //          1) "Name" is redundant, in that it should be added automatically by the "SMO framework" (see 
            //              my comment in the ExternalLibraryBase.cs)
            //          2) There should be a "Content" field instead
            //          3) This object can't be scripted by itself, so... there are really no fields needed to script it.
            // Assuming this code is actually used anywhere, it should be updated.
            return new string[] {
                                    "Name",
                                    "ParentID",
                                    "Platform"
                                };
        }
    }
}
