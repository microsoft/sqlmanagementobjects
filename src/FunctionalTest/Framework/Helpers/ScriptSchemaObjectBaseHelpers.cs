// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Provides helper functionality for SMO ScriptSchemaObjectBase objects.
    /// </summary>
    public static class ScriptSchemaObjectBaseHelpers
    {
        /// <summary>
        /// Returns the schema-qualified name of the object without brackets. Use
        /// FullQualifiedName to get the schema-qualified name with brackets. 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetSchemaQualifiedNameNoBrackets(this ScriptSchemaObjectBase obj)
        {
            return string.Format("{0}.{1}", obj.Schema, obj.Name, CultureInfo.InvariantCulture);
        }
    }
}