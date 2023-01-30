// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

/*
 * 
 * This assembly will act as UNSAFE provider for SMO and assemblies it depends on,
 * since SMO is running inside SQLCLR as SAFE, it can't access resources like IO, reflection, etc.
 * 
 * This assembly will be running as UNSAFE and it'll be internal to SMO and its dependencies only, it won't
 * be exposed to external users so it won't expose any security holes in the product, it'll be visible to SMO only.
 * 
 * 
 * mmeshref
 * 
 * */







using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Soap;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Threading;

namespace Microsoft.SqlServer.Smo.UnSafeInternals
{
    internal class ManagementUtil
    {
        private readonly static byte[] msPublicKey = new byte[] { 0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00, 0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x27, 0x27, 0x36, 0xAD, 0x6E, 0x5F, 0x95, 0x86, 0xBA, 0xC2, 0xD5, 0x31, 0xEA, 0xBC, 0x3A, 0xCC, 0x66, 0x6C, 0x2F, 0x8E, 0xC8, 0x79, 0xFA, 0x94, 0xF8, 0xF7, 0xB0, 0x32, 0x7D, 0x2F, 0xF2, 0xED, 0x52, 0x34, 0x48, 0xF8, 0x3C, 0x3D, 0x5C, 0x5D, 0xD2, 0xDF, 0xC7, 0xBC, 0x99, 0xC5, 0x28, 0x6B, 0x2C, 0x12, 0x51, 0x17, 0xBF, 0x5C, 0xBE, 0x24, 0x2B, 0x9D, 0x41, 0x75, 0x07, 0x32, 0xB2, 0xBD, 0xFF, 0xE6, 0x49, 0xC6, 0xEF, 0xB8, 0xE5, 0x52, 0x6D, 0x52, 0x6F, 0xDD, 0x13, 0x00, 0x95, 0xEC, 0xDB, 0x7B, 0xF2, 0x10, 0x80, 0x9C, 0x6C, 0xDA, 0xD8, 0x82, 0x4F, 0xAA, 0x9A, 0xC0, 0x31, 0x0A, 0xC3, 0xCB, 0xA2, 0xAA, 0x05, 0x23, 0x56, 0x7B, 0x2D, 0xFA, 0x7F, 0xE2, 0x50, 0xB3, 0x0F, 0xAC, 0xBD, 0x62, 0xD4, 0xEC, 0x99, 0xB9, 0x4A, 0xC4, 0x7C, 0x7D, 0x3B, 0x28, 0xF1, 0xF6, 0xE4, 0xC8 };
        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static Assembly LoadAssembly(String fileName)
        {
            return Assembly.Load(fileName);
        }

        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static Assembly LoadAssemblyFromFile(string fileName)
        {
            return Assembly.LoadFile(fileName);
        }


        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static Stream LoadResourceFromAssembly(Assembly assembly, String resourceFileName)
        {
            Stream stream = assembly.GetManifestResourceStream(resourceFileName);
            return stream;
        }


        [FileIOPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static string GetAssemblyName(Assembly assembly)
        {
            return assembly.GetName().Name;
        }

        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static Object CreateInstance(Assembly assembly, string objectType)
        {
            return assembly.CreateInstance(objectType,
                        false, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.CreateInstance,
                        null, null, CultureInfo.InvariantCulture, null);
        }

        [SecurityPermission(SecurityAction.Assert,Unrestricted=true)]
        internal static bool CallerIsMicrosoftAssembly(Assembly currentAssembly)
        {
            if (currentAssembly == null)
                return false;
            StackTrace tracer = new StackTrace();
            bool isNotDmfOrSimilarCall = false;
            //We will loop on the whole tree, in  case of DMF all the members in the tree
            //are supposed to be signed, so if we meet unsigned member in the tree then
            //it's not DMF, it's a user call
            foreach (StackFrame frame in tracer.GetFrames())
            {
                isNotDmfOrSimilarCall = false;
                Assembly functionAssembly = frame.GetMethod().Module.Assembly;

                //Assembly.Evidence.GetEnumerator is obsolete, hence GetAssemblyEnumerator and GetHostEnumerator instead.
                IEnumerator assemblyEnumerator = functionAssembly.Evidence.GetAssemblyEnumerator();
                while (assemblyEnumerator.MoveNext())
                {
                    StrongName sName = assemblyEnumerator.Current as StrongName;
                    if (sName != null && sName.PublicKey.Equals(new StrongNamePublicKeyBlob(msPublicKey)))
                    {
                        isNotDmfOrSimilarCall = true;
                        break;
                    }
                }
                if (!isNotDmfOrSimilarCall) //if the loop wasn't broken earlier.
                {
                    IEnumerator hostEnumerator = functionAssembly.Evidence.GetHostEnumerator();
                    while (hostEnumerator.MoveNext())
                    {
                        StrongName sName = hostEnumerator.Current as StrongName;
                        if (sName != null && sName.PublicKey.Equals(new StrongNamePublicKeyBlob(msPublicKey)))
                        {
                            isNotDmfOrSimilarCall = true;
                            break;
                        }
                    }
                }
                if (isNotDmfOrSimilarCall == false)
                    return false;
            }
            return true;
        }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static void SerializeWithSoapFormatter(MemoryStream memoryStream, Exception pfe)
        {
            new SoapFormatter().Serialize(memoryStream, pfe);
        }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static void EnterMonitor(object lockObject)
        {
            Monitor.Enter(lockObject);
        }

        [SecurityPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static void ExitMonitor(object lockObject)
        {
            Monitor.Exit(lockObject);
        }


        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true)]
        internal static TypeConverter GetTypeConverter(Type t)
        {
            return TypeDescriptor.GetConverter(t);
        }
    }
}