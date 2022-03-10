// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.SqlServer.Management.Common
{
    internal class SafeNativeMethods
    {

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
        int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        /// <summary>
        /// get the HTOKEN of the specifies user, this token can then be used to impersonate the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="domain"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="System.ComponentModel.Win32Exception">Thrown when LogonUser API fails</exception>
        internal static SafeAccessTokenHandle GetUserToken(string user, string domain, string password)
        {
            const int LOGON32_PROVIDER_DEFAULT = 0;
            //This parameter causes LogonUser to create a primary token.   
            const int LOGON32_LOGON_INTERACTIVE = 2;
            bool returnValue = LogonUser(user, domain, password,
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                out SafeAccessTokenHandle safeAccessTokenHandle);

            if (!returnValue)
            {
                    var ret = Marshal.GetLastWin32Error();
                    Trace.TraceInformation("LogonUser failed with error code : {0}", ret);
                    throw new System.ComponentModel.Win32Exception(ret);                
            }
            return safeAccessTokenHandle;
        }
    }
}

