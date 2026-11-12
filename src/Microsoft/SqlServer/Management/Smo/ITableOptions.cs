// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.SqlServer.Management.Sdk.Sfc;

#pragma warning disable 1590,1591,1592,1573,1571,1570,1572,1587
namespace Microsoft.SqlServer.Management.Smo
{
    [CLSCompliantAttribute(false)]
    [Facets.StateChangeEvent("CREATE_TABLE", "TABLE")]
    [Facets.StateChangeEvent("ALTER_TABLE", "TABLE")]
    [Facets.StateChangeEvent("RENAME", "TABLE")]
    [Facets.StateChangeEvent("ALTER_AUTHORIZATION_DATABASE", "TABLE")] // For Owner
    [Facets.StateChangeEvent("ALTER_SCHEMA", "TABLE")] // For Schema
    [Facets.EvaluationMode(Dmf.AutomatedPolicyEvaluationMode.CheckOnChanges | Dmf.AutomatedPolicyEvaluationMode.CheckOnSchedule | Dmf.AutomatedPolicyEvaluationMode.Enforce)]
    [LocalizedPropertyResources("Microsoft.SqlServer.Management.Smo.LocalizableResources")]
    [TypeConverter(typeof(LocalizableTypeConverter))]
    [DisplayNameKey("ITableOptions_Name")]
    [DisplayDescriptionKey("ITableOptions_Desc")]
    public interface ITableOptions : IDmfFacet
    {

        [DisplayNameKey("Table_AnsiNullsStatusName")]
        [DisplayDescriptionKey("Table_AnsiNullsStatusDesc")]
        Boolean AnsiNullsStatus { get; set; }

        [DisplayNameKey("Table_ChangeTrackingEnabledName")]
        [DisplayDescriptionKey("Table_ChangeTrackingEnabledDesc")]
        Boolean ChangeTrackingEnabled { get; set; }

        [DisplayNameKey("Table_CreateDateName")]
        [DisplayDescriptionKey("Table_CreateDateDesc")]
        DateTime CreateDate { get; }

        [DisplayNameKey("Table_FakeSystemTableName")]
        [DisplayDescriptionKey("Table_FakeSystemTableDesc")]
        Boolean FakeSystemTable { get; }

        [DisplayNameKey("Table_IDName")]
        [DisplayDescriptionKey("Table_IDDesc")]
        Int32 ID { get; }

        [DisplayNameKey("Table_IsSchemaOwnedName")]
        [DisplayDescriptionKey("Table_IsSchemaOwnedDesc")]
        Boolean IsSchemaOwned { get; }

        [DisplayNameKey("Table_IsSystemObjectName")]
        [DisplayDescriptionKey("Table_IsSystemObjectDesc")]
        Boolean IsSystemObject { get; }

        [DisplayNameKey("Table_LockEscalationName")]
        [DisplayDescriptionKey("Table_LockEscalationDesc")]
        Microsoft.SqlServer.Management.Smo.LockEscalationType LockEscalation { get; set; }

        [DisplayNameKey("NamedSmoObject_NameName")]
        [DisplayDescriptionKey("NamedSmoObject_NameDesc")]
        String Name { get; }

        [DisplayNameKey("Table_OwnerName")]
        [DisplayDescriptionKey("Table_OwnerDesc")]
        String Owner { get; set; }

        [DisplayNameKey("Table_QuotedIdentifierStatusName")]
        [DisplayDescriptionKey("Table_QuotedIdentifierStatusDesc")]
        Boolean QuotedIdentifierStatus { get; }

        [DisplayNameKey("Table_RemoteDataArchiveEnabledName")]
        [DisplayDescriptionKey("Table_RemoteDataArchiveEnabledDesc")]
        Boolean RemoteDataArchiveEnabled { get; set; }

        [DisplayNameKey("Table_RemoteDataArchiveDataMigrationStateName")]
        [DisplayDescriptionKey("Table_RemoteDataArchiveDataMigrationStateDesc")]
        RemoteDataArchiveMigrationState RemoteDataArchiveDataMigrationState { get; set; }

        [DisplayNameKey("Table_RemoteTableNameName")]
        [DisplayDescriptionKey("Table_RemoteTableNameDesc")]
        String RemoteTableName { get; }

        [DisplayNameKey("Table_RemoteTableProvisionedName")]
        [DisplayDescriptionKey("Table_RemoteTableProvisionedDesc")]
        Boolean RemoteTableProvisioned { get; }

        [DisplayNameKey("Table_ReplicatedName")]
        [DisplayDescriptionKey("Table_ReplicatedDesc")]
        Boolean Replicated { get; }

        [DisplayNameKey("ScriptSchemaObjectBase_SchemaName")]
        [DisplayDescriptionKey("ScriptSchemaObjectBase_SchemaDesc")]
        String Schema { get; }

        [DisplayNameKey("Table_TrackColumnsUpdatedEnabledName")]
        [DisplayDescriptionKey("Table_TrackColumnsUpdatedEnabledDesc")]
        Boolean TrackColumnsUpdatedEnabled { get; set; }

    }
}
