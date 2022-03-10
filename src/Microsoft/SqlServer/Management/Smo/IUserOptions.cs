// Copyright (c) Microsoft.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [Facets.StateChangeEvent("CREATE_USER", "USER", "SQL USER")]
    [Facets.StateChangeEvent("ALTER_USER", "USER", "SQL USER")]
    [Facets.StateChangeEvent("CREATE_USER", "USER", "WINDOWS USER")]
    [Facets.StateChangeEvent("ALTER_USER", "USER", "WINDOWS USER")]
    [Facets.StateChangeEvent("CREATE_USER", "USER", "GROUP USER")]
    [Facets.StateChangeEvent("ALTER_USER", "USER", "GROUP USER")]
    [Facets.StateChangeEvent("CREATE_USER", "USER", "CERTIFICATE USER")]
    [Facets.StateChangeEvent("ALTER_USER", "USER", "CERTIFICATE USER")]
    [Facets.StateChangeEvent("CREATE_USER", "USER", "ASYMMETRIC KEY USER")]
    [Facets.StateChangeEvent("ALTER_USER", "USER", "ASYMMETRIC KEY USER")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [DisplayNameKey("IUserOptions_Name")]
    [DisplayDescriptionKey("IUserOptions_Desc")]
    public interface IUserOptions : IDmfFacet
    {

        [DisplayNameKey("User_AsymmetricKeyName")]
        [DisplayDescriptionKey("User_AsymmetricKeyDesc")]
        String AsymmetricKey { get; }

        [DisplayNameKey("User_CertificateName")]
        [DisplayDescriptionKey("User_CertificateDesc")]
        String Certificate { get; }

        [DisplayNameKey("User_CreateDateName")]
        [DisplayDescriptionKey("User_CreateDateDesc")]
        DateTime CreateDate { get; }

        [DisplayNameKey("User_DefaultSchemaName")]
        [DisplayDescriptionKey("User_DefaultSchemaDesc")]
        String DefaultSchema { get; set; }

        [DisplayNameKey("User_IDName")]
        [DisplayDescriptionKey("User_IDDesc")]
        Int32 ID { get; }

        [DisplayNameKey("User_IsSystemObjectName")]
        [DisplayDescriptionKey("User_IsSystemObjectDesc")]
        Boolean IsSystemObject { get; }

        [DisplayNameKey("User_LoginName")]
        [DisplayDescriptionKey("User_LoginDesc")]
        String Login { get; }

        [DisplayNameKey("User_LoginTypeName")]
        [DisplayDescriptionKey("User_LoginTypeDesc")]
        Microsoft.SqlServer.Management.Smo.LoginType LoginType { get; }

        [DisplayNameKey("NamedSmoObject_NameName")]
        [DisplayDescriptionKey("NamedSmoObject_NameDesc")]
        String Name { get; }

        [DisplayNameKey("User_SidName")]
        [DisplayDescriptionKey("User_SidDesc")]
        Byte[] Sid { get; }

        [DisplayNameKey("User_UserTypeName")]
        [DisplayDescriptionKey("User_UserTypeDesc")]
        Microsoft.SqlServer.Management.Smo.UserType UserType { get; }

    }
}
