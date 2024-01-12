// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

#if !NETSTANDARD2_0
using System;
using System.ComponentModel;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// Enables storage and retrieval of passwords from Windows Credential Manager, keyed by connection type and user name
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public class WindowsCredential
    {
#region Private Methods
        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredRead(string target, CRED_TYPE type, int reservedFlag, out IntPtr CredentialPtr);

        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredWrite([In] ref Credential userCredential, [In] UInt32 flags);

        [DllImport("Advapi32.dll", EntryPoint = "CredFree", SetLastError = true)]
        static extern bool CredFree([In] IntPtr cred);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredDelete(
            string targetName,
            CRED_TYPE type,
            int flags
            );

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CredEnumerate(
            string targetName,
            int flags,
            [Out] out int count,
            [Out] out IntPtr pCredential
            );

        enum CRED_TYPE
        {
            GENERIC = 1,
            DOMAIN_PASSWORD = 2,
            DOMAIN_CERTIFICATE = 3,
            DOMAIN_VISIBLE_PASSWORD = 4
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential
        {
            public UInt32 Flags;
            public CRED_TYPE Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public UInt32 CredentialBlobSize;
            public IntPtr CredentialBlob;
            public UInt32 Persist;
            public UInt32 AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        /// <summary>
        /// Here are factors influencing the choice of key:
        /// 1. SSMS stores the list of saved connections in a versioned file, for both connection dialog and registered servers. If in SSMS 19 we decide we want to use the same credentials as 18,
        /// we can change this code to use the 18 instead of the SSMSMajorVersionString. Having a version allows us to change the semantic in the future, perhaps by encoding the entire blob in the store
        /// and enumerating all the credentials from there instead of from the BIN file.
        /// 2. There are multiple saved connection lists - SSMS registered servers, SqlRepl Ui registered servers, SSMS connection dialog. We have a repo name for each of those lists.
        /// 3. All the lists have groupings by server type, auth type, and user name.
        /// 4. We want it to be human readable so users can edit the passwords using the Windows ui if desired. Unfortunately the Windows UI truncates the key name in the display but
        /// that's beyond our control. The user has to hover over the name to see the full text in a tooltip.
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="instance"></param>
        /// <param name="authType"></param>
        /// <param name="user"></param>
        /// <param name="serverType"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        private static string GetKey(string repo, string instance, int authType, string user, Guid serverType, string version)
        {
            return string.Format("Microsoft:{0}:{1}:{2}:{3}:{4}:{5}", repo, version, instance, user, serverType, authType);
        }

        /// <summary>
        /// Returns a key for Azure Data Studio credential
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="database"></param>
        /// <param name="authType"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static string GetAdsKey(string instance, string database, string authType, string user)
        {
            return string.Format(
                "Microsoft.SqlTools|itemtype:Profile|id:providerName:MSSQL|applicationName:azdata|authenticationType:{0}|database:{1}|server:{2}|user:{3}",
                authType, database ?? String.Empty, instance, user);
        }

        private static void SetSqlCredential(string targetName, string user, SecureString password)
        {
            var cred = new Credential
            {
                TargetName = Marshal.StringToCoTaskMemUni(targetName),
                UserName = Marshal.StringToCoTaskMemUni(user),
                CredentialBlob = Marshal.SecureStringToCoTaskMemUnicode(password),
                CredentialBlobSize = (uint)password.Length * 2,
                AttributeCount = 0,
                Attributes = IntPtr.Zero,
                Comment = IntPtr.Zero,
                TargetAlias = IntPtr.Zero,
                Type = CRED_TYPE.GENERIC,
                Persist = 2 // CRED_PERSIST_LOCAL_MACHINE
            };


            try
            {
                if (!CredWrite(ref cred, 0))
                {
                    throw new Win32Exception(StringConnectionInfo.UnableToSavePasswordFormat(user));
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(cred.TargetName);
                Marshal.FreeCoTaskMem(cred.UserName);
                Marshal.ZeroFreeCoTaskMemUnicode(cred.CredentialBlob);
            }
        }

        private static SecureString GetSqlCredential(string targetName)
        {
            SecureString password = null;
            IntPtr pCredential;
            if (CredRead(targetName, CRED_TYPE.GENERIC, 0, out pCredential))
            {
                try
                {
                    var credential = (Credential)Marshal.PtrToStructure(pCredential, typeof(Credential));
                    // CredentialBlob can be null somehow, perhaps if the user tried to edit it externally.
                    var passwordString = credential.CredentialBlob == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(credential.CredentialBlob,
                        (int)credential.CredentialBlobSize / 2);
                    password = EncryptionUtility.EncryptString(passwordString);
                }
                finally
                {
                    CredFree(pCredential);
                }
            }
            return password;
        }

        private static void RemoveCredential(string targetName)
        {
            CredDelete(targetName, CRED_TYPE.GENERIC, 0);
        }
#endregion

#region Public methods

        /// <summary>
        /// Stores the password for the given SSMS saved connection in Windows Credential Manager
        /// </summary>
        /// <param name="instance">The server name</param>
        /// <param name="authType">The type of authentication used by the connection. The meaning of the value depends on the <paramref name="serverType"/></param>
        /// <param name="user">The user name</param>
        /// <param name="serverType">The type of server connection. The valid values are application dependent.</param>
        /// <param name="password">The password</param>
        /// <param name="version">The version identifier of the calling application</param>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when saving the password fails</exception>
#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static void SetSqlSsmsCredential(string instance, int authType, string user, Guid serverType, SecureString password, string version)
        {
            var targetName = GetKey("SSMS", instance, authType, user, serverType, version);
            SetSqlCredential(targetName, user, password);
        }

        /// <summary>
        /// Stores the password for the given RegSvr connection in Windows Credential Manager. Currently only used by SqlRepl UI, not to be confused with
        /// SSMS registered servers.
        /// </summary>
        /// <param name="instance">The server name</param>
        /// <param name="authType">The type of authentication used by the connection. The meaning of the value depends on the <paramref name="serverType"/></param>
        /// <param name="user">The user name</param>
        /// <param name="serverType">The type of server connection. The valid values are application dependent.</param>
        /// <param name="password">The password</param>
        /// <param name="version">The version identifier of the calling application</param>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when saving the password fails</exception>
#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static void SetSqlRegSvrCredential(string instance, int authType, string user, Guid serverType, SecureString password, string version)
        {
            var targetName = GetKey("RegSvr", instance, authType, user, serverType, version);
            SetSqlCredential(targetName, user, password);
        }

        /// <summary>
        /// Returns the password from Windows Credential Manager for the given SSMS saved connection
        /// </summary>
        /// <param name="instance">The server name</param>
        /// <param name="authType">The type of authentication used by the connection. The meaning of the value depends on the <paramref name="serverType"/></param>
        /// <param name="user">The user name</param>
        /// <param name="serverType">The type of server connection. The valid values are application dependent.</param>
        /// <param name="version">The version identifier of the calling application</param>
        /// <returns></returns>
#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static SecureString GetSqlSsmsCredential(string instance, int authType, string user, Guid serverType, string version)
        {
            var targetName = GetKey("SSMS", instance, authType, user, serverType, version);
            return GetSqlCredential(targetName);
        }

        /// <summary>
        /// Returns the password from Windows Credential Manager for the given RegSvr. Currently only used by SqlRepl UI, not to be confused with
        /// SSMS registered servers.
        /// </summary>
        /// <param name="instance">The server name</param>
        /// <param name="authType">The type of authentication used by the connection. The meaning of the value depends on the <paramref name="serverType"/></param>
        /// <param name="user">The user name</param>
        /// <param name="serverType">The type of server connection. The valid values are application dependent.</param>
        /// <param name="version">The version identifier of the calling application</param>
        /// <returns></returns>
#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static SecureString GetSqlRegSvrCredential(string instance, int authType, string user, Guid serverType, string version)
        {
            var targetName = GetKey("RegSvr", instance, authType, user, serverType, version);
            return GetSqlCredential(targetName);
        }

        /// <summary>
        /// Removes the password of the given SSMS saved connection from Windows Credential Manager
        /// </summary>
        /// <param name="instance">The server name</param>
        /// <param name="authType">The type of authentication used by the connection. The meaning of the value depends on the <paramref name="serverType"/></param>
        /// <param name="user">The user name</param>
        /// <param name="serverType">The type of server connection. The valid values are application dependent.</param>
        /// <param name="version">The version identifier of the calling application</param>
#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static void RemoveSsmsCredential(string instance, int authType, string user, Guid serverType, string version)
        {
            var targetName = GetKey("SSMS", instance, authType, user, serverType, version);
            RemoveCredential(targetName);
        }

        /// <summary>
        /// Removes the password of the given registered server from Windows Credential Manager
        /// </summary>
        /// <param name="instance">The server name</param>
        /// <param name="authType">The type of authentication used by the connection. The meaning of the value depends on the <paramref name="serverType"/></param>
        /// <param name="user">The user name</param>
        /// <param name="serverType">The type of server connection. The valid values are application dependent.</param>
        /// <param name="version">The version identifier of the calling application</param>
#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static void RemoveRegSvrCredential(string instance, int authType, string user, Guid serverType, string version)
        {
            var targetName = GetKey("RegSvr", instance, authType, user, serverType, version);
            RemoveCredential(targetName);
        }

#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static void SetAzureDataStudioCredential(string instance, string database, string authType, string user, SecureString password)
        {
            var targetName = GetAdsKey(instance, database, authType, user);
            SetSqlCredential(targetName, user, password);
        }

#if !NETCOREAPP
        [PermissionSet(SecurityAction.Assert, Unrestricted = true)]
#endif
        public static SecureString GetAzureDataStudioCredential(string instance, string database, string authType, string user)
        {
            var targetName = GetAdsKey(instance, database, authType, user);
            return GetSqlCredential(targetName);
        }
#endregion
    }
}
#endif
