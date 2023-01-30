// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Test.Manageability.Utils.Helpers
{
#if NETSTANDARD2_0
    public static class DataExtensions
    {

        //
        // Summary:
        //     Provides strongly-typed access to each of the column values in the specified
        //     row. The System.Data.DataRowExtensions.Field``1(System.Data.DataRow,System.String)
        //     method also supports nullable types.
        //
        // Parameters:
        //   row:
        //     The input System.Data.DataRow, which acts as the this instance for the extension
        //     method.
        //
        //   columnName:
        //     The name of the column to return the value of.
        //
        // Type parameters:
        //   T:
        //     A generic parameter that specifies the return type of the column.
        //
        // Returns:
        //     The value, of type T, of the System.Data.DataColumn specified by columnName.
        //
        // Exceptions:
        //   T:System.InvalidCastException:
        //     The value type of the underlying column could not be cast to the type specified
        //     by the generic parameter, T.
        //
        //   T:System.IndexOutOfRangeException:
        //     The column specified by columnName does not occur in the System.Data.DataTable
        //     that the System.Data.DataRow is a part of.
        //
        //   T:System.NullReferenceException:
        //     A null value was assigned to a non-nullable type.
        public static T Field<T>(this System.Data.DataRow row, string columnName)
        {
            return (T)row[columnName];
        }
    }
#endif
}
