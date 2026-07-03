# F10 ADR SNAPSHOT 001 - Snapshot Model

Status: Accepted / Conceptual History / Extended by F21

## Context

State capture must stay separate from reset semantics.

F10 recorded the original conceptual decision that Snapshot is different from Reset and that Reset Baseline is not Save Snapshot.

## Decision

Snapshot is different from Reset.

Snapshot has envelope, owner, schema, version and payload.

Reset Baseline is not Save Snapshot.

## F21 Extension

F21 is the canonical operational trail for Save Snapshot work.

The canonical runtime namespace for Save Snapshot envelope and participant contracts is now:

```text
Immersive.Framework.Snapshot
```

F21B added the backend-agnostic Snapshot envelope primitives. F21C added Snapshot participant contracts and synthetic diagnostics smoke.

Older or parallel types that use the word `Snapshot`, such as `PauseSnapshot`, `GateSnapshot`, `TransitionSnapshot`, `TransitionEffectSnapshot` and `ObjectEntryRuntimeContextSnapshot`, remain diagnostic/runtime state snapshots. They are not Save Snapshot envelopes, participants, progression slots or backend records.

## Consequences

Future persistence can evolve independently from cycle reset and active-state reset.

Save Snapshot implementation must follow F21, not create a second F10-era runtime trail.

## Guardrails

- Do not implement Reset as snapshot restore.
- Do not treat save payload as lifecycle owner.
- Version snapshot payloads explicitly.
- Do not add new Save Snapshot contracts outside `Immersive.Framework.Snapshot`.
- Do not reinterpret diagnostic/runtime snapshots as persistence records without an explicit F21+ migration cut.
