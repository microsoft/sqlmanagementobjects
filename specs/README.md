# SMO Design Specifications

This folder contains design specifications for significant features and architectural changes to SQL Management Objects.

Specifications document **what** and **why** — not implementation sequencing. Implementation plans are created separately after a spec is finalized.

## Index

| Spec | Title | Status |
|------|-------|--------|
| [0001-async-interfaces.md](0001-async-interfaces.md) | Async Interfaces for SMO | Draft |
| [0002-async-scripter.md](0002-async-scripter.md) | Async Scripter (`ScriptAsync`) | Placeholder |
| [0003-workload-group-tempdb-rg.md](0003-workload-group-tempdb-rg.md) | Workload Group TempDB Resource Governance in CREATE Scripts | Implemented |
| [0004-xevent-max-duration.md](0004-xevent-max-duration.md) | XEvent Session MAX_DURATION Property | Implemented |
| [0005-ag-cluster-connection-options.md](0005-ag-cluster-connection-options.md) | Availability Group ClusterConnectionOptions for TDS 8.0 | Implemented |

## Conventions

- Specs are numbered sequentially: `NNNN-short-title.md`.
- Status values: **Placeholder** → **Draft** → **Review** → **Accepted** → **Implemented** → **Superseded**.
- Each spec should be self-contained with enough context for a reader unfamiliar with the prior discussion.
- Specs should clearly document dependencies between components to support implementation planning.
- **DDL-related specs** can be generated and implemented using the `@ddl-changes` Copilot Chat agent (see [../.github/agents/ddl-changes.md](../.github/agents/ddl-changes.md)). The agent gathers requirements interactively, produces a spec in this folder, and after user approval, implements the full change.
