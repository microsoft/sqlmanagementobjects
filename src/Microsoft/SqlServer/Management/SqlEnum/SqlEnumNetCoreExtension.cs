// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Globalization;
using System.Reflection;

namespace Microsoft.SqlServer.Management.Smo.SqlEnum
{
    internal static class SqlEnumNetCoreExtension
    {
        public static bool IsUriSchemeHttps(this Uri uri)
        {
            return uri.Scheme == Uri.UriSchemeHttps;
        }

        public static bool GetIsPrimitive(this Type type)
        {
            return type.IsPrimitive;
        }

        public static bool GetIsEnum(this Type type)
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


        public static object CreateInstance(Assembly assembly, string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder binder, object[] args, CultureInfo culture, object[] activationAttributes)
        {
            return assembly.CreateInstance(typeName, ignoreCase, bindingAttr, binder, args, culture, activationAttributes);
        }


        public static object[] GetCustomAttributes(Assembly element, bool inherit)
        {
            return element.GetCustomAttributes(inherit);
        }

        public static object[] GetCustomAttributes(Type element, Type attributeType, bool inherit)
        {
            return element.GetCustomAttributes(attributeType, inherit);
        }

        public static object[] GetCustomAttributes(MemberInfo element, Type attributeType, bool inherit)
        {
            return element.GetCustomAttributes(attributeType, inherit);
        }

        public static object[] GetCustomAttributes(MemberInfo element, bool inherit)
        {
            return element.GetCustomAttributes(inherit);
        }



        public static object[] GetCustomAttributes(Type element, bool inherit)
        {
            return element.GetCustomAttributes(inherit);
        }

        public static int Compare(this string strA, string strB, bool ignoreCase, CultureInfo culture)
        {
            return string.Compare(strA, strA, ignoreCase, culture);
        }

        public static Delegate CreateDelegate(this MethodInfo methodInfo, Type type)
        {
            return Delegate.CreateDelegate(type, methodInfo);
        }

        public static Assembly GetAssembly(this Type type)
        {
            return System.Reflection.Assembly.GetAssembly(type);
        }

        public static MemberInfo[] GetMember(this Type type, string name)
        {
            return type.GetMember(name);
        }
    }
}
