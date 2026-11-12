// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.


using System;

namespace Microsoft.SqlServer.Management.Common
{
    /// <summary>
    /// This interface is a high-level connection interface.
    /// </summary>
    public interface ISfcConnection
    {
        bool Connect();
        bool Disconnect();

        ISfcConnection Copy();
        
        /// <summary>
        /// Returns if the connection is currently active.
        /// </summary>
        bool IsOpen               { get; }

        /// <summary>
        /// Name of the server we are connecting to.
        /// </summary>
        string  ServerInstance    { get; set; }
        
        /// <summary>
        /// Returns the version of the service we are connected to.
        /// </summary>
        Version ServerVersion     { get; }

       // this shouldn't be needed but we didn't have time to pull it out for Katmai
       object ToEnumeratorObject();

       /// <summary>
       /// Enforces a disconnect and ensures that connection cannot be re-opened again
       /// </summary>
       void ForceDisconnected();

       /// <summary>
       /// Indicates that the connection has been forcefully disconnected
       /// </summary>
       bool IsForceDisconnected { get; }
    }
}
