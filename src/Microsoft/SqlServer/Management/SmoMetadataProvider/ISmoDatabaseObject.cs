// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Smo;

namespace Microsoft.SqlServer.Management.SmoMetadataProvider
{
    /// <summary>
    /// Represents an object associated with SMO database metadata object.
    /// </summary>
    public interface ISmoDatabaseObject
    {
        /// <summary>
        /// Gets an <see cref="SqlSmoObject"/> object associated with this object.
        /// </summary>
        SqlSmoObject SmoObject { get; }
    }
}
