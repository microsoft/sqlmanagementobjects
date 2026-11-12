// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule)]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [DisplayNameKey("IServerSettings_Name")]
    [DisplayDescriptionKey("IServerSettings_Desc")]
    [RootFacetAttribute (typeof (Server))]
    public interface IServerSettings : IDmfFacet
    {
        [DisplayNameKey("Server_AuditLevelName")]
        [DisplayDescriptionKey("Server_AuditLevelDesc")]
        AuditLevel AuditLevel { get; set;}

        [DisplayNameKey("Server_BackupDirectoryName")]
        [DisplayDescriptionKey("Server_BackupDirectoryDesc")]
        String BackupDirectory { get; set;}

        [DisplayNameKey("Server_DefaultFileName")]
        [DisplayDescriptionKey("Server_DefaultFileDesc")]
        String DefaultFile { get; set;}

        [DisplayNameKey("Server_DefaultLogName")]
        [DisplayDescriptionKey("Server_DefaultLogDesc")]
        String DefaultLog { get; set;}

        [DisplayNameKey("Server_LoginModeName")]
        [DisplayDescriptionKey("Server_LoginModeDesc")]
        ServerLoginMode LoginMode { get; }

        [DisplayNameKey("Server_MailProfileName")]
        [DisplayDescriptionKey("Server_MailProfileDesc")]
        String MailProfile { get; set;}

        [DisplayNameKey("Server_NumberOfLogFilesName")]
        [DisplayDescriptionKey("Server_NumberOfLogFilesDesc")]
        Int32 NumberOfLogFiles { get; set;}

        [DisplayNameKey("Server_PerfMonModeName")]
        [DisplayDescriptionKey("Server_PerfMonModeDesc")]
        PerfMonMode PerfMonMode { get; set;}

        [DisplayNameKey("Server_TapeLoadWaitTimeName")]
        [DisplayDescriptionKey("Server_TapeLoadWaitTimeDesc")]
        Int32 TapeLoadWaitTime { get; set;}
    }
}
