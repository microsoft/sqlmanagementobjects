// Copyright (c) Microsoft.
// Licensed under the MIT license.
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using Microsoft.SqlServer.Management.Diagnostics;
using Microsoft.SqlServer.Management.Dmf;
using Microsoft.SqlServer.Management.Facets;
using Sfc = Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{

    /// <summary>
    /// Server installation facet
    /// </summary>
    /// 
    [CLSCompliantAttribute(false)]
    [TypeConverter(typeof(Sfc.LocalizableTypeConverter))]
    [Sfc.LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [Sfc.DisplayNameKey("IServerSetupFacet_Name")]
    [Sfc.DisplayDescriptionKey("IServerSetupFacet_Desc")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    public interface IServerSetupFacet : Sfc.IDmfFacet
    {
        /// <summary>
        /// The Service name that the Server instance runs under.
        /// </summary>
        [Sfc.DisplayNameKey("Server_ServiceNameName")]
        [Sfc.DisplayDescriptionKey("Server_ServiceNameDesc")]
        string ServiceName { get; }

        /// <summary>
        /// The Service name that the Server instance runs under.
        /// </summary>
        [Sfc.DisplayNameKey("Server_ServiceAccountName")]
        [Sfc.DisplayDescriptionKey("Server_ServiceAccountDesc")]
        string EngineServiceAccount { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_ServiceStartModeName")]
        [Sfc.DisplayDescriptionKey("Server_ServiceStartModeDesc")]
        ServiceStartMode ServiceStartMode { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_InstanceNameName")]
        [Sfc.DisplayDescriptionKey("Server_InstanceNameDesc")]
        string InstanceName { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("IServerSetupFacet_ServiceInstanceIdSuffixName")]
        [Sfc.DisplayDescriptionKey("IServerSetupFacet_ServiceInstanceIdSuffixDesc")]
        string ServiceInstanceIdSuffix { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_FilestreamLevelName")]
        [Sfc.DisplayDescriptionKey("Server_FilestreamLevelDesc")]
        FileStreamEffectiveLevel FilestreamLevel { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_FilestreamShareNameName")]
        [Sfc.DisplayDescriptionKey("Server_FilestreamShareNameDesc")]
        string FilestreamShareName { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("IServerConfigurationFacet_UserInstancesEnabledName")]
        [Sfc.DisplayDescriptionKey("IServerConfigurationFacet_UserInstancesEnabledDesc")]
        bool UserInstancesEnabled { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_CollationName")]
        [Sfc.DisplayDescriptionKey("Server_CollationDesc")]
        string Collation { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_SqlDomainGroupName")]
        [Sfc.DisplayDescriptionKey("Server_SqlDomainGroupDesc")]
        string SqlDomainGroup { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("IServerSetupFacet_WindowsUsersAndGroupsInSysadminRoleName")]
        [Sfc.DisplayDescriptionKey("IServerSetupFacet_WindowsUsersAndGroupsInSysadminRoleDesc")]
        string[] WindowsUsersAndGroupsInSysadminRole { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_LoginModeName")]
        [Sfc.DisplayDescriptionKey("Server_LoginModeDesc")]
        ServerLoginMode LoginMode { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_InstallDataDirectoryName")]
        [Sfc.DisplayDescriptionKey("Server_InstallDataDirectoryDesc")]
        string InstallDataDirectory { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_BackupDirectoryName")]
        [Sfc.DisplayDescriptionKey("Server_BackupDirectoryDesc")]
        string BackupDirectory { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_DefaultFileName")]
        [Sfc.DisplayDescriptionKey("Server_DefaultFileDesc")]
        string DefaultFile { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_DefaultLogName")]
        [Sfc.DisplayDescriptionKey("Server_DefaultLogDesc")]
        string DefaultLog { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("IServerSetupFacet_TempdbPrimaryFilePathName")]
        [Sfc.DisplayDescriptionKey("IServerSetupFacet_TempdbPrimaryFilePathDesc")]
        string TempdbPrimaryFilePath { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("IServerSetupFacet_TempdbLogPathName")]
        [Sfc.DisplayDescriptionKey("IServerSetupFacet_TempdbLogPathDesc")]
        string TempdbLogPath { get; }


        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("JobServer_ServiceStartModeName")]
        [Sfc.DisplayDescriptionKey("JobServer_ServiceStartModeDesc")]
        ServiceStartMode AgentStartMode { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("JobServer_ServiceAccountName")]
        [Sfc.DisplayDescriptionKey("JobServer_ServiceAccountDesc")]
        string AgentServiceAccount { get; }


        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("JobServer_AgentDomainGroupName")]
        [Sfc.DisplayDescriptionKey("JobServer_AgentDomainGroupDesc")]
        string AgentDomainGroup { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_NamedPipesEnabledName")]
        [Sfc.DisplayDescriptionKey("Server_NamedPipesEnabledDesc")]
        bool NamedPipesEnabled { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_TcpEnabledName")]
        [Sfc.DisplayDescriptionKey("Server_TcpEnabledDesc")]
        bool TcpEnabled { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_InstallSharedDirectoryName")]
        [Sfc.DisplayDescriptionKey("Server_InstallSharedDirectoryDesc")]
        string InstallSharedDirectory { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_BrowserStartModeName")]
        [Sfc.DisplayDescriptionKey("Server_InstallSharedDirectoryDesc")]
        ServiceStartMode BrowserStartMode { get; }

        /// <summary>
        /// 
        /// </summary>
        [Sfc.DisplayNameKey("Server_BrowserServiceAccountName")]
        [Sfc.DisplayDescriptionKey("Server_BrowserServiceAccountDesc")]
        string BrowserServiceAccount { get; }

    }



    /// <summary>
    /// Adapter for AS SAC facet
    /// </summary>
    public class ServerSetupAdapter : ServerAdapterBase, IDmfAdapter, IServerSetupFacet
    {
        #region Constructors
        public ServerSetupAdapter(Microsoft.SqlServer.Management.Smo.Server obj) 
            : base (obj)
        {
        }
        #endregion

        #region Computed Properties
        /// <summary>
        /// The instance Id for this Server  
        /// </summary>
        public string ServiceInstanceIdSuffix
        {
            get
            {
                // Remove the instance prefix, which will be MSSQL10. for Katmai, MSSQL9. for Yukon, and MSSQL8. for Shiloh
                string instanceId = this.Server.ServiceInstanceId;
                int firstDot = instanceId.IndexOf(".", StringComparison.OrdinalIgnoreCase);
                return instanceId.Substring(firstDot+1);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string[] WindowsUsersAndGroupsInSysadminRole
        {
            get
            {
                StringCollection sysadmins = new StringCollection();
                try
                {
                    string urnFormat = @"Server/Role[@Name='{0}']/Member[@LoginType={1} or @LoginType={2}]";
                    string urnReq = string.Format(SmoApplication.DefaultCulture, urnFormat, "sysadmin", ((int)LoginType.WindowsGroup), ((int)LoginType.WindowsUser));
                    Sfc.Request req = new Sfc.Request(urnReq);
                    foreach (DataRow dr in this.Server.ExecutionManager.GetEnumeratorData(req).Rows)
                    {
                        sysadmins.Add(Convert.ToString(dr["Name"], SmoApplication.DefaultCulture));
                    }
                }
                catch (Exception e)
                {
                    SqlSmoObject.FilterException(e);

                    throw new FailedOperationException(ExceptionTemplates.EnumMembers, this, e);
                }

                string[] sysadminsArray = new string[sysadmins.Count];
                sysadmins.CopyTo(sysadminsArray, 0);
                return sysadminsArray;
            }
        }

        /// <summary>
        /// The PrimaryFilePath for the tempdb database.
        /// </summary>
        public string TempdbPrimaryFilePath
        {
            get
            {
                return this.Server.Databases["tempdb"].PrimaryFilePath;
            }
        }

        /// <summary>
        /// The log file path for the tempdb database
        /// </summary>
        public string TempdbLogPath 
        {
            get
            {
                return System.IO.Path.GetDirectoryName(this.Server.Databases["tempdb"].LogFiles[0].FileName);
            }
        }

        /// <summary>
        /// The JobServer ServiceStartMode
        /// </summary>
        public ServiceStartMode AgentStartMode
        {
            get
            {
                return this.Server.JobServer.ServiceStartMode;
            }
        }

        /// <summary>
        /// The JobServer ServiceAccount
        /// </summary>
        public string AgentServiceAccount
        {
            get
            {
                return this.Server.JobServer.ServiceAccount;
            }
        }


        /// <summary>
        /// The JobServer AgentDomainGroup
        /// </summary>
        public string AgentDomainGroup
        {
            get
            {
                return this.Server.JobServer.AgentDomainGroup;
            }
        }

        /// <summary>
        /// The ServiceAccount for the Sql Server Engine
        /// </summary>
        public string EngineServiceAccount
        {
            get
            {
                return this.Server.ServiceAccount;
            }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        public override void Refresh()
        {
            base.Refresh();
            this.Server.Databases["tempdb"].Refresh();
            try
            {
                this.Server.JobServer.Refresh();
            }
            catch (UnsupportedFeatureException)
            {

            }
        }
    }
}
