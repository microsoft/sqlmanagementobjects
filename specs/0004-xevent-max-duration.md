# Spec 0004: XEvent Session MAX_DURATION Property

**Status:** Implemented  
**Created:** 2025-11-15  
**Implemented:** Commit f06bad2f6ce804812991b04cdef3e627bfb75228  
**Authors:** Kapil Thacker  

## 1. Summary

Add support for the `MAX_DURATION` attribute on Extended Events (XEvent) sessions. This property specifies the maximum duration an event session will run before automatically stopping. The feature was introduced in SQL Server 2025.

## 2. Motivation

Extended Events sessions can now have a maximum duration limit, which is useful for:

- **Diagnostic captures:** Automatically stop tracing after a specified time to avoid collecting excessive data.
- **Scheduled monitoring:** Run event sessions for a defined window without manual intervention.
- **Resource management:** Prevent long-running sessions from consuming resources indefinitely.

SMO needs to expose this property to enable SSMS and other tools to script and manage sessions with duration limits.

## 3. Target SQL Server Versions

| Version | Support |
|---------|---------|
| SQL Server 2025 (v17.x) | ✅ Fully supported |
| Azure SQL Database | ⏳ Not yet enabled (tracked by Bug:4806316) |
| Azure SQL Managed Instance | ⏳ Not yet enabled (tracked by Bug:4816977) |
| SQL Server 2022 and earlier | ❌ Not applicable |

**Catalog View:** `sys.server_event_sessions`  
**Relevant Column:** `max_duration` (bigint, nullable)

**Note:** The catalog column may not exist on older SQL versions. The SMO implementation uses a conditional `if exists` pattern to check for column existence before executing the dynamic SQL query.

## 4. DDL Syntax

### 4.1 CREATE EVENT SESSION

```sql
CREATE EVENT SESSION [session_name] ON SERVER
ADD EVENT sqlserver.rpc_starting
WITH (MAX_MEMORY=4096 KB,
      EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
      MAX_DISPATCH_LATENCY=30 SECONDS,
      MAX_EVENT_SIZE=0 KB,
      MEMORY_PARTITION_MODE=NONE,
      TRACK_CAUSALITY=OFF,
      STARTUP_STATE=OFF,
      MAX_DURATION=UNLIMITED)
GO
```

### 4.2 MAX_DURATION Values

| Value | T-SQL Syntax | Description |
|-------|--------------|-------------|
| `0` | `MAX_DURATION=UNLIMITED` | Session runs indefinitely (engine default when omitted) |
| `> 0` | `MAX_DURATION=<n> SECONDS` | Session stops after `n` seconds |

### 4.3 Example with Specific Duration

```sql
CREATE EVENT SESSION [session_name] ON SERVER
ADD EVENT sqlserver.rpc_starting
WITH (MAX_DURATION=3600 SECONDS)  -- 1 hour
GO
```

## 5. SMO Implementation

### 5.1 Properties

| Property | Type | SMO Default | Description |
|----------|------|-------------|-------------|
| `MaxDuration` | `long` | `DefaultMaxDuration` (-1) | Duration in seconds. `0` means unlimited. `-1` is the SMO sentinel for "not explicitly set". |

### 5.2 Constants

```csharp
public const long DefaultMaxDuration = -1;  // Property not explicitly set
public const long UnlimitedDuration = 0;    // Explicitly set to UNLIMITED
```

### 5.3 Scripting Behavior

| Value | Script Output |
|-------|---------------|
| `-1` (default) | Property omitted from script |
| `0` (unlimited) | `MAX_DURATION=UNLIMITED` |
| `> 0` | `MAX_DURATION=<value> SECONDS` |

The property is only included in scripts when explicitly set (not default `-1`), ensuring backward compatibility with existing scripts.

### 5.4 Code Locations

- **Session Class:** [Session.cs](../src/Microsoft/SqlServer/Management/XEvent/core/Session.cs)
- **Session Provider:** [SessionProviderBase.cs](../src/Microsoft/SqlServer/Management/XEvent/core/SessionProviderBase.cs)
- **XML Metadata:** [Session.xml](../src/Microsoft/SqlServer/Management/XEventEnum/xml/Session.xml)

### 5.5 XML Metadata Implementation

The implementation uses a conditional query pattern to handle version differences:

```xml
<property_link fields='#MaxDuration#' left_join='#md' alias='maxdur'>
  maxdur.event_session_id = session.event_session_id
</property_link>
<prefix fields='#MaxDuration#'>
  create table #md (event_session_id int not null, max_duration bigint null)
  if exists (select 1 from sys.all_columns 
             where object_id = object_id('sys.server_event_sessions') 
             and name = 'max_duration')
  begin
    declare @sql nvarchar(max) = 'insert into #md select event_session_id, max_duration from sys.server_event_sessions'
    exec sp_executesql @sql
  end
</prefix>
```

This pattern ensures queries don't fail on older SQL versions lacking the column.

## 6. Testing

### 6.1 Unit Tests

- Verify default value is `DefaultMaxDuration`
- Verify property initialization on new sessions

**Test Location:** [SessionUnitTest.cs](../src/FunctionalTest/Smo/XEvent/SessionUnitTest.cs)

### 6.2 Functional Tests

- `MaxDuration_CreateSession_WithUnlimitedValue_IncludesInScript`: Verify `MAX_DURATION=UNLIMITED` appears when set to `0`
- `MaxDuration_CreateSession_WithValidValue_IncludesInScript`: Verify `MAX_DURATION=<n> SECONDS` appears for positive values
- Tests verify default value omits `MAX_DURATION` from script

**Test Location:** [XEventSessionTests.cs](../src/FunctionalTest/Smo/XEvent/XEventSessionTests.cs)

### 6.3 Version Restrictions

Tests are restricted to:
- SQL Server 2025+ (`MinMajor = 17`)
- Standalone engine type only (not Managed Instance until Bug:4816977 is resolved)

## 7. Documentation References

- [CREATE EVENT SESSION (Transact-SQL)](https://learn.microsoft.com/sql/t-sql/statements/create-event-session-transact-sql)
- [ALTER EVENT SESSION (Transact-SQL)](https://learn.microsoft.com/sql/t-sql/statements/alter-event-session-transact-sql)
- [sys.server_event_sessions](https://learn.microsoft.com/sql/relational-databases/system-catalog-views/sys-server-event-sessions-transact-sql)
- [Extended Events Overview](https://learn.microsoft.com/sql/relational-databases/extended-events/extended-events)
