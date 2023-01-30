// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;

using System.Text;
using System.Xml;

namespace Microsoft.SqlServer.Management.Smo
{
    internal static class NetCoreHelpers
    {
        private const int DefaultFileStreamBufferSize = 4096;

        private static CultureInfo invariantCulture = CultureInfo.InvariantCulture;

        public static CultureInfo InvariantCulture
        {
            get { return invariantCulture; }
        }

        public static int InvariantCultureLcid
        {
            get
            {
                return CultureInfo.InvariantCulture.LCID;
            }
        }

        public static XmlWriter CreateXmlWriter(TextWriter textWriter, XmlWriterSettings xmlSettings)
        {
            return XmlTextWriter.Create(textWriter, xmlSettings);
        }

        public static System.StringComparer GetStringComparer(this CultureInfo culture, bool ignoreCase)
        {
            return System.StringComparer.Create(culture, ignoreCase: ignoreCase);
        }

        public static Assembly GetAssembly(this Type type)
        {
            return type.Assembly;
        }

        public static Type GetBaseType(this Type type)
        {
            return type.BaseType;
        }

        public static Type[] GetGenericArguments(this Type type)
        {
            return type.GetGenericArguments();
        }

        public static bool GetIsAssignableFrom(this Type type, Type c)
        {
            return type.IsAssignableFrom(c);
        }

        public static bool GetIsClass(this Type type)
        {
            return type.IsClass;
        }

        public static bool GetIsEnum(this Type type)
        {
            return type.IsEnum;
        }

        public static bool GetIsGenericType(this Type type)
        {
            return type.IsGenericType;
        }

        public static bool GetIsNestedPrivate(this Type type)
        {
            return type.IsNestedPrivate;
        }

        public static bool GetIsPrimitive(this Type type)
        {
            return type.IsPrimitive;
        }

        public static bool GetIsSealed(this Type type)
        {
            return type.IsSealed;
        }

        public static bool GetIsValueType(this Type type)
        {
            return type.IsValueType;
        }

        public static StreamWriter CreateStreamWriter(string path, bool appendToFile)
        {
            return new StreamWriter(path, appendToFile);
        }

        public static StreamWriter CreateStreamWriter(string path, bool appendToFile, Encoding encoding)
        {
            return new StreamWriter(path, appendToFile, encoding);
        }

        private static FileStream CreateFileStream(string path, bool appendToFile)
        {
            FileMode mode = appendToFile ? FileMode.Append: FileMode.Create;
            FileStream stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read,
                DefaultFileStreamBufferSize, FileOptions.SequentialScan);
            return stream;
        }

        public static int StringCompare(string x, string y, bool ignoreCase, CultureInfo culture)
        {
            System.StringComparer comparer = culture.GetStringComparer(ignoreCase);
            return comparer.Compare(x, y);
        }

        /// <summary>
        /// Converts \r\n in input to Environment.NewLine. Used to convert string literals for environment-friendly line endings.
        /// </summary>
        /// <param name="input">String that may contain \r\n as newline character</param>
        /// <param name="sp">Optional ScriptingPreferences whose NewLine to use for the conversion</param>
        /// <returns></returns>
        public static string FixNewLines(this string input, ScriptingPreferences sp = null)
        {
            var newLine = sp?.NewLine ?? Environment.NewLine;
            if (newLine != "\r\n")
            {
                return input.Replace("\r\n", newLine);
            }

            return input;
        }
    }


}

