// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.SqlServer.Management.Common
{
    public static class NetCoreHelpers
    {

        public static Assembly GetAssembly(this Type type)
        {
            return System.Reflection.Assembly.GetAssembly(type);
        }

        public static Assembly LoadAssembly(string assemblyName)
        {
            return System.Reflection.Assembly.Load(assemblyName);
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

        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static Assembly Assembly(this Type type)
        {
            return type.Assembly;
        }
    }
}