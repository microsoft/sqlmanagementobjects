// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Dmf.Common
{
    /// <summary>
    /// Class that provides various utilities. Public because UI modules also needs some methods here
    /// </summary>
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class Utils
    {
        /// <summary>
        /// Helper for configuration property validation
        /// </summary>
        /// <param name="property"></param>
        /// <param name="runValue"></param>
        /// <param name="configValue"></param>
        public static void CheckConfigurationProperty<T>(string property, T configValue, T runValue) where T : IComparable
        {
            if (!runValue.Equals(configValue))
            {
                throw new RestartPendingException<T>(property, configValue, runValue);
            }
        }
    }
}