// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using Cmn = Microsoft.SqlServer.Management.Common;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// Represents an EdgeConstraintClause object. Objects of EdgeConstraintClause types
    /// are encapsulated within an EdgeConstraint and represent allowed connections between two graph nodes.
    /// </summary>
    public partial class EdgeConstraintClause : ScriptNameObjectBase
    {
        // Constant string for EdgeConstraintClause URN.
        //
        private const string EdgeConstraintClause_URN = "EdgeConstraintClause";

        /// <summary>
        /// Constructor to instantiate an EdgeConstraintClause object. EdgeConstraintClause(s) are non scriptable entities that
        /// are encapsulated by EdgeConstraint objects. They represent allowed connections between graph nodes in a schema.
        /// </summary>
        /// <param name="parentColl"> Parent object that encapsulates this object (EdgeConstraint in this case) </param>
        /// <param name="key">key that identifies the object in collection</param>
        /// <param name="state">current state of the object represented by enumerator <see cref="SqlSmoState"/></param>
        internal EdgeConstraintClause(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state) :
            base(parentColl, key, state)
        {
        }

        /// <summary>
        /// Property accessor for URN suffix needed to traverse to this object in the explorer.
        /// </summary>
        /// <returns>name of the type in the urn expression</returns>
        public static string UrnSuffix
        {
            get
            {
                return EdgeConstraintClause_URN;
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
        /// <returns>Properties available during scripting of the object</returns>
        internal static string[] GetScriptFields(Type parentType,
                                    Microsoft.SqlServer.Management.Common.ServerVersion version,
                                    Cmn.DatabaseEngineType databaseEngineType,
                                    Cmn.DatabaseEngineEdition databaseEngineEdition,
                                    bool defaultTextMode)
        {
            string[] fields = {
                                        "From",
                                        "To"
                              };
            List<string> list = GetSupportedScriptFields(typeof(EdgeConstraintClause.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}