# Spec 0006: Fabric Data Warehouse External Table Support

**Status:** Implemented  
**Created:** 2026-03-17  
**Authors:** Barcelona DW team  

## 1. Summary

Add SMO support for creating, scripting, and managing external tables on Microsoft Fabric Data Warehouse, including schema inference (column-less tables), NOT ENFORCED constraints, and correct property enumeration.

## 2. Motivation

Fabric Data Warehouse introduces external tables backed by OneLake data sources. These external tables differ from other engines in several ways:

- **Schema inference:** Fabric DW can infer column definitions from the underlying data source, allowing tables to be created without explicit column definitions.
- **NOT ENFORCED constraints:** All PK, UQ, and FK constraints on Fabric DW are informational only — the engine does not validate them.
- **No `FileFormatName` property:** Fabric DW external tables use Delta format implicitly; the file format is not user-specified.
- **No `sys.external_file_formats`:** The catalog view is not supported in Fabric DW.
- **`Location`:** Pointing to **Onelake URLs**.

Without these changes, SMO cannot create, script, or enumerate external tables on Fabric DW.

## 3. Target Platforms

| Platform | Support |
|----------|---------|
| Fabric Data Warehouse (`DatabaseEngineEdition.SqlOnDemand` + `IsFabricConnection`) | ✅ Fully supported |

**Distinguishing Fabric DW from Synapse Serverless:** Both report `DatabaseEngineEdition.SqlOnDemand`. Fabric DW is identified by additionally checking `ExecutionManager.IsFabricConnection` (runtime) or `ScriptingPreferences.TargetIsFabricConnection` (scripting).

## 4. DDL Syntax

### 4.1 CREATE EXTERNAL TABLE (with columns)

```sql
CREATE EXTERNAL TABLE [dbo].[MyExtTable]
(
    [Id] INT NOT NULL,
    [Name] VARCHAR(100) NULL
)
WITH (
    DATA_SOURCE = [SystemDefinedOneLakeShortcut],
    LOCATION = N'https://<onelake-host>/<workspaceId>/<artifactId>/<TablePath>'
)
```

### 4.2 CREATE EXTERNAL TABLE (schema inference, no columns)

```sql
CREATE EXTERNAL TABLE [dbo].[MyExtTable]
WITH (
    DATA_SOURCE = [SystemDefinedOneLakeShortcut],
     LOCATION = N'https://<onelake-host>/<workspaceId>/<artifactId>/<TablePath>'
)
```

### 4.3 NOT ENFORCED Constraints

```sql
ALTER TABLE [dbo].[MyExtTable] ADD CONSTRAINT [PK_MyExtTable]
    PRIMARY KEY NONCLUSTERED ([Id]) NOT ENFORCED

ALTER TABLE [dbo].[MyExtTable] ADD CONSTRAINT [FK_MyExtTable]
    FOREIGN KEY ([ParentId]) REFERENCES [dbo].[ParentTable] ([Id])
    NOT ENFORCED

ALTER TABLE [dbo].[MyExtTable] ADD CONSTRAINT [UQ_MyExtTable]
    UNIQUE NONCLUSTERED ([Name]) NOT ENFORCED
```

## 5. SMO Implementation

### 5.1 ScriptingPreferences

| Property / Method | Type | Description |
|-------------------|------|-------------|
| `TargetIsFabricConnection` | `bool` | Set in `SetTargetServerInfo()` from `ExecutionManager.IsFabricConnection` |
| `TargetEngineIsFabricDw()` | `bool` | Returns `true` when `TargetIsFabricConnection && TargetDatabaseEngineEdition == SqlOnDemand` |

### 5.2 Schema Inference (tablebase.cs)

- **Validation:** Column count check exempts Fabric DW external tables (`IsFabricConnection && SqlOnDemand`).
- **Scripting:** `GetExternalTableCreationScript` skips the column list and closing parenthesis when `Columns.Count == 0`.

### 5.3 NOT ENFORCED Constraints

**ForeignKeyBase.cs:**
- `NOT ENFORCED` appended to FK script when `sp.TargetEngineIsFabricDw()`.

**IndexScripter.cs (PK/UQ constraints):**
- `ScriptIndexDetails`: Scripts `NOT ENFORCED` when target is Fabric DW.
- `ScriptIndexOptions`: Skips index options (`PAD_INDEX`, `IGNORE_DUP_KEY`, etc.) for this engine.

### 5.4 Column Nullability (ColumnBase.cs)

Fabric DW external table columns support explicit `NULL`/`NOT NULL`. Added `IsFabricServer` check so nullability is scripted for Fabric DW external tables.


### 5.5 Code Locations

- **ScriptingPreferences:** [ScriptingPreferences.cs](../src/Microsoft/SqlServer/Management/Smo/ScriptingPreferences.cs)
- **Table scripting:** [tablebase.cs](../src/Microsoft/SqlServer/Management/Smo/tablebase.cs)
- **FK scripting:** [ForeignKeyBase.cs](../src/Microsoft/SqlServer/Management/Smo/ForeignKeyBase.cs)
- **PK/UQ scripting:** [IndexScripter.cs](../src/Microsoft/SqlServer/Management/Smo/IndexScripter.cs)
- **Column scripting:** [ColumnBase.cs](../src/Microsoft/SqlServer/Management/Smo/ColumnBase.cs)
- **XML Metadata:** [table.xml](../src/Microsoft/SqlServer/Management/SqlEnum/xml/table.xml)

## 6. Testing

### 6.1 Test Class

`FabricWarehouseTableTests` in [Table_SmoTestSuite.cs](../src/FunctionalTest/SmoInternal/ScriptingTests/Table_SmoTestSuite.cs)

Attributes: `[SqlRequiredFeature(SqlFeature.Fabric)]`, `[SupportedServerVersionRange(Edition = DatabaseEngineEdition.SqlOnDemand)]`

### 6.2 Test Scenarios

| Test | Scenario |
|------|----------|
| `ExternalTable_FabricWarehouse_Create_Script_Drop` | Basic external table lifecycle |
| `ExternalTable_FabricWarehouse_Nullability_Create_Script_Drop` | NULL / NOT NULL column handling |
| `ExternalTable_FabricWarehouse_PrimaryKey_Script_And_Drop` | PK constraint with NOT ENFORCED |
| `ExternalTable_FabricWarehouse_ForeignKey_Script_And_Drop` | FK constraint with NOT ENFORCED |
| `ExternalTable_FabricWarehouse_UniqueConstraint_Script_And_Drop` | UQ constraint with NOT ENFORCED |
| `ExternalTable_FabricWarehouse_ColumnCollation_Create_Script_Drop` | Column collation round-trip |
| `ExternalTable_FabricWarehouse_DynamicDataMasking_Create_Script_Drop` | Dynamic data masking on columns |
| `ExternalTable_FabricWarehouse_DataTypes_Create_Script_Drop` | All supported Fabric DW data types |
| `ExternalTable_FabricWarehouse_Enumerate_Verify_Properties` | Table enumeration and property discovery |
| `ExternalTable_FabricWarehouse_AlterColumn_DataMask` | Add/remove data mask via ALTER |
| `ExternalTable_FabricWarehouse_Create_NoColumns_SchemaInference` | Schema inference (column-less table) |

### 6.3 Test Infrastructure

- `DbSetup_FabricWarehouse.sql`: Creates `SourceTable` and `SourceTable_DataTypes` as source data for external table tests.
- `FabricDatabaseManager`: Added `GetWorkspaceId()` and `GetWarehouseArtifactId()` for building OneLake location URLs.
- `FabricWorkspaceDescriptor`: Exposed `WorkspaceId` and `GetWarehouseArtifactId()`.

### 6.4 Test Environment

- Requires Fabric workspace with external table feature switches enabled.
- Tests run in the **SMO Tests Weekly** pipeline (Fabric tests are disabled in PR validation).
- Requires `fab.exe` CLI authentication (`fab auth login`).