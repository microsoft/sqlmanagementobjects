# Spec 0003: Workload Group TempDB Resource Governance in CREATE Scripts

**Status:** Implemented  
**Created:** 2025-11-26  
**Implemented:** Commit ce35e55b01c6a51615d708fe3b3d8d8f80790a18  
**Authors:** Tong Wu  

## 1. Summary

Include TempDB resource governance parameters (`group_max_tempdb_data_mb` and `group_max_tempdb_data_percent`) in `CREATE WORKLOAD GROUP` scripts when their values are explicitly set to `-1` (representing `NULL`). Previously, these parameters were only included in `ALTER` scripts; this change ensures parity between CREATE and ALTER script generation.

## 2. Motivation

TempDB resource governance parameters control the maximum amount of TempDB space a workload group can consume. When creating a workload group via SMO scripting:

- **Before this change:** The `-1` value (representing `NULL`/unlimited) was excluded from CREATE scripts but included in ALTER scripts, leading to inconsistent scripting behavior.
- **After this change:** Both CREATE and ALTER scripts consistently include these parameters when set to `-1`, scripted as `group_max_tempdb_data_mb=null` and `group_max_tempdb_data_percent=null`.

This consistency is important for script generation tools like SSMS Generate Scripts, where users expect CREATE scripts to fully represent the object state.

## 3. Target SQL Server Versions

| Version | Support |
|---------|---------|
| SQL Server 2025 (v17.x) | ✅ Fully supported |
| Azure SQL Managed Instance | ✅ Supported |
| SQL Server 2022 and earlier | ❌ Not applicable |

**Catalog View:** `sys.resource_governor_workload_groups`  
**Relevant Columns:** `group_max_tempdb_data_mb`, `group_max_tempdb_data_percent`

## 4. DDL Syntax

### 4.1 CREATE WORKLOAD GROUP

```sql
CREATE WORKLOAD GROUP [group_name] WITH(
    group_max_requests=0,
    importance=Medium,
    request_max_cpu_time_sec=0,
    request_max_memory_grant_percent=25,
    request_memory_grant_timeout_sec=0,
    max_dop=0,
    group_max_tempdb_data_mb=null,
    group_max_tempdb_data_percent=null) USING [pool_name], EXTERNAL [default]
GO
```

### 4.2 ALTER WORKLOAD GROUP

```sql
ALTER WORKLOAD GROUP [group_name] WITH(
    group_max_requests=0,
    importance=Medium,
    request_max_cpu_time_sec=0,
    request_max_memory_grant_percent=25,
    request_memory_grant_timeout_sec=0,
    max_dop=0,
    group_max_tempdb_data_mb=null,
    group_max_tempdb_data_percent=null)
GO
```

## 5. SMO Implementation

### 5.1 Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `GroupMaximumTempdbDataMB` | `double` | `-1` | Maximum TempDB data in MB. `-1` means NULL/unlimited. |
| `GroupMaximumTempdbDataPercent` | `double` | `-1` | Maximum TempDB data as percentage. `-1` means NULL/unlimited. |

### 5.2 Scripting Behavior

| Value | CREATE Script | ALTER Script (when dirty) |
|-------|---------------|---------------------------|
| `-1` | `group_max_tempdb_data_mb=null`, `group_max_tempdb_data_percent=null` | `group_max_tempdb_data_mb=null`, `group_max_tempdb_data_percent=null` |
| `> 0` | `group_max_tempdb_data_mb=<value>`, `group_max_tempdb_data_percent=<value>` | `group_max_tempdb_data_mb=<value>`, `group_max_tempdb_data_percent=<value>` |
| Not set (property not dirty) | Not included | Not included |

### 5.3 Code Location

- **Scripter:** [WorkloadGroupBase.cs](../src/Microsoft/SqlServer/Management/Smo/WorkloadGroupBase.cs)
- **XML Metadata:** [WorkloadGroup.xml](../src/Microsoft/SqlServer/Management/SqlEnum/xml/WorkloadGroup.xml)

## 6. Formatting

The implementation ensures proper formatting with line breaks between tempdb parameters in the WITH clause for readability:

```sql
WITH(group_max_requests=0,
                importance=Medium,
                request_max_cpu_time_sec=0,
                request_max_memory_grant_percent=25,
                request_memory_grant_timeout_sec=0,
                max_dop=0,
                group_max_tempdb_data_mb=null,
                group_max_tempdb_data_percent=null)
```

## 7. Testing

Functional tests verify:
1. `-1` values are scripted as `null` in CREATE statements
2. `-1` values are scripted as `null` in ALTER statements (when dirty)
3. Scripts include proper `USING` clause placement after WITH clause

**Test Location:** [WorkloadSmoTests.cs](../src/FunctionalTest/Smo/GeneralFunctionality/WorkloadSmoTests.cs)

## 8. Documentation References

- [Resource Governor Workload Group](https://learn.microsoft.com/sql/relational-databases/resource-governor/resource-governor-workload-group)
- [CREATE WORKLOAD GROUP (Transact-SQL)](https://learn.microsoft.com/sql/t-sql/statements/create-workload-group-transact-sql)
- [ALTER WORKLOAD GROUP (Transact-SQL)](https://learn.microsoft.com/sql/t-sql/statements/alter-workload-group-transact-sql)
- [sys.resource_governor_workload_groups](https://learn.microsoft.com/sql/relational-databases/system-catalog-views/sys-resource-governor-workload-groups-transact-sql)
