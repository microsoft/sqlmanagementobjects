// Copyright (c) Microsoft.
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
    [DisplayNameKey("IServerInformation_Name")]
    [DisplayDescriptionKey("IServerInformation_Desc")]
    [RootFacetAttribute (typeof (Server))]
    public interface IServerInformation : IDmfFacet
    {
        [DisplayNameKey("Server_CollationName")]
        [DisplayDescriptionKey("Server_CollationDesc")]
        String Collation { get;}

        [DisplayNameKey("Server_EditionName")]
        [DisplayDescriptionKey("Server_EditionDesc")]
        String Edition { get;}

        [DisplayNameKey("Server_Name")]
        [DisplayDescriptionKey("Server_Desc")]
        String ErrorLogPath { get;}

        [DisplayNameKey("Server_IsCaseSensitiveName")]
        [DisplayDescriptionKey("Server_IsCaseSensitiveDesc")]
        Boolean IsCaseSensitive { get;}

        [DisplayNameKey("Server_IsClusteredName")]
        [DisplayDescriptionKey("Server_IsClusteredDesc")]
        Boolean IsClustered { get;}

        [DisplayNameKey("Server_IsFullTextInstalledName")]
        [DisplayDescriptionKey("Server_IsFullTextInstalledDesc")]
        Boolean IsFullTextInstalled { get;}

        [DisplayNameKey("Server_IsPolyBaseInstalledName")]
        [DisplayDescriptionKey("Server_IsPolyBaseInstalledDesc")]
        Boolean IsPolyBaseInstalled { get; }

        [DisplayNameKey("Server_IsSingleUserName")]
        [DisplayDescriptionKey("Server_IsSingleUserDesc")]
        Boolean IsSingleUser { get;}

        [DisplayNameKey("Server_LanguageName")]
        [DisplayDescriptionKey("Server_LanguageDesc")]
        String Language { get;}

        [DisplayNameKey("Server_MasterDBLogPathName")]
        [DisplayDescriptionKey("Server_MasterDBLogPathDesc")]
        String MasterDBLogPath { get;}

        [DisplayNameKey("Server_MasterDBPathName")]
        [DisplayDescriptionKey("Server_MasterDBPathDesc")]
        String MasterDBPath { get;}

        [DisplayNameKey("Server_MaxPrecisionName")]
        [DisplayDescriptionKey("Server_MaxPrecisionDesc")]
        Byte MaxPrecision { get;}

        [DisplayNameKey("Server_NetNameName")]
        [DisplayDescriptionKey("Server_NetNameDesc")]
        String NetName { get;}

        [DisplayNameKey("Server_OSVersionName")]
        [DisplayDescriptionKey("Server_OSVersionDesc")]
        String OSVersion { get;}

        [DisplayNameKey("Server_PhysicalMemoryName")]
        [DisplayDescriptionKey("Server_PhysicalMemoryDesc")]
        Int32 PhysicalMemory { get;}

        [DisplayNameKey("Server_PlatformName")]
        [DisplayDescriptionKey("Server_PlatformDesc")]
        String Platform { get;}

        [DisplayNameKey("Server_ProcessorsName")]
        [DisplayDescriptionKey("Server_ProcessorsDesc")]
        Int32 Processors { get;}

        [DisplayNameKey("Server_ProductName")]
        [DisplayDescriptionKey("Server_ProductDesc")]
        String Product { get;}

        [DisplayNameKey("Server_ProductLevelName")]
        [DisplayDescriptionKey("Server_ProductLevelDesc")]
        String ProductLevel { get;}

        [DisplayNameKey("Server_RootDirectoryName")]
        [DisplayDescriptionKey("Server_RootDirectoryDesc")]
        String RootDirectory { get;}

        [DisplayNameKey("Server_VersionStringName")]
        [DisplayDescriptionKey("Server_VersionStringDesc")]
        String VersionString { get;}

        [DisplayNameKey("Server_EngineEditionName")]
        [DisplayDescriptionKey("Server_EngineEditionDesc")]
        Edition EngineEdition { get;}

        [DisplayNameKey("Server_VersionMajorName")]
        [DisplayDescriptionKey("Server_VersionMajorDesc")]
        int VersionMajor { get;}

        [DisplayNameKey("Server_VersionMinorName")]
        [DisplayDescriptionKey("Server_VersionMinorDesc")]
        int VersionMinor { get;}

        [DisplayNameKey("Server_BuildClrVersionStringName")]
        [DisplayDescriptionKey("Server_BuildClrVersionStringDesc")]
        string BuildClrVersionString { get; }

        [DisplayNameKey("Server_BuildNumberName")]
        [DisplayDescriptionKey("Server_BuildNumberDesc")]
        int BuildNumber { get; }

        [DisplayNameKey("Server_CollationIDName")]
        [DisplayDescriptionKey("Server_CollationIDDesc")]
        int CollationID { get; }

        [DisplayNameKey("Server_ComparisonStyleName")]
        [DisplayDescriptionKey("Server_ComparisonStyleDesc")]
        int ComparisonStyle { get; }

        [DisplayNameKey("Server_ComputerNamePhysicalNetBIOSName")]
        [DisplayDescriptionKey("Server_ComputerNamePhysicalNetBIOSDesc")]
        string ComputerNamePhysicalNetBIOS { get; }

        [DisplayNameKey("Server_ResourceLastUpdateDateTimeName")]
        [DisplayDescriptionKey("Server_ResourceLastUpdateDateTimeDesc")]
        DateTime ResourceLastUpdateDateTime { get; }
        
        [DisplayNameKey("Server_ResourceVersionStringName")]
        [DisplayDescriptionKey("Server_ResourceVersionStringDesc")]
        string ResourceVersionString { get; }

        [DisplayNameKey("Server_SqlCharSetName")]
        [DisplayDescriptionKey("Server_SqlCharSetDesc")]
        short SqlCharSet { get; }

        [DisplayNameKey("Server_SqlCharSetNameName")]
        [DisplayDescriptionKey("Server_SqlCharSetNameDesc")]
        string SqlCharSetName { get; }

        [DisplayNameKey("Server_SqlSortOrderName")]
        [DisplayDescriptionKey("Server_SqlSortOrderDesc")]
        short SqlSortOrder { get; }

        [DisplayNameKey("Server_SqlSortOrderNameName")]
        [DisplayDescriptionKey("Server_SqlSortOrderNameDesc")]
        string SqlSortOrderName { get; }

        [DisplayNameKey("Server_IsHadrEnabledName")]
        [DisplayDescriptionKey("Server_IsHadrEnabledDesc")]
        Boolean IsHadrEnabled { get; }

		[DisplayNameKey("Server_IsXTPSupported")]
        [DisplayDescriptionKey("Server_IsXTPSupportedDesc")]
        Boolean IsXTPSupported { get; }

    }
}

