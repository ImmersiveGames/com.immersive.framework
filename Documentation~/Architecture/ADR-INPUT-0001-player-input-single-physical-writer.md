# ADR-INPUT-0001 — PlayerInput Single Physical Writer

**Status:** Accepted and validated  
**Type:** Technical architecture / runtime contract  
**Scope:** Unity Input physical side effects

## Context

The package had multiple independent physical writers for the same `PlayerInput`:

```text
InputModeUnityPlayerInputAdapter
PlayerGameplayInputBindingRuntimeContext
UnityPlayerInputGateAdapter
PauseInputActionTrigger optional map switching
```

Those paths used different Unity APIs (`SwitchCurrentActionMap`, direct
`currentActionMap` assignment, `InputActionMap.Enable/Disable`, and
`PlayerInput.ActivateInput/DeactivateInput`). Their local rollback assumptions could
therefore overwrite a newer posture or re-enable a map after another domain changed it.

## Decision

```text
Domain requester
  InputMode
  Gameplay binding
  Gate evaluation
        |
        v
UnityPlayerInputGateAdapter
  explicit co-located host write port
        |
        v
UnityPlayerInputStateWriter
  only package-owned physical side-effect implementation
        |
        v
PlayerInput / InputActionMap
```

`UnityPlayerInputGateAdapter` remains the explicit component already referenced by the
canonical gameplay binding. It accepts action-map and activation requests in addition
to evaluating Gate blockers.

`UnityPlayerInputStateWriter` is internal and stateless. It performs only validated Unity
side effects and returns exact rollback evidence. It is never discovered globally and owns
no Session, Slot, Actor, Activity, Route, Pause or InputMode policy.

## Rules

1. Requesters must not call `SwitchCurrentActionMap`, assign `currentActionMap`, or call
   `PlayerInput.ActivateInput/DeactivateInput` directly.
2. Requesters use the exact co-located `UnityPlayerInputGateAdapter` associated with the
   target `PlayerInput`.
3. Missing write authority is an explicit failure; there is no fallback writer.
4. Gameplay binding retains its typed binding token and the writer receipt needed for exact
   reverse release.
5. Gate blocking is an overlay. When another explicit posture selects a non-gameplay map
   during a block, Gate release must not resurrect the older gameplay map.
6. Independent `InputAction` subscription enable/disable for the global Pause action is not
   PlayerInput posture and remains owned by the canonical trigger.

## Consequences

### Positive

- one physical implementation of PlayerInput/action-map mutation;
- deterministic rollback evidence;
- no frame-by-frame direct mutation from the Gate adapter;
- explicit failure when the canonical host write port is missing;
- no singleton, service locator or static PlayerInput registry;
- existing Slot, Actor and gameplay-admission identities remain unchanged.

### Trade-offs

- `UnityPlayerInputGateAdapter` temporarily carries the explicit write-port role in addition
  to Gate evaluation;
- logical requesters share one write port but preserve their domain lifecycle tokens.

## Follow-up

ADR-INPUT-0002 completes the previously deferred logical ownership work:

```text
resident InputMode state owner and request arbitration
one canonical Pause InputAction submitter
explicit logical commit / rollback around Pause/InputMode apply
```

Still deferred:

```text
InputActionReference-based product authoring
renaming/extracting the host write-port component after its product shape stabilizes
```

## Validation evidence

```text
[IC1_PLAYER_INPUT_SINGLE_WRITER_SMOKE] status='Passed' cases='14'
```

## Acceptance

```text
only UnityPlayerInputStateWriter contains package-owned physical mutation APIs
InputMode and gameplay binding request through UnityPlayerInputGateAdapter
Gate blocking delegates physical effects to the writer
missing co-located write authority fails explicitly
selection returns rollback evidence
release restores the captured previous map
Gate release does not resurrect a superseded gameplay map
P3K.3 typed control/input binding regression remains green
IC1 single-writer smoke passes
```
