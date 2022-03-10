// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Smo
{
    /// <summary>
    /// The partial definition of the DatabaseScopedConfiguration class.
    /// https://msdn.microsoft.com/library/mt629158.aspx
    /// </summary>
    public partial class DatabaseScopedConfiguration : NamedSmoObject
    {
        /// <summary>
        /// Constructs DatabaseScopedConfiguration object.
        /// </summary>
        /// <param name="parentColl">Parent collection.</param>
        /// <param name="key">Object key.</param>
        /// <param name="state">Object state.</param>
        internal DatabaseScopedConfiguration(AbstractCollectionBase parentColl, ObjectKeyBase key, SqlSmoState state)
            : base(parentColl, key, state)
        {
        }

        /// <summary>
        /// returns the name of the type in the urn expression
        /// </summary>
        public static string UrnSuffix
        {
            get
            {
                return "DatabaseScopedConfiguration";
            }
        }

        internal static string[] GetScriptFields(Type parentType,
            Common.ServerVersion version,
            Common.DatabaseEngineType databaseEngineType,
            Common.DatabaseEngineEdition databaseEngineEdition,
            bool defaultTextMode)
        {
            var fields = new string[] 
            {
                "IsValueDefault",
                "Value",
                "ValueForSecondary"
            };

            var list = GetSupportedScriptFields(typeof(DatabaseScopedConfiguration.PropertyMetadataProvider), fields, version, databaseEngineType, databaseEngineEdition);
            return list.ToArray();
        }
    }
}

