// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// Simple interface to hide details of access token renewal.
    /// Consumers of this interface must defer retrieval of the token string until the point of SqlConnectionUsage
    /// </summary>
    public interface IRenewableToken
    {
        /// <summary>
        /// Returns the expiration time of the most recently retrieved token
        /// If no token has been retrieved the value is undefined.
        /// </summary>
        DateTimeOffset TokenExpiry { get; }

        /// <summary>
        /// Returns an access token that can be used to query the associated Resource until the TokenExpiry time
        /// </summary>
        /// <returns></returns>
        string GetAccessToken();

        /// <summary>
        /// The URL of the resource associated with the token
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// The tenant id associated with the token
        /// </summary>
        string Tenant { get; }

        /// <summary>
        /// The user id associated with the token
        /// </summary>
        string UserId { get; }
    }
}
