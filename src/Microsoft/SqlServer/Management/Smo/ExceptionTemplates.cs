// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    internal class ExceptionTemplates : ExceptionTemplatesImpl
    {
        new public static string IncludeHeader (string objectType, string name, string dateString)
        {
            string safeName;
            if( name.Contains("*/") )
            {
                // Avoid SQL injection: do not pass names containing closing comment '*/' to the header
                safeName = "?";
            }
            else if( name.Contains("/*") )
            {
                // This is not as bad, but will also break the script, so don't let it in the comment
                safeName = "?";
            }
            else
            {
                safeName = name;
            }

            return ExceptionTemplatesImpl.IncludeHeader( objectType, safeName, dateString );
        }
    }

}
