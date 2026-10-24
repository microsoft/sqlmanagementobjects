// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Reflection;

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    internal static class NetCoreExtension
    {

        public static bool IsPrimitive(this Type type)
        {
            return type.IsPrimitive;
        }

        public static bool IsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static bool IsAssignableFrom(this Type type, Type c)
        {
            return type.IsAssignableFrom(c);
        }

        public static Assembly Assembly(this Type type)
        {
            return type.Assembly;
        }

        public static Type GetInterface(this Type type, string name)
        {
            return type.GetInterface(name);
        }

        public static PropertyInfo[] GetProperties(this Type type)
        {
            return type.GetProperties();
        }

        // String.Copy is obsolete in NetCore3. Since we don't know exactly how this API is used,
        // we will preserve the original semantics of creating a new string instance with
        // copy of the contents of the original string.
        public static string Copy(this string value)
        {
            return new string(value.ToCharArray());
        }

        public static int Compare(this string strA, string strB, bool ignoreCase, CultureInfo culture)
        {
            return culture.CompareInfo.Compare(strA, strB, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type type)
        {
            return Delegate.CreateDelegate(type, methodInfo);
        }

        public static Assembly GetAssembly(this Type type)
        {
            return System.Reflection.Assembly.GetAssembly(type);
        }
    }
}
