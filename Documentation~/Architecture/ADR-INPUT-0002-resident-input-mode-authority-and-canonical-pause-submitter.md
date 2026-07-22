# ADR-INPUT-0002 — Resident InputMode Authority and Canonical Pause Submitter

**Status:** Superseded for product composition by `PausePlayerInputBinding` P1
**Type:** Technical architecture / runtime ownership  
**Scope:** Logical InputMode state and Pause product submission

> P1 supersession: the bridge described below remains only for isolated technical
> regression. It is not a startup integration, a `PauseRequestTrigger` dependency,
> or consumer-authoring guidance.

## Context

ADR-INPUT-0001 established one physical writer for `PlayerInput` and
`InputActionMap` mutations. That removed independent physical writers, but it did
not establish one logical owner for the current input posture.

Before this decision, the package still exposed a direct Pause path alongside
the canonical bridge:

```text
former direct Pause input
  -> FrameworkRuntimeHost.RequestPause
  -> optional action-map request

PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> Pause + InputMode application
```

Both paths reached the same physical writer after IC1, but only the second path
kept logical Pause and InputMode application in one operation. A direct Pause
submission could therefore change Pause state without advancing a resident
InputMode state.

The existing `InputModeState` was passive. Every Pause/InputMode operation
reconstructed a temporary state from the current Pause snapshot, so there was no
resident revision, in-flight transaction or stale-request rejection.

## Decision

### Resident authority

`InputModeRuntimeContext` is the scoped logical authority for one input posture.
It owns:

```text
context identity
current InputModeState
monotonic operation sequence
at most one active transaction
exact commit / rollback evidence
runtime snapshot diagnostics
```

It does not own:

```text
Unity objects
PlayerInputManager
PauseRuntime
Player Slots or Actor identity
physical InputActionMap mutation
global registration or discovery
```

The context accepts an explicit initial state and uses the existing pure
`InputModeRequestEvaluator` to prepare transitions. State changes only on an
exact transaction commit. A failed physical application rolls the transaction
back without changing logical state.

### Product composition

`PauseInputModeUnityPlayerInputRuntimeBridge` becomes the PlayerInput-scoped
composition owner for Pause/InputMode product requests:

```text
PauseInputActionRuntimeBridgeTrigger
        ↓
PauseInputModeUnityPlayerInputRuntimeBridge
  resident InputModeRuntimeContext
  request serialization
  Pause/InputMode apply orchestration
  commit or explicit rollback
        ↓
InputMode exact layered map-set application
        ↓
UnityPlayerInputGateAdapter write port
        ↓
UnityPlayerInputStateWriter
```

The bridge is already an explicit scene-authored component tied to one
`PlayerInput`; promoting it avoids introducing a global manager, singleton or
parallel mandatory component.

The resident context identity is derived from the exact Unity `PlayerInput`
entity identity. Rebinding the bridge to another `PlayerInput` clears the local
context explicitly. A live context never silently changes target.

### Canonical Pause submitter

`PauseInputActionRuntimeBridgeTrigger` is the only active package-owned
InputAction submitter for the Pause/InputMode product flow.

H1 physically removed the direct Pause input implementation:

```text
no serialized compatibility component
no Add Component menu
no direct Pause submitter
no action-map request path
```

QA verifies the removed script GUIDs are absent from its serialized fixtures.
Consumers must remove serialized instances before importing this package cut.

## Transaction rules

1. The bridge resolves the exact current Pause snapshot.
2. The resident InputMode state must agree with that snapshot.
3. Drift is an explicit failure; no implicit reconciliation is allowed.
4. The context prepares one exact target-mode transaction.
5. Concurrent logical requests are rejected while a transaction is active.
6. The existing Pause/InputMode service performs preflight and physical apply.
7. On success, the exact transaction commits.
8. On failure, the transaction rolls back without changing resident state.
9. If Pause changed before the failure, the bridge requests the exact previous
   Pause state and records rollback evidence.
10. Physical Unity effects remain exclusively inside
    `UnityPlayerInputStateWriter` through the existing write port.

## Gameplay binding boundary

`PlayerGameplayInputBindingRuntimeContext` retains its current typed binding
ownership and exact writer receipt. It is not moved into `InputModeRuntimeContext`
in this cut.

Reason:

```text
gameplay binding identity belongs to Session / Slot / Actor admission
InputMode posture belongs to the local product interaction state
Gate blocking is an overlay, not a posture owner
```

The domains share one physical write port but preserve separate lifecycle tokens.

## Consequences

### Positive

- one resident logical InputMode state per explicit PlayerInput bridge;
- deterministic revision and transaction evidence;
- reentrant/concurrent posture requests fail explicitly;
- no direct package-owned Pause InputAction path remains;
- Pause and InputMode product changes are submitted through one boundary;
- physical writer ownership from ADR-INPUT-0001 remains unchanged;
- no singleton, service locator or global PlayerInput registry.

### Trade-offs

- direct technical calls to `FrameworkRuntimeHost.RequestPause` remain possible,
  but the product bridge will detect resulting InputMode/Pause drift;
- the existing bridge carries composition and resident-state responsibilities;
- failure after Pause changed requires a compensating Pause request because the
  current Pause runtime does not expose a transaction token;
- serialized consumers must be cleaned before importing H1.

## Out of scope

```text
moving gameplay binding tokens into InputMode
renaming UnityPlayerInputGateAdapter write-port role
changing PlayerInputManager authority
new command-reading API
frontend menu authoring
multi-Player global InputMode manager
changing the canonical Pause/InputMode bridge contract
```

## Acceptance

```text
InputModeRuntimeContext owns a valid resident state and monotonic revision
only one transaction may be in flight
foreign/stale transaction commit and rollback are rejected
rollback preserves the previous logical state
Pause bridge owns the scoped resident context
Pause bridge rejects state drift without fallback
Pause bridge commits only after successful apply
Pause bridge records explicit rollback evidence on failure
PauseInputActionRuntimeBridgeTrigger delegates to the bridge
no direct Pause input component remains in the package
IC1 physical writer smoke remains green
IC2 runtime authority smoke passes
Global remains enabled across Gameplay and PauseOverlay
Gameplay applies exactly Global + Player
the superseded technical bridge applies exactly Global + UI during PauseOverlay
the current scene-local Pause product applies exactly Global during PauseOverlay
existing Pause/InputMode runtime regression remains green
```

## Layered action-map follow-up

ADR-INPUT-0003 replaces the inherited exclusive-map policy with exact layered map sets.
The resident authority and transaction rules in this ADR remain unchanged.
