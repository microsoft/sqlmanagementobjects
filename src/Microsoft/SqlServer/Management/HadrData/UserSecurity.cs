// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using System.Text;

namespace Microsoft.SqlServer.Management.HadrData
{
    internal class UserSecurity
    {

        /// <summary>
        /// Well known Sid Types
        /// </summary>
        // http://msdn.microsoft.com/en-us/library/aa379650(VS.85).aspx
        public enum WellKnownSidType
        {
            /// <summary>
            /// Null
            /// </summary>
            WinNullSid,

            /// <summary>
            /// world
            /// </summary>
            WinWorldSid,

            /// <summary>
            /// local
            /// </summary>
            WinLocalSid,

            /// <summary>
            /// creator owner
            /// </summary>
            WinCreatorOwnerSid,

            /// <summary>
            /// creator group
            /// </summary>
            WinCreatorGroupSid,

            /// <summary>
            /// creator owner server
            /// </summary>
            WinCreatorOwnerServerSid,

            /// <summary>
            /// creator group server
            /// </summary>
            WinCreatorGroupServerSid,

            /// <summary>
            /// NT AUTHORITY
            /// </summary>
            WinNtAuthoritySid,

            /// <summary>
            /// Dialup
            /// </summary>
            WinDialupSid,

            /// <summary>
            /// NETWORK
            /// </summary>
            WinNetworkSid,

            /// <summary>
            /// Batch
            /// </summary>
            WinBatchSid,

            /// <summary>
            /// Interactive
            /// </summary>
            WinInteractiveSid,

            /// <summary>
            /// Service
            /// </summary>
            WinServiceSid,

            /// <summary>
            /// Anonymous
            /// </summary>
            WinAnonymousSid,

            /// <summary>
            /// Proxy
            /// </summary>
            WinProxySid,

            /// <summary>
            /// Enterprise Controllers
            /// </summary>
            WinEnterpriseControllersSid,

            /// <summary>
            /// Self
            /// </summary>
            WinSelfSid,

            /// <summary>
            /// Authenticated User
            /// </summary>
            WinAuthenticatedUserSid,

            /// <summary>
            /// Restricted Code
            /// </summary>
            WinRestrictedCodeSid,

            /// <summary>
            /// Terminal Server
            /// </summary>
            WinTerminalServerSid,

            /// <summary>
            /// Remote Logon Id
            /// </summary>
            WinRemoteLogonIdSid,

            /// <summary>
            /// Logon Ids
            /// </summary>
            WinLogonIdsSid,

            /// <summary>
            /// LOCAL SYSTEM
            /// </summary>
            WinLocalSystemSid,

            /// <summary>
            /// LOCAL SERVICE
            /// </summary>
            WinLocalServiceSid,

            /// <summary>
            /// NETWORK SERVICE
            /// </summary>
            WinNetworkServiceSid,

            /// <summary>
            /// BUILTIN Domain
            /// </summary>
            WinBuiltinDomainSid,

            /// <summary>
            /// BUILTIN\Administrators
            /// </summary>
            WinBuiltinAdministratorsSid,

            /// <summary>
            /// BUILTIN Users
            /// </summary>
            WinBuiltinUsersSid,

            /// <summary>
            /// BUILTIN Guests
            /// </summary>
            WinBuiltinGuestsSid,

            /// <summary>
            /// BUILTIN power users
            /// </summary>
            WinBuiltinPowerUsersSid,

            /// <summary>
            /// BUILTIN account operators
            /// </summary>
            WinBuiltinAccountOperatorsSid,

            /// <summary>
            /// BUILTIN system operators
            /// </summary>
            WinBuiltinSystemOperatorsSid,

            /// <summary>
            /// BUILTIN print operators
            /// </summary>
            WinBuiltinPrintOperatorsSid,

            /// <summary>
            /// BUILTIN  backup operators
            /// </summary>
            WinBuiltinBackupOperatorsSid,

            /// <summary>
            /// BUILTIN replicator
            /// </summary>
            WinBuiltinReplicatorSid,

            /// <summary>
            /// BUILTIN Pre Windows 2000 Compatible Access
            /// </summary>
            WinBuiltinPreWindows2000CompatibleAccessSid,

            /// <summary>
            /// BUILTIN remote desktop users
            /// </summary>
            WinBuiltinRemoteDesktopUsersSid,

            /// <summary>
            /// BUILTIN network configuration operators
            /// </summary>
            WinBuiltinNetworkConfigurationOperatorsSid,

            /// <summary>
            /// account administrators
            /// </summary>
            WinAccountAdministratorSid,

            /// <summary>
            /// account guests
            /// </summary>
            WinAccountGuestSid,

            /// <summary>
            /// account krbtgt
            /// </summary>
            WinAccountKrbtgtSid,

            /// <summary>
            /// account domain admins
            /// </summary>
            WinAccountDomainAdminsSid,

            /// <summary>
            /// account domain users
            /// </summary>
            WinAccountDomainUsersSid,

            /// <summary>
            /// account domain guests
            /// </summary>
            WinAccountDomainGuestsSid,

            /// <summary>
            /// account computers
            /// </summary>
            WinAccountComputersSid,

            /// <summary>
            /// account controllers
            /// </summary>
            WinAccountControllersSid,

            /// <summary>
            /// account cert admins
            /// </summary>
            WinAccountCertAdminsSid,

            /// <summary>
            /// account schema admins
            /// </summary>
            WinAccountSchemaAdminsSid,

            /// <summary>
            /// account enterprise admins
            /// </summary>
            WinAccountEnterpriseAdminsSid,

            /// <summary>
            /// account policy admins
            /// </summary>
            WinAccountPolicyAdminsSid,

            /// <summary>
            /// account RAS and IAS servers
            /// </summary>
            WinAccountRasAndIasServersSid,

            /// <summary>
            /// NT LM authentication
            /// </summary>
            WinNTLMAuthenticationSid,

            /// <summary>
            /// Digest authentication
            /// </summary>
            WinDigestAuthenticationSid,

            /// <summary>
            /// S Channel Authentication
            /// </summary>
            WinSChannelAuthenticationSid,

            /// <summary>
            /// This organization
            /// </summary>
            WinThisOrganizationSid,

            /// <summary>
            /// Other organization
            /// </summary>
            WinOtherOrganizationSid,

            /// <summary>
            /// BUILTIN incoming forest trust builders
            /// </summary>
            WinBuiltinIncomingForestTrustBuildersSid,

            /// <summary>
            /// BUILTIN perf monitoring users
            /// </summary>
            WinBuiltinPerfMonitoringUsersSid,

            /// <summary>
            /// BUILTIN perf logging users
            /// </summary>
            WinBuiltinPerfLoggingUsersSid,

            /// <summary>
            /// BUILTIN authorization access
            /// </summary>
            WinBuiltinAuthorizationAccessSid,

            /// <summary>
            /// BUILTIN terminal server license servers
            /// </summary>
            WinBuiltinTerminalServerLicenseServersSid,

            /// <summary>
            /// BUILTIN DCOM users
            /// </summary>
            WinBuiltinDCOMUsersSid,

            /// <summary>
            /// BUILTIN IUsers
            /// </summary>
            WinBuiltinIUsersSid,

            /// <summary>
            /// IUsers
            /// </summary>
            WinIUserSid,

            /// <summary>
            /// BUILTIN crypto operators
            /// </summary>
            WinBuiltinCryptoOperatorsSid,

            /// <summary>
            /// untrusted label
            /// </summary>
            WinUntrustedLabelSid,

            /// <summary>
            /// low label
            /// </summary>
            WinLowLabelSid,

            /// <summary>
            /// medium label
            /// </summary>
            WinMediumLabelSid,

            /// <summary>
            /// high label
            /// </summary>
            WinHighLabelSid,

            /// <summary>
            /// system label
            /// </summary>
            WinSystemLabelSid,

            /// <summary>
            /// write restricted code
            /// </summary>
            WinWriteRestrictedCodeSid,

            /// <summary>
            /// creator owner rights
            /// </summary>
            WinCreatorOwnerRightsSid,

            /// <summary>
            /// cacheable principals group
            /// </summary>
            WinCacheablePrincipalsGroupSid,

            /// <summary>
            /// cacheable principals group
            /// </summary>
            WinNonCacheablePrincipalsGroupSid,

            /// <summary>
            /// enterprise readonly controllers
            /// </summary>
            WinEnterpriseReadonlyControllersSid,

            /// <summary>
            /// readonly controllers
            /// </summary>
            WinAccountReadonlyControllersSid,

            /// <summary>
            /// BUILTIN event log readers group
            /// </summary>
            WinBuiltinEventLogReadersGroup,

            /// <summary>
            /// new enterprise readonly controllers
            /// </summary>
            WinNewEnterpriseReadonlyControllersSid,

            /// <summary>
            /// BUILTIN Cert Service DCOM access group
            /// </summary>
            WinBuiltinCertSvcDComAccessGroup
        }



        /// <summary>
        /// Win32 API which looks up the sid for an account name
        /// </summary>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool LookupAccountName([In, MarshalAs(UnmanagedType.LPWStr)] string systemName,
                                                     [In, MarshalAs(UnmanagedType.LPWStr)] string accountName,
                                                     IntPtr sid,
                                                     ref int cbSid,
                                                     StringBuilder referencedDomainName,
                                                     ref int cbReferencedDomainName,
                                                     out int use);


        /// <summary>
        /// Win32 API which compares the sid with a sid type
        /// </summary>
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool IsWellKnownSid(IntPtr pSid, Int32 sidType);


        /// <summary>
        /// This function will try to determine if a account name is domain user account.
        /// 1. determines if the account specified by accountName parameter matches
        /// any sid from the sidTypeList parameter.
        /// 2. if we can find it from the domain, LoopupAccountName will try to resolve the name using domain controllers trusted by the local system
        /// </summary>
        /// <param name="accountName">account name.</param>
        /// <param name="sidTypeList">The list of SIDs from which to compare the account to.</param>
        public static bool IsDomainUserAccount(string accountName, IEnumerable<WellKnownSidType> sidTypeList)
        {
            if (string.IsNullOrEmpty(accountName))
            {
                return false;
            }

            if (sidTypeList == null) {
                throw new ArgumentNullException("sidTypeList");
            }

            IntPtr sid = IntPtr.Zero;    // pointer to binary form of SID string.
            int sidLength = 0;           // size of SID buffer.
            int domainLength = 0;        // size of domain name buffer.
            int use;                     // type of object.

            StringBuilder domain = new StringBuilder();
            int error = 0;

            // first call of the function only returns the size of buffers (SID, domain name)
            LookupAccountName(null, accountName, sid, ref sidLength, domain, ref domainLength, out use);

            error = Marshal.GetLastWin32Error();


            if (error == 1332) //not mapping means there is such account, then we will think it's not a domain user
            {
                return false;
            }
            else if (error == 122) // error 122 (The data area passed to a system call is too small) normal behaviour.
            {
                // allocate memory for domain name
                domain = new StringBuilder(domainLength);

                try
                {
                    // allocate memory for SID
                    sid = Marshal.AllocHGlobal(sidLength);

                }
                catch (OutOfMemoryException)
                {
                    return false;
                }
                if (sid == IntPtr.Zero) // allocate memory failed
                {
                    return false;
                }
                try
                {
                    bool returnCode = LookupAccountName(null, accountName, sid, ref sidLength, domain, ref domainLength, out use);

                    if (returnCode == false)
                    {
                        error = Marshal.GetLastWin32Error();
                        return false;
                    }
                    else
                    {
                        foreach (WellKnownSidType sidType in sidTypeList)
                        {
                            if (IsWellKnownSid(sid, (int)sidType))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }
                finally
                {
                    if (sid != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(sid);
                    }
                }
            }
            return false;
        }

    }
}
