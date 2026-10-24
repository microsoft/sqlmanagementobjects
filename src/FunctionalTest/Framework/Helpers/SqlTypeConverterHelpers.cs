// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
    /// <summary>
    /// Helper methods dealing with conversion of types
    /// </summary>
    public static class SqlTypeConverterHelpers
    {
        public static object ConvertToType(Type t, object obj)
        {
            if (t == typeof (DataType))
            {
                return new DataType(DataType.SqlToEnum(obj.ToString()));
            }
            else if (t.IsEnum)
            {
                return obj is string ? Enum.Parse(t, (string)obj, true) : Enum.ToObject(t, obj);
            }
            else
            {
                return Convert.ChangeType(obj, t);
            }
        }
    }
}
