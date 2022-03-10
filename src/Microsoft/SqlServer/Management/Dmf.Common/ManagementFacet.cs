// Copyright (c) Microsoft.
// Licensed under the MIT license.

using Microsoft.SqlServer.Management.Dmf;

namespace Microsoft.SqlServer.Management.Facets
{
    /// <summary>
    /// Base Adapter interface - indicates implementing cass is an Adapter
    /// </summary>
    public interface IDmfAdapter { }

    /// <summary>
    /// An interface for adapters to supply object path (URI) and server info to UI and/or logs
    /// </summary>
    public interface IDmfObjectInfo
    {
        /// <summary>
        /// Object path
        /// </summary>
        string ObjectPath
        {
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
            get;
        }

        /// <summary>
        /// Root path
        /// </summary>
        string RootPath
        {
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
            get;
        }
    }
}

