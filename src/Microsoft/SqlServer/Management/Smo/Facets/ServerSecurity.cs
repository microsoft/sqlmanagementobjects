// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Data;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.FacetSR")]
    [Sfc.DisplayNameKey("ServerSecurityName")]
    [Sfc.DisplayDescriptionKey("ServerSecurityDesc")]
    public interface IServerSecurityFacet : Sfc.IDmfFacet
    {
        #region Interface Properties
        [Sfc.DisplayNameKey("PublicServerRoleIsGrantedPermissionsName")]
        [Sfc.DisplayDescriptionKey("PublicServerRoleIsGrantedPermissionsDesc")]
        bool PublicServerRoleIsGrantedPermissions
        {
            get;
        }

        [Sfc.DisplayNameKey("LoginModeName")]
        [Sfc.DisplayDescriptionKey("LoginModeDesc")]
        ServerLoginMode LoginMode
        {
            get;
        }

        [Sfc.DisplayNameKey("XPCmdShellEnabledName")]
        [Sfc.DisplayDescriptionKey("XPCmdShellEnabledDesc")]
        bool XPCmdShellEnabled
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("CrossDBOwnershipChainingEnabledName")]
        [Sfc.DisplayDescriptionKey("CrossDBOwnershipChainingEnabledDesc")]
        bool CrossDBOwnershipChainingEnabled
        {
            get;
            set;
        }

        [Sfc.DisplayNameKey("CommonCriteriaComplianceEnabledName")]
        [Sfc.DisplayDescriptionKey("CommonCriteriaComplianceEnabledDesc")]
        bool CommonCriteriaComplianceEnabled
        {
            get;
        }


        [Sfc.DisplayNameKey("IServerSecurityFacet_CmdExecRightsForSystemAdminsOnlyName")]
        [Sfc.DisplayDescriptionKey("IServerSecurityFacet_CmdExecRightsForSystemAdminsOnlyDesc")]
        bool CmdExecRightsForSystemAdminsOnly
        {
            get;
        }


        [Sfc.DisplayNameKey("IServerSecurityFacet_ProxyAccountIsGrantedToPublicRoleName")]
        [Sfc.DisplayDescriptionKey("IServerSecurityFacet_ProxyAccountIsGrantedToPublicRoleDesc")]
        bool ProxyAccountIsGrantedToPublicRole
        {
            get;
        }


        [Sfc.DisplayNameKey("IServerSecurityFacet_ReplaceAlertTokensEnabledName")]
        [Sfc.DisplayDescriptionKey("IServerSecurityFacet_ReplaceAlertTokensEnabledDesc")]
        bool ReplaceAlertTokensEnabled
        {
            get;
            set;
        }


        [Sfc.DisplayNameKey("IServerSecurityFacet_ProxyAccountEnabledName")]
        [Sfc.DisplayDescriptionKey("IServerSecurityFacet_ProxyAccountEnabledDesc")]
        bool ProxyAccountEnabled
        {
            get;
        }
        #endregion
    }

    /// <summary>
    /// Relational Engine Security.  This facet has some logical properties and it needs its own Refresh and Alter.
    /// It inherits from the ServerAdapterBase.
    /// </summary>
    public class ServerSecurityAdapter : ServerAdapterBase, IDmfAdapter, IServerSecurityFacet
    {
        #region Constructors
        public ServerSecurityAdapter(Microsoft.SqlServer.Management.Smo.Server obj) 
            : base (obj)
        {
        }
        #endregion

        #region Server Security Logical Properties


        public bool CmdExecRightsForSystemAdminsOnly
        {
            get
            {
                return this.Server.JobServer.SysAdminOnly;
            }
        }


        public bool ReplaceAlertTokensEnabled
        {
            get
            {
                return this.Server.JobServer.ReplaceAlertTokensEnabled;
            }
            set
            {
                this.Server.JobServer.ReplaceAlertTokensEnabled = value;
            }
        }

        public bool ProxyAccountIsGrantedToPublicRole
        {
            get
            {
                DataTable accountDataTable = this.Server.Roles["public"].EnumAgentProxyAccounts();
                return accountDataTable.Rows.Count > 0;
            }
        }

        public bool ProxyAccountEnabled
        {
            get
            {
                return this.Server.ProxyAccount.IsEnabled;
            }
        }


        /// <summary>
        /// The server permissions that are granted to the public server role.
        /// </summary>
        public bool PublicServerRoleIsGrantedPermissions
        {
            get
            {
                bool returnValue = false;
                ServerPermissionInfo[] serverPermissionInfoList = this.Server.EnumServerPermissions("public");
                foreach (ServerPermissionInfo serverPermission in serverPermissionInfoList)
                {
                    if (serverPermission.PermissionState == PermissionState.Grant || serverPermission.PermissionState == PermissionState.GrantWithGrant)
                    {
                        return true;
                    }
                }

                ObjectPermissionInfo[] objectPermissionInfoList = this.Server.EnumObjectPermissions("public");
                foreach (ObjectPermissionInfo objectPermission in objectPermissionInfoList)
                {
                    if (objectPermission.PermissionState == PermissionState.Grant || objectPermission.PermissionState == PermissionState.GrantWithGrant)
                    {
                        return true;
                    }
                }

                return returnValue;
            }
        }

        #endregion


        #region Refresh and Alter
        public override void Refresh()
        {
            this.Server.Information.Refresh();
            this.Server.Configuration.Refresh();
            this.Server.Settings.Refresh();
            this.Server.JobServer.Refresh();
        }

        public override void Alter()
        {
            this.Server.Configuration.Alter(true);
            this.Server.JobServer.Alter();
        }
        #endregion

    }
}

