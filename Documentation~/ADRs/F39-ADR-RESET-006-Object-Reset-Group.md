# F39 — Object Reset Group

Status: Accepted / preview.10

## Context

`ObjectResetTrigger` resets one logical `ObjectEntryDeclaration` target. FIRSTGAME now needs a reusable way to reset a small set of route or activity objects together without introducing full Activity Restart or Cycle Reset semantics.

## Decision

Add an explicit Object Reset Group tool:

- `ObjectResetGroupAsset` stores reusable target lists, normally by `Object Entry Id`.
- `ObjectResetGroupTrigger` executes a group sequentially through the existing `FrameworkRuntimeHost.RequestObjectResetAsync` path.
- `ObjectResetGroupEntry` resolves either a scene `ObjectEntryDeclaration` or a string id.
- `ObjectResetGroupResult` aggregates target, participant and issue counts.

This feature is a composition layer over Object Reset. It does not reload scenes, restart activities, rediscover participants, spawn actors, change save state or replace Cycle Reset.

## Rationale

The group tool gives designers a practical puzzle/room reset surface while preserving the existing deterministic Object Reset architecture:

```text
ObjectResetGroupTrigger
  -> ObjectResetGroupEntry[]
  -> existing ObjectResetRequest per target
  -> existing ObjectResetRuntime and participant source
```

The trigger is opt-in and scene-authored. A group asset is useful for id-based reusable definitions; inline trigger entries are useful when scene references are desired.

## Consequences

- Each target still requires an `ObjectEntryDeclaration`.
- Each physical reset still requires an explicitly registered reset participant.
- Group execution is sequential and stops on first failure when configured.
- Existing Object Reset logs remain visible per target; group logs provide the aggregate result.
- Activity Restart remains a future feature built on top of this group tool, not part of this cut.

## Smoke expectation

A valid group with one configured player target should emit:

```text
Object Reset Group Request completed.
status='Succeeded'
targets='1'
targetSucceeded='1'
resetRequests='1'
blockingIssues='0'
```
