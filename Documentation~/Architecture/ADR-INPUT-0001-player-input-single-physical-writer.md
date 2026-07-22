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
former direct Pause map-switching path (physically removed in H1)
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
canonical gameplay binding. It accepts exact action-map posture requests and evaluates
Gate blockers. It does not own `PlayerInput` activation or provisioning lifecycle.

`UnityPlayerInputStateWriter` is internal and stateless. It performs only validated Unity
side effects and returns exact rollback evidence. It is never discovered globally and owns
no Session, Slot, Actor, Activity, Route, Pause or InputMode policy.

## Rules

1. Requesters must not call `SwitchCurrentActionMap`, assign `currentActionMap`, or call
   `PlayerInput.ActivateInput/DeactivateInput` directly. The canonical map writer also does
   not own activation/deactivation; those operations belong to the Player lifecycle.
2. Requesters use the exact co-located `UnityPlayerInputGateAdapter` associated with the
   target `PlayerInput`.
3. Missing write authority is an explicit failure; there is no fallback writer.
4. Gameplay binding retains its typed binding token and the writer receipt needed for exact
   reverse release.
5. Gate blocking is an overlay. When another explicit posture selects a non-gameplay map
   during a block, Gate release must not resurrect the older gameplay map.
6. Independent `InputAction` subscription ownership remains with the canonical trigger.
7. Baseline InputMode posture is applied as an exact enabled-map set through the writer.
8. `Global` remains enabled while `Player` and `UI` are selected by policy.
9. Gate blocking disables only the gameplay map overlay. It never deactivates the whole
   `PlayerInput`, because that would bypass the layered posture and lose exact restoration.
10. Action-map preparation does not require `PlayerInput.inputIsActive`. Lifecycle readiness,
    device pairing and event delivery remain separate Player lifecycle facts.

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

ADR-INPUT-0003 defines the technical bridge's layered `Global + Player/UI` posture
and exact map-set rollback. The current scene-local Pause product uses `Global + Player`
while running and `Global` while paused; product-level `Global + UI` remains a future
interactive Pause UI posture.

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
action-map writes do not require or mutate PlayerInput lifecycle activation
release restores the captured previous map
Gate release does not resurrect a superseded gameplay map
P3K.3 typed control/input binding regression remains green
IC1 single-writer smoke passes
```
