# IF-ADR-004 — Camera Requests and Output Authority

Status: **Accepted**  
Last updated: 2026-08-09  
Related decisions: IF-ADR-001, IF-ADR-003, IF-ADR-005, IF-ADR-010

> Current implementation, QA and FIRSTGAME integration status is tracked in
> `../Tracking/IF-TRACK-Framework.md`. This ADR is normative and intentionally
> does not carry a mutable completion percentage. UX observations are qualitative
> product feedback and are not part of functional completion arithmetic.

## Context

Camera presentation needs one physical output authority while allowing Session,
Route, Activity and Player contexts to request presentation without directly
mutating shared output or relying on hierarchy discovery.

## Decision

The framework owns a typed Camera request/release model and one physical output
authority for the current single-output scope.

Request priority, ownership, replacement, release, restoration and diagnostics
are explicit. Player Camera admission is contextual to gameplay readiness.
Consumers author intent through product components rather than locating or
mutating the physical output directly.

```text
Camera request source
  -> typed request + ownership evidence
  -> Camera output authority
  -> one active physical output
```

## Authoring boundary

`CameraRigComposer` is a legitimate materialized-composition surface because a
local authored rig deterministically materializes Cinemachine technical state.
Its Apply/Rebuild contract does not imply that other framework features need a
Composer.

`CameraOutputSessionBinding` owns explicit references to the persistent physical
Unity Camera and Cinemachine Brain. Camera rig materialization never creates that
persistent output authority.

## Constraints

- Exactly one physical output authority exists per Session in the current scope.
- Requests/releases are typed, scoped, deterministic and diagnostic.
- No `Camera.main` authority lookup, singleton or service locator.
- No hierarchy/name fallback for output authority.
- Required target/reference failures are explicit.
- Previous presentation is restored only through the request ownership model.

## Current scope

Split-screen and multiple simultaneous outputs are outside the accepted
single-output boundary and require a separate architectural extension.
