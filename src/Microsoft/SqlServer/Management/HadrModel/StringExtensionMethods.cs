// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.SqlServer.Management.HadrModel
{
    public static class StringExtensionMethods
    {
        /// <summary>
        /// Converts a secure string to a string
        /// </summary>
        /// <param name="secureString">Secure string</param>
        /// <returns>Converted secure string to string object</returns>
        public static string SecureStringToString(this SecureString secureString)
        {
            return new string(StringExtensionMethods.SecureStringToCharArray(secureString));
        }

        /// <summary>
        /// Converts string to a secure string
        /// </summary>
        /// <param name="unsecureString">Unsecured string</param>
        /// <returns>Converted string to secure string</returns>
        public static SecureString StringToSecureString(this string unsecureString)
        {
            return CharArrayToSecureString(unsecureString.ToCharArray());
        }

        /// <summary>
        /// Converts secure string to char array
        /// </summary>
        /// <param name="secureString">Secure string</param>
        /// <returns>secure string converted to array of characters</returns>
        private static char[] SecureStringToCharArray(SecureString secureString)
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

            return charArray;
        }

        /// <summary>
        /// Converts char array to secure string
        /// </summary>
        /// <param name="charArray">the array of chars</param>
        /// <returns>Array of characters to secure string</returns>
        private static SecureString CharArrayToSecureString(char[] charArray)
        {
            if (charArray == null)
            {
                return null;
            }

            SecureString secureString = new SecureString();
            foreach (char c in charArray)
            {
                secureString.AppendChar(c);
            }

            secureString.MakeReadOnly();

            return secureString;
        }
    }
}