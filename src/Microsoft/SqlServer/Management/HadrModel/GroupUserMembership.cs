// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

namespace Microsoft.SqlServer.Management.HadrModel
{
    /// <summary>
    /// Used by WHIHelper for the Group User Membership
    /// </summary>
    public enum GroupUserMembership
    {
        /// <summary>
        /// unkownn membership
        /// </summary>
        Unknown,

        /// <summary>
        /// is a member
        /// </summary>
        Member,

        /// <summary>
        /// not a member
        /// </summary>
        NonMember
    }
}
