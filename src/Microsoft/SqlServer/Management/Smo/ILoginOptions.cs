// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [Facets.StateChangeEvent("CREATE_LOGIN", "LOGIN")]
    [Facets.StateChangeEvent("ALTER_LOGIN", "LOGIN")]
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [DisplayNameKey("ILoginOptions_Name")]
    [DisplayDescriptionKey("ILoginOptions_Desc")]
    public interface ILoginOptions : IDmfFacet
    {

        [DisplayNameKey("Login_AsymmetricKeyName")]
        [DisplayDescriptionKey("Login_AsymmetricKeyDesc")]
        string AsymmetricKey { get; set; }

        [DisplayNameKey("Login_CertificateName")]
        [DisplayDescriptionKey("Login_CertificateDesc")]
        string Certificate { get; set; }

        [DisplayNameKey("Login_CreateDateName")]
        [DisplayDescriptionKey("Login_CreateDateDesc")]
        System.DateTime CreateDate { get; }

        [DisplayNameKey("Login_CredentialName")]
        [DisplayDescriptionKey("Login_CredentialDesc")]
        string Credential { get; set; }

        [DisplayNameKey("Login_DefaultDatabaseName")]
        [DisplayDescriptionKey("Login_DefaultDatabaseDesc")]
        string DefaultDatabase { get; set; }

        [DisplayNameKey("Login_IDName")]
        [DisplayDescriptionKey("Login_IDDesc")]
        int ID { get; }

        [DisplayNameKey("Login_IsDisabledName")]
        [DisplayDescriptionKey("Login_IsDisabledDesc")]
        bool IsDisabled { get; }

        [DisplayNameKey("Login_IsLockedName")]
        [DisplayDescriptionKey("Login_IsLockedDesc")]
        bool IsLocked { get; }

        [DisplayNameKey("Login_IsSystemObjectName")]
        [DisplayDescriptionKey("Login_IsSystemObjectDesc")]
        bool IsSystemObject { get; }

        [DisplayNameKey("Login_LanguageName")]
        [DisplayDescriptionKey("Login_LanguageDesc")]
        string Language { get; set; }

        [DisplayNameKey("Login_LanguageAliasName")]
        [DisplayDescriptionKey("Login_LanguageAliasDesc")]
        string LanguageAlias { get; }

        [DisplayNameKey("Login_LoginTypeName")]
        [DisplayDescriptionKey("Login_LoginTypeDesc")]
        LoginType LoginType { get; }

        [DisplayNameKey("Login_MustChangePasswordName")]
        [DisplayDescriptionKey("Login_MustChangePasswordDesc")]
        bool MustChangePassword { get; }

        [DisplayNameKey("NamedSmoObject_NameName")]
        [DisplayDescriptionKey("NamedSmoObject_NameDesc")]
        string Name { get; set; }

        [DisplayNameKey("Login_PasswordExpirationEnabledName")]
        [DisplayDescriptionKey("Login_PasswordExpirationEnabledDesc")]
        bool PasswordExpirationEnabled { get; set; }

        [DisplayNameKey("Login_PasswordPolicyEnforcedName")]
        [DisplayDescriptionKey("Login_PasswordPolicyEnforcedDesc")]
        bool PasswordPolicyEnforced { get; set; }
    }
}
