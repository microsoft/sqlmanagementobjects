// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading;
#if !NETSTANDARD2_0 && !NETCOREAPP
using Microsoft.SqlServer.Smo.UnSafeInternals;
#endif

namespace Microsoft.SqlServer.Management.Sdk.Sfc
{
    /// <summary>
    /// Any security-sensitive operations needed by SMO (reflection particularly) should route through this class
    /// When running in SqlClr these operations need to be run by methods that properly assert permissions
    /// </summary>
    internal static class SmoManagementUtil
    {
        internal static void EnterMonitor(object lockObject)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            ManagementUtil.EnterMonitor(lockObject);
#else
            Monitor.Enter(lockObject);
#endif
        }

        internal static void ExitMonitor(object lockObject)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            ManagementUtil.ExitMonitor(lockObject);
#else
            Monitor.Exit(lockObject);
#endif
        }

        internal static Object CreateInstance(Assembly assembly, string objectType)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            return ManagementUtil.CreateInstance(assembly, objectType);
#else
            return assembly.CreateInstance(objectType,
                        false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance,
                        null, null, System.Globalization.CultureInfo.InvariantCulture, null);
#endif
        }

        internal static Assembly LoadAssembly(String assemblyName)
        {
            return Assembly.Load(assemblyName);
        }

        internal static Assembly LoadAssemblyFromFile(String fileName)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            return ManagementUtil.LoadAssemblyFromFile(fileName);
#else
            return Assembly.LoadFile(fileName);
#endif
        }

        internal static Stream LoadResourceFromAssembly(Assembly assembly, String resourceFileName)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            return ManagementUtil.LoadResourceFromAssembly(assembly, resourceFileName);
#else
            Stream stream = assembly.GetManifestResourceStream(assembly.GetName().Name + "." + resourceFileName);
        // handle the case where our caller hasn't prefixed the resource name with the root namespace
            return stream ?? assembly.GetManifestResourceStream(resourceFileName);
#endif
        }

        internal static string GetAssemblyName(Assembly assembly)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            return ManagementUtil.GetAssemblyName(assembly);
#else
            return assembly.GetName().Name;
#endif

        }

        internal static Assembly GetExecutingAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        internal static TypeConverter GetTypeConverter(Type t)
        {
#if !NETSTANDARD2_0 && !NETCOREAPP
            return ManagementUtil.GetTypeConverter(t);
#else
            return TypeDescriptor.GetConverter(t);
#endif
        }
    }
}
