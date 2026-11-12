// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.SqlServer.Test.Manageability.Utils
{
    /// <summary>
    /// Helpful extensions methods on <see cref="T:System.String" />.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Calls string.Format with the specified args
        /// </summary>
        /// <param name="str"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string FormatStr(this string str, params object[] args)
        {
            return string.Format(str, args);
        }
        /// <summary>
        /// Replaces all occurrences of each key in the given dictionary with the corresponding value.
        /// </summary>
        /// <param name="target">The string to replace in.</param>
        /// <param name="values">The dictionary (or in general: enumeration of key-value pairs) of key-value pairs to replace.</param>
        /// <returns>The string with the keys replaced with values.</returns>
        public static string Replace(this string target, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<string, string>> values)
        {
            foreach (System.Collections.Generic.KeyValuePair<string, string> value in values)
            {
                target = target.Replace(value.Key, value.Value);
            }
            return target;
        }

        /// <summary>
        /// String replace that takes a StringComparison to specify the type of StringComparison to use
        /// </summary>
        /// <param name="str">The string to replace in</param>
        /// <param name="oldValue">The old string value to replace</param>
        /// <param name="newValue">The new string value to replace with</param>
        /// <param name="comparison">The type of string comparison to use</param>
        /// <returns>The string with all occurances of oldValue replaced with newValue</returns>
        public static string Replace(this string str, string oldValue, string newValue, System.StringComparison comparison)
        {
            if (oldValue == null)
            {
                throw new System.ArgumentNullException("oldValue");
            }
            if (oldValue == string.Empty)
            {
                throw new System.ArgumentException("String must be non-empty", "oldValue");
            }
            if (newValue == null)
            {
                throw new System.ArgumentNullException("newValue");
            }
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            int previousIndex = 0;
            for (int index = str.IndexOf(oldValue, comparison); index != -1; index = str.IndexOf(oldValue, index, comparison))
            {
                sb.Append(str.Substring(previousIndex, index - previousIndex));
                sb.Append(newValue);
                index += oldValue.Length;
                previousIndex = index;
            }
            sb.Append(str.Substring(previousIndex));
            return sb.ToString();
        }

        /// <summary>
        /// Converts the specified string <paramref name="value" /> to type <typeparamref name="T" />.
        /// Extension method on <see cref="T:Microsoft.SqlServer.Test.Utilities.StringExtensions" />.
        /// </summary>
        /// <exception cref="T:System.ArgumentException"><paramref name="value" /> is null.</exception>
        /// <typeparam name="T">The type to convert the string to.</typeparam>
        /// <param name="value">The string value to convert.</param>
        /// <returns>Result of the conversion.</returns>
        public static T ConvertTo<T>(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new System.ArgumentNullException("value");
            }
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter == null)
            {
                throw new System.InvalidOperationException("Unable to find a converter for " + typeof(T).FullName);
            }
            return (T)((object)converter.ConvertFromString(value));
        }

        /// <summary>
        /// Converts the specified string <paramref name="value" /> to type <typeparamref name="T" /> or returns <paramref name="defaultValue" />
        /// if it cannot convert the string.
        /// Extension method on <see cref="T:Microsoft.SqlServer.Test.Utilities.StringExtensions" />.
        /// </summary>
        /// <typeparam name="T">The type to convert the string to.</typeparam>
        /// <param name="value">The string value to convert.</param>
        /// <param name="defaultValue">The default value to return if it is unable to convert the string.</param>
        /// <returns>
        /// Result of the conversion or default value.
        /// </returns>
        public static T ConvertToOrDefault<T>(this string value, T defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter == null)
            {
                throw new System.InvalidOperationException("Unable to find a converter for " + typeof(T).FullName);
            }
            if (!converter.IsValid(value))
            {
                return defaultValue;
            }
            return (T)((object)converter.ConvertFromString(value));
        }

        /// <summary>
        /// Converts secure string to string
        /// </summary>
        /// <param name="secureString">Secure string</param>
        /// <returns>secure string converted to string</returns>
        public static string SecureStringToString(this SecureString secureString)
        {
            if (secureString == null)
            {
                return null;
            }

            char[] charArray = new char[secureString.Length];
            IntPtr ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);

            try
            {
                Marshal.Copy(ptr, charArray, 0, secureString.Length);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }

            return new string(charArray);
        }

        /// <summary>
        /// Converts char array to secure string
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Array of characters to secure string</returns>
        public static SecureString StringToSecureString(this string str)
        {
            if (str == null)
            {
                return null;
            }

            var secureString = new SecureString();
            foreach (char c in str)
            {
                secureString.AppendChar(c);
            }

            secureString.MakeReadOnly();

            return secureString;
        }

        /// <summary>
        /// Converts \r\n in input to Environment.NewLine
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FixNewLines(this string input)
        {
            if (Environment.NewLine != "\r\n")
            {
                return input.Replace("\r\n", Environment.NewLine);
            }

            return input;
        }

        /// <summary>
        /// Normalizes whitespace in the input string by replacing multiple whitespace characters with a single space and trimming the result.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string NormalizeWhitespace(this string input) =>
                System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ").Trim();

        /// <summary>
        /// Checks if the file name is a DAC package
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static bool IsPackageFileName(this string fileName) => System.IO.Path.GetExtension(fileName ?? "").EndsWith("acpac", StringComparison.OrdinalIgnoreCase);
    }
}
