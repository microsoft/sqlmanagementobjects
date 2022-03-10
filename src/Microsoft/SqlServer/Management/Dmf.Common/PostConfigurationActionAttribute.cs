// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;

namespace Microsoft.SqlServer.Management.Dmf
{
    /// <summary>
    /// Custom attribute which describes post actions required for property
    /// configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
#if APTCA_ENABLED
    [System.Security.Permissions.StrongNameIdentityPermission(System.Security.Permissions.SecurityAction.LinkDemand, PublicKey = DmfConstants.MICROSOFT_SQLSERVER_PUBLIC_KEY)]
#endif
    public sealed class PostConfigurationActionAttribute : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="postConfigurationAction"></param>
        public PostConfigurationActionAttribute(PostConfigurationAction postConfigurationAction)
        {
            this.postConfigurationAction = postConfigurationAction;
        }

        private PostConfigurationAction postConfigurationAction;

        /// <summary>
        /// 
        /// </summary>
        public PostConfigurationAction PostConfigurationAction
        {
            get
            {
                return postConfigurationAction;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PostConfigurationAction
    {
        /// No action
        None = 0,
        /// Restart service
        RestartService = 1,
    }
}