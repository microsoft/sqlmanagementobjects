// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
#if !NETSTANDARD2_0
using System.Security.Permissions;
#endif

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// Utility methods for working with SecureStrings
    /// </summary>
    internal static class EncryptionUtility
    {
        /// <summary>
        /// Decrypt a SecureString
        /// </summary>
        /// <param name="ss">The secure string to decrypt</param>
        // VSTS 109354: EncryptionUtility.DecryptSecureString(SecureString):String calls into Marshal.ZeroFreeBSTR(IntPtr)
        // which has a LinkDemand. By making this call, Marshal.ZeroFreeBSTR(IntPtr) is indirectly exposed to user code.
        // Review the call stack that might expose a way to circumvent security protection.
        // $ISSUE 122262: Consider using mpu/shared/SqlSecureString for our Common.EncryptionUtility.
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public static string DecryptSecureString(SecureString ss)
        {
            string result = String.Empty;

            if (ss != null)
            {
                // Allow partially trusted/low-privilege callers.  The point of this class
                // is to carry out operations on SecureStrings that require elevated privileges
                // without granting blanket UnmanagedCode permission to client assemblies.
                // Decrypting SqlSecureStrings created by client assemblies is not dangerous,
                // so no special permissions are demanded of clients to do so.
#if  !NETSTANDARD2_0 && !NETCOREAPP
                new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
#endif

                // Decrypt the SecureString

                IntPtr ps = Marshal.SecureStringToBSTR(ss);
                result = Marshal.PtrToStringBSTR(ps);
                Marshal.ZeroFreeBSTR(ps);

            }

            return result;
        }

        /// <summary>
        /// Encrypt a string
        /// </summary>
        /// <param name="s">The string to encrypt</param>
        public static SecureString EncryptString(string s)
        {
            SecureString result = new SecureString();

            if (s != null)
            {
                foreach (char ch in s.ToCharArray())
                {
                    result.AppendChar(ch);
                }
            }

            return result;
        }

    }

    /// <summary>
    /// General utility methods.
    /// </summary>
    internal static class CommonUtils
    {
        /// <summary>
        /// Makes the string a SQL identifier.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        internal static String MakeSqlBraket(String s)
        {
            return string.Format(CultureInfo.InvariantCulture, "[{0}]", EscapeString(s, "]"));
        }

        /// <summary>
        /// Makes the string a unicode sql string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        static internal String MakeSqlString(String value)
        {
            if (null == value)
            {
                return null;
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("N'");
            sb.Append(EscapeString(value, "\'"));
            sb.Append("'");
            return sb.ToString();
        }

        /// <summary>
        /// Escape the specified cEsc character
        /// </summary>
        internal static String EscapeString(String s, string esc)
        {
            if (null == s)
            {
                return null;
            }
            string replace = esc + esc;
            StringBuilder sb = new StringBuilder(s);
            sb.Replace(esc, replace);
            return sb.ToString();
        }
    }
}