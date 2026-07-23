# IF-ADR-006 — Loading, Transition, Persistence and Diagnostics

Status: Accepted
Last updated: 2026-07-23
Supersedes: loading, transition, snapshot, save and diagnostics ADR fragments
Superseded by: none

## Context

Lifecycle operations need loading progress, visual transitions, persistence and
diagnostics without transferring lifecycle authority to presentation adapters
or conflating save data with reset baselines.

## Decision

Loading is an operation/readiness model with typed progress and issues.
`LoadingSurface` presents state; it does not own scene loading, lifecycle,
release or policy.

Transition is the visual envelope around an operation. Route/Activity policy
selects transition and gate behavior. Transition effects execute through
explicit adapters and always release temporary Gate blockers in failure paths.

Snapshot is versioned capture/restore data with an explicit schema, owner and
payload. Preferences are user/application configuration. Progression Save owns
durable slot records and requests. Snapshot, Reset and Save remain separate
concepts.

Human logs and structured framework facts are distinct. Runtime operations
return typed results and publish completed facts; events must not hide mutation
or become a second command path.

## Accepted scope

- Loading operations, weighted progress, readiness and presentation adapters.
- Transition plans, effects, gate policy and explicit results.
- Versioned snapshot contracts and participants.
- Preferences and progression-save runtime contracts.
- Structured facts, typed issues and diagnostics snapshots.

## Rejected scope

- Loading UI owning scene/lifecycle execution.
- Transition effects becoming Route/Activity authority.
- Save, Reset and Snapshot sharing one implicit model.
- Log message text as machine-asserted state.
- Events used as hidden commands or silent recovery.

## Consequences

Presentation remains replaceable and lifecycle remains authoritative. Durable
state can evolve by schema without coupling to object-reset mechanics.

## Current implementation coverage

Runtime source contains loading, transition/effects, snapshot, preferences,
progression-save and diagnostics modules. Their public API status and real-game
coverage vary; the tracker is authoritative for current validation claims.

## Pending decisions

- Product-facing save/profile authoring workflow and real-game sample.
- Final Activity transaction/finalization diagnostics described by IF-ADR-001.
