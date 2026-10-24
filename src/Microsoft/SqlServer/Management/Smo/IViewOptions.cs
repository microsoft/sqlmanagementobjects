// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [Facets.StateChangeEvent("CREATE_VIEW", "VIEW")]
    [Facets.StateChangeEvent("ALTER_VIEW", "VIEW")]
    [Facets.StateChangeEvent("RENAME", "VIEW")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "VIEW")] // For Owner
    [Facets.StateChangeEvent("ALTER_SCHEMA", "VIEW")] // For Schema
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [DisplayNameKey("IViewOptions_Name")]
    [DisplayDescriptionKey("IViewOptions_Desc")]
    public interface IViewOptions :IDmfFacet
    {
        [DisplayNameKey("View_AnsiNullsStatusName")]
        [DisplayDescriptionKey("View_AnsiNullsStatusDesc")]
        Boolean AnsiNullsStatus { get; }

        [DisplayNameKey("View_CreateDateName")]
        [DisplayDescriptionKey("View_CreateDateDesc")]
        System.DateTime CreateDate { get; }

        [DisplayNameKey("View_IDName")]
        [DisplayDescriptionKey("View_IDDesc")]
        System.Int32 ID { get; }

        [DisplayNameKey("View_IsEncryptedName")]
        [DisplayDescriptionKey("View_IsEncryptedDesc")]
        System.Boolean IsEncrypted { get; }

        [DisplayNameKey("View_IsSchemaBoundName")]
        [DisplayDescriptionKey("View_IsSchemaBoundDesc")]
        System.Boolean IsSchemaBound { get; }

        [DisplayNameKey("View_IsSchemaOwnedName")]
        [DisplayDescriptionKey("View_IsSchemaOwnedDesc")]
        System.Boolean IsSchemaOwned { get; }

        [DisplayNameKey("View_IsSystemObjectName")]
        [DisplayDescriptionKey("View_IsSystemObjectDesc")]
        System.Boolean IsSystemObject { get; }

        [DisplayNameKey("NamedSmoObject_NameName")]
        [DisplayDescriptionKey("NamedSmoObject_NameDesc")]
        System.String Name { get; }

        [DisplayNameKey("View_OwnerName")]
        [DisplayDescriptionKey("View_OwnerDesc")]
        String Owner { get; }

        [DisplayNameKey("ScriptSchemaObjectBase_SchemaName")]
        [DisplayDescriptionKey("ScriptSchemaObjectBase_SchemaDesc")]
        String Schema { get; }

        [DisplayNameKey("View_QuotedIdentifierStatusName")]
        [DisplayDescriptionKey("View_QuotedIdentifierStatusDesc")]
        Boolean QuotedIdentifierStatus { get; }

        [DisplayNameKey("View_ReturnsViewMetadataName")]
        [DisplayDescriptionKey("View_ReturnsViewMetadataDesc")]
        System.Boolean ReturnsViewMetadata { get; }

    }
}
