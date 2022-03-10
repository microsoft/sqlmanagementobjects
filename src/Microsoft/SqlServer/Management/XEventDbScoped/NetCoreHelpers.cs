// Copyright (c) Microsoft.
// Licensed under the MIT license.

//Helper Class to decide appropriate methods to call between .net and .netcore frameworks
using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;


namespace Microsoft.SqlServer.Management.XEventDbScoped
{
    internal static class NetCoreHelpers
    {
        public static string UriSchemaHttps = "Https";
        public static string UriSchemaHttp = "Http";

        public static Assembly GetAssembly(this Type type)
        {
            return Assembly.GetAssembly(type);
        }

        public static Assembly LoadAssembly(string assemblyName)
        {
            return Assembly.Load(assemblyName);
        }

        public static IntPtr ConvertSecureStringToBSTR(SecureString ss)
        {
            return Marshal.SecureStringToBSTR(ss);
        }


        public static int StringCompare(this String firstString, String secondString, bool ignoreCase, CultureInfo culture)
        {
            return string.Compare( firstString, secondString , ignoreCase, culture);
        }

        public static string StringToUpper(this String str, CultureInfo culture )
        {
            return str.ToUpper(culture);
        }

        public static CultureInfo GetNewCultureInfo(int lcid)
        {
            return new CultureInfo(lcid);
        }

        public static void ZeroFreeBSTR(IntPtr ps)
        {
            Marshal.ZeroFreeBSTR(ps);
        }
    }
}