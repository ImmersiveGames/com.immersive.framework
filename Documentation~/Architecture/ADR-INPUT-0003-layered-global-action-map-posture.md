# ADR-INPUT-0003 — Layered Global Action Map Posture

**Status:** Accepted for implementation validation  
**Type:** Technical architecture / Unity Input posture  
**Scope:** Global Pause continuity and exact InputMode action-map sets

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

The initial policies are:

```text
Gameplay     -> Global + Player
PauseOverlay -> Global + UI
FrontendMenu -> Global + UI
InputLocked  -> Global
```

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

The InputMode application path does not call `PlayerInput.ActivateInput()`.
Activation, device pairing and local-player provisioning remain owned by the
existing Player lifecycle. InputMode changes only action-map posture.

`UnityPlayerInputGateAdapter` remains the explicit write port and applies Gate
blocking as an overlay after the baseline map set is changed or restored.

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
- gameplay and UI maps are mutually exclusive by explicit policy;
- no transient default-map activation occurs during Pause application;
- exact map-set rollback is deterministic;
- local multiplayer keeps one Global map per private PlayerInput action copy;
- IC1 single-writer and IC2 resident authority boundaries remain intact.

### Trade-offs

- action assets must provide an explicit Global map;
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
```

## Acceptance

```text
Global is required before Pause mutation
Gameplay enables exactly Global + Player
PauseOverlay enables exactly Global + UI
Player is disabled during Pause
UI is disabled during Gameplay
PauseToggle exists in Global and Global remains enabled
InputMode application does not call ActivateInput
exact map-set rollback restores the prior posture
missing Global or UI fails before Pause mutation
IC1 layered writer smoke passes
IC2 authority and runtime regression smokes pass
```
