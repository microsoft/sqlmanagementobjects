# Spec 0002: Async Scripter (ScriptAsync)

**Status:** Placeholder  
**Created:** 2026-02-10  
**Authors:** SMO Team  

## 1. Summary

Add `ScriptAsync()` to the `Scripter` class, enabling fully asynchronous script generation for SMO objects. The `Scripter` walks object trees, reads properties, and emits T-SQL — making it a high-value target for async since scripting large schemas involves many database round-trips.

## 2. Prerequisites

This spec depends on the foundational async infrastructure defined in [0001-async-interfaces.md](0001-async-interfaces.md):

- `SqlSmoObject.InitializeAsync()` / `RefreshAsync()` for async property fetching.
- `SmoCollectionBase.LoadAsync()` for async collection population.
- SFC async layer (`Enumerator.GetDataAsync`).
- `ServerConnection` async execution methods.

## 3. Scope

*To be defined.* This spec will cover:

- `Scripter.ScriptAsync()` method signature and behavior.
- How the scripter asynchronously pre-fetches properties and collections for the objects being scripted.
- Cancellation support via `CancellationToken`.
- Whether scripting can be streamed (`IAsyncEnumerable<string>`) or is batch-only.
- Interaction with `ScriptingOptions` and dependency discovery.
- Testing requirements and parity with synchronous `Script()`.

## 4. Design

*To be completed after [0001-async-interfaces.md](0001-async-interfaces.md) is accepted and implementation is underway.*
