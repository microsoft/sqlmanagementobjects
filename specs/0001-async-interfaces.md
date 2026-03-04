# Spec 0001: Async Interfaces for SMO

**Status:** Draft  
**Created:** 2026-02-10  
**Authors:** SMO Team  

## 1. Summary

Add asynchronous interfaces to SQL Management Objects (SMO) so that consumers can perform all database interactions — property initialization, collection population, and DDL execution — without blocking threads. An opt-in **async-only mode** on `Server` prevents accidental synchronous database round-trips by throwing `InvalidOperationException` when code accesses un-fetched properties or uninitialized collections.

## 2. Motivation

SMO's current programming model is entirely synchronous. Every property access, collection enumeration, and DDL operation can block the calling thread while waiting for a SQL Server round-trip. This is problematic for:

- **UI applications** (e.g., SSMS, Azure Data Studio) where blocking the UI thread degrades responsiveness.
- **High-throughput services** that manage many connections and cannot afford to pin a thread per operation.
- **Modern .NET development** where `async/await` is the standard pattern for I/O-bound work.

The underlying ADO.NET layer (`SqlCommand`) already provides `ExecuteReaderAsync`, `ExecuteNonQueryAsync`, and `ExecuteScalarAsync`. SMO should surface this capability to consumers.

## 3. Design Principles

1. **Do not break existing synchronous behavior.** All existing method signatures, semantics, and default-mode behavior remain unchanged.
2. **Async methods are purely additive.** They are new API surface alongside existing sync methods.
3. **Async methods are available in both modes.** Consumers can use `LoadAsync()` or `InitializeAsync()` without enabling async-only mode. Async-only mode only restricts the *sync* path.
4. **No sync-over-async.** Existing synchronous methods must never internally call async methods and block on the result. This is strictly prohibited to avoid deadlocks.
5. **Minimize async-over-sync.** New async methods should be truly async end-to-end, using `await` down to `SqlCommand` async methods. Wrapping synchronous calls in `Task.Run()` is strongly discouraged but permitted as a last resort with documented justification when unforeseen barriers arise.
6. **`ConfigureAwait(false)` everywhere.** All internal `await` calls within SMO library code must use `ConfigureAwait(false)` to avoid `SynchronizationContext` capture and potential deadlocks.
7. **Binary compatibility.** Existing compiled code that references current SMO assemblies must continue to work without recompilation.

## 4. Async-Only Mode

### 4.1 Scope

Async-only mode is scoped to a `Server` instance:

```csharp
var server = new Server(connection);
server.AsyncOnlyMode = true;
```

When enabled, any operation that would trigger a **synchronous database fetch** throws `InvalidOperationException` with a descriptive message. This includes:

- Accessing a property on an `Existing` object that has not been pre-fetched.
- Accessing a collection that has not been explicitly loaded.
- Any implicit lazy initialization triggered by `Count`, indexer, `foreach`, or property getters.

### 4.2 Exception Behavior

The exception type is `InvalidOperationException`. Messages should clearly indicate the cause and the remedy:

- Property access: *"Property '{name}' has not been initialized. In async-only mode, call InitializeAsync() before accessing properties."*
- Collection access: *"Collection has not been loaded. In async-only mode, call LoadAsync() before accessing the collection."*

### 4.3 Mode Interaction Matrix

| Mode | Sync access (triggers fetch) | Async methods | Un-fetched property access |
|------|------------------------------|---------------|---------------------------|
| **Default** | ✅ Works as today | ✅ Available | Triggers sync fetch (as today) |
| **Async-only** | ❌ Throws `InvalidOperationException` | ✅ Required path | Throws `InvalidOperationException` |

### 4.4 Incremental Adoption

A consumer can incrementally adopt async:

1. Start using `LoadAsync()` and `InitializeAsync()` in default mode alongside existing sync code.
2. Once confident that all data paths are pre-fetched asynchronously, enable `server.AsyncOnlyMode = true` to catch any remaining sync fetches during development/testing.

## 5. ServerConnection Async Methods

### 5.1 Overview

`ServerConnection` (and its base class `ConnectionManager`) is the single choke-point for all SQL execution. Today, all execution flows through `ConnectionManager.ExecuteTSql()`, which calls synchronous `SqlCommand` methods.

New async methods are added to `ServerConnection`:

```csharp
public Task<int> ExecuteNonQueryAsync(string sqlCommand, CancellationToken cancellationToken = default);
public Task<int> ExecuteNonQueryAsync(IEnumerable<string> sqlCommands, CancellationToken cancellationToken = default);
public Task<SqlDataReader> ExecuteReaderAsync(string sqlCommand, CancellationToken cancellationToken = default);
public Task<DataTable> ExecuteWithResultsAsync(string sqlCommand, CancellationToken cancellationToken = default);
public Task<DataTable> ExecuteWithResultsAsync(IEnumerable<string> sqlCommands, CancellationToken cancellationToken = default);
public Task<object> ExecuteScalarAsync(string sqlCommand, CancellationToken cancellationToken = default);
```

> **Note on `ExecuteWithResultsAsync` return type:** The sync `ExecuteWithResults` returns `DataSet`, but `DataAdapter.Fill()` is inherently synchronous. The async version returns `DataTable` instead, populated by reading from `SqlDataReader` asynchronously via `ExecuteReaderAsync`. If multiple result sets are needed, callers should use `ExecuteReaderAsync` directly and process result sets via `SqlDataReader.NextResultAsync()`.

### 5.2 Batch Execution Semantics (`IEnumerable<string>` Overloads)

The `IEnumerable<string>` overloads execute commands **sequentially**, matching the behavior of the existing synchronous batch methods. If `CancellationToken` is cancelled mid-batch:

- The currently executing command is cancelled via `SqlCommand` cancellation.
- Remaining commands in the batch are **not** executed.
- `OperationCanceledException` is thrown.
- Commands that completed before cancellation are **not** rolled back (consistent with the existing sync behavior, which does not use implicit transactions for batches). Callers requiring atomicity should wrap batch calls in an explicit transaction.

### 5.3 CancellationToken Propagation

`CancellationToken` is an optional parameter (defaulting to `CancellationToken.None`) on all async methods. It propagates all the way down to `SqlCommand.ExecuteReaderAsync(CancellationToken)` / `ExecuteNonQueryAsync(CancellationToken)` / `ExecuteScalarAsync(CancellationToken)`, enabling true cancellation of in-flight SQL queries.

### 5.4 Internal Async Execution Path

A new internal method `ExecuteTSqlAsync()` parallels the existing `ExecuteTSql()`. It uses `SqlCommand` async methods at the leaf:

- `SqlCommand.ExecuteNonQueryAsync(CancellationToken)`
- `SqlCommand.ExecuteReaderAsync(CancellationToken)`
- `SqlCommand.ExecuteScalarAsync(CancellationToken)`

### 5.5 Dependencies

- None — this is the foundational layer. All other async components depend on this.

## 6. SFC Async Layer (Enumerator / Request / ExecuteSql)

### 6.1 Overview

The SFC (SQL Foundation Classes) layer orchestrates query construction and execution. The key types in the async path are:

| Type | Role |
|------|------|
| `Enumerator` | Front-end entry point — `GetData()` processes a `Request` |
| `Request` | Describes what to fetch (URN, fields, result type) |
| `ExecuteSql` | Wraps `ServerConnection` — executes constructed SQL |
| `Environment` | Builds the object list from URN, chains `EnumObject.GetData()` calls |

### 6.2 New Async Methods

```csharp
// Enumerator (parameter order matches existing sync Enumerator.GetData)
public static Task<EnumResult> GetDataAsync(object connectionInfo, Request request, CancellationToken cancellationToken = default);

// ExecuteSql
internal Task<DataTable> GetDataTableAsync(CancellationToken cancellationToken = default);
internal Task<SqlDataReader> GetDataReaderAsync(CancellationToken cancellationToken = default);

// Environment (parameter order matches Enumerator for consistency)
internal Task<EnumResult> GetDataAsync(object connectionInfo, Request request, CancellationToken cancellationToken = default);
```

### 6.3 Dependencies

- Depends on: **ServerConnection async methods** (Section 5).

## 7. Object Initialization and Property Access

### 7.1 Overview

Today, `SqlSmoObject` lazily fetches properties via `Initialize()` → `ImplInitialize()` → SFC `Enumerator.GetData()`. The property bag tracks state:

- **Pending** — no fields fetched yet.
- **Existing (default fields)** — default field set fetched.
- **Existing (all fields)** — all fields fetched.

### 7.2 New Async Methods on SqlSmoObject

```csharp
public Task InitializeAsync(CancellationToken cancellationToken = default);
public Task InitializeAsync(string[] fields, CancellationToken cancellationToken = default);
public Task RefreshAsync(CancellationToken cancellationToken = default);
```

These mirror the sync granularity:

- `InitializeAsync()` — fetches the default field set.
- `InitializeAsync(fields)` — fetches a specific set of fields.
- `RefreshAsync()` — fetches all fields (equivalent to `Refresh()`).

After an async initialization call, properties in the fetched set are accessible synchronously from the in-memory cache.

### 7.3 Behavior Summary

| Object State | Property in fetched set | Property NOT in fetched set (default mode) | Property NOT in fetched set (async-only mode) |
|---|---|---|---|
| `Creating` | ✅ Returns value | ✅ Returns value (no DB fetch) | ✅ Returns value (no DB fetch) |
| `Existing` (initialized) | ✅ Returns cached value | Triggers sync fetch | ❌ Throws `InvalidOperationException` |
| `Existing` (not initialized) | N/A | Triggers sync fetch | ❌ Throws `InvalidOperationException` |

### 7.4 Dependencies

- Depends on: **SFC async layer** (Section 6).

## 8. Collection Async Initialization

### 8.1 Overview

SMO collections (`server.Databases`, `database.Tables`, etc.) lazily populate on first access (`Count`, indexer, `foreach`). The base class hierarchy is:

```
AbstractCollectionBase
  └─ SmoCollectionBase
       └─ SortedListCollectionBase
            └─ SimpleObjectCollectionBase
                 └─ RemovableCollectionBase
```

### 8.2 New Async Method on SmoCollectionBase

```csharp
public Task LoadAsync(CancellationToken cancellationToken = default);
```

`LoadAsync()` eagerly and asynchronously populates the collection. After it completes:

- `Count`, indexer, and `foreach` work synchronously from the in-memory cache.
- Objects in the collection are initialized with the default field set (same as today's lazy init).

### 8.3 Cancellation Behavior

If `CancellationToken` is cancelled during `LoadAsync()`:

- `LoadAsync()` throws `OperationCanceledException`.
- The collection remains in its **pre-call state** (uninitialized). No partial population.
- The caller can retry with a new token or give up.

The same rollback-on-cancellation behavior applies to `InitializeAsync()` on `SqlSmoObject` — if cancelled, the object stays in whatever state it was before the call.

### 8.4 Dependencies

- Depends on: **SFC async layer** (Section 6).

## 9. DDL Async Operations (IScriptCreate / IScriptAlter / IScriptDrop)

### 9.1 Overview

DDL operations (`Create`, `Alter`, `Drop`) generate T-SQL scripts and execute them. Today, the script generation is handled by virtual methods on SMO object base classes, and execution goes through `ServerConnection.ExecuteNonQuery()`.

Rather than adding async methods to every individual SMO class, we introduce **public interfaces** for the script-generation contract and **extension methods** that provide async execution.

### 9.2 New Interfaces

Defined in the `ConnectionInfo` assembly (alongside existing `ICreatable`, `IAlterable`, `IDroppable`):

```csharp
/// <summary>
/// Represents an SMO object that can generate a CREATE script.
/// </summary>
public interface IScriptCreate
{
    /// <summary>
    /// Generates the CREATE T-SQL script for this object.
    /// </summary>
    IEnumerable<string> GenerateCreateScript();
}

/// <summary>
/// Represents an SMO object that can generate an ALTER script.
/// </summary>
public interface IScriptAlter
{
    IEnumerable<string> GenerateAlterScript();
}

/// <summary>
/// Represents an SMO object that can generate a DROP script.
/// </summary>
public interface IScriptDrop
{
    IEnumerable<string> GenerateDropScript();
}
```

The interfaces are intentionally focused on **script generation only**. They do not expose connection or execution context — the extension methods resolve the `ServerConnection` through the object's existing parent-chain machinery (every `SqlSmoObject` can walk up to its `Server` to obtain the connection). This keeps the interfaces lean and avoids coupling them to `ServerConnection`.

> **Note:** The exact return types above are directional. Implementation may refine them (e.g., `IReadOnlyList<string>` vs `IEnumerable<string>`) based on how the existing script-generation methods produce output.

SMO base classes (e.g., `ScriptNameObjectBase`, `NamedSmoObject`) implement the appropriate interfaces. Objects outside the `Smo` assembly that support these operations can also implement the interfaces.

### 9.3 Extension Methods

```csharp
public static class SmoAsyncExtensions
{
    public static Task CreateAsync(this IScriptCreate obj, CancellationToken cancellationToken = default);
    public static Task AlterAsync(this IScriptAlter obj, CancellationToken cancellationToken = default);
    public static Task DropAsync(this IScriptDrop obj, CancellationToken cancellationToken = default);
}
```

Consumers call these naturally:

```csharp
await table.CreateAsync(cancellationToken);
await table.AlterAsync(cancellationToken);
await table.DropAsync(cancellationToken);
```

### 9.4 Sync Refactoring Opportunity

The existing synchronous `Create()`, `Alter()`, and `Drop()` methods could eventually be refactored to consume the same `IScriptCreate` / `IScriptAlter` / `IScriptDrop` interfaces, unifying the script-generation contract. This is not required for the initial async implementation but is a desirable long-term goal.

### 9.5 Dependencies

- Depends on: **ServerConnection async methods** (Section 5) for execution.
- Depends on: Existing script-generation machinery in SMO base classes.

## 10. Component Dependency Graph

```
┌──────────────────────────────────────────────────┐
│  DDL Async (IScriptCreate/Alter/Drop extensions) │
│                  (Section 9)                     │
└──────────────────┬───────────────────────────────┘
                   │ uses
┌──────────────────▼───────────────────────────────┐
│  Object InitializeAsync / Collection LoadAsync   │
│              (Sections 7 & 8)                    │
└──────────────────┬───────────────────────────────┘
                   │ uses
┌──────────────────▼───────────────────────────────┐
│  SFC Async Layer (Enumerator/ExecuteSql)         │
│                (Section 6)                       │
└──────────────────┬───────────────────────────────┘
                   │ uses
┌──────────────────▼───────────────────────────────┐
│  ServerConnection Async Methods                  │
│                (Section 5)                       │
└──────────────────┬───────────────────────────────┘
                   │ uses
┌──────────────────▼───────────────────────────────┐
│  SqlCommand Async (ADO.NET — already exists)     │
└──────────────────────────────────────────────────┘

Async-Only Mode (Section 4) is an orthogonal enforcement
layer on the Server object — it gates sync access paths but
does not depend on or affect the async method implementations.
```

## 11. Backward Compatibility Guarantees

1. **No existing method signatures change.** `Create()`, `Alter()`, `Drop()`, `Initialize()`, etc. remain exactly as they are.
2. **No behavioral change in default mode.** Unless `Server.AsyncOnlyMode` is explicitly set to `true`, everything behaves exactly as it does today. Lazy property fetches happen synchronously. Collections auto-populate on access.
3. **New interfaces on existing classes are not breaking.** Adding `IScriptCreate` to a class that already has `Create()` is additive. Existing code that doesn't reference the interface is unaffected.
4. **Binary compatibility.** Existing compiled code that references current SMO assemblies continues to work without recompilation against the new version.

## 12. Assembly Location

| Component | Assembly |
|-----------|----------|
| `IScriptCreate`, `IScriptAlter`, `IScriptDrop` interfaces | `Microsoft.SqlServer.ConnectionInfo` |
| `ServerConnection` async methods | `Microsoft.SqlServer.ConnectionInfo` |
| SFC async layer (`Enumerator`, `ExecuteSql`) | `Microsoft.SqlServer.SqlEnum` |
| `SqlSmoObject.InitializeAsync`, `SmoCollectionBase.LoadAsync` | `Microsoft.SqlServer.Smo` |
| `Server.AsyncOnlyMode` property | `Microsoft.SqlServer.Smo` |
| `SmoAsyncExtensions` (extension methods) | TBD — `Microsoft.SqlServer.Smo` or a new assembly depending on whether all implementors of the interfaces are reachable |

## 13. Testing Requirements

### 13.1 Test Organization

- Async methods have **dedicated test classes** separate from sync tests (e.g., `TableAsyncTests` alongside `TableTests`).
- Shared validation assertions may be refactored into helper methods consumed by both sync and async test classes.
- Follow existing naming conventions: `ObjectType_ScenarioAsync_ExpectedResult`.

### 13.2 Async-Only Mode Tests

A dedicated test suite must verify:

- Pre-fetched properties are accessible after `InitializeAsync()`.
- Un-fetched properties throw `InvalidOperationException` with a descriptive message.
- Uninitialized collections throw `InvalidOperationException` on `Count`, indexer, and `foreach`.
- Collections loaded via `LoadAsync()` are fully accessible synchronously.
- Cancelled `LoadAsync()` leaves the collection uninitialized (no partial state).
- Cancelled `InitializeAsync()` leaves the object in its pre-call state.
- Extension methods (`CreateAsync`, `AlterAsync`, `DropAsync`) work correctly through the `IScriptCreate` / `IScriptAlter` / `IScriptDrop` interfaces.

### 13.3 Parity Tests

Async and sync operations must produce **identical T-SQL output**. Tests should verify that `table.Create()` and `await table.CreateAsync()` generate the same script.

## 14. Open Questions

- Exact member signatures for `IScriptCreate`, `IScriptAlter`, `IScriptDrop` — a directional sketch is provided in Section 9.2 but return types and shared base interface design need to be finalized during implementation.
- Should `Transfer` (bulk copy/scripting) get async support in this spec or a separate one?
- `IAsyncEnumerable<T>` support on collections — deferred from v1 but may be valuable for large collections in a future iteration.
- Detailed error messages and exception hierarchy — should `InvalidOperationException` subclasses be introduced for more granular `catch` handling?

## 15. Related Specifications

- [0002-async-scripter.md](0002-async-scripter.md) — Async support for `Scripter.ScriptAsync()` (placeholder).
