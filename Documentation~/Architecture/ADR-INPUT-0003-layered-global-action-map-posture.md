# ADR-INPUT-0003 — Layered Global Action Map Posture

**Status:** Superseded and removed from the active product/runtime surface
**Type:** Technical architecture / Unity Input posture  
**Scope:** Global Pause continuity and exact InputMode action-map sets

> Historical record only. The technical bridge topology described below was
> removed. The scene-local `PausePlayerInputBinding` product owns consumer Pause
> binding and lifecycle; old bridge policies are not supported regression APIs.
>
> P2.3A product revision: the current physical Pause product uses `Global` only
> during `PauseOverlay`. `Global + UI` is reserved for a future interactive Pause UI
> with its own authorable contract.

## Context

ADR-INPUT-0001 established one physical writer. ADR-INPUT-0002 established one
resident logical `InputMode` owner and one canonical Pause submitter. The remaining
physical policy still represented each `InputMode` as one selected action map:

```text
Gameplay     -> Player
PauseOverlay -> UI
FrontendMenu -> UI
```

That model cannot keep one canonical `PauseToggle` action available before and
during Pause without duplicating the action between maps or introducing another
input path. It also caused `PlayerInput.ActivateInput()` to attempt the serialized
default map before the intended Pause map was applied.

## Decision

Input posture is an exact enabled action-map set, not one exclusive map.

The initial canonical topology is:

```text
Global
  PauseToggle

Player
  gameplay commands

UI
  navigation and UI commands
```

The initial technical bridge policies are:

```text
Gameplay     -> Global + Player
PauseOverlay -> Global + UI
FrontendMenu -> Global + UI
InputLocked  -> Global
```

These policies remain regression evidence for the superseded bridge. The current
scene-local Pause product materializes `PauseOverlay -> Global`; product-level
`Global + UI` is deferred until UI actions, bindings, an input module and real
consumers have an explicit authorable contract.

`Global` means persistent within one explicit `PlayerInput` action-asset instance.
It does not mean an application singleton, static input registry or shared global
`InputActionAsset`.

## Physical application

`UnityPlayerInputStateWriter` remains the only package-owned physical writer. It
now accepts:

```text
primary action map
exact enabled action-map set
```

The writer:

```text
captures the previous primary map and complete enabled-map set
validates every desired map before mutation
applies the requested primary map
reconciles every map to the exact desired enabled state
verifies the resulting state
returns exact rollback evidence
restores the previous primary map and enabled set on rollback
```

The InputMode application path does not call `PlayerInput.ActivateInput()` and action-map
preparation does not require `PlayerInput.inputIsActive`. Activation, device pairing, event
delivery and local-player provisioning remain owned by the existing Player lifecycle.
InputMode changes only action-map posture.

`UnityPlayerInputGateAdapter` remains the explicit write port and applies Gate
blocking as a gameplay-map overlay after the baseline map set is changed or restored.
It never calls `PlayerInput.DeactivateInput()` or `ActivateInput()`. PlayerInput lifecycle,
device pairing and provisioning remain outside InputMode and Gate policy.

## Pause product flow

```text
Global/PauseToggle
  -> PauseInputActionRuntimeBridgeTrigger
  -> PauseInputModeUnityPlayerInputRuntimeBridge
  -> resident InputMode transaction
  -> exact Global + Player/UI map set
  -> commit or explicit rollback
```

The Pause bridge exposes one required `globalActionMapName`, initially `Global`.
Missing Global evidence fails preflight before Pause state changes. No action is
silently duplicated into Player or UI.

## Consequences

### Positive

- one `PauseToggle` remains available in Gameplay and Pause;
- gameplay and UI maps are mutually exclusive by explicit bridge policy;
- no transient default-map activation occurs during Pause application;
- exact map-set rollback is deterministic;
- local multiplayer keeps one Global map per private PlayerInput action copy;
- IC1 single-writer and IC2 resident authority boundaries remain intact.

### Trade-offs

- bridge action assets must provide explicit Global, Player and UI maps;
- consumers must move PauseToggle to Global;
- old diagnostics that assumed `ActivatedPlayerInput == true` need to follow the
  new non-activation posture semantics;
- split-screen Pause ownership remains product policy above this physical model.

## Out of scope

```text
device/control-scheme policy
which local Player may request Pause
application-wide UI input routing
online input authority
removing historical single-map result fields
renaming the temporary Gate write-port component
removing the now-unused historical UnityPlayerInputGateBlockMode enum file
```

## Acceptance

```text
Global is required before Pause mutation
Gameplay enables exactly Global + Player
the superseded bridge enables exactly Global + UI during PauseOverlay
the current scene-local Pause product enables exactly Global during PauseOverlay
Player is disabled during Pause
UI is disabled during Gameplay for the superseded bridge
PauseToggle exists in Global and Global remains enabled
InputMode application does not call ActivateInput
action-map writes do not require PlayerInput.inputIsActive
exact map-set rollback restores the prior posture
missing Global or UI fails before bridge Pause mutation
IC1 layered writer smoke passes without PlayerInput activation/deactivation warnings
IC2 authority and runtime regression smokes pass
```
