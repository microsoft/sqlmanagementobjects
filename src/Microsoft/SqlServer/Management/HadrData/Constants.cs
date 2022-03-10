// Copyright (c) Microsoft.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrData
{
    /// <summary>
    /// Common place to capture all the constants that we use in the HadrData
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Well known sid types
        /// </summary>
        internal static readonly UserSecurity.WellKnownSidType[] WellKnownSidTypes ={ 
                                                                                        UserSecurity.WellKnownSidType.WinLocalSystemSid,
                                                                                        UserSecurity.WellKnownSidType.WinLocalServiceSid,
                                                                                        UserSecurity.WellKnownSidType.WinNetworkServiceSid,
                                                                                        UserSecurity.WellKnownSidType.WinAnonymousSid,
                                                                                        UserSecurity.WellKnownSidType.WinWorldSid,
                                                                                        UserSecurity.WellKnownSidType.WinNtAuthoritySid 
                                                                                    };

        /// <summary>
        /// Returns set of fields passed to Server.SetDefaultInitFields for Database object
        /// to optimize the data retrieval from SQL server.
        /// </summary>
        internal static string[] DefaultDatabaseInitFields
        {
            get
            {
                return new string[]
                {
                    "AvailabilityGroupName",
                    "HasFullBackup",
                    "IsAccessible",
                    "IsDatabaseSnapshot",
                    "IsMirroringEnabled",
                    "IsSystemObject",
                    "Name",
                    "RecoveryModel",
                    "UserAccess",
                    "ReadOnly",
                    "AutoClose",
                    "HasDatabaseEncryptionKey"
                };
            }
        }
    }
}
