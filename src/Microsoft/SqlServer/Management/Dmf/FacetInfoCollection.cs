// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.



using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// This is the collection for FacetInfo.
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class FacetInfoCollection : KeyedCollection<string, FacetInfo>
    {
        /// <summary>
        ///
        /// </summary>
        protected override string GetKeyForItem(FacetInfo item)
        {
            return item.Name;
        }
    }
}
