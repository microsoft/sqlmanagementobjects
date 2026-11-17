// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal partial class ExceptionTemplates
    {
        public static string IncludeHeader (string objectType, string name, string dateString) =>
            ExceptionTemplates.FormatIncludeHeaderTemplate(
                objectType, SmoUtility.EscapeSqlBlockComment(name), dateString);
    }

}
